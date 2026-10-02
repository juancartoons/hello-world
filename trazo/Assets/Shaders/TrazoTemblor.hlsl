#ifndef TRAZO_TEMBLOR_INCLUDED
#define TRAZO_TEMBLOR_INCLUDED

// Temblor de líneas ("line boil"): cada punto se mueve un poquito con un ruido suave
// que cambia de forma unas 10 veces por segundo, como un dibujo animado a mano.
// Los valores los pone Temblor.cs para todos los materiales a la vez (propiedades globales).
float _TrazoTemblor;      // cuánto se mueve (metros). 0 = apagado
float _TrazoTemblorFase;  // cambia de número en cada "dibujo" nuevo

float TrazoHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

// Ruido suave (de 0 a 1) en 3D.
float TrazoRuido(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    float3 u = f * f * (3.0 - 2.0 * f);
    float a = TrazoHash(i);
    float b = TrazoHash(i + float3(1, 0, 0));
    float c = TrazoHash(i + float3(0, 1, 0));
    float d = TrazoHash(i + float3(1, 1, 0));
    float e = TrazoHash(i + float3(0, 0, 1));
    float g = TrazoHash(i + float3(1, 0, 1));
    float h = TrazoHash(i + float3(0, 1, 1));
    float k = TrazoHash(i + float3(1, 1, 1));
    return lerp(lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y),
                lerp(lerp(e, g, u.x), lerp(h, k, u.x), u.y), u.z);
}

// La línea y su relleno usan la misma función, así se mueven juntos.
float3 TrazoTemblar(float3 posWS)
{
    if (_TrazoTemblor <= 0.0)
        return posWS;
    float3 p = posWS * 9.0 + _TrazoTemblorFase * float3(17.31, 5.73, 11.97);
    float3 d = float3(TrazoRuido(p), TrazoRuido(p + 31.4), TrazoRuido(p + 67.2)) * 2.0 - 1.0;
    return posWS + d * _TrazoTemblor;
}

#endif
