using System;
using System.Collections.Generic;
using System.Text;

namespace BarcodeKanojo
{
    /// <summary>
    /// 밝기(0~255) 한 줄에서 EAN-13 / UPC-A 바코드를 읽는다. Unity에 의존하지 않는 순수 C#.
    ///
    /// EAN-13 구조 (모듈 95개):
    ///   시작 가드(101) + 왼쪽 6자리(각 7모듈, L/G 코드) + 가운데 가드(01010)
    ///   + 오른쪽 6자리(R 코드) + 끝 가드(101)
    /// 첫 번째 자리는 직접 인코딩되지 않고, 왼쪽 6자리의 L/G 패턴으로 숨겨져 있다.
    /// </summary>
    public class Ean13Decoder
    {
        // L 코드의 [공백, 막대, 공백, 막대] 폭. R 코드는 같은 폭을 [막대, 공백, 막대, 공백] 순서로 쓴다.
        // G 코드는 L 코드 폭을 거꾸로 뒤집은 것이다.
        static readonly int[][] LWidths =
        {
            new[] { 3, 2, 1, 1 }, new[] { 2, 2, 2, 1 }, new[] { 2, 1, 2, 2 }, new[] { 1, 4, 1, 1 },
            new[] { 1, 1, 3, 2 }, new[] { 1, 2, 3, 1 }, new[] { 1, 1, 1, 4 }, new[] { 1, 3, 1, 2 },
            new[] { 1, 2, 1, 3 }, new[] { 3, 1, 1, 2 },
        };

        // 첫 자리 → 왼쪽 6자리의 L/G 패턴
        static readonly string[] FirstDigitParity =
        {
            "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
            "LGGLLG", "LGGGLG", "LGLGLG", "LGLGGL", "LGGLGL",
        };

        const int RunsPerCode = 3 + 24 + 5 + 24 + 3; // 59
        const float MaxDigitError = 1.6f;

        const float MinContrast = 30f;

        float[] smooth = new float[0];
        float[] blockMin = new float[0], blockMax = new float[0];
        float[] winMin = new float[0], winMax = new float[0];
        readonly List<int> runs = new List<int>(256);
        readonly int[] digitBuf = new int[13];
        readonly char[] parityBuf = new char[6];

        /// <summary>밝기 배열에서 바코드를 찾는다. 못 찾으면 null.</summary>
        public string Decode(byte[] luminance, int length)
        {
            if (length < RunsPerCode * 2) return null;
            bool firstDark = BuildRuns(luminance, length);
            if (runs.Count < RunsPerCode) return null;

            string code = DecodeRuns(firstDark);
            if (code != null) return code;

            // 바코드가 거꾸로(180도) 찍힌 경우
            runs.Reverse();
            bool lastDark = (runs.Count % 2 == 1) ? firstDark : !firstDark;
            return DecodeRuns(lastDark);
        }

