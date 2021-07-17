using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveUtility
{
    public static class CustomSaveData
    {
        internal static SerializableDictionary<string, SerializableDictionary<string, object>> customSaves = new SerializableDictionary<string, SerializableDictionary<string, object>>();

        public static object GetSaveValue(string GUID, string key)
        {
            if (customSaves.ContainsKey(GUID))
            {
                if (customSaves[GUID].ContainsKey(key))
                {
                    return customSaves[GUID][key];
                }
            }

            return null;
        }

        public static void AddToSave(string GUID, string key, object value)
        {
            if (customSaves.ContainsKey(GUID))
            {
                if (customSaves[GUID].ContainsKey(key))
                {
                    return;
                }

                customSaves[GUID].Add(key, value);

                return;
            }

            customSaves.Add(GUID, new SerializableDictionary<string, object>());
            customSaves[GUID].Add(key, value);
        }
    }
}
