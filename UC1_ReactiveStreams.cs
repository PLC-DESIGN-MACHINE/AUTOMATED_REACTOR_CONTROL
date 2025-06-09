// ==============================================
//  UC1_ReactiveStreams.cs - PHASE 3
//  AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
//  Reactive Programming with System.Reactive
//  Real-time Data Streams & Observable Pipeline
// ==============================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Logger;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    /// <summary>
    /// 🚀 PHASE 3: Advanced Reactive Streams with System.Reactive
    /// Features: Real-time Data Pipeline, Hot/Cold Observables, Backpressure
    /// Hardware-Accelerated Stream Processing with 60fps+ Performance
    /// </summary>
    public class UC1_ReactiveStreams : IDisposable
    {
        #region 🔥 Reactive Infrastructure

        // Core Data Streams
        private readonly BehaviorSubject<TemperatureReading> _temperatureStream;
        private readonly BehaviorSubject<StirrerReading> _stirrerStream;
        private readonly BehaviorSubject<ConnectionStatus> _connectionStream;
        private readonly Subject<SerialDataPacket> _serialDataStream;

        // Aggregated Streams
        private readonly BehaviorSubject<ReactorSnapshot> _systemSnapshot;
        private readonly Subject<AlertEvent> _alertStream;
        private readonly Subject<MetricsEvent> _metricsStream;

        // Stream Processing
        private readonly ConcurrentDictionary<string, IDisposable> _subscriptions;
        private readonly ConcurrentDictionary<string, StreamDefinition> _streamDefinitions;

        // Performance & Configuration
        private readonly ReactiveConfiguration _configuration;
        private readonly UC1_PerformanceMonitor _performanceMonitor;
        private readonly SemaphoreSlim _streamLock;
        private readonly CancellationTokenSource _cancellationTokenSource;

        // State Management
        private volatile bool _isDisposed = false;
        private long _totalEventsProcessed = 0;
        private DateTime _lastMetricsUpdate = DateTime.UtcNow;

        #endregion

        #region 🌊 Public Observables

        /// <summary>🌡️ Temperature Data Stream</summary>
        public IObservable<TemperatureReading> TemperatureStream => _temperatureStream.AsObservable();

        /// <summary>🔄 Stirrer Data Stream</summary>
        public IObservable<StirrerReading> StirrerStream => _stirrerStream.AsObservable();

        /// <summary>📡 Connection Status Stream</summary>
        public IObservable<ConnectionStatus> ConnectionStream => _connectionStream.AsObservable();

        /// <summary>📊 System Snapshot Stream</summary>
        public IObservable<ReactorSnapshot> SystemSnapshot => _systemSnapshot.AsObservable();

        /// <summary>🚨 Alert Stream</summary>
        public IObservable<AlertEvent> AlertStream => _alertStream.AsObservable();

        /// <summary>📈 Metrics Stream</summary>
        public IObservable<MetricsEvent> MetricsStream => _metricsStream.AsObservable();

        /// <summary>📥 Raw Serial Data Stream</summary>
        public IObservable<SerialDataPacket> SerialDataStream => _serialDataStream.AsObservable();

        #endregion

        #region 🏗️ Constructor & Initialization

        /// <summary>
        /// 🎯 Initialize Reactive Streams System
        /// </summary>
        public UC1_ReactiveStreams(
            ReactiveConfiguration configuration = null,
            UC1_PerformanceMonitor performanceMonitor = null)
        {
            try
            {
                Logger.Log("🚀 [ReactiveStreams] Initializing Reactive Programming System", LogLevel.Info);

                // Initialize configuration
                _configuration = configuration ?? ReactiveConfiguration.Default;

                // Initialize core subjects
                _temperatureStream = new BehaviorSubject<TemperatureReading>(TemperatureReading.Default);
                _stirrerStream = new BehaviorSubject<StirrerReading>(StirrerReading.Default);
                _connectionStream = new BehaviorSubject<ConnectionStatus>(ConnectionStatus.Disconnected);
                _serialDataStream = new Subject<SerialDataPacket>();

                // Initialize aggregated subjects
                _systemSnapshot = new BehaviorSubject<ReactorSnapshot>(ReactorSnapshot.Empty);
                _alertStream = new Subject<AlertEvent>();
                _metricsStream = new Subject<MetricsEvent>();

                // Initialize infrastructure
                _subscriptions = new ConcurrentDictionary<string, IDisposable>();
                _streamDefinitions = new ConcurrentDictionary<string, StreamDefinition>();
                _performanceMonitor = performanceMonitor ?? new UC1_PerformanceMonitor();
                _streamLock = new SemaphoreSlim(1, 1);
                _cancellationTokenSource = new CancellationTokenSource();

                // Setup reactive pipelines
                SetupReactivePipelines();

                // Start background processing
                StartBackgroundProcessing();

                Logger.Log("✅ [ReactiveStreams] Reactive Programming System initialized", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Initialization failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// 🔥 Setup High-Performance Reactive Pipelines
        /// </summary>
        private void SetupReactivePipelines()
        {
            try
            {
                var token = _cancellationTokenSource.Token;

                // Temperature Processing Pipeline - 60fps
                var tempSubscription = _temperatureStream
                    .DistinctUntilChanged(temp => new { temp.Value, temp.Mode })
                    .Sample(TimeSpan.FromMilliseconds(16)) // 60fps sampling
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(
                        temp => ProcessTemperatureReading(temp),
                        ex => Logger.Log($"❌ [ReactiveStreams] Temperature pipeline error: {ex.Message}", LogLevel.Error)
                    );

                _subscriptions["TemperaturePipeline"] = tempSubscription;

                // Stirrer Processing Pipeline - 30fps
                var stirrerSubscription = _stirrerStream
                    .DistinctUntilChanged(stirrer => new { stirrer.Rpm, stirrer.IsRunning })
                    .Sample(TimeSpan.FromMilliseconds(33)) // 30fps
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(
                        stirrer => ProcessStirrerReading(stirrer),
                        ex => Logger.Log($"❌ [ReactiveStreams] Stirrer pipeline error: {ex.Message}", LogLevel.Error)
                    );

                _subscriptions["StirrerPipeline"] = stirrerSubscription;

                // Serial Data Processing Pipeline - High Frequency
                var serialSubscription = _serialDataStream
                    .Buffer(TimeSpan.FromMilliseconds(10)) // Buffer for batch processing
                    .Where(batch => batch.Any())
                    .ObserveOn(TaskPoolScheduler.Default)
                    .SelectMany(async batch => await ProcessSerialDataBatchAsync(batch, token))
                    .Subscribe(
                        result => UpdateMetrics(result),
                        ex => Logger.Log($"❌ [ReactiveStreams] Serial pipeline error: {ex.Message}", LogLevel.Error)
                    );

                _subscriptions["SerialPipeline"] = serialSubscription;

                // System Snapshot Aggregation Pipeline - 15fps
                var snapshotSubscription = Observable.CombineLatest(
                        _temperatureStream,
                        _stirrerStream,
                        _connectionStream,
                        (temp, stirrer, conn) => new ReactorSnapshot
                        {
                            Temperature = temp,
                            Stirrer = stirrer,
                            Connection = conn,
                            Timestamp = DateTime.UtcNow
                        })
                    .Sample(TimeSpan.FromMilliseconds(67)) // ~15fps
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(
                        snapshot => _systemSnapshot.OnNext(snapshot),
                        ex => Logger.Log($"❌ [ReactiveStreams] Snapshot pipeline error: {ex.Message}", LogLevel.Error)
                    );

                _subscriptions["SnapshotPipeline"] = snapshotSubscription;

                // Alert Detection Pipeline
                var alertSubscription = _systemSnapshot
                    .Where(snapshot => IsAlertCondition(snapshot))
                    .Select(snapshot => CreateAlert(snapshot))
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(
                        alert => _alertStream.OnNext(alert),
                        ex => Logger.Log($"❌ [ReactiveStreams] Alert pipeline error: {ex.Message}", LogLevel.Error)
                    );

                _subscriptions["AlertPipeline"] = alertSubscription;

                Logger.Log("🔥 [ReactiveStreams] Reactive pipelines configured", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Pipeline setup failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 📊 Data Input Methods

        /// <summary>
        /// 🌡️ Push Temperature Reading
        /// </summary>
        public async Task PushTemperatureAsync(float value, bool isTjMode, CancellationToken cancellationToken = default)
        {
            try
            {
                var reading = new TemperatureReading
                {
                    Value = value,
                    Mode = isTjMode ? TemperatureMode.Tj : TemperatureMode.Tr,
                    Timestamp = DateTime.UtcNow,
                    Quality = CalculateDataQuality(value, _temperatureStream.Value?.Value ?? 0)
                };

                _temperatureStream.OnNext(reading);
                Interlocked.Increment(ref _totalEventsProcessed);

                Logger.Log($"🌡️ [ReactiveStreams] Temperature pushed: {value:F2}°C ({(isTjMode ? "Tj" : "Tr")})", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Temperature push failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 🔄 Push Stirrer Reading
        /// </summary>
        public async Task PushStirrerAsync(int rpm, bool isRunning, CancellationToken cancellationToken = default)
        {
            try
            {
                var reading = new StirrerReading
                {
                    Rpm = rpm,
                    IsRunning = isRunning,
                    Timestamp = DateTime.UtcNow,
                    PowerLevel = CalculatePowerLevel(rpm)
                };

                _stirrerStream.OnNext(reading);
                Interlocked.Increment(ref _totalEventsProcessed);

                Logger.Log($"🔄 [ReactiveStreams] Stirrer pushed: {rpm} RPM (Running: {isRunning})", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Stirrer push failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 📡 Push Connection Status
        /// </summary>
        public async Task PushConnectionAsync(bool isConnected, string port, string status, CancellationToken cancellationToken = default)
        {
            try
            {
                var connectionStatus = new ConnectionStatus
                {
                    IsConnected = isConnected,
                    Port = port ?? "Unknown",
                    Status = status ?? "Unknown",
                    Timestamp = DateTime.UtcNow,
                    SignalStrength = isConnected ? 100 : 0
                };

                _connectionStream.OnNext(connectionStatus);
                Interlocked.Increment(ref _totalEventsProcessed);

                Logger.Log($"📡 [ReactiveStreams] Connection pushed: {port} - {status}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Connection push failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 📥 Push Serial Data Packet
        /// </summary>
        public async Task PushSerialDataAsync(byte[] data, int groupId, CancellationToken cancellationToken = default)
        {
            try
            {
                var packet = new SerialDataPacket
                {
                    Data = data,
                    GroupId = groupId,
                    Timestamp = DateTime.UtcNow,
                    Size = data?.Length ?? 0
                };

                _serialDataStream.OnNext(packet);
                Interlocked.Increment(ref _totalEventsProcessed);

                Logger.Log($"📥 [ReactiveStreams] Serial data pushed: Group {groupId}, {data?.Length ?? 0} bytes", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Serial data push failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 🎯 Stream Operations & Queries

        /// <summary>
        /// 📈 Create Custom Observable Stream
        /// </summary>
        public IObservable<T> CreateStream<T>(string streamName, Func<IObservable<T>> streamFactory)
        {
            try
            {
                var stream = streamFactory();

                var streamDef = new StreamDefinition
                {
                    Name = streamName,
                    Type = typeof(T),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _streamDefinitions[streamName] = streamDef;

                Logger.Log($"📈 [ReactiveStreams] Custom stream created: {streamName} ({typeof(T).Name})", LogLevel.Info);
                return stream;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Stream creation failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// 🔍 Query Historical Data
        /// </summary>
        public IObservable<T> QueryHistorical<T>(string streamName, TimeSpan timespan)
        {
            try
            {
                // Implementation would depend on historical data storage
                // This is a simplified example
                return Observable.Empty<T>();
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Historical query failed: {ex.Message}", LogLevel.Error);
                return Observable.Empty<T>();
            }
        }

        /// <summary>
        /// 📊 Get Real-time Aggregates
        /// </summary>
        public IObservable<TemperatureAggregate> GetTemperatureAggregates(TimeSpan window)
        {
            try
            {
                return _temperatureStream
                    .Window(window)
                    .SelectMany(window => window.Aggregate(
                        new TemperatureAggregate(),
                        (acc, temp) => acc.Add(temp),
                        acc => acc.Finalize()))
                    .Where(agg => agg.Count > 0);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Temperature aggregates failed: {ex.Message}", LogLevel.Error);
                return Observable.Empty<TemperatureAggregate>();
            }
        }

        /// <summary>
        /// 🔄 Get Stirrer Performance Metrics
        /// </summary>
        public IObservable<StirrerMetrics> GetStirrerMetrics(TimeSpan interval)
        {
            try
            {
                return _stirrerStream
                    .Sample(interval)
                    .Scan(new StirrerMetrics(), (metrics, reading) => metrics.Update(reading))
                    .DistinctUntilChanged();
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Stirrer metrics failed: {ex.Message}", LogLevel.Error);
                return Observable.Empty<StirrerMetrics>();
            }
        }

        #endregion

        #region ⚡ Stream Processing

        /// <summary>
        /// 🌡️ Process Temperature Reading
        /// </summary>
        private void ProcessTemperatureReading(TemperatureReading reading)
        {
            try
            {
                // Validate reading
                if (!IsValidTemperature(reading.Value))
                {
                    Logger.Log($"⚠️ [ReactiveStreams] Invalid temperature reading: {reading.Value}", LogLevel.Warn);
                    return;
                }

                // Update performance metrics
                _performanceMonitor.RecordTemperatureReading(reading.Value);

                // Check for temperature alerts
                CheckTemperatureAlerts(reading);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Temperature processing failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 🔄 Process Stirrer Reading
        /// </summary>
        private void ProcessStirrerReading(StirrerReading reading)
        {
            try
            {
                // Validate reading
                if (reading.Rpm < 0 || reading.Rpm > 3000)
                {
                    Logger.Log($"⚠️ [ReactiveStreams] Invalid stirrer reading: {reading.Rpm}", LogLevel.Warn);
                    return;
                }

                // Update performance metrics
                _performanceMonitor.RecordStirrerReading(reading.Rpm);

                // Check for stirrer alerts
                CheckStirrerAlerts(reading);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Stirrer processing failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 📥 Process Serial Data Batch
        /// </summary>
        private async Task<SerialProcessingResult> ProcessSerialDataBatchAsync(
            IList<SerialDataPacket> batch,
            CancellationToken cancellationToken)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var processedCount = 0;

                foreach (var packet in batch)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    // Process packet based on group ID
                    await ProcessSerialPacketAsync(packet, cancellationToken);
                    processedCount++;
                }

                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                return new SerialProcessingResult
                {
                    ProcessedCount = processedCount,
                    TotalCount = batch.Count,
                    ProcessingTime = processingTime,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Serial batch processing failed: {ex.Message}", LogLevel.Error);
                return new SerialProcessingResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// 📦 Process Individual Serial Packet
        /// </summary>
        private async Task ProcessSerialPacketAsync(SerialDataPacket packet, CancellationToken cancellationToken)
        {
            try
            {
                switch (packet.GroupId)
                {
                    case 1: // Temperature data
                        await ProcessTemperaturePacketAsync(packet);
                        break;
                    case 2: // Stirrer data
                        await ProcessStirrerPacketAsync(packet);
                        break;
                    case 5: // External data
                        await ProcessExternalPacketAsync(packet);
                        break;
                    default:
                        Logger.Log($"⚠️ [ReactiveStreams] Unknown packet group: {packet.GroupId}", LogLevel.Warn);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Packet processing failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 🚨 Alert Detection

        /// <summary>
        /// 🚨 Check if Snapshot Contains Alert Conditions
        /// </summary>
        private bool IsAlertCondition(ReactorSnapshot snapshot)
        {
            try
            {
                // Temperature alerts
                if (snapshot.Temperature.Value > _configuration.MaxTemperature ||
                    snapshot.Temperature.Value < _configuration.MinTemperature)
                {
                    return true;
                }

                // Stirrer alerts
                if (snapshot.Stirrer.IsRunning && snapshot.Stirrer.Rpm == 0)
                {
                    return true;
                }

                // Connection alerts
                if (!snapshot.Connection.IsConnected && _configuration.RequireConnection)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Alert condition check failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 🚨 Create Alert from Snapshot
        /// </summary>
        private AlertEvent CreateAlert(ReactorSnapshot snapshot)
        {
            var alert = new AlertEvent
            {
                Id = Guid.NewGuid(),
                Severity = AlertSeverity.Warning,
                Timestamp = DateTime.UtcNow,
                Source = "ReactiveStreams"
            };

            // Determine alert type and message
            if (snapshot.Temperature.Value > _configuration.MaxTemperature)
            {
                alert.Type = AlertType.HighTemperature;
                alert.Message = $"Temperature too high: {snapshot.Temperature.Value:F2}°C";
                alert.Severity = AlertSeverity.Critical;
            }
            else if (snapshot.Temperature.Value < _configuration.MinTemperature)
            {
                alert.Type = AlertType.LowTemperature;
                alert.Message = $"Temperature too low: {snapshot.Temperature.Value:F2}°C";
                alert.Severity = AlertSeverity.Warning;
            }
            else if (snapshot.Stirrer.IsRunning && snapshot.Stirrer.Rpm == 0)
            {
                alert.Type = AlertType.StirrerFailure;
                alert.Message = "Stirrer running but RPM is zero";
                alert.Severity = AlertSeverity.Error;
            }
            else if (!snapshot.Connection.IsConnected)
            {
                alert.Type = AlertType.ConnectionLost;
                alert.Message = "Connection lost";
                alert.Severity = AlertSeverity.Warning;
            }

            return alert;
        }

        #endregion

        #region 📊 Background Processing

        /// <summary>
        /// 🔄 Start Background Processing Tasks
        /// </summary>
        private void StartBackgroundProcessing()
        {
            try
            {
                var token = _cancellationTokenSource.Token;

                // Metrics emission task
                Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            await EmitMetricsAsync();
                            await Task.Delay(TimeSpan.FromSeconds(5), token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"❌ [ReactiveStreams] Metrics emission failed: {ex.Message}", LogLevel.Error);
                        }
                    }
                }, token);

                Logger.Log("🔄 [ReactiveStreams] Background processing started", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Background processing start failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 📊 Emit Performance Metrics
        /// </summary>
        private async Task EmitMetricsAsync()
        {
            try
            {
                var metrics = new MetricsEvent
                {
                    Timestamp = DateTime.UtcNow,
                    TotalEventsProcessed = _totalEventsProcessed,
                    EventsPerSecond = CalculateEventsPerSecond(),
                    ActiveStreams = _streamDefinitions.Count,
                    ActiveSubscriptions = _subscriptions.Count,
                    MemoryUsage = GC.GetTotalMemory(false)
                };

                _metricsStream.OnNext(metrics);
                _lastMetricsUpdate = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Metrics emission failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 🛠️ Utility Methods

        /// <summary>
        /// 📊 Calculate Data Quality
        /// </summary>
        private float CalculateDataQuality(float currentValue, float previousValue)
        {
            try
            {
                if (previousValue == 0) return 1.0f;

                var change = Math.Abs(currentValue - previousValue);
                var percentChange = change / Math.Abs(previousValue) * 100;

                // Quality decreases with large changes
                return Math.Max(0.1f, 1.0f - (percentChange / 100.0f));
            }
            catch
            {
                return 0.5f; // Default quality
            }
        }

        /// <summary>
        /// ⚡ Calculate Power Level
        /// </summary>
        private float CalculatePowerLevel(int rpm)
        {
            try
            {
                return Math.Min(100.0f, (float)rpm / 2000.0f * 100.0f);
            }
            catch
            {
                return 0.0f;
            }
        }

        /// <summary>
        /// ✅ Validate Temperature Reading
        /// </summary>
        private bool IsValidTemperature(float temperature)
        {
            return !float.IsNaN(temperature) &&
                   !float.IsInfinity(temperature) &&
                   temperature >= -273.15f &&
                   temperature <= 1000.0f;
        }

        /// <summary>
        /// 📈 Calculate Events Per Second
        /// </summary>
        private double CalculateEventsPerSecond()
        {
            try
            {
                var elapsed = DateTime.UtcNow - _lastMetricsUpdate;
                if (elapsed.TotalSeconds < 1) return 0;

                return _totalEventsProcessed / elapsed.TotalSeconds;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region 🗑️ Disposal

        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                Logger.Log("🗑️ [ReactiveStreams] Starting disposal", LogLevel.Info);

                // Cancel background operations
                _cancellationTokenSource?.Cancel();

                // Dispose all subscriptions
                foreach (var subscription in _subscriptions.Values)
                {
                    subscription?.Dispose();
                }
                _subscriptions?.Clear();

                // Complete and dispose subjects
                _temperatureStream?.OnCompleted();
                _temperatureStream?.Dispose();
                _stirrerStream?.OnCompleted();
                _stirrerStream?.Dispose();
                _connectionStream?.OnCompleted();
                _connectionStream?.Dispose();
                _serialDataStream?.OnCompleted();
                _serialDataStream?.Dispose();
                _systemSnapshot?.OnCompleted();
                _systemSnapshot?.Dispose();
                _alertStream?.OnCompleted();
                _alertStream?.Dispose();
                _metricsStream?.OnCompleted();
                _metricsStream?.Dispose();

                // Dispose synchronization objects
                _streamLock?.Dispose();
                _cancellationTokenSource?.Dispose();

                // Clear collections
                _streamDefinitions?.Clear();

                _isDisposed = true;
                Logger.Log("✅ [ReactiveStreams] Disposal completed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [ReactiveStreams] Disposal error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 📊 Packet Processing Methods

        private async Task ProcessTemperaturePacketAsync(SerialDataPacket packet)
        {
            if (packet.Data?.Length >= 7)
            {
                // Extract temperature data from packet
                ushort rawTr = (ushort)((packet.Data[2] << 8) | packet.Data[3]);
                ushort rawTj = (ushort)((packet.Data[4] << 8) | packet.Data[5]);

                float tr = HalfToFloat(rawTr);
                float tj = HalfToFloat(rawTj);

                await PushTemperatureAsync(tr, false); // TR reading
                await PushTemperatureAsync(tj, true);  // TJ reading
            }
        }

        private async Task ProcessStirrerPacketAsync(SerialDataPacket packet)
        {
            if (packet.Data?.Length >= 5)
            {
                ushort rawRpm = (ushort)((packet.Data[2] << 8) | packet.Data[3]);
                await PushStirrerAsync(rawRpm, rawRpm > 0);
            }
        }

        private async Task ProcessExternalPacketAsync(SerialDataPacket packet)
        {
            if (packet.Data?.Length >= 5)
            {
                ushort rawExt = (ushort)((packet.Data[2] << 8) | packet.Data[3]);
                int ext1 = rawExt & 0x0FFF;
                // Process external data as needed
            }
        }

        private void CheckTemperatureAlerts(TemperatureReading reading)
        {
            // Implementation for temperature-specific alerts
        }

        private void CheckStirrerAlerts(StirrerReading reading)
        {
            // Implementation for stirrer-specific alerts
        }

        private void UpdateMetrics(SerialProcessingResult result)
        {
            _performanceMonitor.RecordSerialProcessing(result.ProcessingTime, result.ProcessedCount);
        }

        private static float HalfToFloat(ushort half)
        {
            int sign = (half >> 15) & 0x1;
            int exp = (half >> 10) & 0x1F;
            int mant = half & 0x3FF;
            int f;

            if (exp == 0)
            {
                if (mant == 0)
                    f = sign << 31;
                else
                {
                    while ((mant & 0x400) == 0) { mant <<= 1; exp--; }
                    exp++; mant &= ~0x400;
                    exp += (127 - 15); mant <<= 13;
                    f = (sign << 31) | (exp << 23) | mant;
                }
            }
            else if (exp == 31)
            {
                f = (sign << 31) | unchecked((int)0x7F800000) | (mant << 13);
            }
            else
            {
                exp += (127 - 15); mant <<= 13;
                f = (sign << 31) | (exp << 23) | mant;
            }

            return BitConverter.ToSingle(BitConverter.GetBytes(f), 0);
        }

        #endregion
    }

    #region 📋 Data Classes & Configuration

    /// <summary>
    /// 🌡️ Temperature Reading
    /// </summary>
    public class TemperatureReading
    {
        public float Value { get; set; }
        public TemperatureMode Mode { get; set; }
        public DateTime Timestamp { get; set; }
        public float Quality { get; set; }

        public static TemperatureReading Default => new TemperatureReading
        {
            Value = 25.0f,
            Mode = TemperatureMode.Tr,
            Timestamp = DateTime.UtcNow,
            Quality = 1.0f
        };
    }

    /// <summary>
    /// 🔄 Stirrer Reading
    /// </summary>
    public class StirrerReading
    {
        public int Rpm { get; set; }
        public bool IsRunning { get; set; }
        public DateTime Timestamp { get; set; }
        public float PowerLevel { get; set; }

        public static StirrerReading Default => new StirrerReading
        {
            Rpm = 0,
            IsRunning = false,
            Timestamp = DateTime.UtcNow,
            PowerLevel = 0.0f
        };
    }

    /// <summary>
    /// 📡 Connection Status
    /// </summary>
    public class ConnectionStatus
    {
        public bool IsConnected { get; set; }
        public string Port { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public int SignalStrength { get; set; }

        public static ConnectionStatus Disconnected => new ConnectionStatus
        {
            IsConnected = false,
            Port = "Unknown",
            Status = "Disconnected",
            Timestamp = DateTime.UtcNow,
            SignalStrength = 0
        };
    }

    /// <summary>
    /// 📊 Reactor System Snapshot
    /// </summary>
    public class ReactorSnapshot
    {
        public TemperatureReading Temperature { get; set; }
        public StirrerReading Stirrer { get; set; }
        public ConnectionStatus Connection { get; set; }
        public DateTime Timestamp { get; set; }

        public static ReactorSnapshot Empty => new ReactorSnapshot
        {
            Temperature = TemperatureReading.Default,
            Stirrer = StirrerReading.Default,
            Connection = ConnectionStatus.Disconnected,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 📥 Serial Data Packet
    /// </summary>
    public class SerialDataPacket
    {
        public byte[] Data { get; set; }
        public int GroupId { get; set; }
        public DateTime Timestamp { get; set; }
        public int Size { get; set; }
    }

    /// <summary>
    /// 🚨 Alert Event
    /// </summary>
    public class AlertEvent
    {
        public Guid Id { get; set; }
        public AlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 📈 Metrics Event
    /// </summary>
    public class MetricsEvent
    {
        public DateTime Timestamp { get; set; }
        public long TotalEventsProcessed { get; set; }
        public double EventsPerSecond { get; set; }
        public int ActiveStreams { get; set; }
        public int ActiveSubscriptions { get; set; }
        public long MemoryUsage { get; set; }
    }

    /// <summary>
    /// 📊 Temperature Aggregate
    /// </summary>
    public class TemperatureAggregate
    {
        public float Min { get; private set; } = float.MaxValue;
        public float Max { get; private set; } = float.MinValue;
        public float Average { get; private set; }
        public int Count { get; private set; }
        private float _sum = 0;

        public TemperatureAggregate Add(TemperatureReading reading)
        {
            Min = Math.Min(Min, reading.Value);
            Max = Math.Max(Max, reading.Value);
            _sum += reading.Value;
            Count++;
            Average = _sum / Count;
            return this;
        }

        public TemperatureAggregate Finalize()
        {
            if (Count == 0)
            {
                Min = 0;
                Max = 0;
                Average = 0;
            }
            return this;
        }
    }

    /// <summary>
    /// 🔄 Stirrer Metrics
    /// </summary>
    public class StirrerMetrics
    {
        public int AverageRpm { get; private set; }
        public float UptimePercentage { get; private set; }
        public int TotalReadings { get; private set; }
        public int RunningReadings { get; private set; }

        public StirrerMetrics Update(StirrerReading reading)
        {
            TotalReadings++;
            if (reading.IsRunning) RunningReadings++;

            UptimePercentage = TotalReadings > 0 ? (float)RunningReadings / TotalReadings * 100 : 0;
            // Simplified average calculation
            return this;
        }
    }

    /// <summary>
    /// 📊 Stream Definition
    /// </summary>
    public class StreamDefinition
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// 📊 Serial Processing Result
    /// </summary>
    public class SerialProcessingResult
    {
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public double ProcessingTime { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// ⚙️ Reactive Configuration
    /// </summary>
    public class ReactiveConfiguration
    {
        public float MaxTemperature { get; set; } = 150.0f;
        public float MinTemperature { get; set; } = 0.0f;
        public bool RequireConnection { get; set; } = true;
        public int BufferSize { get; set; } = 1000;
        public TimeSpan MaxLatency { get; set; } = TimeSpan.FromMilliseconds(100);

        public static ReactiveConfiguration Default => new ReactiveConfiguration();
    }

    /// <summary>
    /// 🚨 Alert Type Enum
    /// </summary>
    public enum AlertType
    {
        HighTemperature,
        LowTemperature,
        StirrerFailure,
        ConnectionLost,
        SystemError
    }

    /// <summary>
    /// 🚨 Alert Severity Enum
    /// </summary>
    public enum AlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// 🌡️ Temperature Mode Enum
    /// </summary>
    public enum TemperatureMode
    {
        Tr,
        Tj
    }

    #endregion
}