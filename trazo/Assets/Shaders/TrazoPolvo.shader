Shader "TrazoVR/Polvo"
{
    // POLVO BRILLANTE (v36): puntitos de luz suaves (se suman a lo de atrás). Lo usan:
    //  - el polvo en que se deshace una línea al borrarla (sistema de partículas, color por vértice);
    //  - la lucecita de la punta de los índices.
    // El puntito es un círculo suave calculado aquí mismo (no necesita textura).
    Properties
    {
        _BaseColor ("Tinte", Color) = (1, 1, 1, 1)
        // v38: cómo se mezcla. Normal: suma luz (SrcAlpha, One). Con fondo claro: mezcla normal (SrcAlpha, OneMinusSrcAlpha).
        [HideInInspector] _SrcBlend ("_SrcBlend", Float) = 5
        [HideInInspector] _DstBlend ("_DstBlend", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Polvo"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
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
                float2 c = i.uv * 2.0 - 1.0;
                float r = saturate(1.0 - dot(c, c));
                return half4(i.color.rgb, i.color.a * r * r);
            }
            ENDHLSL
        }
    }
}
