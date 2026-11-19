/*********************************************************************************************
 * COMPONENT OF: none (helper used by GameUI and MainMenu)
 * REQUIRED DEPENDENCIES: uGUI (Canvas, Image, Text, Button), Input System package
 * DESCRIPTION: Small helpers that make the on-screen UI from code: a canvas, panels, text,
 *              buttons, and the EventSystem that lets you click the buttons.
 * AUTHOR: Ben Xiao (written with Claude)
 * VERSION: 1.0
 *********************************************************************************************/
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class UIKit
{
    public static readonly Color PanelColor = new Color(0.12f, 0.1f, 0.08f, 0.9f);
    public static readonly Color ButtonColor = new Color(0.95f, 0.6f, 0.15f);
    public static readonly Color Gold = new Color(1f, 0.85f, 0.3f);

    private static Font font;

    public static Font Font
    {
        get
        {
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

    // A screen-sized canvas that scales with the window
    public static RectTransform MakeCanvas(GameObject owner, int order)
    {
        Canvas canvas = owner.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        CanvasScaler scaler = owner.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        owner.AddComponent<GraphicRaycaster>();

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            InputSystemUIInputModule module = events.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        return (RectTransform)owner.transform;
    }

    public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    public static Image Image(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    public static RectTransform Panel(Transform parent, string name, Color color)
    {
        return Image(parent, name, color).rectTransform;
    }

    public static Text Label(Transform parent, string value, int size, TextAnchor anchor, Color color, bool darkEdge = true)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = Font;
        text.text = value;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.supportRichText = true;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        // Dark edge so it is readable on the bright sky
        if (darkEdge)
        {
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
        return text;
    }

    public static Button Button(Transform parent, string label, Color color, System.Action onClick, int fontSize = 18)
    {
        Image image = Image(parent, "Button", color);
        Button button = image.gameObject.AddComponent<Button>();
        // No keyboard navigation: otherwise the last clicked button stays "selected" and
        // pressing Enter later would click it again
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() =>
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            Sfx.Play("click", 0.5f);
            onClick();
        });

        if (!string.IsNullOrEmpty(label))
        {
            Text text = Label(image.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
            Place(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }
        return button;
    }

    // Changes the text on a button made with Button()
    public static void SetButtonText(Button button, string label)
    {
        Text text = button.GetComponentInChildren<Text>();
        if (text != null)
            text.text = label;
    }
}
