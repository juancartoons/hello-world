# Brazos con escorzo en MacroFlash — "brazo invisible 3D"

Documento técnico para la sesión que desarrolla **MacroFlash.html**. Resume lo que aprendimos en JCartoons (app VR) con la
**cabeza invisible** y cómo aplicarlo a los **brazos** (personaje de traje y corbata), para resolver:

1. **Escorzos**: el brazo apunta hacia la cámara o se aleja, y se ve más corto.
2. **Cruces**: el antebrazo pasa por delante (o por detrás) del brazo. Por ejemplo, del brazo extendido a los brazos
   arriba, pasando por el momento en que el antebrazo apunta a la cámara. Ahí hoy se forma un "lío" de líneas.

Nada de esto necesita IA: es geometría y unas reglas. Todo cabe en el sistema actual (`HU`, `mfHuesos`, claves, capas,
correcciones a mano) y se puede guardar como XFL que Animate entiende.

---

## 0. Diagnóstico del sistema actual (por qué se hace el lío)

Hoy un rig (`crearRig`) deforma **una sola forma** (`DOMShape`) con una cadena de articulaciones `J` y pesos `W`
(`HU.pesos`, con una zona de mezcla `ancho = 0.18` cerca de cada articulación). `HU.deformar` hace un *linear blend
skinning* 2D: cada punto se mueve con su hueso, a lo largo (`u·L`) y de lado (`q·G`).

Esto tiene tres límites cuando hay escorzo:

| Problema | Por qué pasa |
|---|---|
| **El contorno se cruza consigo mismo** | Brazo y antebrazo son UN contorno cerrado. Si el antebrazo pasa por encima del brazo, el contorno se cruza y la regla de relleno (par/impar) hace huecos y rayas. |
| **El codo se "derrite"** | En la zona de mezcla, los puntos del lado de adentro de los dos huesos se promedian: se aplastan, se pellizcan o se dan vuelta. |
| **El escorzo solo acorta** | Al acercarse `J[b]` y `J[b+1]`, el hueso se acorta (`u·L`), pero las líneas que van ALREDEDOR del brazo (puño de la manga, dobleces) siguen rectas, cuando en 3D deberían volverse elipses. |

La cabeza invisible de JCartoons resolvió los mismos tres problemas en la cabeza. La traducción al brazo es directa.

---

## 1. Lo que aprendimos con la cabeza (las 6 ideas que se reutilizan)

1. **Un volumen invisible**: la cabeza es un huevo 3D que no se ve. Cada nodo del dibujo se "pega" a su superficie
   con coordenadas propias del volumen, no de la pantalla. Al girar, se gira el volumen y se proyecta.
2. **Sin girar, el dibujo queda idéntico**: en la vista dibujada, `x, y` de cada nodo no cambian; solo se inventa la
   profundidad. Así el original nunca se arruina.
3. **Los nodos del BORDE van a la silueta**: los nodos que forman el contorno no se pegan a la superficie; se recolocan
   sobre la silueta del volumen girado, en su mismo "ángulo". Los del interior (ojos, boca) sí van pegados.
4. **Lo que queda atrás se pega al vecino visible**: un nodo interior que queda detrás del volumen se coloca encima del
   nodo visible más cercano de su misma línea. Así la línea no desaparece ni cruza: se "esconde" sin cambiar el número
   de nodos, y el morph sigue funcionando.
5. **Partes con nombre, no adivinadas**: cada nodo sabe qué es ("borde", "oreja", "nariz"...). Las partes distintas
   NO se unen en una misma línea. Esto fue la clave para que la oreja no arrastrara al contorno.
6. **Silueta por piezas sin huecos**: las piezas del borde se estiran hasta la mitad del hueco que las separa, así la
   silueta siempre queda cerrada.

En el brazo: el volumen es una **cápsula por hueso** (un cilindro con media esfera en cada punta), y las "partes" son
**manga del brazo, manga del antebrazo, puño de la camisa y mano**.

---

## 2. El modelo: cápsulas invisibles

### 2.1 Datos nuevos (compatibles con lo actual)
En el JSON de `mfHuesos` de cada clave se agrega, sin romper nada:

