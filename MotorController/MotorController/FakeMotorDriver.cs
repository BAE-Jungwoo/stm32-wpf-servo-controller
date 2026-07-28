using System.Diagnostics;

namespace MotorController
{
    internal sealed class FakeMotorDriver : IMotorDriver
    {
        private readonly Random _random = new Random();

        public async Task<MotorState> ActivateAsync()
        {
            Debug.WriteLine("[Motor] Activate (sending command)");
            await Task.Delay(120); // simulate UART roundtrip
            var speed = _random.Next(0, 11);
            var state = new MotorState(speed);
            Debug.WriteLine($"[Motor] Activate -> speed {state.Speed}");
            return state;
        }

        public async Task<MotorState> DeactivateAsync()
        {
            Debug.WriteLine("[Motor] Deactivate (sending command)");
            await Task.Delay(100);
            var state = new MotorState(0);
            Debug.WriteLine($"[Motor] Deactivate -> speed {state.Speed}");
            return state;
        }

        public async Task<MotorState> AccelerateAsync()
        {
            Debug.WriteLine("[Motor] Accelerate (sending command)");
            await Task.Delay(80);
            var state = new MotorState(_random.Next(0, 11));
            Debug.WriteLine($"[Motor] Accelerate -> speed {state.Speed}");
            return state;
        }

        public async Task<MotorState> DecelerateAsync()
        {
            Debug.WriteLine("[Motor] Decelerate (sending command)");
            await Task.Delay(80);
            var val = Math.Max(0, _random.Next(0, 11) - 1);
            var state = new MotorState(val);
            Debug.WriteLine($"[Motor] Decelerate -> speed {state.Speed}");
            return state;
        }
    }
}
