# JCartoons — Pendientes y recomendaciones

*Actualizado: 16 de octubre de 2026 · Versión actual en la rama `claude/trazo-vr`: **v46***

Este documento reúne todo lo que falta por hacer y las recomendaciones para el futuro. Las casillas `[ ]` se marcan `[x]` cuando algo queda hecho.

**Enlaces útiles**
- Descarga del proyecto: https://github.com/juancartoons/hello-world/archive/refs/heads/claude/trazo-vr.zip
- Simulador de sonidos (para probar en la computadora): https://claude.ai/artifact/4irW6KNFZ6bB38WRQ9CpbB (y en el proyecto: `trazo/Herramientas/simulador-sonidos.html`)
- Simulador de la paleta (a escala, igual que en el visor): https://claude.ai/artifact/2ekoCGCN2CACFTmDHhDyrn (y en el proyecto: `trazo/Herramientas/simulador-paleta.html`)

**Cómo instalar cada versión en Unity**
1. Copia las carpetas **Scripts**, **Shaders** y **Plugins** a tu proyecto.
2. Menú **TrazoVR → ★ Armar escena** (siempre que la versión agregue botones o materiales).
3. **Ctrl+S** para guardar la escena.
4. **Build And Run**.

---

## 0+. Hecho en la v46 (falta probarlo en el visor)

- [x] **Dedo jeringa**: el color ahora sigue las **falanges** del dedo (aunque esté doblado) y por dentro lleva un "alma" de color fina que se ve siempre. Tocar **líneas** es más fácil (más margen) y funciona también cerca de la paleta.
- [x] **Mirada**: lo señalado ya **no se infla**: conserva su forma y grosor y **titila suave** de color (la línea y el relleno).
- [x] **"Hola, ¿cómo estás?"**: la voz ahora **graba tu frase** (Vosk con su propio micrófono) y la cabeza la **repite con tu voz** mientras mueve la boca, una vez. Con el botón ¡Hola! (sin voz) la dice muda, como antes.
- [x] **Cuadro a cuadro**: capas dedicadas (botón en la página Animar de la mesa, en la tarjeta de la capa, o voz "cuadro a cuadro"). Cada cuadro nuevo empieza **vacío**, sin morph; cada dibujo dura hasta el siguiente; papel cebolla siempre prendido (anterior rojizo, siguiente verdoso). Si la capa ya tiene dibujo, se crea una capa nueva "Cuadros".
- [x] **Arena del desierto amarilla** (dorada al sol, ocre en la sombra), ya no del color del cielo.
- [x] **Timeline**: al arrastrar el cabezal se siente un poquito pesado (inercia suave).
- [ ] Cuadro a cuadro: botón "copiar el cuadro anterior" (para retocar en vez de redibujar) y ver 2 o 3 cuadros de cebolla.
- [ ] Si el dedo jeringa aún no se ve lleno en el visor: revisar qué mano está dibujando el visor (puede no ser la de vidrio).

## 0+. Hecho en la v45 (falta probarlo en el visor)

- [x] **Dedo jeringa de verdad**: ahora se pinta **el mismo dedo índice** de la mano (desde la yema hacia el nudillo), con la orilla del "líquido" más clara. Se quitó la cápsula aparte que se salía del dedo.
- [x] **Mano izquierda**: con el índice estirado (señalando, para tocar el micrófono o el ojo) **ya no sale el borrador**. Y después de una pausa ya no hay que abrir toda la mano: basta separar pulgar de índice y medio (o esperar medio segundo).
- [x] **Alt con pellizco**: si tocas un tirador con el índice y luego pellizcas (aunque sea un instante después), pasa a Alt: el otro tirador vuelve a su lugar y solo se mueve ese lado. El pellizco manda siempre.
- [x] **Voz**: **"copiar"** / **"pegar"** = las líneas seleccionadas (o la que miras), con sus rellenos; se pegan 5 cm a tu derecha. **"copiar color"** / **"pegar color"** = el color.
- [x] Flechas de girar **más al centro**. La **mesa arriba 10 cm más cerca**.
- [x] **Miniatura** de la capa **dentro** de la tarjeta (la tarjeta crece a la izquierda).
- [x] **Cara nueva** (bocas y tutorial): cabeza rosada redonda cerrada abajo, ojos grandes medio cerrados mirando de lado, boca ancha con dientes (sin lengua). Dice **"hola, ¿cómo estás?"** / **"hello, how are you?"** (dos veces, con tiempo para verla).
- [x] Fondo **Estudio de animación** reemplazado por **Atardecer en el desierto** (fondo vivo: dunas con luz dorada y sombras violeta, cielo del atardecer con cirros, primeras estrellas y polvo del viento). Voz: "desierto".
- [ ] Si la mano izquierda sigue lenta, revisar con una prueba: ¿pasa solo después de tocar botones o siempre?

## 0+. Hecho en la v44 (falta probarlo en el visor)

**La mirada** (el ojo, debajo del micrófono)
- [x] Tócalo (con cualquier índice) o di **"mirada"**: sale una **mira** en el centro de tu vista, con **imán** invisible. Lo señalado **brilla** (celeste = línea, dorado = relleno).
- [x] La mirada solo señala; la voz actúa: un **color** ("rojo") va a lo que brilla; si nada brilla, a tus líneas nuevas. **"línea roja"** / **"relleno rojo"** para ser exacto (los 12 colores).
- [x] **"copiar"** / **"copiar color"** y **"pegar"**. **"borra esto"**, **"oculta esto"** / **"ocúltalo"**, **"esta capa"**, **"selecciona"**.
- [x] En nodos: mira un nodo y di **"esquina"** (o esquinas, alas, alitas, manijas, antenas, palancas, tiradores, handles, alt): cada tirador se mueve por su lado con el índice. **"suave"** = vuelven a alinearse.
- [x] Junto a la mira, una **sugerencia** de qué decir.
- [ ] Seguimiento de ojos real (Quest Pro): por ahora la mirada es hacia donde apunta tu cabeza.

**Dedo jeringa**
- [x] Toca un color de la paleta: tu índice se **llena** de ese color. Toca una **línea** = la pintas; toca **dentro de una forma** = la rellenas. Al pintar se vacía. Caen **gotitas** al llevarlo cargado.
- [x] El cuentagotas y "copiar" también cargan el dedo.
- [ ] Si funciona bien: **quitar la cubeta** en la siguiente versión.

**Capas**
- [x] Voz **"capa 1"** ... **"capa 12"** (o "layer one"...).
- [x] Borrar = un **basurerito** rojo en la esquina (toca dos veces). Se quitó el botón "Nombre": **deja el dedo quieto en el nombre** 1 segundo y sale el teclado.
- [x] **Miniatura** de la capa a la izquierda de la tarjeta (se actualiza solo cuando cambia algo).
- [x] **La cámara es una capa**: siempre hay una, adelante de todas, con ícono de cámara. Elígela y aparece la camarita. En su tarjeta: **"+ Cámara"** (otra cámara) y **"Sugiere"**. La que elegiste es la que se usa al exportar. En una capa de cámara no se dibuja, y "Clave" guarda una clave de cámara.
- [x] Monitor de la cámara con **borde gris**.

**Arreglos**
- [x] Las **persianas** (capas y mesa) se abren con cualquier mano, aunque la otra haga un gesto o no se vea. Solo esperan si estás trazando una línea con esa mano.

**Fondo nuevo: Estudio de animación** (dibujado por la app, a lápiz)
- [x] Un cuarto completo alrededor de ti: líneas azules de construcción y grafito encima, mesa de luz con su disco, lámpara, frasco de lápices, corcho con dibujos, tendedero con la pelota que rebota, repisas con libros, ventana, planta, puerta, póster y lámpara colgante. Tiene profundidad real (paredes a su distancia). Voz: **"estudio"**.

**Tutoriales**
- [x] La ayuda de la mesa ya no está en la mesa: **Mis archivos → Ayuda** (y en el panel "?": **6 · Ayuda de la mesa**).
- [x] Nuevo: **5 · Mira y di** (la mirada y la voz, 4 páginas).
- [x] El primer tutorial termina con una **cabeza** que dice **"hola"**: di "hola" (o toca ¡Hola!) y su boca habla. La voz "hola" también hace hablar tus bocas en cualquier momento.
- [x] Novedades: páginas **13 · La mirada y el dedo jeringa** y **14 · Estudio de animación y capas**.

## 0+. Hecho en la v43 (falta probarlo en el visor)

**La cámara** (nueva "capa" de cámara)
- [x] Capas → botón **Cámara** (o di **"cámara"**). Se ve una camarita con su **cuadro** (16:9 o 9:16 según el formato de Exportar) y encima un **monitor** en vivo con sus botones: **Clave, Quita, Sugiere, + / −** (zoom) y **Quitar**.
- [x] En **2D** apunta de frente al plano de tu capa. En **3D** se pone sola en el mejor ángulo.
- [x] **Pellizco derecho** = la mueves y gira alrededor de lo que mira. **Pellizco izquierdo** = se corre sin cambiar hacia dónde mira.
- [x] **Sugiere** (o di "sugiere" / "encuadre"): prueba 28 ángulos y propone los **3 mejores** (llena más el cuadro, menos líneas encimadas, deja aire arriba). Salen 3 miniaturas encima del monitor: toca una.
- [x] **Cámara animada**: "Clave" (o "clave de cámara") la guarda en el fotograma; entre claves viaja suave. Con claves, moverla guarda la clave sola. Sus claves salen como **rombitos** encima de la barra.
- [x] Fotos y videos salen desde la cámara. **Quitar** = encuadre automático. Se guarda con el proyecto y entra en deshacer.

**Capas**
- [x] Botones **redondos 2D / 3D** en cada capa (cada capa recuerda el suyo). Se quitó el botón Plano del reloj.
- [x] **Borrar capa** (toca dos veces; deshacer la devuelve; las de encima bajan un número). Ya no hay capas fijas: siempre queda al menos una.
- [x] **Nombre**: el teclado del Quest; se muestra "3 · Fondo" (el número se queda).
- [x] El **maniquí** y las **bocas** crean su propia capa nueva ("Maniquí", "Bocas").
- [x] Mientras empujas las capas, los botones se **esconden** y salen 1 s después de que todo queda quieto. Se tocan **a propósito**: el dedo llega de frente y se activa 1 cm detrás del botón.

**Voz**
- [x] **"Lápiz"** = lápiz y 2D de una vez en la capa actual; **"tinta"** = sin lápiz; **"dos de" / "tres de"**.
- [x] **"Siguiente fondo"** (sin abrir la ventanita) y cada fondo **por su nombre**: roma, shanghai, venecia, callejón, amanecer, atardecer, mirador, parque, flores, campo, golf, nueva york, sala, cuadrícula, realidad (y universo, nubes).
- [x] **Instrumento por voz**: piano, arpa, orquesta, cajita, japón, post punk, drum and bass, dubstep, marimba.
- [x] "exportar", "cebolla", "cámara", "sugiere", "clave de cámara".

