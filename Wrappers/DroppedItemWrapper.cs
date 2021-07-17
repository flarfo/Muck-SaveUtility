using System;
using UnityEngine;

namespace SaveUtility
{
    [Serializable]
    public class DroppedItemWrapper
    {
        public int itemId;
        public int amount;
        public float[] position;

        public DroppedItemWrapper(GameObject originalItem)
        {
            position = new float[3];
            position[0] = originalItem.transform.position.x;
            position[1] = originalItem.transform.position.y;
            position[2] = originalItem.transform.position.z;

            itemId = originalItem.GetComponent<Item>().item.id;
            amount = originalItem.GetComponent<Item>().item.amount;
        }

        public DroppedItemWrapper()
        {

        }
    }
}
