using System;
using System.Collections.Generic;
using JiangnanDye;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum WorkshopSeason { Spring, Summer, Autumn, Winter }

public enum WorkshopItem
{
    MulberryLeaf, SilkwormEgg, Silkworm, Salt, Suoxu, Spool,
    Shuttle, RawSilkCloth, Statue, DyedCloth, CutPieces, Needle
}

public static class WorkshopProgress
{
    public const string HubScene = "FourSeasonsWorkshop";
    private const string SaveKey = "ShiXu.Workshop.v1";

    private static readonly string[] Scenes = { "Level1_Spring", "Level2_Summer", "Level3_Autumn", "Level4_Winter" };
    private static readonly string[] Seasons = { "春", "夏", "秋", "冬" };
    private static readonly string[] Products = { "蚕茧", "生丝", "素绸", "成衣" };
    private static readonly string[] Names =
    {
        "桑叶", "蚕种", "蚕宝宝", "盐", "索绪", "线轴",
        "梭子", "生坯绸", "嫘祖雕像", "染布", "衣片", "缝衣针"
    };
    private static readonly WorkshopItem[][] Recipes =
    {
        new[] { WorkshopItem.MulberryLeaf, WorkshopItem.SilkwormEgg, WorkshopItem.Silkworm },
        new[] { WorkshopItem.Salt, WorkshopItem.Suoxu, WorkshopItem.Spool },
        new[] { WorkshopItem.Shuttle, WorkshopItem.RawSilkCloth, WorkshopItem.Statue },
        new[] { WorkshopItem.DyedCloth, WorkshopItem.CutPieces, WorkshopItem.Needle }
    };

    [Serializable]
    private sealed class SaveData
    {
        public int items;
        public int chapters;
        public int solarTerms;
        public int rawDyes;
        public int healthyDyes;
        public int groundDyes;
        public string dyedColor;
        public string garmentId;
        public List<string> steps = new List<string>();
    }

    private static SaveData save;
    private static WorkshopItem? nextSceneCard;
    public static event Action Changed;

    private static SaveData State
    {
        get
        {
            if (save != null) return save;
            string stored = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(stored))
            {
                try { save = JsonUtility.FromJson<SaveData>(stored); }
                catch (ArgumentException) { Debug.LogWarning("无法读取四时工坊存档，将使用初始进度。"); }
            }
            if (save == null) save = new SaveData();
            if (save.steps == null) save.steps = new List<string>();
            return save;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        SceneManager.sceneLoaded -= ShowQueuedCard;
        nextSceneCard = null;
        save = null;
        Changed = null;
    }

    public static bool HasSavedGame => !string.IsNullOrEmpty(PlayerPrefs.GetString(SaveKey, ""));

    internal static void StartNewGame()
    {
        SceneManager.sceneLoaded -= ShowQueuedCard;
        nextSceneCard = null;
        save = new SaveData();
        WinterLevelState.PendingGrindingPrompt = false;
        WinterLevelState.HasReturnPosition = false;
        Commit();
    }

    public static string SceneName(WorkshopSeason season) => Scenes[(int)season];
    public static string SeasonName(WorkshopSeason season) => Seasons[(int)season];
    public static string ProductName(WorkshopSeason season) => Products[(int)season];
    public static string ItemName(WorkshopItem item) => Names[(int)item];
    public static WorkshopItem[] Items(WorkshopSeason season) => (WorkshopItem[])Recipes[(int)season].Clone();

    public static bool TryGetSeason(string sceneName, out WorkshopSeason season)
    {
        int index = Array.IndexOf(Scenes, sceneName);
        season = index >= 0 ? (WorkshopSeason)index : WorkshopSeason.Spring;
        return index >= 0;
    }

    public static bool Has(WorkshopItem item) => (State.items & (1 << (int)item)) != 0;
    public static bool IsComplete(WorkshopSeason season) => (State.chapters & (1 << (int)season)) != 0;
    public static bool IsUnlocked(WorkshopSeason season) => season == WorkshopSeason.Spring || IsComplete(season - 1);
    public static bool HasSolarTerms(WorkshopSeason season) => (State.solarTerms & (1 << (int)season)) != 0;
    public static bool HasStep(string id) => State.steps.Contains(id);
    public static string DyedColor => State.dyedColor ?? "";
    public static string GarmentId => State.garmentId ?? "";

