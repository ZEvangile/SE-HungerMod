using System;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using VRage.Game;
using VRage.Library.Utils;	// For MyGameModeEnum
using Draygo.API;
using VRageMath;

namespace Rek.FoodSystem
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Client : MySessionComponentBase
    {
        private bool mStarted = false;
        private IMyHudNotification mNotify = null;
        private PlayerData mPlayerData = null;
        private HUDTextAPI mHud;

        private void onMessageEntered(string messageText, ref bool sendToOthers)
        {
            sendToOthers = false;

            if (!messageText.StartsWith("/")) return;

            var words = messageText.Trim().ToLower().Replace("/", "").Split(' ');

            if (words.Length > 0)
            {
                switch (words[0])
                {
                    case "food":
                        if (MyAPIGateway.Session.SessionSettings.GameMode == MyGameModeEnum.Creative) MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Hunger and thirst are disabled in creative mode.");
                        else if (mPlayerData != null)
                        {
                            if (words.Length > 1 && words[1] == "detail") MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Hunger: " + mPlayerData.hunger + "% Thirst: " + mPlayerData.thirst + "%");
                            else MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Hunger: " + Math.Floor(mPlayerData.hunger) + "% Thirst: " + Math.Floor(mPlayerData.thirst) + "%");
                        }

                        break;

                    /*
                    case "debug":
                        if (words.Length > 1 && words[1] == "sun") MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Sun rotation interval: " + MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes);
                        else if (words.Length > 1 && words[1] == "world") MyAPIGateway.Utilities.ShowMessage("FoodSystem", "World name: " + MyAPIGateway.Session.Name);
                        break;
                    */

                    default:

                        Command cmd = new Command(MyAPIGateway.Multiplayer.MyId, messageText);

                        byte[] message = MyAPIGateway.Utilities.SerializeToBinary<Command>(cmd);
                        MyAPIGateway.Multiplayer.SendMessageToServer(
                            1338,
                            message
                        );
                        break;
                }
            }
        }

        private void init()
        {
            mHud = new HUDTextAPI(591816613);

            if (Utils.isDev())
            {
                MyAPIGateway.Utilities.ShowMessage("CLIENT", "INIT");
            }

            MyAPIGateway.Utilities.MessageEntered += onMessageEntered;

            MyAPIGateway.Multiplayer.RegisterMessageHandler(1337, FoodUpdateMsgHandler);
        }

        private void ShowNotification(string text, MyFontEnum color)
        {
            if (mNotify == null)
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
            //MyAPIGateway.Utilities.ShowMessage("Debug", "Heartbeat: " + mHud.Heartbeat);
            mPlayerData = MyAPIGateway.Utilities.SerializeFromBinary<PlayerData>(data);

            //MyAPIGateway.Utilities.ShowMessage("FoodSystem", "Hunger: " + Math.Floor(mPlayerData.hunger) + "% Thirst: " + Math.Floor(mPlayerData.thirst) + "%");

            if (mPlayerData.thirst <= 10 && mPlayerData.hunger <= 10)
            {
                ShowNotification("Warning: You are Thirsty (" + Math.Floor(mPlayerData.thirst) + "%) and Hungry (" + Math.Floor(mPlayerData.hunger) + "%)", MyFontEnum.Red);
            }
            else if (mPlayerData.thirst <= 10)
            {
                ShowNotification("Warning: You are Thirsty (" + Math.Floor(mPlayerData.thirst) + "%)", MyFontEnum.Red);
            }
            else if (mPlayerData.hunger <= 10)
            {
                ShowNotification("Warning: You are Hungry (" + Math.Floor(mPlayerData.hunger) + "%)", MyFontEnum.Red);
            }

            if (mHud.Heartbeat)
            {
                mHud.CreateAndSend(1, (mPlayerData.thirst <= 10) ? 10 : 1000, new Vector2D(-0.98f, -0.15f), "Thirst: " + ((mPlayerData.thirst <= 10) ? "<color=255,0,0>" : "<color=0,255,0>") + Math.Floor(mPlayerData.thirst) + "%");
                mHud.CreateAndSend(2, (mPlayerData.hunger <= 10) ? 10 : 1000, new Vector2D(-0.98f, -0.2f), "Hunger: " + ((mPlayerData.hunger <= 10) ? " <color=255,0,0>" : "<color=0,255,0>") + Math.Floor(mPlayerData.hunger) + "% ");
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session == null)
                return;

            try
            {
                var isHost = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer;

                var isDedicatedHost = isHost && MyAPIGateway.Utilities.IsDedicated;

                if (isDedicatedHost)
                    return;

                if (!mStarted)
                {
                    mStarted = true;
                    init();
                }
            }
            catch (Exception e)
            {
                //MyLog.Default.WriteLineAndConsole("(FoodSystem) Error: " + e.Message + "\n" + e.StackTrace);
                MyAPIGateway.Utilities.ShowMessage("Error", e.Message + "\n" + e.StackTrace);
            }
        }

        protected override void UnloadData()
        {
            mHud.Close();
            mStarted = false;
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(1337, FoodUpdateMsgHandler);
            MyAPIGateway.Utilities.MessageEntered -= onMessageEntered;
        }
    }
}