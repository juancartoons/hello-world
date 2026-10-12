# Azotea de Nueva York (v38): lo que se quita con LaMa.
#  - La BARANDA de vidrio: la barra de arriba (una raya oscura casi horizontal) y los postes (rayitas oscuras
#    verticales que van de la barra de arriba a la de abajo). Los postes se encuentran solos: columnas donde, a lo
#    alto de toda la baranda, el pixel es más oscuro que sus vecinos de los lados.
#  - Los dos ARBOLITOS en macetas que se mezclan con los edificios de atrás (con su maceta).
# Coordenadas en pixeles de la foto 4096 x 2048.
# Uso: python3 mascara.py foto.jpg mascara.png
import sys
import numpy as np, cv2
foto, salida = sys.argv[1:3]
solo = sys.argv[3] if len(sys.argv) > 3 else "todo"   # "baranda", "arboles" o "todo"
img = cv2.imread(foto)
H, W = img.shape[:2]
g = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY).astype(np.float32) / 255.0
m = np.zeros((H, W), np.uint8)
X0, X1 = 500, 3570                 # de dónde a dónde va la baranda (de frente)
# La barra de arriba: en cada tramo de 64 columnas, la fila más oscura entre 1080 y 1112.
for x in range(X0, X1, 64):
    banda = g[1080:1112, x:x + 64].mean(1)
    y = 1080 + int(np.argmin(banda))
    cv2.rectangle(m, (x, y - 7), (min(X1, x + 64), y + 7), 255, -1)
# Los postes.
Y0, Y1 = 1104, 1228
b = cv2.GaussianBlur(g[Y0:Y1], (0, 0), 0.8)
izq = np.roll(b, 6, axis=1)
der = np.roll(b, -6, axis=1)
oscuro = (b < np.minimum(izq, der) - 0.025)
frac = oscuro.mean(0)
postes = []
for x in range(X0, X1):
    if frac[x] > 0.62 and frac[x] >= frac[max(0, x - 3):x + 4].max():
        if not postes or x - postes[-1] > 20:
            postes.append(x)
# Los que el detector no encontró (revisados a ojo).
EXTRA = [575, 671, 945, 1548, 1830, 2535, 3132]
for x in sorted(set(postes) | set(EXTRA)):
    cv2.rectangle(m, (x - 9, Y0 - 24), (x + 9, Y1 + 26), 255, -1)
if solo == "arboles":
    m[:] = 0
print("postes encontrados:", len(postes), postes[:40])
# Arbolitos (copa, tronco y maceta).
if solo == "baranda":
    m2 = m.copy()
cv2.fillPoly(m, [np.array([(250, 905), (330, 895), (420, 890), (505, 905), (512, 1000), (500, 1110), (470, 1168),
                           (462, 1250), (342, 1250), (338, 1168), (300, 1150), (250, 1110), (246, 1000)], np.int32)], 255)
cv2.fillPoly(m, [np.array([(3555, 935), (3640, 925), (3740, 930), (3802, 950), (3806, 1060), (3790, 1150), (3702, 1172),
                           (3700, 1248), (3590, 1248), (3588, 1172), (3560, 1150), (3552, 1040)], np.int32)], 255)
if solo == "baranda":
    m = m2
m = cv2.dilate(m, np.ones((5, 5), np.uint8))
cv2.imwrite(salida, m)
print("pixeles a rellenar:", int((m > 0).sum()))
