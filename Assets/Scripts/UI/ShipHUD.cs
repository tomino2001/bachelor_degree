﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShipHUD : MonoBehaviour {

    [Header ("Aim")]
    public float dotSize = 1;
    public float minAimAngle = 30;
    public Image centreDot;
    
    // TextMeshPro
    public TMPro.TMP_Text planetInfo;

    [Header ("Velocity indicators")]
    public VelocityIndicator velocityHorizontal;
    public VelocityIndicator velocityVertical;
    public Vector2 velocityIndicatorSizeMinMax;
    public Vector2 velocityIndicatorThicknessMinMax;
    public float maxVisDst;
    public float velocityDisplayScale = 1;

    CelestialBody lockedBody;
    Camera camera;
    Transform camTransform;
    LockOnUI lockOnUI;
    ShipController ship;

    void Start () {
        // Need to draw UI AFTER floating origin updates, otherwise may flicker when origin changes
        FindObjectOfType<EndlessManager> ().PostFloatingOriginUpdate += UpdateUI;
    }

    void Init () {
        if (camera == null) {
            camera = Camera.main;
        }
        camTransform = camera.transform;

        if (lockOnUI == null) {
            lockOnUI = GetComponent<LockOnUI> ();
        }

        if (ship == null) {
            ship = FindObjectOfType<ShipController> ();
        }
    }

    void UpdateUI () {
        Init ();

        centreDot.rectTransform.localScale = Vector3.one * dotSize;

        if (ship.ShowHUD) {
            
            CelestialBody aimedBody = FindAimedBody ();

            if (aimedBody && aimedBody != lockedBody) {
                lockOnUI.DrawLockOnUI (aimedBody, false);
            }

            if (Input.GetMouseButtonDown (0)) {
                if (lockedBody == aimedBody) {
                    lockedBody = null;
                } else {
                    lockedBody = aimedBody;
                }
            }

            if (lockedBody) {
                lockOnUI.DrawLockOnUI (lockedBody, true);
                DrawPlanetHUD (lockedBody);
            } else {
                SetHudActive (false);
            }
        } else {
            lockedBody = null;
            SetHudActive (false);
        }
    }

    void SetHudActive (bool active) {
        planetInfo.gameObject.SetActive (active);
        velocityHorizontal.SetActive (active);
        velocityVertical.SetActive (active);
    }

    void DrawPlanetHUD (CelestialBody planet) {
        SetHudActive (true);
        Vector3 dirToPlanet = (planet.transform.position - camTransform.position).normalized;
        float dstToPlanetCentre = (planet.transform.position - camTransform.position).magnitude;
        float dstToPlanetSurface = dstToPlanetCentre - planet.radius;

        // Calculate horizontal/vertical axes relative to direction toward planet
        Vector3 horizontal = Vector3.Cross (dirToPlanet, camTransform.up).normalized;
        horizontal *= Mathf.Sign (Vector3.Dot (horizontal, camTransform.right)); // make sure roughly same direction as right vector of cam
        Vector3 vertical = Vector3.Cross (dirToPlanet, horizontal).normalized;
        vertical *= Mathf.Sign (Vector3.Dot (vertical, camTransform.up));

        // Calculate relative velocity
        Vector3 relativeVelocityWorldSpace = ship.Rigidbody.velocity - planet.velocity;
        //Debug.Log(relativeVelocityWorldSpace + "  planet: " + planet.velocity);
        float vx = -Vector3.Dot (relativeVelocityWorldSpace, horizontal);
        float vy = -Vector3.Dot (relativeVelocityWorldSpace, vertical);
        float vz = Vector3.Dot (relativeVelocityWorldSpace, dirToPlanet);
        Vector3 relativeVelocity = new Vector3 (vx, vy, vz);

        // Planet info
        Vector3 planetInfoWorldPos = planet.transform.position + horizontal * planet.radius * lockOnUI.lockedRadiusMultiplier + vertical * planet.radius * 0.35f;
        planetInfo.gameObject.SetActive (PointIsOnScreen (planetInfoWorldPos));
        planetInfo.rectTransform.localPosition = CalculateUIPos (planetInfoWorldPos);
        planetInfo.text = $"{planet.bodyName} \n{FormatDistance(dstToPlanetSurface)} \n{relativeVelocity.z:0}m/s";

        // Relative velocity lines
        if (PointIsOnScreen (planet.transform.position)) {
            float arrowHeadSizePercent = dstToPlanetSurface / maxVisDst;
            //Debug.Log (arrowHeadSizePercent);
            float arrowHeadSize = Mathf.Lerp (velocityIndicatorSizeMinMax.y, velocityIndicatorSizeMinMax.x, arrowHeadSizePercent);
            float indicatorThickness = Mathf.Lerp (velocityIndicatorThicknessMinMax.y, velocityIndicatorThicknessMinMax.x, dstToPlanetSurface / maxVisDst);
            float indicatorAngle = (relativeVelocity.x < 0) ? 180 : 0;
            var indicatorPos = CalculateUIPos (planet.transform.position + horizontal * planet.radius * lockOnUI.lockedRadiusMultiplier * Mathf.Sign (relativeVelocity.x));
            float indicatorMagnitude = Mathf.Abs (relativeVelocity.x) * velocityDisplayScale;
            velocityHorizontal.Update (indicatorAngle, indicatorPos, indicatorMagnitude, arrowHeadSize, indicatorThickness);

            indicatorAngle = (relativeVelocity.y < 0) ? 270 : 90;
            indicatorPos = CalculateUIPos (planet.transform.position + camTransform.up * planet.radius * lockOnUI.lockedRadiusMultiplier * Mathf.Sign (relativeVelocity.y));
            indicatorMagnitude = Mathf.Abs (relativeVelocity.y) * velocityDisplayScale;
            velocityVertical.Update (indicatorAngle, indicatorPos, indicatorMagnitude, arrowHeadSize, indicatorThickness);

        } else {
            velocityHorizontal.SetActive (false);
            velocityVertical.SetActive (false);
        }

    }

    CelestialBody FindAimedBody () {
        CelestialBody[] bodies = FindObjectsOfType<CelestialBody> ();
        CelestialBody aimedBody = null;

        Vector3 viewForward = camera.transform.forward;
        Vector3 viewOrigin = camera.transform.position;

        float nearestSqrDst = float.PositiveInfinity;

        // If aimed directly at any body, return the closest one
        foreach (var body in bodies) {
            Vector3 intersection;
            if (MathUtility.RaySphere (body.transform.position, body.radius, viewOrigin, viewForward, out intersection)) {
                float sqrDst = (viewOrigin - intersection).sqrMagnitude;
                if (sqrDst < nearestSqrDst) {
                    nearestSqrDst = sqrDst;
                    aimedBody = body;
                }
            }
        }

        if (aimedBody) {
            return aimedBody;
        }

        // Return body with min angle to view direction
        float minAngle = minAimAngle * Mathf.Deg2Rad;

        foreach (var body in bodies) {
            Vector3 offsetToBody = body.transform.position - camera.transform.position;
            float dstToBody = offsetToBody.magnitude;
            float aimAngle = Mathf.Acos (Vector3.Dot (viewForward, offsetToBody.normalized));

            if (aimAngle < minAngle) {
                minAngle = aimAngle;
                aimedBody = body;
            }
        }

        return aimedBody;
    }

    bool PointIsOnScreen (Vector3 worldPoint) {
        Vector3 p = camera.WorldToViewportPoint (worldPoint);
        return p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1 && p.z > 0;
    }

    static string FormatDistance (float distance) {
        const int maxMetreDst = 1000;
        string dstString = (distance < maxMetreDst) ? (int) distance + "m" : $"{distance/1000:0}km";
        return dstString;
    }

    Vector3 CalculateRelativeVelocity (CelestialBody body) {
        Vector3 dirToBody = (body.transform.position - camTransform.position).normalized;
        Vector3 relativeVelocityWorldSpace = ship.Rigidbody.velocity - body.velocity;

        // Calculate horizontal/vertical axes relative to direction toward planet
        Vector3 horizontal = Vector3.Cross (dirToBody, camTransform.up).normalized;
        horizontal *= Mathf.Sign (Vector3.Dot (horizontal, camTransform.right)); // make sure roughly same direction as right vector of cam
        Vector3 vertical = Vector3.Cross (dirToBody, horizontal).normalized;
        vertical *= Mathf.Sign (Vector3.Dot (vertical, camTransform.up));

        float vx = -Vector3.Dot (relativeVelocityWorldSpace, horizontal);
        float vy = -Vector3.Dot (relativeVelocityWorldSpace, vertical);
        float vz = Vector3.Dot (relativeVelocityWorldSpace, dirToBody);
        Vector3 relativeV = new Vector3 (vx, vy, vz);

        // Debug.Log ($"Rel world: {relativeVelocityWorldSpace} rel: {relativeV} speed world: {relativeVelocityWorldSpace.magnitude} speed rel: {relativeV.magnitude}");

        return relativeV;
    }

    Vector2 CalculateUIPos (Vector3 worldPos) {
        const int referenceWidth = 1920;
        const int referenceHeight = 1080;

        Vector3 viewportCentre = camera.WorldToViewportPoint (worldPos);
        if (viewportCentre.z <= 0) {
            viewportCentre.x = (viewportCentre.x <= 0.5f) ? 1 : 0;
            viewportCentre.y = (viewportCentre.y <= 0.5f) ? 1 : 0;
        }
        //screenCentre = new Vector2 (screenCentre.x / Screen.width, screenCentre.y / Screen.height);

        return new Vector2 ((viewportCentre.x - 0.5f) * referenceWidth, (viewportCentre.y - 0.5f) * referenceHeight);
    }

    

    [System.Serializable]
    public struct VelocityIndicator {
        public Image line;
        public Image head;

        public void Update (float angle, Vector2 pos, float magnitude, float arrowHeadSize, float thickness) {
            line.rectTransform.pivot = new Vector2 (0, 0.5f);
            line.rectTransform.eulerAngles = Vector3.forward * angle;
            line.rectTransform.localPosition = pos;
            line.rectTransform.sizeDelta = new Vector2 (magnitude, thickness);
            line.material.SetVector ("_Size", line.rectTransform.sizeDelta);

            head.rectTransform.localPosition = pos + (Vector2) line.rectTransform.right * magnitude;
            head.rectTransform.eulerAngles = Vector3.forward * angle;

            head.rectTransform.localScale = Vector3.one * arrowHeadSize;
        }

        public void SetActive (bool active) {
            line.gameObject.SetActive (active);
            head.gameObject.SetActive (active);
        }
    }
}