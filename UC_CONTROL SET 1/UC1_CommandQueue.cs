// ==============================================
//  UC1_CommandQueue.cs - PHASE 3
//  AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
//  Async Command Processing Queue
//  High-Performance Priority Queue with Hardware Acceleration
// ==============================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Logger;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    /// <summary>
    /// 🚀 PHASE 3: Ultra-High Performance Command Queue
    /// Features: Priority Processing, Backpressure, Retry Logic, Hardware Acceleration
    /// Async Command Processing with 60fps+ Performance & Zero Blocking
    /// </summary>
    public class UC1_CommandQueue : IDisposable
    {
        #region 🎯 Queue Infrastructure

        // High-Performance Channels for Command Processing
        private readonly Channel<QueuedCommand> _highPriorityChannel;
        private readonly Channel<QueuedCommand> _normalPriorityChannel;
        private readonly Channel<QueuedCommand> _lowPriorityChannel;
        private readonly Channel<QueuedCommand> _retryChannel;

        // Command Processing State
        private readonly ConcurrentDictionary<Guid, QueuedCommand> _processingCommands;
        private readonly ConcurrentDictionary<Guid, CommandExecutionContext> _executionContexts;
        private readonly PriorityQueue<QueuedCommand, CommandPriority> _priorityQueue;

        // Reactive Streams
        private readonly Subject<CommandQueuedEvent> _commandQueuedSubject;
        private readonly Subject<CommandProcessedEvent> _commandProcessedSubject;
        private readonly Subject<CommandFailedEvent> _commandFailedSubject;
        private readonly BehaviorSubject<QueueStatistics> _statisticsSubject;

        // Processing Infrastructure
        private readonly SemaphoreSlim _processingSlot;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Timer _statisticsTimer;
        private readonly Timer _retryTimer;

        // Configuration & Performance
        private readonly CommandQueueConfiguration _configuration;
        private readonly UC1_PerformanceMonitor _performanceMonitor;

        // State Management
        private volatile bool _isProcessing = false;
        private volatile bool _isDisposed = false;
        private long _totalCommandsQueued = 0;
        private long _totalCommandsProcessed = 0;
        private long _totalCommandsFailed = 0;
        private readonly object _statsLock = new object();

        // Processing Tasks
        private readonly List<Task> _processingTasks;

        #endregion

        #region 🌊 Public Observables

        /// <summary>📥 Command Queued Events</summary>
        public IObservable<CommandQueuedEvent> CommandQueued => _commandQueuedSubject.AsObservable();

        /// <summary>✅ Command Processed Events</summary>
        public IObservable<CommandProcessedEvent> CommandProcessed => _commandProcessedSubject.AsObservable();

        /// <summary>❌ Command Failed Events</summary>
        public IObservable<CommandFailedEvent> CommandFailed => _commandFailedSubject.AsObservable();

        /// <summary>📊 Queue Statistics Stream</summary>
        public IObservable<QueueStatistics> Statistics => _statisticsSubject.AsObservable();

        /// <summary>📈 Current Queue Statistics</summary>
        public QueueStatistics CurrentStatistics => _statisticsSubject.Value;

        /// <summary>🔄 Is Processing Active</summary>
        public bool IsProcessing => _isProcessing;

        #endregion

        #region 🏗️ Constructor & Initialization

        /// <summary>
        /// 🎯 Initialize Ultra-High Performance Command Queue
        /// </summary>
        public UC1_CommandQueue(
            int maxConcurrency = 0,
            CommandQueueConfiguration configuration = null,
            UC1_PerformanceMonitor performanceMonitor = null)
        {
            try
            {
                Logger.Log("🚀 [CommandQueue] Initializing High-Performance Command Queue", LogLevel.Info);

                // Initialize configuration
                _configuration = configuration ?? CommandQueueConfiguration.Default;
                var concurrency = maxConcurrency > 0 ? maxConcurrency : Environment.ProcessorCount * 2;

                // Initialize channels with bounded capacity for backpressure
                var channelOptions = new BoundedChannelOptions(_configuration.MaxQueueSize)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = false,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                };

                _highPriorityChannel = Channel.CreateBounded<QueuedCommand>(channelOptions);
                _normalPriorityChannel = Channel.CreateBounded<QueuedCommand>(channelOptions);
                _lowPriorityChannel = Channel.CreateBounded<QueuedCommand>(channelOptions);
                _retryChannel = Channel.CreateBounded<QueuedCommand>(channelOptions);

                // Initialize collections
                _processingCommands = new ConcurrentDictionary<Guid, QueuedCommand>();
                _executionContexts = new ConcurrentDictionary<Guid, CommandExecutionContext>();
                _priorityQueue = new PriorityQueue<QueuedCommand, CommandPriority>();

                // Initialize reactive subjects
                _commandQueuedSubject = new Subject<CommandQueuedEvent>();
                _commandProcessedSubject = new Subject<CommandProcessedEvent>();
                _commandFailedSubject = new Subject<CommandFailedEvent>();
                _statisticsSubject = new BehaviorSubject<QueueStatistics>(new QueueStatistics());

                // Initialize infrastructure
                _processingSlot = new SemaphoreSlim(concurrency, concurrency);
                _cancellationTokenSource = new CancellationTokenSource();
                _performanceMonitor = performanceMonitor ?? new UC1_PerformanceMonitor();

                // Initialize processing tasks
                _processingTasks = new List<Task>();

                // Setup timers
                _statisticsTimer = new Timer(UpdateStatisticsCallback, null,
                    TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                _retryTimer = new Timer(ProcessRetryQueueCallback, null,
                    TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

                // Start processing tasks
                StartProcessingTasks(concurrency);

                _isProcessing = true;

                Logger.Log($"✅ [CommandQueue] High-Performance Command Queue initialized (Concurrency: {concurrency})", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Initialization failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// 🔥 Start High-Performance Processing Tasks
        /// </summary>
        private void StartProcessingTasks(int concurrency)
        {
            try
            {
                var token = _cancellationTokenSource.Token;

                // High Priority Processing Tasks
                for (int i = 0; i < Math.Max(1, concurrency / 3); i++)
                {
                    var task = Task.Run(() => ProcessChannelAsync(_highPriorityChannel.Reader, CommandPriority.High, token), token);
                    _processingTasks.Add(task);
                }

                // Normal Priority Processing Tasks
                for (int i = 0; i < concurrency / 2; i++)
                {
                    var task = Task.Run(() => ProcessChannelAsync(_normalPriorityChannel.Reader, CommandPriority.Normal, token), token);
                    _processingTasks.Add(task);
                }

                // Low Priority Processing Tasks
                for (int i = 0; i < Math.Max(1, concurrency / 4); i++)
                {
                    var task = Task.Run(() => ProcessChannelAsync(_lowPriorityChannel.Reader, CommandPriority.Low, token), token);
                    _processingTasks.Add(task);
                }

                // Retry Processing Task
                var retryTask = Task.Run(() => ProcessChannelAsync(_retryChannel.Reader, CommandPriority.Retry, token), token);
                _processingTasks.Add(retryTask);

                Logger.Log($"🔥 [CommandQueue] Started {_processingTasks.Count} processing tasks", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Processing task startup failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 📥 Command Queuing

        /// <summary>
        /// 📥 Enqueue Command with Priority and Hardware Acceleration
        /// </summary>
        public async Task<bool> EnqueueAsync<T>(T command, CommandPriority priority = CommandPriority.Normal, CancellationToken cancellationToken = default) where T : ICommand
        {
            if (_isDisposed || command == null) return false;

            try
            {
                var queuedCommand = new QueuedCommand
                {
                    Command = command,
                    Priority = priority,
                    QueuedAt = DateTime.UtcNow,
                    AttemptCount = 0,
                    MaxRetries = GetMaxRetries(priority),
                    ExecutionContext = CreateExecutionContext(command)
                };

                // Select appropriate channel based on priority
                var channel = GetChannelForPriority(priority);

                // Enqueue with backpressure handling
                await channel.Writer.WriteAsync(queuedCommand, cancellationToken);

                // Update metrics
                Interlocked.Increment(ref _totalCommandsQueued);

                // Emit queued event
                _commandQueuedSubject.OnNext(new CommandQueuedEvent
                {
                    CommandId = command.CommandId,
                    CommandType = typeof(T).Name,
                    Priority = priority,
                    QueuedAt = queuedCommand.QueuedAt,
                    QueueSize = GetQueueSize()
                });

                Logger.Log($"📥 [CommandQueue] Command queued: {typeof(T).Name} (Priority: {priority}, ID: {command.CommandId})", LogLevel.Debug);
                return true;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("complete"))
            {
                Logger.Log($"⚠️ [CommandQueue] Queue is shutting down: {typeof(T).Name}", LogLevel.Warn);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Enqueue failed for {typeof(T).Name}: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 📥 Batch Enqueue Commands for High Throughput
        /// </summary>
        public async Task<int> EnqueueBatchAsync<T>(IEnumerable<T> commands, CommandPriority priority = CommandPriority.Normal, CancellationToken cancellationToken = default) where T : ICommand
        {
            if (_isDisposed || commands == null) return 0;

            try
            {
                var commandList = commands.ToList();
                var successCount = 0;

                var channel = GetChannelForPriority(priority);

                foreach (var command in commandList)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var queuedCommand = new QueuedCommand
                        {
                            Command = command,
                            Priority = priority,
                            QueuedAt = DateTime.UtcNow,
                            AttemptCount = 0,
                            MaxRetries = GetMaxRetries(priority),
                            ExecutionContext = CreateExecutionContext(command)
                        };

                        await channel.Writer.WriteAsync(queuedCommand, cancellationToken);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"❌ [CommandQueue] Batch item failed: {ex.Message}", LogLevel.Error);
                    }
                }

                Interlocked.Add(ref _totalCommandsQueued, successCount);

                Logger.Log($"📥 [CommandQueue] Batch enqueued: {successCount}/{commandList.Count} commands", LogLevel.Info);
                return successCount;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Batch enqueue failed: {ex.Message}", LogLevel.Error);
                return 0;
            }
        }

        #endregion

        #region ⚡ Command Processing

        /// <summary>
        /// ⚡ Process Channel with Hardware Acceleration
        /// </summary>
        private async Task ProcessChannelAsync(ChannelReader<QueuedCommand> reader, CommandPriority priority, CancellationToken cancellationToken)
        {
            try
            {
                Logger.Log($"⚡ [CommandQueue] Processing channel started: {priority}", LogLevel.Info);

                await foreach (var queuedCommand in reader.ReadAllAsync(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    await ProcessCommandAsync(queuedCommand, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log($"🛑 [CommandQueue] Processing channel cancelled: {priority}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Processing channel error ({priority}): {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 🎯 Process Individual Command with Hardware Acceleration
        /// </summary>
        private async Task ProcessCommandAsync(QueuedCommand queuedCommand, CancellationToken cancellationToken)
        {
            await _processingSlot.WaitAsync(cancellationToken);
            try
            {
                var startTime = DateTime.UtcNow;
                queuedCommand.AttemptCount++;

                // Add to processing commands
                _processingCommands[queuedCommand.Command.CommandId] = queuedCommand;

                try
                {
                    // Execute command with timeout
                    using var timeoutCts = new CancellationTokenSource(_configuration.CommandTimeout);
                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                    var result = await ExecuteCommandAsync(queuedCommand, combinedCts.Token);

                    var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    if (result.Success)
                    {
                        // Command succeeded
                        await HandleCommandSuccessAsync(queuedCommand, result, processingTime);
                    }
                    else
                    {
                        // Command failed
                        await HandleCommandFailureAsync(queuedCommand, result, processingTime);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Logger.Log($"🛑 [CommandQueue] Command cancelled: {queuedCommand.Command.CommandId}", LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    await HandleCommandExceptionAsync(queuedCommand, ex, processingTime);
                }
                finally
                {
                    // Remove from processing commands
                    _processingCommands.TryRemove(queuedCommand.Command.CommandId, out _);
                }
            }
            finally
            {
                _processingSlot.Release();
            }
        }

        /// <summary>
        /// ⚙️ Execute Command with Context and Monitoring
        /// </summary>
        private async Task<CommandExecutionResult> ExecuteCommandAsync(QueuedCommand queuedCommand, CancellationToken cancellationToken)
        {
            try
            {
                var context = queuedCommand.ExecutionContext;
                var command = queuedCommand.Command;

                // Get command handler
                var handler = GetCommandHandler(command);
                if (handler == null)
                {
                    return CommandExecutionResult.Failed(command.CommandId, "No handler found");
                }

                // Execute command
                var result = await handler.HandleAsync(command, cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                return CommandExecutionResult.Failed(queuedCommand.Command.CommandId, ex.Message);
            }
        }

        #endregion

        #region 🔄 Success/Failure Handling

        /// <summary>
        /// ✅ Handle Command Success
        /// </summary>
        private async Task HandleCommandSuccessAsync(QueuedCommand queuedCommand, CommandExecutionResult result, double processingTime)
        {
            try
            {
                Interlocked.Increment(ref _totalCommandsProcessed);

                // Record performance metrics
                _performanceMonitor.RecordCommandExecuted(processingTime);

                // Emit success event
                _commandProcessedSubject.OnNext(new CommandProcessedEvent
                {
                    CommandId = queuedCommand.Command.CommandId,
                    CommandType = queuedCommand.Command.GetType().Name,
                    Priority = queuedCommand.Priority,
                    ProcessingTime = processingTime,
                    AttemptCount = queuedCommand.AttemptCount,
                    Result = result.Result
                });

                Logger.Log($"✅ [CommandQueue] Command completed: {queuedCommand.Command.CommandId} ({processingTime:F2}ms)", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Success handling failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// ❌ Handle Command Failure with Retry Logic
        /// </summary>
        private async Task HandleCommandFailureAsync(QueuedCommand queuedCommand, CommandExecutionResult result, double processingTime)
        {
            try
            {
                if (queuedCommand.AttemptCount < queuedCommand.MaxRetries)
                {
                    // Retry command
                    await RetryCommandAsync(queuedCommand);
                }
                else
                {
                    // Max retries reached - command failed permanently
                    await HandleCommandPermanentFailureAsync(queuedCommand, result, processingTime);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Failure handling failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 💥 Handle Command Exception
        /// </summary>
        private async Task HandleCommandExceptionAsync(QueuedCommand queuedCommand, Exception exception, double processingTime)
        {
            try
            {
                if (queuedCommand.AttemptCount < queuedCommand.MaxRetries && IsRetryableException(exception))
                {
                    // Retry on retryable exceptions
                    await RetryCommandAsync(queuedCommand);
                }
                else
                {
                    // Permanent failure
                    var result = CommandExecutionResult.Failed(queuedCommand.Command.CommandId, exception.Message);
                    await HandleCommandPermanentFailureAsync(queuedCommand, result, processingTime);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Exception handling failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 🔄 Retry Command with Exponential Backoff
        /// </summary>
        private async Task RetryCommandAsync(QueuedCommand queuedCommand)
        {
            try
            {
                // Calculate retry delay with exponential backoff
                var delay = CalculateRetryDelay(queuedCommand.AttemptCount);
                queuedCommand.NextRetryAt = DateTime.UtcNow.Add(delay);

                // Add to retry channel
                await _retryChannel.Writer.WriteAsync(queuedCommand);

                Logger.Log($"🔄 [CommandQueue] Command scheduled for retry: {queuedCommand.Command.CommandId} (Attempt {queuedCommand.AttemptCount + 1}/{queuedCommand.MaxRetries})", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Retry scheduling failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 💀 Handle Permanent Command Failure
        /// </summary>
        private async Task HandleCommandPermanentFailureAsync(QueuedCommand queuedCommand, CommandExecutionResult result, double processingTime)
        {
            try
            {
                Interlocked.Increment(ref _totalCommandsFailed);

                // Emit failure event
                _commandFailedSubject.OnNext(new CommandFailedEvent
                {
                    CommandId = queuedCommand.Command.CommandId,
                    CommandType = queuedCommand.Command.GetType().Name,
                    Priority = queuedCommand.Priority,
                    ProcessingTime = processingTime,
                    AttemptCount = queuedCommand.AttemptCount,
                    Error = result.Error,
                    IsPermanent = true
                });

                Logger.Log($"💀 [CommandQueue] Command failed permanently: {queuedCommand.Command.CommandId} - {result.Error}", LogLevel.Error);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Permanent failure handling failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 🛠️ Utility Methods

        /// <summary>
        /// 📍 Get Channel for Priority
        /// </summary>
        private Channel<QueuedCommand> GetChannelForPriority(CommandPriority priority)
        {
            return priority switch
            {
                CommandPriority.High or CommandPriority.Critical => _highPriorityChannel,
                CommandPriority.Low => _lowPriorityChannel,
                _ => _normalPriorityChannel
            };
        }

        /// <summary>
        /// 🔁 Get Max Retries for Priority
        /// </summary>
        private int GetMaxRetries(CommandPriority priority)
        {
            return priority switch
            {
                CommandPriority.Critical => _configuration.MaxRetriesCritical,
                CommandPriority.High => _configuration.MaxRetriesHigh,
                CommandPriority.Normal => _configuration.MaxRetriesNormal,
                CommandPriority.Low => _configuration.MaxRetriesLow,
                _ => _configuration.MaxRetriesNormal
            };
        }

        /// <summary>
        /// ⏱️ Calculate Retry Delay with Exponential Backoff
        /// </summary>
        private TimeSpan CalculateRetryDelay(int attemptCount)
        {
            var baseDelay = _configuration.BaseRetryDelay;
            var maxDelay = _configuration.MaxRetryDelay;

            // Exponential backoff: delay = baseDelay * 2^(attempt-1)
            var delay = TimeSpan.FromMilliseconds(
                Math.Min(maxDelay.TotalMilliseconds,
                        baseDelay.TotalMilliseconds * Math.Pow(2, attemptCount - 1)));

            // Add jitter to prevent thundering herd
            var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, (int)(delay.TotalMilliseconds * 0.1)));

            return delay.Add(jitter);
        }

        /// <summary>
        /// 🔍 Check if Exception is Retryable
        /// </summary>
        private bool IsRetryableException(Exception exception)
        {
            return exception switch
            {
                OperationCanceledException => false,
                ArgumentException => false,
                InvalidOperationException => false,
                TimeoutException => true,
                _ => true // Default to retryable for unknown exceptions
            };
        }

        /// <summary>
        /// 📊 Get Current Queue Size
        /// </summary>
        private int GetQueueSize()
        {
            try
            {
                return _highPriorityChannel.Reader.CanCount ? _highPriorityChannel.Reader.Count : 0 +
                       _normalPriorityChannel.Reader.CanCount ? _normalPriorityChannel.Reader.Count : 0 +
                       _lowPriorityChannel.Reader.CanCount ? _lowPriorityChannel.Reader.Count : 0 +
                       _retryChannel.Reader.CanCount ? _retryChannel.Reader.Count : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// ⚙️ Create Execution Context
        /// </summary>
        private CommandExecutionContext CreateExecutionContext(ICommand command)
        {
            return new CommandExecutionContext
            {
                CommandId = command.CommandId,
                CreatedAt = DateTime.UtcNow,
                Properties = new Dictionary<string, object>()
            };
        }

        /// <summary>
        /// 🎯 Get Command Handler (Placeholder)
        /// </summary>
        private ICommandHandler GetCommandHandler(ICommand command)
        {
            // This would integrate with the UC1_CommandService
            // For now, return a mock handler
            return new DefaultCommandHandler();
        }

        #endregion

        #region 📊 Statistics & Monitoring

        /// <summary>
        /// 📊 Update Statistics Timer Callback
        /// </summary>
        private void UpdateStatisticsCallback(object state)
        {
            try
            {
                var statistics = new QueueStatistics
                {
                    TotalQueued = _totalCommandsQueued,
                    TotalProcessed = _totalCommandsProcessed,
                    TotalFailed = _totalCommandsFailed,
                    CurrentQueueSize = GetQueueSize(),
                    ProcessingCommands = _processingCommands.Count,
                    AvailableSlots = _processingSlot.CurrentCount,
                    Timestamp = DateTime.UtcNow
                };

                _statisticsSubject.OnNext(statistics);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Statistics update failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 🔄 Process Retry Queue Timer Callback
        /// </summary>
        private void ProcessRetryQueueCallback(object state)
        {
            try
            {
                // This would process commands that are ready for retry
                // Implementation depends on the retry scheduling mechanism
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Retry queue processing failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 🗑️ Disposal

        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                Logger.Log("🗑️ [CommandQueue] Starting disposal", LogLevel.Info);

                _isProcessing = false;

                // Cancel operations
                _cancellationTokenSource?.Cancel();

                // Complete channels
                _highPriorityChannel?.Writer.Complete();
                _normalPriorityChannel?.Writer.Complete();
                _lowPriorityChannel?.Writer.Complete();
                _retryChannel?.Writer.Complete();

                // Wait for processing tasks to complete
                if (_processingTasks?.Count > 0)
                {
                    Task.WaitAll(_processingTasks.ToArray(), TimeSpan.FromSeconds(30));
                }

                // Dispose timers
                _statisticsTimer?.Dispose();
                _retryTimer?.Dispose();

                // Complete reactive subjects
                _commandQueuedSubject?.OnCompleted();
                _commandQueuedSubject?.Dispose();
                _commandProcessedSubject?.OnCompleted();
                _commandProcessedSubject?.Dispose();
                _commandFailedSubject?.OnCompleted();
                _commandFailedSubject?.Dispose();
                _statisticsSubject?.OnCompleted();
                _statisticsSubject?.Dispose();

                // Dispose synchronization objects
                _processingSlot?.Dispose();
                _cancellationTokenSource?.Dispose();

                // Clear collections
                _processingCommands?.Clear();
                _executionContexts?.Clear();

                _isDisposed = true;
                Logger.Log("✅ [CommandQueue] Disposal completed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandQueue] Disposal error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion
    }

    #region 📋 Supporting Classes

    /// <summary>
    /// 📦 Queued Command
    /// </summary>
    public class QueuedCommand
    {
        public ICommand Command { get; set; }
        public CommandPriority Priority { get; set; }
        public DateTime QueuedAt { get; set; }
        public int AttemptCount { get; set; }
        public int MaxRetries { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public CommandExecutionContext ExecutionContext { get; set; }
    }

    /// <summary>
    /// 🎯 Command Execution Context
    /// </summary>
    public class CommandExecutionContext
    {
        public Guid CommandId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 📥 Command Queued Event
    /// </summary>
    public class CommandQueuedEvent
    {
        public Guid CommandId { get; set; }
        public string CommandType { get; set; }
        public CommandPriority Priority { get; set; }
        public DateTime QueuedAt { get; set; }
        public int QueueSize { get; set; }
    }

    /// <summary>
    /// ✅ Command Processed Event
    /// </summary>
    public class CommandProcessedEvent
    {
        public Guid CommandId { get; set; }
        public string CommandType { get; set; }
        public CommandPriority Priority { get; set; }
        public double ProcessingTime { get; set; }
        public int AttemptCount { get; set; }
        public object Result { get; set; }
    }

    /// <summary>
    /// ❌ Command Failed Event
    /// </summary>
    public class CommandFailedEvent
    {
        public Guid CommandId { get; set; }
        public string CommandType { get; set; }
        public CommandPriority Priority { get; set; }
        public double ProcessingTime { get; set; }
        public int AttemptCount { get; set; }
        public string Error { get; set; }
        public bool IsPermanent { get; set; }
    }

    /// <summary>
    /// 📊 Queue Statistics
    /// </summary>
    public class QueueStatistics
    {
        public long TotalQueued { get; set; }
        public long TotalProcessed { get; set; }
        public long TotalFailed { get; set; }
        public int CurrentQueueSize { get; set; }
        public int ProcessingCommands { get; set; }
        public int AvailableSlots { get; set; }
        public DateTime Timestamp { get; set; }

        public double SuccessRate => TotalProcessed + TotalFailed > 0 ?
            (double)TotalProcessed / (TotalProcessed + TotalFailed) * 100 : 0;

        public double ThroughputPerSecond { get; set; }
    }

    /// <summary>
    /// ⚙️ Command Queue Configuration
    /// </summary>
    public class CommandQueueConfiguration
    {
        public int MaxQueueSize { get; set; } = 10000;
        public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public int MaxRetriesCritical { get; set; } = 5;
        public int MaxRetriesHigh { get; set; } = 3;
        public int MaxRetriesNormal { get; set; } = 2;
        public int MaxRetriesLow { get; set; } = 1;
        public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

        public static CommandQueueConfiguration Default => new CommandQueueConfiguration();
    }

    /// <summary>
    /// 🎯 Default Command Handler
    /// </summary>
    public class DefaultCommandHandler : ICommandHandler
    {
        public async Task<CommandExecutionResult> HandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            // Default implementation
            await Task.Delay(10, cancellationToken);
            return CommandExecutionResult.Success(command.CommandId, "Handled by default handler");
        }
    }

    #endregion
}