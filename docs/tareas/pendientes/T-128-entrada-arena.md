# T-128 El mecha de la historia entra a la arena
Dueño: Tony | Rama: tony/T-128-entrada-arena | Modelo: Sonnet 5.5, medium
Depende de: T-121, T-125, T-127

## Objetivo
Una pelea del clandestino se juega con el mecha de la historia (el que descansa en el taller y conserva su daño): con un fundido, el
jugador aparece ya dentro de su mecha en la arena, pelea sin revancha y, al terminar, otro fundido lo deja a pie en la salida del
clandestino mientras el mecha vuelve solo al taller con el daño que le quedó. Es lo que usan la pelea tutorial (T-132) y la pelea casera
(T-129), y resuelve "aparecer ya dentro del mecha" (pendiente de D-069).

## Contexto
- Diseño: demo.md (pelea tutorial imposible de perder; primera pelea casera que sí se puede perder), combate.md (el daño es persistente).
- Decisiones: D-042 (el director busca al jugador como el `IPeleador` del lado Jugador y la arena más cercana), D-060 (arranque
  Estacionado), D-061 (una pelea de arena deja el mecha encendido), D-062 (llamado: estados y regreso), D-063 (el calor solo corre en
  `EnCalle`), D-tony-42 y D-tony-59 (el daño persiste solo en el mecha de la historia). Propuesta nueva: D-tony-65.
- Piezas: `LlamadoMecha` y `EstadoLlamado` (Mecha/Llamado/), `MontajeMecha` (Mecha/Montaje/), `JugadorAPie.Subir/Bajar/Colocar`
  (Personaje/Jugador/), `DirectorPelea` (Pelea/Flujo/: `EmpezarPelea`, revancha con R, crea el rival desde `prefabRival` al empezar),
  `FlujoPeleaTutorial.prefab` (rival del tutorial con piedad, presentador, premio 1500), `PantallasHistoria.prefab` (T-125),
  `Pruebas_Prologo.unity` y su creador (T-127).

## Qué hacer
1. `EstadoLlamado.EnArena` (AL FINAL del enum) y en `LlamadoMecha`: `LlevarA(Pose)` lo pone en la pose al instante en `EnArena` (sin
   calor, sin regreso automático por lejanía, M no lo regresa, se puede subir); `VolverAlTallerYa()` lo deja al instante en su
   estacionamiento en `EnTaller` con la vida que tenga. Los demás estados no cambian de comportamiento.
2. `DirectorPelea`: `permitirRevancha` (serializado, true por defecto; en false no muestra "R: revancha" y R no hace nada) y `Reiniciar()`
   (desde Resultado: destruye el rival que creó, limpia la pantalla y vuelve a Preparando; después `EmpezarPelea` arranca una pelea nueva,
   con presentación si el config lo dice). `FlujoPeleaTutorial.prefab` queda con `permitirRevancha` en false (con su creador).
   `FlujoPelea.prefab` no cambia (Alan lo usa en Clandestino.unity con revancha).
3. `Progresion/Prologo/EntradaArena.cs` + `ConfigEntradaArena` (`Progresion/Prologo/Datos/ConfigEntradaArena_Base.asset`: id de la
   aparición de salida "Clandestino_Salida", 3 s tras el resultado):
   - `bool Empezar(DirectorPelea flujo)`: solo con el jugador a pie y ninguna entrada en curso (si no, false). Fundido a negro; el mecha
     de la historia (`LlamadoMecha` del `IMontable` de modo Mecha) va con `LlevarA` al `InicioJugador` de la arena; el jugador sube
     (`JugadorAPie.Subir`); `flujo.EmpezarPelea()`; fundido de vuelta.
   - Al terminar esa pelea (`EventosPelea.PeleaTerminada`) y pasados los segundos del config: fundido a negro, el jugador baja y se coloca
     en la aparición de salida (si falta, en el punto de subida del mecha y un warning), `VolverAlTallerYa()`, `flujo.Reiniciar()`, fundido
     de vuelta y evento `Terminada(ResultadoPelea)`.
   - Usa `PantallaFundido.Actual` (T-125); si no hay ninguna en la escena, instancia `PantallasHistoria.prefab` una vez.
   - Mientras dura, `EnCurso` es true. Si algo falta (arena, mecha, jugador) no empieza, avisa con un error claro y devuelve false.
