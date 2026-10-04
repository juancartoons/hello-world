Shader "TrazoVR/Brillo"
{
    // Escarcha y papelitos de celebración (partículas). Usa el color de cada partícula.
    // _Suave = 0: papelito cuadrado · _Suave = 1: chispa redonda que brilla en el centro.
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Suave ("Suave", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        Pass
        {
            Name "Brillo"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Suave;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color * _BaseColor;
                o.uv = v.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 d = i.uv * 2.0 - 1.0;
                // Chispa: una estrellita de 4 puntas con el centro blanco.
                float r = length(d);
                float estrella = saturate(1.0 - r) + saturate(1.0 - abs(d.x) * 6.0) * saturate(1.0 - abs(d.y)) + saturate(1.0 - abs(d.y) * 6.0) * saturate(1.0 - abs(d.x));
                float chispa = saturate(estrella);
                float3 colorChispa = lerp(i.color.rgb, float3(1, 1, 1), saturate(1.0 - r * 2.5) * 0.7);
                float3 c = lerp(i.color.rgb, colorChispa, _Suave);
                float a = i.color.a * lerp(1.0, chispa, _Suave);
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