**Animación y exportar**
- [x] **Papel cebolla** (página Animar → "Cebolla"): con la animación en pausa se ven la clave anterior (roja) y la siguiente (verde) de tu capa. No salen en fotos ni videos.
- [x] **Exportar** (antes "Video"): **1920x1080** horizontal o **1080x1920** vertical, y **fondo verde** opcional (solo en el MP4).
- [x] "Grabar proceso" se quitó de la vista (el código queda para después).

**Nodos**
- [x] **Pellizco derecho** sobre un tirador = se mueve solo ese lado (como Alt), con un campo invisible más grande. El índice sigue moviendo nodos y tiradores normales. Se quitó el gesto de la V.
- [x] Una esquina ya rota **respeta siempre** sus lados (no se vuelve a alinear sola).

**Otros**
- [x] La mesa arriba, 4.5 cm más cerca. La **mano izquierda** también toca las flechas de girar, el micrófono y las pastillas (plegadas y desplegadas).
- [x] **Universo**: sin las formas grises estiradas; nebulosas más chicas, lejanas y redondeadas.
- [x] Fondos: se quitó la **avenida ancha** y la **sala de lujo repetida**.
- [x] Novedades: páginas **11 · Capas 2D/3D, cebolla y exportar** y **12 · La cámara**.

**Para después**
- [x] **Estudio de animación** (fondo nuevo): hecho en la v44.
- [ ] Meter los idiomas de la voz dentro de la app (solo para la versión final del concurso).

## 0+. Hecho en la v42 (falta probarlo en el visor)

**Etiquetas = palabras de voz** (en TODOS los botones)
- [x] Deja el índice cerquita de cualquier botón un momento y sale su etiqueta de vidrio (como en la paleta): en el reloj, la mesa, la tarjeta de capas, Mis archivos, la ventanita de fondos, la paleta, las pastillas, el parlante, el micrófono y las flechas de girar.
- [x] Si esa palabra también se puede **decir**, la etiqueta lleva un **microfonito** verde azulado y es **exactamente** la palabra de la voz ("Capa nueva", "Gira derecha", "Rojo"...).
- [x] Los botones dicen lo mismo que su voz: "+ Clave" → **Clave**, "- Clave" → **Quita clave**, "+ Capa" → **Capa nueva**, "Colores" → **Paleta**, "Video anim" → **Video**, y el de ver la capa dice lo que hace (**Oculta** / **Muestra**).
- [x] **Comandos nuevos**: plano, fondo, archivos, esfera, cubo, cilindro, a líneas, ayuda, inicio, zoom, animar, bocas, títere, video, muestra, oculta, lápiz, liberar, avanzar, universo, nubes. Y sin botón: los **12 colores** ("rojo", "azul"...), **más grueso** / **más fino**.
- [x] **Idioma por voz**: en inglés di **"spanish"** (o "español"); en español di **"inglés"** (o "english"). La voz se vuelve a preparar en el otro idioma (la primera vez lo baja).

**Fondos vivos** (no son fotos: los dibuja la app)
- [x] **Universo**: miles de estrellas (las cercanas de verdad en 3D), la Vía Láctea con su polvo oscuro y nebulosas de colores. Sin piso.
- [x] **Sobre las nubes**: un mar de nubes en dos capas, el cielo en degradado y un sol cálido a lo lejos.
- [x] En la ventanita de fondos (después de los de la app). Botón **"Avanzar" / "Volar"**: las estrellas o las nubes pasan despacio (velocidad suave y pareja). Las flechas de girar también los giran. Voz: "universo", "nubes", "avanzar".

**Girar**
- [x] Dos **flechitas fijas** en la fila de la pastillita de abajo: la izquierda debajo del micrófono y la derecha debajo del parlante. Solo con un fondo 360 (o vivo). **Toque** = 30° de una vez; **toca y arrastra** de lado = el fondo sigue tu dedo. Se quitó lo de girar la cabeza para que salga la flecha.

**Mesa y línea de tiempo**
- [x] Con el dedo sobre **toda la mesa** (reproductor, línea de tiempo, las 3 pestañas y la página abierta) no se dibuja ni se borra (tampoco a lápiz). La línea que venía se corta ahí.
- [x] La **barra sujeta** (doble toque) nunca deja dibujar, ni por accidente (ni un momento después de sujetarla o soltarla).
- [x] La mesa **arriba** queda 1.5 cm más cerca de ti.

**Otros**
- [x] **Lápiz: Sí / No** en la tarjeta de capas (se quitaron Boceto gris y azul). Las líneas de vectores ya no cambian de color en una capa de lápiz: para bocetar con vectores, cambia el color de la línea.
- [x] Micrófono un 13 % más hacia el centro y parlante un 10 % más a la derecha.
- [x] **Tutorial con voz**: al empezar, **"Solo manos"** o **"Manos + voz"**. Con voz, en los pasos de la mano izquierda la viñeta dice también qué decir ("o di "dibuja"", "ya", "borra", "nodos", "deshaz"). Durante el tutorial la voz solo acepta esas palabras (y "silencio").
- [x] Novedades: páginas de la voz y de girar al día, y una nueva: **10 · Fondos vivos**.

## 0+. Hecho en la v41 (falta probarlo en el visor)

**Paneles flexibles** (el experimento de las pastillas)
- [x] **Pellizca** una pastillita (pulgar + índice derechos, sin jalarla) y llévala **arriba o abajo** de tu vista. Al soltarla vuela suave a su sitio nuevo. Basta con llevarla unos 8 cm hacia arriba o hacia abajo. Se recuerda al cerrar la app.
- [x] **Arriba** se jala **hacia abajo** y su panel se despliega hacia abajo. **Abajo** se jala **hacia arriba** y se despliega hacia arriba.
- [x] **Mesa arriba**: sale encima de tu mirada (unos 20°), en **espejo**: las pestañas (Animar, Bocas, Títere) quedan **debajo del reproductor** y sus páginas se despliegan hacia abajo. El tirador para guardarla queda encima (súbelo). La copia azul de la ayuda sale debajo.
- [x] **Capas abajo**: salen debajo de tu mirada, inclinadas hacia ti (las ves desde arriba). En espejo: las de adelante bajan y las de atrás suben. El tirador queda encima de la tarjeta (bájalo).
- [x] Las dos pastillas en la **misma orilla** se ponen **lado a lado** (capas a la izquierda, mesa a la derecha).
- [x] Si abres las dos desde la misma orilla, se acomodan **una arriba y otra abajo**: mientras una está abierta, la pastilla de la otra salta a la orilla libre (y su panel sale ahí).
- [x] Con las capas o la mesa abiertas **abajo**, las letras de cómic de arriba sí pueden salir (antes se escondían con las capas abiertas).
- [x] Novedades (tutorial): página 9, "Paneles flexibles", con su animación.

## 0+. Hecho en la v40 (falta probarlo en el visor)

**La voz** (nueva, gratis y sin internet después de la primera vez)
- [x] **Micrófono** arriba a la **izquierda**, en espejo del parlante y del mismo tamaño. Tócalo con el índice derecho para prenderla o apagarla (se recuerda). Verde azulado = escuchando (con un aro que salta cuando entiende algo); naranja = preparándose; gris con raya = apagada.
- [x] Usa **Vosk** (código abierto): el visor entiende tus palabras **sin internet**. Solo la **primera vez** baja el idioma de la app (unos 40 MB, español o inglés, el que esté puesto, no los dos) y lo guarda. El Quest pide el **permiso del micrófono**.
- [x] Los gestos **siguen funcionando** al mismo tiempo. Si la mano izquierda hace un gesto de verdad (pellizco o puño), la mano manda y el modo de voz se acaba.
- [x] Sin pellizcar: **"dibuja"** y dibujas solo con el índice; **"ya"** para. Igual **"borra"**, **"nodos"**, **"recta"** (hasta decir "ya").
- [x] Comandos (español / inglés): dibuja · borra · nodos · recta · ya (listo, para, alto, basta) · deshaz · rehaz · gira izquierda (gira a la izquierda) · gira derecha · gira atrás · capas · capa nueva · mesa · play · pausa · siguiente · anterior · clave · quita clave · paleta · guarda · vista limpia · silencio · halo. En inglés: draw · erase · nodes · straight · stop · undo · redo · turn left · turn right · turn around · layers · new layer · desk · play · pause · next · back · key · remove key · palette · save · clean view · mute · halo.
- [x] Con la voz prendida **no salen las letras de cómic** de arriba: en su lugar, a la **izquierda y un poco más abajo**, un panelito: **"Sugerencia: di en voz alta:"** y debajo qué decir según lo que haces (sin hacer nada: «Dibuja»; en un modo de voz: «Ya»; recién editaste: «Nodos»; con la mesa: «Play»...). Ahí también salen los avisos y, en verde, lo que entendió. *(Es el primer paso del mini-tutorial predictivo.)*

**Girar** (ideas 5 + 7 juntas)
- [x] Con un fondo 360, gira la cabeza **bastante hacia un lado** (unos 45°) y en esa orilla sale una **flechita**. Tócala con el índice: se prende el **modo girar** (se pone azul, "<  >"). Desliza el índice a **cualquier lado** y el fondo sigue a tu dedo (10 cm = 30°). Se apaga con el dedo quieto 1.5 s o tocando otra vez la flecha. **Sin flecha no gira nada** (no hay accidentes). Se quitó lo de la mano plana.

**Halo** (las puntas cortadas con fondo 360 o passthrough)
- [x] La causa de verdad: el **halo** (solo existe con fondo 360 o la realidad) **terminaba recto** en la punta, y la línea se veía cortada. Ahora el halo **envuelve la punta**, redondo y suave. La línea **no cambia**: sigue fina como antes.

**Línea de tiempo con gestos**
- [x] Con el **gesto de borrar** (puño izquierdo) tocas una **clave** de la barra y se **quita**. Con el **gesto de dibujar** (pulgar + índice izquierdos) tocas la barra y se **pone una clave** ahí. (Haz el gesto primero y luego toca.) Junto a la barra el dedo no pinta ni borra líneas.
- [x] **Barra sujeta**: mientras arrastras el cabezal con el índice (o una clave con el pellizco), un **doble toque izquierdo** (como el del candado) hace que la barra siga tu dedo **solo de lado**, aunque el dedo se salga de ella. El cabezal se ve más grande. Otro doble toque y vuelve a la normalidad.

**Uniones**
- [x] Al unir dos líneas o cerrar una figura, la unión queda en **esquina** (dos rectas siguen rectas). En la paleta → **LÍNEA → Uniones** (el ícono "V") cambias a **Suaves** (curva, como antes). Se recuerda.

**Otros**
- [x] **Parlante** a **30°** (antes 25°). El micrófono queda en espejo a la izquierda.
- [x] Novedades (tutorial): páginas nuevas de la voz, de la línea de tiempo con gestos y de girar con las flechas.

## 0+. Hecho en la v39 (falta probarlo en el visor)

