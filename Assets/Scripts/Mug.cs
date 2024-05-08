using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Mug : MonoBehaviour
{
    public int ind = 0;
    [SerializeField] Transform cylinder;
    [SerializeField] Transform col;
    public int collided = 0;
    public int maxCollided = 30;

    public Vector3 initPos;

    bool mugFull = false;
    [SerializeField] Slider slid;

    public static List<Mug> mugs =  new List<Mug>();

    private void Start()
    {
        if(!mugs.Contains(this))mugs.Add(this);
        slid = Flow.i.mugSlider;
        slid.transform.position -= new Vector3(0f, ind * 100f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (mugFull)
        {
            if (other.name.ToLower().Contains("table"))
            {
                ConvertToStatue();
            }
            return;
        }
        collided++;
        UpdateCylinder((float)collided / maxCollided);
        if (collided >= maxCollided)
        {
            Flow.i.MugFull(ind);
            mugFull = true;
        }
    }

    private void ConvertToStatue()
    {
        GetComponent<Collider>().enabled = false;
        Destroy(GetComponentInParent<Collider>());
        transform.parent.parent = Flow.i.table;
        transform.parent.localPosition = initPos;
        transform.parent.localScale = new Vector3(20f, 10f, 7f);
        transform.parent.localRotation = Quaternion.Euler(270f, 180f, 0);
        GetComponentInParent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        Flow.i.MugPlaced(gameObject);
        enabled = false;
    }

    private void UpdateCylinder(float t)
    {
        cylinder.localScale = new Vector3(0.0175f, t * 0.01f, 0.0175f);
        cylinder.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-0.0092f, 0.0001f, t));
        col.localPosition = new Vector3(0f, 0f, Mathf.Lerp(-0.0129f, 0.00356f, t));
        slid.value = (float)collided / maxCollided;
    }
}
