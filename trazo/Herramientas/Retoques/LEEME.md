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

**Ya montado en la app (v38):**
- `venecia_canal/`: cielo de atardecer, sin barandal torcido ni chimeneas, escaleras derechas (`atardecer.py`, `mascara_atardecer.py`). Vista: `vista_atardecer.jpg` y `antes_despues_atardecer.jpg`.
- `nueva_york/`: sin la reja de vidrio ni los arbolitos, más nítido, la ciudad de abajo lejos (`mascara.py`, `profundidad.py`). Vista: `antes_despues.jpg`.
- `sala_lujo/`: sin el sofá cercano ni la mesita oscura, la mesa de centro con solo los libros, profundidad arreglada (`mascara.py` → LaMa → `ajustar_color.py` → `mesa.py`, y `profundidad.py`). Vista: `antes_despues_v38.jpg` y `vista_profundidad_v38.png`.
