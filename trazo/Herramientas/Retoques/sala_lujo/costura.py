# Une las orillas izquierda y derecha de una foto 360 (para que no se vea la "costura" detrás de ti):
# rellena con LaMa una franja vertical justo en la unión, en ventanas de 512 que dan la vuelta.
import sys
import numpy as np, cv2
import onnxruntime as ort

modelo, entrada, salida = sys.argv[1:4]
ancho = int(sys.argv[4]) if len(sys.argv) > 4 else 48
so = ort.SessionOptions(); so.log_severity_level = 3
net = ort.InferenceSession(modelo, so, providers=['CPUExecutionProvider'])
img = cv2.cvtColor(cv2.imread(entrada), cv2.COLOR_BGR2RGB)
H, W = img.shape[:2]
L = 512
xs = np.arange(W - L // 2, W + L // 2) % W          # la ventana centrada en la unión
mascara = np.zeros((H, L), bool)
mascara[:, L // 2 - ancho // 2: L // 2 + ancho // 2] = True
ys = list(range(0, max(1, H - L + 1), 384))
if ys[-1] != H - L:
    ys.append(H - L)
quedan = mascara.copy()
for y0 in ys:
    mv = quedan[y0:y0 + L]
    if not mv.any():
        continue
    iv = img[y0:y0 + L][:, xs].astype(np.float32) / 255.0
    out = net.run(None, {'image': iv.transpose(2, 0, 1)[None], 'mask': mv[None, None].astype(np.float32)})[0][0]
    out = np.clip(out.transpose(1, 2, 0), 0, 255)
    if out.max() <= 1.5:
        out *= 255.0
    hasta = L if y0 == ys[-1] else 384
    for j, x in enumerate(xs):
        col = quedan[y0:y0 + hasta, j]
        if col.any():
            img[y0:y0 + hasta, x][col] = out[:hasta, j][col].astype(np.uint8)
            quedan[y0:y0 + hasta, j] = False
cv2.imwrite(salida, cv2.cvtColor(img, cv2.COLOR_RGB2BGR))
a = img.astype(float)
print('diferencia entre orillas ahora:', np.abs(a[:, :3].mean(1) - a[:, -3:].mean(1)).mean().round(1))
