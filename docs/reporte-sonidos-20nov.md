# Reporte 20-nov: todos los sonidos de "3d sonidos" integrados

Todo está en `main` (merge --no-ff de `integ/sonidos-20nov`). Tareas T-1530 a T-1536, una rama `tony/T-xxxx` cada una, hechas por subagentes
Sonnet e integradas, compiladas y probadas por clase en `.sesiones/s2`. Progreso en `docs/progreso-sonidos-20nov.md`; lo que falta en
`docs/sonidos-faltan.md`. Tablas completas:
- `herramientas/sonidos/ids-20nov.csv`: cada ID, su archivo, estado y uso.
- `comparacion-20nov.csv`: carpeta contra Assets.
- `niveles-20nov.csv`: nivel medido y volumen asignado.

**No lo escuché**: todo se midió, con duración, envolvente, RMS y LUFS, y se probó con pruebas y en Play. Lo que hay que oír está al final.

## Archivos (T-1530)
- **230 sonidos útiles** en la carpeta. Sin contar 98_Descartados ni 99_No_es_sonido, y sin subir el zip.
  - **122 WAV nuevos** en `Assets/Arte/Audio` (unos 126 MB por Git LFS).
  - **104 iguales** a los del 19-nov: se comprobó byte a byte y no se tocaron.
  - **5 renombrados** con su mismo GUID:
    - ARM_W07_DISPARO_01 → ARM_W07_DISPARO
    - ARM_W07_RAYO → ARM_W07_RAYO_LOOP
    - ARM_NOVA_DISPARO_01 → ARM_W07_GATILLO
    - ARM_NOVA_CARGA_DISPARO → ARM_W07_SIN_ENERGIA
    - POL_NOVA_AVISO → NOV_CAIDA_Y_ENCENDIDO
  - **0 reemplazos**: ningún ID repetido traía audio distinto.
  - **2 quitados** de la carpeta: POL_RADIO_DESPACHO_01/02. Quedan en Assets sin usarse; la policía usa POL_RADIO_AVISO_*, como dice el LEEME.
- **Carpetas nuevas**: 11_Nova, 12_NPCs, 13_Robots, 14_Ambiente, 15_Exo_Implantes, 16_Casino, 17_Misiones_HUD y 18_Radio_Musica.
- **Recortes**, cortados en cruce por cero y con crossfade de 120 ms en los loops:
  - EXO_RUEDA_CAMARA_LENTA en ENTRAR (primeros 2 s) y SALIR (la cola, 1.5 s).
  - ROB_MUERTE: el primer segundo.
  - FUE_LOOP_GRANDE: de 145 a 28 s.
  - NOV_JETPACK_LOOP: de 52 a 15 s.
  - ROB_VOZ_ALERTA partido en sus **5 frases** (_01.._05; el catálogo elige una al azar).
  - Revisé el empalme de todos los loops: sin clic medible.
- **Conversiones**:
  - MP3 a WAV PCM: NAV_REFLECTOR, IMP_INSTALAR_TALADRO, AMB_LOMAS, INT_TALLER_CANEPA y la tele.
  - WAV "extensible" a PCM 16.
  - 96 y 192 kHz bajados a 48 kHz.
  - Mono los del mundo; estéreo UI/HUD, radio y la rueda.
- **Importación**:
  - Cortos: Decompress On Load en ADPCM, con precarga.
  - Medianos: Compressed en Vorbis 0.7.
  - De más de 8 s: Streaming.
  - Mono/3D los del mundo; estéreo/2D UI/HUD.
  - **Encontrado y arreglado**: el importador viejo (`ImportadorAudio`) corría al final y dejaba *todas* las carpetas numeradas en Compressed
    y mono, también las del 19-nov. Por eso las reglas del 19-nov nunca se aplicaron. Ahora sí: 30 clips largos en streaming, cortos en ADPCM
    y UI en estéreo.

## Las carpetas 00_*
- **00_Plasma_dime_para_que_es** ya venía vacía: los del plasma estaban en 02_Armas con el nombre que dice el LEEME. Medidos, encajan con el flujo:
  - GATILLO al apretar
  - CARGA mientras carga (sube hasta el final)
  - DISPARO al terminar la carga
  - RAYO_LOOP + RAYO_CAPA_EXTRA en bucle mientras dura el rayo
  - ENFRIAR al soltar (siseo que decae)
  - SIN_ENERGIA sin celda

  Dos dudas: GATILLO dura 1.2 s y SIN_ENERGIA suena más a "boom" que a clic.
