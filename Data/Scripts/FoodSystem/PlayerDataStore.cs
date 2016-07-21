using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game;

namespace Rek.FoodSystem {
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
