using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSR : ScriptableRendererFeature
{

    
    class CustomRenderPass : ScriptableRenderPass
    {
        //define a material to run shader 
        public Material _HiZGeneratorMaterial;
        public Material _SSReflectionMaterial;
        public RenderTargetIdentifier blitSrc;
        public RenderTargetHandle tempRTHandle;
        static int HizMaxLevel = 5;
        RenderTexture m_depthTexture;
        int m_depthTextureShaderID;
        public RenderTexture depthTexture => m_depthTexture;
        int depth_w = 0;
        int depth_h = 0;

        //************************ for generate HizBuffer *******************************
        RenderTexture currentRenderTexture = null;//当前mipmapLevel对应的mipmap
        RenderTexture preRenderTexture = null;//上一层的mipmap，即mipmapLevel-1对应的mipmap


        const RenderTextureFormat m_depthTextureFormat = RenderTextureFormat.R8;//深度取值范围0-1，单通道即可。
        void InitDepthTexture()
        {
            depth_w = Screen.width;
            depth_h = Screen.height;
            if (m_depthTexture != null) return;
            m_depthTexture = new RenderTexture(depth_w, depth_h, 0, m_depthTextureFormat);
            m_depthTexture.autoGenerateMips = false;//Mipmap手动生成
            m_depthTexture.useMipMap = true;
            m_depthTexture.filterMode = FilterMode.Point;
            m_depthTexture.Create();
            m_depthTextureShaderID = Shader.PropertyToID("_CameraDepthTexture");
        }


        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {

            RenderTextureDescriptor rtDesc = renderingData.cameraData.cameraTargetDescriptor;
            cmd.GetTemporaryRT(tempRTHandle.id, rtDesc);


            blitSrc = renderingData.cameraData.renderer.cameraColorTarget;
            preRenderTexture = null;
            currentRenderTexture = null;
            InitDepthTexture();


    }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("HizGeneration");
            RenderTargetIdentifier tempRT = tempRTHandle.Identifier();


            for(int i = 1; i <=HizMaxLevel; i++)
            {

                currentRenderTexture = RenderTexture.GetTemporary(depth_w, depth_h, 0, m_depthTextureFormat);
                currentRenderTexture.filterMode = FilterMode.Point;
                _HiZGeneratorMaterial.SetInt("_ SSR_HiZ_PrevZLeve", i - 1);
                if (preRenderTexture == null)
                {
                    cmd.Blit(m_depthTextureShaderID, currentRenderTexture);
                }
                else
                {

                    cmd.Blit(preRenderTexture, currentRenderTexture, _HiZGeneratorMaterial);
                    RenderTexture.ReleaseTemporary(preRenderTexture);
                }
                cmd.CopyTexture(currentRenderTexture, 0, 0, m_depthTexture, 0, i-1);
                preRenderTexture = currentRenderTexture;
                depth_h /= 2;
                depth_w /= 2;



            }
            RenderTexture.ReleaseTemporary(preRenderTexture);


            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);

            cmd.SetGlobalTexture("_HiZDepth", m_depthTexture);
           


            CommandBuffer cmd_ssr = CommandBufferPool.Get("SSR");
            cmd.Blit(blitSrc, tempRT,_SSReflectionMaterial);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);




        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempRTHandle.id);
        }
    }

    CustomRenderPass m_ScriptablePass;
    public Material HiZGeneratorMaterial;
    public Material SSReflectionMaterial;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        m_ScriptablePass._HiZGeneratorMaterial = HiZGeneratorMaterial;
        m_ScriptablePass._SSReflectionMaterial = SSReflectionMaterial;
       
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


