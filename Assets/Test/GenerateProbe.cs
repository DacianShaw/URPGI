using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

[CustomEditor(typeof(Probe))]
public class GenerateProbe: Editor
{
    public override void OnInspectorGUI() 
    {
        DrawDefaultInspector();

        if(GUILayout.Button("capture Gbuffer")) 
        {
            Probe probe = (Probe)target;
            probe.generateGbuffer();
        }
    }
}
