using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static bool levelStarted, levelFailed, levelPassed, minigameStarted, frontHit, endHit, cheatOn, playMiniGame, reloadLevel, inLoading
        , leftHit, rightHit;

    [SerializeField]
    Camera myCam;

    [SerializeField] Transform spawnFolder, mainPlane, uiFolder;

    [SerializeField] SpriteRenderer gameTitle, gameTitleBG, controlsTitle, controllsTitleBG, whiteSquare;
    [SerializeField] SpriteRenderer minigameTime, minigameTimeBG;

    [SerializeField] Transform[] retryTexts, winTexts;

    bool restartLogic, startLogic, passedLogic, startCounting, fasterLoad, coolDownTime;

    [SerializeField] TextMeshPro currentScore;

    [SerializeField] Animator camShake, scoreAnim;

    [SerializeField] SpriteRenderer[] soundIcons;

    [SerializeField] SoundManagerLogic mySoundManager;

    SpriteRenderer retryTitle, retryBg;
    [SerializeField] GameObject warpParticleObj1, warpParticleObj2, planePropulsionParticles;

    [SerializeField] Transform mainLevel, miniGameLevel;

    float minigameSpawnHieght, mainGameSpawnHieght;

    public static int myScore, perfectHitCounts;
    int currentCheat;

    // Sounds: GameManager.cs, PlayerController.cs
    float finishLinePostion;

    [SerializeField] GameObject pumpkin1, pumpkin2;

    [SerializeField] GameObject[] mainCourseObstacleMenu, miniGameObstacleMenu;
    List<GameObject> spawnedObstacles = new List<GameObject>();
    List<GameObject> spawnedminiGameObstacles = new List<GameObject>();

    [SerializeField] Material pipeMat, obsMat;

    [SerializeField] AudioSource menuMusic, difficultMenuMusic, minigameMusic;

    [SerializeField] AudioSource[] perfectHitSounds;

    [SerializeField] GameObject[] perfectHitsTitles;

    int freeHue;

    private void Awake()
    {
        Application.targetFrameRate = 60;

        levelStarted = false;
        levelFailed = false;
        levelPassed = false;

        restartLogic = false;
        passedLogic = false;
        startLogic = false;

        leftHit = false;
        coolDownTime = false;
        rightHit = false;

        perfectHitCounts = 0;

        minigameStarted = false;
        startCounting = false;

        fasterLoad = false;
        reloadLevel = false;
        inLoading = false;

        cheatOn = false;
        playMiniGame = false;

        myScore = 0;
        minigameSpawnHieght = 0;
        mainGameSpawnHieght = 0;

        pumpkin1.SetActive(false);
        pumpkin2.SetActive(false);
        currentCheat = 0;

        freeHue = 0;

        SpawnLevel();

        currentScore.text = "level " + PlayerPrefs.GetInt("levelCount", 1);
    }

    private void Start()
    {
        StartCoroutine(StartLogic());
        PlayerPrefs.SetInt("GamesSinceLastAdPop", PlayerPrefs.GetInt("GamesSinceLastAdPop", 0)+1);
    }

    private void FixedUpdate()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * 4f);
    }

    IEnumerator PerfectCoolDown()
    {
        yield return new WaitForSeconds(0.5f);
        coolDownTime = false;

    }

    private void Update()
    {
        if (levelStarted)
        {
            freeHue++;
            Color pipeColor = Color.HSVToRGB((freeHue % 360f)/360, 0.36f, 0.50f);
            pipeMat.color = pipeColor;
        }

        if (!levelFailed)
        {
            if (leftHit || rightHit)
            {
                if (!coolDownTime)
                {
                    PerfectHit();
                    coolDownTime = true;
                    StartCoroutine(PerfectCoolDown());
                }
                leftHit = false;
                rightHit = false;
            }
        }

        if (reloadLevel)
        {
            fasterLoad = true;
            inLoading = true;
            StartCoroutine(RestartLevel(whiteSquare));
            reloadLevel = false;
        }

        if (cheatOn && currentCheat == 0) // turn on
        {
            pumpkin1.SetActive(true);
            pumpkin2.SetActive(true);
            PlayerPrefs.SetInt("CheatsEnabled", 1);
            currentCheat = 1;
        }
        else if (!cheatOn && currentCheat == 1) // turn off
        {
            pumpkin1.SetActive(false);
            pumpkin2.SetActive(false);
            PlayerPrefs.SetInt("CheatsEnabled", 0);
            currentCheat = 0;
        }

        if (!restartLogic && levelFailed)
        {
            if (difficultMenuMusic.enabled)
                StartCoroutine(FadeOut(difficultMenuMusic));

            if (minigameMusic.enabled)
                StartCoroutine(FadeOut(minigameMusic));

            PlayerPrefs.SetInt("FailedInARow", PlayerPrefs.GetInt("FailedInARow", 0) + 1); //

            // set high score
            if (myScore > PlayerPrefs.GetInt("highScore", 0))
                PlayerPrefs.SetInt("highScore", myScore);

            mySoundManager.Play("lose_jingle"); // minigame jingle sound
            Transform tempObj = retryTexts[Random.Range(0, retryTexts.Length)].transform;
            retryTitle = tempObj.GetComponent<SpriteRenderer>();
            retryBg = tempObj.GetComponentsInChildren<SpriteRenderer>()[1];

            StartCoroutine(RetryLiterature(retryTitle, retryBg));
            StartCoroutine(RestartWait());
            restartLogic = true;
        }

        if (!startLogic && levelStarted)
        {
            foreach (SpriteRenderer sprite in soundIcons)
            {
                StartCoroutine(FadeImageOut(sprite));
            }
            if (menuMusic.enabled)
                StartCoroutine(FadeOut(menuMusic));                

            StartCoroutine(FadeImageOut(gameTitle));
            StartCoroutine(FadeImageOut(gameTitleBG));
            StartCoroutine(FadeImageOut(controlsTitle));
            StartCoroutine(FadeImageOut(controllsTitleBG));

            startLogic = true;
        }

        if (!passedLogic && levelPassed && !FollowCamera.levelPassedFollow)
        {
            PlayerPrefs.SetInt("FailedInARow", 0); //

            if (difficultMenuMusic.enabled)
                StartCoroutine(FadeOut(difficultMenuMusic));

            mySoundManager.Play("win_jingle"); // minigame jingle sound

            PlayerPrefs.SetInt("SpawnNewLevel", 1);
            Transform tempObj = winTexts[Random.Range(0, winTexts.Length)].transform;
            retryTitle = tempObj.GetComponent<SpriteRenderer>();
            retryBg = tempObj.GetComponentsInChildren<SpriteRenderer>()[1];

            StartCoroutine(RetryLiterature(retryTitle, retryBg));

            if (PlayerPrefs.GetInt("levelCount", 1) % 5 == 0)
                playMiniGame = true;
            else
                StartCoroutine(RestartWait());

            PlayerPrefs.SetInt("levelCount", PlayerPrefs.GetInt("levelCount", 1) + 1);
            passedLogic = true;
        }

        if (passedLogic && FollowCamera.levelPassedFollow && playMiniGame)
        {
            mySoundManager.Play("minigame_jingle"); // minigame jingle sound
            if (difficultMenuMusic.enabled)
                StartCoroutine(FadeIn(minigameMusic));

            SpawnMiniGameObj();

            // move minigame transform
            miniGameLevel.position = new Vector3(miniGameLevel.position.x, miniGameLevel.position.y, mainPlane.position.z);

            retryTitle.enabled = false;
            retryBg.enabled = false;

            // flash "mini game time" on screen
            StartCoroutine(FlashMiniGameTime());
            planePropulsionParticles.transform.localPosition = new Vector3(planePropulsionParticles.transform.localPosition.x, 0.1153f, planePropulsionParticles.transform.localPosition.z);

            // rotate wapr particles
            warpParticleObj1.transform.localEulerAngles = new Vector3(90, 180, 0);
            warpParticleObj2.transform.localEulerAngles = new Vector3(90, 180, 0);

            // translate warp particles
            warpParticleObj1.transform.localPosition = new Vector3(warpParticleObj1.transform.localPosition.x, 6.25f, warpParticleObj1.transform.localPosition.z);
            warpParticleObj2.transform.localPosition = new Vector3(warpParticleObj1.transform.localPosition.x, 6.25f, warpParticleObj1.transform.localPosition.z);

            // fade in highscore and current score where level count is currently
            currentScore.text = "highscore: " + PlayerPrefs.GetInt("highScore", 0) + "m";

            passedLogic = false;
        }

        if (!startCounting && minigameStarted)
        {
            StartCoroutine(IncrementScore());
            startCounting = true;
        }

        // spawn new
        if (frontHit)
        {
            SpawnMiniGameObj();
            frontHit = false;
        }

        // despawn old
        if (endHit)
        {
            DespawnMiniGameObj();
            endHit = false;
        }
    }

    void PerfectHit()
    {
        GameObject tempObj = Instantiate(perfectHitsTitles[perfectHitCounts],
            Vector3.zero, Quaternion.identity, uiFolder);
        perfectHitSounds[perfectHitCounts].Play();

        tempObj.transform.localPosition = new Vector3(0, 0.125f+(0.225f* perfectHitCounts), 0);

        float scaleInt = 0.6f + (0.05f * perfectHitCounts);
        tempObj.transform.localScale = new Vector3(scaleInt, scaleInt, scaleInt);

        perfectHitCounts++;
        if (perfectHitCounts > perfectHitSounds.Length - 1)
            perfectHitCounts = perfectHitSounds.Length - 1;
    }

    IEnumerator FadeIn(AudioSource myAudio)
    {
        float timer = 0, totalTime = 24;
        difficultMenuMusic.enabled = false;
        yield return new WaitForSeconds(1.8f);
        minigameMusic.enabled = true;
        while (timer <= totalTime)
        {
            myAudio.volume = Mathf.Lerp(0, 0.5f, timer / totalTime);
            yield return new WaitForFixedUpdate();
            timer++;
        }
    }

    IEnumerator FadeOut(AudioSource myAudio)
    {
        float timer = 0, totalTime = 24;
        float startingLevel = myAudio.volume;
        while(timer <= totalTime)
        {
            myAudio.volume = Mathf.Lerp(startingLevel, 0, timer / totalTime);
            yield return new WaitForFixedUpdate();
            timer++;
        }
    }

    void SpawnLevel()
    {
        // check if change color
        // pipeMat, obsMat;
        // [SerializeField] Color[] matHues;
        int levelInt = PlayerPrefs.GetInt("levelCount", 1);

        // determine menu music
        if (levelInt % 5 == 0)
            menuMusic.enabled = false;
        else
            difficultMenuMusic.enabled = false;

        minigameMusic.enabled = false;

        SetMatColors(levelInt);

        for (int i = 0; i < 3; i++)
            SpawnMainGameObj(i, -0.01625f, levelInt);
        PlayerPrefs.SetInt("SpawnNewLevel", 0);
    }

    void SetMatColors(int levelInt)
    {
        // base 156, 21, 60 /// 0-> 11
        // special: pipe: h, 22, 14, obs: h, 21, 64

        // determine colors
        if (levelInt % 5 == 0) // 5 or 10
        {
            // determine pipe hue
            int hueInt = (levelInt % 60) / 5; // reset every 60 levels
            float H, S, V;
            Color.RGBToHSV(pipeMat.color, out H, out S, out V);
            Color pipeColor = Color.HSVToRGB(hueInt * 30 / 360f, 0.22f, 0.14f);
            pipeMat.color = pipeColor;

            // determine obstacle hue
            Color.RGBToHSV(obsMat.color, out H, out S, out V);
            Color obsColor = Color.HSVToRGB(((hueInt * 30) + 180) % 360 / 360f, 0.21f, 0.64f);
            obsMat.color = obsColor;
        }
        else
        {
            // determine pipe hue
            int hueInt = (levelInt % 60) / 5; // reset every 60 levels
            float H, S, V;
            Color.RGBToHSV(pipeMat.color, out H, out S, out V);
            Color pipeColor = Color.HSVToRGB(hueInt * 30 / 360f, 0.21f, 0.60f);
            pipeMat.color = pipeColor;

            // determine obstacle hue
            Color.RGBToHSV(obsMat.color, out H, out S, out V);
            Color obsColor = Color.HSVToRGB(((hueInt * 30) + 180) % 360 / 360f, 0.21f, 0.64f);
            obsMat.color = obsColor;
        }
    }

    void SpawnMainGameObj(int incrementor, float distance, int levelInt)
    {
        int objectToSpawn;

        if (PlayerPrefs.GetInt("SpawnNewLevel", 1) == 1)
        {
            objectToSpawn = Random.Range(0, mainCourseObstacleMenu.Length); // default
            switch (levelInt)
            {
                case 1: // level 1
                    switch (incrementor)
                    {
                        case 0: // obstacle 1
                            objectToSpawn = 10;
                            break;
                        case 1: // obstacle 2
                            objectToSpawn = 11;
                            break;
                        case 2: // obstacle 3
                            objectToSpawn = 8;
                            break;
                    }
                    break;
                case 2: // level 2
                    switch (incrementor)
                    {
                        case 0: // obstacle 1
                            objectToSpawn = 4;
                            break;
                        case 1: // obstacle 2
                            objectToSpawn = 5;
                            break;
                        case 2: // obstacle 3
                            objectToSpawn = 4;
                            break;
                    }
                    break;
                case 3: // level 3
                    switch (incrementor)
                    {
                        case 0: // obstacle 1
                            objectToSpawn = 0;
                            break;
                        case 1: // obstacle 2
                            objectToSpawn = 1;
                            break;
                        case 2: // obstacle 3
                            objectToSpawn = 0;

                            break;
                    }
                    break;
                case 4: // level 4
                    switch (incrementor)
                    {
                        case 0: // obstacle 1
                            objectToSpawn = 6;
                            break;
                        case 1: // obstacle 2
                            objectToSpawn = 7;
                            break;
                        case 2: // obstacle 3
                            objectToSpawn = 6;
                            break;
                    }
                    break;
                case 5: // level 5
                    switch (incrementor)
                    {
                        case 0: // obstacle 1
                            objectToSpawn = 14;
                            break;
                        case 1: // obstacle 2
                            objectToSpawn = 15;
                            break;
                        case 2: // obstacle 3
                            objectToSpawn = 14;
                            break;
                    }
                    break;
                default:
                    break;
            }

            PlayerPrefs.SetInt("ObstacleInt" + incrementor, objectToSpawn);
        }
        else
            objectToSpawn = PlayerPrefs.GetInt("ObstacleInt" + incrementor, Random.Range(0, mainCourseObstacleMenu.Length));

        GameObject tempObj = Instantiate(mainCourseObstacleMenu[objectToSpawn], new Vector3(0, mainGameSpawnHieght, 0), Quaternion.identity, mainLevel);
        tempObj.transform.localPosition = new Vector3(0, mainGameSpawnHieght, 0);
        tempObj.transform.localEulerAngles = Vector3.zero;
        mainGameSpawnHieght += distance;
    }


    void SpawnMiniGameObj()
    {
        GameObject tempObj = Instantiate(miniGameObstacleMenu[Random.Range(0, miniGameObstacleMenu.Length)], new Vector3(0, minigameSpawnHieght, 0), Quaternion.identity, miniGameLevel);
        tempObj.transform.localPosition = new Vector3(0, tempObj.transform.localPosition.y, 0);
        spawnedminiGameObstacles.Add(tempObj);
        minigameSpawnHieght += 7.5f;
    }

    void DespawnMiniGameObj()
    {
        if (spawnedminiGameObstacles.Count > 2)
        {
            Destroy(spawnedminiGameObstacles[0]);
            spawnedminiGameObstacles.RemoveAt(0);
        }
    }

    IEnumerator IncrementScore()
    {
        while (!levelFailed)
        {
            myScore++;
            currentScore.text = "score: " + myScore + "m";
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator FlashMiniGameTime()
    {
        yield return new WaitForSeconds(0.15f);
        minigameTime.enabled = true;
        minigameTimeBG.enabled = true;
        yield return new WaitForSeconds(1.7f);
        minigameTime.enabled = false;
        minigameTimeBG.enabled = false;


        currentScore.color = new Color(currentScore.color.r, currentScore.color.g, currentScore.color.b, 0);
        yield return new WaitForSeconds(0.5f);
        currentScore.text = "score: " + myScore + "m";
        StartCoroutine(FadeTextIn(currentScore));

    }

    IEnumerator StartLogic()
    {
        whiteSquare.enabled = true;
        whiteSquare.color = Color.white;
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(FadeImageOut(whiteSquare));
    }

    IEnumerator RetryLiterature(SpriteRenderer mainText, SpriteRenderer bgText)
    {
        float timer = 0, totalTime = 40;
        Color startingColor1 = mainText.color;
        Color startingColor2 = bgText.color;
        Transform textTransform = mainText.gameObject.transform.parent.transform;

        Vector3 startingScale = textTransform.localScale;

        while (timer <= totalTime)
        {
            if (timer <= 18)
                textTransform.localScale = Vector3.Lerp(startingScale*0.1f, startingScale * 1.7f, timer / (totalTime-18));

            if (timer < totalTime * 0.75f)
            {
                mainText.color = Color.Lerp(startingColor1, new Color(startingColor1.r, startingColor1.g, startingColor1.b, 1), timer / (totalTime*0.7f));
                bgText.color = Color.Lerp(startingColor2, new Color(startingColor2.r, startingColor2.g, startingColor2.b, 1), timer / (totalTime*0.7f));
            }

            yield return new WaitForFixedUpdate();
            timer++;
        }

        timer = 0;
        totalTime = 80;
        startingScale = textTransform.localScale;
        while (timer <= totalTime)
        {
            textTransform.localScale = Vector3.Lerp(startingScale, new Vector3(startingScale.x*1.15f, startingScale.y*1.5f, startingScale.z*1.5f), timer / totalTime);
            yield return new WaitForFixedUpdate();
            timer++;
        }
    }

    IEnumerator RestartWait()
    {
        for (int i = 0; i < 42; i++)
        {
            if (ControlsLogic.fastTap)
                break;
            yield return new WaitForSeconds(0.1f);

        }
        StartCoroutine(RestartLevel(whiteSquare));
    }

    IEnumerator RestartLevel(SpriteRenderer myImage)
    {
        float timer = 0, totalTime = 24;
        Color startingColor = myImage.color;
        myImage.enabled = true;
        while (timer <= totalTime)
        {
            myImage.color = Color.Lerp(new Color(startingColor.r, startingColor.g, startingColor.b, 0), new Color(startingColor.r, startingColor.g, startingColor.b, 1), timer / totalTime);
            yield return new WaitForFixedUpdate();
            timer++;
        }
        if (fasterLoad)
        {
            fasterLoad = false;
            yield return new WaitForSecondsRealtime(0.7f);
            inLoading = false;
        }
        else
            yield return new WaitForSecondsRealtime(0.3f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }



    IEnumerator FadeImageOut(SpriteRenderer myImage)
    {
        float timer = 0, totalTime = 24;
        Color startingColor = myImage.color;
        myImage.enabled = true;
        while (timer <= totalTime)
        {
            myImage.color = Color.Lerp(new Color(startingColor.r, startingColor.g, startingColor.b, 1), new Color(startingColor.r, startingColor.g, startingColor.b, 0), timer / totalTime);
            yield return new WaitForFixedUpdate();
            timer++;
        }
        myImage.enabled = false;
    }

    IEnumerator FadeImageIn(SpriteRenderer myImage, float totalTime)
    {
        float timer = 0;
        Color startingColor = myImage.color;
        myImage.enabled = true;
        while (timer <= totalTime)
        {
            myImage.color = Color.Lerp(new Color(startingColor.r, startingColor.g, startingColor.b, 0), new Color(startingColor.r, startingColor.g, startingColor.b, 1), timer / totalTime);
            yield return new WaitForFixedUpdate();
            timer++;
        }
    }

    IEnumerator FadeTextOut(TextMeshPro myTtext)
    {
        float timer = 0, totalTime = 24;
        Color startingColor = myTtext.color;
        while (timer <= totalTime)
        {
            myTtext.color = Color.Lerp(new Color(startingColor.r, startingColor.g, startingColor.b, 1), new Color(startingColor.r, startingColor.g, startingColor.b, 0), timer / totalTime);
            yield return new WaitForFixedUpdate();
            timer++;
        }
    }

    IEnumerator FadeTextIn(TextMeshPro myTtext)
    {
        float timer = 0, totalTime = 24;
        Color startingColor = myTtext.color;
        while (timer <= totalTime)
        {
            myTtext.color = Color.Lerp(new Color(startingColor.r, startingColor.g, startingColor.b, 0), new Color(startingColor.r, startingColor.g, startingColor.b, 1), timer / totalTime);
            yield return new WaitForFixedUpdate();
            timer++;
        }
    }
}
