using DivaHook.Emulator.Config;
using System;
using System.Runtime.InteropServices;

namespace DivaHook.Emulator.Components
{
    public class FastLoader : IEmulatorComponent
    {
        private const long UPDATE_TASKS_ADDRESS = 0x000000014019B980L;
        private const long CURRENT_GAME_STATE_ADDRESS = 0x0000000140EDA810L;
        private const long DATA_INIT_STATE_ADDRESS = 0x0000000140EDA7A8L;
        private const long SYSTEM_WARNING_ELAPSED_ADDRESS = 0x00000001411A1430L;
        private const long SYSTEM_WARNING_ELAPSED_FRAME_ADDRESS = (SYSTEM_WARNING_ELAPSED_ADDRESS + 0x68L);
        // private const long AET_FRAME_DURATION_ADDRESS = 0x00000001409A0A58L;

        private GameState currentGameState;
        private GameState previousGameState;
        const int updatesPerFrame = 39;
        bool dataInitialized = false;

        public KeyConfig KeyConfig { get; private set; }
        public MemoryManipulator MemoryManipulator { get; private set; }

        // [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        private delegate void UpdateTask();
        // private static readonly UpdateTasksDelegate UpdateTasks = GetDelegateForFunctionPointer<UpdateTasksDelegate>(UPDATE_TASKS_ADDRESS);

        public FastLoader(MemoryManipulator memoryManipulator, KeyConfig keyConfig)
        {
            MemoryManipulator = memoryManipulator;
            KeyConfig = keyConfig;
        }

        public void InitializeDivaMemory()
        {
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            if (dataInitialized) return;

            previousGameState = currentGameState;
            currentGameState = (GameState)MemoryManipulator.ReadInt32(CURRENT_GAME_STATE_ADDRESS);

            if (currentGameState == GameState.GS_STARTUP)
            {
                var ptr = new IntPtr(UPDATE_TASKS_ADDRESS);
                var updateTask = Marshal.GetDelegateForFunctionPointer<UpdateTask>(ptr);

                // Speed up TaskSystemStartup
                for (int i = 0; i < updatesPerFrame; i++) updateTask();

                // Skip most of TaskDataInit
                MemoryManipulator.WriteInt32(DATA_INIT_STATE_ADDRESS, 3);
                // DATA_INITIALIZED = 3;

                // Skip the 600 frames of TaskWarning
                MemoryManipulator.WriteInt32(SYSTEM_WARNING_ELAPSED_FRAME_ADDRESS, 3939);
            }
            else if (previousGameState == GameState.GS_STARTUP)
            {
                dataInitialized = true;
            }
        }

    }
}
