using System;
using UnityEngine;
using System.Collections.Generic;

namespace SaveUtility
{
    [Serializable]
    public class MobWrapper
    {
        public int mobType;
        public int bossType;
        public int guardianType;

        public float multiplier;
        public float bossMultiplier;

        public float[] position;

        public MobWrapper(Mob originalMob)
        {
            //save guarding color
            if (originalMob.gameObject.GetComponent<Guardian>())
            {
                guardianType = (int)originalMob.gameObject.GetComponent<Guardian>().type;
            }
            else
            {
                guardianType = -1;
            }

            mobType = originalMob.mobType.id;
            bossType = (int)originalMob.bossType;

            multiplier = originalMob.multiplier;
            bossMultiplier = originalMob.bossMultiplier;

            position = new float[3];
            position[0] = originalMob.transform.position.x;
            position[1] = originalMob.transform.position.y;
            position[2] = originalMob.transform.position.z;
        }

        public MobWrapper()
        {

        }
    }
}
