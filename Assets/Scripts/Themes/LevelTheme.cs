using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "NewLevelTheme", menuName = "Game/Level Theme")]
public class LevelTheme : ScriptableObject
{
    public string themeName;
    public string stageName;
    public TMP_FontAsset themeFont;
    public float fontSize;
    public float characterSpacing;
    public Color backgroundColor;
    public Sprite UIContainerImage;
    public Sprite UILogoImage;
}