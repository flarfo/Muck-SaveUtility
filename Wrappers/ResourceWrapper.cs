using System;
using UnityEngine;
using System.Collections.Generic;

namespace SaveUtility
{
    [Serializable]
    class ResourceWrapper
    {
        public string resourceName;
        public float[] position;
        public float[] rotation;

        internal ResourceWrapper(GameObject originalResource)
        {
            resourceName = originalResource.name;

            position = new float[3];

            position[0] = originalResource.transform.position.x;
            position[1] = originalResource.transform.position.y;
            position[2] = originalResource.transform.position.z;

            rotation = new float[3];

            rotation[0] = originalResource.transform.eulerAngles.x;
            rotation[1] = originalResource.transform.eulerAngles.y;
            rotation[2] = originalResource.transform.eulerAngles.z;
        }
    }
}
