Shader "TrazoVR/Linea"
{
    // Línea negra tipo vector para TrazoVR.
    // - Cinta (uv.y = 0): la malla guarda el centro de la línea y su dirección; aquí se abre
    //   hacia los lados mirando siempre a la cámara, así se ve como un trazo de tinta limpio.
    // - Tubo (uv.y = 1): la malla ya es un tubo 3D; se le da un brillo suave arriba para que se lea el volumen.
    // Compatible con URP, SRP Batcher y estéreo de Meta Quest (multiview / single-pass instanced).
    // v39: la cinta nunca es más fina que un pixel de la pantalla: así la punta fina se ve entera hasta el final
    // (antes, con poco detalle, la puntita desaparecía y la línea se veía cortada).
    // v49: TEXTURA LÁPIZ (vivoB.z = 1): grano de papel, orilla irregular, más oscura al centro (presión) y "hervida"
    // a 8 cuadros por segundo (el grano y la orilla cambian, como animación cuadro a cuadro, aunque la línea no se mueva).
    // v36: al terminarla, un destello la recorre de punta a punta (_JCDestello = cuándo empezó); y al deshacer se
    // "rebobina" (_JCCorte: lo que pasa de ahí no se dibuja). (v37: la luz del dedo ya no ilumina las líneas.)
    Properties
    {
        _BaseColor ("Color de la línea", Color) = (0, 0, 0, 1)
        _ColorLuz ("Brillo del tubo", Color) = (0.45, 0.45, 0.45, 1)
        [HideInInspector] _JCDestello ("Destello (segundos)", Float) = 0
        [HideInInspector] _JCCorte ("Corte (0 a 1)", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Linea"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "TrazoTemblor.hlsl"
            #include "TrazoLuz.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _ColorLuz;
                float _JCDestello;
                float _JCCorte;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;    // cinta: dirección de la línea / tubo: normal
                float3 uv         : TEXCOORD0; // x: medio grosor con signo (cinta), y: 0 cinta / 1 tubo, z: nivel (frente/fondo)
                float2 uv2        : TEXCOORD1; // x: número de hebra, y: lugar a lo largo de la línea (0 a 1)
                float4 vivoA      : TEXCOORD2; // estilo vivo de la capa (ver TrazoTemblor.hlsl)
                float4 vivoB      : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float esTubo      : TEXCOORD1;
                float lugar       : TEXCOORD2; // 0 al empezar la línea, 1 al terminarla
                float lado        : TEXCOORD3; // v49: de -1 a 1 a lo ancho de la cinta (0 = el centro)
                float lapiz       : TEXCOORD4; // v49: 1 = textura de lápiz
                float3 grano      : TEXCOORD5; // v49: dónde está (en el dibujo) para el grano del papel
                float4 ajusteLapiz : TEXCOORD6; // v52: grano, densidad, orilla borrosa, hervido (de 0 a 1; 0.55 = normal)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 original = TransformObjectToWorld(v.positionOS.xyz);
                float fase = TrazoFase(v.vivoA, v.vivoB);
                float3 posWS = TrazoTemblar(original, v.vivoA, v.vivoB, fase);
                float3 nWS = float3(0, 1, 0);
                if (v.uv.y < 0.5)
                {
                    float3 dirWS = TransformObjectToWorldDir(v.normalOS);
                    float3 haciaCamara = normalize(GetCameraPositionWS() - posWS);
                    float3 lado = cross(dirWS, haciaCamara);
                    float largo = length(lado);
                    if (largo > 1e-4)
                        lado = lado / largo;
                    else
                        lado = normalize(cross(dirWS, float3(0, 1, 0)) + float3(1e-3, 0, 0));
                    float3x3 m = (float3x3)GetObjectToWorldMatrix();
                    float escala = length(float3(m[0][0], m[1][0], m[2][0]));
                    float medio = abs(v.uv.x) * escala;
                    posWS += TrazoHebra(original, v.uv2.x, v.uv2.y, medio, v.vivoA, v.vivoB, fase);
                    float ancho = v.uv.x * escala * TrazoGrosorVivo(original, v.uv2.x, v.vivoA, v.vivoB, fase);
                    // v50: el lápiz es un poco más ancho (sus orillas se deshacen en granitos). v52: su ancho se ajusta.
                    if (v.vivoB.z > 0.5)
                    {
                        float anchoL = v.vivoB.w > 0.5 ? floor(fmod(v.vivoB.w / 1000.0, 10.0)) / 9.0 : 0.555;
                        ancho *= lerp(1.0, 2.0, anchoL);
                    }
                    // v40: la tapa del halo en las puntas (uv.y de 0.2 a 0.4) es solo del halo: aquí no se abre.
                    if (v.uv.y > 0.1)
                        ancho = 0.0;
                    // Lo que mide un pixel a esta distancia (con la cámara de cada ojo): mínimo 1.2 pixeles de ancho.
                    float w = TransformWorldToHClip(posWS).w;
                    float pixel = 2.0 * w / max(abs(UNITY_MATRIX_P[1][1]) * _ScreenParams.y, 1.0);
                    if (ancho != 0.0)
                        ancho = sign(ancho) * max(abs(ancho), 0.6 * pixel);
                    posWS += lado * ancho;
                }
                else
                {
                    nWS = TransformObjectToWorldNormal(v.normalOS);
                }

                // Frente / fondo (solo cambia quién tapa a quién; se ve en el mismo lugar).
                posWS = TrazoAdelante(posWS, v.uv.z);
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS = nWS;
                o.esTubo = v.uv.y;
                o.lugar = v.uv2.y;
                o.lado = v.uv.y < 0.5 ? sign(v.uv.x) : 0.0;
                o.lapiz = v.vivoB.z;
                o.grano = v.positionOS.xyz;
                // v52: los ajustes del lápiz (empacados en vivoB.w: grano + 10·densidad + 100·borde + 1000·ancho + 10000·hervido).
                float w = v.vivoB.w;
                o.ajusteLapiz = w > 0.5
                    ? float4(fmod(w, 10.0), floor(fmod(w / 10.0, 10.0)), floor(fmod(w / 100.0, 10.0)), floor(fmod(w / 10000.0, 10.0))) / 9.0
                    : float4(0.555, 0.555, 0.555, 0.555);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // Rebobinar (deshacer): lo que pasa del corte no se dibuja.
                clip(_JCCorte - i.lugar);
                float luz = saturate(dot(normalize(i.normalWS), normalize(float3(0.3, 0.9, -0.3))));
                float k = step(0.5, i.esTubo) * luz * luz;
                float3 col = lerp(_BaseColor.rgb, _ColorLuz.rgb, k);
                // v50: LÁPIZ natural (como grafito o pastel sobre papel): fibras finas A LO LARGO del trazo, orillas que se
                // deshacen en granitos (no un corte), más oscuro al centro, presión que cambia a lo largo y un "hervido"
                // suave a 8 cuadros por segundo. Sin transparencia: el grano se hace con granitos de pixeles (tramado).
                if (i.lapiz > 0.5)
                {
                    float4 aj = i.ajusteLapiz;
                    // Hervido: de quieto (0) a 16 cuadros por segundo (el normal es unos 8).
                    float cuadro = floor(_TrazoTiempo * aj.w * 16.0);
                    float borde = abs(i.lado);                       // 0 = centro, 1 = orilla
                    float largo = i.lugar * 420.0;                   // a lo largo de la línea
                    // Fibras: cambian rápido de lado a lado y despacio a lo largo (rayitas paralelas al trazo).
                    float fibra = TrazoRuido(float3(largo * 0.08, borde * 9.0 + i.lado * 3.0, cuadro * 0.37));
                    float fibra2 = TrazoRuido(float3(largo * 0.25, i.lado * 22.0, 11.0 + cuadro * 0.21));
                    // La presión del lápiz sube y baja a lo largo.
                    float presion = 0.72 + 0.28 * TrazoRuido(float3(largo * 0.012, 3.0, cuadro * 0.05));
                    float cuerpo = 1.0 - smoothstep(lerp(0.6, 0.02, aj.z), 1.0, borde);   // orilla más o menos borrosa
                    float densidad = cuerpo * presion * (0.45 + 0.4 * fibra + 0.25 * fibra2);
                    // v54: SIN granitos (se veían de 8 bits): solo las FIBRAS a lo largo del trazo dan la orilla.
                    // "Grano" ahora es qué tan finas son las fibras. El halo suave va aparte (detrás de la línea).
                    densidad *= lerp(0.7, 1.4, aj.y);                                     // más o menos grafito
                    float fino = TrazoRuido(float3(largo * lerp(0.05, 0.2, aj.x), i.lado * lerp(6.0, 30.0, aj.x), 23.0 + cuadro * 0.29));
                    clip(densidad * 1.12 - 0.3 - fino * 0.2);
                    // Más oscuro donde hay más grafito; más claro en las orillas.
                    float tono = saturate(densidad * 1.15 - 0.05);
                    float3 claro = lerp(col, float3(0.6, 0.6, 0.62), 0.6);
                    col = lerp(claro, col, tono);
                }
                // Se materializa: un destello de luz recorre la línea de punta a punta (unos 0.55 segundos).
                if (_JCDestello > 0.0)
                {
                    float paso = (_Time.y - _JCDestello) / 0.55;
                    if (paso > -0.25 && paso < 1.35)
                    {
                        float x = (i.lugar - paso) / 0.07;
                        col += TrazoColorLuz * (0.95 * exp(-x * x));
                    }
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
