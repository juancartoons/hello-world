# JCartoons — Pendientes y recomendaciones

*Actualizado: 11 de octubre de 2026 · Versión actual en la rama `claude/trazo-vr`: **v35***

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

## 0. Hecho en la v35 (falta probarlo en el visor)

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

- [ ] **Profundidad con IA para tus propias fotos 360** (dentro del Quest, con Unity Inference Engine, gratis). Por ahora usan "piso real".
- [ ] Más fondos 360 temáticos (café de París, mesita en el andén, terraza al atardecer, sala de lujo): bájalos gratis de polyhaven.com (JPG 4K) y ponlos en **Plugins/Fondos** o elígelos con **Mis archivos → Imágenes → Traer imagen → Fondo 360**. Para la profundidad IA y la nitidez 4K: `trazo/Herramientas/profundidad360.py` y `nitidez360.py`.

- [ ] **Fondo verde (croma)** para grabar videos (pospuesto: "todavía no es urgente"). Se graba en el Quest con fondo verde y luego, en un editor gratis (CapCut o DaVinci Resolve), se pone encima de un video real de tu cuarto.

## 2. Propuestas esperando tu decisión (escribe HY para hacerlas)

- [ ] **Dar vida (con un soplo)**: se pasó a la **sección 8 → C. Movimiento automático** (con todos sus detalles).
- [ ] **Tutorial: tu primer personaje cobra vida**: al final del tutorial el usuario dibuja un círculo, dos puntos y una sonrisa, y la app le da vida. Como el tutorial pide el orden (cara, ojos, boca), nunca falla.
- [ ] **Tutorial: el logo se dibuja en tu cuarto**: al empezar, en passthrough, una línea mágica dibuja el logo JCartoons en el aire, al ritmo de la música, y el muñequito sale caminando.
- [ ] **Tutorial: tu primera película**: al terminar, un video automático de unos 10 segundos con lo que hiciste, con música y el título "Mi primera animación", listo para compartir.
- [ ] **Papel cebolla en 3D**: al animar, ver fantasmas del cuadro anterior (rojo) y del siguiente (verde) flotando en el aire.
- [ ] **Cámara de cine**: poner "cámaras" alrededor del dibujo con las manos y exportar un recorrido como video o GIF para WhatsApp o Instagram.
- [ ] **Modo Teatro**: reproducir una animación a tu alrededor, a escala real y sin menús, para caminar dentro de ella (como Quill Theater).

## 3. Para el final, si sobra tiempo

- [ ] **Pegado a tu cuarto** (anclajes espaciales de Meta): los dibujos se quedan pegados a tu pared o tu mesa y siguen ahí al volver a abrir la app.
- [ ] **Comandos de voz** (Meta Voice SDK / Wit.ai, gratis) como atajos.
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
