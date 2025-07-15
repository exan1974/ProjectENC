Shader "Custom/TVStatic"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _StaticIntensity ("Static Intensity", Range(0, 1)) = 0.8
        _StaticSpeed ("Static Speed", Range(0, 10)) = 5
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 1
        _Scanlines ("Scanlines", Range(0, 1)) = 0.3
        _ScanlineSpeed ("Scanline Speed", Range(0, 10)) = 2
        _FlickerSpeed ("Flicker Speed", Range(0, 10)) = 1
        _FlickerIntensity ("Flicker Intensity", Range(0, 1)) = 0.1
        _Transparency ("Transparency", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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
            float4 _MainTex_ST;
            float _StaticIntensity;
            float _StaticSpeed;
            float _NoiseScale;
            float _Scanlines;
            float _ScanlineSpeed;
            float _FlickerSpeed;
            float _FlickerIntensity;
            float _Transparency;

            // Noise function
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Improved noise function
            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(.1031, .1030, .0973));
                p3 += dot(p3, p3.yzx+33.33);
                return frac((p3.xx+p3.yz)*p3.zy);
            }

            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f*f*(3.0-2.0*f);
                
                return lerp(lerp(dot(hash22(i + float2(0.0,0.0)), f - float2(0.0,0.0)),
                               dot(hash22(i + float2(1.0,0.0)), f - float2(1.0,0.0)), u.x),
                          lerp(dot(hash22(i + float2(0.0,1.0)), f - float2(0.0,1.0)),
                               dot(hash22(i + float2(1.0,1.0)), f - float2(1.0,1.0)), u.x), u.y);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Base static noise
                float2 uv = i.uv * _NoiseScale;
                float staticNoise = noise2D(uv + _Time.y * _StaticSpeed);
                
                // Additional noise layers for more realistic effect
                float noise2 = noise2D(uv * 2.0 + _Time.y * _StaticSpeed * 0.7);
                float noise3 = noise2D(uv * 4.0 + _Time.y * _StaticSpeed * 1.3);
                
                // Combine noise layers
                float combinedNoise = (staticNoise + noise2 * 0.5 + noise3 * 0.25) / 1.75;
                
                // Scanlines effect
                float scanlines = sin(i.uv.y * 100 + _Time.y * _ScanlineSpeed) * 0.5 + 0.5;
                scanlines = lerp(1, scanlines, _Scanlines);
                
                // Flicker effect
                float flicker = sin(_Time.y * _FlickerSpeed) * 0.5 + 0.5;
                flicker = lerp(1, flicker, _FlickerIntensity);
                
                // Combine all effects
                float finalNoise = combinedNoise * _StaticIntensity * scanlines * flicker;
                
                // Convert to black and white with transparency
                fixed4 col = fixed4(finalNoise, finalNoise, finalNoise, _Transparency);
                
                return col;
            }
            ENDCG
        }
    }
} 