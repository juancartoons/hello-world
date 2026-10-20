Shader "TrazoVR/Relleno"
{
    // Relleno plano de las figuras cerradas (color por vértice, sin luces).
    // "Offset" lo empuja un poquito hacia atrás para que la línea negra siempre quede encima. Además su borde está
    // un poquito hacia adentro, escondido debajo de la línea (ver Trazo.MeterBordeBajoLinea).
    // RELLENO VIVO (opcional, estilo Quill): dentro del color hay manchitas de tonos cercanos que cambian
    // solas (facetas, manchas o pinceladas), como un fondo pintado que respira.
    // Frente / fondo: el mismo escalón que su línea (ver TrazoAdelante en TrazoTemblor.hlsl).
    Properties
    {
        _BaseColor ("Tinte", Color) = (1, 1, 1, 1)
        // v46: el titileo de la mirada (rgb = hacia qué color, a = cuánto). Lo pone Mirada con un PropertyBlock.
        _Titileo ("Titileo", Color) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Relleno"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            // Un poco más atrás que su línea, y más cuando lo miras de lado (el primer número crece con la inclinación).
            Offset 2, 3

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "TrazoTemblor.hlsl"
            #include "TrazoLuz.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Titileo;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float4 textura    : TEXCOORD0; // x, y: lugar en el plano de la figura; z: textura (0 lisa); w: cambios por segundo
                float2 extra      : TEXCOORD1; // x: semilla de la figura; y: nivel (frente/fondo)
                float4 vivoA      : TEXCOORD2; // estilo vivo de la capa (ver TrazoTemblor.hlsl)
                float4 vivoB      : TEXCOORD3;
                float3 normalOS   : NORMAL;    // v57: solo la plastilina la usa (su luz)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float4 textura    : TEXCOORD0;
                float semilla     : TEXCOORD1;
                float3 posWS      : TEXCOORD2;
                float3 normalWS   : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float fase = TrazoFase(v.vivoA, v.vivoB);
                float3 posWS = TrazoTemblar(TransformObjectToWorld(v.positionOS.xyz), v.vivoA, v.vivoB, fase);
                // v57: plastilina con "hervido" (stop-motion): la superficie se acomoda un poquito unas veces por segundo.
                if (v.textura.z > 3.5 && v.textura.w > 0.0)
                {
                    float paso = floor(_TrazoTiempo * v.textura.w);
                    float3 q = v.positionOS.xyz * 40.0 + paso * 7.31;
                    posWS += (float3(TrazoRuido(q), TrazoRuido(q + 17.1), TrazoRuido(q + 41.7)) - 0.5) * 0.0035;
                }
                o.positionCS = TransformWorldToHClip(TrazoAdelante(posWS, v.extra.y));
                o.color = v.color;
                o.textura = v.textura;
                o.semilla = v.extra.x;
                o.posWS = posWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            // La celda (de Voronoi) más cercana al punto p. "fase" cambia el dibujo de las celdas.
            float2 CeldaCercana(float2 p, float fase, float semilla)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float mejor = 8.0;
                float2 celda = i;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 g = float2(x, y);
                        float2 c = i + g;
                        float2 punto = float2(TrazoHash(float3(c, semilla + fase * 7.13)),
                                              TrazoHash(float3(c + 19.7, semilla + fase * 3.71)));
                        float2 r = g + punto - f;
                        float d = dot(r, r);
                        if (d < mejor)
                        {
                            mejor = d;
                            celda = c;
                        }
                    }
                }
                return celda;
            }

            // Un tono cercano al color (más claro u oscuro y un poquito de otro matiz), distinto en cada celda.
            // (v34: unas 2.5 veces más marcado que antes, para que el cambio se note bien.)
            float3 TonoCercano(float3 c, float2 celda, float fase, float semilla, float fuerza)
            {
                float3 r = float3(TrazoHash(float3(celda, semilla + fase * 1.31 + 5.0)),
                                  TrazoHash(float3(celda + 3.3, semilla + fase * 2.17 + 9.0)),
                                  TrazoHash(float3(celda + 7.7, semilla + fase * 0.73 + 2.0))) * 2.0 - 1.0;
                float luz = dot(c, float3(0.299, 0.587, 0.114));
                float3 v = c * (1.0 + r.x * fuerza) + r.yzx * (fuerza * 0.27 * max(luz, 0.2));
                return saturate(v);
            }

            float3 RellenoVivo(float3 c, float2 uv, float estilo, float cambios, float semilla)
            {
                // El dibujo de las manchas cambia "cambios" veces por segundo, en un ciclo de 4 (como el temblor).
                float fase = cambios > 0.0 ? fmod(floor(_TrazoTiempo * cambios), 4.0) : 0.0;
                if (estilo < 1.5)
                {
                    // Facetas: polígonos como cristales (celdas de 3 cm).
                    return TonoCercano(c, CeldaCercana(uv / 0.03, fase, semilla), fase, semilla, 0.36);
                }
                if (estilo < 2.5)
                {
                    // Manchas: formas irregulares grandes y otras pequeñas encima (como pasto o nubes pintadas).
                    float2 p = uv / 0.045;
                    p += (float2(TrazoRuido(float3(p * 0.6, semilla + fase * 1.7)),
                                 TrazoRuido(float3(p * 0.6 + 11.3, semilla + fase * 2.3))) - 0.5) * 1.4;
                    float3 grande = TonoCercano(c, CeldaCercana(p, fase, semilla), fase, semilla, 0.32);
                    float3 chica = TonoCercano(c, CeldaCercana(p * 2.3 + 5.1, fase, semilla + 3.0), fase, semilla + 3.0, 0.26);
                    return lerp(grande, chica, 0.35);
                }
                // Pinceladas: manchas alargadas, todas más o menos en la misma dirección (cambia un poco por zonas).
                float ang = semilla * 2.1 + (TrazoRuido(float3(uv * 6.0, semilla)) - 0.5) * 1.2;
                float2 dir = float2(cos(ang), sin(ang));
                float2 q = float2(dot(uv, dir) / 0.075, dot(uv, float2(-dir.y, dir.x)) / 0.02);
                return TonoCercano(c, CeldaCercana(q, fase, semilla), fase, semilla, 0.34);
            }

            // v57: PLASTILINA: luz mate y suave (de arriba), un poquito más oscura en las orillas, un brillito apenas,
            // granito fino y HUELLAS de dedos (anillos ovalados aquí y allá). "huellas" de 0 a 1.
            float3 Plastilina(float3 c, float2 uv, float huellas, float cambios, float semilla, float3 n, float3 posWS)
            {
                float3 V = normalize(GetCameraPositionWS() - posWS);
                n = normalize(n);
                if (dot(n, V) < 0.0)
                    n = -n;
                float fase = cambios > 0.0 ? fmod(floor(_TrazoTiempo * cambios), 4.0) : 0.0;
                float2 p = uv / 0.022;
                float2 i0 = floor(p);
                float2 f0 = frac(p);
                float marca = 0.0;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 g = float2(x, y);
                        float2 celda = i0 + g;
                        float h = TrazoHash(float3(celda, semilla + 4.7 + fase * 3.1));
                        if (h > 0.62)
                        {
                            float2 centro = g + float2(TrazoHash(float3(celda + 5.3, semilla + fase)), TrazoHash(float3(celda + 9.1, semilla + fase))) - f0;
                            float ang = h * 31.0;
                            float2 dir = float2(cos(ang), sin(ang));
                            float2 d = float2(dot(centro, dir), dot(centro, float2(-dir.y, dir.x)) * 1.45);
                            float r = length(d);
                            float dentro = saturate(1.0 - r / 0.6);
                            marca += dentro * dentro * (0.5 + 0.5 * sin(r * 48.0 + h * 6.0));
                        }
                    }
                }
                float grano = TrazoRuido(float3(uv * 260.0, semilla)) - 0.5;
                float3 L = normalize(float3(0.35, 0.9, -0.3));
                float dif = dot(n, L) * 0.5 + 0.5;
                float luz = 0.6 + 0.5 * dif * dif;
                float orilla = 1.0 - 0.2 * pow(1.0 - saturate(dot(n, V)), 2.0);
                float3 r3 = c * luz * orilla;
                r3 *= 1.0 - huellas * (0.17 * saturate(marca) + 0.08 * grano);
                float3 H = normalize(L + V);
                r3 += 0.05 * pow(saturate(dot(n, H)), 12.0);
                return saturate(r3);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 c = i.color.rgb * _BaseColor.rgb;
                if (i.textura.z > 3.5)
                    c = Plastilina(c, i.textura.xy, saturate((i.textura.z - 4.0) / 0.9), i.textura.w, i.semilla, i.normalWS, i.posWS);
                else if (i.textura.z > 0.5)
                    c = RellenoVivo(c, i.textura.xy, i.textura.z, i.textura.w, i.semilla);
                // v36: la luz de la punta del dedo (suave).
                c += TrazoColorLuz * (0.3 * TrazoLuzDedo(i.posWS));
                c = lerp(c, _Titileo.rgb, _Titileo.a);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
