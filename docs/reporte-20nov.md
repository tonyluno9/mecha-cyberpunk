# Reporte 20-nov: modelos sin integrar y pendientes del playtest del 19-nov

Todo está en `main` (merge --no-ff de `integ/20nov`). Tareas T-1490 a T-1507, una rama `tony/T-xxx` cada una, hechas por
subagentes Sonnet e integradas, compiladas y probadas por clase en `.sesiones/s2`. Lo de "Le gustó" no se tocó (ver abajo los
dos casos en que hubo que cuidarlo). Progreso detallado en `docs/progreso-20nov.md`.

## 1. Modelos

| Pedido | Qué quedó | Variante |
| --- | --- | --- |
| E15, E16, E18 a escala real | E15 2.6 m, E16 3.5 m (gigante), E18 2.2 m con los modelos de `blender/personajes/escala_real`; esqueleto del avatar rehecho a ese tamaño; cápsula, boca del arma, cabeza y alcance de golpe escalados. Salen en Nova y en la policía de 5 estrellas (ConfigRespuestaNova). | E15 _a y _b, E16 _a y _b (el _b ya tiene avatar válido y entra a Nova), E18 _a (el _b solo es PNG) |
| E17 perro robot | 1.2 m de alto. Patrulla, te detecta, corre y muerde (T-1494; antes se apagaba por faltarle "brazo"). | _a (el _b solo es PNG) |
| E19, E20 SKULLS | Ya estaban en los grupos de SKULLS de la ciudad (las 4 variantes). | E19 _a_2 y _b, E20 _a y _b |
| W10 arma de Nova | `W09_ArmaNova` renombrada a **W10_ArmaNova** (archivos, texturas, material, prefab). En la mano de guardias (E15, E18, guardia armado) y robots (E16), a su tamaño. | — |
| Drones DR02, DR03, DR04, E13 | Rotores que giran. Drones de combate en la persecución: DR04 explorador desde 2 estrellas, DR02 pesado desde 4, E13 asalto con 5; con su indicador de daño. DR03 se queda decorativo. | DR02 _a, DR04 _a (sin _b 3D), E13 provisional (Blender lo hizo con DR02_b reducido) |
| Naves NV01, NV03 | Con 4-5 estrellas una nave baja, flota a 10 m y deja 2 a 4 agentes de Nova colgando de una cuerda, que caen con la animación de caída de Blender; luego se va. Las que pasan lejos siguen igual. | NV03 _a, NV01, NV03 _b |
| V11 camioneta | Ya era la patrulla camioneta pero solo salía con 5 estrellas; ahora sale desde 3. Con llantas de policía nuevas y escala real. | — |
| NP01-NP14 | NP07-NP14 ya estaban mezclados en los peatones (14 variantes parejas). **NP01-NP06 no se pudieron**: solo hay zip de Tripo sin rig (anotado en assets-faltan para Blender). | NP10 y NP12 solo _a |
| TR01, TR02 | **No se pudieron**: solo hay PNG, no hay modelo 3D (anotado en assets-faltan). | — |
| MA01 maletín | Reemplaza al de primitivas en el atraco, 45 cm. | — |
| Parque | Plaza K09: kiosco PZ02, 4 bancas PZ01, 2 jardineras PZ03, árboles AR01/AR02 (foto en `docs/capturas/_20nov/parque-k09.png`). | — |

## 2. Juego

