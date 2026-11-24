# Reporte VR Quest 3 · 23-nov

Rama `vr/quest3` (subida, sin merge a main). Carpeta: `C:\Users\Angel Perez\Desktop\videojuego\wtVR`.
Build: `wtVR\Builds\VR\CyberCity.exe` (PC, OpenXR, mismas escenas que ConstruirJuego).

## Cómo se abre
1. Prende Quest Link (o Air Link) y deja el visor en el escritorio de Link.
2. Abre `CyberCity.exe`. Si hay visor, entra en VR solo. Si no hay visor, el juego sale normal (tercera persona).
3. `CyberCity.exe -novr` fuerza el modo normal. `-vrsim` usa el simulador de XR sin visor.

## Botones
| Acción | Botón |
| --- | --- |
| Caminar (1.62 m/s, hacia donde miras) | Stick izquierdo |
| Correr (4.53 m/s) | Mantener el clic del stick izquierdo |
| Girar 45° | Stick derecho a un lado (soltar para el siguiente) |
| Saltar | X |
| Interactuar (la E: Tino, máquinas, Doc, terminales) | B |
| Disparar | Gatillo derecho |
| Recargar | A |
| Golpear | Mover la mano rápido (> 1.5 m/s) contra el enemigo |
| Agarrar enemigo (solo con implante de brazos) | Grip, con la mano a < 0.4 m; soltar = lanzar |
| Elegir en menús | Láser del control derecho + gatillo |
| Pausa | Botón Menú del control izquierdo |
| Rueda (Tab) | Mantener Y, elegir con el láser |
| Vida y dinero | Mira la muñeca izquierda |

## Qué quedó
- **VR1 base**: OpenXR con perfiles Oculus Touch y Meta Quest Touch Plus. La cámara del jugador pasa a ser la cabeza, con los ojos a 1.72 m (PJ01 de 1.84) sea cual sea tu estatura. El cuerpo solo da sombra y se ven los controles de los Starter Assets. Hay viñeta al moverte y al girar. Si caminas en el cuarto, la cápsula te sigue y choca con las paredes. En VR se quitaron derribos, empujones, la rodada al caer, los temblores (Cinemachine apagado) y las cámaras de cinemática (se ven en la ventana del PC, no en el visor). Calidad: sombra a 30 m con 1 cascada, MSAA 2x, y fuera desenfoque, motion blur, aberración, grano y reflejos de espejos y charcos. El bloom y el color se quedan.
- **VR2**: la pistola real va en la mano derecha. La bala sale del cañón con el mismo daño, munición, sonido y trazos de siempre. A recarga. Las manos golpean con más daño cuanto más rápido, y vibran y suenan.
- **VR3**: con el implante de brazos, el grip agarra a SKULLs, pandilleros y objetivos del campo de tiro. Al soltar sale lanzado con la velocidad de tu mano ×3 (×4 con nivel 2) y se daña al chocar. Si lo golpeas con la otra mano mientras lo sostienes, sale volando. Después vuelve a su IA, o queda KO. Sin implante no agarra.
- **VR4**: los menús salen en un panel a 1.5 m frente a ti y se eligen con el láser y el gatillo. La pausa va en Menú, la rueda en Y y el HUD en la muñeca. Los carros y motos están fuera: sale el aviso "NO DISPONIBLE EN VR".

**Pruebas automáticas con el simulador:** las 29 de la tanda normal pasan. La de la ciudad real (`CiudadVRPruebas`) va en tanda aparte y también pasa. La partida `partida_auto.json` no se tocó: el md5 es el mismo antes y después.

## Qué no quedó / limitaciones
- Nada se probó con el visor puesto: solo con el simulador.
- Lanzagranadas, granada y rifle de plasma no disparan en VR. Solo disparan las armas de bala.
- El menú de Inicio usa un mini rig propio, que no está probado en Inicio.
- Los avisos "B interactuar" del HUD de pantalla no se ven en VR.
- Correr es manteniendo el clic del stick, no alterna.
- No hay recentrado manual de altura: la altura se calibra sola al arrancar.

