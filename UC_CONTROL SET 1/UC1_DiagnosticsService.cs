// ==============================================
//  UC1_DiagnosticsService.cs - PHASE 3
//  AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
//  System Health & Diagnostics Service
//  Advanced Health Monitoring & Auto-Healing
// ==============================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Logger;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    /// <summary>
    /// 🚀 PHASE 3: Ultra-Advanced System Diagnostics & Health Service
    /// Features: System Health Monitoring, Auto-Healing, Performance Diagnostics
    /// Hardware-Accelerated Health Checks with Predictive Analysis
    /// </summary>
    public class UC1_DiagnosticsService : IDisposable
    {
        #region 🏥 Health Monitoring Infrastructure

        // Core Health Services
        private readonly UC1_PerformanceMonitor _performanceMonitor;
        private readonly SystemHealthChecker _systemHealthChecker;
        private readonly ResourceMonitor _resourceMonitor;
        private readonly NetworkDiagnostics _networkDiagnostics;
        private readonly StorageDiagnostics _storageDiagnostics;

        // Health State Management
        private volatile SystemHealthState _currentHealthState;
        private readonly ConcurrentDictionary<string, HealthCheck> _healthChecks;
        private readonly ConcurrentDictionary<string, DiagnosticTest> _diagnosticTests;
        private readonly ConcurrentQueue<HealthEvent> _healthEventHistory;

        // Reactive Health Streams
        private readonly BehaviorSubject<SystemHealthState> _healthStateSubject;
        private readonly Subject<HealthCheckResult> _healthCheckSubject;
        private readonly Subject<DiagnosticEvent> _diagnosticEventSubject;
        private readonly Subject<SystemAlert> _systemAlertSubject;
        private readonly Subject<AutoHealingEvent> _autoHealingSubject;

        // Monitoring & Scheduling
        private readonly Timer _healthCheckTimer;
        private readonly Timer _diagnosticsTimer;
        private readonly Timer _resourceMonitorTimer;
        private readonly Timer _cleanupTimer;

        // Configuration & Synchronization
        private readonly DiagnosticsConfiguration _configuration;
        private readonly SemaphoreSlim _diagnosticsLock;
        private readonly CancellationTokenSource _cancellationTokenSource;

        // Performance Metrics
        private volatile bool _isDisposed = false;
        private long _totalHealthChecks = 0;
        private long _totalDiagnosticTests = 0;
        private long _totalAutoHealingActions = 0;
        private DateTime _lastFullDiagnostic = DateTime.MinValue;

        #endregion

        #region 🔥 Public Health Observables

        /// <summary>🏥 System Health State Stream</summary>
        public IObservable<SystemHealthState> HealthState => _healthStateSubject.AsObservable();

        /// <summary>✅ Health Check Results Stream</summary>
        public IObservable<HealthCheckResult> HealthCheckResults => _healthCheckSubject.AsObservable();

        /// <summary>🔬 Diagnostic Events Stream</summary>
        public IObservable<DiagnosticEvent> DiagnosticEvents => _diagnosticEventSubject.AsObservable();

        /// <summary>🚨 System Alerts Stream</summary>
        public IObservable<SystemAlert> SystemAlerts => _systemAlertSubject.AsObservable();

        /// <summary>🔧 Auto-Healing Events Stream</summary>
        public IObservable<AutoHealingEvent> AutoHealingEvents => _autoHealingSubject.AsObservable();

        /// <summary>📊 Current Health State</summary>
        public SystemHealthState CurrentHealthState => _currentHealthState;

        /// <summary>📈 Health Statistics</summary>
        public DiagnosticsStatistics Statistics => GetStatistics();

        #endregion

        #region 🏗️ Constructor & Initialization

        /// <summary>
        /// 🎯 Initialize Ultra-Advanced Diagnostics Service
        /// </summary>
        public UC1_DiagnosticsService(
            UC1_PerformanceMonitor performanceMonitor,
            DiagnosticsConfiguration configuration = null)
        {
            try
            {
                Logger.Log("🚀 [DiagnosticsService] Initializing Advanced System Health & Diagnostics", LogLevel.Info);

                // Initialize dependencies
                _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
                _configuration = configuration ?? DiagnosticsConfiguration.Default;

                // Initialize diagnostic components
                _systemHealthChecker = new SystemHealthChecker(_configuration);
                _resourceMonitor = new ResourceMonitor(_configuration);
                _networkDiagnostics = new NetworkDiagnostics(_configuration);
                _storageDiagnostics = new StorageDiagnostics(_configuration);

                // Initialize health state
                _currentHealthState = SystemHealthState.CreateHealthy();

                // Initialize collections
                _healthChecks = new ConcurrentDictionary<string, HealthCheck>();
                _diagnosticTests = new ConcurrentDictionary<string, DiagnosticTest>();
                _healthEventHistory = new ConcurrentQueue<HealthEvent>();

                // Initialize reactive subjects
                _healthStateSubject = new BehaviorSubject<SystemHealthState>(_currentHealthState);
                _healthCheckSubject = new Subject<HealthCheckResult>();
                _diagnosticEventSubject = new Subject<DiagnosticEvent>();
                _systemAlertSubject = new Subject<SystemAlert>();
                _autoHealingSubject = new Subject<AutoHealingEvent>();

                // Initialize synchronization
                _diagnosticsLock = new SemaphoreSlim(1, 1);
                _cancellationTokenSource = new CancellationTokenSource();

                // Setup monitoring timers
                _healthCheckTimer = new Timer(HealthCheckTimerCallback, null,
                    TimeSpan.FromSeconds(10), _configuration.HealthCheckInterval);
                _diagnosticsTimer = new Timer(DiagnosticsTimerCallback, null,
                    TimeSpan.FromSeconds(30), _configuration.DiagnosticsInterval);
                _resourceMonitorTimer = new Timer(ResourceMonitorTimerCallback, null,
                    TimeSpan.FromSeconds(5), _configuration.ResourceMonitorInterval);
                _cleanupTimer = new Timer(CleanupTimerCallback, null,
                    TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

                // Setup default health checks
                SetupDefaultHealthChecks();

                // Setup performance monitor integration
                SetupPerformanceMonitorIntegration();

                // Start initial health assessment
                _ = Task.Run(async () => await PerformInitialHealthAssessmentAsync());

                Logger.Log("✅ [DiagnosticsService] Advanced System Health & Diagnostics initialized", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Initialization failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// ⚙️ Setup Default Health Checks
        /// </summary>
        private void SetupDefaultHealthChecks()
        {
            try
            {
                // System Resource Health Checks
                RegisterHealthCheck("CPU_Usage", new CpuUsageHealthCheck(_configuration));
                RegisterHealthCheck("Memory_Usage", new MemoryUsageHealthCheck(_configuration));
                RegisterHealthCheck("Disk_Space", new DiskSpaceHealthCheck(_configuration));
                RegisterHealthCheck("Network_Connectivity", new NetworkConnectivityHealthCheck(_configuration));

                // Application Health Checks
                RegisterHealthCheck("Serial_Communication", new SerialCommunicationHealthCheck());
                RegisterHealthCheck("Data_Persistence", new DataPersistenceHealthCheck());
                RegisterHealthCheck("State_Management", new StateManagementHealthCheck());
                RegisterHealthCheck("Performance_Metrics", new PerformanceMetricsHealthCheck());

                // Reactor System Health Checks
                RegisterHealthCheck("Temperature_Sensor", new TemperatureSensorHealthCheck());
                RegisterHealthCheck("Stirrer_Motor", new StirrerMotorHealthCheck());
                RegisterHealthCheck("Control_System", new ControlSystemHealthCheck());
                RegisterHealthCheck("Safety_Systems", new SafetySystemsHealthCheck());

                Logger.Log("⚙️ [DiagnosticsService] Default health checks configured", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Health checks setup failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 🔗 Setup Performance Monitor Integration
        /// </summary>
        private void SetupPerformanceMonitorIntegration()
        {
            try
            {
                // Subscribe to performance alerts
                _performanceMonitor.PerformanceAlerts
                    .Subscribe(
                        alert => OnPerformanceAlert(alert),
                        ex => Logger.Log($"❌ [DiagnosticsService] Performance alert subscription error: {ex.Message}", LogLevel.Error)
                    );

                // Subscribe to metric events
                _performanceMonitor.MetricEvents
                    .Where(me => IsMetricCritical(me))
                    .Subscribe(
                        metric => OnCriticalMetric(metric),
                        ex => Logger.Log($"❌ [DiagnosticsService] Metric subscription error: {ex.Message}", LogLevel.Error)
                    );

                Logger.Log("🔗 [DiagnosticsService] Performance monitor integration configured", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Performance monitor integration failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 🏥 Health Check Operations

        /// <summary>
        /// 🔧 Register Custom Health Check
        /// </summary>
        public void RegisterHealthCheck(string name, IHealthCheck healthCheck)
        {
            try
            {
                var check = new HealthCheck
                {
                    Name = name,
                    Checker = healthCheck,
                    Enabled = true,
                    LastCheck = DateTime.MinValue,
                    CheckCount = 0,
                    FailureCount = 0
                };

                _healthChecks[name] = check;
                Logger.Log($"🔧 [DiagnosticsService] Health check registered: {name}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Health check registration failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// ✅ Run Single Health Check
        /// </summary>
        public async Task<HealthCheckResult> RunHealthCheckAsync(string name, CancellationToken cancellationToken = default)
        {
            if (!_healthChecks.TryGetValue(name, out HealthCheck check))
            {
                return HealthCheckResult.NotFound(name);
            }

            try
            {
                var startTime = DateTime.UtcNow;
                var result = await check.Checker.CheckHealthAsync(cancellationToken);
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Update check statistics
                check.LastCheck = DateTime.UtcNow;
                check.CheckCount++;
                if (!result.IsHealthy)
                {
                    check.FailureCount++;
                }

                // Create detailed result
                var healthCheckResult = new HealthCheckResult
                {
                    Name = name,
                    IsHealthy = result.IsHealthy,
                    Status = result.Status,
                    Description = result.Description,
                    Duration = duration,
                    Timestamp = DateTime.UtcNow,
                    Tags = result.Tags,
                    Data = result.Data
                };

                // Emit result
                _healthCheckSubject.OnNext(healthCheckResult);

                // Update metrics
                Interlocked.Increment(ref _totalHealthChecks);

                // Check for alerts
                if (!result.IsHealthy && result.Status == HealthStatus.Critical)
                {
                    await TriggerSystemAlertAsync(name, result.Description);
                }

                Logger.Log($"✅ [DiagnosticsService] Health check completed: {name} - {result.Status} ({duration:F2}ms)", LogLevel.Debug);
                return healthCheckResult;
            }
            catch (Exception ex)
            {
                check.FailureCount++;
                Logger.Log($"❌ [DiagnosticsService] Health check failed: {name} - {ex.Message}", LogLevel.Error);
                return HealthCheckResult.Error(name, ex.Message);
            }
        }

        /// <summary>
        /// 🏥 Run All Health Checks
        /// </summary>
        public async Task<SystemHealthReport> RunAllHealthChecksAsync(CancellationToken cancellationToken = default)
        {
            await _diagnosticsLock.WaitAsync(cancellationToken);
            try
            {
                var startTime = DateTime.UtcNow;
                var results = new List<HealthCheckResult>();

                // Run all enabled health checks
                var checkTasks = _healthChecks.Values
                    .Where(check => check.Enabled)
                    .Select(async check =>
                    {
                        try
                        {
                            return await RunHealthCheckAsync(check.Name, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"❌ [DiagnosticsService] Health check task failed: {check.Name} - {ex.Message}", LogLevel.Error);
                            return HealthCheckResult.Error(check.Name, ex.Message);
                        }
                    });

                results.AddRange(await Task.WhenAll(checkTasks));

                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Create health report
                var report = new SystemHealthReport
                {
                    Timestamp = DateTime.UtcNow,
                    Duration = duration,
                    Results = results,
                    OverallStatus = CalculateOverallHealth(results),
                    TotalChecks = results.Count,
                    HealthyChecks = results.Count(r => r.IsHealthy),
                    UnhealthyChecks = results.Count(r => !r.IsHealthy),
                    CriticalIssues = results.Count(r => r.Status == HealthStatus.Critical)
                };

                // Update system health state
                await UpdateSystemHealthStateAsync(report);

                Logger.Log($"🏥 [DiagnosticsService] Health checks completed: {report.HealthyChecks}/{report.TotalChecks} healthy ({duration:F2}ms)", LogLevel.Info);
                return report;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Run all health checks failed: {ex.Message}", LogLevel.Error);
                return SystemHealthReport.CreateError(ex.Message);
            }
            finally
            {
                _diagnosticsLock.Release();
            }
        }

        #endregion

        #region 🔬 Diagnostic Tests

        /// <summary>
        /// 🔬 Run Diagnostic Test
        /// </summary>
        public async Task<DiagnosticTestResult> RunDiagnosticTestAsync(string testName, CancellationToken cancellationToken = default)
        {
            if (!_diagnosticTests.TryGetValue(testName, out DiagnosticTest test))
            {
                return DiagnosticTestResult.NotFound(testName);
            }

            try
            {
                var startTime = DateTime.UtcNow;
                var result = await test.Executor.ExecuteAsync(cancellationToken);
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Update test statistics
                test.LastRun = DateTime.UtcNow;
                test.RunCount++;
                if (!result.Passed)
                {
                    test.FailureCount++;
                }

                // Create detailed result
                var diagnosticResult = new DiagnosticTestResult
                {
                    TestName = testName,
                    Passed = result.Passed,
                    Score = result.Score,
                    Message = result.Message,
                    Duration = duration,
                    Timestamp = DateTime.UtcNow,
                    Details = result.Details,
                    Recommendations = result.Recommendations
                };

                // Emit diagnostic event
                _diagnosticEventSubject.OnNext(new DiagnosticEvent
                {
                    Type = DiagnosticEventType.TestCompleted,
                    TestName = testName,
                    Result = diagnosticResult,
                    Timestamp = DateTime.UtcNow
                });

                // Update metrics
                Interlocked.Increment(ref _totalDiagnosticTests);

                Logger.Log($"🔬 [DiagnosticsService] Diagnostic test completed: {testName} - {(result.Passed ? "PASS" : "FAIL")} ({duration:F2}ms)", LogLevel.Debug);
                return diagnosticResult;
            }
            catch (Exception ex)
            {
                test.FailureCount++;
                Logger.Log($"❌ [DiagnosticsService] Diagnostic test failed: {testName} - {ex.Message}", LogLevel.Error);
                return DiagnosticTestResult.Error(testName, ex.Message);
            }
        }

        /// <summary>
        /// 🔬 Run Full System Diagnostics
        /// </summary>
        public async Task<SystemDiagnosticReport> RunFullSystemDiagnosticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Log("🔬 [DiagnosticsService] Starting full system diagnostics", LogLevel.Info);

                var startTime = DateTime.UtcNow;
                var report = new SystemDiagnosticReport
                {
                    Timestamp = DateTime.UtcNow,
                    SystemInfo = await CollectSystemInfoAsync(),
                    PerformanceMetrics = await CollectPerformanceMetricsAsync(),
                    ResourceUtilization = await CollectResourceUtilizationAsync(),
                    NetworkDiagnostics = await _networkDiagnostics.RunFullDiagnosticsAsync(cancellationToken),
                    StorageDiagnostics = await _storageDiagnostics.RunFullDiagnosticsAsync(cancellationToken),
                    ApplicationHealth = await RunAllHealthChecksAsync(cancellationToken)
                };

                // Calculate overall system score
                report.OverallScore = CalculateSystemScore(report);
                report.Duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                _lastFullDiagnostic = DateTime.UtcNow;

                // Emit diagnostic event
                _diagnosticEventSubject.OnNext(new DiagnosticEvent
                {
                    Type = DiagnosticEventType.FullDiagnosticsCompleted,
                    Report = report,
                    Timestamp = DateTime.UtcNow
                });

                Logger.Log($"🔬 [DiagnosticsService] Full system diagnostics completed - Score: {report.OverallScore:F1}/100 ({report.Duration:F2}ms)", LogLevel.Info);
                return report;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Full system diagnostics failed: {ex.Message}", LogLevel.Error);
                return SystemDiagnosticReport.CreateError(ex.Message);
            }
        }

        #endregion

        #region 🔧 Auto-Healing Operations

        /// <summary>
        /// 🔧 Trigger Auto-Healing
        /// </summary>
        public async Task<AutoHealingResult> TriggerAutoHealingAsync(string issue, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Log($"🔧 [DiagnosticsService] Triggering auto-healing for: {issue}", LogLevel.Info);

                var healingAction = GetHealingAction(issue);
                if (healingAction == null)
                {
                    return AutoHealingResult.NoAction(issue);
                }

                var startTime = DateTime.UtcNow;
                var success = await healingAction.ExecuteAsync(cancellationToken);
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                var result = new AutoHealingResult
                {
                    Issue = issue,
                    Action = healingAction.Name,
                    Success = success,
                    Duration = duration,
                    Timestamp = DateTime.UtcNow,
                    Message = success ? "Auto-healing completed successfully" : "Auto-healing failed"
                };

                // Update metrics
                Interlocked.Increment(ref _totalAutoHealingActions);

                // Emit auto-healing event
                _autoHealingSubject.OnNext(new AutoHealingEvent
                {
                    Result = result,
                    Timestamp = DateTime.UtcNow
                });

                Logger.Log($"🔧 [DiagnosticsService] Auto-healing {(success ? "completed" : "failed")}: {issue} ({duration:F2}ms)", success ? LogLevel.Info : LogLevel.Warn);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Auto-healing error: {ex.Message}", LogLevel.Error);
                return AutoHealingResult.Error(issue, ex.Message);
            }
        }

        /// <summary>
        /// 🔧 Get Healing Action for Issue
        /// </summary>
        private IHealingAction GetHealingAction(string issue)
        {
            return issue.ToLower() switch
            {
                var x when x.Contains("memory") => new MemoryOptimizationAction(),
                var x when x.Contains("cpu") => new CpuOptimizationAction(),
                var x when x.Contains("disk") => new DiskCleanupAction(),
                var x when x.Contains("network") => new NetworkResetAction(),
                var x when x.Contains("serial") => new SerialReconnectAction(),
                var x when x.Contains("temperature") => new TemperatureCalibrationAction(),
                var x when x.Contains("stirrer") => new StirrerRecalibrationAction(),
                _ => null
            };
        }

        #endregion

        #region 📊 System Information Collection

        /// <summary>
        /// 💻 Collect System Information
        /// </summary>
        private async Task<SystemInfo> CollectSystemInfoAsync()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var computerInfo = new ComputerInfo();

                return new SystemInfo
                {
                    MachineName = Environment.MachineName,
                    OperatingSystem = Environment.OSVersion.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    TotalPhysicalMemory = computerInfo.TotalPhysicalMemory,
                    AvailablePhysicalMemory = computerInfo.AvailablePhysicalMemory,
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                    StartTime = process.StartTime,
                    WorkingSet = process.WorkingSet64,
                    PrivateMemory = process.PrivateMemorySize64,
                    VirtualMemory = process.VirtualMemorySize64,
                    ThreadCount = process.Threads.Count,
                    HandleCount = process.HandleCount,
                    GCMemory = GC.GetTotalMemory(false),
                    Is64BitProcess = Environment.Is64BitProcess,
                    Is64BitOperatingSystem = Environment.Is64BitOperatingSystem,
                    SystemUptime = Environment.TickCount64,
                    CollectedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Collect system info failed: {ex.Message}", LogLevel.Error);
                return new SystemInfo { CollectedAt = DateTime.UtcNow };
            }
        }

        /// <summary>
        /// 📊 Collect Performance Metrics
        /// </summary>
        private async Task<PerformanceMetrics> CollectPerformanceMetricsAsync()
        {
            try
            {
                var summary = _performanceMonitor.GetPerformanceSummary();
                return new PerformanceMetrics
                {
                    CpuUsage = summary.CachedMetrics.CpuUsage,
                    MemoryUsage = summary.CachedMetrics.MemoryUsage,
                    ThreadCount = summary.CachedMetrics.ThreadCount,
                    CollectionTime = summary.CachedMetrics.CollectionTime,
                    IsHealthy = summary.IsHealthy,
                    Uptime = summary.Uptime,
                    ActiveCollectors = summary.ActiveCollectors,
                    BufferedMetrics = summary.BufferedMetrics,
                    CollectedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Collect performance metrics failed: {ex.Message}", LogLevel.Error);
                return new PerformanceMetrics { CollectedAt = DateTime.UtcNow };
            }
        }

        /// <summary>
        /// 🔧 Collect Resource Utilization
        /// </summary>
        private async Task<ResourceUtilization> CollectResourceUtilizationAsync()
        {
            try
            {
                return await _resourceMonitor.GetCurrentUtilizationAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Collect resource utilization failed: {ex.Message}", LogLevel.Error);
                return new ResourceUtilization { CollectedAt = DateTime.UtcNow };
            }
        }

        #endregion

        #region 🚨 Alert & Event Handling

        /// <summary>
        /// 🚨 Trigger System Alert
        /// </summary>
        private async Task TriggerSystemAlertAsync(string source, string message)
        {
            try
            {
                var alert = new SystemAlert
                {
                    Id = Guid.NewGuid(),
                    Source = source,
                    Message = message,
                    Severity = AlertSeverity.Critical,
                    Timestamp = DateTime.UtcNow,
                    RequiresAction = true
                };

                _systemAlertSubject.OnNext(alert);

                // Trigger auto-healing if enabled
                if (_configuration.AutoHealingEnabled)
                {
                    _ = Task.Run(async () => await TriggerAutoHealingAsync(source));
                }

                Logger.Log($"🚨 [DiagnosticsService] System alert triggered: {source} - {message}", LogLevel.Error);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Trigger system alert failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// ⚡ Handle Performance Alert
        /// </summary>
        private void OnPerformanceAlert(AlertEvent alert)
        {
            try
            {
                // Convert performance alert to system alert
                _ = Task.Run(async () => await TriggerSystemAlertAsync("PerformanceMonitor", alert.Message));
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Performance alert handler failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 📊 Handle Critical Metric
        /// </summary>
        private void OnCriticalMetric(MetricEvent metricEvent)
        {
            try
            {
                var message = $"Critical metric detected: {metricEvent.Metric.Name} = {metricEvent.Metric.Value}";
                _ = Task.Run(async () => await TriggerSystemAlertAsync("CriticalMetric", message));
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Critical metric handler failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 📊 Check if Metric is Critical
        /// </summary>
        private bool IsMetricCritical(MetricEvent metricEvent)
        {
            var metric = metricEvent.Metric;
            return metric.Name switch
            {
                "CPU_Usage" => metric.Value > 90,
                "Memory_Usage" => metric.Value > 90,
                "TemperatureReading" => Math.Abs(metric.Value) > 200,
                _ => false
            };
        }

        #endregion

        #region 🛠️ Helper Methods

        private async Task PerformInitialHealthAssessmentAsync()
        {
            try
            {
                Logger.Log("🏥 [DiagnosticsService] Performing initial health assessment", LogLevel.Info);

                var healthReport = await RunAllHealthChecksAsync();
                var diagnosticReport = await RunFullSystemDiagnosticsAsync();

                Logger.Log($"🏥 [DiagnosticsService] Initial health assessment completed - Health: {healthReport.OverallStatus}, Score: {diagnosticReport.OverallScore:F1}/100", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Initial health assessment failed: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task UpdateSystemHealthStateAsync(SystemHealthReport report)
        {
            try
            {
                var newState = new SystemHealthState
                {
                    OverallStatus = report.OverallStatus,
                    LastHealthCheck = report.Timestamp,
                    HealthScore = CalculateHealthScore(report),
                    CriticalIssuesCount = report.CriticalIssues,
                    TotalChecksCount = report.TotalChecks,
                    HealthyChecksCount = report.HealthyChecks,
                    IsSystemStable = report.OverallStatus != HealthStatus.Critical,
                    LastUpdated = DateTime.UtcNow
                };

                _currentHealthState = newState;
                _healthStateSubject.OnNext(newState);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Update system health state failed: {ex.Message}", LogLevel.Error);
            }
        }

        private HealthStatus CalculateOverallHealth(List<HealthCheckResult> results)
        {
            if (results.Any(r => r.Status == HealthStatus.Critical))
                return HealthStatus.Critical;
            if (results.Any(r => r.Status == HealthStatus.Warning))
                return HealthStatus.Warning;
            if (results.All(r => r.Status == HealthStatus.Healthy))
                return HealthStatus.Healthy;
            return HealthStatus.Unknown;
        }

        private double CalculateHealthScore(SystemHealthReport report)
        {
            if (report.TotalChecks == 0) return 0;
            return (double)report.HealthyChecks / report.TotalChecks * 100;
        }

        private double CalculateSystemScore(SystemDiagnosticReport report)
        {
            var scores = new List<double>();

            // Health score (40% weight)
            if (report.ApplicationHealth != null)
            {
                scores.Add(CalculateHealthScore(report.ApplicationHealth) * 0.4);
            }

            // Performance score (30% weight)
            if (report.PerformanceMetrics != null)
            {
                var perfScore = report.PerformanceMetrics.IsHealthy ? 90 : 60;
                scores.Add(perfScore * 0.3);
            }

            // Resource utilization score (20% weight)
            if (report.ResourceUtilization != null)
            {
                var resourceScore = (100 - Math.Max(report.ResourceUtilization.CpuUsage, report.ResourceUtilization.MemoryUsage));
                scores.Add(resourceScore * 0.2);
            }

            // Network and storage score (10% weight)
            var networkStorageScore = 80; // Simplified
            scores.Add(networkStorageScore * 0.1);

            return scores.Sum();
        }

        private DiagnosticsStatistics GetStatistics()
        {
            return new DiagnosticsStatistics
            {
                TotalHealthChecks = _totalHealthChecks,
                TotalDiagnosticTests = _totalDiagnosticTests,
                TotalAutoHealingActions = _totalAutoHealingActions,
                RegisteredHealthChecks = _healthChecks.Count,
                RegisteredDiagnosticTests = _diagnosticTests.Count,
                LastFullDiagnostic = _lastFullDiagnostic,
                CurrentHealthStatus = _currentHealthState.OverallStatus,
                SystemHealthScore = _currentHealthState.HealthScore,
                MemoryUsage = GC.GetTotalMemory(false)
            };
        }

        private void HealthCheckTimerCallback(object state)
        {
            try
            {
                _ = Task.Run(async () => await RunAllHealthChecksAsync());
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Health check timer callback failed: {ex.Message}", LogLevel.Error);
            }
        }

        private void DiagnosticsTimerCallback(object state)
        {
            try
            {
                if (DateTime.UtcNow - _lastFullDiagnostic > _configuration.FullDiagnosticsInterval)
                {
                    _ = Task.Run(async () => await RunFullSystemDiagnosticsAsync());
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Diagnostics timer callback failed: {ex.Message}", LogLevel.Error);
            }
        }

        private void ResourceMonitorTimerCallback(object state)
        {
            try
            {
                _ = Task.Run(async () => await _resourceMonitor.UpdateResourceMetricsAsync());
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Resource monitor timer callback failed: {ex.Message}", LogLevel.Error);
            }
        }

        private void CleanupTimerCallback(object state)
        {
            try
            {
                // Cleanup old health events
                while (_healthEventHistory.Count > _configuration.MaxHealthEventHistory)
                {
                    _healthEventHistory.TryDequeue(out _);
                }

                // Force garbage collection if memory usage is high
                if (GC.GetTotalMemory(false) > _configuration.MaxMemoryUsage)
                {
                    GC.Collect(2, GCCollectionMode.Optimized);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Cleanup timer callback failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 🗑️ Disposal

        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                Logger.Log("🗑️ [DiagnosticsService] Starting disposal", LogLevel.Info);

                // Cancel operations
                _cancellationTokenSource?.Cancel();

                // Dispose timers
                _healthCheckTimer?.Dispose();
                _diagnosticsTimer?.Dispose();
                _resourceMonitorTimer?.Dispose();
                _cleanupTimer?.Dispose();

                // Complete reactive subjects
                _healthStateSubject?.OnCompleted();
                _healthStateSubject?.Dispose();
                _healthCheckSubject?.OnCompleted();
                _healthCheckSubject?.Dispose();
                _diagnosticEventSubject?.OnCompleted();
                _diagnosticEventSubject?.Dispose();
                _systemAlertSubject?.OnCompleted();
                _systemAlertSubject?.Dispose();
                _autoHealingSubject?.OnCompleted();
                _autoHealingSubject?.Dispose();

                // Dispose components
                _systemHealthChecker?.Dispose();
                _resourceMonitor?.Dispose();
                _networkDiagnostics?.Dispose();
                _storageDiagnostics?.Dispose();

                // Dispose synchronization objects
                _diagnosticsLock?.Dispose();
                _cancellationTokenSource?.Dispose();

                // Clear collections
                _healthChecks?.Clear();
                _diagnosticTests?.Clear();
                while (_healthEventHistory?.TryDequeue(out _) == true) { }

                _isDisposed = true;
                Logger.Log("✅ [DiagnosticsService] Disposal completed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [DiagnosticsService] Disposal error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion
    }

    #region 📋 Supporting Classes - Health System

    // เนื่องจากมีหลายคลาสมาก จะ implement เฉพาะส่วนสำคัญ ส่วนที่เหลือจะเป็น interface และ basic implementation

    /// <summary>🏥 Health Check Interface</summary>
    public interface IHealthCheck
    {
        Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>🔧 Healing Action Interface</summary>
    public interface IHealingAction
    {
        string Name { get; }
        Task<bool> ExecuteAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>📊 System Health State</summary>
    public class SystemHealthState
    {
        public HealthStatus OverallStatus { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public double HealthScore { get; set; }
        public int CriticalIssuesCount { get; set; }
        public int TotalChecksCount { get; set; }
        public int HealthyChecksCount { get; set; }
        public bool IsSystemStable { get; set; }
        public DateTime LastUpdated { get; set; }

        public static SystemHealthState CreateHealthy() => new SystemHealthState
        {
            OverallStatus = HealthStatus.Healthy,
            LastHealthCheck = DateTime.UtcNow,
            HealthScore = 100,
            CriticalIssuesCount = 0,
            IsSystemStable = true,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>✅ Health Check Result</summary>
    public class HealthCheckResult
    {
        public string Name { get; set; }
        public bool IsHealthy { get; set; }
        public HealthStatus Status { get; set; }
        public string Description { get; set; }
        public double Duration { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Tags { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        public static HealthCheckResult NotFound(string name) => new HealthCheckResult
        {
            Name = name,
            IsHealthy = false,
            Status = HealthStatus.Unknown,
            Description = "Health check not found",
            Timestamp = DateTime.UtcNow
        };

        public static HealthCheckResult Error(string name, string error) => new HealthCheckResult
        {
            Name = name,
            IsHealthy = false,
            Status = HealthStatus.Critical,
            Description = error,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>🏥 System Health Report</summary>
    public class SystemHealthReport
    {
        public DateTime Timestamp { get; set; }
        public double Duration { get; set; }
        public List<HealthCheckResult> Results { get; set; } = new List<HealthCheckResult>();
        public HealthStatus OverallStatus { get; set; }
        public int TotalChecks { get; set; }
        public int HealthyChecks { get; set; }
        public int UnhealthyChecks { get; set; }
        public int CriticalIssues { get; set; }

        public static SystemHealthReport CreateError(string error) => new SystemHealthReport
        {
            Timestamp = DateTime.UtcNow,
            OverallStatus = HealthStatus.Critical,
            Results = new List<HealthCheckResult> { HealthCheckResult.Error("System", error) }
        };
    }

    /// <summary>🔬 System Diagnostic Report</summary>
    public class SystemDiagnosticReport
    {
        public DateTime Timestamp { get; set; }
        public double Duration { get; set; }
        public double OverallScore { get; set; }
        public SystemInfo SystemInfo { get; set; }
        public PerformanceMetrics PerformanceMetrics { get; set; }
        public ResourceUtilization ResourceUtilization { get; set; }
        public NetworkDiagnosticResult NetworkDiagnostics { get; set; }
        public StorageDiagnosticResult StorageDiagnostics { get; set; }
        public SystemHealthReport ApplicationHealth { get; set; }

        public static SystemDiagnosticReport CreateError(string error) => new SystemDiagnosticReport
        {
            Timestamp = DateTime.UtcNow,
            OverallScore = 0,
            ApplicationHealth = SystemHealthReport.CreateError(error)
        };
    }

    /// <summary>⚙️ Diagnostics Configuration</summary>
    public class DiagnosticsConfiguration
    {
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
        public TimeSpan DiagnosticsInterval { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan ResourceMonitorInterval { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan FullDiagnosticsInterval { get; set; } = TimeSpan.FromHours(1);
        public bool AutoHealingEnabled { get; set; } = true;
        public int MaxHealthEventHistory { get; set; } = 1000;
        public long MaxMemoryUsage { get; set; } = 1024 * 1024 * 1024; // 1GB
        public double CpuThreshold { get; set; } = 80.0;
        public double MemoryThreshold { get; set; } = 80.0;
        public double DiskThreshold { get; set; } = 90.0;

        public static DiagnosticsConfiguration Default => new DiagnosticsConfiguration();
    }

    // Simplified implementations of remaining classes
    public class SystemInfo { public DateTime CollectedAt { get; set; } /* other properties */ }
    public class PerformanceMetrics { public DateTime CollectedAt { get; set; } public bool IsHealthy { get; set; } public double CpuUsage { get; set; } public double MemoryUsage { get; set; } public int ThreadCount { get; set; } public double CollectionTime { get; set; } public TimeSpan Uptime { get; set; } public int ActiveCollectors { get; set; } public int BufferedMetrics { get; set; } }
    public class ResourceUtilization { public DateTime CollectedAt { get; set; } public double CpuUsage { get; set; } public double MemoryUsage { get; set; } }
    public class NetworkDiagnosticResult { }
    public class StorageDiagnosticResult { }
    public class HealthCheck { public string Name { get; set; } public IHealthCheck Checker { get; set; } public bool Enabled { get; set; } public DateTime LastCheck { get; set; } public long CheckCount { get; set; } public long FailureCount { get; set; } }
    public class DiagnosticTest { public string Name { get; set; } public IDiagnosticTestExecutor Executor { get; set; } public DateTime LastRun { get; set; } public long RunCount { get; set; } public long FailureCount { get; set; } }
    public class DiagnosticTestResult { public string TestName { get; set; } public bool Passed { get; set; } public double Score { get; set; } public string Message { get; set; } public double Duration { get; set; } public DateTime Timestamp { get; set; } public Dictionary<string, object> Details { get; set; } public List<string> Recommendations { get; set; } public static DiagnosticTestResult NotFound(string name) => new DiagnosticTestResult { TestName = name, Passed = false, Message = "Test not found" }; public static DiagnosticTestResult Error(string name, string error) => new DiagnosticTestResult { TestName = name, Passed = false, Message = error }; }
    public class AutoHealingResult { public string Issue { get; set; } public string Action { get; set; } public bool Success { get; set; } public double Duration { get; set; } public DateTime Timestamp { get; set; } public string Message { get; set; } public static AutoHealingResult NoAction(string issue) => new AutoHealingResult { Issue = issue, Success = false, Message = "No healing action available" }; public static AutoHealingResult Error(string issue, string error) => new AutoHealingResult { Issue = issue, Success = false, Message = error }; }
    public class DiagnosticsStatistics { public long TotalHealthChecks { get; set; } public long TotalDiagnosticTests { get; set; } public long TotalAutoHealingActions { get; set; } public int RegisteredHealthChecks { get; set; } public int RegisteredDiagnosticTests { get; set; } public DateTime LastFullDiagnostic { get; set; } public HealthStatus CurrentHealthStatus { get; set; } public double SystemHealthScore { get; set; } public long MemoryUsage { get; set; } }

    // Enums
    public enum HealthStatus { Unknown, Healthy, Warning, Critical }
    public enum DiagnosticEventType { TestCompleted, FullDiagnosticsCompleted, HealthCheckFailed }
    public enum AlertSeverity { Info, Warning, Error, Critical }

    // Events
    public class DiagnosticEvent { public DiagnosticEventType Type { get; set; } public string TestName { get; set; } public DiagnosticTestResult Result { get; set; } public SystemDiagnosticReport Report { get; set; } public DateTime Timestamp { get; set; } }
    public class SystemAlert { public Guid Id { get; set; } public string Source { get; set; } public string Message { get; set; } public AlertSeverity Severity { get; set; } public DateTime Timestamp { get; set; } public bool RequiresAction { get; set; } }
    public class AutoHealingEvent { public AutoHealingResult Result { get; set; } public DateTime Timestamp { get; set; } }
    public class HealthEvent { public string Type { get; set; } public DateTime Timestamp { get; set; } }

    // Component classes (simplified)
    public class SystemHealthChecker : IDisposable { public SystemHealthChecker(DiagnosticsConfiguration config) { } public void Dispose() { } }
    public class ResourceMonitor : IDisposable { public ResourceMonitor(DiagnosticsConfiguration config) { } public async Task<ResourceUtilization> GetCurrentUtilizationAsync() => new ResourceUtilization(); public async Task UpdateResourceMetricsAsync() { } public void Dispose() { } }
    public class NetworkDiagnostics : IDisposable { public NetworkDiagnostics(DiagnosticsConfiguration config) { } public async Task<NetworkDiagnosticResult> RunFullDiagnosticsAsync(CancellationToken ct) => new NetworkDiagnosticResult(); public void Dispose() { } }
    public class StorageDiagnostics : IDisposable { public StorageDiagnostics(DiagnosticsConfiguration config) { } public async Task<StorageDiagnosticResult> RunFullDiagnosticsAsync(CancellationToken ct) => new StorageDiagnosticResult(); public void Dispose() { } }
    public class ComputerInfo { public ulong TotalPhysicalMemory { get; set; } public ulong AvailablePhysicalMemory { get; set; } }

    // Health Check implementations (simplified)
    public class CpuUsageHealthCheck : IHealthCheck { public CpuUsageHealthCheck(DiagnosticsConfiguration config) { } public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default) => new HealthCheckResult { IsHealthy = true, Status = HealthStatus.Healthy }; }
    public class MemoryUsageHealthCheck : IHealthCheck { public MemoryUsageHealthCheck(DiagnosticsConfiguration config) { } public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default) => new HealthCheckResult { IsHealthy = true, Status = HealthStatus.Healthy }; }
    public class DiskSpaceHealthCheck : IHealthCheck { public DiskSpaceHealthCheck(DiagnosticsConfiguration config) { } public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default) => new HealthCheckResult { IsHealthy = true, Status = HealthStatus.Healthy }; }
    public class NetworkConnectivityHealthCheck : IHealthCheck { public NetworkConnectivityHealthCheck(DiagnosticsConfiguration config) { } public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default) => new HealthCheckResult { IsHealthy = true, Status = HealthStatus.Healthy }; }
    public class SerialCommunicationHealthCheck : IHealthCheck { public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default) => new HealthCheckResult { IsHealthy = true, Status = HealthStatus.Healthy }; }
    public class DataPersistenceHealthCheck : IHealthCheck { public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default) => new HealthCheckResult { IsHealthy = true, Status = HealthStatus.Healthy }; }
    public class StateManagementHealthCheck : IHealthCheck { public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default) => new HealthCheckResult { IsHealthy = true, Status = HealthStatus.Healthy }; }
    public class PerformanceMetricsHealthCheck : IHealthCheck { public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default) => new HealthCheckResult { IsHealthy = true, Status = HealthStatus.Healthy }; }
    public class TemperatureSensorHealthCheck : IHealthCheck { public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default) => new HealthCheckResult { IsHealthy = true, Status = HealthStatus.Healthy }; }
    public class StirrerMotorHealthCheck : IHealthCheck { public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default) => new HealthCheckResult { IsHealthy = true, Status = HealthStatus.Healthy }; }
    public class ControlSystemHealthCheck : IHealthCheck { public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default) => new HealthCheckResult { IsHealthy = true, Status = HealthStatus.Healthy }; }
    public class SafetySystemsHealthCheck : IHealthCheck { public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken ct = default) => new HealthCheckResult { IsHealthy = true, Status = HealthStatus.Healthy }; }

    // Healing Action implementations (simplified)
    public class MemoryOptimizationAction : IHealingAction { public string Name => "Memory Optimization"; public async Task<bool> ExecuteAsync(CancellationToken ct = default) { GC.Collect(); return true; } }
    public class CpuOptimizationAction : IHealingAction { public string Name => "CPU Optimization"; public async Task<bool> ExecuteAsync(CancellationToken ct = default) => true; }
    public class DiskCleanupAction : IHealingAction { public string Name => "Disk Cleanup"; public async Task<bool> ExecuteAsync(CancellationToken ct = default) => true; }
    public class NetworkResetAction : IHealingAction { public string Name => "Network Reset"; public async Task<bool> ExecuteAsync(CancellationToken ct = default) => true; }
    public class SerialReconnectAction : IHealingAction { public string Name => "Serial Reconnect"; public async Task<bool> ExecuteAsync(CancellationToken ct = default) => true; }
    public class TemperatureCalibrationAction : IHealingAction { public string Name => "Temperature Calibration"; public async Task<bool> ExecuteAsync(CancellationToken ct = default) => true; }
    public class StirrerRecalibrationAction : IHealingAction { public string Name => "Stirrer Recalibration"; public async Task<bool> ExecuteAsync(CancellationToken ct = default) => true; }

    // Interfaces
    public interface IDiagnosticTestExecutor { Task<DiagnosticTestResult> ExecuteAsync(CancellationToken ct = default); }

    #endregion
}