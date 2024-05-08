using LightBuzz.Kinect4Azure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Conformance : MonoBehaviour 
{
    [SerializeField] bool sample;
    [SerializeField] List<Sequence> sequences;
    int sequenceInd = -1;

    [SerializeField] GameObject goalPrefab;
    Vector3[] positions  = new Vector3[3];
    Vector3[] worldSpaces = new Vector3[3];
    public List<Goal> goals = new List<Goal>();
    const float LOCK_THRESHOLD = 0.2f;

    [SerializeField] RectTransform handTracker;

    public GameObject[] leftRightLines; //contains the left and right tracking guidelines
    [SerializeField] SpriteRenderer[] handIndicators;

    public static Conformance i;

    public KinectManager.MovementData[] movedata = new KinectManager.MovementData[3];

    [SerializeField] Camera cam;
    List<IHover> hovers = new List<IHover>();
    List<IHover> buffer = new List<IHover>();
    [SerializeField] Canvas kinectCanvas;
    [SerializeField] Canvas c;
    [SerializeField] RectTransform CRect;
    [SerializeField] GraphicRaycaster gr;
    [SerializeField] EventSystem EventSystem;
    [SerializeField] TextMeshProUGUI txt;

    public Transform handPositioner;
    [SerializeField] StickmanManager sm;
    [SerializeField] Graph graph;

    float[] sequenceScores =  new float[5];

    BoundingBoxSO SO;

    private void Awake()
    {
        SO = Resources.Load<BoundingBoxSO>("BoundingBox");
        i = this;
        StickmanManager.i = sm;
        sequenceInd = 0;
        leftRightLines[HandManager.currentInd].SetActive(true);
    }

    private IEnumerator Start()
    {
        positions = sequences[sequenceInd].pos; //select the position combo
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(StickmanManager.i.SetFullBody(false));//waits for skele to exist then sets joints correctly AND sets conformance hand transform.
        yield return new WaitForSeconds(1f);
        RefreshSystem();
        yield return WaitForStart();
        Goal x = goals[^1];
        Debug.Log(x.gameObject, x.gameObject);
        yield return StartCoroutine(KinectManager.RecordMovement(() => x.completed, ctx => movedata[0] = ctx));
        yield return new WaitForSeconds(3f);
        RefreshSystem();
        yield return WaitForStart();
        x = goals[^1];
        yield return StartCoroutine(KinectManager.RecordMovement(() => x.completed, ctx => movedata[1] = ctx));
        yield return new WaitForSeconds(3f);
        RefreshSystem();
        yield return WaitForStart();
        x = goals[^1];
        yield return StartCoroutine(KinectManager.RecordMovement(() => x.completed, ctx => movedata[2] = ctx));
        yield return new WaitForSeconds(3f);
        TrimMoveData(); //Removes first few frames of static movement (if any) to ensure consistent start
        TransformMoveData(); //Centres data around starting position and accounts for radius of arm
        if (sample)
        {
            print(KinectManager.WriteMovementData(movedata[^1], sequenceInd + "Conformance" + (HandManager.currentInd == 0 ? "Left" : "Right") + "Sample", true));
        }
        else
        {
            sequenceScores[sequenceInd] = CalculateScore();
            yield return new WaitForSeconds(2f);
            txt.text = "";
        }
        yield return new WaitForSeconds(0.5f);
        sequenceInd++;
        if (sequenceInd < sequences.Count)
        {
            StartCoroutine(Start());
        }
        float av = 0f;
        for (int i = 0; i < 5; i++)
        {
            av += sequenceScores[i];
        }
        av /= 5f;
        KinectManager.Write(av.ToString(),Path.Combine(Application.persistentDataPath, "Conformance"));
        yield return StartCoroutine(KinectManager.ScrollText("Your average score was " + av.ToString("F0") + " / 100",txt,3f));
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(3);
    }

    private IEnumerator WaitForStart()
    {
        Goal g = goals[0];
        while (g.completed == false)
        {
            yield return null;
        }
    }

    private void TrimMoveData() //Start the movement from 3 frames in the correct direction occurs
    {
        Vector3 correct = worldSpaces[1] - worldSpaces[0];
        for (int i  = 0; i < 3; i++)
        {
            int trim = -1;
            for (int prev = 0; prev < Mathf.Min(60, movedata[i].hands.Count - 5); prev++)
            {
                Vector3 dir = movedata[i].hands[prev + 1] - movedata[i].hands[prev];
                if(Vector3.Angle(correct,dir) < 30f)
                {
                    dir = movedata[i].hands[prev + 3] - movedata[i].hands[prev];
                    if (Vector3.Angle(correct, dir) < 30f)
                    {
                        dir = movedata[i].hands[prev + 5] - movedata[i].hands[prev];
                        if (Vector3.Angle(correct, dir) < 30f)
                        {
                            trim = prev;
                            break;
                        }
                    }
                }
            }
            if(trim == -1)
            {
                Debug.LogWarning("no consistent start motion found within first 60 frames");
                continue;
            }
            movedata[i] = new KinectManager.MovementData(movedata[i], trim);
        }
    }

    private void TransformMoveData()
    {
        for(int i  = 0; i < 3; i++)
        {
            movedata[i] = new KinectManager.MovementData(movedata[i],SO.radius);
        }
    }

 
    private void RefreshSystem()
    {
        goals = new List<Goal>();
        for(int i = 0; i < 3; i++)
        {
            goals.Add(Instantiate(goalPrefab, PositionToCanvas(positions[i],i), Quaternion.identity, c.transform).GetComponentInChildren<Goal>());
            goals[^1].order = i;
            goals[^1].depth = positions[i].z;
        }
    }
    
    private float OneRemap(float x)
    {
        return Mathf.Lerp(-1f,1f,x);
    }

    private Vector3 PositionToCanvas(Vector3 v, int ind)
    {
        //y is to be between SO.centrePoint.y - SO.radius and SO.centrePoint.y + SO.radius
        //x is to be  between SO.centrePoint.x - SO.raius and SO.centrePoint.x + SO.radius
        //We need a world space to screen space transform that takes into account the goal's depth and positioning

        //world space
        Vector3 worldSpace = SO.centrePoint + SO.radius * new Vector3(OneRemap(v.x),OneRemap(v.y), -v.z);
        worldSpaces[ind] = worldSpace;
        return cam.WorldToScreenPoint(worldSpace);
    }

    void Update()
    {
        PlaceHandTracker();
        ProcessGoals();
        DoHovers();
        DepthIndicators();
    }

    private void PlaceHandTracker()
    {
        if (handPositioner == null)
        {
            return;
        }
        //hand's position is in world space, so we need to convert it to screen space in the KinectCanvas, and then that can be applied to the handTracker's position (on the other canvas)

        Vector3 handPosition = cam.WorldToScreenPoint(handPositioner.position);
        Vector2 anchoredPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(c.transform as RectTransform, handPosition, c.worldCamera, out anchoredPosition);
        handTracker.anchoredPosition = anchoredPosition;
        handTracker.Rotate(Vector3.forward, Time.deltaTime * 180f);
    }

    private void DoHovers()
    {
        buffer.Clear();
        PointerEventData ped = new PointerEventData(EventSystem);
        ped.position = handTracker.position;
        List<RaycastResult> results = new List<RaycastResult>();
        gr.Raycast(ped, results);
        foreach (RaycastResult result in results)
        {
            IHover hover = result.gameObject.GetComponent<IHover>();
            if (hover != null)
            {
                buffer.Add(hover);
                if (!hovers.Contains(hover))
                {
                    if (hover.OnHover())
                    {
                        hovers.Add(hover);
                    }
                }
            }
        }
        for (int i = 0; i < hovers.Count; i++)
        {
            if (!buffer.Contains(hovers[i]))
            {
                hovers[i].OnUnhover();
                hovers.RemoveAt(i);
                i--;
            }
        }
    }

    void ProcessGoals()
    {
        float depth = -(HandManager.i[HandManager.currentInd].pos.z - SO.centrePoint.z) / SO.radius;
        foreach (Goal g in goals)
        {
            float delta = depth - g.depth;
            if (Mathf.Abs(delta) < LOCK_THRESHOLD)
            {
                g.Prepare();
                delta = 0f;
            }
            else
            {
                g.Deprepare();
                delta = Mathf.Sign(delta) * (Mathf.Abs(delta) - LOCK_THRESHOLD) / (1f - LOCK_THRESHOLD);
            }
            g.transform.parent.localScale = (1f - Mathf.Min(0.8f, Mathf.Abs(delta))) * Vector3.one; //WARP TO SIGNIFY DEPTH
            g.transform.parent.localRotation = Quaternion.Euler(Mathf.Max(0, 90f * delta), Mathf.Max(0, -90f * delta), 0);
        }
    }
    private void DepthIndicators()
    {
        handIndicators[0].transform.localPosition = new Vector3(0, 0, -2.5f * (HandManager.i[0].pos.z - SO.centrePoint.z) / SO.radius);
        handIndicators[1].transform.localPosition = new Vector3(0, 0, -2.5f * (HandManager.i[1].pos.z - SO.centrePoint.z) / SO.radius);
    }

    private float CalculateScore()
    {
        KinectManager.MovementData ideal = KinectManager.ReadMovementData(sequenceInd.ToString()  + "Conformance" + (HandManager.currentInd == 0 ? "Left" : "Right") + "Ideal");
        float score = 0f;
        int bestInd = -1;
        for (int i = 0; i < 3; i++) 
        {
            int nIdeal = ideal.hands.Count;
            int n = movedata[i].hands.Count;

            float timeScore = (float)n / nIdeal; //0.5 - 1 - 4
            timeScore = Mathf.Abs(1 - Mathf.Sqrt(timeScore));  // 0.3 - 0 - 1
            timeScore = Mathf.Lerp(1f, 0f, timeScore); // 0.7 - 1 - 0
            
            //Adjust the trajectories to be of the same length. Whichever is bigger is chosen.
            if (nIdeal > n)
            {
                movedata[i] = new KinectManager.MovementData(
                    InterpolateList(movedata[i].hands, nIdeal), 
                    InterpolateList(movedata[i].elbows, nIdeal), 
                    InterpolateList(movedata[i].shoulders, nIdeal));
            }
            else if(n > nIdeal)
            {
                ideal = new KinectManager.MovementData(
                    InterpolateList(ideal.hands, n),
                    InterpolateList(ideal.elbows, n),
                    InterpolateList(ideal.shoulders, n));
            }

            float angleScore = CalculateDifferenceInElbowAngles(
                ideal.shoulders, ideal.elbows, ideal.hands,
                movedata[i].shoulders, movedata[i].elbows, movedata[i].hands);
            angleScore = 1f - Mathf.InverseLerp(12.5f, 40f, angleScore);

            float trajectoryScore = CalculateDifferenceInTrajectories(ideal.hands, movedata[i].hands); //0.1f is good, 100%
            trajectoryScore = Mathf.Clamp01(1 - 1.25f*Mathf.Sqrt(Mathf.Max(0,trajectoryScore - 0.1f)));

            float newScore = trajectoryScore * 35f + timeScore * 35f + angleScore * 30f;

            Debug.Log("Attempt number " + i + ": Trajectory Score: " + trajectoryScore + " Angle Score: " + angleScore + " Time Score: " + timeScore + " Total Score /100: " + newScore);
            
            if(newScore  > score)
            {
                score = newScore;
                bestInd = i;
            }
        }
        txt.text = "Your best attempt was attempt number " + (bestInd + 1).ToString() + " with a score of " + score.ToString("F0") + " / 100";
        return score;
    }
    
  

    public List<Vector3> InterpolateList(List<Vector3> original, int targetLength)
    {
        List<Vector3> result = new List<Vector3>();
        for (int i = 0; i < targetLength; i++)
        {
            float t = i / (float)(targetLength - 1);
            int originalIndex = (int)(t * (original.Count - 1));
            int nextIndex = Math.Min(originalIndex + 1, original.Count - 1);
            float localT = (t * (original.Count - 1)) - originalIndex;
            result.Add(Vector3.Lerp(original[originalIndex], original[nextIndex], localT));
        }
        return result;
    }

    public float CalculateDifferenceInTrajectories(List<Vector3> ideal, List<Vector3> traj)
    {
        float totalDistance = 0;
        for (int i = 1; i < ideal.Count; i++)
        {
            totalDistance += Vector3.Distance(ideal[i], traj[i]);
        }
        return totalDistance / traj.Count;
    }

    public float CalculateDifferenceInElbowAngles(List<Vector3> shoulders1, List<Vector3> elbows1, List<Vector3> hands1, List<Vector3> shoulders2, List<Vector3> elbows2, List<Vector3> hands2)
    {
        float totalAngleDifference = 0;
        for (int i = 1; i < shoulders1.Count; i++)
        {
            float angle1 = CalculateAngle(shoulders1[i], elbows1[i], hands1[i]);
            float angle2 = CalculateAngle(shoulders2[i], elbows2[i], hands2[i]);
            totalAngleDifference += Math.Abs(angle1 - angle2);
        }
        return totalAngleDifference / shoulders1.Count;
    }
    private float CalculateAngle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 cb = b - c;
        float dotProduct = Vector3.Dot(ab, cb);
        float cosTheta = dotProduct / (ab.magnitude * cb.magnitude);
        return Mathf.Acos(cosTheta) * (180f / Mathf.PI);
    }

    [Serializable]
    public struct Sequence
    {
        public Vector3[] pos;
        public Sequence(Vector3[] vs)
        {
            pos = vs;
        }
    }
}