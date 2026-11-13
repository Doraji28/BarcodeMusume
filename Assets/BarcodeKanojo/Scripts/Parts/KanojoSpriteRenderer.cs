using System.Collections.Generic;
using UnityEngine;

namespace BarcodeKanojo
{
    /// <summary>
    /// Resources/KanojoParts 폴더의 파츠 PNG를 읽어서 KanojoComposer로 합성하고 Texture2D로 돌려준다.
    /// 파츠가 하나도 없으면 Available == false → 앱은 예전 절차적 그림(KanojoPortrait)을 쓴다.
    /// </summary>
    public static class KanojoSpriteRenderer
    {
        const string Folder = "KanojoParts/";
        static readonly Dictionary<string, PartImage> cache = new Dictionary<string, PartImage>();
        static bool? available;

        public static bool Available
        {
            get
            {
                if (available == null) available = Load("body1") != null;
                return available.Value;
            }
        }

        public static Texture2D Render(KanojoData d, int size, Texture2D reuse = null)
        {
            var rgba = new byte[size * size * 4];
            KanojoComposer.Compose(d, Load, rgba, size);

            // 합성 결과는 위쪽 행부터, Unity 텍스처는 아래쪽 행부터 → 뒤집어서 넣는다
            var flipped = new byte[rgba.Length];
            int row = size * 4;
            for (int y = 0; y < size; y++)
                System.Buffer.BlockCopy(rgba, y * row, flipped, (size - 1 - y) * row, row);

            Texture2D tex = reuse;
            if (tex == null || tex.width != size || tex.height != size || tex.format != TextureFormat.RGBA32)
            {
                tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
            }
            tex.LoadRawTextureData(flipped);
            tex.Apply(false);
            return tex;
        }

        static PartImage Load(string name)
        {
            if (cache.TryGetValue(name, out PartImage cached)) return cached;

            Texture2D tex = Resources.Load<Texture2D>(Folder + name);
            PartImage img = tex == null ? null : ToPartImage(tex);
            cache[name] = img;
            return img;
        }

        static PartImage ToPartImage(Texture2D tex)
        {
            Color32[] px;
            if (tex.isReadable) px = tex.GetPixels32();
            else px = ReadViaGpu(tex); // 임포트 설정에서 Read/Write가 꺼져 있어도 동작하게

            int w = tex.width, h = tex.height;
            var rgba = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
            {
                int src = (h - 1 - y) * w; // Unity는 아래쪽 행부터
                for (int x = 0; x < w; x++)
                {
                    Color32 c = px[src + x];
                    int i = (y * w + x) * 4;
                    rgba[i] = c.r; rgba[i + 1] = c.g; rgba[i + 2] = c.b; rgba[i + 3] = c.a;
                }
            }
            return new PartImage(w, h, rgba);
        }

        static Color32[] ReadViaGpu(Texture2D tex)
        {
            RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture prev = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            copy.Apply(false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            Color32[] px = copy.GetPixels32();
            Object.Destroy(copy);
            return px;
        }
    }
}
