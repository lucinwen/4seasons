internal sealed partial class WorkshopObservationView
{
    private bool DrawAdditionalSeasonGame()
    {
        if (!ChallengeIsActive) return false;

        switch (observation)
        {
            case WorkshopObservation.MangzhongCocoon: DrawXiazhiPuzzle(); break;
            case WorkshopObservation.MangzhongCocoonHarvest: DrawCocoonHarvestChallenge(); break;
            case WorkshopObservation.LidongDye: DrawWinterDye(); break;
            case WorkshopObservation.DaxueSewing: DrawDaxueSewing(); break;
            case WorkshopObservation.DongzhiFitting: DrawDongzhiFitting(); break;
            default: return false;
        }
        return true;
    }
}
