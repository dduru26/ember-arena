using UnityEngine;
using UnityEngine.UI;
using EmberArena.Algorithms;
using EmberArena.Core;
using EmberArena.Patterns;

namespace EmberArena.UI
{
    // Observer: the HUD never checks the score or the wave every frame, it just listens for GameEvents and the player's HealthChanged and redraws when told.
    public class HudController : MonoBehaviour
    {
        private static bool hasStarted;

        private const string BestScoreKey = "EmberBestScore";
        private const string BestWaveKey = "EmberBestWave";
        private const int HealthSegments = 10;
        private const float BannerDuration = 1.8f;

        private static readonly Color Ground = new Color(0.063f, 0.043f, 0.035f, 0.92f);
        private static readonly Color PanelColor = new Color(0.118f, 0.078f, 0.063f, 0.96f);
        private static readonly Color Ember = new Color(1f, 0.416f, 0.169f);
        private static readonly Color Amber = new Color(1f, 0.706f, 0.329f);
        private static readonly Color Cream = new Color(0.965f, 0.922f, 0.867f);
        private static readonly Color Muted = new Color(0.71f, 0.643f, 0.584f);
        private static readonly Color Dark = new Color(0.102f, 0.059f, 0.039f);
        private static readonly Color Danger = new Color(1f, 0.35f, 0.35f);
        private static readonly Color SegmentEmpty = new Color(0.965f, 0.922f, 0.867f, 0.14f);
        private static readonly Color SegmentTrail = new Color(1f, 0.416f, 0.169f, 0.35f);
        private static readonly Color Line = new Color(0.965f, 0.922f, 0.867f, 0.2f);
        private static readonly Color Soft = new Color(0.965f, 0.922f, 0.867f, 0.06f);

        private PlayerController player;
        private Font font;
        private Sprite roundedSprite;

        private Image[] healthSegments;
        private Text healthText;
        private Text scoreText;
        private Text waveText;
        private Text bannerText;
        private Image hitFlashImage;

        private GameObject gameOverPanel;
        private GameObject newRecordBadge;
        private Text finalScoreText;
        private Text finalWaveText;
        private Text bestScoreText;
        private Text previousBestText;
        private GameObject[] leaderboardRows;
        private Image[] leaderboardRowImages;
        private Text[] leaderboardRanks;
        private Text[] leaderboardScores;

        private GameObject titlePanel;
        private Text promptText;
        private Text titleBestScoreText;
        private Text titleBestWaveText;

        private float targetFraction = 1f;
        private float shownFraction = 1f;
        private float trailFraction = 1f;
        private int previousHealth = -1;
        private float hitFlash;
        private float scorePop = 1f;
        private float bannerTimer;
        private float openedAt;
        private bool titleActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            hasStarted = false;
        }

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            roundedSprite = CreateRoundedSprite(32, 12);
            BuildUi();

            titleBestScoreText.text = PlayerPrefs.GetInt(BestScoreKey, 0).ToString("N0");
            titleBestWaveText.text = PlayerPrefs.GetInt(BestWaveKey, 0).ToString();

