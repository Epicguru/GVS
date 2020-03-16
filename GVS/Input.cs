using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GVS
{
    /// <summary>
    /// A static class that collects all of XNA's input utilities into one place.
    /// Mouse, Keyboard and (possibly in the future) Gamepad inputs are all available here.
    /// Input is polled and updated on a per-frame basis, so input state will not change in the middle of
    /// a frame execution. Has various extra features, such as disabling input when necessary, and detecting when the
    /// mouse exits the game window.
    /// </summary>
    public static class Input
    {
        public static bool Enabled { get; set; } = true;

        public static Point MousePos { get; private set; }
        public static Vector2 MouseWorldPos { get; private set; }
        public static bool MouseInWindow { get; private set; }
        public static int MouseScroll { get; private set; }
        public static int MouseScrollDelta { get; private set; }

        private static KeyboardState currentKeyState;
        private static KeyboardState lastKeyState;

        private static MouseState currentMouseState;
        private static MouseState lastMouseState;

        public static void StartFrame()
        {
            lastKeyState = currentKeyState;
            currentKeyState = Keyboard.GetState();

            lastMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();

            MousePos = currentMouseState.Position;
            MouseWorldPos = Main.Camera.ScreenToWorldPosition(MousePos.ToVector2());
            MouseInWindow = Screen.Contains(MousePos.X, MousePos.Y);

            MouseScrollDelta = currentMouseState.ScrollWheelValue - MouseScroll;
            MouseScroll = currentMouseState.ScrollWheelValue;
        }

        public static bool KeyPressed(Keys key)
        {
            return Enabled && Pressed(currentKeyState[key]);
        }

        public static bool KeyDown(Keys key)
        {
            return Enabled && Down(currentKeyState[key], lastKeyState[key]);
        }

        public static bool KeyUp(Keys key)
        {
            return Enabled && Up(currentKeyState[key], lastKeyState[key]);
        }

        private static bool MousePressed(ButtonState s)
        {
            return Enabled && s == ButtonState.Pressed;
        }

        private static bool MouseDown(ButtonState current, ButtonState last)
        {
            return Enabled && current == ButtonState.Pressed && last == ButtonState.Released;
        }

        private static bool MouseUp(ButtonState current, ButtonState last)
        {
            return Enabled && current == ButtonState.Released && last == ButtonState.Pressed;
        }

        public static bool RightMousePressed()
        {
            return MousePressed(currentMouseState.RightButton);
        }

        public static bool LeftMousePressed()
        {
            return MousePressed(currentMouseState.LeftButton);
        }

        public static bool MiddleMousePressed()
        {
            return MousePressed(currentMouseState.MiddleButton);
        }

        public static bool RightMouseDown()
        {
            return MouseDown(currentMouseState.RightButton, lastMouseState.RightButton);
        }

        public static bool LeftMouseDown()
        {
            return MouseDown(currentMouseState.LeftButton, lastMouseState.LeftButton);
        }

        public static bool MiddleMouseDown()
        {
            return MouseDown(currentMouseState.MiddleButton, lastMouseState.MiddleButton);
        }

        public static bool RightMouseUp()
        {
            return MouseUp(currentMouseState.RightButton, lastMouseState.RightButton);
        }

        public static bool LeftMouseUp()
        {
            return MouseUp(currentMouseState.LeftButton, lastMouseState.LeftButton);
        }

        public static bool MiddleMouseUp()
        {
            return MouseUp(currentMouseState.MiddleButton, lastMouseState.MiddleButton);
        }

        private static bool Pressed(KeyState s)
        {
            return s == KeyState.Down;
        }

        private static bool Down(KeyState current, KeyState last)
        {
            return current == KeyState.Down && last == KeyState.Up;
        }

        private static bool Up(KeyState current, KeyState last)
        {
            return current == KeyState.Up && last == KeyState.Down;
        }

        public static void EndFrame()
        {

        }
    }
}
