Shader "TrazoVR/Nv12"
{
    // Convierte un cuadro de color (RGB) al formato NV12 que pide el codificador de video, en la tarjeta gráfica.
    // La imagen de salida (un solo canal) mide ancho x (alto * 1.5): primero el brillo (Y) de cada píxel y después
    // el color (U y V intercalados) de cada bloque de 2x2. Las filas quedan en el orden que espera el video.
    Properties
    {
        _MainTex ("Cuadro", 2D) = "white" {}
        _Tam ("Ancho, alto", Vector) = (1280, 720, 0, 0)
    }
    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Tam;

            float3 Color(float2 uv)
            {
                float3 c = tex2Dlod(_MainTex, float4(uv, 0, 0)).rgb;
            #if !defined(UNITY_COLORSPACE_GAMMA)
                c = LinearToGammaSpace(c); // el video guarda colores sRGB
            #endif
                return saturate(c);
            }

            float4 frag(v2f_img i) : SV_Target
            {
                float w = _Tam.x;
                float h = _Tam.y;
                float px = floor(i.uv.x * w);
                float fila = floor(i.uv.y * h * 1.5);
                float valor;
                if (fila < h)
                {
                    // Brillo del píxel (fila 0 = arriba de la imagen).
                    float2 uv = float2((px + 0.5) / w, (h - fila - 0.5) / h);
                    float3 c = Color(uv);
                    valor = 0.257 * c.r + 0.504 * c.g + 0.098 * c.b + 16.0 / 255.0;
                }
                else
                {
                    // Color de un bloque de 2x2 (se toma justo en su centro: promedia los 4 píxeles).
                    float q = fila - h;
                    float bloque = floor(px * 0.5);
                    float2 uv = float2((2.0 * bloque + 1.0) / w, (h - 2.0 * q - 1.0) / h);
                    float3 c = Color(uv);
                    float u = -0.148 * c.r - 0.291 * c.g + 0.439 * c.b + 128.0 / 255.0;
                    float v = 0.439 * c.r - 0.368 * c.g - 0.071 * c.b + 128.0 / 255.0;
                    valor = fmod(px, 2.0) < 0.5 ? u : v;
                }
                return float4(valor, valor, valor, 1);
            }
            ENDCG
        }
    }
}
