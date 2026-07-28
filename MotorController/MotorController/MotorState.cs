using System;

namespace MotorController
{
    internal sealed record MotorState
    {
        public int Speed { get; init; }

        public MotorState(int speed)
        {
            if (speed < 0 || speed > 10)
                throw new ArgumentOutOfRangeException(nameof(speed), "Speed must be between 0 and 10.");

            Speed = speed;
        }
    }
}
