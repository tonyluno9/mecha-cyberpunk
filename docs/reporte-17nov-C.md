# Reporte 17-nov, sesión C (Opus dirige, subagentes Sonnet): pendientes de docs/playtest-17nov-b.md

Tony pidió detenerse a las 17:30: lo terminado y probado está en main; tres tareas se quedan en su rama (abajo). **No se hizo build** (Tony lo pidió así al detener).

Forma de trabajo: 12 subagentes Sonnet, uno por tarea, en worktrees sin Unity. Yo junté sus ramas, compilé, corrí los `Aplicar()`, las pruebas por clase y las capturas, y arreglé lo que falló.
Lo de "Le gustó" no se tocó. Nada se inventó con primitivos: lo que necesita un asset quedó con un hueco en el código, anotado en `docs/assets-faltan.md`.

## En main (merge --no-ff de integ/c2)

| Tarea | Punto | Qué quedó | Pruebas |
| --- | --- | --- | --- |
| T-1321 | Policía | **Persecución al inicio**: una patrulla que chocaba sola contra un barril o un poste al aparecer daba la primera estrella; ahora no cuenta en sus primeros 4 s ni 1.5 s después de un choque ajeno. **Ya no se deslizan al apuntar**: el estado de apuntar del Policia.controller es un BlendTree por velocidad (apuntar, caminar, correr). **Barriles**: ya no cierran el carril, y si la patrulla se atora retrocede; un choque a 6 m/s o más revienta el barril y daña la patrulla. | EditMode PatrullaCulpaT1321, PoliciaApuntarT1321 |
| T-1322 | Estrellas | Disparar a una patrulla o a un policía da 2 estrellas; reventarla o matarlo da 3. Si disparas a un carro frente a un policía que te ve, la búsqueda sube a 2 en ese momento. Con 5 estrellas llegan 3 patrullas (salen en vez de las camionetas) y 3 drones extra. | EditMode EstrellasT1322 |
| T-1323 | Nova | **Cabeza**: una esfera en el hueso Head cuenta como cabeza (x3); antes la cabeza de E12 y del jefe sobresalía de la cápsula. **Ya no desaparecen**: el que te vio a 40 m o menos te sigue con 1 a 3 estrellas y solo se van con 0. **Caen del cielo** desde 12 m, con destello al salir y al aterrizar. **Más de 5 estrellas**: aviso con sonido y la oleada cae en ese momento (lo arreglé en la revisión: antes esperaba su turno). **Escudo**: el pesado mide 1.15 veces más (estimado, falta verlo junto a un guardia). | PlayMode NovaT1323 5/5, RespuestaNovaT1304, EditMode NovaT1323Reglas |
| T-1324 | Armas | **Sin balas no se apunta**: con el cargador vacío recarga sola si hay reserva. **Francotirador**: FOV de 8 (antes 15), cuesta 2200 y la mira queda libre arriba y abajo. Ya era sin caída de bala. **Policías que no morían**: la bala revisaba solo 16 colliders y a 150 m pasaba de largo; ahora busca siempre el más cercano. **Rayo**: 400 a 650 de daño por segundo, atraviesa 3 paredes y a toda la gente, y mata de un toque a cualquier persona (nunca al jugador). | EditMode ArmasT1324Edit 8/8 |
| T-1325 | Bomba | La granada revienta de un golpe los carros que alcanza (también lo arreglé en la revisión: antes, si ya venía una cadena, no reventaba). Mata a las personas dentro del 60 % del radio. Un carro que explota revienta a sus vecinos en cadena y suelta sus llantas con física. | PlayMode ExplosionesT1325 3/3 |
| T-1327 | Jugador | **Atropello**: un carro a más de 4 m/s te tira y te levantas. **Golpe**: además de girar, se acerca al enemigo. **Daño de caída**: solo a la vida, nunca al escudo. **Escaleras**: el clip de bajar ya no va al revés, y al bajar escalones se pega al piso. Esto último lo arreglé en la revisión con un rayo recto, porque el `SphereCast` chocaba con la esquina del escalón. | PlayMode JugadorT1327 5/5, EditMode JugadorT1327Edit 9/9 |
| T-1328 | Muerte | Al despertar con el Doc: 0 estrellas, la policía a pie se va, los pandilleros dejan de perseguir y Nova se retira. **Indicador de dirección de daño** en el HUD a pie (barras rojas hacia la fuente, 1 s). | PlayMode MuerteLimpiaT1328 y DespertarLimpiaT1328, EditMode ReglaIndicadorDanioT1328. **Sin correr**: PlayMode IndicadorDanioT1328 (tanda de UI) |
| T-1331 | Ciudad | **Toldos**: en la calle comercial K10 se quitaron 7 toldos TL01 que tenían las patas en el aire y 8 puestos y huacales parados en el arroyo; `ColocarT1320Calles` ya no los crea. **Tino**: el frente del CIC (los paños de fuera, las pilastras y el dintel) tenía la textura de ladrillo muy ampliada y la de ventanas estirada; ahora lleva el muro liso de la fachada de al lado. **Postes**: intensidad 140, alcance 16 m, cono de 100°. **Ruedas**: V01a, V01b, V05 y la patrulla pasan a la rueda de Blender de V09. **Carros**: los estacionados ya no están encima de nada. | EditMode AjustesT1331 y RuedasBlenderT1331; capturas en docs/capturas/t-1331/ |
| T-1332 | NPCs | Los que platican se miran de frente. Reciben daño (`VidaGente`, 40 de vida), mueren cayendo y quedan tirados. | PlayMode GenteT1332 4/4 |

