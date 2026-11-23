using System;
using Qingming.LevelBackground;
using UnityEngine;

/// <summary>World-space destination used by the playable main hall.</summary>
public sealed class WorkshopHallInteractable : MonoBehaviour
{
    public string actionLabel { get; private set; }
    public bool isAvailable { get; private set; }
    public bool isNearest { get; private set; }

    private FloatingActionButton actionTag;
    private Action action;

    public void Configure(string label, Action clicked, bool available)
    {
        actionLabel = label;
        action = clicked;
        isAvailable = available;

        actionTag = gameObject.GetComponent<FloatingActionButton>();
        if (actionTag == null) actionTag = gameObject.AddComponent<FloatingActionButton>();
        actionTag.Configure(label, new Vector3(0f, 3.15f, 0f), Invoke,
            FloatingActionButton.VisualStyle.PaperTag);
        actionTag.SetOpacity(0.96f);
        actionTag.SetInteractable(available);
        actionTag.Hide();
    }

    public void SetNearest(bool nearest, bool blocked)
    {
        isNearest = nearest && !blocked;
        if (actionTag == null) return;
        actionTag.SetInteractable(isAvailable);
        if (isNearest) actionTag.Show();
        else actionTag.Hide();
    }

    public void Invoke()
    {
        if (!isAvailable) return;
        action?.Invoke();
    }
}