```js
// mfHuesos = { J: [[x,y],...], G: [[g0,g1],...],   // lo de siempre
//              Z: [z0, z1, ...],                    // NUEVO: profundidad de cada articulación (+ = hacia la cámara)
//              S: [s0, s1, ...] }                   // NUEVO (opcional): signo elegido del escorzo de cada hueso (+1 / -1)
```

En el rig (`r`) se guarda una vez:

```js
r.R0 = [r0, r1, ...];   // radio de cada hueso en reposo (la mitad del ancho de la manga, medido en el dibujo)
r.L0 = [L0, L1, ...];   // largo real de cada hueso en reposo (= |J0[b+1] - J0[b]|)
r.piezas = [...];       // NUEVO: qué hueso manda cada pieza (ver 3)
r.cls = [...];          // NUEVO: clase de cada nodo de cada pieza: 'borde' | 'anillo' | 'detalle'
```

`R0` se puede medir solo: es la distancia promedio de los nodos de borde a su hueso.

### 2.2 La profundidad sale sola del escorzo
El largo real de un hueso no cambia. Si en pantalla se ve más corto, es porque apunta hacia la cámara (o hacia atrás):

```js
function profundidades(r, J, S) {            // S[b] = +1 (hacia ti) o -1 (hacia atrás)
  const Z = [0];
  for (let b = 0; b < J.length - 1; b++) {
    const L2d = Math.hypot(J[b+1][0] - J[b][0], J[b+1][1] - J[b][1]);
    const dz = Math.sqrt(Math.max(0, r.L0[b] ** 2 - L2d ** 2));   // Pitágoras
    Z.push(Z[b] + (S[b] || 1) * dz);
  }
  return Z;
}
```

- El animador **solo mueve las articulaciones en 2D**, como hoy. La profundidad se calcula sola.
- La única ambigüedad es el **signo** (¿hacia la cámara o hacia atrás?). Se resuelve así:
  1. Por **continuidad**: se usa el mismo signo que la clave anterior.
  2. Cuando el hueso pasa por "largo completo" (`dz ≈ 0`), el signo puede cambiar. Ahí se mira la clave siguiente.
  3. Un botón por hueso (**"hacia mí / hacia atrás"**) para forzarlo. Se guarda en `S`.
- Si el animador estira un hueso más allá de `L0`, no hay escorzo (`dz = 0`) y se permite el estiramiento cartoon de
  siempre (`G`).

### 2.3 Cada nodo, sobre su cápsula
Igual que en el huevo, cada nodo se describe en **coordenadas del volumen**, calculadas una vez en reposo (`J0`, `P0`):

```js
// u: dónde cae a lo largo del hueso (0 = articulación de arriba, 1 = la de abajo)
// s: de qué lado está, de -1 a 1 (distancia con signo al eje / radio)  →  ángulo alrededor: θ = asin(s)
// (θ = 0 es el frente que mira a la cámara; ±90° es la silueta)
function coordsCapsula(P0, J0, R0, hueso) {
  return P0.map(p => {
    const a = J0[hueso], b = J0[hueso + 1], L = Math.hypot(b[0]-a[0], b[1]-a[1]);
    const d = [(b[0]-a[0])/L, (b[1]-a[1])/L], n = [-d[1], d[0]];
    const u = ((p[0]-a[0])*d[0] + (p[1]-a[1])*d[1]) / L;
    const s = Math.max(-1, Math.min(1, ((p[0]-a[0])*n[0] + (p[1]-a[1])*n[1]) / R0[hueso]));
    return { u, s, th: Math.asin(s) };
  });
}
```

Para colocar el nodo en una pose, se arma el cilindro en 3D y se proyecta (vista ortográfica, como el escenario 2D):

```js
// A, B: articulaciones en 3D [x, y, z] · R: radio · (u, th): coords del nodo
function puntoEnCapsula(A, B, R, u, th) {
  const D = sub3(B, A), L = len3(D), d = scale3(D, 1 / (L || 1e-9));
  // un "lado" perpendicular al hueso que quede en el plano de la pantalla (para que s=±1 siga siendo la silueta)
  let n = norm3(cross3(d, [0, 0, 1]));            // si el hueso apunta justo a la cámara, usar el n de la clave anterior
  const v = cross3(n, d);                          // tercer eje: el "frente" del cilindro (hacia la cámara)
  const c = add3(A, scale3(D, u));
  const p = add3(c, add3(scale3(n, R * Math.sin(th)), scale3(v, R * Math.cos(th))));
  return [p[0], p[1], p[2]];                        // pantalla = (x, y); z sirve para saber qué tapa a qué
}
```

