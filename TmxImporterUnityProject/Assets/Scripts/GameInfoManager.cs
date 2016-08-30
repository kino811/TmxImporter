using UnityEngine;

namespace Kino {
    [System.SerializableAttribute]
    public class ItemData {
        public int id;
        public Sprite icon;
        public string name;
        public int attackPower;
        public int defensePower;
        public uint storeCost;
    }

    public class GameInfoManager : MonoBehaviour {
        public ItemData[] itemDatas;
    }
}

