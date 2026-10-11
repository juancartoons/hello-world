# Retoques de fondos 360

Cada carpeta guarda el trabajo **aparte**: los fondos que usa la app (`Assets/Plugins/Fondos`) no se tocan hasta
que Juan Carlos dé el visto bueno. Todo con herramientas gratis y abiertas (OpenCV Model Zoo: LaMa y YOLOX,
Real-ESRGAN y Depth Anything V2), sin internet una vez descargados los modelos.

| Carpeta | Qué se hizo | Archivo listo |
|---|---|---|
| `shanghai_noche/` | 1) sin gente · 2) sin neblina, con contraste | `paso2_sin_neblina.jpg` (faltan nitidez, 8K opcional y profundidad) |
| `atardecer_mar/` | Sin la baranda cercana (la lejana de la derecha se queda: ahí empieza) + profundidad nueva | `atardecer_mar360_sin_baranda.jpg` y su `_profundidad.bytes` |
| `sala_lujo/` | Sala de lujo (hecha con ChatGPT): costura unida, 4K con Real-ESRGAN y profundidad | `sala_lujo360.jpg` y su `_profundidad.bytes` |

Herramientas: `Herramientas/nitidez360.py` (4K) y `Herramientas/profundidad360.py` (profundidad).
