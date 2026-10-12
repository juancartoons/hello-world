Shader "TrazoVR/CieloVivo"
{
    // FONDOS VIVOS (v42): el cielo a tu alrededor (una esfera grande que va contigo), calculado aquí mismo. No es una
    // foto: lo dibuja la app.
    //  - _Modo 0 = UNIVERSO: el negro del espacio, la Vía Láctea (más brillante y cálida hacia su centro, con su polvo
    //    oscuro) y nebulosas de colores. Las estrellas van aparte (TrazoVR/Estrellas), de verdad en 3D.
    //  - _Modo 1 = SOBRE LAS NUBES: el cielo en degradado, el sol con su resplandor y la bruma del horizonte (el mar
    //    de nubes va aparte: TrazoVR/Nubes).
    // _Ruido: una textura de ruido que se repite (la hace FondosVivos.cs al empezar; cada canal, un tamaño distinto).
    Properties
    {
        _Ruido ("Ruido", 2D) = "gray" {}
        _Modo ("Modo (0 universo, 1 nubes)", Float) = 0
        _SolDir ("Dirección del sol", Vector) = (0.35, 0.18, 0.92, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+450" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Cielo"
            Tags { "LightMode" = "UniversalForward" }
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
                float _Modo;
                float4 _SolDir;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir        : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            float4 Ruido(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_Ruido, sampler_Ruido, uv);
            }

            // Una nebulosa: una mancha de color suave alrededor de la dirección c, con su ruido.
            half3 Nebulosa(float3 d, float3 c, half3 color, float ruido, float fuerza)
            {
                float k = saturate(dot(d, c));
                float mancha = pow(k, 14.0);
                return color * mancha * saturate(ruido * 1.6 - 0.25) * fuerza;
            }

            half3 Universo(float3 d)
            {
                // La Vía Láctea: una banda inclinada que da toda la vuelta al cielo.
                float3 n = normalize(float3(0.25, 0.92, 0.3));             // perpendicular a la banda
                float3 t = normalize(cross(n, float3(0.0, 0.0, 1.0)));
                float3 b = cross(n, t);
                float alto = dot(d, n);                                      // 0 = en medio de la banda
                float vuelta = atan2(dot(d, b), dot(d, t)) / 6.2831853 + 0.5;
                float2 uv = float2(vuelta * 4.0, alto * 2.0);
                float4 r1 = Ruido(uv);
                float4 r2 = Ruido(uv * 3.0 + float2(0.37, 0.11));
                float ancho = 0.17 + 0.09 * (r1.r - 0.5);
                float banda = exp(-alto * alto / (ancho * ancho));
                // Más brillante (y más cálida) hacia el centro de la galaxia.
                float nucleo = pow(saturate(dot(d, t) * 0.5 + 0.5), 4.0);
                float brillo = banda * (0.3 + 0.7 * r1.g) * (0.22 + 1.25 * nucleo);
                // El polvo oscuro que la parte por la mitad.
                float polvo = exp(-alto * alto / 0.0035) * smoothstep(0.35, 0.75, r2.r);
                brillo *= 1.0 - 0.8 * polvo;
                half3 tono = lerp(half3(0.55, 0.63, 0.92), half3(1.0, 0.84, 0.66), nucleo);
                half3 col = half3(0.004, 0.005, 0.014);                      // el negro azulado del espacio
                col += tono * brillo * 0.4;
                col += tono * banda * pow(r2.a, 6.0) * 0.7;                  // granitos de estrellas lejanas
                // Nebulosas de colores.
                col += Nebulosa(d, normalize(float3(0.8, 0.25, -0.5)), half3(0.85, 0.25, 0.55), r1.b, 0.22);
                col += Nebulosa(d, normalize(float3(-0.4, 0.5, -0.75)), half3(0.2, 0.55, 0.9), r2.g, 0.18);
                col += Nebulosa(d, normalize(float3(-0.85, -0.25, 0.4)), half3(0.5, 0.3, 0.95), r1.a, 0.16);
                return col;
            }

            half3 Nubes(float3 d)
            {
                float3 sol = normalize(_SolDir.xyz);
                float s = saturate(dot(d, sol));
                float h = pow(saturate(1.0 - max(d.y, 0.0)), 4.0);
                half3 col = lerp(half3(0.16, 0.36, 0.74), half3(0.76, 0.85, 0.95), h);
                // Más cálido hacia el sol, cerca del horizonte.
                col = lerp(col, half3(1.0, 0.86, 0.66), pow(s, 6.0) * 0.45 * h);
                // Bajo el horizonte: la bruma del mar de nubes (el mismo color que tienen las nubes a lo lejos).
                half3 bruma = lerp(half3(0.84, 0.88, 0.94), half3(1.0, 0.93, 0.82), pow(s, 4.0) * 0.5);
                col = lerp(col, bruma, smoothstep(0.02, -0.06, d.y));
                // El sol: su disco y su resplandor.
                col += half3(1.0, 0.95, 0.85) * smoothstep(0.99955, 0.9997, s) * 2.0;
                col += half3(1.0, 0.9, 0.72) * (pow(s, 60.0) * 0.35 + pow(s, 12.0) * 0.12);
                return min(col, 1.6);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 d = normalize(i.dir);
                half3 col;
                if (_Modo < 0.5)
                    col = Universo(d);
                else
                    col = Nubes(d);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
