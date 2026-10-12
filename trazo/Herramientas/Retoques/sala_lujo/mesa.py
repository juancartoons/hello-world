# Sala de lujo (v38): el toque final de la mesa de centro, después de LaMa.
#  - La TAPA DEL LIBRO de arriba (donde estaba el tazón) se rehace lisa: un degradado suave sacado de lo que se ve
#    del libro a los lados del tazón. Así los libros quedan limpios y rectos.
#  - El MÁRMOL donde se reflejaba el florero: LaMa lo dejaba con una mancha amarilla. Aquí el color
#    de fondo sale del mármol de los lados y de LaMa solo queda la textura fina.
# Uso: python3 mesa.py original.png con_lama.jpg mascara_mesa.png salida.jpg
import sys
import numpy as np, cv2
ruta_o, ruta_l, ruta_m, salida = sys.argv[1:5]
o = cv2.imread(ruta_o).astype(np.float32)
lama = cv2.imread(ruta_l).astype(np.float32)
m = cv2.imread(ruta_m, 0) > 0
H, W = m.shape
res = lama.copy()

# 1) La tapa del libro de arriba.
tapa = np.zeros((H, W), np.uint8)
cv2.fillPoly(tapa, [np.array([(2046, 1341), (2172, 1341), (2189, 1372), (2046, 1372)], np.int32)], 255)
tapa = tapa > 0
# Lo que se ve de la tapa (sin las orillas) decide un degradado plano (color = a + b·x + c·y) que llena el resto.
borde = cv2.erode(tapa.astype(np.uint8), np.ones((5, 5), np.uint8)) > 0
# Solo la parte más clara cuenta: lo oscuro era la sombra del tazón, que ya no está. Y se rehace toda la tapa.
ys, xs = np.nonzero(borde & ~m)
luz = o[ys, xs].mean(1)
claro = luz >= np.median(luz)
ys, xs = ys[claro], xs[claro]
A = np.stack([np.ones(len(xs)), xs - 2100.0, ys - 1355.0], 1)
coef = np.linalg.lstsq(A, o[ys, xs], rcond=None)[0]
ys, xs = np.nonzero(borde)
res[ys, xs] = np.stack([np.ones(len(xs)), xs - 2100.0, ys - 1355.0], 1) @ coef

# 2) El mármol donde se reflejaba el florero (de la fila 1412 para abajo, a la izquierda de los libros): el color
#    de fondo es un degradado entre el mármol de la izquierda y el de debajo de los libros; de LaMa, la textura fina.
marmol = m.copy()
marmol[:1412] = False
marmol[:, 2046:] = False
fondo = np.zeros_like(o)
for y in range(1412, min(H, 1490)):
    xs = np.where(marmol[y])[0]
    if len(xs) == 0:
        continue
    a = xs.min()
    ci = np.median(res[y, max(0, a - 30):a - 4], axis=0)
    cd = np.median(o[y, 2050:2092], axis=0)
    t = ((xs - a) / max(1, 2046 - a))[:, None]
    fondo[y, xs] = ci * (1 - t) + cd * t
peso = cv2.GaussianBlur(marmol.astype(np.float32), (0, 0), 3)[..., None]
fondo = cv2.GaussianBlur(fondo, (0, 0), 3) / np.maximum(peso, 1e-3)
detalle = lama - cv2.GaussianBlur(lama, (0, 0), 5)
nuevo = fondo + detalle * 0.4
# A la derecha (junto a lo de debajo de los libros) el degradado ya empata: ahí no se suaviza la orilla.
ext = marmol.copy()
ext[1412:1490, 2040:2060] = True
dist = cv2.distanceTransform(ext.astype(np.uint8), cv2.DIST_L2, 5)
a = np.clip(dist / 6.0, 0, 1)[..., None]
res = res * (1 - a) + nuevo * a
cv2.imwrite(salida, np.clip(res + 0.5, 0, 255).astype(np.uint8), [cv2.IMWRITE_JPEG_QUALITY, 95])
print("listo")
