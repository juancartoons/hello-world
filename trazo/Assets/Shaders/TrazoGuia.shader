Shader "TrazoVR/Guia"
{
    // Manos guía del tutorial: semitransparentes, con el borde más brillante (como un fantasma amable).
    // Primero escribe la profundidad (sin color) y luego pinta solo la cara de adelante:
    // así los dedos que se cruzan no se ven "dobles".
    Properties
    {
        _BaseColor ("Color", Color) = (0.45, 0.75, 1, 0.5)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        HLSLINCLUDE
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
        ENDHLSL

        Pass
        {
            Name "Profundidad"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            ColorMask 0
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragProfundidad
            #pragma multi_compile_instancing
            half4 FragProfundidad(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Guia"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.normalWS);
                float3 vista = normalize(i.vistaWS);
                float frente = saturate(abs(dot(n, vista)));
                float borde = pow(1.0 - frente, 1.6);
                // Contorno oscuro y fino en la silueta (así la pose se entiende de un vistazo).
                float contorno = 1.0 - smoothstep(0.16, 0.3, frente);
                float3 c = lerp(_BaseColor.rgb * 0.85, float3(1, 1, 1), borde * 0.4);
                c = lerp(c, float3(0.04, 0.1, 0.28), contorno);
                float a = lerp(_BaseColor.a * (0.45 + 0.4 * borde), saturate(_BaseColor.a * 1.7), contorno);
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
