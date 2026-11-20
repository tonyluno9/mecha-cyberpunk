# Reporte 20-nov B: pendientes de playtest-20nov.md (sin Blender ni chat de arte)

Todo está en `main` (merge --no-ff de `integ/20nov-C`). Tareas T-1520 a T-1529, una rama `tony/T-xxxx` cada una, hechas por
subagentes Sonnet, integradas, compiladas y probadas en `.sesiones/s2`. Progreso detallado en `docs/progreso-20nov-B.md`.

**Cómo se comprobó**: cada punto se miró en el juego con Unity en Play (editor vivo en s2), con el jugador puesto en el lugar y la
cámara del jugador, y una captura en `docs/capturas/_20nov-B/`. Donde no se pudo ver en una captura, se midió en el juego (valor
leído en Play) o solo con pruebas, y así lo digo. Lo de "Le gustó" no se tocó (el muro y su torre, las ondas del agua, la rueda de armas).

## Paso 0

- Los cambios sin guardar de la carpeta principal (entregas del chat de Blender) quedaron en `tony/blender-20nov` y en `main`.
  Fueron 5 commits porque GitHub no acepta un push de más de 2 GB.
- **No subí**: `Audio-20261031T070509Z-1-001.zip` (142 MB, GitHub no acepta archivos de más de 100 MB), `Logs_v121.txt`,
  `__pycache__`, los respaldos `.blend1` y `imageassets/blender/_REVISION_TONY/revision_personajes.blend` (140 MB; el chat de
  Blender lo estaba editando). Siguen en disco.
- El chat de Blender estaba trabajando en la carpeta principal, así que el merge a main se hizo sin checkout y la integración en `.sesiones/s2`.

## Resultado por punto

| # | Pedido | Quedó | Captura / evidencia |
| --- | --- | --- | --- |
| 1 | Sin cuarto; empezar levantándose en la calle | **Sí** | `01-inicio-2/3.png`, `01c-inicio-de-pie.png` |
| 2 | Doc a escala; civiles parejos | **Sí** (medido) | `02-doc-junto-al-jugador.png`, `02-civiles-de-cerca.png` |
| 2 | Civil acostada bugueada | **No reproducida** | guarda nueva (ver abajo) |
| 3 | Armería: mostradores y armero | **Sí** | `03-armeria-entrada.png`, `03-armeria-mostrador.png` |
| 4 | Formas básicas y edificios de cubo | **En parte** | `04-ciudad-*.png`, `04-edificios-relleno-ED.png`, `docs/primitivas-20nov-B.md` |
| 4 | Nada flotando | **Sí** (diagnóstico: 0) | `docs/primitivas-20nov-B.md` |
| 4 | Muro cerrado, puertas sólidas | **Sí** | `04-muro-puerta-norte.png`, `04-muro-puerta-este.png` |
| 4 | Costales CB02 | **Sí** | `04-costales-cb02.png` |
| 4 | Agua: sin salpicadura, ondas en piso mojado | **Sí** | `04-agua-onda-piso-mojado.png` |
| 5 | Carros solo dentro del muro | **Sí** (medido en Play) | log y conteo |
| 5 | Patrulla configurada como civil | **Sí** (medido) | lista del tráfico |
| 5 | Sonido al subir al carro | **Cambiado, no escuchado** | código |
| 6 | Balas a la mira; colliders de postes | **Hecho, no comprobado disparando** | pruebas y prefabs |
| 6 | Francotirador: daño y zoom | **Daño sí (dato); zoom solo con prueba** | pruebas |
| 6 | Patrulla de lejos recibe daño | **Hecho, no comprobado disparando** | código |
| 6 | Una arma por tipo en la ruleta | **Sí** (medido en Play) | arsenal |
| 6 | Rueda suena a cámara lenta | **Sí** (id medido; el sonido es sintético) | código |
| 7 | Estrellas más despacio | **Sí** (medido en Play) | valor |
| 7 | Maleantes provocan primero | **Sí** | `07-maleantes-provocan.png` |
| 7 | Drones con el indicador de daño, solo con visor | **Solo con prueba** | EditMode |
| 8 | Consumibles en la tiendita ED02 | **Sí** | `08-tiendita-ED02.png` |
| 8 | Sin números ni texto repetido en consumibles | **Sí con prueba; sin captura** (la captura de cámara no incluye el HUD) | PlayMode |
| 8 | Segundo consumible (papitas tras la bebida) | **Sí con prueba** | PlayMode |
| 8 | Prótesis dan vida | **Sí** (medido en Play) | `08-protesis-laser-vida165.png` |

## Detalle

