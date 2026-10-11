# Máscara de la baranda junto al mar (medida a mano sobre una cuadrícula): postes como polígonos y barras
# como líneas gruesas que siguen su curva. Coordenadas de la vista a la mitad: completa = 1550 + 2x, 950 + 2y.
import sys, json
import numpy as np, cv2

foto, salida, vista = sys.argv[1:4]
img = cv2.imread(foto)
H, W = img.shape[:2]
m = np.zeros((H, W), np.uint8)
P = lambda pts: np.array([[1550 + 2 * x, 950 + 2 * y] for x, y in pts], np.int32)
postes = [
    [(128, 128), (160, 108), (188, 124), (198, 338), (128, 344)],          # poste grande izquierdo
    [(930, 108), (955, 92), (978, 104), (988, 298), (928, 304)],           # poste grande derecho
    [(28, 25), (62, 25), (138, 118), (138, 250), (100, 215), (28, 130)],   # tramo lejano izquierdo
    [(972, 70), (1046, 25), (1060, 30), (1060, 110), (1038, 165), (992, 250), (972, 205)],  # tramo lejano derecho
]
postes_m = np.zeros((H, W), np.uint8)
for p in postes[:2]:
    cv2.fillPoly(postes_m, [P(p)], 255)
# los postes, un poco más grandes (que no quede el filito claro de su borde)
m |= cv2.dilate(postes_m, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (31, 31)))
barras = [
    [(160, 172), (200, 200), (260, 240), (330, 270), (400, 290), (470, 300), (540, 305), (610, 300),
     (680, 285), (750, 260), (820, 225), (880, 190), (930, 160), (955, 146)],
    [(160, 274), (200, 300), (260, 340), (330, 370), (400, 390), (470, 402), (540, 405), (610, 402),
     (680, 392), (750, 375), (820, 345), (880, 305), (930, 265), (955, 240)],
]
# Dónde buscar las barras: una franja ancha alrededor de las líneas medidas a mano (y los tramos lejanos).
zona = np.zeros((H, W), np.uint8)
for b in barras:
    cv2.polylines(zona, [P(b)], False, 255, thickness=150)
cv2.fillPoly(zona, [P(postes[2])], 255)   # tramo lejano izquierdo (el derecho se queda: ahí empieza la baranda)
zona[:, 3420:] = 0   # a la derecha, las barras terminan en el poste grande
# La baranda es más "cálida" (naranja/beige) que lo de alrededor y delgada: un "top-hat" resalta lo delgado
# y claro aunque el fondo (el agua con el sol) también sea claro.
img16 = img.astype(np.int16)
calido = np.clip((img16[..., 2] - img16[..., 0]) * 3, 0, 255).astype(np.uint8)
ys, xs = np.nonzero(zona)
y0, y1, x0, x1 = ys.min() - 80, ys.max() + 80, xs.min() - 80, xs.max() + 80
sub = calido[y0:y1, x0:x1]
th = cv2.morphologyEx(sub, cv2.MORPH_TOPHAT, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (71, 71)))
fino = (th > 28).astype(np.uint8) * 255
fino = cv2.morphologyEx(fino, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5)))
detectado = np.zeros((H, W), np.uint8)
detectado[y0:y1, x0:x1] = fino
detectado &= zona
# solo los pedazos grandes (las barras), no puntitos sueltos del agua
n, etiq, stats, _ = cv2.connectedComponentsWithStats(detectado, 8)
for i in range(1, n):
    if stats[i, cv2.CC_STAT_AREA] < 1500:
        detectado[etiq == i] = 0
m |= detectado
for b in barras:   # y por si acaso, una línea delgada en el centro medido a mano
    cv2.polylines(m, [P(b)], False, 255, thickness=24)
m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (21, 21)))
m = cv2.dilate(m, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (19, 19)))
# los pedacitos de barra entre el poste grande derecho y el primer poste lejano (que se queda), en coordenadas
# completas: terminan justo antes de ese poste.
for b in ([(3495, 1172), (3530, 1138), (3552, 1117)], [(3510, 1295), (3535, 1250), (3553, 1222)]):
    cv2.polylines(m, [np.array(b, np.int32)], False, 255, thickness=44)
cv2.imwrite(salida, m)
v = img.copy()
v[m > 0] = (0.5 * v[m > 0] + 0.5 * np.array([255, 0, 255])).astype(np.uint8)
cv2.imwrite(vista, cv2.resize(v[950:1850, 1550:3750], (1100, 450)))
cv2.imwrite('zoom_der_vista.png', v[950:1350, 3350:3750])
print('pixeles marcados: %.2f %%' % (100.0 * (m > 0).mean()))
