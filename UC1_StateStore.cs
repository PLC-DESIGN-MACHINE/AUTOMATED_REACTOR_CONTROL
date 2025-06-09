// ==============================================
//  UC1_StateStore.cs - PHASE 3
//  AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
//  State Storage & Persistence System
//  Time Travel Debugging & State History Management
// ==============================================

using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Logger;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    /// <summary>
    /// 🚀 PHASE 3: Ultra-High Performance State Storage System
    /// Features: State Persistence, Time Travel Debugging, State Snapshots
    /// Hardware-Accelerated State Storage with Real-time Backup
    /// </summary>
    public class UC1_StateStore : IDisposable
    {
        #region 💾 State Storage Infrastructure

        // State Storage & History
        private readonly ConcurrentDictionary<string, StateEntry> _stateEntries;
        private readonly ConcurrentQueue<StateSnapshot> _stateHistory;
        private readonly ImmutableList<StateBackup>.Builder _backupHistory;

        // Time Travel Management
        private volatile int _currentTimeIndex = -1;
        private readonly SemaphoreSlim _timelineAccessLock;
        private readonly object _historyLock = new object();

        // Reactive State Streams
        private readonly BehaviorSubject<StateStoreSnapshot> _storeSnapshotSubject;
        private readonly Subject<StateChangedEvent> _stateChangedSubject;
        private readonly Subject<StatePersistenceEvent> _persistenceEventSubject;
        private readonly Subject<TimelineEvent> _timelineEventSubject;

        // Storage Configuration
        private readonly StateStoreConfiguration _configuration;
        private readonly UC1_PerformanceMonitor _performanceMonitor;
        private readonly UC1_StateManager _stateManager;

        // Persistence & File Management
        private readonly Timer _persistenceTimer;
        private readonly Timer _cleanupTimer;
        private readonly SemaphoreSlim _persistenceLock;
        private readonly CancellationTokenSource _cancellationTokenSource;

        // Performance Metrics
        private volatile bool _isDisposed = false;
        private long _totalStatesStored = 0;
        private long _totalStatesRestored = 0;
        private long _totalBackupsCreated = 0;
        private DateTime _lastPersistenceTime = DateTime.UtcNow;

        #endregion

        #region 🔥 Public Observables

        /// <summary>📊 Store Snapshot Stream</summary>
        public IObservable<StateStoreSnapshot> StoreSnapshot => _storeSnapshotSubject.AsObservable();

        /// <summary>⚡ State Change Events</summary>
        public IObservable<StateChangedEvent> StateChanged => _stateChangedSubject.AsObservable();

        /// <summary>💾 Persistence Events</summary>
        public IObservable<StatePersistenceEvent> PersistenceEvents => _persistenceEventSubject.AsObservable();

        /// <summary>🕐 Timeline Events</summary>
        public IObservable<TimelineEvent> TimelineEvents => _timelineEventSubject.AsObservable();

        /// <summary>📈 Current Store Statistics</summary>
        public StateStoreStatistics CurrentStatistics => GetStatistics();

        /// <summary>🕐 Time Travel State</summary>
        public TimeTravelState TimeTravelState => GetTimeTravelState();

        #endregion

        #region 🏗️ Constructor & Initialization

        /// <summary>
        /// 🎯 Initialize Ultra-High Performance State Store
        /// </summary>
        public UC1_StateStore(
            UC1_StateManager stateManager,
            StateStoreConfiguration configuration = null,
            UC1_PerformanceMonitor performanceMonitor = null)
        {
            try
            {
                Logger.Log("🚀 [StateStore] Initializing State Storage & Time Travel System", LogLevel.Info);

                // Initialize dependencies
                _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
                _configuration = configuration ?? StateStoreConfiguration.Default;
                _performanceMonitor = performanceMonitor ?? new UC1_PerformanceMonitor();

                // Initialize storage collections
                _stateEntries = new ConcurrentDictionary<string, StateEntry>();
                _stateHistory = new ConcurrentQueue<StateSnapshot>();
                _backupHistory = ImmutableList.CreateBuilder<StateBackup>();

                // Initialize reactive subjects
                _storeSnapshotSubject = new BehaviorSubject<StateStoreSnapshot>(StateStoreSnapshot.Empty);
                _stateChangedSubject = new Subject<StateChangedEvent>();
                _persistenceEventSubject = new Subject<StatePersistenceEvent>();
                _timelineEventSubject = new Subject<TimelineEvent>();

                // Initialize synchronization
                _timelineAccessLock = new SemaphoreSlim(1, 1);
                _persistenceLock = new SemaphoreSlim(1, 1);
                _cancellationTokenSource = new CancellationTokenSource();

                // Setup timers
                _persistenceTimer = new Timer(PersistenceTimerCallback, null,
                    _configuration.AutoPersistenceInterval, _configuration.AutoPersistenceInterval);
                _cleanupTimer = new Timer(CleanupTimerCallback, null,
                    TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

                // Setup state manager integration
                SetupStateManagerIntegration();

                // Initialize storage directory
                InitializeStorageDirectory();

                // Load existing state if available
                _ = LoadInitialStateAsync();

                Logger.Log("✅ [StateStore] State Storage & Time Travel System initialized", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Initialization failed: {ex.Message}", LogLevel.Error);
                throw;
            }
        }

        /// <summary>
        /// 🔗 Setup State Manager Integration
        /// </summary>
        private void SetupStateManagerIntegration()
        {
            try
            {
                // Subscribe to state changes
                _stateManager.StateChanged
                    .Subscribe(
                        state => OnStateManagerStateChanged(state),
                        ex => Logger.Log($"❌ [StateStore] State manager subscription error: {ex.Message}", LogLevel.Error)
                    );

                // Subscribe to state change details
                _stateManager.StateChangeDetails
                    .Subscribe(
                        change => OnStateManagerStateChangeDetails(change),
                        ex => Logger.Log($"❌ [StateStore] State change details subscription error: {ex.Message}", LogLevel.Error)
                    );

                Logger.Log("🔗 [StateStore] State manager integration configured", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] State manager integration failed: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// 📁 Initialize Storage Directory
        /// </summary>
        private void InitializeStorageDirectory()
        {
            try
            {
                var storageDir = Path.GetDirectoryName(_configuration.StateStoragePath);
                if (!Directory.Exists(storageDir))
                {
                    Directory.CreateDirectory(storageDir);
                    Logger.Log($"📁 [StateStore] Created storage directory: {storageDir}", LogLevel.Info);
                }

                var backupDir = Path.GetDirectoryName(_configuration.BackupStoragePath);
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                    Logger.Log($"📁 [StateStore] Created backup directory: {backupDir}", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Storage directory initialization failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 💾 State Storage Operations

        /// <summary>
        /// 💾 Store State with Metadata
        /// </summary>
        public async Task<bool> StoreStateAsync(ReactorState state, string description = null, bool createSnapshot = true)
        {
            if (_isDisposed || state == null) return false;

            await _persistenceLock.WaitAsync();
            try
            {
                var stateEntry = new StateEntry
                {
                    Id = Guid.NewGuid(),
                    State = state,
                    Timestamp = DateTime.UtcNow,
                    Description = description ?? "Manual Store",
                    Size = CalculateStateSize(state),
                    Version = _configuration.StateVersion
                };

                // Store in memory
                var entryKey = stateEntry.Id.ToString();
                _stateEntries[entryKey] = stateEntry;

                // Create snapshot if requested
                if (createSnapshot)
                {
                    await CreateSnapshotAsync(stateEntry, "Store Operation");
                }

                // Update metrics
                Interlocked.Increment(ref _totalStatesStored);

                // Emit events
                _stateChangedSubject.OnNext(new StateChangedEvent
                {
                    EntryId = stateEntry.Id,
                    ChangeType = StateChangeType.Stored,
                    State = state,
                    Timestamp = stateEntry.Timestamp,
                    Description = stateEntry.Description
                });

                // Update performance metrics
                _performanceMonitor.RecordCustomMetric("StateStored", stateEntry.Size, MetricCategory.State);

                Logger.Log($"💾 [StateStore] State stored: {entryKey} - {description}", LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Store state failed: {ex.Message}", LogLevel.Error);
                return false;
            }
            finally
            {
                _persistenceLock.Release();
            }
        }

        /// <summary>
        /// 📥 Retrieve State by ID
        /// </summary>
        public async Task<ReactorState> RetrieveStateAsync(Guid stateId)
        {
            try
            {
                var entryKey = stateId.ToString();

                if (_stateEntries.TryGetValue(entryKey, out StateEntry entry))
                {
                    Interlocked.Increment(ref _totalStatesRestored);

                    _performanceMonitor.RecordCustomMetric("StateRetrieved", entry.Size, MetricCategory.State);

                    Logger.Log($"📥 [StateStore] State retrieved: {entryKey}", LogLevel.Debug);
                    return entry.State;
                }

                // Try to load from persistent storage
                var persistentState = await LoadStateFromStorageAsync(stateId);
                if (persistentState != null)
                {
                    Interlocked.Increment(ref _totalStatesRestored);
                    Logger.Log($"📥 [StateStore] State loaded from storage: {entryKey}", LogLevel.Debug);
                    return persistentState;
                }

                Logger.Log($"⚠️ [StateStore] State not found: {entryKey}", LogLevel.Warn);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Retrieve state failed: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        /// <summary>
        /// 📋 Get All Stored States
        /// </summary>
        public async Task<ImmutableList<StateEntry>> GetAllStatesAsync()
        {
            try
            {
                var states = _stateEntries.Values.OrderByDescending(e => e.Timestamp).ToImmutableList();
                Logger.Log($"📋 [StateStore] Retrieved {states.Count} states", LogLevel.Debug);
                return states;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Get all states failed: {ex.Message}", LogLevel.Error);
                return ImmutableList<StateEntry>.Empty;
            }
        }

        #endregion

        #region 📸 Snapshot Management

        /// <summary>
        /// 📸 Create State Snapshot
        /// </summary>
        public async Task<bool> CreateSnapshotAsync(StateEntry stateEntry, string reason)
        {
            try
            {
                var snapshot = new StateSnapshot
                {
                    Id = Guid.NewGuid(),
                    StateEntryId = stateEntry.Id,
                    State = stateEntry.State,
                    Timestamp = DateTime.UtcNow,
                    Reason = reason,
                    Size = stateEntry.Size,
                    Index = GetNextSnapshotIndex()
                };

                // Add to history
                _stateHistory.Enqueue(snapshot);

                // Update timeline index
                lock (_historyLock)
                {
                    _currentTimeIndex = snapshot.Index;
                }

                // Limit history size
                await TrimHistoryIfNeededAsync();

                // Emit timeline event
                _timelineEventSubject.OnNext(new TimelineEvent
                {
                    Type = TimelineEventType.SnapshotCreated,
                    SnapshotId = snapshot.Id,
                    Index = snapshot.Index,
                    Timestamp = snapshot.Timestamp,
                    Description = reason
                });

                Logger.Log($"📸 [StateStore] Snapshot created: {snapshot.Id} - {reason}", LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Create snapshot failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 📊 Get Snapshot History
        /// </summary>
        public async Task<ImmutableList<StateSnapshot>> GetSnapshotHistoryAsync()
        {
            try
            {
                var snapshots = _stateHistory.ToArray().OrderByDescending(s => s.Timestamp).ToImmutableList();
                Logger.Log($"📊 [StateStore] Retrieved {snapshots.Count} snapshots", LogLevel.Debug);
                return snapshots;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Get snapshot history failed: {ex.Message}", LogLevel.Error);
                return ImmutableList<StateSnapshot>.Empty;
            }
        }

        #endregion

        #region 🕐 Time Travel Operations

        /// <summary>
        /// ⏪ Travel Back in Time
        /// </summary>
        public async Task<bool> TravelBackAsync(int steps = 1)
        {
            if (steps <= 0) return false;

            await _timelineAccessLock.WaitAsync();
            try
            {
                var snapshots = _stateHistory.ToArray().OrderBy(s => s.Index).ToArray();

                if (snapshots.Length == 0 || _currentTimeIndex <= 0)
                {
                    Logger.Log("⏪ [StateStore] Cannot travel back - no history available", LogLevel.Warn);
                    return false;
                }

                var targetIndex = Math.Max(0, _currentTimeIndex - steps);
                var targetSnapshot = snapshots.FirstOrDefault(s => s.Index == targetIndex);

                if (targetSnapshot != null)
                {
                    var success = await RestoreSnapshotAsync(targetSnapshot);
                    if (success)
                    {
                        lock (_historyLock)
                        {
                            _currentTimeIndex = targetIndex;
                        }

                        _timelineEventSubject.OnNext(new TimelineEvent
                        {
                            Type = TimelineEventType.TraveledBack,
                            SnapshotId = targetSnapshot.Id,
                            Index = targetIndex,
                            Timestamp = DateTime.UtcNow,
                            Description = $"Traveled back {steps} steps"
                        });

                        Logger.Log($"⏪ [StateStore] Traveled back {steps} steps to index {targetIndex}", LogLevel.Info);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Travel back failed: {ex.Message}", LogLevel.Error);
                return false;
            }
            finally
            {
                _timelineAccessLock.Release();
            }
        }

        /// <summary>
        /// ⏩ Travel Forward in Time
        /// </summary>
        public async Task<bool> TravelForwardAsync(int steps = 1)
        {
            if (steps <= 0) return false;

            await _timelineAccessLock.WaitAsync();
            try
            {
                var snapshots = _stateHistory.ToArray().OrderBy(s => s.Index).ToArray();
                var maxIndex = snapshots.Length > 0 ? snapshots.Max(s => s.Index) : -1;

                if (_currentTimeIndex >= maxIndex)
                {
                    Logger.Log("⏩ [StateStore] Cannot travel forward - already at latest state", LogLevel.Warn);
                    return false;
                }

                var targetIndex = Math.Min(maxIndex, _currentTimeIndex + steps);
                var targetSnapshot = snapshots.FirstOrDefault(s => s.Index == targetIndex);

                if (targetSnapshot != null)
                {
                    var success = await RestoreSnapshotAsync(targetSnapshot);
                    if (success)
                    {
                        lock (_historyLock)
                        {
                            _currentTimeIndex = targetIndex;
                        }

                        _timelineEventSubject.OnNext(new TimelineEvent
                        {
                            Type = TimelineEventType.TraveledForward,
                            SnapshotId = targetSnapshot.Id,
                            Index = targetIndex,
                            Timestamp = DateTime.UtcNow,
                            Description = $"Traveled forward {steps} steps"
                        });

                        Logger.Log($"⏩ [StateStore] Traveled forward {steps} steps to index {targetIndex}", LogLevel.Info);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Travel forward failed: {ex.Message}", LogLevel.Error);
                return false;
            }
            finally
            {
                _timelineAccessLock.Release();
            }
        }

        /// <summary>
        /// 🎯 Jump to Specific Time Index
        /// </summary>
        public async Task<bool> JumpToTimeIndexAsync(int timeIndex)
        {
            await _timelineAccessLock.WaitAsync();
            try
            {
                var snapshots = _stateHistory.ToArray();
                var targetSnapshot = snapshots.FirstOrDefault(s => s.Index == timeIndex);

                if (targetSnapshot != null)
                {
                    var success = await RestoreSnapshotAsync(targetSnapshot);
                    if (success)
                    {
                        lock (_historyLock)
                        {
                            _currentTimeIndex = timeIndex;
                        }

                        _timelineEventSubject.OnNext(new TimelineEvent
                        {
                            Type = TimelineEventType.JumpedToIndex,
                            SnapshotId = targetSnapshot.Id,
                            Index = timeIndex,
                            Timestamp = DateTime.UtcNow,
                            Description = $"Jumped to time index {timeIndex}"
                        });

                        Logger.Log($"🎯 [StateStore] Jumped to time index {timeIndex}", LogLevel.Info);
                        return true;
                    }
                }

                Logger.Log($"⚠️ [StateStore] Time index not found: {timeIndex}", LogLevel.Warn);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Jump to time index failed: {ex.Message}", LogLevel.Error);
                return false;
            }
            finally
            {
                _timelineAccessLock.Release();
            }
        }

        #endregion

        #region 💾 Persistence Operations

        /// <summary>
        /// 💾 Persist State to Storage
        /// </summary>
        public async Task<bool> PersistStateAsync(bool includeHistory = true)
        {
            await _persistenceLock.WaitAsync();
            try
            {
                var persistenceData = new StatePersistenceData
                {
                    Version = _configuration.StateVersion,
                    Timestamp = DateTime.UtcNow,
                    States = _stateEntries.Values.ToImmutableList(),
                    Snapshots = includeHistory ? _stateHistory.ToArray().ToImmutableList() : null,
                    CurrentTimeIndex = _currentTimeIndex,
                    Metadata = CreatePersistenceMetadata()
                };

                var json = JsonConvert.SerializeObject(persistenceData, Formatting.Indented, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                });

                await File.WriteAllTextAsync(_configuration.StateStoragePath, json);

                _lastPersistenceTime = DateTime.UtcNow;

                _persistenceEventSubject.OnNext(new StatePersistenceEvent
                {
                    Type = PersistenceEventType.StateSaved,
                    Timestamp = DateTime.UtcNow,
                    FilePath = _configuration.StateStoragePath,
                    Success = true,
                    StateCount = _stateEntries.Count,
                    SnapshotCount = includeHistory ? _stateHistory.Count : 0
                });

                Logger.Log($"💾 [StateStore] State persisted to storage: {_stateEntries.Count} states", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Persist state failed: {ex.Message}", LogLevel.Error);

                _persistenceEventSubject.OnNext(new StatePersistenceEvent
                {
                    Type = PersistenceEventType.SaveFailed,
                    Timestamp = DateTime.UtcNow,
                    FilePath = _configuration.StateStoragePath,
                    Success = false,
                    Error = ex.Message
                });

                return false;
            }
            finally
            {
                _persistenceLock.Release();
            }
        }

        /// <summary>
        /// 📥 Load State from Storage
        /// </summary>
        public async Task<bool> LoadStateFromStorageAsync()
        {
            await _persistenceLock.WaitAsync();
            try
            {
                if (!File.Exists(_configuration.StateStoragePath))
                {
                    Logger.Log("📥 [StateStore] No existing state file found", LogLevel.Info);
                    return false;
                }

                var json = await File.ReadAllTextAsync(_configuration.StateStoragePath);
                var persistenceData = JsonConvert.DeserializeObject<StatePersistenceData>(json, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                });

                if (persistenceData == null)
                {
                    Logger.Log("❌ [StateStore] Failed to deserialize state data", LogLevel.Error);
                    return false;
                }

                // Clear existing data
                _stateEntries.Clear();
                while (_stateHistory.TryDequeue(out _)) { }

                // Load states
                foreach (var state in persistenceData.States)
                {
                    _stateEntries[state.Id.ToString()] = state;
                }

                // Load snapshots
                if (persistenceData.Snapshots != null)
                {
                    foreach (var snapshot in persistenceData.Snapshots)
                    {
                        _stateHistory.Enqueue(snapshot);
                    }
                }

                // Restore timeline position
                lock (_historyLock)
                {
                    _currentTimeIndex = persistenceData.CurrentTimeIndex;
                }

                _persistenceEventSubject.OnNext(new StatePersistenceEvent
                {
                    Type = PersistenceEventType.StateLoaded,
                    Timestamp = DateTime.UtcNow,
                    FilePath = _configuration.StateStoragePath,
                    Success = true,
                    StateCount = _stateEntries.Count,
                    SnapshotCount = _stateHistory.Count
                });

                Logger.Log($"📥 [StateStore] State loaded from storage: {_stateEntries.Count} states, {_stateHistory.Count} snapshots", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Load state failed: {ex.Message}", LogLevel.Error);

                _persistenceEventSubject.OnNext(new StatePersistenceEvent
                {
                    Type = PersistenceEventType.LoadFailed,
                    Timestamp = DateTime.UtcNow,
                    FilePath = _configuration.StateStoragePath,
                    Success = false,
                    Error = ex.Message
                });

                return false;
            }
            finally
            {
                _persistenceLock.Release();
            }
        }

        #endregion

        #region 💾 Backup Operations

        /// <summary>
        /// 💾 Create Backup
        /// </summary>
        public async Task<bool> CreateBackupAsync(string description = null)
        {
            try
            {
                var backup = new StateBackup
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    Description = description ?? "Manual Backup",
                    States = _stateEntries.Values.ToImmutableList(),
                    Snapshots = _stateHistory.ToArray().ToImmutableList(),
                    CurrentTimeIndex = _currentTimeIndex
                };

                // Add to backup history
                lock (_historyLock)
                {
                    _backupHistory.Add(backup);

                    // Limit backup history
                    if (_backupHistory.Count > _configuration.MaxBackupCount)
                    {
                        _backupHistory.RemoveAt(0);
                    }
                }

                // Save to file
                var backupFileName = $"StateBackup_{backup.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                var backupPath = Path.Combine(Path.GetDirectoryName(_configuration.BackupStoragePath), backupFileName);

                var json = JsonConvert.SerializeObject(backup, Formatting.Indented);
                await File.WriteAllTextAsync(backupPath, json);

                Interlocked.Increment(ref _totalBackupsCreated);

                Logger.Log($"💾 [StateStore] Backup created: {backupFileName}", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Create backup failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// 📥 Restore from Backup
        /// </summary>
        public async Task<bool> RestoreFromBackupAsync(Guid backupId)
        {
            try
            {
                StateBackup backup;
                lock (_historyLock)
                {
                    backup = _backupHistory.FirstOrDefault(b => b.Id == backupId);
                }

                if (backup == null)
                {
                    Logger.Log($"⚠️ [StateStore] Backup not found: {backupId}", LogLevel.Warn);
                    return false;
                }

                await _persistenceLock.WaitAsync();
                try
                {
                    // Clear existing data
                    _stateEntries.Clear();
                    while (_stateHistory.TryDequeue(out _)) { }

                    // Restore states
                    foreach (var state in backup.States)
                    {
                        _stateEntries[state.Id.ToString()] = state;
                    }

                    // Restore snapshots
                    foreach (var snapshot in backup.Snapshots)
                    {
                        _stateHistory.Enqueue(snapshot);
                    }

                    // Restore timeline position
                    lock (_historyLock)
                    {
                        _currentTimeIndex = backup.CurrentTimeIndex;
                    }

                    Logger.Log($"📥 [StateStore] Restored from backup: {backupId}", LogLevel.Info);
                    return true;
                }
                finally
                {
                    _persistenceLock.Release();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Restore from backup failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        #endregion

        #region 📊 Statistics & Monitoring

        /// <summary>
        /// 📊 Get Store Statistics
        /// </summary>
        private StateStoreStatistics GetStatistics()
        {
            return new StateStoreStatistics
            {
                TotalStatesStored = _totalStatesStored,
                TotalStatesRestored = _totalStatesRestored,
                TotalBackupsCreated = _totalBackupsCreated,
                CurrentStateCount = _stateEntries.Count,
                SnapshotCount = _stateHistory.Count,
                BackupCount = _backupHistory.Count,
                CurrentTimeIndex = _currentTimeIndex,
                LastPersistenceTime = _lastPersistenceTime,
                MemoryUsage = GC.GetTotalMemory(false),
                StorageSize = GetStorageSize()
            };
        }

        /// <summary>
        /// 🕐 Get Time Travel State
        /// </summary>
        private TimeTravelState GetTimeTravelState()
        {
            var snapshots = _stateHistory.ToArray();
            var maxIndex = snapshots.Length > 0 ? snapshots.Max(s => s.Index) : -1;

            return new TimeTravelState
            {
                CurrentIndex = _currentTimeIndex,
                MaxIndex = maxIndex,
                CanTravelBack = _currentTimeIndex > 0,
                CanTravelForward = _currentTimeIndex < maxIndex,
                TotalSnapshots = snapshots.Length,
                HistoryRange = snapshots.Length > 0 ?
                    new TimeRange
                    {
                        Start = snapshots.Min(s => s.Timestamp),
                        End = snapshots.Max(s => s.Timestamp)
                    } : null
            };
        }

        #endregion

        #region 🛠️ Helper Methods

        private void OnStateManagerStateChanged(ReactorState state)
        {
            try
            {
                // Auto-store state changes
                if (_configuration.AutoStoreEnabled)
                {
                    _ = Task.Run(async () => await StoreStateAsync(state, "Auto Store", true));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] State manager state changed handler failed: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnStateManagerStateChangeDetails(StateChange change)
        {
            try
            {
                // Create detailed snapshot for significant changes
                if (_configuration.DetailedSnapshotsEnabled)
                {
                    var stateEntry = new StateEntry
                    {
                        Id = Guid.NewGuid(),
                        State = change.NewState,
                        Timestamp = change.Timestamp,
                        Description = $"Action: {change.Action?.GetType()?.Name}",
                        Size = CalculateStateSize(change.NewState),
                        Version = _configuration.StateVersion
                    };

                    _ = Task.Run(async () => await CreateSnapshotAsync(stateEntry, $"State Change: {change.Action?.GetType()?.Name}"));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] State change details handler failed: {ex.Message}", LogLevel.Error);
            }
        }

        private async Task<ReactorState> LoadStateFromStorageAsync(Guid stateId)
        {
            // Implementation for loading specific state from persistent storage
            // This would typically involve file system or database operations
            return null;
        }

        private async Task<bool> RestoreSnapshotAsync(StateSnapshot snapshot)
        {
            try
            {
                // Restore state through state manager
                await _stateManager.JumpToStateAsync(snapshot.Index);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Restore snapshot failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task LoadInitialStateAsync()
        {
            try
            {
                await LoadStateFromStorageAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Load initial state failed: {ex.Message}", LogLevel.Error);
            }
        }

        private long CalculateStateSize(ReactorState state)
        {
            try
            {
                var json = JsonConvert.SerializeObject(state);
                return System.Text.Encoding.UTF8.GetByteCount(json);
            }
            catch
            {
                return 0;
            }
        }

        private int GetNextSnapshotIndex()
        {
            var snapshots = _stateHistory.ToArray();
            return snapshots.Length > 0 ? snapshots.Max(s => s.Index) + 1 : 0;
        }

        private async Task TrimHistoryIfNeededAsync()
        {
            try
            {
                if (_stateHistory.Count > _configuration.MaxHistorySize)
                {
                    var itemsToRemove = _stateHistory.Count - _configuration.MaxHistorySize;
                    for (int i = 0; i < itemsToRemove; i++)
                    {
                        _stateHistory.TryDequeue(out _);
                    }

                    Logger.Log($"✂️ [StateStore] Trimmed {itemsToRemove} items from history", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Trim history failed: {ex.Message}", LogLevel.Error);
            }
        }

        private PersistenceMetadata CreatePersistenceMetadata()
        {
            return new PersistenceMetadata
            {
                Version = _configuration.StateVersion,
                CreatedAt = DateTime.UtcNow,
                StateCount = _stateEntries.Count,
                SnapshotCount = _stateHistory.Count,
                TotalStatesStored = _totalStatesStored,
                LastBackupTime = _backupHistory.Count > 0 ? _backupHistory.Last().Timestamp : (DateTime?)null
            };
        }

        private long GetStorageSize()
        {
            try
            {
                var fileInfo = new FileInfo(_configuration.StateStoragePath);
                return fileInfo.Exists ? fileInfo.Length : 0;
            }
            catch
            {
                return 0;
            }
        }

        private void PersistenceTimerCallback(object state)
        {
            try
            {
                if (_configuration.AutoPersistenceEnabled)
                {
                    _ = Task.Run(async () => await PersistStateAsync());
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Persistence timer callback failed: {ex.Message}", LogLevel.Error);
            }
        }

        private void CleanupTimerCallback(object state)
        {
            try
            {
                _ = Task.Run(async () => await TrimHistoryIfNeededAsync());
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Cleanup timer callback failed: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region 🗑️ Disposal

        public void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                Logger.Log("🗑️ [StateStore] Starting disposal", LogLevel.Info);

                // Cancel operations
                _cancellationTokenSource?.Cancel();

                // Dispose timers
                _persistenceTimer?.Dispose();
                _cleanupTimer?.Dispose();

                // Final persistence
                try
                {
                    PersistStateAsync().Wait(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    Logger.Log($"⚠️ [StateStore] Final persistence failed: {ex.Message}", LogLevel.Warn);
                }

                // Complete reactive subjects
                _storeSnapshotSubject?.OnCompleted();
                _storeSnapshotSubject?.Dispose();
                _stateChangedSubject?.OnCompleted();
                _stateChangedSubject?.Dispose();
                _persistenceEventSubject?.OnCompleted();
                _persistenceEventSubject?.Dispose();
                _timelineEventSubject?.OnCompleted();
                _timelineEventSubject?.Dispose();

                // Dispose synchronization objects
                _timelineAccessLock?.Dispose();
                _persistenceLock?.Dispose();
                _cancellationTokenSource?.Dispose();

                // Clear collections
                _stateEntries?.Clear();
                while (_stateHistory?.TryDequeue(out _) == true) { }
                _backupHistory?.Clear();

                _isDisposed = true;
                Logger.Log("✅ [StateStore] Disposal completed", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ [StateStore] Disposal error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion
    }

    #region 📋 Supporting Classes & Data Structures

    /// <summary>💾 State Entry</summary>
    public class StateEntry
    {
        public Guid Id { get; set; }
        public ReactorState State { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public long Size { get; set; }
        public string Version { get; set; }
    }

    /// <summary>📸 State Snapshot</summary>
    public class StateSnapshot
    {
        public Guid Id { get; set; }
        public Guid StateEntryId { get; set; }
        public ReactorState State { get; set; }
        public DateTime Timestamp { get; set; }
        public string Reason { get; set; }
        public long Size { get; set; }
        public int Index { get; set; }
    }

    /// <summary>💾 State Backup</summary>
    public class StateBackup
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public ImmutableList<StateEntry> States { get; set; }
        public ImmutableList<StateSnapshot> Snapshots { get; set; }
        public int CurrentTimeIndex { get; set; }
    }

    /// <summary>📊 Store Snapshot</summary>
    public class StateStoreSnapshot
    {
        public DateTime Timestamp { get; set; }
        public int StateCount { get; set; }
        public int SnapshotCount { get; set; }
        public int CurrentTimeIndex { get; set; }
        public long MemoryUsage { get; set; }

        public static StateStoreSnapshot Empty => new StateStoreSnapshot
        {
            Timestamp = DateTime.UtcNow,
            StateCount = 0,
            SnapshotCount = 0,
            CurrentTimeIndex = -1,
            MemoryUsage = 0
        };
    }

    /// <summary>⚡ State Changed Event</summary>
    public class StateChangedEvent
    {
        public Guid EntryId { get; set; }
        public StateChangeType ChangeType { get; set; }
        public ReactorState State { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }

    /// <summary>💾 State Persistence Event</summary>
    public class StatePersistenceEvent
    {
        public PersistenceEventType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string FilePath { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
        public int StateCount { get; set; }
        public int SnapshotCount { get; set; }
    }

    /// <summary>🕐 Timeline Event</summary>
    public class TimelineEvent
    {
        public TimelineEventType Type { get; set; }
        public Guid SnapshotId { get; set; }
        public int Index { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
    }

    /// <summary>📊 State Store Statistics</summary>
    public class StateStoreStatistics
    {
        public long TotalStatesStored { get; set; }
        public long TotalStatesRestored { get; set; }
        public long TotalBackupsCreated { get; set; }
        public int CurrentStateCount { get; set; }
        public int SnapshotCount { get; set; }
        public int BackupCount { get; set; }
        public int CurrentTimeIndex { get; set; }
        public DateTime LastPersistenceTime { get; set; }
        public long MemoryUsage { get; set; }
        public long StorageSize { get; set; }
    }

    /// <summary>🕐 Time Travel State</summary>
    public class TimeTravelState
    {
        public int CurrentIndex { get; set; }
        public int MaxIndex { get; set; }
        public bool CanTravelBack { get; set; }
        public bool CanTravelForward { get; set; }
        public int TotalSnapshots { get; set; }
        public TimeRange HistoryRange { get; set; }
    }

    /// <summary>📅 Time Range</summary>
    public class TimeRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    /// <summary>💾 State Persistence Data</summary>
    public class StatePersistenceData
    {
        public string Version { get; set; }
        public DateTime Timestamp { get; set; }
        public ImmutableList<StateEntry> States { get; set; }
        public ImmutableList<StateSnapshot> Snapshots { get; set; }
        public int CurrentTimeIndex { get; set; }
        public PersistenceMetadata Metadata { get; set; }
    }

    /// <summary>📊 Persistence Metadata</summary>
    public class PersistenceMetadata
    {
        public string Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public int StateCount { get; set; }
        public int SnapshotCount { get; set; }
        public long TotalStatesStored { get; set; }
        public DateTime? LastBackupTime { get; set; }
    }

    /// <summary>⚙️ State Store Configuration</summary>
    public class StateStoreConfiguration
    {
        public string StateStoragePath { get; set; } = "./Data/ReactorState.json";
        public string BackupStoragePath { get; set; } = "./Backups/";
        public string StateVersion { get; set; } = "1.0";
        public int MaxHistorySize { get; set; } = 1000;
        public int MaxBackupCount { get; set; } = 10;
        public TimeSpan AutoPersistenceInterval { get; set; } = TimeSpan.FromMinutes(5);
        public bool AutoPersistenceEnabled { get; set; } = true;
        public bool AutoStoreEnabled { get; set; } = true;
        public bool DetailedSnapshotsEnabled { get; set; } = true;

        public static StateStoreConfiguration Default => new StateStoreConfiguration();
    }

    /// <summary>🔄 State Change Type Enum</summary>
    public enum StateChangeType
    {
        Stored,
        Restored,
        Snapshot,
        TimeTravelBack,
        TimeTravelForward,
        BackupCreated,
        BackupRestored
    }

    /// <summary>💾 Persistence Event Type Enum</summary>
    public enum PersistenceEventType
    {
        StateSaved,
        StateLoaded,
        SaveFailed,
        LoadFailed,
        BackupCreated,
        BackupRestored
    }

    /// <summary>🕐 Timeline Event Type Enum</summary>
    public enum TimelineEventType
    {
        SnapshotCreated,
        TraveledBack,
        TraveledForward,
        JumpedToIndex,
        HistoryCleared,
        BackupPoint
    }

    #endregion
}