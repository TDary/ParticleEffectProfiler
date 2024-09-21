using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static Assets.Scripts.Tool;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

namespace Assets.Scripts
{
    public class PlayController : MonoBehaviour
    {
        //public VFXManifest vfxListAsset;
        public Text _count;
        public Text _effectname;
        [HideInInspector]
        public Transform effects_CachePoolRoot;
        public BoxCollider simulateRange;
        public EffectManifest _effectmanifest;
        private int _runningCounts = 0;
        private Coroutine _restartCoroutine;
        private List<GameObject> _runnningEffects = new List<GameObject>();
        private GameObjectCachePool<int> _effectCachPool;
        private GameObject _currentEffectobj;
        private bool _useCache = true;   //默认使用缓存     
        public bool loop = false; //特效是否循环播放
        private bool isBeginPlay = false;  //特效是否开始播放
        [Range(1, 10)]
        public int effectRunTime = 3;
        private float beginTime = 0f;
        public bool useCache
        {
            get { return _useCache; }
            set
            {
                if (_useCache != value)
                {
                    ClearCache();
                    _useCache = value;
                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if(loop)
                DoLoopPlay();
            if (isBeginPlay) 
            {
                beginTime += Time.deltaTime;
                if(beginTime > effectRunTime)
                {
                    DoStopPlay();
                    beginTime = 0f;
                    isBeginPlay = false;
                }
            }
        }
        void DoLoopPlay()
        {
            foreach (GameObject prefab in _runnningEffects)
            {
                var allparticles = prefab.GetComponentsInChildren<ParticleSystem>();
                foreach (var particle in allparticles)
                {
                    if(!particle.isPlaying)
                        particle.Play();
                }
            }
        }

        void DoStopPlay()
        {
            foreach (GameObject prefab in _runnningEffects)
            {
                var allparticles = prefab.GetComponentsInChildren<ParticleSystem>();
                var allvfxs = prefab.GetComponentsInChildren<VisualEffect>();
                foreach (var particle in allparticles)
                {
                    particle.Stop();
                }
                foreach (var vfx in allvfxs)
                {
                    vfx.Stop();
                }
            }
        }

        public void Refresh(int count = 0)
        {
            if(_effectCachPool!=null)
                _effectCachPool.Destroy();

            _effectCachPool = new GameObjectCachePool<int>(effects_CachePoolRoot);
            ChangeVFXCount(count);
        }

        bool ChangeCurrentEffect(string infoName = "")
        {
            if (string.IsNullOrEmpty(infoName))
            {
                if (_effectname != null)
                {
                    var inputPrefabName = _effectname.text.ToString();
                    if (_effectmanifest != null)
                    {
                        foreach (var effect in _effectmanifest.effectList)
                        {
                            if (effect.prefab.gameObject.name.Equals(inputPrefabName))
                            {
                                _currentEffectobj = effect.prefab;
                                return true;
                            }
                        }
                        return false;
                    }
                    else
                    {
                        Debug.LogError("特效Manifest为空");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError("特效名不能为空");
                    return false;
                }
            }
            else
            {
                if (_effectmanifest != null)
                {
                    foreach (var effect in _effectmanifest.effectList)
                    {
                        if (effect.prefab.gameObject.name.Equals(infoName))
                        {
                            _currentEffectobj = effect.prefab;
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    Debug.LogError("特效Manifest为空");
                    return false;
                }
            }
        }

        GameObject SpawnOne()
        {
            if (_currentEffectobj == null)
                return null;

            GameObject vfx = Instantiate(_currentEffectobj);

            if (!vfx)
            {
                vfx = Instantiate(_currentEffectobj);
                vfx.SetActive(true);
            }

            if (EffectSimulateRange.instance != null)
                EffectSimulateRange.instance.RandomInRange(vfx.transform);
            else
            {
                Vector3 outPos;
                Quaternion outRot;
                EffectSimulateRange.instance.RandomInBox(simulateRange.center, Quaternion.identity, simulateRange.size, false, out outPos, out outRot);
                vfx.transform.SetParent(simulateRange.transform, false);
                vfx.transform.localPosition = outPos;
                vfx.transform.localRotation = outRot;
            }

            return vfx;
        }

        public void ChangeVFXCount(int count)
        {
            if (_count)
                _count.text = count.ToString();

            if (count > _runningCounts)
            {
                for (int i = _runningCounts; i < count; ++i)
                {
                    GameObject vfx;
                    if (i >= _runnningEffects.Count)
                    {
                        vfx = SpawnOne();
                        _runnningEffects.Add(vfx);
                    }
                    else
                    {
                        vfx = _runnningEffects[i];
                        if (!vfx)
                        {
                            vfx = SpawnOne();
                            _runnningEffects[i] = vfx;
                        }
                        else
                            vfx.SetActive(true);
                    }
                }
                isBeginPlay = true;
            }
            else if (count < _runningCounts)
            {
                for (int i = count; i < _runningCounts; ++i)
                {
                    var vfx = _runnningEffects[i];
                    if (vfx)
                    {
                        if (_useCache)
                            vfx.SetActive(false);
                        else
                        {
                            Destroy(vfx);
                            _runnningEffects[i] = null;
                        }
                    }
                }
            }
            _runningCounts = count;
        }

        public void Play()
        {
            if (!ChangeCurrentEffect())
                return;
            if (_runningCounts > 0)
                RestartPlay();
            else
            {
                if (int.TryParse(_count.text, out int counts))
                {
                    for (int i = _runningCounts; i < counts; ++i)
                    {
                        GameObject vfx;
                        if (i >= _runnningEffects.Count)
                        {
                            vfx = SpawnOne();
                            _runnningEffects.Add(vfx);
                        }
                        else
                        {
                            vfx = _runnningEffects[i];
                            if (!vfx)
                            {
                                vfx = SpawnOne();
                                _runnningEffects[i] = vfx;
                            }
                            else
                                vfx.SetActive(true);
                        }
                    }
                    _runningCounts = counts;
                    isBeginPlay=true;
                }
            }
        }

        public void RestartPlay()
        {
            StopAllCoroutines();
            _restartCoroutine = StartCoroutine(DoRestartPlay());
        }

        IEnumerator DoRestartPlay()
        {
            for (int i = 0; i < _runningCounts; ++i)
            {
                var vfx = _runnningEffects[i];
                if (vfx)
                {
                    vfx.SetActive(false);
                    yield return null;
                    vfx.SetActive(true);
                }
            }
            _restartCoroutine = null;
        }

        public void ClearCache()
        {
            for (int i = _runningCounts; i < _runnningEffects.Count; ++i)
            {
                var vfx = _runnningEffects[i];
                if (vfx)
                {
                    Destroy(vfx);
                    _runnningEffects[i] = null;
                }
            }
            _effectCachPool.Clear();
        }

    }
}
