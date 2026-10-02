# TrazoVR ✏️

App de dibujo 3D con las manos para Meta Quest: fondo blanco (o tu cuarto real), línea negra tipo vector,
gruesa en el centro y en punta a los lados. Con capas y animación por morph.

## Gestos (todo con las manos)

| Gesto | Qué hace |
|---|---|
| Izquierda: **pulgar + índice** (sostener) | Dibujas con la punta del **índice derecho** |
| Izquierda: **pulgar + índice + medio juntos** (sostener) — o pulgar + meñique | **Línea recta** desde donde empiezas hasta tu dedo. Las puntas se pegan a otras líneas (polígonos que se cierran solos) |
| Izquierda: **pulgar + medio** (sostener) | **Modo nodos**: pellizca un nodo con la derecha y arrástralo. El nodo tocado muestra sus **asas** (verdes): pellízcalas para curvar. Pellizcar la **línea seleccionada** lejos de sus nodos = **agregar un nodo** |
| Modo nodos: arrastrar una **punta** sobre otra punta | Imán: se unen en una sola línea, o se **cierra la figura** y se rellena |
| Izquierda: **pulgar + anular** (sostener) | **Grosor**: sube/baja la mano izquierda = más grueso/delgado (solo la línea seleccionada, o todo si no hay selección). Pellizca un **nodo** con la derecha y súbela/bájala = grosor solo de ese nodo. **Gira el índice derecho en un círculo pequeño** (como un teléfono de disco) = grosor de las **líneas nuevas**: hacia la derecha más grueso, hacia la izquierda más delgado |
| Izquierda: **puño** (pulgar sobre los dedos o al lado) | **Borrador** (la mano se vuelve una goma de borrar; se borra con el índice derecho): tocar un **nodo** o un **relleno** lo borra. La **línea entera** solo se borra si la **frotas** (ida y vuelta, ~6 cm) lejos de sus nodos; se pone roja mientras |
| **Pellizcar una línea** con la derecha (sin gesto izquierdo) | La **selecciona** (se pone azul) y la **mueves**. Pellizcar en el aire = quitar la selección |
| Izquierda: **puño con el pulgar hacia tu izquierda** | La mano se vuelve una **flecha**: toca la **diana roja** con la punta = **Deshacer** (retírala y vuelve a tocar para deshacer otra vez) |
| Izquierda: **puño con el pulgar hacia tu derecha** | Flecha hacia la derecha: toca la **diana verde** = **Rehacer** |
| Izquierda: **doble toque rápido** de pulgar + índice | **Bloquear / desbloquear** el dibujo (no se dibuja por accidente). Se ve un candado arriba a la derecha |
| Izquierda abierta, **punta del pulgar en la base de los dedos** (sobre la palma) | **Menú**: Libre 3D / Plano 2D, Fondo, Guardar, Cargar, Borrar todo |
| **Las dos manos pellizcando** (índice + pulgar) | Separar/juntar = escalar · girar como un volante = rotar · mover = trasladar (solo la línea seleccionada, o todo si no hay selección) |
| Tocar un **relleno** con el índice derecho | Cambia su color |
| **Mirar hacia arriba** | Panel de arriba: línea de tiempo (2000 fotogramas), capas, menú, exportar, videos, imágenes y bocas |
| Pellizcar el **asa azul** del panel de arriba | Moverlo a donde quieras (queda fijo). Pellizcando también con la izquierda: separar/juntar = **tamaño** |
| Pellizcar una **imagen** con la derecha | Moverla (queda seleccionada). Con las dos manos: escalarla y girarla |

- Mientras aprendes, sobre la mano aparece el **nombre del gesto** que estás haciendo.
- Las líneas nuevas salen con el **grosor promedio** de las que ya hay.
- En **Plano (2D)** el dedo tiene que estar sobre el plano: si lo alejas más de ~2.5 cm, la línea se corta
  (como levantar el lápiz). El gesto izquierdo sigue activo: al acercar el dedo empieza otra línea.

## Animación (morph)

1. Mira hacia arriba y toca la **barra** para ir a otro fotograma (por ejemplo, el 40).
   El botón **Zoom** cambia cuántos fotogramas caben en la barra: Todo, 400, 100 o 25.
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
- **Video anim** (página Medios): la animación en MP4 (1280x720), con el audio de las bocas si hay.
- **Grabar** (página Medios) graba tu proceso: tus manos y cómo aparecen las líneas. **Detener** para.
  **Video proceso** lo convierte en MP4 (líneas + manos en gris). **Vel** = x1, x2, x4 u x8.
- Si el MP4 falla, se guarda una carpeta con imágenes PNG y un `hacer_video.bat` (necesita ffmpeg en el PC).
- Ni los paneles, ni los nodos, ni las imágenes de referencia salen en fotos ni videos.
- **Temblor** (página Medios): las líneas vibran como dibujadas a mano (No, Suave, Medio, Fuerte). Sale en los videos y se guarda con el dibujo.
- Se guardan en las gafas, en `Android/data/<tu app>/files/Dibujos` (con Meta Quest Developer Hub → File Manager).

## Títere con los dedos (caminar)

1. Panel de arriba → página **Títere** → **Muñeco prueba**: aparece un muñeco listo frente a ti.
2. **Títere**: pon el índice y el medio derechos "parados", como dos piernas, 2 segundos.
3. Camina con los dedos (como sobre una mesa): el nudillo es la cadera, el medio del dedo la rodilla, la punta el pie.
   El cuerpo sigue a la cadera. **Pellizco izquierdo** = apagar.
4. **Grabar**: cuenta 3 segundos y guarda una clave por fotograma. **Pellizco izquierdo** = parar. Luego **Play**.
5. Con tus dibujos: pellizca una línea y toca **Pierna 1**, **Pierna 2** o **Cuerpo +/-**. **Voltear** = rodillas al otro lado.

## Imágenes de referencia

1. Copia imágenes (png o jpg) a `Android/data/<tu app>/files/Dibujos/Imagenes`.
2. Página **Medios** → **Imagen +**: aparece la siguiente imagen frente a ti.
3. Pellízcala con la derecha para moverla; con las dos manos, escálala y gírala. Se queda donde la dejes.
4. **Imagen -** quita la imagen seleccionada (la que tiene tono azul).
5. **Para calcar**: en modo Plano (2D), suelta la imagen cerca del plano y **se pega detrás** como imán.
   Si mueves, giras o escalas el dibujo, la imagen lo sigue. Botón **Despegar** arriba a la derecha de la imagen.

## Bocas automáticas (lipsync)

1. Dibuja la boca en **su propia capa** (por ejemplo, Capa 2) y deja esa capa elegida.
2. Página **Bocas**, **Modo: Guardar**: mueve los nodos de la boca y toca **Reposo, A, E, I, O, U o M**
   para guardar cada forma (los botones guardados se ven oscuros).
3. **Voz** graba tu voz (otra vez Voz = parar), o **Audio** elige un audio de `Dibujos/Audio` (wav, mp3).
4. **Lipsync**: crea las claves de la boca según el audio. **Play** reproduce con sonido.
5. **Modo: Probar**: tocar una boca la pone en el fotograma actual (para corregir a mano).

## Capas

- Toca **Capa 1-4** para elegir en cuál dibujas; **Ver/Oculta** la muestra o la esconde.

## Archivos

- `Assets/Scripts/` — scripts (el menú está en `Editor/ArmarEscenaTrazo.cs`).
- `Assets/Shaders/` — la línea tipo vector y el relleno.

## Ideas para después

- Títeres: parpadeo automático, ojos y cejas, poses con gestos de la mano, cuerpo con los dedos.
