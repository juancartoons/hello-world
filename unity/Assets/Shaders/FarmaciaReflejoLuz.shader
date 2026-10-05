Shader "FarmaciaVR/Reflejo Luz"
{
    // Brillo de las luces (LEDs, focos y su resplandor) reflejado en el piso pulido.
    // Va en la copia "de cabeza" de las luces y se SUMA encima del piso, por eso se ve como un brillo
    // fuerte y limpio, aunque el resto del reflejo sea suave.
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Intensidad ("Intensidad del brillo", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+1" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ReflejoLuz"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Intensidad;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return half4(_BaseColor.rgb * i.color.rgb * i.color.a * _Intensidad, 1);
            }
            ENDHLSL
        }
    }
}