- **00_Sin_nombre_ponselo_tu**:
  - `percusion_cumbia.wav` → **RAD_EST1_CUMBIA_PERCUSION** (18_Radio_Musica). Es la estación 1 de la radio y suena en 3D en una bocina de
    fiesta (`Bocina_Fiesta*`). El juego todavía no tiene radio de carro ni estaciones, así que hoy solo suena en la bocina, y solo si
    algún objeto se llama `Bocina_Fiesta`.
  - `zumbido_refrigerador.wav` → **OBJ_REFRIGERADOR**: refrigerador de la tiendita ED02 y de la clínica del Doc.
  - `tele_reiniciando.mp3` → **OBJ_TELE_REINICIO**: la tele del hacker (`Set_Hacker`) y teles de casa (`H06_TeleVieja`), cada 45 a 120 s.

## Qué sonido quedó en qué parte

### Armas, impactos y explosiones (T-1532)
| Evento | Sonido |
| --- | --- |
| Plasma | GATILLO → CARGA (bucle) → DISPARO → RAYO_LOOP + RAYO_CAPA_EXTRA (bucles) → ENFRIAR; SIN_ENERGIA sin celda |
| Lanzagranadas | ARM_W08_DISPARO |
| Revólver | ARM_W06_MECANICA (recarga y martillo 0.3 s tras cada tiro), ARM_W06_SACAR |
| Sacar o guardar las demás armas | la recarga de cada arma (LEEME) |
| Clic sin balas | ARM_SIN_BALAS |
| Casquillos | CAS_GRANDE (escopeta, francotirador), CAS_CHICO (pistola, subfusil, fusil) |
| Misiles de la prótesis | IMP_MISILES_LANZAR |
| Explosión de carro | EXP_CARRO, a los 0.3 s EXP_ESCOMBROS + EXP_CRISTALES; luego FUE_ENCENDER + FUE_LOOP_GRANDE |
| Explosión de barril | EXP_BARRIL + EXP_ESCOMBROS; FUE_ENCENDER + FUE_LOOP_CHICO 8 s |
| Explosión de dron | EXP_DRON_ROBOT |
| Golpe | MEL_GOLPE_METAL a robots, drones y armaduras; MEL_GOLPE_CARNE al resto. Bloqueo del mecha: MEL_BLOQUEO |
| **Rueda de armas** | **EXO_RUEDA_CAMARA_LENTA_ENTRAR al abrir y _SALIR al cerrar** (el sonido real). El sintetizado UI_CAMARA_LENTA se borró |

### Nova, robots, drones, NPCs y policía (T-1533)
| Evento | Sonido |
| --- | --- |
| Guardia Nova y élite caminan / corren | NOV_PASOS_CAMINAR / NOV_PASOS_CORRER (por distancia), NOV_ARMADURA_MOVER cada 2 pasos |
| Pesados (E12, E16) y jefe E14 | ROB_PES_PASO = NOV_PASOS_CAMINAR con **tono 0.7** |
| Centinela E15 / perro E17 | PAS_METAL / ROB_PERRO_MOVER |
| Robot en movimiento, alerta, daño, muerte | ROB_SERVOS (o ROB_PES_HIDRAULICA si es gigante); ROB_VOZ_ALERTA (una de 5 frases); ROB_CHISPAS_DANIO; ROB_MUERTE |
| Apagado tras morir | ROB_PES_PLACA_CAE (pesado, jefe); ROB_APAGADO = ROB_SERVOS con **tono 0.6** (centinela, perro) |
| Llegadas | gigantes ROB_PES_LLEGADA; los que caen del cielo, ROB_ENCENDER |
| Mordida del perro | ROB_PERRO_GRUNIDO avisa antes |
| Se cubren tras escudo / bala contra escudo | NOV_ESCUDO_ACTIVAR / NOV_ESCUDO_IMPACTO |
| Caída del cielo | NOV_JETPACK_LOOP con fade (por tono; ver abajo) |
| Dron | DRN_PES_AMETRALLADORA en ráfaga de 3; derribado NAV_DANIADA |
| Nave de Nova | NAV_ROTORES_LOOP; NAV_REFLECTOR al llegar a flotar |
| NPC herido o atropellado | NPC_H_DOLOR / NPC_M_DOLOR según el visual (hombre o mujer); robots, chispas |
| Policía | radio: POL_RADIO_ESTATICA y luego POL_RADIO_AVISO; sube estrella: POL_ESTRELLA_SUBE; 5 estrellas: POL_SIRENA_MURO desde la torre del muro más cercana |

