using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Assets.Scripts
{
    public class DataCollecter : MonoBehaviour
    {
        private static DataCollecter instance;
        public static DataCollecter _instance
        {
            get 
            { 
                return instance; 
            }
        }
        public bool isBeginCollect = false;
        private bool isAutoCollect = false;
        class DetailData
        {
            public List<int> collectedFps = new List<int>();
            public List<float> SelfMemorys = new List<float>();
            public List<float> ResourceMemorys = new List<float>();
            public List<float> ShaderMemortys = new List<float>();
            public List<int> DrawCalls = new List<int>();
            public List<int> SetPassCalls = new List<int>();
            public List<int> OverDraws = new List<int>();
            public List<int> ParticlesCount = new List<int>();
            public List<float> FrameTimes = new List<float>();
            public List<int> VertexCount = new List<int>();
        }
        DetailData data;
        #region 只计算一次
        struct SimpleCount
        {
            public int ShadersCount;
            public int TransformCount;
            public int CollidersCount;
            public int ShadowsCount;
            public int AnimatorsCount;
            public int AnimatorNull;
            public int Prefab_instanceCount;
        }
        SimpleCount sc_Data;
        #endregion
        private void Start()
        {

        }

        private void Update()
        {
            if (isBeginCollect)
            {

            }
        }

        public void BeginCollect()
        {
            isBeginCollect = true;
            //初始化
            if (sc_Data.Equals(default(SimpleCount)))
            {
                sc_Data = new SimpleCount();
                sc_Data.ShadersCount = 0;
                sc_Data.TransformCount = 0;
                sc_Data.CollidersCount = 0;
                sc_Data.ShadowsCount = 0;
                sc_Data.AnimatorsCount = 0;
                sc_Data.AnimatorNull = 0;
                sc_Data.Prefab_instanceCount = 0;
            }
            if (data == null)
                data = new DetailData();
            else
                ClearDatailData();
        }

        public void ClearDatailData()
        {
            data.collectedFps.Clear();
            data.collectedFps.Clear();
            data.SelfMemorys.Clear();
            data.ResourceMemorys.Clear();
            data.ShaderMemortys.Clear();
            data.DrawCalls.Clear();
            data.SetPassCalls.Clear();
            data.OverDraws.Clear();
            data.ParticlesCount.Clear();
            data.FrameTimes.Clear();
            data.VertexCount.Clear();
        }
    }
}