## Qué debe probar Tony con el visor puesto
1. Abre el exe con Link. ¿Entra en VR? ¿El menú de inicio se ve frente a ti y puedes elegir con el láser?
2. En la ciudad, ¿la altura se siente de adulto alto (1.84)? ¿Ves tus controles y no tu cuerpo?
3. Camina con el stick hacia donde miras, corre con el clic y gira 45° con el stick derecho. ¿Marea? ¿La viñeta ayuda o estorba? ¿Hay temblor?
4. ¿Los fps se ven fluidos (72–90) en la calle con neón y tráfico?
5. ¿La pistola en la mano apunta derecho con el control? Si sale torcida, dime cuántos grados. Dispara a un SKULL en el campo de tiro y en la calle: ¿le baja la vida? ¿A recarga?
6. Dale puñetazos rápidos a un SKULL. ¿Le quita vida? ¿Vibra el control?
7. Con el implante de brazos, agarra a un SKULL con el grip y lánzalo contra una pared. Golpéalo con la otra mano mientras lo sostienes. Sin implante, comprueba que no agarra.
8. Botón Menú: ¿sale la pausa y la cierras con el láser? Y: ¿sale la rueda y eliges con el láser? ¿Se lee el HUD de la muñeca?
9. Entra con B a Tino, a una máquina y al Doc, y úsalos con el láser.
10. Acércate a un carro y pulsa B. Debe salir "NO DISPONIBLE EN VR" y no subir.
11. Ve una cinemática (por ejemplo la silla del Doc). En el visor debes seguir viendo desde tu cabeza, sin que la cámara te mueva.

---

## Segunda tanda (V5–V11) · build en `wtVR\Builds\VR2\CyberCity.exe` (PENDIENTE: el build se cortó por falta de memoria en la máquina; hay que relanzarlo)
`Builds\VR` (el que estás probando) no se tocó. Se metió lo nuevo de `main` con merge normal.

### Botones nuevos o que cambiaron
| Acción | Botón |
| --- | --- |
| Cambiar de arma | Stick derecho arriba (siguiente) o un toque abajo (anterior). También la rueda con Y |
| Recentrar altura (1.72 m) y vista al frente | Mantener el stick derecho abajo 1 s. Mantener el botón Meta lo hace el sistema |
| Lanzagranadas y rifle de plasma | Gatillo derecho: sale del cañón del arma en la mano. El plasma carga con el gatillo sostenido |
| Granada | Grip derecho la toma y al soltar se lanza con la velocidad de tu mano (×1.5) |
| Interactuar | B. Sobre Tino, las máquinas, el Doc, la terminal y los carros sale un letrero "B · …" |

### Qué quedó
- **V5**: las armas especiales hacen el mismo daño y tienen los mismos sonidos y efectos que en PC.
  - En VR la mira del plasma sale siempre del cañón.
  - Con la granada en la mano, el grip no agarra enemigos.
- **V6**: el letrero flota sobre el objeto, te mira y se lee a 1–3 m. Los carros dicen "NO DISPONIBLE EN VR".
  - La terminal del campo de tiro ahora se abre con B en VR (antes solo leía la E del teclado).
- **V7**: en Inicio, NUEVA PARTIDA, CONTINUAR, OPCIONES y SALIR se eligen con el láser.
  - Al cargar, el rig de menús se quita en el acto: queda un solo XR Origin, una sola cámara al visor y un solo AudioListener.
  - El láser sigue en la ciudad y la pantalla de carga es un panel a 1.5 m frente a ti.
  - Se arreglaron:
    - el láser, que se perdía al cargar;
    - el panel, que se quedaba donde estaba la cabeza vieja;
    - el fundido, que se quedaba en negro si se pedía en pausa.
