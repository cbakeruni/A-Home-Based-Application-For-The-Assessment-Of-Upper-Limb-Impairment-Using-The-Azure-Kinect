using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeMug : MonoBehaviour
{
    public float size = 1f;
    private void OnTriggerEnter(Collider other)
    {
        Flow.i.MakeMug(gameObject,size);
    }
}
