using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 秋天关卡门前石阶的“按上键走上台阶”逻辑。
/// 门廊和石阶在场景里只是背景图，没有碰撞体，因此运行时生成：
/// 一块门廊高度的平台碰撞体 + 一块攀爬触发区。
/// 玩家在触发区内按住 ↑/W 沿台阶升到门廊，按住 ↓/S 下来。
/// 不修改场景文件。
/// </summary>
public class AutumnPorchClimb : MonoBehaviour
{
    private const float PorchTopY = -1.90f;   // 门廊地面高度（与背景图石台对齐）
    private const float GroundTopY = -3.20f;  // 台阶下方地面高度
    private const float ClimbSpeed = 2.5f;

    private Collider2D _platformCollider;
    private Rigidbody2D _rb;
    private LevelPlayerController _controller;
    private Collider2D _playerCol;
    private bool _climbing;
    private bool _descending;
    private bool _onPorch;

    public static void Setup()
    {
        if (GameObject.Find("AutumnPorchClimb") != null) return;

        var root = new GameObject("AutumnPorchClimb");

        var platform = new GameObject("PorchPlatform");
        platform.transform.SetParent(root.transform, false);
        platform.transform.position = new Vector3(-5.2f, PorchTopY - 0.1f, 0f);
        var platformCol = platform.AddComponent<BoxCollider2D>();
        platformCol.size = new Vector2(3.6f, 0.2f);
        platformCol.enabled = false;

        var zone = new GameObject("ClimbZone");
        zone.transform.SetParent(root.transform, false);
        zone.transform.position = new Vector3(-5.2f, -2.35f, 0f);
        var zoneCol = zone.AddComponent<BoxCollider2D>();
        zoneCol.isTrigger = true;
        zoneCol.size = new Vector2(2.2f, 3.1f);

        var climb = zone.AddComponent<AutumnPorchClimb>();
        climb._platformCollider = platformCol;
    }

    private void Update()
    {
        if (_climbing)
        {
            if (WorkshopQuestGuide.BlocksWorldInput) StopClimb();
            else UpdateClimb();
            return;
        }

        // 在门廊上走出平台或掉下去时，及时关闭平台碰撞，避免它挡住地面上的玩家
        if (_onPorch && _playerCol != null && _playerCol.bounds.min.y < PorchTopY - 0.15f)
        {
            _onPorch = false;
            if (_platformCollider != null) _platformCollider.enabled = false;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        var controller = rb.GetComponent<LevelPlayerController>();
        if (controller == null || WorkshopQuestGuide.BlocksWorldInput) return;

        _rb = rb;
        _controller = controller;
        _playerCol = other;

        bool up = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        bool down = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);

        if (!up && !down)
        {
            if (_climbing) StopClimb();
            return;
        }

        float feet = _playerCol.bounds.min.y;

        if (up && feet < PorchTopY - 0.02f)
        {
            BeginClimb(false);
        }
        else if (down && feet > GroundTopY + 0.02f)
        {
            BeginClimb(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_rb != null && other.attachedRigidbody == _rb) StopClimb();
    }

    private void BeginClimb(bool down)
    {
        if (!_climbing)
        {
            _climbing = true;
            _rb.gravityScale = 0f;
            if (_controller != null) _controller.IsClimbing = true;
        }

        _descending = down;

        // 从门廊往下走时要先关掉平台碰撞，否则会被平台顶住
        if (down)
        {
            _onPorch = false;
            if (_platformCollider != null) _platformCollider.enabled = false;
        }
    }

    private void UpdateClimb()
    {
        if (_rb == null || _playerCol == null)
        {
            StopClimb();
            return;
        }

        bool up = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        bool down = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
        if ((_descending && !down) || (!_descending && !up))
        {
            StopClimb();
            return;
        }

        _rb.velocity = new Vector2(0f, _descending ? -ClimbSpeed : ClimbSpeed);

        float feet = _playerCol.bounds.min.y;
        if (!_descending && feet >= PorchTopY)
        {
            SnapTo(PorchTopY, true);
        }
        else if (_descending && feet <= GroundTopY)
        {
            SnapTo(GroundTopY, false);
        }
    }

    private void SnapTo(float targetFeetY, bool onPorch)
    {
        float footOffset = _playerCol.bounds.min.y - _rb.position.y;
        Vector2 pos = _rb.position;
        pos.y = targetFeetY - footOffset;
        _rb.position = pos;
        _rb.velocity = Vector2.zero;

        _onPorch = onPorch;
        if (_platformCollider != null) _platformCollider.enabled = onPorch;

        StopClimb();
    }

    private void StopClimb()
    {
        if (!_climbing) return;

        _climbing = false;
        if (_rb != null) _rb.gravityScale = 3f;
        if (_controller != null) _controller.IsClimbing = false;
        if (_platformCollider != null) _platformCollider.enabled = _onPorch;
    }
}
