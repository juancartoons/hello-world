# Sube una foto 360 de 2K a 4K con Real-ESRGAN (realesr-general-x4v3, licencia BSD-3), sin PyTorch:
# lee los pesos del .pth a mano, arma la red (SRVGGNetCompact) en ONNX y la corre con onnxruntime por pedazos.
# Uso: python3 nitidez.py pesos.pth entrada.jpg salida.jpg [ancho_final=4096]
import sys, zipfile, pickle, io
import numpy as np
from PIL import Image
import onnx
from onnx import helper, TensorProto, numpy_helper
import onnxruntime as ort

ruta_pesos, ruta_in, ruta_out = sys.argv[1:4]
ancho_final = int(sys.argv[4]) if len(sys.argv) > 4 else 4096


def cargar_pth(ruta):
    z = zipfile.ZipFile(ruta)
    raiz = z.namelist()[0].split('/')[0]
    datos = {}

    class Almacen:
        def __init__(self, dtype):
            self.dtype = dtype

    def rebuild_tensor_v2(storage, offset, size, stride, *args):
        tipo, clave = storage
        if clave not in datos:
            datos[clave] = np.frombuffer(z.read(f'{raiz}/data/{clave}'), dtype=tipo)
        arr = datos[clave]
        n = int(np.prod(size)) if len(size) else 1
        plano = arr[offset:offset + max(n, 1)] if len(size) else arr[offset:offset + 1]
        # contiguo (los pesos de Real-ESRGAN lo son)
        return np.array(plano).reshape(size) if len(size) else np.array(plano[0])

    class Lector(pickle.Unpickler):
        def find_class(self, mod, nombre):
            if mod == 'torch._utils' and nombre == '_rebuild_tensor_v2':
                return rebuild_tensor_v2
            if mod == 'torch' and nombre.endswith('Storage'):
                return {'FloatStorage': np.float32, 'HalfStorage': np.float16, 'LongStorage': np.int64}.get(nombre, np.float32)
            if mod == 'collections' and nombre == 'OrderedDict':
                import collections
                return collections.OrderedDict
            return super().find_class(mod, nombre)

        def persistent_load(self, pid):
            # ('storage', tipo, clave, lugar, cantidad)
            return (pid[1], pid[2])

    return Lector(io.BytesIO(z.read(f'{raiz}/data.pkl'))).load()


sd = cargar_pth(ruta_pesos)
if 'params' in sd:
    sd = sd['params']
elif 'params_ema' in sd:
    sd = sd['params_ema']
claves = sorted({k.split('.')[1] for k in sd if k.startswith('body.')}, key=int)

# ---- Red en ONNX: conv + PReLU (x N), conv final, PixelShuffle x4, + vecino más cercano x4 ----
nodos, inits = [], []
x = 'entrada'
i = 0
cont = 0
for c in claves:
    idx = int(c)
    w = sd.get(f'body.{idx}.weight')
    b = sd.get(f'body.{idx}.bias')
    if w is None:
        continue
    w = np.asarray(w, np.float32)
    if w.ndim == 4:
        nw, nb = f'w{idx}', f'b{idx}'
        inits += [numpy_helper.from_array(w, nw), numpy_helper.from_array(np.asarray(b, np.float32), nb)]
        y = f'c{idx}'
        nodos.append(helper.make_node('Conv', [x, nw, nb], [y], pads=[1, 1, 1, 1], kernel_shape=[3, 3]))
        x = y
    else:  # PReLU (un valor por canal)
        nw = f'p{idx}'
        inits.append(numpy_helper.from_array(w.reshape(-1, 1, 1), nw))
        y = f'r{idx}'
        nodos.append(helper.make_node('PRelu', [x, nw], [y]))
        x = y
nodos.append(helper.make_node('DepthToSpace', [x], ['ps'], blocksize=4, mode='CRD'))
inits.append(numpy_helper.from_array(np.array([1, 1, 4, 4], np.float32), 'escalas'))
nodos.append(helper.make_node('Resize', ['entrada', '', 'escalas'], ['base'], mode='nearest'))
nodos.append(helper.make_node('Add', ['ps', 'base'], ['salida']))
g = helper.make_graph(nodos, 'srvgg', [helper.make_tensor_value_info('entrada', TensorProto.FLOAT, [1, 3, None, None])],
                      [helper.make_tensor_value_info('salida', TensorProto.FLOAT, [1, 3, None, None])], inits)
m = helper.make_model(g, opset_imports=[helper.make_opsetid('', 13)])
m.ir_version = 8
opciones = ort.SessionOptions()
opciones.intra_op_num_threads = 4
s = ort.InferenceSession(m.SerializeToString(), opciones, providers=['CPUExecutionProvider'])

img = np.asarray(Image.open(ruta_in).convert('RGB'), np.float32) / 255.0
H, W = img.shape[:2]
T, M = 192, 12   # pedazo y margen (con la vuelta de la foto 360 en los lados)
salida = np.zeros((H * 4, W * 4, 3), np.float32)
for y0 in range(0, H, T):
    for x0 in range(0, W, T):
        y1, x1 = min(H, y0 + T), min(W, x0 + T)
        ya, yb = max(0, y0 - M), min(H, y1 + M)
        cols = np.arange(x0 - M, x1 + M) % W
        pedazo = img[ya:yb][:, cols]
        r = s.run(None, {'entrada': pedazo.transpose(2, 0, 1)[None]})[0][0].transpose(1, 2, 0)
        oy, ox = (y0 - ya) * 4, M * 4
        salida[y0 * 4:y1 * 4, x0 * 4:x1 * 4] = r[oy:oy + (y1 - y0) * 4, ox:ox + (x1 - x0) * 4]
    print('fila', y0, 'de', H, flush=True)
out = Image.fromarray((np.clip(salida, 0, 1) * 255 + 0.5).astype(np.uint8))
out = out.resize((ancho_final, ancho_final // 2), Image.LANCZOS)
out.save(ruta_out, quality=90)
print('listo', out.size)
