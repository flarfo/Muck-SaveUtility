using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Steamworks;

namespace SaveUtility
{
    [HarmonyPatch]
    internal class ServerMethods
    {
        [HarmonyPatch(typeof(Server), "InitializeServerPackets")]
        [HarmonyPostfix]
        static void InitializeCustomPackets()
        {
            Debug.Log("SERVER DATA INITIALIZED");

            Server.PacketHandlers.Add(100, new Server.PacketHandler(ReceiveInventory));
            Server.PacketHandlers.Add(101, new Server.PacketHandler(ReceivePowerups));
            Server.PacketHandlers.Add(102, new Server.PacketHandler(ReceivePosition));
            Server.PacketHandlers.Add(103, new Server.PacketHandler(ReceivePlayerStatus));
            Server.PacketHandlers.Add(104, new Server.PacketHandler(ReceiveArmor));
            Server.PacketHandlers.Add(105, new Server.PacketHandler(ReceivePlayerReady));
        }

        //SteamPacketManager
        //LocalClient packethandlers / initializeclientdata()
        //LocalClient HandleData(byte[] data)
        //handledata takes in a packet, and reads received data. num = packet.readint is casted to a serverpacket in serverpackets (enum)
        //LocalClient then passes the key, num, to the packethandlers dictionary, and runs the returned method, passing in the packet

        //initialize packet with specific packethandler method key    => Packet packet = new Packet(47)
        //write data to that packet   => packet.write(data)
        //send the packet    => SendTCPDataToAll(packet));
        private static void SendTCPDataToAll(Packet _packet)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

            _packet.WriteLength();
            if (NetworkController.Instance.networkType == NetworkController.NetworkType.Classic)
            {
                for (int i = 1; i < Server.MaxPlayers; i++)
                {
                    Server.clients[i].tcp.SendData(_packet);
                }
                return;
            }
            foreach (Client client in Server.clients.Values)
            {
                if (((client != null) ? client.player : null) != null)
                {
                    Debug.Log("Sending packet to id: " + client.id);

                    var tcpVariant = typeof(ServerSend).GetField("TCPvariant", flags);

                    SteamPacketManager.SendPacket(client.player.steamId.Value, _packet, (P2PSend)tcpVariant.GetValue("TCPVariant"), SteamPacketManager.NetworkChannel.ToClient);
                }
            }
        }

        private static void SendTCPDataToAll(int exceptClient, Packet _packet)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

