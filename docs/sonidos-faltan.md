# Sonidos que faltan (actualizado 20-nov, T-1530)

Cruce de `3d sonidos/FALTANTES.md` (20-nov, noche) con `docs/lista-sonidos-cybercity.txt` y con lo que ya está en `Assets/Arte/Audio`
(tabla de IDs en `herramientas/sonidos/ids-20nov.csv`). Lo del 19-nov se conserva abajo, sin borrar; aquí se marca qué llegó.

## Llegó el 20-nov (estaba en la lista "Faltan" del 19-nov)
- **Armas:** clic vacío -> ARM_SIN_BALAS. Casquillos -> CAS_CHICO_01/02, CAS_GRANDE_01. Tambor/martillo del revólver -> ARM_W06_MECANICA_01..05.
  Plasma completo -> ARM_W07_GATILLO, CARGA, DISPARO, RAYO_LOOP, RAYO_CAPA_EXTRA, ENFRIAR, SIN_ENERGIA. Lanzamiento de misiles -> IMP_MISILES_LANZAR.
  "Bloop" del lanzagranadas -> ARM_W08_DISPARO (es el mismo audio del rebote EMP: revisar a oído).
- **Jugador:** roce de ropa (JUG_ROPA_ROCE), latido (JUG_LATIDO_VIDA_BAJA), pitido de oído (JUG_ZUMBIDO_EXPLOSION), bloqueo (MEL_BLOQUEO), golpe a metal (MEL_GOLPE_METAL).
- **Interacción y consumibles:** recoger dinero (INT_RECOGER_DINERO), bolsa de papitas (CON_PAPITAS_ABRIR).
- **Policía:** ambulancia (AMB_SIRENA_LOOP), sube estrella (POL_ESTRELLA_SUBE), rotores (NAV_ROTORES_LOOP), reflector (NAV_REFLECTOR), nave dañada (NAV_DANIADA),
  estática de radio (POL_RADIO_ESTATICA), sirena del muro (POL_SIRENA_MURO). Quinta voz "Mexican Police" (POL_RADIO_AVISO_05).
- **Ya no estaba en ninguna lista, llegó completo:** Nova (pasos 12 + 10, armadura, escudo, jetpack, caída y encendido), NPC dolor hombre/mujer, robots y drón pesado,
  explosiones y fuego (EXP_CARRO, DRON_ROBOT, NAVE, BARRIL, ESCOMBROS, CRISTALES, FUE_ENCENDER, FUE_LOOP_GRANDE, DES_CRISTAL_VENTANA),
  exo e implantes (puesto, servos, rueda de cámara lenta entrar/salir, salto súper, escudo, visor, taladro, sinapsis),
  ambientes (Nova, Lomas, suburbios de noche, azotea), clima (lluvia fuerte, tormenta, viento, lluvia en lámina y dentro del carro),
  interiores (tienda de Tino, taller de Canepa, armería, bodega), objetos (transformador, aire, cámara, expendedora, refrigerador, tele que se reinicia),
  casino (tragamonedas, premios, fichas, ruleta, cartas), misiones y HUD (temporizador, mensaje de celular, esposas, has muerto),
  y una percusión de cumbia para la radio (RAD_EST1_CUMBIA_PERCUSION).

## Decidido en el LEEME del 20-nov: no lleva sonido propio (sale de lo que ya hay)
- Sacar y guardar arma: la recarga de cada arma. Brazo robótico: EXO_PUESTO_LOOP o el exo puesto.
- Motores deportivos: el loop del carro normal (sin turbo ni petardeo). Charco, rin, cajuela, llaves, direccionales, puerta de tijera, raspón y atropello: VEH_CHOQUE_*.
- Atropello y daño a NPCs: NPC_H_DOLOR / NPC_M_DOLOR. Cortina metálica: el sonido de garage que ya está en el juego.
- Altavoz de la policía: de las voces POL_RADIO_AVISO_*. Las voces de despachador (POL_RADIO_DESPACHO_01/02) se descartaron.
- Misiones y HUD que repiten lo que hay: nueva misión y objetivo -> HUD_NOTIFICACION; completada -> HUD_LOGRO; fallida -> UI_ERROR; checkpoint y marcador -> UI_SELECCIONAR;
  pausa y mapa -> UI_MENU_ABRIR/CERRAR; guardar -> HUD_NOTIFICACION; indicador de daño -> IMP_BALA_ZUMBIDO; arrestado -> MEL_GOLPE + sirena alejándose + esposas.
