using System.Collections;
using UnityEngine;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private enum WorkGesture { Tap, Move, Drag, Hold }

    private static readonly Color GuideGold = new Color32(255, 214, 105, 255);
    private static readonly Color GuideShadow = new Color32(34, 32, 24, 240);

    private RectTransform VisualStep(RectTransform target, Color? color = null)
    {
        Vector2 size = target.rect.size;
        var border = WorkshopUi.Rect(target, "Current Action Highlight", new Rect(0, 0, size.x, size.y));
        float corner = Mathf.Clamp(Mathf.Min(size.x, size.y) * .27f, 16f, 39f);
        for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            {
                float left = x == 0 ? -4f : size.x - corner + 4f;
                float top = y == 0 ? -4f : size.y - corner + 4f;
                float innerX = x == 0 ? left : size.x - 4f;
                float innerY = y == 0 ? top : size.y - 4f;
                WorkshopUi.Fill(border, "Corner Shadow", new Rect(left - 2, innerY - 2, corner + 4, 12), GuideShadow);
                WorkshopUi.Fill(border, "Corner Shadow", new Rect(innerX - 2, top - 2, 12, corner + 4), GuideShadow);
                WorkshopUi.Fill(border, "Corner Edge", new Rect(left, innerY, corner, 8), color ?? GuideGold);
                WorkshopUi.Fill(border, "Corner Edge", new Rect(innerX, top, 8, corner), color ?? GuideGold);
            }
        var fade = border.gameObject.AddComponent<CanvasGroup>();
        fade.blocksRaycasts = false;
        fade.interactable = false;
        StartCoroutine(PulseWorkbenchCue(fade));
        return border;
    }

    private static IEnumerator PulseWorkbenchCue(CanvasGroup fade)
    {
        while (fade != null && fade.gameObject.activeInHierarchy)
        {
            fade.alpha = .77f + .23f * Mathf.Cos(Time.unscaledTime * 4f);
            yield return null;
        }
        // Callers can show the same marker again without recreating it.
        if (fade != null) fade.alpha = 1f;
    }

    private RectTransform ShowGesture(RectTransform parent, Vector2 from, Vector2 to, WorkGesture gesture = WorkGesture.Move)
    {
        var cue = WorkshopUi.Rect(parent, "Action Demonstration " + gesture, new Rect());
        var surface = cue.gameObject.AddComponent<CanvasGroup>();
        surface.blocksRaycasts = false;
        surface.interactable = false;
        bool moving = gesture == WorkGesture.Move || gesture == WorkGesture.Drag;
        if (moving && (to - from).sqrMagnitude > 900f)
        {
            Vector2 direction = (to - from).normalized;
            Vector2 start = from + direction * 28f;
            Vector2 tip = to - direction * 25f;
            float distance = Vector2.Distance(start, tip);
            int dots = Mathf.Clamp(Mathf.RoundToInt(distance / 35f), 1, 12);
            for (int i = 0; i < dots; i++)
            {
                Vector2 point = Vector2.Lerp(start, tip, i / (float)dots);
                DrawCueStroke(cue, point, point + direction * 13f, 6f, GuideGold);
            }
            Vector2 cross = new Vector2(-direction.y, direction.x);
            DrawCueStroke(cue, tip - direction * 26f + cross * 15f, tip, 8f, GuideGold);
            DrawCueStroke(cue, tip - direction * 26f - cross * 15f, tip, 8f, GuideGold);
        }

        var mouse = WorkshopUi.Rect(cue, "Mouse Pointer", new Rect());
        DrawCueStroke(mouse, new Vector2(0, 0), new Vector2(3, 34), 7f, WorkshopUi.Cream);
        DrawCueStroke(mouse, new Vector2(0, 0), new Vector2(26, 18), 7f, WorkshopUi.Cream);
        DrawCueStroke(mouse, new Vector2(26, 18), new Vector2(14, 21), 7f, WorkshopUi.Cream);
        DrawCueStroke(mouse, new Vector2(14, 21), new Vector2(3, 34), 7f, WorkshopUi.Cream);
        var press = WorkshopUi.Rect(mouse, "Mouse Press", new Rect());
        DrawCueStroke(press, new Vector2(2, -8), new Vector2(2, -25), 6f, GuideGold);
        DrawCueStroke(press, new Vector2(-7, -5), new Vector2(-20, -17), 6f, GuideGold);
        DrawCueStroke(press, new Vector2(-9, 5), new Vector2(-26, 5), 6f, GuideGold);
        var holdMarks = new Image[gesture == WorkGesture.Hold ? 10 : 0];
        for (int i = 0; i < holdMarks.Length; i++)
        {
            float angle = -Mathf.PI * .5f + i * 2f * Mathf.PI / holdMarks.Length;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 start = direction * 46f;
            var block = WorkshopUi.Fill(mouse, "Hold Progress Notch", new Rect(start.x - 5, start.y - 5, 11, 11), GuideGold);
            WorkshopUi.ApplyPixelShape(block);
            holdMarks[i] = block;
        }
        StartCoroutine(AnimateWorkbenchGesture(cue, mouse, press, holdMarks, from, to, gesture));
        return cue;
    }

    private static void DrawCueStroke(Transform parent, Vector2 a, Vector2 b, float width, Color color)
    {
        float length = Vector2.Distance(a, b);
        Vector2 delta = b - a;
        float angle = -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        var edge = WorkshopUi.Fill(parent, "Gesture Outline", new Rect(a.x, a.y, length, width + 5f), GuideShadow).rectTransform;
        var line = WorkshopUi.Fill(parent, "Gesture Mark", new Rect(a.x, a.y, length, width), color).rectTransform;
        edge.pivot = line.pivot = new Vector2(0f, .5f);
        edge.localEulerAngles = line.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    private static IEnumerator AnimateWorkbenchGesture(RectTransform cue, RectTransform mouse, RectTransform press,
        Image[] holdMarks, Vector2 from, Vector2 to, WorkGesture gesture)
    {
        float elapsed = 0f;
        while (cue != null && cue.gameObject.activeInHierarchy)
        {
            float t = Mathf.Repeat(elapsed, 2.15f);
            float movement = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - .27f) / 1.1f));
            if (gesture == WorkGesture.Move && t > 1.37f)
                movement = 1f - Mathf.SmoothStep(0f, 1f, (t - 1.37f) / .78f);
            bool moving = gesture == WorkGesture.Move || gesture == WorkGesture.Drag;
            Vector2 at = moving ? Vector2.Lerp(from, to, movement) : from;
            bool pressed = gesture == WorkGesture.Tap ? t < .32f :
                gesture == WorkGesture.Drag || gesture == WorkGesture.Hold ? t < 1.36f : false;
            if (!pressed && !moving) at += new Vector2(7, 8);
            mouse.anchoredPosition = new Vector2(at.x, -at.y);
            mouse.localScale = Vector3.one * (pressed ? .91f : 1f);
            press.gameObject.SetActive(pressed);
            for (int i = 0; i < holdMarks.Length; i++)
                holdMarks[i].color = pressed && t >= i * .13f ? GuideGold : GuideShadow;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static Vector2 WorkbenchPoint(RectTransform parent, RectTransform target)
    {
        Vector3 local = parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
        return new Vector2(local.x - parent.rect.xMin, parent.rect.yMax - local.y);
    }

}
