using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.VFX;

namespace Assets.Scripts
{
    public class DataCollecter : MonoBehaviour
    {
        [HideInInspector]
        public MainController _mainController;
        [HideInInspector]
        public bool isBeginCollect = false;   //开始采集信号
        [HideInInspector]
        public bool isEndCollect = true;   //结束采集信号
        [HideInInspector]
        public bool isAutoEnd = false;  //自动采集结束
        [HideInInspector]
        public bool EndOneCollectData = true;  //一个数据是否采集完毕
        ProfilerRecorder drawCallsRecorder;
        ProfilerRecorder setPassCallsRecorder;
        ProfilerRecorder verticesRecorder;
        FrameTiming[] timing;
//#if UNITY_EDITOR
//        EffectEvla m_EffectEvla;
//#endif
        class DetailData
        {
            public List<int> collectedFps = new List<int>();
            public List<double> collectedGpuTime = new List<double>();
            public List<long> DrawCalls = new List<long>();
            public List<long> SetPassCalls = new List<long>();
            public List<int> OverDraws = new List<int>();
            public List<int> ParticlesCount = new List<int>();
            public List<long> VertexCount = new List<long>();
        }
        DetailData data;
        #region 结构体数据
        struct SimpleCount
        {
            public int ShadersCount;
            public int MaterialsCount;
            public int TransformCount;
            public int CollidersCount;
            public int ShadowsCount;
            public int AnimatorsCount;
            public int AnimatorNull;
            public int Prefab_instanceCount;
        }
        struct DetailCount
        {
            public int fpsTp90;
            public double gpuTimeTp90;
            public long maxVertex;
            public long maxDrawCall;
            public long maxSetPassCall;
            public int maxOverDraw;
            public int maxParticles;
            public Vector3 maxBoundSize;
            public uint maxCapacity;
            public float maxCameraDistance;
        }
        #endregion
        SimpleCount sc_Data;
        DetailCount dc_Data;
        List<string> allResultData = new List<string>(100);
        public bool _segmentStoring = false;   //分段存储结果数据(到100就写一个文件)
        StreamWriter sw;
        string res_filepath;
        FPSCounter fpsCounter;
        private void OnEnable()
        {
            setPassCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        }
        private void Start()
        {
            fpsCounter = GameObject.Find("FPS_Counter").GetComponent<FPSCounter>();
        }

        private void OnDisable()
        {
            setPassCallsRecorder.Dispose();
            drawCallsRecorder.Dispose();
            verticesRecorder.Dispose();
        }

        public void BeginCollect(Camera _camera,string filepath = null)
        {
            isAutoEnd = false;
            isBeginCollect = true;
            isEndCollect = false;
            EndOneCollectData = false;
            //初始化
            if (string.IsNullOrEmpty(filepath) && string.IsNullOrEmpty(res_filepath))
            {
                filepath = Path.Combine(Application.persistentDataPath, $"ParticleEffectData_{DateTime.Now.ToString("hh_mm_ss")}.csv");
                res_filepath = filepath;

            }
            if (sw == null)
                sw = new StreamWriter(new FileStream(res_filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), new System.Text.UTF8Encoding(true));
            if (sc_Data.Equals(default(SimpleCount)))
            {
                sc_Data = new SimpleCount();
                InitSimpleData();
            }
            else
                InitSimpleData();
            if (data == null)
            {
                data = new DetailData();
            }
            if (dc_Data.Equals(default(SimpleCount)))
            {
                dc_Data = new DetailCount();
                InitDetailData();
            }
            else
                InitDetailData();
            if (timing==null)
                timing = new FrameTiming[1];
//#if UNITY_EDITOR
//            if (m_EffectEvla == null)
//                m_EffectEvla = new EffectEvla(_camera);
//#endif
            StartCoroutine(CollcetData());
        }

        public void StopCollect()
        {
            isBeginCollect = false;
            isEndCollect = true;
        }

