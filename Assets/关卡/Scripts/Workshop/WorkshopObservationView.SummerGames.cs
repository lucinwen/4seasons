using UnityEngine;

internal sealed partial class WorkshopObservationView
{
    private static readonly Color SummerJade = new Color32(55, 105, 91, 255);
    private static readonly Color SummerGold = new Color32(253, 217, 139, 255);
    private static readonly Color SummerDark = new Color32(31, 61, 52, 255);
    private static readonly Color SummerNeutral = new Color32(247, 236, 206, 255);

    private void ResetSummerChallenge()
    {
        if (observation == WorkshopObservation.MangzhongCocoon) ResetXiazhiPuzzle();
    }

    private static WorkshopProgress.ChallengeWork WorkshopUpstreamWork(WorkshopObservation previous, int order)
    {
        var delivered = OrderWork(previous);
        if (!string.IsNullOrEmpty(delivered?.sample)) return delivered;
        var matching = BestWork(previous, order);
        if (!string.IsNullOrEmpty(matching?.sample)) return matching;
        var alternate = BestWork(previous, 1 - order);
        return !string.IsNullOrEmpty(alternate?.sample) ? alternate : matching ?? alternate;
    }

}
