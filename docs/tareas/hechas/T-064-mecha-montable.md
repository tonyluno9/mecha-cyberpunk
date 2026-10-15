# T-064 El mecha como montable: subir y bajar a pie
Dueño: Tony | Rama: tony/T-064-mecha-montable | Modelo: Sonnet 5.5, medium
Depende de: nada nuevo (T-050, T-058 y T-059 ya están en tony/fase-3)

## Objetivo
`MechaJugador` implementa `IMontable` con modo Mecha: el jugador a pie camina hasta el mecha estacionado, pulsa E, pasa a la
cabina y lo maneja; con E (detenido) se baja frente al mecha. Sigue funcionando como hasta ahora cuando no hay piloto (arena,
Pruebas_Tony y todas las pruebas de pelea). Nace la escena de pruebas de la calle, donde se armará la emboscada de T-F4.

## Contexto
- Hoja de ruta, T-F4. Contrato: Core/Jugador/IMontable.cs (lee sus `///` completos: quién prende cámara y oído, quién lee la E).
- El lado del jugador ya existe y no cambia: `JugadorAPie.Subir/Bajar`, `InteraccionAPie` (elige montables por `PuntoSubida` a
  2.5 m y 60°), `ReglaMontar`, y el doble `Personaje/Pruebas/CarroDePrueba.cs` como ejemplo de montable.
- Propuestas: D-tony-28 (dos arranques y permiso de piloto) y D-tony-36 (Pruebas_Calle) en docs/decisiones-pendientes-tony.md.
- Lo que ya hay en el mecha: `ControladorMecha` (Mecha/), `PeleadorMecha` con su lista `controlados` y `VidaMecha` (Combate/),
  la cámara bajo "Sacudida" con su `AudioListener` y tag MainCamera. El prefab lo arman varios creadores (CreadorEscenaMecha,
  CreadorCabina, CreadorVidaMecha, CreadorGolpeBloqueo): **no vuelvas a correr CreadorEscenaMecha.Crear**, que rehace el prefab y
  Pruebas_Tony desde cero. Agrega lo tuyo con un creador nuevo e idempotente que edite el prefab (`LoadPrefabContents`).
- Permiso de control: hoy `DirectorPelea` usa `IPeleador.ActivarControl`. El montaje necesita apagar el control sin pelear con
  el director, así que `PeleadorMecha` gana un segundo permiso: control efectivo = permiso de pelea Y permiso de piloto Y no KO.
- `MechaCyberpunk.Mecha` pasa a referenciar `MechaCyberpunk.Combate` (mismo dueño, D-tony-28) y `MechaCyberpunk.Mecha.Editor`
  a `MechaCyberpunk.Personaje` (solo editor, para armar la escena).

## Qué hacer
1. `Combate/Peleador/PeleadorMecha.cs`: `FijarPermisoPiloto(bool)` y `PermisoPiloto` (por defecto true). `ControlActivo` y los
   `controlados` siguen la regla de arriba; `ActivarControl` y `PrepararPelea` no cambian de comportamiento con el permiso en true.
2. `Mecha/Montaje/MontajeMecha.cs : MonoBehaviour, IMontable` en la raíz del mecha, con `ConfigMontajeMecha` (Mecha/Datos/):
   - Campo `arranque`: `Directo` (como hasta T-F3: sin piloto, cámara, oído y control propios desde el inicio; NO se registra en
     `Registro<IMontable>`) o `Estacionado` (vacío: cámara y oído apagados, permiso de piloto false, registrado como montable).
     El prefab `MechaJugador.prefab` queda en `Directo`; crea la variante `Mecha/Prefabs/MechaEstacionado.prefab` en `Estacionado`.
   - `Modo` Mecha, `Nombre` "Mecha", `Asiento` (hijo dentro del cuerpo, a la altura de la cabina), `Camara` (la del mecha),
     `PuntoSubida` (hijo en el piso, 1.8 m al frente).
   - `PuedeSubir`: estacionado, no ocupado, habilitado y no en KO. `PuedeBajar`: ocupado y velocidad horizontal menor que
     `velocidadMaximaParaBajar` (0.5 m/s). T-065 agregará "y no encendiendo".
   - `IntentarSubir`: prende cámara y oído, permiso de piloto true, avisa. `IntentarBajar`: busca una pose libre para una persona
     (cápsula de 1.8 × 0.35 m con `Physics.CheckCapsule`, ignorando el propio mecha) probando el frente, la izquierda, la derecha y
     atrás a `distanciaSalida`; si ninguna está libre, devuelve false. Al bajar: permiso de piloto false, cámara y oído apagados.
