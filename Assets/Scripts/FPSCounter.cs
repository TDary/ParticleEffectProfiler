using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class FPSCounter : MonoBehaviour
    {
        public bool switchFps = false;
        public int targetFrameRate = 60;
        private float _deltaTime = 0.0f;
        private int _fps = 0;
        private float updateInterval = 0.5f; // 更新间隔（毫秒）
        private Text fpsView;
        private float _msec;

        private void Start()
        {
            if (switchFps)
            {
                Application.targetFrameRate = targetFrameRate;
                fpsView = GameObject.Find("FPS_Counter").GetComponent<Text>();
                StartCoroutine(UpdateFPS());
            }
        }

        private IEnumerator UpdateFPS()
        {
            switchFps = true;
            while (true)
            {
                yield return new WaitForSeconds(updateInterval); // 将毫秒转换为秒
                //_msec = _deltaTime * 1000.0f;
                _fps = (int)(1.0f / _deltaTime);
                fpsView.text = string.Format("{0} fps", _fps);
            }
        }

        private void Update()
        {
            if (switchFps)
                _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }

        public void StopFps()
        {
            StopCoroutine(UpdateFPS());
            switchFps = false;
        }

        public void StartFps()
        {
            if (switchFps)
            {
                Debug.Log("已启用FPS");
                return;
            }
            Application.targetFrameRate = targetFrameRate;
            fpsView = GameObject.Find("FPS_Counter").GetComponent<Text>();
            StartCoroutine(UpdateFPS());
        }
    }
}
