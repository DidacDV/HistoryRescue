using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public AudioClip music;

    private void Awake()
    {
        if (!SceneManager.GetSceneByName("Persistent").isLoaded)
            SceneManager.LoadScene("Persistent", LoadSceneMode.Additive);
    }

    void Start()
    {
        if (music != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.Play(music);
        }
    }

    public void StartGame() {
        Debug.Log("Starting game...");

        if (GameManager.Instance == null)
        {
            //load persistent scene first if GameManager doesn't exist
            SceneManager.LoadScene("Persistent", LoadSceneMode.Additive);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Stop();
        }
        //start the first level
        GameManager.Instance.StartGame();
    }

    public void QuitGame() {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}