        /// <summary>
        /// 1) [1,2,1] 블러  2) 주변 최소/최대 중간값 기준 적응형 이진화(히스테리시스)  3) 같은 색이 이어진 길이(run)로 압축.
        /// 반환값: 첫 번째 run이 어두운 색인지.
        /// </summary>
        bool BuildRuns(byte[] lum, int n)
        {
            if (smooth.Length < n) smooth = new float[n];

            for (int i = 0; i < n; i++)
            {
                int a = lum[Math.Max(i - 1, 0)], b = lum[i], c = lum[Math.Min(i + 1, n - 1)];
                smooth[i] = (a + 2 * b + c) * 0.25f;
            }
            // 구간(block)별 최소/최대 밝기 → 주변 몇 구간의 (최소+최대)/2 를 기준값으로 쓴다.
            // 평균을 쓰면 넓은 흰 여백 옆에서 기준이 너무 밝아져 가는 공백이 사라진다.
            int block = Math.Max(4, n / 96);
            int blocks = (n + block - 1) / block;
            if (blockMin.Length < blocks) { blockMin = new float[blocks]; blockMax = new float[blocks]; winMin = new float[blocks]; winMax = new float[blocks]; }
            for (int b = 0; b < blocks; b++)
            {
                float mn = 255f, mx = 0f;
                int end = Math.Min(n, (b + 1) * block);
                for (int i = b * block; i < end; i++)
                {
                    if (smooth[i] < mn) mn = smooth[i];
                    if (smooth[i] > mx) mx = smooth[i];
                }
                blockMin[b] = mn; blockMax[b] = mx;
            }
            int reach = Math.Max(1, (n / 14) / block);
            for (int b = 0; b < blocks; b++)
            {
                float mn = 255f, mx = 0f;
                for (int k = Math.Max(0, b - reach); k <= Math.Min(blocks - 1, b + reach); k++)
                {
                    if (blockMin[k] < mn) mn = blockMin[k];
                    if (blockMax[k] > mx) mx = blockMax[k];
                }
                winMin[b] = mn; winMax[b] = mx;
            }

            runs.Clear();
            bool dark = false, firstDark = false;
            int runLength = 0;
            for (int i = 0; i < n; i++)
            {
                int b = i / block;
                float contrast = winMax[b] - winMin[b];
                float mid = (winMax[b] + winMin[b]) * 0.5f;
                float margin = contrast * 0.08f;

                bool next = dark;
                if (contrast < MinContrast) next = false;          // 무늬 없는 영역은 흰색 취급
                else if (smooth[i] < mid - margin) next = true;
                else if (smooth[i] > mid + margin) next = false;

                if (i == 0) { dark = next; firstDark = next; runLength = 1; continue; }
                if (next == dark) runLength++;
                else { runs.Add(runLength); dark = next; runLength = 1; }
            }
            runs.Add(runLength);
            return firstDark;
        }

        string DecodeRuns(bool firstDark)
        {
            for (int start = 1; start + RunsPerCode <= runs.Count; start++)
            {
                bool isDark = ((start % 2) == 0) == firstDark;
                if (!isDark) continue;
                string code = TryAt(start);
                if (code != null) return code;
            }
            return null;
        }

        string TryAt(int i)
        {
            // 시작 가드: 막대-공백-막대, 각 1모듈
            float guard = runs[i] + runs[i + 1] + runs[i + 2];
            float module = guard / 3f;
            if (!AllNear(i, 3, module)) return null;

            // 앞쪽 여백(quiet zone)이 충분히 넓어야 한다
            if (runs[i - 1] < module * 3f) return null;

            int pos = i + 3;

            // 왼쪽 6자리: [공백, 막대, 공백, 막대]
            for (int k = 0; k < 6; k++)
            {
                int digit = MatchDigit(pos, true, out bool isG);
                if (digit < 0) return null;
                digitBuf[k + 1] = digit;
                parityBuf[k] = isG ? 'G' : 'L';
                pos += 4;
            }

            // 가운데 가드: 공백-막대-공백-막대-공백
            if (!AllNear(pos, 5, module)) return null;
            pos += 5;

            // 오른쪽 6자리: [막대, 공백, 막대, 공백] (R 코드)
            for (int k = 0; k < 6; k++)
            {
                int digit = MatchDigit(pos, false, out _);
                if (digit < 0) return null;
                digitBuf[k + 7] = digit;
                pos += 4;
            }

            // 끝 가드
            if (!AllNear(pos, 3, module)) return null;

            // L/G 패턴 → 첫 자리
            string parity = new string(parityBuf);
            int first = Array.IndexOf(FirstDigitParity, parity);
            if (first < 0) return null;
            digitBuf[0] = first;

            if (!IsValidChecksum(digitBuf)) return null;

            var sb = new StringBuilder(13);
            for (int k = 0; k < 13; k++) sb.Append((char)('0' + digitBuf[k]));
            return sb.ToString();
        }

        bool AllNear(int from, int count, float module)
        {
            for (int k = 0; k < count; k++)
            {
                float r = runs[from + k] / module;
                if (r < 0.4f || r > 1.9f) return false;
            }
            return true;
        }

