using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Steamworks;

namespace SaveUtility
{
    [HarmonyPatch]
    internal class ClientMethods
    {
        [HarmonyPatch(typeof(LocalClient), "InitializeClientData")]
        static void Postfix()
        {
            Debug.Log("CLIENT DATA INITIALIZED");

            LocalClient.packetHandlers.Add(100, new LocalClient.PacketHandler(ReceiveServerHasSave));
            LocalClient.packetHandlers.Add(101, new LocalClient.PacketHandler(HandleSave));
            LocalClient.packetHandlers.Add(102, new LocalClient.PacketHandler(ReceiveInventory));
            LocalClient.packetHandlers.Add(103, new LocalClient.PacketHandler(ReceivePowerups));
            LocalClient.packetHandlers.Add(104, new LocalClient.PacketHandler(ReceivePosition));
            LocalClient.packetHandlers.Add(105, new LocalClient.PacketHandler(ReceivePlayerStatus));
            LocalClient.packetHandlers.Add(106, new LocalClient.PacketHandler(ReceiveArmor));
            LocalClient.packetHandlers.Add(107, new LocalClient.PacketHandler(ReceiveTime));
        }

        private static void SendTCPData(Packet _packet)
        {
            ClientSend.bytesSent += _packet.Length();
            ClientSend.packetsSent++;
            _packet.WriteLength();
            if (NetworkController.Instance.networkType == NetworkController.NetworkType.Classic)
            {
                LocalClient.instance.tcp.SendData(_packet);
                return;
            }
            SteamPacketManager.SendPacket(LocalClient.instance.serverHost.Value, _packet, P2PSend.Reliable, SteamPacketManager.NetworkChannel.ToServer);
        }

        #region Send Packets

        public static void SendInventory()
        {
            Debug.Log("SENDING INVENTORY");

            using (Packet packet = new Packet(100))
            {
                packet.Write(SteamManager.Instance.PlayerSteamIdString);

                foreach (InventoryCell cell in InventoryUI.Instance.cells)
                {
                    if (cell.currentItem)
                    {
                        int cellAmount = cell.currentItem.amount;
                        if (cellAmount > 0)
                        {
                            packet.Write((short)cell.currentItem.id);
                            packet.Write((short)cellAmount);
                        }
                    }
                    else
                    {
                        packet.Write((short)-1);
                        packet.Write((short)0);
                    }
                }

                SendTCPData(packet);
            }
        }

        public static void SendPowerups()
        {
            Debug.Log("SENDING POWERUPS");

            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

            int[] powerups = (int[])typeof(PowerupInventory).GetField("powerups", flags).GetValue(PowerupInventory.Instance);

            using (Packet packet = new Packet(101))
            {
                packet.Write(SteamManager.Instance.PlayerSteamIdString);

                foreach (int powerup in powerups)
                {
                    packet.Write((short)powerup);
                }

                SendTCPData(packet);
            }
        }

        public static void SendPosition()
        {
            Debug.Log("SENDING POSITION");

            float[] position = new float[3];

            position[0] = PlayerMovement.Instance.transform.position.x;
            position[1] = PlayerMovement.Instance.transform.position.y;
            position[2] = PlayerMovement.Instance.transform.position.z;

            using (Packet packet = new Packet(102))
            {
                packet.Write(SteamManager.Instance.PlayerSteamIdString);

                packet.Write(position[0]);
                packet.Write(position[1]);
                packet.Write(position[2]);

                SendTCPData(packet);
            }
        }

        public static void SendPlayerStatus()
        {
            Debug.Log("SENDING PLAYERSTATUS");

            using (Packet packet = new Packet(103))
            {
                packet.Write(SteamManager.Instance.PlayerSteamIdString);

                packet.Write(PlayerStatus.Instance.hp);
                packet.Write(PlayerStatus.Instance.maxHp);
                packet.Write(PlayerStatus.Instance.stamina);
                packet.Write(PlayerStatus.Instance.maxStamina);
                packet.Write(PlayerStatus.Instance.shield);
                packet.Write(PlayerStatus.Instance.maxShield);
                packet.Write(PlayerStatus.Instance.hunger);
                packet.Write(PlayerStatus.Instance.maxHunger);
                packet.Write(PlayerStatus.Instance.draculaStacks);

                SendTCPData(packet);
            }
        }

        public static void SendArmor()
        {
            Debug.Log("SENDING ARMOR");

            using (Packet packet = new Packet(104))
            {
                packet.Write(SteamManager.Instance.PlayerSteamIdString);

                for (int i = 0; i < 4; i++)
                {
                    if (InventoryUI.Instance.armorCells[i].currentItem)
                    {
                        packet.Write((short)InventoryUI.Instance.armorCells[i].currentItem.id);
                    }
                    else
                    {
                        packet.Write((short)-1);
                    }

                }

                if (InventoryUI.Instance.arrows.currentItem)
                {
                    packet.Write((short)InventoryUI.Instance.arrows.currentItem.id);
                    packet.Write(InventoryUI.Instance.arrows.currentItem.amount);
                }
                else
                {
                    packet.Write((short)-1);
                    packet.Write(-1);
                }

                SendTCPData(packet);
            }
        }

        public static void SendPlayerReady()
        {
            using (Packet packet = new Packet(105))
            {
                packet.Write(SteamManager.Instance.PlayerSteamIdString);

                SendTCPData(packet);
            }
        }

