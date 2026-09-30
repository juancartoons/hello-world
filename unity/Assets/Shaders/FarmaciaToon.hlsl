#ifndef FARMACIA_TOON_INCLUDED
#define FARMACIA_TOON_INCLUDED

// Shader toon estilo "vector": colores planos, una sola sombra de borde nítido
// y contorno de grosor uniforme en pantalla (no depende de la distancia).
// Compatible con URP, SRP Batcher y estéreo de Meta Quest (multiview / single-pass instanced).

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _ShadowColor;
    float _ShadowThreshold;
    float _ShadowSoftness;
    float _UseVertexColor;
    float4 _OutlineColor;
    float _OutlineWidth;
    float _OutlineMode;
CBUFFER_END

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 color      : COLOR;
    float3 outlineDir : TEXCOORD3; // dirección del contorno (mallas generadas por FarmaciaVR)
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsToon
{
    float4 positionCS : SV_POSITION;
    float3 normalWS   : TEXCOORD0;
    float4 color      : COLOR;
    UNITY_VERTEX_OUTPUT_STEREO
};

struct VaryingsBorde
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsToon ToonVert(Attributes v)
{
    VaryingsToon o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
    o.color = lerp(float4(1, 1, 1, 1), v.color, _UseVertexColor);
    return o;
}

half4 ToonFrag(VaryingsToon i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    Light luz = GetMainLight();
    float ndl = dot(normalize(i.normalWS), luz.direction);
    float luzPlana = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, ndl);
    float3 color = _BaseColor.rgb * i.color.rgb;
    color *= lerp(_ShadowColor.rgb, float3(1, 1, 1), luzPlana);
    return half4(color, 1);
}

VaryingsBorde BordeVert(Attributes v)
{
    VaryingsBorde o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    // Modo 0: normales (formas suaves). Modo 1: caja (desde el centro del objeto).
    // Modo 2: dirección guardada en la malla (UV3), para las mallas generadas.
    float3 dirOS = v.normalOS;
    if (_OutlineMode > 1.5)
        dirOS = v.outlineDir;
    else if (_OutlineMode > 0.5)
        dirOS = sign(v.positionOS.xyz);

    float4 posCS = TransformObjectToHClip(v.positionOS.xyz);
    float3 dirWS = TransformObjectToWorldNormal(dirOS, false);
    float3 dirVS = TransformWorldToViewDir(dirWS, false);
    float2 dirCS = mul((float2x2)UNITY_MATRIX_P, dirVS.xy);

    float largo = length(dirCS);
    if (largo > 1e-5 && dot(dirOS, dirOS) > 1e-8)
    {
        // Grosor constante en pantalla (milésimas del alto de la vista): contorno uniforme tipo vector.
        float2 desplazamiento = (dirCS / largo) * (_OutlineWidth * 0.002) * posCS.w;
        desplazamiento.x *= _ScreenParams.y / _ScreenParams.x;
        posCS.xy += desplazamiento;
    }

    o.positionCS = posCS;
    return o;
}

half4 BordeFrag(VaryingsBorde i) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    return half4(_OutlineColor.rgb, 1);
}

#endif