### 1. Inicio (T-1520)
- **Por qué seguía el cuarto**: T-1496 solo había *apagado* `Cuarto_Habitacion`. `Menu_Calles` tenía su propia copia y la casa de Alan
  (`Barrio`, `Lugar_Casa`) seguía con la sala, la cama y el espejo encendidos. T-1514 no movió nada (su contenedor quedó vacío).
- Ahora se **borraron** los 75 objetos del cuarto en `Ciudad_Prologo` y los 78 de `Menu_Calles`, más el espejo `S2t_Espejo`. La sala y la
  cama vieja quedaron apagadas (son del prefab de Alan).
- Partida nueva: aparece en `Inicio_Calle` (-53, 0, -76.5), en la calle frente a la casa. Medido en Play: `PJ01_Muerte_Desmayo` (tirado)
  → `PJ01_Dano_Levantarse_BocaArriba` → idle con el control. Morir sigue llevando a la clínica del Doc.
- Dormir y espejo están junto a la puerta de la casa, sin modelo. Si la escena se descarga a media levantada, la pantalla ya no se queda en negro.

### 2. Escalas (T-1521)
- Todos los humanos se midieron **animados y de pie**, no por bounds (`Logs/t1521-alturas.txt`): jugador 1.80, Doc 1.80, civiles 1.66-1.80,
  pandilleros 1.78-1.82. Se corrigió la escala del modelo en cada prefab `Visual_*`. El Doc tenía 1.09 veces la altura del jugador a propósito
  (T-812): eso se quitó.
- En Play: la cabeza del Doc está a 1.51 m de sus pies y la del jugador a 1.53 m.
- Los de Nova (E15, E16, E18 y el guardia de Nova de 2.4 m) se quedaron como están, más altos a propósito.
- **Civil acostada**: no la pude reproducir y no encontré la causa en la escena. Quedó una guarda: un civil vivo con la cabeza a menos de 0.5 m
  del suelo más de 2.5 s se levanta solo y escribe `[IA] T-1521 ... estaba vivo y acostado` en la consola. **Si la vuelves a ver, dime dónde.**

### 3. Armería (T-1522)
- Los racks de rifles de la pared (`I18`) regresaron a como estaban antes de T-1503. Las vitrinas de enfrente del armero (`I17`) se giraron 180°.
  El armero se pegó a la pared (x -28.5 → -29.31). En la captura, la vitrina muestra el vidrio y las armas hacia el jugador.

### 4. Ciudad
- **Formas básicas (T-1523)**. Cuenta completa en `docs/primitivas-20nov-B.md`.
  - Por cambiar (edificios y props hechos con cubos): **845 antes → 36 después**. Cambiados: 61 props por modelos (huacales, toldos, bolardos,
    postes, barreras) y 184 piezas chicas apagadas. Los 69 "muñecos" de cubo del público del Clandestino se apagaron.
  - **9 edificios de cubo** (3 torres de 20-30 m en el Mercado, 3 bloques de las salidas y una caja de azotea) se rellenaron con modelos ED
    apilados o se apagaron.
  - Se quedan a propósito: las calcomanías planas (rayas, charcos, ventanas), los focos y letreros, las losas de piso y las tragamonedas
    (esperan TR01/TR02).
  - Un primer intento apagó el asfalto, la autopista y los puentes. Lo vi en el log y lo regresé antes de seguir: la estructura no se toca.
  - El fondo del menú (`Inicio`) ya no son cajas: usa ED, NV01, P11, P12, carros V02-V10, drones y la antena.
  - El maletín, el dinero y la munición del piso usan modelos (MA01 y CB03; son sustitutos, no hay modelo propio de billetes).
  - **Lo que NO quedó** (lo vi en las capturas):
    - **143 casas generadas como cajas** (`S2c_Suburbios`, `S2c_Mercado`, `S2c_CalleDorada`, `T1102_CalleDoc`, `S2c_Nova`). De frente tienen
      su fachada del kit; de lado y por detrás son una caja de un color (`04-costales-cb02.png` las enseña al fondo). Cambiarlas por ED
      rehace todas las calles: no lo hice sin preguntarte.
    - **Las fachadas del Clandestino, el Bar y el Mercado Negro** son cubos con textura (`04-pendiente-clandestino.png`). Tienen interior y
      entrada, y un ED los taparía. Hace falta un modelo con entrada (chat de arte).
- **Flotantes**: se asentaron 26 objetos. El diagnóstico da 0 flotantes y 0 objetos sobre 60 m.
- **Muro (T-1524)**: había 6 huecos (las 6 salidas). Cada puerta MU03 tiene ahora un collider sólido de 10 m que cubre el vano, y el muro viejo
  sube a 10 m de collider. En Play empujé al jugador 30 m contra cada puerta: se queda a 2 m de la línea del muro en las 6. La autopista
  elevada corre por fuera del muro norte y no se llega desde adentro.