        #endregion

        #region Receive Packets

        public static void ReceiveServerHasSave(Packet _packet)
        {
            Debug.Log("Server Has Save Received!");
            LoadManager.serverHasSaveLoaded = _packet.ReadBool(true);
        }

        public static void HandleSave(Packet _packet)
        {
            Debug.Log("Send Save Received!");

            SendInventory();
            SendPowerups();
            SendPosition();
            SendPlayerStatus();
            SendArmor();
        }

        private static void ReceiveInventory(Packet _packet)
        {
            Debug.Log("Inventory Received!");

            InventoryItem[] allScriptableItems = ItemManager.Instance.allScriptableItems;

            int count = 0;

            while (_packet.UnreadLength() >= 2)
            {
                int id = _packet.ReadShort(true);
                int amount = _packet.ReadShort(true);

                if (id != -1)
                {
                    InventoryUI.Instance.cells[count].ForceAddItem(allScriptableItems[id], amount);
                }
                
                count++;
            }

            InventoryUI.Instance.UpdateAllCells();
            Hotbar.Instance.UpdateHotbar();
        }


        private static void ReceivePowerups(Packet _packet)
        {
            Debug.Log("Powerups Received!");

            List<int> powerups = new List<int>();

            while (_packet.UnreadLength() >= 2)
            {
                int amount = _packet.ReadShort(true);

                powerups.Add(amount);
            }

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            typeof(PowerupInventory).GetField("powerups", flags).SetValue(PowerupInventory.Instance, powerups.ToArray());

            for (int i = 0; i < powerups.Count; i++)
            {
                for (int j = 0; j < powerups[i]; j++)
                {
                    PowerupUI.Instance.AddPowerup(i);
                }
            }   
        }

        private static void ReceivePosition(Packet _packet)
        {
            Debug.Log("Position Received!");

            Vector3 receivedPosition = new Vector3();

            receivedPosition.x = _packet.ReadFloat(true);
            receivedPosition.y = _packet.ReadFloat(true);
            receivedPosition.z = _packet.ReadFloat(true);

            PlayerMovement.Instance.transform.position = receivedPosition;
        }

        private static void ReceivePlayerStatus(Packet _packet)
        {
            Debug.Log("Player Status Received!");

            PlayerStatus.Instance.hp = _packet.ReadFloat(true);
            PlayerStatus.Instance.maxHp = _packet.ReadInt(true);
            PlayerStatus.Instance.stamina = _packet.ReadFloat(true);
            PlayerStatus.Instance.maxStamina = _packet.ReadFloat(true);
            PlayerStatus.Instance.shield = _packet.ReadFloat(true);
            PlayerStatus.Instance.maxShield = _packet.ReadInt(true);
            PlayerStatus.Instance.hunger = _packet.ReadFloat(true);
            PlayerStatus.Instance.maxHunger = _packet.ReadFloat(true);

            PlayerStatus.Instance.draculaStacks = _packet.ReadInt(true);

            PlayerStatus.Instance.UpdateStats();

            if (PlayerStatus.Instance.hp <= 0)
            {
                try
                {
                    ClientSend.PlayerHitObject(1, 1,1, new Vector3(), 1);
                    WorldTimer timer = new GameObject("World Timer", new[] { typeof(WorldTimer) }).GetComponent<WorldTimer>();

                    timer.StartPlayerDeath(0.3f);
                }
                catch
                {
                    var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

                    typeof(PlayerStatus).GetMethod("PlayerDied", flags).Invoke(PlayerStatus.Instance, new object[] { 0, -1 });
                }
            }
        }

        private static void ReceiveArmor(Packet _packet)
        {
            Debug.Log("Armor Received!");

            InventoryItem[] allScriptableItems = ItemManager.Instance.allScriptableItems;

            OtherInput.Instance.ToggleInventory(OtherInput.CraftingState.Inventory);
            for (int i = 0; i < 4; i++)
            {
                short id = _packet.ReadShort(true);

                if (id != -1)
                {
                    InventoryUI.Instance.AddArmor(allScriptableItems[id]);
                    PlayerStatus.Instance.UpdateArmor(i, id);
                }
            }
            OtherInput.Instance.ToggleInventory(OtherInput.CraftingState.Inventory);

            short arrowId = _packet.ReadShort(true);
            int arrowAmount = _packet.ReadInt(true);

            if (arrowId != -1)
            {
                InventoryUI.Instance.arrows.ForceAddItem(allScriptableItems[arrowId], arrowAmount);
            }
        }

        private static void ReceiveTime(Packet _packet)
        {
            Debug.Log("Time Receieved!");

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            DayCycle.time = _packet.ReadFloat(true);
            typeof(DayCycle).GetProperty("totalTime", flags).SetValue(null, _packet.ReadFloat(true));
        }

        #endregion 

        [HarmonyPatch(typeof(GameManager), "SpawnPlayer")]
        [HarmonyPostfix]
        static void PlayerSpawned(int id, string username, Color color, Vector3 position, float orientationY)
        {
            if (!LocalClient.serverOwner)
            {
                Debug.Log("Not Owner!");
                if (LoadManager.serverHasSaveLoaded)
                {
                    Debug.Log("Has Save!");
                    if (id == LocalClient.instance.myId)
                    {
                        Debug.Log("Sending Player Ready!");

                        SendPlayerReady();
                    }
                }
            }
        }
    }
}
