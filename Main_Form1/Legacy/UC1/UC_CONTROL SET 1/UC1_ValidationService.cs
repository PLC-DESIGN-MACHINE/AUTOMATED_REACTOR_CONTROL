// ==============================================
//  UC1_ValidationService.cs
//  AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
//  Input Validation & Range Checking System
//  Extracted from UC_CONTROL_SET_1.cs
// ==============================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Logger;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    /// <summary>
    /// Modern Validation Service with Async/Await and Advanced Error Handling
    /// Provides Min/Max Range Validation, Input Sanitization, and Error Recovery
    /// </summary>
    public class UC1_ValidationService : IDisposable
    {
        #region Events & Delegates

        public event EventHandler<ValidationErrorEventArgs> ValidationError;
        public event EventHandler<ValidationSuccessEventArgs> ValidationSuccess;

        #endregion

        #region Private Fields

        private readonly Dictionary<string, ValidationRule> _validationRules;
        private readonly Dictionary<string, string> _lastValidValues;
        private bool _isDisposed = false;

        #endregion

        #region Constructor & Initialization

        public UC1_ValidationService()
        {
            _validationRules = new Dictionary<string, ValidationRule>();
            _lastValidValues = new Dictionary<string, string>();

            InitializeDefaultRules();
            Logger.Log("[UC1_ValidationService] Initialized with modern validation architecture", LogLevel.Info);
        }

        private void InitializeDefaultRules()
        {
            // Temperature validation rules
            _validationRules["Temperature"] = new ValidationRule
            {
                DataType = typeof(float),
                MinValue = 0,
                MaxValue = 500,
                DecimalPlaces = 2,
                AllowNegative = false,
                ErrorMessageTemplate = "Temperature must be between {0:F2}°C and {1:F2}°C"
            };

            // RPM validation rules
            _validationRules["RPM"] = new ValidationRule
            {
                DataType = typeof(int),
                MinValue = 0,
                MaxValue = 2000,
                DecimalPlaces = 0,
                AllowNegative = false,
                ErrorMessageTemplate = "RPM must be between {0} and {1}"
            };

            // Percentage validation rules
            _validationRules["Percentage"] = new ValidationRule
            {
                DataType = typeof(float),
                MinValue = 0,
                MaxValue = 100,
                DecimalPlaces = 1,
                AllowNegative = false,
                ErrorMessageTemplate = "Percentage must be between {0:F1}% and {1:F1}%"
            };

            Logger.Log("[UC1_ValidationService] Default validation rules initialized", LogLevel.Debug);
        }

        #endregion

        #region Public Validation Methods

        /// <summary>
        /// Async range validation with hardware acceleration
        /// </summary>
        public async Task<ValidationResult> ValidateRangeAsync<T>(T value, T minValue, T maxValue, string parameterName)
            where T : IComparable<T>
        {
            return await Task.Run(() =>
            {
                try
                {
                    Logger.Log($"[UC1_ValidationService] Validating range for {parameterName}: {value} (Range: {minValue} - {maxValue})", LogLevel.Debug);

                    var result = new ValidationResult
                    {
                        ParameterName = parameterName,
                        Value = value,
                        MinValue = minValue,
                        MaxValue = maxValue,
                        Timestamp = DateTime.Now
                    };

                    // Range validation
                    if (value.CompareTo(minValue) < 0)
                    {
                        result.IsValid = false;
                        result.ErrorType = ValidationErrorType.BelowMinimum;
                        result.ErrorMessage = CreateRangeErrorMessage(value, minValue, maxValue, parameterName, true);

                        OnValidationError(new ValidationErrorEventArgs
                        {
                            ErrorMessage = result.ErrorMessage,
                            ErrorType = result.ErrorType.ToString(),
                            ParameterName = parameterName,
                            InvalidValue = value.ToString()
                        });

                        return result;
                    }

                    if (value.CompareTo(maxValue) > 0)
                    {
                        result.IsValid = false;
                        result.ErrorType = ValidationErrorType.AboveMaximum;
                        result.ErrorMessage = CreateRangeErrorMessage(value, minValue, maxValue, parameterName, false);

                        OnValidationError(new ValidationErrorEventArgs
                        {
                            ErrorMessage = result.ErrorMessage,
                            ErrorType = result.ErrorType.ToString(),
                            ParameterName = parameterName,
                            InvalidValue = value.ToString()
                        });

                        return result;
                    }

                    // Success
                    result.IsValid = true;
                    result.ClampedValue = value;

                    OnValidationSuccess(new ValidationSuccessEventArgs
                    {
                        ParameterName = parameterName,
                        ValidatedValue = value.ToString(),
                        Timestamp = DateTime.Now
                    });

                    Logger.Log($"[UC1_ValidationService] Range validation successful for {parameterName}: {value}", LogLevel.Debug);
                    return result;
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UC1_ValidationService] Error in ValidateRangeAsync: {ex.Message}", LogLevel.Error);

                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorType = ValidationErrorType.CriticalError,
                        ErrorMessage = $"Critical validation error: {ex.Message}",
                        ParameterName = parameterName,
                        Value = value,
                        Timestamp = DateTime.Now
                    };
                }
            });
        }

        /// <summary>
        /// Async input parsing and validation
        /// </summary>
        public async Task<ParseValidationResult> ValidateAndParseAsync(string input, string parameterType)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Logger.Log($"[UC1_ValidationService] Parsing and validating {parameterType}: '{input}'", LogLevel.Debug);

                    var result = new ParseValidationResult
                    {
                        OriginalInput = input,
                        ParameterType = parameterType,
                        Timestamp = DateTime.Now
                    };

                    // Get validation rule
                    if (!_validationRules.TryGetValue(parameterType, out ValidationRule rule))
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"No validation rule found for parameter type: {parameterType}";
                        return result;
                    }

                    // Input sanitization
                    string sanitizedInput = SanitizeInput(input);
                    if (string.IsNullOrWhiteSpace(sanitizedInput))
                    {
                        result.IsValid = false;
                        result.ErrorType = ValidationErrorType.EmptyInput;
                        result.ErrorMessage = $"Input cannot be empty for {parameterType}";
                        return result;
                    }

                    // Type-specific parsing
                    bool parseSuccess = false;
                    object parsedValue = null;

                    if (rule.DataType == typeof(float))
                    {
                        parseSuccess = float.TryParse(sanitizedInput, NumberStyles.Float,
                            CultureInfo.InvariantCulture, out float floatValue);
                        parsedValue = floatValue;
                    }
                    else if (rule.DataType == typeof(int))
                    {
                        parseSuccess = int.TryParse(sanitizedInput, out int intValue);
                        parsedValue = intValue;
                    }
                    else if (rule.DataType == typeof(double))
                    {
                        parseSuccess = double.TryParse(sanitizedInput, NumberStyles.Float,
                            CultureInfo.InvariantCulture, out double doubleValue);
                        parsedValue = doubleValue;
                    }

                    if (!parseSuccess)
                    {
                        result.IsValid = false;
                        result.ErrorType = ValidationErrorType.ParseError;
                        result.ErrorMessage = CreateParseErrorMessage(input, rule);
                        return result;
                    }

                    result.ParsedValue = parsedValue;

                    // Range validation
                    bool rangeValid = ValidateValueRange(parsedValue, rule, out string rangeError);
                    if (!rangeValid)
                    {
                        result.IsValid = false;
                        result.ErrorType = ValidationErrorType.RangeError;
                        result.ErrorMessage = rangeError;
                        return result;
                    }

                    // Apply clamping if needed
                    result.ClampedValue = ClampValue(parsedValue, rule);
                    result.FormattedValue = FormatValue(result.ClampedValue, rule);
                    result.IsValid = true;

                    // Store as last valid value
                    _lastValidValues[parameterType] = result.FormattedValue;

                    Logger.Log($"[UC1_ValidationService] Parse validation successful for {parameterType}: {result.FormattedValue}", LogLevel.Debug);
                    return result;
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UC1_ValidationService] Error in ValidateAndParseAsync: {ex.Message}", LogLevel.Error);

                    return new ParseValidationResult
                    {
                        IsValid = false,
                        ErrorType = ValidationErrorType.CriticalError,
                        ErrorMessage = $"Critical parsing error: {ex.Message}",
                        OriginalInput = input,
                        ParameterType = parameterType,
                        Timestamp = DateTime.Now
                    };
                }
            });
        }

        /// <summary>
        /// Show validation error with modern UI
        /// </summary>
        public void ShowValidationError(string errorMessage, TextBox targetTextBox = null, string recommendedValue = null)
        {
            try
            {
                // Show MessageBox with error details
                MessageBox.Show(errorMessage,
                               "ค่าที่ใส่เกินขอบเขตที่กำหนด - Modern Validation",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);

                // Restore recommended value if provided
                if (targetTextBox != null && !string.IsNullOrEmpty(recommendedValue))
                {
                    targetTextBox.Text = recommendedValue;
                    Logger.Log($"[UC1_ValidationService] Restored recommended value: {recommendedValue} to {targetTextBox.Name}", LogLevel.Info);

                    // Focus and select for user correction
                    if (targetTextBox.Enabled)
                    {
                        targetTextBox.Focus();
                        targetTextBox.SelectAll();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_ValidationService] Error showing validation error: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Rule Management

        /// <summary>
        /// Add or update validation rule
        /// </summary>
        public void SetValidationRule(string parameterType, ValidationRule rule)
        {
            try
            {
                _validationRules[parameterType] = rule;
                Logger.Log($"[UC1_ValidationService] Validation rule set for {parameterType}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_ValidationService] Error setting validation rule: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Get validation rule for parameter type
        /// </summary>
        public ValidationRule GetValidationRule(string parameterType)
        {
            return _validationRules.TryGetValue(parameterType, out ValidationRule rule) ? rule : null;
        }

        /// <summary>
        /// Get last valid value for parameter type
        /// </summary>
        public string GetLastValidValue(string parameterType)
        {
            return _lastValidValues.TryGetValue(parameterType, out string value) ? value : null;
        }

        #endregion

        #region Private Helper Methods

        private string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove leading/trailing whitespace
            input = input.Trim();

            // Remove multiple spaces
            while (input.Contains("  "))
            {
                input = input.Replace("  ", " ");
            }

            return input;
        }

        private bool ValidateValueRange(object value, ValidationRule rule, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                if (rule.DataType == typeof(float))
                {
                    float floatValue = (float)value;
                    float min = Convert.ToSingle(rule.MinValue);
                    float max = Convert.ToSingle(rule.MaxValue);

                    if (floatValue < min || floatValue > max)
                    {
                        errorMessage = string.Format(rule.ErrorMessageTemplate, min, max);
                        return false;
                    }
                }
                else if (rule.DataType == typeof(int))
                {
                    int intValue = (int)value;
                    int min = Convert.ToInt32(rule.MinValue);
                    int max = Convert.ToInt32(rule.MaxValue);

                    if (intValue < min || intValue > max)
                    {
                        errorMessage = string.Format(rule.ErrorMessageTemplate, min, max);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_ValidationService] Error validating range: {ex.Message}", LogLevel.Error);
                errorMessage = $"Range validation error: {ex.Message}";
                return false;
            }
        }

        private object ClampValue(object value, ValidationRule rule)
        {
            try
            {
                if (rule.DataType == typeof(float))
                {
                    float floatValue = (float)value;
                    float min = Convert.ToSingle(rule.MinValue);
                    float max = Convert.ToSingle(rule.MaxValue);
                    return Math.Max(min, Math.Min(max, floatValue));
                }
                else if (rule.DataType == typeof(int))
                {
                    int intValue = (int)value;
                    int min = Convert.ToInt32(rule.MinValue);
                    int max = Convert.ToInt32(rule.MaxValue);
                    return Math.Max(min, Math.Min(max, intValue));
                }

                return value;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_ValidationService] Error clamping value: {ex.Message}", LogLevel.Error);
                return value;
            }
        }

        private string FormatValue(object value, ValidationRule rule)
        {
            try
            {
                if (rule.DataType == typeof(float))
                {
                    return ((float)value).ToString($"F{rule.DecimalPlaces}", CultureInfo.InvariantCulture);
                }
                else if (rule.DataType == typeof(int))
                {
                    return ((int)value).ToString();
                }
                else if (rule.DataType == typeof(double))
                {
                    return ((double)value).ToString($"F{rule.DecimalPlaces}", CultureInfo.InvariantCulture);
                }

                return value.ToString();
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_ValidationService] Error formatting value: {ex.Message}", LogLevel.Error);
                return value.ToString();
            }
        }

        private string CreateRangeErrorMessage<T>(T value, T minValue, T maxValue, string parameterName, bool belowMin)
        {
            string direction = belowMin ? "ต่ำเกินไป" : "สูงเกินไป";
            string symbol = belowMin ? "🔻" : "🔺";
            string unit = GetUnitForParameter(parameterName);

            return $"{symbol} {parameterName}{direction}!\n\n" +
                   $"ค่าที่ใส่: {value}{unit}\n" +
                   $"ค่าที่อนุญาต: {minValue} - {maxValue}{unit}\n\n" +
                   $"กรุณาใส่ค่าระหว่าง {minValue} - {maxValue}{unit}";
        }

        private string CreateParseErrorMessage(string input, ValidationRule rule)
        {
            string typeName = GetFriendlyTypeName(rule.DataType);
            string unit = GetUnitForDataType(rule.DataType);

            return $"❌ รูปแบบ{typeName}ไม่ถูกต้อง!\n\n" +
                   $"ค่าที่ใส่: '{input}'\n\n" +
                   $"กรุณาใส่{GetInputExample(rule.DataType)}{unit}";
        }

        private string GetUnitForParameter(string parameterName)
        {
            return parameterName switch
            {
                "Temperature" => "°C",
                "RPM" => " RPM",
                "Percentage" => "%",
                _ => ""
            };
        }

        private string GetUnitForDataType(Type dataType)
        {
            if (dataType == typeof(float) || dataType == typeof(double))
                return " (เช่น 25.5)";
            else if (dataType == typeof(int))
                return " (เช่น 500)";

            return "";
        }

        private string GetFriendlyTypeName(Type dataType)
        {
            if (dataType == typeof(float) || dataType == typeof(double))
                return "ตัวเลขทศนิยม";
            else if (dataType == typeof(int))
                return "ตัวเลขจำนวนเต็ม";

            return "ข้อมูล";
        }

        private string GetInputExample(Type dataType)
        {
            if (dataType == typeof(float) || dataType == typeof(double))
                return "ตัวเลขทศนิยม";
            else if (dataType == typeof(int))
                return "ตัวเลขจำนวนเต็มเท่านั้น";

            return "ข้อมูลที่ถูกต้อง";
        }

        #endregion

        #region Event Invocation

        protected virtual void OnValidationError(ValidationErrorEventArgs e)
        {
            ValidationError?.Invoke(this, e);
        }

        protected virtual void OnValidationSuccess(ValidationSuccessEventArgs e)
        {
            ValidationSuccess?.Invoke(this, e);
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
                _validationRules?.Clear();
                _lastValidValues?.Clear();

                Logger.Log("[UC1_ValidationService] Validation service disposed", LogLevel.Info);
                _isDisposed = true;
            }
        }

        #endregion
    }

    #region Supporting Classes & Enums

    public class ValidationRule
    {
        public Type DataType { get; set; }
        public object MinValue { get; set; }
        public object MaxValue { get; set; }
        public int DecimalPlaces { get; set; }
        public bool AllowNegative { get; set; }
        public string ErrorMessageTemplate { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ParameterName { get; set; }
        public object Value { get; set; }
        public object MinValue { get; set; }
        public object MaxValue { get; set; }
        public object ClampedValue { get; set; }
        public string ErrorMessage { get; set; }
        public ValidationErrorType ErrorType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ParseValidationResult
    {
        public bool IsValid { get; set; }
        public string OriginalInput { get; set; }
        public string ParameterType { get; set; }
        public object ParsedValue { get; set; }
        public object ClampedValue { get; set; }
        public string FormattedValue { get; set; }
        public string ErrorMessage { get; set; }
        public ValidationErrorType ErrorType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ValidationErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
        public string ErrorType { get; set; }
        public string ParameterName { get; set; }
        public string OriginalInput { get; set; }
        public string InvalidValue { get; set; }
        public string RecommendedValue { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class ValidationSuccessEventArgs : EventArgs
    {
        public string ParameterName { get; set; }
        public string ValidatedValue { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum ValidationErrorType
    {
        ParseError,
        RangeError,
        BelowMinimum,
        AboveMaximum,
        EmptyInput,
        CriticalError
    }

    #endregion
}