# Venecia al atardecer (v38), sobre la foto ya sin baranda, chimeneas ni antena (mascara_atardecer.py + LaMa):
#  1) CIELO NUEVO: un atardecer (dorado en el horizonte, rosado, lila y azul arriba) con nubes suaves que dan la
#     vuelta completa (360) y el resplandor del sol donde estaba el sol de la foto. La orilla de los edificios se
#     recorta suave y sin el azul viejo pegado (se "descontamina" el borde).
#  2) LUZ CÁLIDA: las partes iluminadas se vuelven doradas y las sombras un poco lilas; el agua refleja el naranja.
#  3) PROFUNDIDAD (el archivo .bytes, 512 x 256):
#     - donde se quitaron cosas, la distancia se rellena con la de alrededor;
#     - el cielo nuevo queda lejos (también donde estaban las chimeneas y la antena);
#     - EL PUENTE: tú estás arriba del puente; las escaleras bajan suave y la plaza y las calles quedan más abajo
#       (antes todo el piso estaba a la altura del puente y se veía plano, con las escaleras torcidas).
# Uso: python3 atardecer.py foto_sin_baranda.jpg mascara_quitado.png profundidad_entrada.bytes foto_salida.jpg \
#                           profundidad_salida.bytes vista.jpg
import sys, math, struct
import numpy as np, cv2

foto_p, quitado_p, prof_in, foto_out, prof_out, vista_p = sys.argv[1:7]
img = cv2.cvtColor(cv2.imread(foto_p), cv2.COLOR_BGR2RGB).astype(np.float32) / 255.0
H, W = img.shape[:2]
vv = (np.arange(H) + 0.5) / H
uu = (np.arange(W) + 0.5) / W
elev = (0.5 - vv) * 180.0                         # grados (+ arriba)

# ---------------- 1) La máscara del cielo ----------------
R, G, B = img[..., 0], img[..., 1], img[..., 2]
mx, mn = img.max(2), img.min(2)
candidato = ((B - R > 0.035) | ((mn > 0.9) & (mx - mn < 0.08))) & (vv[:, None] < 0.52)
n, etiquetas = cv2.connectedComponents(candidato.astype(np.uint8), connectivity=8)
arriba = np.unique(etiquetas[:4][candidato[:4]])
cielo = np.isin(etiquetas, arriba[arriba > 0])
# Cables y antenas finitas dentro del cielo: se vuelven cielo (solo cerca de cielo de verdad).
cerrado = cv2.morphologyEx(cielo.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((11, 11), np.uint8)) > 0
cerca = cv2.dilate(cielo.astype(np.uint8), np.ones((9, 9), np.uint8)) > 0
cielo = (cerrado & cerca) | cielo
cielo &= vv[:, None] < 0.53

def guiado(I, p, r, eps):
    # Filtro guiado (He et al.): la orilla de la máscara sigue los bordes de la foto.
    def caja(x):
        return cv2.blur(x, (2 * r + 1, 2 * r + 1))
    mI, mp = caja(I), caja(p)
    a = (caja(I * p) - mI * mp) / (caja(I * I) - mI * mI + eps)
    b = mp - a * mI
    return caja(a) * I + caja(b)

gris = (0.299 * R + 0.587 * G + 0.114 * B).astype(np.float32)
alfa = np.clip(guiado(gris, cielo.astype(np.float32), 4, 2e-3), 0, 1)
alfa[vv >= 0.53] = 0.0

# Color del cielo viejo cerca de cada borde (promedio del cielo de alrededor).
def promedio_cielo(x, m, s):
    num = cv2.GaussianBlur(x * m[..., None], (0, 0), s)
    den = cv2.GaussianBlur(m, (0, 0), s)[..., None]
    return num / np.maximum(den, 1e-4)
viejo = promedio_cielo(img, cielo.astype(np.float32), 20)

# ---------------- El cielo nuevo ----------------
SOL_U, SOL_E = 0.62, 1.5            # donde estaba el sol de la foto, ya bajito
lon = (uu - 0.5) * 2 * np.pi
lat = np.radians(elev)
dx = np.cos(lat)[:, None] * np.sin(lon)[None, :]
dy = np.sin(lat)[:, None] * np.ones((1, W))
dz = np.cos(lat)[:, None] * np.cos(lon)[None, :]
sl, se = (SOL_U - 0.5) * 2 * np.pi, math.radians(SOL_E)
sol = np.array([math.cos(se) * math.sin(sl), math.sin(se), math.cos(se) * math.cos(sl)])
ang = np.degrees(np.arccos(np.clip(dx * sol[0] + dy * sol[1] + dz * sol[2], -1, 1)))
e = np.clip(elev, 0, 90)[:, None] * np.ones((1, W))
paradas = [(0, (1.00, 0.72, 0.45)), (6, (0.99, 0.60, 0.40)), (16, (0.92, 0.52, 0.50)),
           (30, (0.70, 0.48, 0.62)), (48, (0.45, 0.42, 0.66)), (90, (0.24, 0.30, 0.55))]
