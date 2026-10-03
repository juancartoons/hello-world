Shader "TrazoVR/ManoVideo"
{
    // Manos del video del proceso: una forma suave y sombreada (como una mano de verdad, sin huesos).
    Properties
    {
        _BaseColor ("Color", Color) = (0.82, 0.78, 0.75, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Mano"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 vistaWS    : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 pos = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(pos);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.vistaWS = GetWorldSpaceViewDir(pos);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS);
                float3 vista = normalize(i.vistaWS);
                if (dot(n, vista) < 0) n = -n;
                float3 luz = normalize(float3(0.3, 0.8, 0.5));
                float difusa = saturate(dot(n, luz)) * 0.55 + 0.45;
                // Borde un poco más oscuro (se lee como contorno, estilo dibujo).
                float borde = smoothstep(0.0, 0.35, saturate(dot(n, vista)));
                float3 c = _BaseColor.rgb * difusa * lerp(0.55, 1.0, borde);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