### Vehículos y pasos (T-1534)
- **Pasos: uno por pisada, en el cuadro de la bitácora de Blender.**
  - Caminar, trote, sprint y trote de lado y atrás ya traían eventos (T-1505).
  - Caminar apuntando (4 direcciones) y escaleras (subir y bajar) se agregaron **por código**, leyendo el tiempo del Animator. **No se tocó
    ningún clip ni .meta de PJ01.**
  - El tipo de piso decide el sonido: concreto, metal, madera, tierra, grava o mojado. Arena y pasto usan tierra y loseta usa concreto: no hay
    archivo propio de esos tres.
  - Comprobado: con W (trote) suena una pisada cada 1.575 m, la cadencia del clip.
- **Carro eléctrico**:
  - VEH_ELECTRICO_ARRANQUE al encender; parado, silencio.
  - ACELERAR al pisar a fondo.
  - MOVER en bucle, con el tono subiendo con la velocidad.
  - PASAR al llegar al 60 % de la velocidad.
- **Otros sonidos del carro**:
  - VEH_LLANTAS_GRAVA en grava o tierra.
  - VEH_PUENTE en juntas de puente.
  - Raspón de costado con VEH_CHOQUE.
  - CLI_LLUVIA_DENTRO_CARRO si llueve y vas dentro.
- **Sin cambio**: al subirse, el portazo y luego el arranque (T-1526). Motos y deportivos con el motor normal, como dice el LEEME.

### Ambiente, clima, interiores, casino, HUD, exo (T-1535)
- **Ambiente por barrio**, con **cruce de 3 s** al cambiar de barrio (entrar o salir de un local sigue en 1 s):
  - Las Lomas: AMB_LOMAS.
  - Calles de Nova: AMB_NOVA.
  - Suburbios de noche: AMB_SUBURBIOS_NOCHE.
  - Azoteas (a más de 20 m): AMB_AZOTEA_CIUDAD.
  - Tianguis, Neón y el resto: siguen con los que ya tenían.
  - Comprobado en Play: en la calle de Nova suena AMB_NOVA y en el tianguis se pide AMB_TIANGUIS. **AMB_CALLE_DIA y AMB_TIANGUIS están apagados
    en el catálogo desde antes** (ya estaban así en main): no los encendí.
- **Interiores y objetos** (3D; suenan solo las fuentes más cercanas, con tope):
  - En Play encontró 241 fuentes: taller de Cánepa, tienda de Tino (radio + ventilador), armería, 33 transformadores, 160 aires, 19 cámaras,
    la tiendita y la clínica (refrigerador), expendedoras y la tele del hacker.
  - La bodega con goteras no suena: no hay ningún objeto llamado "Bodega".
- **Casino**: tragamonedas, premio chico o grande y fichas con la cajera. La ruleta y las cartas no tienen lógica de juego en el casino, así que
  no suenan.
- **Misiones y HUD**:
  - Temporizador en los últimos 10 s.
  - Mensaje del celular: HUD_MENSAJE_CELULAR.
  - Misión fallida: UI_ERROR.
  - Marcador del mapa: UI_SELECCIONAR.
  - Indicador de daño: IMP_BALA_ZUMBIDO.
  - Arrestado: golpe, sirena que se aleja y esposas.
  - HAS MUERTO: últimos latidos.
- **Exo e implantes**:
  - Súper salto y su aterrizaje.
  - Escudo, visor, servos de brazos y piernas.
  - Prótesis puesta: golpe suave cada 2.5 s.
  - Instalar con el Doc: taladro, clics y sinapsis.
  - Rueda de habilidades: mover y elegir.
  - Latido con vida baja y pitido de oído tras una explosión.
  - Dinero y papitas: INT_RECOGER_DINERO y CON_PAPITAS_ABRIR→COMER.

### T-1536 (contrato de Core, hecho por mí)
- **ClimaMundo**: Mundo escribe la lluvia, y la oyen el sonido (lluvia, tormenta, viento) y el carro (lluvia dentro).
- **Hoy la lluvia está apagada en el juego** (desde el 5-nov), así que el clima no suena hasta que la prendas.

