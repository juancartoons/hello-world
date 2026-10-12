# Cuando LaMa rellena un hueco MUY grande, el centro le sale de un color apagado (gris) y casi sin textura. Esto:
#  - le pone los colores "de fondo" (lo borroso) que vienen de alrededor del hueco: se calculan rellenando el hueco
#    en chiquito (1/16) y ampliándolo;
#  - y la textura fina (el pelo del tapete) la toma de la MISMA fila de la foto, pero del otro lado de la sala
#    (corrida "desplazar" pixeles): en una foto 360 la misma fila es la misma inclinación, así que el tapete tiene
#    el mismo tamaño. Donde allá también hay hueco, se queda la textura de LaMa.
# Solo cambia la parte "piso" del hueco; "respetar" (opcional) es un rectángulo x0,y0,x1,y1 donde LaMa se queda tal
# cual (por ejemplo donde rehízo el frente del sofá).
# Uso: python3 ajustar_color.py original.png rellena.jpg mascara.png salida.jpg [realce] [desplazar] [x0,y0,x1,y1]
import sys
import numpy as np, cv2
ruta_o, ruta_r, ruta_m, salida = sys.argv[1:5]
realce = float(sys.argv[5]) if len(sys.argv) > 5 else 1.0
desplazar = int(sys.argv[6]) if len(sys.argv) > 6 else 0
o = cv2.imread(ruta_o).astype(np.float32)
r = cv2.imread(ruta_r).astype(np.float32)
m = cv2.imread(ruta_m, 0) > 0
H, W = m.shape
piso = m.copy()
if len(sys.argv) > 7:
    x0, y0, x1, y1 = map(int, sys.argv[7].split(","))
    piso[y0:y1, x0:x1] = False
K = 16
base = o.copy()
base[m & ~piso] = r[m & ~piso]          # lo que LaMa rehízo y se respeta también sirve de alrededor
chica = cv2.resize(base, (W // K, H // K), interpolation=cv2.INTER_AREA)
mc = cv2.resize(piso.astype(np.uint8) * 255, (W // K, H // K), interpolation=cv2.INTER_AREA) > 0
mc = cv2.dilate(mc.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
chica = cv2.inpaint(np.clip(chica, 0, 255).astype(np.uint8), mc.astype(np.uint8) * 255, 8, cv2.INPAINT_TELEA)
fondo = cv2.resize(chica.astype(np.float32), (W, H), interpolation=cv2.INTER_CUBIC)
S = 24
fondo = cv2.GaussianBlur(fondo, (0, 0), S)
detalle = r - cv2.GaussianBlur(r, (0, 0), S)
if desplazar:
    otro = np.roll(o, -desplazar, axis=1)
    sirve = ~np.roll(cv2.dilate(m.astype(np.uint8), np.ones((31, 31), np.uint8)) > 0, -desplazar, axis=1)
    det_otro = otro - cv2.GaussianBlur(otro, (0, 0), S)
    b = cv2.GaussianBlur(sirve.astype(np.float32), (0, 0), 6)[..., None]
    detalle = detalle * (1 - b) + det_otro * b
nuevo = fondo + detalle * realce
# Solo en el piso del hueco, con la orilla suave (pegado a la orilla manda lo de LaMa tal cual).
dist = cv2.distanceTransform(piso.astype(np.uint8), cv2.DIST_L2, 5)
a = np.clip((dist - 3) / 20.0, 0, 1)[..., None]
res = r * (1 - a) + nuevo * a
cv2.imwrite(salida, np.clip(res + 0.5, 0, 255).astype(np.uint8), [cv2.IMWRITE_JPEG_QUALITY, 95])
print("listo")
