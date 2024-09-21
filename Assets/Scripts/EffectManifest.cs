using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class EffectManifest : MonoBehaviour
    {
        [Serializable]
        public class EffectItem
        {
            public GameObject prefab;
            public string path;
        }
        public string[] effectPrefabFindPaths = new string[] { "Assets/Art/Effects", };
        public List<EffectItem> effectList = new List<EffectItem>();
    }
}
