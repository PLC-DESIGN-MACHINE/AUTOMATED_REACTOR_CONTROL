using AUTOMATED_REACTOR_CONTROL_Ver4_FINAL;
using AutomatedReactorControl.Core.Caching;
using AutomatedReactorControl.Core.Memory;
using AutomatedReactorControl.Core.Visualization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Testing.Platform.Logging;
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AutomatedReactorControl.Core.Development
{
    /// <summary>
    /// Development Hot Reload Support with Live Code Updates
    /// Features: Real-time compilation, Dynamic assembly loading, State preservation, Error handling
    /// Performance: <100ms reload time, Incremental compilation, Memory-efficient reloading
    /// </summary>
    public sealed class UC1_HotReload : BackgroundService, IDisposable
    {
        private readonly ILogger<UC1_HotReload> _logger;
        private readonly HotReloadConfiguration _config;
        private readonly UC1_CacheManager _cacheManager;
        private readonly UC1_MemoryPool _memoryPool;

        // File System Monitoring
        private readonly FileSystemWatcher _fileWatcher;
        private readonly ConcurrentDictionary<string, FileChangeInfo> _pendingChanges;
        private readonly Timer _debounceTimer;

        // Compilation Pipeline
        private readonly Channel<CompilationRequest> _compilationChannel;
        private readonly ChannelWriter<CompilationRequest> _compilationWriter;
        private readonly ChannelReader<CompilationRequest> _compilationReader;

        // Assembly Management
        private readonly AssemblyManager _assemblyManager;
        private readonly StateManager _stateManager;
        private readonly TypeResolver _typeResolver;

        // Compilation Infrastructure
        private readonly IncrementalCompiler _incrementalCompiler;
        private readonly CompilationCache _compilationCache;
        private readonly DependencyAnalyzer _dependencyAnalyzer;

        // Event Streams
        private readonly Subject<FileChangeEvent> _fileChangeStream;
        private readonly Subject<CompilationEvent> _compilationStream;
        private readonly Subject<ReloadEvent> _reloadStream;
        private readonly Subject<ErrorEvent> _errorStream;

        // Performance Monitoring
        private readonly HotReloadMetrics _metrics;
        private readonly PerformanceProfiler _profiler;

        // Error Handling & Recovery
        private readonly ErrorHandler _errorHandler;
        private readonly RollbackManager _rollbackManager;
        private readonly SafetyManager _safetyManager;

        // Development Tools Integration
        private readonly DebuggerIntegration _debuggerIntegration;
        private readonly ProfilerIntegration _profilerIntegration;

        private volatile bool _disposed;
        private volatile bool _hotReloadEnabled;

        public UC1_HotReload(
            ILogger<UC1_HotReload> logger,
            IOptions<HotReloadConfiguration> config,
            UC1_CacheManager cacheManager,
            UC1_MemoryPool memoryPool)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _memoryPool = memoryPool ?? throw new ArgumentNullException(nameof(memoryPool));

            // Initialize Collections
            _pendingChanges = new ConcurrentDictionary<string, FileChangeInfo>();

            // Initialize File System Monitoring
            _fileWatcher = new FileSystemWatcher(_config.SourceDirectory)
            {
                IncludeSubdirectories = true,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };

            // Initialize Compilation Pipeline
            var channelOptions = new BoundedChannelOptions(_config.MaxCompilationQueueSize)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };

            _compilationChannel = Channel.CreateBounded<CompilationRequest>(channelOptions);
            _compilationWriter = _compilationChannel.Writer;
            _compilationReader = _compilationChannel.Reader;

            // Initialize Event Streams
            _fileChangeStream = new Subject<FileChangeEvent>();
            _compilationStream = new Subject<CompilationEvent>();
            _reloadStream = new Subject<ReloadEvent>();
            _errorStream = new Subject<ErrorEvent>();

            // Initialize Core Systems
            _assemblyManager = new AssemblyManager(_logger);
            _stateManager = new StateManager(_cacheManager);
            _typeResolver = new TypeResolver();

            // Initialize Compilation Infrastructure
            _incrementalCompiler = new IncrementalCompiler(_config.CompilationSettings, _logger);
            _compilationCache = new CompilationCache(_cacheManager, _config.CacheSettings);
            _dependencyAnalyzer = new DependencyAnalyzer(_config.DependencySettings);

            // Initialize Performance Monitoring
            _metrics = new HotReloadMetrics();
            _profiler = new PerformanceProfiler();

            // Initialize Error Handling
            _errorHandler = new ErrorHandler(_config.ErrorHandling, _logger);
            _rollbackManager = new RollbackManager(_config.RollbackSettings);
            _safetyManager = new SafetyManager(_config.SafetySettings, _logger);

            // Initialize Development Tools Integration
            _debuggerIntegration = new DebuggerIntegration(_config.DebuggerIntegration);
            _profilerIntegration = new ProfilerIntegration(_config.ProfilerIntegration);

            // Initialize Debounce Timer
            _debounceTimer = new Timer(ProcessPendingChanges, null, Timeout.Infinite, Timeout.Infinite);

            SetupFileWatcher();
            SetupEventStreams();

            _logger.LogInformation("UC1_HotReload initialized - Source Directory: {Directory}, Enabled: {Enabled}",
                _config.SourceDirectory, _config.EnableHotReload);
        }

        #region Public API

        /// <summary>
        /// Enable hot reload functionality
        /// </summary>
        public async Task<bool> EnableHotReloadAsync(CancellationToken cancellationToken = default)
        {
            if (_hotReloadEnabled)
                return true;

            try
            {
                // Validate environment
                if (!await ValidateEnvironmentAsync(cancellationToken))
                {
                    _logger.LogError("Hot reload environment validation failed");
                    return false;
                }

                // Initialize compilation environment
                await _incrementalCompiler.InitializeAsync(cancellationToken);

                // Start file monitoring
                _fileWatcher.EnableRaisingEvents = true;
                _hotReloadEnabled = true;

                _logger.LogInformation("Hot reload enabled successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable hot reload");
                return false;
            }
        }

        /// <summary>
        /// Disable hot reload functionality
        /// </summary>
        public async Task DisableHotReloadAsync(CancellationToken cancellationToken = default)
        {
            if (!_hotReloadEnabled)
                return;

            try
            {
                // Stop file monitoring
                _fileWatcher.EnableRaisingEvents = false;

                // Complete pending compilations
                _compilationWriter.Complete();

                // Save state if needed
                await _stateManager.SaveStateAsync(cancellationToken);

                _hotReloadEnabled = false;
                _logger.LogInformation("Hot reload disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling hot reload");
            }
        }

        /// <summary>
        /// Force reload of specific file
        /// </summary>
        public async Task<ReloadResult> ForceReloadAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!_hotReloadEnabled)
                return ReloadResult.Failed("Hot reload is not enabled");

            try
            {
                var startTime = DateTime.UtcNow;

                // Create compilation request
                var request = new CompilationRequest
                {
                    FilePath = filePath,
                    RequestType = CompilationRequestType.ForceReload,
                    Priority = CompilationPriority.High,
                    Timestamp = DateTime.UtcNow
                };

                // Queue compilation
                await _compilationWriter.WriteAsync(request, cancellationToken);

                // Wait for completion with timeout
                var completionTask = WaitForCompletionAsync(request.Id, _config.CompilationTimeout, cancellationToken);
                var result = await completionTask;

                var duration = DateTime.UtcNow - startTime;
                _metrics.RecordReloadTime(duration);

                return result ?? ReloadResult.Failed("Compilation timeout");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Force reload failed for file: {FilePath}", filePath);
                return ReloadResult.Failed(ex.Message);
            }
        }

        /// <summary>
        /// Get real-time file change events stream
        /// </summary>
        public IObservable<FileChangeEvent> GetFileChangeStream() => _fileChangeStream.AsObservable();

        /// <summary>
        /// Get compilation events stream
        /// </summary>
        public IObservable<CompilationEvent> GetCompilationStream() => _compilationStream.AsObservable();

        /// <summary>
        /// Get reload events stream
        /// </summary>
        public IObservable<ReloadEvent> GetReloadStream() => _reloadStream.AsObservable();

        /// <summary>
        /// Get error events stream
        /// </summary>
        public IObservable<ErrorEvent> GetErrorStream() => _errorStream.AsObservable();

        /// <summary>
        /// Get hot reload performance metrics
        /// </summary>
        public HotReloadMetrics GetMetrics() => _metrics.Clone();

        /// <summary>
        /// Get loaded assembly information
        /// </summary>
        public IEnumerable<AssemblyInfo> GetLoadedAssemblies()
        {
            return _assemblyManager.GetLoadedAssemblies();
        }

        /// <summary>
        /// Get compilation history
        /// </summary>
        public IEnumerable<CompilationHistory> GetCompilationHistory(int maxEntries = 100)
        {
            return _compilationCache.GetCompilationHistory(maxEntries);
        }

        /// <summary>
        /// Rollback to previous version
        /// </summary>
        public async Task<bool> RollbackToPreviousVersionAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            try
            {
                var rollbackResult = await _rollbackManager.RollbackAsync(filePath, cancellationToken);
                if (rollbackResult.Success)
                {
                    await ForceReloadAsync(filePath, cancellationToken);
                    _logger.LogInformation("Rollback successful for file: {FilePath}", filePath);
                }

                return rollbackResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rollback failed for file: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// Clear compilation cache
        /// </summary>
        public async Task ClearCacheAsync(CancellationToken cancellationToken = default)
        {
            await _compilationCache.ClearAsync(cancellationToken);
            _logger.LogInformation("Compilation cache cleared");
        }

        #endregion

        #region Background Processing

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hot reload background processing started");

            // Start multiple compilation processors
            var processorCount = Math.Max(1, Environment.ProcessorCount / 4);
            var processorTasks = Enumerable.Range(0, processorCount)
                .Select(i => ProcessCompilationRequestsAsync($"CompilationProcessor-{i}", stoppingToken))
                .ToArray();

            try
            {
                await Task.WhenAll(processorTasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Hot reload background processing cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hot reload background processing failed");
                throw;
            }
        }

        private async Task ProcessCompilationRequestsAsync(string processorName, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Compilation processor {ProcessorName} started", processorName);

            await foreach (var request in _compilationReader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var startTime = DateTime.UtcNow;
                    await ProcessCompilationRequestAsync(request, cancellationToken);
                    var processingTime = DateTime.UtcNow - startTime;

                    _profiler.RecordCompilationTime(processingTime);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing compilation request: {FilePath}", request.FilePath);
                    await HandleCompilationErrorAsync(request, ex, cancellationToken);
                }
            }

            _logger.LogDebug("Compilation processor {ProcessorName} stopped", processorName);
        }

        private async Task ProcessCompilationRequestAsync(CompilationRequest request, CancellationToken cancellationToken)
        {
            // Emit compilation started event
            _compilationStream.OnNext(new CompilationEvent
            {
                RequestId = request.Id,
                FilePath = request.FilePath,
                Status = CompilationStatus.Started,
                Timestamp = DateTime.UtcNow
            });

            try
            {
                // Safety checks
                if (!await _safetyManager.ValidateCompilationSafetyAsync(request, cancellationToken))
                {
                    throw new InvalidOperationException("Compilation safety validation failed");
                }

                // Check compilation cache
                var cachedResult = await _compilationCache.GetCachedCompilationAsync(request.FilePath, cancellationToken);
                if (cachedResult != null && !request.ForceRecompile)
                {
                    await ApplyCachedCompilationAsync(cachedResult, request, cancellationToken);
                    return;
                }

                // Analyze dependencies
                var dependencies = await _dependencyAnalyzer.AnalyzeDependenciesAsync(request.FilePath, cancellationToken);

                // Perform incremental compilation
                var compilationResult = await _incrementalCompiler.CompileAsync(request, dependencies, cancellationToken);

                if (compilationResult.Success)
                {
                    // Save current state before applying changes
                    await _stateManager.SaveStateSnapshotAsync(request.FilePath, cancellationToken);

                    // Apply compilation result
                    await ApplyCompilationResultAsync(compilationResult, request, cancellationToken);

                    // Cache successful compilation
                    await _compilationCache.CacheCompilationAsync(request.FilePath, compilationResult, cancellationToken);

                    // Emit success event
                    _compilationStream.OnNext(new CompilationEvent
                    {
                        RequestId = request.Id,
                        FilePath = request.FilePath,
                        Status = CompilationStatus.Succeeded,
                        Duration = compilationResult.Duration,
                        Timestamp = DateTime.UtcNow
                    });

                    _metrics.IncrementSuccessfulCompilations();
                }
                else
                {
                    await HandleCompilationFailureAsync(request, compilationResult, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleCompilationErrorAsync(request, ex, cancellationToken);
            }
        }

        private async Task ApplyCompilationResultAsync(CompilationResult compilationResult, CompilationRequest request, CancellationToken cancellationToken)
        {
            // Load new assembly
            var assembly = await _assemblyManager.LoadAssemblyAsync(compilationResult.AssemblyBytes, cancellationToken);

            // Preserve state if needed
            var preservedState = await _stateManager.PreserveStateAsync(request.FilePath, cancellationToken);

            // Update type mappings
            await _typeResolver.UpdateTypeMappingsAsync(assembly, cancellationToken);

            // Restore state
            if (preservedState != null)
            {
                await _stateManager.RestoreStateAsync(preservedState, assembly, cancellationToken);
            }

            // Notify integrations
            await _debuggerIntegration.NotifyReloadAsync(assembly, cancellationToken);
            await _profilerIntegration.NotifyReloadAsync(assembly, cancellationToken);

            // Emit reload event
            _reloadStream.OnNext(new ReloadEvent
            {
                FilePath = request.FilePath,
                AssemblyName = assembly.GetName().Name,
                Success = true,
                LoadTime = compilationResult.Duration,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Hot reload successful for file: {FilePath}", request.FilePath);
        }

        private async Task ApplyCachedCompilationAsync(CachedCompilation cachedResult, CompilationRequest request, CancellationToken cancellationToken)
        {
            // Load cached assembly
            var assembly = await _assemblyManager.LoadAssemblyAsync(cachedResult.AssemblyBytes, cancellationToken);

            // Update type mappings
            await _typeResolver.UpdateTypeMappingsAsync(assembly, cancellationToken);

            // Emit reload event
            _reloadStream.OnNext(new ReloadEvent
            {
                FilePath = request.FilePath,
                AssemblyName = assembly.GetName().Name,
                Success = true,
                LoadTime = TimeSpan.Zero,
                FromCache = true,
                Timestamp = DateTime.UtcNow
            });

            _metrics.IncrementCacheHits();
            _logger.LogDebug("Used cached compilation for file: {FilePath}", request.FilePath);
        }

        #endregion

        #region File System Monitoring

        private void SetupFileWatcher()
        {
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Deleted += OnFileChanged;
            _fileWatcher.Renamed += OnFileRenamed;
            _fileWatcher.Error += OnFileWatcherError;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!_hotReloadEnabled || !IsMonitoredFile(e.FullPath))
                return;

            var changeInfo = new FileChangeInfo
            {
                FilePath = e.FullPath,
                ChangeType = e.ChangeType,
                Timestamp = DateTime.UtcNow
            };

            _pendingChanges.AddOrUpdate(e.FullPath, changeInfo, (key, existing) => changeInfo);

            // Emit file change event
            _fileChangeStream.OnNext(new FileChangeEvent
            {
                FilePath = e.FullPath,
                ChangeType = e.ChangeType,
                Timestamp = DateTime.UtcNow
            });

            // Restart debounce timer
            _debounceTimer.Change(_config.DebounceDelay, Timeout.InfiniteTimeSpan);
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (!_hotReloadEnabled)
                return;

            _logger.LogInformation("File renamed: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);
            OnFileChanged(sender, e);
        }

        private void OnFileWatcherError(object sender, ErrorEventArgs e)
        {
            _logger.LogError(e.GetException(), "File watcher error");

            // Emit error event
            _errorStream.OnNext(new ErrorEvent
            {
                ErrorType = ErrorType.FileWatcherError,
                Message = e.GetException().Message,
                Timestamp = DateTime.UtcNow
            });
        }

        private bool IsMonitoredFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return _config.MonitoredExtensions.Contains(extension);
        }

        private void ProcessPendingChanges(object state)
        {
            if (!_hotReloadEnabled || _pendingChanges.IsEmpty)
                return;

            var changes = _pendingChanges.ToArray();
            _pendingChanges.Clear();

            _ = Task.Run(async () =>
            {
                foreach (var change in changes)
                {
                    try
                    {
                        var request = new CompilationRequest
                        {
                            FilePath = change.Value.FilePath,
                            RequestType = CompilationRequestType.FileChange,
                            Priority = DetermineCompilationPriority(change.Value),
                            Timestamp = DateTime.UtcNow
                        };

                        await _compilationWriter.WriteAsync(request);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error queuing compilation for file: {FilePath}", change.Value.FilePath);
                    }
                }
            });
        }

        private CompilationPriority DetermineCompilationPriority(FileChangeInfo changeInfo)
        {
            // Determine priority based on file type and change frequency
            var extension = Path.GetExtension(changeInfo.FilePath).ToLowerInvariant();

            return extension switch
            {
                ".cs" => CompilationPriority.High,
                ".razor" => CompilationPriority.High,
                ".xaml" => CompilationPriority.Medium,
                ".json" => CompilationPriority.Low,
                _ => CompilationPriority.Normal
            };
        }

        #endregion

        #region Helper Methods

        private async Task<bool> ValidateEnvironmentAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Check if source directory exists
                if (!Directory.Exists(_config.SourceDirectory))
                {
                    _logger.LogError("Source directory does not exist: {Directory}", _config.SourceDirectory);
                    return false;
                }

                // Check compilation environment
                var compilationValid = await _incrementalCompiler.ValidateEnvironmentAsync(cancellationToken);
                if (!compilationValid)
                {
                    _logger.LogError("Compilation environment validation failed");
                    return false;
                }

                // Check safety settings
                if (!_safetyManager.ValidateSafetySettings())
                {
                    _logger.LogError("Safety settings validation failed");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Environment validation error");
                return false;
            }
        }

        private async Task<ReloadResult> WaitForCompletionAsync(string requestId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                var completionSource = new TaskCompletionSource<ReloadResult>();

                // Subscribe to completion events
                var subscription = _reloadStream
                    .Where(evt => evt.FilePath.Contains(requestId) || evt.AssemblyName.Contains(requestId))
                    .Take(1)
                    .Subscribe(evt =>
                    {
                        var result = evt.Success
                            ? ReloadResult.Success(evt.LoadTime, evt.FromCache)
                            : ReloadResult.Failed("Reload failed");

                        completionSource.TrySetResult(result);
                    });

                using (subscription)
                {
                    return await completionSource.Task;
                }
            }
            catch (OperationCanceledException)
            {
                return null; // Timeout
            }
        }

        private async Task HandleCompilationFailureAsync(CompilationRequest request, CompilationResult result, CancellationToken cancellationToken)
        {
            _metrics.IncrementFailedCompilations();

            // Emit compilation failed event
            _compilationStream.OnNext(new CompilationEvent
            {
                RequestId = request.Id,
                FilePath = request.FilePath,
                Status = CompilationStatus.Failed,
                Errors = result.Errors,
                Timestamp = DateTime.UtcNow
            });

            // Emit error event
            _errorStream.OnNext(new ErrorEvent
            {
                ErrorType = ErrorType.CompilationError,
                Message = string.Join(Environment.NewLine, result.Errors),
                FilePath = request.FilePath,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogWarning("Compilation failed for file: {FilePath}. Errors: {Errors}",
                request.FilePath, string.Join(", ", result.Errors));
        }

        private async Task HandleCompilationErrorAsync(CompilationRequest request, Exception exception, CancellationToken cancellationToken)
        {
            _metrics.IncrementCompilationErrors();

            // Handle with error handler
            await _errorHandler.HandleErrorAsync(exception, request, cancellationToken);

            // Emit error event
            _errorStream.OnNext(new ErrorEvent
            {
                ErrorType = ErrorType.SystemError,
                Message = exception.Message,
                FilePath = request.FilePath,
                Exception = exception,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogError(exception, "Compilation error for file: {FilePath}", request.FilePath);
        }

        private void SetupEventStreams()
        {
            // Setup file change throttling
            _fileChangeStream
                .GroupBy(evt => evt.FilePath)
                .Subscribe(group =>
                {
                    group.Throttle(TimeSpan.FromMilliseconds(500))
                         .Subscribe(evt =>
                         {
                             _logger.LogDebug("File changed: {FilePath}", evt.FilePath);
                         });
                });

            // Setup error event handling
            _errorStream
                .Where(evt => evt.ErrorType == ErrorType.SystemError)
                .Subscribe(evt =>
                {
                    _logger.LogError("Hot reload system error: {Message}", evt.Message);
                });

            // Setup compilation event monitoring
            _compilationStream
                .Where(evt => evt.Status == CompilationStatus.Failed)
                .Buffer(TimeSpan.FromMinutes(1))
                .Subscribe(failedCompilations =>
                {
                    if (failedCompilations.Count > _config.MaxFailedCompilationsPerMinute)
                    {
                        _logger.LogWarning("High compilation failure rate detected: {Count} failures in 1 minute",
                            failedCompilations.Count);
                    }
                });
        }

        #endregion

        #region IDisposable

        public override void Dispose()
        {
            if (_disposed) return;

            _compilationWriter.Complete();
            _fileWatcher?.Dispose();
            _debounceTimer?.Dispose();

            // Dispose event streams
            _fileChangeStream?.Dispose();
            _compilationStream?.Dispose();
            _reloadStream?.Dispose();
            _errorStream?.Dispose();

            // Dispose core systems
            _assemblyManager?.Dispose();
            _stateManager?.Dispose();
            _incrementalCompiler?.Dispose();
            _compilationCache?.Dispose();
            _errorHandler?.Dispose();
            _rollbackManager?.Dispose();

            base.Dispose();
            _disposed = true;

            _logger.LogInformation("UC1_HotReload disposed");
        }

        #endregion
    }

    #region Supporting Classes and Enums

    public enum CompilationRequestType
    {
        FileChange, ForceReload, DependencyUpdate
    }

    public enum CompilationPriority
    {
        Low, Normal, High, Critical
    }

    public enum CompilationStatus
    {
        Queued, Started, Succeeded, Failed, Cancelled
    }

    public enum ErrorType
    {
        CompilationError, SystemError, FileWatcherError, SafetyViolation
    }

    // Configuration and Data Classes
    public class HotReloadConfiguration
    {
        public bool EnableHotReload { get; set; } = true;
        public string SourceDirectory { get; set; } = "src";
        public int MaxCompilationQueueSize { get; set; } = 1000;
        public TimeSpan DebounceDelay { get; set; } = TimeSpan.FromMilliseconds(300);
        public TimeSpan CompilationTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public int MaxFailedCompilationsPerMinute { get; set; } = 10;
        public HashSet<string> MonitoredExtensions { get; set; } = new() { ".cs", ".razor", ".xaml" };
        public CompilationSettings CompilationSettings { get; set; } = new();
        public CacheSettings CacheSettings { get; set; } = new();
        public DependencySettings DependencySettings { get; set; } = new();
        public ErrorHandlingSettings ErrorHandling { get; set; } = new();
        public RollbackSettings RollbackSettings { get; set; } = new();
        public SafetySettings SafetySettings { get; set; } = new();
        public DebuggerIntegrationSettings DebuggerIntegration { get; set; } = new();
        public ProfilerIntegrationSettings ProfilerIntegration { get; set; } = new();
    }

    // Additional supporting classes would be implemented...
    // This is a simplified version showing the main structure

    #endregion
}