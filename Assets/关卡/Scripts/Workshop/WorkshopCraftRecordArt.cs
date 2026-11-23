using UnityEngine;

internal sealed class WorkshopCraftRecordArt : ScriptableObject
{
    [SerializeField] private Sprite[] chapterCompletion;
    [SerializeField] private Sprite greigeSilk;

    internal Sprite ChapterCover(WorkshopSeason season) =>
        chapterCompletion != null && (int)season < chapterCompletion.Length
            ? chapterCompletion[(int)season]
            : null;

    internal Sprite GreigeSilk => greigeSilk;
}
