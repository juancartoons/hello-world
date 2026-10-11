# Retoque del fondo 360 "Shanghai de noche"

El fondo que usa la app **no se ha tocado** (`Assets/Plugins/Fondos/shanghai_noche360.jpg`).
Aquí se guarda cada paso aparte, para revisarlo antes de montarlo.

| Paso | Archivo | Estado |
|---|---|---|
| 1. Quitar la gente | `paso1_sin_gente.jpg` (4096 × 2048) | Hecho |
| 2. Quitar la neblina y dar contraste | `paso2_sin_neblina.jpg` | Hecho |
| 3. Nitidez (Real-ESRGAN) | `paso3_nitidez.jpg` (4K, el que iría a la app) | Hecho, falta tu visto bueno |
| 4. Versión 8K (opcional, para probar en el visor) | `paso4_8k.jpg` (8192 × 4096) | Guardada; la app hoy carga hasta 4K |
| 5. Profundidad 3D nueva | `paso3_profundidad.bytes` (sin gente) | Hecho |

## Cómo se hizo el paso 1 (todo gratis y abierto)
1. `detectar.py`: busca personas con **YOLOX** (licencia Apache-2.0) en pedazos de la foto → `cajas.json` (124 personas).
2. `mascara.py`: agranda cada caja (más hacia abajo, por los pies y su reflejo en el piso mojado), une las que están casi pegadas y suma las zonas marcadas a mano en `extras.json` (gente "fantasma" movida).
3. `rellenar.py`: rellena lo marcado con **LaMa** (licencia Apache-2.0), en ventanas de 512 × 512 a tamaño real que se traslapan, para que cada pedazo continúe al anterior sin costuras.

Modelos (del OpenCV Model Zoo): `inpainting_lama_2025jan.onnx` y `object_detection_yolox_2022nov.onnx`.

## Cómo se hizo el paso 2
`neblina.py paso1_sin_gente.jpg paso2_sin_neblina.jpg 0.6 0.5`: quita el velo lechoso (el "canal oscuro" suavizado
siguiendo los bordes, fuerza 0.6), luego contraste local en la luz (CLAHE, mezcla 0.5) y un 8 % más de color.
Todos los filtros dan la vuelta (360), así que la costura sigue invisible.

## Cómo se hizo el paso 3 (y el 4)
1. `Herramientas/nitidez360.py pesos.pth paso2_sin_neblina.jpg paso3_8k.jpg 8192`: Real-ESRGAN x4 sobre el 4K y bajado a 8K.
2. `detalle.py paso2_sin_neblina.jpg paso3_8k.jpg paso4_8k.jpg 3`: colores y luz del paso 2 + solo el detalle fino de la
   IA (así no se queman los blancos de los edificios).
3. `paso3_nitidez.jpg` = `paso4_8k.jpg` bajada a 4096 × 2048 (más limpia que el paso 2, sin ruido en el cielo).
