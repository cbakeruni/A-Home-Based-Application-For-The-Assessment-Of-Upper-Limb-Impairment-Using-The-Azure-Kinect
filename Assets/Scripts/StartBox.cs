using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartBox : MonoBehaviour
{

    [SerializeField] Vector3 size;
    [SerializeField] Vector3 endPos;
    public IEnumerator Start()
    {
        Vector3 delta = size - transform.localScale;
        yield return new WaitForSeconds(1.5F);
        for(float  t = 0f; t  <4;  t+=Time.deltaTime)
        {
            transform.position = Vector3.Lerp(transform.position, endPos, Time.deltaTime * t);
            transform.localScale += 0.25F * Time.deltaTime * delta;
            yield return null;
        }
    }
    public void Shrink()
    {
        GetComponent<Collider>().enabled = false;
        StartCoroutine(IShrink());
    }
    public IEnumerator IShrink()
    {
        for (float t = 1f; t < 2f; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * t * t * t);
            yield return null;
        }
        Destroy(gameObject);
    }

}