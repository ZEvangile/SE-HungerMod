using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage;
using VRage.Utils;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.ModAPI;

namespace Rek.FoodSystem
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Server : MySessionComponentBase
    {
        private int player_update_skip = 0;
        private int food_logic_skip = 0;
        private const int PLAYER_UPDATE_SKIP_TICKS = 60*30;
        private const int FOOD_LOGIC_SKIP_TICKS = 60*10;
        
        private static float MAX_VALUE = 100;
        private const float THIRST_PER_DAY = 60f;
        private const float HUNGER_PER_DAY = 30f;
        private const float DAMAGE_SPEED = 5;
        private const float DEFAULT_MODIFIER = 1f;
        private const float RUNNING_MODIFIER = 1.5f;
        private const float SPRINTING_MODIFIER = 2f;
        
        private float mHungerPerMinute;
        private float mThirstPerMinute;
        private float mCurrentModifier = 0;
        
        private static Config mConfig = Config.Load("hatm.cfg");
        private static PlayerDataStore mPlayerDataStore = new PlayerDataStore();
        private static List<IMyPlayer> mPlayers = new List<IMyPlayer>();
        private static Dictionary<string, float> mFoodTypes = new Dictionary<string, float>();
        private static Dictionary<string, float> mBeverageTypes = new Dictionary<string, float>();
		    private const string OBJECT_BUILDER_PREFIX = "ObjectBuilder_";
        private static bool mStarted = false;
        
        private MyGameTimer mTimer;
        
        public static void RegisterFood(string szItemName, float hungerValue)
        {

            mFoodTypes.Add(szItemName, hungerValue);

        }
        
        public static void RegisterBeverage(string szItemName, float thirstValue)
        {

            mBeverageTypes.Add(szItemName, thirstValue);

        }
        
        private static bool playerEatSomething(IMyEntity entity, PlayerData playerData)
        {

            MyInventoryBase inventory = ((MyEntity)entity).GetInventoryBase();
            var items = inventory.GetItems();

            foreach(IMyInventoryItem item in items)
            {

                float result;

                // Getting the item type

                string szItemContent = item.Content.ToString();
                string szTypeName = szItemContent.Substring(szItemContent.IndexOf(OBJECT_BUILDER_PREFIX)+OBJECT_BUILDER_PREFIX.Length);

                // Type verification
        
                if(!szTypeName.Equals("Ingot"))continue;

                if (mFoodTypes.TryGetValue(item.Content.SubtypeName, out result))
                {

                    float canConsumeNum = Math.Min(((MAX_VALUE - playerData.hunger) / result), (float)item.Amount);

                    //MyAPIGateway.Utilities.ShowMessage("DEBUG", "canEat: " + canConsumeNum);

                    if (canConsumeNum > 0)
                    {

                        inventory.Remove(item, (MyFixedPoint)canConsumeNum);
                        playerData.hunger += result * (float)canConsumeNum;

                        return true;

                    }

                }

            }

            return false;

        }

        private static bool playerDrinkSomething(IMyEntity entity, PlayerData playerData)
        {

            MyInventoryBase inventory = ((MyEntity)entity).GetInventoryBase();
            var items = inventory.GetItems();

            foreach(IMyInventoryItem item in items)
            {

                float result;

                // Getting the item type

                string szItemContent = item.Content.ToString();

                //MyAPIGateway.Utilities.ShowMessage("DEBUG", "szItemContent: " + item.Content.SubtypeName);

                string szTypeName = szItemContent.Substring(szItemContent.IndexOf(OBJECT_BUILDER_PREFIX)+OBJECT_BUILDER_PREFIX.Length);

                // Type verification

                if(!szTypeName.Equals("Ingot"))continue;

                if(mBeverageTypes.TryGetValue(item.Content.SubtypeName, out result))
                {

                    float canConsumeNum = Math.Min(((MAX_VALUE - playerData.thirst) / result), (float)item.Amount);

                    //MyAPIGateway.Utilities.ShowMessage("DEBUG", "canDrink: " + canConsumeNum);

                    if (canConsumeNum > 0)
                    {

                        inventory.Remove(item, (MyFixedPoint)canConsumeNum);
                        //inventory.RemoveItems(item.ItemId, canConsumeNum);
                        playerData.thirst += result * (float)canConsumeNum;

                        return true;

                    }

                }   

            }

            return false;

        }
        
        private void init()
        {
            mPlayerDataStore.Load();

            if (Utils.isDev())MyAPIGateway.Utilities.ShowMessage("SERVER", "INIT");

            MyAPIGateway.Multiplayer.RegisterMessageHandler(1338, AdminCommandHandler);

            mTimer = new MyGameTimer();

            float dayLen = MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes;

            mHungerPerMinute = HUNGER_PER_DAY / dayLen;
            mThirstPerMinute = THIRST_PER_DAY / dayLen;

            updatePlayerList();

            // We just need the subtype name, not the type (wich is Ingot for everyone)

            Server.RegisterBeverage("WaterFood", 10f);
            Server.RegisterBeverage("CoffeeFood", 15f);
            Server.RegisterFood("WarmFood", 20f);
            Server.RegisterFood("FreshFood", 15f);
            Server.RegisterFood("GummybearsFood", 5f);
            Server.RegisterFood("SyntheticFood", 3f);

        }

        private void updatePlayerList() {
            // Update the player list
            mPlayers.Clear();
            MyAPIGateway.Players.GetPlayers(mPlayers);
        }

        private IMyEntity GetCharacterEntity(IMyEntity entity) {
            if(entity is MyCockpit)
                return (entity as MyCockpit).Pilot as IMyEntity;

            if(entity is MyRemoteControl)
                return (entity as MyRemoteControl).Pilot as IMyEntity;

            //TODO: Add more pilotable entities
            return entity;

        }

        private void updateFoodLogic() {
            foreach(IMyPlayer player in mPlayers) {
                if(player.Controller != null &&  player.Controller.ControlledEntity != null &&  player.Controller.ControlledEntity.Entity != null) {
                    PlayerData playerData = mPlayerDataStore.get(player);
                    IMyEntity entity = GetCharacterEntity(player.Controller.ControlledEntity.Entity);

                    //MyAPIGateway.Utilities.ShowMessage("DEBUG", "State: " + character.MovementState);
                    //if(playerData.entity != null) {
                    //    MyAPIGateway.Utilities.ShowMessage  ("DEBUG", "Entity: " + playerData.entity.Closed);
                    //}

                    mCurrentModifier = DEFAULT_MODIFIER;
                    if(entity is IMyCharacter) {
                        MyObjectBuilder_Character character = entity.GetObjectBuilder(false) as MyObjectBuilder_Character;

                        if(playerData.entity == null || playerData.entity.Closed || playerData.entity.EntityId != entity.EntityId) {
                            playerData.hunger = 50f;
                            playerData.thirst = 50f;
                            playerData.entity = entity;
                        }   

                        switch(character.MovementState) {   
                            case MyCharacterMovementEnum.Running:
                            case MyCharacterMovementEnum.Backrunning:
                            case MyCharacterMovementEnum.RunStrafingLeft:
                            case MyCharacterMovementEnum.RunStrafingRight:
                            case MyCharacterMovementEnum.RunningRightFront:
                            case MyCharacterMovementEnum.RunningRightBack:
                            case MyCharacterMovementEnum.RunningLeftBack:
                            case MyCharacterMovementEnum.RunningLeftFront:
                                mCurrentModifier = RUNNING_MODIFIER;
                                break;
                            
                            case MyCharacterMovementEnum.Sprinting:
                                mCurrentModifier = SPRINTING_MODIFIER;
                                break;
                                
                            case MyCharacterMovementEnum.Died:
                                mCurrentModifier = 0;
                                break;
                        }        
                    } else if(playerData.entity != null || !playerData.entity.Closed) {
                        entity = playerData.entity;
                    }

                    float elapsedMinutes = (float)(mTimer.Elapsed.Seconds / 60);

                    if (playerData.thirst <= 0 || playerData.hunger <= 0)
                    {

                        var destroyable = entity as IMyDestroyableObject;

                        destroyable.DoDamage(elapsedMinutes * DAMAGE_SPEED, MyStringHash.GetOrCompute("Hunger/Thirst"), true);

                    }

                    if(playerData.hunger < 100) {
                        //Eat
                        playerEatSomething(entity, playerData);
                    }
                    
                    if(playerData.thirst < 100) {
                        //Drink
                        playerDrinkSomething(entity, playerData);
                    }
                    
                    if (playerData.thirst > 0) {
                        float gain = Math.Min(elapsedMinutes * mThirstPerMinute * mCurrentModifier, playerData.thirst);
                        playerData.thirst -= gain;
                        //MyAPIGateway.Utilities.ShowMessage("DEBUG", "Thirst Gain: " + gain);
                    }
                    
                    if (playerData.hunger > 0) {
                        playerData.hunger -= Math.Min(elapsedMinutes * mHungerPerMinute * (mCurrentModifier / 2), playerData.hunger);
                    }
                    
                    mTimer = new MyGameTimer();
                    
                    string message = MyAPIGateway.Utilities.SerializeToXML<PlayerData>(playerData);
                    MyAPIGateway.Multiplayer.SendMessageTo(
                        1337,
                        Encoding.Unicode.GetBytes(message),
                        player.SteamUserId
                    );
                }
            } 
        }
        
        public void AdminCommandHandler(byte[] data) {
            //Keen why do you not pass the steamId? :/
            Command command = MyAPIGateway.Utilities.SerializeFromXML<Command>(Encoding.Unicode.GetString(data));
            
            /*if (Utils.isAdmin(command.sender)) {
                var words = command.content.Trim().ToLower().Replace("/", "").Split(' ');
                if (words.Length > 0 && words[0] == "hatm") {
                    switch (words[1])
                    {
                        case "blacklist":
                            IMyPlayer player = mPlayers.Find(p => words[2] == p.DisplayName);
                            mConfig.BlacklistAdd(player.SteamUserId);
                            break;
                    }
                }
            }*/
            
        }
        
        public override void UpdateAfterSimulation()
        {
            if(MyAPIGateway.Session == null)
                return;
        
            try {
                if(MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer) {
                    if (!mStarted) {
                        mStarted = true;
                        init();
                    }
                    
                    if(++player_update_skip >= PLAYER_UPDATE_SKIP_TICKS) {
                        player_update_skip = 0;
                        updatePlayerList();
                    }
                        
                    if(++food_logic_skip >= FOOD_LOGIC_SKIP_TICKS) {
                        food_logic_skip = 0;
                        updateFoodLogic();
                    }
                }
            } catch(Exception e) {
                //MyAPIGateway.Utilities.ShowMessage("ERROR", "Logger error: " + e.Message + "\n" + e.StackTrace);
                //MyLog.Default.WriteLineAndConsole(MOD_NAME + " had an error while logging message='"+msg+"'\nLogger error: " + e.Message + "\n" + e.StackTrace);
            }
        }
        
        protected override void UnloadData()
        {
            mStarted = false;
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(1338, AdminCommandHandler);
            mPlayers.Clear();
            mFoodTypes.Clear();
            mBeverageTypes.Clear();

            mPlayerDataStore.Save();
            mPlayerDataStore.clear();
        }
    }
}
