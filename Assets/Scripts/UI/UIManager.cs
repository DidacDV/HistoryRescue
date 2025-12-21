using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Subtitle Settings")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private GameObject subtitlePanel;
    [SerializeField] private float floatAmplitude = 10f; 
    [SerializeField] private float floatSpeed = 2f;    
    private RectTransform subtitleRectTransform;
    private Vector2 originalSubtitlePosition;

    [SerializeField] private TextMeshProUGUI movementCountValueText;
    [SerializeField] private TextMeshProUGUI movementCountLabel;

    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private TextMeshProUGUI levelValue;
    private int movementCount = 0;

    [Header("Difficulty Settings")]
    [SerializeField] private Sprite[] difficultySprites;

    [SerializeField] private Canvas UICanvas;

    [Header("Theme options")]
    [SerializeField] private Image UIPanel;
    [SerializeField] private Image UIPanelLogo;

    [SerializeField] private GameObject PauseUI;

    private Coroutine subtitleCoroutine;
    private Coroutine floatCoroutine;

    public void ShowAutoSubtitles(string fullText, float duration)
    {
        if (subtitleCoroutine != null) StopCoroutine(subtitleCoroutine);
        if (floatCoroutine != null) StopCoroutine(floatCoroutine);

        subtitleCoroutine = StartCoroutine(AnimateSubtitles(fullText, duration));
        floatCoroutine = StartCoroutine(IdleFloatAnimation()); 
    }

    private IEnumerator AnimateSubtitles(string text, float totalDuration)
    {
        CanvasGroup canvasGroup = subtitlePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = subtitlePanel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;
        subtitleRectTransform.anchoredPosition = originalSubtitlePosition;
        subtitlePanel.SetActive(true);

        float timePerChar = totalDuration / text.Length;
        string[] words = text.Split(' ');
        List<string> fragments = new List<string>();
        string currentLine = "";

        foreach (string word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            subtitleText.text = testLine;
            subtitleText.ForceMeshUpdate();

            if (subtitleText.preferredWidth > subtitleText.rectTransform.rect.width)
            {
                fragments.Add(currentLine);
                currentLine = word;
            }
            else currentLine = testLine;
        }
        fragments.Add(currentLine);

        foreach (string fragment in fragments)
        {
            subtitleText.text = "";
            foreach (char c in fragment)
            {
                subtitleText.text += c;
                yield return new WaitForSeconds(timePerChar);
            }
        }

        yield return new WaitForSeconds(1f);

        // 2. Fade Downwards Animation
        if (floatCoroutine != null) StopCoroutine(floatCoroutine); 

        float fadeDuration = 0.5f;
        float timer = 0f;
        Vector2 startPos = subtitleRectTransform.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, -40f); 

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            subtitleRectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            yield return null;
        }

        subtitleText.text = "";
        subtitlePanel.SetActive(false);
        canvasGroup.alpha = 1f;
        subtitleRectTransform.anchoredPosition = originalSubtitlePosition;
    }

    private IEnumerator IdleFloatAnimation()
    {
        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime * floatSpeed;
            float newY = originalSubtitlePosition.y + (Mathf.Sin(timer) * floatAmplitude);
            subtitleRectTransform.anchoredPosition = new Vector2(originalSubtitlePosition.x, newY);
            yield return null;
        }
    }

    void Awake()
    {
        //singleton pattern 
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        if (subtitlePanel != null)
        {
            subtitleRectTransform = subtitlePanel.GetComponent<RectTransform>();
            originalSubtitlePosition = subtitleRectTransform.anchoredPosition;
        }


        UpdateMovementText();

        // Disable UI if starting in MainMenu
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            DisableUI();
        }
    }


    public void InitUIVars()
    {
        Debug.Log("reseting UI");
        ResetMovementCount();
        PauseUI.SetActive(false);
    }


    #region Movement Count
    public void IncrementMovement()
    {
        movementCount++;
        UpdateMovementText();
    }

    public void ResetMovementCount()
    {
        movementCount = 0;
        UpdateMovementText();
    }

    private void UpdateMovementText()
    {
        movementCountValueText.text = movementCount.ToString();
    }

    public int GetMovementCount()
    {
        return movementCount;
    }

    public void SetMovementCount(int count)
    {
        movementCount = count;
        UpdateMovementText();
    }
    #endregion

    #region theme

    public void ApplyTheme(LevelTheme theme)
    {
        if (theme.themeFont != null)
        {
            movementCountLabel.font = theme.themeFont;
            levelLabel.font = theme.themeFont;
            movementCountValueText.font = theme.themeFont;
            levelValue.font = theme.themeFont;
        }
        if (theme.UIContainerImage != null)
            UIPanel.sprite = theme.UIContainerImage;
        if (theme.UILogoImage != null)
            UIPanelLogo.sprite = theme.UILogoImage;
        if (theme.stageName != null)
            levelValue.text = theme.stageName;
        else levelValue.text = "Floating";

        levelValue.fontSize = theme.fontSize;
        levelLabel.fontSize = theme.fontSize;
        movementCountValueText.fontSize = theme.fontSize;
        movementCountLabel.fontSize = theme.fontSize;
    }

    #endregion

    #region visibility

    public void DisableUI()
    {
        Debug.Log("disabling ui");
        UICanvas.enabled = false;
    }

    public void EnableUI()
    {
        Debug.Log("enabling ui");
        UICanvas.enabled = true;
    }

    #endregion

    #region pause menu

    public void OnResumeGamePress()
    {
        Time.timeScale = 1f;
        PauseUI.SetActive(false);
    }

    public void OnBackToMenuPress()
    {
        Time.timeScale = 1;
        PauseUI.SetActive(false);
        GameManager.Instance.ReturnToMainMenu();
    }

    public void OnEnterPause()
    {
        PauseUI.SetActive(true);
        Time.timeScale = 0;
    }

    #endregion
}