using System.Security.Cryptography;
using System.Text;

namespace BarcodeKanojo
{
    /// <summary>
    /// 바코드 문자열 → MD5(128비트) → 비트 구간별 파츠 값.
    /// 팬 복원 서버 kanojo.py의 generate()를 그대로 포팅했다.
    /// </summary>
    public static class KanojoGenerator
    {
        /// <summary>확인용 예시에서 쓰인 secret. 원작의 실제 값은 알 수 없다.</summary>
        public const string DefaultSecret = "1qa2";

        public static KanojoData Generate(string barcode, string secret = DefaultSecret)
        {
            byte[] hash = HashOf(barcode, secret);

            var d = new KanojoData { barcode = barcode };
            //                     bit[start, end)  min   max
            d.hairColor     = Pick(hash,   0,  24,    1,   24);
            d.eyeColor      = Pick(hash,  16,  40,    1,   12);
            d.skinColor     = Pick(hash,  32,  56,    1,   12);
            d.clothesColor  = Pick(hash,  48,  59,    1,    6);
            d.mouthType     = Pick(hash,  51,  63,    1,   12);
            d.raceType      = Pick(hash,  55,  67,    1,   10);
            d.spotType      = Pick(hash,  59,  70,    1,    7);
            d.clothesType   = Pick(hash,  62,  73,    1,    5);
            d.browType      = Pick(hash,  65,  77,    1,   12);
            d.accessoryType = Pick(hash,  69,  79,    1,    5);
            d.hairType      = Pick(hash,  71,  84,    1,   26);
            d.earType       = Pick(hash,  76,  77,    1,    2);
            d.glassesType   = Pick(hash,  77,  78,    1,    2);
            d.faceType      = Pick(hash,  78,  89,    1,    6);
            d.eyeType       = Pick(hash,  81,  92,  101,  115);
            d.fringeType    = Pick(hash,  84,  97,    1,   22);
            d.noseType      = Pick(hash,  89, 100,    1,    6);
            d.eyePosition   = Pick(hash,  92, 105,  -10,   10);
            d.browPosition  = Pick(hash,  97, 110,  -10,   10);
            d.mouthPosition = Pick(hash, 102, 115,  -10,   10);
            d.consumption   = Pick(hash, 107, 123,    1,  100);
            d.possession    = Pick(hash,  99, 115,    1,  100);
            d.recognition   = Pick(hash,  91, 107,    1,  100);
            d.sexual        = Pick(hash,  83,  99,    1,  100);
            d.flirtable     = Pick(hash,  75,  91,    1,  100);
            return d;
        }

        /// <summary>secret 앞절반 + barcode + secret 뒷절반 을 MD5로 해시한다.</summary>
        public static byte[] HashOf(string barcode, string secret)
        {
            int half = secret.Length / 2;
            string source = secret.Substring(0, half) + barcode + secret.Substring(half);
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(Encoding.UTF8.GetBytes(source));
            }
        }

        /// <summary>
        /// 해시를 128글자 비트열로 봤을 때 [start, end) 구간을 정수로 읽는다.
        /// bit 0 = hash[0]의 최상위 비트(MSB).
        /// </summary>
        public static long ReadBits(byte[] hash, int start, int end)
        {
            long value = 0;
            for (int i = start; i < end; i++)
            {
                int bit = (hash[i / 8] >> (7 - (i % 8))) & 1;
                value = (value << 1) | (long)bit;
            }
            return value;
        }

        static int Pick(byte[] hash, int start, int end, int min, int max)
        {
            return (int)(min + ReadBits(hash, start, end) % (max - min + 1));
        }
    }
}
