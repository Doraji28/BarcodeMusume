using UnityEngine;

namespace BarcodeKanojo
{
    /// <summary>
    /// Color32 버퍼에 Shape를 안티앨리어싱해서 그리는 아주 작은 소프트웨어 래스터라이저.
    /// 디자인 좌표 0~100(위→아래)을 텍스처 픽셀(아래→위)로 바꿔 준다.
    /// </summary>
    public class PixelCanvas
    {
        public readonly int Size;
        readonly Color32[] pixels;
        readonly float unitPerPixel;

        public PixelCanvas(int size)
        {
            Size = size;
            pixels = new Color32[size * size];
            unitPerPixel = 100f / size;
        }

        /// <summary>위(top)→아래(bottom) 세로 그라데이션으로 전체를 칠한다.</summary>
        public void Gradient(Color top, Color bottom)
        {
            for (int py = 0; py < Size; py++)
            {
                // 텍스처는 py=0이 맨 아래
                Color32 c = Color.Lerp(bottom, top, py / (float)(Size - 1));
                int row = py * Size;
                for (int px = 0; px < Size; px++) pixels[row + px] = c;
            }
        }

        public void Fill(Shape s, Color color)
        {
            if (s.X1 <= s.X0 || s.Y1 <= s.Y0) return;

            int px0 = Mathf.Max(0, Mathf.FloorToInt(s.X0 / unitPerPixel) - 1);
            int px1 = Mathf.Min(Size - 1, Mathf.CeilToInt(s.X1 / unitPerPixel) + 1);
            int dy0 = Mathf.Max(0, Mathf.FloorToInt(s.Y0 / unitPerPixel) - 1);
            int dy1 = Mathf.Min(Size - 1, Mathf.CeilToInt(s.Y1 / unitPerPixel) + 1);

            for (int dy = dy0; dy <= dy1; dy++)
            {
                float y = (dy + 0.5f) * unitPerPixel;
                int row = (Size - 1 - dy) * Size;
                for (int px = px0; px <= px1; px++)
                {
                    float x = (px + 0.5f) * unitPerPixel;
                    float d = s.Sdf(x, y) / unitPerPixel; // 픽셀 단위 거리
                    if (d >= 0.5f) continue;
                    float coverage = Mathf.Clamp01(0.5f - d) * color.a;
                    Color32 dst = pixels[row + px];
                    pixels[row + px] = new Color32(
                        (byte)(dst.r + (color.r * 255f - dst.r) * coverage),
                        (byte)(dst.g + (color.g * 255f - dst.g) * coverage),
                        (byte)(dst.b + (color.b * 255f - dst.b) * coverage),
                        255);
                }
            }
        }

        /// <summary>외곽선(outline) 두께 w만큼 먼저 칠하고 그 위에 본체를 칠한다.</summary>
        public void FillOutlined(Shape s, Color fill, Color outline, float w = 0.7f)
        {
            Fill(s.Expand(w), outline);
            Fill(s, fill);
        }

        public Texture2D ToTexture(Texture2D reuse = null)
        {
            Texture2D tex = reuse;
            if (tex == null || tex.width != Size || tex.height != Size)
            {
                tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);
            return tex;
        }
    }
}
