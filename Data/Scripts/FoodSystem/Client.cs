using System;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using VRage.Game;

namespace Rek.FoodSystem
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Client : MySessionComponentBase
    {        
        private bool mStarted = false;
        private IMyHudNotification mNotify = null;
        private PlayerData mPlayerData = null;

        private void onMessageEntered(string messageText, ref bool sendToOthers)
        {
            if (!messageText.StartsWith("/")) { return; }
            var words = messageText.Trim().ToLower().Replace("/", "").Split(' ');
            sendToOthers = false;
            if (words.Length > 0 && mPlayerData != null) {
                switch (words[0])
                {
                    case "food":
                        if (words.Length > 1 && words[1] == "detail") MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Hunger: " + mPlayerData.hunger + "% Thirst: " + mPlayerData.thirst + "%");
                        else if (words.Length > 1 && words[1] == "sun") MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Sun rotation interval: " + MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes);
                        else MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Hunger: " + Math.Floor(mPlayerData.hunger) + "% Thirst: " + Math.Floor(mPlayerData.thirst) + "%");
                        break;
                        
                    /*
                    case "debug":
                        float sunRotationInterval = MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes;
                        MyAPIGateway.Utilities.ShowMessage("Debug", "SunRotationInterval: " + sunRotationInterval);
                        break;
                    */
                        
                    default:
                        Command cmd = new Command(MyAPIGateway.Multiplayer.MyId, messageText);
                        string message = MyAPIGateway.Utilities.SerializeToXML<Command>(cmd);
                        MyAPIGateway.Multiplayer.SendMessageToServer(
                            1338,
                            Encoding.Unicode.GetBytes(message)
                        );
                        break;
                        
                }
            }
        }
        
        private void init() {
            if (Utils.isDev()) {
                MyAPIGateway.Utilities.ShowMessage("CLIENT", "INIT");
            }
            
            MyAPIGateway.Utilities.MessageEntered += onMessageEntered;

            MyAPIGateway.Multiplayer.RegisterMessageHandler(1337, FoodUpdateMsgHandler);
        }
        
        private void ShowNotification(string text, MyFontEnum color) {
            if(mNotify == null)
            {
                mNotify = MyAPIGateway.Utilities.CreateNotification(text, 10000, MyFontEnum.Red);
            }
            else
            {
                mNotify.Text = text;
                mNotify.ResetAliveTime();
            }
                
            mNotify.Show();
        }
        
        private void FoodUpdateMsgHandler(byte[] data)
        {
            mPlayerData = MyAPIGateway.Utilities.SerializeFromXML<PlayerData>(Encoding.Unicode.GetString(data));
        
            if (mPlayerData.thirst <= 10 && mPlayerData.hunger <= 10) {
                ShowNotification("Warning: You are Thirsty (" + Math.Floor(mPlayerData.thirst) + "%) and Hungry (" + Math.Floor(mPlayerData.hunger) + "%)", MyFontEnum.Red);
            } else if(mPlayerData.thirst <= 10) {
                ShowNotification("Warning: You are Thirsty (" + Math.Floor(mPlayerData.thirst) + "%)", MyFontEnum.Red);
            } else if(mPlayerData.hunger <= 10) {
                ShowNotification("Warning: You are Hungry (" + Math.Floor(mPlayerData.hunger) + "%)", MyFontEnum.Red);
            }
        }
        
        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session == null)
                return;
        
            try {
                var isHost = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer;
                var isDedicatedHost = isHost && MyAPIGateway.Utilities.IsDedicated;
            
                if (isDedicatedHost)
                    return;
            
                if (!mStarted) {
                    mStarted = true;
                    init();
                }
            } catch(Exception e) {
                //MyLog.Default.WriteLineAndConsole("(FoodSystem) Error: " + e.Message + "\n" + e.StackTrace);
                MyAPIGateway.Utilities.ShowMessage("Error", e.Message + "\n" + e.StackTrace);
            }
        }
        
        protected override void UnloadData()
        {
            mStarted = false;
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(1337, FoodUpdateMsgHandler);
            MyAPIGateway.Utilities.MessageEntered -= onMessageEntered;
        }
    }
}
