# T-1471 Catalogo y mezcla de los sonidos nuevos (19-nov)

Rama: tony/T-1471-catalogo-mezcla. Sin compilar ni correr pruebas en el worktree (no tiene Library): las corre el orquestador.

## Que se hizo
- `CreadorCatalogoSonidos.Crear` lee ademas `Assets/Arte/Audio/01_UI .. 10_Policia`. Si un ID nuevo ya existia (UI_MOVER, UI_ERROR, VEH_PUERTA_ABRIR, VEH_CHOQUE_FUERTE...), sus archivos REEMPLAZAN a los finales viejos (`MapaSonidos19nov.Reemplazar`); los provisionales se conservan.
- `MapaSonidos19nov` (clase pura): lista de IDs nuevos y mapa viejo -> nuevo. `CatalogoSonidos.Elegir(viejo)` usa los clips del nuevo si los tiene; si no, el viejo (el Alias de ARM_SUB_RECARGA queda de respaldo).
- Interruptores: los IDs nuevos y los viejos mapeados quedan encendidos. `SincronizarInterruptores` prende los nuevos siempre y un viejo mapeado aunque estuviera apagado, si el nuevo ya tiene clips. Por eso esos interruptores no se pueden apagar a mano en el asset (se vuelven a prender al sincronizar).
- Mezcla: `PoliticaSonido` gana `TonoBase` (1) y `VariacionVolumen` (0), opcionales. `GestorSonidos` aplica tono = TonoBase x variacion, volumen x (1 - azar x VariacionVolumen), `AudioSource.priority` (`ReglaMezclaSonidos.Prioridad`) y ducking del ambiente (x0.7 durante 0.3 s al sonar un `ARM_*_DISPARO` o `EXP_*`; sin chequeo de distancia).
- Bucles: la sirena (POL_SIRENA_LOOP / CAL_PATRULLA_SIRENA) permite 3 a la vez, los RALENTI (VEH_NORMAL, MOTO, VEH_PESADO) 4, VEH_ELECTRICO_MOVER y FUE_LOOP_CHICO 3, VEH_ALARMA 2; el resto 1 (como antes).
- Tiros 3D ajenos: un `ReproducirEn` de un ID del grupo "arma" a mas de 3 m del jugador (IJugador registrado) usa el grupo "arma-enemiga" (3 voces): no corta la voz del arma del jugador ni comparte su tiempo minimo. Sin jugador registrado se comporta como antes. Riesgo: un tiro del jugador en 3D a mas de 3 m de su raiz se trataria como ajeno.
- T-676: el mismo clip por arma, pero tono +-2.5 % y volumen hasta -5 % (viejos y nuevos). Subfusil: ARM_SUB_DISPARO usa W01 con tono base 1.25.
- Secciones de volumen: los prefijos HUD_ y TIE_ van a Interfaz.
- Claxon: VEH_CLAXON_VIEJO se enciende (lo toca el jugador; claxon 2 voces, 0.8 s); VEH_CLAXON_TRAFICO sigue apagado (spam de trafico, T-977). ARM_BALA_ROZA se mapea a IMP_BALA_ZUMBIDO pero sigue apagado (T-729).
- Pruebas viejas actualizadas: T676 (variacion chica), `UnIdSinReglaSuenaNormal` (usa un ID inexistente: UI_ERROR ya tiene regla), T620 (volumen parejo solo para los de volumen no propio; asset: conteo >=; JUG_PASO_INTERIOR ya suena), T977 (idem), PlayMode T593 (tono dentro de +-2.5 %), PlayMode T620 (sirenas multiples; un viejo mapeado cuenta como sonado), IdsSonidoPruebas (escanea tambien las 10 carpetas nuevas).

