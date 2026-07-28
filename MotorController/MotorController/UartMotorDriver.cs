using System;
using System.IO;
using System.Threading.Tasks;

namespace MotorController
{
    internal sealed class UartMotorDriver : IMotorDriver, IDisposable
    {
        private readonly Uart uart;

        public UartMotorDriver(string port)
        {
            uart = new Uart(port);
            if (!uart.IsConnected)
            {
                throw new Exception($"Failed to connect to UART port {port}");
            }
        }

        public void Dispose()
        {
            try
            {
                // dispose underlying UART
                uart?.Dispose();
            }
            catch
            {
                // swallow dispose exceptions
            }
        }

        public async Task<MotorState> ActivateAsync()
        {
            return await SendAndReceiveAsync(MotorCommand.Activate).ConfigureAwait(false);
        }

        public async Task<MotorState> DeactivateAsync()
        {
            return await SendAndReceiveAsync(MotorCommand.Deactivate).ConfigureAwait(false);
        }

        public async Task<MotorState> AccelerateAsync()
        {
            return await SendAndReceiveAsync(MotorCommand.Accelerate).ConfigureAwait(false);
        }

        public async Task<MotorState> DecelerateAsync()
        {
            return await SendAndReceiveAsync(MotorCommand.Decelerate).ConfigureAwait(false);
        }

        private Task<MotorState> SendAndReceiveAsync(MotorCommand cmdType)
        {
            return Task.Run(() =>
            {
                // send command: use enum numeric value as single byte
                var byteCmd = (byte)cmdType;
                var ok = uart.Send(byteCmd);
                if (!ok)
                    throw new IOException("UART send failed");

                // receive response (blocking read with timeout inside Uart.Recv)
                var response = uart.Recv();
                if (!response.HasValue)
                    throw new TimeoutException("No response from motor (UART receive timed out)");

                // interpret response as 0..10 speed value (single byte)
                var raw = response.Value;
                var speed = Math.Clamp(raw, 0, 10);
                return new MotorState(speed);
            });
        }
    }
}
