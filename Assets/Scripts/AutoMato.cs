using UnityEngine;
using Matory;
using System;
using Assets.Scripts;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using System.Collections;
using UnityEngine.Profiling;

namespace Mator
{
    public class AutoMato : MonoBehaviour
    {
        // Start is called before the first frame update
        static Mato runner = null;
        private bool swich = false;
        bool Snapcapture = false;
        double memoryMaxValue = 0;
        string memorySnapPath = string.Empty;
        void Awake()
        {
            if (runner == null)
            {
                runner = gameObject.AddComponent<Mato>();
                try
                {
                    if (runner != null)
                    {
                        runner.Init();
                        runner.m_Pro.AddMethod("gm",gmMethod);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private object gmMethod(string ip, string[] args)
        {
            try
            {
                string command = args[0];
                if (command == "FrameTargetRelease")
                {
                    Application.targetFrameRate = -1;
                    QualitySettings.vSyncCount = 0;
                }
                else if (command == "ShowFps")
                {
                    var fpseSet = FindObjectOfType<Assets.Scripts.FPSCounter>();
                    fpseSet.StartFps();
                }
                else if (command == "ShutUpFps")
                {
                    var fpseSet = FindObjectOfType<Assets.Scripts.FPSCounter>();
                    fpseSet.StopFps();
                }
                else if (command == "SetRunningTime")
                {
                    var mainControllerObj = FindObjectOfType<Assets.Scripts.MainController>();
                    if (int.TryParse(args[2], out int val))
                    {
                        mainControllerObj.effectRunTime = int.Parse(args[2]);
                    }
                }
                else if (command == "PlayEndData")
                {
                    var dataControllerObj = FindObjectOfType<Assets.Scripts.DataCollecter>();
                    if (bool.TryParse(args[2], out bool val))
                    {
                        dataControllerObj.isAutoEnd = val;
                        dataControllerObj.TryEndOutPut();
                        return "成功结束数据输出";
                    }
                }
                else if (command == "AutoEffectProfiler")
                {
                    if (args[2] != "")
                    {
                        int count = int.Parse(args[2]);
                        var ulodSwitch = FindObjectOfType<Assets.Scripts.MainController>();
                        ulodSwitch.AutoRun(count);
                    }
                }
                else if(command == "PlayEffect")
                {
                    var ulodSwitch = FindObjectOfType<Assets.Scripts.MainController>();
                    if (args[1] != "")
                    {
                        var input = GameObject.Find("EffectName");
                        var texts = input.GetComponent<UnityEngine.UI.Text>();
                        texts.text = args[1];
                    }
                    if (args[2] != "")
                    {
                        var input = GameObject.Find("count");
                        var count = input.GetComponent<UnityEngine.UI.Text>();
                        count.text = args[2];
                    }
                    ulodSwitch.Play();
                }
                else if (command == "SetEffectLoop")
                {
                    var mainControllerObj = FindObjectOfType<Assets.Scripts.MainController>();
                    if (mainControllerObj != null)
                    {
                        if (bool.TryParse(args[2], out bool val))
                        {
                            mainControllerObj.loop = val;
                        }
                    }
                }
                else if(command == "SetMaxMemory")
                {
                    if (double.TryParse(args[1], out double val))
                        memoryMaxValue = val;
                    else
                        return "传入参数不正确,请传入Double类型";
                }
                else if(command == "SetSnapPath")
                {
                    memorySnapPath = args[1];
                }
                else if(command == "GetCurrentSnapPath")
                {
                    return memorySnapPath;
                }
#if DEBUG
                else if (command == "StartMonitor")
                {
                    StartCoroutine(MonitorLogic());
                }
#endif
                else if(command == "TestPrintPath")
                {
                    Debug.Log(Path.Combine(Application.persistentDataPath, DateTimeOffset.Now.ToUnixTimeSeconds().ToString()));
                }
                else if (command == "EnableSample")
                {
                    var mainControllerObj = FindObjectOfType<Assets.Scripts.MainController>();
                    if (mainControllerObj != null)
                    {
                        if (bool.TryParse(args[2], out bool val))
                        {
                            mainControllerObj.enableCollect = val;
                        }
                    }
                }
                else if (command == "SetStoringData")
                {
                    var dataControllerObj = FindObjectOfType<Assets.Scripts.DataCollecter>();
                    if (bool.TryParse(args[2], out bool val))
                    {
                        dataControllerObj._segmentStoring = val;
                        return "成功开启数据缓存模式";
                    }
                }
                else if (command == "TimeFrame")
                {
                    Debug.Log("当前帧号：" + Time.frameCount);
                }
                return "ok";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        double GetCurrentMemory()
        {
            long memory = Profiler.GetTotalReservedMemoryLong() + Profiler.GetMonoUsedSizeLong();
            double currentMem = memory / (1024.0 * 1024.0);
            Debug.Log("当前内存占用：" + currentMem);
            return currentMem;  // MB
        }

        IEnumerator MonitorLogic()
        {
            if (memoryMaxValue == 0)
                yield return null;
            WaitForSeconds waitSecond = new WaitForSeconds(1f);
            Debug.Log("开始进行内存监控——");
            while (true)
            {
                if (GetCurrentMemory() >= memoryMaxValue)
                {
                    if (string.IsNullOrEmpty(memorySnapPath))
                        memorySnapPath = Path.Combine(Application.persistentDataPath, DateTimeOffset.Now.ToUnixTimeSeconds().ToString(), ".snap");
                    TakeMemoryProfileSnapshot(memorySnapPath);
                    break;
                }
                yield return waitSecond;  // 每秒执行一次
            }
        }

        private void MemoryCaptureCallback(string path, bool result)
        {
            Debug.Log("Take snap success.");
        }
        private void CopyDataToTexture(Texture2D tex, NativeArray<byte> byteArray)
        {
            unsafe
            {
                void* srcPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(byteArray);
                void* dstPtr = tex.GetRawTextureData<byte>().GetUnsafeReadOnlyPtr();
                UnsafeUtility.MemCpy(dstPtr, srcPtr, byteArray.Length * sizeof(byte));
            }
        }
        private void MemoryCaptureScreen(string path, bool result, Unity.Profiling.DebugScreenCapture screenCapture)
        {
            Texture2D tex = new Texture2D(screenCapture.Width, screenCapture.Height, screenCapture.ImageFormat, false);
            CopyDataToTexture(tex, screenCapture.RawImageDataReference);
            string screensavePath = Path.ChangeExtension(path, ".png");
            File.WriteAllBytes(screensavePath, tex.EncodeToPNG());
            tex.SafeDestroy();

            Debug.Log($"save screenshot to {screensavePath} success!");
            Snapcapture = result;
        }
        private void TakeMemoryProfileSnapshot(string snapFilePath)
        {
            Snapcapture = false;
            Unity.Profiling.Memory.MemoryProfiler.TakeSnapshot(snapFilePath, MemoryCaptureCallback, MemoryCaptureScreen,
                Unity.Profiling.Memory.CaptureFlags.ManagedObjects
                | Unity.Profiling.Memory.CaptureFlags.NativeObjects
                | Unity.Profiling.Memory.CaptureFlags.NativeAllocations
                | Unity.Profiling.Memory.CaptureFlags.NativeAllocationSites
                | Unity.Profiling.Memory.CaptureFlags.NativeStackTraces);
        }
    }
}
