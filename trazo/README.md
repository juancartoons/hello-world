# TrazoVR ✏️

App de dibujo 3D con las manos para Meta Quest: fondo blanco, línea negra tipo vector,
gruesa en el centro y en punta a los lados.

## Gestos (todo con las manos)

| Gesto | Qué hace |
|---|---|
| Izquierda: **pulgar + índice** (sostener) | Dibujas con la punta del **índice derecho** |
| Izquierda: **pulgar + medio** (sostener) | Aparecen los **nodos** (rombos azules). Pellizca uno con la derecha y arrástralo. Al soltar, ves otra vez el dibujo terminado |
| Izquierda: **pulgar + anular** (sostener) | **Grosor**: sube la mano izquierda = más grueso, bájala = más delgado (todo proporcional) |
| **Rombo con las dos manos** (índices juntos y pulgares juntos, ~½ segundo) | Activa/desactiva la **caja** con 8 esquinas. Pellizca una esquina y aléjala/acércala = escalar. Pellizca dentro de la caja = mover |
| **Palma izquierda mirándote** | Aparece el **panel**: Deshacer, Borrar, Guardar, Cargar, Cinta, Tubo, Por línea, Fondo |

- **Cinta / Tubo**: con "Por línea: No" cambian todo el dibujo; con "Por línea: Sí" solo las líneas nuevas.
- La esfera gris en tu índice derecho muestra el grosor actual del pincel.
- El dibujo se guarda solo al salir y se recupera al abrir la app.

## Archivos

- `Assets/Scripts/` — scripts (el menú está en `Editor/ArmarEscenaTrazo.cs`).
- `Assets/Shaders/TrazoLinea.shader` — la línea tipo vector.

## Ideas para después

- Importar imágenes de referencia.
- Animar las líneas por "morph" (de un dibujo a otro).
