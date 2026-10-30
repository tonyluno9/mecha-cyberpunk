# T-407 Ronda 2: nombre CYBERFIX, GestorSonidos y los bugs del mercado negro, el Tianguis y la escopeta
Dueño: Tony | Rama: tony/T-407-cyberfix-sonido-bugs | Modelo: Sonnet 5.5, medium | MODO RÁPIDO (docs/ronda3.md)
Depende de: T-406 (la fase G ya está en tony/fase-g). Es la primera de la ronda 3: las demás dependen de ella.

## Objetivo
El juego se llama CYBERFIX en la ventana, en el ejecutable y en pantalla. Deja de estar en silencio: un GestorSonidos toca los sonidos por
su ID de la LISTA 1 y, mientras llegan los finales, tiene provisionales generados por código. El mercado negro por dentro, el Tianguis y la
escopeta funcionan y se reconocen.

## Contexto
- Lista de sonidos, convención de nombres y prioridades: docs/listas-equipo-cyberfix.md (LISTA 1). Guía de UI: docs/diseno/ui-cyberfix.md.
- Builds: `Progresion/Prologo/Editor/ConstruirDemo.cs` (`ConstruirJuego`, `ConstruirDemoPandillas`; hoy hacen `MechaCyberpunk.exe`).
  `ProjectSettings.asset` tiene `productName: Mecha Cyberpunk (demo)`.
- Escena: `Progresion/Prologo/Editor/CreadorCiudadPandillas.cs` (D-tony-205): un método por tarea que rehace solo su raíz `GTA_T40x`.
  Esta tarea agrega `T407()` y `GTA_T407`, y la suma a `Todo()`.
- Sonido que ya hay: `Mundo/Ambiente/AmbienteSonoro.cs` (Alan, en Barrio.prefab, 4 capas provisionales en Assets/Arte/Audio/) y el
  campo `sonidoAlarma` vacío de `Vehiculos/Carro/CarroEstacionado.cs`. Las armas, los golpes, los pasos y la UI no suenan.
- Mercado negro: el interior lo hizo Alan (`Mundo/Editor/CreadorMercadoNegroInterior.cs`, T-320) dentro de Barrio.prefab; T-403 lo abrió
  siempre. Escopeta: `Personaje/Combate/ArmasAPie.cs` y `PistolaAPie.cs` (perdigones), datos en `Personaje/Datos/Armas/`.
- Decisiones: D-tony-233 (nombre), D-tony-234 a D-tony-236 (sonido), D-tony-237 (arreglos en la escena).
- **Cambios de Core autorizados por Tony en esta tarea**: `Core/Sonido/` (nuevo) y `productName` en `ProjectSettings/ProjectSettings.asset`.

## Carpetas permitidas
Core/Sonido/ (nuevo), ProjectSettings/ProjectSettings.asset (solo `productName`), Progresion/Sonido/ (nuevo), Progresion/Editor/,
Progresion/Datos/, Progresion/Prologo/ (Editor/, Pandillas/, Clinica/), Progresion/Territorio/, Progresion/Calor/, Personaje/ (Combate/,
Movimiento/, Vida/), IA/ (APie/, Aliados/), Vehiculos/Carro/, UI/Tiendas/, UI/HudAPie/, UI/Historia/, Assets/Arte/Audio/Provisional/ (nueva),
Escenas/Ciudad_Prologo.unity (solo con código), Tests/EditMode/Progresion/, Tests/PlayMode/Progresion/, docs/sonidos-cyberfix.md (nuevo), docs de Tony.
No tocar: Radio/, Mundo/ (ni Barrio.prefab ni AmbienteSonoro), ProjectSettings fuera de `productName`.

## Qué hacer
1. **Recorrido de diagnóstico primero** (prueba 1, abajo, antes de arreglar nada): con entrada simulada, compra la escopeta en el mercado
   negro, entra al cuarto, ve al Tianguis y dispara la escopeta. Anota en la bitácora qué falla en cada parada (qué se ve vacío y por qué,
   qué no se reconoce, qué hace mal la escopeta, si hay un AudioListener prendido y si algo suena). Con eso se decide qué se arregla.
