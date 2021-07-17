using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SaveUtility
{
    [HarmonyPatch]
    public static class LoadManager
    {
        public static WorldSave loadedSave;

        public static string[] worldSaves;
        public static string currentSave = "";

        public static bool hasSaveLoaded = false;
        public static bool serverHasSaveLoaded = false;
        internal static bool playersUpdated = false;

        public static SerializableDictionary<string, PlayerWrapper> players = new SerializableDictionary<string, PlayerWrapper>();

        [HarmonyPatch(typeof(MenuUI), "StartGame")]
        [HarmonyPrefix]
        static void LoadOnStart()
        {
            if (hasSaveLoaded)
            {
                Debug.Log("HAS SAVE LOADED");

                loadedSave = SaveSystem.Load(currentSave);

                Debug.Log("SAVE LOADED " + currentSave);

                if (loadedSave.customSaves != null)
                {
                    Debug.Log("CUSTOM SAVES");
                    CustomSaveData.customSaves = loadedSave.customSaves;
                    Debug.Log("CUSTOM SAVES LOADED");
                }
                
                ServerMethods.SendHasSave();
            }
        }

        [HarmonyPatch(typeof(GameManager), "SendPlayersIntoGame")]
        [HarmonyPostfix]
        static void AllPlayersReady()
        {
            Debug.Log("All Players Ready");
            if (LocalClient.serverOwner)
            {
                if (hasSaveLoaded)
                {
                    ServerMethods.SendHasSave();
                }
            }
            
        }

        [HarmonyPatch(typeof(GameLoop), "StartLoop")]
        [HarmonyPostfix]
        static void Postfix()
        {
            Debug.Log("SPAWN PLAYERS THING");

            if (!playersUpdated)
            {
                if (LocalClient.serverOwner)
                {
                    if (hasSaveLoaded)
                    {
                        GameLoop.Instance.currentDay = loadedSave.currentDay;
                        GameManager.instance.currentDay = loadedSave.currentDay;
                        GameManager.instance.UpdateDay(loadedSave.currentDay);
                        ServerSend.NewDay(loadedSave.currentDay);

                        LoadChestWrappers(loadedSave.chests, World.chest);
                        LoadChestWrappers(loadedSave.furnaces, World.furnace);
                        LoadChestWrappers(loadedSave.cauldrons, World.cauldron);

                        LoadMainPlayer();
                        LoadMobs();
                        LoadItems();
                        LoadBuilds();
                        LoadBoat();

                        players = loadedSave.clientPlayers;
                    }
                    else
                    {
                        currentSave = World.worldSeed + ".muck";
                    }
                    World.doSave = true;
                }
                playersUpdated = true;
            }
        }

        [HarmonyPatch(typeof(DayCycle), "Awake")]
        [HarmonyPostfix]
        static void DayPatch(DayCycle __instance) 
        {
            if (LocalClient.serverOwner)
            {
                if (hasSaveLoaded)
                {
                    var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

                    DayCycle.time = loadedSave.time;
                    typeof(DayCycle).GetProperty("totalTime", flags).SetValue(__instance, loadedSave.totalTime);
                }
            }
        }

        static void LoadChestWrappers(List<ChestWrapper> chestWrappers, int buildId)
        {
            foreach (ChestWrapper chestWrapper in chestWrappers)
            {
                int nextId = ChestManager.Instance.GetNextId();

                BuildManager.Instance.BuildItem(0, buildId, nextId, new Vector3(chestWrapper.position[0], chestWrapper.position[1], chestWrapper.position[2]), chestWrapper.rotation);
                ServerSend.SendBuild(0, buildId, nextId, new Vector3(chestWrapper.position[0], chestWrapper.position[1], chestWrapper.position[2]), chestWrapper.rotation);

                Chest curChest = ChestManager.Instance.chests[nextId];
                curChest.cells = new InventoryItem[chestWrapper.chestSize];

                for (int i = 0; i < chestWrapper.fullCells; i++)
                {
                    if (chestWrapper.cells[i].Item1 != -1)
                    {
                        InventoryItem inventoryItem = ScriptableObject.CreateInstance<InventoryItem>();
                        inventoryItem.Copy(ItemManager.Instance.allItems[chestWrapper.cells[i].Item1], chestWrapper.cells[i].Item2);
                        curChest.cells[chestWrapper.cells[i].Item3] = inventoryItem;

                        ServerSend.UpdateChest(0, nextId, chestWrapper.cells[i].Item3, chestWrapper.cells[i].Item1, chestWrapper.cells[i].Item2);
                    }
                }
            }
        }

        static void LoadItems()
        {
            foreach (DroppedItemWrapper droppedItem in loadedSave.droppedItems)
            {
                int id = droppedItem.itemId;
                int amount = droppedItem.amount;
                int nextId = ItemManager.Instance.GetNextId();
                Vector3 pos = new Vector3(droppedItem.position[0], droppedItem.position[1], droppedItem.position[2]);

                ItemManager.Instance.DropItemAtPosition(id, amount, pos, nextId);
                ServerSend.DropItemAtPosition(id, amount, nextId, pos);
            }
        }

        static void LoadBuilds()
        {
            foreach (BuildWrapper build in loadedSave.builds)
            {
                int nextId = BuildManager.Instance.GetNextBuildId();
                BuildManager.Instance.BuildItem(0, build.itemId, nextId, new Vector3(build.position[0], build.position[1], build.position[2]), build.rotation);
                ServerSend.SendBuild(0, build.itemId, nextId, new Vector3(build.position[0], build.position[1], build.position[2]), build.rotation);
            }
        }

        static void LoadMobs()
        {
            Debug.Log("Spawning Mobs!");

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            int mobCount = 0;

            foreach (MobWrapper mob in loadedSave.mobs)
            {
                int nextId = MobManager.Instance.GetNextId();

                MobSpawner.Instance.ServerSpawnNewMob(nextId, mob.mobType, new Vector3(mob.position[0], mob.position[1], mob.position[2]),
                    mob.multiplier, mob.bossMultiplier, (Mob.BossType)mob.bossType, mob.guardianType);
                mobCount++;
            }

            typeof(MobManager).GetField("mobId", flags).SetValue(MobManager.Instance, mobCount);
        }

        static void LoadMainPlayer()
        {
            InventoryItem[] allScriptableItems = ItemManager.Instance.allScriptableItems;

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            typeof(PowerupInventory).GetField("powerups", flags).SetValue(PowerupInventory.Instance, loadedSave.powerups);

            PlayerStatus.Instance.hp = loadedSave.health;
            PlayerStatus.Instance.maxHp = loadedSave.maxHealth;
            PlayerStatus.Instance.stamina = loadedSave.stamina;
            PlayerStatus.Instance.maxStamina = loadedSave.maxStamina;
            PlayerStatus.Instance.shield = loadedSave.shield;
            PlayerStatus.Instance.maxShield = loadedSave.maxShield;
            PlayerStatus.Instance.hunger = loadedSave.hunger;
            PlayerStatus.Instance.maxHunger = loadedSave.maxHunger;

            PlayerStatus.Instance.draculaStacks = loadedSave.draculaHpIncrease;

            Vector3 position = new Vector3(loadedSave.position[0], loadedSave.position[1], loadedSave.position[2]);

            PlayerMovement.Instance.transform.position = position;

            for (int i = 0; i < loadedSave.powerups.Length; i++)
            {
                for (int j = 0; j < loadedSave.powerups[i]; j++)
                {
                    PowerupUI.Instance.AddPowerup(i);
                }
            }

            OtherInput.Instance.ToggleInventory(OtherInput.CraftingState.Inventory);
            for (int i = 0; i < 4; i++)
            {
                if (loadedSave.armor[i] != -1)
                {
                    InventoryUI.Instance.AddArmor(allScriptableItems[loadedSave.armor[i]]);
                    PlayerStatus.Instance.UpdateArmor(i, loadedSave.armor[i]);
                }
            }
            OtherInput.Instance.ToggleInventory(OtherInput.CraftingState.Inventory);

            for (int i = 0; i < InventoryUI.Instance.cells.Count; i++)
            {
                if (loadedSave.inventory[i].Item1 != -1)
                {
                    InventoryUI.Instance.cells[i].ForceAddItem(allScriptableItems[loadedSave.inventory[i].Item1], loadedSave.inventory[i].Item2);
                }
            }

            if (loadedSave.arrows.Item1 != -1)
            {
                InventoryUI.Instance.arrows.ForceAddItem(allScriptableItems[loadedSave.arrows.Item1], loadedSave.arrows.Item2);
            }

            for (int i = 0; i < loadedSave.softUnlocks.Length; i++)
            {
                if (loadedSave.softUnlocks[i])
                {
                    typeof(UiEvents).GetMethod("UnlockItemSoft", flags).Invoke(UiEvents.Instance, new object[] { i });
                }
            }

            typeof(UiEvents).GetMethod("Unlock", flags).Invoke(UiEvents.Instance, null);
            InventoryUI.Instance.UpdateAllCells();
            Hotbar.Instance.UpdateHotbar();
            PlayerStatus.Instance.UpdateStats();
        }

        static void LoadClients()
        {
            List<Player> clientPlayers = new List<Player>();

            Client[] clients = Server.clients.Values.ToArray();

            for (int i = 0; i < Server.clients.Values.Count; i++)
            {
                if (clients[i].player != null)
                {
                    Debug.Log(i);
                    clientPlayers.Add(clients[i].player);
                }
            }

            for (int i = 0; i < clientPlayers.Count; i++)
            {
                if (players.ContainsKey(clientPlayers[i].steamId.ToString()))
                {
                    if (i != 0)
                    {
                        int curId = clients[i].id;

                        PlayerWrapper curPlayer = players[clientPlayers[i].steamId.ToString()];

                        Debug.Log("Sending Inventory To: " + i);
                        Debug.Log("Steam Id: " + clientPlayers[i].steamId.ToString());

                        ServerMethods.SendPlayer(curId, curPlayer);
                    } 
                }
            }
        }

        static void LoadBoat()
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            foreach (int id in loadedSave.repairs)
            {
                ClientSend.Interact(id);
            }

            switch (loadedSave.boatStatus)
            {
                case 0:
                    return;
                case 1:
                    Boat.Instance.MarkShip();
                    return;
                case 2:
                    Boat.Instance.MarkShip();
                    typeof(Boat).GetMethod("MarkGems", flags).Invoke(Boat.Instance, null);
                    return;
                case 3:
                    Boat.Instance.MarkShip();
                    typeof(Boat).GetMethod("MarkGems", flags).Invoke(Boat.Instance, null);
                    Boat.Instance.BoatFinished(ResourceManager.Instance.GetNextId());
                    return;
            }
        }

        [HarmonyPatch(typeof(GameLoop), "Awake")]
        [HarmonyPostfix]
        static void UpdateMobInfo(GameLoop __instance)
        {
            if (LocalClient.serverOwner)
            {
                if (hasSaveLoaded)
                {
                    var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

                    int mobCount = 0;

                    foreach (MobWrapper mob in loadedSave.mobs)
                    {
                        mobCount++;
                    }

                    typeof(GameLoop).GetField("activeMobs", flags).SetValue(__instance, mobCount);
                }
            }
        }

        [HarmonyPatch(typeof(SteamLobby), "FindSeed")]
        [HarmonyPostfix]
        static void GetLoadedSeed(ref int __result)
        {
            if (hasSaveLoaded)
            {
                __result = loadedSave.worldSeed;
            }
        }

        [HarmonyPatch(typeof(SpawnChestsInLocations), "SetChests")]
        [HarmonyPrefix]
        static bool StopChestLoad()
        {
            if (LocalClient.serverOwner)
            {
                return !hasSaveLoaded;
            }
            else
            {
                return !serverHasSaveLoaded;
            }
        }
    }
}
