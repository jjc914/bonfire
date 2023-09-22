Shader "Hidden/Custom/PostProcessing/Outline" {
	Properties {
		_MainTex("Main Texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

		Pass {
			Name "Outline"

			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

			#pragma vertex vert
			#pragma fragment frag

			TEXTURE2D(_MainTex);
			TEXTURE2D(_CameraColorTexture);
			TEXTURE2D(_CameraDepthTexture);
			TEXTURE2D(_CameraDepthNormalsTexture);
			TEXTURE2D(_CameraMaskTexture);
			TEXTURE2D(_CameraUnlitTexture);
			SAMPLER(sampler_MainTex);
			SAMPLER(sampler_CameraColorTexture);
			SAMPLER(sampler_CameraDepthTexture);
			SAMPLER(sampler_CameraDepthNormalsTexture);
			SAMPLER(sampler_CameraMaskTexture);
			SAMPLER(sampler_CameraUnlitTexture);
			float4 _MainTex_TexelSize;

			CBUFFER_START(UnityPerMaterial)
				// general parameters
				int _algorithm; // 0: none, 1: roberts cross, 2: soble, 3: jump flood
				half4 _outlineColor;
				float _maxDepth;

				// kernel parameters
				int _hardCutoff;
				float _depthThreshold;
				float _normalsThreshold;
				float _textureThreshold;

				// gaussian parameters
				int _kernelSize;
			CBUFFER_END

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv         : TEXCOORD0;
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv     : TEXCOORD0;
			};

#define SOBEL_XKERNEL \
{ \
	{ -1, 0, 1, 0, 0, 0, 0, 0, 0, 0 }, \
	{ -2, 0, 2, 0, 0, 0, 0, 0, 0, 0 }, \
	{ -1, 0, 1, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0, 0,  0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0, 0,  0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0, 0,  0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0, 0,  0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0, 0,  0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0, 0,  0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0, 0,  0, 0, 0, 0, 0, 0, 0 }, \
};

#define SOBEL_YKERNEL \
{ \
	{  1,  2,  1, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,   0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ -1, -2, -1, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,   0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,   0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,   0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,   0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,   0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,   0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,   0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
};

#define SOBEL_KERNEL_SIZE \
float2(3, 3)

#define ROBERTS_XKERNEL \
{ \
	{ 1, 0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, -1, 0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0, 0,  0, 0, 0, 0, 0, 0, 0, 0 }, \
};

#define ROBERTS_YKERNEL \
{ \
	{ 0,  1, 0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ -1, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,  0, 0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,  0, 0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,  0, 0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,  0, 0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,  0, 0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,  0, 0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,  0, 0, 0, 0, 0, 0, 0, 0, 0 }, \
	{ 0,  0, 0, 0, 0, 0, 0, 0, 0, 0 }, \
};

#define ROBERTS_KERNEL_SIZE \
float2(2, 2)

			float3 decodeNormals(float4 enc) {
				float kScale = 1.7777;
				float3 nn = enc.xyz * float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
				float g = 2.0 / dot(nn.xyz, nn.xyz);
				float3 n;
				n.xy = g * nn.xy;
				n.z = g - 1;
				return n;
			}

			float toGreyscale(float3 color) {
				return 0.299 * color.x + 0.587 * color.y + 0.114 * color.z;
            }

			// kernel-based algorithms
			float kernelTestDepth(float2 uv, float xKernel[10][10], float yKernel[10][10], float2 kernelSize) {
				float xSum = 0;
				float ySum = 0;

				float2 halfScaleFloor = floor(kernelSize * 0.5);
				for (int y = 0; y < kernelSize.y; y++) {
					for (int x = 0; x < kernelSize.x; x++) {
						float2 newUV = uv + (float2(x, y) - halfScaleFloor) * _MainTex_TexelSize.xy;
						float sample = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, newUV);
						xSum += sample * xKernel[y][x];
						ySum += sample * yKernel[y][x];
                    }
                }
				float magnitude = sqrt(xSum * xSum + ySum * ySum);
				return magnitude;
            }

			float kernelTestNormals(float2 uv, float xKernel[10][10], float yKernel[10][10], float2 kernelSize) {
				float xSum = 0;
				float ySum = 0;

				float2 halfScaleFloor = floor(kernelSize * 0.5);
				for (int y = 0; y < kernelSize.y; y++) {
					for (int x = 0; x < kernelSize.x; x++) {
						float2 newUV = uv + (float2(x, y) - halfScaleFloor) * _MainTex_TexelSize.xy;
						float3 sample = decodeNormals(SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, newUV));
						xSum += (sample.x + sample.y + sample.z) * xKernel[y][x] / kernelSize.x;
						ySum += (sample.x + sample.y + sample.z) * yKernel[y][x] / kernelSize.y;
                    }
                }
				float magnitude = sqrt(xSum * xSum + ySum * ySum);
				return magnitude;
            }

			float kernelTestTexture(float2 uv, float xKernel[10][10], float yKernel[10][10], float2 kernelSize) {
				float xSum = 0;
				float ySum = 0;

				float2 halfScaleFloor = floor(kernelSize * 0.5);
				for (int y = 0; y < kernelSize.y; y++) {
					for (int x = 0; x < kernelSize.x; x++) {
						float2 newUV = uv + (float2(x, y) - halfScaleFloor) * _MainTex_TexelSize.xy;
						float3 sample = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, newUV).xyz;
						xSum += (sample.x + sample.y + sample.z) * xKernel[y][x] / kernelSize.x;
						ySum += (sample.x + sample.y + sample.z) * yKernel[y][x] / kernelSize.y;
                    }
                }
				float magnitude = sqrt(xSum * xSum + ySum * ySum);
				return magnitude;
            }

			// gaussian algorithms 
			float gaussian(float x, float y, float sig) { 
				return 1 / (2 * PI * sig * sig) * exp(-(x * x + y * y) / (2 * sig * sig));
            }

			float4 gaussianBlur(Texture2D tex, SamplerState samp, float2 uv, float sig) {
				float4 sum = float4(0, 0, 0, 0);
				float totalWeight = 0;

				float halfWidthFloor = floor(_kernelSize * 0.5);
				for (int y = 0; y < _kernelSize; y++) {
					for (int x = 0; x < _kernelSize; x++) {
						float2 newUV = uv + (float2(x, y) - halfWidthFloor) * _MainTex_TexelSize.xy;
						float weight = gaussian(x - halfWidthFloor, y - halfWidthFloor, sig);
						sum += weight * SAMPLE_TEXTURE2D(tex, samp, newUV);
						totalWeight += weight;
                    }
                }
				return sum / totalWeight;
            }

			float4 encodeGaussian(float4 mask, float2 uv) {
				float4 encoded = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
				encoded.w = mask.x;
				return encoded;
            }

			Varyings vert(Attributes input) {
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = vertexInput.positionCS;
				output.uv = input.uv;

				return output;
			}

			float4 frag(Varyings input) : SV_Target {

				float4 color;
				if (_algorithm == 1) {
					float xKernel[10][10] = ROBERTS_XKERNEL;
					float yKernel[10][10] = ROBERTS_YKERNEL;

					float depthMagnitude = kernelTestDepth(input.uv, xKernel, yKernel, ROBERTS_KERNEL_SIZE); // sobel operator on texture buffer 
					float normalsMagnitude = kernelTestNormals(input.uv, xKernel, yKernel, ROBERTS_KERNEL_SIZE); // sobel operator on texture buffer 
					float textureMagnitude = kernelTestTexture(input.uv, xKernel, yKernel, ROBERTS_KERNEL_SIZE); // sobel operator on texture buffer

					if (depthMagnitude > _depthThreshold || normalsMagnitude > _normalsThreshold || textureMagnitude > _textureThreshold) {
						if (_hardCutoff == 1) {
							color = _outlineColor;
						} else {
							color = max(max(depthMagnitude, normalsMagnitude), textureMagnitude) * _outlineColor;
                        }
					} else {
						color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
					}

                } else if (_algorithm == 2) {
					float xKernel[10][10] = SOBEL_XKERNEL;
					float yKernel[10][10] = SOBEL_YKERNEL;
					float depthMagnitude = kernelTestDepth(input.uv, xKernel, yKernel, SOBEL_KERNEL_SIZE); // sobel operator on texture buffer 
					float normalsMagnitude = kernelTestNormals(input.uv, xKernel, yKernel, SOBEL_KERNEL_SIZE); // sobel operator on texture buffer
					float textureMagnitude = kernelTestTexture(input.uv, xKernel, yKernel, SOBEL_KERNEL_SIZE); // sobel operator on texture buffer

					if (depthMagnitude > _depthThreshold || normalsMagnitude > _normalsThreshold || textureMagnitude > _textureThreshold) {
						if (_hardCutoff == 1) {
							color = _outlineColor;
						} else {
							color = max(max(depthMagnitude, normalsMagnitude), textureMagnitude) * _outlineColor;
                        }
					} else {
						color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
					}

				} else if (_algorithm == 3) {
					// blur pass  
					float4 mask = gaussianBlur(_CameraMaskTexture, sampler_CameraMaskTexture, input.uv, 1.5);
					color = encodeGaussian(mask, input.uv);
                } else if (_algorithm == 4) {
					color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
					// TODO: jump flood
                }
				//color = SAMPLE_TEXTURE2D(_CameraUnlitTexture, sampler_CameraUnlitTexture, input.uv); 
				return color;
			}

			ENDHLSL
		}

		Pass {
			Name "GaussianOverlay"

			HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

			#pragma vertex vert
			#pragma fragment frag

			TEXTURE2D(_MainTex);
			TEXTURE2D(_CameraColorTexture);
			TEXTURE2D(_CameraDepthTexture);
			TEXTURE2D(_CameraDepthNormalsTexture);
			TEXTURE2D(_CameraMaskTexture);
			SAMPLER(sampler_MainTex);
			SAMPLER(sampler_CameraColorTexture);
			SAMPLER(sampler_CameraDepthTexture);
			SAMPLER(sampler_CameraDepthNormalsTexture);
			SAMPLER(sampler_CameraMaskTexture);
			float4 _MainTex_TexelSize;

			// general parameters
			int _algorithm; // 0: none, 1: roberts cross, 2: soble, 3: jump flood

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv         : TEXCOORD0;
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv         : TEXCOORD0;
			};

			float4 overlay(float2 uv) {
				// FIXME: alpha channel not saving from last pass?
				float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
				/*if (color.w == 0) {
					return float4(0, 0, 0, 1);
                }
				return color;*/
				return color.wwww;
            }

			Varyings vert(Attributes input) {
				Varyings output = (Varyings)0;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = vertexInput.positionCS;
				output.uv = input.uv;

				return output;
			}

			float4 frag(Varyings input) : SV_Target {
				float4 color = float4(0, 0, 0, 1);// = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
				if (_algorithm == 1) {
                } else if (_algorithm == 2) {
				} else if (_algorithm == 3) {
					color = overlay(input.uv);
                } else if (_algorithm == 4) {
                }
				return color;
			}

			ENDHLSL
        }
	}
	FallBack "Diffuse"
}