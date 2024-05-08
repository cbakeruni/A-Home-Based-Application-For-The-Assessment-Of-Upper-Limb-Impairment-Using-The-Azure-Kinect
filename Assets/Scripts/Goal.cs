using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Goal : MonoBehaviour, IHover
{
    [SerializeField] Image centre;
    [SerializeField] Image border;
    [SerializeField] Sprite[] sprites;
    [SerializeField] TextMeshProUGUI txt;
    public float depth;
    public bool prepared;
    public bool completed;
    public static float fadeSpeed = 2f;

    [SerializeField] SpriteRenderer sr;
    public int handIndex;

    public int order = -1;

    private void Awake()
    {
        border.alphaHitTestMinimumThreshold = 0.001f;
        centre.alphaHitTestMinimumThreshold = 0.001f;
    }

    private void Start()
    {   
        sr = Instantiate(sr);
        sr.transform.parent = Conformance.i.leftRightLines[HandManager.currentInd].transform;
        handIndex = HandManager.currentInd;
        sr.transform.localPosition = new Vector3(0,0, depth * 2.5f);
    }

    public IEnumerator OnHoverI()
    {
        border.color = Color.green;
        yield return null;
        border.sprite = sprites[2];
        while (centre.color.a < 1f)
        {
            centre.color += new Color(0, 0, 0, fadeSpeed *  Time.deltaTime);
            yield return null;
        }
        Conformance.i.goals.Remove(this);
        Destroy(sr.gameObject);
        prepared = false;
        completed = true;
        for(float t = 1f; t < 1.2f; t+= 3 * Time.deltaTime)
        {
            transform.parent.localScale = Vector3.one * Mathf.Sqrt(t);
            yield return null;
        }
        for(float t = 1.2f; t > 0; t -= 5 * Time.deltaTime)
        {
            transform.parent.localScale = Vector3.one * Mathf.Sqrt(t);
            yield return null;
        }
        Destroy(transform.parent.gameObject);
    }

    public IEnumerator OnUnhoverI()
    {
        border.color = Color.white;
        yield return null;
        if (border.sprite != sprites[0])
        {
            border.sprite = sprites[1];
        }
        while (centre.color.a > 0.1f)
        {
            centre.color -= new Color(0, 0, 0, fadeSpeed  * Time.deltaTime);
            yield return null;
        }
        centre.color = new Color(1, 1, 1, 0.1f);
    }

    public void OnExterior()
    {
        if (border.sprite != sprites[2])
        {
            border.sprite = sprites[1];
        }
    }

    public void OffExterior()
    {
        if (border.sprite != sprites[2])
        {
            border.sprite = sprites[0];
        }
    }

    public bool OnHover()
    {
        if (!prepared) return false;
        if (this != Conformance.i.goals[0]) return false;
        StopAllCoroutines();
        StartCoroutine(OnHoverI());
        return true;
    }

    public void OnUnhover()
    {
        if (!prepared) return;
        StopAllCoroutines();
        StartCoroutine(OnUnhoverI());
        
    }

    public void Prepare()
    {
        if (prepared) return;
        prepared = true;
        border.color = Color.white;
    }

    public void Deprepare()
    {
        if (!prepared) return;
        prepared = false;
        border.color = Color.red;
    }
}
