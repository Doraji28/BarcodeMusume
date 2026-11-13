using BarcodeKanojo;

public static class Measure
{
    /// <summary>알파 기준으로 각 행의 [왼쪽, 오른쪽] 끝을 출력 (불투명 >128).</summary>
    public static void Rows(string name, PartImage p, int step = 4)
    {
        Console.WriteLine($"== {name} {p.Width}x{p.Height}");
        for (int y = 0; y < p.Height; y += step)
        {
            int l = -1, r = -1;
            for (int x = 0; x < p.Width; x++) if (p.Rgba[(y * p.Width + x) * 4 + 3] > 128) { if (l < 0) l = x; r = x; }
            // 가운데 줄에서 투명한 구간(얼굴 구멍) 찾기
            int mid = p.Width / 2; int hl = -1, hr = -1;
            if (p.Rgba[(y * p.Width + mid) * 4 + 3] < 60)
            {
                hl = mid; while (hl > 0 && p.Rgba[(y * p.Width + hl - 1) * 4 + 3] < 60) hl--;
                hr = mid; while (hr < p.Width - 1 && p.Rgba[(y * p.Width + hr + 1) * 4 + 3] < 60) hr++;
            }
            Console.WriteLine($"  y{y,3}: {l,3}..{r,3} w{(l < 0 ? 0 : r - l + 1),3}  hole {(hl < 0 ? "-" : $"{hl}..{hr} w{hr - hl + 1}")}");
        }
    }
}

public static class Measure2
{
    public static void Summary(string name, PartImage p)
    {
        int top = -1, hole = -1, domeW = 0, domeL = 0, domeR = 0;
        for (int y = 0; y < p.Height; y++)
        {
            int l = -1, r = -1;
            for (int x = 0; x < p.Width; x++) if (p.Rgba[(y * p.Width + x) * 4 + 3] > 128) { if (l < 0) l = x; r = x; }
            if (l < 0) continue;
            if (top < 0) top = y;
            bool midClear = p.Rgba[(y * p.Width + p.Width / 2) * 4 + 3] < 60;
            if (y > top + 10 && midClear && hole < 0) hole = y;
            if (hole < 0 && r - l + 1 > domeW) { domeW = r - l + 1; domeL = l; domeR = r; }
        }
        Console.WriteLine($"{name}: size {p.Width}x{p.Height} top {top} holeStart {hole} domeW {domeW} ({domeL}..{domeR}) center {(domeL + domeR) / 2.0}");
    }
}

public static class AutoLayout
{
    // 기본 몸: Place(150,160,134), 원본 139x275 → 0.964 단위/px, 머리 위끝 28.4, 머리 폭 99.3
    const float HeadTop = 28.4f, HeadW = 99.3f;

    public static void Print(Dictionary<string, PartImage> parts)
    {
        var dome = new Dictionary<string, (float dome, float k, float lift)>
        {
            // (머리통 부분 폭 px, 머리 폭 대비 배율, 머리 위로 띄우는 단위)
            ["back_long"] = (122, 1.14f, 3), ["back_wavy"] = (103, 1.12f, 3),
            ["hair_twin"] = (100, 1.04f, 4), ["hair_pony"] = (91, 1.04f, 4), ["hair_buns"] = (96, 1.04f, 4), ["hair_bob"] = (96, 1.04f, 4),
            ["bangs1"] = (88, 1.04f, 4), ["bangs2"] = (96, 1.04f, 4), ["bangs3"] = (132, 1.10f, 4), ["bangs4"] = (89, 1.0f, 4), ["bangs5"] = (104, 1.06f, 4),
            ["head1"] = (87, 1.0f, 0), ["head2"] = (87, 1.0f, 0), ["head3"] = (87, 1.0f, 0),
        };
        foreach (var kv in dome)
        {
            var p = parts[kv.Key];
            float s = HeadW * kv.Value.k / kv.Value.dome;
            int top = 0; while (top < p.Height && !RowHas(p, top)) top++;
            float topUnits = HeadTop - kv.Value.lift;
            float cy = topUnits - top * s + p.Height * s / 2f;
            Console.WriteLine($"            [\"{kv.Key}\"] = new Place(150, {cy:F1}f, {p.Width * s:F1}f),");
        }
    }

    static bool RowHas(PartImage p, int y)
    {
        for (int x = 0; x < p.Width; x++) if (p.Rgba[(y * p.Width + x) * 4 + 3] > 128) return true;
        return false;
    }
}
