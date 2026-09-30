Shader "FarmaciaVR/Toon Sin Borde"
{
    // Igual que FarmaciaVR/Toon pero sin contorno: para piso, techo, lámparas y discos.
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Color de sombra (multiplica)", Color) = (0.74, 0.78, 0.88, 1)
        _ShadowThreshold ("Umbral de sombra", Range(-1, 1)) = 0.05
        _ShadowSoftness ("Suavidad del borde de sombra", Range(0, 0.5)) = 0.02
        [ToggleUI] _UseVertexColor ("Usar colores de la malla", Float) = 0
        [HideInInspector] _OutlineColor ("Color del contorno", Color) = (0, 0, 0, 1)
        [HideInInspector] _OutlineWidth ("Grosor del contorno", Float) = 0
        [HideInInspector] _OutlineMode ("Modo del contorno", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Toon"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex ToonVert
            #pragma fragment ToonFrag
            #pragma multi_compile_instancing
            #include "FarmaciaToon.hlsl"
            ENDHLSL
        }
    }
}
