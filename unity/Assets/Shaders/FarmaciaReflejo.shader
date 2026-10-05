Shader "FarmaciaVR/Reflejo"
{
    // Para la copia "de cabeza" de la farmacia que se ve reflejada en el piso brillante.
    // Igual que el toon (colores planos y una sombra), pero sin contorno y con la luz calculada
    // como en el objeto original (para que el reflejo tenga los mismos colores).
    // "Sin luz" = se ve siempre al 100% (para los LEDs y las lámparas).
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Color de sombra (multiplica)", Color) = (0.78, 0.82, 0.9, 1)
        [ToggleUI] _UseVertexColor ("Usar colores de la malla", Float) = 1
        [ToggleUI] _SinLuz ("Sin luz (brilla siempre)", Float) = 0
        _Intensidad ("Intensidad del reflejo", Range(0, 1.5)) = 0.95
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+450" }

        Pass
        {
            Name "Reflejo"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ShadowColor;
                float _UseVertexColor;
                float _SinLuz;
                float _Intensidad;
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
                float3 normalWS : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = lerp(float4(1, 1, 1, 1), v.color, _UseVertexColor);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // La copia está volteada de cabeza: se voltea la normal otra vez para iluminar igual que el original.
                float3 n = normalize(i.normalWS);
                n.y = -n.y;
                float ndl = dot(n, GetMainLight().direction);
                float luzPlana = smoothstep(0.03, 0.07, ndl);
                float3 sombra = lerp(_ShadowColor.rgb, float3(1, 1, 1), luzPlana);
                float3 color = _BaseColor.rgb * i.color.rgb * lerp(sombra, float3(1, 1, 1), _SinLuz);
                return half4(color * _Intensidad, 1);
            }
            ENDHLSL
        }
    }
}
