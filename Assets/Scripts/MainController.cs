using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

namespace Assets.Scripts
{
    public class MainController : MonoBehaviour
    {
        public TMP_InputField _count;
        public TMP_InputField _effectname;
        public Transform UICanava;
        public Camera _camera;
        [HideInInspector]
        public Transform effects_CachePoolRoot;
        public BoxCollider simulateRange;
        public EffectManifest _effectmanifest;
        [HideInInspector]
        public int _runningCounts = 0;
        [HideInInspector]
        public float _currentPrefabMaxDistance;
        private Coroutine _restartCoroutine;
        private List<GameObject> _runnningEffects = new List<GameObject>();
        [HideInInspector]
        public Dictionary<int, ParticleSystem> _runningParticles = new Dictionary<int, ParticleSystem>();
        private Dictionary<int, VisualEffect> _runningVfx = new Dictionary<int, VisualEffect>();
        [HideInInspector]
        public GameObject _currentEffectobj;
        public bool loop = false; //特效是否循环播放
        bool isBeginPlay = false;  //特效是否开始播放
        bool is_currentHasLineAndTrail=false;
        bool autoBeginOne = false;
        [Range(1, 20)]
        public int effectRunTime = 5;
        public float moveSpeed = 10;
        public bool enableCollect = false;
        //[Header("ULODLevel值")]
        //public ULODLevel ulod;
        float beginTime = 0f;
        Canvas ui_canvas;
        DataCollecter _dataCollecter;
        Bounds _moveRange;
        Vector3 _moveTargetPos;
        // Start is called before the first frame update
        void Start()
        {
            ui_canvas = UICanava.GetComponent<Canvas>();
            _dataCollecter = GameObject.Find("EffectsProfiler").GetComponent<DataCollecter>();
            _moveRange = new Bounds(simulateRange.center, simulateRange.size);
            _moveTargetPos = simulateRange.center;
        }

