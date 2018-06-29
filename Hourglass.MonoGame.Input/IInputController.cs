namespace Hourglass.Input
{
    public interface IInputController
    {
        bool Activated { get; }
        float Pressure { get; }

        void Poll(InputManager inputManager);
    }
}