**Sin escorzo (`Z` = 0 en todo el hueso), esto da exactamente lo mismo que `HU.deformar`** (el punto 2 de la sección 1):
`u·L` a lo largo y `s·R` de lado. Por eso se puede activar sin miedo: las claves sin escorzo no cambian.

---

## 3. La clave contra el "lío": PIEZAS separadas con orden de profundidad

### 3.1 Partir el brazo en piezas (una vez, al poner los huesos)
En lugar de un solo `DOMShape` para todo el brazo:

| Pieza | Hueso que manda | Contenido |
|---|---|---|
| **Manga del brazo** | hombro → codo | contorno de la manga hasta un poco después del codo |
| **Manga del antebrazo** | codo → muñeca | contorno del antebrazo, con su comienzo **redondeado** (media luna) |
| **Puño de camisa** | antebrazo (u ≈ 1) | el anillo blanco que asoma |
| **Mano** | muñeca → nudillos | (ya existe como pieza o símbolo) |

- **Todas las piezas comparten el MISMO rig**: un solo `J` (más `Z`) por clave mueve las cuatro piezas. Para el
  animador sigue siendo "un brazo con huesos".
- Cada pieza tiene peso **1 a su propio hueso** y solo mezcla en SU punta (no comparte zona de mezcla con la otra
  pieza). Así ya no hay promedio que derrita el codo.
- **La rótula (codo)**: el comienzo del antebrazo se dibuja como media circunferencia de radio `R0` alrededor de la
  articulación (una "rótula"). Al doblar, esa media luna siempre cubre la unión, en cualquier ángulo y cualquier
  escorzo. Es el truco clásico del cut-out, pero con la forma recalculada en cada pose.

Para partir: la sesión de MacroFlash ya tiene herramientas para cortar y unir formas. Basta con que `crearRig`
ofrezca **"Brazo en piezas"**: corta el contorno con dos líneas perpendiculares al hueso, una a cada lado del codo
(a ±0.5·R0), y cierra cada mitad. Así el brazo de arriba y el antebrazo se solapan un poquito y nunca queda rendija.

### 3.2 El orden se decide por profundidad, en cada clave
Con `Z` ya se sabe qué está delante:

```js
// profundidad de una pieza = z de su hueso en el punto más cercano a la otra pieza (en el codo, u ≈ 0 del antebrazo)
function ordenCodo(Z, codo) {   // devuelve 'antebrazo-delante' o 'brazo-delante'
  const zBrazo = Z[codo] - 0.0001;                  // el brazo termina en el codo
  const zAnte  = (Z[codo] + Z[codo + 1]) / 2;       // el antebrazo, en su mitad
  return zAnte >= zBrazo ? 'antebrazo-delante' : 'brazo-delante';
}
```

- Con **histéresis** (no cambiar el orden si la diferencia es menor que `0.15·R0`) para que no parpadee.
- **Dónde cambiar el orden sin que se note**: justo en el cuadro en que las piezas **no se tocan** en pantalla, o en
  el que la de delante cubre por completo la zona de cruce (se mira el solape de los dos contornos ya calculados). Si
  no hay tal cuadro, se cambia en el cuadro de mínimo solape.

### 3.3 Cómo guardarlo en XFL (para que Animate lo entienda)
Animate no tiene "orden por cuadro" dentro de una capa. Solución compatible:

- La pieza que cambia de orden (el antebrazo) vive en **dos capas**: `antebrazo (delante)` arriba de `brazo`, y
  `antebrazo (detrás)` debajo.
- En cada tramo, el sistema escribe el dibujo en la capa que corresponde y deja **clave vacía** en la otra. Es lo
  mismo que haría un animador a mano, pero automático.
- En MacroFlash se pueden mostrar como una sola capa (`antebrazo ⇅`) con un icono, como ya se hace con 🦴 y ⊕.