            _packet.WriteLength();
            if (NetworkController.Instance.networkType == NetworkController.NetworkType.Classic)
            {
                for (int i = 1; i < Server.MaxPlayers; i++)
                {
                    if (i != exceptClient)
                    {
                        Server.clients[i].tcp.SendData(_packet);
                    }
                }
                return;
            }
            foreach (Client client in Server.clients.Values)
            {
                if (((client != null) ? client.player : null) != null && SteamLobby.steamIdToClientId[client.player.steamId.Value] != exceptClient)
                {
                    var tcpVariant = typeof(ServerSend).GetField("TCPvariant", flags);

                    SteamPacketManager.SendPacket(client.player.steamId.Value, _packet, (P2PSend)tcpVariant.GetValue("TCPVariant"), SteamPacketManager.NetworkChannel.ToClient);
                }
            }
        }

        private static void SendTCPData(int toClient, Packet _packet)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
            var tcpVariant = typeof(ServerSend).GetField("TCPvariant", flags);

            Packet packet2 = new Packet();
            packet2.SetBytes(_packet.CloneBytes());
            packet2.WriteLength();
            if (NetworkController.Instance.networkType == NetworkController.NetworkType.Classic)
            {
                Server.clients[toClient].tcp.SendData(packet2);
                return;
            }
            SteamPacketManager.SendPacket(Server.clients[toClient].player.steamId.Value, packet2, (P2PSend)tcpVariant.GetValue("TCPVariant"), SteamPacketManager.NetworkChannel.ToClient);
        }

        public static void SendPlayer(int toClient, PlayerWrapper player)
        {
            if (player.position.Length > 0)
            {
                SendPosition(toClient, player.position);
                GameManager.players[toClient].transform.position = new Vector3(player.position[0], player.position[1], player.position[2]);
            }

            if (player.inventory.Any())
            {
                SendInventory(toClient, player.inventory);
            }

            if (player.powerups.Any())
            {
                SendPowerups(toClient, player.powerups);
            }

            SendPlayerStatus(toClient, player.health, player.maxHealth, player.stamina, player.maxStamina, player.shield, player.maxShield, player.hunger, player.maxHunger, player.draculaHpIncrease);

            if (player.armor.Length > 0)
            {
                SendArmor(toClient, player.armor, player.arrows);
            }
        }

        #region Send Packets
        public static void SendHasSave()
        {
            using (Packet packet = new Packet(100))
            {
                packet.Write(true);
                SendTCPDataToAll(0, packet);
            }
        }

        public static void SendServerSave()
        {
            using (Packet packet = new Packet(101))
            {
                SendTCPDataToAll(0, packet);
            }
        }

        public static void SendInventory(int toClient, List<SerializableTuple<int, int>> inventory)
        { 
            using (Packet packet = new Packet(102))
            {
                foreach (Tuple<int, int> cell in inventory)
                {
                    packet.Write((short)cell.Item1);
                    packet.Write((short)cell.Item2);
                }

                SendTCPData(toClient, packet);
            }
        }

        public static void SendPowerups(int toClient, List<int> powerups)
        {
            using (Packet packet = new Packet(103))
            {
                foreach (int powerup in powerups)
                {
                    packet.Write((short)powerup);
                }

                SendTCPData(toClient, packet);
            }
        }

        public static void SendPosition(int toClient, float[] position)
        {
            using (Packet packet = new Packet(104))
            {
                packet.Write(position[0]);
                packet.Write(position[1]);
                packet.Write(position[2]);

                SendTCPData(toClient, packet);
            }
        }

        public static void SendPlayerStatus(int toClient, float hp, int maxhp, float stamina, float maxstamina, float shield, int maxshield, float hunger, float maxhunger, int draculastacks) // add playerstatus (maybe convert all variables to a single array?
        {
            using (Packet packet = new Packet(105))
            {
                packet.Write(hp);
                packet.Write(maxhp);
                packet.Write(stamina);
                packet.Write(maxstamina);
                packet.Write(shield);
                packet.Write(maxshield);
                packet.Write(hunger);
                packet.Write(maxhunger);
                packet.Write(draculastacks);

                SendTCPData(toClient, packet);
            }
        }

        public static void SendArmor(int toClient, int[] armor, Tuple<int,int> arrows)
        {
            using (Packet packet = new Packet(106))
            {
                packet.Write((short)armor[0]);
                packet.Write((short)armor[1]);
                packet.Write((short)armor[2]);
                packet.Write((short)armor[3]);

                packet.Write((short)arrows.Item1);
                packet.Write(arrows.Item2);

                SendTCPData(toClient, packet);
            }
        }

        public static void SendTime(int toClient, float time, float totalTime)
        {
            using (Packet packet = new Packet(107))
            {
                packet.Write(time);
                packet.Write(totalTime);

                SendTCPData(toClient, packet);
            }
        }

        #endregion

        #region Receive Packets

        private static void ReceiveInventory(int fromClient, Packet _packet)
        {
            Debug.Log("PACKET RECEIVED");
            Debug.Log("CLIENT: " + fromClient);

            string steamId = _packet.ReadString(true);

            Debug.Log("STEAMID: " + steamId);

            List<SerializableTuple<int, int>> inventory = new List<SerializableTuple<int, int>>();

            while (_packet.UnreadLength() >= 2)
            {
                inventory.Add(new Tuple<int, int>(_packet.ReadShort(true), _packet.ReadShort(true)));
            }

            if (!LoadManager.players.ContainsKey(steamId))
            {
                LoadManager.players.Add(steamId, new PlayerWrapper());
                LoadManager.players[steamId].inventory = inventory;
            }
            else
            {
                LoadManager.players[steamId].inventory = inventory;
            }
        }

        private static void ReceivePowerups(int fromClient, Packet _packet)
        {
            Debug.Log("PACKET RECEIVED");
            Debug.Log("CLIENT: " + fromClient);

            string steamId = _packet.ReadString(true);

            Debug.Log("STEAMID: " + steamId);

            List<int> powerups = new List<int>();

            while (_packet.UnreadLength() >= 2)
            {
                powerups.Add(_packet.ReadShort(true));
            }

            if (!LoadManager.players.ContainsKey(steamId))
            {
                LoadManager.players.Add(steamId, new PlayerWrapper());
                LoadManager.players[steamId].powerups = powerups;
            }
            else
            {
                LoadManager.players[steamId].powerups = powerups;
            }
        }

        private static void ReceivePosition(int fromClient, Packet _packet)
        {
            Debug.Log("PACKET RECEIVED");
            Debug.Log("CLIENT: " + fromClient);

            string steamId = _packet.ReadString(true);

            Debug.Log("STEAMID: " + steamId);

            float[] position = new float[3];

            position[0] = _packet.ReadFloat(true);
            position[1] = _packet.ReadFloat(true);
            position[2] = _packet.ReadFloat(true);

            if (!LoadManager.players.ContainsKey(steamId))
            {
                LoadManager.players.Add(steamId, new PlayerWrapper());
                LoadManager.players[steamId].position = position;
            }
            else
            {
                LoadManager.players[steamId].position = position;
            }
        }

        private static void ReceivePlayerStatus(int fromClient, Packet _packet)
        {
            Debug.Log("PACKET RECEIVED");
            Debug.Log("CLIENT: " + fromClient);

            string steamId = _packet.ReadString(true);

            Debug.Log("STEAMID: " + steamId);

            float health = _packet.ReadFloat(true);
            int maxHealth = _packet.ReadInt(true);
            float stamina = _packet.ReadFloat(true);
            float maxStamina = _packet.ReadFloat(true);
            float shield = _packet.ReadFloat(true);
            int maxShield = _packet.ReadInt(true);
            float hunger = _packet.ReadFloat(true);
            float maxHunger = _packet.ReadFloat(true);

            int draculaHpIncrease = _packet.ReadInt(true);

            if (!LoadManager.players.ContainsKey(steamId))
            {
                LoadManager.players.Add(steamId, new PlayerWrapper());

                LoadManager.players[steamId].health = health;
                LoadManager.players[steamId].maxHealth = maxHealth;
                LoadManager.players[steamId].stamina = stamina;
                LoadManager.players[steamId].maxStamina = maxStamina;
                LoadManager.players[steamId].shield = shield;
                LoadManager.players[steamId].maxShield = maxShield;
                LoadManager.players[steamId].hunger = hunger;
                LoadManager.players[steamId].maxHunger= maxHunger;
                LoadManager.players[steamId].draculaHpIncrease = draculaHpIncrease;
            }
            else
            {
                LoadManager.players[steamId].health = health;
                LoadManager.players[steamId].maxHealth = maxHealth;
                LoadManager.players[steamId].stamina = stamina;
                LoadManager.players[steamId].maxStamina = maxStamina;
                LoadManager.players[steamId].shield = shield;
                LoadManager.players[steamId].maxShield = maxShield;
                LoadManager.players[steamId].hunger = hunger;
                LoadManager.players[steamId].maxHunger = maxHunger;
                LoadManager.players[steamId].draculaHpIncrease = draculaHpIncrease;
            }
        }

        private static void ReceiveArmor(int fromClient, Packet _packet)
        {
            Debug.Log("PACKET RECEIVED");
            Debug.Log("CLIENT: " + fromClient);

            string steamId = _packet.ReadString(true);

            Debug.Log("STEAMID: " + steamId);

            if (!LoadManager.players.ContainsKey(steamId))
            {
                LoadManager.players.Add(steamId, new PlayerWrapper());

                int[] armor = new int[] { _packet.ReadShort(), _packet.ReadShort(), _packet.ReadShort(), _packet.ReadShort() } ;

                LoadManager.players[steamId].armor = armor;
                LoadManager.players[steamId].arrows = new Tuple<int, int>(_packet.ReadShort(true), _packet.ReadInt(true)); 
            }
            else
            {
                int[] armor = new int[] { _packet.ReadShort(), _packet.ReadShort(), _packet.ReadShort(), _packet.ReadShort() };

                LoadManager.players[steamId].armor = armor;
                LoadManager.players[steamId].arrows = new Tuple<int, int>(_packet.ReadShort(true), _packet.ReadInt(true));
            }
        }

        private static void ReceivePlayerReady(int fromClient, Packet _packet)
        {
            Debug.Log("PACKET RECEIVED");
            Debug.Log("CLIENT: " + fromClient);

            string steamId = _packet.ReadString(true);

            Debug.Log("STEAMID: " + steamId);

            if (LoadManager.players.ContainsKey(steamId))
            {
                SendPlayer(fromClient, LoadManager.players[steamId]);
            }

            SendTime(fromClient, LoadManager.loadedSave.time, LoadManager.loadedSave.totalTime);
        }

        #endregion
    }
}
        