- **V8–V10**: en Opciones → CONTROLES, solo en VR, salen tres filas nuevas. Se guardan.
  - **GIRO**: POR PASOS 45° o SUAVE (90°/s).
  - **VIÑETA**: SÍ o NO.
  - **CORRER**: MANTENER o ALTERNAR. En ALTERNAR, un clic corre, otro para, y también para solo al soltar el stick.
- **V11**: la terminal del campo de tiro está más a la derecha y más baja, y ya no tapa el arma. Para verla, voltea a la derecha.

### Pruebas (simulador XR)
- **Tanda VR normal:** 68 de 68 pasan. Son las viejas más las nuevas `ArmasEspecialesVR`, `AvisoInteraccionVR`, `ComodidadVR` y `TeclaVR`.
- **`InicioVR` y `BaseVR`:** 8 de 8.
- **`CiudadVR`:** 1 de 1.
- **EditMode:** `OpcionesVR` 4 de 4 y `ReglaCampoTiro` 8 de 8.
- **Pruebas de PC del código tocado:**
  - Pasan `OpcionesPruebas`, `InicioPruebas`, `ArmasDistancia`, `ChispeoPlasma` y casi todas las de `ArmasEspeciales`, `ArmasNuevas` y `RuedaArmas`.
  - Fallan 3 pruebas (4 casos), pero también fallan igual con el código de antes (lo comprobé con stash): son fallos viejos, no de esta tanda.
- **Partidas:** las `partida_auto.json` tienen el mismo md5 antes y después.

### Qué probar con el visor (build VR2)
1. Inicio: ¿el menú sale enfrente? ¿Eliges con el láser? Al dar NUEVA PARTIDA o CONTINUAR, ¿pasas a la ciudad sin pantalla negra y con el láser?
2. Stick derecho arriba y abajo para cambiar de arma. Con el lanzagranadas y el plasma, ¿sale del cañón hacia donde apuntas?
3. Granada: toma con el grip y lanza. ¿La distancia se siente bien? El factor ×1.5 se puede ajustar.
4. Acércate a Tino, a una máquina o a un carro. ¿Se lee el letrero "B · …"? ¿Es muy grande o muy chico?
5. Mantén el stick derecho abajo 1 s: ¿te pone a 1.72 m y mirando al frente?
6. Opciones: prueba GIRO SUAVE, VIÑETA NO y CORRER ALTERNAR. ¿Marea el giro suave?
7. Campo de tiro: ¿encuentras la terminal a la derecha y la abres con B?

### Riesgos
- El letrero usa TextMeshPro (la fuente LiberationSans del paquete). Si no se ve en el build, es eso.
- CORRER ALTERNAR usa un botón virtual del Input System. Si falla al crearse, se queda en MANTENER y lo avisa en el log.
- Nada se probó con el visor, solo con el simulador.

---

## Tercera tanda (V12: sin mareo en Inicio y cinemáticas) · build en `wtVR\Builds\VR3\CyberCity.exe`
`Builds\VR` y `Builds\VR2` no se tocaron.

**Por qué mareaba y no salía el menú ni el video:** el código usaba `stereoTargetEye` para decidir qué cámara va al visor, y en URP eso no hace nada (avisaba 8791 veces en tu Player.log). La cámara de Menu_Calles, que vuela por las calles, se dibujaba encima de la cabeza y tapaba el menú y el video. Ahora se usa `allowXRRendering`: solo tu cabeza va al visor, revisado cada cuadro y justo antes de dibujar. Las pruebas viejas comprobaban `stereoTargetEye`, por eso pasaban; ya comprueban lo correcto.

### Qué cambió
- **Inicio**: estás en un cuarto oscuro quieto.
  - A 4 m hay una pantalla grande con el recorrido de calles; solo tu cabeza mueve la vista.
  - El video del logo sale en esa pantalla, con audio, y se salta con B o con el gatillo.
  - El menú está a 1.5 m, abajo de la pantalla, sin encimarse, y se elige con el láser desde el primer momento.
