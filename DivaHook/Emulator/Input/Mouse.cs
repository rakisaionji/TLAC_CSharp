using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace DivaHook.Emulator.Input
{
    public class Mouse
    {
        private const string USER32_DLL = "user32.dll";

        [DllImport(USER32_DLL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport(USER32_DLL)]
        private static extern bool SetCursorPos(int X, int Y);

        public static MouseState GetMouseState()
        {
            return new MouseState()
            {
                Position = GetMousePosition(),
            };
        }

        public static POINT GetMousePosition()
        {
            GetCursorPos(out POINT mousePosition);
            return mousePosition;
        }

        public static void SetMousePosition(Point position)
        {
            SetCursorPos(position.X, position.Y);
        }

        public static void SetMousePosition(Vector2 position)
        {
            SetCursorPos((int)position.X, (int)position.Y);
        }
    }
}
