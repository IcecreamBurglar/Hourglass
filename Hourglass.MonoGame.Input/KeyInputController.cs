using Microsoft.Xna.Framework.Input;

namespace Hourglass.Input
{
    public class KeyInputController : IInputController
    {
        public Keys TargetKey { get; private set; }
        public bool Activated { get; private set; }
        public float Pressure => Activated ? 1.0F : 0.0F;

        public KeyInputController(Keys targetKey)
        {
            TargetKey = targetKey;
        }

        public void Poll(InputManager inputManager)
        {
            Activated = inputManager.CurrentKeyboardState.IsKeyDown(TargetKey);
        }
    }
}
