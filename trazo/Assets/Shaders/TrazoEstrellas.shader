Shader "TrazoVR/Estrellas"
{
    // ESTRELLAS (v42, fondo vivo Universo): cada estrella es un cuadradito que siempre te mira, con un punto de luz
    // suave en el medio (se suma a lo de atrás).
    //  - _Envolver 0 = las LEJANAS: en la esfera del cielo, giran con él.
    //  - _Envolver 1 = las CERCANAS, de verdad en 3D (cada ojo las ve distinto): llenan una caja de _Caja metros
    //    alrededor de ti (la que se sale por un lado vuelve a entrar por el otro, ya apagada) y con _Avance pasan
    //    despacio a tu lado, como si viajaras. Cerca de la orilla de la caja y muy cerca de tus ojos se apagan suave.
    // Color por vértice (a = brillo). uv = esquina (-1..1). uv2.x = tamaño (metros), uv2.y = fase (titileo suave).
    Properties
    {
        _Envolver ("Cercanas (1) o lejanas (0)", Float) = 0
        _Caja ("Caja de las cercanas (m)", Float) = 80
        _Avance ("Avance (m)", Float) = 0
        _Brillo ("Brillo", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+460" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Estrellas"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _JCCentroVivo;   // tu cabeza (la pone FondosVivos.cs cada cuadro)

            CBUFFER_START(UnityPerMaterial)
                float _Envolver;
                float _Caja;
                float _Avance;
                float _Brillo;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                float2 uv2        : TEXCOORD1;
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
                float3 c = v.positionOS.xyz;
                float alfa = 1.0;
                if (_Envolver > 0.5)
                {
                    float3 centro = TransformWorldToObject(_JCCentroVivo.xyz);
                    float3 rel = c - centro;
                    rel.z -= _Avance;                                   // avanzas: las estrellas vienen hacia ti
                    rel -= _Caja * floor(rel / _Caja + 0.5);            // siempre a tu alrededor
                    float borde = max(abs(rel.x), max(abs(rel.y), abs(rel.z))) / (0.5 * _Caja);
                    alfa = (1.0 - smoothstep(0.7, 1.0, borde)) * smoothstep(0.6, 1.6, length(rel));
                    c = centro + rel;
                }
                float3 centroWS = TransformObjectToWorld(c);
                float3 derecha = UNITY_MATRIX_V[0].xyz;
                float3 arriba = UNITY_MATRIX_V[1].xyz;
                float dist = distance(centroWS, GetCameraPositionWS());
                float tam = max(v.uv2.x, dist * 0.0016);                // nunca más chica que ~0.1° (si no, parpadea)
                float3 posWS = centroWS + (derecha * v.uv.x + arriba * v.uv.y) * tam;
                o.positionCS = TransformWorldToHClip(posWS);
                float titila = 0.9 + 0.1 * sin(_Time.y * (0.7 + v.uv2.y * 1.3) + v.uv2.y * 40.0);
                o.color = float4(v.color.rgb, v.color.a * alfa * _Brillo * titila);
                o.uv = v.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float r2 = dot(i.uv, i.uv);
                float nucleo = saturate(1.0 - r2 * 4.0);
                float halo = saturate(1.0 - r2);
                float a = nucleo * nucleo + halo * halo * halo * 0.35;
                return half4(i.color.rgb, i.color.a * a);
            }
            ENDHLSL
        }
    }
}
