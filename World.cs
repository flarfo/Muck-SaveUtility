using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace SaveUtility
{
    [HarmonyPatch]
    public static class World
    {
        public static bool doSave = false;

        public static int worldSeed;

        internal static Dictionary<int, BuildWrapper> builds = new Dictionary<int, BuildWrapper>();

        public static int chest;
        public static int furnace;
        public static int cauldron;

        [HarmonyPatch(typeof(ItemManager), "InitAllItems")]
        [HarmonyPostfix]
        static void GetBuildIds()
        {
            chest = ItemManager.Instance.GetItemByName("Chest").id;
            furnace = ItemManager.Instance.GetItemByName("Furnace").id;
            cauldron = ItemManager.Instance.GetItemByName("Cauldron").id;
        }

        [HarmonyPatch(typeof(SteamLobby), "FindSeed")]
        static void Postfix(ref int __result)
        {
            worldSeed = __result;
        }

        [HarmonyPatch(typeof(GameLoop), "NewDay")]
        static void Postfix(int day)
        {
            if (UIManager.useAutoSave)
            {
                Debug.Log("Day " + day);
                if (day == 0 || !doSave)
                {
                    Debug.Log("DO NOT SAVE");
                    return;
                }

                Save();
            }
        }

        public static void Save()
        {
            if (LocalClient.serverOwner)
            {
                if (LoadManager.currentSave != "")
                {
                    ClientSend.SendChatMessage("<color=#ADD8E6>Saving...");
                    ChatBox.Instance.AppendMessage(-1, "<color=#ADD8E6>Saving...", "");

                    Debug.Log("SAVING");

                    //send packets to receive player data
                    ServerMethods.SendServerSave();
                    
                    WorldTimer timer = new GameObject("World Timer", new[] { typeof(WorldTimer) }).GetComponent<WorldTimer>();
                    timer.StartSave(5);
                }
            }  
        }

        [HarmonyPatch(typeof(GameManager), "LeaveGame")]
        [HarmonyPostfix]
        static void LeaveGameReset()
        {
            Debug.Log("RESETTING");

            builds.Clear();

            doSave = false;

            LoadManager.players.Clear();
            LoadManager.currentSave = "";
            LoadManager.hasSaveLoaded = false;
            LoadManager.serverHasSaveLoaded = false;
            LoadManager.playersUpdated = false;
            LoadManager.loadedSave = null;

            CustomSaveData.customSaves = new SerializableDictionary<string, SerializableDictionary<string, object>>();
        }

        [HarmonyPatch(typeof(BuildManager), "BuildItem")]
        static void Postfix(int buildOwner, int itemID, int objectId, Vector3 position, int yRotation)
        {
            if (LocalClient.serverOwner)
            {
                if (itemID == chest || itemID == furnace || itemID == cauldron || builds.ContainsKey(objectId))
                {
                    return;
                }

                builds.Add(objectId, new BuildWrapper(itemID, position, yRotation));
            }
        }

        [HarmonyPatch(typeof(ResourceManager), "RemoveItem")]
        [HarmonyPostfix]
        static void RemoveBuild(int id)
        {
            if (LocalClient.serverOwner)
            {
                if (builds.ContainsKey(id))
                {
                    builds.Remove(id);
                }
            }
        }
    }

    class WorldTimer : MonoBehaviour
    {
        private IEnumerator coroutine;

        public void StartSave(int _time)
        {
            coroutine = SaveCoroutine(_time);
            StartCoroutine(coroutine);
        }

        public void StartPlayerDeath(float _time)
        {
            Debug.Log("Starting Player Death");
            coroutine = PlayerDeathCoroutine(_time);
            Debug.Log("Player Death Started");
            StartCoroutine(coroutine);
        }

        private IEnumerator PlayerDeathCoroutine(float _time)
        {
            Debug.Log("Player Death Started Coroutine");
            yield return new WaitForSecondsRealtime(_time);

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            Debug.Log("PLAYER DYING");

            typeof(PlayerStatus).GetMethod("PlayerDied", flags).Invoke(PlayerStatus.Instance, new object[] { 0, -1 });
            Debug.Log("Player Died");
            UnityEngine.Object.Destroy(this);
        }

        private IEnumerator SaveCoroutine(float _time)
        {
            yield return new WaitForSecondsRealtime(_time);

            if (LoadManager.currentSave != "")
            {
                try
                {
                    SaveSystem.Save(LoadManager.currentSave);
                    ClientSend.SendChatMessage($"<color=#ADD8E6>Save Completed! Seed: {World.worldSeed}");
                    ChatBox.Instance.AppendMessage(-1, $"<color=#ADD8E6>Save Completed! Seed: {World.worldSeed}", "");
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                    ClientSend.SendChatMessage("<color=#FF0000>Save Failed!");
                    ChatBox.Instance.AppendMessage(-1, "<color=#FF0000>Save Failed", "");
                }
            }

            UnityEngine.Object.Destroy(this);
        }
    }
}
