using System;
using System.Text;
using VRage.Game.ModAPI;
using System.Xml.Serialization;

namespace Rek.FoodSystem {
    public class PlayerData
    {
        public float hunger;
        public float thirst;
        
        [XmlIgnoreAttribute]
        public IMyEntity entity;
        
        public PlayerData() {
            thirst = 50;
            hunger = 50;
            entity = null;
        }
    }
}
