using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

namespace Assets.Scripts
{
    public class MainController : MonoBehaviour
    {
        //public VFXManifest vfxListAsset;
        public Text _count;
        public Text _effectname;
        public Transform UICanava;
        [HideInInspector]
        public Transform effects_CachePoolRoot;
        public BoxCollider simulateRange;
        public EffectManifest _effectmanifest;
        [HideInInspector]
        public int _runningCounts = 0;
        private Coroutine _restartCoroutine;
        private List<GameObject> _runnningEffects = new List<GameObject>();
        [HideInInspector]
        public Dictionary<int, ParticleSystem> _runningParticles = new Dictionary<int, ParticleSystem>();
        private Dictionary<int, VisualEffect> _runningVfx = new Dictionary<int, VisualEffect>();
        [HideInInspector]
        public GameObject _currentEffectobj;
        public bool loop = false; //特效是否循环播放
        private bool isBeginPlay = false;  //特效是否开始播放
        [Range(1, 20)]
        public int effectRunTime = 3;
        public bool enableCollect = false;
        private float beginTime = 0f;
        Canvas ui_canvas;
        DataCollecter _dataCollecter;
        // Start is called before the first frame update
        void Start()
        {
            ui_canvas = UICanava.GetComponent<Canvas>();
            _dataCollecter = GameObject.Find("EffectsProfiler").GetComponent<DataCollecter>();
        }

        // Update is called once per frame
        void Update()
        {
            if (loop)
                DoLoopPlay();
            if (isBeginPlay)
            {
                beginTime += Time.deltaTime;
                if (beginTime > effectRunTime)
                {
                    DoStopPlay();
                    beginTime = 0f;
                    isBeginPlay = false;
                }
            }
        }

        public GameObject FindGameObj(string prefabName)
        {
            if (_effectmanifest != null)
            {
                foreach (var effect in _effectmanifest.effectList)
                {
                    if (effect.prefab.gameObject.name.Equals(prefabName))
                    {
                        return effect.prefab;
                    }
                }
            }
            return null;
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

        GameObject SpawnOne(GameObject obj)
        {
            GameObject vfx = Instantiate(obj);
            if (!vfx)
            {
                vfx = Instantiate(obj);
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

        public void ChangeVFXCount(int count, GameObject changeObj)
        {
            if (count > _runningCounts)
            {
                if (changeObj != null && changeObj.gameObject.name == _currentEffectobj.gameObject.name)
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
                    _runningCounts = count;
                    isBeginPlay = true;
                }
                else if (changeObj != null && changeObj.gameObject.name != _currentEffectobj.gameObject.name)
                {
                    //先清除一下缓存
                    ClearCache();
                    //重新进行实例化
                    for (int i = 0; i < count; ++i)
                    {
                        var vfx = SpawnOne(changeObj);
                        _runnningEffects.Add(vfx);
                        CacheEffectComponent(vfx);
                    }
                    PlayRemain();
                    _runningCounts = count;
                    isBeginPlay = true;
                }
                else
                    Debug.LogError("传入的GameObject为空");
            }
            else if (count < _runningCounts)
            {
                if (changeObj == null)
                {
                    ClearCache();
                    _currentEffectobj = null;
                    _runningCounts = 0;
                }
                else if (changeObj.gameObject.name == _currentEffectobj.gameObject.name)
                {
                    for (int i = count; i < _runningCounts; ++i)
                    {
                        var vfx = _runnningEffects[i];
                        if (vfx)
                        {
                            Destroy(vfx);
                            _runnningEffects[i] = null;
                        }
                    }
                    PlayRemain();
                    _runningCounts = count;
                    isBeginPlay = true;
                }
                else
                {
                    //先清除一下缓存
                    ClearCache();
                    //重新进行实例化
                    for (int i = 0; i < count; ++i)
                    {
                        var vfx = SpawnOne(changeObj);
                        _runnningEffects.Add(vfx);
                        CacheEffectComponent(vfx);
                    }
                    _runningCounts = count;
                    isBeginPlay = true;
                }
            }
            else
            {
                if (changeObj == null )
                {
                    ClearCache();
                    _currentEffectobj = null;
                    _runningCounts = 0;
                }
                else if(changeObj.gameObject.name == _currentEffectobj.gameObject.name)
                {
                    RestartPlay();
                    isBeginPlay = true;
                }
                else
                {
                    //先清除一下缓存
                    ClearCache();
                    //重新进行实例化
                    for (int i = 0; i < count; ++i)
                    {
                        var vfx = SpawnOne(changeObj);
                        _runnningEffects.Add(vfx);
                        CacheEffectComponent(vfx);
                    }
                    _runningCounts = count;
                    isBeginPlay = true;
                }
            }
        }

#if UNITY_EDITOR
        public void SetGameViewMaximized(bool value)
        {
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            EditorWindow window = EditorWindow.GetWindow(gameViewType);
            if (window != null)
                window.maximized = value;
        }
#endif

        #region 播放设置
        IEnumerator RunEffect()
        {
#if UNITY_EDITOR
            SetGameViewMaximized(true);
#endif
            Debug.Log("Simulate will be start after 5 seconds...");
            yield return new WaitForSeconds(5.0f);
            int counts = 0;
            if (int.TryParse(_count.text, out counts))
            {
                if (counts > 0)
                {
                    if (enableCollect)
                    {
                        _dataCollecter.BeginCollect();
                    }
                    if (_currentEffectobj == null)
                    {
                        if (ChangeCurrentEffect())
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
                                CacheEffectComponent(vfx);
                            }
                            _runningCounts = counts;
                            isBeginPlay = true;
                        }
                        else
                        {
                            ui_canvas.gameObject.SetActive(true);
#if UNITY_EDITOR
                            SetGameViewMaximized(false);
#endif
                            Debug.LogError("输入EffectName无效");
                        }
                    }
                    else
                    {
                        ChangeVFXCount(counts, FindGameObj(_effectname.text));
                    }
                }
                else if (counts == 0)
                {
                    if (_currentEffectobj == null)
                    {
                        Debug.Log("未开始播放特效");
                        ui_canvas.gameObject.SetActive(true);
#if UNITY_EDITOR
                        SetGameViewMaximized(false);
#endif
                    }
                    else
                    {
                        DoStopPlay();
                        ClearCache();  //清除特效
                        _runningCounts = counts;
                    }
                }
                else
                {
                    ui_canvas.gameObject.SetActive(true);
#if UNITY_EDITOR
                    SetGameViewMaximized(false);
#endif
                    Debug.LogError("实例数不能小于0");
                }
            }
            else
            {
                ui_canvas.gameObject.SetActive(true);
#if UNITY_EDITOR
                SetGameViewMaximized(false);
#endif
                Debug.LogError("实例数不能为非整形类型");
            }
            yield return null;
        }

