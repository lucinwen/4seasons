using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared scenery and quiet controls for hands-on workshop activities.</summary>
internal static class WorkshopImmersive
{
    internal static string Backdrop(WorkshopSeason season) => season == WorkshopSeason.Spring
        ? "WorkshopObservation/Spring_LeafDesk" : season == WorkshopSeason.Summer
        ? "WorkshopObservation/Summer_CocoonBench" : season == WorkshopSeason.Autumn
        ? "WordRevision/AutumnDyeWorkbench" : "WorkshopFinish/DyeWorkbench";

    internal static void Background(RectTransform frame, WorkshopSeason season, string resource = null)
    {
        WorkshopUi.Block(frame, Color.black);
        var sprite = Resources.Load<Sprite>(resource ?? Backdrop(season));
        if (sprite == null) sprite = Resources.Load<Sprite>("WordRevision/AutumnDyeWorkbench");
        var image = WorkshopUi.Picture(frame, "Immersive Scenery", new Rect(0, 0, 1920, 1080), sprite, stretch:true);
        image.transform.SetAsFirstSibling();
    }

    internal static Text Text(Transform parent, string name, Rect bounds, string text, int size = 27,
        TextAnchor align = TextAnchor.MiddleLeft)
    {
        var label = WorkshopUi.Label(parent, name, bounds, text, size, WorkshopUi.Cream, align, WorkshopArt.ReadingFont);
        var shadow = label.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(32, 21, 13, 235);
        shadow.effectDistance = new Vector2(1.5f, -2f);
        label.raycastTarget = false;
        return label;
    }

    internal static Button Button(Transform parent, string name, Rect bounds, string text, Action clicked)
    {
        var button = WorkshopUi.Button(parent, name, bounds, "", clicked, new Color(0,0,0,.002f));
        Text(button.transform, "Label", new Rect(0,0,bounds.width,bounds.height),text,27,TextAnchor.MiddleCenter);
        return button;
    }

    internal static void StripSurface(Transform root)
    {
        if (root == null) return;
        foreach (var name in new[]{"Shadow","Edge","Surface","Mulberry Paper","Paper Face","Bronze Rule","Divider"})
        {
            var child = root.Find(name);
            if (child != null) child.gameObject.SetActive(false);
        }
        var image = root.GetComponent<Image>();
        if (image != null) { image.color = Color.clear; image.raycastTarget = false; }
    }

    internal static void QuietText(Text label)
    {
        if (label == null) return;
        label.font = WorkshopArt.ReadingFont;
        label.color = WorkshopUi.Cream;
        if (label.GetComponent<Shadow>() == null)
        {
            var shadow=label.gameObject.AddComponent<Shadow>();
            shadow.effectColor=new Color32(28,20,13,235); shadow.effectDistance=new Vector2(1,-2);
        }
    }
}
