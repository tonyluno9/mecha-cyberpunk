# Reporte: bugs del inicio de partida

Rama `integ/inicio` → main. Capturas en `docs/capturas/_inicio/build-demo/` (Demo) y `docs/capturas/_inicio/build-juego/` (Juego, a 1366×768). En cada carpeta, `resultados.txt` tiene lo medido.

## 1. CONTINUAR: el jugador aparecía en blanco y caía por el mapa
**La causa, comprobada con la partida real de Tony** (`partida_auto.json` del 20-nov):
- El jugador estaba guardado en (-42.8, 0.1, **154.1**). Es la zona de afuera que se quitó el 21-nov: el muro ahora llega a z = ±88 y todo lo de afuera está apagado.
- Al continuar, `JugadorAPie.Cargar` buscaba piso debajo con un rayo. Si no encontraba nada, lo ponía en el punto guardado tal cual. Lo dejaba en el vacío (solo cielo, "en blanco") y caía sin fin.
- No había ninguna comprobación de piso, de que el jugador cupiera ni de que estuviera dentro del mapa, ni red de seguridad.
- La escena sí terminaba de cargar antes de soltar al jugador (`ServicioGuardado` carga la partida en `Start`, con la escena ya activa). No era un problema de tiempos.

**Arreglo** (Personaje/Jugador):
- **`ReglaPosicionSegura`** (lógica pura): un punto vale solo si cumple cuatro condiciones:
  - hay piso sólido debajo;
  - el piso no está bajo el mapa (menos de -20 m);
  - está dentro del mapa, es decir, en una zona del territorio (en escenas sin territorio, como el clandestino, esto no se pide);
  - la cápsula del jugador cabe ahí, sin estar dentro de una pared, un carro o un mueble.
- **`JugadorAPie.Cargar`:**
  - mide el punto guardado;
  - si no vale, busca el punto válido más cercano en un radio de 12 m (anillos cada 1.5 m);
  - si no hay ninguno, no lo mueve y aparece en el inicio de la escena, en la calle frente a su casa;
  - deja en la consola por qué se descartó;
  - si la escena guardada ya no existe, ya no se movía antes, y sigue igual.
- **`RedSeguridadJugador`** (la agrega `JugadorAPie` solo):
  - cada 0.5 s, si pisa piso válido, guarda ese punto como seguro;
  - si el jugador baja de -20 m, o cae más de 30 m por debajo de ese punto, lo regresa ahí;
  - empieza con el punto donde lo puso la escena.

**Partidas viejas:**
- La de Tony (fuera del muro) carga su progreso ($6,510, implantes) y lo pone de pie en la calle frente al Canepa, sin caer.
- Una posición dentro de algo (como el cuarto que se quitó) va al punto libre más cercano. Lo cubre la prueba PlayMode.

## 2. NUEVA PARTIDA pasaba por el menú de personalización
**La causa:** `MenuInicio.NuevaPartida` abría la pantalla de creación siempre que existía en el menú.

**Arreglo:**
- NUEVA PARTIDA ahora va directo a la pantalla de carga y a `FlujoPartida.IrANuevaPartida()`, con el personaje por defecto. Empieza levantándose en la calle.
- La pantalla de creación (`PantallaCreacionPandilla`) sigue en el proyecto y en el menú, pero fuera de este flujo.

## Pruebas
- EditMode `ReglaPosicionSeguraPruebas` 3/3: los motivos, el orden de los candidatos y la red de seguridad.
- PlayMode:
  - `PosicionSeguraJugadorPruebas` 4/4: posición válida; sin piso (se queda en el inicio, no cae); dentro de una caja (al punto seguro más cercano); bajo el mapa (la red lo regresa).
  - `AparicionJugadorPruebas` 6/6.
  - `InicioPruebas` 11/11. La prueba de nueva partida ahora exige ir directo a la carga sin creación (decisión de Tony).
- **En el build** (`UI/Pruebas/PruebaInicioBuild`, solo con `-pruebaInicio <carpeta> -partidaVieja <archivo>`). Usa los botones reales del menú y guarda las partidas en la carpeta de la prueba, nunca en las de Tony:

| Paso | Demo (2560×1600) | Juego (1366×768) |
| --- | --- | --- |
| A) CONTINUAR con la partida vieja real (z = 154) | Aparece en (-55.5, 0.28, -77), sobre el piso y dentro del muro. A los 6 s sigue ahí | Igual |
| B) NUEVA PARTIDA | Estado "Cargando", sin creación. Carga en 3.0 s y se levanta en la calle | Igual |
| B) Guardar en la ranura 1 | True | True |
| C) Salir al menú y CONTINUAR | Ofrece la ranura 1. Aparece a **0.00 m** de donde guardó, sobre el piso | Igual |

Capturas: `A0_menu_con_partida_vieja`, `A1`/`A2_continuar_vieja`, `B1_nueva_levantandose`, `B2_nueva_en_la_calle`, `C0_menu` y `C1_continuar`.

## Builds
`Builds/Demo/CyberCity.exe` y `Builds/Juego/CyberCity.exe`, construidos uno por uno desde `.sesiones/s2`, con "Build Finished, Result: Success" y probados como arriba.

Para construir hubo que liberar disco: había 147 MB libres y el primer intento falló. Borré mis worktrees de subagentes (`wt23`, ya integrados) y mi build de medición. `Builds/Demo.viejo` (5.5 GB) no lo toqué porque no es mío.

## Qué probar (Tony)
1. Con tu partida de siempre: CONTINUAR. Debes aparecer en la calle frente al Canepa con tu progreso.
2. NUEVA PARTIDA: entra directo a la carga y te levantas en la calle.
3. Guardar, salir al menú y CONTINUAR: apareces donde guardaste.
