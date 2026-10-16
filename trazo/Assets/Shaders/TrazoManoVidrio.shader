Shader "TrazoVR/ManoVidrio"
{
    // MANOS DE VIDRIO (v36): casi transparentes por dentro, con el contorno iluminado (fresnel), y las yemas de los
    // índices brillan con la luz de la punta del dedo (ver TrazoLuz.hlsl).
    // v45: EL DEDO JERINGA: el índice derecho se llena de color desde la yema hacia el nudillo (ControlManosJeringa).
    // v46: sigue las falanges (no una línea recta), con un poco más de grosor.
    Properties
    {
        _BaseColor ("Color del vidrio", Color) = (0.72, 0.86, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ManoVidrio"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "TrazoLuz.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            // v45 (globales, los pone ControlManosJeringa): xyz = la yema / el nudillo del índice derecho;
            // _JCJeringaPunta.w = cuánto está lleno (0 a 1); _JCJeringaNudillo.w = 1 si hay color cargado.
            float4 _JCJeringaPunta;
            float4 _JCJeringaNudillo;
            float4 _JCJeringaColor;
            // v46: las falanges del medio (yema → A → B → nudillo): así el color sigue el dedo aunque esté doblado.
            float4 _JCJeringaA;
            float4 _JCJeringaB;

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

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 V = normalize(i.posWS - GetCameraPositionWS());
                float3 N = normalize(i.normalWS);
                float fres = pow(1.0 - saturate(abs(dot(V, N))), 2.2);
                float luz = TrazoLuzDedos(i.posWS);
                float3 col = _BaseColor.rgb * (0.35 + 0.65 * fres) + fres * 0.2 + TrazoColorLuz * (0.9 * luz);
                float a = saturate(0.1 + 0.6 * fres + 0.55 * luz);
                // v45: el índice derecho lleno de color, desde la yema hasta donde llegue el "líquido".
                if (_JCJeringaNudillo.w > 0.5)
                {
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
                    float lleno = step(t, _JCJeringaPunta.w) * (1.0 - smoothstep(0.0125, 0.017, d));
                    // La orilla del líquido, un poquito más clara (como el menisco de una jeringa).
                    float orilla = lleno * (1.0 - smoothstep(0.0, 0.06, _JCJeringaPunta.w - t)) * 0.25;
                    float3 tinta = _JCJeringaColor.rgb * (0.78 + 0.3 * fres) + orilla;
                    col = lerp(col, tinta, lleno);
                    a = lerp(a, 0.96, lleno);
                }
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