3. Creador `Mecha/Editor/CreadorMontajeMecha.cs` (idempotente): agrega MontajeMecha, Asiento y PuntoSubida al prefab, crea el
   config y la variante. Comprueba después que las pruebas de Pruebas_Tony siguen pasando.
4. Escena `Escenas/Tony/Pruebas_Calle.unity`, generada solo por `Mecha/Editor/CreadorEscenaCalle.cs` (cada tarea de T-F4 agrega
   su parte a este creador y la regenera; nunca se edita a mano): piso de 120 × 60 m, una calle de 14 m a lo largo de +Z con muros
   de edificios greybox a los lados y un callejón al fondo (lo usará la emboscada), luz direccional, `JugadorAPie.prefab` con
   `idAparicion` "Calle_Inicio" y un `PuntoAparicionDePrueba` con ese id en (0, 0, 0) mirando +Z, y `MechaEstacionado.prefab` a
   unos 8 m del inicio.
5. Pruebas PlayMode en `Tests/PlayMode/Mecha/MontajeMechaPruebas.cs` (cargan Pruebas_Calle, entrada simulada):
   a) a pie, W hasta quedar a menos de 2 m del `PuntoSubida`, E: `Modo` Mecha, `Camara` del jugador = la del mecha, un solo
   `AudioListener` prendido; b) W mueve al mecha y el jugador va en el asiento (distancia raíz-asiento < 0.05 m cada frame);
   c) moviéndose, E no baja (`BajarNegado`); detenido, E baja: `Modo` APie, el jugador queda fuera del collider del mecha y a menos
   de 3 m del `PuntoSubida`; d) con muros alrededor del frente, baja por otro lado; con todo bloqueado, no baja;
   e) estacionado y vacío, WASD y clic no mueven ni golpean al mecha; f) en Pruebas_Tony (arranque Directo) no hay ningún
   `IMontable` registrado y el mecha se mueve con WASD como antes.
6. EditMode en `Tests/EditMode/Combate/`: la regla de control efectivo de PeleadorMecha (si la sacas a una función pura) o, si no,
   cúbrela en PlayMode.

## Carpetas permitidas
Assets/_Proyecto/Mecha/, Assets/_Proyecto/Combate/Peleador/PeleadorMecha.cs, Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity,
Assets/_Proyecto/Tests/EditMode/Mecha/, Assets/_Proyecto/Tests/EditMode/Combate/, Assets/_Proyecto/Tests/PlayMode/Mecha/
No tocar: Core/, Personaje/ (solo se usa su prefab y su doble de aparición), Pruebas_Tony.unity, las carpetas y escenas de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa, incluidas todas las pruebas que ya usaban Pruebas_Tony y MechaJugador
- [ ] MontajeMechaPruebas pasa los casos a) a f)
- [ ] En Pruebas_Calle, recién cargada: exactamente un `IJugador`, un `IMontable` de modo Mecha, una cámara y un `AudioListener`
      prendidos
- [ ] Medido en Unity vivo en Pruebas_Calle: subir, manejar 10 m y bajar 5 veces seguidas sin errores en consola
- [ ] Captura desde la cabina recién subido, con el barrio greybox al frente: docs/capturas/T-064-1.png
- [ ] `git diff` de MechaJugador.prefab no quita ningún componente existente (lo comprueba la prueba de que Pruebas_Tony sigue igual)

## Criterios de sensación (para un humano)
- Subir al mecha con E y bajar frente a él se siente inmediato y claro; el cambio de la vista a pie (1.65 m) a la cabina (3.5 m)
  no marea.
- Al bajar quedas en un lugar que tiene sentido (frente a la escotilla), no dentro de un muro ni a espaldas del mecha sin razón.

## Pruebas automáticas
- PlayMode (Tests/PlayMode/Mecha/): MontajeMechaPruebas. EditMode si la regla de control queda como función pura.

## Fuera de alcance
- Encendido de 3 pasos (T-065), llamado y regreso al taller (T-066), calor (T-067)
- Aparecer ya dentro del mecha desde un punto de aparición (lo pide el director del prólogo en T-F6)
- Animación de subir por la escotilla (es un corte directo en greybox)
