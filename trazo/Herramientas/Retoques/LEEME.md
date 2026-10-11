# Retoques de fondos 360

Cada carpeta guarda el trabajo **aparte**: los fondos que usa la app (`Assets/Plugins/Fondos`) no se tocan hasta
que Juan Carlos dé el visto bueno. Todo con herramientas gratis y abiertas (OpenCV Model Zoo: LaMa y YOLOX,
Real-ESRGAN y Depth Anything V2), sin internet una vez descargados los modelos.

| Carpeta | Qué se hizo | Archivo listo |
|---|---|---|
| `shanghai_noche/` | 1) sin gente · 2) sin neblina · 3) nitidez · 4) 8K aparte · profundidad nueva | `paso3_nitidez.jpg` y `paso3_profundidad.bytes` |
| `atardecer_mar/` | Sin la baranda cercana (la lejana de la derecha se queda: ahí empieza) + profundidad nueva | `atardecer_mar360_sin_baranda.jpg` y su `_profundidad.bytes` |
| `venecia_canal/` | Sin la reja de hierro deformada del puente (los dos postes de piedra se quedan) + profundidad nueva | `venecia_canal360_sin_baranda.jpg` y su `_profundidad.bytes` |
| `sala_lujo/` | Sala de lujo (hecha con ChatGPT): costura unida, 4K con Real-ESRGAN y profundidad | `sala_lujo360.jpg` y su `_profundidad.bytes` |

Herramientas: `Herramientas/nitidez360.py` (4K) y `Herramientas/profundidad360.py` (profundidad).

**Ya montado en la app (v36):** `nueva_york360` ("Azotea en Nueva York", hecha con ChatGPT, 4K + profundidad)
reemplaza a `grafitis360` ("Refugio con grafitis"). Vista previa: `nueva_york_vista.jpg`.
