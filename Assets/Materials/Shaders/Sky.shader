Shader "Universal Render Pipeline/Custom/Skybox/Sky" {
    Properties {
        [MainColor] _SkyColor("Sky Color", Color) = (1, 1, 1, 1)
        _VoidColor("Void Color", Color) = (1, 1, 1, 1)
        [MainTexture] _MainTex("Star Texture", 2D) = "white" { }
    }
    SubShader {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        // color pass
        Pass {
            Name "Sky"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma vertex vert
			#pragma fragment frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            half4 _SkyColor;
            half4 _VoidColor;
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
                float4 color = lerp(_VoidColor, _SkyColor, normalize(input.positionWS).y);

                return color;
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
