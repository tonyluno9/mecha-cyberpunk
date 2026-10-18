# T-165 Cambiar los datos de un mecha en juego sin perder el daño
Dueño: Tony | Rama: tony/T-165-cambiar-datos-mecha | Modelo: Sonnet 5.5, medium
Depende de: nada (puede ir mientras T-163 espera aprobación)

## Objetivo
A un mecha ya armado se le pueden cambiar sus `DatosMecha` en juego y todo lo que los lee se entera: la vida máxima cambia conservando
los puntos de daño que traía, el golpe pega con el nuevo daño y la nueva recarga, la guardia sube con su nuevo tiempo y el movimiento usa
la nueva velocidad. Es lo que hace falta para que cambiar una pieza en el taller (T-167) cambie de verdad al mecha.

## Contexto
- Diseño: mecha-taller.md ("Compras las mejoras en el taller y las ves al subirte al mecha"), combate.md ("El daño es persistente hasta que reparas").
- Decisiones: D-tony-42 y D-tony-59 (daño persistente; piso de emergencia), D-tony-43 (mecha dañado). Propuesta nueva: D-tony-119 (al
  cambiar una pieza se conservan los puntos de daño).
- Código que lee `DatosMecha` hoy: `Combate/Vida/VidaMecha.cs` (`Configurar` reinicia la vida al máximo: no sirve aquí),
  `Combate/Golpe/GolpeMecha.cs` (crea su `CicloGolpe` en `OnEnable`), `Combate/Bloqueo/GuardiaMecha.cs` (lee `TiempoSubirGuardia`),
  `Mecha/Movimiento/ControladorMecha.cs` (tiene su PROPIA referencia `datos`, separada de la de `VidaMecha`), `PeleadorMecha.Nombre`.
  Los rivales (`RivalBasico`) no cambian.
- `Mecha/Estado/ReglaVidaPersistente.cs`: estilo de las cuentas puras de vida.

## Qué hacer
1. `VidaMecha.CambiarDatos(DatosMecha nuevos)`: cambia los datos sin restaurar: conserva los puntos de daño (`daño = máxima − actual`),
   la nueva actual es `nuevaMaxima − daño` limitada a [1, nuevaMaxima] (si el mecha estaba en KO, sigue en KO). Cuenta pura en una regla
   nueva de Combate (por ejemplo `Combate/Vida/ReglaCambioDatos.cs`). Avisa `Cambio` y un evento nuevo `DatosCambiados`. Null no hace nada
   (warning). `Configurar` sigue igual.
2. `GolpeMecha` y `GuardiaMecha` escuchan `DatosCambiados` y rehacen lo que calcularon con los datos viejos (ciclo de golpe con la nueva
   preparación y recarga; tiempo de subir guardia). Un golpe a medias termina con su ciclo viejo o se reinicia; decide y anótalo.
3. `ControladorMecha`: si el objeto tiene `VidaMecha`, usa `VidaMecha.Datos` (y lo sigue con `DatosCambiados`); su campo `datos` queda
   solo para un mecha sin vida. No cambies el prefab a mano: si hace falta, con su creador.
4. EditMode `Tests/EditMode/Combate/ReglaCambioDatosPruebas.cs` (100/100 → 130 conserva 0 de daño; 60/100 → 130 da 90/130; 60/100 → 50 da
   10/50; 20/100 → 50 da 1/50, nunca 0; en KO sigue en KO).
5. PlayMode `Tests/PlayMode/Mecha/CambiarDatosMechaPruebas.cs` con `MechaJugador.prefab` y un `SacoDePrueba`, con entrada simulada y dos
   assets de prueba (el de serie y uno con más vida, daño y velocidad, creados en la prueba o con un creador de Pruebas/):
   a) con daño de 40, `CambiarDatos` a vida 130: vida 90/130 y la pantalla de vida de la cabina lo muestra;
   b) clic izquierdo al saco: le quita el daño nuevo (no el viejo);
   c) W sostenida 3 s: la velocidad horizontal medida llega a la nueva `VelocidadMaxima` (±0.05 m/s) y no la pasa;
   d) cambiar a los datos de serie lo deja como antes (vida máxima 100 con el mismo daño).

## Carpetas permitidas
Assets/_Proyecto/Combate/ (Vida/, Golpe/, Bloqueo/, Pruebas/), Assets/_Proyecto/Mecha/Movimiento/, Assets/_Proyecto/Mecha/Editor/ (solo si
el prefab necesita un cambio, con su creador), Assets/_Proyecto/Tests/EditMode/Combate/, Assets/_Proyecto/Tests/PlayMode/Mecha/,
Assets/_Proyecto/Tests/PlayMode/Combate/
No tocar: Core/, IA/, Pelea/, UI/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] ReglaCambioDatosPruebas y CambiarDatosMechaPruebas a) a d) pasan; las pruebas de Combate, Mecha e IA que ya existían siguen en verde
      (`AREAS='Combate Mecha IA' bash herramientas/verificar.sh playmode-por-area`)
- [ ] Grep: `ControladorMecha` ya no puede quedar con datos distintos de los de su `VidaMecha` (una prueba lo comprueba tras `CambiarDatos`)
- [ ] Tras la suite, `git status` no muestra prefabs, materiales ni assets `DatosMecha_*` modificados

## Criterios de sensación (para un humano)
- Ninguno todavía (el cambio se ve con las piezas, T-167 a T-169).

## Pruebas automáticas
- EditMode: ReglaCambioDatosPruebas. PlayMode: CambiarDatosMechaPruebas.

## Fuera de alcance
- Las piezas y su suma (T-166, T-167). Cambiar datos en plena pelea (solo se hará en el taller). Los rivales.