- **Cinemáticas con cámara** (silla del Doc, asalto, flashback): salen en la pantalla grande, con un fundido corto al empezar y al terminar.
- **Videos de cinemática**: salen en la pantalla grande.
- **Pantallas de texto** ("CINEMÁTICA · …", "CONTINUARÁ", HAS MUERTO / arresto, carga): son un panel legible a 1.5 m.
- **KO**: ya no baja ni ladea la cámara; solo hay fundido.
- **Arreglo importante**: las cinemáticas apagaban todas las entradas, también el seguimiento de la cabeza. En el visor la imagen se habría quedado pegada a la cara. Ya no pasa, y B salta la cinemática ("B: SALTAR").

### Pruebas (simulador XR)
- Tanda VR completa, 83 de 83, ninguna omitida. Incluye:
  - las nuevas `InicioVR`: cuarto quieto, la cabeza no se mueve mientras vuela la cámara, video, B y gatillo saltan, menú bajo la pantalla, botón con el láser sin moverlo antes;
  - las nuevas `CinematicasVR`: silla del Doc, cámara y video en la pantalla, la cabeza sigue con su seguimiento, B salta.
- `CiudadVR` 1 de 1.
- Pruebas de PC de lo tocado: `PantallasT1316` e `IntroInicioT1646` pasan, 8 de 8. Fallan 3 (2 de KO en `VidaAPie` y `HasMuerto` por la jaula), pero fallan igual con el código de antes.
- Las partidas tienen el mismo md5 que el respaldo de `Logs/respaldo-partidas-v12`.

### Qué probar con el visor (VR3)
1. Abre el exe: ¿ves el video del logo en la pantalla grande? ¿B lo salta?
2. ¿Te mareas en Inicio? Tu vista solo debe moverse con tu cabeza. ¿Ves las calles en la pantalla y el menú abajo, legible y elegible con el láser?
3. NUEVA PARTIDA o CONTINUAR: ¿la carga se ve como panel y llegas a la ciudad sin negro?
4. Siéntate en la silla del Doc: ¿sale en la pantalla grande sin mover tu cabeza? ¿El fundido tapa bien el salto?
5. En una cinemática "CINEMÁTICA · …", ¿se lee el panel? ¿B la salta? ¿Tu cabeza sigue moviéndose normal?
6. Si mueres o te noquean: ¿solo hay fundido y panel, sin que la cámara caiga?

### Riesgos
- La pantalla grande se dibuja siempre encima de las paredes (en un cuarto chico no la tapan). Puede que la lluvia o las partículas se vean encima de ella.
- Si miras más de 60° a otro lado durante más de 1 s en Inicio, la pantalla y el menú se vuelven a poner frente a ti.
- El flashback se salta con ESPACIO; en VR termina solo.
- Nada se probó con el visor, solo con el simulador.

---

## Cuarta tanda (VR13–VR22) · build en `wtVR\Builds\VR4\CyberCity.exe`
Lleva main `f6e47ad` (T80–T86, HUD final). `Builds\VR` y `Builds\VR3` no se tocaron. Tareas en `docs/tareas/vr-23nov-b/`.

### Qué quedó
- **VR13**: OpenXR en Multi-pass. Los dos ojos dibujan todo (neón, letreros, charcos).
- **VR14**: el HUD final de PC completo (vida, dinero, minimapa, objetivo, arma, avisos, tutoriales, subtítulos) va en un panel curvo de 70° a 1.3 m, pegado a la cabeza con un retraso suave. Ya no hay HUD en la muñeca.
- **VR15**: hay una sola pantalla curva de 105° a 3.5 m en un cuarto oscuro. En ella van el logo, el menú sobre las calles (como en PC), la carga y las cinemáticas. El láser apunta sobre ella.
- **VR16**: en VR se salta "Lo molestan": no hay SKULLS ni aviso, y se pasa directo al maletín.
- **VR17**: el láser y el cañón salen de la punta del control (pose "aim").
- **VR18**: el giro es SUAVE por defecto, hasta 120°/s. En Opciones se puede cambiar la velocidad (30–180) o poner giro por pasos.
- **VR19**: botones nuevos (tabla de abajo). El stick derecho ya no cambia de arma.
- **VR20**: gestos de prótesis.
  - Piernas: las dos manos en los muslos con grip durante 0.5 s.
  - Brazos: los brazos abiertos a los lados con grip durante 0.5 s.
  - La guía de controles usa los modelos reales de los controles. Sale la primera vez y desde Pausa → CONTROLES.
  - Los textos de prótesis ya no dicen "Tab".