- Robots: caída y apagado reciclan ROB_MUERTE, ROB_PES_PLACA_CAE y ROB_SERVOS con pitch bajo. Drones: sin enjambre, caída ni cápsula (se reciclan los de dron que ya hay).
- Fuera del juego: caer al agua, burbujas, moto cayendo, semáforo de arrancones, alarma del atraco, caja fuerte. Zonas viejas sin buscar (muro cerrado): Malecón, Puerto, Cerro, Autopista.
- Arma de Nova: sin sonidos propios (recicla el plasma con otro pitch).

## Sigue faltando (según FALTANTES.md del 20-nov)
1. **Por buscar:** pasos en loseta. (Arena y pasto dejaron de pedirse; NOV_PASOS_CAMINAR_12 es de pasto y podría servir para pasto de Nova.)
2. **Los graba el usuario:** voces del jugador (esfuerzo/salto, muerte, respiración cansada, tos, quejido al instalar implante), voces de SKULLS, policías a pie,
   agentes de Nova (con filtro de casco) y el altavoz si no sale de las voces "Mexican".
3. **Pendiente de decidir:** misil en vuelo. Arma de Nova propia (hoy recicla el plasma).
4. **Categorías que no se han buscado:** música (MUS_*, 15 temas en la lista) y estaciones de radio (RAD_EST2..5, RAD_LOCUTOR_*, RAD_APAGAR; de la EST1 solo hay una percusión).
   Ambiente sin sonido propio: Deshuesadero, Centro y muro. Jingle de la pantalla de anuncio (OBJ_PANTALLA_ANUNCIO).

## En la lista, sin archivo y sin decisión escrita (para que alguien decida si se piden)
- Peatones: grito de miedo, muerte, enojo, saludo, risa, llama a la policía, foto de celular, huye, plática (solo hay NPC_H/M_DOLOR).
- UI/HUD: deslizador, pantalla de carga y su fin, dinero sube/baja, escudo roto/lleno, cambiar arma, ping del minimapa, aparece interacción, despertar en la clínica.
- Ambiente: suburbios de día, mercado, tianguis, neón alto, callejón, túnel, clima de lluvia ligera, truenos sueltos, amanecer y anochecer.
  Interiores: cuarto, clínica del Doc, casino, hotel, oficinas de Nova. Objetos: zumbido de neón y poste, tinaco, semáforo peatonal, gotera, alcantarilla, pluma de caseta,
  montacargas, campanas, kiosco, tendedero. Casino: bola de ruleta suelta (CAS_RULETA_GIRA ya trae la bolita).
- Carros y policía: motores de tipos sin grabación propia, robar carros, neumáticos y derrapes por superficie.
  (Nota: el 19-nov se listó que casi todo esto se reutiliza de los sonidos de carro; revisar contra la mezcla antes de pedir archivos nuevos.)

## Notas de integración (T-1530) que afectan a lo que falta
- Renombres con el mismo GUID: ARM_W07_DISPARO_01 -> ARM_W07_DISPARO, ARM_W07_RAYO -> ARM_W07_RAYO_LOOP, ARM_NOVA_DISPARO_01 -> ARM_W07_GATILLO,
  ARM_NOVA_CARGA_DISPARO -> ARM_W07_SIN_ENERGIA y POL_NOVA_AVISO -> NOV_CAIDA_Y_ENCENDIDO. El código y el catálogo que pidan los nombres viejos hay que cambiarlos (T-1531 en adelante).
- POL_RADIO_DESPACHO_01/02 siguen en Assets/Arte/Audio/10_Policia (el código las usa) aunque la fuente ya las descartó: quitarlas cuando POL_RADIO_AVISO_* las reemplace.
- ARM_W07_GATILLO (1.2 s) y ARM_W07_SIN_ENERGIA (1 s, grave y con cola) son más largos y "de golpe" que un clic de gatillo: se respetó lo que dijo el usuario; confirmar a oído.
- EXO_PUESTO_LOOP_01/02 y OBJ_MAQUINA_EXPENDEDORA decaen a silencio (no son bucles continuos): sirven como evento repetido, no como zumbido continuo.
- EXO_RUEDA_CAMARA_LENTA_SALIR parte en 2.0 s (no en el último segundo literal, que ya es casi silencio).

## Historial 19-nov (se conserva tal cual)

