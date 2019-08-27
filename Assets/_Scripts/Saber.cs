﻿using UnityEngine;
using VRTK;
using DG.Tweening;

public class Saber : MonoBehaviour
{
    public LayerMask layer;
    private Vector3 previousPos;
    private Slice slicer;

    private float impactMagnifier = 120f;
    private float collisionForce = 0f;
    private float maxCollisionForce = 4000f;
    private VRTK_ControllerReference controllerReference;

    private ScoreHandling scoreHandling;
    private AudioHandling audioHandling;

    private void Start()
    {
        slicer = GetComponentInChildren<Slice>(true);
        var controllerEvent = GetComponentInChildren<VRTK_ControllerEvents>(true);
        if (controllerEvent != null && controllerEvent.gameObject != null)
        {
            controllerReference = VRTK_ControllerReference.GetControllerReference(controllerEvent.gameObject);
        }

        scoreHandling = GameObject.FindGameObjectWithTag("ScoreHandling").GetComponent<ScoreHandling>();
        audioHandling = GameObject.FindGameObjectWithTag("AudioHandling").GetComponent<AudioHandling>();
    }

    private float Pulse()
    {
        var hapticStrength = 0f;

        if (VRTK_ControllerReference.IsValid(controllerReference))
        {
            collisionForce = VRTK_DeviceFinder.GetControllerVelocity(controllerReference).magnitude * impactMagnifier;
            hapticStrength = collisionForce / maxCollisionForce;
            VRTK_ControllerHaptics.TriggerHapticPulse(controllerReference, hapticStrength, 0.5f, 0.01f);
        }
        else
        {
            var controllerEvent = GetComponentInChildren<VRTK_ControllerEvents>();
            if (controllerEvent != null && controllerEvent.gameObject != null)
            {
                controllerReference = VRTK_ControllerReference.GetControllerReference(controllerEvent.gameObject);
            }
        }

        return hapticStrength;
    }

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 1f, layer))
        {
            float hapticStrength = 0f;

            if (VRTK_ControllerReference.IsValid(controllerReference))
            {
                collisionForce = VRTK_DeviceFinder.GetControllerVelocity(controllerReference).magnitude * impactMagnifier;
                hapticStrength = collisionForce / maxCollisionForce;
            }

            if (hapticStrength > 0.05f)
            {
                if (!string.IsNullOrWhiteSpace(hit.transform.tag) && hit.transform.CompareTag("CubeNonDirection"))
                {
                    if (Vector3.Angle(transform.position - previousPos, hit.transform.up) > 130 ||
                        Vector3.Angle(transform.position - previousPos, hit.transform.right) > 130 ||
                        Vector3.Angle(transform.position - previousPos, -hit.transform.up) > 130 ||
                        Vector3.Angle(transform.position - previousPos, -hit.transform.right) > 130)
                    {
                        SliceObject(hit.transform);
                    }
                }
                else
                {
                    if (Vector3.Angle(transform.position - previousPos, hit.transform.up) > 130)
                    {
                        SliceObject(hit.transform);
                    }
                }
            }
        }
        
        previousPos = transform.position;
    }

    private void SliceObject(Transform hittedObject)
    {
        var cutted = slicer.SliceObject(hittedObject.gameObject);
        var go = Instantiate(hittedObject.gameObject);

        var cubeHandling = hittedObject.gameObject.GetComponent<CubeHandling>();
        var cutDirection = cubeHandling._note.CutDirection;
        var lineIndex = cubeHandling._note.LineIndex;
        var lineLayer = cubeHandling._note.LineLayer;
        go.GetComponent<CubeHandling>().enabled = false;
        go.GetComponentInChildren<BoxCollider>().enabled = false;
        go.layer = 0;

        foreach (var renderer in go.transform.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = false;
        }

        foreach (var cut in cutted)
        {
            cut.transform.SetParent(go.transform);
            cut.AddComponent<BoxCollider>();
            var rigid = cut.AddComponent<Rigidbody>();
            rigid.useGravity = true;
            cut.transform.DOScale(0, 1f);
        }

        var audioSource = go.AddComponent<AudioSource>();
        audioSource.volume = 0.13f;
        audioSource.clip = audioHandling.GetAudioClip(cutDirection);
        audioSource.loop = false;
        audioSource.pitch = PitchValue(lineLayer);

        go.transform.SetPositionAndRotation(hittedObject.position, hittedObject.rotation);

        var strength = Pulse();
        AddPointsToScore(strength);

        Destroy(hittedObject.gameObject);
        audioSource.Play();
        Destroy(go, 2f);
    }

    private float PitchValue(int lineLayer)
    {
        var pitch = 1.0f;

        if (lineLayer == 0)
        {
            pitch = 0.5f;
        }
        else if (lineLayer == 2)
        {
            pitch = 1.5f;
        }

        return pitch;
    }

    private void AddPointsToScore(float strength)
    {
        scoreHandling.IncreaseScore(System.Convert.ToInt32(10 + (strength * 100)));
        scoreHandling.IncreaseComboHits();
    }
}
