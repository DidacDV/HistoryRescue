using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "NewLevelTheme", menuName = "Game/Level Theme")]
public class LevelTheme : ScriptableObject
{
    public string themeName;
    public TMP_FontAsset themeFont;
    public Color backgroundColor;
    public Sprite backgroundImage;
    public Sprite UIContainerImage;
}