### 3.4 Un cruce de verdad: ¿y si se cruza DENTRO de una pieza?
Con piezas por hueso, un contorno ya no puede cruzarse consigo mismo. Un cilindro proyectado nunca se cruza: es siempre
un "estadio" (rectángulo con puntas redondas) o, en escorzo total, un círculo. Si aun así un nodo queda en mal lugar,
se aplica la regla 4 de la cabeza: **el nodo escondido se pega a su vecino visible**. El número de nodos no cambia y el
morph sigue sano.

---

## 4. Las tres clases de nodos (lo que hace que el escorzo se vea 3D)

Esto es la idea de "borde" y "rasgos" de la cabeza, aplicada a la manga. Se calcula sola con `s` (y se puede
corregir a mano con un menú: "este nodo es borde / anillo / detalle"):

| Clase | Cuáles | Qué hacen en el escorzo |
|---|---|---|
| **borde** | `|s| > 0.85` (los lados de la manga) | Van a la **silueta** de la cápsula proyectada: dos rectas paralelas al hueso a distancia `R` (el ancho NO se acorta en escorzo, solo el largo). |
| **anillo** | líneas que dan la vuelta al brazo: el borde del puño, la costura del codo, el final de la manga | Se pegan a la superficie con `(u, θ)`: al inclinarse el hueso, **una recta se vuelve elipse**. Es lo que más "vende" el escorzo. |
| **detalle** | dobleces y arrugas, botones del puño, gemelos | Pegados a la superficie. Si `θ` queda detrás (`z` de la normal < 0), se pegan al vecino visible (regla 4). |

La elipse del anillo sale sola de `puntoEnCapsula`: un anillo en `u = 1` con `θ` de -90° a 90° es media elipse cuyo
eje menor mide `R · |dz| / L0` (redonda si el brazo apunta a la cámara, plana si está de lado).

**Cuándo se ve "el otro lado" del anillo** (la parte de atrás del puño, como un tubo visto desde adentro): cuando el
hueso apunta hacia la cámara, la boca de la manga se ve completa (elipse entera). Para eso el anillo del puño se dibuja
**completo** (360°) en reposo, con su mitad de atrás oculta (pegada a sus vecinos visibles). Al apuntar a la cámara,
aparece sola.

### Silueta del escorzo extremo (el antebrazo apuntando a la cámara)
Cuando `L2d → 0` el estadio se vuelve un círculo de radio `R`. En ese caso:

- Los nodos de **borde** se reparten por ángulo alrededor del círculo (como la silueta del huevo: cada nodo al ángulo
  de su lado). Así no se amontonan en dos puntos.
- La transición es continua: se mezcla de "dos rectas" a "círculo" según `k = L2d / L0`, usando el mismo truco de la
  cabeza (`Vector2.Lerp(orig, EnSilueta(ang), w)`).

---

## 5. El codo (doblar sin que se derrita)

Con piezas separadas y la rótula, el codo ya no se aplasta. Para que el doblez se vea natural:

1. **Lado de afuera**: el borde exterior del brazo sigue un **arco** alrededor de la articulación (radio `R`) en vez de
   hacer esquina. Lo da la media luna del antebrazo.
2. **Lado de adentro (el pliegue)**: los nodos de borde del lado interior de las dos piezas se juntan en el **punto de
   cruce de sus dos siluetas interiores** (la intersección de las rectas interiores de los dos estadios). Así se forma
   el pliegue limpio de la tela del saco, sin bucles.
3. **Ángulo máximo**: si el codo se dobla más de ~150°, el antebrazo se aprieta contra el brazo. Se recomienda limitar
   el ángulo o dejar que el pliegue "infle" el brazo (`G` sube un poco en el punto de cruce).

---

## 6. Correcciones que se recuerdan (cuando la geometría no basta)

Hay poses en las que el dibujante sabe más que la geometría (el saco hace una arruga especial con los brazos arriba).
MacroFlash ya tiene `correccionDe(r, P, J, G)`: guarda lo que el dibujo de una clave se aparta de los huesos, en el
espacio de reposo. La idea es llevarla un paso más allá (en el mundo 3D se llama *Pose Space Deformation*):

- Cada corrección se guarda junto con **la pose en que se hizo**: ángulo del codo, ángulo del hombro, escorzo de cada
  hueso (`dz / L0`).
