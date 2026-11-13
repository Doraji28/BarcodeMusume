using BarcodeKanojo;

/// <summary>
/// 6 x 2 칸 파츠 시트(2000x1493)에서 칸 단위로 파츠를 잘라낸다.
/// 모든 시트가 같은 칸 위치에 맞춰 그려져 있으므로, 잘라낸 그림의 "칸 안 좌표"(offset)를 그대로 저장해 두면
/// 합성할 때 따로 위치를 맞출 필요가 없다.
/// </summary>
public static class SlicerV2
{
    public const int SheetW = 2000, SheetH = 1493;
    public const float CellW = SheetW / 6f;
    public const int CellH = 747;

    public enum Mode { Key, KeyFlood, Feature }

    public record Def(string Name, int Sheet, int Row, int Col, int Y0 = 0, int Y1 = 746, Mode Mode = Mode.Key, (int x, int y)[] Seeds = null);

    public record Part(string Name, int OffsetX, int OffsetY, PartImage Image);

    public static readonly Def[] Defs =
    {
        // 기본 몸 (얼굴형 4종)
        new("body1", 7, 0, 0), new("body2", 7, 0, 1), new("body3", 7, 0, 2), new("body4", 7, 0, 3),
        // 눈 (윗줄 6 + 아랫줄의 다른 처진 눈 1) + 감은 눈 2 (눈썹/입 시트에 덤으로 그려진 것)
        new("eyes1", 8, 0, 0), new("eyes2", 8, 0, 1), new("eyes3", 8, 0, 2), new("eyes4", 8, 0, 3), new("eyes5", 8, 0, 4), new("eyes6", 8, 0, 5),
        new("eyes7", 8, 1, 1),
        new("eyes_closed1", 14, 0, 0, 232, 290), new("eyes_closed2", 14, 0, 4, 232, 290),
        // 옷 (윗줄 6벌만. 아랫줄은 몸보다 크게 그려져서 제외)
        new("outfit_sailor", 9, 0, 0), new("outfit_blazer", 9, 0, 1), new("outfit_hoodie", 9, 0, 2),
        new("outfit_dress", 9, 0, 3), new("outfit_tshirt", 9, 0, 4), new("outfit_cardigan", 9, 0, 5),
        // 뒷머리 9
        new("back1", 10, 0, 0), new("back2", 10, 0, 1), new("back3", 10, 0, 2), new("back4", 10, 0, 3), new("back5", 10, 0, 4), new("back6", 10, 0, 5),
        new("back7", 10, 1, 0), new("back8", 10, 1, 1), new("back9", 10, 1, 2),
        // 앞머리 10 (긴 생머리/웨이브/포니테일은 안쪽 회색 면을 지운다)
        new("front1", 11, 0, 0, Mode: Mode.KeyFlood, Seeds: new[] { (165, 470) }),
        new("front2", 11, 0, 1, Mode: Mode.KeyFlood, Seeds: new[] { (167, 470) }),
        new("front3", 11, 0, 2),
        new("front4", 11, 0, 3, Mode: Mode.KeyFlood, Seeds: new[] { (122, 362) }),
        new("front5", 11, 0, 4), new("front6", 11, 0, 5),
        new("front7", 11, 1, 0), new("front8", 11, 1, 1), new("front9", 11, 1, 2), new("front10", 11, 1, 5, 0, 600),
        // 볼·코 (머리 그림에서 피부색과 다른 부분만 뽑는다)
        new("blush", 12, 0, 0, Mode: Mode.Feature), new("freckles", 12, 0, 1, Mode: Mode.Feature), new("mole", 12, 0, 2, Mode: Mode.Feature),
        new("bandage", 12, 0, 3, Mode: Mode.Feature), new("nose1", 12, 0, 4, Mode: Mode.Feature), new("nose2", 12, 0, 5, Mode: Mode.Feature),
        // 장식
        new("ribbon", 13, 0, 0), new("hairpins", 13, 0, 1), new("headband", 13, 0, 2),
        new("catears", 13, 0, 3), new("elfears", 13, 0, 4), new("glasses", 13, 0, 5),
        // 눈썹 6 / 입 6
        new("brow1", 14, 0, 0, 130, 232), new("brow2", 14, 0, 1, 130, 232), new("brow3", 14, 0, 2, 130, 232),
        new("brow4", 14, 0, 3, 130, 232), new("brow5", 14, 0, 4, 130, 232), new("brow6", 14, 0, 5, 130, 232),
        new("mouth1", 14, 0, 0, 285, 400), new("mouth2", 14, 0, 1, 285, 400), new("mouth3", 14, 0, 2, 285, 400),
        new("mouth4", 14, 0, 3, 285, 400), new("mouth5", 14, 0, 4, 285, 400), new("mouth6", 14, 0, 5, 285, 400),
    };

