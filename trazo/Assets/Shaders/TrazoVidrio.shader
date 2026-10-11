Shader "TrazoVR/Vidrio"
{
    // VIDRIO VIVO (v36) para los menús y la paleta: vidrio esmerilado.
    // El truco: el desenfoque de verdad es carísimo para el Quest; pero ya conocemos la foto del fondo 360, así que
    // el vidrio solo "mira" una versión chiquita y borrosa de esa misma foto en la dirección en que lo ves
    // (_JCFondoBorroso, la prepara Escenario). Sin fondo 360 usa un color (_JCFondoColor).
    // En el borde, un brillo que se mueve cuando mueves la cabeza (como el cristal de un reloj caro).
    // _Atencion (0 a 1, la pone Respira): si no lo miras, el vidrio se vuelve más transparente.
    Properties
    {
        _BaseColor ("Tinte del vidrio", Color) = (0.97, 0.97, 0.98, 1)
        _Opacidad ("Opacidad", Range(0, 1)) = 0.9
        _Esmerilado ("Cuánto blanco lleva", Range(0, 1)) = 0.62
        [HideInInspector] _Atencion ("Atención", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-10" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Vidrio"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "TrazoLuz.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Opacidad;
                float _Esmerilado;
                float _Atencion;
            CBUFFER_END

            TEXTURE2D(_JCFondoBorroso);
            SAMPLER(sampler_JCFondoBorroso);
            float4x4 _JCFondoRot;   // del mundo al fondo 360 (el fondo puede estar girado)
            float _JCFondoHay;      // 1 = hay fondo 360
            float4 _JCFondoColor;   // sin fondo 360: el color de detrás

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 posWS      : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
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
                o.uv = v.uv;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 V = normalize(i.posWS - GetCameraPositionWS());   // de tus ojos hacia el vidrio
                float3 N = normalize(i.normalWS);
                if (dot(N, V) > 0.0)
                    N = -N;                                               // la cara que ves mira hacia ti
                float3 fondo = _JCFondoColor.rgb;
                if (_JCFondoHay > 0.5)
                {
                    // Lo que hay detrás del vidrio en el fondo 360 (borroso): la misma dirección en que lo miras.
                    float3 d = mul((float3x3)_JCFondoRot, V);
                    float2 uv = float2(atan2(d.x, d.z) * (0.5 * INV_PI) + 0.5, asin(clamp(d.y, -1.0, 1.0)) * INV_PI + 0.5);
                    fondo = SAMPLE_TEXTURE2D_LOD(_JCFondoBorroso, sampler_JCFondoBorroso, uv, 0).rgb;
                }
                float3 col = lerp(fondo, _BaseColor.rgb, _Esmerilado);
                // El borde (con el brillo que se mueve con tu cabeza) y un reflejo suave del cielo.
                float b = min(min(i.uv.x, 1.0 - i.uv.x), min(i.uv.y, 1.0 - i.uv.y));
                float borde = 1.0 - smoothstep(0.0, 0.03, b);
                float3 R = reflect(V, N);
                float reflejo = pow(saturate(dot(R, normalize(float3(-0.35, 0.85, 0.4)))), 16.0);
                float fres = pow(1.0 - saturate(dot(-V, N)), 3.0);
                col += borde * (0.16 + 0.55 * reflejo) + reflejo * 0.07 + fres * 0.1;
                // La luz de la punta del dedo.
                col += TrazoColorLuz * (0.3 * TrazoLuzDedo(i.posWS));
                float a = _Opacidad * lerp(0.45, 1.0, saturate(_Atencion));
                a = saturate(a + borde * 0.15);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