- **Costales (T-1525)**: el material era metálico y brillante (metallic 1, smoothness 0.8). Ahora es tela mate con detalle de yute.
- **Agua (T-1525)**: no hay salpicadura (el jugador ya no tiene ese sistema de partículas). Las ondas al pisar salen también en asfalto mojado,
  banquetas mojadas y calcomanías de charco. En la captura se ve el anillo en una calcomanía de charco sin shader de agua. En carro no
  (Vehiculos no tiene detección de superficie).

### 5. Carros (T-1526)
- **Dentro del muro**: el recorte de rutas no corría nunca. Solo quedaban 2 nodos adentro y se saltaba sin aviso. En Play, 4 de 6 carros estaban
  siempre a ±220 m. Hicieron falta 5 vueltas para dejarlo bien:
  - Las calles de afuera se juntan con los finales de calle de la cuadrícula, 1 m dentro del muro.
  - Los tramos largos se parten (hay más lugares donde aparecer).
  - En los finales de calle el carro da vuelta en U.
  - Todo carro o patrulla que quede fuera se recicla adentro.
  - Medido en Play durante 3 min: **0 de 6 carros y 0 de 3 patrullas fuera, y se mueven**. El log dice: 14 nodos, 6 fusionados, 6 movidos y
    14 intermedios.
- **Patrulla civil**: V05, V06 y V08 son modelos de policía y estaban puestos como carros civiles estacionados en la escena. Se cambiaron por
  V02, V03 y C19, y el tráfico civil ya no acepta ningún modelo de policía. La lista del tráfico medida en Play: V01-V04, V09, V10 y colores.
  Con estrellas, la patrulla más cercana reaccionó (bajaron los policías).
- **Sonido al subir**: antes sonaba `VEH_PUERTA_ABRIR`. Ahora suena `VEH_PUERTA_CERRAR` (portazo) y luego el arranque de siempre. Al bajar,
  la puerta abriendo. **No lo escuché**; si el que no te gusta es el arranque, dime.

### 6. Armas (T-1527)
- **Balas a la mira**: el rayo de la mira sale de la altura del jugador (no de la cámara, para no pegar en lo que está entre cámara y jugador).
  La bala va de la boca del arma a ese punto y solo pega antes si algo de verdad está en medio.
  - Postes, faroles, semáforos y transformadores tenían una caja del tamaño de todo el modelo, con el brazo incluido. Por ejemplo, el C07 tenía
    una caja de 2.9 × 7 × 1.7 m. Ahora llevan una cápsula de 0.2 m en la base real del poste. Árboles y el farol ML01 conservan su tronco de cápsula.
  - **No lo probé disparando a 100 m**.
- **Francotirador**: daño 70/85/100/120 → 40/44/48/52. Un pandillero de 60 de vida no muere de un tiro al cuerpo; de un tiro a la cabeza
  (x3) sí. Apuntando con el francotirador, la rueda del ratón hace zoom (FOV 3 a 24) y no cambia de arma. El zoom está probado en EditMode,
  **no en el juego**. No encontré un control con cruceta configurado, así que lo tomé como la rueda del ratón.
- **Patrulla de lejos**: los vehículos ya no pierden daño con la distancia y se alcanzan hasta el doble del alcance del arma. **No lo comprobé
  disparando**; si sigue sin hacerle nada, la causa es otra.
- **Una por tipo**: medido en Play. Al darle todas las armas quedan 8: el revólver reemplaza a la pistola y el rifle de plasma al lanzagranadas.
  La que se reemplaza pierde sus mejoras.
- **Sonido de la rueda**: `UI_CAMARA_LENTA` en lugar del golpe (`PUN_GOLPE_AIRE`). No había ningún sonido de cámara lenta, así que es un
  barrido grave **sintetizado**: cámbialo si tienes uno mejor (`Assets/Arte/Audio/UI/UI_CAMARA_LENTA.wav`).

### 7. Enemigos (T-1528)
- **Estrellas**: las acciones suman la mitad de calor. Medido en Play sobre el servicio de calor: una pelea de 20 suma 10; lo que pide una misión
  suma completo. El calor de la calle solo cuenta con testigos, como antes.
- **Maleantes**: SKULLS y SNAKES de calle (acoso y riñas) se acercan, gritan y empujan (1 punto) y no sacan arma. Si les pegas, sacas arma o
  disparas cerca, atacan. Al principio hacen 35 % del daño, con 50 % de puntería y más pausa, y llegan al 100 % en 90 s.
  - Medido en Play: tres llegaron a 2.3 m, ninguno hostil, y en 14 s la vida bajó de 120 a 109 (empujones), sin disparos.
  - Lo encontré en el juego: la ciudad usa otros datos (E19E20 y E10) que T-1528 no había cambiado. Ya están.
  - Jefes, Nova, escoltas del jefe y enemigos de misión no cambian.
  - Bug arreglado: "hostil desde el inicio" dejaba a los maleantes sin atacar nunca en los primeros 90 s de juego.
