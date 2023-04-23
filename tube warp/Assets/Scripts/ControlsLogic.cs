using System.Collections;
using System.Collections.Generic;
using Unity.Services.Mediation.Samples;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class ControlsLogic : MonoBehaviour
{
    bool touchedDown;

    Vector3 prevPoint = Vector3.zero;

    [SerializeField]
    Transform rotationRing, planeTransform, rotatorParticles;

    public static float delta;

    Vector3 targetRot;

    float rotationSpeed = 1;
    private Vector3 velocity = Vector3.zero;

    [SerializeField] GameObject noIcon;
    [SerializeField] Animator soundAnim;

    bool centeringPlane = false, startedCentering, completedCentering;

    public static bool fastTap = false;

    bool changeParticles;

    int cheatCounter, baseTurnSpeed = 3150;

    void Awake()
    {
        touchedDown = false;
        centeringPlane = false;
        startedCentering = false;
        completedCentering = false;
        fastTap = false;
        changeParticles = false;

        cheatCounter = 0;

        if (PlayerPrefs.GetInt("SoundStatus", 1) == 1)
        {
            noIcon.SetActive(false);
            AudioListener.volume = 1;
        }
        else
        {
            noIcon.SetActive(true);
            AudioListener.volume = 0;
        }
    }

    private void FixedUpdate()
    {
        if (!GameManager.levelFailed && GameManager.levelStarted)
        {
            if (!changeParticles && FollowCamera.levelPassedFollow)
            {
                rotatorParticles.transform.localEulerAngles = new Vector3(rotatorParticles.transform.localEulerAngles.x, 0, 0);
                changeParticles = true;
            }

            if (!touchedDown)
            {
                if (!GameManager.levelPassed)
                {
                    // bring plane to even wings
                    if (!centeringPlane && Mathf.Abs(planeTransform.localEulerAngles.x) > 270)
                    {
                        centeringPlane = true;
                        completedCentering = false;
                        StartCoroutine(BringToCenter());
                    }
                    // carry out movement
                    if (Mathf.Abs(delta) > 0)
                    {
                        delta -= delta * 0.0667f;
                        rotationSpeed = (Mathf.Abs(delta) / 10) + baseTurnSpeed; // was 2000
                        rotationRing.Rotate(Vector3.down * delta * rotationSpeed);

                        rotatorParticles.Rotate(Vector3.back * delta * rotationSpeed);

                        if (!startedCentering && !completedCentering)
                        {
                            rotationSpeed = (Mathf.Abs(delta) / 10) + (baseTurnSpeed/2); // was 1000
                            planeTransform.Rotate(Vector3.up * delta * rotationSpeed);

                            // clamp
                            planeTransform.localEulerAngles = new Vector3(Mathf.Min(planeTransform.localEulerAngles.x, 307), planeTransform.localEulerAngles.y, planeTransform.localEulerAngles.z);
                        }
                    }
                }
                else if (FollowCamera.levelPassedFollow && GameManager.playMiniGame)
                {
                    // bring plane to even wings
                    if (!centeringPlane && Mathf.Abs(planeTransform.localPosition.x) > 0)
                    {
                        centeringPlane = true;
                        completedCentering = false;
                        StartCoroutine(BringToCenter());
                    }
                    // carryout movement
                    if (Mathf.Abs(delta) > 0)
                    {
                        delta -= delta * 0.0667f;
                        rotationSpeed = (Mathf.Abs(delta) / 10) + baseTurnSpeed; // was 2000

                        float moveSpeed = (Mathf.Abs(delta) / 5) + 12;
                        planeTransform.Translate(Vector3.left * delta * moveSpeed * Time.fixedDeltaTime * 50);

                        if (!startedCentering && !completedCentering)
                        {
                            rotationSpeed = (Mathf.Abs(delta) / 10) + (baseTurnSpeed/2); // was 1000
                            planeTransform.Rotate(Vector3.forward * delta * rotationSpeed);

                            // clamp movement
                            planeTransform.localPosition = new Vector3(Mathf.Clamp(planeTransform.localPosition.x, -0.55f, 0.55f), planeTransform.localPosition.y, planeTransform.localPosition.z);
                            // clamp rotations
                            planeTransform.localEulerAngles = new Vector3(planeTransform.localEulerAngles.x, planeTransform.localEulerAngles.y, Mathf.Clamp(planeTransform.localEulerAngles.z, 140f, 220f));
                        }
                    }
                }
            }
        }
    }

    IEnumerator BringToCenter()
    {
        float timer = 0, totalTime = Mathf.Max(Mathf.Abs(delta)*1000, 22);
        yield return new WaitForSeconds(Mathf.Max(3, totalTime / 2)/60);
        startedCentering = true;
        float startingAngle = planeTransform.localEulerAngles.x;
        Vector3 startingPosition = planeTransform.localPosition;
        if (GameManager.levelPassed)
            startingAngle = planeTransform.localEulerAngles.z;

        while (timer <= totalTime && !touchedDown && (!GameManager.levelPassed || FollowCamera.levelPassedFollow))
        {
            float newAngle = Mathf.Lerp(startingAngle, 270, timer / totalTime);

            if (!GameManager.levelPassed)
                planeTransform.localEulerAngles = new Vector3(Mathf.Clamp(newAngle, 270, 307), planeTransform.localEulerAngles.y, planeTransform.localEulerAngles.z);
            else
            {
                // correct angle
                newAngle = Mathf.Lerp(startingAngle, 180, timer / totalTime);
                planeTransform.localEulerAngles = new Vector3(planeTransform.localEulerAngles.x, planeTransform.localEulerAngles.y, Mathf.Clamp(newAngle, 143f, 217f));
                // correct position
                planeTransform.localPosition = Vector3.Lerp(new Vector3(startingPosition.x, planeTransform.localPosition.y, planeTransform.localPosition.z), new Vector3(0, planeTransform.localPosition.y, planeTransform.localPosition.z), timer / totalTime);
            }
            yield return new WaitForFixedUpdate();
            timer++;

            if (touchedDown && !GameManager.levelPassed || PlayerController.rotatingPlane)
                break;
        }
        completedCentering = true;
        startedCentering = false;
        centeringPlane = false;
    }

    void OnTouchDown(Vector3 point)
    {
        if (!touchedDown && !GameManager.inLoading)
        {
            if (ShowAds.poppedUp)
            {
                if (point.x <= 0)
                    ShowAds.shouldShowRewardedAd = true;
                else
                    ShowAds.dontShow = true;
            }
            else if (ShowAds.skipPoppedUp)
            {
                if (point.x <= 0)
                    ShowAds.shouldShowRewardedAd = true;
                else
                    ShowAds.dontShow = true;
            }
            else
            {
                // cheat: top-right, top-right, top-left, bottom-right
                // top right tap
                if (!GameManager.levelStarted && (cheatCounter == 0 || cheatCounter == 1) && point.x >= 0.03f && point.y >= 8f)
                {
                    cheatCounter++;
                }
                // top left tap
                else if (!GameManager.levelStarted && (cheatCounter == 2) && point.x <= -0.03f && point.y >= 8f)
                {
                    cheatCounter++;
                }
                // bottom right tap
                else if (!GameManager.levelStarted && (cheatCounter == 3) && point.x >= 0.03f && point.y <= 7.92f)
                {
                    cheatCounter = 0;
                    if (!GameManager.cheatOn)
                        GameManager.cheatOn = true;
                    else
                        GameManager.cheatOn = false;
                }

                else if (!GameManager.levelStarted && point.x <= -0.01f && point.y <= 7.92f) // bottom left button clicked
                {
                    if (PlayerPrefs.GetInt("SoundStatus", 1) == 1)
                    {
                        PlayerPrefs.SetInt("SoundStatus", 0);
                        noIcon.SetActive(true);
                        AudioListener.volume = 0;
                    }
                    else
                    {
                        PlayerPrefs.SetInt("SoundStatus", 1);
                        noIcon.SetActive(false);
                        AudioListener.volume = 1;
                    }
                    soundAnim.SetTrigger("Blob");
                }
                else
                {
                    touchedDown = true;
                    if (!GameManager.levelFailed)
                    {
                        if (!GameManager.levelStarted)
                            GameManager.levelStarted = true;
                    }
                }
                if ((GameManager.levelFailed || (GameManager.levelPassed && !GameManager.playMiniGame)) && !fastTap)
                    fastTap = true;
            }
        }
    }

    void OnTouchStay(Vector3 point)
    {
        if (!touchedDown && !GameManager.levelFailed)
        {
            touchedDown = true;
        }
        if (prevPoint == Vector3.zero)
            prevPoint = point;
        if (touchedDown && !GameManager.levelFailed)
        {
            delta = Mathf.Clamp(point.x - prevPoint.x, -0.045f, 0.045f);

            prevPoint = point;

            if (!GameManager.levelPassed)
            {
                rotationSpeed = (Mathf.Abs(delta) / 10) + baseTurnSpeed; // was 2000
                rotationRing.Rotate(Vector3.down * delta * rotationSpeed * Time.fixedDeltaTime * 50);

                rotatorParticles.Rotate(Vector3.back * delta * rotationSpeed * Time.fixedDeltaTime * 50);

                rotationSpeed = (Mathf.Abs(delta) / 10) + (baseTurnSpeed/2); // was 1000
                planeTransform.Rotate(Vector3.up * delta * rotationSpeed * Time.fixedDeltaTime * 50);

                // clamp
                planeTransform.localEulerAngles = new Vector3(Mathf.Min(planeTransform.localEulerAngles.x, 307), planeTransform.localEulerAngles.y, planeTransform.localEulerAngles.z);
            }
            else if (FollowCamera.levelPassedFollow)
            {
                // move plane left or right
                float moveSpeed = (Mathf.Abs(delta) / 5) + 12;
                // store y
                float planeY = planeTransform.localPosition.y;
                planeTransform.Translate(Vector3.left * delta * moveSpeed * Time.fixedDeltaTime * 50);

                // rotate plane left or right
                rotationSpeed = (Mathf.Abs(delta) / 10) + 1000;
                planeTransform.Rotate(Vector3.forward * delta * rotationSpeed * Time.fixedDeltaTime * 50);

                // clamp movement
                planeTransform.localPosition = new Vector3(Mathf.Clamp(planeTransform.localPosition.x, -0.55f, 0.55f), planeY, planeTransform.localPosition.z);
                // clamp rotations
                planeTransform.localEulerAngles = new Vector3(planeTransform.localEulerAngles.x, planeTransform.localEulerAngles.y, Mathf.Clamp(planeTransform.localEulerAngles.z, 140f, 220f));
            }
        }
    }

    void OnTouchUp()
    {
        prevPoint = Vector3.zero;
        if (touchedDown)
        {
            touchedDown = false;
        }
    }

    void OnTouchExit()
    {
        prevPoint = Vector3.zero;
        if (touchedDown)
        {
            touchedDown = false;
        }
    }
}
