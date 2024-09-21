using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    internal static class Tool
    {
        public static void SafeDestroy(this UnityEngine.Object obj)
        {
            if (UnityEngine.Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
        }

        internal class GameObjectCache
        {
            private static readonly Vector3 VECTOR3_ZERO = Vector3.zero;
            private static readonly Vector3 VECTOR3_ONE = Vector3.one;
            private static readonly Quaternion QUATERNION_IDENTITY = Quaternion.identity;

            public Transform root { get; private set; }
            public int capacity { get; private set; }
            private LinkedList<GameObject> _cacheList = new LinkedList<GameObject>();
            private LinkedList<GameObject> _emptyList = new LinkedList<GameObject>();


            public bool deParentWhenSpawn = true;
            public bool deTransformWhenDespawn = true;
            //public bool disactivateWhenDespawn = false;

            private Vector3 _orgPosition;
            private Quaternion _orgRotation;
            private Vector3 _orgScale;


            public string name
            {
                get { return root.name; }
                set { root.name = value; }
            }

            public int Count
            {
                get { return _cacheList.Count; }
            }

            private GameObject _asset;
            private bool _assetMustExist;
            private string _defaultObjName;


            public GameObjectCache(GameObject asset, Transform root, int capacity = int.MaxValue, string defaultObjName = null)
            {
                if (root == null)
                    root = new GameObject(asset != null ? asset.name : "_GameObjectCache_").transform;

                this.root = root;
                this.capacity = capacity;
                this._asset = asset;
                this._assetMustExist = (object)asset != (object)null;
                this._defaultObjName = defaultObjName == null ? asset == null ? "GameObject" : asset.name : defaultObjName;

                if (asset != null)
                {
                    Transform transform = asset.transform;
                    _orgPosition = transform.position;
                    _orgRotation = transform.rotation;
                    _orgScale = transform.localScale;
                }
                else
                {
                    _orgPosition = VECTOR3_ZERO;
                    _orgRotation = QUATERNION_IDENTITY;
                    _orgScale = VECTOR3_ONE;
                }

                //root.gameObject.SetActive(false);
            }

            public GameObject Spawn()
            {
                GameObject gameObject = Fetch();
                if (gameObject == null)
                {
                    if (_asset == (GameObject)null)
                    {
                        if (_assetMustExist)
                            throw new Exception("Asset of GameObject Pool is null.");
                        else
                            gameObject = new GameObject(this._defaultObjName);
                    }
                    else
                    {
                        gameObject = GameObject.Instantiate<GameObject>(_asset);
                        gameObject.name = _asset.name;
                    }
                }
                return gameObject;
            }

            public void Despawn(GameObject gameObject)
            {
                Cache(gameObject);
            }

            public GameObject Fetch()
            {
                if (_cacheList.Count == 0)
                    return null;

                GameObject gameObject = null;
                while (gameObject == (GameObject)null && _cacheList.Count > 0)
                {
                    LinkedListNode<GameObject> node = _cacheList.Last;
                    gameObject = node.Value;
                    node.Value = null;
                    _cacheList.RemoveLast();
                    _emptyList.AddLast(node);
                }

                if (gameObject == null)
                    return null;

                if (deParentWhenSpawn)
                    gameObject.transform.SetParent(null, false);

                //if (disactivateWhenDespawn)
                gameObject.SetActive(true);

                return gameObject;
            }

            public void Cache(GameObject gameObject)
            {
                if (_cacheList.Count > capacity)
                    return;

                LinkedListNode<GameObject> node = null;
                if (_emptyList.Count > 0)
                {
                    node = _emptyList.Last;
                    _emptyList.RemoveLast();
                    node.Value = gameObject;
                }
                else
                    node = new LinkedListNode<GameObject>(gameObject);

                Transform transform = gameObject.transform;
                transform.SetParent(root, false);

                //if(disactivateWhenDespawn)
                {
                    gameObject.SetActive(false);
                }

                if (deTransformWhenDespawn)
                {
                    transform.localPosition = _orgPosition;
                    transform.localRotation = _orgRotation;
                    transform.localScale = _orgScale;
                }

                _cacheList.AddLast(node);
            }

            public void Clear()
            {
                foreach (var node in _cacheList)
                    node.SafeDestroy();

                _cacheList.Clear();
                _emptyList.Clear();
            }

            public void Destroy(bool destroyRoot = true)
            {
                Clear();

                if (destroyRoot)
                    this.root.gameObject.SafeDestroy();
            }
        }

        internal class GameObjectCachePool<TKey>
        {
            private Dictionary<TKey, GameObjectCache> _objCachePools = new Dictionary<TKey, GameObjectCache>();

            public int totalsGameObjCount
            {
                get
                {
                    int count = 0;
                    foreach (var cache in _objCachePools.Values)
                        count += cache.Count;
                    return count;
                }
            }

            public Transform root { get; private set; }
            public int cacheCapacity { get; private set; }

            public GameObjectCachePool(Transform root, int cacheCapacity = int.MaxValue)
            {
                if (root == null)
                    root = new GameObject("_cache_pools").transform;

                this.root = root;
                this.cacheCapacity = cacheCapacity;

                root.gameObject.SetActive(false);
            }

            public GameObject Fetch(TKey key)
            {
                GameObjectCache cache;
                if (!_objCachePools.TryGetValue(key, out cache))
                    return null;

                return cache.Fetch();
            }

            public void Cache(TKey key, GameObject o)
            {
                GameObjectCache cache;
                if (!_objCachePools.TryGetValue(key, out cache))
                {
                    string cacheName = key + "_cache";
                    Transform cacheRoot = root.Find(cacheName);
                    cache = new GameObjectCache(null, cacheRoot, cacheCapacity);
                    cache.root.SetParent(this.root, false);
                    _objCachePools.Add(key, cache);
                }

                cache.Despawn(o);
            }

            public void Clear(TKey key)
            {
                GameObjectCache cache;
                if (_objCachePools.TryGetValue(key, out cache))
                {
                    cache.Clear();
                }
            }

            public void Clear()
            {
                foreach (var kvp in _objCachePools)
                    kvp.Value.Clear();

                _objCachePools.Clear();
            }

            public void Destroy()
            {
                Clear();

                if (root != null)
                {
                    foreach (Transform child in root)
                        child.SafeDestroy();
                }
            }
        }

    }
}
