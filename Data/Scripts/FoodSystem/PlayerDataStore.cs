using System.Collections.Generic;
using VRage.Game.ModAPI;
using Sandbox.ModAPI;
using System.IO;
using System;

namespace Rek.FoodSystem
{
    public class PlayerDataStore
    {
        private Dictionary<ulong, PlayerData> mPlayerData;
        private const string mFilename = "playerData.xml";
    
        public PlayerDataStore()
        {
            mPlayerData = new Dictionary<ulong, PlayerData>();
        }
        
        public PlayerData get(IMyPlayer player) {
            PlayerData result;
            if(!mPlayerData.TryGetValue(player.SteamUserId , out result)) {
                result = new PlayerData(player.SteamUserId);
                mPlayerData.Add(player.SteamUserId , result);
            }
            return result;
        }

        public void Save()
        {
            try
            {
                PlayerData[] tmp = new PlayerData[mPlayerData.Count];
                mPlayerData.Values.CopyTo(tmp, 0);

                TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(mFilename, typeof(PlayerDataStore));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML<PlayerData[]>(tmp));
                writer.Flush();
                writer.Close();
            } catch (Exception e)
            {
                //MyAPIGateway.Utilities.ShowMessage("ERROR", "Error: " + e.Message + "\n" + e.StackTrace);
            }
        }

        public void Load()
        {
            try {
                //MyAPIGateway.Utilities.ShowMessage("DEBUG", "Loading file");
            
                TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(mFilename, typeof(PlayerDataStore));
                string xmlText = reader.ReadToEnd();
                reader.Close();

                PlayerData[] tmp = MyAPIGateway.Utilities.SerializeFromXML<PlayerData[]>(xmlText);

                foreach (PlayerData x in tmp)
                {
                    //MyAPIGateway.Utilities.ShowMessage("DEBUG", "found player");
                    x.loaded = true;
                    mPlayerData.Add(x.steamid, x);
                }
            } catch(Exception e) {
                MyAPIGateway.Utilities.ShowMessage("ERROR", "Error: " + e.Message + "\n" + e.StackTrace);
            }
        }

        
        public void clear() {
            mPlayerData.Clear();
        }
    }
}
