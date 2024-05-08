using LightBuzz;
using LightBuzz.Kinect4Azure;
using LightBuzz.Kinect4Azure.Avateering;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using static KinectManager;
using Avatar = LightBuzz.Kinect4Azure.Avateering.Avatar;
using System.IO;
using UnityEngine.SceneManagement;

public class Flex : MonoBehaviour
{
    [SerializeField] VideoClip[] vids;
    [SerializeField] VideoPlayer player;
    [SerializeField] AudioClip[] audios;
    [SerializeField] AudioSource source;
    [SerializeField] GameObject img;
    [SerializeField] TextMeshProUGUI txt;

    MovementData[] movements = new MovementData[2];
    public static bool stop;

    [SerializeField] private Avatar[] avatars;

    bool avatarOn = false;

    float r1;
    float r2;
    float e1;
    float e2;

    bool complete = false;

    private IEnumerator Start()
    {
        txt.text = "Say 'left' or 'right' to choose your dominant hand";
        VoiceManager.AddWord("left", () => { complete = true; HandManager.currentInd = 0; });
        VoiceManager.AddWord("right", () => { complete = true; HandManager.currentInd = 1; });
        while (!complete)
        {
            yield return null;
        }
        VoiceManager.RemoveWord("left");
        VoiceManager.RemoveWord("right");
        movements = new MovementData[2];
        if(HandManager.currentInd == 1) 
        {
            img.transform.localScale = new Vector3(-1f, 1f, 1f);
        }
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(RecordOneLength(0));
        yield return StartCoroutine(RecordOneLength(1));
        Resources.Load<BoundingBoxSO>("BoundingBox").Data(CalculateBoundingBox());

        r1 = Mathf.Clamp01(1f - (159f - Mathf.Min(r1, 159)) / 30f);
        r2 = Mathf.Clamp01(1f - (162f - Mathf.Min(r2, 162)) / 25f);
        e1 = Mathf.Clamp01(1f - (Mathf.Max(e1,38) - 38f) / 22f);
        e2 = Mathf.Clamp01(1f - (Mathf.Max(e2, 34) - 34f) / 20f);

        float score = 100f * (r1 + r2 + e1 + e2) / 4f;
        Write(score.ToString("F1"), Path.Combine(Application.persistentDataPath, "Flex"));

        txt.text = "You have completed the exercise";
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(1);
    }

    private void Update()
    {
        if (!avatarOn) return;
        Frame frame = KinectManager.frame;
        if (frame == null || frame.BodyFrameSource == null) return;
        UpdateAvatars(frame.BodyFrameSource.Bodies);
    }

    private void UpdateAvatars(IList<Body> bodies)
    {
        if (bodies == null || bodies.Count == 0) return;
        if (avatars == null || avatars.Length == 0) return;
        Body body = bodies.Closest();

        foreach (Avatar avatar in avatars)
        {
            avatar.Update(body);
        }
    }

    private IEnumerator RecordOneLength(int ind)
    {
        img.SetActive(true);
        //source.PlayOneShot(audios[ind+1]);
        player.clip = vids[ind];
        player.Play();
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(ScrollText("Observe the motion demonstrated", txt, 1f));
        while (player.time + 0.1f < vids[ind].length)
        {
            yield return null;
        }
        VoiceManager.AddWord("test", () => stop = true);
        Coroutine cor = StartCoroutine(ScrollText("When you are ready place your hands by your side, say 'Test' and then proceed to copy the motion. When you've finished your motion and your hand has reached as far up as it can go say the word 'Finish' with your hand still raised.", txt, 4f));
        float time = Time.time;
        stop = false;
        while (Time.time < time + 7f && stop == false)
        {
            yield return null;
        }
        StopCoroutine(cor);
        yield return null;
        avatarOn = true;
        img.SetActive(false);
        if (stop == false) { txt.text = "Say 'Test' to begin the exercise"; }
        while (stop == false)
        {
            yield return null;
        }
        txt.text = "Say 'Finish' when you have reached the top of your range of motion";
        VoiceManager.RemoveWord("test");
        stop = false;
        VoiceManager.AddWord("finish", () => stop = true);
        yield return StartCoroutine(RecordMovement(() => stop, ctx => movements[ind] = ctx));
        VoiceManager.RemoveWord("finish");

        avatarOn = false;
        if (!CheckDataIsValid(ind))
        {
            yield return StartCoroutine(ScrollText("Invalid data, please try again keeping your arm motion consistent with the demo.", txt, 3f));
            yield return new WaitForSeconds(1F);
            yield return StartCoroutine(RecordOneLength(ind));
            yield break;
        }
        float rom = CalculateRangeOfMotion(ind);
        float avBend = AverageElbowBend(ind);
        txt.text = "Range of Motion: " + rom.ToString("F1") + ". Average Elbow Bend:" + avBend.ToString("F1");
        if (ind == 0)
        {
            r1 = rom;
            e1 = avBend;
        }
        else
        {
            r2 = rom;
            e2 = avBend;
        }
        yield return new WaitForSeconds(2f);
    }

