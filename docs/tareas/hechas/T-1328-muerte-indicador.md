# T-1328: despertar con el Doc limpia todo e indicador de dirección de daño

Rama: tony/T-1328-muerte-indicador. Del playtest de Tony (17-nov B).

## Qué se hizo
- Core/Jugador/EventosJugador.cs (contrato nuevo, pequeño): evento `Desperto`. Lo publica `DoctorClinica` justo después de borrar el calor (estrellas de policía y Nova a 0).
- IA/APie/PandilleroAPie: al `Desperto`, la policía a pie se destruye; los demás pandilleros sueltan la persecución (regla, táctica, turno, cubierta, objetivo, alerta y provocación a cero) sin curarse.
- IA/APie/RespuestaNova: al `Desperto` llama `RetirarTodos()` (guardias, pesados y jefe).
- Patrullas y drones de Trafico (Alan) ya se retiran solos cuando el calor llega a nivel 0 (`GestorPersecucion.Reconciliar`); no se tocó.
- UI/HudAPie/IndicadorDanio.cs + ReglaIndicadorDanio.cs: anillo de 150 px con barras rojas que apuntan hacia la fuente del daño al jugador a pie (usa `InfoDanio.Direccion` y la cámara del jugador), se desvanecen en 1 s. Lo agrega `HudAPie.Awake` (como el Hitmarker), sin escena ni prefab que cambiar: no hay método `Aplicar()`.

## Pruebas (sin correr: la noche no tenía Unity)
- EditMode UI: ReglaIndicadorDanioT1328Pruebas.
- PlayMode UI: IndicadorDanioT1328Pruebas (izquierda, enfrente, lejos).
- PlayMode IA: DespertarLimpiaT1328Pruebas (pandillero en pelea se calma; policía se va).
- PlayMode Progresion/Implantes: MuerteLimpiaT1328Pruebas (ciudad real: 5 estrellas, morir, despertar: 0 estrellas, sin policía ni persecución).

## Notas
- docs/playtest-17nov-b.md no existe en esta rama; se siguió el encargo.
- La pantalla HAS MUERTO (T-1316) no se tocó.
