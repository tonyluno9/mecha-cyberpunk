# T-176 Usar las habilidades desde la cabina, con enfriamiento y munición del inventario
Dueño: Tony | Rama: tony/T-176-usar-habilidades | Modelo: Sonnet 5.5, medium
Depende de: T-174, T-175 (y T-167 EquipoMecha, ya en main)

## Objetivo
En plena pelea, el piloto lanza la habilidad de su brazo izquierdo con la tecla 1 y la de su torso con la 2. Cada uso gasta una munición de
su rama del inventario; la habilidad no se puede volver a usar hasta que pase su enfriamiento (más corto con piezas tech); sin munición no
sale nada y solo quedan los brazos. Es el criterio del diseño "Un misil no se puede volver a usar hasta que pase su cooldown, y cuando se
acaba la munición no dispara".

## Contexto
- Diseño: combate.md (criterio de arriba; "Te quedas sin munición en plena pelea: solo te quedan los brazos"; "al soltarlo usas palancas y
  botones de la cabina. En PC esas acciones van a teclas").
- Decisiones: D-tony-83 (kits: solo dentro de una pelea del flujo, con el jugador piloteando y el control activo), D-tony-118 y D-tony-130
  (`EquipoMecha` solo en `MechaEstacionado.prefab`), propuestas D-tony-135 (teclas 1 y 2), D-tony-138 (reglas de uso).
- Código modelo: `Mecha/Kits/UsoKitReparacion.cs` (en pelea por `EventosPelea`, `PermisoPiloto`, `ControlActivo`, gasta del
  `ServicioInventario` con `Registro<ServicioInventario>.Actual`, `event Cambio`) y `Mecha/Editor/CreadorKitReparacion.cs` (lo pone en
  `MechaJugador.prefab` y por herencia en `MechaEstacionado.prefab`; idempotente). Teclas: `Mecha/Editor/EntradaMechaEditor.cs` agrega
  acciones a `ControlesMecha.inputactions` sin regenerarlo. `ReglaHabilidad`, `DatosPieza.habilidad/municion` (T-174), `LanzadorHabilidades` (T-175).
- Pruebas modelo: `Tests/PlayMode/Mecha/KitReparacionPruebas.cs` y `EquipoMechaPruebas.cs` (`MechaEstacionado.prefab` en modo Directo, D-tony-130 e).

## Qué hacer
1. Acciones `Habilidad1` (`<Keyboard>/1`) y `Habilidad2` (`<Keyboard>/2`) en el mapa `Mecha` de `ControlesMecha.inputactions`, con
   `EntradaMechaEditor`. Comprueba que el mapa a pie (1 y 2 = puños y pistola) está apagado mientras se pilotea; si no, anótalo y apágalo en
   el lado del mecha (sin tocar Personaje).
2. `Mecha/Habilidades/UsoHabilidades.cs` (en la raíz del mecha, junto a `LanzadorHabilidades`): dos ranuras (`BrazoIzquierdo` → 1,
   `Torso` → 2) que leen la pieza puesta de `EquipoMecha` (sin `EquipoMecha` o con la pieza de serie: `SinHabilidad`). Por ranura:
   `Habilidad`, `Nivel`, `Municion` (cuántas hay en el inventario, -1 sin inventario), `EnfriamientoRestante`, `EnfriamientoTotal`, `Motivo`
   (el de `ReglaHabilidad.Decidir`, con "guardia arriba" solo para el brazo) y `event Action Cambio` (no en cada cuadro: al lanzar, al acabar el
   enfriamiento, al cambiar munición, piezas o pelea). `bool IntentarUsar(ranura)`: si el motivo es `Puede`, quita 1 munición, lanza con el
   nivel de la pieza y arranca el enfriamiento = `ReglaHabilidad.Enfriamiento(base, ReduccionEnfriamiento(piezas puestas))`. Si el
   `LanzadorHabilidades` dice que no, no gasta munición ni arranca el enfriamiento.
3. Reglas (D-tony-138): solo entre `PeleaIniciada` y `PeleaTerminada`; los enfriamientos se ponen en 0 al empezar cada pelea; con KO, sin
   control, aturdido o sin `PermisoPiloto` no sale nada; la munición se gasta al lanzar, pegue o no.
4. Creador `Mecha/Editor/CreadorHabilidadesMecha.cs` (idempotente): `LanzadorHabilidades` y `UsoHabilidades` en `MechaJugador.prefab` (y por
   herencia en `MechaEstacionado.prefab`), con los orígenes en el brazo izquierdo y el pecho del mecha (que el proyectil salga a la vista desde
   la cabina, a la izquierda). Los rivales no cambian.
5. PlayMode `Tests/PlayMode/Mecha/UsoHabilidadesPruebas.cs` con `MechaEstacionado.prefab` piloteado, un saco o un rival enfrente, un
   `ServicioInventario` y entrada simulada (teclas 1 y 2; clic derecho sostenido para la guardia):
   a) con las piezas de serie, 1 y 2 no hacen nada y el inventario no cambia;
   b) BRAZO IZQ. MILITAR I y 3 misiles, en pelea: 1 dispara (quedan 2 y el saco pierde 20); 1 otra vez a los 2 s no hace nada (`Enfriando`,
      quedan 2); a los 8.1 s sí (queda 1);
   c) con 0 misiles, 1 no dispara (`SinMunicion`) y el clic izquierdo sigue golpeando (solo los brazos);
   d) fuera de pelea (antes de `PeleaIniciada` y después de `PeleaTerminada`) 1 no dispara y no gasta;
   e) con la guardia arriba, 1 no sale (`GuardiaArriba`) y 2 sí (TORSO TECH I con celdas: el escudo se activa);
   f) con las 5 piezas tech IV, el enfriamiento del ESCUDO medido es 8 × 0.6 = 4.8 s (±0.1);
   g) al empezar una pelea nueva (`PeleaIniciada` otra vez) los enfriamientos están en 0;
   h) con el mecha `Directo` de Pruebas_Tony (sin `EquipoMecha`) 1 y 2 no hacen nada y no hay errores;
   i) cambiar el brazo izquierdo a FUEGO II en el taller cambia la ranura 1 a LANZALLAMAS II y su munición a combustible (sin errores).

