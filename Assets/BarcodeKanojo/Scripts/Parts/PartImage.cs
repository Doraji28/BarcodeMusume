namespace BarcodeKanojo
{
    /// <summary>
    /// 파츠 그림 한 장의 픽셀 (RGBA, 위쪽 행부터 저장). Unity에 의존하지 않는 순수 데이터.
    /// </summary>
    public class PartImage
    {
        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Rgba;

        public PartImage(int width, int height, byte[] rgba)
        {
            Width = width;
            Height = height;
            Rgba = rgba;
        }
    }
}
