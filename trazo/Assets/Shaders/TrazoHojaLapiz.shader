Shader "TrazoVR/HojaLapiz"
{
    // La hoja del lápiz de boceto: el papel es transparente y el grafito se ve gris (o azul).
    Properties
    {
        _MainTex ("Hoja", 2D) = "black" {}
        _BaseColor ("Color del lápiz", Color) = (0.32, 0.32, 0.36, 1)
        _Borde ("Grosor del borde (fracción de la hoja)", Float) = 0.0012
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Hoja"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float _Borde;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float grafito = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r;
                // Borde delgado y semitransparente (siempre visible): así sabes hasta dónde llega la hoja.
                float2 d = min(i.uv, 1.0 - i.uv);
                float borde = step(min(d.x, d.y), _Borde) * 0.35;
                return half4(_BaseColor.rgb, max(saturate(grafito) * _BaseColor.a, borde));
            }
            ENDHLSL
        }
    }
}
