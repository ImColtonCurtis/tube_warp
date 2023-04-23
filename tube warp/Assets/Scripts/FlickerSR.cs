using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FlickerSR : MonoBehaviour
{
    SpriteRenderer mySR;
    bool changeColor;

    // Update is called once per frame
    void OnEnable()
    {
        changeColor = false;

        mySR = GetComponent<SpriteRenderer>();
        SetColor();
        StartCoroutine(Flicker());
    }

    private void Update()
    {
        // change hue
        if (!changeColor && GameManager.myScore % 20 == 0)
        {
            SetColor();

            changeColor = true;
        }
        if (changeColor && GameManager.myScore % 20 == 1) // reset lock
            changeColor = false;

    }

    void SetColor()
    {
        int hueInt = (GameManager.myScore % 240) / 20; // reset every 60 levels
        float H, S, V;
        Color.RGBToHSV(mySR.color, out H, out S, out V);
        Color laserColor = Color.HSVToRGB(hueInt * 30 / 360f, S, V);
        mySR.color = laserColor;
    }

    IEnumerator Flicker()
    {
        while (!GameManager.levelFailed)
        {
            float newAlpha = Random.Range(0.1f, 1);
            Color startingColor = mySR.color;
            Color endingColor = new Color(mySR.color.r, mySR.color.g, mySR.color.b, newAlpha);
            float timer = 0, toatlTimer = Random.Range(1, 12);

            while (timer <= toatlTimer)
            {
                mySR.color = Color.Lerp(startingColor, endingColor, timer / toatlTimer);
                yield return new WaitForFixedUpdate();
                timer++;
            }
            yield return new WaitForSeconds(Random.Range(0.05f, 0.5f));
        }
    }
}