    public static bool Award(WorkshopItem item)
    {
        if (Has(item)) return false;
        State.items |= 1 << (int)item;
        Commit();
        WorkshopAudioFeedback.Collect();
        return true;
    }

    public static void UnlockSolarTerms(WorkshopSeason season)
    {
        if (HasSolarTerms(season)) return;
        State.solarTerms |= 1 << (int)season;
        Commit();
        WorkshopAudioFeedback.Collect();
    }

    public static void MarkStep(string id)
    {
        if (string.IsNullOrEmpty(id) || HasStep(id)) return;
        State.steps.Add(id);
        Commit();
        WorkshopAudioFeedback.Progress();
    }

    public static bool CanSubmit(WorkshopSeason season)
    {
        if (!IsUnlocked(season) || IsComplete(season)) return false;
        return Array.TrueForAll(Recipes[(int)season], Has);
    }

    public static bool Submit(WorkshopSeason season)
    {
        if (season == WorkshopSeason.Winter || !CanSubmit(season)) return false;
        // Submitted items stay recorded for the journal and completed quest display.
        State.chapters |= 1 << (int)season;
        Commit();
        WorkshopAudioFeedback.Milestone();
        return true;
    }

    public static bool CompleteWinter(string garmentId)
    {
        if (!CanSubmit(WorkshopSeason.Winter) || !WorkshopTailor.IsGarmentAvailable(garmentId, DyedColor)) return false;

        State.garmentId = garmentId;
        State.chapters |= 1 << (int)WorkshopSeason.Winter;
        Commit();
        WorkshopAudioFeedback.Milestone();
        return true;
    }

    public static bool RecordDyedCloth(DyeColor dye)
    {
        if (Has(WorkshopItem.CutPieces)) return false;

        bool first = !Has(WorkshopItem.DyedCloth);
        string color = DyeColorUtil.ChineseName(dye);
        if (!first && State.dyedColor == color) return false;

        State.dyedColor = color;
        State.items |= 1 << (int)WorkshopItem.DyedCloth;
        Commit();
        WorkshopAudioFeedback.Collect();
        return first;
    }

    public static void RecordRawDyes(IEnumerable<ItemData> items)
    {
        if (items == null) return;
        int available = State.rawDyes;
        int healthy = State.healthyDyes;
        foreach (var item in items)
        {
            if (item == null) continue;
            int bit = 1 << (int)item.dyeColor;
            available |= bit;
            if (item.condition == DyeCondition.Normal) healthy |= bit;
        }

        if (available == State.rawDyes && healthy == State.healthyDyes) return;
        State.rawDyes = available;
        State.healthyDyes = healthy;
        if (!HasStep("winter.dyes")) State.steps.Add("winter.dyes");
        Commit();
        WorkshopAudioFeedback.Collect();
    }

    public static bool IsDyeGround(DyeColor dye) => (State.groundDyes & (1 << (int)dye)) != 0;

    public static ItemData PendingDye(DyeColor dye)
    {
        int bit = 1 << (int)dye;
        if ((State.rawDyes & bit) == 0 || IsDyeGround(dye)) return null;
        return WorkshopArt.RawDye(dye, (State.healthyDyes & bit) != 0);
    }

    public static void RecordGroundDye(DyeColor dye)
    {
        if (IsDyeGround(dye)) return;
        State.groundDyes |= 1 << (int)dye;
        if (!HasStep("winter.ground")) State.steps.Add("winter.ground");
        Commit();
        WorkshopAudioFeedback.Progress();
    }

    public static void ViewCardAfterTravel(WorkshopItem item)
    {
        nextSceneCard = item;
        SceneManager.sceneLoaded -= ShowQueuedCard;
        SceneManager.sceneLoaded += ShowQueuedCard;
    }

    private static void ShowQueuedCard(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        SceneManager.sceneLoaded -= ShowQueuedCard;
        if (nextSceneCard.HasValue && TryGetSeason(scene.name, out _))
            WorkshopKnowledgeView.ShowItem(nextSceneCard.Value);
        nextSceneCard = null;
    }

    private static void Commit()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(State));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
