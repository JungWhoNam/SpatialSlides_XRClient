using UnityEngine;
using UnityEngine.UI;

namespace SlidesXR
{
    [RequireComponent(typeof(RawImage))]
    public class View : MonoBehaviour
    {
        public Vector3 pos { get; private set; }
        public Quaternion rot { get; private set; }
        public Vector3 scale { get; private set; }

        private RawImage rawImage;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
        }

        public void SetImage(Texture2D tex)
        {
            if (rawImage == null || tex == null) return;

            rawImage.texture = tex;

            RectTransform rt = rawImage.rectTransform;
            float aspectRatio = (float)tex.height / tex.width;
            rt.localScale = new Vector3(rt.localScale.x, rt.localScale.x * aspectRatio, rt.localScale.z);
        }

        public void SetTransform(Vector3 position, Quaternion rotation, Vector3 scaling)
        {
            pos = position;
            rot = rotation;
            scale = scaling;
        }

        public Texture GetTexture()
        {
            return rawImage ? rawImage.texture : null;
        }

        /// <summary>
        /// Compares two views for approximate equality (position, rotation, scale).
        /// </summary>
        public static bool ApproximatelyEqual(View a, View b, float tolerance = 0.001f, bool ignoreRotation = false)
        {
            if (a == null || b == null) return false;

            bool posEqual = Vector3.Distance(a.pos, b.pos) <= tolerance;
            bool rotEqual = ignoreRotation || Quaternion.Angle(a.rot, b.rot) <= tolerance * 100f; // angle in degrees
            bool scaleEqual = Vector3.Distance(a.scale, b.scale) <= tolerance;

            return posEqual && rotEqual && scaleEqual;
        }

    }
}
