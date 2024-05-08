using System.Collections.Generic;
using UnityEngine;
using LightBuzz.Kinect4Azure;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEditor;
using System.IO;
using System;
public class KinectManager : MonoBehaviour
{
    [SerializeField] private Configuration _configuration;
    [SerializeField] private UniformImage _image;
    [SerializeField] private StickmanManager _stickmanManager;

    Vector3 elbow;
    Vector3 shoulder;

    public enum scene {Flex, Flash, Flow, Fortitude, Conformance}
    public static scene currentScene = scene.Flex;

    public bool imageOn = true;
    public bool stickmanOn = true;

    public static KinectManager i;

    public KinectSensor sensor;
    public static Frame frame = null;

    private void Awake()
    {
        sensor = null;
        frame = null;
        i = this;
        switch(SceneManager.GetActiveScene().name.ToLower())
        {
            case "flex":
                currentScene = scene.Flex;
                break;
            case "flash":
                currentScene = scene.Flash;
                break;
            case "flow":
                currentScene = scene.Flow;
                break;
            case "fortitude":
                currentScene = scene.Fortitude;
                break;
            case "conformance":
                currentScene = scene.Conformance;
                break;
        }
    }

    private void Start()
    {
        VoiceManager.AddWord("hello", () => Debug.Log("hi"));

        sensor = KinectSensor.Create(_configuration);

        if (sensor == null)
        {
            Debug.LogWarning("Sensor not connected!");
            return;
        }

        sensor.Open();
        if (imageOn)
        {
            _image.gameObject.SetActive(true);
        }
        if (stickmanOn)
        {
            _stickmanManager.Toggle(false);
        }
      
    }

    private void Update()
    {
        if (sensor == null || !sensor.IsOpen) return;

        frame = sensor.Update();

        if (frame != null)
        {
            if (frame.ColorFrameSource != null && imageOn)
            {
                _image.Load(frame.ColorFrameSource);
            }

            if (frame.BodyFrameSource != null && stickmanOn)
            {
                
                _stickmanManager.Load(frame.BodyFrameSource.Bodies);
                
            }

            List<Body> bodies = frame.BodyFrameSource?.Bodies;

            if (bodies.Count > 0)
            {
                if (HandManager.i[1].origin == Vector3.zero)
                {
                    Vector3 o = bodies[0].Joints[JointType.HandRight].Position;
                    HandManager.i[1].origin = new Vector3(-o.x, -o.y, o.z);
                    Vector3 o2 = bodies[0].Joints[JointType.HandLeft].Position;
                    HandManager.i[0].origin = new Vector3(-o2.x, -o2.y, o2.z);
                    return;
                }
                Vector3 v = bodies[0].Joints[JointType.HandRight].Position;
                HandManager.i[1].pos = new Vector3(-v.x, -v.y, v.z);
                Vector3 v2 = bodies[0].Joints[JointType.HandLeft].Position;
                HandManager.i[0].pos = new Vector3(-v2.x, -v2.y, v2.z);
                elbow = bodies[0].Joints[HandManager.currentInd == 0 ? JointType.ElbowLeft : JointType.ElbowRight].Position;
                elbow =  new Vector3(-elbow.x, -elbow.y, elbow.z);
                shoulder = bodies[0].Joints[HandManager.currentInd == 0 ? JointType.ShoulderLeft : JointType.ShoulderRight].Position;
                shoulder = new Vector3(-shoulder.x, -shoulder.y, shoulder.z);
            }
        }
    }

    private void OnDestroy()
    {
        sensor?.Close();
    }

    public static IEnumerator RecordMovement(System.Func<bool> check, System.Action<MovementData> onComplete)
    {
        List<Vector3> hs = new List<Vector3>();
        List<Vector3> es = new List<Vector3>();
        List<Vector3> ss = new List<Vector3>();
        while (!check.Invoke())
        {
            if (hs.Count == 0 || hs[^1] != HandManager.i[HandManager.currentInd].pos)
            {
                hs.Add(HandManager.i[HandManager.currentInd].pos);
                es.Add(i.elbow);
                ss.Add(i.shoulder);
            }
            yield return null;
        }
        onComplete.Invoke(new MovementData(hs, es, ss));
    }

