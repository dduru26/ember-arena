using System.Text;
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
        private PlayerController player;
        private RectTransform healthFill;
        private Image healthFillImage;
        private Text healthText;
        private Text scoreText;
        private Text waveText;
        private Text gameOverText;
        private GameObject gameOverPanel;
        private Font font;

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            BuildUi();
        }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            player.HealthChanged += UpdateHealth;
            UpdateHealth(player.CurrentHealth, player.MaxHealth);
        }

        private void OnEnable()
        {
            GameEvents.ScoreChanged += UpdateScore;
            GameEvents.WaveStarted += UpdateWave;
            GameEvents.GameOver += ShowGameOver;
        }

        private void OnDisable()
        {
            GameEvents.ScoreChanged -= UpdateScore;
            GameEvents.WaveStarted -= UpdateWave;
            GameEvents.GameOver -= ShowGameOver;
        }

        private void OnDestroy()
        {
            if (player != null) player.HealthChanged -= UpdateHealth;
        }

        private void UpdateHealth(int current, int max)
        {
            float fraction = max > 0 ? (float)current / max : 0f;
            healthFill.anchorMax = new Vector2(fraction, 1f);
            healthFillImage.color = fraction > 0.3f ? new Color(0.48f, 0.85f, 0.56f) : new Color(1f, 0.35f, 0.35f);
            healthText.text = $"{current}/{max}";
        }

        private void UpdateScore(int score)
        {
            scoreText.text = $"Score {score}";
        }

        private void UpdateWave(int wave)
        {
            waveText.text = $"Wave {wave}";
        }

        private void ShowGameOver(int finalScore)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("GAME OVER");
            builder.AppendLine($"Score {finalScore}");
            builder.AppendLine();
            builder.AppendLine("Best scores");

            var top = Leaderboard.Top(5);
            for (int i = 0; i < top.Count; i++)
            {
                builder.AppendLine($"{i + 1}.  {top[i]}");
            }

            builder.AppendLine();
            builder.Append("Press R to restart");

            gameOverText.text = builder.ToString();
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

            Image barBackground = CreateImage("Health Bar", root, new Color(0f, 0f, 0f, 0.6f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(220f, 22f));

            healthFillImage = CreateImage("Health Fill", barBackground.transform, new Color(0.48f, 0.85f, 0.56f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            healthFill = healthFillImage.rectTransform;
            healthFill.anchorMin = Vector2.zero;
            healthFill.anchorMax = Vector2.one;
            healthFill.offsetMin = Vector2.zero;
            healthFill.offsetMax = Vector2.zero;

            healthText = CreateText("Health Text", barBackground.transform, "100/100", 14, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 22f));
            scoreText = CreateText("Score", root, "Score 0", 22, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-20f, -16f), new Vector2(240f, 36f));
            waveText = CreateText("Wave", root, "Get ready", 22, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(240f, 36f));

            Image panel = CreateImage("Game Over Panel", root, new Color(0f, 0f, 0f, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            panel.rectTransform.anchorMin = Vector2.zero;
            panel.rectTransform.anchorMax = Vector2.one;
            panel.rectTransform.offsetMin = Vector2.zero;
            panel.rectTransform.offsetMax = Vector2.zero;
            gameOverPanel = panel.gameObject;

            gameOverText = CreateText("Game Over Text", panel.transform, "", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 400f));
            gameOverPanel.SetActive(false);
        }

        private Image CreateImage(string objectName, Transform parent, Color color, Vector2 anchor, Vector2 position, Vector2 size)
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
            return image;
        }

        private Text CreateText(string objectName, Transform parent, string content, int size, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 boxSize)
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
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