- **Menú**: niebla del fondo casi quitada (solo en Menu_Calles).
- **Sin cuarto**: la partida nueva empieza afuera, frente a la puerta de la casa; el paso "SAL A LA CALLE" se salta y el tutorial abre en el asalto. Cama y espejo siguen funcionando donde estaban pero sin modelo (eran hijos del cuarto): pendiente decidir dónde van.
- **Cámara**: temblor al ~18 % (golpe, disparo, daño), sacudida del zoom al 20 %, balanceo a la mitad, temblor del carro a la mitad.
- **Combo de 2 golpes** (sin el tercero). Los puños hacen 0.45 del daño contra pandilleros y rivales: un pandillero aguanta 7 golpes en vez de 3.
- **Armas**: francotirador 300 m sin caída (antes 8° de dispersión sin apuntar, por eso no pegaba), fusil 150 m, subfusil 70 m con caída de daño, escopeta con caída fuerte. Trazos finos y brillantes (1 de cada 3 en el subfusil, 1 de cada 2 en el fusil). Granada con círculo donde cae. La rueda de armas suena al abrirse (se reusa un "whoosh": falta un sonido grave propio).
- **Drones**: el indicador de daño ahora marca sus tiros (antes el escudo lo anulaba) y un cuadro rojo "DRON" lo señala, o una flecha en el borde si está fuera de pantalla. No atraviesan muros (suben para librarlos) y llevan luz.
- **Vida baja** avisa al 40 % (antes 30 %).
- **Curarse en persecución**: ya se puede en el carro.
- **Animaciones que bloquean**: bajar y subir de moto y carro, puertas, botones, recoger, levantarse y el aterrizaje fuerte no dejan moverte hasta terminar; golpes, disparos, recargar, teléfono y rodar no bloquean.
- **Nova**: al recibir un tiro o oír uno tuyo a menos de 60 m te buscan, disparan y se cubren agachados; 30-40 % más vida.
- **SKULLS**: tope de 5 vivos por grupo y 8 en 40 m (las misiones con número fijo no cuentan).
- **NPCs**: al recibir disparos reaccionan, se asustan, huyen corriendo y mueren con animación (antes la muerte animada nunca se activaba por un nombre de parámetro mal). Meseras de frente; el hacker teclea sentado en su silla y sigue volteando a verte.
- **Prótesis**: un clic elige la casilla y doble clic instala (Enter sigue sirviendo).
- **Consumibles**: 2 ranuras, **E** y **C** a pie (E solo si no hay nada con qué interactuar enfrente), **Z** y **X** en el carro; **Alt+E / Alt+C** cambia qué va en cada ranura; el HUD muestra ícono, cantidad y tecla, y el aviso de la primera vez explica las teclas.

## 3. Ciudad

- **Barrido**: diagnóstico de flotantes, hundidos y calles sin textura (`BarridoCiudadT1501.Diagnostico`); se asentaron 50 cosas (carros robables, expendedoras, postes y puestos). Quedan 205 "a revisar" (fachadas de kit a 20 cm, carros de la autopista sin collider de losa): no se movieron a ciegas, están en `Logs/t1501-diagnostico.txt`. Carros estacionados con detección continua para que no se hundan.
- **Edificios de primitivas**: 52 de los 95 edificios de las afueras ahora son modelos (NV01, P11, P12, LO, CC, CH02, ML05, PU03/04) según el barrio (foto `afueras-salida2.png`). 43 no tienen modelo que llene su hueco (torres de 120 m, muros): anotados con medida en `docs/assets-faltan.md`.
- **Armería**: gabinetes volteados hacia dentro, texturas rotas cambiadas, calle asentada y sin piezas repetidas (arreglos automáticos: revisar a ojo).
- **Charcos**: shader de agua nuevo con reflejo, ondas suaves y ondas que salen donde pisas; salpicadura con más gotas y un anillo. El sonido de pisar agua no cambió.

## 4. Lo que llegó de Blender (bitácora del pedido del 19-nov noche)

