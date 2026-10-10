# JCartoons — Pendientes y recomendaciones

*Actualizado: 10 de octubre de 2026 · Versión actual en la rama `claude/trazo-vr`: **v30***

Este documento reúne todo lo que falta por hacer y las recomendaciones para el futuro. Las casillas `[ ]` se marcan `[x]` cuando algo queda hecho.

**Enlaces útiles**
- Descarga del proyecto: https://github.com/juancartoons/hello-world/archive/refs/heads/claude/trazo-vr.zip
- Simulador de sonidos (para probar en la computadora): https://claude.ai/artifact/4irW6KNFZ6bB38WRQ9CpbB (y en el proyecto: `trazo/Herramientas/simulador-sonidos.html`)

**Cómo instalar cada versión en Unity**
1. Copia las carpetas **Scripts**, **Shaders** y **Plugins** a tu proyecto.
2. Menú **TrazoVR → ★ Armar escena** (siempre que la versión agregue botones o materiales).
3. **Ctrl+S** para guardar la escena.
4. **Build And Run**.

---

## 0. Hecho en la v30 (falta probarlo en el visor)

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

- [ ] **Dar vida (con un soplo)**: cualquier dibujo cobra vida sin animarlo.
  - Respirar y balancearse funciona con **cualquier** dibujo.
  - Parpadear y mover la boca: la app **adivina** los ojos con reglas de forma (formas pequeñas cerradas, en pareja y arriba dentro de una forma grande) y la boca (una curva debajo).
  - Si adivina mal, tú marcas una línea como **"Ojo"** o **"Boca"** (igual que en el títere se marcan las piernas).
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

## 8. IA y "trucos que parecen IA" (sin granjas de servidores)

**Recomendación:** primero los trucos, que son gratis, instantáneos, sin internet y respetuosos con los artistas: *"tú dibujas y la app le da vida"*.

- [ ] **Reconocer garabatos dentro del visor**: Unity Inference Engine (antes "Sentis", gratis) con un modelo entrenado con los datos abiertos de *Quick Draw* de Google. La app sabe que dibujaste un gato y te ofrece orejas, cola o una versión limpia de tu biblioteca.
- [ ] Física de línea: pelo, colas y orejas que rebotan como gelatina.
- [ ] Dibujos intermedios automáticos entre dos poses (la base ya existe en la animación).
- [ ] Efectos que se dibujan solos: lluvia, fuego, chispas, humo, burbujas.
- [ ] Multitud: duplicar un personaje con pequeñas diferencias.
- [ ] Cámara de cine automática.
- [x] Ya existen: títere que camina y salta, lipsync, música automática y temblor de línea.
- [ ] Más adelante: IA en tu propio PC (modelos abiertos) o en la nube **como opción de pago**, nunca como base de la app.

## 9. Forma de trabajo (para cualquier sesión futura)

- Responder **siempre en español**, claro y con pasos numerados (Juan Carlos está empezando en VR).
- **"HY"** = *Hazlo Ya*: no cambiar código hasta recibirlo (tampoco los arreglos).
- **"rcpt"** = responder corto (solo en el mensaje que lo trae).
- **Todo gratis.** **Manos primero** (sin controles). Menos botones "de app 2D".
- Todos los cambios juntos en una sola tanda (cada compilación tarda).
- Trabajar solo en la rama `claude/trazo-vr`. No tocar la carpeta `unity/` ni la rama de la farmacia.
- No hay compilador de Unity en la nube: revisar el código con mucho cuidado antes de subirlo.
- Sonidos: probarlos primero en el simulador y pasarlos a la app cuando estén aprobados (las fórmulas son las mismas en `Sonidos.cs` y en el simulador).
