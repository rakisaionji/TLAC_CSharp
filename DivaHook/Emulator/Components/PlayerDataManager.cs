using DivaHook.Emulator.Config;
using System;
using System.Text;

namespace DivaHook.Emulator.Components
{
    public class PlayerDataManager : IEmulatorComponent
    {
        ////////////////////////////////////////////////////////////////////////////////
        // ===== PATCH.TXT DESCRIPTIONS =====
        // // Return early before resetting to the default PlayerData so we don't need to keep updating the PlayerData struct
        // 0x00000001404A7370 : 0x5 : 48 89 5C 24 08 : C3 90 90 90 90 
        // // Allow player to select the module and extra items (by vladkorotnev)
        // 0x00000001405869AD : 0x2 : 32 C0 : B0 01 
        // // Fix annoying behavior of closing after changing module or item (by vladkorotnev)
        // 0x0000000140583B45 : 0x1 : 84 : 85 
        // 0x0000000140583C8C : 0x1 : 84 : 85 
        ////////////////////////////////////////////////////////////////////////////////

        private const long PLAYER_DATA_ADDRESS = 0x00000001411A8850L;
        private const long PLAYER_NAME_ADDRESS = PLAYER_DATA_ADDRESS + 0x0E0L;
        private const long PLAYER_LEVEL_ADDRESS = PLAYER_DATA_ADDRESS + 0x120L;
        private const long PLAYER_SKIN_EQUIP_ADDRESS = PLAYER_DATA_ADDRESS + 0x548L;
        private const long PLAYER_PLATE_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x124L;
        private const long PLAYER_PLATE_EFF_ADDRESS = PLAYER_DATA_ADDRESS + 0x128L;
        private const long PLAYER_VP_ADDRESS = PLAYER_DATA_ADDRESS + 0x12CL;
        private const long PLAYER_HP_VOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x130L;
        private const long PLAYER_ACT_TOGGLE_ADDRESS = PLAYER_DATA_ADDRESS + 0x134L;
        private const long PLAYER_ACT_VOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x138L;
        private const long PLAYER_ACT_SLVOL_ADDRESS = PLAYER_DATA_ADDRESS + 0x13CL;
        private const long PLAYER_PV_SORT_KIND_ADDRESS = PLAYER_DATA_ADDRESS + 0x584L;
        private const long PLAYER_PWD_STAT_ADDRESS = PLAYER_DATA_ADDRESS + 0x668L;
        private const long PLAYER_RANK_DISP_ADDRESS = PLAYER_DATA_ADDRESS + 0xE34L; // interim_ranking_disp_flag
        private const long PLAYER_OPTION_DISP_ADDRESS = PLAYER_DATA_ADDRESS + 0xE35L; // rhythm_game_opt_disp_flag

        private const long PLAYER_PLAY_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x0D0L; // play_data_id
        private const long PLAYER_ACCEPT_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x0D4L; // accept_index
        private const long PLAYER_START_ID_ADDRESS = PLAYER_DATA_ADDRESS + 0x0D8L; // start_index

        private const long SET_DEFAULT_PLAYER_DATA_ADDRESS = 0x00000001404A7370L;
        private const long MODSELECTOR_CHECK_FUNCTION_ERRRET_ADDRESS = 0x00000001405869ADL;
        private const long MODSELECTOR_CLOSE_AFTER_MODULE = 0x0000000140583B45L;
        private const long MODSELECTOR_CLOSE_AFTER_CUSTOMIZE = 0x0000000140583C8CL;

        private const long MODULE_TABLE_START = 0x00000001411A8990L;
        private const long MODULE_TABLE_END = 0x00000001411A8A0FL;
        private const long ITEM_TABLE_START = 0x00000001411A8B08L;
        private const long ITEM_TABLE_END = 0x00000001411A8B87L;

        private const long CURRENT_SUB_STATE = 0x0000000140EDA82CL;

        private const string DefaultName = "ＮＯ－ＮＡＭＥ";
        private byte[] PlayerNameValue;
        private Int32 PlayerNameAddress;
        private Int32 Level = 1;
        private Int32 PlateId = 0;
        private Int32 PlateEff = -1;
        private Int32 VocaloidPoint = 0;
        private Int32 SkinEquip = 0;
        private Byte ActToggle = 1;
        private Int32 ActVol = 100;
        private Int32 ActSlideVol = 100;
        private Int32 HpVol = 100;
        private Int32 PasswordStatus = -1;
        private Int32 PvSortKind = 2;
        private Int32 PlayIndex = 1;

        private int step = 0;

        public KeyConfig KeyConfig { get; private set; }
        public PlayerConfig PlayerConfig { get; private set; }

        public MemoryManipulator MemoryManipulator { get; private set; }

        public PlayerDataManager(MemoryManipulator memoryManipulator, PlayerConfig playerConfig)
        {
            MemoryManipulator = memoryManipulator;
            PlayerConfig = playerConfig;
        }

        // private void SetIfNotEqual(ref int target, int value, int comparison)
        // {
        //    if (value != comparison)
        //        target = value;
        // }

