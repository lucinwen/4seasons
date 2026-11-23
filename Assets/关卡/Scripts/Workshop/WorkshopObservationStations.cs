using System.Collections;
using System.Collections.Generic;
using Qingming.LevelBackground;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Secondary observations beside the original seasonal workstations.</summary>
internal sealed class WorkshopObservationStations : MonoBehaviour
{
    private sealed class Station
    {
        internal Component Source;
        internal FloatingActionButton Button;
        internal WorkshopObservation Observation;
    }

    private readonly List<Station> stations = new List<Station>();
    private WorkshopSeason season;
    private LevelPlayerController player;
    private Station nearby;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= Attach;
        SceneManager.sceneLoaded += Attach;
        Attach(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void Attach(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || !WorkshopProgress.TryGetSeason(scene.name, out var season))
            return;

        foreach (var binding in FindObjectsOfType<WorkshopObservationStations>())
            if (binding.gameObject.scene == scene)
                return;

        var root = new GameObject("Workshop Observation Stations");
        if (root.scene != scene)
            SceneManager.MoveGameObjectToScene(root, scene);
        root.AddComponent<WorkshopObservationStations>().season = season;
    }

    private IEnumerator Start()
    {
        // Autumn and winter install several of their workstations from other Start methods.
        yield return null;

        switch (season)
        {
            case WorkshopSeason.Spring:
                Register<SpringPrompt>(WorkshopObservation.QingmingMulberry, 4.18f);
                Register<SilkwormRackTrigger>(WorkshopObservation.GuyuLeaf, 3.25f);
                break;
            case WorkshopSeason.Summer:
                Register<SummerCocoonRackTrigger>(WorkshopObservation.MangzhongCocoon, 3.25f);
                Register<SummerCookerTrigger>(WorkshopObservation.DashuReeling, 2.75f);
                break;
            case WorkshopSeason.Autumn:
                Register<AutumnRitualTrigger>(WorkshopObservation.QiufenOffering, 2.85f);
                Register<AutumnLoomTrigger>(WorkshopObservation.HanluLoom, 3.25f);
                break;
            case WorkshopSeason.Winter:
                Register<WinterGrindingClickTarget>(WorkshopObservation.LidongDye, 2.5f);
                Register<WinterDyeVatClickTarget>(WorkshopObservation.LidongDye, 2.5f);
                Register<WinterClothCuttingTrigger>(WorkshopObservation.XiaoxueTailoring, 3.05f);
                break;
        }
    }

    private void Register<T>(WorkshopObservation observation, float height) where T : Component
    {
        foreach (var source in FindObjectsOfType<T>())
        {
            if (source.gameObject.scene != gameObject.scene) continue;

            var marker = new GameObject("Observation Entry " + observation);
            marker.transform.SetParent(transform, false);
            marker.transform.position = source.transform.position;
            var station = new Station
            {
                Source = source,
                Observation = observation,
                Button = marker.AddComponent<FloatingActionButton>()
            };
            string challenge = WorkshopObservationView.SeasonGameTitle(observation);
            station.Button.Configure((challenge ?? WorkshopObservationView.StationLabel(observation)) + " · O",
                new Vector3(0f, height, 0f),
                () => Open(station), FloatingActionButton.VisualStyle.PaperTag);
            station.Button.SetOpacity(0.97f);
            stations.Add(station);
        }
    }

    private bool Available()
    {
        if (Time.timeScale <= 0f || WorkshopQuestGuide.BlocksWorldInput || WorkshopQuestGuide.IsWorldOverlayActive)
            return false;
        return season != WorkshopSeason.Spring ||
            !SilkwormWashingController.washingStarted || SilkwormWashingController.washingCompleted;
    }

    private void Update()
    {
        if (stations.Count == 0) return;
        if (player == null)
            player = FindObjectOfType<LevelPlayerController>();

        ShowStation(Available() && player != null ? FindNearbyStation() : null);
        if (nearby != null && Input.GetKeyDown(KeyCode.O))
            Open(nearby);
    }

    private Station FindNearbyStation()
    {
        Station best = null;
        Vector2 playerPosition = player.transform.position;
        float bestDistance = float.PositiveInfinity;
        foreach (var station in stations)
        {
            if (station.Source == null || !station.Source.gameObject.activeInHierarchy) continue;

            Vector2 stationPosition = station.Source.transform.position;
            var delta = playerPosition - stationPosition;
            if (Mathf.Abs(delta.x) > 2.7f || Mathf.Abs(delta.y) > 3.7f) continue;
            if (delta.sqrMagnitude >= bestDistance) continue;

            best = station;
            bestDistance = delta.sqrMagnitude;
        }
        return best;
    }

    private void ShowStation(Station next)
    {
        if (nearby == next) return;
        if (nearby != null)
            nearby.Button.Hide();
        nearby = next;
        if (nearby != null)
            nearby.Button.Show();
    }

    private void Open(Station station)
    {
        if (nearby != station || !Available()) return;
        WorkshopObservationView.Show(station.Observation);
        ShowStation(null);
    }

    private void OnDisable() => ShowStation(null);
}
