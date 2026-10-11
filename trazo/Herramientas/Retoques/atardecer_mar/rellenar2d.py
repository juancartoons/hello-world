# Rellena lo marcado con LaMa (Apache-2.0) en una cuadrícula de ventanas 512x512 a tamaño real que se traslapan,
# de arriba a abajo y de izquierda a derecha: cada ventana deja solo su centro, así la siguiente tiene
# contexto ya rellenado arriba y a la izquierda (sin costuras). Funciona para zonas de cualquier tamaño.
import sys
import numpy as np, cv2
import onnxruntime as ort

modelo, foto, mascara, salida = sys.argv[1:5]
so = ort.SessionOptions(); so.log_severity_level = 3
net = ort.InferenceSession(modelo, so, providers=['CPUExecutionProvider'])
img = cv2.cvtColor(cv2.imread(foto), cv2.COLOR_BGR2RGB)
quedan = cv2.imread(mascara, 0) > 0
H, W = quedan.shape
L, PASO = 512, 256
filas = np.where(quedan.any(1))[0]; cols = np.where(quedan.any(0))[0]
ya = max(0, min(H - L, filas.min() - 128)); yb = min(H - L, max(0, filas.max() - L + 128))
xa = cols.min() - 128; xb = cols.max() - L + 128
ys = list(range(ya, yb + 1, PASO)) or [ya]
if ys[-1] < yb: ys.append(yb)
xs0 = list(range(xa, xb + 1, PASO)) or [xa]
if xs0[-1] < xb: xs0.append(xb)
for vuelta in range(3):
    for iy, y0 in enumerate(ys):
        for ix, x0 in enumerate(xs0):
            xs = np.arange(x0, x0 + L) % W
            mv = quedan[y0:y0 + L][:, xs]
            if not mv.any():
                continue
            iv = img[y0:y0 + L][:, xs].astype(np.float32) / 255.0
            out = net.run(None, {'image': iv.transpose(2, 0, 1)[None], 'mask': mv[None, None].astype(np.float32)})[0][0]
            out = np.clip(out.transpose(1, 2, 0), 0, 255)
            if out.max() <= 1.5:
                out *= 255.0
            # qué parte de la ventana se queda: el centro (y hasta la orilla si es la primera o la última)
            ya_ = 0 if iy == 0 or vuelta > 0 else 128
            yb_ = L if iy == len(ys) - 1 or vuelta > 0 else 384
            xa_ = 0 if ix == 0 or vuelta > 0 else 128
            xb_ = L if ix == len(xs0) - 1 or vuelta > 0 else 384
            sub = mv.copy(); sub[:ya_] = False; sub[yb_:] = False; sub[:, :xa_] = False; sub[:, xb_:] = False
            yy, xx = np.nonzero(sub)
            img[y0 + yy, xs[xx]] = out[yy, xx].astype(np.uint8)
            quedan[y0 + yy, xs[xx]] = False
    print('vuelta', vuelta, 'sin rellenar:', int(quedan.sum()))
    if not quedan.any():
        break
cv2.imwrite(salida, cv2.cvtColor(img, cv2.COLOR_RGB2BGR), [cv2.IMWRITE_JPEG_QUALITY, 95])
