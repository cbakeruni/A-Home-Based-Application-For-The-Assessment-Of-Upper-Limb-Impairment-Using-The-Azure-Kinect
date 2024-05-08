using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

public class HandCollider : MonoBehaviour
{
    [SerializeField] string[] targets;

    public Action triggered;
    public bool turnOff = true;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.name);
        if (Array.IndexOf(targets, other.name.ToLower()) != -1)
        {
            other.attachedRigidbody.transform.SetParent(transform);
            if(turnOff) enabled = false;
            triggered?.Invoke();
        }
    }
}