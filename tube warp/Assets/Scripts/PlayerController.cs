using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Animator cameraShake;

    [SerializeField] Rigidbody myRigidBody;
    Transform myTransform;

    [SerializeField] SoundManagerLogic mySoundManager;

    AudioSource currentSound, nextSound;

    float moveSpeed = 0.1f*1.25f; // was 0.085f*1.25f
    bool changedMS;

    [SerializeField] bool oppositeDir;

    [SerializeField] ParticleSystem myParticles1, myParticles2, myParticles3;

    float prevZ, deltaMove;

    [SerializeField] GameObject shatterObj, bobObj, explosionParticles;
    bool rotateUpward = false;
    Vector3 speedVector;

    public static bool rotatingPlane;

    private void Start()
    {
        rotatingPlane = false;

        changedMS = false;

        myTransform = transform;
        deltaMove = 0;
        prevZ = 0;
        rotateUpward = false;
        if (PlayerPrefs.GetInt("levelCount", 1) % 5 == 0)
            moveSpeed = 0.1f * 1.4f; // was 0.085f*1.5f

        if (oppositeDir)
            moveSpeed *= -1;

        speedVector = new Vector3(0, 0, moveSpeed);
    }

    public void FixedUpdate()
    {
        if (GameManager.levelStarted && !GameManager.levelFailed)
        {
            if (!GameManager.levelPassed)
            {
                transform.localPosition += new Vector3(0, 0, moveSpeed);
                deltaMove = Mathf.Abs(transform.localPosition.z - prevZ);
                prevZ = transform.localPosition.z;
            }
            else
            {
                transform.localPosition += speedVector;
                if (!rotateUpward)
                {
                    StartCoroutine(RotatePlane());
                    rotateUpward = true;
                }

                if (!FollowCamera.levelPassedFollow)
                {
                    deltaMove = 0;
                    prevZ = 0;
                }
                else
                {
                    deltaMove = Mathf.Abs(transform.localPosition.y - prevZ);
                    prevZ = transform.localPosition.y;
                }

            }
        }
        else
            deltaMove = 0;
    }

    private void Update()
    {
        myParticles1.startSpeed = deltaMove * 411.7691627f;
        myParticles2.startSpeed = deltaMove * 470.5933288f;
        myParticles3.startSpeed = deltaMove * 411.7691627f;

        if (!changedMS && GameManager.playMiniGame)
        {
            moveSpeed = 0.085f * 1.5f;
            changedMS = true;
        }
    }

    IEnumerator RotatePlane()
    {
        float timer = 0, totalTimer = 40;

        yield return new WaitForSeconds(0.5f);

        rotatingPlane = true;

        while (timer <= totalTimer)
        {
            float newX = Mathf.Lerp(270, 180, timer / totalTimer);

            transform.localEulerAngles = new Vector3(newX, 0, 0);

            if (transform.localEulerAngles.x > 270)
            {
                transform.localEulerAngles = new Vector3(newX, 0, 0);
            }

            speedVector = Vector3.Lerp(new Vector3(0, 0, moveSpeed), new Vector3(0, moveSpeed/2.25f, 0), timer / totalTimer);
            yield return new WaitForFixedUpdate();
            timer++;
        }
        yield return new WaitForSeconds(0.5f);
        transform.localEulerAngles = new Vector3(180, 0, 0);
        rotatingPlane = false;

        FollowCamera.levelPassedFollow = true;
    }

    IEnumerator CrossFade(AudioSource oldSound, AudioSource newSound)
    {
        float timer = 0, totalTime = 6;
        newSound.Play();

        float newSoundStart = newSound.volume;
        float oldSoundStart = 1;
        if (oldSound != null)
            oldSoundStart = oldSound.volume;

        while (timer <= totalTime)
        {
            if (oldSound != null)
                oldSound.volume = Mathf.Lerp(oldSoundStart, 0, timer / totalTime);
            newSound.volume = Mathf.Lerp(newSoundStart, 1,  timer / totalTime);

            yield return new WaitForFixedUpdate();
            timer++;
        }
        newSound.volume = 1;
        if (oldSound != null)
        {
            oldSound.volume = 0;
            oldSound.Stop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!GameManager.levelFailed && (!GameManager.levelPassed || FollowCamera.levelPassedFollow) && other.tag == "obstacle")
        {
            bobObj.SetActive(false);
            shatterObj.SetActive(true);
            explosionParticles.SetActive(true);
            GameManager.levelFailed = true;
        }

        if (GameManager.levelFailed && (!GameManager.levelPassed || FollowCamera.levelPassedFollow) && other.tag == "obstacle")
        {
            bobObj.SetActive(false);
            shatterObj.SetActive(true);
            explosionParticles.SetActive(true);
            GameManager.levelFailed = true;
        }

        if (!GameManager.levelFailed && !GameManager.levelPassed && other.tag == "finish")
        {
            GameManager.levelPassed = true;
        }

        if (other.tag == "start" && !GameManager.minigameStarted)
            GameManager.minigameStarted = true;

        if (other.tag == "front" && !GameManager.frontHit)
            GameManager.frontHit = true;

        if (other.tag == "end" && !GameManager.endHit)
            GameManager.endHit = true;
    }
}
