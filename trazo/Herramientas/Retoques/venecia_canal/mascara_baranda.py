# Máscara de la baranda del puente (Canal de Venecia), medida a mano sobre una cuadrícula:
# la reja de hierro es una franja entre el pasamanos y el borde de piedra; los postes, polígonos.
# Coordenadas de la vista a la mitad: completa = 350 + 2x, 1200 + 2y.
import sys
import numpy as np, cv2

foto, salida, vista = sys.argv[1:4]
img = cv2.imread(foto)
H, W = img.shape[:2]
m = np.zeros((H, W), np.uint8)
P = lambda pts: np.array([[350 + 2 * x, 1200 + 2 * y] for x, y in pts], np.int32)
# Solo la reja de hierro entre los dos postes de piedra: los postes y sus tramos hacia las escaleras se quedan.
arriba = [(152, 118), (180, 133), (250, 183), (320, 208), (400, 218), (480, 221), (560, 215), (640, 203),
          (720, 183), (790, 158), (850, 123), (874, 106)]
abajo = [(154, 224), (200, 260), (260, 292), (330, 307), (420, 314), (520, 314), (600, 309), (680, 294),
         (760, 274), (820, 252), (860, 228), (874, 218)]
cv2.fillPoly(m, [P(arriba + abajo[::-1])], 255)
for p in (
):
    cv2.fillPoly(m, [P(p)], 255)
m = cv2.dilate(m, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (13, 13)))
cv2.imwrite(salida, m)
v = img.copy()
v[m > 0] = (0.5 * v[m > 0] + 0.5 * np.array([255, 0, 255])).astype(np.uint8)
cv2.imwrite(vista, cv2.resize(v[1200:1900, 350:2350], (1000, 350)))
print('pixeles marcados: %.2f %%' % (100.0 * (m > 0).mean()))
