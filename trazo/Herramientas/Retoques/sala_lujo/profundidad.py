# Sala de lujo (v38): arregla la profundidad (archivo .bytes, 512 x 256) después de los retoques de la foto.
#  1) Donde se quitó el SOFÁ CERCANO y la mesita oscura: el piso, plano (a 1 altura de cámara bajo tus ojos). Donde
#     LaMa rehízo el frente del sofá grande (en la costura), la distancia del sofá de los lados.
#  2) Donde se quitaron la planta, el plato y el tazón: lo que había alrededor (fila por fila).
#  3) La MESA DE CENTRO: la tapa es un plano horizontal y el frente un plano vertical (antes la esquina se veía rota).
#  4) Las ORILLAS: donde algo cercano está pegado a algo lejano, lo cercano se agranda 2 pixeles (16 en la foto).
#     Así lo que se estira entre los dos es el pedacito de fondo liso (cielo, piso) y no la orilla del objeto:
#     las rayas de las tablillas del techo ya no "se barren" hacia el cielo, y el sillón y las mesitas no tienen alas.
#  5) La CIUDAD de abajo: la herramienta de profundidad la había pegado al piso (por debajo del horizonte nada podía
#     estar más lejos que el piso), por eso se veía como después de un terremoto. Ahora va lejos, como el horizonte,
#     hasta un poquito dentro de la piscina (lo que se estira es el agua lisa, no los edificios).
#  6) El cielo, liso.
# Uso: python3 profundidad.py entrada.bytes salida.bytes vista.png mascara_sofa.png mascara_mesa.png
import sys, math, struct
import numpy as np, cv2
ruta_in, ruta_out, ruta_vista, ruta_sofa, ruta_mesa = sys.argv[1:6]
DMIN, DMAX = 0.05, 60.0
ln0, ln1 = math.log(DMIN), math.log(DMAX)
datos = open(ruta_in, "rb").read()
assert datos[:4] == b"JCP1"
W, H = struct.unpack("<HH", datos[4:8])
cod = np.frombuffer(datos[8:8 + W * H * 2], "<u2").reshape(H, W).astype(np.float64)
dist = np.exp(ln0 + cod / 65535.0 * (ln1 - ln0))
K = 4096 // W                                       # pixeles de la foto por pixel de profundidad (8)
filas = (np.arange(H) + 0.5) / H                    # 0 arriba, 1 abajo
baja = (filas - 0.5) * math.pi                      # cuánto se mira hacia abajo (radianes)

def chica(ruta):
    m = cv2.imread(ruta, 0).astype(np.float32) / 255.0
    return cv2.resize(m, (W, H), interpolation=cv2.INTER_AREA) > 0.3

def piso(f):
    s = math.sin(baja[f])
    return DMAX if s <= 1.0 / DMAX else min(DMAX, 1.0 / s)

def interpolar_filas(L, hueco):
    # En cada fila, lo marcado se llena con una línea recta (en logaritmo) entre lo que hay a los dos lados.
    for f in range(H):
        xs = np.where(hueco[f])[0]
        if len(xs) == 0:
            continue
        cortes = np.split(xs, np.where(np.diff(xs) > 1)[0] + 1)
        for c in cortes:
            a, b = c[0] - 1, c[-1] + 1
            if a < 0 and b >= W:
                continue
            va = L[f, a] if a >= 0 else L[f, b]
            vb = L[f, b] if b < W else L[f, a]
            t = (c - a) / float(b - a)
            L[f, c] = va * (1 - t) + vb * t

L = np.log(dist)
sofa = chica(ruta_sofa)
mesa = chica(ruta_mesa)

