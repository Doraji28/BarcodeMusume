using BarcodeKanojo;

/// <summary>
/// 그림 AI에게 첨부할 가이드 템플릿: 초록 배경 위 칸마다 기본 몸(또는 머리) 실루엣을 연한 파란 선으로.
/// </summary>
public static class Template
{
    static readonly byte[] Green = { 0, 255, 0 };
    static readonly byte[] Guide = { 150, 190, 255 };
    static readonly byte[] CellLine = { 0, 200, 0 };

    public static void Make(PartImage body)
    {
        // 전신 템플릿: 4 x 2칸
        Draw(body, 0, body.Height, 2048, 1366, 4, 2, 0.86f, "template_body.png");
        // 머리 템플릿: 3 x 2칸, 몸의 위쪽 120px(머리+목)만
        Draw(body, 0, 122, 2048, 1366, 3, 2, 0.62f, "template_head.png");
    }

    static void Draw(PartImage body, int y0, int y1, int W, int H, int cols, int rows, float fill, string path)
    {
        var o = new byte[W * H * 3];
        for (int i = 0; i < W * H; i++) { o[i * 3] = Green[0]; o[i * 3 + 1] = Green[1]; o[i * 3 + 2] = Green[2]; }

        int cw = W / cols, ch = H / rows;
        int srcH = y1 - y0;
        float s = ch * fill / srcH;
        float lineW = Math.Max(2f, s * 0.9f);

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            int ox = c * cw + (int)((cw - body.Width * s) / 2);
            int oy = r * ch + (int)((ch - srcH * s) / 2) + (int)(ch * 0.04f);
            for (int y = 0; y < (int)(srcH * s); y++)
            for (int x = 0; x < (int)(body.Width * s); x++)
            {
                float a = Alpha(body, x / s, y / s + y0);
                // 윤곽선: 주변 lineW 픽셀 안에 안/밖이 섞여 있으면 선
                bool inside = a > 0.5f;
                bool edge = false;
                for (int k = 0; k < 8 && !edge; k++)
                {
                    double ang = k * Math.PI / 4;
                    float nx = (float)(x + Math.Cos(ang) * lineW / 2), ny = (float)(y + Math.Sin(ang) * lineW / 2);
                    if ((Alpha(body, nx / s, ny / s + y0) > 0.5f) != inside) edge = true;
                }
                if (!edge) continue;
                int px = ox + x, py = oy + y;
                if (px < 0 || py < 0 || px >= W || py >= H) continue;
                int j = (py * W + px) * 3;
                o[j] = Guide[0]; o[j + 1] = Guide[1]; o[j + 2] = Guide[2];
            }
        }
        // 칸 경계(살짝 진한 초록)
        for (int c = 1; c < cols; c++) for (int y = 0; y < H; y++) for (int t = 0; t < 3; t++) Set(o, W, c * cw + t - 1, y, CellLine);
        for (int r = 1; r < rows; r++) for (int x = 0; x < W; x++) for (int t = 0; t < 3; t++) Set(o, W, x, r * ch + t - 1, CellLine);

        Png.Save(path, W, H, o, 3);
        Console.WriteLine($"saved {path}");
    }

    static void Set(byte[] o, int W, int x, int y, byte[] c)
    {
        int j = (y * W + x) * 3;
        if (j < 0 || j + 2 >= o.Length) return;
        o[j] = c[0]; o[j + 1] = c[1]; o[j + 2] = c[2];
    }

    static float Alpha(PartImage p, float fx, float fy)
    {
        int x = (int)fx, y = (int)fy;
        if (x < 0 || y < 0 || x >= p.Width || y >= p.Height) return 0;
        return p.Rgba[(y * p.Width + x) * 4 + 3] / 255f;
    }
}
