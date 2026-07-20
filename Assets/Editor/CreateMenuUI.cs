#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Editor utility to create sample HowToPlay and Difficulty panels under a new Canvas
public static class CreateMenuUI
{
    [MenuItem("Tools/Snake2.0/Create Sample Menus")] 
    public static void CreateSampleMenus()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("UI_Sample_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = canvasGO.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);

        // Create HowToPlay Panel
        GameObject howPanel = CreatePanel(canvasGO.transform, "HowToPlayPanel");
        RectTransform rt = howPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.1f);
        rt.anchorMax = new Vector2(0.9f, 0.9f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Title
        CreateText(howPanel.transform, "How To Play", 36, TextAlignmentOptions.Center, new Vector2(0.5f, 0.95f));

        // Scroll View
        GameObject scroll = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
        scroll.transform.SetParent(howPanel.transform, false);
        RectTransform srt = scroll.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.05f, 0.1f);
        srt.anchorMax = new Vector2(0.95f, 0.85f);
        srt.offsetMin = srt.offsetMax = Vector2.zero;
        Image sim = scroll.GetComponent<Image>(); sim.color = new Color(1,1,1,0.05f);

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(scroll.transform, false);
        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0,1);
        crt.anchorMax = new Vector2(1,1);
        crt.pivot = new Vector2(0.5f,1);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(0, 600);

        ScrollRect sr = scroll.GetComponent<ScrollRect>();
        sr.content = crt;
        sr.vertical = true;

        // Content text entries
        CreateText(content.transform, "OBJECTIVE\n\nEat food.\nAvoid walls.\nAvoid your own body.", 18, TextAlignmentOptions.Left, new Vector2(0.5f, 1f));
        CreateText(content.transform, "\nCONTROLS\n\nArrow Keys\nWASD\nESC = Pause", 18, TextAlignmentOptions.Left, new Vector2(0.5f, 0.8f));
        CreateText(content.transform, "\nSPECIAL FOODS\n", 20, TextAlignmentOptions.Left, new Vector2(0.5f, 0.6f));

        // Individual food entries
        CreateText(content.transform, "Normal - Regular food that grows the snake and gives points.", 16, TextAlignmentOptions.Left, new Vector2(0.5f, 0.5f));
        CreateText(content.transform, "Golden - Extra points when collected.", 16, TextAlignmentOptions.Left, new Vector2(0.5f, 0.45f));
        CreateText(content.transform, "Bomb - Ends the game if collected.", 16, TextAlignmentOptions.Left, new Vector2(0.5f, 0.4f));
        CreateText(content.transform, "Shrink - Reduces snake length.", 16, TextAlignmentOptions.Left, new Vector2(0.5f, 0.35f));
        CreateText(content.transform, "Speed - Temporarily increases snake speed.", 16, TextAlignmentOptions.Left, new Vector2(0.5f, 0.3f));
        CreateText(content.transform, "Slow - Temporarily decreases snake speed.", 16, TextAlignmentOptions.Left, new Vector2(0.5f, 0.25f));
        CreateText(content.transform, "Ghost - Temporarily allows passing through walls/obstacles.", 16, TextAlignmentOptions.Left, new Vector2(0.5f, 0.2f));
        CreateText(content.transform, "Shield - Protects from one collision.", 16, TextAlignmentOptions.Left, new Vector2(0.5f, 0.15f));

        // Back Button
        CreateButton(howPanel.transform, "Back", new Vector2(0.5f, 0.03f));

        // Difficulty Panel
        GameObject diffPanel = CreatePanel(canvasGO.transform, "DifficultyPanel");
        RectTransform drt = diffPanel.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.3f, 0.3f);
        drt.anchorMax = new Vector2(0.7f, 0.7f);
        drt.offsetMin = drt.offsetMax = Vector2.zero;

        // Left arrow, name, right arrow
        CreateButton(diffPanel.transform, "<", new Vector2(0.15f, 0.6f));
        CreateText(diffPanel.transform, "Normal", 28, TextAlignmentOptions.Center, new Vector2(0.5f, 0.6f));
        CreateButton(diffPanel.transform, ">", new Vector2(0.85f, 0.6f));

        // Description
        CreateText(diffPanel.transform, "Balanced gameplay. Recommended for most players.", 16, TextAlignmentOptions.Center, new Vector2(0.5f, 0.4f));

        // Start & Back
        CreateButton(diffPanel.transform, "Start", new Vector2(0.7f, 0.15f));
        CreateButton(diffPanel.transform, "Back", new Vector2(0.3f, 0.15f));

        Debug.Log("Sample HowToPlay and Difficulty panels created under UI_Sample_Canvas.");
        Selection.activeGameObject = canvasGO;
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>(); img.color = new Color(0f, 0f, 0f, 0.6f);
        return go;
    }

    private static void CreateText(Transform parent, string text, int size, TextAlignmentOptions align, Vector2 anchor)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(800, 60);
    }

    private static void CreateButton(Transform parent, string label, Vector2 anchor)
    {
        GameObject go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>(); img.color = new Color(1f,1f,1f,0.1f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(160, 40);
        GameObject txt = new GameObject("Label", typeof(RectTransform)); txt.transform.SetParent(go.transform, false);
        TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>(); tmp.text = label; tmp.alignment = TextAlignmentOptions.Center;
        RectTransform trt = txt.GetComponent<RectTransform>(); trt.anchorMin = trt.anchorMax = new Vector2(0.5f,0.5f); trt.anchoredPosition = Vector2.zero; trt.sizeDelta = rt.sizeDelta;
    }
}
#endif
