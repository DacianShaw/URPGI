Shader "PRT/SSReflection"
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
             #include "Assets/Shaders/SSRPass.hlsl"
     

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
            
        

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            
            }



            sampler2D _MainTex;
            TEXTURE2D_X(_HiZDepth);
            TEXTURE2D_X(_CameraDepthTexture);
            TEXTURE2D_X_HALF(_GBuffer0);
            TEXTURE2D_X_HALF(_GBuffer1);
            TEXTURE2D_X_HALF(_GBuffer2);
            SamplerState my_point_clamp_sampler;
            SAMPLER(sampler_LinearClamp);


            float4 GetFragmentViewPos(float2 screenPos)
            {
                float sceneRawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, my_point_clamp_sampler, screenPos);
                float4 ndc = float4(screenPos.x * 2 - 1, screenPos.y * 2 - 1, sceneRawDepth, 1);
                float4 ViewPos = mul(unity_CameraInvProjection, ndc);
                ViewPos /= ViewPos.w;

                return ViewPos;
            }

             float4 frag (v2f i) : SV_Target
            {
                float3 normal = SAMPLE_TEXTURE2D_X_LOD(_GBuffer2, my_point_clamp_sampler, i.uv, 0).xyz;
                  
                float4 _viewNormal = mul(UNITY_MATRIX_V,float4(normal,1.0));
                float3 ViewNormal = normalize(_viewNormal.xyz/ _viewNormal.w);
                float3 viewPos= GetFragmentViewPos(i.uv);


                //find the reflected color 
                
                float3 castDir = 2 * ViewNormal + normalize(viewPos);
                float3 castOrigin = viewPos;


                //SSR
                

                //origin coloir
              //  float4 color = tex2D(_MainTex, i.uv);

                int maxStep= 60;
                float stepsize = 1.0f;
                float maxDistance;
                float thickness = 1.f;
                /*

                float3 rayPos = castOrigin;
                float4 clipRayPos = mul(unity_CameraProjection,float4(rayPos,1) );
                clipRayPos.xy /= clipRayPos.w;
                float2 uv = clipRayPos.xy * 0.5 + 0.5;
                float  uv_depth = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture,my_point_clamp_sampler,uv,0);
                uv_depth = LinearEyeDepth(1-uv_depth , _ZBufferParams) ;

                float4 color = tex2D(_MainTex, uv);

                color.rgb = float3( uv_depth,0.0f,0.0f) * 2.f;
                color.rgb = float3(-rayPos.z - uv_depth * 2.f,0.0f,0.0f) ;
                */
          
               
                float4 color = tex2D(_MainTex,i.uv);
               // color.xyz = ViewNormal - normal;
              // color.xyz += tex2D(_MainTex,uv).xyz;
            
                
                 UNITY_LOOP
                 for(int  step= 1; step <= maxStep; step++){
                    float3 rayPos = castOrigin + castDir * stepsize * step;
                    float4 clipRayPos = mul(unity_CameraProjection,float4(rayPos,1) );
                    clipRayPos.xy /= clipRayPos.w;
                    float2 uv = clipRayPos.xy * 0.5 + 0.5;
                    float  uv_depth = SAMPLE_TEXTURE2D_X_LOD(_HiZDepth,sampler_LinearClamp,uv,0);
                    //uv_depth 
                    //put clip depth into uv depth space;
                     uv_depth = LinearEyeDepth(1- uv_depth , _ZBufferParams) * 2;
                     float view_depth  =-rayPos.z;



                   if(uv.x > 0.f && uv.y > 0.f && uv.x <1.f && uv.y< 1.f
                    && view_depth  > uv_depth  && view_depth < uv_depth + thickness
                    ){

                       
                        color.xyz = tex2D(_MainTex,uv);
                        //color.xyz = float3(uv_depth - view_depth,0.f,0.f);
                        break;
                    }
                
                 }
                 
                 
                  
                 
                

                
                

                


                return color;


            }


             ENDHLSL

        }
    }
}
