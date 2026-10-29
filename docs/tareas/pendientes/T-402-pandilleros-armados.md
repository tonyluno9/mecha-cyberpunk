# T-402 Pandilleros armados en su territorio: dinero, calor, drones y patrullas
Dueño: Tony (Trafico/ es de Alan; Tony lo autorizó) | Rama: tony/T-402-pandilleros-armados | Modelo: Sonnet 5.5, medium | MODO RÁPIDO
Depende de: T-400 (muñeco, cámara y pistola). Puede ir antes o después de T-401, pero no a la vez (las dos tocan Ciudad_Prologo).

## Objetivo
En EL TIANGUIS y en NEÓN ALTO hay grupos de pandilleros con el muñeco humanoide. Si entras a su territorio te disparan. Los vences,
sueltan dinero, te sube el calor y llegan drones y patrullas; desde el nivel 2 los drones también disparan. Si caes, despiertas en la clínica.

## Contexto
- Ya existen `IA/APie/PandilleroAPie.cs` (cápsula a puños), `PandillaAPie.cs` (grupo, turnos, solo de noche, paga al caer),
  `IA/Datos/DatosPandilleroAPie.cs` y su creador `IA/Editor/CreadorPandillaAPie.cs`.
- Persecución: `Trafico/Persecucion/GestorPersecucion.cs` (ya está en Barrio.prefab y escucha `ICalor.NivelCambio`), `DronMundo.cs`.
- Caer en la calle: `Progresion/Prologo/Clinica/DoctorClinica.cs` y su prefab; la clínica está en el mercado negro (`Clinica_Mostrador`, camilla).
- Diseño: docs/diseno/pandillas.md. Decisiones: D-tony-210 (números), D-tony-211 (territorio e IdPandilla), D-tony-212 (vida y doctor),
  D-tony-213 (drones y calor).
- **Contrato nuevo de Core (Tony lo autorizó en esta tarea)**: `Core/Territorio/IdPandilla.cs`: `Ninguna, Jugador, Calaveras, Viboras`
  ("solo se agregan valores al final"). Nada más en Core.

## Carpetas permitidas
Core/Territorio/IdPandilla.cs (nuevo), IA/, Trafico/Persecucion/ y Trafico/Datos/, Personaje/Vida/ y Personaje/Combate/,
Progresion/Prologo/ (Clinica, Editor/CreadorCiudadPandillas.cs, datos del doctor), Escenas/Ciudad_Prologo.unity (solo con código),
Tests/PlayMode/IA/, docs de Tony. No tocar: Radio/, Mundo/Prefabs/Barrio.prefab.

## Qué hacer
1. **Pandillero con arma**: el pandillero usa `Assets/Arte/Personajes/MunecoHumano/ModeloHumano.prefab` (T-400) con el color de su
   pandilla (Calaveras magenta, Víboras verde ácido; MaterialPropertyBlock) y mueve su Animator. Arma nueva en IA: `PistolaPandillero`:
   aviso con un láser rojo de 0.35 s, luego un rayo al pecho del jugador con fallo por distancia (70 % de acierto a 10 m, 30 % a 30 m),
   8 de daño por `IRecibeDanio`, un tiro cada 1.2 s, sin disparar a través de muros (Raycast). Se queda a 6 a 12 m y se cubre de lado.
   Vida 60. Datos en `DatosPandilleroAPie_Calaveras` y `_Viboras` (+ campos nuevos con `[Tooltip]` y unidades).
2. **Territorio**: `PandillaAPie` gana `IdPandilla`, centro y radio (35 m) y la opción "siempre" (el juego nuevo no espera a la noche).
   Atacan si el jugador entra al radio y lo persiguen hasta radio + 10 m; luego vuelven. Al quedar vacío, el grupo reaparece a los 40 s
   si el jugador está a más de 30 m. Máximo 2 disparando a la vez (los turnos que ya existen).
3. **Premio y calor**: cada vencido paga $40 (como hoy) y suma +8 de calor (`MotivoCalor.Pelea`). Cada disparo del jugador en la calle
   (fuera de un interior) suma +2, como máximo una vez cada 0.5 s.
4. **Drones que disparan**: con `ICalor.Nivel` ≥ 2, cada `DronMundo` dispara al jugador a pie cada 1.5 s (4 de daño, 70 % de acierto,
   aviso de 0.3 s con su luz en rojo). Número en `ConfigPersecucion`. A los que van en carro no les disparan.
5. **Vida y caída**: `VidaAPie` se regenera 5/s tras 6 s sin recibir daño. Si caes en la calle, el `DoctorClinica` te despierta en la
   camilla del mercado negro y cobra $100 (asset de datos nuevo `DatosDoctor_Pandillas`); el calor se borra.
6. **Escena**: `CreadorCiudadPandillas.T402()` crea `GTA_T402` con 4 grupos: 2 de Calaveras (3 y 2) en el centro (manzanas 2 y 5,
   lejos de las puertas del clandestino, del bar y del mercado negro) y 2 de Víboras (3 y 3) en el este (manzanas 3 y 6), en las
   banquetas; y el `DoctorClinica` en la clínica. Los pandilleros no entran a interiores. Apaga los `MunecoAPie` de prueba de `GTA_T400`.

## Pruebas (solo estas 2 nuevas, PlayMode con InputTestFixture)
- `PandillaArmadaPruebas.TeDisparanEnSuTerritorio`: escena mínima con un grupo de 2; jugador a 50 m → nadie dispara 3 s; jugador a
  15 m → en menos de 4 s la vida del jugador baja.
- `PandillaArmadaPruebas.VencerlosPagaYTraeDrones`: con la pistola (entrada simulada: 2, clic derecho, clics) vence a 2 pandilleros →
  el dinero sube $80, el calor llega al nivel 1 y `GestorPersecucion.Drones` ≥ 1 en menos de 5 s (puedes subir el calor con
  `CalorDePrueba` para no depender de los tiros).

## Al terminar
- Build: `-executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirJuego` (ver T-400) → Builds/Juego/MechaCyberpunk.exe.
- docs/guia-para-jugar.md, sección "Juego de pandillas": 3 líneas de qué probar.
- Bitácora de una línea: docs/bitacora/<fecha>-tony-T-402.md. Fila de docs/estado-tony.md en "hecha". Merge a tony/fase-g.

## Sensación (Tony)
- ¿Los tiroteos se sienten justos (ves venir el tiro) o te matan sin avisar? ¿El calor da miedo o estorba? ¿$40 por pandillero se siente bien?

## Fuera de alcance
- Zonas, respeto y jefes (T-404). Escopeta y subfusil (T-403). Patrullas que disparan o arrestan.
