using System;
using UnityEngine;
using System.Collections.Generic;

namespace SaveUtility
{
    [Serializable]
    public class PlayerWrapper
    {
        public float health;
        public int maxHealth;
        public float stamina;
        public float maxStamina;
        public float shield;
        public int maxShield;
        public float hunger;
        public float maxHunger;

        public int draculaHpIncrease;

        public List<int> powerups;
        public int[] armor;

        public float[] position;

        public List<SerializableTuple<int, int>> inventory = new List<SerializableTuple<int, int>>();
        public SerializableTuple<int, int> arrows;
    }
}
