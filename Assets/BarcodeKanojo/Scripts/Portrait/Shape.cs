using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarcodeKanojo
{
    /// <summary>
    /// 부호 거리 함수(SDF)로 정의한 2D 도형. 좌표계는 0~100 "디자인 단위", x는 오른쪽, y는 아래쪽.
    /// Sdf(x, y) &lt; 0 이면 도형 안, &gt; 0 이면 밖. 경계 근처 값으로 안티앨리어싱한다.
    /// </summary>
    public class Shape
    {
        public Func<float, float, float> Sdf;
        public float X0, Y0, X1, Y1; // 대략적인 경계 상자 (이 밖은 그리지 않는다)

        public Shape(Func<float, float, float> sdf, float x0, float y0, float x1, float y1)
        {
            Sdf = sdf; X0 = x0; Y0 = y0; X1 = x1; Y1 = y1;
        }

        // ---------- 기본 도형 ----------

        public static Shape Circle(float cx, float cy, float r)
        {
            return new Shape((x, y) => Len(x - cx, y - cy) - r, cx - r, cy - r, cx + r, cy + r);
        }

        public static Shape Ellipse(float cx, float cy, float rx, float ry)
        {
            float m = Mathf.Min(rx, ry);
            return new Shape((x, y) =>
            {
                float k = Len((x - cx) / rx, (y - cy) / ry);
                return (k - 1f) * m;
            }, cx - rx, cy - ry, cx + rx, cy + ry);
        }

        public static Shape Box(float cx, float cy, float hx, float hy, float round = 0f)
        {
            return new Shape((x, y) =>
            {
                float qx = Mathf.Abs(x - cx) - hx + round, qy = Mathf.Abs(y - cy) - hy + round;
                return Len(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - round;
            }, cx - hx, cy - hy, cx + hx, cy + hy);
        }

        /// <summary>두께 r인 선분(캡슐).</summary>
        public static Shape Segment(float ax, float ay, float bx, float by, float r)
        {
            return new Shape((x, y) => SegDist(x, y, ax, ay, bx, by) - r,
                Mathf.Min(ax, bx) - r, Mathf.Min(ay, by) - r, Mathf.Max(ax, bx) + r, Mathf.Max(ay, by) + r);
        }

        /// <summary>두께 r인 꺾은선.</summary>
        public static Shape Polyline(IList<Vector2> pts, float r)
        {
            var p = new List<Vector2>(pts);
            GetBounds(p, out float x0, out float y0, out float x1, out float y1);
            return new Shape((x, y) =>
            {
                float d = float.MaxValue;
                for (int i = 0; i + 1 < p.Count; i++)
                    d = Mathf.Min(d, SegDist(x, y, p[i].x, p[i].y, p[i + 1].x, p[i + 1].y));
                return d - r;
            }, x0 - r, y0 - r, x1 + r, y1 + r);
        }

        /// <summary>임의의 단순 다각형 (Inigo Quilez의 polygon SDF).</summary>
        public static Shape Polygon(IList<Vector2> pts)
        {
            var v = new List<Vector2>(pts).ToArray();
            GetBounds(v, out float x0, out float y0, out float x1, out float y1);
            return new Shape((x, y) =>
            {
                float d = (x - v[0].x) * (x - v[0].x) + (y - v[0].y) * (y - v[0].y);
                float s = 1f;
                for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
                {
                    float ex = v[j].x - v[i].x, ey = v[j].y - v[i].y;
                    float wx = x - v[i].x, wy = y - v[i].y;
                    float len2 = ex * ex + ey * ey;
                    if (len2 < 1e-8f) continue; // 겹친 점(길이 0인 변)은 건너뛴다 — 0으로 나누면 NaN
                    float t = Mathf.Clamp01((wx * ex + wy * ey) / len2);
                    float bx = wx - ex * t, by = wy - ey * t;
                    d = Mathf.Min(d, bx * bx + by * by);
                    bool c1 = y >= v[i].y, c2 = y < v[j].y, c3 = ex * wy > ey * wx;
                    if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
                }
                return s * Mathf.Sqrt(d);
            }, x0, y0, x1, y1);
        }

        /// <summary>2차 베지어 곡선 위의 점들.</summary>
        public static List<Vector2> Bezier(Vector2 a, Vector2 c, Vector2 b, int steps = 12)
        {
            var list = new List<Vector2>(steps + 1);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps, u = 1f - t;
                list.Add(u * u * a + 2f * u * t * c + t * t * b);
            }
            return list;
        }

        // ---------- 조합 ----------

        public Shape Union(Shape o)
        {
            var a = Sdf; var b = o.Sdf;
            return new Shape((x, y) => Mathf.Min(a(x, y), b(x, y)),
                Mathf.Min(X0, o.X0), Mathf.Min(Y0, o.Y0), Mathf.Max(X1, o.X1), Mathf.Max(Y1, o.Y1));
        }

        public Shape Intersect(Shape o)
        {
            var a = Sdf; var b = o.Sdf;
            return new Shape((x, y) => Mathf.Max(a(x, y), b(x, y)),
                Mathf.Max(X0, o.X0), Mathf.Max(Y0, o.Y0), Mathf.Min(X1, o.X1), Mathf.Min(Y1, o.Y1));
        }

        public Shape Subtract(Shape o)
        {
            var a = Sdf; var b = o.Sdf;
            return new Shape((x, y) => Mathf.Max(a(x, y), -b(x, y)), X0, Y0, X1, Y1);
        }

        /// <summary>도형을 e만큼 부풀린다 (외곽선용).</summary>
        public Shape Expand(float e)
        {
            var a = Sdf;
            return new Shape((x, y) => a(x, y) - e, X0 - e, Y0 - e, X1 + e, Y1 + e);
        }

        /// <summary>도형의 테두리만 남긴다 (두께 t).</summary>
        public Shape Ring(float t)
        {
            var a = Sdf;
            return new Shape((x, y) => Mathf.Abs(a(x, y)) - t, X0 - t, Y0 - t, X1 + t, Y1 + t);
        }

        public Shape Offset(float dx, float dy)
        {
            var a = Sdf;
            return new Shape((x, y) => a(x - dx, y - dy), X0 + dx, Y0 + dy, X1 + dx, Y1 + dy);
        }

        /// <summary>x = cx 축으로 좌우 반전.</summary>
        public Shape MirrorX(float cx)
        {
            var a = Sdf;
            return new Shape((x, y) => a(2f * cx - x, y), 2f * cx - X1, Y0, 2f * cx - X0, Y1);
        }

        // ---------- 유틸 ----------

        static float Len(float x, float y) => Mathf.Sqrt(x * x + y * y);

        static float SegDist(float px, float py, float ax, float ay, float bx, float by)
        {
            float pax = px - ax, pay = py - ay, bax = bx - ax, bay = by - ay;
            float len2 = bax * bax + bay * bay;
            float h = len2 > 0f ? Mathf.Clamp01((pax * bax + pay * bay) / len2) : 0f;
            return Len(pax - bax * h, pay - bay * h);
        }

        static void GetBounds(IList<Vector2> p, out float x0, out float y0, out float x1, out float y1)
        {
            x0 = y0 = float.MaxValue; x1 = y1 = float.MinValue;
            foreach (var v in p)
            {
                x0 = Mathf.Min(x0, v.x); y0 = Mathf.Min(y0, v.y);
                x1 = Mathf.Max(x1, v.x); y1 = Mathf.Max(y1, v.y);
            }
        }
    }
}
