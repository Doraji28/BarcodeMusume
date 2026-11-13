using System;
using System.Collections.Generic;

namespace BarcodeKanojo
{
    /// <summary>
    /// 파츠 그림(PartImage)을 겹쳐서 SD 캐릭터 한 장을 만든다. Unity에 의존하지 않는 순수 C#.
    ///
    /// 좌표계: 300 x 300 "캔버스 단위", 원점은 왼쪽 위, y는 아래로.
    /// 각 파츠는 시트 칸 안의 원래 위치(KanojoPartOffsets)를 그대로 캔버스로 옮겨 그린다.
    /// 회색으로 그려진 파츠(머리카락, 홍채, 옷)는 KanojoData의 색 번호로 칠한다.
    /// </summary>
    public static class KanojoComposer
    {
        public const float Canvas = 300f;

        public enum Tint { None, Hair, Skin, Iris, Clothes, ClothesSoft, Accent }

        public struct Place
        {
            public float Cx, Cy, Width;
            public Place(float cx, float cy, float width) { Cx = cx; Cy = cy; Width = width; }
        }

        // ---------------- 배치 ----------------
        // 파츠 시트는 6 x 2 칸으로 되어 있고, 모든 파츠가 칸 안의 같은 위치(같은 몸 기준)에 그려져 있다.
        // 그래서 각 파츠의 "칸 안 좌표"(KanojoPartOffsets, 자르기 도구가 생성)만 알면 그대로 겹치면 된다.
        // 칸(333 x 747 px) 중 CropTop 아래 부분을 300 x 300 캔버스 가운데에 맞춰 그린다.
        public const float CellWidth = 2000f / 6f;
        public const float CropTop = 30f, CropBottom = 746f;
        static float PxToUnit => Canvas / (CropBottom - CropTop);
        static float CellLeft => (Canvas - CellWidth * PxToUnit) / 2f;

        /// <summary>
        /// 이름이 이 접두어로 시작하는 파츠를 옮긴다 (칸 안 px 단위).
        /// 눈 시트는 눈을 너무 높게(앞머리에 가려지게), 입 시트는 입을 너무 낮게 그려서 여기서 맞춘다.
        /// </summary>
        public static readonly Dictionary<string, (float dx, float dy)> Nudge = new Dictionary<string, (float, float)>
        {
            ["eyes_closed"] = (0, 8),
            ["eyes"] = (0, 58),
            ["glasses"] = (0, 14),
            ["brow"] = (0, 32),
            ["mouth"] = (0, -10),
            ["nose"] = (0, 22),
            ["blush"] = (0, 22),
            ["freckles"] = (0, 18),
            ["mole"] = (0, 8),
            ["bandage"] = (0, 10),
        };

        static (float dx, float dy) NudgeFor(string name)
        {
            foreach (var kv in Nudge)
                if (name.StartsWith(kv.Key)) return kv.Value;
            return (0, 0);
        }

        /// <summary>칸 안 좌표 → 캔버스 배치. extraDy는 눈/눈썹/입 위치 보정(칸 px).</summary>
        static bool TryPlace(string name, PartImage img, float extraDy, out Place place)
        {
            place = default;
            if (!KanojoPartOffsets.Offsets.TryGetValue(name, out var o)) return false;
            var n = NudgeFor(name);
            float s = PxToUnit;
            float ox = o.x + n.dx, oy = o.y + n.dy + extraDy;
            place = new Place(CellLeft + (ox + img.Width / 2f) * s, (oy - CropTop + img.Height / 2f) * s, img.Width * s);
            return true;
        }

        // ---------------- 파츠 고르기 ----------------

        public struct Step
        {
            public string Name; public Tint Tint; public float Alpha; public float Dy;
            public Step(string name, Tint tint, float alpha = 1f, float dy = 0f) { Name = name; Tint = tint; Alpha = alpha; Dy = dy; }
        }

        static readonly string[] Outfits = { "outfit_sailor", "outfit_blazer", "outfit_hoodie", "outfit_dress", "outfit_tshirt", "outfit_cardigan" };

