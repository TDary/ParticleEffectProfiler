using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Matory;
using System;
using UnityEditor.PackageManager;

namespace Mator
{
    public class AutoMato : MonoBehaviour
    {
        // Start is called before the first frame update
        static Mato runner = null;
        private bool swich = false;
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
    }
}
