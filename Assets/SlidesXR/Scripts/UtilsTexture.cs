using UnityEngine;

namespace SlidesXR.Utils
{
    public static class UtilsTexture
    {
        public static Texture2D ReadRenderTexture(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            RenderTexture.active = prev;
            return tex;
        }

        public static Texture2D Crop(Texture2D src, int x, int y, int w, int h)
        {
            var pixels = src.GetPixels(x, y, w, h);
            var result = new Texture2D(w, h);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        public static Texture2D Resize(Texture2D src, int w, int h)
        {
            Texture2D result = new Texture2D(w, h, src.format, true);
            Color[] dest = result.GetPixels(0);
            float incX = 1f / w;
            float incY = 1f / h;

            for (int i = 0; i < dest.Length; i++)
            {
                int px = i % w;
                int py = i / w;
                dest[i] = src.GetPixelBilinear(px * incX, py * incY);
            }

            result.SetPixels(dest, 0);
            result.Apply();
            return result;
        }

        public static bool IsD3D()
        {
            var type = SystemInfo.graphicsDeviceType;
            return type == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 ||
                   type == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12;
        }
    }
}
