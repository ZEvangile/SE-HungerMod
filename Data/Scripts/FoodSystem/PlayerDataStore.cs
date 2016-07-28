using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace Rek.FoodSystem
{
    public class PlayerDataStore
    {
        private Dictionary<ulong, PlayerData> mPlayerData;
    
        public PlayerDataStore()
        {
            mPlayerData = new Dictionary<ulong, PlayerData>();
        }
        
        public PlayerData get(IMyPlayer player) {
            PlayerData result;
            if(!mPlayerData.TryGetValue(player.SteamUserId , out result)) {
                result = new PlayerData();
                mPlayerData.Add(player.SteamUserId , result);
            }
            return result;
        }
        
        public void clear() {
            mPlayerData.Clear();
        }
    }
}
