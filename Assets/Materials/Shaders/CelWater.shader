Shader "Universal Render Pipeline/Custom/CelWater" {
    Properties {
        [MainColor] _DeepColor("Deep Color", Color) = (0, 0, 0, 1)
        _ShallowColor("Shallow Color", Color) = (1, 1, 1, 1)
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _MaxDepth("Maximum Depth", Float) = 2
        _ParaShoreFoamDepth("Parallel Shore Foam Depth", Float) = 0.2
        _PerpShoreFoamDepth("Perpendicular Shore Foam Depth", Float) = 1
        [MainTexture] _SurfaceNoiseTex("Surface Noise Texture", 2D) = "white" { }
        _ShoreNoiseTex("Shore Noise Texture", 2D) = "white" { }
        _UVDistortionNoiseTex("UV Distortion Noise Texture", 2D) = "white" { }
        // TODO: [ShowAsVector2] MaterialPropertyDrawer
        _NoiseScrollSpeed("Noise Scroll Speed", Vector) = (0.03, 0.03, 0, 0)
        _SurfaceUVDistortionFactor("Surface UV Distortion Factor", Range(0, 1)) = 0.1
        _ShoreUVDistortionFactor("Shore UV Distortion Factor", Range(0, 1)) = 1
        _ShoreFoamNoiseFalloff("Shore Foam Falloff", Range(0, 10)) = 1
        _SurfaceFoamMinCutoff("Surface Foam Minimum Cutoff", Range(0, 1)) = 0.2
        _SurfaceFoamMaxCutoff("Surface Foam Maximum Cutoff", Range(0, 1)) = 0.25
    }
    SubShader {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        // color pass
        Pass {
            Name "CelWater"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
			#pragma fragment frag

            TEXTURE2D(_SurfaceNoiseTex);
            TEXTURE2D(_ShoreNoiseTex);
            TEXTURE2D(_UVDistortionNoiseTex);
            TEXTURE2D(_CameraDepthTexture);
            TEXTURE2D(_CameraDepthNormalsTexture);
            SAMPLER(sampler_SurfaceNoiseTex);
            SAMPLER(sampler_ShoreNoiseTex);
            SAMPLER(sampler_UVDistortionNoiseTex);
            SAMPLER(sampler_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthNormalsTexture);

            CBUFFER_START(UnityPerMaterial)
                float _SurfaceUVDistortionFactor;
                float _ShoreUVDistortionFactor;
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _FoamColor;
                float _MaxDepth;
                float _ParaShoreFoamDepth;
                float _PerpShoreFoamDepth;
                float4 _SurfaceNoiseTex_ST;
                float4 _ShoreNoiseTex_ST;
                float4 _UVDistortionNoiseTex_ST;
                float2 _NoiseScrollSpeed;
                float _ShoreFoamNoiseFalloff;
                float _SurfaceFoamMinCutoff;
                float _SurfaceFoamMaxCutoff;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
				float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS     : SV_POSITION;

                float3 positionWS     : TEXCOORD1;
                float3 positionVS     : TEXCOORD3;
                float4 positionNDC    : TEXCOORD4;
                float3 normalWS       : TEXCOORD2;
                float2 surfaceUV      : TEXCOORD0;
                float2 shoreUV         : TEXCOORD5;
                float2 uvDistortionUV : TEXCOORD6;
            };

            float linear01Depth(float depth) {
                return 1 / (_ZBufferParams.x * depth + _ZBufferParams.y);
            }

            float3 decodeNormals(float4 enc) {
				float kScale = 1.7777;
				float3 nn = enc.xyz * float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
				float g = 2.0 / dot(nn.xyz, nn.xyz);
				float3 n;
				n.xy = g * nn.xy;
				n.z = g - 1;
				return n;
			}

            Varyings vert(Attributes input) {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInputs.positionCS;
                output.positionWS = vertexInputs.positionWS;
                output.positionVS = vertexInputs.positionVS;
                output.positionNDC = vertexInputs.positionNDC;
                output.normalWS = normInputs.normalWS;
                output.surfaceUV = TRANSFORM_TEX(input.uv, _SurfaceNoiseTex);
                output.shoreUV = TRANSFORM_TEX(input.uv, _ShoreNoiseTex);
                output.uvDistortionUV = TRANSFORM_TEX(input.uv, _UVDistortionNoiseTex);

                return output;
            }

            float4 frag(Varyings input) : SV_Target {
                // TODO: antialiasing
                // TODO: vertex offset 
                // calculate depth values 
                float submergedDepth = linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, float2(input.positionNDC.x, input.positionNDC.y) / input.positionNDC.w));
                float surfaceDepth = linear01Depth(input.positionCS.z);
                float depthDifference = (submergedDepth - surfaceDepth) * (_ProjectionParams.z - _ProjectionParams.y);

                // calculate modulation based on normals
                float3 submergedNormals = decodeNormals(SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, float2(input.positionNDC.x, input.positionNDC.y) / input.positionNDC.w));
                float normalsDot = dot(input.normalWS, submergedNormals);
                float shoreFoamDepth = lerp(_PerpShoreFoamDepth, _ParaShoreFoamDepth, saturate(normalsDot));

                // interpolation factors based on depth
                float depthInterpolation = saturate(depthDifference / _MaxDepth);
                float shoreInterpolation = saturate(depthDifference / shoreFoamDepth);

                // noise samples
                float4 rawSurfaceNoiseSample = SAMPLE_TEXTURE2D(_SurfaceNoiseTex, sampler_SurfaceNoiseTex, input.surfaceUV);
                float4 rawShoreNoiseSample = SAMPLE_TEXTURE2D(_ShoreNoiseTex, sampler_ShoreNoiseTex, input.shoreUV);
                float4 rawUVDistortionNoiseSample = SAMPLE_TEXTURE2D(_UVDistortionNoiseTex, sampler_UVDistortionNoiseTex, input.uvDistortionUV);
                float4 distortedSurfaceNoiseSample = SAMPLE_TEXTURE2D(_SurfaceNoiseTex, sampler_SurfaceNoiseTex, (input.surfaceUV + _NoiseScrollSpeed * _Time.y) + rawUVDistortionNoiseSample.xy * _SurfaceUVDistortionFactor);
                float4 distortedShoreNoiseSample = SAMPLE_TEXTURE2D(_ShoreNoiseTex, sampler_ShoreNoiseTex, (input.shoreUV + _NoiseScrollSpeed * _Time.y) + rawUVDistortionNoiseSample.xy * _ShoreUVDistortionFactor);

                // depth color
                float4 color = lerp(_ShallowColor, _DeepColor, depthInterpolation);

                // shore foam
                float shoreNoiseCutoff = shoreFoamDepth - distortedShoreNoiseSample.r * pow(shoreInterpolation, _ShoreFoamNoiseFalloff);
                float shoreNoise = depthInterpolation < shoreNoiseCutoff ? 1 : 0;

                // surface foam
                float surfaceNoise = (distortedSurfaceNoiseSample.x > _SurfaceFoamMinCutoff && distortedSurfaceNoiseSample.x < _SurfaceFoamMaxCutoff ? 1 : 0);

                // resultant noise
                float noise = shoreNoise + surfaceNoise;
                if (noise > 0) {
                    color = _FoamColor;
                }
                return color;
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