## Mezcla (T-1531)
- Cada ID nuevo tiene su política: grupo, voces, volumen, distancia 3D o 2D, tiempo mínimo y prioridad.
  - Para empatar volúmenes se midió el nivel de cada WAV después de la normalización de Unity.
  - **Jerarquía**: disparos, voces y avisos van por encima; ambiente, clima, interiores y objetos van al menos 10 dB abajo y en su propia
    sección (el ducking los baja con cada disparo o explosión).
  - **Variación** de tono y volumen en todo. Los bucles de ambiente casi no varían, para que no bombeen.
- **Alias**: los IDs viejos siguen sonando con los nuevos (VEH_EXPLOSION→EXP_CARRO, UI_TELEFONO→HUD_MENSAJE_CELULAR, …; la lista completa está en
  `MapaSonidos20nov`).
  - IDs que el código pide sin archivo: **0**.
- Para cambiar un volumen: edita su regla en `herramientas/sonidos/generar_mezcla_20nov.py` y vuélvelo a correr.

## Pruebas
- **EditMode de sonido, armas, vehículos, pasos, enemigos, ambiente y rueda: 837/839** (839 pruebas; el latido quedó en verde aparte, 3/3).
  Las 2 que fallan ya fallaban en main: costales CB02 en la calle de Nova y rueda de la patrulla PA01.
  - El EditMode completo lo cortó Claude Code por falta de memoria y no lo relancé; se corrió por clases.
- **PlayMode por clase**:
  - Sonido: 19/19, después de 2 ajustes.
  - Pasos, disparo, explosión de carro, plasma, locomoción, armas especiales, drones e IA de Nova: 33/33.
- **Arreglos que salieron al probar**:
  - El importador viejo que pisaba todo.
  - El latido contaba 40 de 100 como vida baja (precisión de Mono).
  - Pruebas viejas al día con los sonidos nuevos: ambiente de suburbios, alias de robots, mensaje del celular, dinero, pasos del centinela y
    cadencia de pasos.

## Qué falta (detalle en docs/sonidos-faltan.md)
- **Pasos en loseta**: no hay archivo.
- **Voces**: las del jugador, pandilleros, policías a pie y Nova las grabas tú.
- **Música y estaciones de radio**: solo existe la percusión de cumbia, y el juego no tiene radio.
- **Sin sonido propio**: Deshuesadero, Centro y muro.
- **Por decidir**: el misil en vuelo y el arma propia de Nova.
- **Sin evento en el juego** (hay archivo pero no hay mecánica):
  - plasma de Nova
  - explosión de nave
  - ambulancia
  - ruleta y cartas
  - checkpoint
  - bodega
- **Para el jetpack con fade de volumen de verdad** (hoy el fade es de tono) y para la bocina o EXP_ONDA "bajito" hace falta volumen por llamada
  en `ISonidos`: es una tarea de Core.

## Qué probar (a oído)
1. **Plasma**: apretar, cargar, rayo y soltar. Que GATILLO (1.2 s) y SIN_ENERGIA (el "boom") te sirvan.
2. **Rueda de armas**: abrir y cerrar, con la cámara lenta real.
3. **Carro**: explota (escombros, cristales, fuego grande); barril; dron derribado.
4. **Nova y robots**:
   - pasos con armadura;
   - el pesado más grave;
   - voz de alerta (5 frases);
   - escudo;
   - jetpack al caer;
   - sirena del muro a 5 estrellas.
5. **Pasos**: en cada piso, apuntando y en escaleras (que no se desfasen).
6. **Ambiente**:
   - cambia de Lomas a Nova y a suburbios de noche, con cruce de 3 s;
   - en las azoteas;
   - que el ambiente no tape disparos ni voces.
7. **Interiores**: taller de Cánepa, tienda de Tino, armería y el refrigerador de la tiendita y la clínica. Que no suenen fuertes los transformadores
   ni los aires.
8. **Casino**, celular, arresto, muerte, instalar implante, latido con vida baja.
9. **Eléctrico**: el bucle MOVER (3.2 s) puede notarse al repetirse.
10. **Volúmenes viejos contra nuevos**: los ambientes viejos (carpeta AMB/) no se normalizan y los nuevos sí; quedaron empatados a ojo.

## Builds
Se agrega al final en `docs/progreso-sonidos-20nov.md`.
