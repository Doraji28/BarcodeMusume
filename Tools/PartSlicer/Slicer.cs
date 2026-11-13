using BarcodeKanojo;

public static class Slicer
{
    const int W = 1024;

    public static Dictionary<string, PartImage> Run(string rawPath)
    {
        var raw = File.ReadAllBytes(rawPath);
        var defs = new (string n, int x0, int y0, int x1, int y1, int sx, int sy)[]
        {
            ("body",14,15,153,290,0,0),("head1",161,14,251,102,0,0),("head2",161,108,251,197,0,0),("head3",161,201,251,290,0,0),
            ("back_long",276,14,401,194,0,0),("back_wavy",407,12,513,138,0,0),("hair_twin",519,13,664,139,592,80),("hair_pony",671,12,774,134,735,82),
            ("hair_buns",780,18,899,110,838,92),("hair_bob",906,14,1010,110,955,88),
            ("bangs1",416,145,507,244,0,0),("bangs2",527,137,626,243,0,0),("bangs3",643,137,779,242,0,0),("bangs4",794,136,886,242,0,0),("bangs5",902,137,1010,228,0,0),
            ("eyes1",26,306,126,345,0,0),("eyes2",139,313,246,343,0,0),("eyes3",27,354,124,393,0,0),("eyes4",141,361,245,387,0,0),("eyes5",26,413,132,449,0,0),("eyes6",143,417,243,449,0,0),
            ("eyes7",279,251,380,288,0,0),("eyes8",277,297,383,333,0,0),("eyes9",397,303,512,331,0,0),("eyes10",531,313,640,337,0,0),
            ("brow1",399,258,510,279,0,0),("brow2",533,255,637,270,0,0),("brow3",533,281,639,292,0,0),("brow4",539,298,632,313,0,0),
            ("mouth1",668,264,705,282,0,0),("mouth2",719,264,755,285,0,0),("mouth3",767,263,806,282,0,0),("mouth4",823,264,853,287,0,0),
            ("mouth5",668,305,705,322,0,0),("mouth6",726,302,748,325,0,0),("mouth7",779,307,793,319,0,0),("nose",825,310,832,317,0,0),
            ("blush",886,256,944,291,0,0),("freckles",954,262,1002,282,0,0),("mole",929,309,938,318,0,0),("bandage",958,296,1001,324,0,0),
            ("outfit_sailor1",260,343,369,457,0,0),("outfit_sailor2",383,343,491,458,0,0),("outfit_blazer",504,343,611,457,0,0),("outfit_hoodie",626,343,738,454,0,0),
            ("outfit_dress",751,343,886,467,0,0),("outfit_tshirt",908,343,993,458,0,0),
            ("ribbon1",38,473,114,542,0,0),("ribbon2",157,480,231,532,0,0),("ribbon3",281,501,359,565,0,0),("hairpins",387,511,474,554,0,0),("headband",500,502,603,562,0,0),
            ("catear_l",626,504,673,562,0,0),("catear_r",695,504,742,562,0,0),("glasses",766,518,872,553,0,0),("elfear_l",892,509,942,558,0,0),("elfear_r",952,509,1001,558,0,0),
        };

        var result = new Dictionary<string, PartImage>();
        foreach (var d in defs)
        {
            int w = d.x1 - d.x0, h = d.y1 - d.y0;
            var px = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int si = ((d.y0 + y) * W + d.x0 + x) * 4;
                float b = raw[si], g = raw[si + 1], r = raw[si + 2];
                // 크로마키: 초록이 얼마나 튀는지(spill)로 투명도 계산, 배경색(2,254,8)을 빼서 원래 색 복원
                float spill = Math.Max(0, g - Math.Max(r, b));
                float a = 1 - spill / 246f;
                if (a < 0.08f) a = 0;
                if (a > 0.94f) a = 1;
                int o = (y * w + x) * 4;
                if (a <= 0) continue;
                float fr = (r - (1 - a) * 2) / a, fg = (g - (1 - a) * 254) / a, fb = (b - (1 - a) * 8) / a;
                if (a < 1)
                {
                    // 가장자리 초록/노랑 번짐 제거: 초록이 빨강·파랑보다 밝을 수 없게 하고, 채도를 낮춘다
                    fg = Math.Min(fg, Math.Max(fr, fb));
                    float l = (fr + fg + fb) / 3f;
                    float keep = a; // 많이 투명할수록 색 정보를 덜 믿는다
                    fr = l + (fr - l) * keep; fg = l + (fg - l) * keep; fb = l + (fb - l) * keep;
                }
                px[o] = C(fr); px[o + 1] = C(fg); px[o + 2] = C(fb); px[o + 3] = C(a * 255);
            }
            if (d.n == "blush") Recolor(px, 255, 140, 160);
            if (d.sx > 0) FloodClear(px, w, h, d.sx - d.x0, d.sy - d.y0);
            result[d.n] = Trim(px, w, h);
        }
        return result;
    }

    static byte C(float v) => (byte)Math.Clamp(v + 0.5f, 0, 255);

    /// <summary>
    /// 반투명 가장자리 픽셀은 압축(색 서브샘플링) 때문에 색이 틀어져 있다(노랑/초록 테두리).
    /// 투명도는 그대로 두고 색만 가장 가까운 불투명 픽셀 색으로 바꾼다.
    /// </summary>
    static void BleedEdges(byte[] px, int w, int h)
    {
        var src = (byte[])px.Clone();
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = (y * w + x) * 4;
            if (src[i + 3] == 0 || src[i + 3] == 255) continue;
            int best = -1, bestD = int.MaxValue;
            for (int dy = -3; dy <= 3; dy++)
            for (int dx = -3; dx <= 3; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                int j = (ny * w + nx) * 4;
                if (src[j + 3] < 250) continue;
                int dd = dx * dx + dy * dy;
                if (dd < bestD) { bestD = dd; best = j; }
            }
            if (best >= 0) { px[i] = src[best]; px[i + 1] = src[best + 1]; px[i + 2] = src[best + 2]; }
            else
            {
                // 주변에 불투명 픽셀이 없으면 초록 기운만 뺀다
                byte m = Math.Max(px[i], px[i + 2]);
                if (px[i + 1] > m) px[i + 1] = m;
            }
        }
    }

    static void Recolor(byte[] px, byte r, byte g, byte b)
    {
        for (int i = 0; i < px.Length; i += 4) { px[i] = r; px[i + 1] = g; px[i + 2] = b; }
    }

    /// <summary>머리카락 그림 안의 회색 얼굴 자리를 투명하게.</summary>
    static void FloodClear(byte[] px, int w, int h, int sx, int sy)
    {
        int s = (sy * w + sx) * 4;
        int sr = px[s], sg = px[s + 1], sb = px[s + 2];
        var st = new Stack<int>();
        var seen = new bool[w * h];
        st.Push(sy * w + sx); seen[sy * w + sx] = true;
        int n = 0;
        while (st.Count > 0)
        {
            int p = st.Pop();
            px[p * 4 + 3] = 0; n++;
            int x = p % w, y = p / w;
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                int q = ny * w + nx;
                if (seen[q]) continue;
                int j = q * 4;
                if (px[j + 3] > 0 && Math.Abs(px[j] - sr) < 26 && Math.Abs(px[j + 1] - sg) < 26 && Math.Abs(px[j + 2] - sb) < 26)
                {
                    seen[q] = true; st.Push(q);
                }
            }
        }
        // 회색 얼굴의 윤곽선(어두운 선)도 지운다: 지운 영역에서 3px 안쪽의 어두운 픽셀
        for (int pass = 0; pass < 5; pass++)
        {
            var clear = new List<int>();
            for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int i = (y * w + x) * 4;
                if (px[i + 3] == 0 || !seenNear(x, y)) continue;
                float lum = (px[i] + px[i + 1] + px[i + 2]) / 3f;
                // 아래쪽 1/4(턱선)은 연한 점선까지 지운다
                float limit = pass >= 3 ? (y > h * 6 / 10 ? 225 : 0) : 150;
                if (lum < limit || (y > h * 6 / 10 && px[i + 3] < 200)) clear.Add(y * w + x);
            }
            foreach (int p in clear) { px[p * 4 + 3] = 0; seen[p] = true; }
        }
        bool seenNear(int x, int y) => seen[y * w + x - 1] || seen[y * w + x + 1] || seen[(y - 1) * w + x] || seen[(y + 1) * w + x];
        Console.WriteLine($"  flood cleared {n}px seed=({sr},{sg},{sb})");
    }

    static PartImage Trim(byte[] px, int w, int h)
    {
        int x0 = w, y0 = h, x1 = -1, y1 = -1;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            if (px[(y * w + x) * 4 + 3] > 10) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
        x0 = Math.Max(0, x0 - 1); y0 = Math.Max(0, y0 - 1); x1 = Math.Min(w - 1, x1 + 1); y1 = Math.Min(h - 1, y1 + 1);
        int nw = x1 - x0 + 1, nh = y1 - y0 + 1;
        var o = new byte[nw * nh * 4];
        for (int y = 0; y < nh; y++) Buffer.BlockCopy(px, ((y + y0) * w + x0) * 4, o, y * nw * 4, nw * 4);
        return new PartImage(nw, nh, o);
    }
}