        /// <summary>그릴 순서대로 파츠 목록을 만든다 (뒤 → 앞).</summary>
        public static List<Step> Plan(KanojoData d)
        {
            var list = new List<Step>();

            // 머리 스타일 9종: 뒷머리 i 와 앞머리 i 가 한 쌍. 9번(짧은 머리)은 앞머리가 2종.
            int style = (d.hairType - 1) % 9 + 1;
            string front = "front" + style;
            if (style == 9 && d.fringeType % 2 == 0) front = "front10";

            float eyeDy = d.eyePosition * 0.8f;
            float browDy = d.browPosition * 0.6f;
            float mouthDy = d.mouthPosition * 0.5f;

            // 1) 뒷머리 → 몸(얼굴형) → 옷
            list.Add(new Step("back" + style, Tint.Hair));
            list.Add(new Step("body" + ((d.faceType - 1) % 4 + 1), Tint.Skin));
            string outfit = Outfits[((d.clothesType - 1) * 2 + d.clothesColor) % Outfits.Length];
            list.Add(new Step(outfit, outfit == "outfit_hoodie" || outfit == "outfit_dress" ? Tint.ClothesSoft : Tint.Clothes));
            if (d.earType == 2) list.Add(new Step("elfears", Tint.Skin));

            // 2) 얼굴
            switch (d.spotType)
            {
                case 2: case 6: list.Add(new Step("blush", Tint.None, 0.9f)); break;
                case 3: list.Add(new Step("freckles", Tint.None)); break;
                case 4: list.Add(new Step("mole", Tint.None)); break;
                case 5: list.Add(new Step("bandage", Tint.None)); break;
            }
            int eye = (d.eyeType - 101) % 9;
            list.Add(eye < 7
                ? new Step("eyes" + (eye + 1), Tint.Iris, 1f, eyeDy)
                : new Step("eyes_closed" + (eye - 6), Tint.None, 1f, eyeDy));
            int nose = d.noseType % 3;
            if (nose > 0) list.Add(new Step("nose" + nose, Tint.None));
            list.Add(new Step("mouth" + ((d.mouthType - 1) % 6 + 1), Tint.None, 1f, mouthDy));
            if (d.glassesType == 2 && d.hairColor % 2 == 0) list.Add(new Step("glasses", Tint.None, 1f, eyeDy)); // 약 25%

            // 3) 고양이 귀(앞머리 뒤) → 앞머리 → 눈썹(앞머리 위로 살짝 비침) → 머리 장식
            if (d.accessoryType == 5) list.Add(new Step("catears", Tint.Hair));
            list.Add(new Step(front, Tint.Hair));
            list.Add(new Step("brow" + ((d.browType - 1) % 6 + 1), Tint.None, 0.75f, browDy));
            switch (d.accessoryType)
            {
                case 2: list.Add(new Step("ribbon", Tint.Accent)); break;
                case 3: list.Add(new Step("hairpins", Tint.Accent)); break;
                case 4: list.Add(new Step("headband", Tint.Accent)); break;
            }
            return list;
        }

        // ---------------- 합성 ----------------

        /// <summary>
        /// 결과를 dst(size x size, RGBA, 위쪽 행부터)에 그린다.
        /// load(이름)이 null을 돌려주는 파츠는 건너뛴다.
        /// </summary>
        public static void Compose(KanojoData d, Func<string, PartImage> load, byte[] dst, int size)
        {
            float[] bg = KanojoPalette.Rgb(Backgrounds[(Math.Max(1, d.raceType) - 1) % Backgrounds.Length]);
            FillBackground(dst, size, bg);

            var colors = new Colors
            {
                Hair = KanojoPalette.Pick(KanojoPalette.Hair, d.hairColor),
                Eye = KanojoPalette.Pick(KanojoPalette.Eye, d.eyeColor),
                Clothes = KanojoPalette.Pick(KanojoPalette.Clothes, d.clothesColor),
                Accent = KanojoPalette.Pick(KanojoPalette.Eye, (d.eyeColor + 4) % 12 + 1),
            };
            float[] skin = KanojoPalette.Pick(KanojoPalette.Skin, d.skinColor);
            colors.SkinRatio = new[] { skin[0] / BaseSkin[0], skin[1] / BaseSkin[1], skin[2] / BaseSkin[2] };

            foreach (var step in Plan(d))
            {
                PartImage img = load(step.Name);
                if (img == null || !TryPlace(step.Name, img, step.Dy, out Place place)) continue;
                Draw(img, place, step.Tint, step.Alpha, colors, dst, size);
            }
        }

