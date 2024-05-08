using UnityEditor;
using UnityEngine;

namespace LightBuzz.Kinect4Azure
{
    [CustomEditor(typeof(StickmanManager))]
    public class CustomEditors : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            StickmanManager stickman = (StickmanManager)target;
            if(GUILayout.Button("Just Upper"))
            {
                stickman.SetFullBody(false);
            }
            if (GUILayout.Button("Full Body"))
            {
                stickman.SetFullBody(true);
            }
        }

    }

    public class VMEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            VoiceManager stickman = (VoiceManager)target;
            if (GUILayout.Button("List actions"))
            {
                foreach (var item in VoiceManager.words)
                {
                    GUILayout.Label(item.Key);
                    Debug.Log(item.Key.ToString());
                }
            }
            
        }

    }


}