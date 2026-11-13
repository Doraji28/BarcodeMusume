using System.IO.Compression;

public static class Png
{
    public static void Save(string path, int w, int h, byte[] px, int ch)
    {
        using var ms = new MemoryStream();
        void Chunk(string type, byte[] data)
        {
            var len = BitConverter.GetBytes(data.Length); Array.Reverse(len); ms.Write(len);
            var td = new byte[4 + data.Length];
            System.Text.Encoding.ASCII.GetBytes(type).CopyTo(td, 0); data.CopyTo(td, 4); ms.Write(td);
            var crc = BitConverter.GetBytes(Crc(td)); Array.Reverse(crc); ms.Write(crc);
        }
        ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ih = new byte[13];
        void BE(int v, int o) { ih[o] = (byte)(v >> 24); ih[o + 1] = (byte)(v >> 16); ih[o + 2] = (byte)(v >> 8); ih[o + 3] = (byte)v; }
        BE(w, 0); BE(h, 4); ih[8] = 8; ih[9] = (byte)(ch == 4 ? 6 : 2);
        Chunk("IHDR", ih);
        var raw = new MemoryStream();
        for (int y = 0; y < h; y++) { raw.WriteByte(0); raw.Write(px, y * w * ch, w * ch); }
        var z = new MemoryStream();
        using (var zs = new ZLibStream(z, CompressionLevel.Optimal, true)) { raw.Position = 0; raw.CopyTo(zs); }
        Chunk("IDAT", z.ToArray());
        Chunk("IEND", new byte[0]);
        File.WriteAllBytes(path, ms.ToArray());
    }

    static uint Crc(byte[] b)
    {
        uint c = 0xFFFFFFFF;
        foreach (var x in b) { c ^= x; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1; }
        return ~c;
    }
}
