using LightBuzz.Kinect4Azure;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Avatar = LightBuzz.Kinect4Azure.Avateering.Avatar;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
public class Flow : MonoBehaviour
{
    [SerializeField] private Avatar[] _avatars;

    [SerializeField] Kettle kettle;
    [SerializeField] GameObject prefab;
    [SerializeField] Mug mug;
    [SerializeField] Transform[] mugParent;
    [SerializeField] Transform otherKettleParent;

    [SerializeField] List<GameObject> fakeMugs;

    [SerializeField] HandCollider[] cols;
    private bool[] ready = new bool[2] { false, false };
    [SerializeField] StartBox[] boxes = new StartBox[2];

    public Slider mugSlider;

    private float max = 1000;

    public float difficulty = 0;
    int mugCount = 3;
    int placed = 0;
    public static Flow i;

    public Transform table;

    [SerializeField] TextMeshProUGUI txt;

    Vector3[] tablePositions = new Vector3[2] { new Vector3(-0.35f,0.16f,0.605f), new Vector3(-0.35f, -1.05f, 0.605f) };
    bool tableUp = false;

    private void Awake()
    {
        i = this;
        string[] prevScores = KinectManager.Read(Path.Combine(Application.persistentDataPath, "Flow"));
        difficulty = 0.01f * Mathf.Lerp(0, float.Parse(prevScores.Last()), prevScores.Length / 7);
        kettle.max = max;
        cols[0].triggered += () => HandReady(0);
        cols[1].triggered += () => HandReady(1);
        if(HandManager.currentInd == 1)
        {
            fakeMugs.Reverse();
        }

        for(int i = 2; i  >=  0; i--) //DIFFICULTY LEVEL
        {
            float size = 1f;
            fakeMugs[i].transform.localScale = new Vector3(20, 10, 7.14f) * size;
            fakeMugs[i].GetComponent<FakeMug>().size = size;
         
        }
        _avatars[0].SmoothDelta = Mathf.Lerp(0.12f,0.22f, difficulty);
    }

    private void HandReady(int ind)
    {
        boxes[ind].Shrink();
        ready[ind] = true;
        if (ready[0] ==  true && ready[1] == true)
        {
            StartCoroutine(StartScene());
        }
    }

    private IEnumerator StartScene()
    {
        cols[HandManager.currentInd].GetComponent<Collider>().enabled = false;
        yield return StartCoroutine(MoveTable(true));
        if(HandManager.currentInd == 1)
        {
            kettle.transform.parent = otherKettleParent;
            kettle.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        kettle.gameObject.SetActive(true);
    }

    public void MakeMug(GameObject fakeMug, float mugSize)
    {
        if (ready[0])
        {
            Vector3 pos = fakeMug.transform.localPosition;
            fakeMugs.Remove(fakeMug);
            Destroy(fakeMug);
            mug = Instantiate(prefab, mugParent[HandManager.currentInd]).GetComponentInChildren<Mug>(true);
            mug.ind = mugCount;
            mugCount++;
            mug.transform.parent.localPosition = Vector3.zero;
            mug.transform.parent.localRotation = Quaternion.identity;
            mug.transform.parent.localScale = 10f * Vector3.one * mugSize * 0.5f * (1 + difficulty);
            mug.transform.parent.gameObject.SetActive(true);
            mug.initPos = pos;
            ready[0] = false;
            StartCoroutine(MoveTable(false));
        }
    }
   

    void Update()
    {
        Frame frame = KinectManager.frame;
        if (frame == null || frame.BodyFrameSource == null) return;
        UpdateAvatars(frame.BodyFrameSource.Bodies);
    }

    private void UpdateAvatars(IList<Body> bodies)
    {
        if (bodies == null || bodies.Count == 0) return;
        if (_avatars == null || _avatars.Length == 0) return;

        Body body = bodies.Closest();

        foreach (Avatar avatar in _avatars)
        {
            avatar.Update(body);
        }
    }

    public static void KettleEmpty()
    {
        Debug.Log("Kettle Empty");
        i.StopScene();
    }

    public void MugFull(int ind)
    {
       StartCoroutine(MoveTable(true));
    }

    private IEnumerator MoveTable(bool up)
    {
        if (up && tableUp || !up && !tableUp) yield break;
        if (up)
        {
            fakeMugs.ForEach(x => x.SetActive(true));
        }
        tableUp = up;
        for(float t =  0f; t < 1f; t += Time.deltaTime)
        {
            table.transform.position = Vector3.Lerp(up? tablePositions[1] : tablePositions[0], up ? tablePositions[0] : tablePositions[1], t);
            yield return null;
        }
        if(!up)
        {
            fakeMugs.ForEach(x => x.SetActive(false));
        }
    }

    public void MugPlaced(GameObject g)
    {
        ready[0] = true;
        fakeMugs.Add(g);
        StartCoroutine(MakeScaleGood(g.transform.parent));
        placed++;
        if(placed == 3)
        {
            StartCoroutine(StopScene());
        }
    }

    IEnumerator MakeScaleGood(Transform t)
    {
        for(int i = 0; i < 100; i++)
        {
            yield return null;
            t.transform.localScale = new Vector3(20f, 10f, 7.14f);
        }
    }
    private IEnumerator StopScene()
    {
        Destroy(kettle.gameObject);
        yield return new WaitForSeconds(0.5f);
        int x = 0;
        foreach (Mug g in Mug.mugs)
        {
            x += g.collided;
        }
        float c = x /(float)(kettle.max - kettle.current);
        Debug.Log(c);
        float score = 50 * (1 + 1) * (0.25f * Mathf.Clamp01(1f - (Mathf.Max(Time.timeSinceLevelLoad, 44) - 44f) / 19f) + 0.75f * Mathf.Clamp01(1f - (75f - Mathf.Min(100 * c, 75)) / 26f));
        KinectManager.Write(score.ToString("F1"), Path.Combine(Application.persistentDataPath, "Flow"));
        yield return StartCoroutine(KinectManager.ScrollText("Your Flow score: " + score.ToString("F1"), txt, 2f));
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(2);
    }
}