    [System.Serializable]
    public struct MovementData
    {
        public List<Vector3> hands;
        public List<Vector3> elbows;
        public List<Vector3> shoulders;

        public MovementData(List<Vector3> hs, List<Vector3> es, List<Vector3> ss)
        {
            this.hands = hs;
            this.elbows = es;
            this.shoulders = ss;
        }
        public MovementData(MovementData orig, int startInd)
        {
            int n = orig.hands.Count - startInd;
            this.hands = orig.hands.GetRange(startInd, n);
            this.elbows = orig.elbows.GetRange(startInd, n);
            this.shoulders = orig.shoulders.GetRange(startInd, n);
        }

        public MovementData(MovementData orig,float rad)
        {
            List<Vector3> hands = orig.hands;
            for (int i = 1; i < hands.Count; i++)
            {
                hands[i] -= orig.hands[0];
                hands[i] /= rad;
            }
            List<Vector3> elbows = orig.elbows;
            for (int i = 1; i < elbows.Count; i++)
            {
                elbows[i] -= orig.hands[0];
                elbows[i] /= rad;
            }
            List<Vector3> shoulders = orig.shoulders;
            for (int i = 1; i < shoulders.Count; i++)
            {
                shoulders[i] -= orig.hands[0];
                shoulders[i] /= rad;
            }
            this.hands = hands;
            this.elbows = elbows;
            this.shoulders = shoulders;
        }
    }

    public static IEnumerator ScrollText(string text, TextMeshProUGUI textMesh, float time)
    {
        float elapsedTime = 0f;
        while (elapsedTime < time)
        {
            float completionPercentage = elapsedTime / time;
            int endIndex = Mathf.FloorToInt(completionPercentage * text.Length);
            textMesh.text = text.Substring(0, endIndex);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        textMesh.text = text;
    }

    public static void Write(string t, string path, bool replace = false)
    {
        path = Path.Combine(Application.persistentDataPath, path);
        using (StreamWriter writer = new StreamWriter(path,!replace))
        {
           writer.WriteLine(t);
        }
        Debug.Log("Data saved to " + path);
    }


    public static string[] Read(string path)
    {
        path = Path.Combine(Application.persistentDataPath, path);
        if (File.Exists(path))
        {
            return File.ReadAllLines(path);
        }
        else
        {
            Debug.LogError("File not found: " + path);
            return new string[0]; // Return an empty array if the file does not exist
        }
    }

    //Makes a new file for each movement data
    public static string WriteMovementData(MovementData mov, string path, bool sampling = false)
    {
        path = Path.Combine(Application.persistentDataPath, path);
        int i = 0;
        if (sampling)
        {
            while (true)
            {
                if (!File.Exists(path + i.ToString())) break;
                i++;
            }
            path += i.ToString();
        }
        using (StreamWriter writer = new StreamWriter(path))
        {
            for (i = 0; i < mov.hands.Count; i++)
            {
                writer.WriteLine(mov.hands[i].ToString() + " " + mov.elbows[i].ToString() + " " + mov.shoulders[i].ToString());
            }
        }
        return path;
    }
    public static MovementData ReadMovementData(string path)
    {
        path = Path.Combine(Application.persistentDataPath, path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The file specified does not exist.");
        }

        List<Vector3> hands = new List<Vector3>();
        List<Vector3> elbows = new List<Vector3>();
        List<Vector3> shoulders = new List<Vector3>();

        using (StreamReader reader = new StreamReader(path))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(' ');
                if (parts.Length == 9)
                {
                    hands.Add(ParseVector3(parts[0] + " " + parts[1] + " " + parts[2]));
                    elbows.Add(ParseVector3(parts[3] + " " + parts[4] + " " + parts[5]));
                    shoulders.Add(ParseVector3(parts[6] + " " + parts[7] + " " + parts[8]));
                }
            }
        }

        return new MovementData(hands, elbows, shoulders);
    }

    private static Vector3 ParseVector3(string str)
    {
        str = str.Trim(new char[] { '(', ')', ' '});
        string[] vals = str.Split(',');
        if (vals.Length != 3)
            throw new FormatException("Input string is not a valid Vector3 format.");

        return new Vector3(
            float.Parse(vals[0]),
            float.Parse(vals[1]),
            float.Parse(vals[2]));
    }
}
