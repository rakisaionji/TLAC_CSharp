using DivaHook.Emulator.Config;
using System;
using System.Runtime.InteropServices;

namespace DivaHook.Emulator.Components
{
    public class DebugComponent : IEmulatorComponent
    {
        private const long CHANGE_MODE_ADDRESS = 0x00000001401953D0L;
        // private const long CHANGE_SUB_MODE_ADDRESS = 0x0000000140195260L;
        private const long AET_FRAME_DURATION_ADDRESS = 0x00000001409A0A58L;

        public KeyConfig KeyConfig { get; private set; }
        public MemoryManipulator MemoryManipulator { get; private set; }

        // [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate void ChangeGameState(GameState gs);
        private static readonly ChangeGameState changeGameState = Marshal.GetDelegateForFunctionPointer<ChangeGameState>((IntPtr)CHANGE_MODE_ADDRESS);

        // [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate void ChangeSubState(GameState gs, SubGameState sg);
        // private static readonly ChangeSubState changeSubState = Marshal.GetDelegateForFunctionPointer<ChangeSubState>((IntPtr)CHANGE_SUB_MODE_ADDRESS);

        public DebugComponent(MemoryManipulator memoryManipulator, KeyConfig keyConfig)
        {
            MemoryManipulator = memoryManipulator;
            KeyConfig = keyConfig;
        }
        /*
        private void InjectPatches()
        {
            // Prevent the DATA_TEST game state from exiting on the first frame
            MemoryManipulator.WritePatch(0x0000000140284B01, new byte[] { 0x00 });
            // Enable dw_gui sprite draw calls
            MemoryManipulator.WritePatch(0x0000000140192601, new byte[] { 0x00 });
            // Update the dw_gui display
            MemoryManipulator.WritePatch(0x0000000140302600, new byte[] { 0xB0, 0x01 });
            // Draw the dw_gui display
            MemoryManipulator.WritePatch(0x0000000140302610, new byte[] { 0xB0, 0x01 });
            // Enable the dw_gui widgets
            MemoryManipulator.WritePatch(0x0000000140192D00, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });
        }
        */
        public void InitializeDivaMemory()
        {
            // InjectPatches();
            // In case the FrameRateManager isn't enabled
            MemoryManipulator.VirtualProtect((IntPtr)AET_FRAME_DURATION_ADDRESS, sizeof(float));
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            // if (KeyConfig.ToggleGsAdvertise.IsAnyTapped()) ChangeGameState(GameState.GS_ADVERTISE);
            if (KeyConfig.ToggleGsGameplay.IsAnyTapped()) changeGameState(GameState.GS_GAME);
            if (KeyConfig.ToggleGsDataTest.IsAnyTapped()) changeGameState(GameState.GS_DATA_TEST);
            if (KeyConfig.ToggleGsAppError.IsAnyTapped()) changeGameState(GameState.GS_APP_ERROR);
        }
    }
}