## Mapa ID viejo -> ID nuevo
| Viejo | Nuevo | Nota |
| --- | --- | --- |
| ARM_PIS_DISPARO | ARM_W01_DISPARO | |
| ARM_SUB_DISPARO | ARM_W01_DISPARO | tono base 1.25 |
| ARM_ESC_DISPARO | ARM_W02_DISPARO | |
| ARM_ESC_BOMBEO | ARM_W02_BOMBA | |
| ARM_FUS_DISPARO | ARM_W03_DISPARO | |
| ARM_FUS_RECARGA, ARM_SUB_RECARGA | ARM_W03_RECARGA_CARGADOR | |
| ARM_SNI_DISPARO | ARM_W04_DISPARO | |
| ARM_SNI_CERROJO | ARM_W04_CERROJO | |
| ARM_REV_DISPARO | ARM_W06_DISPARO | |
| ARM_PLA_DISPARO | ARM_W07_DISPARO | |
| ARM_PLA_CARGA | ARM_W07_CARGA | |
| ARM_BALA_IMPACTO_CARNE / CONCRETO / METAL | IMP_BALA_CARNE / CONCRETO / METAL | |
| ARM_BALA_ZUMBIDO | IMP_BALA_ZUMBIDO | |
| ARM_BALA_ROZA | IMP_BALA_ZUMBIDO | queda apagado (T-729) |
| VEH_EXPLOSION | EXP_ONDA | |
| UI_ACEPTAR / UI_CANCELAR | UI_SELECCIONAR / UI_ATRAS | |
| UI_MAPA_ABRIR / UI_MAPA_CERRAR | UI_MENU_ABRIR / UI_MENU_CERRAR | |
| UI_TITULO_INICIO | UI_LOGO_INTRO | |
| UI_OBJETIVO_NUEVO / UI_OBJETIVO_CUMPLIDO | HUD_NOTIFICACION / HUD_LOGRO | |
| JUG_VIDA_BAJA | HUD_VIDA_BAJA | |
| ECO_COMPRA | TIE_COMPRAR | |
| ECO_SIN_DINERO | UI_ERROR | |
| ECO_ARMA_RECOGER, ECO_DINERO_RECOGER | INT_RECOGER_OBJETO | |
| JUG_PASO_ASFALTO / METAL / ASFALTO_MOJADO / INTERIOR | PAS_CONCRETO / PAS_METAL / PAS_MOJADO / PAS_MADERA | |
| JUG_CAER | JUG_ATERRIZAJE | |
| PUN_GOLPE_CARNE / PUN_GOLPE_AIRE | MEL_GOLPE_CARNE / MEL_GOLPE_AIRE | |
| MUN_PUERTA_CASA_ABRIR / CERRAR | INT_PUERTA_MADERA | |
| VEH_VIEJO_ARRANQUE | VEH_NORMAL_ARRANQUE | |
| VEH_VIEJO_MOTOR_RALENTI | VEH_NORMAL_RALENTI | |
| VEH_VIEJO_MOTOR_ACELERA | VEH_ACELERON | |
| VEH_CHOQUE_LEVE | VEH_CHOQUE | |
| VEH_FRENO_DERRAPE | VEH_FRENO_FUERTE | |
| VEH_ALARMA_ROBO | VEH_ALARMA | |
| VEH_CLAXON_VIEJO | VEH_CLAXON_NORMAL | encendido |
| VEH_CLAXON_TRAFICO | VEH_CLAXON_NORMAL | queda apagado |
| CAL_PATRULLA_SIRENA | POL_SIRENA_LOOP | |

Sin archivo nuevo que encaje (siguen con el viejo): ARM_LGR_DISPARO, ARM_PIS_RECARGA, ARM_ESC_RECARGA_CARTUCHO, ARM_SNI_RECARGA, ARM_PIS_SACAR, ARM_LASER_AVISO, VEH_FORZAR_PUERTA, VEH_PASA_CERCA, CAL_PATRULLA_MEGAFONO, UI_GUARDADO, UI_TELEFONO.

