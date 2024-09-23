using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Assets.Scripts
{
    public class DataCollecter : MonoBehaviour
    {
        public MainController _mainController;
        private static DataCollecter instance;
        [HideInInspector]
        public static DataCollecter _instance
        {
            get 
            { 
                return instance; 
            }
        }
        [HideInInspector]
        public bool isBeginCollect = false;
        [HideInInspector]
        public bool isEndCollect = false;
        private bool isAutoCollect = false;
        private IEnumerator collectEnumer;
        class DetailData
        {
            public List<int> collectedFps = new List<int>();
            public List<float> SelfMemorys = new List<float>();
            public List<float> ResourceMemorys = new List<float>();
            public List<float> ShaderMemorys = new List<float>();
            public List<int> DrawCalls = new List<int>();
            public List<int> SetPassCalls = new List<int>();
            public List<int> OverDraws = new List<int>();
            public List<int> ParticlesCount = new List<int>();
            public List<float> FrameTimes = new List<float>();
            public List<int> VertexCount = new List<int>();
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
            public float maxSelfMemory;
            public float maxResourceMemory;
            public float maxShaderMemory;
            public float frameTImetp90;
            public int fpsTp90;
            public int maxVertex;
            public int maxDrawCall;
            public int maxSetPassCall;
            public int maxOverDraw;
            public int maxParticles;
        }
        #endregion
        SimpleCount sc_Data;
        DetailCount dc_Data;
        private void Start()
        {

        }

        private void Update()
        {

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
            collectEnumer = CollcetData();
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
                if (isBeginCollect && isEndCollect)
                {
                    //结束采集，将所有数据进行处理
                    GetSimpleCount();  //获取简单数据
                    GetDetailData();    //获取均值或最大值
                }
                else if (isBeginCollect && !isEndCollect)
                {
                    //开始采集

                }
                else
                {
                    Debug.Log("未开始采集，不需要结束");
                    break;
                }
                yield return null;
            }
        }

        #region 获取数据
        private void GetDetailData()
        {
            float maxSelfMemory = GetMaxFloatValue(data.SelfMemorys);
            float maxResourceMemory = GetMaxFloatValue(data.ResourceMemorys);
            float maxShaderMemory = GetMaxFloatValue(data.ShaderMemorys);
            float frameTImetp90 = GetTop90ForFloat(data.FrameTimes);
            int fpsTp90 = GetTop90ForInt(data.collectedFps);
            int maxVertex = GetMaxIntValue(data.VertexCount);
            int maxDrawCall = GetMaxIntValue(data.DrawCalls);
            int maxSetPassCall = GetMaxIntValue(data.SetPassCalls);
            int maxOverDraw = GetMaxIntValue(data.OverDraws);
            int maxParticles = GetMaxIntValue(data.ParticlesCount);
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
                foreach (Material mat in renderer.materials)
                {
                    materialSet.Add(mat);
                    shaderSet.Add(mat.shader);
                }
            }
            foreach (ParticleSystemRenderer particle in particleSystems)
            {
                if (particle.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                    sc_Data.ShadersCount += 1;
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

        void OutputData()
        {
            //todo:WriteFile
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
            dc_Data.maxSelfMemory = 0;
            dc_Data.maxResourceMemory = 0;
            dc_Data.maxShaderMemory = 0;
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
            data.SelfMemorys.Clear();
            data.ResourceMemorys.Clear();
            data.ShaderMemorys.Clear();
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