- **VR22**: si te quitas el visor o se cae Link, el juego se pausa con el aviso "VISOR DESCONECTADO". A o el gatillo recentran y sigues.
- **VR21**: ya se puede manejar carros y motos. Subes con B, la cabeza va en el asiento y bajas con B.

### Qué no quedó
- **VR23** (cosas flotando): NO QUEDÓ, no dio tiempo.
- Los tutoriales generales (no los de prótesis) y algunos consejos de carga todavía dicen teclas de PC.
- Mientras estás en VR se ignora la orden de cerrar de OpenXR. Así el juego no se cierra solo si se cae Link, pero "salir de la app" desde el menú del visor tampoco lo cierra: queda en pausa y se cierra desde la PC.

### Botones (final)
| Acción | Botón |
|---|---|
| Caminar / correr | Stick izquierdo / clic del stick izquierdo |
| Girar | Stick derecho (suave) |
| Saltar | A |
| Interactuar / subir y bajar del carro | B |
| Disparar / clic en menús | Gatillo derecho |
| Agarrar y lanzar (implante) | Grip de cualquier mano |
| Granada | Grip derecho para tomarla, soltar para lanzar |
| Recargar | X |
| Rueda de armas | Mantener Y; eliges con la mano izquierda o el stick izquierdo |
| Rueda de prótesis | Mantener grip izquierdo + gatillo izquierdo |
| Prótesis de piernas / brazos | Gesto: manos en los muslos / brazos abiertos, con grip 0.5 s |
| Pausa / Opciones | Menú (izquierdo) |
| Recentrar | Mantener el stick derecho abajo 1 s |
| Carro: acelerar / frenar / dirigir / freno de mano | Gatillo derecho / gatillo izquierdo / stick izquierdo / A |

### Pruebas
- **Tanda VR:** 113 pruebas. Después de los arreglos pasan todas las clases nuevas y viejas.
- **PC (EditMode):** 60 de 60.
- **PC (PlayMode):** fallan 6 pruebas, todas fallos viejos que también fallan en la base:
  - 5 por `JaulaClandestino`;
  - 1 de la cama del prólogo.
- **Partidas:** respaldo en `Logs/respaldo-partidas-vr4`, restaurado al final.

### Qué probar con el visor (VR4)
1. Cierra un ojo y luego el otro frente al bar, el neón del mercado y el cartel de tacos: ¿se ven igual?
2. Al abrir: ¿una sola pantalla grande con logo → menú sobre las calles → carga?
3. ¿El láser cae justo donde apuntas en el menú?
4. En la ciudad: ¿se lee el HUD (dinero, vida, minimapa, objetivo) al mirar a cualquier lado?
5. ¿El HUD se ve transparente sobre el mundo, sin fondo negro?
6. Partida nueva: ¿no salen los SKULLS y aparece el siguiente objetivo?
7. Stick derecho a medias y a fondo: ¿gira suave y sin saltos?
8. A salta, X recarga, Y + mano izquierda eligen arma: ¿funcionan?
9. ¿Sale la guía de controles la primera vez y se entiende?
10. Con prótesis: ¿el gesto de muslos y el de brazos abiertos la activan?
11. Quítate el visor y vuelve a ponértelo: ¿sale el aviso y A te deja seguir?
12. Sube con B a un carro y a una moto, maneja una cuadra y baja con B: ¿marea?
13. Siéntate en la silla del Doc o ve una cinemática: ¿llena la pantalla grande?

