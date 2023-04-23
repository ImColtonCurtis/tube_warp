using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class AtmosphereNoise : MonoBehaviour
{
    private static AtmosphereNoise instance;

    [SerializeField] AudioSource myAudio;

    float baseVol = 0.21f; // was 0.011f
    float basePitch = 1;

    bool fadedOut;

    public static AtmosphereNoise Instance
    {
        get { return instance;  }
    }

    private void Awake()
    {
        fadedOut = false;

        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        if (GameManager.levelStarted && !GameManager.levelFailed && !GameManager.levelPassed)
        {
            // control based on
            myAudio.volume = baseVol + (Mathf.Min((Mathf.Abs(ControlsLogic.delta)/0.00152659f*0.0075f), 0.0075f*2)*19.09f);
            myAudio.pitch = basePitch + Mathf.Min((Mathf.Abs(ControlsLogic.delta) / 0.00152659f * 0.075f), 0.075f);
        }
        if ((GameManager.levelFailed || GameManager.levelPassed) && GameManager.levelStarted && !fadedOut)
        {
            fadedOut = true;
            StartCoroutine(FadeOut(myAudio));
        }
    }

    IEnumerator FadeOut(AudioSource myAudio)
    {
        float timer = 0, totalTime = 24;
        float startingLevel = myAudio.volume;
        while (timer <= totalTime)
        {
            myAudio.volume = Mathf.Lerp(startingLevel, 0, timer / totalTime);
            yield return new WaitForFixedUpdate();
            timer++;
        }
    }
}
