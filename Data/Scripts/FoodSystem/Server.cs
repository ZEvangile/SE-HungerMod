using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
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
        private int food_logic_skip = 0;
        private const int FOOD_LOGIC_SKIP_TICKS = 60 * 5;	// Updating every 5s
        private static float MAX_VALUE = 100;
        private const float THIRST_PER_DAY = 120f;
        private const float HUNGER_PER_DAY = 30f;
        private const float DAMAGE_SPEED = 5;
        private const float DEFAULT_MODIFIER = 1f;
        private const float FLYING_MODIFIER = 1f;
        private const float RUNNING_MODIFIER = 1.5f;
        private const float SPRINTING_MODIFIER = 2f;
        private const float NO_MODIFIER = 0;

        private float mHungerPerMinute;
        private float mThirstPerMinute;

        //private static Config mConfig = Config.Load("hatm.cfg");
        private static PlayerDataStore mPlayerDataStore = new PlayerDataStore();
        private static List<IMyPlayer> mPlayers = new List<IMyPlayer>();
        private static Dictionary<string, float> mFoodTypes = new Dictionary<string, float>();
        private static Dictionary<string, float> mBeverageTypes = new Dictionary<string, float>();
        private const string OBJECT_BUILDER_PREFIX = "ObjectBuilder_";
        private static bool mStarted = false;

        private MyGameTimer mTimer;

        /*public static void RegisterFood(string szItemName, float hungerValue)
        {
            mFoodTypes.Add(szItemName, hungerValue);
        }

        public static void RegisterBeverage(string szItemName, float thirstValue)
        {
            mBeverageTypes.Add(szItemName, thirstValue);
        }
        
        */

        private static bool playerEatSomething(IMyEntity entity, PlayerData playerData)
        {
            MyInventoryBase inventory = ((MyEntity)entity).GetInventoryBase();
            var items = inventory.GetItems();

            foreach (IMyInventoryItem item in items)
            {
                float result;

                // Getting the item type

                string szItemContent = item.Content.ToString();
                string szTypeName = szItemContent.Substring(szItemContent.IndexOf(OBJECT_BUILDER_PREFIX) + OBJECT_BUILDER_PREFIX.Length);

                // Type verification

                if (!szTypeName.Equals("Ingot")) continue;

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

            foreach (IMyInventoryItem item in items)
            {
                float result;

                // Getting the item type

                string szItemContent = item.Content.ToString();

                //MyAPIGateway.Utilities.ShowMessage("DEBUG", "szItemContent: " + item.Content.SubtypeName);

                string szTypeName = szItemContent.Substring(szItemContent.IndexOf(OBJECT_BUILDER_PREFIX) + OBJECT_BUILDER_PREFIX.Length);

                // Type verification

                if (!szTypeName.Equals("Ingot")) continue;

                if (mBeverageTypes.TryGetValue(item.Content.SubtypeName, out result))
                {
                    float canConsumeNum = Math.Min(((MAX_VALUE - playerData.thirst) / result), (float)item.Amount);

                    //MyAPIGateway.Utilities.ShowMessage("DEBUG", "canDrink: " + canConsumeNum);

                    if (canConsumeNum > 0)
                    {
                        inventory.Remove(item, (MyFixedPoint)canConsumeNum);
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

            if (Utils.isDev()) MyAPIGateway.Utilities.ShowMessage("SERVER", "INIT");

            MyAPIGateway.Multiplayer.RegisterMessageHandler(1338, AdminCommandHandler);
            MyAPIGateway.Utilities.RegisterMessageHandler(1339, NeedsApiHandler);

            // Minimum of 2h, because it's unplayable under....

            float dayLen = Math.Max(MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes, 120f);

            mHungerPerMinute = HUNGER_PER_DAY / dayLen;
            mThirstPerMinute = THIRST_PER_DAY / dayLen;

            NeedsApi api = new NeedsApi();

            // Registering drinks

            api.RegisterDrinkableItem("WaterFood", 10f);
            api.RegisterDrinkableItem("CoffeeFood", 15f);

            //Server.RegisterBeverage("WaterFood", 10f);
            //Server.RegisterBeverage("CoffeeFood", 15f);

            // Registering foods

            api.RegisterEdibleItem("WarmFood", 20f);
            api.RegisterEdibleItem("FreshFood", 15f);
            api.RegisterEdibleItem("GummybearsFood", 5f);
            api.RegisterEdibleItem("SyntheticFood", 3f);

            //Server.RegisterFood("WarmFood", 20f);
            //Server.RegisterFood("FreshFood", 15f);
            //Server.RegisterFood("GummybearsFood", 5f);
            //Server.RegisterFood("SyntheticFood", 3f);

            // Initiate the timer

            mTimer = new MyGameTimer();
        }

        // Update the player list

        private void updatePlayerList()
        {
            mPlayers.Clear();
            MyAPIGateway.Players.GetPlayers(mPlayers);
        }

        private IMyEntity GetCharacterEntity(IMyEntity entity)
        {
            if (entity is MyCockpit)
                return (entity as MyCockpit).Pilot as IMyEntity;

            if (entity is MyRemoteControl)
                return (entity as MyRemoteControl).Pilot as IMyEntity;

            //TODO: Add more pilotable entities
            return entity;
        }

        private void updateFoodLogic()
        {
            float elapsedMinutes = (float)(mTimer.Elapsed.Seconds / 60);

            foreach (IMyPlayer player in mPlayers)
            {
                if (player.Controller != null && player.Controller.ControlledEntity != null && player.Controller.ControlledEntity.Entity != null)
                {
                    PlayerData playerData = mPlayerDataStore.get(player);
                    IMyEntity entity = GetCharacterEntity(player.Controller.ControlledEntity.Entity);

                    //MyAPIGateway.Utilities.ShowMessage("DEBUG", "State: " + character.MovementState);
                    //if(playerData.entity != null) {
                    //    MyAPIGateway.Utilities.ShowMessage  ("DEBUG", "Entity: " + playerData.entity.Closed);
                    //}

                    float CurrentModifier = DEFAULT_MODIFIER;

                    if (entity is IMyCharacter)
                    {
                        MyObjectBuilder_Character character = entity.GetObjectBuilder(false) as MyObjectBuilder_Character;

                        if (playerData.entity == null || playerData.entity.Closed || playerData.entity.EntityId != entity.EntityId)
                        {
                            bool bReset = false;

                            if (!playerData.loaded)
                            {
                                bReset = true;
                                playerData.loaded = true;
                            }
                            else if ((playerData.entity != null) && (playerData.entity != entity)) bReset = true;

                            if (bReset)
                            {
                                playerData.hunger = 100f;
                                playerData.thirst = 100f;
                            }

                            playerData.entity = entity;
                        }

                        switch (character.MovementState)
                        {
                            case MyCharacterMovementEnum.Flying:
                                CurrentModifier = FLYING_MODIFIER;
                                break;
                            case MyCharacterMovementEnum.Running:
                            case MyCharacterMovementEnum.Backrunning:
                            case MyCharacterMovementEnum.RunStrafingLeft:
                            case MyCharacterMovementEnum.RunStrafingRight:
                            case MyCharacterMovementEnum.RunningRightFront:
                            case MyCharacterMovementEnum.RunningRightBack:
                            case MyCharacterMovementEnum.RunningLeftBack:
                            case MyCharacterMovementEnum.RunningLeftFront:
                                CurrentModifier = RUNNING_MODIFIER;
                                break;

                            case MyCharacterMovementEnum.Sprinting:
                                CurrentModifier = SPRINTING_MODIFIER;
                                break;
                        }
                    }
                    else if (playerData.entity != null || !playerData.entity.Closed) entity = playerData.entity;

                    // Rise the thirst
                    if (playerData.thirst > 0)
                    {
                        playerData.thirst -= elapsedMinutes * mThirstPerMinute * CurrentModifier;
                        playerData.thirst = Math.Max(playerData.thirst, 0);
                    }

                    // Rise the hunger
                    if (playerData.hunger > 0)
                    {
                        playerData.hunger -= elapsedMinutes * mHungerPerMinute * CurrentModifier;
                        playerData.hunger = Math.Max(playerData.hunger, 0);
                    }

                    // Eat
                    if (playerData.hunger < 30) playerEatSomething(entity, playerData);

                    // Drink
                    if (playerData.thirst < 30) playerDrinkSomething(entity, playerData);

                    // Get some damages for not being well feed!

                    if (playerData.thirst <= 0 || playerData.hunger <= 0)
                    {
                        var destroyable = entity as IMyDestroyableObject;
                        destroyable.DoDamage(DAMAGE_SPEED, MyStringHash.GetOrCompute("Hunger/Thirst"), true);
                    }

                    string message = MyAPIGateway.Utilities.SerializeToXML<PlayerData>(playerData);
                    MyAPIGateway.Multiplayer.SendMessageTo(
                        1337,
                        Encoding.Unicode.GetBytes(message),
                        player.SteamUserId
                    );
                }
            }

            // Reinitialize the timer

            mTimer = new MyGameTimer();
        }

        public void AdminCommandHandler(byte[] data)
        {
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

        public void NeedsApiHandler(object data)
        {
            //mFoodTypes.Add(szItemName, hungerValue);
            //mBeverageTypes.Add(szItemName, thirstValue);

            NeedsApi.Event e = (NeedsApi.Event)data;

            if (e.type == NeedsApi.Event.Type.RegisterEdibleItem)
            {
                NeedsApi.RegisterEdibleItemEvent edibleItemEvent = (NeedsApi.RegisterEdibleItemEvent)e.payload;
                //MyAPIGateway.Utilities.ShowMessage("DEBUG", "EdibleItem " + edibleItemEvent.item + " registered");
                mFoodTypes.Add(edibleItemEvent.item, edibleItemEvent.value);
            }
            else if (e.type == NeedsApi.Event.Type.RegisterDrinkableItem)
            {
                NeedsApi.RegisterDrinkableItemEvent drinkableItemEvent = (NeedsApi.RegisterDrinkableItemEvent)e.payload;
                //MyAPIGateway.Utilities.ShowMessage("DEBUG", "DrinkableItem " + drinkableItemEvent.item + " registered");
                mBeverageTypes.Add(drinkableItemEvent.item, drinkableItemEvent.value);
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session == null) return;

            // Food logic is desactivated in creative mode

            if (MyAPIGateway.Session.SessionSettings.GameMode == MyGameModeEnum.Creative) return;

            try
            {
                if (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer)
                {
                    if (!mStarted)
                    {
                        mStarted = true;
                        init();

                        food_logic_skip = FOOD_LOGIC_SKIP_TICKS;
                    }

                    if (++food_logic_skip >= FOOD_LOGIC_SKIP_TICKS)
                    {
                        food_logic_skip = 0;

                        updatePlayerList();
                        updateFoodLogic();
                    }
                }
            }

            catch (Exception e)
            {
                //MyAPIGateway.Utilities.ShowMessage("ERROR", "Logger error: " + e.Message + "\n" + e.StackTrace);
                //MyLog.Default.WriteLineAndConsole(MOD_NAME + " had an error while logging message='"+msg+"'\nLogger error: " + e.Message + "\n" + e.StackTrace);
            }
        }

        // Saving datas when requested

        public override void SaveData()
        {
            mPlayerDataStore.Save();
        }

        protected override void UnloadData()
        {
            mStarted = false;
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(1338, AdminCommandHandler);
            mPlayers.Clear();
            mFoodTypes.Clear();
            mBeverageTypes.Clear();
            mPlayerDataStore.clear();
        }
    }
}