    public static List<Part> Run(string dir)
    {
        var sheets = new Dictionary<int, byte[]>();
        var result = new List<Part>();
        foreach (var d in Defs)
        {
            if (!sheets.TryGetValue(d.Sheet, out var raw)) sheets[d.Sheet] = raw = File.ReadAllBytes(Path.Combine(dir, $"s{d.Sheet}.bgra"));

            int cellX = (int)Math.Round(d.Col * CellW), cellY = d.Row * CellH;
            // 칸 경계선(진한 초록)을 피하려고 4px 안쪽만 쓴다
            int x0 = Math.Max(4, 0) , x1 = (int)Math.Round((d.Col + 1) * CellW) - cellX - 4;
            int y0 = Math.Max(4, d.Y0), y1 = Math.Min(742, d.Y1);
            int w = x1 - x0, h = y1 - y0;

            var px = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int si = ((cellY + y0 + y) * SheetW + cellX + x0 + x) * 4;
                float b = raw[si], g = raw[si + 1], r = raw[si + 2];
                KeyPixel(r, g, b, out byte[] rgba);
                Buffer.BlockCopy(rgba, 0, px, (y * w + x) * 4, 4);
            }

            if (d.Mode == Mode.KeyFlood)
                foreach (var s in d.Seeds) Flood.Clear(px, w, h, s.x - x0, s.y - y0);
            if (d.Mode == Mode.Feature)
                ExtractFeature(px, w, h, x0, y0);

            var (img, ox, oy) = Trim(px, w, h);
            if (img == null) { Console.WriteLine($"  !! {d.Name}: empty"); continue; }
            result.Add(new Part(d.Name, x0 + ox, y0 + oy, img));
        }
        return result;
    }

    /// <summary>초록 크로마키: 초록이 얼마나 튀는지(spill)로 투명도, 배경(약 5,254,2)을 빼서 원래 색 복원.</summary>
    static void KeyPixel(float r, float g, float b, out byte[] rgba)
    {
        rgba = new byte[4];
        float spill = Math.Max(0, g - Math.Max(r, b));
        float a = 1 - spill / 248f;
        if (a < 0.08f) a = 0;
        if (a > 0.94f) a = 1;
        if (a <= 0) return;
        float fr = (r - (1 - a) * 5) / a, fg = (g - (1 - a) * 254) / a, fb = (b - (1 - a) * 2) / a;
        if (a < 1)
        {
            fg = Math.Min(fg, Math.Max(fr, fb));
            float l = (fr + fg + fb) / 3f;
            fr = l + (fr - l) * a; fg = l + (fg - l) * a; fb = l + (fb - l) * a;
        }
        rgba[0] = C(fr); rgba[1] = C(fg); rgba[2] = C(fb); rgba[3] = C(a * 255);
    }

    /// <summary>
    /// 머리(피부)를 그린 칸에서 피부와 다른 부분(볼터치, 점, 반창고, 코)만 남긴다.
    /// 머리 윤곽선과 귀는 버린다: 머리 영역을 9px 깎고, 귀가 있는 좌우 끝은 제외.
    /// </summary>
    static void ExtractFeature(byte[] px, int w, int h, int x0, int y0)
    {
        var inside = new bool[w * h];
        for (int i = 0; i < w * h; i++) inside[i] = px[i * 4 + 3] > 128;
        var eroded = new bool[w * h];
        const int E = 9;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            bool ok = inside[y * w + x];
            for (int dy = -E; dy <= E && ok; dy += 3)
            for (int dx = -E; dx <= E && ok; dx += 3)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h || !inside[ny * w + nx]) ok = false;
            }
            eroded[y * w + x] = ok;
        }

        float[] skin = { 238, 211, 193 };
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = (y * w + x) * 4;
            int cx = x + x0;
            if (!eroded[y * w + x] || cx < 60 || cx > 270) { px[i + 3] = 0; continue; }
            float r = px[i], g = px[i + 1], b = px[i + 2];
            float d = Math.Max(Math.Abs(r - skin[0]), Math.Max(Math.Abs(g - skin[1]), Math.Abs(b - skin[2])));
            float a = Math.Clamp((d - 12) / 45f, 0, 1);
            if (a <= 0) { px[i + 3] = 0; continue; }
            px[i] = C((r - (1 - a) * skin[0]) / a);
            px[i + 1] = C((g - (1 - a) * skin[1]) / a);
            px[i + 2] = C((b - (1 - a) * skin[2]) / a);
            px[i + 3] = C(a * 255);
        }
    }

    static byte C(float v) => (byte)Math.Clamp(v + 0.5f, 0, 255);

    static (PartImage, int, int) Trim(byte[] px, int w, int h)
    {
        int x0 = w, y0 = h, x1 = -1, y1 = -1;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            if (px[(y * w + x) * 4 + 3] > 10) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
        if (x1 < 0) return (null, 0, 0);
        x0 = Math.Max(0, x0 - 1); y0 = Math.Max(0, y0 - 1); x1 = Math.Min(w - 1, x1 + 1); y1 = Math.Min(h - 1, y1 + 1);
        int nw = x1 - x0 + 1, nh = y1 - y0 + 1;
        var o = new byte[nw * nh * 4];
        for (int y = 0; y < nh; y++) Buffer.BlockCopy(px, ((y + y0) * w + x0) * 4, o, y * nw * 4, nw * 4);
        return (new PartImage(nw, nh, o), x0, y0);
    }
}

