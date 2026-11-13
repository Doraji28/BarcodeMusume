using BarcodeKanojo;

var parts = Slicer.Run("sheet.bgra");
if (args.Length > 0 && args[0] == "template") { Template.Make(parts["body"]); return; }

if (args.Length > 0 && args[0] == "export")
{
    Directory.CreateDirectory("stage");
    foreach (var kv in parts) Png.Save($"stage/{kv.Key}.png", kv.Value.Width, kv.Value.Height, kv.Value.Rgba, 4);
    Console.WriteLine($"exported {parts.Count}");
    return;
}

if (args.Length > 0 && args[0] == "zoom") { int k = 4; var list = args[1..].Select(n => parts[n]).ToList(); int W2 = list.Sum(p => p.Width * k + 10), H2 = list.Max(p => p.Height * k); var o = new byte[W2 * H2 * 3]; for (int i = 0; i < W2 * H2; i++) { int x = i % W2, y = i / W2; byte v = (byte)(((x / 16 + y / 16) % 2 == 0) ? 70 : 110); o[i*3]=v; o[i*3+1]=(byte)(v/2); o[i*3+2]=v; } int ox = 0; foreach (var p in list) { for (int y = 0; y < p.Height * k; y++) for (int x = 0; x < p.Width * k; x++) { int i = ((y / k) * p.Width + x / k) * 4; float a = p.Rgba[i + 3] / 255f; int j = (y * W2 + ox + x) * 3; for (int c = 0; c < 3; c++) o[j + c] = (byte)(o[j + c] * (1 - a) + p.Rgba[i + c] * a); } ox += p.Width * k + 10; } Png.Save("out/zoom.png", W2, H2, o, 3); return; }
if (args.Length > 0 && args[0] == "auto") { AutoLayout.Print(parts); var b = parts["body"]; int i = (50 * b.Width + 69) * 4; Console.WriteLine($"skin {b.Rgba[i]},{b.Rgba[i+1]},{b.Rgba[i+2]}"); return; }
if (args.Length > 0 && args[0] == "summary") { foreach (var kv in parts) if (kv.Key.StartsWith("hair_") || kv.Key.StartsWith("bangs") || kv.Key.StartsWith("back_") || kv.Key.StartsWith("head")) Measure2.Summary(kv.Key, kv.Value); return; }
if (args.Length > 0 && args[0] == "measure") { foreach (var n in args[1..]) Measure.Rows(n, parts[n], 6); return; }
if (args.Length > 0 && args[0] == "sizes")
{
    foreach (var kv in parts) Console.WriteLine($"{kv.Key} {kv.Value.Width}x{kv.Value.Height}");
    return;
}

// 1) 잘라낸 파츠 한눈에 보기 (체크무늬 배경)
{
    int S = 900;
    var o = new byte[S * S * 3];
    for (int i = 0; i < S * S; i++) { int x = i % S, y = i / S; byte v = (byte)(((x / 10 + y / 10) % 2 == 0) ? 190 : 235); o[i * 3] = v; o[i * 3 + 1] = v; o[i * 3 + 2] = v; }
    int cx = 5, cy = 5, rowH = 0;
    foreach (var kv in parts)
    {
        var p = kv.Value; int sc = p.Width < 40 ? 3 : 1;
        if (cx + p.Width * sc > S) { cx = 5; cy += rowH + 5; rowH = 0; }
        for (int y = 0; y < p.Height * sc; y++)
        for (int x = 0; x < p.Width * sc; x++)
        {
            if (cy + y >= S) continue;
            int i = ((y / sc) * p.Width + x / sc) * 4; float a = p.Rgba[i + 3] / 255f; int j = ((cy + y) * S + cx + x) * 3;
            for (int c = 0; c < 3; c++) o[j + c] = (byte)(o[j + c] * (1 - a) + p.Rgba[i + c] * a);
        }
        cx += p.Width * sc + 5; rowH = Math.Max(rowH, p.Height * sc);
    }
    Directory.CreateDirectory("out");
    Png.Save("out/overview.png", S, S, o, 3);
}

// 2) 실제 합성 테스트
string[] codes = args.Length > 0 ? args : new[] { "4901234567894", "8801234567893", "9780306406157", "4006381333931", "0012345678905", "8809876543217", "4902102072618", "8801043014809" };
int N = 300, cols = 4, rows = (codes.Length + cols - 1) / cols;
var sheet = new byte[N * cols * N * rows * 3];
var buf = new byte[N * N * 4];
for (int k = 0; k < codes.Length; k++)
{
    var d = KanojoGenerator.Generate(codes[k]);
    KanojoComposer.Compose(d, n => parts.TryGetValue(n, out var p) ? p : null, buf, N);
    int ox = (k % cols) * N, oy = (k / cols) * N;
    for (int y = 0; y < N; y++)
    for (int x = 0; x < N; x++)
    {
        int i = (y * N + x) * 4, j = ((oy + y) * N * cols + ox + x) * 3;
        sheet[j] = buf[i]; sheet[j + 1] = buf[i + 1]; sheet[j + 2] = buf[i + 2];
    }
    Console.WriteLine($"{k}: {codes[k]} style{(d.hairType - 1) % 7} bangs{(d.fringeType - 1) % 5 + 1} eyes{(d.eyeType - 101) % 10 + 1} clothes{d.clothesType} acc{d.accessoryType} spot{d.spotType} face{(d.faceType - 1) % 4} ear{d.earType} glasses{d.glassesType}");
}
Png.Save("out/compose.png", N * cols, N * rows, sheet, 3);
