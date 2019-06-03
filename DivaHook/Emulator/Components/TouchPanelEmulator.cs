using System;
using DivaHook.Emulator.Config;
using DivaHook.Emulator.Input;
using DivaHook.Emulator.Input.Ds4;

namespace DivaHook.Emulator.Components
{
    public class TouchPanelEmulator : IEmulatorComponent
    {
        private const long TOUCH_PANEL_TASK_OBJECT_ADDRESS = 0x000000014CC9EC30L;

        public KeyConfig KeyConfig { get; private set; }

        public MemoryManipulator MemoryManipulator { get; private set; }

        private bool checkTouchPanelState = true;

        public TouchPanelEmulator(MemoryManipulator memoryManipulator, KeyConfig keyConfig)
        {
            MemoryManipulator = memoryManipulator;
            KeyConfig = keyConfig;
        }

        public void InitializeDivaMemory()
        {
            MemoryManipulator.WriteInt32(GetConnectionStateAddress(), 1);
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            if (checkTouchPanelState && MemoryManipulator.ReadInt32(GetConnectionStateAddress()) != 1)
            {
                MemoryManipulator.WriteInt32(GetConnectionStateAddress(), 1);
                checkTouchPanelState = false;
            }

            if (Ds4Device.Instance.IsConnected && Ds4Device.Instance.IsTouched)
            {
                MemoryManipulator.WriteInt32(GetAdvTouchIsTappedAddress(), 1);
            }

            if (InputHelper.Instance.HasMouseMoved())
            {
                var mousePos = InputHelper.Instance.CurrentMouseState.RelativePosition;
                var relPos = MemoryManipulator.GetMouseRelativePos(mousePos);

                MemoryManipulator.WriteSingle(GetTouchPanelXPositionAddress(), relPos.X);
                MemoryManipulator.WriteSingle(GetTouchPanelYPositionAddress(), relPos.Y);
            }

            bool tapped = InputHelper.IsDown(Keys.MouseLeft);
            bool released = InputHelper.IsReleased(Keys.MouseLeft);

            int contactType = tapped ? 2 : released ? 1 : 0;
            MemoryManipulator.WriteInt32(GetTouchPanelContactTypeAddress(), contactType);

            float pressure = contactType != 0 ? 1 : 0;
            MemoryManipulator.WriteSingle(GetTouchPanelPressureAddress(), pressure);
        }

        private long GetConnectionStateAddress()
        {
            return TOUCH_PANEL_TASK_OBJECT_ADDRESS + 0x78L;
        }

        private long GetTouchPanelContactTypeAddress()
        {
            return TOUCH_PANEL_TASK_OBJECT_ADDRESS + 0xA0L;
        }

        private long GetTouchPanelXPositionAddress()
        {
            return TOUCH_PANEL_TASK_OBJECT_ADDRESS + 0x94L;
        }

        private long GetTouchPanelYPositionAddress()
        {
            return TOUCH_PANEL_TASK_OBJECT_ADDRESS + 0x98L;
        }

        private long GetTouchPanelPressureAddress()
        {
            return TOUCH_PANEL_TASK_OBJECT_ADDRESS + 0x9CL;
        }

        private long GetAdvTouchIsTappedAddress()
        {
            return 0x140EC52D0L + 0x70L;
        }
    }
}
