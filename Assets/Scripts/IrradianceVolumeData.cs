using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;



[Serializable]
[CreateAssetMenu(fileName = "IrradianceVolumeData", menuName = "IrradianceVolumeData")]
public class IrradianceVolumeData : ScriptableObject
{
    const int surfelsNums = 512;
    const int dataNums = 10;

    [SerializeField]
    public Vector3 volumePosition;

    [SerializeField]
    public float[] surfelArray;

    // pack all probe's data to 1D array
    public void saveData2SurfelBuffer(IrradianceVolume volume)
    {
        int probeNum = volume.probeSizeX * volume.probeSizeY * volume.probeSizeZ;
        Array.Resize<float>(ref surfelArray, probeNum * surfelsNums * dataNums);
        int j = 0;
        for(int i=0; i<volume.probes.Length; i++)
        {
            Probe probe = volume.probes[i].GetComponent<Probe>();
            foreach (var surfel in probe.surfels_cpu)
            {
                surfelArray[j++] = surfel.position.x;
                surfelArray[j++] = surfel.position.y;
                surfelArray[j++] = surfel.position.z;
                surfelArray[j++] = surfel.normal.x;
                surfelArray[j++] = surfel.normal.y;
                surfelArray[j++] = surfel.normal.z;
                surfelArray[j++] = surfel.albedo.x;
                surfelArray[j++] = surfel.albedo.y;
                surfelArray[j++] = surfel.albedo.z;
                surfelArray[j++] = surfel.skyMask;
            }
        }

        volumePosition = volume.gameObject.transform.position;
        EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
    }

    // load surfel data from storage
    public void LoadDataFromSurfelBuffer(IrradianceVolume volume)
    {
        int probeNum = volume.probeSizeX * volume.probeSizeY * volume.probeSizeZ;
        bool dataDirty = surfelArray.Length != probeNum * surfelsNums * dataNums;
        bool posDirty = volume.gameObject.transform.position != volumePosition;
        int j = 0;
        foreach (var go in volume.probes)
        {
            Probe probe = go.GetComponent<Probe>();
            for(int i=0; i<probe.surfels_cpu.Length; i++)
            {
                probe.surfels_cpu[i].position.x = surfelArray[j++];
                probe.surfels_cpu[i].position.y = surfelArray[j++];
                probe.surfels_cpu[i].position.z = surfelArray[j++];
                probe.surfels_cpu[i].normal.x = surfelArray[j++];
                probe.surfels_cpu[i].normal.y = surfelArray[j++];
                probe.surfels_cpu[i].normal.z = surfelArray[j++];
                probe.surfels_cpu[i].albedo.x = surfelArray[j++];
                probe.surfels_cpu[i].albedo.y = surfelArray[j++];
                probe.surfels_cpu[i].albedo.z = surfelArray[j++];
                probe.surfels_cpu[i].skyMask = surfelArray[j++];
            }
            probe.surfels.SetData(probe.surfels_cpu);
        }
    }
}