- **Jugador**: trote (paso normal), sprint propio (Shift), **caminar de verdad con Bloq Mayús** (1.6 m/s), trote de lado y hacia atrás, salto nuevo (despegue, aire pataleando, aterrizaje), aterrizaje fuerte en caídas de más de 2 m, y **un sonido de paso por pisada** con eventos en los fotogramas de la bitácora. Golpe 1 y 2 nuevos, daño frente/espalda, muerte, manejar carro, caminar apuntando.
- **NPCs**: caminar de hombre, mujer, mayor con bastón y con celular, esperar, huir, asustarse, balazo y muerte; mesera con charola; hacker tecleando.
- **Nova**: E15, E16 y E18 con sus clips (caminar, correr, disparar, impacto, giros, golpe de escudo, jetpack) y la caída de Nova (aire, aterrizaje y levantarse) para los que bajan de la nave.
- **Carros revisados**: V01-V11 y motos a escala real (se quitó el x4.5), patrullas con puertas, cofre y rin de policía, V22 con el cristal rehecho (Blender no rehízo la carrocería).

## No quedó y por qué

- **NP01-NP06, TR01, TR02**: no hay modelo 3D con rig o no hay modelo (solo PNG/zip). Pedido en `docs/assets-faltan.md`.
- **Clips del perro (E17_*)**: son del rig a escala real de Blender y no calzan con el perro del juego (190 curvas sin hueso); el perro sigue con sus clips de antes. Para usarlos hay que cambiar su modelo por el de `escala_real` (pendiente).
- **Muerte de los robots de Nova**: Blender mandó clips nuevos pero no se usaron, porque "la caída de los robots de Nova" está en "Le gustó".
- **Escaleras**: los clips de Blender son de subir escalones; la "escalera" del juego es de mano: no se pusieron ahí (se verían mal).
- **Cobertura de PJ01 (quieto, asomarse, regresar), daño de lado, manejar moto, platicar, sentado en banca**: importados, sin estado que los use todavía.
- **Robar la W10** al guardia golpeado: el botín es un enum de Core (Pistola/Escopeta); hace falta una tarea de Core.
- **HistoriaDemoPruebas.RecorridoDocInventorHacker** falla: hay 8 SNAKES antes de "Libre" al empezar afuera; no encontré la causa (puede ser que al empezar en la calle aparezcan los ambientales). Revisar jugando que no te ataquen SNAKES al inicio.
- **ReglasEnsambladosPruebas** (una prueba de PlayMode de Mundo que referencia Personaje) ya fallaba antes.
- **Pruebas que siguen en rojo** (encontradas al final, sin tiempo para la causa):
  - `CarrosEstacionadosT1501PlayPruebas`: la patrulla queda con una llanta 12 cm bajo el piso tras 3 s (la suspensión con la escala real la hunde): revisar patrullas estacionadas.
  - `MaletinMA01T1499Pruebas`: el maletín mide 57 cm de largo y no 45 (la escala sale de la caja del modelo antes de acostarlo).
  - `IaNovaT1494Pruebas.Dron_HaciaUnMuro`: el dron libra el muro rozando su borde (29.999 contra 30 m): no lo atraviesa, pero pasa justo.
  - `HistoriaDemoPruebas.RecorridoDocInventorHacker` (SNAKES antes de tiempo, ver arriba).

## Qué probar

1. Partida nueva: empiezas afuera, el tutorial avanza; menú con la ciudad clara.
2. Caminar (Bloq Mayús), trotar, sprintar, de lado apuntando, saltar y caer de un techo: pies sin patinar, un paso por pisada.
3. Combo de 2 golpes contra un grupo de SKULLS: te obliga a sacar arma; el temblor apenas se nota.
4. Francotirador y subfusil de lejos; trazos; círculo de la granada.
5. 2 a 5 estrellas: drones DR04/DR02/E13 con su marcador, la camioneta V11, una nave que baja agentes de Nova, curarte con Z/X en el carro.
6. Nova de lejos: te buscan, disparan y se agachan; el gigante E16 de 3.5 m no se atora en calles; el perro patrulla y muerde.
7. Civiles: dispárales cerca y a ellos (se asustan, huyen, mueren animados). Meseras, hacker.
8. Consumibles con E y C; prótesis con doble clic.
9. Ciudad: parque K09, salidas de las afueras, armería, charcos; carros estacionados y patrullas (puertas, llantas).


