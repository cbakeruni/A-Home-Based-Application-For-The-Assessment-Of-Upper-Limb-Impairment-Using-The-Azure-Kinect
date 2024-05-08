using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Kettle : MonoBehaviour
{   
    public float current;
    public float max;
    float rate = 30f;
    [SerializeField] Slider slider;
    [SerializeField] private Rigidbody water;
    [SerializeField] Transform spawnpos;
    Vector2 v;
    float t;

    private void Start()
    {
        current = max;
        rate = Mathf.Lerp(20f, 40f, Flow.i.difficulty);
    }

    private void Update()
    {
        if (current <= 0)
        {
            Flow.KettleEmpty();
            enabled = false;
        }
        if(AngledDown())
        {
            t -= Time.deltaTime * rate;
            if(t <= 0f)
            {
                slider.value = 1 - current / max;
                var  rb = Instantiate(water, spawnpos.position + Random.insideUnitSphere * 0.01f, Quaternion.identity);
                v = Random.insideUnitCircle * 0.05f;
                rb.velocity = new Vector3(v.x, -0.8f, v.y);
                current--;
                t += 1;
            }
        }
    }

    //length of each vector * cosine angle between them = dot product
    private bool AngledDown()
    {
        float dot = Vector3.Dot(transform.up, Vector3.down);
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg ;
        float threshold = 100 + 40f * current / max;
        return angle < threshold;
    }
}