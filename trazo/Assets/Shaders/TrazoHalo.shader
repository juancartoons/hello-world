Shader "TrazoVR/Halo"
{
    // Halo suave DETRÁS de las líneas, para que no se pierdan sobre un fondo 360 o sobre la realidad.
    // Usa la misma malla que la línea (TrazoVR/Linea), un poco más ancha y un poco más atrás:
    // - Cinta (uv.y = 0): se abre hacia los lados igual que la línea, más ancha, y se desvanece hacia afuera.
    // - Tubo (uv.y = 1): se infla un poco y solo se ve la parte de atrás (un borde suave alrededor del tubo).
    // Solo lo dibuja la cámara de tus ojos (Dibujo.LateUpdate): no sale en fotos, videos ni SVG.
    Properties
    {
        _BaseColor ("Color del halo", Color) = (1, 1, 1, 0.85)
        _Ancho ("Ancho extra (metros)", Float) = 0.0035
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-20" }

        Pass
        {
            Name "Halo"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            // El alfa también se acumula bien (importa con la realidad: passthrough detrás).
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "TrazoTemblor.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Ancho;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float2 uv2        : TEXCOORD1;
                float4 vivoA      : TEXCOORD2;
                float4 vivoB      : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 halo       : TEXCOORD0; // cinta: x = de -1 a 1 a lo ancho, y = hasta dónde llega la línea (0 a 1)
                float3 normalWS   : TEXCOORD1;
                float3 posWS      : TEXCOORD2;
                float esTubo      : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 original = TransformObjectToWorld(v.positionOS.xyz);
                float fase = TrazoFase(v.vivoA, v.vivoB);
                float3 posWS = TrazoTemblar(original, v.vivoA, v.vivoB, fase);
                float3 nWS = float3(0, 1, 0);
                o.halo = float2(0, 0);
                if (v.uv.y < 0.5)
                {
                    float3 dirWS = TransformObjectToWorldDir(v.normalOS);
                    float3 haciaCamara = normalize(GetCameraPositionWS() - posWS);
                    float3 lado = cross(dirWS, haciaCamara);
                    float largo = length(lado);
                    if (largo > 1e-4)
                        lado = lado / largo;
                    else
                        lado = normalize(cross(dirWS, float3(0, 1, 0)) + float3(1e-3, 0, 0));
                    float3x3 m = (float3x3)GetObjectToWorldMatrix();
                    float escala = length(float3(m[0][0], m[1][0], m[2][0]));
                    float medio = abs(v.uv.x) * escala;
                    posWS += TrazoHebra(original, v.uv2.x, v.uv2.y, medio, v.vivoA, v.vivoB, fase);
                    // Medio grosor de la línea aquí (con el grosor vivo) y el halo alrededor.
                    float medioLinea = medio * TrazoGrosorVivo(original, v.uv2.x, v.vivoA, v.vivoB, fase);
                    float total = medioLinea + _Ancho + medioLinea * 0.6;
                    float lad = v.uv.x < 0 ? -1.0 : 1.0;
                    posWS += lado * (lad * total);
                    // Un poquito más atrás que la línea: así la línea siempre queda encima de su halo.
                    posWS -= haciaCamara * 0.003;
                    o.halo = float2(lad, medioLinea / max(total, 1e-6));
                }
                else
                {
                    // Tubo: un poco más gordo (hacia afuera).
                    nWS = TransformObjectToWorldNormal(v.normalOS);
                    posWS += normalize(nWS) * _Ancho;
                }

                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = nWS;
                o.posWS = posWS;
                o.esTubo = v.uv.y;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float alfa;
                if (i.esTubo < 0.5)
                {
                    // Lleno junto a la línea y se desvanece suave hacia afuera.
                    float s = abs(i.halo.x);
                    float c = saturate(i.halo.y);
                    float k = saturate((s - c) / max(1.0 - c, 1e-4));
                    alfa = (1.0 - k) * (1.0 - k);
                }
                else
                {
                    // Solo la parte de atrás del tubo inflado (la de adelante taparía el tubo).
                    float3 vista = normalize(GetCameraPositionWS() - i.posWS);
                    float nd = dot(normalize(i.normalWS), vista);
                    clip(-nd);
                    alfa = saturate(-nd);
                    alfa *= alfa;
                }
                return half4(_BaseColor.rgb, _BaseColor.a * alfa);
            }
            ENDHLSL
        }
    }
}