**Puntas de las líneas**
- [x] Vuelven a ser **finas como antes** (se quitaron las puntas redondas de la v38).
- [x] La causa de que se vieran **cortadas** con fondo 360 y passthrough: con esos fondos el visor va más cargado, el **modo rendimiento** encendía la **foveación** (el borde de la vista con menos detalle) y la puntita fina desaparecía. Ahora el modo rendimiento **ya no usa la foveación** (solo pide más potencia al Quest) y la app la apaga al empezar.
- [x] Además, la línea **nunca es más fina que un pixel** de la pantalla: la punta se ve entera hasta el final, sin cambiar su forma.
- [x] En el menú del reloj (arriba, en letras grises) se ven los **cuadros por segundo** y, si se enciende, "modo rendimiento 1".

**Solo persiana**
- [x] Mirar arriba o abajo **ya no abre nada** (ni las capas ni la mesa). Se quitaron los circulitos. Las capas y la mesa salen **solo jalando sus pastillas** (o con los botones Capas y Mesa del reloj).

**Vista limpia** (sin menús fijos)
- [x] Esconde las **dos pastillas**, el **parlante con sus íconos** y las **letras de cómic** (nombres de los gestos y avisos; los avisos siguen saliendo en el menú del reloj, en letras pequeñas). Se recuerda al cerrar la app.
- [x] Se pone y se quita con el botón **"Vista limpia"** del reloj o con un **doble puño derecho** (cerrar, abrir y cerrar rápido, en menos de un segundo). Un solo puño no hace nada (así descansar la mano no la cambia). No usa el meñique.
- [x] Botón nuevo **"Mesa"** en el reloj (para sacar la mesa sin su pastilla). El menú del reloj creció 2.7 cm hacia abajo por esa fila (Vista limpia / Mesa).

**Girar** (opción A)
- [x] **"Pasar página"**: mano **derecha** abierta y plana, **de lado** (como un cuchillo), y un barrido rápido de unos **20 cm** a la izquierda o la derecha = 30°. El fondo sigue a tu mano. No usa la mano izquierda (no se abren la paleta ni el menú). Se quitó lo del antebrazo.
- [x] Siguen las flechas **"← Girar" / "Girar →"** del reloj.

**Fondos**
- [x] **Sala de lujo**: vuelve la **foto de antes** (con las matas y todo) y **va contigo**: se centra en tu cabeza cada cuadro. Sigue siendo 3D (cada ojo la ve un poquito distinto), pero como siempre la ves desde el centro, nada se estira ni se tuerce. Solo este fondo. Mi versión ordenada (v38) quedó guardada en `Retoques/sala_lujo/sala_lujo360_v38_ordenada.jpg`.
- [x] **Venecia**: sin la **ropa colgada** que flotaba al fondo del canal de la izquierda.
- [x] **Parlante e íconos** más a la derecha (unos 25° de hacia donde miras; antes 20°).

## 0+. Hecho en la v38 (falta probarlo en el visor)

**Borrador**
- [x] **Puño más fácil justo al empezar a borrar**: basta con 2 de los 3 dedos bien doblados; un puño apretado le gana al pellizco; acepta la mano un poco más de frente; un temblor de la mano (0.15 s) ya no lo suelta; y espera un instante (0.12 s) antes de borrar, para no llevarse nada sin querer al cerrar la mano. *(Sin el plan B de "voltear el lápiz", como pediste.)*
- [x] En lugar del círculo rojo: una **esfera de vidrio** que brilla más al tocar: **oro rosa** para las líneas y **champaña** para los rellenos (el destello al borrar es del mismo color).

**Líneas**
- [x] ~~Puntas redondas~~ (se quitaron en la v39: no gustaron; ver v39) y la foveación de Alta a Media (en la v39 ya no se usa).
- [x] **Polvo oscuro en los fondos blancos** (el claro no se notaba).

**Persianas** (capas arriba, mesa abajo)
- [x] **Arriba**: la pastillita "Capa N · de M" se **jala hacia abajo** con el dedo y salen las capas. Debajo de las capas queda otra pastillita: **jálala hacia arriba** para guardarlas.
- [x] **Abajo**: una pastillita nueva con **"Fotograma N · m:ss"** y una barrita de avance de la animación. **Jálala hacia arriba** y sale la mesa de trabajo (reproductor y línea de tiempo, con sus 3 pestañas replegadas). Debajo de la mesa queda otra pastillita: **jálala hacia abajo** para guardarla.
- [x] Cada pastilla tiene a la derecha un **circulito** que cambia el modo: **persiana** (jalar) o **mirada** (mirar arriba / abajo). Se recuerda. *(Por ahora las dos empiezan en persiana; decide tú cuál se queda.)*
- [x] Mirar arriba o abajo ahora pide menos: **12° arriba** y **20° abajo** (antes casi 30°).
- [x] La mesa sale **un poquito más abajo** (26° debajo de hacia donde miras; antes 20°).

**Paneles y paleta**
- [x] **Mover cualquier panel con el pellizco**: pellizca una parte vacía (no un botón) de **Mis archivos**, la **ventanita de fondos 360**, el **buscador de imágenes** o la **mesa** (su cuerpo) y llévalo a donde quieras; se queda mirándote. Pellizcando también con la izquierda: separar o juntar las manos = tamaño.
- [x] **Etiquetas al pasar el dedo** en la paleta: deja el índice cerquita de un botón o de un color (un cuarto de segundo) y sale una etiquetita de vidrio con su nombre.
- [x] **Menú del reloj**: los botones más separados (el panel quedó del mismo tamaño) y una fila nueva con **"← Girar"** y **"Girar →"**.

**Girar el fondo 360** (B y C)
- [x] **B: panel táctil en el antebrazo izquierdo**: desliza la punta del índice derecho unos 5 cm sobre tu antebrazo izquierdo (la "manga"), a la izquierda o a la derecha = 30°. *Cambio respecto a la propuesta:* iba a ser sobre la **palma hacia arriba**, pero esa es la pose que abre la paleta, así que se pasó al antebrazo. Solo funciona con la mano izquierda libre y sin la paleta ni el menú abiertos.
- [x] **C: flechas en el menú del reloj** ("← Girar" y "Girar →").
- [x] Se quitó el gesto del pulgar sobre el índice (se disparaba solo mientras dibujabas).

**Fondos 360**
- [x] **Canal de Venecia al atardecer**: cielo nuevo con naranjas, el sol bajo y un reflejo cálido en el agua; sin el barandal torcido, sin las chimeneas de la casa roja (ni la antena); las escaleras del puente ya no se tuercen (la plaza de abajo baja poco a poco desde el puente). Herramientas: `Retoques/venecia_canal/atardecer.py` y `mascara_atardecer.py`.
- [x] **Azotea en Nueva York**: sin la reja de vidrio ni los dos arbolitos que se mezclaban con los edificios; edificios más nítidos (como Shanghai); la ciudad que se ve por debajo del borde ya no queda "pintada" en el piso de la azotea (era lo que torcía el techito del bar). Herramientas: `Retoques/nueva_york/`.
- [x] **Sala de lujo** (la que viene con la app): sin el **sofá cercano** que pasaba por debajo de ti ni la **mesita oscura** (el sofá grande quedó entero); en la mesa de centro **solo los libros**, limpios y rectos; la **mesa** con su tapa y su frente planos (la esquina ya no se ve rota); la **ciudad** ya no se ve "después de un terremoto" (estaba pegada al piso); las **orillas** del sillón, las mesitas y las tablillas del techo se estiran mucho menos (sin alas ni rayas barridas). Herramientas: `Retoques/sala_lujo/`.
- [x] La **otra "Sala de lujo"** que veías es **tu propia foto** ("Mi foto 360"). Ahora tus fotos 360 tienen una **X** en la esquina de su miniatura: tócala **dos veces** (en 3 segundos) para borrarla.

**Otros**
- [x] La **mano de cómic** (el guante) un **10 % más pequeña**.
- [x] Tutorial Novedades y ayudas al día.
- [ ] **Fondo que sigue la cabeza** (para que no se estire nada): **pospuesto**, porque tiende a marear más. Solo si los fondos se siguen viendo mal después de estos arreglos.

## 0+. Hecho en la v37 (falta probarlo en el visor)

**Paleta "de lujo"**
- [x] Sin aros negros: el disco grande y el centro son de **vidrio** (se ve tu fondo borroso a través, con un brillo fino en la orilla). El centro es más esmerilado, para distinguirlo del disco grande.
- [x] Los colores son **perlas**: sin aro, un poco más oscuras en la orilla (así hasta el blanco se distingue) y con un brillito arriba a la izquierda. Círculos más redondos.

**Mesa de trabajo**
- [x] **Más abajo**, como un espejo de las capas: unos 20° debajo de hacia donde miras. Te sigue con calma (de lado y de arriba a abajo); mover un poco la cabeza no la mueve, así puedes mirarla; con el dedo encima se queda quieta.
- [x] Las pestañas **Animar, Bocas y Títere** pasaron **arriba** del reproductor y sus páginas se abren **hacia arriba**.
- [x] Las **X** de la mesa (arriba a la derecha) y de las capas (arriba a la derecha de la tarjeta, junto a "+ Capa") ya existían; ahora son un poco más grandes.

**Otros**
- [x] Al **borrar un nodo**, los tramos de línea que lo sostenían se deshacen en **polvo**, como una línea borrada (y suena la burbuja o el cristal).
- [x] La **luz del dedo ya no ilumina las líneas** (ni la punta de la que dibujas). Sigue en los rellenos, el vidrio y las manos.
- [x] **Vidrio un poquito más transparente** en Mis archivos, la mesa de trabajo y el menú de la muñeca.
- [x] **Canal de Venecia**: el agua ahora está unos 4 m debajo de tus ojos (antes, al nivel del puente). Herramienta: `trazo/Herramientas/Retoques/venecia_canal/agua_mas_abajo.py`.

## 0+. Hecho en la v36 "como de 2030" (falta probarlo en el visor)

**Fondos** (★ Armar escena borra solos de tu proyecto los que se fueron)
- [x] **Shanghai de noche retocado**: sin gente, sin neblina y más nítido.
- [x] **Atardecer en el mar** sin la baranda cercana y **Canal de Venecia** sin la reja deformada del puente.
- [x] **Sala de lujo** (nuevo, hecho con ChatGPT): costura unida, 4K y profundidad IA.
- [x] **Azotea en Nueva York** en lugar de Refugio con grafitis.
- [x] Se quitó **Orilla de Shanghai** (el de día).

**Como de 2030**
- [x] **Vidrio vivo**: los menús y la paleta son de vidrio esmerilado; a través se ve tu fondo 360 borroso (y gris en modo realidad), con un brillo en el borde que cambia al moverte.
- [x] **Interfaz que respira** (la mesa de trabajo): discreta si no la miras, despierta al mirarla o acercar el dedo; aparece con un rebote de resorte y un sonidito fino.
- [x] **Luz en la punta del dedo**: el índice lleva una lucecita; las líneas, los rellenos y el vidrio se iluminan un poquito cerca del dedo. **Manos de vidrio** con el contorno iluminado. *(Si no te gusta: se vuelve a lo de antes dejando vacío "Material Mano Vidrio" en ControlManos, o me escribes y lo quito.)*
- [x] **Líneas que cobran vida**: al terminar una línea, un destello la recorre; al borrarla, se deshace en **polvo brillante**; al deshacer, se **rebobina** (se recoge desde su final).
- [x] **Burbujita secreta** de la paleta (arriba a la derecha, por fuera de los botones): **llena** = con burbujas (el "plop" de siempre); **hueca** = sin burbujas (un "tin" de cristal). Tócala para cambiar. No sale en los tutoriales.

