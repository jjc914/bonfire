Shader "Hidden/Custom/PostProcessing/DebugView" {
	Properties {
		_MainTex("Main Texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

		Pass {
			Name "DebugView"

			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

			#pragma vertex vert
			#pragma fragment frag

			TEXTURE2D(_MainTex);
			TEXTURE2D(_CameraDepthTexture);
			SAMPLER(sampler_MainTex);
			SAMPLER(sampler_CameraDepthTexture);

			int _debugMode; // 0: none, 1: depth, 2: normals

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv         : TEXCOORD0;
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv         : TEXCOORD0;
			};

			Varyings vert(Attributes input) {
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = vertexInput.positionCS;
				output.uv = input.uv;

				return output;
			}

			float4 frag(Varyings input) : SV_Target {
				float4 color = float4(1, 1, 1, 1);
				if (_debugMode == 0) {
					color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                } else if (_debugMode == 1) {
					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv);
					color = float4(depth, depth, depth, 1);
                }
				return color;
			}

			ENDHLSL
		}
	}
	FallBack "Diffuse"
}