- **Drones**: el cuadro rojo "DRON" se quitó. Con el visor puesto (implante de ojos y habilidad CABEZA) el dron marca su dirección con el mismo
  indicador de daño; sin visor, nada. **Probado solo en EditMode, no en el juego.**

### 8. Tienda e implantes (T-1529)
- **Tiendita**: un ED02 en el lote baldío de la esquina de la calle del Doc (-120.5, -74; suroeste), con mostrador y tendero. Ahí se compran los
  5 consumibles. Las 20 máquinas expendedoras quedaron de decorado (medido: el único vendedor en la escena es el mostrador de la tiendita).
  La primera versión caía en medio de la calle; la moví.
- **Sin números**: se quitó la línea "[E] BEBIDA x2 [C] PAPITAS x3" y la cantidad en cada casilla; queda el ícono con su tecla.
  Comprobado con prueba PlayMode, **no en captura** (la captura de la cámara no incluye el HUD).
- **Segundo consumible**: al acabarse la bebida (E), las papitas pasaban solas a la casilla E y la C quedaba vacía. Ahora un objeto no cambia
  solo de casilla mientras lo lleves. Prueba PlayMode "bebida y luego papitas" en verde.
- **Prótesis**: +15 de vida máxima cada prótesis de ataque. Medido en Play: 150 → 165 con el brazo láser. Los implantes dan +10 (nivel I) o +25 (nivel II).

## Pruebas
- **EditMode completo**: ver el número final en `docs/progreso-20nov-B.md`. Las únicas fallas que quedan son las 8 que ya fallaban en main:
  V22 x3, patrulla PA01 x3, el clip `PJ01_Moto_Manejar` y los costales CB02 en la calle de Nova S2c (T-1511).
- **PlayMode, por clase y en tandas** (así lo pide la memoria del proyecto): verde cada clase corrida sola. Juntas en una tanda fallan por orden:
  `CalorMecha` deja al jugador como si fuera en vehículo.
  - Arreglos reales que salieron de aquí: las ranuras de consumibles, la pantalla negra a media levantada y lo de hostil en los primeros 90 s.
  - **Abiertas**: 3 pruebas de `BalasCiudadT711` (balas de SKULL contra carros y casas de la ciudad real) y `PandillaArmada` (paga 500 en vez
    de 580). No sé si ya fallaban antes; no las pude atribuir a un cambio.
  - **Tráfico, tanda de 27**. Con el código de tráfico de main ya fallaban 6: `PersecucionBarrio` x3-4, `CierresCiudadT986` (por las puertas ahora
    sólidas) y un atorón de 126 s.
    - Con T-1526 fallan 8. Las 4 que agrega son de "dónde aparece un carro":
      - `TraficoCalle`: 5 carros en vez de 6, y 0 de día en la prueba de "ninguno en la calle del jugador".
      - `TraficoFluido`: no encuentra dónde salir fuera de la vista, y no se destraba saliendo en otra calle.
    - Causa: las calles de afuera eran el lugar donde aparecían los carros fuera de la vista; ahora se juntan con los finales de calle,
      dentro del muro.
    - En Play hay 6 carros y 3 patrullas, todos dentro, y se mueven. Un carro en (-51, -10) no se movió en 3 min: revisar. Si notas menos
      tráfico o carros que aparecen a la vista, es esto.

## Qué probar
1. Partida nueva desde el menú: te levantas en la calle, sin cuarto.
2. Muere: el Doc mide como tú. Camina entre civiles y busca a la civil acostada (si sale, mira la consola).
3. Armería desde la puerta.
4. Recorre la ciudad: dime qué cajas siguen molestando (las 143 casas generadas y las fachadas del Clandestino y el Bar son las conocidas).
   Intenta salir por las 6 puertas, a pie y en carro.
5. Carros: que ninguno salga del muro ni se quede atorado en los finales de calle.
6. Dispara a 100 m con un poste cerca; francotirador apuntando con la rueda; dispárale a una patrulla de lejos.
7. Acércate a un grupo de SKULLS: deben empujarte sin disparar; pégale a uno y atacan.
8. Visor con drones. Tiendita de la calle del Doc. Bebida y luego papitas con C. Brazo láser y tu vida máxima.

## Builds
Ver al final de `docs/progreso-20nov-B.md`.
