using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PerformanceMonitor : MonoBehaviour
{
    public int goodFps = 60;
    public int badFps = 30;
    public int goodDrawCalls = 50;
    public int badDrawCalls = 150;
    public int goodBatches = 50;
    public int badBatches = 150;

    private Text performanceText;
    private float deltaTime;
    private bool isPerformanceVisible = true;

    void Start()
    {
        GameObject canvasObject = new GameObject("PerformanceCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject textObject = new GameObject("PerformanceText");
        textObject.transform.SetParent(canvasObject.transform, false);
        performanceText = textObject.AddComponent<Text>();
        performanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        performanceText.fontSize = 24; // Adjust the font size as needed
        performanceText.alignment = TextAnchor.UpperLeft;

        RectTransform rectTransform = performanceText.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(10, -10);
        rectTransform.sizeDelta = new Vector2(400, 100); // Adjust the size as needed

        performanceText.color = Color.white; // Set the default color to white

        CreateToggleButton(canvasObject);
        TogglePerformanceVisibility();
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        int drawCalls = UnityStats.drawCalls;
        int batches = UnityStats.batches;

        string fpsColor = (fps >= goodFps) ? "green" : (fps <= badFps) ? "red" : "yellow";
        string drawCallsColor = (drawCalls <= goodDrawCalls) ? "green" : (drawCalls >= badDrawCalls) ? "red" : "yellow";
        string batchesColor = (batches <= goodBatches) ? "green" : (batches >= badBatches) ? "red" : "yellow";

        performanceText.text = string.Format(
            "<color={0}>FPS: {1:0.0}</color>\n<color={2}>Draw Calls: {3}</color>\n<color={4}>Batches: {5}</color>",
            fpsColor, fps, drawCallsColor, drawCalls, batchesColor, batches);
    }

    void CreateToggleButton(GameObject canvasObject)
    {
        GameObject buttonObject = new GameObject("ToggleButton");
        buttonObject.transform.SetParent(canvasObject.transform, false);
        Button button = buttonObject.AddComponent<Button>();
        Text buttonText = buttonObject.AddComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.text = "Toggle Debug";
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.black;

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.pivot = new Vector2(0, 0);
        rectTransform.anchoredPosition = new Vector2(10, 10);
        rectTransform.sizeDelta = new Vector2(100, 40); // Adjust the size as needed

        button.onClick.AddListener(TogglePerformanceVisibility);
    }

    void TogglePerformanceVisibility()
    {
        isPerformanceVisible = !isPerformanceVisible;
        performanceText.enabled = isPerformanceVisible;
    }
}
