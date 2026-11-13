Shader "TrazoVR/Semipassthrough"
{
    // v62: SEMIPASSTHROUGH: una esfera grande a tu alrededor, BLANCA a los lados y TRANSPARENTE en una ventana
    // (ahí se ve tu cuarto), con un degradado suave entre los dos. _Centro = hacia dónde está la ventana (mundo).
    // Va en Resources para que Escenario la cargue sola (Resources.Load), sin pasos en Unity.
    Properties
    {
        _BaseColor ("Color de los lados", Color) = (1, 1, 1, 1)
        _Centro ("Centro de la ventana (mundo)", Vector) = (0, 0, 1, 0)
        _Dentro ("Coseno: transparente desde aquí", Float) = 0.766
        _Fuera ("Coseno: blanco desde aquí", Float) = 0.26
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-400" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Semi"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Centro;
                float _Dentro;
                float _Fuera;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirWS      : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dirWS = TransformObjectToWorldDir(v.positionOS.xyz);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float c = dot(normalize(i.dirWS), normalize(_Centro.xyz + float3(0, 0, 1e-5)));
                float a = 1.0 - smoothstep(_Fuera, _Dentro, c);
                return half4(_BaseColor.rgb, a * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
