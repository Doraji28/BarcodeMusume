using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace BarcodeKanojo
{
    /// <summary>
    /// WebCamTexture로 카메라를 켜고, 몇 프레임마다 화면 가운데 근처의 가로줄/세로줄 여러 개를
    /// Ean13Decoder로 읽는다. 같은 코드가 연속 2번 읽히면 확정한다(오인식 방지).
    /// 휴대폰은 카메라 영상이 90도 돌아가 있는 경우가 많아서 가로/세로 둘 다 훑는다.
    /// </summary>
    public class BarcodeScanner : MonoBehaviour
    {
        public event Action<string> Detected;
        public event Action<string> Failed;

        public WebCamTexture CameraTexture { get; private set; }
        public bool IsRunning => CameraTexture != null && CameraTexture.isPlaying;

        const int ScanEveryNFrames = 3;
        const int LinesEachDirection = 9;

        readonly Ean13Decoder decoder = new Ean13Decoder();
        Color32[] pixels;
        byte[] line;
        int frameCounter;
        string lastCode;
        int sameCount;
        float lastTime;
        bool starting;

        public void StartScan()
        {
            if (IsRunning || starting) return;
            StartCoroutine(StartRoutine());
        }

        public void StopScan()
        {
            if (CameraTexture != null)
            {
                CameraTexture.Stop();
                Destroy(CameraTexture);
                CameraTexture = null;
            }
            lastCode = null;
            sameCount = 0;
        }

        IEnumerator StartRoutine()
        {
            starting = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                float waited = 0f;
                while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && waited < 15f)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                {
                    starting = false;
                    Failed?.Invoke("카메라 권한이 필요해요. 설정에서 허용해 주세요.");
                    yield break;
                }
            }
#else
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                starting = false;
                Failed?.Invoke("카메라 권한이 필요해요.");
                yield break;
            }
#endif
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                starting = false;
                Failed?.Invoke("카메라를 찾지 못했어요. 아래 칸에 바코드 숫자를 직접 입력해 보세요.");
                yield break;
            }

            string deviceName = devices[0].name;
            foreach (var d in devices)
                if (!d.isFrontFacing) { deviceName = d.name; break; }

            CameraTexture = new WebCamTexture(deviceName, 1280, 720, 30);
            CameraTexture.Play();
            starting = false;
        }

        void Update()
        {
            if (!IsRunning || !CameraTexture.didUpdateThisFrame) return;
            if (++frameCounter % ScanEveryNFrames != 0) return;

            int w = CameraTexture.width, h = CameraTexture.height;
            if (w < 100 || h < 100) return; // 아직 초기화 중

            if (pixels == null || pixels.Length != w * h) pixels = new Color32[w * h];
            CameraTexture.GetPixels32(pixels);

            string code = ScanRows(w, h) ?? ScanColumns(w, h);
            if (code != null) Confirm(code);
        }

        string ScanRows(int w, int h)
        {
            EnsureLine(w);
            for (int k = 0; k < LinesEachDirection; k++)
            {
                int y = h / 2 + Offset(k) * h / 28;
                if (y < 1 || y >= h - 1) continue;
                for (int x = 0; x < w; x++)
                {
                    // 위아래 3줄 평균으로 노이즈를 줄인다
                    line[x] = (byte)((Luma(pixels[(y - 1) * w + x]) + 2 * Luma(pixels[y * w + x]) + Luma(pixels[(y + 1) * w + x])) >> 2);
                }
                string code = decoder.Decode(line, w);
                if (code != null) return code;
            }
            return null;
        }

        string ScanColumns(int w, int h)
        {
            EnsureLine(h);
            for (int k = 0; k < LinesEachDirection; k++)
            {
                int x = w / 2 + Offset(k) * w / 28;
                if (x < 1 || x >= w - 1) continue;
                for (int y = 0; y < h; y++)
                {
                    int i = y * w + x;
                    line[y] = (byte)((Luma(pixels[i - 1]) + 2 * Luma(pixels[i]) + Luma(pixels[i + 1])) >> 2);
                }
                string code = decoder.Decode(line, h);
                if (code != null) return code;
            }
            return null;
        }

        /// <summary>0, +1, -1, +2, -2 ... 순서로 가운데부터 바깥으로.</summary>
        static int Offset(int k) => (k % 2 == 0 ? 1 : -1) * ((k + 1) / 2);

        void EnsureLine(int n)
        {
            if (line == null || line.Length < n) line = new byte[n];
        }

        static int Luma(Color32 c) => (c.r * 77 + c.g * 150 + c.b * 29) >> 8;

        void Confirm(string code)
        {
            if (code == lastCode && Time.unscaledTime - lastTime < 1.5f) sameCount++;
            else sameCount = 1;
            lastCode = code;
            lastTime = Time.unscaledTime;

            if (sameCount >= 2)
            {
                StopScan();
                Detected?.Invoke(code);
            }
        }

        void OnDisable() => StopScan();
    }
}