        IEnumerator CollcetData()
        {
            while (true)
            {
                if (isEndCollect)
                {
                    //结束采集，将所有数据进行处理
                    GetSimpleCount();  //获取简单数据
                    GetDetailData();    //获取均值或最大值
                    OutputData();
                    //清除数组
                    ClearDatailData();
                    break;
                }
                else if (isBeginCollect)
                {
                    //开始采集数据
                    long drawcalls = drawCallsRecorder.CurrentValue - 1;  //摄像机Call去掉
                    long setpasscalls = setPassCallsRecorder.CurrentValue - 1;  //摄像机Call去掉
                    long vertexs = verticesRecorder.CurrentValue - 4;  //空场景顶点
                    int particleCount = GetRealTimeParticles();//获取实时粒子数
                    int fps = fpsCounter.current_Fps;
                    FrameTimingManager.GetLatestTimings(1, timing);
                    data.DrawCalls.Add(drawcalls);
                    data.SetPassCalls.Add(setpasscalls);
                    data.VertexCount.Add(vertexs);
                    data.ParticlesCount.Add(particleCount);
                    data.collectedFps.Add(fps);
                    data.collectedGpuTime.Add(timing[0].gpuFrameTime);
//#if UNITY_EDITOR
//                    int overdraw = m_EffectEvla.UpdateGetOverDraw();
//                    if(overdraw > 0)
//                        data.OverDraws.Add(overdraw);
//#endif
                }
                else
                {
                    Debug.Log("未开始采集，不需要结束");
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
            EndOneCollectData = true;
            yield return null;
        }

        Vector3 GetEffectBoundSize()
        {
            Vector3 boundsize = Vector3.zero;
            var effectRenders = _mainController._currentEffectobj.GetComponentsInChildren<Renderer>();
            var allvisualEffects = _mainController._currentEffectobj.GetComponentsInChildren<VisualEffect>();
            foreach (var render in effectRenders)
            {
                if (render.bounds.size.x > boundsize.x || render.bounds.size.y > boundsize.y || render.bounds.size.z > boundsize.z)
                    boundsize = render.bounds.size;
            }
            var tarilRenderers = _mainController._currentEffectobj.GetComponentsInChildren<TrailRenderer>();
            foreach (var render in tarilRenderers)
            {
                if (render.bounds.size.x > boundsize.x || render.bounds.size.y > boundsize.y || render.bounds.size.z > boundsize.z)
                    boundsize = render.bounds.size;
            }
            if (allvisualEffects.Length > 0)
            {
                foreach (var vfx in allvisualEffects)
                {
                    List<string> allNameCache = new List<string>();
                    vfx.GetParticleSystemNames(allNameCache);
                    foreach (var name in allNameCache)
                    {
                        try
                        {
                            var info = vfx.GetParticleSystemInfo(name);
                            if (info.bounds.size.x > boundsize.x || info.bounds.size.y > boundsize.y || info.bounds.size.z > boundsize.z)
                                boundsize = info.bounds.size;
                            if (info.capacity > dc_Data.maxCapacity)
                                dc_Data.maxCapacity = info.capacity;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                }
            }
            return boundsize;
        }

        int GetRealTimeParticles()
        {
            int m_ParticleCount = 0;
            foreach (var ps in _mainController._runningParticles.Values)
            {
                if (ps != null)
                    m_ParticleCount += ps.particleCount;
            }
            return m_ParticleCount;
        }

        #region 获取数据
        private void GetDetailData()
        {
            int fpsTp90 = GetTop90ForInt(data.collectedFps);
            double tp90GpuTime = GetTop90GpuTime(data.collectedGpuTime);
            long maxVertex = data.VertexCount.Max();
            long maxDrawCall = data.DrawCalls.Max();
            long maxSetPassCall = data.SetPassCalls.Max();
//#if UNITY_EDITOR
//            if (data.OverDraws.Count > 0)
//            {
//                int maxOverDraw = data.ParticlesCount.Max();
//                dc_Data.maxOverDraw = maxOverDraw;
//            }
//#endif
            if (data.ParticlesCount.Count > 0)
            {
                int maxParticles = data.ParticlesCount.Max();
                dc_Data.maxParticles = maxParticles;
            }
            dc_Data.maxDrawCall = maxDrawCall;
            dc_Data.maxSetPassCall = maxSetPassCall;
            dc_Data.maxVertex = maxVertex;
            dc_Data.fpsTp90 = fpsTp90;
            dc_Data.gpuTimeTp90 = tp90GpuTime;
            dc_Data.maxBoundSize = GetEffectBoundSize();
        }

        int GetTop90ForInt(List<int> alldata)
        {
            if (alldata.Count == 0)
                return 0;
            if (alldata.Count == 1)
                return alldata[0];
            alldata.Sort();
            double number = 0.9 * alldata.Count;
            int index = (int)number;
            return alldata[index - 1];
        }

        double GetTop90GpuTime(List<double> alldata)
        {
            if (alldata.Count == 0)
                return 0;
            if (alldata.Count == 1)
                return alldata[0];
            alldata.Sort();
            double number = 0.9 * alldata.Count;
            int index = (int)number;
            return alldata[index - 1];
        }

        float GetTop90ForFloat(List<float> alldata)
        {
            if (alldata.Count == 0)
                return 0;
            if (alldata.Count == 1)
                return alldata[0];
            alldata.Sort();
            double number = 0.9 * alldata.Count;
            int index = (int)number;
            return alldata[index - 1];
        }

        void GetSimpleCount()
        {
            HashSet<Shader> shaderSet = new HashSet<Shader>();
            HashSet<Material> materialSet = new HashSet<Material>();
            Renderer[] renderers = _mainController._currentEffectobj.GetComponentsInChildren<Renderer>();
            ParticleSystemRenderer[] particleSystems = _mainController._currentEffectobj.GetComponentsInChildren<ParticleSystemRenderer>();
            Transform[] transforms = _mainController._currentEffectobj.GetComponentsInChildren<Transform>();
            Collider[] colliders = _mainController._currentEffectobj.GetComponentsInChildren<Collider>();
            Animator[] animators = _mainController._currentEffectobj.GetComponentsInChildren<Animator>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat != null)
                    {
                        materialSet.Add(mat);
                        if (mat.shader != null)
                            shaderSet.Add(mat.shader);
                    }
                }
            }
            foreach (ParticleSystemRenderer particle in particleSystems)
            {
                if (particle.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                    sc_Data.ShadowsCount += 1;
            }
            foreach (Animator animator in animators)
            {
                if (animator.avatar == null || animator.runtimeAnimatorController == null)
                    sc_Data.AnimatorNull += 1;
            }
            sc_Data.MaterialsCount = materialSet.Count;
            sc_Data.ShadersCount = shaderSet.Count;
            sc_Data.TransformCount = transforms.Length;
            sc_Data.CollidersCount = colliders.Length;
            sc_Data.AnimatorsCount = animators.Length;
            sc_Data.Prefab_instanceCount = _mainController._runningCounts;
        }

        /// <summary>
        /// 最后一个结束并输出结果数据
        /// </summary>
        /// <param name="filepath"></param>
        public void TryEndOutPut()
        {
            if (allResultData.Count != 0)
            {
                if (_segmentStoring)
                {
                    if (allResultData.Count == 100 || isAutoEnd)
                    {
                        //写入数据至存储文件
                        WriteToFile(allResultData);
                        //清空
                        allResultData.Clear();
                        DisposeFileObject();
                    }
                }
                else
                {
                    //写入数据至存储文件
                    WriteToFile(allResultData);
                    //清空
                    allResultData.Clear();
                    DisposeFileObject();
                }
            }
        }

        public void OutputData()
        {
            string result = $"{_mainController._currentEffectobj.name},{dc_Data.fpsTp90},{dc_Data.gpuTimeTp90},{dc_Data.maxParticles},{dc_Data.maxOverDraw},{dc_Data.maxDrawCall},{dc_Data.maxSetPassCall},{dc_Data.maxVertex},{sc_Data.ShadowsCount},{sc_Data.MaterialsCount},{sc_Data.ShadersCount},{sc_Data.TransformCount},{sc_Data.CollidersCount},{sc_Data.AnimatorsCount},{sc_Data.AnimatorNull},{dc_Data.maxBoundSize.x}-{dc_Data.maxBoundSize.y}-{dc_Data.maxBoundSize.z},{dc_Data.maxCapacity},{dc_Data.maxCameraDistance},{sc_Data.Prefab_instanceCount}";
            allResultData.Add(result);
            if (_segmentStoring)
            {
                if (allResultData.Count == 100 || isAutoEnd)
                {
                    //写入数据至存储文件
                    WriteToFile(allResultData);
                    //清空
                    allResultData.Clear();
                    DisposeFileObject();
                }
            }
            else
            {
                //写入数据至存储文件
                WriteToFile(allResultData);
                //清空
                allResultData.Clear();
                DisposeFileObject();
            }
        }

        void WriteToFile(List<string> datas)
        {
            try
            {
                sw.WriteLine("PrefabName,FPS TP90,GPUTime_Tp90,MaxParticleCount,MaxOverDraw,MaxDrawCall,MaxSetPassCall,MaxVertex,ShadowsCount,MaterialsCount,ShadersCount,TransformCount,CollidersCount,AnimatorsCount,AnimatorNullCount,MaxBoundSize,MaxCapacity,MaxULOD_Distance,Prefab_instanceCount");
                foreach (var data in datas)
                {
                    sw.WriteLine(data);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        #endregion

        #region 初始化以及清除
        public void InitSimpleData()
        {
            sc_Data.ShadersCount = 0;
            sc_Data.TransformCount = 0;
            sc_Data.CollidersCount = 0;
            sc_Data.ShadowsCount = 0;
            sc_Data.AnimatorsCount = 0;
            sc_Data.AnimatorNull = 0;
            sc_Data.Prefab_instanceCount = 0;
        }

        public void InitDetailData()
        {
            dc_Data.fpsTp90 = 0;
            dc_Data.gpuTimeTp90 = 0;
            dc_Data.maxVertex = 0;
            dc_Data.maxDrawCall = 0;
            dc_Data.maxSetPassCall = 0;
            dc_Data.maxOverDraw = 0;
            dc_Data.maxParticles = 0;
            dc_Data.maxBoundSize = Vector3.zero;
            dc_Data.maxCapacity = 0;
            //dc_Data.maxCameraDistance = 0;
        }

        public void ClearDatailData()
        {
            data.collectedFps.Clear();
            data.collectedFps.Clear();
            data.DrawCalls.Clear();
            data.SetPassCalls.Clear();
            data.OverDraws.Clear();
            data.ParticlesCount.Clear();
            data.VertexCount.Clear();
        }

        public void DisposeFileObject()
        {
            sw.Flush();
            sw.Close();
            sw.Dispose();
            sw = null;
            res_filepath = null;
        }
        #endregion
    }
}
