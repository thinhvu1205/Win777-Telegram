using System.Collections;
using System.Collections.Generic;
using Globals;
using UnityEngine;
using UnityEngine.UI;

public class AutoResize : MonoBehaviour
{
    void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (Screen.width >= Screen.height) return;
        RectTransform thisRT = gameObject.GetComponent<RectTransform>();
        thisRT.anchorMin = new Vector2(.5f, .5f);
        thisRT.anchorMax = new Vector2(.5f, .5f);
        thisRT.anchoredPosition = Vector2.zero;
        thisRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1280);
        thisRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 720);
        float ratio = Mathf.Min(Screen.width / 720f, Screen.height / 1280f);
        thisRT.localScale = ratio * Vector3.one;
        gameObject.transform.parent.GetComponent<CanvasScaler>().enabled = false; //canvas
        thisRT.localRotation = Quaternion.Euler(0, 0, -90);
#else
#endif

        //test, remove on build
        // if (Screen.width >= Screen.height) return;
        // RectTransform thisRT = gameObject.GetComponent<RectTransform>();
        // thisRT.anchorMin = new Vector2(.5f, .5f);
        // thisRT.anchorMax = new Vector2(.5f, .5f);
        // thisRT.anchoredPosition = Vector2.zero;
        // thisRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1280);
        // thisRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 720);
        // float ratio = Mathf.Min(Screen.width / 720f, Screen.height / 1280f);
        // thisRT.localScale = ratio * Vector3.one;
        // gameObject.transform.parent.GetComponent<CanvasScaler>().enabled = false; //canvas
        // thisRT.localRotation = Quaternion.Euler(0, 0, -90);
    }
}
