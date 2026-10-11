# Venecia: el agua del canal más abajo que el puente.
# La herramienta de profundidad (profundidad360.py) pone el piso a 1 altura de cámara y nada de abajo puede quedar
# más lejos que ese piso. Por eso el agua quedaba al mismo nivel del puente. Aquí, solo en el canal (un contorno
# dibujado a mano sobre la foto, con los botes incluidos), el agua baja a un plano más hondo.
# Uso: python3 agua_mas_abajo.py foto.jpg profundidad_entrada.bytes profundidad_salida.bytes vista.jpg
import sys, math, struct
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ALTURA_AGUA = 2.6        # en alturas de cámara (1.6 m): el agua unos 4 m debajo de tus ojos
LEJOS_MAX = 25.0         # el agua más lejana no pasa de aquí (alturas de cámara)
DMIN, DMAX = 0.05, 60.0

# El canal (u, v de la foto: u de izquierda a derecha, v de arriba a abajo), con los botes.
CANAL = [
    (0.312, 0.505), (0.330, 0.505), (0.338, 0.530), (0.350, 0.550), (0.370, 0.575), (0.395, 0.600),
    (0.430, 0.620), (0.465, 0.645), (0.475, 0.665), (0.520, 0.690), (0.530, 0.720), (0.545, 0.740),
    (0.535, 0.785), (0.500, 0.820), (0.460, 0.850), (0.400, 0.872), (0.330, 0.885), (0.270, 0.880),
    (0.220, 0.860), (0.192, 0.830), (0.172, 0.790), (0.176, 0.765), (0.192, 0.700), (0.215, 0.675),
    (0.240, 0.665), (0.245, 0.630), (0.260, 0.600), (0.275, 0.580), (0.290, 0.555), (0.300, 0.530),
    (0.310, 0.515),
]

ruta_foto, ruta_in, ruta_out, ruta_vista = sys.argv[1:5]
datos = open(ruta_in, "rb").read()
assert datos[:4] == b"JCP1"
W, H = struct.unpack("<HH", datos[4:8])
cod = np.frombuffer(datos[8:8 + W * H * 2], "<u2").reshape(H, W).astype(np.float64)
ln0, ln1 = math.log(DMIN), math.log(DMAX)
dist = np.exp(ln0 + cod / 65535.0 * (ln1 - ln0))

# Máscara del canal (4 veces más grande y luego achicada = borde suave).
k = 4
m = Image.new("L", (W * k, H * k), 0)
ImageDraw.Draw(m).polygon([(u * W * k, v * H * k) for u, v in CANAL], fill=255)
m = m.resize((W, H), Image.LANCZOS).filter(ImageFilter.GaussianBlur(0.8))
mascara = np.asarray(m, np.float64) / 255.0

# El plano del agua: a ALTURA_AGUA debajo de la cámara.
vv = (np.arange(H) + 0.5) / H
lat = (0.5 - vv) * math.pi
abajo = np.maximum(-np.sin(lat), 1e-4)[:, None] * np.ones((1, W))
agua = np.minimum(ALTURA_AGUA / abajo, LEJOS_MAX)
nueva = np.where(mascara > 0, np.exp(np.log(dist) * (1 - mascara) + np.log(np.maximum(agua, dist)) * mascara), dist)
nueva = np.clip(nueva, DMIN, DMAX)
print("pixeles de agua:", int((mascara > 0.5).sum()), " distancia media antes %.2f, después %.2f"
      % (dist[mascara > 0.5].mean(), nueva[mascara > 0.5].mean()))

c = np.round((np.log(nueva) - ln0) / (ln1 - ln0) * 65535).astype("<u2")
with open(ruta_out, "wb") as f:
    f.write(b"JCP1"); f.write(struct.pack("<HH", W, H)); f.write(c.tobytes())

# Vista de control: la foto con el canal marcado (arriba) y el mapa de distancias antes / después (abajo).
foto = Image.open(ruta_foto).convert("RGB").resize((1024, 512))
marca = Image.fromarray((mascara * 255).astype(np.uint8)).resize((1024, 512))
tinte = Image.new("RGB", (1024, 512), (0, 160, 255))
foto = Image.composite(Image.blend(foto, tinte, 0.45), foto, marca)
def gris(d):
    g = (1 - (np.log(d) - ln0) / (ln1 - ln0)) * 255
    return Image.fromarray(g.astype(np.uint8)).resize((512, 256)).convert("RGB")
lienzo = Image.new("RGB", (1024, 768), "white")
lienzo.paste(foto, (0, 0)); lienzo.paste(gris(dist), (0, 512)); lienzo.paste(gris(nueva), (512, 512))
lienzo.save(ruta_vista, quality=88)
