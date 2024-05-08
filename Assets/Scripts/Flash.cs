using LightBuzz.Kinect4Azure;
using LightBuzz.Kinect4Azure.Avateering;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Flash : MonoBehaviour
{
    //4 ZONES and neutral
    
    float[] zoneStats = new float[4];
    [SerializeField] List<Bop> bops;
    float t;
    float prepTime = 1.25f;

    [SerializeField] HandCollider[] col;
    private bool ready = false;
    [SerializeField] StartBox[] box;

    [SerializeField] Renderer[] renderers;

    Transform hand;
    [SerializeField] Transform[] hands;

    int[] n = new int[4];
    int[] hit = new int[4];
    [SerializeField] TextMeshProUGUI accuracyText;
    [SerializeField] TextMeshProUGUI scoreText;

    bool closeFadeIenumerator = false;

    [SerializeField] Color[] cols;

    [SerializeField] LightBuzz.Kinect4Azure.Avateering.Avatar[] avatars;

    [SerializeField]  NoteGenerator note;

    BoundingBoxSO boxSO;

    //Personalisation
    float difficulty = 1f;

    private void Awake() //Randomly transforms bop groups to 0-3.
    {
        string[] prevScores = KinectManager.Read(Path.Combine(Application.persistentDataPath,"Flash"));
        difficulty = 0.01f * Mathf.Lerp(0,float.Parse(prevScores.Last()),prevScores.Length/7);
        List<int> gs = new List<int>();
        foreach (Bop b in bops)
        {
            if (!gs.Contains(b.group))
            {
                gs.Add(b.group);
            }
        }
        gs = gs.OrderBy(x => Random.Range(0, 100)).ToList();
        Dictionary<int, int> map = new Dictionary<int, int>();
        map.Add(gs[0], 0);
        map.Add(gs[1], 1);
        map.Add(gs[2], 2);
        map.Add(gs[3], 3);
        for (int i = 4; i < gs.Count; i++)
        {
            map.Add(gs[i], Random.Range(0, 4));
        }
        for (int i = 0; i < bops.Count; i++)
        {
            bops[i] = new Bop(bops[i].time, map[bops[i].group]); 
        }
    }

    private void Start()
    {
        col[HandManager.currentInd].triggered += () => HandReady();
        hand = hands[HandManager.currentInd];
        box[HandManager.currentInd].gameObject.SetActive(true);
        boxSO = Resources.Load<BoundingBoxSO>("BoundingBox");
        Time.timeScale = Mathf.Lerp(1f,1.5f,difficulty);
        foreach(Renderer r in renderers)
        {
            r.transform.position += Random.insideUnitSphere * 0.2f;
        }
    }

    private void HandReady()
    {
        box[HandManager.currentInd].Shrink();
        StartCoroutine(StartScene());
    }

    IEnumerator StartScene()
    {
        yield return new WaitForSeconds(1f);
        ready = true;
    }   

    private void Update()
    {
        //Visualise the 'bops' and compute how succesful the 'bop' was
        Frame frame = KinectManager.frame;
        if (!(frame == null || frame.BodyFrameSource == null)) 
        {
            UpdateAvatars(frame.BodyFrameSource.Bodies);
        }
       
        if (!ready) return;
        foreach(Bop b  in bops)
        {
            if(b.time > t && b.time <= Time.time)
            {
                Beep(b.group);
            }
            else if(b.time > t + prepTime && b.time <= Time.time + prepTime)
            {
                note.PlayNote(b.group);
                Visualise(b.group);
            }
        }
        t = Time.time;
        if(t > bops.Last().time + 3f)
        {
            StartCoroutine(FinishScene());
            ready = false;
        }
    }

    private void UpdateAvatars(IList<Body> bodies)
    {
        Body body = bodies.Closest();
        foreach (LightBuzz.Kinect4Azure.Avateering.Avatar avatar in avatars)
        {
            avatar.Update(body);
        }
    }

    private void Beep(int ind)
    {
        n[ind]++;
        Renderer r = renderers.OrderBy(x => Vector3.Distance(x.transform.position, hand.position)).First();
        LightUp(r, renderers[ind]);
        if (r != renderers[ind])
        {
            FadeText("Miss", Color.red);
            return;
        }
        else
        {
            FadeText("Hit", Color.green);
            hit[ind]++;
            DetermineAccuracy(ind, r.transform);
        }
    }

    void DetermineAccuracy(int ind, Transform t)
    {
        float distance = Vector3.Distance(t.position, hand.position);
        float diffDelta = 0.5f - 0.25f * difficulty;
        if(distance < diffDelta)
        {
            StartCoroutine(CheckReturn(ind, 1));
        }
        else
        {
            StartCoroutine(CheckReturn(ind, 1 - Mathf.InverseLerp(diffDelta, 2*diffDelta, distance)));
        }
       
    }

    void LightUp(Renderer r, Renderer correct)
    {
        if (r == correct)
        {
            r.material.color = cols[1];
        }
        else
        {
            r.material.color = cols[3];
            StartCoroutine(ReturnToRegular(r));
        }
    }

    void FadeText(string  s, Color col)
    {
        accuracyText.text = s;
        accuracyText.color = col;
        StartCoroutine(FadeTextI(col));
    }

    IEnumerator FadeTextI(Color  col)
    {
        for(float i = 0f; i < 0.5f; i += Time.deltaTime) //wait for 0.5 seconds and interrupt other instances of this
        {
            closeFadeIenumerator = true;
            accuracyText.color = col;
            yield return null;
        }
        closeFadeIenumerator = false;
        for(float i  = 0f; i  < 1f; i += Time.deltaTime * 0.5f)
        {
            yield return null;
            if (closeFadeIenumerator)
            {
                closeFadeIenumerator = false;
                yield break;
            }
            accuracyText.color = Color.Lerp(col, Color.clear, i);
        }
    }

    //Check that the hand  returns to within 0.3m of  origin position  (so.centralPosition) within a certain time frame.
    //Record the directenss, and the time. 
    private IEnumerator CheckReturn(int ind, float distanceParam)
    {
        float maxT = Time.time + 2f - difficulty;
        while(Time.time > maxT)
        {
            if (Vector3.Distance(hand.position,boxSO.centrePoint) < 0.35f)
            {
                break;
            }
        }
        float returnScore = Mathf.Clamp01(2 * (maxT - Time.deltaTime)/(2f  -  difficulty));
        zoneStats[ind] += 0.5f * (distanceParam  + returnScore);
        scoreText.text = zoneStats[ind].ToString();
        yield return null;
    }

    private void Visualise(int ind)
    {
        renderers[ind].material.color = Color.Lerp(cols[2], cols[0], 0.5f);
        StartCoroutine(VisualiseRamp(ind));
    }

    private IEnumerator VisualiseRamp(int ind)
    {
        for(float t  =  0f; t < prepTime - Time.deltaTime*2; t += Time.deltaTime)
        {
            renderers[ind].material.color = Color.Lerp(renderers[ind].material.color, cols[0], 5f * Time.deltaTime / prepTime);
            yield return null;
        }
     
        StartCoroutine(ReturnToRegular(ind));
    }

    private IEnumerator ReturnToRegular(int ind)
    {
        yield return new WaitForSeconds(0.5f);
        renderers[ind].material.color = cols[2];
    }

    private IEnumerator ReturnToRegular(Renderer rend)
    {
        yield return new WaitForSeconds(0.5f);
        rend.material.color = cols[2];
    }
    private IEnumerator FinishScene()
    {
        float precision = 0;
        int tot = 0;
        int h = 0;
        for (int i = 0; i < 4; i++)
        {
            zoneStats[i] /= n[i];
            precision += zoneStats[i];
            tot += n[i];
            h += hit[i];
            renderers[i].material.color = Color.Lerp(Color.red,Color.green, zoneStats[i]);
        }
        precision /= 4f;
        if(precision < 0.5f)
        {
            StartCoroutine(KinectManager.ScrollText("Remember to bring your hand to the starting position after hitting a target!", scoreText, 3f));
            yield return new WaitForSeconds(3f);
        }
        float score = 50f * (1 + difficulty) * precision * (float)h / tot;
        StartCoroutine(KinectManager.ScrollText("Your Flash score: " + score.ToString("F1"), scoreText, 3f));
        KinectManager.Write(score.ToString("F1"), Path.Combine(Application.persistentDataPath, "Flash"));
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(4);
    }

    [System.Serializable]
    public struct Bop
    {
        public float time;
        public int group;

        public Bop(float time, int group)
        {
            this.time = time;
            this.group = group;
        }
    }
}