# Sonidos que faltan (19-nov)

Lista "Faltan" de `3d sonidos/LEEME_SONIDOS.md`, más lo que se encontró al integrar los sonidos nuevos (T-1470..T-1476).

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
