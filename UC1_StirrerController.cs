// ==============================================
//  UC1_StirrerController.cs
//  AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
//  Stirrer/Motor Control & RPM Management System
//  Extracted from UC_CONTROL_SET_1.cs
// ==============================================

using System;
using System.Threading.Tasks;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Logger;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    /// <summary>
    /// Modern Stirrer Controller with Async/Await and Event-driven Architecture
    /// Handles RPM Validation, Motor Control, and Speed Management
    /// </summary>
    public class UC1_StirrerController : IDisposable
    {
        #region Events & Delegates

        public event EventHandler<StirrerSpeedChangedEventArgs> SpeedChanged;
        public event EventHandler<StirrerStatusChangedEventArgs> StatusChanged;
        public event EventHandler<ValidationErrorEventArgs> ValidationError;

        #endregion

        #region Private Fields

        private readonly DeviceSettings_1 _deviceSettings;
        private readonly UC1_ValidationService _validationService;
        private string _lastValidRpm = "0";
        private bool _isRunning = false;
        private int _currentRpm = 0;
        private bool _isDisposed = false;

        #endregion

        #region Constructor & Initialization

        public UC1_StirrerController(DeviceSettings_1 deviceSettings, UC1_ValidationService validationService)
        {
            _deviceSettings = deviceSettings ?? throw new ArgumentNullException(nameof(deviceSettings));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));

            Logger.Log("[UC1_StirrerController] Initialized with modern architecture", LogLevel.Info);
        }

        #endregion

        #region Public Properties

        public string LastValidRpm => _lastValidRpm;
        public bool IsRunning => _isRunning;
        public int CurrentRpm => _currentRpm;
        public int MinRpm => _deviceSettings.StirrerMin_1;
        public int MaxRpm => _deviceSettings.StirrerMax_1;

        #endregion

        #region RPM Validation & Control

        /// <summary>
        /// Async RPM validation with modern error handling
        /// </summary>
        public async Task<StirrerValidationResult> ValidateRpmAsync(string input)
        {
            try
            {
                Logger.Log($"[UC1_StirrerController] Validating RPM input: '{input}'", LogLevel.Debug);

                // Parse validation
                if (!int.TryParse(input, out int rpm))
                {
                    var parseError = new StirrerValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = CreateParseErrorMessage(input),
                        ErrorType = StirrerErrorType.ParseError,
                        OriginalInput = input,
                        RecommendedValue = _lastValidRpm
                    };

                    OnValidationError(parseError);
                    return parseError;
                }

                // Range validation using validation service
                var rangeResult = await _validationService.ValidateRangeAsync(
                    rpm, MinRpm, MaxRpm, "RPM");

                if (!rangeResult.IsValid)
                {
                    var rangeError = new StirrerValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = CreateRangeErrorMessage(rpm, rangeResult.ErrorMessage),
                        ErrorType = StirrerErrorType.RangeError,
                        OriginalInput = input,
                        ParsedValue = rpm,
                        RecommendedValue = _lastValidRpm
                    };

                    OnValidationError(rangeError);
                    return rangeError;
                }

                // Success validation
                var successResult = new StirrerValidationResult
                {
                    IsValid = true,
                    ParsedValue = rpm,
                    FormattedValue = rpm.ToString(),
                    OriginalInput = input
                };

                Logger.Log($"[UC1_StirrerController] RPM validation successful: {rpm} RPM", LogLevel.Info);
                return successResult;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_StirrerController] Critical error in ValidateRpmAsync: {ex.Message}", LogLevel.Error);

                return new StirrerValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"⚠️ เกิดข้อผิดพลาดร้ายแรง!\n\n{ex.Message}\n\nระบบจะคืนค่าเดิม",
                    ErrorType = StirrerErrorType.CriticalError,
                    OriginalInput = input,
                    RecommendedValue = _lastValidRpm
                };
            }
        }

        /// <summary>
        /// Async stirrer speed saving with event-driven updates
        /// </summary>
        public async Task<bool> SaveStirrerSpeedAsync(string input, bool updateLastValid = true)
        {
            try
            {
                var validationResult = await ValidateRpmAsync(input);

                if (!validationResult.IsValid)
                {
                    return false;
                }

                var previousRpm = _currentRpm;
                _currentRpm = validationResult.ParsedValue;

                // Update last valid value
                if (updateLastValid)
                {
                    _lastValidRpm = validationResult.FormattedValue;
                }

                // Update running status
                var previousStatus = _isRunning;
                _isRunning = _currentRpm > 0;

                // Fire speed changed event
                OnSpeedChanged(new StirrerSpeedChangedEventArgs
                {
                    NewRpm = _currentRpm,
                    PreviousRpm = previousRpm,
                    FormattedValue = validationResult.FormattedValue,
                    Timestamp = DateTime.Now
                });

                // Fire status changed event if status changed
                if (_isRunning != previousStatus)
                {
                    OnStatusChanged(new StirrerStatusChangedEventArgs
                    {
                        IsRunning = _isRunning,
                        PreviousStatus = previousStatus,
                        CurrentRpm = _currentRpm,
                        Timestamp = DateTime.Now
                    });
                }

                Logger.Log($"[UC1_StirrerController] Stirrer speed saved successfully: {_currentRpm} RPM", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_StirrerController] Error in SaveStirrerSpeedAsync: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        #endregion

        #region Motor Control Operations

        /// <summary>
        /// Start stirrer with specified RPM
        /// </summary>
        public async Task<bool> StartStirrerAsync(int rpm)
        {
            try
            {
                Logger.Log($"[UC1_StirrerController] Starting stirrer at {rpm} RPM", LogLevel.Info);

                var result = await SaveStirrerSpeedAsync(rpm.ToString());
                if (!result)
                {
                    return false;
                }

                Logger.Log($"[UC1_StirrerController] Stirrer started successfully at {rpm} RPM", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_StirrerController] Error starting stirrer: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Stop stirrer (set RPM to 0)
        /// </summary>
        public async Task<bool> StopStirrerAsync()
        {
            try
            {
                Logger.Log("[UC1_StirrerController] Stopping stirrer", LogLevel.Info);

                var result = await SaveStirrerSpeedAsync("0");
                if (!result)
                {
                    return false;
                }

                Logger.Log("[UC1_StirrerController] Stirrer stopped successfully", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_StirrerController] Error stopping stirrer: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Gradually change stirrer speed (ramp up/down)
        /// </summary>
        public async Task<bool> RampSpeedAsync(int targetRpm, int stepSize = 50, int delayMs = 100)
        {
            try
            {
                Logger.Log($"[UC1_StirrerController] Ramping speed from {_currentRpm} to {targetRpm} RPM", LogLevel.Info);

                // Validate target RPM
                var validation = await ValidateRpmAsync(targetRpm.ToString());
                if (!validation.IsValid)
                {
                    return false;
                }

                int currentStep = _currentRpm;
                int direction = targetRpm > currentStep ? 1 : -1;
                stepSize *= direction;

                while ((direction > 0 && currentStep < targetRpm) || (direction < 0 && currentStep > targetRpm))
                {
                    currentStep += stepSize;

                    // Clamp to target
                    if ((direction > 0 && currentStep > targetRpm) || (direction < 0 && currentStep < targetRpm))
                    {
                        currentStep = targetRpm;
                    }

                    await SaveStirrerSpeedAsync(currentStep.ToString(), false);
                    await Task.Delay(delayMs);
                }

                // Final update with last valid value update
                await SaveStirrerSpeedAsync(targetRpm.ToString(), true);

                Logger.Log($"[UC1_StirrerController] Speed ramping completed to {targetRpm} RPM", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_StirrerController] Error in RampSpeedAsync: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        #endregion

        #region Status & Information

        /// <summary>
        /// Get stirrer status information
        /// </summary>
        public StirrerStatusInfo GetStatusInfo()
        {
            return new StirrerStatusInfo
            {
                CurrentRpm = _currentRpm,
                IsRunning = _isRunning,
                LastValidRpm = _lastValidRpm,
                MinRpm = MinRpm,
                MaxRpm = MaxRpm,
                PercentageOfMax = _currentRpm > 0 ? (float)_currentRpm / MaxRpm * 100 : 0,
                Timestamp = DateTime.Now
            };
        }

        #endregion

        #region Error Message Generation

        private string CreateParseErrorMessage(string input)
        {
            return $"❌ รูปแบบความเร็วกวนไม่ถูกต้อง!\n\n" +
                   $"ค่าที่ใส่: '{input}'\n\n" +
                   $"กรุณาใส่ตัวเลขจำนวนเต็มเท่านั้น (เช่น 500)";
        }

        private string CreateRangeErrorMessage(int rpm, string baseMessage)
        {
            int difference = rpm < MinRpm ? MinRpm - rpm : rpm - MaxRpm;
            string direction = rpm < MinRpm ? "ต่ำเกินไป" : "สูงเกินไป";
            string symbol = rpm < MinRpm ? "🔻" : "🔺";

            return $"{symbol} ความเร็วกวน{direction}!\n\n" +
                   $"ค่าที่ใส่: {rpm} RPM\n" +
                   $"ค่าที่อนุญาต: {MinRpm} - {MaxRpm} RPM\n" +
                   $"เกินขอบเขต: {difference} RPM\n\n" +
                   $"กรุณาใส่ค่าระหว่าง {MinRpm} - {MaxRpm} RPM";
        }

        #endregion

        #region Event Handlers

        protected virtual void OnSpeedChanged(StirrerSpeedChangedEventArgs e)
        {
            SpeedChanged?.Invoke(this, e);
        }

        protected virtual void OnStatusChanged(StirrerStatusChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        protected virtual void OnValidationError(StirrerValidationResult error)
        {
            ValidationError?.Invoke(this, new ValidationErrorEventArgs
            {
                ErrorMessage = error.ErrorMessage,
                ErrorType = error.ErrorType.ToString(),
                OriginalInput = error.OriginalInput,
                RecommendedValue = error.RecommendedValue
            });
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
                // Stop stirrer if running
                if (_isRunning)
                {
                    _ = Task.Run(async () => await StopStirrerAsync());
                }

                Logger.Log("[UC1_StirrerController] Stirrer controller disposed", LogLevel.Info);
                _isDisposed = true;
            }
        }

        #endregion
    }

    #region Supporting Classes & Enums

    public class StirrerValidationResult
    {
        public bool IsValid { get; set; }
        public int ParsedValue { get; set; }
        public string FormattedValue { get; set; }
        public string ErrorMessage { get; set; }
        public StirrerErrorType ErrorType { get; set; }
        public string OriginalInput { get; set; }
        public string RecommendedValue { get; set; }
    }

    public class StirrerSpeedChangedEventArgs : EventArgs
    {
        public int NewRpm { get; set; }
        public int PreviousRpm { get; set; }
        public string FormattedValue { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class StirrerStatusChangedEventArgs : EventArgs
    {
        public bool IsRunning { get; set; }
        public bool PreviousStatus { get; set; }
        public int CurrentRpm { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class StirrerStatusInfo
    {
        public int CurrentRpm { get; set; }
        public bool IsRunning { get; set; }
        public string LastValidRpm { get; set; }
        public int MinRpm { get; set; }
        public int MaxRpm { get; set; }
        public float PercentageOfMax { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum StirrerErrorType
    {
        ParseError,
        RangeError,
        CriticalError
    }

    #endregion
}