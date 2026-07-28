
namespace MotorController
{
    internal interface IMotorDriver
    {
        Task<MotorState> ActivateAsync();
        Task<MotorState> DeactivateAsync();

        Task<MotorState> AccelerateAsync();
        Task<MotorState> DecelerateAsync();
    }
}
