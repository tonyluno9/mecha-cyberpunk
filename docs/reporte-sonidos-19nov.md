# Reporte 19-nov: sonidos nuevos de "3d sonidos" integrados al juego

Rama `integ/sonidos-19nov` (worktree `.sesiones/s2`), tareas T-1470..T-1476 hechas por subagentes Sonnet y revisadas e integradas por Opus.
Detalle por tarea en docs/tareas/hechas/T-1470..T-1476-*.md. Lo que falta: docs/sonidos-faltan.md.

## Archivos (T-1470)
- 138 WAV en `Assets/Arte/Audio/01_UI` .. `10_Policia` (unos 116 MB, por Git LFS; el mayor ~6 MB). La carpeta "3d sonidos" está en .gitignore.
  `99_No_es_sonido` (redes.3mf) se ignoró.
- Script de copia y recorte: `herramientas/sonidos/importar_sonidos_19nov.py` (log en `herramientas/sonidos/importar-19nov.log`).
  Las grabaciones largas originales no se copiaron, solo sus recortes.
- Recortes:
  - `VEH_NORMAL_ARRANQUE/RALENTI/APAGAR_01..03` desde las tres grabaciones de carro.
  - `MOTO_ARRANQUE/RALENTI/APAGAR_01..03`.
  - `VEH_PESADO_RALENTI_01` y `VEH_PESADO_APAGAR_01`.
  - `PAS_TIERRA_01..08` y `PAS_GRAVA_01..08` (pisadas sueltas).
  - `VEH_ALARMA` en un loop de 8.75 s y `VEH_LLANTAS_GRAVA` en uno de 9 s.

  Los loops tienen crossfade de 120 ms y cortes en cruce por cero.
- Convertidos a PCM 16: los 3 "extensible" (TIE_EQUIPAR, ARM_GRANADA_REBOTE, ARM_W07_CARGA) y 8 con la cabecera de datos truncada
  (TIE_COMPRAR, ARM_W01_DISPARO_01, IMP_BALA_CARNE_01, PAS_MADERA_02, PAS_MOJADO_01, MEL_GOLPE_AIRE, CON_VENDA, VEH_ELECTRICO_ARRANQUE).
- Importación (`Assets/Arte/Editor/ImportarSonidos19nov.cs`, AssetPostprocessor solo en las 10 carpetas):
  - Cortos: Decompress On Load en ADPCM, con precarga.
  - Medianos y ralentís: Compressed In Memory en Vorbis 0.7.
  - De más de 8 s: Streaming.
  - Mono en los del mundo (armas, impactos, explosiones, pasos, vehículos, policía); estéreo en UI.

## Catálogo y mezcla (T-1471)
- `CreadorCatalogoSonidos` lee las carpetas nuevas. Si un ID coincide con uno viejo, los archivos nuevos reemplazan a los finales viejos.
  El catálogo quedó con 230 IDs.
- `MapaSonidos19nov`: los IDs viejos que el código ya pedía suenan con el archivo nuevo. La tabla completa está en T-1471. Ejemplos:
  - ARM_PIS → W01. ARM_SUB → W01 con tono base 1.25.
  - ARM_ESC → W02, ARM_FUS → W03, ARM_SNI → W04, ARM_REV → W06, ARM_PLA → W07.
  - ARM_BALA_IMPACTO_* → IMP_BALA_*. VEH_EXPLOSION → EXP_ONDA.
  - UI_ACEPTAR/CANCELAR → UI_SELECCIONAR/ATRAS. UI_OBJETIVO_* → HUD_*. ECO_COMPRA → TIE_COMPRAR. ECO_SIN_DINERO → UI_ERROR.
  - JUG_PASO_* → PAS_*. VEH_VIEJO_* → VEH_NORMAL_*. CAL_PATRULLA_SIRENA → POL_SIRENA_LOOP.
- Todos los IDs nuevos suenan por defecto.
- Política de mezcla para cada ID nuevo (grupo, voces, volumen, distancia, tiempo mínimo, prioridad):
  - Disparos: 0.8–0.9, de 100 a 150 m.
  - Explosiones: 0.9, de 160 a 200 m.
  - Impactos: 0.6–0.7, 35 m.
  - Pasos: 0.45, 20 m.
  - UI: 0.55–0.6.
  - Motores: 0.45–0.5, de 40 a 60 m.
  - Sirena: 3 a la vez, 120 m.
- Nada suena idéntico dos veces: todo ID tiene variación de tono y de volumen.
  - Los disparos: ±2.5 % de tono y hasta −5 % de volumen, con el mismo clip por arma. Así se relajó la regla T-676, como pidió Tony.
- Los tiros 3D de enemigos (a más de 3 m del jugador) van en un grupo aparte y no cortan los del jugador.
- Ducking simple: el ambiente baja a ×0.7 durante 0.3 s con cada disparo o explosión.

