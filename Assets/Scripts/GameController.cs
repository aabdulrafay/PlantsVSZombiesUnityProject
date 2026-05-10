using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum ZombieType
{
    Zombie1,
    Zombie2,
    FlagZombie,
    ConeHeadZombie,
    BucketHeadZombie
}

[Serializable]
public struct Wave
{
    [Serializable]
    public struct Data
    {
        public ZombieType zombieType;
        public uint count;
    }

    public bool isLargeWave;
    [Range(0f, 1f)]
    public float percentage;
    public Data[] zombieData;
}

public class GameController : MonoBehaviour
{
    [SerializeField] private string defaultNextStage = "MainScene";

    public GameObject zombie1;
    private GameModel model;
    public AudioClip readySound;
    public AudioClip zombieComing;
    public AudioClip hugeWaveSound;
    public AudioClip finalWaveSound;
    public AudioClip lostMusic;
    public AudioClip winMusic;

    public string nextStage;
    public GameObject progressBar;
    public GameObject gameLabel;
    public GameObject sunPrefab;
    public GameObject cardDialog;
    public GameObject sunLabel;
    public GameObject shovelBG;
    public GameObject BtnSubmitObj;
    
    public GameObject resultBannerObj;
    public Sprite winSprite;
    public Sprite loseSprite;
    public bool skipCardSelectionUI = false;

    public float readyTime;
    public float elapsedTime;
    public float playTime;
    public float sunInterval;
    public Wave[] waves;
    public int initSun;
    public int minTotalZombies = 15; // Total zombies across all waves (3 waves x 5 = 15)
    public float spawnInterval = 15f; // seconds between individual zombie spawns (INCREASED FOR MORE GAP)
    public float gapBetweenWaves = 10f; // gap after each wave finishes (INCREASED FOR MORE GAP)
    private bool isLostGame = false;
    private bool hasStartedGameplay = false;

    void Awake()
    {
        model = GameModel.GetInstance();
        HideResultBanner();
    }

