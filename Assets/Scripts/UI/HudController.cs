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

        private static readonly Color PanelColor = new Color(0.10f, 0.08f, 0.06f, 0.88f);
        private static readonly Color Ember = new Color(1f, 0.48f, 0.18f);
        private static readonly Color Cream = new Color(0.95f, 0.91f, 0.88f);
        private static readonly Color Muted = new Color(0.64f, 0.60f, 0.54f);
        private static readonly Color Danger = new Color(1f, 0.35f, 0.35f);
        private static readonly Color Glow = new Color(1f, 0.85f, 0.66f, 0.55f);

        private PlayerController player;
        private Font font;
        private Sprite roundedSprite;

        private RectTransform healthFill;
        private RectTransform healthTrail;
        private Image healthFillImage;
        private Image healthTrailImage;
        private Text healthText;
        private Text scoreText;
        private Text waveText;
        private Text bannerText;
        private Image hitFlashImage;

        private GameObject gameOverPanel;
        private Text finalScoreText;
        private Text newBestText;
        private Text[] leaderboardRows;

        private GameObject titlePanel;
        private Text promptText;

        private float targetFraction = 1f;
        private float shownFraction = 1f;
        private float trailFraction = 1f;
        private int previousHealth = -1;
        private float hitFlash;
        private float scorePop = 1f;
        private float bannerTimer;
        private float openedAt;
        private bool titleActive;

        private const float BannerDuration = 1.8f;

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

            openedAt = Time.unscaledTime;
            titleActive = !hasStarted;
            titlePanel.SetActive(titleActive);
            if (titleActive) Time.timeScale = 0f;
        }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
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
                prompt.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
                promptText.color = prompt;

                if (Time.unscaledTime - openedAt > 0.3f && Input.anyKeyDown) BeginGame();
            }

            shownFraction = Mathf.Lerp(shownFraction, targetFraction, 1f - Mathf.Exp(-14f * dt));
            trailFraction = Mathf.Lerp(trailFraction, targetFraction, 1f - Mathf.Exp(-3f * dt));

            healthFill.anchorMax = new Vector2(shownFraction, 1f);
            healthTrail.anchorMax = new Vector2(trailFraction, 1f);
            healthFillImage.enabled = shownFraction > 0.03f;
            healthTrailImage.enabled = trailFraction > 0.03f;
            healthFillImage.color = targetFraction > 0.3f ? Ember : Danger;

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
            healthText.text = $"{current} / {max}";
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
            var top = Leaderboard.Top(5);
            int highlight = top.IndexOf(finalScore);

            finalScoreText.text = finalScore.ToString("N0");
            bool newBest = highlight == 0 && top.Count > 1 && top[1] < finalScore;
            newBestText.text = newBest ? "NEW BEST SCORE" : "";

            for (int i = 0; i < leaderboardRows.Length; i++)
            {
                if (i < top.Count)
                {
                    leaderboardRows[i].text = $"{i + 1}.   {top[i]:N0}";
                    leaderboardRows[i].color = i == highlight ? Ember : Cream;
                }
                else
                {
                    leaderboardRows[i].text = "";
                }
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
            Vector2 center = new Vector2(0.5f, 0.5f);

            Image bar = CreateImage("Health Bar", root, PanelColor, topLeft, new Vector2(20f, -20f), new Vector2(250f, 28f), true);
            healthTrailImage = CreateImage("Health Trail", bar.transform, Glow, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, true);
            healthTrail = healthTrailImage.rectTransform;
            Stretch(healthTrail, 3f);
            healthFillImage = CreateImage("Health Fill", bar.transform, Ember, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, true);
            healthFill = healthFillImage.rectTransform;
            Stretch(healthFill, 3f);
            healthText = CreateText("Health Text", bar.transform, "100 / 100", 14, TextAnchor.MiddleCenter, center, Vector2.zero, new Vector2(250f, 28f), Cream, true);

            CreateText("Score Label", root, "SCORE", 12, TextAnchor.UpperRight, topRight, new Vector2(-22f, -14f), new Vector2(200f, 18f), Muted, true);
            scoreText = CreateText("Score", root, "0", 32, TextAnchor.UpperRight, topRight, new Vector2(-20f, -28f), new Vector2(240f, 44f), Cream, true);

            Image pill = CreateImage("Wave Pill", root, PanelColor, topCenter, new Vector2(0f, -20f), new Vector2(130f, 30f), true);
            waveText = CreateText("Wave Text", pill.transform, "GET READY", 16, TextAnchor.MiddleCenter, center, Vector2.zero, new Vector2(130f, 30f), Ember, true);

            hitFlashImage = CreateImage("Hit Flash", root, new Color(1f, 0.2f, 0.15f, 0f), center, Vector2.zero, Vector2.zero, false);
            Stretch(hitFlashImage.rectTransform, 0f);

            bannerText = CreateText("Banner", root, "", 54, TextAnchor.MiddleCenter, center, new Vector2(0f, 90f), new Vector2(700f, 80f), new Color(Ember.r, Ember.g, Ember.b, 0f), true);

            Image dim = CreateImage("Game Over", root, new Color(0f, 0f, 0f, 0.72f), center, Vector2.zero, Vector2.zero, false);
            Stretch(dim.rectTransform, 0f);
            gameOverPanel = dim.gameObject;

            Image card = CreateImage("Game Over Card", dim.transform, PanelColor, center, Vector2.zero, new Vector2(380f, 430f), true);
            CreateText("Title", card.transform, "GAME OVER", 34, TextAnchor.UpperCenter, topCenter, new Vector2(0f, -26f), new Vector2(340f, 44f), Ember, true);
            CreateText("Score Label", card.transform, "SCORE", 13, TextAnchor.UpperCenter, topCenter, new Vector2(0f, -84f), new Vector2(340f, 20f), Muted, true);
            finalScoreText = CreateText("Final Score", card.transform, "0", 46, TextAnchor.UpperCenter, topCenter, new Vector2(0f, -102f), new Vector2(340f, 58f), Cream, true);
            newBestText = CreateText("New Best", card.transform, "", 16, TextAnchor.UpperCenter, topCenter, new Vector2(0f, -162f), new Vector2(340f, 24f), Ember, true);
            CreateText("List Label", card.transform, "BEST SCORES", 13, TextAnchor.UpperCenter, topCenter, new Vector2(0f, -198f), new Vector2(340f, 20f), Muted, true);

            leaderboardRows = new Text[5];
            for (int i = 0; i < leaderboardRows.Length; i++)
            {
                leaderboardRows[i] = CreateText($"Row {i + 1}", card.transform, "", 22, TextAnchor.UpperCenter, topCenter, new Vector2(0f, -224f - i * 30f), new Vector2(340f, 30f), Cream, false);
            }

            CreateText("Footer", card.transform, "Press R to restart", 16, TextAnchor.UpperCenter, topCenter, new Vector2(0f, -392f), new Vector2(340f, 24f), Muted, false);
            gameOverPanel.SetActive(false);

            Image title = CreateImage("Title Screen", root, new Color(0.06f, 0.05f, 0.04f, 0.92f), center, Vector2.zero, Vector2.zero, false);
            Stretch(title.rectTransform, 0f);
            titlePanel = title.gameObject;
            CreateText("Game Title", title.transform, "EMBER ARENA", 64, TextAnchor.MiddleCenter, center, new Vector2(0f, 50f), new Vector2(760f, 90f), Ember, true);
            CreateText("Tagline", title.transform, "Survive the waves", 22, TextAnchor.MiddleCenter, center, new Vector2(0f, -8f), new Vector2(600f, 34f), Cream, false);
            CreateText("Controls", title.transform, "WASD or arrow keys to move. Your gun fires on its own.", 16, TextAnchor.MiddleCenter, center, new Vector2(0f, -50f), new Vector2(700f, 26f), Muted, false);
            promptText = CreateText("Prompt", title.transform, "Press any key to start", 20, TextAnchor.MiddleCenter, center, new Vector2(0f, -120f), new Vector2(600f, 30f), Cream, true);
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