        /// <summary>시트에 그려진 기본 몸의 피부색. 이 색을 기준으로 피부 번호 색으로 바꾼다.</summary>
        static readonly float[] BaseSkin = { 240 / 255f, 213 / 255f, 195 / 255f };

        static readonly string[] Backgrounds =
        {
            "FBE3EC", "E4EEFB", "E9F7E8", "FBF1E0", "EFE7FA", "E3F6F7", "FAE7E3", "F3F7DF", "ECECF6", "F8EBF5",
        };

        class Colors
        {
            public float[] Hair, Eye, Clothes, Accent, SkinRatio;
        }

        static void FillBackground(byte[] dst, int size, float[] bg)
        {
            for (int y = 0; y < size; y++)
            {
                float t = y / (float)(size - 1);
                // 위는 흰색에 가깝게, 아래는 배경색
                byte r = (byte)(255 * (1 + (bg[0] - 1) * t));
                byte g = (byte)(255 * (1 + (bg[1] - 1) * t));
                byte b = (byte)(255 * (1 + (bg[2] - 1) * t));
                for (int x = 0; x < size; x++)
                {
                    int i = (y * size + x) * 4;
                    dst[i] = r; dst[i + 1] = g; dst[i + 2] = b; dst[i + 3] = 255;
                }
            }
        }

