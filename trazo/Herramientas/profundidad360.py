# Mapa de distancias (profundidad) para una foto 360 equirectangular, con Depth Anything V2 (small, ONNX).
# Uso: python3 profundidad360.py modelo.onnx foto.jpg salida.bytes vista.png
# Distancias en unidades de la ALTURA DE LA CÁMARA (el piso está a 1 unidad debajo del centro).
import sys, math, struct
import numpy as np
from PIL import Image
import onnxruntime as ort

N = 518
MEAN = np.array([0.485, 0.456, 0.406], np.float32)
STD = np.array([0.229, 0.224, 0.225], np.float32)
FOV = math.radians(90.0)
TANH = math.tan(FOV / 2)
DMIN, DMAX = 0.05, 60.0

modelo, ruta_foto, ruta_salida, ruta_vista = sys.argv[1:5]
sess = ort.InferenceSession(modelo, providers=["CPUExecutionProvider"])
foto = np.asarray(Image.open(ruta_foto).convert("RGB"), np.float32) / 255.0
H, W = foto.shape[:2]


def rot(yaw, pitch):
    # cámara -> mundo. Mundo: x derecha, y arriba, z adelante. yaw a la derecha, pitch hacia arriba.
    cy, sy = math.cos(yaw), math.sin(yaw)
    cp, sp = math.cos(pitch), math.sin(pitch)
    Ry = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]], np.float64)
    Rx = np.array([[1, 0, 0], [0, cp, sp], [0, -sp, cp]], np.float64)
    return Ry @ Rx


def muestrear_equi(dirs):
    lon = np.arctan2(dirs[..., 0], dirs[..., 2])
    lat = np.arcsin(np.clip(dirs[..., 1], -1, 1))
    u = (lon / (2 * np.pi) + 0.5) * W - 0.5
    v = (0.5 - lat / np.pi) * H - 0.5
    u0 = np.floor(u).astype(int); v0 = np.floor(v).astype(int)
    fu = (u - u0)[..., None]; fv = (v - v0)[..., None]
    u1 = (u0 + 1) % W; u0 = u0 % W
    v1 = np.clip(v0 + 1, 0, H - 1); v0 = np.clip(v0, 0, H - 1)
    a = foto[v0, u0] * (1 - fu) + foto[v0, u1] * fu
    b = foto[v1, u0] * (1 - fu) + foto[v1, u1] * fu
    return a * (1 - fv) + b * fv


# Rayos de la cámara (coordenadas de cámara), una vez.
jj, ii = np.meshgrid(np.arange(N), np.arange(N))
xc = (2 * (jj + 0.5) / N - 1) * TANH
yc = (1 - 2 * (ii + 0.5) / N) * TANH
rc = np.stack([xc, yc, np.ones_like(xc)], -1)
cosc = 1.0 / np.linalg.norm(rc, axis=-1)          # coseno con el eje de la vista
rc = rc * cosc[..., None]

vistas = []
for yaw in range(0, 360, 45):
    vistas.append((math.radians(yaw), 0.0))
for yaw in range(0, 360, 90):
    vistas.append((math.radians(yaw + 45), math.radians(55)))
    vistas.append((math.radians(yaw + 45), math.radians(-55)))
vistas.append((0.0, math.radians(90)))
vistas.append((0.0, math.radians(-90)))

preds, Rs = [], []
for k, (yaw, pitch) in enumerate(vistas):
    R = rot(yaw, pitch)
    dirs = rc @ R.T
    img = muestrear_equi(dirs)
    x = ((img - MEAN) / STD).transpose(2, 0, 1)[None].astype(np.float32)
    p = sess.run(None, {sess.get_inputs()[0].name: x})[0][0].astype(np.float64)
    p = p / max(1e-6, np.percentile(p, 99))       # escala cómoda (la IA da distancias relativas)
    preds.append(p)
    Rs.append(R)
    print("vista", k, "listo", flush=True)
K = len(vistas)


def proyectar(R, dirs):
    # mundo -> pixel de la vista (fila, columna) y dentro/fuera
    d = dirs @ R            # = R.T @ dir
    z = d[..., 2]
    ok = z > 1e-3
    x = np.where(ok, d[..., 0] / np.maximum(z, 1e-3), 9)
    y = np.where(ok, d[..., 1] / np.maximum(z, 1e-3), 9)
    col = (x / TANH + 1) * N / 2 - 0.5
    fila = (1 - y / TANH) * N / 2 - 0.5
    ok &= (np.abs(x) < TANH * 0.98) & (np.abs(y) < TANH * 0.98)
    return fila, col, ok, z / np.linalg.norm(d, axis=-1)


def bilineal(img, fila, col):
    f0 = np.clip(np.floor(fila).astype(int), 0, N - 2); c0 = np.clip(np.floor(col).astype(int), 0, N - 2)
    ff = np.clip(fila - f0, 0, 1); fc = np.clip(col - c0, 0, 1)
    a = img[f0, c0] * (1 - fc) + img[f0, c0 + 1] * fc
    b = img[f0 + 1, c0] * (1 - fc) + img[f0 + 1, c0 + 1] * fc
    return a * (1 - ff) + b * ff


