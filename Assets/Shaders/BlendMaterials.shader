Shader "Custom/URP/BlendMaterials"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _SecondTex ("Second Texture", 2D) = "white" {}
        _SecondBaseColor ("Second Color", Color) = (1,1,1,1)
        _BlendAmount ("Blend Amount", Range(0,1)) = 0
        [Toggle(_EMISSION)] _UseEmission("Use Emission", Float) = 0
        [HDR]_EmissionColor("Emission Color", Color) = (0,0,0)
        [HDR]_SecondEmissionColor("Second Emission Color", Color) = (0,0,0)
        _Surface("Surface Type", Float) = 0.0
        _Blend("Blend", Float) = 0.0
        _Cull("Cull", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _SecondTex_ST;
            half4 _BaseColor;
            half4 _SecondBaseColor;
            float _BlendAmount;
            half4 _EmissionColor;
            half4 _SecondEmissionColor;
            float _UseEmission;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        TEXTURE2D(_SecondTex);
        SAMPLER(sampler_MainTex);
        SAMPLER(sampler_SecondTex);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _EMISSION

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample both textures
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 secondTex = SAMPLE_TEXTURE2D(_SecondTex, sampler_SecondTex, input.uv);

                // Blend colors and textures
                half4 albedo = lerp(mainTex * _BaseColor, secondTex * _SecondBaseColor, _BlendAmount);

                // Get main light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 normalWS = normalize(input.normalWS);
                float NdotL = saturate(dot(normalWS, mainLight.direction));

                // Calculate lighting
                float3 lighting = mainLight.color * (mainLight.shadowAttenuation * NdotL);
                
                // Add ambient light
                float3 ambient = SampleSH(normalWS);
                lighting += ambient;

                // Final color
                float3 finalColor = albedo.rgb * lighting;

                // Add emission if enabled
                #ifdef _EMISSION
                if (_UseEmission > 0.5)
                {
                    float3 emission = lerp(_EmissionColor.rgb, _SecondEmissionColor.rgb, _BlendAmount);
                    finalColor += emission;
                }
                #endif

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // Shadow casting support
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
} 