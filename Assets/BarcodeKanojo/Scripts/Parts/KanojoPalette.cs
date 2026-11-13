using System;

namespace BarcodeKanojo
{
    /// <summary>생성기 색 번호(1부터) → RGB(0~1). 파츠 합성기가 회색 파츠를 칠할 때 쓴다.</summary>
    public static class KanojoPalette
    {
        public static readonly string[] Hair =
        {
            "3A3440", "5A3E32", "8A5A3C", "B8845A", "9A4A36", "F0D488", "F4ECD2", "D4D8E4",
            "9EA2AC", "B8583A", "D85A5A", "F0A060", "F8B8D0", "F07AA8", "A884D8", "D4C0F4",
            "4A5A9A", "6A9AE8", "A8D8F4", "5ABCB4", "B4ECD4", "7AB46A", "FFFFFF", "8A3A5A",
        };

        public static readonly string[] Eye =
        {
            "8A5A3A", "4A3A3A", "4A88E8", "7AC8F0", "4EAA6A", "2E9A88",
            "E8AA3A", "D84A54", "9A5AD8", "F07AB8", "8A909A", "F0C850",
        };

        public static readonly string[] Skin =
        {
            "FFF4EC", "FDEEE2", "FBE6D6", "FBDECA", "F8D8C4", "F6D2B8",
            "F2CAAA", "EEC4A4", "E8B896", "DCA684", "CC9270", "B07A5A",
        };

        public static readonly string[] Clothes = { "3A4A7E", "D84A58", "F09AC0", "5A9A6E", "3A3A46", "7AB4EA" };

        public static float[] Pick(string[] palette, int oneBased)
        {
            int i = Math.Max(0, Math.Min(palette.Length - 1, oneBased - 1));
            return Rgb(palette[i]);
        }

        public static float[] Rgb(string hex)
        {
            return new[]
            {
                Convert.ToInt32(hex.Substring(0, 2), 16) / 255f,
                Convert.ToInt32(hex.Substring(2, 2), 16) / 255f,
                Convert.ToInt32(hex.Substring(4, 2), 16) / 255f,
            };
        }
    }
}
