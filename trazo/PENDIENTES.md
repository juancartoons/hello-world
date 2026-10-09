# JCartoons — Pendientes y recomendaciones

*Actualizado: 9 de octubre de 2026 · Versión actual en la rama `claude/trazo-vr`: **v26***

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

## 1. Próxima tanda (ya aprobado)

- [ ] **Fondo verde (croma)** para grabar videos: se graba en el Quest con fondo verde y luego, en un editor gratis (CapCut o DaVinci Resolve), se pone encima de un video real de tu cuarto. Así se ve como el passthrough de un Quest 3.
- [ ] **Fondo "Mi cuarto" (foto 360)**: una foto panorámica 360° de tu cuarto, tomada con el celular, que la app muestra alrededor tuyo como si fuera el passthrough. Sirve para grabar con el Quest 2.

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
1. **Foto 360 de tu cuarto** como fondo: rápido y fácil (pendiente en la próxima tanda).
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
- **"HY"** = *Hazlo Ya*: no cambiar código hasta recibirlo, salvo arreglos directos.
- **"rcpt"** = responder corto (solo en el mensaje que lo trae).
- **Todo gratis.** **Manos primero** (sin controles). Menos botones "de app 2D".
- Todos los cambios juntos en una sola tanda (cada compilación tarda).
- Trabajar solo en la rama `claude/trazo-vr`. No tocar la carpeta `unity/` ni la rama de la farmacia.
- No hay compilador de Unity en la nube: revisar el código con mucho cuidado antes de subirlo.
- Sonidos: probarlos primero en el simulador y pasarlos a la app cuando estén aprobados (las fórmulas son las mismas en `Sonidos.cs` y en el simulador).
