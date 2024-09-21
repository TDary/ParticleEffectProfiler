//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//using Kingsoft.Engine;


//namespace Assets.Scripts
//{
//    [Serializable]
//    [GroupStyle(enableField = "enabled")]
//    public class VFXAssetFindPathsFilter
//    {
//        public string name;
//        [HideInInspector]
//        public bool enabled = true;

//        [AssetFolderPathListSelector]
//        [GroupStyle(noFold = true, noHeader = true)]
//        public string[] findPaths = new string[] { "Assets/Art", };

//        [BeginPropGroup("Filter", groupStyle = "HelpBox", indent = false)]
//        //[ToolTip("Support Regular Expression")]
//        public string includeKeyword;
//        [EndPropGroup]
//        //[ToolTip("Support Regular Expression")]
//        public string excludeKeyword;

//    }

//    [CreateAssetMenu(fileName = "VFXManifest.asset", menuName = "Client Tools/VFX Profiler/create VFX Manifest for profiling")]
//    public class VFXManifest : ScriptableObject
//    {
//        [Serializable]
//        public class VFXItem
//        {
//            public GameObject prefab;
//            public ulong nameHash;
//            public string path;            
//            public string group;
//            public long localFileID;
//        }

//        public string tag;

//        [LabelAlias("单次压测创建实例数量")]
//        public int spawnCount = 50;

//        [Obsolete("use 'assetFindPathFilters' instead."), HideInInspector]
//        [AssetFolderPathListSelector]
//        public string[] effectPrefabFindPaths;

//        public VFXAssetFindPathsFilter[] assetFindPathFilters;

//        [DrawArrayElemWithoutBorder]
//        public List<VFXItem> vfxList = new List<VFXItem>();

//        private Dictionary<long, int> _prefabInstanceID2Index;

//        public VFXItem GetVFXItemByPrefabInstanceID(int prefabInstanceID)
//        {
//            var vfxList = this.vfxList;
//            var prefabInstanceID2Index = _prefabInstanceID2Index;
//            if (_prefabInstanceID2Index == null)
//            {
//                prefabInstanceID2Index = new Dictionary<long, int>();
//                for (int i = 0; i < vfxList.Count; i++)
//                    prefabInstanceID2Index.Add(vfxList[i].prefab.GetInstanceID(), i);
//                _prefabInstanceID2Index = prefabInstanceID2Index;
//            }
//            VFXItem result = null;
//            int index;
//            if (prefabInstanceID2Index.TryGetValue(prefabInstanceID, out index))
//                result = vfxList[index];
//            return result;
//        }

//        private Dictionary<long, int> _localFileID2Index;

//        public VFXItem GetVFXItemByLocalFileID(long localFileID)
//        {
//            var vfxList = this.vfxList;
//            var localFileID2Index = _localFileID2Index;
//            if(_localFileID2Index == null)
//            {
//                localFileID2Index = new Dictionary<long, int>();
//                for (int i = 0; i < vfxList.Count; i++)
//                    localFileID2Index.Add(vfxList[i].localFileID, i);
//                _localFileID2Index = localFileID2Index;
//            }
//            VFXItem result = null;
//            int index;
//            if (localFileID2Index.TryGetValue(localFileID, out index))
//                result = vfxList[index];
//            return result;
//        }

//        private Dictionary<ulong, int> _nameHash2Index;

//        public VFXItem GetVFXItemByNameHash(ulong nameHash)
//        {
//            var vfxList = this.vfxList;
//            var nameHash2Index = _nameHash2Index;
//            if(_nameHash2Index == null)
//            {
//                nameHash2Index = new Dictionary<ulong, int>();
//                for (int i = 0; i < vfxList.Count; i++)
//                {
//                    var item = vfxList[i];
//                    if (nameHash2Index.ContainsKey(item.nameHash))
//                        Debug.LogError(string.Format("VFX List Asset : Prefab with Name Hash (value = {0}, name = {1}, index = {2}) already exists",
//                            item.nameHash, item.prefab == null?"<Unknown>" : item.prefab.name, i));
//                    else
//                        nameHash2Index.Add(item.nameHash, i);
//                }
//                _nameHash2Index = nameHash2Index;
//            }

//            VFXItem result = null;
//            int index;
//            if (nameHash2Index.TryGetValue(nameHash, out index))
//                result = vfxList[index];
//            return result;
//        }

//    }
//}
