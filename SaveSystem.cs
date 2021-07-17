using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace SaveUtility
{
    public static class SaveSystem
    {
        public static void Save(string saveName)
        {
            string savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves", saveName);
            WorldSave worldSave = new WorldSave(0);
            if (worldSave.position != null)
            {
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(WorldSave));

                    serializer.Serialize(stream, worldSave);

                    Path.ChangeExtension(savePath, "muck");
                }
            }    
        }

        public static WorldSave Load(string saveName)
        {
            Debug.Log("Loading");
            string loadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves", saveName);

            if (File.Exists(loadPath))
            {
                using (var stream = new FileStream(loadPath, FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(WorldSave));

                    WorldSave loadedWorld = (WorldSave)serializer.Deserialize(stream);

                    return loadedWorld;
                }
            }
            else
            {
                Debug.LogError($"Save File does not Exist at {loadPath}");
                return null;
            }
        }

        public static string[] GetAllSaves()
        {
            string[] saveFileNames = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves"));
            List<string> savePaths = new List<string>();

            for (int i = 0; i < saveFileNames.Length; i++)
            {
                if (Path.GetExtension(saveFileNames[i]) == ".muck")
                {
                    savePaths.Add(Path.GetFileName(saveFileNames[i]));
                }
            }

            return savePaths.ToArray();
        }
    }
}
