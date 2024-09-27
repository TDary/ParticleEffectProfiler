using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace Assets.Scripts
{
    public class DataCollecter : MonoBehaviour
    {
        public MainController _mainController;
        [HideInInspector]
        public bool isBeginCollect = false;   //开始采集信号
        [HideInInspector]
        public bool isEndCollect = false;   //结束采集信号
        bool isAutoCollect = false;   //自动采集
        bool isAutoEnd = false;  //自动采集结束
        ProfilerRecorder drawCallsRecorder;
        ProfilerRecorder setPassCallsRecorder;
        ProfilerRecorder verticesRecorder;
        EffectEvla m_EffectEvla;
        class DetailData
        {
            public List<int> collectedFps = new List<int>();
            public List<long> DrawCalls = new List<long>();
            public List<long> SetPassCalls = new List<long>();
            public List<int> OverDraws = new List<int>();
            public List<int> ParticlesCount = new List<int>();
            public List<float> FrameTimes = new List<float>();
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
            public float frameTImetp90;
            public int fpsTp90;
            public long maxVertex;
            public long maxDrawCall;
            public long maxSetPassCall;
            public int maxOverDraw;
            public int maxParticles;
        }
        #endregion
        SimpleCount sc_Data;
        DetailCount dc_Data;
        List<string> allResultData = new List<string>(100);
        float _deltaTime;
        [HideInInspector]
        public bool _segmentStoring = false;   //分段存储结果数据
        private void OnEnable()
        {
            setPassCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
        }

        private void OnDisable()
        {
            setPassCallsRecorder.Dispose();
            drawCallsRecorder.Dispose();
            verticesRecorder.Dispose();
        }

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        public void BeginCollect()
        {
            isBeginCollect = true;
            isEndCollect = false;
            //初始化
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
            else
                ClearDatailData();
            if (dc_Data.Equals(default(SimpleCount)))
            {
                dc_Data = new DetailCount();
                InitDetailData();
            }
            else
                InitDetailData();
            if(m_EffectEvla == null)
                m_EffectEvla = new EffectEvla(Camera.main);
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
                    break;
                }
                else if (isBeginCollect)
                {
                    //开始采集数据
                    long drawcalls = drawCallsRecorder.CurrentValue - 1;  //摄像机Call去掉
                    long setpasscalls = setPassCallsRecorder.CurrentValue - 1;  //摄像机Call去掉
                    long vertexs = verticesRecorder.CurrentValue - 4;  //空场景顶点
                    float frameTime = Time.unscaledDeltaTime;
                    int particleCount = GetRealTimeParticles();//获取实时粒子数
                    int fps = (int)(1.0f / _deltaTime);
                    data.DrawCalls.Add(drawcalls);
                    data.SetPassCalls.Add(setpasscalls);
                    data.VertexCount.Add(vertexs);
                    data.FrameTimes.Add(frameTime);
                    data.ParticlesCount.Add(particleCount);
                    data.collectedFps.Add(fps);
                    int overdraw = m_EffectEvla.UpdateGetOverDraw();
                    if(overdraw > 0)
                        data.OverDraws.Add(overdraw);
                }
                else
                {
                    Debug.Log("未开始采集，不需要结束");
                    break;
                }
                yield return new WaitForEndOfFrame();
            }
            yield return null;
        }

        int GetRealTimeParticles()
        {
            int m_ParticleCount = 0;
            foreach (var ps in _mainController._runningParticles.Values)
            {
                m_ParticleCount += ps.particleCount;
            }
            return m_ParticleCount;
        }

        #region 获取数据
        private void GetDetailData()
        {
            float frameTImetp90 = GetTop90ForFloat(data.FrameTimes);
            int fpsTp90 = GetTop90ForInt(data.collectedFps);
            long maxVertex = GetMaxLongValue(data.VertexCount);
            long maxDrawCall = GetMaxLongValue(data.DrawCalls);
            long maxSetPassCall = GetMaxLongValue(data.SetPassCalls);
            int maxOverDraw = GetMaxIntValue(data.OverDraws);
            int maxParticles = GetMaxIntValue(data.ParticlesCount);
            dc_Data.maxDrawCall = maxDrawCall;
            dc_Data.frameTImetp90 = frameTImetp90;
            dc_Data.maxSetPassCall = maxSetPassCall;
            dc_Data.maxVertex = maxVertex;
            dc_Data.maxOverDraw = maxOverDraw;
            dc_Data.maxParticles = maxParticles;
            dc_Data.fpsTp90 = fpsTp90;
        }

        float GetMaxFloatValue(List<float> alldata)
        {
            alldata.Sort();
            return alldata[alldata.Count - 1];
        }

        int GetMaxIntValue(List<int> alldata)
        {
            alldata.Sort();
            return alldata[alldata.Count-1];
        }

        long GetMaxLongValue(List<long> alldata)
        {
            alldata.Sort();
            return alldata[alldata.Count - 1];
        }

        int GetTop90ForInt(List<int> alldata)
        {
            alldata.Sort();
            double number = 0.9 * alldata.Count;
            int index = (int)number;
            return alldata[index];
        }

        float GetTop90ForFloat(List<float> alldata)
        {
            alldata.Sort();
            double number = 0.9 * alldata.Count;
            int index = (int)number;
            return alldata[index];
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
                    materialSet.Add(mat);
                    shaderSet.Add(mat.shader);
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

        public void OutputData(string filepath="")
        {
            string result = $"{_mainController._currentEffectobj.name},{dc_Data.fpsTp90},{dc_Data.frameTImetp90},{dc_Data.maxParticles},{dc_Data.maxOverDraw},{dc_Data.maxDrawCall}," +
                        $"{dc_Data.maxSetPassCall},{dc_Data.maxVertex},{sc_Data.ShadowsCount},{sc_Data.MaterialsCount},{sc_Data.ShadersCount}," +
                        $"{sc_Data.TransformCount},{sc_Data.CollidersCount},{sc_Data.AnimatorsCount},{sc_Data.AnimatorNull},{sc_Data.Prefab_instanceCount}";
            allResultData.Add(result);
            if (string.IsNullOrEmpty(filepath))
            {
                filepath = Path.Combine(Application.persistentDataPath, $"EffectPerformance_{DateTime.Now.ToString("hh_mm_ss")}.csv");
            }
            if (_segmentStoring)
            {
                if (allResultData.Count == 100 || isAutoEnd)
                {
                    //写入数据至存储文件
                    WriteToFile(allResultData, filepath);
                    //清空
                    allResultData.Clear();
                }
            }
            else
            {
                //写入数据至存储文件
                WriteToFile(allResultData, filepath);
                //清空
                allResultData.Clear();
            }
        }

        void WriteToFile(List<string> datas,string filepath)
        {
            try
            {
                using (var sw = new StreamWriter(new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite), new System.Text.UTF8Encoding(true)))
                {
                    sw.WriteLine("PrefabName,FPS TP90,FrameTimeTp90,MaxParticleCount,MaxOverDraw,MaxDrawCall,MaxSetPassCall,MaxVertex,ShadowsCount,MaterialsCount,ShadersCount," +
                        "TransformCount,CollidersCount,AnimatorsCount,AnimatorNullCount,Prefab_instanceCount");
                    sw.Flush();
                    foreach (var data in datas)
                    {
                        sw.WriteLine(data);
                    }
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
            dc_Data.frameTImetp90 = 0;
            dc_Data.fpsTp90 = 0;
            dc_Data.maxVertex = 0;
            dc_Data.maxDrawCall = 0;
            dc_Data.maxSetPassCall = 0;
            dc_Data.maxOverDraw = 0;
            dc_Data.maxParticles = 0;
        }

        public void ClearDatailData()
        {
            data.collectedFps.Clear();
            data.collectedFps.Clear();
            data.DrawCalls.Clear();
            data.SetPassCalls.Clear();
            data.OverDraws.Clear();
            data.ParticlesCount.Clear();
            data.FrameTimes.Clear();
            data.VertexCount.Clear();
        }
        #endregion
    }
}
