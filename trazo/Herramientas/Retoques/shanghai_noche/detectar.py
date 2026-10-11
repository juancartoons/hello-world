# Busca personas en la foto 360 (YOLOX, Apache-2.0) por pedazos y guarda las cajas.
import sys, json
import numpy as np
from PIL import Image, ImageDraw
import onnxruntime as ort

modelo, foto, salida, vista = sys.argv[1:5]
so = ort.SessionOptions(); so.log_severity_level = 3
net = ort.InferenceSession(modelo, so, providers=['CPUExecutionProvider'])
im = Image.open(foto).convert('RGB')
W, H = im.size
arr = np.asarray(im)

# rejillas de YOLOX
grids, strides = [], []
for s in (8, 16, 32):
    n = 640 // s
    xv, yv = np.meshgrid(np.arange(n), np.arange(n))
    grids.append(np.stack((xv, yv), 2).reshape(-1, 2))
    strides.append(np.full((n * n, 1), s))
grids = np.concatenate(grids); strides = np.concatenate(strides)

def detectar(pedazo, escala):
    img = Image.fromarray(pedazo).resize((int(pedazo.shape[1] * escala), int(pedazo.shape[0] * escala)), Image.BICUBIC)
    lienzo = np.full((640, 640, 3), 114, np.float32)
    a = np.asarray(img, np.float32)
    lienzo[:a.shape[0], :a.shape[1]] = a[:640, :640]
    out = net.run(None, {'images': lienzo.transpose(2, 0, 1)[None]})[0][0]
    xy = (out[:, :2] + grids) * strides
    wh = np.exp(out[:, 2:4]) * strides
    score = out[:, 4] * out[:, 5]   # clase 0 = persona
    k = score > 0.25
    cajas = []
    for (cx, cy), (w, h), sc in zip(xy[k], wh[k], score[k]):
        cajas.append([(cx - w / 2) / escala, (cy - h / 2) / escala, (cx + w / 2) / escala, (cy + h / 2) / escala, float(sc)])
    return cajas

def nms(cajas, umbral=0.45):
    cajas = sorted(cajas, key=lambda c: -c[4])
    quedan = []
    for c in cajas:
        ok = True
        for q in quedan:
            ix = max(0, min(c[2], q[2]) - max(c[0], q[0])); iy = max(0, min(c[3], q[3]) - max(c[1], q[1]))
            inter = ix * iy
            ua = (c[2] - c[0]) * (c[3] - c[1]) + (q[2] - q[0]) * (q[3] - q[1]) - inter
            menor = min((c[2] - c[0]) * (c[3] - c[1]), (q[2] - q[0]) * (q[3] - q[1]))
            if inter / max(ua, 1e-6) > umbral or inter / max(menor, 1e-6) > 0.8:
                ok = False; break
        if ok:
            quedan.append(c)
    return quedan

todas = []
y0, y1 = 760, 1400          # la franja donde está la gente (alrededor del horizonte)
for escala, lado in ((2.0, 320), (1.25, 512)):
    paso = lado * 3 // 4
    for x in range(0, W, paso):
        for y in range(y0, y1 - lado // 2, paso):
            xs = [(x + i) % W for i in range(lado)]
            pedazo = arr[y:y + lado][:, xs]
            for c in detectar(pedazo, escala):
                todas.append([c[0] + x, c[1] + y, c[2] + x, c[3] + y, c[4]])
cajas = [c for c in nms(todas) if (c[3] - c[1]) > 12 and (c[3] - c[1]) < 600]
json.dump(cajas, open(salida, 'w'))
print('personas:', len(cajas))
d = ImageDraw.Draw(im)
for c in cajas:
    d.rectangle(c[:4], outline=(0, 255, 0), width=3)
im.crop((0, 760, W, 1400)).save(vista)
