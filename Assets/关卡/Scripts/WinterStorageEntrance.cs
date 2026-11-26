using UnityEngine;
using Qingming.LevelBackground;

/// <summary>The right-hand winter storehouse opens the existing material room.</summary>
public sealed class WinterStorageEntrance : MonoBehaviour
{
    public float triggerDistance = 3.5f;
    private FloatingActionButton _button;
    private Transform _player;
    private bool _shown;

    private void Awake()
    {
        _button = gameObject.AddComponent<FloatingActionButton>();
        _button.Configure("储物屋 · 查看材料", new Vector3(0f, 2.3f, 0f), OpenStorage,
            FloatingActionButton.VisualStyle.InkPlaque);
    }

    private void Update()
    {
        if (_player == null)
        {
            var player = GameObject.Find("Player");
            if (player != null) _player = player.transform;
        }
        bool near = _player != null && !WorkshopQuestGuide.BlocksWorldInput &&
            !WorkshopQuestGuide.IsWorldOverlayActive && !WorkshopStorageView.IsOpen &&
            !WorkshopDialoguePresenter.IsOpen && !MiniGameOverlay.IsActive &&
            Vector2.Distance(_player.position, transform.position) <= triggerDistance;
        if (near == _shown) return;
        _shown = near;
        if (near) _button.Show(); else _button.Hide();
    }

    private void OpenStorage()
    {
        _shown = false;
        _button.Hide();
        WorkshopStorageView.Show(WorkshopSeason.Winter);
    }
}