## Quinta tanda (VR23–VR31) · build en `wtVR\Builds\VR5\CyberCity.exe`
Main no traía nada nuevo (`f6e47ad` ya estaba incluido). No se tocaron `Builds\VR`, `VR3` ni `VR4`. Tareas en `docs/tareas/vr-23nov-c/`.

### Qué quedó
- **VR29 (lo grave)**: el Doc, Tino (tienda, banco y mejoras), las máquinas, el campo de tiro, la pausa, las opciones y el mapa ya no se pegan a la cara: se abren completos en el panel fijo, como el menú de Inicio, y se eligen con el láser. **B o Y cierran o regresan** en cualquier pantalla. El cuerpo del Doc y el arma de Tino se ven dentro del panel.
- **VR25**: las esquinas negras del HUD eran tres degradados oscuros del HUD de PC. En VR ya no salen.
- **VR26**: la viñeta es BAJA por defecto: cierra ~20 % de lo de antes y entra y sale en 0.1 s. En Opciones: VIÑETA NO / BAJA / MEDIA.
- **VR27**: el reparto de botones final (tabla de abajo).
  - **Sacar arma con gesto**: mano derecha a la cadera derecha + grip = pistola; sobre el hombro derecho + grip = arma larga. Vuelve a hacerlo para guardarla.
  - **Recarga manual**: A suelta el cargador; mano izquierda a la cadera izquierda + grip toma uno nuevo; lo acercas a la pistola y entra.
  - La rueda de armas ya no parpadea: se abre con X mantenida.
- **VR28**: una sola tabla cambia todas las teclas de PC por botones de VR en cualquier texto (por ejemplo, "E para hablar" ahora dice "B para hablar").
- **VR31**: la guía de controles son las 6 imágenes nuevas, con etiquetas cian, en páginas con ANTERIOR / SIGUIENTE / CERRAR. Sale la primera vez y desde Pausa → CONTROLES.
- **VR24** (cambia la historia también en PC): después de C03 apareces en tu punto de inicio y te salen 3 SKULLS. Pelea protegida: sin policía y no puedes perder. Al vencerlos sale "TIENES Ƀ20,000 EN EL BANCO" y sigue VE CON EL DOC. Ya no hay objetivo de la ranura.
- **VR30**: hay ruta de chevrones cian en el piso, cada 2 m y hasta 25 m hacia el objetivo. Se puede quitar en Opciones (RUTA SÍ/NO).

### Qué no quedó
- **VR23 (cosas flotando)**: NO QUEDÓ. Medí los 461 props de la ciudad contra los techos reales y ninguno flota más de 5 cm, así que no se movió nada (lista en `docs/capturas/_tareas/VR23/movidos.txt`). Hace falta que Tony diga en qué lugar exacto vio cosas flotando.
- Algunas teclas de PC no tienen botón de VR propio y se tradujeron a lo más cercano: R de revancha → A, M (mapa) y T (teléfono) → Y.
- Las pantallas del taller, la cartelera, la apuesta y el teléfono sí van al panel, pero se cierran con su botón SALIR y no con B/Y.

### Botones (final)
| Acción | Botón |
|---|---|
| Caminar / correr | Stick izquierdo / clic del stick izquierdo |
| Girar | Stick derecho |
| Saltar | Clic del stick derecho |
| Disparar / clic en menús | Gatillo derecho |
| Sacar / guardar pistola | Mano derecha a la cadera derecha + grip derecho |
| Sacar / guardar arma larga | Mano derecha sobre el hombro derecho + grip derecho |
| Recargar | A suelta el cargador + mano izquierda a la cadera izquierda + grip, y lo acercas a la pistola |
| Interactuar | B |
| Rueda de armas | Mantener X |
| Pausa | Y (y Menú) |
| Cerrar / regresar en pantallas | B o Y |
| Rueda de prótesis | Grip izquierdo + gatillo izquierdo, o gestos (manos en los muslos / brazos abiertos) |
| Agarrar / lanzar (implante) | Grip con la mano libre cerca del enemigo |
| Recentrar | Mantener el stick derecho abajo 1 s |