Lista "Faltan" de `3d sonidos/LEEME_SONIDOS.md` (19-nov), más lo que se encontró al integrar los sonidos nuevos (T-1470..T-1476).

## Del LEEME
- **Armas:** clic vacío, casquillos cayendo, tambor/martillo del revólver, sacar o guardar las demás armas, sobrecalentado y enfriado del plasma,
  lanzamiento y vuelo de misiles, "bloop" del lanzagranadas, más variaciones de disparo (_02, _03).
- **Impactos:** rebote (opcional) y más variaciones.
- **Pasos:** arena, pasto, loseta.
- **Jugador:** roce de ropa, todas las voces (esfuerzo, dolor, muerte, respiración, tos), latido, pitido de oído, bloqueo, golpe a metal, brazo robótico.
- **Interacción:** cortina metálica, recoger dinero.
- **Consumibles:** bolsa de papitas al abrirse.
- **Vehículos:** motor deportivo (turbo, petardeo), charco, rin con chispas, cajuela, llaves, direccionales, puerta de tijera, raspón lateral,
  atropello, caer al agua y burbujas, moto cayendo.
- **Policía:** sirena de ambulancia, sube estrella, altavoz, rotores de nave, reflector, nave dañada, estática de radio.

## Encontrado al integrar
- **Sin evento en el juego (el archivo ya está):**
  - INT_TECLEO_HACKEO, INT_HACKEO_EXITO, INT_HACKEO_FALLO: no hay hackeo.
  - INT_INTERRUPTOR_LUZ: no hay interruptores.
  - ARM_NOVA_DISPARO, ARM_NOVA_CARGA_DISPARO: Nova no dispara.
  - VEH_PUENTE: no hay juntas de puente.
  - VEH_LLANTAS_GRAVA: no se sabe el piso bajo el carro.
  - VEH_ELECTRICO_ACELERAR, VEH_ELECTRICO_MOVER, VEH_ELECTRICO_PASAR.
  - MEL_GOLPE.
- **Faltan archivos para IDs que el código pide** (siguen con el sonido viejo):
  - ARM_LGR_DISPARO (lanzagranadas);
  - recargas propias de pistola, escopeta y francotirador (ARM_PIS_RECARGA, ARM_ESC_RECARGA_CARTUCHO, ARM_SNI_RECARGA);
  - ARM_PIS_SACAR y un sacar propio por arma;
  - ARM_LASER_AVISO, VEH_FORZAR_PUERTA, VEH_PASA_CERCA;
  - CAL_PATRULLA_MEGAFONO, UI_GUARDADO, UI_TELEFONO.
- **Pisos sin sonido propio** (suenan a concreto): arena, pasto, loseta, alfombra, vidrio roto.
- **Volumen por llamada:** correr no suena más fuerte ni agacharse más bajo, porque ISonidos no tiene volumen por llamada (tarea de Core si se quiere).
- **Clima:** la calle mojada por lluvia no se detecta en los pasos; solo suena mojado por el nombre del charco o del agua.
- **Explosiones:** una propia para el carro (hoy usa EXP_ONDA), impactos de balas enemigas contra el mundo, lanzamiento de misiles (hoy suena W02).
- **Alarma:** suena al forzar carros estacionados, no al robar uno del tráfico. Falta una tecla de claxon para el jugador.
- **Policía:** sonido al detener al jugador (no hay evento de arresto), estática de radio, megáfono nuevo, rotores, reflector, ambulancia.
- **Config de clínica y mejora de armas:** siguen pidiendo ECO_SIN_DINERO en sus Config; el mapa lo hace sonar con UI_ERROR.

## T65: sonidos viejos desconectados que quedan en silencio

El catálogo (CreadorCatalogoSonidos.Crear) ya no lee las carpetas viejas (JUG, PUN, ARM, VEH, PAN, CAL, ZON, ECO, UI, MUN, AMB ni Provisional). Estos IDs los pide el código y no tienen equivalente en 01_UI a 19_Voces: no suenan hasta que haya un archivo nuevo.

