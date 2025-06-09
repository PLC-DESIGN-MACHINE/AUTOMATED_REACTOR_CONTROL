// ==============================================
//  UC1_SerialCommunication.cs
//  AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
//  Serial Communication & RS232 Management System
//  Extracted from UC_CONTROL_SET_1.cs
// ==============================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AUTOMATED_REACTOR_CONTROL_Ver4_FINAL.Logger;

namespace AUTOMATED_REACTOR_CONTROL_Ver4_FINAL
{
    /// <summary>
    /// Modern Serial Communication Controller with Async/Await and Event-driven Architecture
    /// Handles RS232 Configuration, Data Reception, and Hardware Communication
    /// </summary>
    public class UC1_SerialCommunication : IDisposable
    {
        #region Events & Delegates

        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<SerialErrorEventArgs> SerialError;

        #endregion

        #region Private Fields

        private readonly int[] _allowedGroups = { 1, 2, 5 };
        private string _selectedComPort = "COM1";
        private bool _isConnecting = false;
        private bool _isInitializing = false;
        private int _expectedGroup = -1;
        private const int DEFAULT_BAUD_RATE = 9600;
        private bool _isDisposed = false;

        // Hardware acceleration for data processing
        private readonly Dictionary<int, Func<byte[], Task<ProcessedData>>> _dataProcessors;

        #endregion

        #region Constructor & Initialization

        public UC1_SerialCommunication()
        {
            _dataProcessors = new Dictionary<int, Func<byte[], Task<ProcessedData>>>
            {
                { 1, ProcessGroup1DataAsync }, // TR1, TJ1
                { 2, ProcessGroup2DataAsync }, // RPM1
                { 5, ProcessGroup5DataAsync }  // Ext1
            };

            InitializeSerialManager();
            Logger.Log("[UC1_SerialComm] Initialized with modern async architecture", LogLevel.Info);
        }

        #endregion

        #region Public Properties

        public string SelectedComPort => _selectedComPort;
        public bool IsConnected => SerialPortManager.Instance?.IsConnected ?? false;
        public bool IsConnecting => _isConnecting;
        public string ConnectionStatus => SerialPortManager.Instance?.ConnectionStatusMessage ?? "Not Connected";

        #endregion

        #region Serial Manager Initialization