**Capas y mesa de trabajo**
- [x] **Pastillita "Capa 2 · de 5"**, siempre visible, chiquita y de vidrio, arriba de tu vista. Se despierta cuando la miras.
- [x] **Mirar arriba** (medio segundo) = se despliegan las capas, **en el mismo lugar de siempre**. **Bajar la mirada** = se recogen. No salen mientras dibujas ni justo después (1 s), ni con la paleta, el títere, un tutorial o una exportación. Abiertas con la mano: se cierran solas a los 20 s, como antes.
- [x] **Mesa de trabajo**: el menú de arriba aparece al **mirar abajo** (medio segundo), inclinado frente a tu cintura. Con la página **Animar** (reproductor y línea de tiempo) o las pestañas guardadas, **se queda** y te sigue despacio; **Medios, Bocas y Títere** se van al volver a mirar al frente (salvo si tienes el dedo encima). La **X** la guarda. El asa sigue funcionando para moverla y dejarla fija.
- [x] Tutorial Novedades y ayudas al día.

## 0+. Hecho en la v35 (falta probarlo en el visor)

- [x] **Capas más cerca y todo el montón se mueve**: la capa actual queda a unos **35 cm** de tus ojos (al alcance del dedo), en la dirección del cartel de cómic; entre capas siguen **7 cm**. Al empujar o atraer, **todas** las capas se mueven juntas y la tarjeta sólida viaja con su capa hasta el lugar fijo. Las de adelante suben más mientras más cerca están de tus ojos y se desvanecen: la que queda casi en tus ojos no se ve, la siguiente al 10 %, la otra al 20 %... Las de atrás bajan un poco. La tarjeta es un poco más angosta (26 cm).
- [x] **Botón "+ Capa"** en la tarjeta: una capa nueva adelante de todas (hasta **12**), y el montón se mueve hasta ella. Se puede deshacer. Los personajes del títere siguen yendo a la Capa 4.
- [x] Las capas se cierran solas a los **20 s** (antes 8).
- [x] **Tela con pellizco**: en el modo nodos, **pellizca** un relleno con la derecha (pulgar + índice) en cualquier figura (sin elegirla antes), jálalo hacia ti o empújalo al fondo y abre el pellizco. Pellizcar a menos de 2 cm de un rombito agarra ese mismo. Los rombitos se mueven con el **índice**, como los nodos. Si se sueltan planos, se quitan solos. Ya no hay que quedarse quieto medio segundo.
- [x] **Arreglo de la tela**: en figuras con lados rectos (hechas con la recta o con esquinas en cero) la tela no se armaba y el rombito se movía solo. Ahora los puntos en línea recta se saltan y, si algo se traba, hay un plan B. Además, un punto nuevo sobre un relieve que ya existía ya no lo duplica.
- [x] **Grosor de las líneas nuevas**: el grosor elegido con el dial (índice derecho en círculos) ahora se mide en el dibujo: si agrandas o achicas todo, las líneas nuevas cambian igual que las demás y combinan. Los proyectos de antes se convierten solos.
- [x] Tutorial Novedades y ayuda del menú de arriba al día.

## 0+. Hecho en la v34

- [x] **Capas en profundidad** (experimento de computación espacial): botón **Capas** en el menú de la mano (y el botón de la capa del menú de arriba). Las capas cuelgan como papeles arriba al centro de tu vista, donde sale el cartel de cómic (que se esconde mientras tanto). La actual se ve sólida con su nombre y sus botones (Ver, Boceto, Plano, Liberar, X); las de adelante y atrás son fantasmas transparentes con bordes iluminados. Capa 1 al fondo, Capa 4 más cerca. **Empujar** la actual (3 cm, en la parte del nombre) = la de adelante; **atraerla** = la de atrás. Se cierran solas a los 8 s. Cerca de ellas el dedo queda libre (sin bolita, no dibuja). Ya no se abre la lista vertical de capas.
- [x] **Puntos de tela**: en el modo nodos, tocar dentro del relleno de la figura elegida y quedarse quieto medio segundo = rombito morado; al moverlo, la tela se dobla en campana suave y el borde queda pegado a la línea. Borrador sobre el rombito = se quita. Se guarda en el proyecto y en las claves de animación.
- [x] **Girar con el pulgar**: puño suelto (cualquier mano) y el pulgar se desliza sobre el costado del índice = el fondo 360 gira 30° (como si giraras tú); los dibujos se quedan delante de ti.
- [x] **Lápiz: Pegado / Suave / Cuerda** (paleta > LÍNEA > Lápiz, solo en una capa de boceto 2D). Los hijos de las raíces ahora quedan seguidos, sin huecos.
- [x] **El relleno ya no muerde la línea**: sigue la curva con más puntos (hasta 200), su borde queda escondido debajo de la línea y se va un poco más atrás (sobre todo de lado).
- [x] **Texturas del relleno vivo unas 2.5 veces más marcadas**.
- [x] Tutorial **Novedades** con 8 temas (capas, tela, giro, lápiz, paleta, relleno vivo, fondos y papel, hoja 2D). Simulador de la paleta al día.

## 0+. Hecho en la v33

- [x] **Paleta con botones hijos**: alrededor de la paleta hay dos raíces, **LÍNEA** (a las 10) y **RELLENO** (a las 2). Al tocar una se pone amarilla y le salen sus hijos por el borde, unidos con un arco. Solo una abierta a la vez; al abrir la paleta empiezan cerradas.
  - Hijos de LÍNEA: Temblor, Hebras, Grosor vivo, Ciclo de 3, Suavidad, Velocidad, **Imán** y **Halo** (el halo solo con Realidad o 360).
  - Hijos de RELLENO: **Textura** (Liso → Facetas → Manchas → Pinceladas), **Cubeta** y **Velocidad** de la textura (solo con textura).
  - **Tinta invisible** a las 12 (con RELLENO abierto = sin relleno).
  - Se quitó el botón de **tamaño** de la textura (ahora se cambia con los dedos).
- [x] **Centro = vista previa**: la línea que tiembla, o con RELLENO abierto un circulito con el relleno y su textura moviéndose. Abajo, dentro del círculo, un textito con lo último que tocaste ("Hebras 3", "Temblor 2 · Medio", "Relleno: Rojo").
- [x] **Color del relleno aparte** del de la línea: los 12 colores pintan lo que esté abierto. Con líneas elegidas, con RELLENO abierto se pinta su relleno. El cartel de datos muestra también el color y la textura del relleno.
- [x] **Paleta estable**: no copia los temblores chiquitos de la mano (filtro como el del lápiz) y, con tu índice derecho a menos de 12 cm, se queda quieta en el aire (solo se mueve si la mano izquierda se va más de 3 cm).
- [x] **Tamaño de la textura con dos dedos**: con la paleta abierta, el pulgar y el índice derechos sobre un relleno con textura; ábrelos o ciérralos (x0.25 a x8, sin escalones). Se puede deshacer y ese tamaño queda para los próximos rellenos. Los dibujos de antes conservan su tamaño.
- [x] **Recta con el lápiz de boceto**: en una capa de Boceto en 2D, el gesto de la recta muestra una guía gris y al soltar queda en grafito, con presión. Si al empezar a dibujar juntas el dedo medio, se borra el poquito de lápiz que alcanzó a pintar.
- [x] **El relleno ya no cambia de color sin querer**: se quitó "tocar un relleno = siguiente color" (saltaba entre línea y línea). Tocar dentro para rellenar ahora solo funciona con **RELLENO abierto** y espera un instante. Y al unir una línea nueva con una figura abierta rellena, la figura conserva su relleno.
- [x] **Tutoriales al día**: Rellenos (la cubeta y tocar dentro ahora están en RELLENO) y Novedades (7 temas: relleno vivo con dos dedos, paleta con hijos y recta de lápiz).
- [x] **Simulador de la paleta** en HTML, a escala, con las medidas del código (enlace arriba).

## 0+. Hecho en la v32

- [x] **Fondos 360**: se quitaron Cuarto, Crucero (el "centro comercial"), Columnas y La Luna (★ Armar escena los borra solo de Unity). Nuevos: **Shanghai de noche** (subido por Juan Carlos) y 6 libres: Orilla de Shanghai, Avenida ancha, Camino entre flores, Refugio con grafitis, Golf de noche y Campo seco (4K + profundidad IA). Total: 14.
- [x] **Botón Fondo**: Cuadrícula → Blanco → Realidad → **360**. Al llegar a 360 se abre una **ventanita con miniaturas** de todos los fondos 360 (y tus fotos); toca uno para probarlo. Arriba tiene Cuadrícula, Blanco, Realidad y X.
- [x] **Papel blanco** detrás del lápiz en capas de boceto en 2D (1.2 x 0.8, centrado donde empezaste a dibujar). Botón en su esquina de arriba a la derecha: Blanco → 50 % → Transparente (cada capa recuerda el suyo).
- [x] **Imán** pasó a la paleta (a la derecha). La paleta queda **1 cm más lejos** de la mano.
- [x] **Relleno vivo**: botón de **tamaño** (Chico, Normal, Grande, Enorme) junto a la velocidad.
- [x] **Proyectos en Descargas → JCartoons → Proyectos** (una sola copia; los de antes se mueven solos). Cada **Guardar** conserva las **5 versiones anteriores** (Mis archivos → Mostrar: Versiones). Fotos, videos y SVG ya no quedan repetidos dentro de la app.
  - Ojo: si algún día desinstalas la app, los archivos siguen en Descargas, pero Android no deja que la app nueva los vea sola: haría falta un botón "Recuperar" (pendiente, solo si pasa).
- [x] **Modo rendimiento** automático: si los cuadros bajan de ~68 por segundo, pide más potencia al Quest y luego foveación dinámica (solo el borde de la vista). Nunca baja la resolución ni apaga efectos. Los fps se ven pequeñitos en el menú de la mano.
- [x] **Tutoriales**: nueva sección **4 · Novedades** en el "?" (6 temas con animación). Textos que mencionaban "Mis archivos > Tutorial" corregidos.

## 0+. Hecho en la v31

- [x] **Encantamiento 2D**: lo que dibujas en Plano 2D queda en la hoja de su capa. Nodos, tiradores, lazo, plastilina, mover con el pellizco y girar/escalar con las dos manos se quedan dentro de esa hoja, aunque cambies a 3D. La capa **recuerda su hoja** al volver a 2D.
  - Botón **Liberar** en la fila de cada capa (solo se ve si hay algo que liberar): con líneas elegidas libera solo esas; si no, toda la capa (y si su hoja es "propia", la borra). Se puede deshacer.
  - Dibujos de antes: las líneas que estaban sobre la hoja de su capa quedan encantadas.
