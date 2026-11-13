using System.Collections.Generic;
using UnityEngine;

namespace BarcodeKanojo
{
    /// <summary>
    /// KanojoData 값만 보고 초상화를 절차적으로 그린다(이미지 에셋 없음).
    /// 좌표는 0~100 디자인 단위, x 오른쪽 / y 아래쪽. 레이어는 뒤에서 앞 순서로 그린다.
    /// 나중에 직접 그린 스프라이트로 바꾸고 싶으면, 각 Draw* 함수를 "타입 번호 → 스프라이트" 로 교체하면 된다.
    /// </summary>
    public static class KanojoPortrait
    {
        // ---------------- 팔레트 ----------------

        static readonly Color[] HairColors = Hexes(
            "2A2A35", "4A3228", "7A4B32", "A8714A", "8B3A2A", "E8C872", "EDE3C4", "C9CCD6",
            "8A8C94", "A8452E", "C83C3C", "E8853A", "F2A1C0", "E0508C", "8E5BB8", "C3A6E6",
            "2E3A78", "4A7FD6", "8EC8F0", "3AA6A0", "9EE0C0", "5A9A50", "F4F4F8", "6E2440");

        static readonly Color[] EyeColors = Hexes(
            "7A4A2A", "3A2A2A", "3A78D8", "6AB8E8", "3E9A5A", "1E8A78",
            "D89A2A", "C8323C", "8A4AC8", "E06AA8", "7A808A", "E8C040");

        static readonly Color[] SkinColors = Hexes(
            "FFF1E8", "FDE7D8", "FADBC6", "F6D0B4", "F0C3A0", "E8B48C",
            "DDA27A", "CF9068", "BF7E58", "A86A48", "8E583C", "74452E");

        static readonly Color[] ClothesColors = Hexes("2C3E6E", "C8404A", "E89AB8", "4E8A5E", "2E2E36", "6AA8DE");

        static readonly Color[] BackgroundColors = Hexes(
            "F7C6D9", "C9DDF7", "D4F0D2", "F7E3C1", "E0D0F5",
            "C8EEF0", "F5D0C8", "E8F0C0", "D8D8EE", "F2D7EC");

        static readonly Color Ink = new Color(0.20f, 0.13f, 0.14f);
        static readonly Color White = new Color(1f, 1f, 1f);

        // ---------------- 진입점 ----------------

        public static Texture2D Render(KanojoData d, int size = 512, Texture2D reuse = null)
        {
            var cv = new PixelCanvas(size);
            var p = new Ctx(d);

            Color bg = Pick(BackgroundColors, d.raceType);
            cv.Gradient(Color.Lerp(bg, White, 0.55f), bg);
            cv.Fill(Shape.Circle(50, 46, 42), new Color(1, 1, 1, 0.25f));

            DrawBackHair(cv, p);
            DrawBody(cv, p);
            DrawHairTails(cv, p);
            DrawEars(cv, p);
            cv.FillOutlined(p.Face, p.Skin, p.SkinLine, 0.6f);
            DrawSpots(cv, p);
            DrawEye(cv, p, +1);
            DrawEye(cv, p, -1);
            DrawBrows(cv, p);
            DrawNose(cv, p);
            DrawMouth(cv, p);
            DrawGlasses(cv, p);
            if (d.accessoryType == 5) DrawCatEars(cv, p);
            DrawFrontHair(cv, p);
            DrawAccessory(cv, p);

            return cv.ToTexture(reuse);
        }

        /// <summary>그리는 동안 여러 함수가 같이 쓰는 값들.</summary>
        class Ctx
        {
            public readonly KanojoData D;
            public readonly Color Hair, HairDark, HairLight, Skin, SkinShade, SkinLine, Eye, Clothes;
            public readonly float Fw;        // 얼굴 반폭
            public readonly float ChinY;
            public readonly float EyeY, EyeRx, EyeRy, EyeDx;
            public readonly int HairLength;  // 0 짧음 ~ 3 아주 김
            public readonly int HairFeature; // 0~6
            public readonly Shape Face;

