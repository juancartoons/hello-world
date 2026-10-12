Shader "TrazoVR/Orbe"
{
    // EL BORRADOR (v38): una esfera de vidrio con el contorno de un color fino (oro rosa sobre las líneas, champaña
    // sobre los rellenos): casi transparente al centro, con la orilla iluminada y un brillito. _Brillo la enciende
    // un poco cuando toca algo que se puede borrar.
    Properties
    {
        _BaseColor ("Color", Color) = (0.88, 0.6, 0.55, 1)
        _Centro ("Opacidad del centro", Range(0, 1)) = 0.2
        _Brillo ("Brillo (tocando algo)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Orbe"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Centro;
                float _Brillo;
            CBUFFER_END

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
                float3 V = normalize(GetCameraPositionWS() - i.posWS);   // hacia tus ojos
                float3 N = normalize(i.normalWS);
                float fres = pow(1.0 - saturate(dot(N, V)), 2.5);         // 1 en la orilla
                float3 col = lerp(_BaseColor.rgb, float3(1.0, 1.0, 1.0), 0.2 * fres + 0.3 * _Brillo);
                float a = lerp(_Centro + 0.2 * _Brillo, 0.95, fres);
                // Un brillito, como una perla de vidrio.
                float spec = pow(saturate(dot(reflect(-V, N), normalize(float3(-0.3, 0.85, -0.4)))), 24.0);
                col += spec * 0.5;
                a = saturate(a + spec * 0.5);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
