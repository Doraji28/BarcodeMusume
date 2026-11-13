using System.IO;
using UnityEditor;
using UnityEngine;

namespace BarcodeKanojo.EditorTools
{
    public static class KanojoEditorTools
    {
        const string Expected =
            "hair_color 4, eye_color 12, skin_color 9, clothes_color 3, mouth 11, race 4, spot 2, clothes 5, brow 3, " +
            "accessory 5, hair 20, ear 1, glasses 2, face 3, eye 115, fringe 6, nose 4, eye_pos 0, brow_pos 9, " +
            "mouth_pos 5, consumption 35, possession 48, recognition 51, sexual 42, flirtable 24";

        [MenuItem("Barcode Kanojo/Self Test")]
        public static void SelfTest()
        {
            // 1) 생성기: 팬 서버 예시 값과 비교
            string got = KanojoGenerator.Generate("4901234567894", "1qa2").ToString();
            if (got == Expected) Debug.Log("[BarcodeKanojo] 생성기 OK — 예시 값 25개 모두 일치");
            else Debug.LogError("[BarcodeKanojo] 생성기 불일치\n기대: " + Expected + "\n실제: " + got);

            // 2) 디코더: 합성 바코드 한 줄을 만들어 읽어 보기
            var decoder = new Ean13Decoder();
            string[] codes = { "4901234567894", "8801234567893", "9780306406157" };
            foreach (string code in codes)
            {
                bool[] modules = Ean13Decoder.Encode(code);
                const int px = 4;
                var lum = new byte[modules.Length * px];
                for (int i = 0; i < lum.Length; i++) lum[i] = (byte)(modules[i / px] ? 40 : 215);
                string read = decoder.Decode(lum, lum.Length);
                if (read == code) Debug.Log($"[BarcodeKanojo] 디코더 OK — {code}");
                else Debug.LogError($"[BarcodeKanojo] 디코더 실패 — {code} → {read ?? "null"}");
            }
        }

        [MenuItem("Barcode Kanojo/Export Sample Portraits")]
        public static void ExportSamples()
        {
            string dir = Path.Combine(Application.dataPath, "../PortraitSamples");
            Directory.CreateDirectory(dir);
            string[] codes = { "4901234567894", "8801234567893", "9780306406157", "4006381333931", "0012345678905", "8809876543217" };
            foreach (string code in codes)
            {
                Texture2D tex = KanojoPortrait.Render(KanojoGenerator.Generate(code), 512);
                File.WriteAllBytes(Path.Combine(dir, code + ".png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            Debug.Log("[BarcodeKanojo] 샘플 초상화 저장: " + Path.GetFullPath(dir));
            EditorUtility.RevealInFinder(dir);
        }

        [MenuItem("Barcode Kanojo/Apply Android Player Settings")]
        public static void ApplyAndroidSettings()
        {
            PlayerSettings.productName = "바코드 그녀";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.student.barcodekanojo");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            Debug.Log("[BarcodeKanojo] Android 설정 적용: 이름=바코드 그녀, id=com.student.barcodekanojo, 세로 고정");
        }
    }
}