- En una clave nueva **sin** corrección propia, se aplica una **mezcla de las correcciones guardadas, pesadas por qué
  tan parecida es la pose** (por ejemplo, peso = `exp(-(distancia de pose)² / σ²)`, normalizado).
- Resultado: corriges UNA vez "brazos arriba" y "antebrazo hacia la cámara", y todas las demás claves parecidas salen
  ya corregidas. Es el equivalente de la "biblioteca de vistas" de la cabeza invisible.

---

## 7. Flujo para el animador (cómo se usa)

1. Dibuja el brazo en reposo (como en la hoja de personaje: de frente, brazo colgando o extendido).
2. **Huesos (B)** → "Brazo en piezas": hombro, codo, muñeca, nudillos. El sistema corta las piezas, mide `R0` y
   clasifica los nodos (borde / anillo / detalle). El dibujo no cambia.
3. Anima moviendo SOLO las articulaciones en 2D, como hoy. Si un hueso se ve más corto, el sistema entiende que es
   escorzo.
4. Si el escorzo va al revés (se ve como si se alejara), clic en el hueso → **"hacia mí / hacia atrás"**.
5. El orden (antebrazo delante o detrás) se decide solo. Se puede forzar con un clic en el tramo.
6. Si una pose no convence, se corrige a mano como siempre. La corrección queda en la "memoria de poses" para las
   parecidas.

---

## 8. Prueba concreta: el cruce de la imagen

Secuencia de referencia (la del baile): **brazo extendido de lado → antebrazo apuntando a la cámara (el cruce) →
brazos arriba**.

| Cuadro | Qué debe pasar | Qué se revisa |
|---|---|---|
| A. Extendido de lado | Igual que hoy (`Z ≈ 0`). | Que el dibujo no cambie respecto a `HU.deformar` (diferencia < 0.5 px). |
| B. El codo empieza a doblar, antebrazo hacia la cámara | `dz` del antebrazo crece con signo `+`. El puño se vuelve elipse. El antebrazo pasa a la capa **delante**. | Que el orden cambie en el cuadro de menor solape, sin parpadeo. |
| C. **El cruce**: antebrazo casi de frente a la cámara | Antebrazo = círculo (boca de la manga + puño completo). El brazo de arriba queda detrás, tapado por la rótula. | Que no haya rayas ni huecos. Que el número de nodos de cada pieza sea el mismo que en A. |
| D. Brazos arriba | `dz` del antebrazo baja (vuelve a verse largo), el signo puede cambiar al pasar por `dz = 0`. El orden vuelve a "brazo delante" si corresponde. | Que el morph de C a D sea suave (mismos nodos) y que el pliegue del codo quede limpio. |

Recomendación para las pruebas: hacer primero **solo el antebrazo** (una pieza, un hueso) yendo de lado a apuntar a
la cámara (prueba de la sección 4), después agregar el brazo y el orden (sección 3), y al final el pliegue (5) y la
memoria de poses (6).

---

## 9. Orden de implementación sugerido

1. `profundidades()` + guardar `Z`/`S` en `mfHuesos` (sin cambiar el dibujo todavía). Mostrar `Z` con color en
   `dibujarHuesos` (rojo = hacia la cámara, azul = hacia atrás) para verificar.
2. "Brazo en piezas": cortar, pesos 1 por pieza, rótula. Mismo rig para todas las piezas.
3. Orden por profundidad + escritura en dos capas (`delante` / `detrás`) al guardar el XFL.
4. Clases de nodos y `puntoEnCapsula()` para **anillo** y **detalle** (la elipse del puño).
5. Silueta de escorzo extremo (estadio → círculo) para los nodos de **borde**.
6. Pliegue del codo.
7. Memoria de poses (correcciones mezcladas por parecido de pose).

Cada paso se puede probar solo, y ninguno rompe las claves que ya existen: sin escorzo, todo da lo mismo que hoy.

---

## 10. Resumen en una frase

**Brazo y antebrazo dejan de ser un solo contorno: son cápsulas 3D invisibles que comparten los mismos huesos. La
profundidad sale sola de qué tan corto se ve cada hueso, y con ella se decide qué pieza va delante, se dibujan las
mangas como elipses en escorzo y el codo se cubre con una rótula.** El número de nodos nunca cambia, así que el morph
y Animate siguen funcionando.