### Guía de controles
![Guía VR, página de botones](capturas/_tareas/VR31/guia_pagina_0.png)

### Pruebas (simulador XR)
- EditMode: 49 de 49 (OpcionesVR, candados de la historia, pelea de inicio VR24).
- PlayMode VR: 16 clases, 142 pruebas. Falló 1: B cerraba la guía y también la pausa. Lo corregí y la segunda corrida (guía, menús y pantallas fijas, 17 pruebas) pasó completa.
- Partidas: respaldo en `Logs/respaldo-partidas-vr5`, restaurado.

### Qué probar con el visor (VR5)
1. Habla con el Doc: ¿se ve todo el cuerpo y su pantalla completa en el panel, eliges con el láser y sales con B o Y?
2. Lo mismo con Tino (tienda, banco y mejoras), una máquina expendedora y el campo de tiro.
3. Abre la pausa con Y, entra a Opciones y regresa con B: ¿todo completo y con salida?
4. ¿El HUD ya no tiene esquinas oscuras?
5. Gira con el stick: ¿apenas se oscurecen las orillas y vuelve al instante?
6. Mano derecha a la cadera + grip: ¿sale la pistola? Otra vez: ¿se guarda? Sobre el hombro + grip: ¿sale el arma larga? ¿Las zonas quedan cómodas para tu cuerpo?
7. A suelta el cargador; mano izquierda a la cadera izquierda + grip; acércalo a la pistola: ¿recarga?
8. Mantén X: ¿la rueda se queda abierta sin parpadear? Clic del stick derecho: ¿saltas?
9. Junto a Joel: ¿dice "B para hablar"? ¿Ves alguna tecla de PC en algún aviso o consejo?
10. La guía (primera vez o Pausa → CONTROLES): ¿cada etiqueta queda junto a su botón real?
11. Partida nueva: cinemáticas → apareces en tu punto → 3 SKULLS → los vences → aviso de Ƀ20,000 → VE CON EL DOC.
12. Con VE CON EL DOC: ¿ves los chevrones en el piso con los dos ojos y te llevan?
13. Si ves algo flotando: ¿dónde exactamente? (para VR23)

## Sexta tanda (VR35) · build en `wtVR\Builds\VR6\CyberCity.exe`
- **Qué quedó:** golpes con las manos (bastan 0.8 m/s, un golpe cada 0.3 s por mano y enemigo; cada uno suena, vibra y el enemigo reacciona). Se ven los puños de PJ01 y ya no los controles. Con el grip sujetas a un enemigo sin necesidad de implante, y con la otra mano le pegas hasta que cae; con implante de brazos, al soltar con fuerza sale lanzado. Los textos dicen "GOLPEA CON TUS MANOS · GRIP: SUJETAR", y el gatillo derecho sin arma no hace nada. Los SKULLS se acercan a 0.8 m y no retroceden. Pruebas con el simulador XR: 107 PlayMode y 40 EditMode, todas pasan.
- **Por qué casi no detectaba:** no eran capas ni triggers (el SKULL tiene un CharacterController en la capa Default y sí se encontraba). Fallaba el alcance: el SKULL se paraba a 1.05 m y regresaba a 2.6 m, mientras que la esfera de 8 cm estaba en la palma y con el brazo estirado no llegaba. Además, al final del golpe la mano ya va lenta, por debajo del umbral de 1.5 m/s, y no se podía volver a pegar sin sacar la mano y meterla otra vez.
- **Qué probar con el visor:** (1) ¿ves tus puños con los dedos cerrados y bien orientados? (2) en la pelea de los 3 SKULLS, ¿cada golpe que toca les baja vida? (3) grip izquierdo cerca de uno, ¿lo sujetas? ¿le pegas con la derecha hasta que cae y se suelta? (4) ¿ganas y sale el aviso de Ƀ20,000? (5) ¿algún aviso dice todavía "gatillo derecho: golpe"?
