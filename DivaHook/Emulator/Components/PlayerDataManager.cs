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
        private const long MODULE_TABLE_START = 0x00000001411A8990L;
        private const long MODULE_TABLE_END = 0x00000001411A8A0FL;
        private const long ITEM_TABLE_START = 0x00000001411A8B08L;
        private const long ITEM_TABLE_END = 0x00000001411A8B87L;

        private string DefaultName = "ＮＯ－ＮＡＭＥ";
        private byte[] PlayerNameValue;
        private Int32 PlayerNameAddress;
        public Int32 Level = 1;
        public Int32 PlateId = 0;
        public Int32 PlateEff = -1;
        public Int32 VocaloidPoint = 0;
        public Int32 SkinEquip = 0;
        public Byte ActToggle = 1;
        public Int32 ActVol = 100;
        public Int32 ActSlideVol = 100;
        public Int32 HpVol = 100;
        public Int32 PasswordStatus = -1;

        private bool checkPlayerDataState = true;

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

        public void InitializeDivaMemory()
        {
            var AppConfig = Properties.Settings.Default;
            if (AppConfig.PlayerName.Equals(PlayerConfig.PlayerName))
            {
                SetPlayerConfig(ref PlayerConfig.VocaloidPoint, AppConfig.VocaloidPoint);
                // SetPlayerConfig(ref PlayerConfig.ActToggle, AppConfig.ActToggle);
                // SetPlayerConfig(ref PlayerConfig.ActVol, AppConfig.ActVol);
                // SetPlayerConfig(ref PlayerConfig.ActSlideVol, AppConfig.ActSlideVol);
                // SetPlayerConfig(ref PlayerConfig.HpVol, AppConfig.HpVol);
            }
            else
            {
                AppConfig.PlayerName = PlayerConfig.PlayerName;
                // AppConfig.VocaloidPoint = PlayerConfig.VocaloidPoint;
                // AppConfig.ActToggle = PlayerConfig.ActToggle;
                // AppConfig.ActVol = PlayerConfig.ActVol;
                // AppConfig.ActSlideVol = PlayerConfig.ActSlideVol;
                // AppConfig.HpVol = PlayerConfig.HpVol;
            }
            PlayerNameValue = new byte[21];
            var b_name = Encoding.UTF8.GetBytes(PlayerConfig.PlayerName);
            Buffer.BlockCopy(b_name, 0, PlayerNameValue, 0, b_name.Length);
            PlayerNameAddress = MemoryManipulator.ReadInt32(GetPlayerNameAddress());
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
            if (Level < 1) Level = 1;
            if (ActVol < 0 || ActVol > 100) ActVol = 100;
            if (HpVol < 0 || HpVol > 100) HpVol = 100;
            for (long i = MODULE_TABLE_START; i <= MODULE_TABLE_END; i++)
            {
                MemoryManipulator.WriteByte(i, 0xFF);
            }
            for (long i = ITEM_TABLE_START; i <= ITEM_TABLE_END; i++)
            {
                MemoryManipulator.WriteByte(i, 0xFF);
            }
        }

        public void UpdateEmulatorTick(TimeSpan deltaTime)
        {
            // use_card = 1 // Required to allow for module selection
            MemoryManipulator.WriteInt32(PLAYER_DATA_ADDRESS, 1);
            var b = MemoryManipulator.Read(PlayerNameAddress, 21);
            var s = Encoding.UTF8.GetString(b).Trim();
            if (s.Equals(DefaultName) && !s.Equals(PlayerConfig.PlayerName))
                checkPlayerDataState = true;
            else
                checkPlayerDataState = false;
            // MemoryManipulator.WriteInt32(GetPlayerNameFAddress(), 0x10);
            MemoryManipulator.Write(PlayerNameAddress, PlayerNameValue);
            MemoryManipulator.WriteInt32(GetPlayerSkinEquipAddress(), SkinEquip);
            if (checkPlayerDataState)
            {
                MemoryManipulator.WriteInt32(GetPlayerLevelAddress(), Level);
                MemoryManipulator.WriteInt32(GetPlayerPlateIdAddress(), PlateId);
                MemoryManipulator.WriteInt32(GetPlayerPlateEffAddress(), PlateEff);
                MemoryManipulator.WriteInt32(GetPlayerVpAddress(), VocaloidPoint);
                MemoryManipulator.WriteByte(GetPlayerActToggleAddress(), ActToggle);
                MemoryManipulator.WriteInt32(GetPlayerActVolAddress(), ActVol);
                MemoryManipulator.WriteInt32(GetPlayerActSlideVolAddress(), ActSlideVol);
                MemoryManipulator.WriteInt32(GetPlayerHpVolAddress(), HpVol);
                MemoryManipulator.WriteInt32(GetPlayerPasswordStatusAddress(), PasswordStatus);
            }
            else
            {
                /*
                var save = false;
                var sett = Properties.Settings.Default;
                var vp = MemoryManipulator.ReadInt32(GetPlayerVpAddress());
                if (!vp.Equals(VocaloidPoint))
                {
                    VocaloidPoint = vp;
                    sett.VocaloidPoint = vp.ToString();
                    save = true;
                }
                var tg = MemoryManipulator.ReadByte(GetPlayerActToggleAddress());
                if (!vp.Equals(ActToggle))
                {
                    ActToggle = tg;
                    sett.ActToggle = tg.ToString();
                    save = true;
                }
                var av = MemoryManipulator.ReadInt32(GetPlayerActVolAddress());
                if (!vp.Equals(ActVol))
                {
                    ActVol = av;
                    sett.ActVol = av.ToString();
                    save = true;
                }
                var sv = MemoryManipulator.ReadInt32(GetPlayerActSlideVolAddress());
                if (!vp.Equals(ActSlideVol))
                {
                    ActSlideVol = sv;
                    sett.ActSlideVol = sv.ToString();
                    save = true;
                }
                var hp = MemoryManipulator.ReadInt32(GetPlayerHpVolAddress());
                if (!vp.Equals(HpVol))
                {
                    HpVol = hp;
                    sett.HpVol = hp.ToString();
                    save = true;
                }
                if (save) sett.Save();
                */
                var vp = MemoryManipulator.ReadInt32(GetPlayerVpAddress());
                if (!vp.Equals(VocaloidPoint))
                {
                    VocaloidPoint = vp;
                    Properties.Settings.Default.VocaloidPoint = vp.ToString();
                    Properties.Settings.Default.Save();
                }
            }
        }

        private long GetPlayerNameAddress()
        {
            return PLAYER_DATA_ADDRESS + 0x0E0L;
        }

        private long GetPlayerLevelAddress()
        {
            return PLAYER_DATA_ADDRESS + 0x120L;
        }

        private long GetPlayerSkinEquipAddress()
        {
            return PLAYER_DATA_ADDRESS + 0x548L;
        }

        private long GetPlayerPlateIdAddress()
        {
            return PLAYER_DATA_ADDRESS + 0x124L;
        }

        private long GetPlayerPlateEffAddress()
        {
            return PLAYER_DATA_ADDRESS + 0x128L;
        }

        private long GetPlayerVpAddress()
        {
            return PLAYER_DATA_ADDRESS + 0x12CL;
        }

        private long GetPlayerHpVolAddress()
        {
            return PLAYER_DATA_ADDRESS + 0x130L;
        }

        private long GetPlayerActToggleAddress()
        {
            return PLAYER_DATA_ADDRESS + 0x134L;
        }

        private long GetPlayerActVolAddress()
        {
            return PLAYER_DATA_ADDRESS + 0x138L;
        }

        private long GetPlayerActSlideVolAddress()
        {
            return PLAYER_DATA_ADDRESS + 0x13CL;
        }

        private long GetPlayerPasswordStatusAddress()
        {
            return PLAYER_DATA_ADDRESS + 0x668L;
        }
    }
}
