# Azotea de Nueva York (v38): arregla la profundidad (archivo .bytes, 512 x 256).
# La herramienta de profundidad pone el piso a 1 altura de cámara y nada de abajo puede quedar más lejos que ese
# piso. En una azotea eso está mal: la ciudad que se ve por debajo del horizonte (detrás del borde) quedaba
# "pintada" sobre el piso de la azotea, a pocos metros, y por eso se veía torcida. Aquí:
#  - de frente (donde estaba la baranda), todo lo que queda entre el horizonte y el borde de la azotea se va lejos;
#  - donde estaban los arbolitos, lo de arriba de las jardineras también se va lejos;
#  - el techito de los bares (atrás, a los lados) queda suave, sin picos.
# Uso: python3 profundidad.py entrada.bytes salida.bytes vista.png
import sys, math, struct
import numpy as np, cv2
ruta_in, ruta_out, ruta_vista = sys.argv[1:4]
DMIN, DMAX = 0.05, 60.0
LEJOS = 40.0                         # la ciudad de abajo (en alturas de cámara: unos 64 m)
ln0, ln1 = math.log(DMIN), math.log(DMAX)
datos = open(ruta_in, "rb").read()
assert datos[:4] == b"JCP1"
W, H = struct.unpack("<HH", datos[4:8])
cod = np.frombuffer(datos[8:8 + W * H * 2], "<u2").reshape(H, W).astype(np.float64)
dist = np.exp(ln0 + cod / 65535.0 * (ln1 - ln0))
nueva = dist.copy()
def col(x): return int(round(x / 4096 * W))
def fila(y): return int(round(y / 2048 * H))
# De frente: del horizonte hasta el borde de la azotea (el murito, en la foto y = 1228).
x0, x1 = col(500), col(3570)
nueva[fila(1024):fila(1226), x0:x1] = np.maximum(nueva[fila(1024):fila(1226), x0:x1], LEJOS)
# Donde estaban los arbolitos: arriba de las jardineras (y < 1135), lejos.
for a, b in [(250, 512), (3552, 3806)]:
    nueva[fila(890):fila(1135), col(a):col(b)] = np.maximum(nueva[fila(890):fila(1135), col(a):col(b)], LEJOS * 0.8)
# Los bares de atrás (a los dos lados de la costura): suaves, sin picos.
lnd = np.log(nueva).astype(np.float32)
for a, b in [(0, 300), (3720, 4096)]:
    y0, y1 = fila(800), fila(1060)
    zona = lnd[y0:y1, col(a):col(b)]
    zona = cv2.medianBlur(zona, 5)
    lnd[y0:y1, col(a):col(b)] = cv2.GaussianBlur(zona, (0, 0), 1.5)
nueva = np.exp(lnd.astype(np.float64))
nueva = np.clip(nueva, DMIN, DMAX)
c = np.round((np.log(nueva) - ln0) / (ln1 - ln0) * 65535).astype("<u2")
with open(ruta_out, "wb") as f:
    f.write(b"JCP1"); f.write(struct.pack("<HH", W, H)); f.write(c.tobytes())
def gris(d):
    return (255 * (1 - (np.log(d) - ln0) / (ln1 - ln0))).astype(np.uint8)
cv2.imwrite(ruta_vista, np.vstack([gris(dist), np.full((4, W), 255, np.uint8), gris(nueva)]))
print("listo")
