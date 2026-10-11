# Igual que rellenar2d.py, pero rellena a media resolución (cada ventana ve el doble de alrededor, mejor
# para huecos grandes) y pega el relleno ampliado solo dentro de lo marcado, con la orilla suavizada.
import sys, subprocess, os
import numpy as np, cv2
modelo, foto, mascara, salida = sys.argv[1:5]
img = cv2.imread(foto); m = cv2.imread(mascara, 0)
H, W = m.shape
cv2.imwrite('_mitad.png', cv2.resize(img, (W // 2, H // 2), interpolation=cv2.INTER_AREA))
cv2.imwrite('_mitad_m.png', (cv2.resize(m, (W // 2, H // 2), interpolation=cv2.INTER_AREA) > 0).astype(np.uint8) * 255)
subprocess.run([sys.executable, os.path.join(os.path.dirname(os.path.abspath(__file__)), 'rellenar2d.py'), modelo, '_mitad.png', '_mitad_m.png', '_mitad_lista.png'], check=True)
lleno = cv2.resize(cv2.imread('_mitad_lista.png'), (W, H), interpolation=cv2.INTER_CUBIC)
a = cv2.GaussianBlur((m > 0).astype(np.float32), (0, 0), 3)[..., None]
a = np.maximum(a, (m > 0)[..., None].astype(np.float32))
res = img.astype(np.float32) * (1 - a) + lleno.astype(np.float32) * a
cv2.imwrite(salida, np.clip(res + 0.5, 0, 255).astype(np.uint8), [cv2.IMWRITE_JPEG_QUALITY, 95])
