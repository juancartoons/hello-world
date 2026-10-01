# TrazoVR ✏️

App de dibujo 3D con las manos para Meta Quest: fondo blanco (o tu cuarto real), línea negra tipo vector,
gruesa en el centro y en punta a los lados. Con capas y animación por morph.

## Gestos (todo con las manos)

| Gesto | Qué hace |
|---|---|
| Izquierda: **pulgar + índice** (sostener) | Dibujas con la punta del **índice derecho** |
| Izquierda: **pulgar + medio** (sostener) | **Modo nodos**: pellizca un nodo con la derecha y arrástralo. El nodo tocado muestra sus **asas** (verdes): pellízcalas para curvar |
| Modo nodos: arrastrar una **punta** sobre otra punta | Imán: se unen en una sola línea, o se **cierra la figura** y se rellena |
| Izquierda: **pulgar + anular** (sostener) | **Grosor**: sube/baja la mano izquierda = todo más grueso/delgado. Pellizca un **nodo** con la derecha y súbela/bájala = grosor solo de ese nodo |
| Izquierda: **puño** (pulgar escondido) | **Borrador**: lo que toques con el índice derecho se borra (nodo, línea o relleno) |
| Izquierda: **puño con el pulgar hacia tu izquierda** | **Deshacer** (la mano destella en azul) |
| Izquierda abierta, **punta del pulgar en la base de los dedos** | **Menú**: Libre 3D / Plano 2D, Fondo, Guardar, Cargar, Borrar todo |
| **Las dos manos pellizcando** (índice + pulgar) | Separar/juntar = escalar · girar como un volante = rotar · mover = trasladar |
| Tocar un **relleno** con el índice derecho | Cambia su color |
| **Mirar hacia arriba** | Línea de tiempo (200 fotogramas) y capas |

## Animación (morph)

1. Mira hacia arriba y toca la **barra** para ir a otro fotograma (por ejemplo, el 40).
2. Mueve los nodos (pulgar + medio). Se guarda una **clave** sola en ese fotograma.
3. **Play**: las líneas se transforman suavemente entre las claves.

- "+ Clave" copia la forma actual en el fotograma; "- Clave" la quita.
- Una línea dibujada en el fotograma 40 aparece desde ahí.

## Capas

- Toca **Capa 1-4** para elegir en cuál dibujas; **Ver/Oculta** la muestra o la esconde.

## Archivos

- `Assets/Scripts/` — scripts (el menú está en `Editor/ArmarEscenaTrazo.cs`).
- `Assets/Shaders/` — la línea tipo vector y el relleno.

## Ideas para después

- Importar imágenes de referencia.
