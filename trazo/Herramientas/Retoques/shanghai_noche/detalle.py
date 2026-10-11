# Mezcla: los colores y la luz de la referencia (paso 2, ampliada) + solo el detalle fino (bordes, ventanas)
# de la versión con Real-ESRGAN. Así no se queman los blancos y se gana nitidez. Da la vuelta (360).
import sys
import numpy as np, cv2
cv2.setNumThreads(4)
ref_p, ia_p, sal_p = sys.argv[1:4]
sigma = float(sys.argv[4]) if len(sys.argv) > 4 else 3.0
ia = cv2.imread(ia_p).astype(np.float32)
H, W = ia.shape[:2]
ref = cv2.resize(cv2.imread(ref_p), (W, H), interpolation=cv2.INTER_CUBIC).astype(np.float32)
def borroso(x):
    P = 32
    e = np.concatenate([x[:, -P:], x, x[:, :P]], 1)
    return cv2.GaussianBlur(e, (0, 0), sigma)[:, P:-P]
out = borroso(ref) + (ia - borroso(ia))
cv2.imwrite(sal_p, np.clip(out + 0.5, 0, 255).astype(np.uint8), [cv2.IMWRITE_JPEG_QUALITY, 95])
