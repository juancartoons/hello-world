# Máscara de la gente: cajas agrandadas (más hacia abajo, por los pies y su reflejo en el piso mojado),
# unidas cuando están casi pegadas, y zonas extra marcadas a mano (gente "fantasma" movida).
import sys, json
import numpy as np, cv2

foto, cajas_json, extras_json, salida, vista = sys.argv[1:6]
img = cv2.imread(foto)
H, W = img.shape[:2]
m = np.zeros((H, W), np.uint8)
for x0, y0, x1, y1, sc in json.load(open(cajas_json)):
    w, h = x1 - x0, y1 - y0
    a = (int(x0 - 0.15 * w - 4), int(y0 - 0.08 * h - 3), int(x1 + 0.15 * w + 4), int(y1 + 0.3 * h + 4))
    for dx in (0, -W, W):  # la foto da la vuelta (360)
        cv2.rectangle(m, (a[0] + dx, a[1]), (a[2] + dx, a[3]), 255, -1)
for x0, y0, x1, y1 in json.load(open(extras_json)):
    cv2.rectangle(m, (x0, y0), (x1, y1), 255, -1)
# unir las que están casi pegadas (en horizontal)
m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_RECT, (81, 61)))
m = cv2.dilate(m, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 7)))
cv2.imwrite(salida, m)
v = img.copy()
v[m > 0] = (0.45 * v[m > 0] + 0.55 * np.array([255, 0, 255])).astype(np.uint8)
cv2.imwrite(vista, v[760:1400])
print('pixeles marcados: %.2f %%' % (100.0 * (m > 0).mean()))
