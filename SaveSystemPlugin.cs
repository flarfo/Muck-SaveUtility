using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace SaveUtility
{
    [BepInPlugin("flarfo.saveutility","SaveUtility","0.1.0")]
    public class SaveSystemPlugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("SaveUtility");

        public void Awake()
        {
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves"));
            }

            Logger.LogInfo("Running Muck SaveUtility!");

            var assetBundle = GetAssetBundleFromResource("saveutility");

            UIManager.backgroundImage = assetBundle.LoadAsset<Texture>("Assets/Texture2D/groundCompressed.png");

            harmony.PatchAll();
        }

        public static AssetBundle GetAssetBundleFromResource(string fileName)
        {
            var execAssembly = Assembly.GetExecutingAssembly();

            var resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(fileName));

            Debug.Log($"Resource Name: {resourceName}");

            using (var stream = execAssembly.GetManifestResourceStream(resourceName))
            {
                return AssetBundle.LoadFromStream(stream);
            }
        }
    }
}