2. **Nombre**: `productName` = `CYBERFIX`. `ConstruirJuego` y `ConstruirDemoPandillas` hacen `Cyberfix.exe` (constante nueva; el viejo
   `Construir()` del mecha no cambia) y antes de construir borran de su carpeta `MechaCyberpunk.exe` y `MechaCyberpunk_Data/`.
   La pantalla de CONTINUARÁ dice "CYBERFIX" arriba. Busca "Mecha Cyberpunk" en los textos que se ven en el juego de pandillas.
3. **Contrato `Core/Sonido/`**: `SeccionSonido` (General, Efectos, Voces, Ambiente, Musica, Interfaz); `ISonidos` con
   `Reproducir(string id)` (2D), `ReproducirEn(string id, Vector3 posicion)` (3D), `int ReproducirBucle(string id, Transform sigue)`,
   `DetenerBucle(int)`, `FijarTono(int, float)`, `float Volumen(SeccionSonido)` y `FijarVolumen(SeccionSonido, float)`; y la fachada estática
   `Sonidos` (igual a esos métodos, por `Registro<ISonidos>`; si no hay gestor no hace nada y no avisa).
4. **`Progresion/Sonido/`**: `ReglaCatalogoSonidos` (pura: sección por prefijo, variante al azar sin repetir la última, el final gana sobre
   el provisional), `CatalogoSonidos` (ScriptableObject: ID → clips) y `GestorSonidos` (ISonidos; 16 fuentes reutilizables; 3D con
   minDistance 2 y maxDistance 40; volumen = general × sección; Interfaz con `ignoreListenerPause`). Un ID sin clips: `Debug.LogWarning`
   **una sola vez por ID** y nada más. Ambientes: AMB_CALLE_NOCHE en la calle, AMB_INTERIOR_CASA en la casa, AMB_MERCADO_NEGRO dentro del
   mercado (por `EventosLugares`) y AMB_TIANGUIS en la calle de la zona `tianguis` (por `ITerritorio`), con cruce de 1 s.
5. **Editor**: `CreadorCatalogoSonidos.Crear()` lee `Assets/Arte/Audio/<SEC>/<ID>.wav` y `<ID>_NN.wav` (SEC: JUG, PUN, ARM, VEH, PAN, CAL,
   ZON, ECO, UI, MUN, AMB, MUS) y, si un ID no tiene final, `Assets/Arte/Audio/Provisional/<SEC>/`. Los dos builds lo corren antes de
   construir: así los WAV que entregue el equipo entran solos. `GeneradorSonidosProvisionales.Crear()` escribe WAV de 16 bits, 44.1 kHz,
   generados por código (ruido con envolvente para disparos, golpes, impactos y pasos; tonos cortos para la UI; ruido filtrado en bucle
   para ambientes y motor; barrido para la sirena), solo de los IDs P1 que conectas aquí (paso 6).
6. **Conecta estos IDs** con `Sonidos.*`: ARM_PIS_DISPARO, ARM_ESC_DISPARO, ARM_SUB_DISPARO, ARM_PIS_RECARGA, ARM_ESC_RECARGA_CARTUCHO,
   ARM_SUB_RECARGA, ARM_VACIO, ARM_CAMBIAR, ARM_BALA_IMPACTO_CONCRETO, ARM_BALA_IMPACTO_CARNE, ARM_LASER_AVISO, PUN_GOLPE_AIRE,
   PUN_GOLPE_CARNE, JUG_PASO_ASFALTO, JUG_SALTO, JUG_CAER, JUG_DANIO, PAN_RIVAL_ALERTA, PAN_RIVAL_CAE, PAN_JEFE_ENTRADA, PAN_ALIADO_RECLUTA,
   VEH_ALARMA_ROBO, VEH_VIEJO_MOTOR_RALENTI (bucle con tono según la rapidez), CAL_SUBE_NIVEL, ZON_RESPETO_SUBE, ZON_CONQUISTADA,
   ECO_COMPRA, ECO_SIN_DINERO, ECO_ARMA_RECOGER, ECO_CLINICA_DESPERTAR, UI_MOVER, UI_ACEPTAR, UI_CANCELAR (en la tienda) y los 4 AMB del
   paso 4. Los demás P1 (UI_OBJETIVO_*, UI_TITULO_INICIO, UI_GUARDADO…) los conectan T-408 a T-413. Escribe `docs/sonidos-cyberfix.md`:
   una tabla de los P1 con "conectado en" y "provisional / final", para que el equipo sepa qué falta.
