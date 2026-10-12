# T-050 Contrato para subir y bajar (carro y mecha), daño de disparo y ubicación del objetivo
Dueño: Tony (tarea de Core, D-032) | Rama: tony/T-050-contrato-subir-bajar | Modelo: Sonnet 5.5, medium
Depende de: nada (T-031, T-032 y T-034 ya están en main)

## Objetivo
En Core/ existe el contrato con el que el jugador se sube a algo y se baja de él (`IMontable`): lo implementarán el carro de
Alan (A-F2) y el mecha de Tony (T-F4), y lo usa el lado del jugador (T-058). También el daño de disparo para la pistola a pie
y un ayudante único para saber dónde está el objetivo de la historia, que usan el HUD de Tony y, más adelante, los
marcadores del mundo de Alan. Los avisos de "nadie lo registra todavía" de `IJugador` e `IObjetivos` se actualizan.

## Contexto
- Decisiones: D-026, D-027 (quien posee el dato lo implementa), D-029, D-032. Propuestas nuevas: D-tony-14 (IMontable),
  D-tony-15 (daño a pie), D-tony-16 (UbicacionObjetivo) en docs/decisiones-pendientes-tony.md.
- Se reutiliza lo que ya hay: `IJugador` (con `Modo`, `Camara`, `ModoCambio` y `Colocar`) y `ModoJugador` (APie, Carro, Mecha)
  NO cambian de forma; el cambio de modo lo hace quien implementa `IJugador` (Tony) usando `IMontable`.
- Lee primero: Core/Jugador/IJugador.cs, Core/Jugador/ModoJugador.cs, Core/Objetivos/IObjetivos.cs, Core/Objetivos/Objetivo.cs,
  Core/Mundo/ILugar.cs, Core/Danio/IRecibeDanio.cs, Core/Danio/TipoDanio.cs, Core/Pelea/EventosPelea.cs.
- Los comentarios `///` SON el contrato. El revisor los lee con la mirada de Alan: ¿le alcanzan para hacer su carro sin preguntar?
- Hecho importante para el comentario de daño: `DirectorPelea` y `RivalBasico` buscan al jugador como el `IPeleador` de lado
  Jugador entre todos los MonoBehaviour. Por eso el jugador a pie NO será `IPeleador`, y lo que no es peleador publica el
  impacto con `objetivo` null. Los suscriptores actuales (SacudidaCabina, PresentacionRival, SacoDePrueba) ya comparan
  `objetivo` con el suyo, así que null no les afecta: compruébalo leyéndolos, sin editarlos.
- Es una tarea de Core: se integra a main en cuanto el verificador la apruebe (Alan la necesita para A-F2).

## Qué hacer
1. `Core/Jugador/IMontable.cs`: algo a lo que el jugador se sube (carro o mecha). Miembros y contrato en los `///`:
   - `ModoJugador Modo` (Carro o Mecha; nunca APie), `string Nombre` (para el HUD, por ejemplo "Carro viejo").
   - `Vector3 PuntoSubida`: punto en el piso junto a la puerta del conductor (o la escotilla), del lado de fuera, en metros.
     El lado del jugador decide desde qué distancia se puede subir (no es parte del contrato).
   - `bool Ocupado`, `bool PuedeSubir` (false si está ocupado, cerrado o todavía no disponible, por ejemplo el mecha llegando),
     `bool PuedeBajar` (false si va demasiado rápido o en pleno encendido).
   - `Transform Asiento`: mientras el jugador va dentro, su raíz sigue a este Transform (posición y rotación) en `LateUpdate`;
     el montable no hace nada con el jugador.
   - `Camera Camara`: la cámara del montable. Apagada (y su `AudioListener` también) mientras no está ocupado.
   - `bool IntentarSubir(IJugador jugador)`: solo si `PuedeSubir`; prende su cámara y su `AudioListener`, empieza a leer su
     propia entrada, queda `Ocupado` y avisa `OcupadoCambio(true)`. Devuelve si subió.
   - `bool IntentarBajar(out Pose salida)`: solo si `Ocupado` y `PuedeBajar`; apaga su cámara, su `AudioListener` y su entrada,
     deja de estar ocupado, avisa `OcupadoCambio(false)` y devuelve en `salida` una pose en el piso, libre de obstáculos, donde
     dejar al jugador. Devuelve si bajó.
   - `event Action<bool> OcupadoCambio`.
   - Reglas en el comentario de la interfaz: se agrega a `Registro<IMontable>` en `OnEnable` y se quita en `OnDisable`.
     La tecla Interactuar (E) es del jugador: el montable no la lee. Quien implementa `IJugador` llama a `IntentarSubir` e
     `IntentarBajar`, apaga su propia cámara, su `AudioListener` y su `CharacterController` mientras va dentro, sigue al
     `Asiento`, cambia `IJugador.Modo` y avisa `ModoCambio`. Solo hay un `AudioListener` prendido a la vez.
   - Aviso para el mundo, en el comentario: en modo Carro o Mecha el `CharacterController` del jugador está apagado y los
     triggers no lo ven; para saber si el jugador en carro está en una zona, usa `Registro<IJugador>.Actual.Raiz.position`
     (sigue al asiento) o los colliders del propio montable.
