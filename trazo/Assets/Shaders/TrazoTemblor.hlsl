#ifndef TRAZO_TEMBLOR_INCLUDED
#define TRAZO_TEMBLOR_INCLUDED

// Temblor de líneas ("line boil"): cada punto se mueve un poquito con un ruido suave
// que cambia de forma unas 10 veces por segundo, como un dibujo animado a mano.
// Los valores los pone Temblor.cs para todos los materiales a la vez (propiedades globales).
float _TrazoTemblor;      // cuánto se mueve (metros). 0 = apagado
float _TrazoTemblorFase;  // cambia de número en cada "dibujo" nuevo
float _TrazoHebras;       // cuánto se separan las hebras (veces el grosor). 0 = juntas
float _TrazoGrosorVivo;   // 0 = grosor normal; 1 = el grosor cambia a lo largo de la línea

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

float3 TrazoRuido3(float3 p)
{
    return float3(TrazoRuido(p), TrazoRuido(p + 31.4), TrazoRuido(p + 67.2)) * 2.0 - 1.0;
}

// La línea y su relleno usan la misma función, así se mueven juntos.
float3 TrazoTemblar(float3 posWS)
{
    if (_TrazoTemblor <= 0.0)
        return posWS;
    float3 p = posWS * 9.0 + _TrazoTemblorFase * float3(17.31, 5.73, 11.97);
    return posWS + TrazoRuido3(p) * _TrazoTemblor;
}

// Cada hebra (menos la 0) se aparta un poquito por su cuenta; más en las puntas de la línea.
// medio: medio grosor de la línea en ese punto (metros); a: lugar a lo largo de la línea (0 a 1).
float3 TrazoHebra(float3 posWS, float hebra, float a, float medio)
{
    if (hebra < 0.5 || _TrazoHebras <= 0.0)
        return float3(0, 0, 0);
    float puntas = 1.0 + 1.5 * (1.0 - sin(3.14159 * saturate(a)));
    float3 p = posWS * 14.0 + hebra * float3(13.7, 7.1, 3.3) + _TrazoTemblorFase * float3(3.1, 9.7, 5.3);
    return TrazoRuido3(p) * _TrazoHebras * max(medio, 0.0015) * puntas;
}

// El grosor sube y baja a lo largo de la línea (como la presión de un pincel) y cambia con el temblor.
float TrazoGrosorVivo(float3 posWS, float hebra)
{
    if (_TrazoGrosorVivo <= 0.0)
        return 1.0;
    float n = TrazoRuido(posWS * 16.0 + hebra * 5.1 + _TrazoTemblorFase * 7.7);
    return lerp(1.0, 0.35 + 1.3 * n, _TrazoGrosorVivo);
}

#endif