- [x] **Frente / fondo** dentro de una capa: con líneas 2D agarradas (lazo o pellizco), empuja la mano ~3 cm hacia adentro de la hoja = atrás un paso; tira hacia ti = adelante. Las nuevas salen al frente. Ya no parpadean dos rellenos encimados. Los SVG respetan el orden.
- [x] **Menú de arriba tipo archivador**: siempre se ven las capas, la línea de tiempo y el reproductor; las páginas (Animar, Bocas, Títere) quedan guardadas y solo se asoman sus pestañas. Tocar = abrir, tocar otra vez = guardar. Recuerda cómo lo dejaste. "Fijar aquí" ahora es un **alfiler** junto al asa.
- [x] **Mis archivos nuevo** (sin pestañas): tarjeta del dibujo actual (nombre = renombrar, ● = sin guardar), Guardar, **Compartir** (Foto, SVG, Video anim, Video proceso, Vel), **● Grabar** (con Pausa); "Mostrar: Todo", "+ Imagen", "Imágenes: ver"; acciones sobre la miniatura (Abrir, Duplicar, Borrar / Ver, Borrar); deslizar el dedo para cambiar de página; ES/EN. Mientras grabas, en el menú de la mano sale "● tiempo" con Pausa y Parar.
- [x] **Relleno vivo estilo Quill** (paleta, arriba a la izquierda): Liso, Facetas, Manchas o Pinceladas, con tonos cercanos que cambian solos; al lado su velocidad (Quieto, Lento, Medio, Rápido). Con líneas elegidas se les pone a ellas; si no, a los próximos rellenos (tocar dentro de una figura o la cubeta). Sale en fotos y videos.
- [x] **Datos de la línea** debajo de la paleta: grosor, color, capa, temblor, hebras, grosor vivo, ciclo, suavidad, velocidad, relleno, cubeta y halo. Lo último que cambiaste sale resaltado en naranja.

## 0+. Hecho en la v30

- [x] **8 fondos 360 nuevos en 3D** (subidos a 4K con IA, Real-ESRGAN, y con profundidad IA): Canal de Venecia, Callejón de Venecia, Atardecer en el mar, Crucero de lujo, Pórtico de columnas, Mirador, Parque y **La Luna**. En total 11 fondos de la app. (Créditos: `Plugins/Fondos/fondos360-creditos.txt`; **ojo:** la licencia de la foto de la Luna no está confirmada, revisarla antes de publicar en la tienda.)
- [x] **Halo** (opcional): un brillo suave detrás de las líneas para que no se pierdan en el fondo. Blanco detrás de las líneas oscuras, oscuro detrás de las claras. El botón **Halo** sale en la **paleta de colores** (arriba a la derecha) solo con un fondo 360 o con la Realidad. No sale en fotos, videos ni SVG.
- [x] **Todo se exporta a Descargas → JCartoons** (fotos, videos y SVG), en la app Archivos del Quest.
- [x] **SVG doble**: `_lineas.svg` (las curvas limpias) y `_como_se_ve.svg` (con grosor, hebras y colores, como en la app).
- [x] **Menú de arriba solo para animar**: pestañas Animar, Bocas y Títere; **Zoom** en la fila del reproductor; **Boceto** y **Plano** en la fila de cada capa (lista de Capas); en la página Animar queda **Video anim**. Se quitó "Imagen −" (las imágenes se quitan con la X de su esquina).
- [x] **Mis archivos con 4 pestañas**: Archivos, **Crear** (Foto y SVG), **Grabar proceso** (Grabar, Pausa, Video proceso, Vel) e **Imágenes** (Traer imagen, ver/ocultar).
- [x] **Tiradores Bézier**: un dedo = los dos lados (curva suave, en espejo); con la **V** (índice + medio) = solo ese lado (esquina, como Alt); llevar la bolita hasta su nodo = **esquina recta** (tirador en cero).
- [x] **Cartel de cómic arriba al centro de la vista**: ahí salen el nombre del gesto y los avisos cortos (uno a la vez, el más nuevo).
- [x] **Autoguardado más seguido**: un respaldo rápido unos 30 s después de cada cambio (cuando la mano izquierda descansa 2 s) y el proyecto con su nombre cada 2 minutos. Si la app se cierra de golpe (por ejemplo con un Build), al volver se abre lo último.

## 0-. Hecho en la v29

- [x] **Tiradores "palanca"**: la bolita de un tirador nunca queda a menos de 4.5 cm de su nodo (si el tirador es más corto, la bolita se ve más lejos y lo mueve en proporción). Así no se enciman nodo y tirador.
- [x] **Lo que vas a agarrar se ilumina antes de tocarlo** (nodo naranja o bolita del tirador más grande).
- [x] **Girar los nodos del lazo con la V**: toca uno de los nodos elegidos con el índice y el medio estirados y gira la muñeca; giran alrededor del centro del grupo (en Plano 2D, solo dentro del plano). Los tiradores hechos a mano giran con ellos.
- [x] **Aviso** si no hay fondos 360 (falta "★ Armar escena").
- Forma de trabajo: desde ahora también los arreglos esperan el **HY**.

## 0a. Hecho en la v28

- [x] **Fondos 360 en 3D**: "Cuarto 360", "Roma de noche" y "Amanecer" (fotos reales de Poly Haven) con **profundidad calculada con IA** (Depth Anything V2 Small): cada cosa queda a su distancia, se ve en 3D con los dos ojos y cambia al mover la cabeza. El piso de la foto coincide con tu piso.
  - Herramienta para calcular la profundidad de otras fotos: `trazo/Herramientas/profundidad360.py`.
- [x] **Varias fotos 360 tuyas** (hasta 8): cada una que eliges con **Imagen + → Fondo 360** queda en el botón **Fondo** ("Mi foto 360 1, 2…"), con "piso real" (sin IA, por ahora).
- [x] **Mano copia del plano 2D**: ahora es una copia de **tu misma mano** (no el guante de Mickey).
- [x] **Tutorial, paso 2**: al llegar a la bandera, la mano derecha queda **atrapada** ahí (una copia quieta), la línea ya no crece y hay que **abrir la mano izquierda** para seguir. La bandera ahora es **a cuadros** (de meta).
- [x] **Tiradores Bézier**: al **acercar** el dedo a un nodo (sin tocarlo) aparecen sus tiradores; tocas la punta de uno y se pega al dedo.
- [x] **Plastilina**: índice y medio estirados (juntos o en V) con el anular y el meñique doblados.
- [x] **Imagen +**: ahora puedes **pellizcar** una imagen (en la lista o en grande) y **sacarla del panel**: queda donde la sueltas (crece al alejarla; cerca del plano 2D se pega detrás).

**Correcciones v28.1:** en el modo nodos no se ve la bolita del dedo; los tiradores Bézier se ven siempre (aunque queden detrás de una línea); cerca de cualquier botón de un menú (arriba, archivos, mano, buscador, tutoriales) la bolita y el borrador se esconden y no se dibuja ni se borra.

## 0b. Hecho en la v27

- [x] **Nodos con el índice**: tocas un nodo y se pega a tu dedo; lo sueltas abriendo la mano izquierda (pulgar + medio).
  - Respaldo de la versión con pellizco (v26): `trazo/Respaldos/nodos-con-pellizco-v26/` (con un LEEME de cómo volver a ella).
- [x] **Lazo**: empiezas en un lugar vacío, rodeas varios nodos (se ponen naranjas) y al tocar uno se mueven todos juntos.
- [x] **Plastilina**: índice + medio derechos juntos y estirados al tocar un nodo = los vecinos lo siguen con suavidad (se ven naranja claro).
- [x] **Nodo nuevo**: dejar el dedo quieto medio segundo sobre la línea elegida.
- [x] **Tutorial "3 · Nodos: lazo y plastilina"** en el panel "?", y el paso 7 del tutorial de presentación actualizado.
- [x] **Plano 2D**: el dedo puede pasar detrás de la hoja y la línea sigue; se ve una copia de tu mano (guante) sobre la hoja y un anillito naranja en la punta. Alejarlo hacia ti sigue levantando el lápiz.
- [x] **Manos de la simulación** (tutorial) más suaves y realistas: sin rayas de polígonos, con más carne en el dorso.
- [x] **Muñeco del tutorial** huyendo a 1.5 cm del dedo borrador.
- [x] **Fondo 360**: "Cuarto 360" (un cuarto real de prueba, de Poly Haven) y "Mi foto 360" (cualquier foto 360 que elijas). Se cambian con el botón **Fondo**.
- [x] **Imagen +** ahora abre un buscador con TODAS las imágenes del Quest (Descargas, Cámara, capturas, WhatsApp, Facebook…): tocas una, la ves en grande y decides **Importar** o **Fondo 360**. La primera vez el Quest pide permiso para ver tus fotos.

## 1. Próxima tanda

- [ ] **Voz y fondos**: "siguiente fondo" (sin abrir el panel) y el nombre de cada fondo ("Venecia", "Roma", "Nueva York"...).
- [ ] **"Lápiz" por voz = capa de lápiz + 2D de una vez** (en la capa actual); antes se veía la hoja pero seguía dibujando en 3D.
- [ ] **2D / 3D en cada capa**: dos botones redondos en la tarjeta de capas (uno activo por capa); se quita el botón Libre/Plano del reloj.
- [ ] **Borrar capas** (botón en la tarjeta).
- [ ] **Capas nuevas al hacer algo** (maniquí, bocas...): sin capas predeterminadas; se crea una nueva después de la actual.
- [ ] **Renombrar capas** (el número se mantiene).
- [ ] **Instrumento por voz**: "arpa", "piano", "drum and bass"...
- [ ] **Que al pasar entre capas no se toquen sus botones sin querer** (ver propuesta).

- [x] ~~**Pastillas que se mueven**~~ (hecho en la v41: paneles flexibles).
- [x] **Tooltips en todos los botones = las palabras de la voz** (hecho en la v42):
  - Todos los botones con etiquetita de vidrio al dejar el índice quieto cerca, como en la paleta: los del reloj, los de la mesa, la tarjeta de capas, el micrófono, el parlante, las pastillas y la flecha de girar.
  - Una sola lista de nombres: cada acción tiene UNA palabra (en el idioma de la app), la misma en el tooltip y en la voz.
  - Un microfonito 🎤 en los tooltips que también se pueden decir ("🎤 Capa nueva").
  - Igualar los textos de los botones a la palabra de voz ("+ Clave" → "Clave", "- Clave" → "Quita clave"...).
  - Comandos de voz nuevos para los botones que no los tienen, si la palabra es fácil de decir y Vosk la conoce ("boceto", "plano", "liberar"...). Las que no conoce (como "fps") quedan sin 🎤.
