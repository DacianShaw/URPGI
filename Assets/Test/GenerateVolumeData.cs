using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(IrradianceVolume))]
public class GenerateProbeVolumeData : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Generate Probes"))
        {
            IrradianceVolume probeVolume = (IrradianceVolume)target;
            probeVolume.GenerateProbes();
        }

        if (GUILayout.Button("Capture Scene Probes"))
        {
            IrradianceVolume probeVolume = (IrradianceVolume)target;
            probeVolume.ProbeCapture();
        }
    }
}