---

# Segunda vuelta 20-nov: nuevos-19nov y lo que faltó

Todo está en `main` (merge --no-ff de `integ/20nov-B`, subido). Tareas T-1510 a T-1516, cada una en su rama `tony/T-xxx`, hechas por
subagentes Sonnet e integradas, compiladas y probadas por clase en `.sesiones/s2`. Progreso en `docs/progreso-20nov.md`.

## Modelos nuevos (nuevos-19nov)

| Pedido | Qué quedó |
| --- | --- |
| PA01 y PA02 patrullas | Partidas con Blender en carrocería, 4 llantas (pivote en el eje, giran), puertas delanteras (bisagra, abren) y cristales. `Patrulla.prefab` = PA01 (4.80 m) y `PatrullaCamioneta.prefab` = PA02 (5.00 m), mismo GUID, con sirena y luces. Fotos en `docs/capturas/_20nov/T1510_*`. |
| V22 | Se eligió la **_a**: sus ruedas venían sueltas, la puerta tiene costura y los cristales son planos. La _b tenía las ruedas fusionadas y el casco pegado al piso. Reemplaza al arruinado en su mismo prefab. |
| Rines de policía V05, V06, V11 | Ya los tenían (T-1507); comprobado con pruebas. PA01/PA02 traen el rin en su textura. |
| CB01-CB04 cobertura | 7 prefabs (CB04 solo _a: la _b es PNG). **53 coberturas** en la ciudad alrededor de los grupos de SKULLS, SNAKES y Nova, las escoltas y el recinto del jefe. SKULLS y guardias de Nova prefieren un CB cercano: van al lado contrario al jugador, se agachan, se asoman a disparar y vuelven. |
| SIN_NOMBRE_01-15 | Identificados comparando renders con los PNG (tabla en `docs/identificacion-nuevos-19nov.md`, renders en `docs/capturas/_20nov/identificar/`). Copiados con su nombre en la misma carpeta (los SIN_NOMBRE siguen ahí). Confianza media en SIN_09 (ED04_b), SIN_03 (MU01_b), SIN_05 (MU02_a) y SIN_14 (MU03_a): **revisa esos 4**. |
| ED01-ED12 | 13 modelos importados. **39 edificios de primitivas** de las afueras cambiados por ED según su barrio. **17 de ellos se apagaron** porque su cubo estaba encimado a otro edificio (dos salidas ponían edificio en el mismo lugar), y ese hueco ya lo llena el otro. Quedan 4 cubos de las afueras sin un modelo que les quede. |
| MU01-MU03 muro | Mismo trazo, alto (6 m) y salidas del muro que te gustó; solo cambia cómo se ve: **48 tramos MU01, 16 torres MU02** (esquinas y a los lados de cada salida) y **6 puertas MU03** en las salidas, que siguen pasables. El muro viejo queda con los renderers apagados y sus colliders intactos. Fotos en `docs/capturas/_20nov/muro_*.png`. |
| Lo de 17nov, 18nov y modelos3d | Ya entró en la primera vuelta (arriba). Siguen sin modelo con rig **NP01-NP06** (zip de Tripo sin esqueleto) y sin modelo 3D **TR01/TR02** (solo PNG). |

## Blender: lo que había quedado importado sin usar

