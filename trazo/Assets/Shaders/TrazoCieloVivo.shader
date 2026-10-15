Shader "TrazoVR/CieloVivo"
{
    // FONDOS VIVOS (v42): el cielo a tu alrededor (una esfera grande que va contigo), calculado aquí mismo. No es una
    // foto: lo dibuja la app.
    //  - _Modo 0 = UNIVERSO: el negro del espacio y nebulosas de colores pequeñas y lejanas (v43: sin la franja gris).
    //    Las estrellas van aparte (TrazoVR/Estrellas), de verdad en 3D.
    //  - _Modo 1 = SOBRE LAS NUBES: el cielo en degradado, el sol con su resplandor y la bruma del horizonte (el mar
    //    de nubes va aparte: TrazoVR/Nubes).
    //  - v45: _Modo 2 = ATARDECER EN EL DESIERTO: el cielo del atardecer (violeta arriba, rosado, naranja en el
    //    horizonte), el sol bajito con su resplandor, cirros rosados y el polvo que el viento levanta a lo lejos.
    //  - v45: _Modo 3 = las DUNAS: la malla trae su color en cada vértice (lo calcula FondosVivos.cs).
    // _Ruido: una textura de ruido que se repite (la hace FondosVivos.cs al empezar; cada canal, un tamaño distinto).
    Properties
    {
        _Ruido ("Ruido", 2D) = "gray" {}
        _Modo ("Modo (0 universo, 1 nubes, 2 desierto, 3 dunas)", Float) = 0
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
                half4 color       : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir        : TEXCOORD0;
                half4 color       : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                o.color = v.color;
                return o;
            }

            float4 Ruido(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_Ruido, sampler_Ruido, uv);
            }

            // Una nebulosa (v43): una mancha de color pequeña y lejana alrededor de la dirección c. Su forma se calcula
            // en un plano propio (así no se estira) con dos capas de ruido, y se apaga redonda hacia su orilla.
            half3 Nebulosa(float3 d, float3 c, half3 color, float2 desplaza, float fuerza)
            {
                float k = dot(d, c);
                float3 t1 = normalize(cross(c, float3(0.0, 1.0, 0.0)));
                float3 t2 = cross(c, t1);
                float2 p = float2(dot(d, t1), dot(d, t2)) / 0.1;
                float4 a = Ruido(p * 0.35 + desplaza);
                float4 b = Ruido(p * 0.9 + desplaza * 1.7);
                float forma = a.r * 0.65 + b.g * 0.35;
                float caida = saturate(1.0 - dot(p, p) * 0.45);
                float nube = smoothstep(0.3, 0.85, forma * caida + caida * 0.2) * smoothstep(0.85, 0.97, k);
                return color * nube * fuerza;
            }

            // v43: sin la franja gris de la Vía Láctea (parecía nubes estiradas): el negro del espacio y unas pocas
            // nebulosas de colores, chiquitas y lejanas. Las estrellas van aparte.
            half3 Universo(float3 d)
            {
                half3 col = half3(0.004, 0.005, 0.014);
                col += Nebulosa(d, normalize(float3(0.8, 0.25, -0.5)), half3(0.85, 0.25, 0.55), float2(0.1, 0.3), 0.22);
                col += Nebulosa(d, normalize(float3(-0.4, 0.5, -0.75)), half3(0.2, 0.55, 0.9), float2(0.6, 0.2), 0.18);
                col += Nebulosa(d, normalize(float3(-0.85, -0.25, 0.4)), half3(0.5, 0.3, 0.95), float2(0.35, 0.75), 0.16);
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

            // v45: el cielo del atardecer en el desierto.
            half3 Desierto(float3 d)
            {
                float3 sol = normalize(_SolDir.xyz);
                float h = d.y;
                float s = saturate(dot(d, sol));
                float alto = saturate(h * 1.6);
                half3 col = lerp(half3(1.0, 0.6, 0.32), half3(0.88, 0.45, 0.54), smoothstep(0.0, 0.4, alto));
                col = lerp(col, half3(0.22, 0.21, 0.47), smoothstep(0.45, 1.0, alto));
                // Más dorado hacia el sol (cerca del horizonte).
                col = lerp(col, half3(1.0, 0.8, 0.48), pow(s, 6.0) * 0.65 * (1.0 - alto));
                // Cirros finos, iluminados de rosado y dorado por abajo.
                float2 uv = d.xz / max(h, 0.06) * 0.12;
                float n = Ruido(uv * 0.5 + float2(_Time.y * 0.003, 0.0)).r * 0.6 + Ruido(uv * 1.6).g * 0.4;
                float nube = smoothstep(0.55, 0.8, n) * smoothstep(0.04, 0.2, h) * (1.0 - smoothstep(0.55, 0.9, h));
                col = lerp(col, half3(1.0, 0.68, 0.62) + pow(s, 3.0) * half3(0.25, 0.15, 0.0), nube * 0.5);
                // El sol: su disco y su resplandor.
                col += half3(1.0, 0.88, 0.62) * smoothstep(0.9993, 0.9996, s) * 1.8;
                col += half3(1.0, 0.62, 0.3) * (pow(s, 40.0) * 0.55 + pow(s, 6.0) * 0.14);
                // Unas pocas estrellas que ya se asoman arriba.
                float2 e = d.xz / max(d.y, 0.2) * 9.0;
                float estrella = step(0.985, Ruido(e).a) * smoothstep(0.6, 0.95, h);
                col += estrella * 0.5;
                // El viento: polvo a ras del horizonte que pasa despacio.
                float ang = atan2(d.x, d.z);
                float polvo = Ruido(float2(ang * 1.2 + _Time.y * 0.012, h * 6.0)).b;
                col = lerp(col, half3(1.0, 0.76, 0.56), smoothstep(0.07, 0.0, abs(h - 0.012)) * smoothstep(0.35, 0.8, polvo) * 0.5);
                // Bajo el horizonte: la bruma (el mismo color de las dunas a lo lejos).
                col = lerp(col, half3(0.96, 0.64, 0.46), smoothstep(0.015, -0.05, h));
                return col;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                if (_Modo > 2.5)
                    return half4(i.color.rgb, 1.0);   // las dunas (color por vértice)
                float3 d = normalize(i.dir);
                half3 col;
                if (_Modo < 0.5)
                    col = Universo(d);
                else if (_Modo < 1.5)
                    col = Nubes(d);
                else
                    col = Desierto(d);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