Además, EditMode de las clases tocadas: 176/176. Antes de cada commit se descartó el cambio `SENTIS_ANALYTICS_ENABLED`.

## Se quedan en su rama (sin probar en PlayMode)

No alcancé a correr la tanda de PlayMode de UI. Compilan juntas en `integ/c1`, que trae también los `.meta` que faltaban y la corrección de la prueba de controles (ff9c063).

- **T-1326 tienda de armas** (`tony/T-1326-tienda-armas`): girar el arma arrastrando con el ratón, rueda para acercar, Q/E de respaldo, y botones SALIR y pestañas con clic en El Mercado y en el banco de Tino. Falta correr la prueba de PlayMode TiendaArmasMouseT1326.
- **T-1329 controles** (`tony/T-1329-controles`):
  - La rueda de armas sale con el botón medio.
  - Tab es la rueda de habilidades del EXO: se mantiene, se mueve el ratón y clic activa. Quitó Q, C, X, 4 y 5 como teclas sueltas.
  - El aviso de interacción sale al centro con la tecla en una caja.

  Falta correr ControlesT1329 y las 7 clases de UI que cambió de Tab a botón medio. Los `.meta` de `RuedaHabilidadesExo.cs` y `ControlesT1329Pruebas.cs` están en integ/c1, no en la rama.
- **T-1330 Doc y pantalla de carga** (`tony/T-1330-doc-carga`):
  - **Implantes**: el de misiles y el brazo láser sí existen. No salían en la ciudad porque Ciudad.unity no trae PantallaTienda, y se agregó una al prefab MostradorClinica con `TiendaEnClinicaT1330.Aplicar`.
  - **Texto**: ahora dice "TIEMPO DE GOLPE Y GUARDIA (MECHA)".
  - **Puerta del Doc**: siempre abierta, se cierra contigo dentro.
  - **Pantalla de carga**: la foto se encaja entera, sin estirar ni recortar, y se quitaron el zoom y el paneo.

  EditMode FotosCargaT1330 y ClinicaT1330 pasaron; falta correr PlayMode PuertaDocT1330. Al integrarla hay que volver a correr `TiendaEnClinicaT1330.Aplicar`.

## Sin hacer o pendiente

- **Estela rara en el techo**: no la encontré. En la escena no hay ninguna estela. El script de T-1331 creía quitarla, pero lo que quitaba eran las 17 grietas del cristal de la cabina del MechaEstacionado, y lo corregí para que no las toque. Tony: hace falta una captura o el lugar exacto.
- **ArmasMercadoPruebas.RecorridoFusilYSniper** falla con todo junto en integ/c1 ("abrir El Mercado con E", tiempo agotado), y falla también corrida sola. No alcancé a ver si ya fallaba en main. Puede venir de T-1329 (avisos de interacción) o de T-1326 (PantallaTienda): revisarlo antes de integrarlas.
- **Animación de caminar** ("arreglarla otra vez"): no venía en la lista de esta sesión; no se tocó.
- **Del playtest, no se hizo** por falta de assets: camionetas, naves con sogas, arma de Nova, más NPCs, parque y texturas (ver `docs/assets-faltan.md`).
- **El atropello no quita vida**, solo tira al jugador. Tony decide si debe hacer daño.
- **Pesado de Nova**: la escala de 1.15 es una estimación; hay que verlo junto a un guardia y ajustar `escalaPesado`.

## Build

No se hizo, como pidió Tony al detener. Los builds de esta tarde (15:52 a 15:57, Builds/Demo y Builds/Juego) son de main antes de esta sesión.

## Ramas

- `integ/c2`: lo que entró a main.
- `integ/c1`: las 12 tareas juntas, con las tres que faltan probar.
- `tony/T-1321` … `tony/T-1332`: una rama por tarea.
