Shader "PRT/Blend"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/SH.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            sampler2D _MainTex;
            TEXTURE2D_X(_CameraDepthTexture);      
            //albedo Gbuffer
            TEXTURE2D_X_HALF(_GBuffer0);
            //normal Gbuffer
            TEXTURE2D_X_HALF(_GBuffer2);
            SamplerState my_point_clamp_sampler;
            float _probeGridSize;
            float4 _probeCorner;
            float4 _probeSize;
            StructuredBuffer<int> _probeSH; 
            StructuredBuffer<int> _temporalProbeSH;
            float _gi_Intensity;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            } 

            float4 frag (v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);
                float3 albedo = SAMPLE_TEXTURE2D_X_LOD(_GBuffer0, my_point_clamp_sampler, i.uv, 0).xyz;
                float3 normal = SAMPLE_TEXTURE2D_X_LOD(_GBuffer2, my_point_clamp_sampler, i.uv, 0).xyz;

                // get world pos
                float2 screenPos = i.uv;
                float sceneRawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, my_point_clamp_sampler, screenPos);
                float4 ndc = float4(screenPos.x * 2 - 1, screenPos.y * 2 - 1, sceneRawDepth, 1);
                ndc.y *= -1;
                float4 worldPos = mul(UNITY_MATRIX_I_VP, ndc);
                worldPos /= worldPos.w;

                float3 gi = Relight(
                    worldPos, 
                    albedo, 
                    normal,
                    _probeSH,
                    _probeGridSize,
                    _probeCorner,
                    _probeSize
                );
                color.rgb += gi * _gi_Intensity;
                
                return color;
            }
            ENDHLSL
        }
    }
}
