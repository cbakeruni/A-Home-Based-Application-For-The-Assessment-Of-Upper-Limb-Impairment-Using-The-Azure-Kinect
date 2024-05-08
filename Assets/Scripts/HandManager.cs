using UnityEngine;

public class HandManager : MonoBehaviour
{
    public Vector3 origin = Vector3.zero;
    public Vector3 pos = new Vector3(0f, 0f, 1f);
    public Vector3 dimDeltas = new Vector3(0.65f, 0.65f, 1f);

    public static HandManager[] i = new HandManager[2];

    public static int currentInd = 0; // 0 for left, 1 for right

    private void Awake()
    {
        currentInd = 0;
        if (name.Contains("Left"))
        {
            i[0] = this;
        }
        else
        {
            i[1] = this;
        }
    }

}