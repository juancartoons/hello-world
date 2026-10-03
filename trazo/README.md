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
| Izquierda: **puño de lado o con el dorso hacia ti** (pulgar sobre los dedos; con la palma hacia ti no cuenta) | **Borrador** (la mano se vuelve una goma de borrar; se borra con el índice derecho): tocar un **nodo** o un **relleno** lo borra. La **línea entera** solo se borra si la **frotas** (ida y vuelta, ~6 cm) lejos de sus nodos; se pone roja mientras |
| **Pellizcar una línea** con la derecha (sin gesto izquierdo) | La **selecciona** (se pone azul) y la **mueves**. Se suelta con solo abrir un poco los dedos. Pellizcar en el aire = quitar la selección |
| Con algo elegido: **toque corto** (pellizco rápido sin mover) sobre otra línea | **Selección múltiple**: la suma (o la quita). Arrastrar cualquiera de las azules **mueve todas**; con las dos manos cerca de ellas, se escalan y giran juntas |
| **Las dos manos pellizcando lejos** de lo elegido | Se transforma **todo el dibujo** (aunque haya una línea, figura o imagen elegida) |
| Izquierda: **puño con el pulgar hacia tu izquierda** (sostener 0.3 s, quieta) | La mano se vuelve una **flecha** (la punta en tu pulgar): toca la **diana roja** = **Deshacer** (retírala y vuelve a tocar para deshacer otra vez) |
| **Derecha**: **puño con el pulgar hacia tu derecha** | La mano derecha se vuelve una flecha: toca la **diana verde** = **Rehacer** |
| Izquierda: **doble toque rápido** de pulgar + índice | **Candado (modo seguro)**: no se dibuja, borra, cambia grosor, editan nodos ni mueven líneas o figuras. Sí puedes girar/mover todo para mirar, usar los paneles, deshacer y rehacer. Se ve un candado arriba a la derecha |
| Menú de la mano: **Esfera / Cubo / Cilindro** | **Figura 3D** que se ve como dibujo (relleno + contorno desde donde la mires). Pellízcala para moverla; con las dos manos: tamaño y giro. Gesto de grosor = suavizar esquinas. Modo nodos = deformarla (pellizca la superficie = nodo nuevo). **A líneas** = convertirla en líneas normales |
| Izquierda abierta, **punta del pulgar en la base de los dedos** (sobre la palma) | **Menú**: Libre 3D / Plano 2D, Fondo, Guardar, Cargar, Borrar todo |
| **Las dos manos pellizcando** (índice + pulgar) | Separar/juntar = escalar · girar como un volante = rotar · mover = trasladar (solo la línea seleccionada, o todo si no hay selección) |
| Tocar un **relleno** con el índice derecho | Cambia su color |
| **Mirar hacia arriba** | Panel de arriba: línea de tiempo (2000 fotogramas), capas, menú, exportar, videos, imágenes y bocas |
| Panel de arriba: botón **?** (esquina) | **Ayuda**: abajo aparece una copia **azul** del panel. Toca cualquier botón de la copia y te explica para qué sirve y cómo se usa (no cambia nada). **?** otra vez = cerrar |
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
- **Líneas vivas** (página Medios, **por capa**: cambian solo la capa activa): **Temblor** (No, Suave, Medio, Fuerte),
  **Hebras** (1, 3 o 5; en las puntas se juntan en una sola), **Grosor vivo**, **Ciclo de 3** o **Libre**,
  **Suavidad** (Suave, Normal, Nervioso) y **Velocidad** (4, 8, 12 o 24 cambios por segundo).
  **Grosor de las hebras**: gesto de grosor (pulgar + anular) y pellizco derecho en el aire, sube/baja.
- **Boceto** (página Medios): la capa activa se ve como lápiz **gris** o **azul** y **no sale** en fotos ni videos.
- **Imán: Sí / No** (página Medios, por capa): con el imán apagado las líneas quedan como las dibujas (no se cierran
  ni se unen). Al poner una capa en boceto, el imán se apaga solo.
- Al borrar nodos suena una burbujita.
- **Plano: propio / unido** (página Medios): cada capa tiene su propio plano 2D. Las capas "unidas" comparten el plano,
  cada una 2 mm más cerca de ti (boceto atrás, tinta adelante).
- Se guardan en las gafas, en `Android/data/<tu app>/files/Dibujos` (con Meta Quest Developer Hub → File Manager).

## Personajes que caminan, corren y saltan (hasta dos a la vez)

1. Panel de arriba → página **Títere** → **Tipo** (Palito, Musculoso, Gordito, Flaco, Niño) → **Crear**.
   Los personajes van en la **Capa 4**: su **Ver/Oculta** los esconde. Acerca la mano derecha a un personaje apagado
   y aparece su **marco con una X**: tócala para borrarlo (se puede deshacer).
   Aparece frente a ti sobre un piso y **se enciende solo** con tu mano derecha (ponla frente a ti un segundo).
   Los personajes dibujados tienen huesos invisibles y sus partes pegadas a ellos.
