using System;
using System.IO.Ports;

namespace VisionSortingController.Services
{
    // Modbus RTU Master Service for PLC Xinje XD2-16T-E
    public class ModbusRtuService : IDisposable
    {
        private SerialPort _serialPort;
        private readonly object _syncLock = new object();

        // Bottom-up signals (Rule 5.1.B)
        public event Action<bool, string> OnConnectionChanged;
        public event Action<string> OnServiceLog;

        public bool IsConnected
        {
            get { return _serialPort != null && _serialPort.IsOpen; }
        }

        public string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        public bool Connect(string portName, int baudRate = 19200, Parity parity = Parity.Even, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            lock (_syncLock)
            {
                try
                {
                    Disconnect();
                    _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
                    {
                        ReadTimeout = 500,
                        WriteTimeout = 500
                    };
                    _serialPort.Open();
                    if (OnServiceLog != null)
                        OnServiceLog(string.Format("Connected to PLC on {0} ({1}, {2}, {3}, {4})", portName, baudRate, parity, dataBits, stopBits));
                    if (OnConnectionChanged != null)
                        OnConnectionChanged(true, portName);
                    return true;
                }
                catch (Exception ex)
                {
                    if (OnServiceLog != null)
                        OnServiceLog(string.Format("Failed to open {0}: {1}", portName, ex.Message));
                    if (OnConnectionChanged != null)
                        OnConnectionChanged(false, portName);
                    return false;
                }
            }
        }

        public void Disconnect()
        {
            lock (_syncLock)
            {
                if (_serialPort != null)
                {
                    string port = _serialPort.PortName;
                    if (_serialPort.IsOpen)
                    {
                        try { _serialPort.Close(); } catch { }
                    }
                    _serialPort.Dispose();
                    _serialPort = null;
                    if (OnServiceLog != null)
                        OnServiceLog(string.Format("Disconnected from {0}", port));
                    if (OnConnectionChanged != null)
                        OnConnectionChanged(false, port);
                }
            }
        }

        // Modbus Function Code 05: Write Single Coil
        public bool WriteSingleCoil(byte slaveId, ushort coilAddress, bool state)
        {
            lock (_syncLock)
            {
                if (!IsConnected || _serialPort == null)
                {
                    if (OnServiceLog != null)
                        OnServiceLog(string.Format("[OFFLINE-SIM] WriteSingleCoil Slave={0}, Addr={1}, Val={2}", slaveId, coilAddress, state ? "ON" : "OFF"));
                    return true;
                }

                try
                {
                    byte[] frame = new byte[8];
                    frame[0] = slaveId;
                    frame[1] = 0x05; // Function 05: Force Single Coil
                    frame[2] = (byte)(coilAddress >> 8);
                    frame[3] = (byte)(coilAddress & 0xFF);
                    frame[4] = state ? (byte)0xFF : (byte)0x00;
                    frame[5] = 0x00;

                    ushort crc = ComputeCrc16(frame, 6);
                    frame[6] = (byte)(crc & 0xFF);        // CRC Low
                    frame[7] = (byte)((crc >> 8) & 0xFF); // CRC High

                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();
                    _serialPort.Write(frame, 0, frame.Length);

                    byte[] response = ReadExactBytes(8);
                    ushort respCrc = ComputeCrc16(response, 6);
                    bool crcOk = response[6] == (byte)(respCrc & 0xFF) && response[7] == (byte)((respCrc >> 8) & 0xFF);

                    if (!crcOk)
                    {
                        if (OnServiceLog != null)
                            OnServiceLog(string.Format("CRC mismatch on FC05 Addr={0}", coilAddress));
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    if (OnServiceLog != null)
                        OnServiceLog(string.Format("Modbus FC05 error (Addr={0}): {1}", coilAddress, ex.Message));
                    return false;
                }
            }
        }

        // Modbus Function Code 01: Read Coils
        public bool[] ReadCoils(byte slaveId, ushort startAddress, ushort count)
        {
            lock (_syncLock)
            {
                if (!IsConnected || _serialPort == null)
                    return null;

                try
                {
                    byte[] frame = new byte[8];
                    frame[0] = slaveId;
                    frame[1] = 0x01; // Function 01: Read Coils
                    frame[2] = (byte)(startAddress >> 8);
                    frame[3] = (byte)(startAddress & 0xFF);
                    frame[4] = (byte)(count >> 8);
                    frame[5] = (byte)(count & 0xFF);

                    ushort crc = ComputeCrc16(frame, 6);
                    frame[6] = (byte)(crc & 0xFF);
                    frame[7] = (byte)((crc >> 8) & 0xFF);

                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();
                    _serialPort.Write(frame, 0, frame.Length);

                    int byteCount = (count + 7) / 8;
                    int expectedLen = 3 + byteCount + 2;
                    byte[] response = ReadExactBytes(expectedLen);

                    ushort respCrc = ComputeCrc16(response, expectedLen - 2);
                    if (response[expectedLen - 2] != (byte)(respCrc & 0xFF) ||
                        response[expectedLen - 1] != (byte)((respCrc >> 8) & 0xFF))
                    {
                        return null;
                    }

                    bool[] states = new bool[count];
                    for (int i = 0; i < count; i++)
                    {
                        int byteIdx = 3 + (i / 8);
                        int bitIdx = i % 8;
                        states[i] = ((response[byteIdx] >> bitIdx) & 0x01) == 1;
                    }
                    return states;
                }
                catch
                {
                    return null;
                }
            }
        }

        private byte[] ReadExactBytes(int count)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count && _serialPort != null)
            {
                int read = _serialPort.Read(buffer, offset, count - offset);
                if (read <= 0)
                    throw new TimeoutException("Serial read timeout");
                offset += read;
            }
            return buffer;
        }

        public static ushort ComputeCrc16(byte[] data, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    else
                        crc >>= 1;
                }
            }
            return crc;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
