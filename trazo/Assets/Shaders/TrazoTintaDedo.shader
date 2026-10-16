Shader "TrazoVR/TintaDedo"
{
    // EL DEDO MANCHADO DE TINTA (v47): una segunda pasada sobre la malla de TU mano derecha (la que el visor dibuje,
    // sea de vidrio o la normal de Meta). Solo pinta la PIEL de la punta del índice, como si lo hubieras metido en
    // tinta: desde la yema hacia el nudillo, con el borde un poco irregular y un brillo húmedo. En el resto de la mano
    // no dibuja nada (se descarta). Los datos los pone ControlManosJeringa cada cuadro (globales _JCJeringa*).
    Properties
    {
        _BaseColor ("Sin uso", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+20" "IgnoreProjector" = "True" }

        Pass
        {
            Name "TintaDedo"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            // xyz = yema / falange de la punta / falange del medio / nudillo del índice derecho.
            // _JCJeringaPunta.w = hasta dónde llega la tinta (0 = nada, 1 = el nudillo); _JCJeringaNudillo.w = 1 si hay tinta.
            float4 _JCJeringaPunta;
            float4 _JCJeringaA;
            float4 _JCJeringaB;
            float4 _JCJeringaNudillo;
            float4 _JCJeringaColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 posWS      : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.posWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            // La distancia del punto p al tramo a-b, y en qué parte del dedo cae (0 = yema, 1 = nudillo).
            void Tramo(float3 p, float3 a, float3 b, float desde, float total, inout float dMin, inout float sMin)
            {
                float3 ab = b - a;
                float t = saturate(dot(p - a, ab) / max(dot(ab, ab), 1e-8));
                float d = distance(p, a + ab * t);
                if (d < dMin)
                {
                    dMin = d;
                    sMin = (desde + t * length(ab)) / total;
                }
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                if (_JCJeringaNudillo.w < 0.5)
                    discard;
                float3 p0 = _JCJeringaPunta.xyz;
                float3 p1 = _JCJeringaA.xyz;
                float3 p2 = _JCJeringaB.xyz;
                float3 p3 = _JCJeringaNudillo.xyz;
                float l1 = distance(p0, p1);
                float l2 = distance(p1, p2);
                float total = max(l1 + l2 + distance(p2, p3), 1e-4);
                float d = 1e5;
                float t = 0.0;
                Tramo(i.posWS, p0, p1, 0.0, total, d, t);
                Tramo(i.posWS, p1, p2, l1, total, d, t);
                Tramo(i.posWS, p2, p3, l1 + l2, total, d, t);
                // Solo la piel del índice (no los dedos de al lado): cerca del eje del dedo.
                float enDedo = 1.0 - smoothstep(0.013, 0.018, d);
                // La yema misma (un poquito más allá del hueso de la punta) también se mancha.
                float yema = 1.0 - smoothstep(0.004, 0.014, distance(i.posWS, p0));
                // El borde de la tinta: irregular, como una mancha (ondas según el lugar en la piel).
                float3 q = i.posWS * 900.0;
                float onda = sin(q.x + sin(q.y * 0.7)) * sin(q.z * 0.8 + q.x * 0.3) * 0.035 + sin(q.y * 0.45 + q.z * 0.2) * 0.02;
                float borde = _JCJeringaPunta.w + onda;
                float tinta = max(smoothstep(borde + 0.015, borde - 0.015, t), yema * step(0.02, _JCJeringaPunta.w)) * enDedo;
                if (tinta < 0.02)
                    discard;
                // Brillo húmedo: más claro en las orillas que miran de lado (fresnel) y un reflejo suave arriba.
                float3 V = normalize(GetCameraPositionWS() - i.posWS);
                float3 N = normalize(i.normalWS);
                float fres = pow(1.0 - saturate(abs(dot(V, N))), 2.5);
                float reflejo = pow(saturate(dot(reflect(-V, N), float3(0.0, 1.0, 0.0))), 18.0);
                half3 col = _JCJeringaColor.rgb * (0.82 + 0.25 * fres) + reflejo * 0.35;
                return half4(col, tinta * 0.97);
            }
            ENDHLSL
        }
    }
}
