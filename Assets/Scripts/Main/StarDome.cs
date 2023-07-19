using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarDome : MonoBehaviour {

    public MeshRenderer starPrefab;
    public Vector2 radiusMinMax;
    public int count = 1000;
    public int starSpeed = 0;
    const float calibrationDst = 2000;
    public Vector2 brightnessMinMax;
    MeshRenderer[] stars;

    Camera cam;

    void Start () 
    {
        cam = Camera.main;
        Debug.Log(cam);
        stars = new MeshRenderer[count];
        int idx = 0;
        //var sw = System.Diagnostics.Stopwatch.StartNew ();
        float starDst = cam.farClipPlane - radiusMinMax.y;
        float scale = starDst / calibrationDst;

        for (int i = 0; i < count; i++) {
            MeshRenderer star = Instantiate (starPrefab, Random.onUnitSphere * starDst, Quaternion.identity, transform);
            stars[idx++] = star;
            float t = SmallestRandomValue (6);
            star.transform.localScale = Vector3.one * Mathf.Lerp (radiusMinMax.x, radiusMinMax.y, t) * scale;
            star.material.color = Color.Lerp (Color.black, star.material.color, Mathf.Lerp (brightnessMinMax.x, brightnessMinMax.y, t));
        }
        //Debug.Log (sw.ElapsedMilliseconds);
    }

    void FixedUpdate () 
    {
        for (int i = 0; i < count; ++i)
        {
            stars[i].transform.position += Vector3.one * starSpeed;
        }
    }

    float SmallestRandomValue (int iterations) {
        float r = 1;
        for (int i = 0; i < iterations; i++) {
            r = Mathf.Min (r, Random.value);
        }
        return r;
    }

    void LateUpdate () {
        if (cam != null) {
            transform.position = cam.transform.position;
        }
    }
}