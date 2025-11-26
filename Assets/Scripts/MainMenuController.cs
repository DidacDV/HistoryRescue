using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour {
    public void StartGame() {
        Debug.Log("Starting game...");

        if (GameManager.Instance == null) {
            //load persistent scene first if GameManager doesn't exist
            SceneManager.LoadScene("Persistent", LoadSceneMode.Additive);
        }

        //start the first level
        SceneManager.LoadScene("Level1Scene");
    }

    public void QuitGame() {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}