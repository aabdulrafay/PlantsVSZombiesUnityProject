using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuListener : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private string menuMusicResource = "Audio/Music/bgm2";

    void Start()
    {
        if (SceneManager.GetActiveScene().name != mainSceneName)
        {
            return;
        }

        AudioClip menuMusic = Resources.Load<AudioClip>(menuMusicResource);
        if (menuMusic != null)
        {
            AudioManager.GetInstance().PlayMusic(menuMusic);
        }
    }

    public void OnQuitClick()
    {
        Application.Quit();
    }

    public void OnPlayClick()
    {
        AudioManager.GetInstance().StopMusic();
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

        if (sceneToLoad != mainSceneName)
        {
            AudioManager.GetInstance().StopMusic();
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}
