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
        public bool loop = false; //��Ч�Ƿ�ѭ������
        private bool isBeginPlay = false;  //��Ч�Ƿ�ʼ����
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
                        Debug.LogError("��ЧManifestΪ��");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError("��Ч������Ϊ��");
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
                    Debug.LogError("��ЧManifestΪ��");
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
                    //�����һ�»���
                    ClearCache();
                    //���½���ʵ����
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
                    Debug.LogError("�����GameObjectΪ��");
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
                    //�����һ�»���
                    ClearCache();
                    //���½���ʵ����
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
                    //�����һ�»���
                    ClearCache();
                    //���½���ʵ����
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

        #region ��������
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
                            Debug.LogError("����EffectName��Ч");
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
                        Debug.Log("δ��ʼ������Ч");
                        ui_canvas.gameObject.SetActive(true);
#if UNITY_EDITOR
                        SetGameViewMaximized(false);
#endif
                    }
                    else
                    {
                        DoStopPlay();
                        ClearCache();  //�����Ч
                        _runningCounts = counts;
                    }
                }
                else
                {
                    ui_canvas.gameObject.SetActive(true);
#if UNITY_EDITOR
                    SetGameViewMaximized(false);
#endif
                    Debug.LogError("ʵ��������С��0");
                }
            }
            else
            {
                ui_canvas.gameObject.SetActive(true);
#if UNITY_EDITOR
                SetGameViewMaximized(false);
#endif
                Debug.LogError("ʵ��������Ϊ����������");
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
        /// ѭ������
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
        /// ֹͣ����
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
        /// �������ŵ���Ч���
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
        /// �ز���Ч���
        /// </summary>
        public void RestartPlay()
        {
            if (_restartCoroutine != null)
                StopCoroutine(_restartCoroutine);
            _restartCoroutine = StartCoroutine(DoRestartPlay());
        }

        /// <summary>
        /// �ز���ЧЭ��
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
        #region ����
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
