using DivaHook.Emulator.Config;
using System;
using System.Runtime.InteropServices;

namespace DivaHook.Emulator.Components
{
    public class ScaleComponent : IEmulatorComponent
    {
        private const long FB1_WIDTH_ADDRESS = 0x00000001411AD5F8;
        private const long FB1_HEIGHT_ADDRESS = 0x00000001411AD5FC;

        private const long UI_WIDTH_ADDRESS = 0x000000014CC621E4;
        private const long UI_HEIGHT_ADDRESS = 0x000000014CC621E8;

        private const long FB_ASPECT_RATIO = 0x0000000140FBC2E8;
        private const long UI_ASPECT_RATIO = 0x000000014CC621D0;

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        public KeyConfig KeyConfig { get; private set; }

        public MemoryManipulator MemoryManipulator { get; private set; }

        public ScaleComponent(MemoryManipulator memoryManipulator)
        {
            MemoryManipulator = memoryManipulator;
        }

        public void InitializeDivaMemory()
        {
            MemoryManipulator.WritePatch(0x00000001404ACD24, new byte[] { 0x44, 0x8B, 0x0D, 0xD1, 0x08, 0xD0, 0x00 });
            MemoryManipulator.WritePatch(0x00000001404ACD2B, new byte[] { 0x44, 0x8B, 0x05, 0xC6, 0x08, 0xD0, 0x00 });
            MemoryManipulator.WritePatchNop(0x00000001405030A0, 6);
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            GetClientRect(MemoryManipulator.AttachedProcess.MainWindowHandle, out RECT hWindow);

            MemoryManipulator.WriteSingle(UI_ASPECT_RATIO, (float)(hWindow.Right - hWindow.Left) / (float)(hWindow.Bottom - hWindow.Top));
            MemoryManipulator.WriteDouble(FB_ASPECT_RATIO, (double)(hWindow.Right - hWindow.Left) / (double)(hWindow.Bottom - hWindow.Top));
            MemoryManipulator.WriteSingle(UI_WIDTH_ADDRESS, hWindow.Right - hWindow.Left);
            MemoryManipulator.WriteSingle(UI_HEIGHT_ADDRESS, hWindow.Bottom - hWindow.Top);
            MemoryManipulator.WriteInt32(FB1_WIDTH_ADDRESS, hWindow.Right - hWindow.Left);
            MemoryManipulator.WriteInt32(FB1_HEIGHT_ADDRESS, hWindow.Bottom - hWindow.Top);

            MemoryManipulator.WriteInt32(0x00000001411AD608, 0);
            MemoryManipulator.WriteInt32(0x0000000140EDA8E4, MemoryManipulator.ReadInt32(0x0000000140EDA8BC));
            MemoryManipulator.WriteInt32(0x0000000140EDA8E8, MemoryManipulator.ReadInt32(0x0000000140EDA8C0));

            MemoryManipulator.WriteSingle(0x00000001411A1900, 0);
            MemoryManipulator.WriteSingle(0x00000001411A1904, (float)MemoryManipulator.ReadInt32(0x0000000140EDA8BC));
            MemoryManipulator.WriteSingle(0x00000001411A1908, (float)MemoryManipulator.ReadInt32(0x0000000140EDA8C0));
        }
    }
}