7. **Arreglos** (lo que salga del paso 1; lo más probable): el cuarto del mercado negro con mostrador visible, estantes con armas, cajas,
   reja, letrero y luz; el Tianguis con 8 a 12 puestos (lona de color, mesa y cajas), un arco "EL TIANGUIS" con neón y mantas magenta de
   los Calaveras en las manzanas 2 y 5; y lo que tenga mal la escopeta. Todo bajo `GTA_T407`, hecho con primitivas por código (son
   provisionales hasta el arte de la LISTA 2), sin tocar Barrio.prefab. `T407()` también pone el `GestorSonidos` con su catálogo.

## Criterios de aceptación
- [ ] La ventana del build dice CYBERFIX; existen `Builds/Juego/Cyberfix.exe` y `Builds/Demo/Cyberfix.exe` y ya no hay `MechaCyberpunk.exe` en esas carpetas.
- [ ] En Ciudad_Prologo hay exactamente un AudioListener prendido y al disparar cada arma el gestor toca su ID (contador de prueba de `GestorSonidos`).
- [ ] Pedir un ID que no existe da un solo warning aunque se pida 10 veces, y no lanza excepciones.
- [ ] Dentro del mercado negro hay mostrador, estantes y luz (la prueba cuenta renderers y luces dentro del cuarto); el Tianguis tiene 8 o más puestos y su arco.
- [ ] La escopeta comprada sale con 3, dispara 8 perdigones, baja el cargador, daña a un muñeco a 8 m y recarga con R.
- [ ] Capturas revisadas a ojo y corregidas si se ven mal: docs/capturas/T-407-1.png (dentro del mercado negro) y T-407-2.png (el Tianguis desde la calle).

## Pruebas (solo estas 2 nuevas)
- PlayMode `RecorridoRonda2Pruebas.MercadoTianguisEscopeta` (InputTestFixture, como `HistoriaPandillasPruebas`): Ciudad_Prologo con
  $400 en la cartera; el jugador aparece frente a la puerta del mercado negro, E abre la tienda, ↓ y Enter compran la ESCOPETA, Esc cierra,
  W entra al cuarto (captura 1); pasa a la calle del Tianguis (captura 2); 3 saca la escopeta, clic derecho apunta y clic izquierdo dispara
  a un muñeco a 8 m; R recarga. Comprueba los criterios de arriba. Es la prueba del paso 1: escríbela primero y déjala en verde al final.
- EditMode `CatalogoSonidosPruebas.VarianteFinalYFaltante`: un ID con 3 variantes las usa todas sin repetir dos veces seguidas; con
  final y provisional gana el final; un ID faltante devuelve null y avisa una vez (`LogAssert`).

## Al terminar
- `-executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.ConstruirJuego` → Builds/Juego/Cyberfix.exe y
  `...ConstruirDemo.ConstruirDemoPandillas` → Builds/Demo/Cyberfix.exe (Unity cerrado, uno tras otro).
- docs/guia-para-jugar.md: cambia `MechaCyberpunk.exe` por `Cyberfix.exe` en la sección de pandillas y agrega 3 líneas de qué probar.
- Bitácora de una línea en docs/bitacora/<fecha>-tony-T-407.md, con lo que encontró el diagnóstico. Fila de docs/estado-tony.md en "hecha".
- Merge a tony/fase-g y después a main, con push de las dos (autorizado por Tony en docs/ronda3.md). Nunca force push; `git restore` a lo
  que Unity cambia solo y no subas los `Assets/InitTestScene*.unity`.

## Sensación (Tony)
- ¿Ya no se siente vacío? ¿Los disparos pegan con el sonido provisional o estorban? ¿El Tianguis se reconoce como tianguis?
  ¿El mercado negro parece una tienda de armas? ¿La escopeta se siente fuerte de cerca?

## Fuera de alcance
- Los sonidos P2 y P3, la música, la radio (Radio/ es de Alan) y el arte final de los puestos y del mercado.
