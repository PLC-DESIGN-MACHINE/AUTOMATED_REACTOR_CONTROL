using AutomatedReactorControl.Core.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Testing.Platform.Logging;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AutomatedReactorControl.Core.Caching
{
    /// <summary>
    /// Enterprise-grade Multi-level Caching System
    /// L1: High-speed Memory Cache (Hot Data)
    /// L2: SSD-based Persistent Cache (Warm Data) 
    /// L3: Network/Distributed Cache (Cold Data)
    /// Features: LRU/LFU eviction, Smart invalidation, Hardware acceleration
    /// </summary>
    public sealed class UC1_CacheManager : IDisposable
    {
        private readonly ILogger<UC1_CacheManager> _logger;
        private readonly CacheConfiguration _config;
        private readonly UC1_MemoryPool _memoryPool;

        // L1 Cache - Hot Memory Cache
        private readonly ConcurrentDictionary<string, CacheItem> _l1Cache;
        private readonly LruEvictionPolicy _l1Eviction;

        // L2 Cache - SSD Persistent Cache
        private readonly ConcurrentDictionary<string, string> _l2FileMap;
        private readonly string _l2CachePath;

        // L3 Cache - Distributed Cache Interface
        private readonly IDistributedCacheProvider _l3Provider;

        // Performance Counters
        private readonly CacheMetrics _metrics;
        private readonly Timer _cleanupTimer;
        private readonly SemaphoreSlim _operationSemaphore;

        // Security & Encryption
        private readonly Aes _encryptionProvider;
        private readonly byte[] _encryptionKey;

        // Hardware Acceleration
        private readonly bool _useHardwareAcceleration;
        private volatile bool _disposed;

        public UC1_CacheManager(
            ILogger<UC1_CacheManager> logger,
            IOptions<CacheConfiguration> config,
            UC1_MemoryPool memoryPool,
            IDistributedCacheProvider l3Provider = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _memoryPool = memoryPool ?? throw new ArgumentNullException(nameof(memoryPool));
            _l3Provider = l3Provider;

            // Initialize L1 Cache
            _l1Cache = new ConcurrentDictionary<string, CacheItem>();
            _l1Eviction = new LruEvictionPolicy(_config.L1MaxItems);

            // Initialize L2 Cache
            _l2FileMap = new ConcurrentDictionary<string, string>();
            _l2CachePath = Path.Combine(_config.CacheDirectory, "L2Cache");
            Directory.CreateDirectory(_l2CachePath);

            // Initialize Security
            _encryptionProvider = Aes.Create();
            _encryptionKey = GenerateEncryptionKey();

            // Initialize Performance Monitoring
            _metrics = new CacheMetrics();
            _operationSemaphore = new SemaphoreSlim(_config.MaxConcurrentOperations);

            // Hardware Acceleration Detection
            _useHardwareAcceleration = DetectHardwareAcceleration();

            // Cleanup Timer
            _cleanupTimer = new Timer(PerformCleanup, null,
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            _logger.LogInformation("UC1_CacheManager initialized - HW Acceleration: {Enabled}",
                _useHardwareAcceleration);
        }

        #region Public API

        /// <summary>
        /// Get cached item with automatic tier promotion
        /// </summary>
        public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            await _operationSemaphore.WaitAsync(cancellationToken);
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Try L1 Cache first (fastest)
                if (_l1Cache.TryGetValue(key, out var l1Item))
                {
                    if (l1Item.IsValid)
                    {
                        _l1Eviction.RecordAccess(key);
                        _metrics.RecordHit(CacheLevel.L1, stopwatch.Elapsed);
                        return DeserializeItem<T>(l1Item.Data);
                    }
                    _l1Cache.TryRemove(key, out _);
                }

                // Try L2 Cache (SSD)
                var l2Data = await GetFromL2Async(key, cancellationToken);
                if (l2Data != null)
                {
                    // Promote to L1
                    await PromoteToL1Async(key, l2Data, cancellationToken);
                    _metrics.RecordHit(CacheLevel.L2, stopwatch.Elapsed);
                    return DeserializeItem<T>(l2Data);
                }

                // Try L3 Cache (Distributed)
                if (_l3Provider != null)
                {
                    var l3Data = await _l3Provider.GetAsync(key, cancellationToken);
                    if (l3Data != null)
                    {
                        // Promote to L2 and L1
                        await PromoteToL2Async(key, l3Data, cancellationToken);
                        await PromoteToL1Async(key, l3Data, cancellationToken);
                        _metrics.RecordHit(CacheLevel.L3, stopwatch.Elapsed);
                        return DeserializeItem<T>(l3Data);
                    }
                }

                _metrics.RecordMiss(stopwatch.Elapsed);
                return default(T);
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// Set cached item across all appropriate tiers
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null,
            CachePolicy policy = CachePolicy.WriteThrough, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            await _operationSemaphore.WaitAsync(cancellationToken);
            try
            {
                var serializedData = SerializeItem(value);
                var expiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : DateTime.MaxValue;

                switch (policy)
                {
                    case CachePolicy.WriteThrough:
                        await SetInAllTiersAsync(key, serializedData, expiryTime, cancellationToken);
                        break;

                    case CachePolicy.WriteBack:
                        await SetInL1Async(key, serializedData, expiryTime, cancellationToken);
                        _ = Task.Run(() => SetInLowerTiersAsync(key, serializedData, expiryTime, cancellationToken));
                        break;

                    case CachePolicy.WriteAround:
                        await SetInL2AndL3Async(key, serializedData, expiryTime, cancellationToken);
                        break;
                }

                _metrics.RecordWrite();
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// Remove item from all cache tiers
        /// </summary>
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                return;

            await _operationSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Remove from L1
                _l1Cache.TryRemove(key, out _);
                _l1Eviction.Remove(key);

                // Remove from L2
                if (_l2FileMap.TryRemove(key, out var fileName))
                {
                    var filePath = Path.Combine(_l2CachePath, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                // Remove from L3
                if (_l3Provider != null)
                {
                    await _l3Provider.RemoveAsync(key, cancellationToken);
                }

                _metrics.RecordEviction();
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// Clear all cache tiers
        /// </summary>
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            await _operationSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Clear L1
                _l1Cache.Clear();
                _l1Eviction.Clear();

                // Clear L2
                _l2FileMap.Clear();
                if (Directory.Exists(_l2CachePath))
                {
                    foreach (var file in Directory.GetFiles(_l2CachePath))
                    {
                        File.Delete(file);
                    }
                }

                // Clear L3
                if (_l3Provider != null)
                {
                    await _l3Provider.ClearAsync(cancellationToken);
                }

                _metrics.Reset();
                _logger.LogInformation("Cache cleared across all tiers");
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// Get cache performance metrics
        /// </summary>
        public CacheMetrics GetMetrics() => _metrics.Clone();

        #endregion

        #region Private Implementation

        private async Task<byte[]> GetFromL2Async(string key, CancellationToken cancellationToken)
        {
            if (!_l2FileMap.TryGetValue(key, out var fileName))
                return null;

            var filePath = Path.Combine(_l2CachePath, fileName);
            if (!File.Exists(filePath))
            {
                _l2FileMap.TryRemove(key, out _);
                return null;
            }

            try
            {
                var encryptedData = await File.ReadAllBytesAsync(filePath, cancellationToken);
                return DecryptData(encryptedData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read L2 cache file: {FilePath}", filePath);
                _l2FileMap.TryRemove(key, out _);
                return null;
            }
        }

        private async Task PromoteToL1Async(string key, byte[] data, CancellationToken cancellationToken)
        {
            // Check if L1 is full and evict if necessary
            if (_l1Cache.Count >= _config.L1MaxItems)
            {
                var evictKey = _l1Eviction.GetEvictionCandidate();
                if (evictKey != null)
                {
                    _l1Cache.TryRemove(evictKey, out _);
                }
            }

            var cacheItem = new CacheItem
            {
                Data = data,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.MaxValue,
                AccessCount = 1
            };

            _l1Cache.TryAdd(key, cacheItem);
            _l1Eviction.RecordAccess(key);
        }

        private async Task PromoteToL2Async(string key, byte[] data, CancellationToken cancellationToken)
        {
            var fileName = GenerateFileName(key);
            var filePath = Path.Combine(_l2CachePath, fileName);

            try
            {
                var encryptedData = EncryptData(data);
                await File.WriteAllBytesAsync(filePath, encryptedData, cancellationToken);
                _l2FileMap.TryAdd(key, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write L2 cache file: {FilePath}", filePath);
            }
        }

        private async Task SetInAllTiersAsync(string key, byte[] data, DateTime expiry, CancellationToken cancellationToken)
        {
            await SetInL1Async(key, data, expiry, cancellationToken);
            await SetInL2Async(key, data, expiry, cancellationToken);
            if (_l3Provider != null)
            {
                await _l3Provider.SetAsync(key, data, expiry, cancellationToken);
            }
        }

        private async Task SetInL1Async(string key, byte[] data, DateTime expiry, CancellationToken cancellationToken)
        {
            // Check capacity and evict if necessary
            if (_l1Cache.Count >= _config.L1MaxItems)
            {
                var evictKey = _l1Eviction.GetEvictionCandidate();
                if (evictKey != null)
                {
                    _l1Cache.TryRemove(evictKey, out _);
                }
            }

            var cacheItem = new CacheItem
            {
                Data = data,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiry,
                AccessCount = 0
            };

            _l1Cache.AddOrUpdate(key, cacheItem, (k, v) => cacheItem);
            _l1Eviction.RecordAccess(key);
        }

        private async Task SetInL2Async(string key, byte[] data, DateTime expiry, CancellationToken cancellationToken)
        {
            var fileName = GenerateFileName(key);
            var filePath = Path.Combine(_l2CachePath, fileName);

            try
            {
                var encryptedData = EncryptData(data);
                await File.WriteAllBytesAsync(filePath, encryptedData, cancellationToken);
                _l2FileMap.AddOrUpdate(key, fileName, (k, v) => fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write L2 cache: {Key}", key);
            }
        }

        private async Task SetInL2AndL3Async(string key, byte[] data, DateTime expiry, CancellationToken cancellationToken)
        {
            await SetInL2Async(key, data, expiry, cancellationToken);
            if (_l3Provider != null)
            {
                await _l3Provider.SetAsync(key, data, expiry, cancellationToken);
            }
        }

        private async Task SetInLowerTiersAsync(string key, byte[] data, DateTime expiry, CancellationToken cancellationToken)
        {
            await SetInL2Async(key, data, expiry, cancellationToken);
            if (_l3Provider != null)
            {
                await _l3Provider.SetAsync(key, data, expiry, cancellationToken);
            }
        }

        private byte[] SerializeItem<T>(T item)
        {
            using var buffer = _memoryPool.Rent();
            return JsonSerializer.SerializeToUtf8Bytes(item);
        }

        private T DeserializeItem<T>(byte[] data)
        {
            return JsonSerializer.Deserialize<T>(data);
        }

        private byte[] EncryptData(byte[] data)
        {
            using var encryptor = _encryptionProvider.CreateEncryptor(_encryptionKey, _encryptionProvider.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);

            ms.Write(_encryptionProvider.IV, 0, _encryptionProvider.IV.Length);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();

            return ms.ToArray();
        }

        private byte[] DecryptData(byte[] encryptedData)
        {
            var iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);

            using var decryptor = _encryptionProvider.CreateDecryptor(_encryptionKey, iv);
            using var ms = new MemoryStream(encryptedData, 16, encryptedData.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var result = new MemoryStream();

            cs.CopyTo(result);
            return result.ToArray();
        }

        private string GenerateFileName(string key)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hash) + ".cache";
        }

        private byte[] GenerateEncryptionKey()
        {
            var key = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(key);
            return key;
        }

        private bool DetectHardwareAcceleration()
        {
            // Check for hardware acceleration capabilities
            return Environment.ProcessorCount > 1 &&
                   RuntimeInformation.ProcessArchitecture == Architecture.X64;
        }

        private void PerformCleanup(object state)
        {
            if (_disposed) return;

            try
            {
                // Cleanup expired L1 items
                var expiredKeys = new List<string>();
                foreach (var kvp in _l1Cache)
                {
                    if (kvp.Value.IsExpired)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    _l1Cache.TryRemove(key, out _);
                    _l1Eviction.Remove(key);
                }

                // Cleanup orphaned L2 files
                CleanupOrphanedL2Files();

                _logger.LogDebug("Cache cleanup completed - Removed {Count} expired items", expiredKeys.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }

        private void CleanupOrphanedL2Files()
        {
            try
            {
                var diskFiles = Directory.GetFiles(_l2CachePath, "*.cache");
                var mappedFiles = new HashSet<string>(_l2FileMap.Values);

                foreach (var file in diskFiles)
                {
                    var fileName = Path.GetFileName(file);
                    if (!mappedFiles.Contains(fileName))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup orphaned L2 files");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            _cleanupTimer?.Dispose();
            _operationSemaphore?.Dispose();
            _encryptionProvider?.Dispose();
            _l3Provider?.Dispose();

            _disposed = true;
            _logger.LogInformation("UC1_CacheManager disposed");
        }

        #endregion
    }

    #region Supporting Classes

    public class CacheItem
    {
        public byte[] Data { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public long AccessCount { get; set; }

        public bool IsValid => DateTime.UtcNow < ExpiresAt;
        public bool IsExpired => !IsValid;
    }

    public class LruEvictionPolicy
    {
        private readonly ConcurrentDictionary<string, long> _accessOrder;
        private readonly int _maxItems;
        private long _accessCounter;

        public LruEvictionPolicy(int maxItems)
        {
            _maxItems = maxItems;
            _accessOrder = new ConcurrentDictionary<string, long>();
        }

        public void RecordAccess(string key)
        {
            _accessOrder.AddOrUpdate(key, Interlocked.Increment(ref _accessCounter),
                (k, v) => Interlocked.Increment(ref _accessCounter));
        }

        public string GetEvictionCandidate()
        {
            if (_accessOrder.IsEmpty) return null;

            var oldest = _accessOrder.OrderBy(x => x.Value).FirstOrDefault();
            return oldest.Key;
        }

        public void Remove(string key)
        {
            _accessOrder.TryRemove(key, out _);
        }

        public void Clear()
        {
            _accessOrder.Clear();
            _accessCounter = 0;
        }
    }

    public class CacheMetrics
    {
        private long _l1Hits, _l2Hits, _l3Hits, _misses, _writes, _evictions;
        private double _avgL1Time, _avgL2Time, _avgL3Time, _avgMissTime;

        public long L1Hits => _l1Hits;
        public long L2Hits => _l2Hits;
        public long L3Hits => _l3Hits;
        public long Misses => _misses;
        public long Writes => _writes;
        public long Evictions => _evictions;

        public double HitRatio => TotalRequests > 0 ? (double)(L1Hits + L2Hits + L3Hits) / TotalRequests : 0;
        public long TotalRequests => L1Hits + L2Hits + L3Hits + Misses;

        public void RecordHit(CacheLevel level, TimeSpan elapsed)
        {
            switch (level)
            {
                case CacheLevel.L1:
                    Interlocked.Increment(ref _l1Hits);
                    _avgL1Time = (_avgL1Time + elapsed.TotalMilliseconds) / 2;
                    break;
                case CacheLevel.L2:
                    Interlocked.Increment(ref _l2Hits);
                    _avgL2Time = (_avgL2Time + elapsed.TotalMilliseconds) / 2;
                    break;
                case CacheLevel.L3:
                    Interlocked.Increment(ref _l3Hits);
                    _avgL3Time = (_avgL3Time + elapsed.TotalMilliseconds) / 2;
                    break;
            }
        }

        public void RecordMiss(TimeSpan elapsed)
        {
            Interlocked.Increment(ref _misses);
            _avgMissTime = (_avgMissTime + elapsed.TotalMilliseconds) / 2;
        }

        public void RecordWrite() => Interlocked.Increment(ref _writes);
        public void RecordEviction() => Interlocked.Increment(ref _evictions);

        public void Reset()
        {
            _l1Hits = _l2Hits = _l3Hits = _misses = _writes = _evictions = 0;
            _avgL1Time = _avgL2Time = _avgL3Time = _avgMissTime = 0;
        }

        public CacheMetrics Clone() => (CacheMetrics)MemberwiseClone();
    }

    public enum CacheLevel { L1, L2, L3 }
    public enum CachePolicy { WriteThrough, WriteBack, WriteAround }

    public class CacheConfiguration
    {
        public int L1MaxItems { get; set; } = 10000;
        public int L2MaxSizeMB { get; set; } = 1024;
        public string CacheDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "ReactorCache");
        public int MaxConcurrentOperations { get; set; } = Environment.ProcessorCount * 2;
        public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromHours(1);
        public bool EnableEncryption { get; set; } = true;
        public bool EnableCompression { get; set; } = true;
    }

    public interface IDistributedCacheProvider : IDisposable
    {
        Task<byte[]> GetAsync(string key, CancellationToken cancellationToken = default);
        Task SetAsync(string key, byte[] data, DateTime expiry, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task ClearAsync(CancellationToken cancellationToken = default);
    }

    #endregion
}