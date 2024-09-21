using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    public class EffectSimulateRange:MonoBehaviour
    {
        private BoxCollider _range;
        private Transform _spawnRoot;
        public Vector3 scale = Vector3.one;
        private static EffectSimulateRange _instance;
        public static EffectSimulateRange instance
        {
            get {
                if (_instance == null)
                    _instance = FindObjectOfType<EffectSimulateRange>();
                return _instance;
            }
        }

        private void Start()
        {
            _range = GetComponent<BoxCollider>();
            _spawnRoot = GetComponent<Transform>();
        }

        public void RandomInRange(Transform target)
        {
            Vector3 outPos;
            Quaternion outRot;
            Vector3 size = _range.bounds.size;
            RandomInBox(_range.center, Quaternion.identity, size, false, out outPos, out outRot);
            target.transform.SetParent(_spawnRoot, false);
            target.transform.localPosition = outPos;
            target.transform.localRotation = outRot;
            target.transform.localScale = Vector3.Scale(target.transform.localScale, scale);
        }

        public void RandomInBox(Vector3 boxCenter, Quaternion boxOrientation, Vector3 boxSize, bool randomDirection, out Vector3 outPos, out Quaternion outRot)
        {
            Vector3 localPosInBox = new Vector3(
                UnityEngine.Random.Range(-boxSize.x / 2, boxSize.x / 2),
                UnityEngine.Random.Range(-boxSize.y / 2, boxSize.y / 2),
                UnityEngine.Random.Range(-boxSize.z / 2, boxSize.z / 2)
            );

            Matrix4x4 boxMatrix = Matrix4x4.TRS(boxCenter, boxOrientation, Vector3.one);
            outPos = boxMatrix.MultiplyPoint3x4(localPosInBox);

            if (randomDirection)
                outRot = UnityEngine.Random.rotation;
            else
                outRot = boxOrientation;
        }
    }
}
