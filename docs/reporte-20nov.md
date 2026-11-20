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

## Builds
(abajo)
