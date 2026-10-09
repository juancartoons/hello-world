# Respaldo: mover nodos con PELLIZCO (versión v26)

Aquí está guardado cómo funcionaban los nodos **antes** del cambio a "tocar con el índice"
(y las manos guía de antes, por si acaso). Estos archivos NO los usa la app: están fuera de `Assets`
y terminan en `.txt` para que Unity no los lea.

## Cómo funcionaba
- Mano izquierda: pulgar + medio = modo nodos.
- Mano derecha: **pellizcar** un nodo o un asa = agarrarlo; al **soltar el pellizco** se suelta.
- Pellizcar la línea elegida (lejos de sus nodos) = agregar un nodo ahí.
- Pellizcar otra línea = elegirla.

## Dónde está el código
- `ControlManos.cs.txt`: funciones `EditarNodos`, `AgregarNodoEn`, `EmpezarArrastre`, `ContinuarArrastre` y `TerminarArrastre`.
- `Tutorial.cs.txt`: el paso 7 (nodos) del tutorial con pellizco (`Demo7` y `Paso7`).
- `ManoVideo.cs.txt`: las manos guía de antes (esferas y tubos).

## Cómo volver a ponerlo
Pídele a Claude: *"vuelve a poner los nodos con pellizco del respaldo v26"*.
También está todo en el historial de GitHub, en el commit de la v26 (`ccacc0d`).
