using VRage.ModAPI;
using System.Xml.Serialization;

namespace Rek.FoodSystem {
    public class PlayerData
    {
        public ulong steamid;
        public float hunger;
        public float thirst;
        
        [XmlIgnoreAttribute]
        public IMyEntity entity;

        public PlayerData(ulong id)
        {
            thirst = 100;
            hunger = 100;
            entity = null;
            steamid = id;
        }

        public PlayerData() {
            thirst = 100;
            hunger = 100;
            entity = null;
        }
    }
}