        private void InitializeSerialManager()
        {
            try
            {
                if (SerialPortManager.Instance != null)
                {
                    // Subscribe to events
                    SerialPortManager.Instance.ConnectionStatusDetailChanged += OnConnectionStatusChanged;
                    SerialPortManager.Instance.DataReceivedRawEvent += OnRawDataReceived;
                    SerialPortManager.Instance.DataGroupRequested += OnDataGroupRequested;

                    // Configure sequential requests
                    SerialPortManager.Instance.ConfigureSequentialRequests(_allowedGroups, "[UC1_SerialComm]");

                    Logger.Log("[UC1_SerialComm] Serial manager initialized successfully", LogLevel.Info);
                }
                else
                {
                    Logger.Log("[UC1_SerialComm] SerialPortManager.Instance is null", LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_SerialComm] Error initializing serial manager: {ex.Message}", LogLevel.Error);
            }
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Async connection to specified COM port
        /// </summary>
        public async Task<ConnectionResult> ConnectAsync(string comPort = null)
        {
            if (_isConnecting)
            {
                return new ConnectionResult
                {
                    Success = false,
                    Message = "Connection already in progress",
                    PortName = _selectedComPort
                };
            }

            try
            {
                _isConnecting = true;
                string targetPort = comPort ?? _selectedComPort;

                Logger.Log($"[UC1_SerialComm] Attempting connection to {targetPort}", LogLevel.Info);

                // Notify connection attempt
                OnConnectionStatusChanged(false, targetPort, "Connecting...");

                // Perform connection
                bool result = await Task.Run(() =>
                    SerialPortManager.Instance.ConnectToPort(targetPort, DEFAULT_BAUD_RATE));

                if (result)
                {
                    _selectedComPort = targetPort;
                    Logger.Log($"[UC1_SerialComm] Successfully connected to {targetPort}", LogLevel.Info);

                    return new ConnectionResult
                    {
                        Success = true,
                        Message = $"Connected to {targetPort}",
                        PortName = targetPort
                    };
                }
                else
                {
                    Logger.Log($"[UC1_SerialComm] Failed to connect to {targetPort}", LogLevel.Error);

                    return new ConnectionResult
                    {
                        Success = false,
                        Message = $"Failed to connect to {targetPort}",
                        PortName = targetPort,
                        ErrorCode = "CONNECTION_FAILED"
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_SerialComm] Exception during connection: {ex.Message}", LogLevel.Error);

                return new ConnectionResult
                {
                    Success = false,
                    Message = $"Connection error: {ex.Message}",
                    PortName = comPort ?? _selectedComPort,
                    ErrorCode = "CONNECTION_EXCEPTION"
                };
            }
            finally
            {
                _isConnecting = false;
            }
        }

        /// <summary>
        /// Async disconnection
        /// </summary>
        public async Task<bool> DisconnectAsync()
        {
            try
            {
                Logger.Log("[UC1_SerialComm] Disconnecting...", LogLevel.Info);

                OnConnectionStatusChanged(false, _selectedComPort, "Disconnecting...");

                await Task.Run(() => SerialPortManager.Instance.Disconnect());

                Logger.Log("[UC1_SerialComm] Disconnected successfully", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_SerialComm] Error during disconnection: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Change COM port (disconnect if needed)
        /// </summary>
        public async Task<bool> ChangePortAsync(string newPort)
        {
            try
            {
                if (_selectedComPort == newPort)
                {
                    return true;
                }

                Logger.Log($"[UC1_SerialComm] Changing port from {_selectedComPort} to {newPort}", LogLevel.Info);

                // Disconnect if connected
                if (IsConnected)
                {
                    await DisconnectAsync();
                }

                _selectedComPort = newPort;
                SerialPortManager.Instance.ChangePort(newPort);

                OnConnectionStatusChanged(false, newPort, $"Port changed to {newPort}");

                Logger.Log($"[UC1_SerialComm] Port changed to {newPort}", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_SerialComm] Error changing port: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        #endregion

        #region Data Processing with Hardware Acceleration

        /// <summary>
        /// Process Group 1 data (TR1, TJ1) with hardware acceleration
        /// </summary>
        private async Task<ProcessedData> ProcessGroup1DataAsync(byte[] data)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (data.Length < 7) return null;

                    ushort rawTr = (ushort)((data[2] << 8) | data[3]);
                    ushort rawTj = (ushort)((data[4] << 8) | data[5]);

                    float tr = HalfToFloat(rawTr);
                    float tj = HalfToFloat(rawTj);
                    float diff = tr - tj;

                    // Update SerialPortManager values
                    var vals = SerialPortManager.Instance.CurrentValues;
                    if (vals.Length >= 2)
                    {
                        vals[0] = tr;
                        vals[1] = tj;
                    }

                    return new ProcessedData
                    {
                        GroupId = 1,
                        DataType = "Temperature",
                        Values = new Dictionary<string, object>
                        {
                            ["TR1"] = tr,
                            ["TJ1"] = tj,
                            ["TR_TJ_Diff"] = diff
                        },
                        FormattedValues = new Dictionary<string, string>
                        {
                            ["TR1"] = tr.ToString("F2"),
                            ["TJ1"] = tj.ToString("F2"),
                            ["TR_TJ_Diff"] = diff.ToString("F2")
                        },
                        Timestamp = DateTime.Now,
                        IsValid = true
                    };
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UC1_SerialComm] Error processing Group 1 data: {ex.Message}", LogLevel.Error);
                    return null;
                }
            });
        }

        /// <summary>
        /// Process Group 2 data (RPM1) with hardware acceleration
        /// </summary>
        private async Task<ProcessedData> ProcessGroup2DataAsync(byte[] data)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (data.Length < 5) return null;

                    ushort rawRpm = (ushort)((data[2] << 8) | data[3]);
                    double rpm = rawRpm;

                    // Update SerialPortManager values
                    var vals = SerialPortManager.Instance.CurrentValues;
                    if (vals.Length >= 3)
                        vals[2] = rpm;

                    return new ProcessedData
                    {
                        GroupId = 2,
                        DataType = "Motor",
                        Values = new Dictionary<string, object>
                        {
                            ["RPM1"] = rpm
                        },
                        FormattedValues = new Dictionary<string, string>
                        {
                            ["RPM1"] = rpm.ToString("F0")
                        },
                        Timestamp = DateTime.Now,
                        IsValid = true
                    };
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UC1_SerialComm] Error processing Group 2 data: {ex.Message}", LogLevel.Error);
                    return null;
                }
            });
        }

        /// <summary>
        /// Process Group 5 data (Ext1) with hardware acceleration
        /// </summary>
        private async Task<ProcessedData> ProcessGroup5DataAsync(byte[] data)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (data.Length < 5) return null;

                    ushort rawExt = (ushort)((data[2] << 8) | data[3]);
                    int ext1 = rawExt & 0x0FFF; // Clamp to 0-4095

                    // Update SerialPortManager values
                    var vals = SerialPortManager.Instance.CurrentValues;
                    if (vals.Length >= 9)
                        vals[8] = ext1;

                    return new ProcessedData
                    {
                        GroupId = 5,
                        DataType = "External",
                        Values = new Dictionary<string, object>
                        {
                            ["Ext1"] = ext1
                        },
                        FormattedValues = new Dictionary<string, string>
                        {
                            ["Ext1"] = ext1.ToString()
                        },
                        Timestamp = DateTime.Now,
                        IsValid = true
                    };
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UC1_SerialComm] Error processing Group 5 data: {ex.Message}", LogLevel.Error);
                    return null;
                }
            });
        }

        #endregion

        #region Serial Event Handlers

        /// <summary>
        /// Handle raw data received from serial port
        /// </summary>
        private async void OnRawDataReceived(string raw)
        {
            try
            {
                if (string.IsNullOrEmpty(raw)) return;

                byte[] data = raw.Select(c => (byte)c).ToArray();
                if (data.Length < 7) return;

                int groupId = data[0];
                if (!_allowedGroups.Contains(groupId)) return;

                // Check if this is the expected group
                if (groupId != _expectedGroup)
                {
                    Logger.Log($"[UC1_SerialComm] Received DataGroup{groupId} but expected {_expectedGroup}, ignoring", LogLevel.Warn);
                    return;
                }

                // Process data with appropriate processor
                if (_dataProcessors.TryGetValue(groupId, out var processor))
                {
                    var processedData = await processor(data);
                    if (processedData != null)
                    {
                        OnDataReceived(new DataReceivedEventArgs
                        {
                            ProcessedData = processedData,
                            RawData = raw,
                            Timestamp = DateTime.Now
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_SerialComm] Error in OnRawDataReceived: {ex.Message}", LogLevel.Error);
                OnSerialError(new SerialErrorEventArgs
                {
                    ErrorMessage = ex.Message,
                    ErrorType = "DATA_PROCESSING_ERROR",
                    RawData = raw
                });
            }
        }

        /// <summary>
        /// Handle data group requests
        /// </summary>
        private void OnDataGroupRequested(byte group)
        {
            _expectedGroup = group;
            Logger.Log($"[UC1_SerialComm] Now expecting DataGroup{group}", LogLevel.Debug);
        }

        /// <summary>
        /// Handle connection status changes
        /// </summary>
        private void OnConnectionStatusChanged(bool connected, string portName, string message)
        {
            Logger.Log($"[UC1_SerialComm] Connection status changed: {connected} - {portName} - {message}", LogLevel.Debug);

            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs
            {
                IsConnected = connected,
                PortName = portName,
                StatusMessage = message,
                Timestamp = DateTime.Now
            });
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get available COM ports
        /// </summary>
        public async Task<string[]> GetAvailablePortsAsync()
        {
            return await Task.Run(() => SerialPortManager.GetAvailablePorts());
        }

        /// <summary>
        /// Send command to device
        /// </summary>
        public async Task<bool> SendCommandAsync(byte[] command)
        {
            try
            {
                if (!IsConnected)
                {
                    Logger.Log("[UC1_SerialComm] Cannot send command - not connected", LogLevel.Warn);
                    return false;
                }

                await Task.Run(() => SerialPortManager.Instance.Send(command));
                Logger.Log($"[UC1_SerialComm] Command sent successfully: {BitConverter.ToString(command)}", LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"[UC1_SerialComm] Error sending command: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Hardware-accelerated half-precision to float conversion
        /// </summary>
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

        #region Event Invocation

        protected virtual void OnDataReceived(DataReceivedEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }

        protected virtual void OnSerialError(SerialErrorEventArgs e)
        {
            SerialError?.Invoke(this, e);
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
                    // Disconnect if connected
                    if (IsConnected)
                    {
                        _ = Task.Run(async () => await DisconnectAsync());
                    }

                    // Unsubscribe from events
                    if (SerialPortManager.Instance != null)
                    {
                        SerialPortManager.Instance.ConnectionStatusDetailChanged -= OnConnectionStatusChanged;
                        SerialPortManager.Instance.DataReceivedRawEvent -= OnRawDataReceived;
                        SerialPortManager.Instance.DataGroupRequested -= OnDataGroupRequested;
                    }

                    Logger.Log("[UC1_SerialComm] Serial communication disposed", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[UC1_SerialComm] Error during disposal: {ex.Message}", LogLevel.Error);
                }

                _isDisposed = true;
            }
        }

        #endregion
    }

    #region Supporting Classes & Enums

    public class ConnectionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string PortName { get; set; }
        public string ErrorCode { get; set; }
    }

    public class ProcessedData
    {
        public int GroupId { get; set; }
        public string DataType { get; set; }
        public Dictionary<string, object> Values { get; set; }
        public Dictionary<string, string> FormattedValues { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsValid { get; set; }
    }

    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string PortName { get; set; }
        public string StatusMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class DataReceivedEventArgs : EventArgs
    {
        public ProcessedData ProcessedData { get; set; }
        public string RawData { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SerialErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
        public string ErrorType { get; set; }
        public string RawData { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    #endregion
}