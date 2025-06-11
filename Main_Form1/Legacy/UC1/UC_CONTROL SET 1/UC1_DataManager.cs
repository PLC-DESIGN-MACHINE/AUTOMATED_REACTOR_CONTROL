// ==============================================
//  UC1_DataManager.cs - FIXED ERRORS
//  AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
//  Data Management & File Operations System
//  แก้ไข: Timer.Stop() -> Timer.Change(), WriteAllTextAsync -> WriteAllText
// ==============================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Logger;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    /// <summary>
    /// Modern Data Manager with Async/Await and Advanced File Operations
    /// Handles Manual Data, Device Settings, and File I/O with Error Recovery
    /// </summary>
    public class UC1_DataManager : IDisposable
    {
        #region Events & Delegates

        public event EventHandler<DataSavedEventArgs> DataSaved;
        public event EventHandler<DataLoadedEventArgs> DataLoaded;
        public event EventHandler<DataErrorEventArgs> DataError;

        #endregion

        #region Private Fields

        private readonly string _baseDirectory;
        private readonly SemaphoreSlim _fileLock;
        private readonly Dictionary<string, DateTime> _lastSaveTime;
        private readonly Dictionary<string, string> _backupPaths;
        private bool _isDisposed = false;

        // Debounced save system - แก้ไข Timer type
        private readonly Dictionary<string, System.Threading.Timer> _saveTimers;
        private readonly object _timerLock = new object();

        #endregion

        #region Constructor & Initialization

        public UC1_DataManager(string baseDirectory = null)
        {
            _baseDirectory = baseDirectory ?? Environment.CurrentDirectory;
            _fileLock = new SemaphoreSlim(1, 1);
            _lastSaveTime = new Dictionary<string, DateTime>();
            _backupPaths = new Dictionary<string, string>();
            _saveTimers = new Dictionary<string, System.Threading.Timer>();

            EnsureDirectoryExists();
            InitializeBackupSystem();

            Logger.Log($"[UC1_DataManager] Initialized with base directory: {_baseDirectory}", LogLevel.Info);
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_baseDirectory))
                {
                    Directory.CreateDirectory(_baseDirectory);
                }

                // Create backup directory
                string backupDir = Path.Combine(_baseDirectory, "Backups");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                Logger.Log($"[UC1_DataManager] Directory structure verified", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_DataManager] Error creating directories: {ex.Message}", LogLevel.Error);
            }
        }

        private void InitializeBackupSystem()
        {
            try
            {
                string backupDir = Path.Combine(_baseDirectory, "Backups");

                _backupPaths["Manual_Set1"] = Path.Combine(backupDir, "Manual_Set1_Backup.xml");
                _backupPaths["DeviceSettings"] = Path.Combine(backupDir, "DeviceSettings_Backup.xml");
                _backupPaths["Emergency"] = Path.Combine(backupDir, "Emergency_Backup.txt");

                Logger.Log("[UC1_DataManager] Backup system initialized", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_DataManager] Error initializing backup system: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Manual Data Operations

        /// <summary>
        /// Async load manual control data with error recovery
        /// </summary>
        public async Task<ManualDataLoadResult> LoadManualDataAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                Logger.Log("[UC1_DataManager] Loading manual control data", LogLevel.Info);

                var result = new ManualDataLoadResult
                {
                    Success = false,
                    Timestamp = DateTime.Now
                };

                // Verify file integrity first
                string filePath = Manual_Data_Set1.DefaultFilePath_1;
                bool fileValid = await VerifyFileIntegrityAsync(filePath);

                if (!fileValid)
                {
                    Logger.Log("[UC1_DataManager] Main file corrupted, attempting recovery", LogLevel.Warn);

                    var recoveryResult = await AttemptDataRecoveryAsync("Manual_Set1");
                    if (!recoveryResult.Success)
                    {
                        result.ErrorMessage = "File corrupted and recovery failed";
                        OnDataError(new DataErrorEventArgs
                        {
                            ErrorType = "FILE_CORRUPTION",
                            ErrorMessage = result.ErrorMessage,
                            FilePath = filePath
                        });
                        return result;
                    }
                }

                // Load data
                var manualData = Manual_Data_Set1.CurrentManual_1;
                if (manualData == null)
                {
                    result.ErrorMessage = "Manual_Data_Set1.CurrentManual_1 is null";
                    Logger.Log($"[UC1_DataManager] {result.ErrorMessage}", LogLevel.Warn);
                    return result;
                }

                // Extract data with null safety
                result.Success = true;
                result.IsTjMode = manualData.IsTjMode_1;
                result.CurrentThermoSetpoint = manualData.CurrentThermoSetpoint_1 ?? "25.0";
                result.StirrerSetpoint = manualData.Stirrer_Set1 ?? "0";
                result.LastTrSetpoint = manualData.LastTrSetpoint_1;
                result.LastTjSetpoint = manualData.LastTjSetpoint_1;

                // Determine display value based on mode
                if (result.IsTjMode)
                {
                    result.DisplayValue = Math.Abs(result.LastTjSetpoint) > 0.01f
                        ? result.LastTjSetpoint.ToString("F2")
                        : result.CurrentThermoSetpoint;
                }
                else
                {
                    result.DisplayValue = Math.Abs(result.LastTrSetpoint) > 0.01f
                        ? result.LastTrSetpoint.ToString("F2")
                        : result.CurrentThermoSetpoint;
                }

                // Create backup on successful load
                await CreateBackupAsync("Manual_Set1", filePath);

                OnDataLoaded(new DataLoadedEventArgs
                {
                    DataType = "Manual_Set1",
                    FilePath = filePath,
                    Success = true,
                    Timestamp = DateTime.Now
                });

                Logger.Log($"[UC1_DataManager] Manual data loaded - Mode: {(result.IsTjMode ? "Tj" : "Tr")}, Temp: {result.DisplayValue}, Stirrer: {result.StirrerSetpoint}", LogLevel.Info);

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_DataManager] Error loading manual data: {ex.Message}", LogLevel.Error);

                OnDataError(new DataErrorEventArgs
                {
                    ErrorType = "LOAD_ERROR",
                    ErrorMessage = ex.Message,
                    FilePath = Manual_Data_Set1.DefaultFilePath_1
                });

                return new ManualDataLoadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.Now
                };
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Async save manual data with debouncing and error recovery
        /// </summary>
        public async Task<DataSaveResult> SaveManualDataAsync(string trigger = "Manual", bool immediate = false)
        {
            if (!immediate)
            {
                // Use debounced save for non-immediate requests
                TriggerDebouncedSave("Manual_Set1", trigger);
                return new DataSaveResult { Success = true, Message = "Debounced save triggered" };
            }

            await _fileLock.WaitAsync();
            try
            {
                Logger.Log($"[UC1_DataManager] Saving manual data - Trigger: {trigger}", LogLevel.Info);

                var result = new DataSaveResult
                {
                    DataType = "Manual_Set1",
                    Trigger = trigger,
                    Timestamp = DateTime.Now
                };

                // Multiple save strategies
                var strategies = new List<Func<Task<bool>>>
                {
                    () => SaveWithStrategy1Async(trigger),
                    () => SaveWithStrategy2Async(),
                    () => SaveWithEmergencyBackupAsync(trigger)
                };

                foreach (var strategy in strategies)
                {
                    try
                    {
                        bool success = await strategy();
                        if (success)
                        {
                            result.Success = true;
                            result.Message = "Save completed successfully";
                            result.FilePath = Manual_Data_Set1.DefaultFilePath_1;

                            _lastSaveTime["Manual_Set1"] = DateTime.Now;

                            OnDataSaved(new DataSavedEventArgs
                            {
                                DataType = "Manual_Set1",
                                FilePath = result.FilePath,
                                Trigger = trigger,
                                Success = true,
                                Timestamp = DateTime.Now
                            });

                            Logger.Log($"[UC1_DataManager] Manual data saved successfully with strategy", LogLevel.Info);
                            return result;
                        }
                    }
                    catch (Exception strategyEx)
                    {
                        Logger.Log($"[UC1_DataManager] Save strategy failed: {strategyEx.Message}", LogLevel.Error);
                        continue;
                    }
                }

                // All strategies failed
                result.Success = false;
                result.Message = "All save strategies failed";

                OnDataError(new DataErrorEventArgs
                {
                    ErrorType = "SAVE_FAILURE",
                    ErrorMessage = result.Message,
                    FilePath = Manual_Data_Set1.DefaultFilePath_1
                });

                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_DataManager] Critical error in SaveManualDataAsync: {ex.Message}", LogLevel.Error);

                return new DataSaveResult
                {
                    Success = false,
                    Message = $"Critical error: {ex.Message}",
                    DataType = "Manual_Set1",
                    Trigger = trigger,
                    Timestamp = DateTime.Now
                };
            }
            finally
            {
                _fileLock.Release();
            }
        }

        #endregion

        #region Device Settings Operations

        /// <summary>
        /// Async load device settings
        /// </summary>
        public async Task<DeviceSettingsLoadResult> LoadDeviceSettingsAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                Logger.Log("[UC1_DataManager] Loading device settings", LogLevel.Info);

                var result = new DeviceSettingsLoadResult
                {
                    Timestamp = DateTime.Now
                };

                try
                {
                    result.DeviceSettings = await Task.Run(() =>
                        DeviceSettings_1.Load_1("Settings_1.xml") ?? new DeviceSettings_1());

                    result.Success = true;
                    result.Message = "Device settings loaded successfully";

                    OnDataLoaded(new DataLoadedEventArgs
                    {
                        DataType = "DeviceSettings",
                        FilePath = "Settings_1.xml",
                        Success = true,
                        Timestamp = DateTime.Now
                    });

                    Logger.Log("[UC1_DataManager] Device settings loaded successfully", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UC1_DataManager] Failed to load device settings: {ex.Message}, using defaults", LogLevel.Warn);

                    result.DeviceSettings = new DeviceSettings_1();
                    result.Success = false;
                    result.Message = $"Loaded defaults due to error: {ex.Message}";
                }

                return result;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        /// <summary>
        /// Async save device settings
        /// </summary>
        public async Task<DataSaveResult> SaveDeviceSettingsAsync(DeviceSettings_1 settings)
        {
            if (settings == null)
            {
                return new DataSaveResult
                {
                    Success = false,
                    Message = "Settings object is null",
                    DataType = "DeviceSettings",
                    Timestamp = DateTime.Now
                };
            }

            await _fileLock.WaitAsync();
            try
            {
                Logger.Log("[UC1_DataManager] Saving device settings", LogLevel.Info);

                await Task.Run(() => settings.Save_Set1("Settings_1.xml"));

                var result = new DataSaveResult
                {
                    Success = true,
                    Message = "Device settings saved successfully",
                    DataType = "DeviceSettings",
                    FilePath = "Settings_1.xml",
                    Timestamp = DateTime.Now
                };

                OnDataSaved(new DataSavedEventArgs
                {
                    DataType = "DeviceSettings",
                    FilePath = "Settings_1.xml",
                    Success = true,
                    Timestamp = DateTime.Now
                });

                Logger.Log("[UC1_DataManager] Device settings saved successfully", LogLevel.Info);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_DataManager] Error saving device settings: {ex.Message}", LogLevel.Error);

                return new DataSaveResult
                {
                    Success = false,
                    Message = ex.Message,
                    DataType = "DeviceSettings",
                    Timestamp = DateTime.Now
                };
            }
            finally
            {
                _fileLock.Release();
            }
        }

        #endregion

        #region Debounced Save System - แก้ไข Timer

        private void TriggerDebouncedSave(string dataType, string trigger)
        {
            lock (_timerLock)
            {
                // Stop existing timer if any - แก้ไข: ใช้ Change() แทน Stop()
                if (_saveTimers.TryGetValue(dataType, out System.Threading.Timer existingTimer))
                {
                    existingTimer.Change(Timeout.Infinite, Timeout.Infinite); // หยุด timer
                    existingTimer.Dispose();
                }

                // Create new timer
                var timer = new System.Threading.Timer(async _ =>
                {
                    await PerformDebouncedSave(dataType, trigger);
                }, null, 1500, Timeout.Infinite); // 1.5 second delay

                _saveTimers[dataType] = timer;
            }
        }

        private async Task PerformDebouncedSave(string dataType, string trigger)
        {
            try
            {
                Logger.Log($"[UC1_DataManager] Performing debounced save for {dataType}", LogLevel.Info);

                if (dataType == "Manual_Set1")
                {
                    await SaveManualDataAsync($"Debounced_{trigger}", immediate: true);
                }

                // Clean up timer
                lock (_timerLock)
                {
                    if (_saveTimers.TryGetValue(dataType, out System.Threading.Timer timer))
                    {
                        timer.Dispose();
                        _saveTimers.Remove(dataType);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_DataManager] Error in debounced save: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Save Strategies

        private async Task<bool> SaveWithStrategy1Async(string trigger)
        {
            try
            {
                if (Manual_Data_Set1.CurrentManual_1 != null)
                {
                    await Task.Run(() =>
                        Manual_Data_Set1.CurrentManual_1.SimpleSave_1(Manual_Data_Set1.DefaultFilePath_1, trigger));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_DataManager] Strategy 1 failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task<bool> SaveWithStrategy2Async()
        {
            try
            {
                await Task.Run(() => Manual_Data_Set1.ForceSave_1());
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_DataManager] Strategy 2 failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task<bool> SaveWithEmergencyBackupAsync(string trigger)
        {
            try
            {
                var manual = Manual_Data_Set1.CurrentManual_1;
                string emergencyData = $"Emergency save at {DateTime.Now}\n" +
                                     $"Trigger: {trigger}\n" +
                                     $"IsTjMode_1: {manual?.IsTjMode_1}\n" +
                                     $"CurrentThermoSetpoint_1: {manual?.CurrentThermoSetpoint_1}\n" +
                                     $"Stirrer_Set1: {manual?.Stirrer_Set1}\n" +
                                     $"LastTrSetpoint_1: {manual?.LastTrSetpoint_1}\n" +
                                     $"LastTjSetpoint_1: {manual?.LastTjSetpoint_1}";

                string emergencyFile = Path.Combine(_baseDirectory, $"Emergency_Save_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                // แก้ไข: ใช้ File.WriteAllText แทน WriteAllTextAsync สำหรับ .NET Framework
                await Task.Run(() => File.WriteAllText(emergencyFile, emergencyData));

                Logger.Log($"[UC1_DataManager] Emergency backup created: {emergencyFile}", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_DataManager] Emergency backup failed: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        #endregion

        #region File Integrity & Recovery

        private async Task<bool> VerifyFileIntegrityAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                return await Task.Run(() => Manual_Data_Set1.VerifyFileIntegrity(filePath));
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_DataManager] Error verifying file integrity: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private async Task<RecoveryResult> AttemptDataRecoveryAsync(string dataType)
        {
            try
            {
                Logger.Log($"[UC1_DataManager] Attempting data recovery for {dataType}", LogLevel.Info);

                if (_backupPaths.TryGetValue(dataType, out string backupPath) && File.Exists(backupPath))
                {
                    // Try to restore from backup
                    string mainPath = dataType == "Manual_Set1" ? Manual_Data_Set1.DefaultFilePath_1 : "Settings_1.xml";

                    await Task.Run(() => File.Copy(backupPath, mainPath, true));

                    return new RecoveryResult
                    {
                        Success = true,
                        Message = $"Restored from backup: {backupPath}",
                        RecoveryMethod = "Backup Restore"
                    };
                }

                return new RecoveryResult
                {
                    Success = false,
                    Message = "No backup available for recovery",
                    RecoveryMethod = "None"
                };
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_DataManager] Data recovery failed: {ex.Message}", LogLevel.Error);

                return new RecoveryResult
                {
                    Success = false,
                    Message = ex.Message,
                    RecoveryMethod = "Failed"
                };
            }
        }

        private async Task CreateBackupAsync(string dataType, string sourcePath)
        {
            try
            {
                if (_backupPaths.TryGetValue(dataType, out string backupPath) && File.Exists(sourcePath))
                {
                    await Task.Run(() => File.Copy(sourcePath, backupPath, true));
                    Logger.Log($"[UC1_DataManager] Backup created: {backupPath}", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_DataManager] Error creating backup: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Event Invocation

        protected virtual void OnDataSaved(DataSavedEventArgs e)
        {
            DataSaved?.Invoke(this, e);
        }

        protected virtual void OnDataLoaded(DataLoadedEventArgs e)
        {
            DataLoaded?.Invoke(this, e);
        }

        protected virtual void OnDataError(DataErrorEventArgs e)
        {
            DataError?.Invoke(this, e);
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                try
                {
                    // Dispose timers - แก้ไข: ใช้ Change() และ Dispose()
                    lock (_timerLock)
                    {
                        foreach (var timer in _saveTimers.Values)
                        {
                            timer?.Change(Timeout.Infinite, Timeout.Infinite);
                            timer?.Dispose();
                        }
                        _saveTimers.Clear();
                    }

                    // Dispose semaphore
                    _fileLock?.Dispose();

                    Logger.Log("[UC1_DataManager] Data manager disposed", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UC1_DataManager] Error during disposal: {ex.Message}", LogLevel.Error);
                }

                _isDisposed = true;
            }
        }

        #endregion
    }

    #region Supporting Classes

    public class ManualDataLoadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsTjMode { get; set; }
        public string CurrentThermoSetpoint { get; set; }
        public string StirrerSetpoint { get; set; }
        public float LastTrSetpoint { get; set; }
        public float LastTjSetpoint { get; set; }
        public string DisplayValue { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DeviceSettingsLoadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DeviceSettings_1 DeviceSettings { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DataSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string DataType { get; set; }
        public string FilePath { get; set; }
        public string Trigger { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class RecoveryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string RecoveryMethod { get; set; }
    }

    public class DataSavedEventArgs : EventArgs
    {
        public string DataType { get; set; }
        public string FilePath { get; set; }
        public string Trigger { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DataLoadedEventArgs : EventArgs
    {
        public string DataType { get; set; }
        public string FilePath { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DataErrorEventArgs : EventArgs
    {
        public string ErrorType { get; set; }
        public string ErrorMessage { get; set; }
        public string FilePath { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    #endregion
}