        static void Draw(PartImage img, Place p, Tint tint, float alpha, Colors c, byte[] dst, int size)
        {
            float unitsPerSrc = p.Width / img.Width;
            float heightUnits = img.Height * unitsPerSrc;
            float left = p.Cx - p.Width / 2f, top = p.Cy - heightUnits / 2f;
            float pxPerUnit = size / Canvas;

            int x0 = Math.Max(0, (int)Math.Floor(left * pxPerUnit));
            int x1 = Math.Min(size - 1, (int)Math.Ceiling((left + p.Width) * pxPerUnit));
            int y0 = Math.Max(0, (int)Math.Floor(top * pxPerUnit));
            int y1 = Math.Min(size - 1, (int)Math.Ceiling((top + heightUnits) * pxPerUnit));

            // 크게 줄일 때(썸네일) 계단 현상을 줄이려고 여러 점을 평균낸다
            float srcPerDstPx = 1f / (pxPerUnit * unitsPerSrc);
            int ss = srcPerDstPx > 1.5f ? 2 : 1;

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float r = 0, g = 0, b = 0, a = 0;
                    for (int sy = 0; sy < ss; sy++)
                    for (int sx = 0; sx < ss; sx++)
                    {
                        float ux = (x + (sx + 0.5f) / ss) / pxPerUnit, uy = (y + (sy + 0.5f) / ss) / pxPerUnit;
                        float fx = (ux - left) / unitsPerSrc - 0.5f, fy = (uy - top) / unitsPerSrc - 0.5f;
                        Sample(img, fx, fy, out float sr, out float sg, out float sb, out float sa);
                        r += sr; g += sg; b += sb; a += sa;
                    }
                    float n = ss * ss;
                    a /= n;
                    if (a <= 0.003f) continue;
                    // Sample은 premultiplied 값을 돌려준다 → 원래 색으로 되돌림
                    r /= n * a; g /= n * a; b /= n * a;

                    ApplyTint(tint, c, ref r, ref g, ref b);

                    a *= alpha;
                    int i = (y * size + x) * 4;
                    dst[i] = Blend(dst[i], r, a);
                    dst[i + 1] = Blend(dst[i + 1], g, a);
                    dst[i + 2] = Blend(dst[i + 2], b, a);
                }
            }
        }

        static byte Blend(byte under, float over, float a)
        {
            float v = under / 255f * (1 - a) + Clamp01(over) * a;
            return (byte)(v * 255f + 0.5f);
        }

        /// <summary>양선형 보간, premultiplied alpha로 (투명 가장자리 색 번짐 방지).</summary>
        static void Sample(PartImage img, float fx, float fy, out float r, out float g, out float b, out float a)
        {
            r = g = b = a = 0;
            int ix = (int)Math.Floor(fx), iy = (int)Math.Floor(fy);
            float tx = fx - ix, ty = fy - iy;
            for (int k = 0; k < 4; k++)
            {
                int px = ix + (k & 1), py = iy + (k >> 1);
                if (px < 0 || py < 0 || px >= img.Width || py >= img.Height) continue;
                float w = ((k & 1) == 1 ? tx : 1 - tx) * ((k >> 1) == 1 ? ty : 1 - ty);
                int i = (py * img.Width + px) * 4;
                float pa = img.Rgba[i + 3] / 255f * w;
                r += img.Rgba[i] / 255f * pa;
                g += img.Rgba[i + 1] / 255f * pa;
                b += img.Rgba[i + 2] / 255f * pa;
                a += pa;
            }
        }

        static void ApplyTint(Tint tint, Colors c, ref float r, ref float g, ref float b)
        {
            if (tint == Tint.None) return;
            float lum = (r + g + b) / 3f;
            float sat = Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));

            switch (tint)
            {
                case Tint.Hair:
                {
                    // 흰 머리카락 → 머리색, 회색 음영 → 어두운 머리색, 검은 선 → 그대로 어둡게
                    float[] h = c.Hair;
                    float k = Math.Min(1.08f, lum * 1.08f);
                    r = h[0] * k; g = h[1] * k; b = h[2] * k;
                    break;
                }
                case Tint.Skin:
                    r *= c.SkinRatio[0]; g *= c.SkinRatio[1]; b *= c.SkinRatio[2];
                    break;
                case Tint.Iris:
                {
                    // 채도 낮은 회색 부분(홍채)만 칠한다. 흰자(아주 밝음)와 속눈썹(아주 어두움)은 그대로.
                    if (sat > 0.12f) return;
                    float w = Clamp01((0.9f - lum) / 0.1f) * Clamp01((lum - 0.12f) / 0.15f);
                    float k = Math.Min(1.15f, lum * 1.45f);
                    r = Lerp(r, c.Eye[0] * k, w); g = Lerp(g, c.Eye[1] * k, w); b = Lerp(b, c.Eye[2] * k, w);
                    break;
                }
                case Tint.Clothes:
                {
                    // 흰 부분(셔츠, 칼라)은 남기고 회색 부분(치마, 재킷)만 옷 색으로
                    if (sat > 0.15f) return;
                    float w = Clamp01((0.9f - lum) / 0.18f);
                    float k = Math.Min(1.1f, lum * 1.5f + 0.05f);
                    r = Lerp(r, c.Clothes[0] * k, w); g = Lerp(g, c.Clothes[1] * k, w); b = Lerp(b, c.Clothes[2] * k, w);
                    break;
                }
                case Tint.ClothesSoft:
                {
                    // 거의 흰 옷(원피스, 후드)은 옅은 옷 색을 전체에 곱한다
                    if (sat > 0.15f) return;
                    r *= Lerp(c.Clothes[0], 1f, 0.4f); g *= Lerp(c.Clothes[1], 1f, 0.4f); b *= Lerp(c.Clothes[2], 1f, 0.4f);
                    break;
                }
                case Tint.Accent:
                {
                    if (sat > 0.15f || lum < 0.15f) return;
                    float k = Math.Min(1.1f, lum * 1.15f);
                    r = c.Accent[0] * k; g = c.Accent[1] * k; b = c.Accent[2] * k;
                    break;
                }
            }
        }

        static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
