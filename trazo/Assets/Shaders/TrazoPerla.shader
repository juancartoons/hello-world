Shader "TrazoVR/Perla"
{
    // PERLAS DE COLOR (v37) para la paleta: cada color es una gotita con volumen, sin aro negro: un poco más oscura
    // hacia la orilla (así hasta el blanco se distingue sobre el vidrio) y con un brillito suave arriba a la izquierda.
    // La malla es el disco de la paleta (radio 0.5 en x, y).
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Perla"
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 p          : TEXCOORD0; // de -1 a 1 dentro del disco
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.p = v.positionOS.xy * 2.0;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float r2 = saturate(dot(i.p, i.p));
                float3 c = _BaseColor.rgb;
                // Volumen: un poquito más clara al centro y más oscura hacia la orilla (como una gota).
                c *= lerp(1.04, 0.78, r2 * r2);
                // Un brillito suave arriba a la izquierda.
                float2 b = i.p - float2(-0.32, 0.36);
                float brillo = exp(-dot(b, b) / 0.05);
                c = lerp(c, float3(1.0, 1.0, 1.0), 0.5 * brillo);
                return half4(c, 1.0);
            }
            ENDHLSL
        }
    }
}
