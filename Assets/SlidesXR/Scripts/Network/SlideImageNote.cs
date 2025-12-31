using UnityEngine;
using Newtonsoft.Json.Linq;

namespace SlidesXR.Data
{
    [System.Serializable]
    public class SlideImageNote
    {
        /// <summary>
        /// View metadata (position, rotation, scale) associated with the image.
        /// </summary>
        public SlideViewMetadata metadata;

        /// <summary>
        /// Image associated with the metadata. Loaded as a Texture2D.
        /// </summary>
        public Texture2D image;
    }

    [System.Serializable]
    public struct SlideViewMetadata
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        /// <summary>
        /// Construct SlideViewMetadata from a View object.
        /// </summary>
        public static SlideViewMetadata FromView(View view)
        {
            return new SlideViewMetadata
            {
                position = view.pos,
                rotation = view.rot,
                scale = view.scale
            };
        }

        /// <summary>
        /// Parse SlideViewMetadata from a JSON object.
        /// </summary>
        public static SlideViewMetadata FromJObject(JObject obj)
        {
            return new SlideViewMetadata
            {
                position = ParseVector3(obj["position"]),
                rotation = ParseQuaternion(obj["rotation"]),
                scale = ParseVector3(obj["scale"])
            };
        }

        private static Vector3 ParseVector3(JToken token)
        {
            return new Vector3(
                token?["x"]?.Value<float>() ?? 0f,
                token?["y"]?.Value<float>() ?? 0f,
                token?["z"]?.Value<float>() ?? 0f
            );
        }

        private static Quaternion ParseQuaternion(JToken token)
        {
            return new Quaternion(
                token?["x"]?.Value<float>() ?? 0f,
                token?["y"]?.Value<float>() ?? 0f,
                token?["z"]?.Value<float>() ?? 0f,
                token?["w"]?.Value<float>() ?? 1f
            );
        }

        /// <summary>
        /// Checks if two SlideViewMetadata instances are approximately equal.
        /// </summary>
        public static bool ApproximatelyEqual(SlideViewMetadata a, SlideViewMetadata b, float tolerance = 0.001f)
        {
            bool posEqual = Vector3.Distance(a.position, b.position) <= tolerance;
            bool rotEqual = Quaternion.Angle(a.rotation, b.rotation) <= tolerance * 100f; // angle in degrees
            bool scaleEqual = Vector3.Distance(a.scale, b.scale) <= tolerance;

            return posEqual && rotEqual && scaleEqual;
        }

    }
}