- **Daño en 4 direcciones**: ahora también izquierda y derecha (golpe a más de 53° hacia un costado).
- **Manejar moto**: usa `PJ01_Manejar_Moto` (el clip nuevo de 19N).
- **Escalones**: dos rayos detectan escalones de 0.1 a 0.4 m y, caminando, se usan `PJ01_Escaleras_Subir/Bajar` (hasta 3.2 m/s; trotando o en sprint se queda la locomoción normal, para que no patine). La escalera de mano sigue igual.
- **Cobertura del jugador**: con V pegado a una pared ahora juega Cobertura_Quieto en bucle; al apuntar se asoma y al soltar regresa.
- **Velocidades del Animator**: revisadas contra la tabla (1.6 / 4.5 / 7.25 / 4 / 2). Ya coincidían; quedan cubiertas por pruebas.
- **NPCs**: 1 de cada 8 peatones platica en pareja o trío a lo largo de la banqueta; al oír tiros, 40 % se agacha cubriéndose y luego huye o sigue; 4 civiles sentados en las bancas PZ01 del parque.
- **Perro E17**: usa su modelo de escala real con sus clips de Blender (caminar, correr, ladrar, morder, muerte), 1.2 m de alto, con la misma conducta (patrulla, detecta, corre y muerde). El modelo viejo quedó sin usar.

## Pruebas que estaban en rojo en la primera vuelta

- **Maletín de 57 cm**: la prueba medía la caja del mundo con el maletín girado; el modelo sí mide 45 cm. Prueba corregida, en verde.
- **Dron al borde del muro**: el fallo era real. El dron podía quedar rozando el muro y desde ahí lo cruzaba. Ahora revisa también el punto de llegada y sube a borde + 2.3 m antes de entrar. En verde.
- **SNAKES al inicio**: son las riñas y escoltas que la ciudad tiene siempre prendidas (las pruebas de T-1107/T-1108 las exigen) y no se quitaron. La historia ya no suma SNAKES antes de "Libre". HistoriaDemo en verde.
- **Patrulla hundida 12 cm**: la prueba medía la llanta con la caja inflada al girar. Medida como cilindro, ningún carro queda bajo el piso. En verde.
- **Cama y espejo**: sin cuarto, "Dormir" y "Mirarte al espejo" son dos interacciones junto a la puerta de la casa, sin cama visible.
- **ReglasEnsamblados**: una prueba de Mundo referenciaba Personaje y se movió a las pruebas de Personaje. En verde.

## playtest-19nov, punto por punto (las dos vueltas)

| Punto | Cómo quedó |
| --- | --- |
| Menú sin niebla | Hecho (primera vuelta). La música del menú queda pendiente tuya. |
| Sin cuarto | Hecho; cama y espejo en la puerta. |
| Casi sin temblor de cámara | Hecho (~18 %). |
| Combo de 2 golpes | Hecho. |
| Más vida contra golpes | Hecho (los puños hacen 0.45 del daño contra pandilleros y rivales). |
| Daño de lejos de todas las armas | Hecho (francotirador 300 m, fusil 150, subfusil 70 con caída, escopeta corta). |
| Trazos de bala | Hecho: finos y brillantes. |
| Mira de granada | Hecho: círculo donde cae. **Los sonidos de la granada no se pulieron.** |
| Cañón de plasma: pulir su animación | **No**: no estaba en la lista de esta noche y el cañón está en "Le gustó"; no se tocó. |
| Sonido de la rueda de armas | Hecho (se reusa un "whoosh"; falta un sonido grave propio). |
| Indicador de daño de drones | Hecho, con marcador "DRON" o flecha en el borde. |
| Aviso de vida baja antes | Hecho, al 40 %. |
| Curarse en persecución | Hecho (Z/X en el carro). |
| Bloquear el movimiento al bajar de la moto | Hecho, también en las demás animaciones que deben bloquear. |
| Cómo te ves sentado en carros y motos | Carro con PJ01_Manejar_Carro; moto con el clip nuevo. Revisa a ojo la altura de la cadera en la moto. |
| Nova y robots: detectan, buscan, disparan y se cubren | Hecho; ahora también se cubren detrás de los CB. Gigantes a escala real (E16 3.5 m). |
| Perro robot que ataca | Hecho; ahora con su modelo y sus clips de Blender. |
| Drones que no atraviesan muros | Hecho, con margen. |
| Naves de Nova en acción | Hecho (bajan agentes con 4-5 estrellas). |
| Tope de SKULLS | Hecho (5 por grupo, 8 en 40 m). |
| Civiles que reaccionan y mueren | Hecho; además platican, se agachan y se sientan. |
| Meseras y hacker | Hecho. |
| Prótesis con clic y doble clic | Hecho. |
| Consumibles en 2 ranuras (E y C) | Hecho. |
| Nada flotando ni hundido | Parcial: 50 objetos asentados y ningún carro hundido. Siguen 205 casos "a revisar" (fachadas de kit a 20 cm) en `Logs/t1501-diagnostico.txt`; no se movieron a ciegas. |
| Calles sin textura | Revisadas en el barrido de la primera vuelta. |
| Edificios hechos con primitivos | 52 cambiados en la primera vuelta y 39 más con ED (17 apagados por estar encimados). Quedan 4 en las afueras, más pisos y techos de interiores que no son fachada. |
| Armería | Hecho (calle, gabinetes volteados, texturas). Revisa a ojo. |
| Charcos | Hecho: agua nueva con ondas. |
| Parque | Hecho (K09), ahora con gente sentada. |
| Casino: TR01/TR02 | **No**: no hay modelo 3D, solo PNG. |
| Muro de la ciudad | Hecho, con MU01-MU03. |
| Patrullas con puertas y llantas | Hecho, con PA01/PA02. |
| Superdeportivo | V22 nuevo (variante _a). |
| Voces, música épica, sonido de dron, pantalla gris | Pendientes tuyos (en el playtest están como "más adelante"). |

