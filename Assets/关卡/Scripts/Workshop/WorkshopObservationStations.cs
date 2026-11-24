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
        if (mode != LoadSceneMode.Single || !WorkshopProgress.TryGetSeason(scene.name, out var season) ||
            season == WorkshopSeason.Spring)
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
            station.Button.Configure(EntryLabel(observation),
                new Vector3(0f, height, 0f),
                () => Open(station), FloatingActionButton.VisualStyle.PaperTag);
            station.Button.SetOpacity(0.97f);
            stations.Add(station);
        }
    }

    private bool Available()
    {
        return Time.timeScale > 0f && !WorkshopQuestGuide.BlocksWorldInput && !WorkshopQuestGuide.IsWorldOverlayActive;
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
        {
            nearby.Button.SetText(EntryLabel(nearby.Observation));
            nearby.Button.Show();
        }
    }

    private static string EntryLabel(WorkshopObservation observation)
    {
        string name = WorkshopObservationView.SeasonGameTitle(observation) ?? WorkshopObservationView.StationLabel(observation);
        string state = WorkshopObservationView.HasRecord(observation) ? " · 已通关" : "";
        return WorkshopObservationView.RecordTerm(observation) + " · " + name + state + " · O";
    }

    private void Open(Station station)
    {
        if (nearby != station || !Available()) return;
        WorkshopObservationView.Show(station.Observation);
        ShowStation(null);
    }

    private void OnDisable() => ShowStation(null);
}