# ---- Ecuaciones: s = 1/distancia = (a_k p_k + b_k) * cos_k ; debe coincidir entre vistas y con el piso ----
rng = np.random.default_rng(7)
filas_A, filas_b, pesos = [], [], []
for k in range(K):
    for m in range(K):
        if m <= k:
            continue
        ang = math.acos(np.clip(np.dot(Rs[k][:, 2], Rs[m][:, 2]), -1, 1))
        if ang > math.radians(80):
            continue
        n = 4000
        fi = rng.integers(0, N, n); co = rng.integers(0, N, n)
        dirs = rc[fi, co] @ Rs[k].T
        f2, c2, ok, cos2 = proyectar(Rs[m], dirs)
        if ok.sum() < 50:
            continue
        pk = preds[k][fi, co][ok]; ck = cosc[fi, co][ok]
        pm = bilineal(preds[m], f2[ok], c2[ok]); cm = cos2[ok]
        A = np.zeros((ok.sum(), 2 * K))
        A[:, 2 * k] = pk * ck; A[:, 2 * k + 1] = ck
        A[:, 2 * m] = -pm * cm; A[:, 2 * m + 1] = -cm
        filas_A.append(A); filas_b.append(np.zeros(ok.sum())); pesos.append(np.ones(ok.sum()))
# Piso: abajo de -55° casi siempre es piso o suelo, a 1 unidad debajo.
for k in range(K):
    n = 6000
    fi = rng.integers(0, N, n); co = rng.integers(0, N, n)
    dirs = rc[fi, co] @ Rs[k].T
    sel = dirs[:, 1] < math.sin(math.radians(-55))
    if sel.sum() < 50:
        continue
    pk = preds[k][fi, co][sel]; ck = cosc[fi, co][sel]
    A = np.zeros((sel.sum(), 2 * K))
    A[:, 2 * k] = pk * ck; A[:, 2 * k + 1] = ck
    filas_A.append(A); filas_b.append(-dirs[sel, 1]); pesos.append(np.full(sel.sum(), 3.0))
A = np.vstack(filas_A); b = np.concatenate(filas_b); w0 = np.concatenate(pesos)
w = w0.copy()
for it in range(8):
    sw = np.sqrt(w)
    x, *_ = np.linalg.lstsq(A * sw[:, None], b * sw, rcond=None)
    r = A @ x - b
    d = 1.5 * np.median(np.abs(r)) + 1e-6
    w = w0 * np.where(np.abs(r) < d, 1.0, d / np.abs(r))
print("ajuste listo; residuo medio", float(np.median(np.abs(r))))

# ---- Mapa final 512 x 256 (equirectangular) ----
SW, SH = 512, 256
uu, vv = np.meshgrid((np.arange(SW) + 0.5) / SW, (np.arange(SH) + 0.5) / SH)
lon = (uu - 0.5) * 2 * np.pi
lat = (0.5 - vv) * np.pi
dirs = np.stack([np.cos(lat) * np.sin(lon), np.sin(lat), np.cos(lat) * np.cos(lon)], -1)
suma = np.zeros((SH, SW)); peso = np.zeros((SH, SW))
for k in range(K):
    fi, co, ok, ck = proyectar(Rs[k], dirs)
    if not ok.any():
        continue
    p = bilineal(preds[k], np.where(ok, fi, 0), np.where(ok, co, 0))
    s = (x[2 * k] * p + x[2 * k + 1]) * ck
    wk = np.where(ok, ck ** 8, 0)
    suma += wk * s; peso += wk
s = suma / np.maximum(peso, 1e-9)
# El piso es lo más lejos que puede estar algo hacia abajo (no hay huecos en el piso).
piso = np.where(dirs[..., 1] < -0.02, -dirs[..., 1], 0)
s = np.maximum(s, piso * 0.98)
s = np.maximum(s, 1.0 / DMAX)
dist = 1.0 / s
# Suavizado suave (mediana 3x3) para quitar ruido.
pad = np.pad(dist, 1, mode="edge"); pad[:, 0] = pad[:, -2]; pad[:, -1] = pad[:, 1]
pila = np.stack([pad[i:i + SH, j:j + SW] for i in range(3) for j in range(3)])
dist = np.median(pila, axis=0)
dist = np.clip(dist, DMIN, DMAX)
print("distancias (alturas de cámara): min %.2f  mediana %.2f  max %.2f" % (dist.min(), np.median(dist), dist.max()))

# ---- Guardar: "JCP1", ancho, alto (uint16), luego uint16 con log(distancia) (fila 0 = arriba) ----
cod = np.round((np.log(dist) - math.log(DMIN)) / (math.log(DMAX) - math.log(DMIN)) * 65535).astype("<u2")
with open(ruta_salida, "wb") as f:
    f.write(b"JCP1"); f.write(struct.pack("<HH", SW, SH)); f.write(cod.tobytes())

# ---- Vista de control: el mapa (gris) y una vista desde arriba (planta) ----
g = (1 - (np.log(dist) - math.log(DMIN)) / (math.log(DMAX) - math.log(DMIN))) * 255
mapa = Image.fromarray(g.astype(np.uint8)).resize((1024, 512))
P = dirs * dist[..., None]
col = muestrear_equi(dirs)
planta = np.full((512, 512, 3), 255, np.uint8)
esc = 512 / 12.0
selp = (P[..., 1] > -0.9) & (P[..., 1] < 1.2) & (np.abs(P[..., 0]) < 6) & (np.abs(P[..., 2]) < 6)
xs = (P[..., 0][selp] * esc + 256).astype(int); zs = (256 - P[..., 2][selp] * esc).astype(int)
planta[np.clip(zs, 0, 511), np.clip(xs, 0, 511)] = (col[selp] * 255).astype(np.uint8)
planta[250:262, 250:262] = (255, 0, 0)
lienzo = Image.new("RGB", (1024 + 512, 512), "white")
lienzo.paste(mapa.convert("RGB"), (0, 0)); lienzo.paste(Image.fromarray(planta), (1024, 0))
lienzo.save(ruta_vista)
