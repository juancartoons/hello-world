Shader "TrazoVR/Linea"
{
    // Línea negra tipo vector para TrazoVR.
    // - Cinta (uv.y = 0): la malla guarda el centro de la línea y su dirección; aquí se abre
    //   hacia los lados mirando siempre a la cámara, así se ve como un trazo de tinta limpio.
    // - Tubo (uv.y = 1): la malla ya es un tubo 3D; se le da un brillo suave arriba para que se lea el volumen.
    // Compatible con URP, SRP Batcher y estéreo de Meta Quest (multiview / single-pass instanced).
    Properties
    {
        _BaseColor ("Color de la línea", Color) = (0, 0, 0, 1)
        _ColorLuz ("Brillo del tubo", Color) = (0.45, 0.45, 0.45, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Linea"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ColorLuz;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;    // cinta: dirección de la línea / tubo: normal
                float2 uv         : TEXCOORD0; // x: medio grosor con signo (cinta), y: 0 cinta / 1 tubo
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float esTubo      : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS = float3(0, 1, 0);
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
                    posWS += lado * (v.uv.x * escala);
                }
                else
                {
                    nWS = TransformObjectToWorldNormal(v.normalOS);
                }

                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = nWS;
                o.esTubo = v.uv.y;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float luz = saturate(dot(normalize(i.normalWS), normalize(float3(0.3, 0.9, -0.3))));
                float k = step(0.5, i.esTubo) * luz * luz;
                return half4(lerp(_BaseColor.rgb, _ColorLuz.rgb, k), 1);
            }
            ENDHLSL
        }
    }
}
