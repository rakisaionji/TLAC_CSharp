using DivaHook.Emulator.Config;
using System;
using System.Runtime.InteropServices;

namespace DivaHook.Emulator.Components
{
    public class ScaleComponent : IEmulatorComponent
    {
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

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
            {
                uint oldProtect, bck;
                VirtualProtect((IntPtr)0x00000001404ACD24, 7, PAGE_EXECUTE_READWRITE, out oldProtect);
                MemoryManipulator.WriteByte((0x00000001404ACD24 + 0), 0x44);
                MemoryManipulator.WriteByte((0x00000001404ACD24 + 1), 0x8B);
                MemoryManipulator.WriteByte((0x00000001404ACD24 + 2), 0x0D);
                MemoryManipulator.WriteByte((0x00000001404ACD24 + 3), 0xD1);
                MemoryManipulator.WriteByte((0x00000001404ACD24 + 4), 0x08);
                MemoryManipulator.WriteByte((0x00000001404ACD24 + 5), 0xD0);
                MemoryManipulator.WriteByte((0x00000001404ACD24 + 6), 0x00);
                VirtualProtect((IntPtr)0x00000001404ACD24, 7, oldProtect, out bck);
            }
            {
                uint oldProtect, bck;
                VirtualProtect((IntPtr)0x00000001404ACD2B, 7, PAGE_EXECUTE_READWRITE, out oldProtect);
                MemoryManipulator.WriteByte((0x00000001404ACD2B + 0), 0x44);
                MemoryManipulator.WriteByte((0x00000001404ACD2B + 1), 0x8B);
                MemoryManipulator.WriteByte((0x00000001404ACD2B + 2), 0x05);
                MemoryManipulator.WriteByte((0x00000001404ACD2B + 3), 0xC6);
                MemoryManipulator.WriteByte((0x00000001404ACD2B + 4), 0x08);
                MemoryManipulator.WriteByte((0x00000001404ACD2B + 5), 0xD0);
                MemoryManipulator.WriteByte((0x00000001404ACD2B + 6), 0x00);
                VirtualProtect((IntPtr)0x00000001404ACD2B, 7, oldProtect, out bck);
            }
            {
                uint oldProtect, bck;
                VirtualProtect((IntPtr)0x00000001405030A0, 6, PAGE_EXECUTE_READWRITE, out oldProtect);
                MemoryManipulator.WriteByte((0x00000001405030A0 + 0), 0x90);
                MemoryManipulator.WriteByte((0x00000001405030A0 + 1), 0x90);
                MemoryManipulator.WriteByte((0x00000001405030A0 + 2), 0x90);
                MemoryManipulator.WriteByte((0x00000001405030A0 + 3), 0x90);
                MemoryManipulator.WriteByte((0x00000001405030A0 + 4), 0x90);
                MemoryManipulator.WriteByte((0x00000001405030A0 + 5), 0x90);
                VirtualProtect((IntPtr)0x00000001404ACD2B, 6, oldProtect, out bck);
            }
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            RECT hWindow;
            GetClientRect(MemoryManipulator.AttachedProcess.MainWindowHandle, out hWindow);

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

        private int GetPointerAddress(long addr)
        {
            return MemoryManipulator.ReadInt32(addr);
        }
    }
}