## Qué sonido quedó en qué parte del juego
| Parte | Evento → sonido |
| --- | --- |
| UI | Mover → UI_MOVER. Aceptar → UI_SELECCIONAR. Volver → UI_ATRAS. Sin dinero o error → UI_ERROR (tienda, guarida, negocios, y clínica y mejoras por mapa). Pestañas del inventario y del teléfono → UI_PESTANA. Abrir y cerrar pausa, teléfono y mapa → UI_MENU_ABRIR/CERRAR. Título del inicio → UI_LOGO_INTRO |
| HUD | Objetivo nuevo → HUD_NOTIFICACION. Objetivo cumplido → HUD_LOGRO. Vida baja → HUD_VIDA_BAJA (2D, una vez al cruzar el umbral) |
| Tienda / inventario | Comprar → TIE_COMPRAR. Poner o quitar de la rueda → TIE_EQUIPAR/TIE_DESEQUIPAR |
| Armas del jugador | Pistola W01. Escopeta W02 + ARM_W02_BOMBA tras cada tiro. Fusil W03 en cada bala de la ráfaga + ARM_COLA_CALLE al soltar. Francotirador W04 + ARM_W04_CERROJO. Subfusil W01 agudo. Revólver W06. Plasma: ARM_W07_CARGA (bucle) → ARM_W07_DISPARO, y ARM_W07_RAYO en bucle. Sacar arma → ARM_W06_SACAR. Recarga del fusil, subfusil y plasma → ARM_W03_RECARGA_CARGADOR |
| Granada y prótesis | ARM_GRANADA_SEGURO al lanzar, ARM_GRANADA_REBOTE (3D) en cada rebote, EXP_GRANADA al explotar. Láser → IMP_LASER_DISPARO. Fijar misiles → IMP_MISILES_FIJAR |
| Enemigos | Pandillero y policía disparan con W01 (o W02 con perdigones) en 3D; antes su tiro no sonaba. IMP_BALA_ZUMBIDO cuando un tiro falla cerca del jugador |
| Impactos | Por material: persona → IMP_BALA_CARNE (en la cabeza IMP_BALA_CABEZA); robot, dron o mecha → IMP_BALA_ROBOT; carro o metal → IMP_BALA_METAL; vidrio → IMP_BALA_CRISTAL; resto → IMP_BALA_CONCRETO |
| Explosiones | Carro, barril y dron → EXP_ONDA (máx. 3 cada 0.6 s en cadena). Carro en llamas → FUE_LOOP_CHICO en bucle hasta que se apaga |
| Pasos | Según el piso bajo el jugador: PAS_CONCRETO (por defecto), PAS_METAL, PAS_MADERA (también interiores), PAS_TIERRA, PAS_GRAVA, PAS_MOJADO (charco o agua). Un paso cada 0.25 s como mínimo |
| Jugador | Caída de más de 1.2 m → JUG_ATERRIZAJE. Rodar → JUG_RODADA. Agarrar un borde, escalera o tubo → JUG_TREPAR. Golpe que conecta → MEL_GOLPE_CARNE; golpe al aire → MEL_GOLPE_AIRE |
| Interacción | Puertas según el tipo: INT_PUERTA_MADERA, METAL, CRISTAL o REJA (ahora suenan todas, también las corredizas y las cortinas). Dinero en el piso → INT_RECOGER_OBJETO. Maletín del asalto → INT_MALETIN_CERRAR |
| Consumibles | Bebida energética → CON_LATA_ABRIR y luego CON_BEBER. Café → CON_BEBER. Papitas → CON_PAPITAS_COMER. Venda → CON_VENDA. Batería de escudo y otros → CON_CURACION |
| Vehículos | Al subirse: ARRANQUE y después el bucle RALENTI, con el tono de 0.8 a 1.8 según la velocidad. Al bajarse: APAGAR. Por tipo: normal VEH_NORMAL_*, moto MOTO_*, moto grande MOTO_* a tono 0.8, pesado VEH_PESADO_*, superdeportivo VEH_NORMAL_* a tono 1.25, eléctrico VEH_ELECTRICO_ARRANQUE + normal a tono 1.4. También: VEH_ACELERON al pisar a fondo desde lento; VEH_FRENO_FUERTE al derrapar; VEH_CHOQUE / VEH_CHOQUE_FUERTE según la fuerza, con VEH_CRISTAL_ROMPER en los muy fuertes; VEH_PUERTA_ABRIR/CERRAR; VEH_ALARMA (8 s) al forzar un carro estacionado; VEH_TOPE en golpes de suspensión; claxon VEH_CLAXON_CAMION en pesados y VEH_CLAXON_NORMAL en el resto |
| Policía | POL_SIRENA_LOOP en bucle por patrulla en persecución (máx. 3). POL_SIRENA_CORTA al empezar a perseguir. POL_RADIO_AVISO al llegar una patrulla o refuerzos (cada 15 s como mínimo). POL_RADIO_DESPACHO al subir el nivel de búsqueda. POL_NOVA_AVISO cuando llega la respuesta de Nova (más de 5 estrellas) |

