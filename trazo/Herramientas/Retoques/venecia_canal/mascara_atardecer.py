# Venecia (v38): lo que se quita con LaMa antes del cielo nuevo:
#  - la baranda de metal torcida junto al pilar del puente (el tubo, el enredo oscuro de abajo y el poste negro);
#  - las chimeneas de la casa roja;
#  - la antena de la casa amarilla.
# Coordenadas en pixeles de la foto 4096 x 2048.
# Uso: python3 mascara_atardecer.py foto.jpg mascara.png
import sys
import numpy as np, cv2
foto, salida = sys.argv[1:3]
img = cv2.imread(foto)
H, W = img.shape[:2]
m = np.zeros((H, W), np.uint8)
def linea(puntos, grosor):
    cv2.polylines(m, [np.array(puntos, np.int32)], False, 255, grosor, cv2.LINE_AA)
# Baranda: todo el metal junto al pilar (el tubo plateado que baja hacia la izquierda, el enredo oscuro de abajo,
# el poste negro y la barra de abajo), en una sola zona entre el borde del muelle y el pilar.
cv2.fillPoly(m, [np.array([(2158, 1340), (2148, 1360), (2148, 1582), (2100, 1612), (2062, 1634), (2000, 1568),
                           (2002, 1492), (2028, 1458), (2068, 1418), (2108, 1380), (2138, 1348)], np.int32)], 255)
# Chimeneas de la casa roja.
for x0, y0, x1, y1 in [(978, 692, 1001, 724), (999, 690, 1021, 737), (1046, 735, 1068, 774), (1058, 732, 1106, 798)]:
    cv2.rectangle(m, (x0, y0), (x1, y1), 255, -1)
# Antena de la casa amarilla (el brazo y el palo).
linea([(661, 476), (728, 495)], 8)
linea([(727, 492), (716, 642)], 9)
m = cv2.dilate(m, np.ones((5, 5), np.uint8))
cv2.imwrite(salida, m)
print("pixeles a rellenar:", int((m > 0).sum()))
