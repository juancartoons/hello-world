# Retoque del fondo 360 "Shanghai de noche"

El fondo que usa la app **no se ha tocado** (`Assets/Plugins/Fondos/shanghai_noche360.jpg`).
Aquí se guarda cada paso aparte, para revisarlo antes de montarlo.

| Paso | Archivo | Estado |
|---|---|---|
| 1. Quitar la gente | `paso1_sin_gente.jpg` (4096 × 2048) | Hecho, falta tu visto bueno |
| 2. Quitar la neblina y dar contraste | — | Pendiente |
| 3. Nitidez (Real-ESRGAN) | — | Pendiente |
| 4. Versión 8K (opcional, para probar en el visor) | — | Pendiente |
| 5. Profundidad 3D nueva | — | Pendiente |

## Cómo se hizo el paso 1 (todo gratis y abierto)
1. `detectar.py`: busca personas con **YOLOX** (licencia Apache-2.0) en pedazos de la foto → `cajas.json` (124 personas).
2. `mascara.py`: agranda cada caja (más hacia abajo, por los pies y su reflejo en el piso mojado), une las que están casi pegadas y suma las zonas marcadas a mano en `extras.json` (gente "fantasma" movida).
3. `rellenar.py`: rellena lo marcado con **LaMa** (licencia Apache-2.0), en ventanas de 512 × 512 a tamaño real que se traslapan, para que cada pedazo continúe al anterior sin costuras.

Modelos (del OpenCV Model Zoo): `inpainting_lama_2025jan.onnx` y `object_detection_yolox_2022nov.onnx`.