# 1) El sofá cercano (se trabaja corrido 160 pixeles para que la costura quede en medio).
R = 160
Lr = np.roll(L, R, axis=1)
sr = np.roll(sofa, R, axis=1)
frente = np.zeros_like(sr)
frente[1300 // K:1505 // K, (3836 - 4096) // K + R:260 // K + R] = True   # donde LaMa rehízo el frente del sofá
frente &= sr
interpolar_filas(Lr, frente)
for f in range(H):
    xs = np.where(sr[f] & ~frente[f])[0]
    if len(xs):
        Lr[f, xs] = math.log(piso(f))
L = np.roll(Lr, -R, axis=1)

# 2) Lo que había sobre la mesa de centro.
interpolar_filas(L, mesa)

# 3) La mesa de centro: tapa horizontal y frente vertical.
def poligono_chico(puntos):
    g = np.zeros((2048, 4096), np.uint8)
    cv2.fillPoly(g, [np.array(puntos, np.int32)], 255)
    return cv2.resize(g.astype(np.float32) / 255.0, (W, H), interpolation=cv2.INTER_AREA) > 0.5
tapa = poligono_chico([(1730, 1330), (2335, 1330), (2476, 1483), (1618, 1486)])
cara = poligono_chico([(1618, 1486), (2476, 1483), (2478, 1531), (1620, 1534)])
adentro = cv2.erode(tapa.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
adentro &= ~mesa
ff, cc = np.nonzero(adentro)
alto = float(np.median(dist[ff, cc] * np.sin(baja[ff])))     # (1 - altura de la mesa), en alturas de cámara
print("tapa de la mesa a", round(1 - alto, 3), "alturas de cámara del piso")
for f, c in zip(*np.nonzero(tapa)):
    L[f, c] = math.log(alto / math.sin(baja[f]))
borde_f = int(1485 / K)
for f, c in zip(*np.nonzero(cara)):
    d_borde = alto / math.sin(baja[borde_f])
    L[f, c] = math.log(d_borde * math.cos(baja[borde_f]) / math.cos(baja[f]))

# Muebles grandes: sin granitos.
for x0, y0, x1, y1 in [(2496, 1150, 2870, 1380), (3430, 1140, 4096, 1590), (0, 1130, 900, 1550)]:
    z = L[y0 // K:y1 // K, x0 // K:x1 // K].astype(np.float32)
    L[y0 // K:y1 // K, x0 // K:x1 // K] = cv2.medianBlur(z, 3)

# 4) Las orillas: lo cercano se agranda 2 pixeles, solo donde hay un salto grande.
Lf = L.astype(np.float32)
ext = np.concatenate([Lf[:, -2:], Lf, Lf[:, :2]], axis=1)    # la foto da la vuelta
minimo = cv2.erode(ext, np.ones((5, 5), np.uint8))[:, 2:-2]
salto = (Lf - minimo) > math.log(1.25)
L = np.where(salto, minimo, Lf).astype(np.float64)

# 5) La ciudad: lejos (como el horizonte de esa columna), de la fila 1040 a la 1143, sin las cosas de la terraza.
ciudad = np.zeros((H, W), bool)
ciudad[1040 // K:1128 // K, 1296 // K:2900 // K] = True
ciudad[1128 // K:1144 // K, 1630 // K:2490 // K] = True      # la orilla lejana de la piscina
for x0, y0, x1, y1 in [(1355, 1096, 1630, 1190), (2490, 1096, 2785, 1190), (2836, 1086, 2955, 1135)]:
    ciudad[y0 // K:(y1 + K - 1) // K, x0 // K:(x1 + K - 1) // K] = False
horizonte = L[1032 // K].copy()
for f in range(H):
    xs = np.where(ciudad[f])[0]
    if len(xs):
        L[f, xs] = np.maximum(L[f, xs], horizonte[xs])

# 6) El cielo y lo de afuera (lo lejano en el ventanal): liso.
lejos = np.zeros((H, W), bool)
lejos[:1150 // K, 1000 // K:3080 // K] = True
lejos &= L > math.log(12.0)
pesos = cv2.GaussianBlur(lejos.astype(np.float32), (0, 0), 6)
liso = cv2.GaussianBlur(np.where(lejos, L, 0).astype(np.float32), (0, 0), 6) / np.maximum(pesos, 1e-4)
L = np.where(lejos, np.maximum(liso, math.log(12.0)), L)

nueva = np.clip(np.exp(L), DMIN, DMAX)
c = np.round((np.log(nueva) - ln0) / (ln1 - ln0) * 65535).astype("<u2")
with open(ruta_out, "wb") as f:
    f.write(b"JCP1"); f.write(struct.pack("<HH", W, H)); f.write(c.tobytes())
def gris(d):
    return (255 * (1 - (np.log(d) - ln0) / (ln1 - ln0))).astype(np.uint8)
cv2.imwrite(ruta_vista, np.vstack([gris(dist), np.full((4, W), 255, np.uint8), gris(nueva)]))
print("listo")