- [x] ~~Comandos de voz~~ (hechos en la v40 con Vosk, gratis y sin internet).
- [ ] **Voz, siguientes pasos**: ~~colores por nombre y más grueso / más fino~~ (hechos en la v42); falta decir un número de fotograma ("fotograma diez").
- [ ] **Mini-tutorial predictivo** (el panelito de la voz ya da el primer paso: dice qué decir según lo que haces) (más adelante, idea de Juan Carlos), para reemplazar las letras de cómic: un cuadrito a la **izquierda** de la vista que se adelanta a lo que vas a hacer. Por ejemplo: al unir el pulgar con el medio izquierdos (modo nodos) sale una manito simulada mostrando cómo mover un nodo con el índice; si pasas el dedo sobre un nodo, muestra que también se pueden mover sus alas (asas), con un ejemplo animado. Para los nuevos que no saben qué sigue o se pierden. Ideas para cuando se haga: que salga solo las primeras veces de cada gesto (luego se calla), que se pueda esconder con la vista limpia, y que use las mismas animaciones cortas de los tutoriales.

- [ ] **Profundidad con IA para tus propias fotos 360** (dentro del Quest, con Unity Inference Engine, gratis). Por ahora usan "piso real".
- [ ] Más fondos 360 temáticos (café de París, mesita en el andén, terraza al atardecer, sala de lujo): bájalos gratis de polyhaven.com (JPG 4K) y ponlos en **Plugins/Fondos** o elígelos con **Mis archivos → Imágenes → Traer imagen → Fondo 360**. Para la profundidad IA y la nitidez 4K: `trazo/Herramientas/profundidad360.py` y `nitidez360.py`.

- [ ] **Fondo verde (croma)** para grabar videos (pospuesto: "todavía no es urgente"). Se graba en el Quest con fondo verde y luego, en un editor gratis (CapCut o DaVinci Resolve), se pone encima de un video real de tu cuarto.

## 2. Propuestas esperando tu decisión (escribe HY para hacerlas)

- [x] **Fondos "vivos" hechos por nosotros** (hecho en la v42: Universo y Sobre las nubes; quedan como ideas el amanecer/atardecer y que el halo brille más en el universo) (idea de Juan Carlos; no son fotos, los genera la app, así que son 100 % nuestros y gratis):
  - **El universo**: miles de estrellas de verdad en 3D (cada ojo las ve distinto, con profundidad real), de colores y brillos distintos, una Vía Láctea y nebulosas suaves. Sin piso: flotas en medio del espacio. Opción **"Avanzar"**: las estrellas pasan despacio a tu lado (siempre la misma velocidad suave, para no marear).
  - **Sobre las nubes**: un mar de nubes abajo, el cielo degradado y un **sol** grande y cálido a lo lejos, con su brillo. Opción **"Volar"**: las nubes pasan despacio debajo de ti, como desde un avión.
  - Salen en la ventanita de fondos 360 con su miniatura; girar con las flechas también funciona. Pesan casi nada (no son fotos de 4K).
  - Ideas extra: amanecer o atardecer sobre las nubes; en el universo, que el halo de las líneas brille un poco más.

- [x] **Tutorial con voz** (hecho en la v42; los idiomas siguen bajándose la primera vez hasta la versión definitiva) (idea de Juan Carlos): en cada paso de un gesto de la mano izquierda, la viñeta dice también «o di "dibuja"». Propuesta:
  - Al empezar el tutorial, elegir **"Solo manos"** o **"Manos + voz"** (el permiso del micrófono se pide ahí, no a mitad de un paso).
  - Con voz: paso 1 «dibuja», paso 3 «ya», paso 5 «borra», paso 7 «nodos» (y «ya» para soltar), paso 8 «deshaz». En otro color (verde azulado, como el micrófono).
  - Los pasos del dedo derecho (dibujar hasta la bandera, mover el muñeco, frotar, mover un nodo) y el de las dos manos se quedan solo con la mano: la voz reemplaza a la mano IZQUIERDA; el índice derecho siempre dibuja y toca.
  - El tutorial ya revisa los modos (GestoIzq), y la voz ya los pone: buena parte funciona casi sola.
  - Mejor después de meter los idiomas dentro de la app (versión definitiva): así no hay que esperar la descarga en medio del tutorial.

- [x] **Persiana o mirada**: ganó la **persiana** (v39). Mirar arriba o abajo ya no abre nada.
- [x] **Fondo que sigue la cabeza**: solo en la **Sala de lujo** (v39), para probarlo. Si gusta, se puede usar en otros.
- [ ] **Burbujas o cristal**: por ahora se quedan las dos (burbujita secreta de la paleta).
- [ ] **Botones redondos de la paleta** (Línea, Relleno, sus hijos y la tinta invisible): ¿también sin aro negro, como los colores?
- [x] **Venecia, el puente curvo**: en la v38 la plaza de abajo baja poco a poco desde el puente (las escaleras ya no se tuercen).
- [ ] **Dar vida (con un soplo)**: se pasó a la **sección 8 → C. Movimiento automático** (con todos sus detalles).
- [ ] **Tutorial: tu primer personaje cobra vida**: al final del tutorial el usuario dibuja un círculo, dos puntos y una sonrisa, y la app le da vida. Como el tutorial pide el orden (cara, ojos, boca), nunca falla.
- [ ] **Tutorial: el logo se dibuja en tu cuarto**: al empezar, en passthrough, una línea mágica dibuja el logo JCartoons en el aire, al ritmo de la música, y el muñequito sale caminando.
- [ ] **Tutorial: tu primera película**: al terminar, un video automático de unos 10 segundos con lo que hiciste, con música y el título "Mi primera animación", listo para compartir.
- [ ] **Papel cebolla en 3D**: al animar, ver fantasmas del cuadro anterior (rojo) y del siguiente (verde) flotando en el aire.
- [ ] **Cámara de cine**: poner "cámaras" alrededor del dibujo con las manos y exportar un recorrido como video o GIF para WhatsApp o Instagram.
- [ ] **Modo Teatro**: reproducir una animación a tu alrededor, a escala real y sin menús, para caminar dentro de ella (como Quill Theater).

## 3. Para el final, si sobra tiempo

- [ ] **Pegado a tu cuarto** (anclajes espaciales de Meta): los dibujos se quedan pegados a tu pared o tu mesa y siguen ahí al volver a abrir la app.
- [x] ~~Comandos de voz~~ (hechos en la v40).
- [ ] **Muñequito acompañante** que te sigue mientras dibujas.
- [ ] **Transparencia del marcador** en la paleta de colores.
- [ ] **Violín real** como instrumento.
- [ ] **DS2** (el dubstep anterior): no quedó en la app. ¿Lo quieres? ¿En qué lugar del carrusel?

## 4. Pequeños arreglos conocidos

- [ ] Los textos de ayuda del panel de arriba todavía dicen que el temblor y las hebras están en la página Medios/Animar. Ahora están en la paleta de colores (`PanelArriba.cs`, ayuda de "ANIMAR" y de "CAPA 1-4").
- [ ] En el **video del proceso**, las líneas de color salen negras (pasaba desde antes). La tinta invisible sí se respeta.
- [ ] Ícono de Drum & Bass: se usó el **rayo**. Si prefieres las **gafas oscuras**, es un cambio rápido.
- [ ] Ahora que la paleta abre bien con la palma, ¿quitamos el botón **"Colores"** de debajo del menú? (Menos botones = menos "app 2D").
- [ ] Cuando ya no haga falta, quitar el botón **Tutorial** de Mis archivos (por ahora se queda; abre el mismo panel "?").

## 5. Para probar en el visor (desde la v21 casi nada se ha probado)

**Nuevo en la v42 (antes de probar: TrazoVR → ★ Armar escena, para los fondos vivos):**
- [ ] Etiquetas: ¿salen en todos los botones al acercar el dedo? ¿Estorban o salen sin querer? ¿Se lee el microfonito?
- [ ] Voz nueva: colores, "más grueso", "universo", "zoom"... ¿Las entiende? Di "inglés" / "spanish": ¿cambia el idioma y la voz se vuelve a preparar?
- [ ] Universo: ¿se ve bonito? ¿Las estrellas cercanas se sienten en 3D? "Avanzar": ¿marea o se siente bien?
- [ ] Sobre las nubes: ¿las nubes se ven bien desde arriba? ¿El sol? "Volar": ¿la velocidad está bien? ¿El visor va fluido?
- [ ] Flechitas de girar abajo: ¿se alcanzan? Toque = 30°; tocar y arrastrar = ¿sigue el dedo?
- [ ] Mesa: con el dedo encima ya no dibuja ni borra (ni las pestañas). Barra sujeta: ¿nunca dibuja?
- [ ] Tutorial con "Manos + voz": ¿salen las pistas en verde? ¿Pasa cada paso diciendo la palabra?
- [ ] Lápiz: Sí / No en la tarjeta de capas.

**Nuevo en la v41:**
- [ ] Pellizcar una pastilla: ¿se agarra a la primera? ¿a veces se abre el panel en vez de moverse (o al revés)?
- [ ] Llevarla arriba o abajo: ¿vuela bien a su sitio? ¿8 cm es la distancia justa para cambiar de orilla?
- [ ] Mesa arriba: ¿se ve bien con las pestañas y la página debajo? ¿20° encima de la mirada es cómodo? ¿el tirador de arriba se alcanza?
- [ ] Capas abajo: ¿se leen bien desde arriba? ¿los fantasmas en espejo se entienden?
- [ ] Las dos en la misma orilla: ¿lado a lado se ven bien? Abrir una y luego la otra: ¿se entiende que la segunda salte a la otra orilla?

**Nuevo en la v40:**
- [ ] **Voz**: la primera vez (con internet) ¿baja el idioma y dice "Te escucho"? ¿pide el permiso del micrófono? Después, sin internet, ¿sigue funcionando? ¿Entiende "dibuja", "ya", "borra", "deshaz", "gira izquierda"? ("gira izq" quizá no lo entienda: el modelo de Vosk no conoce "izq".) ¿Se equivoca y hace cosas sin que hables?
- [ ] El panelito de sugerencias a la izquierda: ¿se lee bien? ¿Estorba? ¿Las sugerencias tienen sentido?
- [ ] Girar con las flechas: ¿sale la flecha al girar la cabeza? ¿se alcanza? ¿el deslizar se siente natural (10 cm = 30°)? ¿sale alguna flecha sin querer?
- [ ] Halo: ¿ya no se ven cortadas las puntas con fondo 360 y passthrough?
- [ ] Línea de tiempo: puño + tocar una clave la quita; pellizco izquierdo + tocar la barra pone una clave. Barra sujeta con el doble toque: ¿se entiende? ¿se suelta bien?
- [ ] Uniones en esquina: une dos rectas, ¿quedan rectas? Prueba "Suaves" en la paleta.
- [ ] Parlante a 30° y micrófono a la izquierda: ¿se alcanzan bien?

