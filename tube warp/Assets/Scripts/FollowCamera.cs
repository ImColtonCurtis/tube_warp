using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    public float smoothTime = 0.05f;
    private Vector3 velocity = Vector3.zero;
    [SerializeField] float zOffset, yOffset;
    Vector3 targetPosition;

    public static bool levelPassedFollow;

    private void Awake()
    {
        levelPassedFollow = false;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (!GameManager.levelFailed && !GameManager.levelPassed)
        {
            // Define a target position above and behind the target transform
            targetPosition = new Vector3(0, 0, target.position.z + zOffset);

            // Smoothly move the camera towards that target position
            transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPosition, ref velocity, smoothTime);
        }
        if (levelPassedFollow && GameManager.playMiniGame)
        {
            // Define a target position above and behind the target transform
            targetPosition = new Vector3(0, target.position.y + yOffset, target.position.z + zOffset * 2.5f);

            // Smoothly move the camera towards that target position
            transform.localPosition = targetPosition;
        }
    }
}
