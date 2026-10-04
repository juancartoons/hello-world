Shader "TrazoVR/Trama"
{
    // Relleno de las viñetas de cómic (estilo Spider-Verse): un color con puntitos de trama (halftone).
    // Los puntitos crecen en diagonal (más grandes abajo a la derecha). Usa la posición del objeto (metros).
    Properties
    {
        _BaseColor ("Color", Color) = (0.97, 0.82, 0.3, 1)
        _ColorPuntos ("Color de los puntitos", Color) = (0.93, 0.55, 0.15, 1)
        _Densidad ("Puntitos por metro", Float) = 260
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Trama"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ColorPuntos;
                float _Densidad;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 local      : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.local = v.positionOS.xy;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // Rejilla girada 45° (como la trama de imprenta).
                float2 p = float2(i.local.x + i.local.y, i.local.x - i.local.y) * 0.7071 * _Densidad;
                float2 celda = frac(p) - 0.5;
                float d = length(celda);
                float gradiente = saturate(0.5 + (i.local.x - i.local.y) * 3.0);
                float radio = lerp(0.12, 0.36, gradiente);
                float ancho = fwidth(d) * 0.8 + 1e-4;
                float punto = 1.0 - smoothstep(radio - ancho, radio + ancho, d);
                float3 c = lerp(_BaseColor.rgb, _ColorPuntos.rgb, punto);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
