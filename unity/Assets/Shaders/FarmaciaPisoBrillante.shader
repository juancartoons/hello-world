Shader "FarmaciaVR/Piso Brillante"
{
    // Piso de mármol súper pulido: deja ver el reflejo del techo, las luces y los muebles
    // (el reflejo es una copia "de cabeza" de la farmacia debajo del piso).
    // El piso se dibuja encima del reflejo, semitransparente: mientras más de lado lo miras, más refleja.
    Properties
    {
        _BaseColor ("Color del mármol", Color) = (0.95, 0.95, 0.97, 1)
        _ColorVetas ("Color de las vetas", Color) = (0.66, 0.68, 0.74, 1)
        _Vetas ("Intensidad de las vetas", Range(0, 1)) = 0.45
        _ColorJunta ("Color de las juntas", Color) = (0.78, 0.79, 0.83, 1)
        _Baldosa ("Tamaño de las baldosas (m)", Float) = 1.2
        _OpacidadCerca ("Opacidad mirando hacia abajo", Range(0, 1)) = 0.5
        _OpacidadLejos ("Opacidad a lo lejos (más reflejo)", Range(0, 1)) = 0.18
    }

    SubShader
    {
        // Justo antes de los transparentes: así el cielo ya está dibujado detrás (se refleja por las ventanas).
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-1" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "PisoBrillante"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ColorVetas;
                float _Vetas;
                float4 _ColorJunta;
                float _Baldosa;
                float _OpacidadCerca;
                float _OpacidadLejos;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Azar(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Ruido(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Azar(i), Azar(i + float2(1, 0)), u.x),
                            lerp(Azar(i + float2(0, 1)), Azar(i + float2(1, 1)), u.x), u.y);
            }

            float Fractal(float2 p)
            {
                return Ruido(p) * 0.55 + Ruido(p * 2.1 + 3.7) * 0.3 + Ruido(p * 4.3 + 7.1) * 0.15;
            }

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 p = i.positionWS.xz;

                // Vetas finas del mármol
                float n = Fractal(p * 0.55);
                float onda = abs(sin((p.x * 0.8 + p.y * 0.45) * 1.6 + n * 6.0));
                float veta = 1.0 - smoothstep(0.0, 0.07, onda);
                veta += (1.0 - smoothstep(0.0, 0.04, abs(sin((p.x * -0.4 + p.y * 0.9) * 2.3 + n * 9.0)))) * 0.5;
                float3 color = lerp(_BaseColor.rgb, _ColorVetas.rgb, saturate(veta) * _Vetas);
                color *= 0.985 + 0.03 * Ruido(p * 3.0);

                // Juntas entre baldosas (finitas y suaves, sin parpadeo a lo lejos)
                float2 g = abs(frac(p / _Baldosa + 0.5) - 0.5) * _Baldosa;
                float d = min(g.x, g.y);
                float ancho = max(fwidth(d), 0.0015);
                float junta = 1.0 - smoothstep(0.0, ancho * 1.5, d);
                color = lerp(color, _ColorJunta.rgb, junta * 0.7);

                // Más reflejo mientras más de lado se mira (efecto Fresnel)
                float3 haciaCamara = normalize(_WorldSpaceCameraPos - i.positionWS);
                float fresnel = pow(1.0 - saturate(haciaCamara.y), 3.0);
                float alfa = lerp(_OpacidadCerca, _OpacidadLejos, fresnel);
                alfa = lerp(alfa, 0.85, junta * 0.6);
                return half4(color, alfa);
            }
            ENDHLSL
        }
    }
}
