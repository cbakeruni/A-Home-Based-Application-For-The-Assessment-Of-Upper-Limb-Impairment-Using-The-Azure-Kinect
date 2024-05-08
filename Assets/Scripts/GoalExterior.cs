using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoalExterior : MonoBehaviour, IHover
{
    public Goal g;
    [SerializeField] Image border;

    public bool OnHover()
    {
        if (!g.prepared) return false;
        g.OnExterior();
        return true;
    }

    public void OnUnhover()
    {
        g.OffExterior();
    }
}
