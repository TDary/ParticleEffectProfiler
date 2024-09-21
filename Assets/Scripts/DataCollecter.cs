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
        List<int> collectedFps = new List<int>();
        List<float> Memorys = new List<float>();
        List<int> DrawCalls = new List<int>();
        List<int> SetPassCalls = new List<int>();
        List<int> OverDraws = new List<int>();
        List<int> ParticlesCount = new List<int>();
        List<float> FrameTimes = new List<float>();

        private void Start()
        {
            
        }

        private void Update()
        {
            
        }

        public void BeginCollect()
        {
            isBeginCollect = true;
        }

        public void ClearData()
        {
            collectedFps.Clear();
            collectedFps.Clear();
            Memorys.Clear();
            DrawCalls.Clear();
            SetPassCalls.Clear();
            OverDraws.Clear();
            ParticlesCount.Clear();
            FrameTimes.Clear();
        }
    }
}
