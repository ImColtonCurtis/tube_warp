using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShatterObj : MonoBehaviour
{
    float radius = 50f, force = 1100f;

    int incrementor;

    private void OnEnable()
    {
        incrementor = 0;

        foreach (Transform transform in gameObject.GetComponentInChildren<Transform>())
        {
            Rigidbody rb = transform.GetComponent<Rigidbody>();
            if (rb != null && incrementor != 0)
            {
                rb.AddExplosionForce(force, transform.position, radius);
            }
            incrementor++;
        }
    }
}