        private void SetPlayerConfig(ref string field1, string field2)
        {
            if (field1.StartsWith("*"))
                field1 = field1.Substring(1);
            else
                field1 = field2;
        }

        private void InjectPatches()
        {
            // Prevent the PlayerData from being reset so we don't need to keep updating the PlayerData struct
            MemoryManipulator.WritePatch(SET_DEFAULT_PLAYER_DATA_ADDRESS, new byte[] { 0xC3 }); // ret
            // Allow player to select the module and extra item (by vladkorotnev)
            MemoryManipulator.WritePatch(MODSELECTOR_CHECK_FUNCTION_ERRRET_ADDRESS, new byte[] { 0xB0, 0x01 }); // xor al,al -> ld al,1
            // Fix annoying behavior of closing after changing module or item  (by vladkorotnev)
            MemoryManipulator.WritePatch(MODSELECTOR_CLOSE_AFTER_MODULE, new byte[] { 0x85 }); // je --> jne
            MemoryManipulator.WritePatch(MODSELECTOR_CLOSE_AFTER_CUSTOMIZE, new byte[] { 0x85 }); // je --> jne
            // Display clear borders on the progress bar (by vladkorotnev)
            MemoryManipulator.WriteByte(PLAYER_DATA_ADDRESS + 0xD94, 0x3);
            // Enable module selection without card (by lybxlpsv) [ WIP / NG ]
            // MemoryManipulator.WritePatch(0x00000001405C5133, new byte[] { 0x74 });
            // MemoryManipulator.WritePatch(0x00000001405BC8E7, new byte[] { 0x74 });
        }

        public void InitializeDivaMemory()
        {
            InjectPatches();
            var AppConfig = Properties.Settings.Default;
            if (AppConfig.PlayerName.Equals(PlayerConfig.PlayerName))
            {
                SetPlayerConfig(ref PlayerConfig.VocaloidPoint, AppConfig.VocaloidPoint);
                SetPlayerConfig(ref PlayerConfig.ActToggle, AppConfig.ActToggle);
                SetPlayerConfig(ref PlayerConfig.ActVol, AppConfig.ActVol);
                SetPlayerConfig(ref PlayerConfig.ActSlideVol, AppConfig.ActSlideVol);
                SetPlayerConfig(ref PlayerConfig.PvSortKind, AppConfig.PvSortKind);
                SetPlayerConfig(ref PlayerConfig.PlayIndex, AppConfig.PlayIndex);
            }
            else
            {
                AppConfig.PlayerName = PlayerConfig.PlayerName;
                AppConfig.VocaloidPoint = PlayerConfig.VocaloidPoint;
                AppConfig.ActToggle = PlayerConfig.ActToggle;
                AppConfig.ActVol = PlayerConfig.ActVol;
                AppConfig.ActSlideVol = PlayerConfig.ActSlideVol;
                AppConfig.HpVol = PlayerConfig.HpVol;
                AppConfig.PvSortKind = PlayerConfig.PvSortKind;
                AppConfig.PlayIndex = PlayerConfig.PlayIndex;
            }
            PlayerNameValue = new byte[21];
            var b_name = Encoding.UTF8.GetBytes(PlayerConfig.PlayerName);
            Buffer.BlockCopy(b_name, 0, PlayerNameValue, 0, b_name.Length);
            PlayerNameAddress = MemoryManipulator.ReadInt32(PLAYER_NAME_ADDRESS);
            ReadPlayerData();
            if (Level < 1) Level = 1;
            if (ActVol < 0 || ActVol > 100) ActVol = 100;
            if (HpVol < 0 || HpVol > 100) HpVol = 100;
            // use_card = 1 // Required to allow for module selection
            MemoryManipulator.WriteInt32(PLAYER_DATA_ADDRESS, 1);
            // Allow player to select the module and extra items (by vladkorotnev)
            for (long i = MODULE_TABLE_START; i <= MODULE_TABLE_END; i++)
            {
                MemoryManipulator.WriteByte(i, 0xFF);
            }
            for (long i = ITEM_TABLE_START; i <= ITEM_TABLE_END; i++)
            {
                MemoryManipulator.WriteByte(i, 0xFF);
            }
            // Display interim rank and rhythm options (despite it is not yet fully functional)
            MemoryManipulator.WriteByte(PLAYER_RANK_DISP_ADDRESS, 1);
            MemoryManipulator.WriteByte(PLAYER_OPTION_DISP_ADDRESS, 1);
            // First write of play start id, only once per starup
            MemoryManipulator.WriteInt32(PLAYER_START_ID_ADDRESS, PlayIndex);
            WritePlayerData();
        }

        private void ReadPlayerData()
        {
            Int32.TryParse(PlayerConfig.Level, out Level);
            Int32.TryParse(PlayerConfig.SkinEquip, out SkinEquip);
            Int32.TryParse(PlayerConfig.PlateId, out PlateId);
            Int32.TryParse(PlayerConfig.PlateEff, out PlateEff);
            Int32.TryParse(PlayerConfig.VocaloidPoint, out VocaloidPoint);
            Byte.TryParse(PlayerConfig.ActToggle, out ActToggle);
            Int32.TryParse(PlayerConfig.ActVol, out ActVol);
            Int32.TryParse(PlayerConfig.ActSlideVol, out ActSlideVol);
            Int32.TryParse(PlayerConfig.HpVol, out HpVol);
            Int32.TryParse(PlayerConfig.PasswordStatus, out PasswordStatus);
            Int32.TryParse(PlayerConfig.PvSortKind, out PvSortKind);
            Int32.TryParse(PlayerConfig.PlayIndex, out PlayIndex);
        }

