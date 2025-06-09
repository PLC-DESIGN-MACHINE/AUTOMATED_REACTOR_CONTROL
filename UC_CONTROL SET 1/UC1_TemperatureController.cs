// ==============================================
//  UC1_TemperatureController.cs
//  AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
//  Temperature Control & Validation System
//  Extracted from UC_CONTROL_SET_1.cs
// ==============================================

using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Logger;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    /// <summary>
    /// Modern Temperature Controller with Async/Await and Event-driven Architecture
    /// Handles Temperature Validation, Setpoint Management, and Tr/Tj Mode Switching
    /// </summary>
    public class UC1_TemperatureController : IDisposable
    {
        #region Events & Delegates

        public event EventHandler<TemperatureChangedEventArgs> TemperatureChanged;
        public event EventHandler<TemperatureModeChangedEventArgs> ModeChanged;
        public event EventHandler<ValidationErrorEventArgs> ValidationError;

        #endregion

        #region Private Fields

        private readonly DeviceSettings_1 _deviceSettings;
        private readonly UC1_ValidationService _validationService;
        private string _lastValidValue = "25.0";
        private bool _isTjMode = false;
        private bool _isDisposed = false;

        #endregion

        #region Constructor & Initialization

        public UC1_TemperatureController(DeviceSettings_1 deviceSettings, UC1_ValidationService validationService)
        {
            _deviceSettings = deviceSettings ?? throw new ArgumentNullException(nameof(deviceSettings));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));

            Logger.Log("[UC1_TempController] Initialized with modern architecture", LogLevel.Info);
        }

        #endregion

        #region Public Properties

        public string LastValidValue => _lastValidValue;
        public bool IsTjMode => _isTjMode;
        public float MinTemperature => _deviceSettings.TempMin_1;
        public float MaxTemperature => _deviceSettings.TempMax_1;

        #endregion

        #region Temperature Validation & Control

        /// <summary>
        /// Async temperature validation with modern error handling
        /// </summary>
        public async Task<TemperatureValidationResult> ValidateTemperatureAsync(string input)
        {
            try
            {
                Logger.Log($"[UC1_TempController] Validating temperature input: '{input}'", LogLevel.Debug);

                // Parse validation
                if (!float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    var parseError = new TemperatureValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = CreateParseErrorMessage(input),
                        ErrorType = TemperatureErrorType.ParseError,
                        OriginalInput = input,
                        RecommendedValue = _lastValidValue
                    };

                    OnValidationError(parseError);
                    return parseError;
                }

                // Range validation using validation service
                var rangeResult = await _validationService.ValidateRangeAsync(
                    value, MinTemperature, MaxTemperature, "Temperature");

                if (!rangeResult.IsValid)
                {
                    var rangeError = new TemperatureValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = CreateRangeErrorMessage(value, rangeResult.ErrorMessage),
                        ErrorType = TemperatureErrorType.RangeError,
                        OriginalInput = input,
                        ParsedValue = value,
                        RecommendedValue = _lastValidValue
                    };

                    OnValidationError(rangeError);
                    return rangeError;
                }

                // Success validation
                var successResult = new TemperatureValidationResult
                {
                    IsValid = true,
                    ParsedValue = value,
                    FormattedValue = value.ToString("F2", CultureInfo.InvariantCulture),
                    OriginalInput = input
                };

                Logger.Log($"[UC1_TempController] Temperature validation successful: {value:F2}°C", LogLevel.Info);
                return successResult;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_TempController] Critical error in ValidateTemperatureAsync: {ex.Message}", LogLevel.Error);

                return new TemperatureValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"⚠️ เกิดข้อผิดพลาดร้ายแรง!\n\n{ex.Message}\n\nระบบจะคืนค่าเดิม",
                    ErrorType = TemperatureErrorType.CriticalError,
                    OriginalInput = input,
                    RecommendedValue = _lastValidValue
                };
            }
        }

        /// <summary>
        /// Async temperature saving with event-driven updates
        /// </summary>
        public async Task<bool> SaveTemperatureAsync(string input, bool updateLastValid = true)
        {
            try
            {
                var validationResult = await ValidateTemperatureAsync(input);

                if (!validationResult.IsValid)
                {
                    return false;
                }

                // Update last valid value
                if (updateLastValid)
                {
                    _lastValidValue = validationResult.FormattedValue;
                }

                // Fire temperature changed event
                OnTemperatureChanged(new TemperatureChangedEventArgs
                {
                    NewValue = validationResult.ParsedValue,
                    FormattedValue = validationResult.FormattedValue,
                    PreviousValue = _lastValidValue,
                    Mode = _isTjMode ? TemperatureMode.Tj : TemperatureMode.Tr,
                    Timestamp = DateTime.Now
                });

                Logger.Log($"[UC1_TempController] Temperature saved successfully: {validationResult.ParsedValue:F2}°C", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_TempController] Error in SaveTemperatureAsync: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        #endregion

        #region Mode Management (Tr/Tj)

        /// <summary>
        /// Async mode switching with validation
        /// </summary>
        public async Task<bool> SwitchModeAsync(bool isTjMode, float? currentValue = null)
        {
            try
            {
                Logger.Log($"[UC1_TempController] Switching mode to {(isTjMode ? "Tj" : "Tr")}", LogLevel.Info);

                var previousMode = _isTjMode;
                _isTjMode = isTjMode;

                // Validate current value if provided
                float valueToUse = currentValue ?? 25.0f;
                if (currentValue.HasValue)
                {
                    var validation = await ValidateTemperatureAsync(currentValue.Value.ToString("F2"));
                    if (!validation.IsValid)
                    {
                        Logger.Log($"[UC1_TempController] Mode switch failed - invalid current value: {currentValue}", LogLevel.Error);
                        _isTjMode = previousMode; // Revert
                        return false;
                    }
                    valueToUse = validation.ParsedValue;
                }

                // Fire mode changed event
                OnModeChanged(new TemperatureModeChangedEventArgs
                {
                    NewMode = _isTjMode ? TemperatureMode.Tj : TemperatureMode.Tr,
                    PreviousMode = previousMode ? TemperatureMode.Tj : TemperatureMode.Tr,
                    CurrentValue = valueToUse,
                    Timestamp = DateTime.Now
                });

                Logger.Log($"[UC1_TempController] Mode switched successfully to {(_isTjMode ? "Tj" : "Tr")}", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_TempController] Error in SwitchModeAsync: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Get recommended setpoint for current mode
        /// </summary>
        public float GetRecommendedSetpoint(float trValue, float tjValue)
        {
            return _isTjMode ? tjValue : trValue;
        }

        #endregion

        #region Error Message Generation

        private string CreateParseErrorMessage(string input)
        {
            return $"❌ รูปแบบอุณหภูมิไม่ถูกต้อง!\n\n" +
                   $"ค่าที่ใส่: '{input}'\n\n" +
                   $"กรุณาใส่ตัวเลขเท่านั้น (เช่น 25.5)";
        }

        private string CreateRangeErrorMessage(float value, string baseMessage)
        {
            float difference = value < MinTemperature ? MinTemperature - value : value - MaxTemperature;
            string direction = value < MinTemperature ? "ต่ำเกินไป" : "สูงเกินไป";
            string symbol = value < MinTemperature ? "🔻" : "🔺";

            return $"{symbol} อุณหภูมิ{direction}!\n\n" +
                   $"ค่าที่ใส่: {value:F2}°C\n" +
                   $"ค่าที่อนุญาต: {MinTemperature:F2} - {MaxTemperature:F2}°C\n" +
                   $"เกินขอบเขต: {difference:F2}°C\n\n" +
                   $"กรุณาใส่ค่าระหว่าง {MinTemperature:F2} - {MaxTemperature:F2}°C";
        }

        #endregion

        #region Event Handlers

        protected virtual void OnTemperatureChanged(TemperatureChangedEventArgs e)
        {
            TemperatureChanged?.Invoke(this, e);
        }

        protected virtual void OnModeChanged(TemperatureModeChangedEventArgs e)
        {
            ModeChanged?.Invoke(this, e);
        }

        protected virtual void OnValidationError(TemperatureValidationResult error)
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
                // Cleanup resources
                Logger.Log("[UC1_TempController] Temperature controller disposed", LogLevel.Info);
                _isDisposed = true;
            }
        }

        #endregion
    }

    #region Supporting Classes & Enums

    public class TemperatureValidationResult
    {
        public bool IsValid { get; set; }
        public float ParsedValue { get; set; }
        public string FormattedValue { get; set; }
        public string ErrorMessage { get; set; }
        public TemperatureErrorType ErrorType { get; set; }
        public string OriginalInput { get; set; }
        public string RecommendedValue { get; set; }
    }

    public class TemperatureChangedEventArgs : EventArgs
    {
        public float NewValue { get; set; }
        public string FormattedValue { get; set; }
        public string PreviousValue { get; set; }
        public TemperatureMode Mode { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class TemperatureModeChangedEventArgs : EventArgs
    {
        public TemperatureMode NewMode { get; set; }
        public TemperatureMode PreviousMode { get; set; }
        public float CurrentValue { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum TemperatureMode
    {
        Tr,
        Tj
    }

    public enum TemperatureErrorType
    {
        ParseError,
        RangeError,
        CriticalError
    }

    #endregion
}