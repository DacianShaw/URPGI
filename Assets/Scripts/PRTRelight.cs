using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class PRTRelight : ScriptableRendererFeature
{
    class PRTRelightPass : ScriptableRenderPass
    {

        public  IrradianceVolume single_volume;

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            IrradianceVolume[] volumes = GameObject.FindObjectsOfType(typeof(IrradianceVolume)) as IrradianceVolume[];
            if (volumes.Length == 0)
            {
                single_volume = null;
            }
            else
            {
                single_volume = volumes[0];
            }

        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
         
            if(single_volume != null)
            {
                single_volume.SwapTemporalBuffer();
                single_volume.ClearVoxelBuffer(cmd);

                Vector3 corner = single_volume.GetVolumeMinCorner();
                Vector4 probeCorner = new Vector4(corner.x, corner.y, corner.z, 0);
                Vector4 probeSize = new Vector4(single_volume.probeSizeX, single_volume.probeSizeY, single_volume.probeSizeZ, 0);
                cmd.SetGlobalFloat("_sky_Intensity", single_volume.skyIntensity);
                cmd.SetGlobalFloat("_gi_Intensity", single_volume.GIIntensity);
                cmd.SetGlobalFloat("_probeGridSize", single_volume.probeGridSize);
                cmd.SetGlobalVector("_probeSize", probeSize);
                cmd.SetGlobalVector("_probeCorner", probeCorner);
                cmd.SetGlobalBuffer("_probeSH", single_volume.ProbeSH);
                cmd.SetGlobalBuffer("_temporalProbeSH", single_volume.TemporalProbeSH);
            }

            // relight every frame for every probe
            Probe[] probes = GameObject.FindObjectsOfType(typeof(Probe)) as Probe[];
            foreach(var probe in probes)
            {
                if (probe == null)
                {
                    continue;
                }
                probe.Initialization();
                probe.ReLight(cmd);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            
        }
    }

    PRTRelightPass prtRelightPass;

    /// <inheritdoc/>
    public override void Create()
    {
        prtRelightPass = new PRTRelightPass();

        // Configures where the render pass should be injected.
        prtRelightPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(prtRelightPass);
    }
}

