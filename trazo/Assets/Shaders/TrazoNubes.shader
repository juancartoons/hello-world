Shader "TrazoVR/Nubes"
{
    // EL MAR DE NUBES (v42, fondo vivo Sobre las nubes): un disco grande debajo de ti con nubes calculadas aquí mismo
    // (con una textura de ruido que se repite). Del lado del sol las nubes brillan y del otro se ven en sombra.
    //  - _Capa 0 = la de ABAJO: cubre casi todo (entre las nubes, huecos azules profundos).
    //  - _Capa 1 = la de ARRIBA: hilachas sueltas, transparentes, que pasan más rápido (así se nota la altura).
    //  - _Avance: con "Volar", las nubes pasan despacio debajo de ti, como desde un avión.
    // A lo lejos se pierden en la bruma del horizonte (el mismo color que el cielo bajo el horizonte).
    Properties
    {
        _Ruido ("Ruido", 2D) = "gray" {}
        _Escala ("Tamaño de las nubes (m)", Float) = 90
        _Avance ("Avance (m)", Float) = 0
        _Capa ("Capa (0 abajo, 1 arriba)", Float) = 0
        _SolDir ("Dirección del sol", Vector) = (0.35, 0.18, 0.92, 0)
        _Radio ("Radio del disco (m)", Float) = 140
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+455" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Nubes"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_Ruido);
            SAMPLER(sampler_Ruido);

            CBUFFER_START(UnityPerMaterial)
                float4 _Ruido_ST;
                float _Escala;
                float _Avance;
                float _Capa;
                float4 _SolDir;
                float _Radio;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 posOS      : TEXCOORD0;
                float3 posWS      : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.posOS = v.positionOS.xyz;
                o.posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.posWS);
                return o;
            }

            float Densidad(float2 uv)
            {
                float4 a = SAMPLE_TEXTURE2D(_Ruido, sampler_Ruido, uv);
                float4 b = SAMPLE_TEXTURE2D(_Ruido, sampler_Ruido, uv * 2.7 + float2(0.31, 0.17));
                return a.r * 0.62 + b.g * 0.28 + b.a * 0.10;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p = i.posOS.xz;
                p.y += _Avance;                                     // vuelas hacia adelante: las nubes pasan
                float2 uv = p / _Escala;
                float dens = Densidad(uv);
                // Luz del sol: comparada con un poquito más hacia el sol (el borde que mira al sol brilla).
                float3 sol = normalize(_SolDir.xyz);
                float2 haciaSol = normalize(sol.xz + float2(1e-4, 1e-4)) * 0.025;
                float dens2 = Densidad(uv + haciaSol);
                float luz = saturate(0.55 + (dens - dens2) * 6.0);
                half3 col = lerp(half3(0.58, 0.64, 0.78), half3(1.0, 0.97, 0.92), luz);
                // Mirando hacia el sol, todo se ve más cálido.
                float3 vista = i.posWS - GetCameraPositionWS();
                float2 vistaPlano = normalize(vista.xz + float2(1e-4, 1e-4));
                float s = pow(saturate(dot(vistaPlano, normalize(sol.xz + float2(1e-4, 1e-4)))), 8.0);
                col = lerp(col, half3(1.0, 0.88, 0.72), s * 0.35);
                float alfa = 1.0;
                if (_Capa < 0.5)
                {
                    float cubre = smoothstep(0.30, 0.52, dens);
                    col = lerp(half3(0.42, 0.52, 0.70), col, cubre);   // los huecos entre las nubes
                }
                else
                {
                    alfa = smoothstep(0.55, 0.72, dens) * 0.85;
                }
                // A lo lejos, la bruma del horizonte.
                float lejos = smoothstep(_Radio * 0.35, _Radio * 0.95, length(i.posOS.xz));
                half3 bruma = lerp(half3(0.84, 0.88, 0.94), half3(1.0, 0.93, 0.82), s * 0.5);
                col = lerp(col, bruma, lejos);
                if (_Capa > 0.5)
                    alfa *= 1.0 - lejos;
                return half4(col, alfa);
            }
            ENDHLSL
        }
    }
}
