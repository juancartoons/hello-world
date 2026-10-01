Shader "FarmaciaVR/Realista"
{
    // Iluminación "realista" sencilla para el exterior: sombreado suave con el sol, luz del cielo
    // y brillo (reflejo del sol) en superficies como el asfalto, vidrios y carros. Sin contorno.
    // Usa los colores de la malla; el alfa del color dice qué tanto brilla cada pieza.
    Properties
    {
        _Brillo ("Intensidad del brillo", Range(0, 2)) = 0.8
        _Dureza ("Dureza del brillo", Range(4, 256)) = 40
        _AmbienteCielo ("Luz del cielo", Color) = (0.55, 0.63, 0.75, 1)
        _AmbienteSuelo ("Luz rebotada del suelo", Color) = (0.36, 0.34, 0.30, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Realista"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Brillo;
                float _Dureza;
                float4 _AmbienteCielo;
                float4 _AmbienteSuelo;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                Light sol = GetMainLight();
                float3 n = normalize(i.normalWS);
                float3 v = normalize(_WorldSpaceCameraPos - i.positionWS);
                float3 h = normalize(sol.direction + v);

                float difusa = saturate(dot(n, sol.direction));
                float3 ambiente = lerp(_AmbienteSuelo.rgb, _AmbienteCielo.rgb, n.y * 0.5 + 0.5);
                float brillo = pow(saturate(dot(n, h)), _Dureza) * _Brillo * i.color.a;

                float3 color = i.color.rgb * (ambiente + difusa * sol.color) + brillo * sol.color;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
