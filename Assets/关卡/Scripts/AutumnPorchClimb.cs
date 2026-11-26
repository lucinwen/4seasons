using UnityEngine;
using Qingming.LevelBackground;

/// <summary>
/// 秋天关卡两处石阶的“按上键走上台阶”逻辑。
/// 门廊和石阶在场景里只是背景图，没有碰撞体，因此运行时分别生成：
/// 门廊高度的平台碰撞体 + 攀爬触发区。
/// 玩家在触发区内按住 ↑/W 沿台阶升到门廊，按住 ↓/S 下来。
/// 不修改场景文件。
/// </summary>
public class AutumnPorchClimb : MonoBehaviour
{
    private const float RitualPorchTopY = -1.90f;
    private const float RitualGroundTopY = -3.20f;
    private const float DyeGardenGroundTopY = -3.02f;
    private const float DefaultPlayerFootOffset = 0.718f;
    private const float ClimbSpeed = 2.5f;

    private Collider2D _platformCollider;
    private Rigidbody2D _rb;
    private LevelPlayerController _controller;
    private Collider2D _playerCol;
    private float _porchTopY;
    private float _groundTopY;
    private bool _climbing;
    private bool _descending;
    private bool _onPorch;

    public static void Setup()
    {
        if (GameObject.Find("AutumnPorchClimb") != null) return;

        var root = new GameObject("AutumnPorchClimb");
        Vector2 ritual = AnchorCenter("祭祀触发点", new Vector2(5.95f, -2.16f));
        Vector2 dyeGarden = AnchorCenter("染料园触点", new Vector2(-12.49f, 0.33f));
        float dyeGardenTopY = dyeGarden.y + PlayerFootOffset();

        CreateClimb(root.transform, "Ritual", ritual.x, 3.6f, 2.2f,
            RitualPorchTopY, RitualGroundTopY);
        CreateClimb(root.transform, "DyeGarden", dyeGarden.x, 3.2f, 2.2f,
            dyeGardenTopY, DyeGardenGroundTopY);
    }

    private static Vector2 AnchorCenter(string objectName, Vector2 fallback)
    {
        var anchor = GameObject.Find(objectName);
        if (anchor == null) return fallback;
        var col = anchor.GetComponent<Collider2D>();
        return col != null ? (Vector2)col.bounds.center : (Vector2)anchor.transform.position;
    }

    private static float PlayerFootOffset()
    {
        var player = FindObjectOfType<LevelPlayerController>();
        if (player == null) return DefaultPlayerFootOffset;
        var col = player.GetComponentInChildren<Collider2D>(true);
        return col != null ? col.bounds.min.y - player.transform.position.y : DefaultPlayerFootOffset;
    }

    private static void CreateClimb(Transform parent, string name, float centerX,
        float platformWidth, float zoneWidth, float porchTopY, float groundTopY)
    {
        var platform = new GameObject(name + "PorchPlatform");
        platform.transform.SetParent(parent, false);
        platform.transform.position = new Vector3(centerX, porchTopY - 0.1f, 0f);
        var platformCol = platform.AddComponent<BoxCollider2D>();
        platformCol.size = new Vector2(platformWidth, 0.2f);
        platformCol.enabled = false;

        float zoneBottom = groundTopY - 0.75f;
        float zoneTop = porchTopY + 2.1f;
        var zone = new GameObject(name + "ClimbZone");
        zone.transform.SetParent(parent, false);
        zone.transform.position = new Vector3(centerX, (zoneBottom + zoneTop) * 0.5f, 0f);
        var zoneCol = zone.AddComponent<BoxCollider2D>();
        zoneCol.isTrigger = true;
        zoneCol.size = new Vector2(zoneWidth, zoneTop - zoneBottom);

        var climb = zone.AddComponent<AutumnPorchClimb>();
        climb._platformCollider = platformCol;
        climb._porchTopY = porchTopY;
        climb._groundTopY = groundTopY;
    }

    private void Update()
    {
        if (_climbing)
        {
            if (WorkshopQuestGuide.BlocksWorldInput) StopClimb();
            else UpdateClimb();
            return;
        }

        // 只有真正走出平台左右边缘后才取消支撑，到顶时的微小高度变化不会让玩家自动下落。
        if (_onPorch && _playerCol != null && _platformCollider != null)
        {
            Bounds playerBounds = _playerCol.bounds;
            Bounds platformBounds = _platformCollider.bounds;
            bool outsidePlatform = playerBounds.max.x < platformBounds.min.x ||
                playerBounds.min.x > platformBounds.max.x;
            if (!outsidePlatform) return;

            _onPorch = false;
            _platformCollider.enabled = false;
            if (_controller != null) _controller.IsOnPorch = false;
            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.gravityScale = 3f;
            }
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

        if (up && feet < _porchTopY - 0.02f)
        {
            BeginClimb(false);
        }
        else if (down && feet > _groundTopY + 0.02f)
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
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.gravityScale = 0f;
            if (_controller != null)
            {
                _controller.IsClimbing = true;
                _controller.IsOnPorch = false;
            }
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
        if (!_descending && feet >= _porchTopY)
        {
            SnapTo(_porchTopY, true);
        }
        else if (_descending && feet <= _groundTopY)
        {
            SnapTo(_groundTopY, false);
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
        Physics2D.SyncTransforms();

        StopClimb();
    }

    private void StopClimb()
    {
        if (!_climbing) return;

        _climbing = false;
        if (_rb != null)
        {
            _rb.bodyType = _onPorch ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            _rb.gravityScale = _onPorch ? 0f : 3f;
            _rb.velocity = new Vector2(_rb.velocity.x, 0f);
        }
        if (_controller != null)
        {
            _controller.IsClimbing = false;
            _controller.IsOnPorch = _onPorch;
        }
        if (_platformCollider != null) _platformCollider.enabled = _onPorch;
    }
}
