using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;

namespace SaveUtility
{
    [Serializable]
    public class WorldSave
    {
        [XmlElement]
        public int currentDay;
        [XmlElement]
        public int worldSeed;

        [XmlElement]
        public int boatStatus;

        [XmlElement]
        public float time;
        [XmlElement]
        public float totalTime;

        //playerstatus variables

        [XmlElement]
        public float health;
        [XmlElement]
        public int maxHealth;
        [XmlElement]
        public float stamina;
        [XmlElement]
        public float maxStamina;
        [XmlElement]
        public float shield;
        [XmlElement]
        public int maxShield;
        [XmlElement]
        public float hunger;
        [XmlElement]
        public float maxHunger;

        [XmlElement]
        public int draculaHpIncrease;

        [XmlElement]
        public float[] position;
        [XmlElement]
        public int[] powerups;
        [XmlElement]
        public int[] armor;

        [XmlElement]
        public bool[] softUnlocks;

        [XmlElement]
        public List<ChestWrapper> chests = new List<ChestWrapper>();
        [XmlElement]
        public List<ChestWrapper> furnaces = new List<ChestWrapper>();
        [XmlElement]
        public List<ChestWrapper> cauldrons = new List<ChestWrapper>();
        [XmlElement]
        public List<BuildWrapper> builds = new List<BuildWrapper>();
        [XmlElement]
        public List<MobWrapper> mobs = new List<MobWrapper>();
        [XmlElement]
        public List<int> repairs = new List<int>();

        //public static List<ResourceWrapper> resources = new List<ResourceWrapper>();
        [XmlElement]
        public List<SerializableTuple<int,int>> inventory = new List<SerializableTuple<int,int>>();

        [XmlElement]
        public SerializableDictionary<string, PlayerWrapper> clientPlayers;

        [XmlElement]
        public SerializableTuple<int, int> arrows;
        //ADD FURNACE ITEM SAVING, AND CAULDRON ITEM SAVING

        [XmlElement]
        public SerializableDictionary<string, SerializableDictionary<string, object>> customSaves = new SerializableDictionary<string, SerializableDictionary<string, object>>();

        [XmlElement]
        public List<DroppedItemWrapper> droppedItems = new List<DroppedItemWrapper>();

        public WorldSave()
        {

        }

        public WorldSave(int id)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

            worldSeed = World.worldSeed;
            currentDay = GameLoop.Instance.currentDay;

            time = DayCycle.time;
            totalTime = DayCycle.totalTime;

            if (PlayerStatus.Instance.hp > 0)
            {
                health = PlayerStatus.Instance.hp;
            }
            else
            {
                health = PlayerStatus.Instance.maxHp;
            }

            maxHealth = PlayerStatus.Instance.maxHp;
            stamina = PlayerStatus.Instance.stamina;
            maxStamina = PlayerStatus.Instance.maxStamina;
            shield = PlayerStatus.Instance.shield;
            maxShield = PlayerStatus.Instance.maxShield;
            hunger = PlayerStatus.Instance.hunger;
            maxHunger = PlayerStatus.Instance.maxHunger;

            draculaHpIncrease = PlayerStatus.Instance.draculaStacks;

            position = new float[3];
            position[0] = PlayerMovement.Instance.transform.position.x;
            position[1] = PlayerMovement.Instance.transform.position.y;
            position[2] = PlayerMovement.Instance.transform.position.z;

            powerups = (int[])typeof(PowerupInventory).GetField("powerups", flags).GetValue(PowerupInventory.Instance);

            armor = new int[4];
            for (int i = 0; i < 4; i++)
            {
                if (InventoryUI.Instance.armorCells[i].currentItem)
                {
                    armor[i] = InventoryUI.Instance.armorCells[i].currentItem.id;
                }
                else
                {
                    armor[i] = -1;
                }

            }

            softUnlocks = (bool[])typeof(UiEvents).GetField("unlockedSoft", flags).GetValue(UiEvents.Instance);

            foreach (InventoryCell cell in InventoryUI.Instance.cells)
            {
                
                if (cell.currentItem)
                {
                    int cellAmount = cell.currentItem.amount;
                    if (cellAmount > 0)
                    {
                        inventory.Add(new Tuple<int, int>(cell.currentItem.id, cellAmount));
                    }
                }
                else
                {
                    inventory.Add(new Tuple<int, int>(-1, 0));
                }
            }

            if (InventoryUI.Instance.arrows.currentItem)
            {
                arrows = new Tuple<int, int>(InventoryUI.Instance.arrows.currentItem.id, InventoryUI.Instance.arrows.currentItem.amount);
            }
            else
            {
                arrows = new Tuple<int, int>(-1, 0);
            }

            //multiplayer save
            clientPlayers = LoadManager.players;

            //world save
            builds = World.builds.Values.ToList();

            foreach (Chest chest in ChestManager.Instance.chests.Values.ToList())
            {
                if (chest.chestSize == 21)
                {
                    chests.Add(new ChestWrapper(chest));
                }
                else if (chest.chestSize == 6)
                {
                    cauldrons.Add(new ChestWrapper(chest));
                }
                else if (chest.chestSize == 3)
                {
                    furnaces.Add(new ChestWrapper(chest));
                }
            }

            foreach (Mob mob in MobManager.Instance.mobs.Values.ToList())
            {
                if (mob.mobType.id == 9 || mob.mobType.id == 10 || mob.mobType.id == 14)
                {
                    mobs.Add(new MobWrapper(mob));
                }
            }

            var repairComponents = (Component[])typeof(Boat).GetField("repairs", flags).GetValue(Boat.Instance);

            foreach (RepairInteract repair in repairComponents)
            {
                if (!repair)
                {
                    repairs.Add(repair.GetId());
                }
            }

            foreach (GameObject droppedItem in ItemManager.Instance.list.Values)
            {
                droppedItems.Add(new DroppedItemWrapper(droppedItem));
            }

            customSaves = CustomSaveData.customSaves;

            Debug.Log("Current Day: " + currentDay);
            Debug.Log("World Seed: " + worldSeed);
        }
    }
}
