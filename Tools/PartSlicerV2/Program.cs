using System.Text;
using BarcodeKanojo;

// 사용법 (이 폴더에서):
//   dotnet run -- slice <시트 bgra 폴더>      파츠 자르기 → stage/*.png + KanojoPartOffsets.cs 생성
//   dotnet run -- render <시트 bgra 폴더> [바코드...]   합성 테스트 → out/compose.png
string dir = args.Length > 1 ? args[1] : "sheets";
var parts = SlicerV2.Run(dir);

if (args.Length > 0 && args[0] == "slice")
{
    Directory.CreateDirectory("stage");
    foreach (var f in Directory.GetFiles("stage", "*.png")) File.Delete(f);
    foreach (var p in parts) Png.Save($"stage/{p.Name}.png", p.Image.Width, p.Image.Height, p.Image.Rgba, 4);

    var sb = new StringBuilder();
    sb.AppendLine("// 이 파일은 Tools/PartSlicerV2 가 자동으로 만든다. 직접 고치지 말 것.");
    sb.AppendLine("using System.Collections.Generic;");
    sb.AppendLine();
    sb.AppendLine("namespace BarcodeKanojo");
    sb.AppendLine("{");
    sb.AppendLine("    /// <summary>파츠 이름 → 시트 칸 안에서 잘라낸 그림의 왼쪽 위 좌표(px).</summary>");
    sb.AppendLine("    public static class KanojoPartOffsets");
    sb.AppendLine("    {");
    sb.AppendLine("        public static readonly Dictionary<string, (int x, int y)> Offsets = new Dictionary<string, (int x, int y)>");
    sb.AppendLine("        {");
    foreach (var p in parts) sb.AppendLine($"            [\"{p.Name}\"] = ({p.OffsetX}, {p.OffsetY}),");
    sb.AppendLine("        };");
    sb.AppendLine("    }");
    sb.AppendLine("}");
    File.WriteAllText("KanojoPartOffsets.cs", sb.ToString(), new UTF8Encoding(false));
    Console.WriteLine($"sliced {parts.Count} parts");
    return;
}

// 합성 테스트
var map = parts.ToDictionary(p => p.Name, p => p.Image);
string[] codes = args.Length > 2 ? args[2..] : new[]
{
    "4901234567894", "8801234567893", "9780306406157", "4006381333931", "0012345678905", "8809876543217",
    "4902102072618", "8801043014809", "4901234567895", "4901234567811", "4901234567802", "4901234567837",
};
int N = 300, cols = 6, rows = (codes.Length + cols - 1) / cols;
var sheet = new byte[N * cols * N * rows * 3];
var buf = new byte[N * N * 4];
Directory.CreateDirectory("out");
for (int k = 0; k < codes.Length; k++)
{
    var d = KanojoGenerator.Generate(codes[k]);
    KanojoComposer.Compose(d, n => map.TryGetValue(n, out var p) ? p : null, buf, N);
    int ox = (k % cols) * N, oy = (k / cols) * N;
    for (int y = 0; y < N; y++)
    for (int x = 0; x < N; x++)
    {
        int i = (y * N + x) * 4, j = ((oy + y) * N * cols + ox + x) * 3;
        sheet[j] = buf[i]; sheet[j + 1] = buf[i + 1]; sheet[j + 2] = buf[i + 2];
    }
    Console.WriteLine($"{k}: {codes[k]} " + string.Join(" ", KanojoComposer.Plan(d).Select(s => s.Name)));
}
Png.Save("out/compose.png", N * cols, N * rows, sheet, 3);
