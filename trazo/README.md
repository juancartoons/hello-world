# TrazoVR ✏️

App de dibujo 3D con las manos para Meta Quest: fondo blanco (o tu cuarto real), línea negra tipo vector,
gruesa en el centro y en punta a los lados. Con capas y animación por morph.

## Gestos (todo con las manos)

| Gesto | Qué hace |
|---|---|
| Izquierda: **pulgar + índice** (sostener) | Dibujas con la punta del **índice derecho** |
| Izquierda: **pulgar + meñique** (sostener) | **Línea recta** desde donde empiezas hasta tu dedo. Las puntas se pegan a otras líneas (polígonos que se cierran solos) |
| Izquierda: **pulgar + medio** (sostener) | **Modo nodos**: pellizca un nodo con la derecha y arrástralo. El nodo tocado muestra sus **asas** (verdes): pellízcalas para curvar. Pellizcar la **línea seleccionada** lejos de sus nodos = **agregar un nodo** |
| Modo nodos: arrastrar una **punta** sobre otra punta | Imán: se unen en una sola línea, o se **cierra la figura** y se rellena |
| Izquierda: **pulgar + anular** (sostener) | **Grosor**: sube/baja la mano izquierda = más grueso/delgado (solo la línea seleccionada, o todo si no hay selección). Pellizca un **nodo** con la derecha y súbela/bájala = grosor solo de ese nodo |
| Izquierda: **puño** (pulgar sobre los dedos o al lado) | **Borrador** (la mano se pone roja): tocar un **nodo** o un **relleno** lo borra. La **línea entera** solo se borra si la **frotas** (ida y vuelta, ~6 cm) lejos de sus nodos; se pone roja mientras |
| **Pellizcar una línea** con la derecha (sin gesto izquierdo) | La **selecciona** (se pone azul) y la **mueves**. Pellizcar en el aire = quitar la selección |
| Izquierda: **puño con el pulgar hacia tu izquierda** | **Deshacer** (la mano destella en azul) |
| Izquierda abierta, **punta del pulgar en la base de los dedos** (sobre la palma) | **Menú**: Libre 3D / Plano 2D, Fondo, Guardar, Cargar, Borrar todo |
| **Las dos manos pellizcando** (índice + pulgar) | Separar/juntar = escalar · girar como un volante = rotar · mover = trasladar (solo la línea seleccionada, o todo si no hay selección) |
| Tocar un **relleno** con el índice derecho | Cambia su color |
| **Mirar hacia arriba** | Línea de tiempo (200 fotogramas), capas, copia del menú y **exportar** (SVG y Foto) |

- Mientras aprendes, sobre la mano aparece el **nombre del gesto** que estás haciendo.
- Las líneas nuevas salen con el **grosor promedio** de las que ya hay.

## Animación (morph)

1. Mira hacia arriba y toca la **barra** para ir a otro fotograma (por ejemplo, el 40).
2. Mueve los nodos (pulgar + medio). Se guarda una **clave** sola en ese fotograma.
3. **Play**: las líneas se transforman suavemente entre las claves.

- "+ Clave" copia la forma actual en el fotograma; "- Clave" la quita.
- **Pellizca una clave** (marca naranja) y arrástrala para moverla a otro fotograma.
- El botón **fps** cambia la velocidad: 12, 24, 30 o 60 cuadros por segundo.
- Una línea dibujada en el fotograma 40 aparece desde ahí.
- Si **borras o agregas un nodo**, pasa en **todas** las claves (así el morph sigue funcionando).

## Exportar

- **SVG**: cada línea es una curva con los mismos nodos que en la app. En Plano se ve de frente al plano; en 3D, desde donde estás.
- **Foto**: imagen PNG del dibujo desde donde estás.
- Se guardan en las gafas, en `Android/data/<tu app>/files/Dibujos` (con Meta Quest Developer Hub → File Manager).

## Capas

- Toca **Capa 1-4** para elegir en cuál dibujas; **Ver/Oculta** la muestra o la esconde.

## Archivos

- `Assets/Scripts/` — scripts (el menú está en `Editor/ArmarEscenaTrazo.cs`).
- `Assets/Shaders/` — la línea tipo vector y el relleno.

## Ideas para después

- Importar imágenes de referencia.
