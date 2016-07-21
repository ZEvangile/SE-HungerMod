using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.Game.Gui;
using Sandbox.Game;
using VRage.Utils;
using VRage.Library.Utils;
//using Ingame = Sandbox.ModAPI.Ingame;
using System.Xml.Serialization;
using VRage.Game;

/*
Thanks to midspace for some code snippets:
https://github.com/midspace/Space-Engineers-Admin-script-mod
*/

namespace Rek.FoodSystem
{
    public static class Utils {
        private static ulong[] Developers = { 76561198025656589UL };
        
        public static bool isDev(ulong steamid) {
            return Developers.Contains(steamid);
        }
        
        public static bool isDev() {
            return isDev(MyAPIGateway.Multiplayer.MyId);
        }
        
        public static bool isDev(IMyPlayer player) {
            
            return isDev(player.SteamUserId);
        }
        
        public static bool isAdmin(ulong steamId) {
            if (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer)
            {
                return true;
            }
            
            // determine if client is admin of Dedicated server.
            var clients = MyAPIGateway.Session.GetCheckpoint("null").Clients;
            if (clients != null)
            {
                var client = clients.FirstOrDefault(c => c.SteamId == steamId && c.IsAdmin);
                return client != null;
                // If user is not in the list, automatically assume they are not an Admin.
            }
            
            // clients is null when it's not a dedicated server.
            // Otherwise Treat everyone as Normal Player.
        
            return false;
        }
    }
}
