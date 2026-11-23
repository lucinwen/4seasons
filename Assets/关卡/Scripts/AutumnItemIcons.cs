using UnityEngine;

public sealed class AutumnItemIcons : ScriptableObject
{
    public Sprite hammer;
    public Sprite banana;
    public Sprite cake;
    public Sprite shuttle;
    public Sprite statue;

    public static AutumnItemIcons Load()
    {
        return Resources.Load<AutumnItemIcons>("AutumnItemIcons");
    }
}
