Shader "TrazoVR/ManoVidrio"
{
    // MANOS DE VIDRIO (v36): casi transparentes por dentro, con el contorno iluminado (fresnel), y las yemas de los
    // índices brillan con la luz de la punta del dedo (ver TrazoLuz.hlsl).
    Properties
    {
        _BaseColor ("Color del vidrio", Color) = (0.72, 0.86, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ManoVidrio"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "TrazoLuz.hlsl"

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
                float3 posWS      : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.posWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 V = normalize(i.posWS - GetCameraPositionWS());
                float3 N = normalize(i.normalWS);
                float fres = pow(1.0 - saturate(abs(dot(V, N))), 2.2);
                float luz = TrazoLuzDedos(i.posWS);
                float3 col = _BaseColor.rgb * (0.35 + 0.65 * fres) + fres * 0.2 + TrazoColorLuz * (0.9 * luz);
                float a = saturate(0.1 + 0.6 * fres + 0.55 * luz);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
