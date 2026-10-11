# Paso 2: quitar la neblina (velo) y dar contraste local, sin romper la costura del 360
# (todos los filtros "dan la vuelta": se rellena a los lados con la otra orilla de la foto).
import sys
import numpy as np, cv2

entrada, salida = sys.argv[1:3]
fuerza = float(sys.argv[3]) if len(sys.argv) > 3 else 0.55     # cuánto velo se quita (0..1)
clahe_mezcla = float(sys.argv[4]) if len(sys.argv) > 4 else 0.5
img = cv2.imread(entrada).astype(np.float32) / 255.0
H, W = img.shape[:2]
P = 256
ext = np.concatenate([img[:, -P:], img, img[:, :P]], 1)       # vuelta 360

def guiado(I, p, r, eps):
    m = lambda x: cv2.boxFilter(x, -1, (r, r))
    mI, mp = m(I), m(p)
    a = (m(I * p) - mI * mp) / (m(I * I) - mI * mI + eps)
    b = mp - a * mI
    return m(a) * I + m(b)

# 1. El velo: el canal oscuro (lo más oscuro de cada zona), suavizado siguiendo los bordes.
oscuro = cv2.erode(ext.min(2), cv2.getStructuringElement(cv2.MORPH_RECT, (15, 15)))
gris = cv2.cvtColor(ext, cv2.COLOR_BGR2GRAY)
velo = np.clip(guiado(gris, oscuro, 61, 1e-3), 0, 1)
# El color de la neblina: el promedio de lo más brillante del canal oscuro (el resplandor morado del cielo).
plano = velo.ravel(); k = max(1, plano.size // 1000)
idx = np.argpartition(plano, -k)[-k:]
A = ext.reshape(-1, 3)[idx].mean(0)
t = np.clip(1.0 - fuerza * velo[..., None] / np.maximum(A, 1e-3).max(), 0.35, 1.0)
J = (ext - A) / t + A
J = np.clip(J, 0, 1)

# 2. Contraste local (CLAHE en la luz, no en el color).
lab = cv2.cvtColor((J * 255).astype(np.uint8), cv2.COLOR_BGR2LAB)
cl = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(32, 8)).apply(lab[..., 0])
lab[..., 0] = (clahe_mezcla * cl + (1 - clahe_mezcla) * lab[..., 0]).astype(np.uint8)
J = cv2.cvtColor(lab, cv2.COLOR_LAB2BGR).astype(np.float32) / 255.0

# 3. Un poquito más de color (sin quemar los neones).
hsv = cv2.cvtColor(J, cv2.COLOR_BGR2HSV)
hsv[..., 1] = np.clip(hsv[..., 1] * 1.08, 0, 1)
J = cv2.cvtColor(hsv, cv2.COLOR_HSV2BGR)

J = J[:, P:P + W]
cv2.imwrite(salida, np.clip(J * 255 + 0.5, 0, 255).astype(np.uint8), [cv2.IMWRITE_JPEG_QUALITY, 95])
print('color de la neblina (BGR):', np.round(A, 3))