2. `Core/Jugador/IJugador.cs`: solo comentarios. Cambia "Por ahora nadie la registra ... T-F4/T-F5" por: la registra
   `JugadorAPie` (Personaje/, tarea T-051 de la fase T-F3); mientras no esté en main, usa tu cámara libre o un doble. Agrega
   que `Camara` y `Modo` cambian al subir o bajar de un `IMontable` y que el jugador a pie no es `IPeleador`.
3. `Core/Objetivos/IObjetivos.cs`: solo comentarios. Lo registra `ServicioObjetivos` (Progresion/, T-055); para dibujar el
   objetivo usa `UbicacionObjetivo.Resolver`.
4. `Core/Objetivos/UbicacionObjetivo.cs`: `public static class UbicacionObjetivo` con
   `static bool Resolver(Objetivo objetivo, IReadOnlyList<ILugar> lugares, out Vector3 posicion)`: si `TienePosicion`, esa;
   si no, la `PosicionEntrada` del primer lugar con `Id == objetivo.Lugar`; si no hay objetivo (`Lugar` Ninguno y sin posición),
   o no encuentra el lugar, devuelve false. Lógica pura, sin `Find` ni `Registro` dentro (quien llama pasa
   `Registro<ILugar>.Todos`).
5. `Core/Danio/TipoDanio.cs`: agrega `Disparo` después de `Golpe` (no cambies el valor de `Golpe`). Comentario: Golpe para
   golpes de mecha y a pie, Disparo para armas de fuego.
6. `Core/Danio/IRecibeDanio.cs` y `Core/Pelea/EventosPelea.cs`: solo comentarios. Quien recibe daño y no es `IPeleador`
   (personas a pie, muñecos de prueba) publica `ImpactoRecibido` con `objetivo` null; los suscriptores deben tolerar null.
7. Pruebas en `Tests/EditMode/Core/`: `UbicacionObjetivoPruebas` (con un `LugarFalso : ILugar` definido en la prueba) y
   `MontableContratoPruebas` (un `MontableFalso : IMontable` en la prueba que se registra en `Registro<IMontable>`: agregar y
   quitar; solo para comprobar que el contrato se puede implementar sin Unity vivo). `Registros.LimpiarTodos()` en SetUp y TearDown.

## Carpetas permitidas
Assets/_Proyecto/Core/Jugador/, Assets/_Proyecto/Core/Objetivos/, Assets/_Proyecto/Core/Danio/,
Assets/_Proyecto/Core/Pelea/EventosPelea.cs (solo comentarios) y Assets/_Proyecto/Tests/EditMode/Core/
No tocar: el resto de Core/, las áreas de Tony y de Alan, escenas, prefabs, ProjectSettings/, Packages/ ni herramientas/.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa: compilar OK, EditMode y PlayMode sin fallas, con las pruebas nuevas contadas
- [ ] UbicacionObjetivoPruebas pasa y cubre: objetivo con posición; objetivo de lugar registrado (da su `PosicionEntrada`);
      lugar que no está en la lista (false); `Objetivo.Ninguno` (false); lista vacía y lista null (false, sin excepción)
- [ ] MontableContratoPruebas pasa: el doble implementa `IMontable`, entra y sale de `Registro<IMontable>`, `IntentarSubir` con
      `PuedeSubir` false devuelve false sin avisar, `IntentarBajar` sin estar ocupado devuelve false
- [ ] `TipoDanio.Golpe` sigue valiendo 0 (prueba o lectura del enum) y existe `TipoDanio.Disparo`
- [ ] `git diff main --stat` solo muestra archivos de las carpetas permitidas; en IJugador, IObjetivos, IRecibeDanio y
      EventosPelea solo cambian líneas de comentario (`git diff -U0` sin líneas de código cambiadas)
- [ ] Ningún archivo de Core/ referencia un ensamblado de área (la prueba ReglasEnsamblados sigue pasando)

## Criterios de sensación (para un humano)
- Tony o Alan leen los `///` de `IMontable` y saben, sin preguntar, cómo hacer que el carro viejo funcione con el jugador:
  quién prende cada cámara, quién lee la E y dónde queda el jugador al bajar.

## Pruebas automáticas
- EditMode (Tests/EditMode/Core/): UbicacionObjetivoPruebas, MontableContratoPruebas.

## Fuera de alcance
- Implementar `IMontable` en algo real (el doble del carro es T-058; el carro de Alan es A-F2; el mecha es T-F4)
- Bajar a la fuerza (carro destruido, mecha en KO en la calle): se agregará con una tarea de Core en T-F4 si hace falta
- Cualquier cambio de forma en `IJugador`, `ModoJugador` o `IObjetivos`
