using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal sealed class WorkshopPointer : MonoBehaviour
{
    private const string ResourceFolder = "WorkshopGameArt/Pointer/";
    private enum Shape { Arrow, Point, Grab }

    private static WorkshopPointer instance;
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private readonly List<MonoBehaviour> handlers = new List<MonoBehaviour>();
    private Texture2D pointTexture;
    private Texture2D grabTexture;
    private EventSystem raycastEventSystem;
    private PointerEventData pointer;
    private Shape shape;
    private GameObject heldTarget;
    private EventSystem heldEventSystem;
    private int worldPointFrame = -1;
    private int worldGrabFrame = -1;
    private Vector2 lastPosition;
    private float nextIdleRaycast;
    private bool lastWorldPoint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;
        var pointer = Resources.Load<Texture2D>(ResourceFolder + "WorkshopPointerHand");
        var grab = Resources.Load<Texture2D>(ResourceFolder + "WorkshopPointerGrab");
        if (pointer == null || grab == null) return;
        var host = new GameObject("Workshop Pointer");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<WorkshopPointer>();
        instance.pointTexture = pointer;
        instance.grabTexture = grab;
    }

    internal static bool CanShowWorldHint => instance != null && Application.isFocused &&
        Input.mousePresent && Input.touchCount == 0 && Cursor.visible;

    internal static void PointAtWorldTarget()
    {
        if (instance != null) instance.worldPointFrame = Time.frameCount;
    }

    internal static void GrabWorldTarget()
    {
        if (instance != null) instance.worldGrabFrame = Time.frameCount;
    }

    private void OnEnable() => SceneManager.activeSceneChanged += OnSceneChanged;

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        ResetShape();
    }

    private void OnSceneChanged(Scene before, Scene after) => ResetShape();

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) ResetShape();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) ResetShape();
    }

    private void LateUpdate()
    {
        if (!CanShowWorldHint)
        {
            ResetShape();
            return;
        }

        bool held = Input.GetMouseButton(0);
        bool pressed = Input.GetMouseButtonDown(0);
        var eventSystem = EventSystem.current;
        if (held && worldGrabFrame == Time.frameCount)
        {
            Show(Shape.Grab);
            return;
        }

        if (held && heldEventSystem == eventSystem && IsEnabledTarget(heldTarget))
        {
            Show(Shape.Grab);
            return;
        }

        bool endedGrab = shape == Shape.Grab;
        heldTarget = null;
        heldEventSystem = null;
        Vector2 position = Input.mousePosition;
        bool worldPoint = worldPointFrame == Time.frameCount;
        bool moved = position != lastPosition;
        if (!pressed && !endedGrab && !moved && worldPoint == lastWorldPoint && Time.unscaledTime < nextIdleRaycast)
            return;

        // Still sample at rest so a disabled button or a popup can change the cursor under a stationary mouse.
        nextIdleRaycast = Time.unscaledTime + .04f;
        lastPosition = position;
        lastWorldPoint = worldPoint;
        bool pointsAtControl = ResolveTarget(eventSystem, position, out GameObject dragTarget);
        if (pressed && dragTarget != null)
        {
            heldTarget = dragTarget;
            heldEventSystem = eventSystem;
            Show(Shape.Grab);
            return;
        }

        Show(pointsAtControl || (hits.Count == 0 && worldPoint) ? Shape.Point : Shape.Arrow);
    }

    internal bool ResolveTarget(EventSystem eventSystem, Vector2 position, out GameObject dragTarget)
    {
        dragTarget = null;
        hits.Clear();
        if (eventSystem == null || !eventSystem.isActiveAndEnabled) return false;
        if (raycastEventSystem != eventSystem)
        {
            raycastEventSystem = eventSystem;
            pointer = new PointerEventData(eventSystem) { pointerId = -1, button = PointerEventData.InputButton.Left };
        }
        pointer.position = position;
        eventSystem.RaycastAll(pointer, hits);
        if (hits.Count == 0) return false;

        var hit = hits[0].gameObject;
        if (!IsEnabledTarget(hit) || hit.GetComponentInParent<InputField>() != null) return false;

        var drag = ExecuteEvents.GetEventHandler<IDragHandler>(hit);
        // A scroll view's blank background is not a workshop object to grab.
        if (IsEnabledTarget(drag) && drag.GetComponent<ScrollRect>() == null &&
            HasHandler<IDragHandler>(drag, EventTriggerType.BeginDrag, EventTriggerType.Drag))
        {
            dragTarget = drag;
            return true;
        }

        var click = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
        if (IsEnabledTarget(click) && HasHandler<IPointerClickHandler>(click, EventTriggerType.PointerClick))
            return true;

        var press = ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit);
        return IsEnabledTarget(press) && HasHandler<IPointerDownHandler>(press, EventTriggerType.PointerDown);
    }

    private bool HasHandler<T>(GameObject target, EventTriggerType primary, EventTriggerType? secondary = null)
        where T : IEventSystemHandler
    {
        target.GetComponents(handlers);
        foreach (var handler in handlers)
        {
            if (!(handler is T) || !handler.isActiveAndEnabled) continue;
            if (!(handler is EventTrigger trigger)) return true;
            // EventTrigger advertises every interface, including events that were never registered.
            foreach (var entry in trigger.triggers)
            {
                if (entry.eventID == primary || entry.eventID == secondary) return true;
            }
        }
        return false;
    }

    private static bool IsEnabledTarget(GameObject target)
    {
        if (target == null || !target.activeInHierarchy) return false;
        var control = target.GetComponentInParent<Selectable>();
        return control == null || (control.isActiveAndEnabled && control.IsInteractable());
    }

    private void Show(Shape next)
    {
        if (shape == next) return;
        shape = next;
        var texture = next == Shape.Point ? pointTexture : next == Shape.Grab ? grabTexture : null;
        var hotspot = next == Shape.Point ? new Vector2(11f, 2f) : next == Shape.Grab ? new Vector2(15f, 13f) : Vector2.zero;
        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
    }

    private void ResetShape()
    {
        heldTarget = null;
        heldEventSystem = null;
        nextIdleRaycast = 0f;
        worldPointFrame = worldGrabFrame = -1;
        Show(Shape.Arrow);
    }
}
