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

## Segunda tanda (V5–V11) · build nuevo en `wtVR\Builds\VR2\CyberCity.exe`
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
