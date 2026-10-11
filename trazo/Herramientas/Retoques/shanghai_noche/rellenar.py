# Rellena lo marcado con LaMa (Apache-2.0), por ventanas de 512x512 a tamaño real, de izquierda a derecha:
# cada ventana solo deja lo de sus primeras 3/4 partes (así la siguiente tiene contexto ya rellenado).
import sys
import numpy as np, cv2
import onnxruntime as ort

modelo, foto, mascara, salida = sys.argv[1:5]
so = ort.SessionOptions(); so.log_severity_level = 3
net = ort.InferenceSession(modelo, so, providers=['CPUExecutionProvider'])
img = cv2.cvtColor(cv2.imread(foto), cv2.COLOR_BGR2RGB)
m = (cv2.imread(mascara, 0) > 0)
H, W = m.shape
L = 512
paso = 256   # cada ventana ve 128 columnas ya rellenadas a su izquierda (continúa lo que dejó la anterior)
quedan = m.copy()
x0 = 0
primera = True
while x0 < W:
    xs = np.arange(x0, x0 + L) % W           # da la vuelta (360)
    zona = quedan[:, xs]
    if zona.any():
        f = np.where(zona.any(1))[0]
        y0 = int(max(0, min(H - L, (f.min() + f.max()) // 2 - L // 2)))
        mv = zona[y0:y0 + L]
        iv = img[y0:y0 + L][:, xs].astype(np.float32) / 255.0
        out = net.run(None, {'image': iv.transpose(2, 0, 1)[None], 'mask': mv[None, None].astype(np.float32)})[0][0]
        out = np.clip(out.transpose(1, 2, 0), 0, 255)
        if out.max() <= 1.5:
            out = out * 255.0
        desde = 0 if primera else 128
        hasta = L if x0 + 384 >= W else 384
        for j in range(desde, hasta):
            x = xs[j]
            col = quedan[y0:y0 + L, x]
            if col.any():
                img[y0:y0 + L, x][col] = out[:, j][col].astype(np.uint8)
                quedan[y0:y0 + L, x] = False
        print('ventana', x0, 'lista (y', y0, ')')
    primera = False
    x0 += paso
print('sin rellenar:', int(quedan.sum()))
cv2.imwrite(salida, cv2.cvtColor(img, cv2.COLOR_RGB2BGR), [cv2.IMWRITE_JPEG_QUALITY, 95])