## Lo que no quedó conectado (hay archivo, falta el evento en el juego)
- No hay hackeo ni interruptores de luz en el juego: INT_TECLEO_HACKEO, INT_HACKEO_EXITO, INT_HACKEO_FALLO e INT_INTERRUPTOR_LUZ no se piden.
- No hay arma de Nova que dispare (el guardia Nova solo golpea): ARM_NOVA_DISPARO y ARM_NOVA_CARGA_DISPARO no se piden.
- No se sabe la superficie bajo el carro: VEH_LLANTAS_GRAVA no se pide. No hay juntas de puente marcadas: VEH_PUENTE tampoco.
- VEH_ELECTRICO_ACELERAR, VEH_ELECTRICO_MOVER y VEH_ELECTRICO_PASAR no se piden (solo VEH_ELECTRICO_ARRANQUE). Además, hoy ningún modelo se
  llama eléctrico, camión o moto grande, así que esos grupos solo suenan si se marcan en el prefab (`tipoSonidoManual`).
- MEL_GOLPE (sin sufijo) no se pide; los puñetazos usan MEL_GOLPE_CARNE y MEL_GOLPE_AIRE.
- EXP_GRANADA y EXP_ONDA son las únicas explosiones: no hay una propia para el carro.

## Por revisar a oído (Tony)
- **POL_NOVA_AVISO**: aviso de llegada de Nova.
- **CON_CURACION**: suena con la batería de escudo; es de estilo fantasía.
- **VEH_ELECTRICO_ARRANQUE**: en carros eléctricos, con el ralentí normal a tono 1.4.
- **IMP_BALA_ROBOT**: impactos en drones, robots y mechas.
- **Tonos reciclados**:
  - subfusil a 1.25;
  - superdeportivo a 1.25;
  - moto grande a 0.8;
  - motor de 0.8 a 1.8;
  - 0.8 s entre el arranque y el ralentí.
- **Recortes de motor**:
  - MOTO_ARRANQUE_01..03 y VEH_NORMAL_ARRANQUE_03: las grabaciones ya traen ralentí casi desde el inicio, y MOTO_ARRANQUE_03 no tiene arranque propio.
  - VEH_NORMAL_APAGAR_02 y _03 son muy cortos.
  - Los ralentís 02 y 03 cambian de nivel dentro de la grabación.
- **Ducking** (×0.7 durante 0.3 s) y el nivel de la UI frente a los disparos.
- **Puertas**: ahora suenan todas, también las cortinas y las corredizas de cristal.
- **Zumbido de bala**: suena cada vez que un pandillero falla con línea libre.
- **Clasificación por nombre**: palabras como "obra", "lote", "tabla" y "reja" pueden poner tierra o grava donde no va.

## Pruebas
- Compila sin errores (un arreglo: `preloadAudioData` ahora va en `AudioImporterSampleSettings`).
- EditMode, 27 clases de sonido por clase: **235/235**. Antes hubo dos arreglos:
  - el prefijo "ARM_BALA_" quedaba como literal en la mezcla;
  - la prueba de "solo el jugador pide pasos" no conocía los PAS_.
- PlayMode por clase:
  - Tests.Sonido: **19/19**.
  - PasosT596, DisparoLimpioT676, ExplosionCarro y ChispeoPlasmaT1309: **13/13**. DisparoLimpioT676 ahora acepta el impacto de cabeza.
  - RecorridoRonda2Pruebas: **0/1**. Falla "El ambiente arranca apagado" (0.8 en vez de 0). Estas tareas no tocaron ese código: es el piso de
    ambiente de T-977 contra una prueba vieja.
- Pruebas que se ajustaron a los IDs nuevos:
  - T593, T596, T620, T977, T1309, T1322 y DisparoSiempreIgualT676 (ahora variación chica, mismo clip);
  - RecorridoRonda2 (W02) y ExplosionCarro (EXP_ONDA).
- **Builds: no se hicieron.** El de Builds/Demo (ConstruirDemo.Construir) lo detuvo Claude Code por falta de memoria del sistema a mitad de la carga
  de escenas y no se relanzó. Falta correr, con memoria libre y uno a la vez:
  `-executeMethod MechaCyberpunk.Progresion.Prologo.Editor.ConstruirDemo.Construir` (Builds/Demo) y `...ConstruirDemo.ConstruirJuego` (Builds/Juego/CyberCity.exe).
