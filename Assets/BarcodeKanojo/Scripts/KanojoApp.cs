using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BarcodeKanojo
{
    /// <summary>
    /// 앱 전체 흐름: 스캔(또는 직접 입력) → 생성 → 초상화/정보 표시 → 도감 저장.
    /// 씬에 아무것도 배치하지 않아도 RuntimeInitializeOnLoadMethod로 자동 생성된다.
    /// </summary>
    public class KanojoApp : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<KanojoApp>() == null)
                new GameObject("KanojoApp").AddComponent<KanojoApp>();
        }

        static readonly Color Pink = new Color(0.91f, 0.36f, 0.55f);
        static readonly Color Navy = new Color(0.20f, 0.24f, 0.40f);
        static readonly Color Paper = new Color(0.99f, 0.96f, 0.97f);
        static readonly Color TextDark = new Color(0.22f, 0.18f, 0.24f);

        BarcodeScanner scanner;

        RectTransform safeArea;
        Rect lastSafe;
        RawImage portraitImage;
        RectTransform cameraView;
        RawImage cameraImage;
        Text stageMessage;
        Text infoText;
        Text scanLabel;
        InputField barcodeInput;

        GameObject collectionPanel;
        RectTransform collectionContent;
        Text collectionTitle;
        readonly List<Texture2D> thumbnails = new List<Texture2D>();

        Texture2D portraitTexture;

        void Start()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;

            scanner = gameObject.AddComponent<BarcodeScanner>();
            scanner.Detected += code => ShowKanojo(code, true);
            scanner.Failed += msg => { SetScanning(false); stageMessage.text = msg; };

            EnsureEventSystem();
            BuildUI();

            var all = KanojoCollection.All();
            if (all.Count > 0) ShowKanojo(all[0], false, quiet: true);
            else ShowIntro();
        }

        void Update()
        {
            if (safeArea != null && Screen.safeArea != lastSafe) ApplySafeArea();
            if (scanner.IsRunning) UpdateCameraPreview();

            // 안드로이드 뒤로가기 = Escape
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (collectionPanel.activeSelf) collectionPanel.SetActive(false);
                else if (scanner.IsRunning) SetScanning(false);
                else Application.Quit();
            }
        }

        // ================= 흐름 =================

        void ShowIntro()
        {
            portraitImage.texture = null;
            portraitImage.color = new Color(1, 1, 1, 0);
            stageMessage.text = "아직 만난 그녀가 없어요";
            infoText.text =
                "<b>모든 물건에는 신이 깃들어 있다.</b>\n\n" +
                "상품 바코드를 카메라로 찍으면, 그 물건에 깃든 그녀가 나타나요.\n" +
                "같은 바코드에서는 언제나 같은 그녀가 나와요.\n\n" +
                "카메라가 없으면 아래 칸에 13자리 숫자를 직접 넣어 보세요.\n" +
                "예) 4901234567894";
        }

        void ToggleScan()
        {
            SetScanning(!scanner.IsRunning);
        }

        void SetScanning(bool on)
        {
            if (on)
            {
                collectionPanel.SetActive(false);
                scanner.StartScan();
                cameraView.gameObject.SetActive(true);
                portraitImage.gameObject.SetActive(false);
                stageMessage.text = "바코드를 빨간 선에 맞춰 주세요";
                scanLabel.text = "스캔 중지";
            }
            else
            {
                scanner.StopScan();
                cameraView.gameObject.SetActive(false);
                portraitImage.gameObject.SetActive(true);
                if (stageMessage.text == "바코드를 빨간 선에 맞춰 주세요") stageMessage.text = "";
                scanLabel.text = "바코드 스캔";
            }
        }

        void OnManualGenerate()
        {
            string code = barcodeInput.text.Trim();
            if (code.Length == 0)
            {
                stageMessage.text = "바코드 숫자를 입력해 주세요";
                return;
            }
            ShowKanojo(code, false);
        }

        void ShowKanojo(string barcode, bool fromScan, bool quiet = false)
        {
            SetScanning(false);
            collectionPanel.SetActive(false);

            KanojoData d = KanojoGenerator.Generate(barcode);
            portraitTexture = RenderKanojo(d, 600, portraitTexture);
            portraitImage.texture = portraitTexture;
            portraitImage.color = Color.white;

            bool isNew = quiet ? false : KanojoCollection.Add(barcode);
            if (quiet) stageMessage.text = "";
            else stageMessage.text = isNew ? "새로운 그녀를 만났어요!" : "다시 만났네요";

            infoText.text = Describe(d, isNew && !quiet);
            barcodeInput.text = barcode;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (fromScan) Handheld.Vibrate();
#endif
        }

        /// <summary>파츠 그림(Resources/KanojoParts)이 있으면 그걸로, 없으면 코드로 그린 초상화로.</summary>
        static Texture2D RenderKanojo(KanojoData d, int size, Texture2D reuse)
        {
            return KanojoSpriteRenderer.Available
                ? KanojoSpriteRenderer.Render(d, size, reuse)
                : KanojoPortrait.Render(d, size, reuse);
        }

        static string Describe(KanojoData d, bool isNew)
        {
            string[] clothes = { "세일러복", "블레이저", "후드티", "원피스", "티셔츠" };
            string[] accessory = { "없음", "리본", "머리핀", "머리띠", "고양이 귀" };
            string[] spot = { "없음", "볼터치", "주근깨", "눈물점", "반창고", "홍조", "입가 점" };

            var sb = new StringBuilder();
            sb.Append("<b>No. ").Append(d.barcode).Append("</b>   출신: ").Append(BarcodeCountry.Of(d.barcode));
            if (isNew) sb.Append("   <color=#E0508C><b>NEW!</b></color>");
            if (d.barcode.Length == 13 && !Ean13Decoder.IsValidEan13(d.barcode)) sb.Append("   <color=#999999>(체크 숫자 불일치)</color>");
            sb.Append('\n');
            sb.Append($"머리 {d.hairType}번 · 앞머리 {d.fringeType}번 · 머리색 {d.hairColor}\n");
            sb.Append($"눈 {d.eyeType}번 · 눈색 {d.eyeColor} · 눈썹 {d.browType} · 코 {d.noseType} · 입 {d.mouthType}\n");
            sb.Append($"얼굴형 {d.faceType} · 피부 {d.skinColor} · 귀 {(d.earType == 2 ? "엘프" : "보통")} · 안경 {(d.glassesType == 2 ? "있음" : "없음")}\n");
            sb.Append($"옷 {clothes[d.clothesType - 1]} · 장식 {accessory[d.accessoryType - 1]} · 특징 {spot[d.spotType - 1]}\n");
            sb.Append('\n');
            sb.Append("<b>성격</b>\n");
            AppendStat(sb, "소비 성향", d.consumption);
            AppendStat(sb, "소유욕", d.possession);
            AppendStat(sb, "인정 욕구", d.recognition);
            AppendStat(sb, "적극성", d.sexual);
            AppendStat(sb, "애교", d.flirtable);
            return sb.ToString();
        }

        static void AppendStat(StringBuilder sb, string label, int value)
        {
            int filled = Mathf.Clamp(Mathf.RoundToInt(value / 10f), 0, 10);
            sb.Append(label).Append("  <color=#E0508C>").Append(new string('■', filled)).Append("</color>")
              .Append("<color=#D8CCD2>").Append(new string('■', 10 - filled)).Append("</color>  ")
              .Append(value).Append('\n');
        }

        // ================= 카메라 미리보기 =================

        void UpdateCameraPreview()
        {
            WebCamTexture cam = scanner.CameraTexture;
            if (cam == null || cam.width < 100) return;

            cameraImage.texture = cam;
            int angle = cam.videoRotationAngle;
            bool rotated = angle % 180 != 0;
            float cw = cam.width, ch = cam.height;
            float dw = rotated ? ch : cw, dh = rotated ? cw : ch;
            Vector2 view = cameraView.rect.size;
            float scale = Mathf.Max(view.x / dw, view.y / dh); // 화면을 꽉 채우도록(cover)

            RectTransform rt = cameraImage.rectTransform;
            rt.sizeDelta = new Vector2(cw * scale, ch * scale);
            rt.localEulerAngles = new Vector3(0, 0, -angle);
            cameraImage.uvRect = cam.videoVerticallyMirrored ? new Rect(0, 1, 1, -1) : new Rect(0, 0, 1, 1);
        }

        // ================= 도감 =================

        void OpenCollection()
        {
            SetScanning(false);
            foreach (Transform child in collectionContent) Destroy(child.gameObject);
            foreach (var t in thumbnails) Destroy(t);
            thumbnails.Clear();

            var all = KanojoCollection.All();
            collectionTitle.text = $"도감  ({all.Count}명)";

            foreach (string code in all)
            {
                KanojoData d = KanojoGenerator.Generate(code);
                Texture2D thumb = RenderKanojo(d, 128, null);
                thumbnails.Add(thumb);

                string captured = code;
                var btn = UIFactory.Button("Entry", collectionContent, "", new Color(1, 1, 1, 0.95f),
                    () => ShowKanojo(captured, false, quiet: true), out Text label, 36);
                btn.gameObject.AddComponent<LayoutElement>().minHeight = 150;

                var img = UIFactory.Rect("Thumb", btn.transform).gameObject.AddComponent<RawImage>();
                img.texture = thumb;
                img.raycastTarget = false;
                img.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, -64), new Vector2(140, 64));

                label.alignment = TextAnchor.MiddleLeft;
                label.color = TextDark;
                label.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(165, 0), new Vector2(-20, 0));
                label.text = $"<b>{code}</b>\n<size=28>{BarcodeCountry.Of(code)} · 애교 {d.flirtable} · 소유욕 {d.possession}</size>";
            }
            if (all.Count == 0)
            {
                var empty = UIFactory.Label("Empty", collectionContent, "아직 아무도 없어요.\n바코드를 스캔해 보세요!", 36, Color.white);
                empty.gameObject.AddComponent<LayoutElement>().minHeight = 200;
            }
            collectionPanel.SetActive(true);
        }

        // ================= UI 구성 =================

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
            var module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        void BuildUI()
        {
            var canvasGo = new GameObject("KanojoCanvas", typeof(RectTransform));
            canvasGo.layer = 5;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0f; // 가로폭 기준 (세로가 긴 폰에서 아래쪽 공간이 늘어남)
            canvasGo.AddComponent<GraphicRaycaster>();

            UIFactory.Panel("Background", canvasGo.transform, Paper).rectTransform.Stretch();

            safeArea = UIFactory.Rect("SafeArea", canvasGo.transform);
            ApplySafeArea();

            // 제목
            var title = UIFactory.Label("Title", safeArea, "<b>바코드 그녀</b>", 60, Pink);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -115), new Vector2(0, -5));

            // 무대 (초상화 / 카메라)
            var stage = UIFactory.Panel("Stage", safeArea, Color.white).rectTransform;
            stage.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-500, -1120), new Vector2(500, -120));
            stage.gameObject.AddComponent<Outline>().effectColor = new Color(0.91f, 0.36f, 0.55f, 0.35f);

            portraitImage = UIFactory.Rect("Portrait", stage).gameObject.AddComponent<RawImage>();
            portraitImage.rectTransform.Stretch();
            portraitImage.raycastTarget = false;

            cameraView = UIFactory.Rect("CameraView", stage).Stretch();
            cameraView.gameObject.AddComponent<RectMask2D>();
            UIFactory.Panel("CameraBg", cameraView, Color.black).rectTransform.Stretch();
            cameraImage = UIFactory.Rect("Camera", cameraView).gameObject.AddComponent<RawImage>();
            cameraImage.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cameraImage.raycastTarget = false;
            var guide = UIFactory.Panel("Guide", cameraView, new Color(1f, 0.2f, 0.25f, 0.9f));
            guide.rectTransform.Place(new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0, -3), new Vector2(0, 3));
            guide.raycastTarget = false;
            cameraView.gameObject.SetActive(false);

            var msgBand = UIFactory.Panel("MessageBand", stage, new Color(0.15f, 0.12f, 0.18f, 0.55f));
            msgBand.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 80));
            msgBand.raycastTarget = false;
            stageMessage = UIFactory.Label("Message", msgBand.transform, "", 36, Color.white);
            stageMessage.rectTransform.Stretch();

            // 정보
            infoText = UIFactory.Label("Info", safeArea, "", 32, TextDark, TextAnchor.UpperLeft);
            infoText.rectTransform.Place(Vector2.zero, Vector2.one, new Vector2(48, 335), new Vector2(-48, -1140));
            infoText.resizeTextForBestFit = true;
            infoText.resizeTextMinSize = 18;
            infoText.resizeTextMaxSize = 32;
            infoText.verticalOverflow = VerticalWrapMode.Truncate;
            infoText.lineSpacing = 1.1f;

            // 직접 입력 줄
            barcodeInput = UIFactory.Input("BarcodeInput", safeArea, "바코드 숫자 직접 입력");
            barcodeInput.contentType = InputField.ContentType.IntegerNumber;
            barcodeInput.characterLimit = 14;
            barcodeInput.GetComponent<Image>().color = new Color(1, 1, 1);
            barcodeInput.gameObject.AddComponent<Outline>().effectColor = new Color(0.22f, 0.18f, 0.24f, 0.25f);
            ((RectTransform)barcodeInput.transform).Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 200), new Vector2(-290, 300));

            var gen = UIFactory.Button("Generate", safeArea, "생성", Navy, OnManualGenerate, out _);
            ((RectTransform)gen.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(-270, 200), new Vector2(-40, 300));

            // 아래 버튼 줄
            var scan = UIFactory.Button("Scan", safeArea, "바코드 스캔", Pink, ToggleScan, out scanLabel, 46);
            ((RectTransform)scan.transform).Place(new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(40, 40), new Vector2(-15, 170));

            var col = UIFactory.Button("Collection", safeArea, "도감", Navy, OpenCollection, out _, 46);
            ((RectTransform)col.transform).Place(new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(15, 40), new Vector2(-40, 170));

            BuildCollectionPanel();
        }

        void BuildCollectionPanel()
        {
            var panel = UIFactory.Panel("CollectionPanel", safeArea, new Color(0.16f, 0.13f, 0.2f, 0.96f));
            panel.rectTransform.Stretch();
            collectionPanel = panel.gameObject;

            collectionTitle = UIFactory.Label("Title", panel.transform, "도감", 52, Color.white);
            collectionTitle.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -130), new Vector2(0, -20));

            var scrollGo = UIFactory.Rect("Scroll", panel.transform);
            scrollGo.Place(Vector2.zero, Vector2.one, new Vector2(40, 210), new Vector2(-40, -150));
            var scroll = scrollGo.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30;

            var viewport = UIFactory.Panel("Viewport", scrollGo, new Color(1, 1, 1, 0.01f));
            viewport.rectTransform.Stretch();
            viewport.gameObject.AddComponent<RectMask2D>();

            collectionContent = UIFactory.Rect("Content", viewport.transform);
            collectionContent.anchorMin = new Vector2(0, 1);
            collectionContent.anchorMax = new Vector2(1, 1);
            collectionContent.pivot = new Vector2(0.5f, 1);
            collectionContent.offsetMin = Vector2.zero;
            collectionContent.offsetMax = Vector2.zero;
            var layout = collectionContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            collectionContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.rectTransform;
            scroll.content = collectionContent;

            var close = UIFactory.Button("Close", panel.transform, "닫기", Pink, () => collectionPanel.SetActive(false), out _, 46);
            ((RectTransform)close.transform).Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 40), new Vector2(-40, 170));

            collectionPanel.SetActive(false);
        }

        void ApplySafeArea()
        {
            Rect safe = Screen.safeArea;
            lastSafe = safe;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
        }

        void OnDestroy()
        {
            if (portraitTexture != null) Destroy(portraitTexture);
            foreach (var t in thumbnails) Destroy(t);
        }
    }
}
