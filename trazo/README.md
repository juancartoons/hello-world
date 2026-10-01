# TrazoVR ✏️

App de dibujo 3D con las manos para Meta Quest: fondo blanco (o tu cuarto real), línea negra tipo vector,
gruesa en el centro y en punta a los lados.

## Gestos (todo con las manos)

| Gesto | Qué hace |
|---|---|
| Izquierda: **pulgar + índice** (sostener) | Dibujas con la punta del **índice derecho** |
| Izquierda: **pulgar + medio** (sostener) | **Modo nodos**: pellizca un nodo con la derecha y arrástralo. El nodo tocado muestra sus **asas** (verdes): pellízcalas para curvar |
| Modo nodos: **doble pellizco** | Sobre un nodo = borrar nodo · sobre la línea = borrar línea · sobre el relleno = quitar color · sobre un asa = asa automática |
| Modo nodos: arrastrar una **punta** sobre otra punta | Imán: se unen en una sola línea, o se **cierra la figura** y se rellena |
| Izquierda: **pulgar + anular** (sostener) | **Grosor**: sube la mano = más grueso, bájala = más delgado (todo proporcional) |
| **Las dos manos pellizcando** (índice + pulgar) | Separar/juntar = escalar · girar como un volante (en cualquier dirección) = rotar · mover = trasladar |
| Izquierda: **puño con el pulgar hacia tu izquierda** | **Deshacer** |
| Tocar un **relleno** con el índice derecho | Cambia su color |
| **Palma izquierda mirándote** | Panel: Línea (Cinta/Tubo), Por línea, Libre 3D / Plano 2D, Fondo, Guardar, Cargar, Borrar todo |

- Si terminas una línea cerca de su inicio, se cierra sola; cerca de la punta de otra línea, se une a ella.
- **Plano (2D)**: tu siguiente línea crea un plano frente a ti; todo lo que dibujes queda sobre él.
- **Fondo**: Cuadrícula → Blanco → Realidad (passthrough).
- El dibujo se guarda solo al salir y se recupera al abrir la app.

## Archivos

- `Assets/Scripts/` — scripts (el menú está en `Editor/ArmarEscenaTrazo.cs`).
- `Assets/Shaders/` — la línea tipo vector y el relleno.

## Ideas para después

- Importar imágenes de referencia.
- Animar las líneas por "morph" (de un dibujo a otro).