4. Pruebas_Prologo (dentro de `CreadorEscenaPrologo.Crear`): `FlujoPeleaTutorial.prefab` (sin arrancar solo), `PantallasHistoria.prefab`,
   un objeto con `EntradaArena` y `Progresion/Prologo/Pruebas/EntradaArenaDePrueba` (tecla T: entrar a la pelea tutorial; solo pruebas).
5. PlayMode `Tests/PlayMode/Progresion/EntradaArenaPruebas.cs` en Pruebas_Prologo (entrada simulada para el combate; se permite rematar
   al rival con `RecibirDanio` una vez que el jugador ya recibió daño real) y casos nuevos en `FlujoPeleaPruebas` para `permitirRevancha`
   y `Reiniciar`.

## Carpetas permitidas
Assets/_Proyecto/Mecha/Llamado/, Assets/_Proyecto/Pelea/Flujo/, Assets/_Proyecto/Pelea/Editor/ y Pelea/Prefabs/FlujoPeleaTutorial.prefab
(con su creador), Assets/_Proyecto/Progresion/Prologo/ (con Datos/, Pruebas/ y Editor/), Assets/_Proyecto/Escenas/Tony/Pruebas_Prologo.unity
(con su creador), Assets/_Proyecto/Tests/PlayMode/Progresion/, Tests/PlayMode/Pelea/, Tests/PlayMode/Mecha/, docs/capturas/
No tocar: Core/, todo lo de Alan, `FlujoPelea.prefab`, Pruebas_Tony.unity, Pruebas_Calle.unity.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (las pruebas de llamado, calor, emboscada y flujo de pelea siguen en verde)
- [ ] EntradaArenaPruebas: desde a pie en la casa, `Empezar(tutorial)`: tras el fundido el jugador va en modo Mecha y el mecha está a menos
      de 0.1 m del `InicioJugador`; hay presentación y conteo; el jugador pelea con entrada simulada y gana; 3 s después está a pie a menos
      de 0.5 m de "Clandestino_Salida", el mecha en `EnTaller` a menos de 0.1 m del estacionamiento y con la vida con que terminó (±0.01),
      el rival ya no existe y el flujo está en Preparando
- [ ] Durante la pelea el calor sigue en 0 y R no arranca una revancha; "R: revancha" no aparece en la pantalla
- [ ] Una segunda `Empezar` funciona igual (un solo rival en escena) y `Empezar` con el jugador en el mecha devuelve false
- [ ] Un solo `AudioListener` activo en todo momento; consola sin errores
- [ ] FlujoPeleaPruebas: con `permitirRevancha` en false R no hace nada; `Reiniciar` deja Preparando y destruye el rival creado
- [ ] Captura docs/capturas/T-128-1.png: la cabina en la arena durante la presentación del tutorial

## Criterios de sensación (para un humano)
- Pruebas_Prologo, Play, T: ¿aparecer dentro del mecha en la arena con el fundido se siente como "empieza el campeonato" y no como
  teletransporte raro? Al terminar, ¿quedar a pie en la salida con el mecha de vuelta en el taller se entiende sin explicación?

## Pruebas automáticas
- PlayMode: EntradaArenaPruebas; casos nuevos de FlujoPeleaPruebas y, si cambia algo del llamado, LlamadoMechaPruebas.

## Fuera de alcance
- La pelea casera y el mostrador (T-129); el director que decide cuándo entrar (T-132); que el equipo repare el mecha tras el tutorial