            openedAt = Time.unscaledTime;
            titleActive = !hasStarted;
            titlePanel.SetActive(titleActive);
            if (titleActive) Time.timeScale = 0f;
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            player.HealthChanged += UpdateHealth;
            UpdateHealth(player.CurrentHealth, player.MaxHealth);
            shownFraction = targetFraction;
            trailFraction = targetFraction;
            hitFlash = 0f;
        }

        private void OnEnable()
        {
            GameEvents.ScoreChanged += UpdateScore;
            GameEvents.WaveStarted += ShowWaveStarted;
            GameEvents.WaveCleared += ShowWaveCleared;
            GameEvents.GameOver += ShowGameOver;
        }

        private void OnDisable()
        {
            GameEvents.ScoreChanged -= UpdateScore;
            GameEvents.WaveStarted -= ShowWaveStarted;
            GameEvents.WaveCleared -= ShowWaveCleared;
            GameEvents.GameOver -= ShowGameOver;
        }

        private void OnDestroy()
        {
            if (player != null) player.HealthChanged -= UpdateHealth;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (titleActive)
            {
                Color prompt = promptText.color;
                prompt.a = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 4f);
                promptText.color = prompt;

                if (Time.unscaledTime - openedAt > 0.3f && Input.anyKeyDown) BeginGame();
            }

            shownFraction = Mathf.Lerp(shownFraction, targetFraction, 1f - Mathf.Exp(-14f * dt));
            trailFraction = Mathf.Lerp(trailFraction, targetFraction, 1f - Mathf.Exp(-3f * dt));
            DrawHealthSegments();

            hitFlash = Mathf.MoveTowards(hitFlash, 0f, dt * 1.2f);
            hitFlashImage.color = new Color(1f, 0.2f, 0.15f, hitFlash);

            scorePop = Mathf.MoveTowards(scorePop, 1f, dt * 2.5f);
            scoreText.rectTransform.localScale = Vector3.one * scorePop;

            if (bannerTimer > 0f)
            {
                bannerTimer -= dt;
                float elapsed = BannerDuration - bannerTimer;
                float alpha = Mathf.Clamp01(Mathf.Min(elapsed / 0.2f, bannerTimer / 0.5f));
                float pop = 1f + (1f - Mathf.Clamp01(elapsed / 0.3f)) * 0.2f;
                bannerText.color = new Color(Ember.r, Ember.g, Ember.b, alpha);
                bannerText.rectTransform.localScale = Vector3.one * pop;
            }
            else if (bannerText.color.a > 0f)
            {
                bannerText.color = new Color(Ember.r, Ember.g, Ember.b, 0f);
            }
        }

        private void DrawHealthSegments()
        {
            Color fill = targetFraction > 0.3f ? Ember : Danger;

            for (int i = 0; i < healthSegments.Length; i++)
            {
                float low = (float)i / healthSegments.Length;
                float filled = Mathf.Clamp01((shownFraction - low) * healthSegments.Length);
                float trailing = Mathf.Clamp01((trailFraction - low) * healthSegments.Length);

                if (filled > 0f) healthSegments[i].color = Color.Lerp(SegmentEmpty, fill, filled);
                else if (trailing > 0f) healthSegments[i].color = Color.Lerp(SegmentEmpty, SegmentTrail, trailing);
                else healthSegments[i].color = SegmentEmpty;
            }
        }

        private void BeginGame()
        {
            hasStarted = true;
            titleActive = false;
            titlePanel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void UpdateHealth(int current, int max)
        {
            targetFraction = max > 0 ? (float)current / max : 0f;
            healthText.text = current.ToString();
            if (previousHealth > current) hitFlash = 0.35f;
            previousHealth = current;
        }

        private void UpdateScore(int score)
        {
            scoreText.text = score.ToString("N0");
            scorePop = 1.35f;
        }

        private void ShowWaveStarted(int wave)
        {
            waveText.text = $"WAVE {wave}";
            ShowBanner($"WAVE {wave}");
        }

        private void ShowWaveCleared(int wave)
        {
            ShowBanner("WAVE CLEARED");
        }

        private void ShowBanner(string message)
        {
            bannerText.text = message;
            bannerTimer = BannerDuration;
        }

        private void ShowGameOver(int finalScore)
        {
            int wave = GameManager.Instance != null ? GameManager.Instance.Wave : 0;
            int previousBest = PlayerPrefs.GetInt(BestScoreKey, 0);
            int best = Mathf.Max(previousBest, finalScore);
            PlayerPrefs.SetInt(BestScoreKey, best);
            PlayerPrefs.SetInt(BestWaveKey, Mathf.Max(PlayerPrefs.GetInt(BestWaveKey, 0), wave));
            PlayerPrefs.Save();

            finalScoreText.text = finalScore.ToString("N0");
            finalWaveText.text = wave.ToString();
            bestScoreText.text = best.ToString("N0");
            previousBestText.text = previousBest.ToString("N0");
            newRecordBadge.SetActive(previousBest > 0 && finalScore > previousBest);

            var top = Leaderboard.Top(5);
            int highlight = top.IndexOf(finalScore);

            for (int i = 0; i < leaderboardRows.Length; i++)
            {
                bool used = i < top.Count;
                leaderboardRows[i].SetActive(used);
                if (!used) continue;

                bool mine = i == highlight;
                leaderboardRowImages[i].color = mine ? Amber : Soft;
                leaderboardRanks[i].text = (i + 1).ToString();
                leaderboardRanks[i].color = mine ? Dark : Cream;
                leaderboardScores[i].text = top[i].ToString("N0");
                leaderboardScores[i].color = mine ? Dark : Cream;
            }

            gameOverPanel.SetActive(true);
        }

        private void BuildUi()
        {
            GameObject canvasObject = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 500f);
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasObject.transform;
            Vector2 topLeft = new Vector2(0f, 1f);
            Vector2 topRight = new Vector2(1f, 1f);
            Vector2 topCenter = new Vector2(0.5f, 1f);
            Vector2 bottomLeft = new Vector2(0f, 0f);
            Vector2 center = new Vector2(0.5f, 0.5f);

            CreateText("Score Label", root, "SCORE", 12, TextAnchor.UpperLeft, topLeft, new Vector2(38f, -26f), new Vector2(200f, 18f), Muted, false);
            scoreText = CreateText("Score", root, "0", 52, TextAnchor.UpperLeft, topLeft, new Vector2(36f, -40f), new Vector2(300f, 64f), Cream, true);

            Image pillBorder = CreateImage("Wave Pill", root, new Color(Amber.r, Amber.g, Amber.b, 0.5f), topRight, new Vector2(-36f, -32f), new Vector2(124f, 40f), true);
            Image pill = CreateImage("Wave Pill Fill", pillBorder.transform, Ground, center, Vector2.zero, Vector2.zero, true);
            Stretch(pill.rectTransform, 1.5f);
            waveText = CreateText("Wave Text", pillBorder.transform, "GET READY", 17, TextAnchor.MiddleCenter, center, Vector2.zero, new Vector2(124f, 40f), Amber, true);

            Image hpBlock = CreateImage("Health Block", root, Ember, bottomLeft, new Vector2(36f, 28f), new Vector2(64f, 44f), true);
            CreateText("Health Label", hpBlock.transform, "HP", 11, TextAnchor.MiddleCenter, center, new Vector2(0f, 11f), new Vector2(64f, 16f), Dark, true);
            healthText = CreateText("Health Text", hpBlock.transform, "100", 20, TextAnchor.MiddleCenter, center, new Vector2(0f, -7f), new Vector2(64f, 26f), Dark, true);

            GameObject segmentRow = new GameObject("Health Segments", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            segmentRow.transform.SetParent(root, false);
            RectTransform segmentRect = segmentRow.GetComponent<RectTransform>();
            segmentRect.anchorMin = new Vector2(0f, 0f);
            segmentRect.anchorMax = new Vector2(1f, 0f);
            segmentRect.pivot = new Vector2(0.5f, 0f);
            segmentRect.offsetMin = new Vector2(116f, 28f);
            segmentRect.offsetMax = new Vector2(-36f, 42f);
            HorizontalLayoutGroup layout = segmentRow.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            healthSegments = new Image[HealthSegments];
            for (int i = 0; i < HealthSegments; i++)
            {
                GameObject segment = new GameObject($"Segment {i + 1}", typeof(RectTransform), typeof(Image));
                segment.transform.SetParent(segmentRow.transform, false);
                healthSegments[i] = segment.GetComponent<Image>();
                healthSegments[i].color = Ember;
                healthSegments[i].raycastTarget = false;
            }

            hitFlashImage = CreateImage("Hit Flash", root, new Color(1f, 0.2f, 0.15f, 0f), center, Vector2.zero, Vector2.zero, false);
            Stretch(hitFlashImage.rectTransform, 0f);

            bannerText = CreateText("Banner", root, "", 54, TextAnchor.MiddleCenter, center, new Vector2(0f, 90f), new Vector2(700f, 80f), new Color(Ember.r, Ember.g, Ember.b, 0f), true);

            BuildGameOver(root, topLeft, topRight, topCenter, center);
            BuildTitle(root, topCenter, center);
        }

        private void BuildGameOver(Transform root, Vector2 topLeft, Vector2 topRight, Vector2 topCenter, Vector2 center)
        {
            Image dim = CreateImage("Game Over", root, new Color(0.04f, 0.024f, 0.02f, 0.78f), center, Vector2.zero, Vector2.zero, false);
            Stretch(dim.rectTransform, 0f);
            gameOverPanel = dim.gameObject;

            Image card = CreateImage("Game Over Card", dim.transform, PanelColor, center, Vector2.zero, new Vector2(700f, 424f), true);
            CreateText("Title", card.transform, "YOU FELL", 40, TextAnchor.UpperLeft, topLeft, new Vector2(28f, -22f), new Vector2(320f, 52f), Ember, true);

            Image badge = CreateImage("New Record", card.transform, Amber, topRight, new Vector2(-28f, -30f), new Vector2(130f, 28f), true);
            CreateText("New Record Text", badge.transform, "NEW RECORD", 13, TextAnchor.MiddleCenter, center, Vector2.zero, new Vector2(130f, 28f), Dark, true);
            newRecordBadge = badge.gameObject;

            finalScoreText = CreateTile(card.transform, topLeft, "SCORE", new Vector2(28f, -92f), new Color(Ember.r, Ember.g, Ember.b, 0.14f), Cream);
            finalWaveText = CreateTile(card.transform, topLeft, "WAVE REACHED", new Vector2(221f, -92f), new Color(Amber.r, Amber.g, Amber.b, 0.12f), Cream);
            bestScoreText = CreateTile(card.transform, topLeft, "BEST SCORE", new Vector2(28f, -204f), Soft, Amber);
            previousBestText = CreateTile(card.transform, topLeft, "PREVIOUS BEST", new Vector2(221f, -204f), Soft, Cream);

            CreateText("List Label", card.transform, "TOP 5", 11, TextAnchor.UpperLeft, topLeft, new Vector2(422f, -92f), new Vector2(250f, 16f), Muted, false);

            leaderboardRows = new GameObject[5];
            leaderboardRowImages = new Image[5];
            leaderboardRanks = new Text[5];
            leaderboardScores = new Text[5];
            for (int i = 0; i < 5; i++)
            {
                Image row = CreateImage($"Row {i + 1}", card.transform, Soft, topLeft, new Vector2(422f, -116f - i * 40f), new Vector2(250f, 34f), true);
                leaderboardRows[i] = row.gameObject;
                leaderboardRowImages[i] = row;
                leaderboardRanks[i] = CreateText("Rank", row.transform, "", 16, TextAnchor.MiddleLeft, topLeft, new Vector2(12f, -17f), new Vector2(60f, 24f), Cream, true);
                leaderboardRanks[i].rectTransform.pivot = new Vector2(0f, 0.5f);
                leaderboardScores[i] = CreateText("Score", row.transform, "", 16, TextAnchor.MiddleRight, topRight, new Vector2(-12f, -17f), new Vector2(150f, 24f), Cream, true);
                leaderboardScores[i].rectTransform.pivot = new Vector2(1f, 0.5f);
            }

            Image button = CreateImage("Play Again", card.transform, Ember, topLeft, new Vector2(28f, -340f), new Vector2(644f, 52f), true);
            CreateText("Play Again Text", button.transform, "PLAY AGAIN (R)", 19, TextAnchor.MiddleCenter, center, Vector2.zero, new Vector2(644f, 52f), Dark, true);

            gameOverPanel.SetActive(false);
        }

        private void BuildTitle(Transform root, Vector2 topCenter, Vector2 center)
        {
            Image title = CreateImage("Title Screen", root, Ground, center, Vector2.zero, Vector2.zero, false);
            Stretch(title.rectTransform, 0f);
            titlePanel = title.gameObject;

            CreateText("Eyebrow", title.transform, "SURVIVE THE WAVES", 14, TextAnchor.MiddleCenter, center, new Vector2(0f, 168f), new Vector2(600f, 22f), Amber, true);
            CreateText("Game Title", title.transform, "EMBER", 88, TextAnchor.MiddleCenter, center, new Vector2(0f, 98f), new Vector2(760f, 96f), Cream, true);
            CreateText("Game Title Accent", title.transform, "ARENA", 88, TextAnchor.MiddleCenter, center, new Vector2(0f, 14f), new Vector2(760f, 96f), Ember, true);

            Image strip = CreateImage("Best Strip", title.transform, new Color(0.063f, 0.043f, 0.035f, 0.6f), center, new Vector2(0f, -86f), new Vector2(340f, 62f), true);
            Image divider = CreateImage("Divider", strip.transform, Line, center, Vector2.zero, new Vector2(1f, 62f), false);
            divider.rectTransform.sizeDelta = new Vector2(1f, 62f);
            CreateText("Best Score Label", strip.transform, "BEST SCORE", 11, TextAnchor.MiddleCenter, center, new Vector2(-85f, 16f), new Vector2(170f, 16f), Muted, false);
            titleBestScoreText = CreateText("Best Score Value", strip.transform, "0", 26, TextAnchor.MiddleCenter, center, new Vector2(-85f, -8f), new Vector2(170f, 32f), Amber, true);
            CreateText("Best Wave Label", strip.transform, "BEST WAVE", 11, TextAnchor.MiddleCenter, center, new Vector2(85f, 16f), new Vector2(170f, 16f), Muted, false);
            titleBestWaveText = CreateText("Best Wave Value", strip.transform, "0", 26, TextAnchor.MiddleCenter, center, new Vector2(85f, -8f), new Vector2(170f, 32f), Amber, true);

            Image button = CreateImage("Play Button", title.transform, Ember, center, new Vector2(0f, -150f), new Vector2(340f, 56f), true);
            promptText = CreateText("Prompt", button.transform, "PRESS ANY KEY TO PLAY", 19, TextAnchor.MiddleCenter, center, Vector2.zero, new Vector2(340f, 56f), Dark, true);

            CreateText("Controls", title.transform, "WASD or arrows to move   ·   Gun fires automatically   ·   R to restart", 13, TextAnchor.MiddleCenter, center, new Vector2(0f, -208f), new Vector2(740f, 20f), Muted, false);
        }

        private Text CreateTile(Transform parent, Vector2 anchor, string label, Vector2 position, Color tint, Color valueColor)
        {
            Image tile = CreateImage($"{label} Tile", parent, tint, anchor, position, new Vector2(181f, 100f), true);
            CreateText($"{label} Label", tile.transform, label, 11, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(160f, 16f), Muted, false);
            return CreateText($"{label} Value", tile.transform, "0", 38, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(14f, -36f), new Vector2(160f, 50f), valueColor, true);
        }

        private Image CreateImage(string objectName, Transform parent, Color color, Vector2 anchor, Vector2 position, Vector2 size, bool rounded)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (rounded)
            {
                image.sprite = roundedSprite;
                image.type = Image.Type.Sliced;
            }
            return image;
        }

        private Text CreateText(string objectName, Transform parent, string content, int size, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 boxSize, Color color, bool bold)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = boxSize;

            Text text = go.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static Sprite CreateRoundedSprite(int size, int radius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    Vector2 nearest = new Vector2(Mathf.Clamp(point.x, radius, size - radius), Mathf.Clamp(point.y, radius, size - radius));
                    float alpha = Mathf.Clamp01(radius - Vector2.Distance(point, nearest) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
    }
}