**Nuevo en la v39:**
- [ ] Puntas de las líneas en los fondos 360 y en el passthrough: ¿ya no se cortan? ¿quedaron finas como antes? Mira en el reloj los cuadros por segundo: si dice "modo rendimiento 1", el visor iba cargado (pero ya no baja el detalle).
- [ ] Persianas: ¿todo bien sin lo de mirar arriba o abajo?
- [ ] Vista limpia: el doble puño derecho, ¿sale a la primera? ¿se dispara alguna vez sin querer? El botón del reloj y el botón Mesa.
- [ ] Girar con la mano derecha plana: ¿sale fácil? ¿se dispara sin querer al mover la mano?
- [ ] Sala de lujo que va contigo: ¿se ve recta? ¿marea?
- [ ] Venecia sin la ropa colgada. El parlante más a la derecha (¿25° está bien?).

**Nuevo en la v38:**
- [ ] Borrador: ¿el puño agarra a la primera, justo al empezar a borrar? ¿borra algo sin querer al cerrar la mano?
- [ ] La esfera de vidrio del borrador: ¿se ve bien el oro rosa (líneas) y la champaña (rellenos)?
- [ ] Puntas de las líneas en los fondos 360 y en el passthrough: ¿ya no se cortan? ¿te gustan redondas?
- [ ] Polvo oscuro en los fondos blancos: ¿se nota bonito?
- [ ] Persianas: ¿se jalan fácil? ¿la de abajo se alcanza bien? ¿la barrita de avance se entiende? Prueba el circulito: ¿cuál modo te gusta más?
- [ ] Mirar arriba (12°) y abajo (20°): ¿ahora es cómodo? ¿se abre algo sin querer?
- [ ] La mesa: ¿la altura ya está bien?
- [ ] Mover paneles con el pellizco: Mis archivos, la ventanita de fondos, el buscador y la mesa. ¿Algún pellizco que querías para otra cosa se lo come el panel?
- [ ] Etiquetas de la paleta: ¿salen a tiempo? ¿estorban?
- [ ] Girar: el antebrazo y las flechas del reloj. ¿El antebrazo se dispara solo alguna vez?
- [ ] Venecia, Nueva York y la Sala de lujo: ¿se ven bien de cerca y al mover la cabeza?
- [ ] La X de tus fotos 360.
- [ ] La mano de cómic más pequeña.

**Nuevo en la v37:**
- [ ] Paleta: ¿se ve "de lujo" sin los aros negros? ¿se distingue el centro del disco grande? ¿se ven bien las perlas (sobre todo el blanco y el gris claro)?
- [ ] Mesa de trabajo: ¿la altura está bien ahora (espejo de las capas)? ¿se puede mirar y tocar sin que se escape? ¿las páginas hacia arriba se entienden?
- [ ] Borrar nodos: ¿se ve bonito el polvo de los tramos?
- [ ] Venecia: ¿ya se siente que estás sobre el puente?

**Nuevo en la v36:**
- [ ] Vidrio vivo: ¿se ve el fondo borroso a través de los menús y la paleta? ¿se leen bien las letras? ¿algo se ve raro detrás del vidrio (líneas que desaparecen detrás del panel)?
- [ ] Luz en el dedo y manos de vidrio: ¿se nota la luz? ¿las manos de vidrio se ven bien o prefieres las de antes?
- [ ] Líneas: ¿el destello al terminar, el polvo al borrar y el rebobinado al deshacer se ven bonitos? ¿alguno molesta o se pone lento?
- [ ] Burbujita secreta: prueba con burbujas y sin burbujas. ¿Cuál te gusta más?
- [ ] Pastillita: ¿se lee? ¿estorba? ¿está a buena altura?
- [ ] Mirar arriba / abajo para las capas: ¿se abren sin querer? ¿el medio segundo está bien? ¿se cierran bien al bajar la mirada?
- [ ] Mesa de trabajo: ¿aparece cómoda al mirar abajo? ¿estorba cuando dibujas abajo? ¿te gusta que el reproductor y la línea de tiempo se queden? ¿respira bien (discreta / despierta)?
- [ ] Fondos: Shanghai de noche, Atardecer en el mar, Canal de Venecia y Sala de lujo: ¿se ven bien los retoques? ¿la profundidad?

**Nuevo en la v35:**
- [ ] Capas: ¿la distancia de 35 cm está bien? ¿se nota que todo el montón se mueve? ¿las de adelante ya no molestan cerca de los ojos? ¿el botón "+ Capa" se toca fácil?
- [ ] Tela con pellizco: ¿se agarra bien la tela al pellizcar? ¿se queda al abrir el pellizco? ¿funciona en figuras de lados rectos?
- [ ] Grosor: con un grosor elegido en el dial, agranda o achica todo y dibuja una línea nueva: ¿combina con las demás?

**Nuevo en la v34:**
- [ ] Capas en profundidad: ¿se entiende empujar y atraer? ¿la distancia entre capas (7 cm) y el tamaño están bien? ¿molesta que tapen la vista arriba?
- [ ] Puntos de tela: ¿se crean fácil sin crear nodos por error? ¿la campana se ve suave? ¿el borde se queda en la línea?
- [ ] Girar con el pulgar: ¿lo detecta bien con cada mano? ¿se dispara sin querer al hacer puño (borrador)?
- [ ] Lápiz Suave y Cuerda: ¿cuál te gusta más?
- [ ] Relleno: ¿ya no muerde la línea de frente, de espalda y de lado?
- [ ] Texturas: ¿ahora sí se notan? ¿demasiado?

**Nuevo en la v33:**
- [ ] Paleta: ¿se entienden LÍNEA y RELLENO con sus hijos? ¿se ve el arco? ¿se alcanzan bien los hijos de abajo?
- [ ] Centro: ¿se lee el textito? ¿se ve bien el circulito del relleno con su textura?
- [ ] Paleta estable: ¿ya no tiembla al acercar el dedo? ¿sigue bien a la mano cuando la mueves de verdad?
- [ ] Textura con dos dedos: ¿arranca fácil? ¿no se confunde con tocar dentro para rellenar?
- [ ] Recta de lápiz en boceto 2D: guía gris, grafito al soltar, presión.
- [ ] Dibujar cerca de rellenos: ¿ya no cambian de color solos?

**Nuevo en la v32:**
- [ ] Ventanita de fondos 360: ¿se ven bien las miniaturas? ¿cómoda al lado del menú?
- [ ] Shanghai de noche y los 6 nuevos: ¿nítidos? ¿profundidad bien?
- [ ] Papel blanco del boceto: ¿tamaño adecuado? ¿el botón de la esquina se alcanza?
- [ ] Imán en la paleta y el botón de tamaño del relleno vivo.
- [ ] Proyectos en Descargas: ¿se movieron los de antes? ¿se ven en la app Archivos del Quest? Versiones.
- [ ] Modo rendimiento: ¿aparece el aviso? ¿cuántos fps marca con dibujos grandes?
- [ ] Tutorial 4 · Novedades.

**Nuevo en la v31:**
- [ ] Encantamiento: dibuja en 2D, pasa a 3D y mueve tiradores, nodos y la línea: ¿se quedan en su hoja? ¿Liberar funciona (todas / solo las elegidas)?
- [ ] Frente/fondo: ¿el empujón de 3 cm se activa sin querer al mover? ¿se ve bien quién tapa a quién (también de lejos)?
- [ ] Menú archivador: ¿se entiende? ¿el alfiler?
- [ ] Mis archivos: acciones sobre la miniatura (¿se presiona algo sin querer?), deslizar para cambiar de página, Compartir, Grabar y el "● tiempo" del menú de la mano.
- [ ] Relleno vivo: ¿se parece a Quill? ¿tamaño de las manchas? ¿baja la fluidez con rellenos grandes?
- [ ] Datos bajo la paleta: ¿se leen bien? ¿estorban?

**Nuevo en la v30:**
- [ ] Los 8 fondos nuevos (¿se ven nítidos? ¿la profundidad se siente bien? La Luna: ¿el cielo negro y la Tierra lejos?).
- [ ] Halo: ¿se separan las líneas del fondo? ¿molesta el borde? ¿se ve bien en la Realidad (passthrough)? ¿baja la fluidez con muchas líneas?
- [ ] Mis archivos: las 4 pestañas; Foto y SVG desde Crear; Grabar proceso (Detener y Video proceso); Traer imagen.
- [ ] Menú de arriba: Boceto y Plano en cada fila de Capas; Zoom en el reproductor.
- [ ] Tiradores: V = un solo lado; bolita hasta el nodo = esquina recta.
- [ ] Cartel de cómic arriba al centro: ¿tapa algo? ¿se lee bien? ¿choca con el parlante?
- [ ] Autoguardado: dibuja algo, espera ~35 s, haz Build y revisa que el dibujo vuelva. ¿Se nota algún tirón al guardar?

**Nuevo en la v28:**
- [ ] Fondos 360 en 3D: ¿se siente la profundidad? ¿el piso de la foto coincide con tu piso? ¿se estiran mucho los bordes al moverte?
- [ ] Mano copia en el plano 2D (tu misma mano) y la mano atrapada en la bandera del tutorial.
- [ ] Tiradores al acercar el dedo; plastilina con dos dedos (juntos o en V).
- [ ] Sacar imágenes del buscador con el pellizco.

**Nuevo en la v27:**
- [ ] Nodos: tocar, seguir el dedo y soltar abriendo la izquierda. ¿Se agarran nodos sin querer?
- [ ] Lazo (que no se active al mover un nodo) y plastilina (índice + medio).
- [ ] Plano 2D: pasar el dedo detrás de la hoja (copia de la mano + anillito).
- [ ] Fondo: los 360 (¿se ve nítido? ¿hay una rayita en la unión de la foto?).
- [ ] Traer imagen (Mis archivos → Imágenes): la ventana de permiso, las carpetas, las miniaturas, Importar y Fondo 360.
- [ ] Manos del tutorial: ¿se ven bien la palma, el dorso y el pulgar?

**De antes:**

- [ ] **Paleta en la palma**: abre fácil; botón "Colores" debajo del menú.
- [ ] **Temblor dentro de la paleta**: la línea del centro y los 5 botoncitos de abajo.
- [ ] **Carrusel de instrumentos sobre el parlante**: arrastrar, tocar los de los lados, tamaño (18 % más chico), desaparece en silencio, la bolita del dedo se esconde cerca.
- [ ] **Sonidos**: los 9 instrumentos. El piano y la orquesta se preparan en segundo plano: ¿hay algún tirón o retraso?
- [ ] **Batería**: para en el siguiente golpe al soltar o al dejar la mano quieta.
- [ ] **Notas al agrandar o achicar** con las dos manos.
- [ ] **Rellenos**: cubeta en vivo, tocar dentro para rellenar, tinta invisible (y que se vea gris clarito al editar).
- [ ] **Panel "?"** y el **tutorial de Rellenos**.
- [ ] **Passthrough** al abrir la app por primera vez.
- [ ] **Títere**: la raya punteada del índice al muñeco.
- [ ] **Tutorial**: la mano del OK se queda; el OK ya hecho cuenta en el paso 2; marimba y piano; un solo número arriba al centro; curva con subidas y bajadas; giro del puño con flechas amarillas; más tiempo en el paso 9.

## 6. Concurso Meta (VR Start Developer Competition 2026)