	// Use this for initialization
	void Start ()
	{
    // Prevent accidental simultaneous spawning from zero/negative inspector values.
    if (spawnInterval < 0.1f) spawnInterval = 0.1f;
    if (gapBetweenWaves < 0f) gapBetweenWaves = 0f;

        model.Clear();
	    model.sun = initSun;
        ArrayList flags = new ArrayList();
	    for (int i = 0; i < waves.Length; i++)
	    {
	        if (waves[i].isLargeWave || i + 1 == waves.Length)
	        {
	            flags.Add(waves[i].percentage);
	        }
	    }
        // Ensure there will be at least minTotalZombies across all waves
        int totalPlanned = 0;
        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i].zombieData != null)
            {
                for (int j = 0; j < waves[i].zombieData.Length; j++)
                {
                    totalPlanned += (int)waves[i].zombieData[j].count;
                }
            }
        }
        if (totalPlanned < minTotalZombies && waves.Length > 0)
        {
            int need = minTotalZombies - totalPlanned;
            int last = waves.Length - 1;
            if (waves[last].zombieData == null || waves[last].zombieData.Length == 0)
            {
                Wave.Data d = new Wave.Data();
                d.zombieType = ZombieType.Zombie1;
                d.count = (uint)need;
                waves[last].zombieData = new Wave.Data[1] { d };
            }
            else
            {
                // add to first entry of last wave
                var arr = waves[last].zombieData;
                arr[0].count = arr[0].count + (uint)need;
                waves[last].zombieData = arr;
            }
            Debug.Log("GameController: increased zombies by " + need + " to meet minTotalZombies=" + minTotalZombies);
        }
        if (progressBar != null)
        {
            progressBar.GetComponent<ProgressBar>().InitWithFlag((float[])flags.ToArray(typeof(float)));
            progressBar.SetActive(false);
        }
        if (cardDialog != null) cardDialog.SetActive(false);
        if (sunLabel != null) sunLabel.SetActive(false);
        if (shovelBG != null) shovelBG.SetActive(false);
        if (BtnSubmitObj != null) BtnSubmitObj.SetActive(false);
	    GetComponent<HandlerForShovel>().enabled = false;
	    GetComponent<HandlerForPlants>().enabled = false;
	    StartCoroutine(GameReady());
    }
    
    Vector3 origin
    {
        get { return new Vector3(-2, -2.6f); }
    }

    void OnDrawGizmos()
    {
        //DebugDrawGrid(origin,0.8f,1,9,5,Color.blue);
    }

    void DebugDrawGrid(Vector3 origin, float x, float y, int col, int row, Color color)
    {
        for (int i = 0; i < col+1; i++)
        {
            Vector3 startPoint = origin + Vector3.right * i * x;
            Vector3 endPoint = startPoint + Vector3.up * row * y;
            Debug.DrawLine(startPoint,endPoint,color);
        }
        for (int i = 0; i < row + 1; i++)
        {
            Vector3 startPoint = origin + Vector3.up * i * y;
            Vector3 endPoint = startPoint + Vector3.right * col * x;
            Debug.DrawLine(startPoint, endPoint, color);
        }
    }

    public void AfterSelectedCard()
    {
        if (hasStartedGameplay)
        {
            return;
        }
        hasStartedGameplay = true;
        
        if (BtnSubmitObj != null) BtnSubmitObj.SetActive(false);
        if (cardDialog != null) Destroy(cardDialog);
        GetComponent<HandlerForShovel>().enabled = true;
        GetComponent<HandlerForPlants>().enabled = true;
        Camera.main.transform.position = new Vector3(1.1f,0,-1);
        StartCoroutine(WorkFlow());
        //InvokeRepeating("ProduceSun", sunInterval, sunInterval);
        InvokeRepeating("ProduceSun", 5f, 15f);

    }
    
    IEnumerator GameReady()
    {
        yield return new WaitForSeconds(0.5f);
        MoveBy move = Camera.main.gameObject.AddComponent<MoveBy>();
        move.offset = new Vector3(3.05f, 0, 0);
        move.time = 1;
        move.Begin();
        yield return new WaitForSeconds(1.5f);
        if (sunLabel != null) sunLabel.SetActive(true);
        if (shovelBG != null) shovelBG.SetActive(true);

        if (!skipCardSelectionUI)
        {
            if (cardDialog != null) cardDialog.SetActive(true);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            model.sun += 50;
        }
        if (!isLostGame)
        {
            for (int row = 0; row < model.zombieList.Length; row++)
            {
                foreach (GameObject zombie in model.zombieList[row])
                {
                    if (zombie.transform.position.x < StageMap.GRID_LEFT - 0.4f)
                    {
                        LostGame();
                        isLostGame = true;
                        return;
                    }
                }
            }
        }
    }

    IEnumerator WorkFlow()
    {
        gameLabel.GetComponent<GameTips>().ShowStartTip();
        AudioManager.GetInstance().PlaySound(readySound);

        yield return new WaitForSeconds(readyTime);
        ShowProgressBar();
        AudioManager.GetInstance().PlaySound(zombieComing);
        for (int i = 0; i < waves.Length; i++)
        {
            yield return StartCoroutine(WaitForWavePercentage(waves[i].percentage));
            if (waves[i].isLargeWave)
            {
                yield return new WaitForSeconds(0.3f);
                gameLabel.GetComponent<GameTips>().ShowApproachingTip();
                AudioManager.GetInstance().PlaySound(hugeWaveSound);
                yield return new WaitForSeconds(3);
            }
            if (i + 1 == waves.Length)
            {
                AudioManager.GetInstance().PlaySound(finalWaveSound);
            }
            yield return StartCoroutine(CreateZombies(waves[i]));
            if (gapBetweenWaves > 0f)
                yield return new WaitForSeconds(gapBetweenWaves);
        }
        yield return StartCoroutine(WaitForZombieClear());
        yield return new WaitForSeconds(2);
        WinGame();
    }

    IEnumerator WaitForZombieClear()
    {
        while (true)
        {
            bool hasZombie = false;
            for (int row = 0; row < StageMap.ROW_MAX; row++)
            {
                if (model.zombieList[row].Count != 0)
                {
                    hasZombie = true;
                    break;
                }
            }
            if (hasZombie)
            {
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                break;
            }
        }
    }

    IEnumerator WaitForWavePercentage(float percentage)
    {
        while (true)
        {
            if (elapsedTime / playTime >= percentage)
            {
                break;
            }
            else
            {
                yield return 0;
            }
        }
    }

    IEnumerator UpdateProgress()
    {
        while (true)
        {
            elapsedTime += Time.deltaTime;
            progressBar.GetComponent<ProgressBar>().SetProgress(elapsedTime/playTime);
            yield return 0;
        }
    }

    void ShowProgressBar()
    {
        progressBar.SetActive(true);
        StartCoroutine(UpdateProgress());
    }

    // Start spawning zombies over time for the given wave and wait until finished
    IEnumerator CreateZombies(Wave wave)
    {
        yield return StartCoroutine(CreateZombiesCoroutine(wave));
    }

    IEnumerator CreateZombiesCoroutine(Wave wave)
    {
        if (wave.zombieData == null)
            yield break;

        float interval = Mathf.Max(0.1f, spawnInterval);

        foreach (Wave.Data data in wave.zombieData)
        {
            for (int i = 0; i < data.count; i++)
            {
                CreateOneZombie(data.zombieType);
                if (i < data.count - 1)
                    yield return new WaitForSeconds(interval);
            }

            // Keep spacing between different zombie types in the same wave too.
            yield return new WaitForSeconds(interval);
        }
    }
    
    void CreateOneZombie(ZombieType type)
    {
        GameObject zombie = null;
        switch (type)
        {
            case ZombieType.Zombie1:
                zombie = Instantiate(zombie1);
                break;
            //case ZombieType.Zombie2:
            //    break;
            //case ZombieType.FlagZombie:
            //    break;
            //case ZombieType.ConeHeadZombie:
            //    break;
            //case ZombieType.BucketHeadZombie:
            //    break;
        }
        int row = Random.Range(0, StageMap.ROW_MAX);
        zombie.transform.position = StageMap.SetZombiePos(row);
        zombie.GetComponent<ZombieMove>().row = row;
        zombie.GetComponent<SpriteDisplay>().SetOrderByRow(row);
        model.zombieList[row].Add(zombie);
    }

    void ProduceSun()
    {
        float x = Random.Range(StageMap.GRID_LEFT, StageMap.GRID_RIGHT);
        float y = Random.Range(StageMap.GRID_BOTTOM, StageMap.GRID_TOP);
        float startY = StageMap.GRID_TOP + 1.5f;
        GameObject sun = Instantiate(sunPrefab);
        sun.transform.position = new Vector3(x, startY, 0);
        MoveBy move = sun.AddComponent<MoveBy>();
        move.offset = new Vector3(0, y-startY, 0);
        move.time = (startY - y) / 1f;
        move.Begin();
    }

    void LostGame()
    {
        gameLabel.GetComponent<GameTips>().ShowLostTip();
        GetComponent<HandlerForPlants>().enabled = false;
        CancelInvoke("ProduceSun");
        AudioManager.GetInstance().PlayMusic(lostMusic, false);
        // Show lose sprite for 4 seconds then go to main scene
        StartCoroutine(ShowResultThenGotoMain(loseSprite, 4f));
    }

    void WinGame()
    {
        CancelInvoke("ProduceSun");
        AudioManager.GetInstance().PlayMusic(winMusic, false);
        // Show win sprite for 4 seconds then go to main scene
        StartCoroutine(ShowResultThenGotoMain(winSprite, 4f));
    }

    IEnumerator ShowResultThenGotoMain(Sprite sprite, float seconds)
    {
        GameObject temp = null;
        if (sprite != null)
        {
            // Create a Screen Space - Overlay Canvas so the image is always on top
            temp = new GameObject("_ResultOverlay_Canvas");
            var canvas = temp.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            temp.AddComponent<CanvasScaler>();
            temp.AddComponent<GraphicRaycaster>();

            GameObject imgGO = new GameObject("_ResultOverlay_Image");
            imgGO.transform.SetParent(temp.transform, false);
            var img = imgGO.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;

            // Stretch to full screen while preserving aspect
            var rt = imgGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        yield return new WaitForSeconds(seconds);

        if (temp != null)
            Destroy(temp);

        // Always go back to the default main scene
        string target = string.IsNullOrWhiteSpace(defaultNextStage) ? "MainScene" : defaultNextStage;
        if (!Application.CanStreamedLevelBeLoaded(target)) target = "MainScene";
        SceneManager.LoadScene(target);
    }

    void ShowResultBanner(Sprite sprite)
    {
        if (resultBannerObj == null || sprite == null)
        {
            return;
        }

        var spriteRenderer = resultBannerObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            resultBannerObj.SetActive(true);
            return;
        }

        var image = resultBannerObj.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = sprite;
            resultBannerObj.SetActive(true);
        }
    }

    void HideResultBanner()
    {
        if (resultBannerObj != null)
        {
            resultBannerObj.SetActive(false);
        }
    }

    void GotoNextStage()
    {
        string targetScene = string.IsNullOrWhiteSpace(nextStage) ? defaultNextStage : nextStage;
        if (!Application.CanStreamedLevelBeLoaded(targetScene))
        {
            targetScene = defaultNextStage;
        }
        SceneManager.LoadScene(targetScene);
    }

    void RestartStage()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