        void PlayRemain()
        {
            foreach(var particle in _runningParticles.Keys)
            {
                if (_runningParticles[particle] != null)
                    _runningParticles[particle].Play();
                else
                    _runningParticles.Remove(particle);
            }
            foreach(var vfx in _runningVfx.Keys)
            {
                if (_runningParticles[vfx] != null)
                    _runningParticles[vfx].Play();
                else
                    _runningParticles.Remove(vfx);
            }
        }

        /// <summary>
        /// 循环播放
        /// </summary>
        void DoLoopPlay()
        {
            foreach (var particle in _runningParticles.Values)
            {
                if (!particle.isPlaying)
                    particle.Play();
            }
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        void DoStopPlay()
        {
            foreach (var particle in _runningParticles.Values)
            {
                if (particle != null&&!particle.isPlaying)
                    particle.Stop();
            }
            foreach (var vfx in _runningVfx.Values)
            {
                if(vfx != null)
                    vfx.Stop();
            }
            if(enableCollect)
                _dataCollecter.StopCollect();
            ui_canvas.gameObject.SetActive(true);
#if UNITY_EDITOR
            SetGameViewMaximized(false);
#endif
        }

        /// <summary>
        /// 正常播放单特效入口
        /// </summary>
        public void Play()
        {
            try
            {
                ui_canvas.gameObject.SetActive(false);
                StartCoroutine(RunEffect());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 重播特效入口
        /// </summary>
        public void RestartPlay()
        {
            if (_restartCoroutine != null)
                StopCoroutine(_restartCoroutine);
            _restartCoroutine = StartCoroutine(DoRestartPlay());
        }

        /// <summary>
        /// 重播特效协程
        /// </summary>
        IEnumerator DoRestartPlay()
        {
            foreach (var item in _runningParticles.Values)
            {
                if(!item.isPlaying)
                    item.Play();
            }
            foreach(var vfx in _runningVfx.Values)
            {
                vfx.Play();
            }
            yield return null;
        }

        #endregion
        #region 缓存
        void CacheEffectComponent(GameObject prefab)
        {
            var allparticles = prefab.GetComponentsInChildren<ParticleSystem>();
            var allvfxs = prefab.GetComponentsInChildren<VisualEffect>();
            foreach (var particle in allparticles)
            {
                if (!_runningParticles.TryGetValue(particle.GetInstanceID(), out ParticleSystem val))
                {
                    _runningParticles.Add(particle.GetInstanceID(), particle);
                }
            }
            foreach (var vfx in allvfxs)
            {
                if (!_runningVfx.TryGetValue(vfx.GetInstanceID(), out VisualEffect val))
                {
                    _runningVfx.Add(vfx.GetInstanceID(), vfx);
                }
            }
        }

        void CacheEffectComponentByAll(List<GameObject> allEffects)
        {
            foreach (GameObject effect in allEffects)
            {
                var allparticles = effect.GetComponentsInChildren<ParticleSystem>();
                var allvfxs = effect.GetComponentsInChildren<VisualEffect>();
                foreach (var particle in allparticles)
                {
                    if (!_runningParticles.TryGetValue(particle.GetInstanceID(), out ParticleSystem val))
                    {
                        _runningParticles.Add(particle.GetInstanceID(), particle);
                    }
                }
                foreach (var vfx in allvfxs)
                {
                    if (!_runningVfx.TryGetValue(vfx.GetInstanceID(), out VisualEffect val))
                    {
                        _runningVfx.Add(vfx.GetInstanceID(), vfx);
                    }
                }
            }
        }

        public void ClearCache()
        {
            for (int i = 0; i < _runnningEffects.Count; ++i)
            {
                var vfx = _runnningEffects[i];
                if (vfx)
                {
                    Destroy(vfx);
                    _runnningEffects[i] = null;
                }
            }
            _runningParticles.Clear();
            _runningVfx.Clear();
        }
        #endregion
    }
}