- **Categoría:** Productividad · División: Experiencia nueva.
- **Fecha límite: 18 de noviembre de 2026.**
- [ ] Grabar el video del concurso.
- [ ] Enviar la app.

**Para la versión definitiva (antes de enviarla):**
- [ ] **Voz dentro de la app**: meter los dos idiomas de Vosk (español e inglés, unos 40 MB cada uno) dentro de la app, para que la voz funcione desde el primer toque, sin internet y sin depender de la página de Vosk. *(Decidido: no ahora, porque mientras probamos haría cada Build más lento. Se hace al final.)*

**Cómo grabar con el Quest 2** (las grabaciones del Quest no capturan el passthrough):
1. **Foto 360 de tu cuarto** como fondo (ya está en la v27): toma una foto 360 de tu cuarto (con una app gratis de fotos 360 en el celular; tiene que ser el doble de ancha que de alta), pásala al Quest y elígela en **Mis archivos → Imágenes → Traer imagen → Fondo 360**.
2. **Fondo verde + video real de tu cuarto**: lo más realista, para tomas en primera persona.
3. **Mixed Reality Capture** (gratis, con PC + OBS + cámara o celular): te filma a ti dibujando en tu cuarto, en tercera persona. Ideal para el tráiler.

**Mensaje recomendado:** *"Quill es para profesionales con controles; JCartoons es para todos, con las manos."*

## 7. A largo plazo: reemplazar a Quill

**Crear** (igualar a Quill):
- [ ] Pinceles de pintura: trazos anchos, con textura, degradados y transparencia.
- [ ] Línea de tiempo profesional: cuadro por cuadro, ciclos y varias animaciones a distinto ritmo.
- [ ] Grupos con jerarquía (el brazo mueve la mano, la mano mueve los dedos).
- [ ] Escenas, cámara del espectador, cortes y sonido por escena.
- [ ] Exportar a formatos estándar (glTF, video 360).

**Ver:**
- [ ] Modo Teatro dentro de la app.
- [ ] Visor web (WebXR): ver una animación desde un enlace en el navegador (Quest, celular o computador) sin instalar nada.
- [ ] Una app aparte, gratis y liviana, solo para ver.

**Compartir:**
- [ ] Etapa A (gratis): **galería curada** por ti, con los archivos guardados en GitHub.
- [ ] Etapa B: biblioteca abierta con cuentas, moderación, reportes y bloqueos (Meta lo exige). Al crecer cuesta dinero, y ahí entraría un modelo de negocio (versión Pro o apoyo de la comunidad).
- [ ] Colaborar en vivo (dos personas dibujando juntas).

**Orden sugerido:** Teatro → pinceles, línea de tiempo y grupos → galería curada y visor web → app solo para ver y biblioteca abierta → exportar y colaborar.

**Para animar y bocetar** (largo plazo, después del concurso):
- [ ] **Modo stop motion**: modelar personajes con la plastilina y animarlos cuadro a cuadro (como Wallace & Gromit), con papel cebolla para ver el cuadro anterior. Idea de Juan Carlos desde antes.
- [ ] **Maniquí para bocetos**: un muñeco 3D (como el muñeco guía) que pones en pose con las manos, al lado o de fondo, como modelo mientras bocetas. Opcional: poses cronometradas para practicar figura humana.

**Otra app aparte (más adelante, aún no): DJ + VJ con las manos**
- [ ] Hacer música electrónica de verdad con movimientos, líneas y figuras: un instrumento y un DJ set a la vez (bucles, capas, mezclas).
- [ ] Gestos predefinidos que crean figuras que se mueven al ritmo (VJ, para video jockeys).
- [ ] Verlo en una pantalla externa para fiestas reales: lo más simple es transmitir el Quest a un televisor (gratis); una "vista para el público" distinta a la tuya necesitaría un computador que la reciba por wifi.
- [ ] Sonido a parlantes de fiesta por cable (la salida de audífonos o el USB-C del Quest), porque el Bluetooth llega con retraso.
- Se puede reusar de JCartoons: el sintetizador de sonidos, las escalas que siempre suenan bonito, los ritmos y las líneas vivas.

## 8. Herramientas automáticas: nuestro camino hacia "lo que hace la IA"

**La idea:** la IA generativa fabrica **píxeles**; JCartoons trabaja con **líneas vivas** (nodos, curvas, capas y 3D). Nuestro camino son herramientas que **entienden los trazos** y ayudan al artista: el resultado siempre se puede editar, animar y mover en 3D, y sigue siendo tuyo.

**Reglas del camino**
- Todo **gratis**.
- Primero **algoritmos propios** (nivel 1): instantáneos, sin internet y sin licencias raras.
- Después, **IA pequeña dentro del visor** (nivel 2), con modelos abiertos.
- La **IA grande** solo como opción en tu PC (nivel 3), **nunca** como base de la app.
- **Respeto a los artistas**: nada entrenado con trabajo ajeno sin permiso. Lo ideal: que la app aprenda de **tus** dibujos.

**Orden recomendado**
1. Calcar (imagen → vector).
2. Dar vida (movimientos para cualquier dibujo).
3. Inflar y Torno (2D → 3D).
4. Pasar a limpio (boceto → tinta).
5. Cubeta entre varias líneas.
6. Esqueleto automático.
7. Después, el nivel 2 (IA pequeña).

### Nivel 1 · Algoritmos propios (sin IA)

**A. De imagen a dibujo**
- [ ] **Calcar** ★ *el primero*: importas una imagen y aparece una **capa nueva** con líneas y rellenos editables.
  - Cómo: reduce la imagen a pocos colores (2 a 8), encuentra los contornos y los convierte en curvas con nodos y rellenos.
  - Opciones: cuántos colores, nivel de detalle, o **solo contornos** (para colorear tú).
  - Es como el "Calco de imagen" de Illustrator (que no es IA). Lo escribimos nosotros (el programa libre conocido, Potrace, tiene una licencia que nos obligaría a abrir todo el código).
  - Tamaño: 1 o 2 tandas.
- [ ] **Línea imán sobre la imagen**: mientras dibujas encima de una imagen, la línea se pega sola a los bordes cercanos (como el lazo magnético de Photoshop).

**B. Del boceto a la tinta**
- [ ] **Pasar a limpio**: los trazos de lápiz que se enciman se agrupan y se vuelven **una sola línea limpia** de tinta, en otra capa (el boceto queda debajo).
- [ ] **Cubeta entre varias líneas**: rellenar una zona encerrada por **varias** líneas que se cruzan (hoy la cubeta necesita una sola línea cerrada). Los huecos pequeñitos se cierran solos.

**C. Movimiento automático**
- [ ] **Dar vida (con un soplo)**: cualquier dibujo cobra vida sin animarlo.
  - Movimientos listos para **cualquier** dibujo: respirar, balancearse, rebotar, saludar, temblar de miedo, saltar de alegría (una "jaula" invisible deforma el dibujo).
  - Parpadear y mover la boca: la app **adivina** los ojos con reglas de forma (formas pequeñas cerradas, en pareja y arriba dentro de una forma grande) y la boca (una curva debajo).
  - Si adivina mal, tú marcas una línea como **"Ojo"** o **"Boca"** (igual que en el títere se marcan las piernas).
- [ ] **Esqueleto automático**: la app calcula los huesos desde la forma del dibujo (su "eje del medio") y cualquier personaje puede caminar y saltar, sin marcar piernas. Base: el títere.
- [ ] **Física de línea**: pelo, colas y orejas que rebotan como gelatina.
- [ ] **Dibujos intermedios automáticos** entre dos poses (la base ya existe en la animación).
- [ ] **Efectos que se dibujan solos**: lluvia, fuego, chispas, humo, burbujas.
- [ ] **Multitud**: duplicar un personaje con pequeñas diferencias.
- [ ] **Cámara de cine automática**: recorridos de cámara listos (acercarse, girar alrededor, paneo).

**D. De 2D a 3D**
- [ ] **Inflar**: una figura rellena se infla como globo y gana volumen (ideas públicas de *Teddy* y *Monster Mash* de Google). Sigue el camino de los puntos de tela de la v35.
- [ ] **Extruir**: darle grosor a una figura plana, como una galleta.
- [ ] **Torno**: dibujas medio perfil y la app lo gira para hacer un jarrón, una botella o un árbol.
- [ ] *Largo plazo*: **dos vistas → 3D**: con un dibujo de frente y otro de lado, la app calcula las líneas en 3D.

### Nivel 2 · IA pequeña dentro del visor (Unity Inference Engine, gratis)
*Límite: el Quest 2 es modesto. Los modelos deben ser pequeños, y algunos tardarían unos segundos en vez de ser instantáneos.*
- [ ] **Reconocer garabatos**: un modelo entrenado con los datos abiertos de *Quick Draw* de Google. La app sabe que dibujaste un gato y te ofrece orejas, cola o una versión limpia de tu biblioteca.
- [ ] **Recortar al personaje** de una foto antes de calcarlo (así el vector sale limpio, sin el fondo).
- [ ] **Profundidad de una imagen importada** para darle relieve (hoy los fondos 360 la calculan en tu PC; ver también la sección 1).
- [ ] **Poses desde un video**: un personaje copia los movimientos de una persona grabada (modelos abiertos de pose).
- [ ] **Tu propia IA** (idea propia): la app aprende de **tus** dibujos (tus ojos, tus manos, tu estilo) y te los ofrece mientras dibujas. Personal, ética y única.

### Nivel 3 · IA grande, opcional, en tu PC (open source)
*Necesita una PC con buena tarjeta de video. Hay que revisar la licencia de cada modelo (algunos no permiten vender lo que hagas).*
- [ ] **Boceto → imagen** (ControlNet "scribble", en tu PC con ComfyUI) y luego **Calcar** la vuelve líneas editables.
- [ ] **Imagen → modelo 3D** con modelos abiertos.
- [ ] La nube, solo como **opción de pago**, nunca como base de la app.

### Ya existe
- [x] Títere que camina y salta, lipsync, música automática y temblor de línea.
- [x] Profundidad IA para los fondos 360 (en tu PC: `trazo/Herramientas/profundidad360.py`).
- [x] Puntos de tela (relieve en el relleno, v35).

## 9. Forma de trabajo (para cualquier sesión futura)

- Responder **siempre en español**, claro y con pasos numerados (Juan Carlos está empezando en VR).
- **"HY"** = *Hazlo Ya*: no cambiar código hasta recibirlo (tampoco los arreglos).
- **"rcpt"** = responder corto (solo en el mensaje que lo trae).
- **Todo gratis.** **Manos primero** (sin controles). Menos botones "de app 2D".
- Todos los cambios juntos en una sola tanda (cada compilación tarda).
- Trabajar solo en la rama `claude/trazo-vr`. No tocar la carpeta `unity/` ni la rama de la farmacia.
- No hay compilador de Unity en la nube: revisar el código con mucho cuidado antes de subirlo.
- Sonidos: probarlos primero en el simulador y pasarlos a la app cuando estén aprobados (las fórmulas son las mismas en `Sonidos.cs` y en el simulador).
