# Sala de lujo (v38): lo que se quita con LaMa. Coordenadas en pixeles de la foto 4096 x 2048.
#  - "sofa": el sofá CERCANO que pasa por debajo de ti (abajo a la izquierda y abajo a la derecha: es una sola
#    pieza que cruza la costura) y la MESITA OSCURA que está en la costura. El sofá grande de atrás se queda.
#  - "mesa": en la mesa de centro, la planta con su florero de vidrio, el plato y el tazón con la figurita (y sus
#    reflejos en el mármol). Los libros se quedan.
# Uso: python3 mascara.py foto.jpg mascara.png [sofa|mesa|todo]
import sys
import numpy as np, cv2
foto, salida = sys.argv[1:3]
solo = sys.argv[3] if len(sys.argv) > 3 else "todo"
img = cv2.imread(foto)
H, W = img.shape[:2]
m = np.zeros((H, W), np.uint8)

def poligono(puntos, desplazar=0):
    p = np.array([[x + desplazar, y] for x, y in puntos], np.int32)
    cv2.fillPoly(m, [p], 255)

if solo in ("sofa", "todo"):
    # El sofá cercano: su orilla de arriba, de abajo-centro a la punta (junto al sofá grande, en la costura) y de
    # ahí otra vez abajo hacia el centro. Se dibuja dos veces (corrido una foto a la izquierda) para cruzar la costura.
    orilla = [(2925, 2048), (3100, 1978), (3300, 1915), (3500, 1840), (3700, 1742), (3800, 1690), (3900, 1622),
              (4000, 1535), (4060, 1440), (4096 + 60, 1480), (4096 + 100, 1730), (4096 + 200, 1780),
              (4096 + 300, 1820), (4096 + 500, 1885), (4096 + 700, 1930), (4096 + 900, 1972),
              (4096 + 1100, 2008), (4096 + 1200, 2030), (4096 + 1260, 2048)]
    poligono(orilla)
    poligono(orilla, -W)
    # La mesita oscura de la costura (de -45 a 200), con su sombra.
    cv2.rectangle(m, (0, 1325), (205, 1712), 255, -1)
    cv2.rectangle(m, (W - 45, 1325), (W - 1, 1712), 255, -1)

if solo in ("mesa", "todo"):
    cv2.ellipse(m, (1988, 1243), (120, 58), 0, 0, 360, 255, -1)            # las hojas de la planta
    cv2.rectangle(m, (1938, 1270), (2040, 1380), 255, -1)                   # el florero de vidrio
    cv2.rectangle(m, (1890, 1372), (2040, 1481), 255, -1)                   # su reflejo en el mármol
    cv2.ellipse(m, (1890, 1381), (82, 30), 0, 0, 360, 255, -1)              # el plato
    cv2.rectangle(m, (1806, 1395), (1966, 1448), 255, -1)                   # su sombra y reflejo
    cv2.ellipse(m, (2106, 1318), (70, 42), 0, 0, 360, 255, -1)              # el tazón
    cv2.rectangle(m, (2092, 1218), (2138, 1312), 255, -1)                   # la figurita
    cv2.rectangle(m, (2080, 1336), (2160, 1366), 255, -1)                   # la base del tazón sobre los libros
    cv2.rectangle(m, (1858, 1232), (1918, 1300), 255, -1)                   # la hojita de la izquierda
    cv2.rectangle(m, (1866, 1334), (1914, 1362), 255, -1)                   # la figurita dentro del plato

m = cv2.dilate(m, np.ones((9, 9), np.uint8))
cv2.imwrite(salida, m)
print("listo", int((m > 0).sum()))
