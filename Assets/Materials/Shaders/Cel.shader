Shader "Universal Render Pipeline/Custom/Cel" {
    Properties {
        [MainColor] _MainColor("Color", Color) = (1, 1, 1, 1)
        [MainTexture] _MainTex("Albedo (RGB)", 2D) = "white" { }
        [NoScaleOffset] _PaletteTex("Palette", 3D) = "white" { }
    }
    SubShader {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        // depth normals pass 
        Pass {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull Back
                ZTest LEqual
                ZWrite On
        }

        Pass {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }

            Cull Back
                ZTest LEqual
                ZWrite On
        }

        // color pass
        Pass {
            Name "Cel"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma vertex vert
			#pragma fragment frag

            TEXTURE2D(_MainTex);
            TEXTURE3D(_PaletteTex);
            SAMPLER(sampler_MainTex);
            SAMPLER(sampler_PaletteTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _MainColor;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
				float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float2 uv         : TEXCOORD0;
            };

            float4 toCel(float4 color) {
                return SAMPLE_TEXTURE3D(_PaletteTex, sampler_PaletteTex, color.xyz);
            }

            float4 phong(float4 albedo, float3 normal) {
                InputData lightingInput = (InputData)0;
                half4 shadowMask = CalculateShadowMask(lightingInput);
                Light mainLight = GetMainLight(lightingInput, shadowMask, (AmbientOcclusionFactor)0);

                float intensity = dot(normal, mainLight.direction);
                return float4(intensity, intensity, intensity, 1) * albedo;
            }

            Varyings vert(Attributes input) {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInputs.positionCS;
                output.positionWS = vertexInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            float4 frag(Varyings input) : SV_Target {
                float4 rawColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _MainColor;
                float4 ph = phong(rawColor, input.normalWS);
                float4 color = toCel(ph);
                
                return color;
            }

ENDHLSL
        }
    }
    FallBack "Diffuse"
}