        /// <summary>
        /// 4개 run의 폭을 합 7모듈로 정규화해 가장 가까운 숫자 패턴을 찾는다.
        /// 카메라가 흐리면 막대는 두꺼워지고 공백은 얇아지는데, "이웃한 두 run의 합"(막대 앞 가장자리 ~
        /// 다음 막대 앞 가장자리 거리)은 이 번짐에 거의 영향을 받지 않는다. 그래서 그 합을 주로 비교하고,
        /// 합이 같은 숫자쌍(1/7, 2/8)을 가르기 위해 개별 폭도 조금 섞는다.
        /// </summary>
        int MatchDigit(int pos, bool leftHalf, out bool isG)
        {
            isG = false;
            float total = runs[pos] + runs[pos + 1] + runs[pos + 2] + runs[pos + 3];
            float scale = 7f / total;
            float w0 = runs[pos] * scale, w1 = runs[pos + 1] * scale, w2 = runs[pos + 2] * scale, w3 = runs[pos + 3] * scale;

            float best = float.MaxValue;
            int bestDigit = -1;
            for (int d = 0; d < 10; d++)
            {
                int[] p = LWidths[d];
                float errL = PatternError(w0, w1, w2, w3, p[0], p[1], p[2], p[3]);
                if (errL < best) { best = errL; bestDigit = d; isG = false; }
                if (leftHalf)
                {
                    float errG = PatternError(w0, w1, w2, w3, p[3], p[2], p[1], p[0]);
                    if (errG < best) { best = errG; bestDigit = d; isG = true; }
                }
            }
            return best <= MaxDigitError ? bestDigit : -1;
        }

        static float PatternError(float w0, float w1, float w2, float w3, int p0, int p1, int p2, int p3)
        {
            float edges = Math.Abs(w0 + w1 - p0 - p1) + Math.Abs(w1 + w2 - p1 - p2) + Math.Abs(w2 + w3 - p2 - p3);
            float widths = Math.Abs(w0 - p0) + Math.Abs(w1 - p1) + Math.Abs(w2 - p2) + Math.Abs(w3 - p3);
            return edges + 0.35f * widths;
        }

        public static bool IsValidChecksum(int[] digits)
        {
            int sum = 0;
            for (int k = 0; k < 12; k++) sum += digits[k] * (k % 2 == 0 ? 1 : 3);
            return (10 - sum % 10) % 10 == digits[12];
        }

        public static bool IsValidEan13(string code)
        {
            if (code == null || code.Length != 13) return false;
            var d = new int[13];
            for (int k = 0; k < 13; k++)
            {
                if (code[k] < '0' || code[k] > '9') return false;
                d[k] = code[k] - '0';
            }
            return IsValidChecksum(d);
        }

        /// <summary>
        /// 테스트용: 13자리 코드를 모듈 배열(true=막대)로 인코딩한다. 앞뒤로 여백 quiet 모듈 포함.
        /// </summary>
        public static bool[] Encode(string code, int quiet = 11)
        {
            var m = new List<bool>();
            for (int k = 0; k < quiet; k++) m.Add(false);
            m.AddRange(new[] { true, false, true });

            string parity = FirstDigitParity[code[0] - '0'];
            for (int k = 1; k <= 6; k++)
            {
                int[] w = LWidths[code[k] - '0'];
                bool g = parity[k - 1] == 'G';
                bool bar = false;
                for (int j = 0; j < 4; j++)
                {
                    int width = g ? w[3 - j] : w[j];
                    for (int t = 0; t < width; t++) m.Add(bar);
                    bar = !bar;
                }
            }
            m.AddRange(new[] { false, true, false, true, false });
            for (int k = 7; k <= 12; k++)
            {
                int[] w = LWidths[code[k] - '0'];
                bool bar = true;
                for (int j = 0; j < 4; j++)
                {
                    for (int t = 0; t < w[j]; t++) m.Add(bar);
                    bar = !bar;
                }
            }
            m.AddRange(new[] { true, false, true });
            for (int k = 0; k < quiet; k++) m.Add(false);
            return m.ToArray();
        }
    }
}