            public Ctx(KanojoData d)
            {
                D = d;
                Hair = Pick(HairColors, d.hairColor);
                HairDark = Shade(Hair, 0.55f);
                HairLight = Color.Lerp(Hair, White, 0.45f);
                Skin = Pick(SkinColors, d.skinColor);
                SkinShade = Shade(Skin, 0.85f);
                SkinLine = Shade(Skin, 0.6f);
                Eye = Pick(EyeColors, d.eyeColor);
                Clothes = Pick(ClothesColors, d.clothesColor);

                int f = d.faceType - 1;
                Fw = 18.5f + (f % 3) * 1.3f;
                ChinY = 63f + (f / 3) * 2.5f;

                int e = d.eyeType - 101;
                EyeRx = 4.3f + (e % 5) * 0.3f;
                EyeRy = 5.0f + (e % 5) * 0.45f;
                EyeY = 48f + d.eyePosition * 0.22f;
                EyeDx = Fw * 0.47f;

                HairLength = (d.hairType - 1) % 4;
                HairFeature = (d.hairType - 1) / 4;

                Face = Shape.Ellipse(50, 42, Fw, 20).Union(Shape.Polygon(new[]
                {
                    V(50 - Fw, 44), V(50 - Fw * 0.88f, 53), V(45, ChinY - 1.5f), V(50, ChinY),
                    V(55, ChinY - 1.5f), V(50 + Fw * 0.88f, 53), V(50 + Fw, 44),
                }));
            }
        }

        // ---------------- 머리카락 (뒤) ----------------

        static void DrawBackHair(PixelCanvas cv, Ctx p)
        {
            float w = p.Fw + 6.5f;
            float[] ends = { 54f, 64f, 86f, 104f };
            float endY = ends[p.HairLength];

            Shape mass = Shape.Ellipse(50, 40, w, 26)
                .Union(Shape.Box(50, (40 + endY) / 2f, w - 0.5f, (endY - 40) / 2f, 5f));

            if (p.HairFeature == 4) // 웨이브: 아래쪽에 동글동글한 컬
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = 50 - w + 3 + i * (2 * w - 6) / 4f;
                    mass = mass.Union(Shape.Circle(x, endY - 2, 4.2f));
                }
            }
            if (p.HairFeature == 3) // 똥머리(번)
            {
                mass = mass.Union(Shape.Circle(50 - p.Fw + 1, 19, 7.5f)).Union(Shape.Circle(50 + p.Fw - 1, 19, 7.5f));
            }

