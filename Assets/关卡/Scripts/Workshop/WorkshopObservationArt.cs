using UnityEngine;

internal sealed class WorkshopObservationArt : ScriptableObject
{
    private static WorkshopObservationArt current;
    internal static WorkshopObservationArt Current =>
        current != null ? current : (current = Resources.Load<WorkshopObservationArt>("WorkshopObservationArt"));

    [SerializeField] private Sprite leaf;
    [SerializeField] private Sprite silkwormTray;
    [SerializeField] private Sprite cooker;
    [SerializeField] private Sprite silkReel;
    [SerializeField] private Sprite loom;
    [SerializeField] private Sprite shuttle;
    [SerializeField] private Sprite dryDye;
    [SerializeField] private Sprite dyedSwatch;
    [SerializeField] private Sprite offeringWine;

    internal Sprite Leaf => leaf;
    internal Sprite SilkwormTray => silkwormTray;
    internal Sprite Cooker => cooker;
    internal Sprite SilkReel => silkReel;
    internal Sprite Loom => loom;
    internal Sprite Shuttle => shuttle;
    internal Sprite DryDye => dryDye;
    internal Sprite DyedSwatch => dyedSwatch;
    internal Sprite OfferingWine => offeringWine;
}