| ID | Lo pide |
| --- | --- |
| AMB_BAR | ReglaAmbienteZona.cs |
| AMB_CALLE_DIA | ReglaAmbienteZona.cs |
| AMB_CALLE_NOCHE | GestorSonidos.cs, ReglaAmbienteZona.cs |
| AMB_CASINO | ReglaAmbienteZona.cs |
| AMB_CLANDESTINO | ReglaAmbienteZona.cs |
| AMB_CLINICA | ReglaAmbienteZona.cs |
| AMB_INTERIOR_CASA | GestorSonidos.cs, ReglaAmbienteZona.cs |
| AMB_MERCADO | ReglaAmbienteZona.cs |
| AMB_NEON_ALTO | ReglaAmbienteZona.cs, ReglaMezclaSonidos.cs |
| AMB_TIANGUIS | ReglaAmbienteZona.cs |
| ARM_CAMBIAR | ReglaMezclaSonidos.cs, SonidosArmasT1472.cs |
| ARM_DISPARO_LEJANO | ReglaMezclaSonidos.cs |
| ARM_ESC_RECARGA_CARTUCHO | ReglaMezclaSonidos.cs, SonidosArmasT1472.cs |
| ARM_FUS_RAFAGA_COLA | ReglaMezclaSonidos.cs |
| ARM_LASER_AVISO | GuardiaNova.cs, PistolaPandillero.cs |
| ARM_LGR_DISPARO | ReglaMezclaSonidos.cs |
| ARM_PIS_RECARGA | ReglaMezclaSonidos.cs, SonidosArmasT1472.cs |
| ARM_PIS_SACAR | ReglaMezclaSonidos.cs |
| ARM_SNI_RECARGA | ReglaMezclaSonidos.cs, SonidosArmasT1472.cs |
| ARM_SNI_ZOOM | ZoomSniper.cs |
| CAL_DRON_ZUMBIDO | CapsulaCaida.cs, DronMundo.cs, ReglaMezclaSonidos.cs |
| ECO_CLINICA_DESPERTAR | DoctorClinica.cs |
| JUG_CAE_DERROTADO | ReglaMezclaSonidos.cs, VidaAPie.cs |
| JUG_CORRER_RESPIRA | ReglaMezclaSonidos.cs |
| JUG_REGENERA | VidaAPie.cs |
| JUG_SALTO | ControladorAPie.cs, ReglaMezclaSonidos.cs |
| MUN_AIRE_ACONDICIONADO | ReglaMezclaSonidos.cs |
| MUN_CHISPAS_ELECTRICAS | CarroTrafico.cs, DanioCarro.cs, ReglaMezclaSonidos.cs |
| MUN_HOLOGRAMA | CapsulaCaida.cs, ReglaMezclaSonidos.cs |
| MUN_NEON_ZUMBIDO | ReglaMezclaSonidos.cs |
| MUN_PERRO_LADRA | ReglaMezclaSonidos.cs |
| PAN_ALIADO_CAE | ReglaMezclaSonidos.cs |
| PAN_ALIADO_ESPERA | ReglaMezclaSonidos.cs |
| PAN_ALIADO_RECLUTA | ReglaMezclaSonidos.cs |
| PAN_ALIADO_SIGUE | ReglaMezclaSonidos.cs |
| PAN_JEFE_CAE | PandilleroAPie.cs |
| PAN_JEFE_ENTRADA | JefePandilla.cs, ReglaMezclaSonidos.cs |
| PAN_RIVAL_ALERTA | PandilleroAPie.cs, ReglaMezclaSonidos.cs |
| PAN_RIVAL_CAE | PandilleroAPie.cs, ReglaMezclaSonidos.cs |
| PAN_RIVAL_PROVOCA | ReglaMezclaSonidos.cs |
| PAN_SILBIDO_LLAMAR | ReglaMezclaSonidos.cs |
| PAN_VECINO_AUXILIO | ReglaMezclaSonidos.cs |
| PUN_BLOQUEO | VidaAPie.cs |
| UI_CONTINUARA | PantallaContinuara.cs |
| UI_TEXTO | ColaAvisos.cs, ReglaMezclaSonidos.cs |
| VEH_FORZAR_PUERTA | CarroEstacionado.cs |
| VEH_PASA_CERCA | CarroTrafico.cs, CarroViejo.cs, ReglaMezclaSonidos.cs |

Quitado del código: la respiración al correr (JUG_CORRER_RESPIRA) y el sonido de confirmación de kill (T64).

Ejemplo: el "¡hey!" cuando un enemigo te ve es PAN_RIVAL_ALERTA; falta una voz nueva de alerta de pandilleros que no sean SKULLS (SKU_GRITO_ALERTA está apagado por VocesPandilla.VocesSkullsActivas).