## Carpetas permitidas
Assets/_Proyecto/Mecha/ (Habilidades/ nueva, Entrada/ solo con `EntradaMechaEditor`, Editor/, Prefabs/ solo con su creador),
Assets/_Proyecto/Tests/PlayMode/Mecha/, Assets/_Proyecto/Tests/EditMode/Mecha/
No tocar: Core/, Combate/ (lo de T-175 ya está; si falta algo, se anota y se pide una corrección), Economia/, UI/, IA/, escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] UsoHabilidadesPruebas a) a i) pasan; las pruebas de kits, encendido, equipo, cabina y daño persistente siguen en verde
      (`AREAS='Mecha UI Progresion' bash herramientas/verificar.sh playmode-por-area`)
- [ ] `MechaJugador.prefab` lleva un `LanzadorHabilidades` y un `UsoHabilidades`; los prefabs de rivales no; el creador corrido dos veces no
      cambia los prefabs ni `ControlesMecha.inputactions` (md5 igual)
- [ ] Con el jugador a pie, 1 y 2 siguen cambiando a puños y pistola (las pruebas de Personaje siguen en verde)

## Criterios de sensación (para un humano)
- Pruebas_Economia, con BRAZO IZQ. MILITAR I, 4 misiles y una pelea contra TORNILLO: ¿el misil sale del brazo izquierdo y se ve volar desde
  la cabina? ¿El enfriamiento de 8 s se siente justo o largo? ¿Quedarte sin misiles a media pelea te obliga a pelear con los brazos?

## Pruebas automáticas
- PlayMode: UsoHabilidadesPruebas.

## Fuera de alcance
- Pantalla de la cabina y ayudas (T-177). Habilidades a pie o en la calle fuera del flujo de pelea (la emboscada y la calle no las usan en este
  bloque). Apuntar con el ratón (las habilidades salen hacia donde mira el mecha).