        // Update is called once per frame
        void Update()
        {
            if (loop)
                DoLoopPlay();
            if (isBeginPlay && isBeginPlay)
            {
                beginTime += Time.deltaTime;
                if (beginTime > effectRunTime)
                {
                    DoStopPlay();
                    beginTime = 0f;
                    isBeginPlay = false;
                    autoBeginOne = false;
                }
            }
            if (moveSpeed != 0 && isBeginPlay)
            {
                //判断不存在粒子特效的时候，才去移动（有LineRender以及拖尾等）
                if (is_currentHasLineAndTrail)
                {
                    var moveDist = _moveTargetPos - simulateRange.transform.position;
                    var moveDelta = moveSpeed * Time.deltaTime;
                    if (moveDelta * moveDelta > moveDist.sqrMagnitude)
                    {
                        _moveTargetPos = new Vector3(
                        UnityEngine.Random.Range(_moveRange.min.x, _moveRange.max.x),
                        UnityEngine.Random.Range(_moveRange.min.y, _moveRange.max.y),
                        UnityEngine.Random.Range(_moveRange.min.z, _moveRange.max.z)
                        );
                        simulateRange.transform.position = _moveTargetPos;
                    }
                    else
                        simulateRange.transform.position += moveDist.normalized * moveDelta;
                }
            }
        }
        #region 逻辑
        /// <summary>
        /// 是否有拖尾和线条特效
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        bool IsHasLineAndTrail(GameObject prefab)
        {
            var allLine = prefab.GetComponentsInChildren<LineRenderer>();
            var allTrail = prefab.GetComponentsInChildren<TrailRenderer>();
            if (allLine.Length != 0 || allTrail.Length != 0)
                return true;
            else
                return false;
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
                        autoBeginOne = false;
                        return false;
                    }
                }
                else
                {
                    Debug.LogError("特效名不能为空");
                    autoBeginOne = false;
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
                    autoBeginOne = false;
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
            is_currentHasLineAndTrail = IsHasLineAndTrail(vfx);
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
            is_currentHasLineAndTrail = IsHasLineAndTrail(vfx);

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
                            CacheEffectComponent(vfx);
                        }
                        else
                        {
                            vfx = _runnningEffects[i];
                            if (!vfx)
                            {
                                vfx = SpawnOne();
                                CacheEffectComponent(vfx);
                                _runnningEffects[i] = vfx;
                            }
                            else
                                vfx.SetActive(true);
                        }
                    }
                    PlayRemain();
                    _runningCounts = count;
                    isBeginPlay = true;
                }
                else if (changeObj != null && changeObj.gameObject.name != _currentEffectobj.gameObject.name)
                {
                    _currentEffectobj = changeObj;
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
                    for (int i = _runnningEffects.Count - 1; i >= count; --i)
                    {
                        var vfx = _runnningEffects[i];
                        if (vfx)
                        {
                            _runnningEffects.RemoveAt(i);
                            DestroyImmediate(vfx);
                        }
                    }
                    PlayRemain();
                    _runningCounts = count;
                    isBeginPlay = true;
                }
                else
                {
                    _currentEffectobj = changeObj;
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
                if (changeObj == null)
                {
                    ClearCache();
                    _currentEffectobj = null;
                    _runningCounts = 0;
                }
                else if (changeObj.gameObject.name == _currentEffectobj.gameObject.name)
                {
                    RestartPlay();
                    isBeginPlay = true;
                }
                else
                {
                    _currentEffectobj = changeObj;
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
        #endregion

        #region 相机控制与转换
        ///// <summary>
        ///// 设置相机可视范围为指定Box范围内（包裹住）
        ///// </summary>
        //void FitCameraToBox()
        //{
        //    if (simulateRange == null)
        //    {
        //        Debug.LogError("Box Collider not set");
        //        return;
        //    }
        //    simulateRange.size = GetEffectBound(_currentEffectobj);
        //    Vector3 size = simulateRange.size;
        //    float maxSize = Mathf.Max(size.x, size.z);
        //    float cameraSize = maxSize / (2 * _camera.orthographicSize);
        //    _camera.orthographicSize = cameraSize / 2;
        //}

        //Vector3 GetEffectBound(GameObject prefab)
        //{
        //    Vector3 bound = Vector3.zero;
        //    var effectPartilces = prefab.GetComponentsInChildren<ParticleSystem>();
        //    var trails = prefab.GetComponentsInChildren<TrailRenderer>();
        //    if (effectPartilces.Length != 0)
        //    {
        //        foreach (ParticleSystem particle in effectPartilces)
        //        {
        //        }
        //    }
        //    if(trails.Length != 0)
        //    {
        //        foreach (TrailRenderer trail in trails)
        //        {
        //            trail.bounds
        //        }
        //    }
        //    return bound;
        //}
        #endregion

        #region 播放设置
        public void AutoRun(int count)
        {
            try
            {
                _dataCollecter._segmentStoring = true;
                _dataCollecter.EndOneCollectData = true;
                StartCoroutine(AutoRunEffect(count));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// 自动化调用的播放
        /// </summary>
        /// <returns></returns>
        IEnumerator AutoRunEffect(int count)
        {
            if (count <= 0)
                yield return new Exception("实例个数不正确");
            else
            {
                int runIndex = 0;
                while (true)
                {
                    //循环检测判断状态，播放完一个接下一个
                    if (!autoBeginOne && _dataCollecter.EndOneCollectData)
                    {
                        if (SwitchCurrentEffectForAuto(ref runIndex, count))
                        {
                            //pass
                        }
                        else
                        {
                            _dataCollecter.isAutoEnd = true;
                            _dataCollecter.TryEndOutPut();
                            break;
                        }
                    }
                    yield return null;
                }
            }
            yield return null;
        }

        /// <summary>
        /// 切换当前特效去播放
        /// </summary>
        bool SwitchCurrentEffectForAuto(ref int runIndex, int count)
        {
            if (runIndex < _effectmanifest.effectList.Count)
            {
                AutoPlay(count, _effectmanifest.effectList[runIndex].prefab);
                runIndex++;
                return true;
            }
            else
            {
                Debug.Log("全部特效已运行完毕，结束当前任务");
                return false;
            }
        }

        /// <summary>
        /// 单特效播放实际协程
        /// </summary>
        /// <returns></returns>
        IEnumerator RunEffect(int counts, GameObject effectObj)
        {
            if (effectObj == null)
            {
                Debug.LogError("传入的GameObject为空");
                autoBeginOne = false;
                ui_canvas.gameObject.SetActive(true);
                _dataCollecter.EndOneCollectData = true;
#if UNITY_EDITOR
                SetGameViewMaximized(false);
#endif
                yield return null;
            }
            else
            {
#if UNITY_EDITOR
                SetGameViewMaximized(true);
#endif
                Debug.Log("Simulate will be start after 2 seconds...");
                Debug.Log($"Simulate：{effectObj.gameObject.name}");
                yield return new WaitForSeconds(2.0f);
                if (counts > 0)
                {
                    if (_currentEffectobj == null)
                    {
                        _currentEffectobj = effectObj;
                        if (enableCollect)
                        {
                            _dataCollecter.BeginCollect(_camera);
                        }
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
                        if (enableCollect)
                        {
                            _dataCollecter.BeginCollect(_camera);
                        }
                        ChangeVFXCount(counts, effectObj);
                    }
                }
                else if (counts == 0)
                {
                    if (_currentEffectobj == null)
                    {
                        Debug.Log("未开始播放特效");
                        ui_canvas.gameObject.SetActive(true);
                        autoBeginOne = false;
#if UNITY_EDITOR
                        SetGameViewMaximized(false);
#endif
                    }
                    else
                    {
                        DoStopPlay();
                        ClearCache();  //清除特效
                        _runningCounts = counts;
                        autoBeginOne = false;
                    }
                }
                else
                {
                    ui_canvas.gameObject.SetActive(true);
                    autoBeginOne = false;
#if UNITY_EDITOR
                    SetGameViewMaximized(false);
#endif
                    Debug.LogError("实例数不能小于0");
                }
                yield return null;
            }
        }


        /// <summary>
        /// 单特效播放实际协程
        /// </summary>
        /// <returns></returns>
        IEnumerator RunEffect(int counts, string effectname)
        {
#if UNITY_EDITOR
            SetGameViewMaximized(true);
#endif
            Debug.Log("Simulate will be start after 2 seconds...");
            yield return new WaitForSeconds(2.0f);
            if (counts > 0)
            {
                if (_currentEffectobj == null)
                {
                    if (ChangeCurrentEffect(effectname))
                    {
                        if (enableCollect)
                        {
                            _dataCollecter.BeginCollect(_camera);
                        }
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
                        autoBeginOne = false;
#if UNITY_EDITOR
                        SetGameViewMaximized(false);
#endif
                        Debug.LogError("输入EffectName无效");
                    }
                }
                else
                {
                    if (enableCollect)
                    {
                        _dataCollecter.BeginCollect(_camera);
                    }
                    ChangeVFXCount(counts, FindGameObj(effectname));
                }
            }
            else if (counts == 0)
            {
                if (_currentEffectobj == null)
                {
                    Debug.Log("未开始播放特效");
                    ui_canvas.gameObject.SetActive(true);
                    autoBeginOne = false;
#if UNITY_EDITOR
                    SetGameViewMaximized(false);
#endif
                }
                else
                {
                    DoStopPlay();
                    ClearCache();  //清除特效
                    _runningCounts = counts;
                    autoBeginOne = false;
                }
            }
            else
            {
                ui_canvas.gameObject.SetActive(true);
                autoBeginOne = false;
#if UNITY_EDITOR
                SetGameViewMaximized(false);
#endif
                Debug.LogError("实例数不能小于0");
            }
            yield return null;
        }

        /// <summary>
        /// 单特效播放实际协程
        /// </summary>
        /// <returns></returns>
        IEnumerator RunEffect()
        {
#if UNITY_EDITOR
            SetGameViewMaximized(true);
#endif
            Debug.Log("Simulate will be start after 2 seconds...");
            yield return new WaitForSeconds(2.0f);
            int counts = 0;
            if (int.TryParse(_count.text, out counts))
            {
                if (counts > 0)
                {
                    if (_currentEffectobj == null)
                    {
                        if (ChangeCurrentEffect())
                        {
                            if (enableCollect)
                            {
                                _dataCollecter.BeginCollect(_camera);
                            }
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
                        if (enableCollect)
                        {
                            _dataCollecter.BeginCollect(_camera);
                        }
                        ChangeVFXCount(counts, FindGameObj(_effectname.text));
                    }
                }
                else if (counts == 0)
                {
                    if (_currentEffectobj == null)
                    {
                        Debug.Log("未开始播放特效");
                        ui_canvas.gameObject.SetActive(true);
                        autoBeginOne = false;
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
            List<int> willDeleteparticle = new List<int>();
            List<int> willDeleteVfx = new List<int>();
            foreach (var particle in _runningParticles.Keys)
            {
                if (_runningParticles[particle] != null)
                    _runningParticles[particle].Play();
                else
                    willDeleteparticle.Add(particle);
            }
            foreach (var vfx in _runningVfx.Keys)
            {
                if (_runningVfx[vfx] != null)
                    _runningVfx[vfx].Play();
                else
                    willDeleteVfx.Add(vfx);
            }
            if (willDeleteparticle.Count != 0)
            {
                foreach (var item in willDeleteparticle)
                    _runningParticles.Remove(item);
            }
            if (willDeleteVfx.Count != 0)
            {
                foreach (var item in willDeleteVfx)
                    _runningVfx.Remove(item);
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
                if (particle != null && particle.isPlaying)
                    particle.Stop();
            }
            foreach (var vfx in _runningVfx.Values)
            {
                if (vfx != null)
                    vfx.Stop();
            }
            if (enableCollect)
                _dataCollecter.StopCollect();
            ui_canvas.gameObject.SetActive(true);
#if UNITY_EDITOR
            SetGameViewMaximized(false);
#endif
        }

        public void AutoPlay(int count, GameObject effectObj)
        {
            try
            {
                autoBeginOne = true;
                ui_canvas.gameObject.SetActive(false);
                StartCoroutine(RunEffect(count, effectObj));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// 播放单特效
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
        /// 重播
        /// </summary>
        public void RestartPlay()
        {
            if (_restartCoroutine != null)
                StopCoroutine(_restartCoroutine);
            _restartCoroutine = StartCoroutine(DoRestartPlay());
        }

        /// <summary>
        /// 重播协程
        /// </summary>
        /// <returns></returns>
        IEnumerator DoRestartPlay()
        {
            foreach (var item in _runningParticles.Values)
            {
                if (!item.isPlaying)
                    item.Play();
            }
            foreach (var vfx in _runningVfx.Values)
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
                if (!_runningParticles.ContainsKey(particle.GetInstanceID()))
                {
                    _runningParticles.Add(particle.GetInstanceID(), particle);
                }
            }
            foreach (var vfx in allvfxs)
            {
                if (!_runningVfx.ContainsKey(vfx.GetInstanceID()))
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
                    if (!_runningParticles.ContainsKey(particle.GetInstanceID()))
                    {
                        _runningParticles.Add(particle.GetInstanceID(), particle);
                    }
                }
                foreach (var vfx in allvfxs)
                {
                    if (!_runningVfx.ContainsKey(vfx.GetInstanceID()))
                    {
                        _runningVfx.Add(vfx.GetInstanceID(), vfx);
                    }
                }
            }
        }

        public void ClearCache()
        {
            for (int i = _runnningEffects.Count - 1; i >= 0; --i)  //从后往前清除
            {
                var vfx = _runnningEffects[i];
                if (vfx)
                {
                    DestroyImmediate(vfx);
                    _runnningEffects.RemoveAt(i);
                }
            }
            _runningParticles.Clear();
            _runningVfx.Clear();
        }
        #endregion
    }
}
