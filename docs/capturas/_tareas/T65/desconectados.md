# T65: sonidos viejos desconectados

Qué se hizo: CreadorCatalogoSonidos.Crear solo lee las carpetas nuevas (01_UI a 19_Voces y Musica con MUS_MENU_PRINCIPAL), ya no llama a GeneradorSonidosProvisionales y no lee Provisional. Ningún archivo se borró.

## Carpetas de Assets/Arte/Audio desconectadas del catálogo

AMB, ARM, CAL, ECO, JUG, MUN, PAN, PUN, UI, VEH, ZON (289 archivos .wav) y Provisional (41 .wav generados). Los 4 Ambiente_*.wav de la raíz siguen puestos en Barrio.prefab (AmbienteSonoro) pero con capasEncendidas = 0: silencio.

## IDs que el código pide y ahora suenan con un archivo nuevo (alias nuevos en MapaSonidos20nov)

| ID viejo | Suena con |
| --- | --- |
| JUG_DANIO | NPC_H_DOLOR |
| PAN_RIVAL_DANIO | NPC_H_DOLOR |
| ZON_CONQUISTADA | HUD_LOGRO |
| ZON_RESPETO_SUBE | HUD_NOTIFICACION |
| CAL_BAJA_NIVEL | HUD_NOTIFICACION |

(Los alias de T-1471 y T-1531 que ya existían siguen igual: armas, impactos, UI, pasos, puertas, vehículos, etc.)

## IDs que quedan en silencio (sin equivalente nuevo)

Ver la sección T65 de docs/sonidos-faltan.md. Total: 47 IDs.

AMB_BAR, AMB_CALLE_DIA, AMB_CALLE_NOCHE, AMB_CASINO, AMB_CLANDESTINO, AMB_CLINICA, AMB_INTERIOR_CASA, AMB_MERCADO, AMB_NEON_ALTO, AMB_TIANGUIS, ARM_CAMBIAR, ARM_DISPARO_LEJANO, ARM_ESC_RECARGA_CARTUCHO, ARM_FUS_RAFAGA_COLA, ARM_LASER_AVISO, ARM_LGR_DISPARO, ARM_PIS_RECARGA, ARM_PIS_SACAR, ARM_SNI_RECARGA, ARM_SNI_ZOOM, CAL_DRON_ZUMBIDO, ECO_CLINICA_DESPERTAR, JUG_CAE_DERROTADO, JUG_CORRER_RESPIRA, JUG_REGENERA, JUG_SALTO, MUN_AIRE_ACONDICIONADO, MUN_CHISPAS_ELECTRICAS, MUN_HOLOGRAMA, MUN_NEON_ZUMBIDO, MUN_PERRO_LADRA, PAN_ALIADO_CAE, PAN_ALIADO_ESPERA, PAN_ALIADO_RECLUTA, PAN_ALIADO_SIGUE, PAN_JEFE_CAE, PAN_JEFE_ENTRADA, PAN_RIVAL_ALERTA, PAN_RIVAL_CAE, PAN_RIVAL_PROVOCA, PAN_SILBIDO_LLAMAR, PAN_VECINO_AUXILIO, PUN_BLOQUEO, UI_CONTINUARA, UI_TEXTO, VEH_FORZAR_PUERTA, VEH_PASA_CERCA

## Otros sonidos desconectados por T55 y T64

- JUG_ROPA_ROCE / JUG_ROPA_MOVER: ya descartados por T49 (MapaSonidos20nov.IdsDescartados).
- JUG_CORRER_RESPIRA: se quitó la llamada en ControladorAPie (respiración al correr).
- HUD_LOGRO como sonido de kill del Hitmarker: ya no suena al matar (T64); el de pegar (HUD_NOTIFICACION) se queda.

## No tocado

- Videos de intro y cinematica (llevan su propio audio).
- MUS_MENU_PRINCIPAL (música del menú, 21-nov): se queda por ser reciente.