        private void WritePlayerData()
        {
            var b = MemoryManipulator.Read(PlayerNameAddress, 21);
            MemoryManipulator.Write(PlayerNameAddress, PlayerNameValue);
            MemoryManipulator.WriteInt32(PLAYER_SKIN_EQUIP_ADDRESS, SkinEquip);
            MemoryManipulator.WriteInt32(PLAYER_LEVEL_ADDRESS, Level);
            MemoryManipulator.WriteInt32(PLAYER_PLATE_ID_ADDRESS, PlateId);
            MemoryManipulator.WriteInt32(PLAYER_PLATE_EFF_ADDRESS, PlateEff);
            MemoryManipulator.WriteInt32(PLAYER_VP_ADDRESS, VocaloidPoint);
            MemoryManipulator.WriteByte(PLAYER_ACT_TOGGLE_ADDRESS, ActToggle);
            MemoryManipulator.WriteInt32(PLAYER_ACT_VOL_ADDRESS, ActVol);
            MemoryManipulator.WriteInt32(PLAYER_ACT_SLVOL_ADDRESS, ActSlideVol);
            MemoryManipulator.WriteInt32(PLAYER_HP_VOL_ADDRESS, HpVol);
            MemoryManipulator.WriteInt32(PLAYER_PWD_STAT_ADDRESS, PasswordStatus);
            MemoryManipulator.WriteInt32(PLAYER_PV_SORT_KIND_ADDRESS, PvSortKind);
            MemoryManipulator.WriteInt32(PLAYER_PLAY_ID_ADDRESS, PlayIndex);
            MemoryManipulator.WriteInt32(PLAYER_ACCEPT_ID_ADDRESS, PlayIndex);
        }

        private void SavePlayerData()
        {
            // MemoryManipulator.WriteInt32(GetPlayerNameFAddress(), 0x10);
            VocaloidPoint = MemoryManipulator.ReadInt32(PLAYER_VP_ADDRESS);
            ActToggle = MemoryManipulator.ReadByte(PLAYER_ACT_TOGGLE_ADDRESS);
            ActVol = MemoryManipulator.ReadInt32(PLAYER_ACT_VOL_ADDRESS); ;
            ActSlideVol = MemoryManipulator.ReadInt32(PLAYER_ACT_SLVOL_ADDRESS);
            HpVol = MemoryManipulator.ReadInt32(PLAYER_HP_VOL_ADDRESS);
            PvSortKind = MemoryManipulator.ReadInt32(PLAYER_PV_SORT_KIND_ADDRESS);
            PlayIndex = MemoryManipulator.ReadInt32(PLAYER_PLAY_ID_ADDRESS);
            var sett = Properties.Settings.Default;
            sett.VocaloidPoint = VocaloidPoint.ToString();
            sett.ActToggle = ActToggle.ToString();
            sett.ActVol = ActVol.ToString();
            sett.ActSlideVol = ActSlideVol.ToString();
            sett.HpVol = HpVol.ToString();
            sett.PvSortKind = PvSortKind.ToString();
            sett.PlayIndex = PlayIndex.ToString();
            sett.Save();
            // First write of play start id, only once per session
            MemoryManipulator.WriteInt32(PLAYER_START_ID_ADDRESS, PlayIndex);
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            var currentSubState = (SubGameState)MemoryManipulator.ReadInt32(CURRENT_SUB_STATE);
            // 4       .. save all when flag = 3 / flag = 0
            // 12 + 14 .. refresh->set flag = 1
            // 13      .. if flag = 1 , increase game id; set flag = 2
            // 15      .. set flag = 3
            switch (currentSubState)
            {
                case SubGameState.SUB_LOGO: // 4
                    if (step == 2) PlayIndex--;
                    if (step == 3) SavePlayerData();
                    step = 0;
                    break;
                case SubGameState.SUB_SELECTOR: // 12
                case SubGameState.SUB_GAME_SEL: // 14
                    if (step == 2) PlayIndex--;
                    if (step == 3) { SavePlayerData(); step = 0; }
                    if (step == 0) WritePlayerData();
                    step = 1;
                    break;
                case SubGameState.SUB_GAME_MAIN: // 13
                    if (step == 1)
                    {
                        PlayIndex++;
                        MemoryManipulator.WriteInt32(PLAYER_PLAY_ID_ADDRESS, PlayIndex);
                        MemoryManipulator.WriteInt32(PLAYER_ACCEPT_ID_ADDRESS, PlayIndex);
                    }
                    step = 2;
                    break;
                case SubGameState.SUB_STAGE_RESULT: // 15
                    step = 3;
                    break;
                default:
                    break;
            }
        }
    }
}
