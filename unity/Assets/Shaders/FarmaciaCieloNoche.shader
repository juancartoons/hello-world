Shader "FarmaciaVR/Cielo Noche"
{
    // Cielo de noche: degradado azul oscuro, resplandor naranja de la ciudad en el horizonte,
    // miles de estrellas de distintos tamaños, brillos y colores (titilan suave) y la luna.
    Properties
    {
        _ColorCenit ("Color arriba", Color) = (0.008, 0.012, 0.04, 1)
        _ColorHorizonte ("Color del horizonte", Color) = (0.05, 0.065, 0.14, 1)
        _ColorCiudad ("Resplandor de la ciudad", Color) = (0.30, 0.18, 0.12, 1)
        _ColorSuelo ("Color debajo del horizonte", Color) = (0.02, 0.02, 0.03, 1)
        _DensidadEstrellas ("Densidad de estrellas", Float) = 95
        _CantidadEstrellas ("Cantidad de estrellas", Range(0, 0.3)) = 0.09
        _BrilloEstrellas ("Brillo de las estrellas", Float) = 1.6
        _DireccionLuna ("Dirección de la luna", Vector) = (0.3, 0.75, 0.6, 0)
        _ColorLuna ("Color de la luna", Color) = (0.96, 0.94, 0.86, 1)
        _TamanoLuna ("Tamaño de la luna", Range(0.005, 0.06)) = 0.026
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorCenit;
                float4 _ColorHorizonte;
                float4 _ColorCiudad;
                float4 _ColorSuelo;
                float _DensidadEstrellas;
                float _CantidadEstrellas;
                float _BrilloEstrellas;
                float4 _DireccionLuna;
                float4 _ColorLuna;
                float _TamanoLuna;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direccion : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 Azar3(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            // Una capa de estrellas: cada celda del espacio puede tener una estrella en un punto al azar.
            float3 Estrellas(float3 dir, float densidad, float cantidad, float semilla)
            {
                float3 p = dir * densidad;
                float3 celda = floor(p);
                float3 h = Azar3(celda + semilla);
                if (h.x > cantidad)
                    return float3(0, 0, 0);
                float3 centro = celda + 0.25 + 0.5 * Azar3(celda + semilla + 17.0);
                float d = length(p - centro);
                float valor = h.x / cantidad;                     // 0..1
                float brillo = pow(1.0 - valor, 4.0) * 0.9 + 0.1; // pocas muy brillantes, muchas tenues
                float tamano = lerp(0.07, 0.2, pow(1.0 - valor, 3.0));
                float forma = saturate(1.0 - d / tamano);
                forma *= forma;
                float titila = 0.8 + 0.2 * sin(_Time.y * (1.5 + h.z * 3.0) + h.y * 6.2831);
                float3 tinte = lerp(float3(0.72, 0.82, 1.0), float3(1.0, 0.88, 0.72), h.z);
                return tinte * forma * brillo * titila;
            }

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.direccion = v.positionOS.xyz;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 dir = normalize(i.direccion);
                float y = dir.y;

                // Degradado del cielo y resplandor de la ciudad cerca del horizonte
                float3 color = lerp(_ColorHorizonte.rgb, _ColorCenit.rgb, pow(saturate(y), 0.45));
                color += _ColorCiudad.rgb * exp(-max(y, 0.0) * 9.0) * 0.7;
                color = lerp(_ColorSuelo.rgb, color, smoothstep(-0.04, 0.01, y));

                // Luna con un brillo suave alrededor
                float3 haciaLuna = normalize(_DireccionLuna.xyz);
                float angulo = sqrt(max(0.0, 2.0 * (1.0 - dot(dir, haciaLuna))));
                float disco = 1.0 - smoothstep(_TamanoLuna * 0.92, _TamanoLuna, angulo);
                float3 local = (dir - haciaLuna) / _TamanoLuna;
                float manchas = 0.82 + 0.18 * Azar3(floor(local * 3.0) + 5.0).x;
                float halo = exp(-angulo / (_TamanoLuna * 2.5)) * 0.22 + exp(-angulo / (_TamanoLuna * 14.0)) * 0.07;
                color += _ColorLuna.rgb * (disco * manchas * 1.2 + halo);

                // Estrellas (dos capas), más tenues cerca del horizonte por la luz de la ciudad
                float3 estrellas = Estrellas(dir, _DensidadEstrellas, _CantidadEstrellas, 0.0)
                                 + Estrellas(dir, _DensidadEstrellas * 1.9, _CantidadEstrellas * 0.8, 41.0) * 0.6;
                estrellas *= smoothstep(0.03, 0.35, y) * (1.0 - disco) * _BrilloEstrellas;
                color += estrellas;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
