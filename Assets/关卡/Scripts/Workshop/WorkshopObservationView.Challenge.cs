using UnityEngine;
using UnityEngine.UI;

internal sealed partial class WorkshopObservationView
{
    private enum ChallengePhase
    {
        None,
        Introduction,
        Playing,
        Complete
    }

    private ChallengePhase challengePhase;
    private bool ChallengeIsActive => challengePhase != ChallengePhase.None;

    partial void ResetChallengePlayState();

    private void PrepareActivity(WorkshopObservation id)
    {
        observation = id;
        showSongJin = false;
        showBatches = false;
        challengePhase = !readOnly && IsSeasonGame(id) ? ChallengePhase.Introduction : ChallengePhase.None;
        stage = readOnly ? CompletionStage(id) : 0;
    }

    private void DrawChallengeIntroduction(Definition definition)
    {
        string preview = "WorkshopObservationCards/" + definition.Season + "_ObservationCard";
        Art("Challenge Preview", preview, ArtBounds);
        Copy("过关目标", definition.ChallengeGoal);
        Act("Challenge Start", "开始小游戏", BeginChallenge, true);
    }

    private void DrawChallengeStatus(Color accent)
    {
        string element = "Challenge Introduction";
        string label = "节气小游戏";
        if (challengePhase == ChallengePhase.Playing)
        {
            element = "Challenge Playing";
            label = "挑战进行中";
        }
        else if (challengePhase == ChallengePhase.Complete)
        {
            element = "Challenge Completed";
            label = "过关 · 已记入织录";
        }

        WorkshopUi.Label(page, element, new Rect(1294, 71, 390, 58), label, 31, accent, TextAnchor.MiddleRight);
    }

    private Button DrawChallengeReplay()
    {
        var button = WorkshopUi.PixelButton(page, "Challenge Replay", new Rect(1218, 848, 237, 58),
            "再玩一次", () =>
            {
                WorkshopAudioFeedback.Click();
                BeginChallenge();
            }, true, 27, Accents[(int)Season(observation)]);
        RegisterButton(button);
        return button;
    }

    private void BeginChallenge()
    {
        if (challengePhase != ChallengePhase.Introduction && challengePhase != ChallengePhase.Complete) return;
        challengePhase = ChallengePhase.Playing;
        showSongJin = false;
        showBatches = false;
        ResetChallengePlayState();
        Step(0);
    }
}
