using DivaHook.Emulator.Config;
using DivaHook.Emulator.Input;
using System;
using System.Runtime.InteropServices;
using static DivaHook.Injection.InjectionHelper;

namespace DivaHook.Emulator.Components
{
    public class DebugComponent : IEmulatorComponent
    {
        private const long CHANGE_MODE_ADDRESS = 0x00000001401953D0L;
        private const long CHANGE_SUB_MODE_ADDRESS = 0x0000000140195260L;
        // private const long AET_FRAME_DURATION_ADDRESS = 0x00000001409A0A58L;

        public KeyConfig KeyConfig { get; private set; }
        public MemoryManipulator MemoryManipulator { get; private set; }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate void ChangeGameStateDelegate(GameState gs);
        private static readonly ChangeGameStateDelegate ChangeGameState = GetDelegateForFunctionPointer<ChangeGameStateDelegate>(CHANGE_MODE_ADDRESS);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate void ChangeLogGameStateDelegate(GameState gs, SubGameState sg);
        private static readonly ChangeLogGameStateDelegate ChangeLogGameState = GetDelegateForFunctionPointer<ChangeLogGameStateDelegate>(CHANGE_SUB_MODE_ADDRESS);

        public DebugComponent(MemoryManipulator memoryManipulator, KeyConfig keyConfig)
        {
            MemoryManipulator = memoryManipulator;
            KeyConfig = keyConfig;
        }

        public void InitializeDivaMemory()
        {
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            // if (KeyConfig.ToggleGsAdvertise.IsAnyTapped()) ChangeGameState(GameState.GS_ADVERTISE);
            if (KeyConfig.ToggleGsGameplay.IsAnyTapped()) ChangeGameState(GameState.GS_GAME);
            if (KeyConfig.ToggleGsDataTest.IsAnyTapped()) ChangeGameState(GameState.GS_DATA_TEST);
            if (KeyConfig.ToggleGsAppError.IsAnyTapped()) ChangeGameState(GameState.GS_APP_ERROR);

            // if (InputHelper.IsDown(Keys.LeftShift) && InputHelper.IsDown(Keys.Tab))
            //     MemoryManipulator.WriteSingle(AET_FRAME_DURATION_ADDRESS, 1.0f / (60 / 4.0f));
        }
    }
}
