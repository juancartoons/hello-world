Shader "TrazoVR/SelloLapiz"
{
    // Sello del lápiz de boceto: se pinta (con GL) sobre la imagen de la hoja.
    // La hoja guarda "cuánto grafito hay" en cada punto (0 = papel limpio, 1 = negro).
    // Pase 0 = lápiz (cada pasada oscurece un poco más), pase 1 = goma (aclara).
    SubShader
    {
        Tags { "RenderType" = "Transparent" }
        ZWrite Off
        ZTest Always
        Cull Off

        CGINCLUDE
        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv     : TEXCOORD0; // -1..1 dentro del sello
            float2 hoja   : TEXCOORD1; // lugar en la hoja (para el grano del papel)
            float4 color  : COLOR;
        };

        struct v2f
        {
            float4 pos   : SV_POSITION;
            float2 uv    : TEXCOORD0;
            float2 hoja  : TEXCOORD1;
            float4 color : COLOR;
        };

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            o.hoja = v.hoja;
            o.color = v.color;
            return o;
        }

        float Grano(float2 p)
        {
            float2 i = floor(p);
            return frac(sin(dot(i, float2(12.9898, 78.233))) * 43758.5453);
        }

        // Cuánto deja este sello: suave en los bordes y con el grano del papel.
        float Cantidad(v2f i)
        {
            float r = length(i.uv);
            float caida = saturate(1.0 - r);
            caida = caida * caida * (3.0 - 2.0 * caida);
            float grano = lerp(Grano(i.hoja * 1400.0), Grano(i.hoja * 700.0 + 17.0), 0.4);
            return caida * i.color.a * lerp(0.25, 1.0, grano);
        }
        ENDCG

        Pass
        {
            // Lápiz: nuevo = viejo + (1 - viejo) * cantidad
            Blend OneMinusDstColor One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragLapiz
            float4 fragLapiz(v2f i) : SV_Target
            {
                float a = Cantidad(i);
                return float4(a, a, a, a);
            }
            ENDCG
        }

        Pass
        {
            // Goma: nuevo = viejo * (1 - cantidad)
            Blend Zero OneMinusSrcColor
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragGoma
            float4 fragGoma(v2f i) : SV_Target
            {
                float r = length(i.uv);
                float a = saturate(1.0 - r) * i.color.a;
                return float4(a, a, a, a);
            }
            ENDCG
        }
    }
}
