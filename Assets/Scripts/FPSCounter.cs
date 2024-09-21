using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class FPSCounter : MonoBehaviour
    {
        private float _deltaTime = 0.0f;
        private int _fps = 0;
        private float updateInterval = 0.5f; // 更新间隔（毫秒）
        private Text fpsView;
        private float _msec;

        private void Start()
        {
            fpsView = GameObject.Find("FPS_Counter").GetComponent<Text>();
            StartCoroutine(UpdateFPS());
        }

        private IEnumerator UpdateFPS()
        {
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
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        }
    }
}
