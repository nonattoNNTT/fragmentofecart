using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Tela_1");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}