    //Check if the arm is consistently along same plane
    public bool CheckDataIsValid(int ind)
    {
        MovementData mov = movements[ind];
        Vector3 start = mov.hands[0];

        if (ind == 0)
        {
            // Check deviation in x for moving up towards the camera and up
            for (int i = 0; i < mov.hands.Count; i++)
            {
                if (Math.Abs(mov.hands[i].x - start.x) > 0.3f)
                {
                    return false;
                }
            }
        }
        else if (ind == 1)
        {
            // Check deviation in z for moving horizontally away and then up
            for (int i = 0; i < mov.hands.Count; i++)
            {
                if (Math.Abs(mov.hands[i].z - start.z) > 0.3f)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private Vector3 CalculateAveragePosition(List<Vector3> positions)
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 pos in positions)
        {
            sum += pos;
        }
        return sum / positions.Count;
    }

    public float CalculateRangeOfMotion(int ind)
    {
        MovementData mov = movements[ind];
        Vector3 averageShoulder = CalculateAveragePosition(mov.shoulders);
       
        Vector3 initialHand = mov.hands[0];
        Vector3 finalHand = mov.hands[mov.hands.Count - 1];

        Vector3 initialVector = initialHand - averageShoulder;
        Vector3 finalVector = finalHand - averageShoulder;
      
        Vector2 v1;
        Vector2 v2;
        if(ind == 0)
        {
            v1 = new Vector2(initialVector.y, initialVector.z);
            v2 = new Vector2(finalVector.y, finalVector.z);
        }
        else
        {
            v1 = new Vector2(initialVector.x, initialVector.y);
            v2 = new Vector2(finalVector.x, finalVector.y);
        }

        float dotProduct = Vector2.Dot(v1.normalized, v2.normalized);
        float angle = Mathf.Acos(dotProduct); 

        return angle * (180f / Mathf.PI);
    }

    private float CalculateAngle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 cb = b - c;

        float dotProduct = Vector3.Dot(ab, cb);
        float cosTheta = dotProduct / (ab.magnitude * cb.magnitude);
        return Mathf.Acos(cosTheta) * Mathf.Rad2Deg; 
    }

    public float AverageElbowBend(int ind)
    {
        MovementData mov = movements[ind];
        float totalAngle = 0f;
        int count = mov.hands.Count;

        for (int i = 0; i < count; i++)
        {
            float angle = CalculateAngle(mov.shoulders[i], mov.elbows[i], mov.hands[i]);
            totalAngle += angle;
        }

        return Mathf.Abs(180f - totalAngle / count); 
    }


    (Vector3,float) CalculateBoundingBox()
    { 
        Vector3 bottom = (movements[0].hands[0] + movements[1].hands[0]) / 2f;
        Vector3 forwards = Vector3.positiveInfinity;
        foreach(Vector3 v in movements[0].hands)
        {
            if(v.z < forwards.z)
            {
                forwards = v;
            }
        }
        Vector3 sideways = movements[1].hands[0];
        foreach (Vector3 v in movements[1].hands)
        {
            if(Mathf.Abs(v.x - movements[1].hands[0].x) > Mathf.Abs(sideways.x - movements[1].hands[0].x))
            {
                sideways = v;
            }
        }
        Vector3 top = (movements[0].hands[^1] + movements[1].hands[^1]) / 2f;
        Vector3 middlePoint =  (bottom + top) / 2f;
        float radius = Mathf.Min(Vector3.Distance(middlePoint, forwards), Vector3.Distance(middlePoint, sideways));
        return (middlePoint, radius);
    }
}