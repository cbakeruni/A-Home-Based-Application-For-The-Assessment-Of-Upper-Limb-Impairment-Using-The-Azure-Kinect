using UnityEngine;

[CreateAssetMenu]
public class BoundingBoxSO : ScriptableObject
{
    public Vector3 centrePoint;
    public float radius;

    public void Data((Vector3,float) data)
    {
        centrePoint = data.Item1;
        radius = data.Item2;
    }
}