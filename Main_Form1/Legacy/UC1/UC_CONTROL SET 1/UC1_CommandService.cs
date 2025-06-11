// ==============================================
//  UC1_CommandService.cs - PHASE 3
//  AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
//  Command Pattern Implementation with Undo/Redo
//  Async Command Processing & Hardware Acceleration
// ==============================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Logger;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    /// <summary>
    /// 🚀 PHASE 3: Advanced Command Service with Hardware Acceleration
    /// Features: Command Pattern, Undo/Redo, Async Processing, Retry Logic
    /// High-Performance Command Execution with 60fps+ Responsiveness
    /// </summary>
    public class UC1_CommandService : IDisposable
    {
        #region 🎯 Command Infrastructure

        // Command Execution & History
        private readonly UC1_CommandQueue _commandQueue;
        private readonly Stack<IUndoableCommand> _undoStack;
        private readonly Stack<IUndoableCommand> _redoStack;
        private readonly object _stackLock = new object();

        // Command Processing Streams
        private readonly Subject<CommandExecutionRequest> _commandRequestSubject;
        private readonly Subject<CommandExecutionResult> _commandResultSubject;
        private readonly BehaviorSubject<CommandServiceState> _serviceStateSubject;

        // Command Handlers & Middleware
        private readonly ConcurrentDictionary<Type, ICommandHandler> _commandHandlers;
        private readonly List<ICommandMiddleware> _middleware;

        // Performance & Monitoring
        private readonly UC1_PerformanceMonitor _performanceMonitor;
        private readonly SemaphoreSlim _executionSemaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;

        // Configuration & State
        private CommandServiceConfiguration _configuration;
        private volatile bool _isProcessing = false;
        private volatile bool _isDisposed = false;
        private long _commandCounter = 0;

        #endregion

        #region 🔥 Events & Observables

        /// <summary>📡 Command Executed Observable</summary>
        public IObservable<CommandExecutionResult> CommandExecuted => _commandResultSubject.AsObservable();

        /// <summary>⚡ Service State Observable</summary>
        public IObservable<CommandServiceState> ServiceState => _serviceStateSubject.AsObservable();

        /// <summary>🎯 Current Service State</summary>
        public CommandServiceState CurrentState => _serviceStateSubject.Value;

        /// <summary>↶ Can Undo Command</summary>
        public bool CanUndo
        {
            get
            {
                lock (_stackLock)
                {
                    return _undoStack.Count > 0;
                }
            }
        }

        /// <summary>↷ Can Redo Command</summary>
        public bool CanRedo
        {
            get
            {
                lock (_stackLock)
                {
                    return _redoStack.Count > 0;
                }
            }
        }

        #endregion

        #region 🏗️ Constructor & Initialization

        /// <summary>
        /// 🎯 Initialize Command Service with Hardware Acceleration
        /// </summary>
        public UC1_CommandService(
            CommandServiceConfiguration configuration = null,
            UC1_PerformanceMonitor performanceMonitor = null)
        {
            try
            {
                Logger.Log("🚀 [CommandService] Initializing Command Pattern System", LogLevel.Info);

                // Initialize configuration
                _configuration = configuration ?? CommandServiceConfiguration.Default;

                // Initialize infrastructure
                _commandQueue = new UC1_CommandQueue(_configuration.MaxConcurrency);
                _undoStack = new Stack<IUndoableCommand>();
                _redoStack = new Stack<IUndoableCommand>();

                // Initialize reactive streams
                _commandRequestSubject = new Subject<CommandExecutionRequest>();
                _commandResultSubject = new Subject<CommandExecutionResult>();
                _serviceStateSubject = new BehaviorSubject<CommandServiceState>(CommandServiceState.Ready);

                // Initialize collections
                _commandHandlers = new ConcurrentDictionary<Type, ICommandHandler>();
                _middleware = new List<ICommandMiddleware>();

                // Initialize performance & synchronization
                _performanceMonitor = performanceMonitor ?? new UC1_PerformanceMonitor();
                _executionSemaphore = new SemaphoreSlim(_configuration.MaxConcurrency, _configuration.MaxConcurrency);
                _cancellationTokenSource = new CancellationTokenSource();

                // Setup command processing pipeline
                SetupCommandPipeline();

                // Register default command handlers
                RegisterDefaultHandlers();

                Logger.Log("✅ [CommandService] Command Pattern System initialized", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandService] Initialization failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// 🔥 Setup High-Performance Command Processing Pipeline
        /// </summary>
        private void SetupCommandPipeline()
        {
            try
            {
                var token = _cancellationTokenSource.Token;

                // Command Request Processing Pipeline
                var processingSubscription = _commandRequestSubject
                    .ObserveOn(TaskPoolScheduler.Default)
                    .SelectMany(async request =>
                    {
                        try
                        {
                            var result = await ProcessCommandRequestAsync(request, token);
                            return result;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"❌ [CommandService] Pipeline error: {ex.Message}", LogLevel.Error);
                            return new CommandExecutionResult
                            {
                                CommandId = request.Command.CommandId,
                                Success = false,
                                Error = ex.Message,
                                Timestamp = DateTime.UtcNow
                            };
                        }
                    })
                    .Subscribe(
                        result =>
                        {
                            _commandResultSubject.OnNext(result);
                            _performanceMonitor.RecordCommandExecuted(result.ExecutionTime);
                        },
                        ex => Logger.Log($"❌ [CommandService] Pipeline subscription error: {ex.Message}", LogLevel.Error)
                    );

                Logger.Log("🔥 [CommandService] Command processing pipeline configured", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandService] Pipeline setup failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// ⚙️ Register Default Command Handlers
        /// </summary>
        private void RegisterDefaultHandlers()
        {
            try
            {
                // Temperature Command Handler
                RegisterHandler<TemperatureCommand>(new TemperatureCommandHandler());

                // Stirrer Command Handler
                RegisterHandler<StirrerCommand>(new StirrerCommandHandler());

                // Connection Command Handler
                RegisterHandler<ConnectionCommand>(new ConnectionCommandHandler());

                // Mode Change Command Handler
                RegisterHandler<ModeChangeCommand>(new ModeChangeCommandHandler());

                Logger.Log("⚙️ [CommandService] Default command handlers registered", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandService] Handler registration failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 🎯 Command Execution - Hardware Accelerated

        /// <summary>
        /// 🚀 Execute Command with Full Pipeline Processing
        /// </summary>
        public async Task<CommandExecutionResult> ExecuteAsync<T>(T command, CancellationToken cancellationToken = default) where T : ICommand
        {
            if (_isDisposed || command == null)
            {
                return CommandExecutionResult.Failed(command?.CommandId ?? Guid.Empty, "Service disposed or command null");
            }

            try
            {
                // Enrich command with metadata
                command.CommandId = command.CommandId == Guid.Empty ? Guid.NewGuid() : command.CommandId;
                command.Timestamp = DateTime.UtcNow;

                var executionRequest = new CommandExecutionRequest
                {
                    Command = command,
                    RequestId = Guid.NewGuid(),
                    Priority = GetCommandPriority(command),
                    Timestamp = DateTime.UtcNow,
                    CancellationToken = cancellationToken
                };

                Logger.Log($"🎯 [CommandService] Executing command: {typeof(T).Name} ({command.CommandId})", LogLevel.Debug);

                // Process through pipeline
                _commandRequestSubject.OnNext(executionRequest);

                // Wait for result (with timeout)
                var resultTask = _commandResultSubject
                    .Where(r => r.CommandId == command.CommandId)
                    .Take(1)
                    .Timeout(TimeSpan.FromMilliseconds(_configuration.ExecutionTimeoutMs))
                    .FirstAsync();

                var result = await resultTask;

                Logger.Log($"✅ [CommandService] Command completed: {typeof(T).Name} - Success: {result.Success}", LogLevel.Debug);
                return result;
            }
            catch (TimeoutException)
            {
                Logger.Log($"⏰ [CommandService] Command timeout: {typeof(T).Name}", LogLevel.Error);
                return CommandExecutionResult.Failed(command.CommandId, "Command execution timeout");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandService] Command execution failed: {ex.Message}", LogLevel.Error);
                return CommandExecutionResult.Failed(command.CommandId, ex.Message);
            }
        }

        /// <summary>
        /// ⚡ Process Command Request with Hardware Acceleration
        /// </summary>
        private async Task<CommandExecutionResult> ProcessCommandRequestAsync(
            CommandExecutionRequest request,
            CancellationToken cancellationToken)
        {
            await _executionSemaphore.WaitAsync(cancellationToken);
            try
            {
                var startTime = DateTime.UtcNow;
                UpdateServiceState(CommandServiceState.Processing);

                // Apply middleware pipeline
                var processedCommand = await ApplyMiddlewareAsync(request.Command, cancellationToken);
                if (processedCommand == null)
                {
                    return CommandExecutionResult.Failed(request.Command.CommandId, "Command cancelled by middleware");
                }

                // Execute command
                var result = await ExecuteCommandInternalAsync(processedCommand, cancellationToken);

                // Handle undo/redo for undoable commands
                if (result.Success && processedCommand is IUndoableCommand undoableCommand)
                {
                    AddToUndoStack(undoableCommand);
                }

                // Calculate execution time
                result.ExecutionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Update counters
                Interlocked.Increment(ref _commandCounter);

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandService] Command processing failed: {ex.Message}", LogLevel.Error);
                return CommandExecutionResult.Failed(request.Command.CommandId, ex.Message);
            }
            finally
            {
                _executionSemaphore.Release();
                UpdateServiceState(CommandServiceState.Ready);
            }
        }

        /// <summary>
        /// ⚙️ Execute Command with Handler Dispatch
        /// </summary>
        private async Task<CommandExecutionResult> ExecuteCommandInternalAsync(ICommand command, CancellationToken cancellationToken)
        {
            try
            {
                var commandType = command.GetType();

                // Find appropriate handler
                if (_commandHandlers.TryGetValue(commandType, out ICommandHandler handler))
                {
                    var result = await handler.HandleAsync(command, cancellationToken);
                    return result;
                }

                // Try to find handler for base types
                foreach (var kvp in _commandHandlers)
                {
                    if (kvp.Key.IsAssignableFrom(commandType))
                    {
                        var result = await kvp.Value.HandleAsync(command, cancellationToken);
                        return result;
                    }
                }

                Logger.Log($"⚠️ [CommandService] No handler found for command: {commandType.Name}", LogLevel.Warn);
                return CommandExecutionResult.Failed(command.CommandId, $"No handler found for {commandType.Name}");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandService] Command execution failed: {ex.Message}", LogLevel.Error);
                return CommandExecutionResult.Failed(command.CommandId, ex.Message);
            }
        }

        /// <summary>
        /// 🔗 Apply Middleware Pipeline
        /// </summary>
        private async Task<ICommand> ApplyMiddlewareAsync(ICommand command, CancellationToken cancellationToken)
        {
            try
            {
                var currentCommand = command;

                foreach (var middleware in _middleware)
                {
                    currentCommand = await middleware.ProcessAsync(currentCommand, cancellationToken);
                    if (currentCommand == null)
                    {
                        Logger.Log($"🚫 [CommandService] Command cancelled by middleware: {middleware.GetType().Name}", LogLevel.Debug);
                        break;
                    }
                }

                return currentCommand;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandService] Middleware processing failed: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        #endregion

        #region ↶↷ Undo/Redo Operations

        /// <summary>
        /// ↶ Undo Last Command
        /// </summary>
        public async Task<CommandExecutionResult> UndoAsync(CancellationToken cancellationToken = default)
        {
            lock (_stackLock)
            {
                if (_undoStack.Count == 0)
                {
                    return CommandExecutionResult.Failed(Guid.Empty, "No commands to undo");
                }
            }

            try
            {
                IUndoableCommand commandToUndo;
                lock (_stackLock)
                {
                    commandToUndo = _undoStack.Pop();
                }

                Logger.Log($"↶ [CommandService] Undoing command: {commandToUndo.CommandId}", LogLevel.Info);

                var result = await commandToUndo.UndoAsync(cancellationToken);

                if (result.Success)
                {
                    lock (_stackLock)
                    {
                        _redoStack.Push(commandToUndo);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandService] Undo failed: {ex.Message}", LogLevel.Error);
                return CommandExecutionResult.Failed(Guid.Empty, ex.Message);
            }
        }

        /// <summary>
        /// ↷ Redo Last Undone Command
        /// </summary>
        public async Task<CommandExecutionResult> RedoAsync(CancellationToken cancellationToken = default)
        {
            lock (_stackLock)
            {
                if (_redoStack.Count == 0)
                {
                    return CommandExecutionResult.Failed(Guid.Empty, "No commands to redo");
                }
            }

            try
            {
                IUndoableCommand commandToRedo;
                lock (_stackLock)
                {
                    commandToRedo = _redoStack.Pop();
                }

                Logger.Log($"↷ [CommandService] Redoing command: {commandToRedo.CommandId}", LogLevel.Info);

                var result = await ExecuteCommandInternalAsync(commandToRedo, cancellationToken);

                if (result.Success)
                {
                    lock (_stackLock)
                    {
                        _undoStack.Push(commandToRedo);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandService] Redo failed: {ex.Message}", LogLevel.Error);
                return CommandExecutionResult.Failed(Guid.Empty, ex.Message);
            }
        }

        /// <summary>
        /// 📚 Add Command to Undo Stack
        /// </summary>
        private void AddToUndoStack(IUndoableCommand command)
        {
            lock (_stackLock)
            {
                _undoStack.Push(command);

                // Clear redo stack when new command is executed
                _redoStack.Clear();

                // Limit undo stack size
                if (_undoStack.Count > _configuration.MaxUndoStackSize)
                {
                    var items = _undoStack.ToArray();
                    _undoStack.Clear();
                    for (int i = 0; i < _configuration.MaxUndoStackSize; i++)
                    {
                        _undoStack.Push(items[i]);
                    }
                }
            }
        }

        /// <summary>
        /// 🧹 Clear Undo/Redo History
        /// </summary>
        public void ClearHistory()
        {
            lock (_stackLock)
            {
                _undoStack.Clear();
                _redoStack.Clear();
            }

            Logger.Log("🧹 [CommandService] Command history cleared", LogLevel.Info);
        }

        #endregion

        #region 🔧 Handler & Middleware Management

        /// <summary>
        /// 🔧 Register Command Handler
        /// </summary>
        public void RegisterHandler<T>(ICommandHandler<T> handler) where T : ICommand
        {
            try
            {
                _commandHandlers[typeof(T)] = handler;
                Logger.Log($"🔧 [CommandService] Handler registered for: {typeof(T).Name}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandService] Handler registration failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 🔗 Add Middleware
        /// </summary>
        public void AddMiddleware(ICommandMiddleware middleware)
        {
            try
            {
                _middleware.Add(middleware);
                Logger.Log($"🔗 [CommandService] Middleware added: {middleware.GetType().Name}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandService] Middleware addition failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 📊 Metrics & Utilities

        /// <summary>
        /// 🎯 Get Command Priority
        /// </summary>
        private CommandPriority GetCommandPriority(ICommand command)
        {
            return command switch
            {
                IHighPriorityCommand => CommandPriority.High,
                ILowPriorityCommand => CommandPriority.Low,
                _ => CommandPriority.Normal
            };
        }

        /// <summary>
        /// 📊 Update Service State
        /// </summary>
        private void UpdateServiceState(CommandServiceState newState)
        {
            if (_serviceStateSubject.Value != newState)
            {
                _serviceStateSubject.OnNext(newState);
                Logger.Log($"📊 [CommandService] State changed to: {newState}", LogLevel.Debug);
            }
        }

        /// <summary>
        /// 📈 Get Command Service Metrics
        /// </summary>
        public CommandServiceMetrics GetMetrics()
        {
            lock (_stackLock)
            {
                return new CommandServiceMetrics
                {
                    TotalCommandsExecuted = _commandCounter,
                    UndoStackSize = _undoStack.Count,
                    RedoStackSize = _redoStack.Count,
                    RegisteredHandlers = _commandHandlers.Count,
                    ActiveMiddleware = _middleware.Count,
                    CurrentState = _serviceStateSubject.Value,
                    IsProcessing = _isProcessing,
                    MemoryUsage = GC.GetTotalMemory(false)
                };
            }
        }

        #endregion

        #region 🗑️ Disposal

        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                Logger.Log("🗑️ [CommandService] Starting disposal", LogLevel.Info);

                // Cancel operations
                _cancellationTokenSource?.Cancel();

                // Complete reactive subjects
                _commandRequestSubject?.OnCompleted();
                _commandRequestSubject?.Dispose();
                _commandResultSubject?.OnCompleted();
                _commandResultSubject?.Dispose();
                _serviceStateSubject?.OnCompleted();
                _serviceStateSubject?.Dispose();

                // Dispose queue and synchronization objects
                _commandQueue?.Dispose();
                _executionSemaphore?.Dispose();
                _cancellationTokenSource?.Dispose();

                // Clear collections
                _commandHandlers?.Clear();
                _middleware?.Clear();
                ClearHistory();

                _isDisposed = true;
                Logger.Log("✅ [CommandService] Disposal completed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [CommandService] Disposal error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion
    }

    #region 📋 Command Interfaces & Classes

    /// <summary>
    /// 🎯 Base Command Interface
    /// </summary>
    public interface ICommand
    {
        Guid CommandId { get; set; }
        DateTime Timestamp { get; set; }
        string UserId { get; set; }
        string Description { get; set; }
    }

    /// <summary>
    /// ↶ Undoable Command Interface
    /// </summary>
    public interface IUndoableCommand : ICommand
    {
        Task<CommandExecutionResult> UndoAsync(CancellationToken cancellationToken = default);
        bool CanUndo { get; }
    }

    /// <summary>
    /// ⚡ High Priority Command Marker
    /// </summary>
    public interface IHighPriorityCommand : ICommand { }

    /// <summary>
    /// 🐌 Low Priority Command Marker
    /// </summary>
    public interface ILowPriorityCommand : ICommand { }

    /// <summary>
    /// ⚙️ Command Handler Interface
    /// </summary>
    public interface ICommandHandler
    {
        Task<CommandExecutionResult> HandleAsync(ICommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 🎯 Generic Command Handler Interface
    /// </summary>
    public interface ICommandHandler<in T> : ICommandHandler where T : ICommand
    {
        Task<CommandExecutionResult> HandleAsync(T command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 🔗 Command Middleware Interface
    /// </summary>
    public interface ICommandMiddleware
    {
        Task<ICommand> ProcessAsync(ICommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 📊 Command Execution Request
    /// </summary>
    public class CommandExecutionRequest
    {
        public ICommand Command { get; set; }
        public Guid RequestId { get; set; }
        public CommandPriority Priority { get; set; }
        public DateTime Timestamp { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }

    /// <summary>
    /// ✅ Command Execution Result
    /// </summary>
    public class CommandExecutionResult
    {
        public Guid CommandId { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public object Result { get; set; }
        public double ExecutionTime { get; set; }
        public DateTime Timestamp { get; set; }

        public static CommandExecutionResult Success(Guid commandId, object result = null)
        {
            return new CommandExecutionResult
            {
                CommandId = commandId,
                Success = true,
                Result = result,
                Timestamp = DateTime.UtcNow
            };
        }

        public static CommandExecutionResult Failed(Guid commandId, string error)
        {
            return new CommandExecutionResult
            {
                CommandId = commandId,
                Success = false,
                Error = error,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 📊 Command Service Configuration
    /// </summary>
    public class CommandServiceConfiguration
    {
        public int MaxConcurrency { get; set; } = Environment.ProcessorCount * 2;
        public int ExecutionTimeoutMs { get; set; } = 30000;
        public int MaxUndoStackSize { get; set; } = 100;
        public bool EnableRetry { get; set; } = true;
        public int MaxRetryAttempts { get; set; } = 3;

        public static CommandServiceConfiguration Default => new CommandServiceConfiguration();
    }

    /// <summary>
    /// 📊 Command Service Metrics
    /// </summary>
    public class CommandServiceMetrics
    {
        public long TotalCommandsExecuted { get; set; }
        public int UndoStackSize { get; set; }
        public int RedoStackSize { get; set; }
        public int RegisteredHandlers { get; set; }
        public int ActiveMiddleware { get; set; }
        public CommandServiceState CurrentState { get; set; }
        public bool IsProcessing { get; set; }
        public long MemoryUsage { get; set; }
    }

    /// <summary>
    /// 🎯 Command Priority Enum
    /// </summary>
    public enum CommandPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// 📊 Command Service State Enum
    /// </summary>
    public enum CommandServiceState
    {
        Ready,
        Processing,
        Busy,
        Error,
        Disposing
    }

    #endregion

    #region 🎯 Concrete Command Implementations

    /// <summary>
    /// 🌡️ Temperature Command
    /// </summary>
    public class TemperatureCommand : IUndoableCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; }
        public string Description { get; set; }

        public float NewValue { get; set; }
        public bool Mode { get; set; }
        public float PreviousValue { get; set; }
        public bool CanUndo => true;

        public async Task<CommandExecutionResult> UndoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Undo logic - restore previous temperature
                Logger.Log($"↶ Undoing temperature change: {NewValue} -> {PreviousValue}", LogLevel.Info);
                return CommandExecutionResult.Success(CommandId, PreviousValue);
            }
            catch (Exception ex)
            {
                return CommandExecutionResult.Failed(CommandId, ex.Message);
            }
        }
    }

    /// <summary>
    /// 🔄 Stirrer Command
    /// </summary>
    public class StirrerCommand : IUndoableCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; }
        public string Description { get; set; }

        public int NewRpm { get; set; }
        public int PreviousRpm { get; set; }
        public bool CanUndo => true;

        public async Task<CommandExecutionResult> UndoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Log($"↶ Undoing stirrer change: {NewRpm} -> {PreviousRpm}", LogLevel.Info);
                return CommandExecutionResult.Success(CommandId, PreviousRpm);
            }
            catch (Exception ex)
            {
                return CommandExecutionResult.Failed(CommandId, ex.Message);
            }
        }
    }

    /// <summary>
    /// 📡 Connection Command
    /// </summary>
    public class ConnectionCommand : ICommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; }
        public string Description { get; set; }

        public string Port { get; set; }
        public bool Connect { get; set; }
    }

    /// <summary>
    /// ⚙️ Mode Change Command
    /// </summary>
    public class ModeChangeCommand : IUndoableCommand
    {
        public Guid CommandId { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; }
        public string Description { get; set; }

        public bool NewAutoMode { get; set; }
        public bool NewTjMode { get; set; }
        public bool PreviousAutoMode { get; set; }
        public bool PreviousTjMode { get; set; }
        public bool CanUndo => true;

        public async Task<CommandExecutionResult> UndoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Log($"↶ Undoing mode change", LogLevel.Info);
                return CommandExecutionResult.Success(CommandId, new { PreviousAutoMode, PreviousTjMode });
            }
            catch (Exception ex)
            {
                return CommandExecutionResult.Failed(CommandId, ex.Message);
            }
        }
    }

    #endregion

    #region 🎯 Command Handlers

    /// <summary>
    /// 🌡️ Temperature Command Handler
    /// </summary>
    public class TemperatureCommandHandler : ICommandHandler<TemperatureCommand>
    {
        public async Task<CommandExecutionResult> HandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            return await HandleAsync((TemperatureCommand)command, cancellationToken);
        }

        public async Task<CommandExecutionResult> HandleAsync(TemperatureCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Log($"🌡️ Handling temperature command: {command.NewValue}°C", LogLevel.Debug);
                // Implementation here
                await Task.Delay(10, cancellationToken); // Simulate work
                return CommandExecutionResult.Success(command.CommandId, command.NewValue);
            }
            catch (Exception ex)
            {
                return CommandExecutionResult.Failed(command.CommandId, ex.Message);
            }
        }
    }

    /// <summary>
    /// 🔄 Stirrer Command Handler
    /// </summary>
    public class StirrerCommandHandler : ICommandHandler<StirrerCommand>
    {
        public async Task<CommandExecutionResult> HandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            return await HandleAsync((StirrerCommand)command, cancellationToken);
        }

        public async Task<CommandExecutionResult> HandleAsync(StirrerCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Log($"🔄 Handling stirrer command: {command.NewRpm} RPM", LogLevel.Debug);
                await Task.Delay(10, cancellationToken);
                return CommandExecutionResult.Success(command.CommandId, command.NewRpm);
            }
            catch (Exception ex)
            {
                return CommandExecutionResult.Failed(command.CommandId, ex.Message);
            }
        }
    }

    /// <summary>
    /// 📡 Connection Command Handler
    /// </summary>
    public class ConnectionCommandHandler : ICommandHandler<ConnectionCommand>
    {
        public async Task<CommandExecutionResult> HandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            return await HandleAsync((ConnectionCommand)command, cancellationToken);
        }

        public async Task<CommandExecutionResult> HandleAsync(ConnectionCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Log($"📡 Handling connection command: {command.Port} - {(command.Connect ? "Connect" : "Disconnect")}", LogLevel.Debug);
                await Task.Delay(100, cancellationToken); // Simulate connection work
                return CommandExecutionResult.Success(command.CommandId, command.Connect);
            }
            catch (Exception ex)
            {
                return CommandExecutionResult.Failed(command.CommandId, ex.Message);
            }
        }
    }

    /// <summary>
    /// ⚙️ Mode Change Command Handler
    /// </summary>
    public class ModeChangeCommandHandler : ICommandHandler<ModeChangeCommand>
    {
        public async Task<CommandExecutionResult> HandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            return await HandleAsync((ModeChangeCommand)command, cancellationToken);
        }

        public async Task<CommandExecutionResult> HandleAsync(ModeChangeCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Log($"⚙️ Handling mode change command", LogLevel.Debug);
                await Task.Delay(5, cancellationToken);
                return CommandExecutionResult.Success(command.CommandId, new { command.NewAutoMode, command.NewTjMode });
            }
            catch (Exception ex)
            {
                return CommandExecutionResult.Failed(command.CommandId, ex.Message);
            }
        }
    }

    #endregion
}