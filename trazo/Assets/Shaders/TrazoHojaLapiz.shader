Shader "TrazoVR/HojaLapiz"
{
    // La hoja del lápiz de boceto: el grafito se ve gris (o azul) sobre un PAPEL blanco (opaco, a la mitad o
    // transparente: lo eliges con el botón de su esquina). El papel es un rectángulo en el centro de la hoja.
    Properties
    {
        _MainTex ("Hoja", 2D) = "black" {}
        _BaseColor ("Color del lápiz", Color) = (0.32, 0.32, 0.36, 1)
        _Borde ("Grosor del borde (fracción de la hoja)", Float) = 0.0012
        _Papel ("Papel blanco (0 transparente, 1 opaco)", Float) = 1
        _PapelMedio ("Medio tamaño del papel (fracción de la hoja, x y)", Vector) = (0.2, 0.1333, 0, 0)
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
                float _Papel;
                float4 _PapelMedio;
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
                float g = max(saturate(grafito) * _BaseColor.a, borde);
                // El papel blanco (solo en su rectángulo del centro), debajo del grafito.
                float2 c = abs(i.uv - 0.5);
                float p = (c.x <= _PapelMedio.x && c.y <= _PapelMedio.y) ? saturate(_Papel) : 0.0;
                float a = 1.0 - (1.0 - g) * (1.0 - p);
                float3 color = (g * _BaseColor.rgb + (1.0 - g) * p * float3(1.0, 1.0, 1.0)) / max(a, 1e-4);
                return half4(color, a);
            }
            ENDHLSL
        }
    }
}
