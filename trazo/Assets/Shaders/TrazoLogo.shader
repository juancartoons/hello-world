Shader "TrazoVR/Logo"
{
    // EL LOGO de JCartoons (v49) en la entrada: una imagen con transparencia que se revela de izquierda a derecha
    // (_Revela, con la orilla suave) al ritmo de las notas del arpa, y se desvanece al final (_Alfa).
    Properties
    {
        _BaseMap ("Logo", 2D) = "white" {}
        _Revela ("Revelado (0 a 1, de izquierda a derecha)", Float) = 1
        _Alfa ("Opacidad", Float) = 1
        _Tinte ("Tinte (las capas de atrás, más oscuras = volumen)", Color) = (1, 1, 1, 1)
        _Ola ("Ola de las letras (metros)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Logo"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _Revela;
                float _Alfa;
                float4 _Tinte;
                float _Ola;
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
                // v50: las letras ondulan (una ola de izquierda a derecha), como si bailaran.
                float3 p = v.positionOS.xyz;
                p.y += sin(v.uv.x * 13.0 - _Time.y * 4.5) * _Ola;
                o.positionCS = TransformObjectToHClip(p);
                o.uv = v.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                float revela = smoothstep(i.uv.x - 0.04, i.uv.x + 0.04, _Revela * 1.08);
                c.rgb *= _Tinte.rgb;
                c.a *= revela * _Alfa;
                return c;
            }
            ENDHLSL
        }
    }
}
