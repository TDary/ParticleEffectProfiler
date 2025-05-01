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
        private float _msec;
        public int current_Fps { get { return _fps; } }
        private void Start()
        {
            if (switchFps)
            {
                Application.targetFrameRate = targetFrameRate;
                StartCoroutine(UpdateFPS());
            }
        }

        private void OnGUI()
        {
            if (_fps != 0)
            {
                GUIStyle style = new GUIStyle();
                style.fontSize = 20;
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(10, 10, 200, 50), string.Format("{0} fps", _fps), style);
            }
            else
            {
                GUI.Label(new Rect(10, 10, 200, 50), "FPS Counter is not started", GUI.skin.label);
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
            StartCoroutine(UpdateFPS());
        }
    }
}
