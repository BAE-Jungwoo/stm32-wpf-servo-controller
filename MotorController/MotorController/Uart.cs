using System;
using System.IO.Ports;

namespace MotorController
{
    public class Uart : IDisposable
    {
        private SerialPort _port;

        public bool IsConnected { get; private set; } = false;

        public Uart(string port)
        {
            try
            {
                _port = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
                _port.Open();
                IsConnected = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Port open failed: {e.Message}");
                IsConnected = false;
            }
        }

        public bool Send(byte cmd)   // int -> byte
        {
            try
            {
                _port.Write(new byte[] { cmd }, 0, 1);   // int[] -> byte[]
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Send failed: {e.Message}");
                return false;
            }
        }

        public int? Recv()
        {
            try
            {
                _port.ReadTimeout = 1000;
                int b = _port.ReadByte();
                return b;
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Receive timeout");
                return null;
            }
        }

        public void Dispose()
        {
            if (_port != null && _port.IsOpen)
            {
                _port.Close();
            }
            _port?.Dispose();
        }
    }
}