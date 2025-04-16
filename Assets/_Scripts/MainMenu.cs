using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Load a level
    public void LoadLevel(int sceneID)
    {
        SceneManager.LoadScene(sceneID);
    }
}