2. **Encender** otro: "choca esos cinco" con el personaje (mano abierta, palma hacia él, empujón rápido cerca).
   Mientras tu mano abierta va hacia un personaje, no se edita nada (no borra nodos ni cambia rellenos sin querer).
   Con la mano **derecha** lo controlas con la derecha; con la **izquierda**, otro personaje con la izquierda.
   **Apagar**: choca esos cinco con la **otra mano** (la libre). Si las dos manos tienen personaje: **palma hacia
   arriba** medio segundo (el muñeco se queda quieto y el aro se llena). O el botón **Parar** (donde dice Crear).
   Mientras un personaje se mueve, el dibujo se bloquea solo (candado) y vuelve como estaba al apagarlo.
3. Mano a los lados = **caminar** (lento) o **correr** (rápido; pasa por caminar antes de correr). Hacia atrás, se voltea.
   Al frenar **después de correr**, la cabeza y el tronco siguen un poquito (inercia).
4. Mano **abajo** (más de 4 cm) = **agacharse**, suave. Subir la mano no lo eleva (así no flota).
5. **Saltar**: **golpe rápido de la mano hacia arriba**. Se agacha solo un instante, despega, se recoge arriba,
   cae, se aplasta al caer y rebota.
6. **Piso +/-**: la línea elegida es piso o plataforma (sube rampas, cae si se acaba). Cada personaje solo pisa
   los pisos que están a su misma profundidad (en Plano 2D, todos los del plano).
7. **Grabar**: cuenta 3 segundos y guarda una clave por fotograma de todos los personajes encendidos.
   "Choca esos cinco" con tu personaje (o **Parar**) = terminar. Luego corrige en la línea de tiempo y exporta con Video anim.
8. **Ciclo**: Normal, **Con estilo** (Richard Williams: paso alto, brazos grandes, mano arrastrada, codo quebrado),
   **Sigiloso** (Ken Harris: agachado, inclinado, el pie pasa rápido por el medio y se apoya con cuidado)
   o tus ciclos (**Guardar ciclo** toma tus claves).
9. **Posar dedos**: el índice y el medio derechos acomodan las piernas; **pellizco izquierdo** = guardar una clave.
10. Con tus dibujos: pellizca una línea y toca **Pierna 1/2**, **Brazo 1/2** o **Cuerpo +/-**. **Voltear** = el otro lado.

## Guardar y abrir

- **Guardar**: cada dibujo se guarda con su propio nombre (Dibujo 1, Dibujo 2...) en `Dibujos/Archivos`.
- **Cargar**: abajo aparece la lista de tus dibujos (el más reciente primero, con fecha). Toca uno para abrirlo.
- **Borrar todo** empieza un dibujo nuevo: al guardarlo recibe otro nombre.

## Imágenes de referencia

1. Copia imágenes (png o jpg) a `Android/data/<tu app>/files/Dibujos/Imagenes`.
2. Página **Medios** → **Imagen +**: aparece la siguiente imagen frente a ti.
3. Pellízcala con la derecha para moverla; con las dos manos, escálala y gírala. Se queda donde la dejes.
4. **Imagen -** quita la imagen seleccionada (la que tiene tono azul), o toca la **X** de su esquina.
5. **Para calcar**: en modo Plano (2D), suelta la imagen cerca del plano y **se pega detrás** como imán.
   Si mueves, giras o escalas el dibujo, la imagen lo sigue. Arriba a la derecha de la imagen: **Despegar** y
   **Ver** (100%, 50%, 20%, oculta). En Medios, **Imágenes: ver / ocultas** las esconde todas a la vez.

## Bocas automáticas (lipsync)

1. Dibuja la boca en **su propia capa** (por ejemplo, Capa 2) y deja esa capa elegida.
2. Página **Bocas**, **Modo: Guardar**: mueve los nodos de la boca y toca **Reposo, A, E, I, O, U o M**
   para guardar cada forma (los botones guardados se ven oscuros).
3. **Voz** graba tu voz (otra vez Voz = parar), o **Audio** elige un audio de `Dibujos/Audio` (wav, mp3).
4. **Lipsync**: crea las claves de la boca según el audio. **Play** reproduce con sonido.
5. **Modo: Probar**: tocar una boca la pone en el fotograma actual (para corregir a mano).

## Capas

- Toca **Capa 1-4** para elegir en cuál dibujas; **Ver/Oculta** la muestra o la esconde.
- Las capas en Plano están **unidas** de fábrica: medio milímetro una de otra (como acetatos sobre papel).
  **Plano** (Medios) une o separa la capa; al unirla, sus líneas se pegan al plano.
- **Lápiz de boceto**: en una capa de **Boceto** (gris o azul) con **Plano 2D**, dibujar pinta con lápiz sobre una
  hoja de **3 m x 3 m** (con grano; más cerca del plano = más oscuro). Su **borde** siempre se ve (línea delgada) y
  avisa si te sales. El puño-borrador es una goma. No sale en fotos ni videos.

## Archivos

- `Assets/Scripts/` — scripts (el menú está en `Editor/ArmarEscenaTrazo.cs`).
- `Assets/Shaders/` — la línea tipo vector y el relleno.

## Ideas para después

- Títeres: parpadeo automático, ojos y cejas, poses con gestos de la mano, cuerpo con los dedos.