            cv.FillOutlined(mass, Shade(p.Hair, 0.85f), p.HairDark, 0.7f);
        }

        /// <summary>트윈테일/포니테일/사이드테일은 어깨 앞에 오도록 몸 다음에 그린다.</summary>
        static void DrawHairTails(PixelCanvas cv, Ctx p)
        {
            int f = p.HairFeature;
            float len = 10f * p.HairLength;
            if (f == 1) // 트윈테일
            {
                Shape tail = Tail(50 + p.Fw + 3, 26, +1, 58 + len);
                cv.FillOutlined(tail, p.Hair, p.HairDark, 0.7f);
                cv.FillOutlined(tail.MirrorX(50), p.Hair, p.HairDark, 0.7f);
            }
            else if (f == 2) // 포니테일 (오른쪽 뒤로 살짝 보이게)
            {
                cv.FillOutlined(Tail(50 + p.Fw + 4, 22, +1, 66 + len), p.Hair, p.HairDark, 0.7f);
            }
            else if (f == 5) // 사이드테일 (왼쪽)
            {
                cv.FillOutlined(Tail(50 + p.Fw + 3, 34, +1, 62 + len).MirrorX(50), p.Hair, p.HairDark, 0.7f);
            }
        }

        static Shape Tail(float x, float y, int side, float endY)
        {
            float s = side;
            var pts = new List<Vector2>();
            pts.AddRange(Shape.Bezier(V(x - 2 * s, y), V(x + 14 * s, y + 4), V(x + 9 * s, endY), 10));
            var back = Shape.Bezier(V(x + 9 * s, endY), V(x + 3 * s, (y + endY) / 2), V(x - 1 * s, y + 9), 10);
            back.RemoveAt(0); // 앞 곡선의 끝점과 같은 점이라 중복 제거
            pts.AddRange(back);
            return Shape.Polygon(pts);
        }

        // ---------------- 몸 / 옷 ----------------

        static void DrawBody(PixelCanvas cv, Ctx p)
        {
            Shape neck = Shape.Box(50, 67, 5.2f, 8f, 1f);
            cv.FillOutlined(neck, p.Skin, p.SkinLine, 0.5f);
            cv.Fill(Shape.Ellipse(50, p.ChinY + 0.5f, 7f, 3.2f).Intersect(neck), p.SkinShade);

            Color c = p.Clothes, cDark = Shade(c, 0.7f), line = Shade(c, 0.45f);
            Shape body = Shape.Polygon(new[]
            {
                V(8, 102), V(13, 86), V(24, 78), V(41, 73.5f), V(59, 73.5f), V(76, 78), V(87, 86), V(92, 102),
            });
            cv.FillOutlined(body, c, line, 0.7f);

            Color accent = Pick(EyeColors, p.D.eyeColor);
            switch (p.D.clothesType)
            {
                case 1: // 세일러복
                {
                    Color collar = p.D.clothesColor == 1 || p.D.clothesColor == 5 ? new Color(0.95f, 0.95f, 0.97f) : Hex("2C3E6E");
                    Shape col = Shape.Polygon(new[] { V(27, 77), V(41, 73.5f), V(50, 91), V(59, 73.5f), V(73, 77), V(68, 87), V(50, 99), V(32, 87) });
                    cv.FillOutlined(col, collar, line, 0.6f);
                    cv.Fill(col.Expand(-1.4f).Ring(0.3f), collar == White || collar.r > 0.9f ? Hex("2C3E6E") : White); // 칼라 줄무늬
                    cv.Fill(Shape.Polygon(new[] { V(43, 74), V(57, 74), V(50, 89) }), p.Skin);
                    // 리본
                    cv.FillOutlined(Shape.Polygon(new[] { V(50, 89), V(43, 85), V(44, 94) }), accent, Shade(accent, 0.5f), 0.4f);
                    cv.FillOutlined(Shape.Polygon(new[] { V(50, 89), V(57, 85), V(56, 94) }), accent, Shade(accent, 0.5f), 0.4f);
                    cv.FillOutlined(Shape.Circle(50, 89, 1.8f), accent, Shade(accent, 0.5f), 0.4f);
                    break;
                }
                case 2: // 블레이저 + 넥타이
                {
                    cv.Fill(Shape.Polygon(new[] { V(42, 73.5f), V(58, 73.5f), V(50, 95) }), new Color(0.97f, 0.97f, 0.99f));
                    cv.FillOutlined(Shape.Polygon(new[] { V(48.6f, 75.5f), V(51.4f, 75.5f), V(52.6f, 92), V(50, 95.5f), V(47.4f, 92) }),
                        accent, Shade(accent, 0.5f), 0.4f);
                    Shape lapel = Shape.Polygon(new[] { V(40, 73.8f), V(45.5f, 73.8f), V(50, 96), V(43, 91), V(36.5f, 80) });
                    cv.FillOutlined(lapel, cDark, line, 0.5f);
                    cv.FillOutlined(lapel.MirrorX(50), cDark, line, 0.5f);
                    break;
                }
                case 3: // 후드티
                {
                    Shape hood = Shape.Ellipse(50, 75, 15, 5.5f).Ring(1.6f);
                    cv.FillOutlined(hood, cDark, line, 0.5f);
                    cv.Fill(Shape.Segment(46, 78, 45.5f, 90, 0.5f), White);
                    cv.Fill(Shape.Segment(54, 78, 54.5f, 90, 0.5f), White);
                    cv.Fill(Shape.Circle(45.5f, 90.5f, 0.9f), White);
                    cv.Fill(Shape.Circle(54.5f, 90.5f, 0.9f), White);
                    break;
                }
                case 4: // 원피스 (프릴 넥라인)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        float a = Mathf.PI * (0.1f + 0.8f * i / 8f);
                        cv.FillOutlined(Shape.Circle(50 - Mathf.Cos(a) * 11f, 72.5f + Mathf.Sin(a) * 5f, 1.8f), White, line, 0.35f);
                    }
                    cv.Fill(Shape.Box(50, 97, 30, 0.6f), White);
                    break;
                }
                default: // 5: 티셔츠
                {
                    cv.Fill(Shape.Ellipse(50, 73.5f, 7.5f, 3.2f).Ring(0.7f), cDark);
                    Shape heart = Shape.Circle(48.3f, 88, 2.2f).Union(Shape.Circle(51.7f, 88, 2.2f))
                        .Union(Shape.Polygon(new[] { V(46.1f, 88.8f), V(53.9f, 88.8f), V(50, 93.5f) }));
                    cv.Fill(heart, new Color(1, 1, 1, 0.85f));
                    break;
                }
            }
        }

        // ---------------- 얼굴 ----------------

        static void DrawEars(PixelCanvas cv, Ctx p)
        {
            Shape ear = p.D.earType == 2
                ? Shape.Polygon(new[] { V(50 + p.Fw - 1.5f, 42), V(50 + p.Fw + 10, 34), V(50 + p.Fw - 0.5f, 53) })   // 엘프 귀
                : Shape.Ellipse(50 + p.Fw, 48, 3f, 4.6f);
            cv.FillOutlined(ear, p.Skin, p.SkinLine, 0.5f);
            cv.FillOutlined(ear.MirrorX(50), p.Skin, p.SkinLine, 0.5f);
        }

        static void DrawEye(PixelCanvas cv, Ctx p, int side)
        {
            float cx = 50 + side * p.EyeDx, cy = p.EyeY, rx = p.EyeRx, ry = p.EyeRy;
            int style = (p.D.eyeType - 101) / 5; // 0 기본, 1 처진 눈, 2 올라간 눈
            float outerDy = style == 1 ? 1.4f : style == 2 ? -1.6f : 0f;

            Shape sclera = Shape.Ellipse(cx, cy, rx, ry);
            cv.Fill(sclera, White);

            Shape iris = Shape.Ellipse(cx, cy + 0.6f, rx * 0.74f, ry * 0.86f).Intersect(sclera);
            cv.Fill(iris.Expand(0.35f).Intersect(sclera), Shade(p.Eye, 0.45f));
            cv.Fill(iris, p.Eye);
            cv.Fill(Shape.Ellipse(cx, cy - ry * 0.3f, rx * 0.74f, ry * 0.55f).Intersect(iris), Shade(p.Eye, 0.65f));
            cv.Fill(Shape.Ellipse(cx, cy + ry * 0.5f, rx * 0.5f, ry * 0.25f).Intersect(iris), Color.Lerp(p.Eye, White, 0.35f));
            cv.Fill(Shape.Ellipse(cx, cy + 0.8f, rx * 0.3f, ry * 0.36f), Shade(p.Eye, 0.25f));
            cv.Fill(Shape.Circle(cx + side * rx * 0.28f, cy - ry * 0.32f, rx * 0.26f), White);
            cv.Fill(Shape.Circle(cx - side * rx * 0.3f, cy + ry * 0.35f, rx * 0.12f), White);

            // 윗 속눈썹 라인
            Vector2 inner = V(cx - side * rx * 1.0f, cy - ry * 0.35f);
            Vector2 outer = V(cx + side * rx * 1.15f, cy - ry * 0.4f + outerDy);
            var lash = Shape.Bezier(inner, V(cx, cy - ry * 1.4f), outer, 14);
            cv.Fill(Shape.Polyline(lash, 0.85f), Ink);
            cv.Fill(Shape.Segment(outer.x, outer.y, outer.x + side * 1.6f, outer.y - 1.2f - outerDy * 0.3f, 0.5f), Ink);
            // 아래 속눈썹
            cv.Fill(Shape.Polyline(Shape.Bezier(V(cx - side * rx * 0.3f, cy + ry * 0.98f), V(cx + side * rx * 0.4f, cy + ry * 1.08f),
                V(cx + side * rx * 0.85f, cy + ry * 0.6f), 8), 0.3f), new Color(Ink.r, Ink.g, Ink.b, 0.6f));
        }

        static void DrawBrows(PixelCanvas cv, Ctx p)
        {
            int b = p.D.browType - 1;
            float thick = 0.5f + (b % 3) * 0.22f;
            int mood = b / 3; // 0 일자, 1 아치, 2 걱정(안쪽 올라감), 3 화남(안쪽 내려감)
            float y = p.EyeY - p.EyeRy - 3.6f + p.D.browPosition * 0.2f;
            float innerDy = mood == 2 ? -1.6f : mood == 3 ? 1.6f : 0f;
            float arch = mood == 1 ? -2.2f : -0.8f;
            Color col = Color.Lerp(p.HairDark, Ink, 0.3f);

            for (int side = -1; side <= 1; side += 2)
            {
                float cx = 50 + side * p.EyeDx;
                Vector2 inner = V(cx - side * p.EyeRx * 0.9f, y + innerDy);
                Vector2 outer = V(cx + side * p.EyeRx * 1.15f, y + 0.6f);
                cv.Fill(Shape.Polyline(Shape.Bezier(inner, V(cx, y + arch), outer, 10), thick), col);
            }
        }

        static void DrawNose(PixelCanvas cv, Ctx p)
        {
            Color c = Shade(p.Skin, 0.7f);
            switch (p.D.noseType)
            {
                case 1: cv.Fill(Shape.Circle(50.4f, 56f, 0.5f), c); break;
                case 2: cv.Fill(Shape.Segment(50.9f, 53.8f, 50.3f, 56.4f, 0.32f), c); break;
                case 3: cv.Fill(Shape.Polyline(new[] { V(49.2f, 55.5f), V(50.3f, 56.6f), V(51.4f, 55.7f) }, 0.3f), c); break;
                case 4: cv.Fill(Shape.Segment(51.6f, 51f, 50.7f, 56f, 0.3f), new Color(c.r, c.g, c.b, 0.7f)); break;
                case 5: cv.Fill(Shape.Polygon(new[] { V(50.2f, 54.6f), V(51.3f, 56.4f), V(49.6f, 56.4f) }), new Color(c.r, c.g, c.b, 0.6f)); break;
                default: cv.Fill(Shape.Polyline(new[] { V(51f, 52f), V(51.5f, 55.5f), V(50.1f, 56.3f) }, 0.3f), c); break;
            }
        }

        static void DrawMouth(PixelCanvas cv, Ctx p)
        {
            float y = Mathf.Min(60.5f + p.D.mouthPosition * 0.18f, p.ChinY - 2.5f);
            Color ink = new Color(0.45f, 0.18f, 0.2f);
            Color inside = new Color(0.55f, 0.15f, 0.2f);
            Color tongue = new Color(0.95f, 0.5f, 0.55f);

            switch (p.D.mouthType)
            {
                case 1: Curve(cv, V(47.6f, y), V(50, y + 1.6f), V(52.4f, y), 0.35f, ink); break;                // 작은 미소
                case 2: Curve(cv, V(46f, y - 0.4f), V(50, y + 2.4f), V(54f, y - 0.4f), 0.38f, ink); break;      // 활짝 미소
                case 3: OpenMouth(cv, y, 2.8f, 2.3f, inside, tongue, false); break;                              // 벌린 미소
                case 4:                                                                                          // 고양이 입 ω
                    Curve(cv, V(46.8f, y), V(48.4f, y + 1.8f), V(50, y), 0.33f, ink);
                    Curve(cv, V(50, y), V(51.6f, y + 1.8f), V(53.2f, y), 0.33f, ink);
                    break;
                case 5: cv.Fill(Shape.Ellipse(50, y + 0.6f, 1.1f, 1.4f), inside); break;                         // o
                case 6: cv.Fill(Shape.Segment(48f, y + 0.5f, 52f, y + 0.5f, 0.32f), ink); break;                // 일자
                case 7: Curve(cv, V(48f, y + 1.2f), V(50, y - 0.3f), V(52f, y + 1.2f), 0.33f, ink); break;      // 시무룩
                case 8: OpenMouth(cv, y, 3f, 2.4f, inside, tongue, true); break;                                 // 송곳니
                case 9: Curve(cv, V(47.5f, y + 0.6f), V(50.5f, y + 1.4f), V(53f, y - 0.6f), 0.35f, ink); break; // 비죽 웃음
                case 10:                                                                                         // 물결
                    cv.Fill(Shape.Polyline(new[] { V(47.4f, y + 0.6f), V(48.7f, y - 0.2f), V(50f, y + 0.6f), V(51.3f, y - 0.2f), V(52.6f, y + 0.6f) }, 0.3f), ink);
                    break;
                case 11: OpenMouth(cv, y, 3.6f, 3.2f, inside, tongue, false); break;                             // 크게 웃음
                default: cv.Fill(Shape.Polygon(new[] { V(48f, y), V(52f, y), V(50f, y + 2.6f) }), inside); break; // 세모 입
            }
        }

        static void OpenMouth(PixelCanvas cv, float y, float rx, float ry, Color inside, Color tongue, bool fang)
        {
            Shape m = Shape.Ellipse(50, y, rx, ry).Intersect(Shape.Box(50, y + ry, rx + 1, ry));
            cv.Fill(m.Expand(0.25f), new Color(0.4f, 0.12f, 0.15f));
            cv.Fill(m, inside);
            cv.Fill(Shape.Ellipse(50, y + ry * 0.85f, rx * 0.6f, ry * 0.45f).Intersect(m), tongue);
            if (fang) cv.Fill(Shape.Polygon(new[] { V(51f, y), V(52.4f, y), V(51.7f, y + 1.2f) }), White);
        }

        static void Curve(PixelCanvas cv, Vector2 a, Vector2 c, Vector2 b, float r, Color col)
        {
            cv.Fill(Shape.Polyline(Shape.Bezier(a, c, b, 10), r), col);
        }

        static void DrawSpots(PixelCanvas cv, Ctx p)
        {
            float cheekX = p.EyeDx + 1.5f, cheekY = p.EyeY + p.EyeRy + 3f;
            Color blush = new Color(1f, 0.45f, 0.5f, 0.38f);
            switch (p.D.spotType)
            {
                case 2: // 볼터치
                    cv.Fill(Shape.Ellipse(50 - cheekX, cheekY, 3.6f, 1.7f), blush);
                    cv.Fill(Shape.Ellipse(50 + cheekX, cheekY, 3.6f, 1.7f), blush);
                    break;
                case 3: // 주근깨
                {
                    Color c = new Color(0.65f, 0.4f, 0.3f, 0.55f);
                    float[] dx = { -1.6f, 0.2f, 1.8f, -0.6f, 1.0f };
                    float[] dy = { 0f, -0.8f, 0.1f, 1.1f, 1.2f };
                    for (int i = 0; i < dx.Length; i++)
                    {
                        cv.Fill(Shape.Circle(50 - cheekX + dx[i], cheekY - 1 + dy[i], 0.35f), c);
                        cv.Fill(Shape.Circle(50 + cheekX - dx[i], cheekY - 1 + dy[i], 0.35f), c);
                    }
                    break;
                }
                case 4: // 눈물점
                    cv.Fill(Shape.Circle(50 - p.EyeDx + 2.5f, p.EyeY + p.EyeRy + 1.6f, 0.45f), Ink);
                    break;
                case 5: // 반창고
                {
                    Shape tape = Shape.Box(50 + cheekX, cheekY, 3.2f, 1.2f, 0.6f);
                    cv.FillOutlined(tape, new Color(0.96f, 0.85f, 0.72f), new Color(0.75f, 0.6f, 0.5f), 0.3f);
                    cv.Fill(Shape.Box(50 + cheekX, cheekY, 1.1f, 0.8f, 0.2f), new Color(0.9f, 0.75f, 0.62f));
                    break;
                }
                case 6: // 홍조 빗금
                {
                    Color c = new Color(1f, 0.4f, 0.45f, 0.6f);
                    for (int i = -1; i <= 1; i++)
                    {
                        float x = i * 1.4f;
                        cv.Fill(Shape.Segment(50 - cheekX + x + 0.5f, cheekY - 1, 50 - cheekX + x - 0.5f, cheekY + 1, 0.22f), c);
                        cv.Fill(Shape.Segment(50 + cheekX + x + 0.5f, cheekY - 1, 50 + cheekX + x - 0.5f, cheekY + 1, 0.22f), c);
                    }
                    break;
                }
                case 7: // 입가 점
                    cv.Fill(Shape.Circle(54.2f, 61.8f + p.D.mouthPosition * 0.18f, 0.4f), Ink);
                    break;
            }
        }

        static void DrawGlasses(PixelCanvas cv, Ctx p)
        {
            if (p.D.glassesType != 2) return;
            Color frame = p.D.clothesColor == 2 ? new Color(0.7f, 0.15f, 0.2f) : new Color(0.18f, 0.16f, 0.2f);
            for (int side = -1; side <= 1; side += 2)
            {
                float cx = 50 + side * p.EyeDx;
                Shape lens = Shape.Box(cx, p.EyeY + 0.3f, p.EyeRx + 2.2f, p.EyeRy + 1.2f, 2.2f);
                cv.Fill(lens, new Color(1, 1, 1, 0.12f));
                cv.Fill(lens.Ring(0.42f), frame);
            }
            float inner = p.EyeDx - p.EyeRx - 2.2f;
            cv.Fill(Shape.Polyline(Shape.Bezier(V(50 - inner, p.EyeY - 1), V(50, p.EyeY - 2.4f), V(50 + inner, p.EyeY - 1), 6), 0.4f), frame);
        }

        // ---------------- 머리카락 (앞) ----------------

        static void DrawFrontHair(PixelCanvas cv, Ctx p)
        {
            int idx = p.D.fringeType - 1;
            int strands = 4 + idx % 4;
            int part = (idx / 4) % 3;     // 0 가운데, 1 왼쪽, 2 오른쪽 가르마
            bool longBangs = idx >= 11;
            float rx = p.Fw + 5.5f;

            float tipBase = longBangs ? p.EyeY - p.EyeRy + 0.5f : p.EyeY - p.EyeRy - 3.5f;
            float left = 50 - p.Fw - 2.5f, right = 50 + p.Fw + 2.5f;
            float partX = part == 0 ? 50 : part == 1 ? 50 - p.Fw * 0.45f : 50 + p.Fw * 0.45f;

            var pts = new List<Vector2>();
            // 머리 윗부분 호 (왼쪽 → 오른쪽)
            for (int i = 0; i <= 16; i++)
            {
                float a = Mathf.PI + Mathf.PI * i / 16f;
                pts.Add(V(50 + rx * Mathf.Cos(a), 41 + 26.5f * Mathf.Sin(a)));
            }
            pts.Add(V(right + 0.5f, 50));

            // 앞머리 끝: 오른쪽 → 왼쪽으로 뾰족한 가닥들
            float width = right - left;
            for (int i = strands - 1; i >= 0; i--)
            {
                float x0 = left + width * (i + 1) / strands;
                float xm = left + width * (i + 0.5f) / strands;
                bool nearPart = Mathf.Abs(x0 - partX) < width / strands * 0.6f;
                float valleyY = nearPart ? 24.5f : 31f + ((i * 7 + idx) % 3) * 1.2f;
                pts.Add(V(x0, i == strands - 1 ? tipBase - 4 : valleyY)); // 맨 오른쪽은 관자놀이, 나머지는 가닥 사이 골

                float sweep = (xm < partX ? -1f : 1f) * 1.4f;   // 가르마 반대쪽으로 쓸려 내려가게
                float tipY = tipBase + ((i * 5 + idx * 3) % 4) * 0.9f;
                if (i == 0 || i == strands - 1) tipY -= 2.5f;
                pts.Add(V(xm + sweep, tipY));
            }
            pts.Add(V(left, tipBase - 4));
            pts.Add(V(left - 0.5f, 50));

            Shape bangs = Shape.Polygon(pts);
            cv.FillOutlined(bangs, p.Hair, p.HairDark, 0.7f);

            // 옆머리
            float[] lockLen = { -4f, 3f, 9f, 11f };
            float lockEnd = 58f + lockLen[p.HairLength];
            Shape lockShape = p.HairFeature == 6
                ? Shape.Polygon(new[] { V(50 + p.Fw - 2, 34), V(50 + p.Fw + 4.5f, 36), V(50 + p.Fw + 4, lockEnd + 2), V(50 + p.Fw - 2.5f, lockEnd + 2) }) // 히메컷
                : Shape.Polygon(new[] { V(50 + p.Fw - 3, 33), V(50 + p.Fw + 4.5f, 36), V(50 + p.Fw + 3, lockEnd), V(50 + p.Fw - 0.5f, lockEnd + 6), V(50 + p.Fw - 2.5f, 50) });
            cv.FillOutlined(lockShape, p.Hair, p.HairDark, 0.6f);
            cv.FillOutlined(lockShape.MirrorX(50), p.Hair, p.HairDark, 0.6f);

            // 가닥 사이 결 표현 + 천사의 고리(하이라이트)
            for (int i = 1; i < strands; i++)
            {
                float x = left + width * i / strands;
                cv.Fill(Shape.Segment(x, 26f, x + (x < partX ? -0.6f : 0.6f), tipBase - 2.5f, 0.25f).Intersect(bangs), new Color(p.HairDark.r, p.HairDark.g, p.HairDark.b, 0.45f));
            }
            var ring = Shape.Bezier(V(50 - p.Fw + 1, 27.5f), V(50, 18f), V(50 + p.Fw - 1, 27.5f), 14);
            cv.Fill(Shape.Polyline(ring, 1.1f).Intersect(bangs.Expand(-0.8f)), new Color(p.HairLight.r, p.HairLight.g, p.HairLight.b, 0.7f));
        }

        // ---------------- 액세서리 ----------------

        static void DrawCatEars(PixelCanvas cv, Ctx p)
        {
            Shape ear = Shape.Polygon(new[] { V(50 - p.Fw + 1, 25), V(50 - p.Fw - 3, 7), V(50 - p.Fw + 12, 17) });
            Shape inner = Shape.Polygon(new[] { V(50 - p.Fw + 2.5f, 22), V(50 - p.Fw - 0.8f, 11), V(50 - p.Fw + 9, 18) });
            Color pink = new Color(1f, 0.7f, 0.75f);
            cv.FillOutlined(ear, p.Hair, p.HairDark, 0.6f);
            cv.Fill(inner, pink);
            cv.FillOutlined(ear.MirrorX(50), p.Hair, p.HairDark, 0.6f);
            cv.Fill(inner.MirrorX(50), pink);
        }

        static void DrawAccessory(PixelCanvas cv, Ctx p)
        {
            Color accent = Color.Lerp(Pick(EyeColors, p.D.eyeColor), new Color(1f, 0.3f, 0.45f), 0.35f);
            Color accentLine = Shade(accent, 0.5f);
            switch (p.D.accessoryType)
            {
                case 2: // 리본
                {
                    float x = 50 + p.Fw * 0.65f, y = 20.5f;
                    cv.FillOutlined(Shape.Polygon(new[] { V(x, y), V(x - 8, y - 6), V(x - 7, y + 5) }), accent, accentLine, 0.5f);
                    cv.FillOutlined(Shape.Polygon(new[] { V(x, y), V(x + 8, y - 6), V(x + 7, y + 5) }), accent, accentLine, 0.5f);
                    cv.FillOutlined(Shape.Circle(x, y, 2f), accent, accentLine, 0.5f);
                    break;
                }
                case 3: // 머리핀 두 개
                {
                    Color gold = new Color(0.98f, 0.82f, 0.3f);
                    float x = 50 - p.Fw * 0.7f;
                    cv.FillOutlined(Shape.Segment(x - 4, 31, x + 2.5f, 28.5f, 0.6f), gold, Shade(gold, 0.55f), 0.35f);
                    cv.FillOutlined(Shape.Segment(x - 3.5f, 34, x + 3f, 31.5f, 0.6f), accent, accentLine, 0.35f);
                    break;
                }
                case 4: // 머리띠
                {
                    var band = Shape.Bezier(V(50 - p.Fw - 5, 36), V(50, 6), V(50 + p.Fw + 5, 36), 18);
                    cv.FillOutlined(Shape.Polyline(band, 1.5f), accent, accentLine, 0.45f);
                    break;
                }
            }
        }

        // ---------------- 유틸 ----------------

        static Vector2 V(float x, float y) => new Vector2(x, y);

        static Color Pick(Color[] palette, int oneBased)
        {
            int i = Mathf.Clamp(oneBased - 1, 0, palette.Length - 1);
            return palette[i];
        }

        static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }

        static Color[] Hexes(params string[] hex)
        {
            var arr = new Color[hex.Length];
            for (int i = 0; i < hex.Length; i++) arr[i] = Hex(hex[i]);
            return arr;
        }
    }
}