Los IDs viejos con regla propia (disparos, pasos JUG_PASO_, puertas de casa, derrape, choque, dinero, objetivos) conservan su regla; los que no tenian heredan la del nuevo.

## Mezcla de los IDs nuevos (grupo, voces, volumen, +-tono, +-volumen, minimo s, distancia m)
| IDs | Grupo | Voces | Vol | Tono | Vol var | Min s | Dist |
| --- | --- | --- | --- | --- | --- | --- | --- |
| ARM_W01/W03/W07_DISPARO | arma | 1 | 0.8 | 0.025 | 0.05 | 0 | 100 / 120 / 100 |
| ARM_W02_DISPARO, W06_DISPARO | arma | 1 | 0.85 | 0.025 | 0.05 | 0 | 100 / 110 |
| ARM_W04_DISPARO | arma | 1 | 0.9 | 0.025 | 0.05 | 0 | 150 |
| ARM_NOVA_DISPARO, NOVA_CARGA_DISPARO | arma-nova | 1 | 0.85 / 0.8 | 0.025 | 0.05 | 0 | 100 |
| ARM_W03_RECARGA_CARGADOR, W06_SACAR | arma | 1 | 0.7 / 0.6 | 0.02-0.03 | 0.05 | 0 | 2D |
| ARM_W02_BOMBA, W04_CERROJO | arma-accesorio | 2 | 0.7 | 0.02 | 0.05 | 0 | 2D |
| ARM_W07_CARGA / W07_RAYO | arma-accesorio | 1 | 0.5 / 0.7 | 0.03 | 0.05 | 0 / 0.1 | 2D / 80 |
| ARM_GRANADA_SEGURO / REBOTE | accesorio / granada-rebote | 2 | 0.6 / 0.55 | 0.04 / 0.06 | 0.06 | 0.1 | 40 |
| ARM_COLA_CALLE | arma-cola | 1 | 0.5 | 0.03 | 0.06 | 0.1 | 150 |
| IMP_LASER_DISPARO / IMP_MISILES_FIJAR | laser / misiles | 1 | 0.7 / 0.6 | 0.04 / 0.03 | 0.06 | 0.1 / 0.5 | 80 / 60 |
| IMP_BALA_* | arma-impacto | 2 | 0.6-0.7 | 0.06 | 0.08 | 0.1 | 35 |
| IMP_BALA_ZUMBIDO | bala-zumbido | 2 | 0.5 | 0.08 | 0.08 | 0.25 | 30 |
| EXP_GRANADA / EXP_ONDA | explosion | 2 | 0.9 | 0.04 | 0.05 | 0.1 | 160 / 200 |
| FUE_LOOP_CHICO | fuego | 3 | 0.3 | 0 | 0.1 | 0.5 | 30 |
| PAS_* | paso | 1 | 0.45 | 0.06 | 0.1 | 0.15 | 20 |
| JUG_ATERRIZAJE / RODADA / TREPAR | jugador-cuerpo | 1 | 0.6 / 0.55 / 0.5 | 0.05 | 0.06 | 0.3 | 25 |
| MEL_GOLPE* | melee | 2 | 0.75 | 0.05 | 0.06 | 0.1 | 30 |
| INT_PUERTA_* | puerta-interaccion | 2 | 0.6 | 0.04 | 0.06 | 0.3 | 22 |
| INT_HACKEO_*, INT_TECLEO_HACKEO | hackeo | 1 | 0.6 / 0.4 | 0.03-0.05 | 0.06 | 0.5 / 0.1 | 15 / 10 |
| INT_INTERRUPTOR_LUZ, MALETIN_CERRAR | interaccion | 1 | 0.5 | 0.04 | 0.06 | 0.2 | 15 |
| INT_RECOGER_OBJETO | dinero | 1 | 0.55 | 0.05 | 0.06 | 0.25 | 20 |
| CON_* | consumible | 1 | 0.6 | 0.03 | 0.05 | 0.3 | 2D |
| UI_* | ui | 2 | 0.55 (ERROR 0.6) | 0.03 | 0.05 | 0.05-0.1 | 2D |
| UI_MENU_*, UI_LOGO_INTRO | ui-menu / ui-logo | 1 | 0.55 / 0.6 | 0.02 / 0 | 0.05 / 0.03 | 0.1 / 1 | 2D |
| HUD_LOGRO, HUD_NOTIFICACION | objetivo | 1 | 0.6 | 0.02 | 0.05 | 1 | 2D |
| HUD_VIDA_BAJA | vida-baja | 1 | 0.55 | 0.02 | 0.05 | 2 | 2D |
| TIE_* | tienda | 1 | 0.55 | 0.03 | 0.05 | 0.2 | 2D |
| VEH_*_ARRANQUE / APAGAR, MOTO_ARRANQUE / APAGAR | arranque | 1 | 0.6 | 0.03 | 0.05 | 0.3 | 50 (pesado 60) |
| VEH_NORMAL_RALENTI, MOTO_RALENTI, VEH_PESADO_RALENTI | motor | 4 | 0.5 | 0.03 | 0.05 | 0.3 | 45 / 45 / 60 |
| VEH_ELECTRICO_MOVER | motor | 4 | 0.45 | 0.03 | 0.05 | 0.3 | 40 |
| VEH_ACELERON, VEH_ELECTRICO_ACELERAR | motor-acelera | 2 | 0.55 | 0.05 | 0.06 | 0.5 | 50 |
| VEH_ELECTRICO_PASAR | pasa-cerca | 1 | 0.7 | 0.06 | 0.06 | 1.5 | 45 |
| VEH_ALARMA | alarma | 1 | 0.45 | 0.02 | 0.05 | 1 | 60 |
| VEH_CHOQUE / VEH_CHOQUE_FUERTE | choque | 2 | 0.75 / 0.85 | 0.05 | 0.06 | 0.15 | 60 / 80 |
| VEH_CLAXON_CAMION / NORMAL | claxon | 2 | 0.6 | 0.06 / 0.08 | 0.06 | 0.8 | 70 / 40 |
| VEH_CRISTAL_ROMPER | cristal | 2 | 0.65 | 0.05 | 0.06 | 0.2 | 40 |
| VEH_FRENO_FUERTE | derrape | 1 | 0.5 | 0.05 | 0.06 | 1.5 | 40 |
| VEH_LLANTAS_GRAVA, VEH_PUENTE, VEH_TOPE | llantas / puente / tope | 1 | 0.4 / 0.5 / 0.5 | 0.03-0.04 | 0.06 | 1 / 1 / 0.5 | 30 / 40 / 30 |
| VEH_PUERTA_ABRIR / CERRAR | puerta-carro | 1 | 0.6 | 0.04 | 0.06 | 0.5 | 25 |
| POL_SIRENA_LOOP | sirena | 3 | 0.7 | 0 | 0.05 | 0.5 | 120 |
| POL_SIRENA_CORTA | sirena-corta | 1 | 0.7 | 0.03 | 0.06 | 1 | 100 |
| POL_NOVA_AVISO | nova-voz | 1 | 0.85 | 0.03 | 0.05 | 3 | 40 |
| POL_RADIO_AVISO / DESPACHO | radio-policia | 1 | 0.6 | 0.03 | 0.06 | 2 / 3 | 25 |

Prioridad de AudioSource: disparos 32, voces JUG/PAN/POL 48, EXP/IMP/MEL 64, resto 100, pasos 160, ambiente y fuego 200.

## Despues de importar los WAV
Correr `MechaCyberpunk.Progresion.Editor.CreadorCatalogoSonidos.Crear` (menu Mecha Cyberpunk/Sonido/Crear catalogo): arma el catalogo y sincroniza los interruptores.

## Por revisar a oido
Tono 1.25 del subfusil, ducking (x0.7 durante 0.3 s) y niveles de UI frente a los disparos.
