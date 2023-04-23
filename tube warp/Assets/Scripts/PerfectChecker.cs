using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

public class PerfectChecker : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "obstacle")
        {
            if (tag == "leftCol")
            {
                GameManager.leftHit = true;
            }
            else if (tag == "rightCol")
            {
                GameManager.rightHit = true;
            }
        }
    }
}
