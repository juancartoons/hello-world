#ifndef TRAZO_LUZ_INCLUDED
#define TRAZO_LUZ_INCLUDED

// LUZ EN LA PUNTA DEL DEDO (v36): ControlManos pone cada cuadro dónde está la punta del índice derecho.
// Las líneas, los rellenos, el vidrio de los menús y las manos se iluminan un poquito cuando el dedo se acerca.
// _JCDedo: xyz = punta del índice derecho (mundo); w = fuerza (0 = apagada).
// _JCDedo2: lo mismo con el índice izquierdo (más suave).
float4 _JCDedo;
float4 _JCDedo2;

static const float3 TrazoColorLuz = float3(0.62, 0.80, 1.00);

float TrazoLuzPunto(float3 posWS, float4 dedo, float radio)
{
    float d = distance(posWS, dedo.xyz);
    float k = 1.0 - smoothstep(0.003, radio, d);
    return dedo.w * k * k;
}

// 0 = lejos, 1 = pegado al índice derecho (llega hasta unos 6 cm).
float TrazoLuzDedo(float3 posWS)
{
    return TrazoLuzPunto(posWS, _JCDedo, 0.06);
}

// Los dos índices (el izquierdo más suave).
float TrazoLuzDedos(float3 posWS)
{
    return max(TrazoLuzPunto(posWS, _JCDedo, 0.06), TrazoLuzPunto(posWS, _JCDedo2, 0.045) * 0.6);
}

#endif
