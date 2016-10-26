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
        
        [XmlIgnoreAttribute]
        public bool loaded;

        public PlayerData(ulong id)
        {
            thirst = 100;
            hunger = 100;
            entity = null;
            steamid = id;
            loaded = false;
        }

        public PlayerData() {
            thirst = 100;
            hunger = 100;
            entity = null;
            loaded = false;
        }
    }
}
