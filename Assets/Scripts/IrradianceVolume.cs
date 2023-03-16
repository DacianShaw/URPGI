using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;


[ExecuteAlways]
//[System.Serializable]
public class IrradianceVolume : MonoBehaviour
{
    public GameObject probePrefab;

    [Range(0.0f, 50.0f)]
    public float skyIntensity = 1.0f;

    [Range(0.0f, 50.0f)]
    public float GIIntensity = 1.0f;

    RenderTexture RT_WorldPos;
    RenderTexture RT_Normal;
    RenderTexture RT_Albedo;

    public int probeSizeX = 8;
    public int probeSizeY = 4;
    public int probeSizeZ = 8;
    public float probeGridSize = 2.0f;

    public IrradianceVolumeData data;

    // all SH values of probes
    public ComputeBuffer ProbeSH;          
    //temporal SH values to multi-bounce
    public ComputeBuffer TemporalProbeSH; 
    int[] SHclear;

    public GameObject[] probes;

    void Start()
    {
        GenerateProbes();
        data.LoadDataFromSurfelBuffer(this);
    }

    void Update()
    {
        
    }

    void OnDestroy()
    {
        if (ProbeSH != null)
        {
            ProbeSH.Release();
        }
        if (TemporalProbeSH != null)
        {
            TemporalProbeSH.Release();
        }
    }

    public void ClearVoxelBuffer(CommandBuffer cmd)
    {
        if (ProbeSH == null || SHclear == null)
        {
            return;
        }
        cmd.SetBufferData(ProbeSH, SHclear);
    }


    public void SwapTemporalBuffer()
    {
        if (ProbeSH == null || TemporalProbeSH == null)
        {
            return;
        }
        (ProbeSH, TemporalProbeSH) = (TemporalProbeSH, ProbeSH);
    }


    public void GenerateProbes()
    {
        if(probes != null)
        {
            for(int i=0; i<probes.Length; i++)
            {
                DestroyImmediate(probes[i]);
            }
        }
        if (ProbeSH != null)
        {
            ProbeSH.Release();
        }
        if (TemporalProbeSH != null)
        {
            TemporalProbeSH.Release();
        }

        int probeNum = probeSizeX * probeSizeY * probeSizeZ;

    
        probes = new GameObject[probeNum];
        for(int x=0; x<probeSizeX; x++)
        {
            for(int y=0; y<probeSizeY; y++)
            {
                for(int z=0; z<probeSizeZ; z++)
                {
                    Vector3 relativePos = new Vector3(x, y, z) * probeGridSize;
                    Vector3 parentPos = gameObject.transform.position;

                    // setup probe
                    int index = x * probeSizeY * probeSizeZ + y * probeSizeZ + z;
                    probes[index] = Instantiate(probePrefab, gameObject.transform) as GameObject;
                    probes[index].transform.position = relativePos + parentPos; 
                    probes[index].GetComponent<Probe>().index = index;
                    probes[index].GetComponent<Probe>().TryInit();
                }
            }
        }

        
        ProbeSH = new ComputeBuffer(probeNum * 27, sizeof(int));
        TemporalProbeSH = new ComputeBuffer(probeNum * 27, sizeof(int));
        SHclear = new int[probeNum *  27];
        for(int i=0; i< SHclear.Length; i++) 
        {
            SHclear[i] = 0;
        }  
    }

    public void ProbeCapture()
    {
        foreach (var go in probes)
        {
            go.GetComponent<MeshRenderer>().enabled = false;
        }

        // cap
        foreach (var go in probes)
        {
            Probe probe = go.GetComponent<Probe>(); 
            probe.generateGbuffer();
        }

        data.saveData2SurfelBuffer(this);
    }

    public Vector3 GetVolumeMinCorner()
    {
        return gameObject.transform.position;
    }
}
