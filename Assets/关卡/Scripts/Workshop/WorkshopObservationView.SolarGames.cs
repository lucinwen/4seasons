internal sealed partial class WorkshopObservationView
{
    private bool DrawAdditionalSeasonGame()
    {
        if (!ChallengeIsActive) return false;

        switch (observation)
        {
            case WorkshopObservation.MangzhongCocoon: DrawXiazhiPuzzle(); break;
            case WorkshopObservation.QiufenOffering: DrawOfferingWorkshop(); break;
            case WorkshopObservation.HanluLoom: DrawShuttleWorkshop(); break;
            case WorkshopObservation.LidongDye: DrawWinterDye(); break;
            case WorkshopObservation.XiaoxueTailoring: DrawWinterCutting(); break;
            default: return false;
        }
        return true;
    }
}