## Qué no quedó y por qué

- **NP01-NP06, TR01, TR02**: no hay modelo con rig, o no hay modelo.
- **13 variantes ED/MU sin zip** (solo PNG): ED01_b, ED02_b, ED03_a, ED04_a, ED05_b, ED06_b, ED07_b, ED09_a, ED10_b, ED11_b, ED12_b, MU02_b y MU03_b. Anotadas en `docs/assets-faltan.md`.
- **CB04_b**: solo PNG.
- **Cofre de PA01/PA02**: solo se cortaron las puertas delanteras.
- **Caminar apuntando de PJ01**: el clip existe, pero al apuntar las piernas siguen saliendo del árbol de caminar (necesita su propio árbol).
- **Riñas de SNAKES prendidas al empezar**: se dejaron porque otras pruebas las exigen. Si molestan al inicio se pueden apagar hasta el tutorial, cambiando esas pruebas.
- **Foto "muro_este"**: la cámara quedó dentro de un edificio. En la foto de la esquina se ve un plano azul junto a la torre: revisa ese punto.

## Qué probar

1. Patrullas (2 o más estrellas): PA01 sedán y PA02 camioneta, puertas que se abren, llantas que giran, cristal. El V22 nuevo.
2. Pelea con SKULLS o Nova cerca de barreras y costales: se cubren detrás, se asoman y disparan.
3. Las afueras: edificios ED en vez de cubos; el muro con sus torres y puertas en las 6 salidas, y que puedas salir por donde salías antes.
4. Con V pégate a una pared, apunta y suelta. Recibe tiros de lado. Sube y baja escalones caminando. Maneja la moto.
5. Peatones que platican, que se agachan con tus tiros, y los sentados en las bancas del parque K09.
6. El perro robot: que camine y muerda con sus animaciones nuevas.
7. Partida nueva: Dormir y Espejo junto a la puerta de la casa.

## Builds (segunda vuelta)

Construidos uno por uno en `.sesiones/s2` (mismo árbol que `main`) y copiados con robocopy a la carpeta principal:
- `Builds/Demo/CyberCity.exe` (`ConstruirDemoPandillas`): Build Finished, Result: Success, 2845 MB.
- `Builds/Juego/CyberCity.exe` (`ConstruirJuego`): Build Finished, Result: Success, 2845 MB.

Unity quedó cerrado (sin lockfile).
