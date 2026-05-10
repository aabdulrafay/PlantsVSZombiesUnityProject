using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuListener : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string mainSceneName = "MainScene";

    public void OnQuitClick()
    {
        Application.Quit();
    }

    public void OnPlayClick()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnStageClick(string levelName)
    {
        string sceneToLoad = string.IsNullOrWhiteSpace(levelName) ? gameSceneName : levelName;
        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            sceneToLoad = gameSceneName;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            sceneToLoad = mainSceneName;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}