/// <summary>머리카락 그림 안쪽의 평평한 회색 면과 그 윤곽선을 투명하게.</summary>
public static class Flood
{
    public static void Clear(byte[] px, int w, int h, int sx, int sy)
    {
        int s = (sy * w + sx) * 4;
        int sr = px[s], sg = px[s + 1], sb = px[s + 2];
        var seen = new bool[w * h];
        var st = new Stack<int>();
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
                if (px[j + 3] > 0 && Math.Abs(px[j] - sr) < 20 && Math.Abs(px[j + 1] - sg) < 20 && Math.Abs(px[j + 2] - sb) < 20)
                { seen[q] = true; st.Push(q); }
            }
        }
        // 회색 면 둘레의 어두운 선(3px)도 지운다
        for (int pass = 0; pass < 3; pass++)
        {
            var clear = new List<int>();
            for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int i = (y * w + x) * 4;
                if (px[i + 3] == 0) continue;
                int p = y * w + x;
                if (!(seen[p - 1] || seen[p + 1] || seen[p - w] || seen[p + w])) continue;
                float lum = (px[i] + px[i + 1] + px[i + 2]) / 3f;
                if (lum < 150 || px[i + 3] < 200) clear.Add(p);
            }
            foreach (int p in clear) { px[p * 4 + 3] = 0; seen[p] = true; }
        }
        Console.WriteLine($"  flood cleared {n}px seed=({sr},{sg},{sb})");
    }
}
