using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Rendering;

public struct Surfel
{
    public Vector3 position;
    public Vector3 normal;
    public Vector3 albedo;
    public float skyMask;
}



[ExecuteAlways]
public class Probe : MonoBehaviour
{
    const int tX = 32;
    const int tY = 16;
    const int surfelNum = tX * tY;        

    MaterialPropertyBlock matPropBlock;

    //probes Gbuffer textures 
    public RenderTexture WorldPosCubeMap;
    public RenderTexture NormalCubeMap;
    public RenderTexture AlbedoCubeMap;


    // CPU sufel buffer
    public Surfel[] surfels_cpu; 
    // GPU side surfels buffer
    public ComputeBuffer surfels;
    //To clear SH coefficients
    int[] SHclearValues;
    // coefficients of 2order SH
    public ComputeBuffer SH2Order;

    public ComputeShader SampleSurfelCS;
    public ComputeShader surfelReLightCS;

   

  

    [HideInInspector]
    //probe index
    public int index = -1; 

    public void Initialization()
    {

        if (surfels == null)
            // 3 float3 + 1 float
            surfels = new ComputeBuffer(surfelNum, 3 * 4 * 3 + 4);

        if (SH2Order == null)
        {
            SH2Order = new ComputeBuffer(27, sizeof(int));
            SHclearValues = new int[27];
            for (int i = 0; i < 27; i++) SHclearValues[i] = 0;
        }

        if (surfels_cpu == null)
            surfels_cpu = new Surfel[surfelNum];

        if (matPropBlock == null)
            matPropBlock = new MaterialPropertyBlock();

    }


    void Start()
    {
        Initialization();
    }

    void OnDestroy()
    {
        if(surfels!=null) surfels.Release();
        if(SH2Order != null) SH2Order.Release();
    }

    // relight pass
    public void ReLight(CommandBuffer cmd)
    {
        // set shader params
        Vector3 p = gameObject.transform.position;
        var kid = surfelReLightCS.FindKernel("CSMain");
        cmd.SetComputeVectorParam(surfelReLightCS, "_probePos", new Vector4(p.x, p.y, p.z, 1.0f));
        cmd.SetComputeBufferParam(surfelReLightCS, kid, "_surfels", surfels);
        cmd.SetComputeBufferParam(surfelReLightCS, kid, "_SH2Order", SH2Order);

        var parent = transform.parent;
        IrradianceVolume irradianceVolume;
        ComputeBuffer probeSH;
        if (parent == null)
        {
            irradianceVolume = null;
            probeSH = new ComputeBuffer(1, 4);

        }
        else
        {
            irradianceVolume = parent.gameObject.GetComponent<IrradianceVolume>();
            probeSH = irradianceVolume.ProbeSH;
        }
        cmd.SetComputeBufferParam(surfelReLightCS, kid, "_probeSH", probeSH);
        cmd.SetComputeIntParam(surfelReLightCS, "_index", index);

        // start CS
        cmd.SetBufferData(SH2Order, SHclearValues);
        cmd.DispatchCompute(surfelReLightCS, kid, 1, 1, 1);

    }

    void SetShader(GameObject[] gameObjects, Shader shader)
    {
        foreach(var go in gameObjects)
        {
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if(meshRenderer!=null)
            {
                meshRenderer.sharedMaterial.shader = shader;
            }
        }
    }

    public void generateGbuffer()
    {
      
        GameObject go = new GameObject("CaptureCubeMap");
        go.transform.position = transform.position;
        go.transform.rotation = Quaternion.identity;
        go.AddComponent<Camera>();
        Camera camera = go.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

        GameObject[] gameObjects = FindObjectsOfType(typeof(GameObject)) as GameObject[];
       
        //capture Gbuffer for each probe
        SetShader(gameObjects, Shader.Find("PRT/ProbeWorldPos"));
        camera.RenderToCubemap(WorldPosCubeMap);
        SetShader(gameObjects, Shader.Find("PRT/ProbeNormal"));
        camera.RenderToCubemap(NormalCubeMap);
        SetShader(gameObjects, Shader.Find("Universal Render Pipeline/Unlit"));
        camera.RenderToCubemap(AlbedoCubeMap);

        // reset shader
        SetShader(gameObjects, Shader.Find("Universal Render Pipeline/Lit"));
        //initialization
        Initialization();

       // set necessary data and start sample surfels form Gbuffer
       Vector3 p = gameObject.transform.position;
        var kid = SampleSurfelCS.FindKernel("CSMain");
        SampleSurfelCS.SetVector("_probePos", new Vector4(p.x, p.y, p.z, 1.0f));
        SampleSurfelCS.SetTexture(kid, "_worldPosCubemap", WorldPosCubeMap);
        SampleSurfelCS.SetTexture(kid, "_normalCubemap", NormalCubeMap);
        SampleSurfelCS.SetTexture(kid, "_albedoCubemap", AlbedoCubeMap);
        SampleSurfelCS.SetBuffer(kid, "_surfels", surfels);

        SampleSurfelCS.Dispatch(kid, 1, 1, 1);
        surfels.GetData(surfels_cpu);

        DestroyImmediate(go);
    }
}
