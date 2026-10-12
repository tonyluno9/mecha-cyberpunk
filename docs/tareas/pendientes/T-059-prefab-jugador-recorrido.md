# T-059 Prefab completo del jugador a pie y recorrido de prueba
Dueño: Tony | Rama: tony/T-059-prefab-jugador-recorrido | Modelo: Sonnet 5.5, medium
Depende de: T-051 a T-058

## Objetivo
`JugadorAPie.prefab` es el único prefab que Alan coloca en sus escenas para tener al jugador a pie completo: movimiento, vida,
golpes y pistola, puertas, carro, HUD y objetivos, y aparece donde diga un punto de aparición. Pruebas_APie.unity se vuelve un
barrio mínimo con una esquina, y una prueba de PlayMode lo recorre de punta a punta con entrada simulada siguiendo solo la
flecha del HUD. Es la prueba de cierre de T-F3 del lado de Tony mientras llega lo de Alan.

## Contexto
- Hoja de ruta, T-F3: "PlayMode con entrada simulada que recorre un tramo a pie, entra por una puerta y sigue un objetivo".
  Aquí se hace en la escena de Tony con los dobles; en el barrio de Alan es T-060 (espera A-F1).
- Decisiones: D-029 (Alan coloca el prefab de Tony y no lo edita). Propuestas: D-tony-23 (un solo prefab con HUD y servicios
  anidados), D-tony-25 (lo que espera a Alan) en docs/decisiones-pendientes-tony.md.
- Contrato: Core/Jugador/IPuntoAparicion.cs (`Id`, `Pose`, `Modo`). Alan pone `Casa_Puerta`, `Casa_Cochera` y
  `Clandestino_Puerta` en su barrio (T-040, parte B).
- Lo que ya hay: JugadorAPie.prefab (T-051 a T-058), HudAPie.prefab (T-056, T-057), ServiciosProgresion.prefab y
  FijadorObjetivoDePrueba (T-055), LocalDePrueba.prefab (T-054), CarroDePrueba.prefab (T-058), MunecoAPie.prefab (T-052).

## Qué hacer
1. `Personaje/Jugador/AparicionJugador.cs` en el jugador: si su campo `idAparicion` no está vacío, en `Start` (o con
   `Registro<IPuntoAparicion>.Agregado` si llega después) busca el punto con ese `Id` y llama `Colocar(Pose)`. Si el punto es de
   modo Carro o Mecha, coloca igual a pie y escribe un aviso una vez (aparecer dentro de un montable es de T-F4). Si no lo encuentra
   en 1 s, un aviso con el id y se queda donde está.
2. Doble `Personaje/Pruebas/PuntoAparicionDePrueba.cs : MonoBehaviour, IPuntoAparicion`.
3. Prefab: el creador de Personaje anida dentro de `JugadorAPie.prefab` una instancia de `HudAPie.prefab` y una de
   `ServiciosProgresion.prefab`, y agrega `AparicionJugador`. Así hay un solo HUD y un solo `IObjetivos` por jugador.
   Quita de la escena las instancias sueltas del HUD y de los servicios que pusieron T-056 y T-057.
4. Pruebas_APie.unity, regenerada como barrio mínimo: dos calles de 14 m en L con banquetas, la esquina tapada por un edificio
   (la línea recta del inicio al Bar choca con él), los tres locales de prueba en la segunda calle, el carro en la primera, dos
   muñecos en un callejón, un punto de aparición "Prueba_Inicio" y un `FijadorObjetivoDePrueba` con "avanzar al llegar" y la
   secuencia: "Ve a la esquina" (posición de la esquina), "Entra al bar" (Bar), "Sube al carro" (posición del carro).
5. `Tests/PlayMode/Personaje/RecorridoAPiePruebas.cs`: carga Pruebas_APie.unity y, con entrada simulada:
   a) el jugador aparece en "Prueba_Inicio"; b) camina siguiendo SOLO la flecha del HUD (gira con delta del ratón proporcional al
   ángulo de la flecha y avanza con W) hasta llegar a la esquina; c) el objetivo pasa solo al Bar y lo sigue hasta su entrada;
   d) E abre, W entra (`JugadorEntro(Bar)`), media vuelta y sale; e) sigue la flecha al carro, E sube, maneja 10 m con las flechas,
   se detiene, E baja; f) en otro caso, desde la esquina camina 40 m en dirección contraria al Bar y aparece "REGRESA".

## Carpetas permitidas
Assets/_Proyecto/Personaje/, Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity, Assets/_Proyecto/Tests/EditMode/Personaje/ y
Assets/_Proyecto/Tests/PlayMode/Personaje/
No tocar: Core/, UI/ y Progresion/ (solo anidar sus prefabs), las carpetas y escenas de Alan, Pruebas_Tony.unity.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] PlayMode: con solo `JugadorAPie.prefab` instanciado en una escena vacía hay exactamente un `IJugador`, un `IObjetivos` y un
      HUD; al destruirlo, `Registro<IJugador>` y `Registro<IObjetivos>` quedan vacíos
- [ ] PlayMode: con `idAparicion` = "Prueba_Inicio" el jugador aparece a menos de 0.1 m de la pose del punto y con su rotación
      (±1°), también si el punto se registra después que el jugador; con un id que no existe, un solo aviso y sin errores
- [ ] RecorridoAPiePruebas pasa completo: llega a la esquina y al Bar siguiendo solo la flecha en menos de 60 s de juego, entra y
      sale (entró y salió una vez cada uno), sube al carro, se mueve más de 10 m y baja en modo APie
- [ ] RecorridoAPiePruebas, caso f: "REGRESA" aparece en menos de 4 s tras alejarse 40 m
- [ ] En todo el recorrido: ningún frame dura más de 100 ms (`Time.unscaledDeltaTime` máximo medido) y la consola no tiene errores
- [ ] Capturas en Pruebas_APie: el inicio con el HUD y la flecha (docs/capturas/T-059-1.png), frente al Clandestino cerrado con su
      aviso (T-059-2.png) y dentro del carro (T-059-3.png)

Nota para el orquestador (no es trabajo del implementador): al integrar, anota en docs/revision-tony.md el pedido para Alan
(D-tony-25): colocar `Personaje/Prefabs/JugadorAPie.prefab` en Ciudad.unity y Pruebas_Alan.unity con `idAparicion`
"Casa_Puerta", y apagar ahí su cámara libre para que haya un solo `IJugador`.

## Criterios de sensación (para un humano)
- Recorrer el barrio mínimo a pie, entrar al bar y subir al carro se siente como un juego, no como una prueba: nada estorba,
  nada se atora y la flecha basta para saber a dónde ir.
- El HUD completo (vida, objetivo, canción, arma, avisos) se siente mínimo y legible.

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Personaje/): RecorridoAPiePruebas, AparicionJugadorPruebas y PrefabJugadorAPiePruebas.

## Fuera de alcance
- Colocar el jugador en Ciudad.unity o Pruebas_Alan.unity (son de Alan, D-029; la prueba en su barrio es T-060)
- Aparecer dentro del carro o del mecha (T-F4); guardar dónde quedó el jugador (T-F5)
