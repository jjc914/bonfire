Shader "Hidden/Custom/PostProcessing/Pixelate" {
	Properties {
		_MainTex("Main Texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

		Pass {
			Name "Pixelate"

			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

			#pragma vertex vert
			#pragma fragment frag

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);

			CBUFFER_START(UnityPerMaterial)
				int _resolution;
			CBUFFER_END

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
				float aspectRatio = _ScreenParams.y / _ScreenParams.x;
				float resolutionY = _resolution * aspectRatio;

				float2 newUV = input.uv;
				newUV.x = round(newUV.x * _resolution) / _resolution;
				newUV.y = round(newUV.y * resolutionY) / resolutionY;
				float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, newUV);

				return color;
			}

			ENDHLSL
		}
	}
	FallBack "Diffuse"
}