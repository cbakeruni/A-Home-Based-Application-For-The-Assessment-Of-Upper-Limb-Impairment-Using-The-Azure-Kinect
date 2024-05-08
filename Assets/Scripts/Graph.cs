using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using static UnityEditor.VersionControl.Message;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class Graph : MonoBehaviour
{
    List<Line> lines = new List<Line>();
    public static Graph i;

    public float graphWidth = 10f;
    public float graphHeight = 10f;
    public int numXLabels = 10;
    public int numYLabels = 11;

    [SerializeField] TextMeshProUGUI text;

    public void AddLine(List<float> values, Color col)
    {
        lines.Add(new Line(values, col));
    }
    struct Line
    {
        public List<float> values;
        public Color col;
        public LineRenderer lr;

        public Line(List<float> values, Color col)
        {
            this.values = values;
            this.col = col;
            lr = Instantiate(Resources.Load<GameObject>("Line")).GetComponent<LineRenderer>();
            lr.positionCount = values.Count;
            lr.startColor = lr.endColor = col;
            lr.transform.SetParent(i.transform);
            lr.startWidth = 10f;
            lr.endWidth = 10f;
        }
    }

    private IEnumerator Start()
    {
        i = this;
        Time.timeScale = 1f;
        string[] flex  = KinectManager.Read(Path.Combine(Application.persistentDataPath, "Flex"));
        string[] flow = KinectManager.Read(Path.Combine(Application.persistentDataPath, "Flow"));
        string[] conf = KinectManager.Read(Path.Combine(Application.persistentDataPath, "Conformance"));
        string[] flash = KinectManager.Read(Path.Combine(Application.persistentDataPath, "Flash"));

        List<List<float>> lines =  new List<List<float>>() { new List<float>(), new List<float>(), new List<float>(), new List<float>() };

        foreach (string str in flex)
        {
            lines[0].Add(float.Parse(str));
        }

        foreach (string str in flow)
        {
            lines[1].Add(float.Parse(str));
        }

        foreach (string str in conf)
        {
            lines[2].Add(float.Parse(str));
        }

        foreach (string str in flash)
        {
            lines[3].Add(float.Parse(str));
        }

        AddLine(lines[0], Color.green);
        AddLine(lines[1], Color.blue);
        AddLine(lines[2], Color.yellow);
        AddLine(lines[3], Color.red);
        yield return StartCoroutine(DisplayGraph(5f));
        string txt = "Your arm movement has been sampled and compared to a healthy user database. As of today, your " +
            (HandManager.currentInd == 0 ? "left " : "right ") +  "arm’s range of motion is " + DataToString(lines[0][^1], 1f)  +
            ", the stability is " + DataToString(lines[1][^1],0.7014f) + ", the arm motion regularity is " + DataToString(lines[2][^1],0.8033f) +
            ", and the arm reactivity is " + DataToString(lines[3][^1],0.8237f) + ".";
        yield return KinectManager.ScrollText(txt, text, 7.5f);
        yield return new WaitForSeconds(2f);
        Application.Quit();
    }

    private string DataToString(float x, float scale)
    {
        x /= scale;
        switch(x)
        {
            case < 50f:
                return "severely limited";
            case < 75f:
                return "poor";
            case < 100f:
                return "under-average";
            default:
                return "healthy";
        }
    }
    public IEnumerator DisplayGraph(float displayTime)
    {
        float elapsedTime = 0f;
        Vector3[] lastPositions =  new Vector3[lines.Count];
        foreach (var line in lines)
        {
            line.lr.positionCount = 0;
        }
        while (elapsedTime < displayTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / displayTime;
            int lineInd = 0;
            foreach (var line in lines)
            {
                int targetNumPoints = Mathf.FloorToInt(line.values.Count * progress);
                if (line.lr.positionCount < targetNumPoints)
                {
                    line.lr.positionCount = targetNumPoints;
                    if (line.lr.positionCount > 0)
                    {
                        int newPointIndex = line.lr.positionCount - 1;
                        if (newPointIndex == 0)
                        {
                            // If it's the first point, initialize it directly to prevent undefined behavior.
                            lastPositions[lineInd] = InitializePoint(line, newPointIndex);
                        }
                        else
                        {
                            // Start new points at the position of the last visible point to avoid jumps.
                            line.lr.SetPosition(newPointIndex, lastPositions[lineInd]);
                        }
                    }
                }

                // Update points based on progress
                for (int i = 0; i < targetNumPoints; i++)
                {
                    float targetX = MapValue(i, 0, line.values.Count - 1, 100 -graphWidth / 2, graphWidth / 2 - 100f);
                    float targetY = MapValue(line.values[i], 0, 100, 50-graphHeight / 2, -50+graphHeight / 2);
                    Vector3 targetPosition = new Vector3(targetX, targetY);

                    //Interpolation
                    Vector3 currentPosition = line.lr.GetPosition(i);
                    Vector3 newPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * 10); // Adjust smoothing speed.
                    line.lr.SetPosition(i, newPosition);
                    if (i == line.lr.positionCount - 1)
                    {
                        lastPositions[lineInd] = newPosition; // Update the last position to the newest end point.
                    }
                }
                lineInd++;
            }
            yield return null;
        }
        FinalizeLinePositions();
    }

    private Vector3 InitializePoint(Line line, int index)
    {
        float x = MapValue(index, 0, line.values.Count - 1, 100 + -graphWidth / 2, graphWidth / 2 - 100f);
        float y = MapValue(line.values[index], 0, 100, 50 - graphHeight / 2, -50 + graphHeight / 2);
        Vector3 position = new Vector3(x, y);
        line.lr.SetPosition(index, position);
        return position;
    }

    private void FinalizeLinePositions()
    {
        foreach (var line in lines)
        {
            line.lr.positionCount = line.values.Count;
            for (int i = 0; i < line.values.Count; i++)
            {
                InitializePoint(line, i);
            }
        }
    }


    private float MapValue(float value, float fromSourceMin, float fromSourceMax, float toDestinationMin, float toDestinationMax)
    {
        return (value - fromSourceMin) / (fromSourceMax - fromSourceMin) * (toDestinationMax - toDestinationMin) + toDestinationMin;
    }


    /* public void SetupCanvas()
     {
         float xLabelSpacing = graphWidth / (numXLabels - 1);
         float yLabelSpacing = graphHeight / (numYLabels - 1);

         for (int i = 0; i < numXLabels; i++)
         {
             CreateTextLabel(i.ToString(), -graphWidth / 2 + i * xLabelSpacing, -graphHeight / 2 - 0.5f, 16);
         }

         for (int i = 0; i < numYLabels; i++)
         {
             CreateTextLabel((numYLabels - i - 1) * 10 + "", -graphWidth / 2 - 0.5f, -graphHeight / 2 + i * yLabelSpacing, 16);
         }

         for (int i = 0; i < numYLabels; i++)
         {
             GameObject line = new GameObject("HorizontalLine");
             line.transform.position = new Vector3(0, -graphHeight / 2 + i * yLabelSpacing);
             LineRenderer lr = line.AddComponent<LineRenderer>();
             lr.startColor = lr.endColor = new Color(0.75f, 0.75f, 0.75f, 0.5f);
             lr.startWidth = lr.endWidth = 0.01f;
             lr.SetPosition(0, new Vector3(-graphWidth / 2, 0));
             lr.SetPosition(1, new Vector3(graphWidth / 2, 0));
             line.transform.SetParent(transform, false);
         }
     }

     private TextMeshPro CreateTextLabel(string text, float x, float y, int fontSize)
     {
         GameObject gameObject = new GameObject("TMP_" + text, typeof(TextMeshPro));
         gameObject.transform.position = new Vector3(x, y);
         TextMeshPro textMesh = gameObject.GetComponent<TextMeshPro>();
         textMesh.text = text;
         textMesh.fontSize = fontSize;
         gameObject.transform.SetParent(transform, false);
         return textMesh;
     }*/

}