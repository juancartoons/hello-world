#ifndef TRAZO_TEMBLOR_INCLUDED
#define TRAZO_TEMBLOR_INCLUDED

// Líneas vivas ("line boil"), como un dibujo animado a mano. Cada capa tiene su estilo,
// que viene en cada vértice:
//   a = (amplitud del temblor en metros, separación de hebras, grosor vivo 0/1, cambios por segundo)
//   b = (ciclo de 3 dibujos 0/1, frecuencia del ruido = suavidad)
// El tiempo lo pone Temblor.cs (los videos ponen el tiempo de cada cuadro).
float _TrazoTiempo;

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

// Número del "dibujo" actual: 0, 1, 2 (ciclo de 3) o siempre distinto. Sin temblor, no cambia.
float TrazoFase(float4 a, float4 b)
{
    if (a.x <= 0.0 || a.w <= 0.0)
        return 0.0;
    float paso = floor(_TrazoTiempo * a.w);
    return b.x > 0.5 ? fmod(paso, 3.0) : fmod(paso, 1000.0);
}

float TrazoFrecuencia(float4 b)
{
    return b.y > 0.0 ? b.y : 9.0;
}

// La línea y su relleno usan la misma función, así se mueven juntos.
float3 TrazoTemblar(float3 posWS, float4 a, float4 b, float fase)
{
    if (a.x <= 0.0)
        return posWS;
    float3 p = posWS * TrazoFrecuencia(b) + fase * float3(17.31, 5.73, 11.97);
    return posWS + TrazoRuido3(p) * a.x;
}

// Cada hebra (menos la 0) se aparta un poquito por su cuenta. En las PUNTAS de la línea
// todas se juntan en una sola (como un mechón de pelo con gel).
// medio: medio grosor de la línea en ese punto (metros); t: lugar a lo largo de la línea (0 a 1).
float3 TrazoHebra(float3 posWS, float hebra, float t, float medio, float4 a, float4 b, float fase)
{
    if (hebra < 0.5 || a.y <= 0.0)
        return float3(0, 0, 0);
    float juntas = saturate(sin(3.14159 * saturate(t)) * 1.6);
    float3 p = posWS * (TrazoFrecuencia(b) * 1.55) + hebra * float3(13.7, 7.1, 3.3) + fase * float3(3.1, 9.7, 5.3);
    return TrazoRuido3(p) * a.y * max(medio, 0.0015) * juntas;
}

// El grosor sube y baja a lo largo de la línea (como la presión de un pincel) y cambia con el temblor.
float TrazoGrosorVivo(float3 posWS, float hebra, float4 a, float4 b, float fase)
{
    if (a.z <= 0.0)
        return 1.0;
    float n = TrazoRuido(posWS * (TrazoFrecuencia(b) * 1.8) + hebra * 5.1 + fase * 7.7);
    return lerp(1.0, 0.35 + 1.3 * n, a.z);
}

#endif
