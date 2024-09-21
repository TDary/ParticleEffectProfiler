using Assets.Scripts;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace Assets
{
    [CustomEditor(typeof(EffectManifest))]
    public class EffectManifestEditor : OdinEditor
    {
        public static void Collect(EffectManifest effectAsset)
        {
            effectAsset.effectList.Clear();
            foreach (var findPath in effectAsset.effectPrefabFindPaths)
            {
                var vfxPrefabs = FindPrefabs(new string[] { findPath });
                foreach (var vfxprefab in vfxPrefabs)
                {
                    EffectManifest.EffectItem item = new EffectManifest.EffectItem();
                    string assetPath = AssetDatabase.GetAssetPath(vfxprefab);
                    item.prefab = vfxprefab;
                    item.path = assetPath;
                    effectAsset.effectList.Add(item);
                }
            }
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Collect Effects Asset List"))
            {
                var vfxListAsset = target as EffectManifest;
                Collect(vfxListAsset);
                Show("OK");
            }
        }

        public static void Show(string msg, bool showOnConsoleAlso = true, float duration = 1.2f)
        {
            EditorWindow activeWindow = EditorWindow.focusedWindow;
            if (!activeWindow)
            {
                string[] titles = { "Game", "Hierarchy", "Inspector", "Project" };
                for (int i = 0; i < titles.Length; ++i)
                {
                    activeWindow = EditorWindow.GetWindow<EditorWindow>(titles[i]);
                    if (activeWindow)
                        break;
                }
            }
            if (activeWindow)
            {
                activeWindow.RemoveNotification();
                activeWindow.ShowNotification(new GUIContent(msg), duration);
            }
            if (showOnConsoleAlso)
                Debug.Log(msg);
        }
        public static GameObject[] FindPrefabs(string[] assetsFindPath)
        {
            List<GameObject> result = new List<GameObject>();
            string[] allPrefabGUIDs = AssetDatabase.FindAssets("t:Prefab", assetsFindPath);
            foreach (var prefabGUID in allPrefabGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (obj)
                    result.Add(obj);
            }
            return result.ToArray();
        }
    }
}