cielo_n = np.zeros((H, W, 3), np.float32)
for c in range(3):
    xs = [p[0] for p in paradas]; ys = [p[1][c] for p in paradas]
    cielo_n[..., c] = np.interp(e, xs, ys)
# Al otro lado del sol, un poco más frío y con la franja rosada (el "cinturón de Venus").
lejos_sol = np.clip((ang - 60) / 100, 0, 1)
cielo_n = cielo_n * (1 - 0.18 * lejos_sol[..., None]) + 0.18 * lejos_sol[..., None] * np.array([0.62, 0.52, 0.72])
banda = np.exp(-((e - 11) / 7) ** 2) * lejos_sol
cielo_n += banda[..., None] * np.array([0.16, 0.06, 0.10])
# El resplandor del sol.
cielo_n += np.exp(-(ang / 22) ** 2)[..., None] * np.array([0.35, 0.22, 0.08])
cielo_n += np.exp(-(ang / 4.5) ** 2)[..., None] * np.array([0.45, 0.42, 0.30])
# Nubes suaves: ruido que da la vuelta (FFT), estirado de lado, solo en una franja del cielo.
rng = np.random.default_rng(38)
hn, wn = 512, 2048
ruido = rng.standard_normal((hn, wn))
fy = np.fft.fftfreq(hn)[:, None]; fx = np.fft.fftfreq(wn)[None, :]
filtro = np.exp(-((fx / 0.012) ** 2 + (fy / 0.035) ** 2))
nube = np.real(np.fft.ifft2(np.fft.fft2(ruido) * filtro))
fino = np.real(np.fft.ifft2(np.fft.fft2(rng.standard_normal((hn, wn))) * np.exp(-((fx / 0.05) ** 2 + (fy / 0.12) ** 2))))
nube = nube / nube.std() + 0.35 * fino / fino.std()
nube = cv2.resize(nube.astype(np.float32), (W, H // 2), interpolation=cv2.INTER_CUBIC)
nube = np.vstack([nube, np.zeros((H - H // 2, W), np.float32)])
franja = np.clip((e - 3) / 6, 0, 1) * np.clip((58 - e) / 20, 0, 1)
dens = np.clip((nube - 0.55) / 0.9, 0, 1) ** 1.3 * franja * 0.85
luz_nube = np.exp(-(ang / 60) ** 2)[..., None]
color_nube = (1 - luz_nube) * np.array([0.93, 0.60, 0.62]) + luz_nube * np.array([1.0, 0.70, 0.46])
alto = np.clip((e - 20) / 30, 0, 1)[..., None]
color_nube = color_nube * (1 - 0.45 * alto) + 0.45 * alto * np.array([0.52, 0.44, 0.60])
cielo_n = cielo_n * (1 - dens[..., None]) + color_nube * dens[..., None]
cielo_n = np.clip(cielo_n, 0, 1)

# ---------------- 2) La luz cálida en todo lo demás ----------------
a_fg = 1.0 - alfa
F = img.copy()
borde = (alfa > 0.02) & (alfa < 0.98)
F[borde] = np.clip((img[borde] - alfa[borde][:, None] * viejo[borde]) / np.maximum(a_fg[borde], 0.08)[:, None], 0, 1)
L = 0.299 * F[..., 0] + 0.587 * F[..., 1] + 0.114 * F[..., 2]
calido = np.clip((L - 0.2) / 0.7, 0, 1)
calido = calido * calido * (3 - 2 * calido)
tinte = (1 - calido)[..., None] * np.array([0.92, 0.88, 1.02]) + calido[..., None] * np.array([1.12, 0.93, 0.74])
F = np.clip(F * tinte * 0.93, 0, 1)

# El agua del canal refleja el naranja (el mismo contorno del canal de agua_mas_abajo.py).
CANAL = [
    (0.312, 0.505), (0.330, 0.505), (0.338, 0.530), (0.350, 0.550), (0.370, 0.575), (0.395, 0.600),
    (0.430, 0.620), (0.465, 0.645), (0.475, 0.665), (0.520, 0.690), (0.530, 0.720), (0.545, 0.740),
    (0.535, 0.785), (0.500, 0.820), (0.460, 0.850), (0.400, 0.872), (0.330, 0.885), (0.270, 0.880),
    (0.220, 0.860), (0.192, 0.830), (0.172, 0.790), (0.176, 0.765), (0.192, 0.700), (0.215, 0.675),
    (0.240, 0.665), (0.245, 0.630), (0.260, 0.600), (0.275, 0.580), (0.290, 0.555), (0.300, 0.530),
    (0.310, 0.515),
]
agua = np.zeros((H, W), np.uint8)
cv2.fillPoly(agua, [np.array([(u * W, v * H) for u, v in CANAL], np.int32)], 255)
agua = cv2.GaussianBlur(agua.astype(np.float32) / 255, (0, 0), 6)[..., None]
lejania = np.clip((0.75 - vv) / 0.25, 0, 1)[:, None, None]       # más reflejo hacia lo lejos
F = F + agua * (0.10 + 0.14 * lejania) * np.array([1.0, 0.50, 0.15])
F = np.clip(F, 0, 1)

res = a_fg[..., None] * F + alfa[..., None] * cielo_n
cv2.imwrite(foto_out, cv2.cvtColor((np.clip(res, 0, 1) * 255 + 0.5).astype(np.uint8), cv2.COLOR_RGB2BGR),
            [cv2.IMWRITE_JPEG_QUALITY, 95])

# ---------------- 3) La profundidad ----------------
DMIN, DMAX = 0.05, 60.0
ln0, ln1 = math.log(DMIN), math.log(DMAX)
datos = open(prof_in, "rb").read()
assert datos[:4] == b"JCP1"
w, h = struct.unpack("<HH", datos[4:8])
cod = np.frombuffer(datos[8:8 + w * h * 2], "<u2").reshape(h, w).astype(np.float64)
dist = np.exp(ln0 + cod / 65535.0 * (ln1 - ln0))
lnd = np.log(dist).astype(np.float32)
# Donde se quitaron cosas: se rellena con lo de alrededor.
quitado = cv2.imread(quitado_p, 0)
q = cv2.resize(quitado, (w, h), interpolation=cv2.INTER_AREA) > 0
q = cv2.dilate(q.astype(np.uint8), np.ones((3, 3), np.uint8))
lnd = cv2.inpaint(lnd, q, 3, cv2.INPAINT_TELEA)
dist = np.exp(lnd.astype(np.float64))
# El cielo nuevo, lejos.
cielo_p = cv2.resize(alfa, (w, h), interpolation=cv2.INTER_AREA) > 0.5
dist[cielo_p] = DMAX
# El piso: arriba del puente (cerca, debajo de ti) a 1 altura de cámara; la plaza y las calles, más abajo; las
# escaleras bajan suave entre los dos. Solo donde la distancia era la del piso (lo que se había aplanado).
vp = (np.arange(h) + 0.5) / h
latp = (0.5 - vp) * math.pi
abajo = np.maximum(-np.sin(latp), 1e-4)[:, None] * np.ones((1, w))
altura_vieja = dist * abajo                     # en alturas de cámara: 1 = el piso del puente
agua_p = cv2.resize(agua[..., 0], (w, h), interpolation=cv2.INTER_AREA) > 0.3
piso = (altura_vieja > 0.95) & (altura_vieja < 1.06) & (vp[:, None] > 0.505) & ~agua_p
k = np.clip((vp - 0.64) / (0.78 - 0.64), 0, 1)
k = k * k * (3 - 2 * k)
CALLE, PUENTE = 1.9, 1.0                        # la calle unos 1.4 m más abajo que el puente
altura = (CALLE + (PUENTE - CALLE) * k)[:, None] * np.ones((1, w))
nueva = np.where(piso, altura / abajo, dist)
nueva = np.clip(nueva, DMIN, DMAX)
# Suave (sin escalones de pixel), sin tocar el cielo.
suave = np.exp(cv2.GaussianBlur(np.log(nueva).astype(np.float32), (0, 0), 1.0))
nueva = np.where(cielo_p, nueva, suave)
print("piso bajado: %d pixeles; cielo: %d" % (int(piso.sum()), int(cielo_p.sum())))
c = np.round((np.log(nueva) - ln0) / (ln1 - ln0) * 65535).astype("<u2")
with open(prof_out, "wb") as f:
    f.write(b"JCP1"); f.write(struct.pack("<HH", w, h)); f.write(c.tobytes())

# Vista de control: la foto nueva y la profundidad antes / después.
chica = cv2.resize(cv2.imread(foto_out), (1024, 512), interpolation=cv2.INTER_AREA)
def gris_d(d):
    g = (1 - (np.log(d) - ln0) / (ln1 - ln0)) * 255
    return cv2.cvtColor(cv2.resize(g.astype(np.uint8), (512, 256)), cv2.COLOR_GRAY2BGR)
lienzo = np.full((768, 1024, 3), 255, np.uint8)
lienzo[:512] = chica
lienzo[512:, :512] = gris_d(np.exp(ln0 + cod / 65535.0 * (ln1 - ln0)))
lienzo[512:, 512:] = gris_d(nueva)
cv2.imwrite(vista_p, lienzo, [cv2.IMWRITE_JPEG_QUALITY, 88])
