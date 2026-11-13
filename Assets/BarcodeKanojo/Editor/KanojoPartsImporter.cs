using UnityEditor;

namespace BarcodeKanojo.EditorTools
{
    /// <summary>
    /// KanojoParts 폴더의 PNG는 픽셀을 직접 읽어 합성하므로 Read/Write를 켜고 압축·밉맵을 끈다.
    /// </summary>
    public class KanojoPartsImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/KanojoParts/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.sRGBTexture = true;
        }
    }
}
