# T-1470: importar y recortar los sonidos nuevos (19-nov)

Rama: tony/T-1470-importar-sonidos. Sin .meta de los WAV: los genera el orquestador al importar en Unity
(luego correr `MechaCyberpunk.Arte.Editor.ImportarSonidos19nov.Aplicar` para aplicar los ajustes de importación).

## Qué se hizo
- `herramientas/sonidos/importar_sonidos_19nov.py <fuente> <Assets/Arte/Audio>`: copia 01_UI..10_Policia (ignora 99_No_es_sonido),
  convierte a PCM 16 bits (misma frecuencia) y recorta. Log completo con segundos de corte: `herramientas/sonidos/importar-19nov.log`.
- Convertidos a PCM16: TIE_EQUIPAR, ARM_GRANADA_REBOTE, ARM_W07_CARGA (formato extensible) y TIE_COMPRAR, ARM_W01_DISPARO_01,
  IMP_BALA_CARNE_01, PAS_MADERA_02, PAS_MOJADO_01, MEL_GOLPE_AIRE, CON_VENDA, VEH_ELECTRICO_ARRANQUE (cabecera con data truncada).
  El resto se copia sin tocar (int32/int16 a su frecuencia original; Unity los baja con OptimizeSampleRate).
- `Assets/Arte/Editor/ImportarSonidos19nov.cs`: AssetPostprocessor (solo en las 10 carpetas) y `Aplicar()`; `EsLoop(nombre)` pública.
- `.gitignore`: `3d sonidos/`.

## Cortes (segundos del original)
| Salida | Origen | Corte |
| --- | --- | --- |
| VEH_NORMAL_ARRANQUE_01 / RALENTI_01 / APAGAR_01 | ..._APAGAR_01 (22.3 s) | 0-3.00 / 3.50-8.50 / 12.70-21.15 |
| VEH_NORMAL_*_02 | ..._APAGAR_02 (59.1 s) | 0-2.45 / 6.10-11.10 / 57.35-59.05 |
| VEH_NORMAL_*_03 | ..._APAGAR_03 (71.0 s) | 0-1.20 / 46.10-51.10 / 70.10-70.85 |
| MOTO_*_01 | MOTO_ARRANQUE_01 (77.6 s) | 0-1.00 / 67.70-72.70 / 75.05-76.80 |
| MOTO_*_02 | MOTO_ARRANQUE_02 (70.1 s) | 0-1.00 / 51.40-56.40 / 69.25-70.05 |
| MOTO_*_03 | MOTO_ARRANQUE_RALENTI_APAGAR (63.7 s) | 0-1.00 / 3.90-8.90 / 62.05-63.65 |
| VEH_PESADO_RALENTI_01 / APAGAR_01 | VEH_PESADO_RALENTI (72.7 s) | 62.70-67.70 / 69.20-72.71 (VEH_PESADO_ARRANQUE queda igual) |
| VEH_ALARMA | 50.3 s | loop 8.75 s desde 13.70 s (25 periodos de 0.35 s) |
| VEH_LLANTAS_GRAVA | 47.4 s | loop 9.00 s desde 20.00 s |
| PAS_TIERRA_01..08 | secuencia 7.9 s (15 pisadas) | pisadas en 0.13, 2.04, 2.59, 4.12, 4.66, 5.15, 6.68, 7.17 s |
| PAS_GRAVA_01..08 | secuencia 8.9 s (8 pisadas) | pisadas en 0.07, 0.94, 2.09, 4.21, 4.92, 5.55, 6.21, 7.58 s |

Loops: crossfade de 120 ms (potencia constante), inicio y fin en cruce por cero. Salto entre el último y el primer sample
de 0.0004 a 0.0065 (escala completa 1.0), del orden de la diferencia normal entre samples. Pasos: -50 ms a +350 ms, fades 5 ms / 20 ms.
Ganancia de recortes: pico igual al del original, con tope de +6 dB (x1.00 a x1.54).

## Por revisar a oído
- Arranques de moto 01/02/03 y NORMAL_03: la grabación ya suena a ralentí casi desde el segundo 0; el "arranque" es el primer 1.0-1.2 s.
  MOTO_ARRANQUE_03 no trae arranque propio (empieza ya estable). Si suena raro, no usarlo.
- VEH_NORMAL_APAGAR_01 dura 8.5 s (cola larga). APAGAR_02 y 03 son cortos (1.7 y 0.75 s).
- El principio del apagado se busca por caída de nivel; puede entrar unos 0.1-0.3 s tarde.
- Pasos de tierra: se eligieron 8 de 15 por limpieza (relación pico/ruido).

## Importador (ajustes por archivo)
Cortos (< 2 s, o prefijos UI_ HUD_ TIE_ IMP_ MEL_ PAS_, o ARM_*_DISPARO): DecompressOnLoad ADPCM con precarga.
Medianos y bucles de 5 s: CompressedInMemory Vorbis 0.7. Más de 8 s: Streaming con carga en segundo plano.
Mono forzado en 02, 03, 04, 05, 09, 10. Bucles (EsLoop): *_RALENTI*, FUE_LOOP_CHICO, POL_SIRENA_LOOP, VEH_ALARMA,
VEH_LLANTAS_GRAVA, VEH_ELECTRICO_MOVER. Los bucles de más de 8 s quedan en Streaming (el AudioSource pone el loop).

## Archivos resultantes
| Carpeta | Nombre | Dur. s | Origen | Bucle | Import |
| --- | --- | --- | --- | --- | --- |
| 01_UI | HUD_LOGRO | 4.36 | copia |  | DecompressOnLoad ADPCM |
| 01_UI | HUD_NOTIFICACION | 1.61 | copia |  | DecompressOnLoad ADPCM |
| 01_UI | HUD_VIDA_BAJA | 3.16 | copia |  | DecompressOnLoad ADPCM |
| 01_UI | TIE_COMPRAR | 0.37 | convertido |  | DecompressOnLoad ADPCM |
| 01_UI | TIE_DESEQUIPAR | 1.37 | copia |  | DecompressOnLoad ADPCM |
| 01_UI | TIE_EQUIPAR | 1.63 | convertido |  | DecompressOnLoad ADPCM |
| 01_UI | UI_ATRAS | 0.85 | copia |  | DecompressOnLoad ADPCM |
| 01_UI | UI_ERROR | 0.25 | copia |  | DecompressOnLoad ADPCM |
| 01_UI | UI_LOGO_INTRO | 3.50 | copia |  | DecompressOnLoad ADPCM |
| 01_UI | UI_MENU_ABRIR | 4.00 | copia |  | DecompressOnLoad ADPCM |
| 01_UI | UI_MENU_CERRAR | 1.50 | copia |  | DecompressOnLoad ADPCM |
| 01_UI | UI_MOVER | 0.38 | copia |  | DecompressOnLoad ADPCM |
| 01_UI | UI_PESTANA | 0.50 | copia |  | DecompressOnLoad ADPCM |
| 01_UI | UI_SELECCIONAR | 2.29 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_COLA_CALLE | 4.40 | copia |  | CompressedInMemory Vorbis 0.7 |
| 02_Armas | ARM_GRANADA_REBOTE | 1.19 | convertido |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_GRANADA_SEGURO | 0.37 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_NOVA_CARGA_DISPARO | 1.02 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_NOVA_DISPARO_01 | 1.21 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_W01_DISPARO_01 | 1.61 | convertido |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_W02_BOMBA | 0.71 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_W02_DISPARO_01 | 1.88 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_W03_DISPARO_01 | 0.80 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_W03_RECARGA_CARGADOR | 0.38 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_W04_CERROJO | 0.63 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_W04_DISPARO_01 | 1.50 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_W06_DISPARO_01 | 1.46 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_W06_SACAR | 0.33 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_W07_CARGA | 3.40 | convertido |  | CompressedInMemory Vorbis 0.7 |
| 02_Armas | ARM_W07_DISPARO_01 | 1.42 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | ARM_W07_RAYO | 3.64 | copia |  | CompressedInMemory Vorbis 0.7 |
| 02_Armas | IMP_LASER_DISPARO | 0.40 | copia |  | DecompressOnLoad ADPCM |
| 02_Armas | IMP_MISILES_FIJAR | 1.00 | copia |  | DecompressOnLoad ADPCM |
| 03_Impactos | IMP_BALA_CABEZA_01 | 0.99 | copia |  | DecompressOnLoad ADPCM |
| 03_Impactos | IMP_BALA_CARNE_01 | 0.23 | convertido |  | DecompressOnLoad ADPCM |
| 03_Impactos | IMP_BALA_CONCRETO_01 | 0.58 | copia |  | DecompressOnLoad ADPCM |
| 03_Impactos | IMP_BALA_CRISTAL_01 | 0.66 | copia |  | DecompressOnLoad ADPCM |
| 03_Impactos | IMP_BALA_METAL_01 | 1.50 | copia |  | DecompressOnLoad ADPCM |
| 03_Impactos | IMP_BALA_ROBOT_01 | 1.03 | copia |  | DecompressOnLoad ADPCM |
| 03_Impactos | IMP_BALA_ZUMBIDO_01 | 0.22 | copia |  | DecompressOnLoad ADPCM |
| 04_Explosiones | EXP_GRANADA_01 | 1.55 | copia |  | DecompressOnLoad ADPCM |
| 04_Explosiones | EXP_ONDA | 4.54 | copia |  | CompressedInMemory Vorbis 0.7 |
| 04_Explosiones | FUE_LOOP_CHICO | 11.25 | copia | loop | Streaming |
| 05_Pasos | PAS_CONCRETO_01 | 0.83 | copia |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_GRAVA_01 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_GRAVA_02 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_GRAVA_03 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_GRAVA_04 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_GRAVA_05 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_GRAVA_06 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_GRAVA_07 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_GRAVA_08 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_MADERA_01 | 0.37 | copia |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_MADERA_02 | 0.39 | convertido |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_METAL_01 | 0.61 | copia |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_MOJADO_01 | 0.74 | convertido |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_TIERRA_01 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_TIERRA_02 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_TIERRA_03 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_TIERRA_04 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_TIERRA_05 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_TIERRA_06 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_TIERRA_07 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 05_Pasos | PAS_TIERRA_08 | 0.40 | recorte |  | DecompressOnLoad ADPCM |
| 06_Jugador | JUG_ATERRIZAJE | 0.50 | copia |  | DecompressOnLoad ADPCM |
| 06_Jugador | JUG_RODADA | 1.09 | copia |  | DecompressOnLoad ADPCM |
| 06_Jugador | JUG_TREPAR | 1.26 | copia |  | DecompressOnLoad ADPCM |
| 06_Jugador | MEL_GOLPE_01 | 0.73 | copia |  | DecompressOnLoad ADPCM |
| 06_Jugador | MEL_GOLPE_02 | 0.72 | copia |  | DecompressOnLoad ADPCM |
| 06_Jugador | MEL_GOLPE_AIRE | 0.32 | convertido |  | DecompressOnLoad ADPCM |
| 06_Jugador | MEL_GOLPE_CARNE | 0.36 | copia |  | DecompressOnLoad ADPCM |
| 07_Interaccion | INT_HACKEO_EXITO | 0.54 | copia |  | DecompressOnLoad ADPCM |
| 07_Interaccion | INT_HACKEO_FALLO | 0.20 | copia |  | DecompressOnLoad ADPCM |
| 07_Interaccion | INT_INTERRUPTOR_LUZ | 1.23 | copia |  | DecompressOnLoad ADPCM |
| 07_Interaccion | INT_MALETIN_CERRAR | 0.99 | copia |  | DecompressOnLoad ADPCM |
| 07_Interaccion | INT_PUERTA_CRISTAL | 3.99 | copia |  | CompressedInMemory Vorbis 0.7 |
| 07_Interaccion | INT_PUERTA_MADERA | 1.02 | copia |  | DecompressOnLoad ADPCM |
| 07_Interaccion | INT_PUERTA_METAL | 0.96 | copia |  | DecompressOnLoad ADPCM |
| 07_Interaccion | INT_PUERTA_REJA | 5.22 | copia |  | CompressedInMemory Vorbis 0.7 |
| 07_Interaccion | INT_RECOGER_OBJETO | 0.47 | copia |  | DecompressOnLoad ADPCM |
| 07_Interaccion | INT_TECLEO_HACKEO | 5.13 | copia |  | CompressedInMemory Vorbis 0.7 |
| 08_Consumibles | CON_BEBER | 3.24 | copia |  | CompressedInMemory Vorbis 0.7 |
| 08_Consumibles | CON_CURACION | 3.00 | copia |  | CompressedInMemory Vorbis 0.7 |
| 08_Consumibles | CON_LATA_ABRIR | 6.46 | copia |  | CompressedInMemory Vorbis 0.7 |
| 08_Consumibles | CON_PAPITAS_COMER | 1.07 | copia |  | DecompressOnLoad ADPCM |
| 08_Consumibles | CON_VENDA | 1.81 | convertido |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | MOTO_APAGAR_01 | 1.75 | recorte |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | MOTO_APAGAR_02 | 0.80 | recorte |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | MOTO_APAGAR_03 | 1.60 | recorte |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | MOTO_ARRANQUE_01 | 1.00 | recorte |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | MOTO_ARRANQUE_02 | 1.00 | recorte |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | MOTO_ARRANQUE_03 | 1.00 | recorte |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | MOTO_RALENTI_01 | 5.00 | recorte | loop | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | MOTO_RALENTI_02 | 5.00 | recorte | loop | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | MOTO_RALENTI_03 | 5.00 | recorte | loop | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_ACELERON_01 | 3.11 | copia |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_ACELERON_02 | 3.35 | copia |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_ALARMA | 8.75 | recorte | loop | Streaming |
| 09_Vehiculos | VEH_CHOQUE_01 | 1.47 | copia |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | VEH_CHOQUE_02 | 1.65 | copia |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | VEH_CHOQUE_FUERTE_01 | 2.12 | copia |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_CHOQUE_FUERTE_02 | 3.39 | copia |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_CLAXON_CAMION | 3.01 | copia |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_CLAXON_NORMAL_01 | 1.59 | copia |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | VEH_CLAXON_NORMAL_02 | 0.60 | copia |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | VEH_CRISTAL_ROMPER | 1.16 | copia |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | VEH_ELECTRICO_ACELERAR_01 | 10.58 | copia |  | Streaming |
| 09_Vehiculos | VEH_ELECTRICO_ACELERAR_02 | 6.86 | copia |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_ELECTRICO_ARRANQUE | 2.56 | convertido |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_ELECTRICO_MOVER | 3.22 | copia | loop | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_ELECTRICO_PASAR | 3.26 | copia |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_FRENO_FUERTE | 1.38 | copia |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | VEH_LLANTAS_GRAVA | 9.00 | recorte | loop | Streaming |
| 09_Vehiculos | VEH_NORMAL_APAGAR_01 | 8.45 | recorte |  | Streaming |
| 09_Vehiculos | VEH_NORMAL_APAGAR_02 | 1.70 | recorte |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | VEH_NORMAL_APAGAR_03 | 0.75 | recorte |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | VEH_NORMAL_ARRANQUE_01 | 3.00 | recorte |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_NORMAL_ARRANQUE_02 | 2.45 | recorte |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_NORMAL_ARRANQUE_03 | 1.20 | recorte |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | VEH_NORMAL_RALENTI_01 | 5.00 | recorte | loop | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_NORMAL_RALENTI_02 | 5.00 | recorte | loop | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_NORMAL_RALENTI_03 | 5.00 | recorte | loop | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_PESADO_APAGAR_01 | 3.51 | recorte |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_PESADO_ARRANQUE | 2.79 | copia |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_PESADO_RALENTI_01 | 5.00 | recorte | loop | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_PUENTE | 2.19 | copia |  | CompressedInMemory Vorbis 0.7 |
| 09_Vehiculos | VEH_PUERTA_ABRIR | 0.90 | copia |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | VEH_PUERTA_CERRAR | 0.55 | copia |  | DecompressOnLoad ADPCM |
| 09_Vehiculos | VEH_TOPE | 2.09 | copia |  | CompressedInMemory Vorbis 0.7 |
| 10_Policia | POL_NOVA_AVISO | 2.13 | copia |  | CompressedInMemory Vorbis 0.7 |
| 10_Policia | POL_RADIO_AVISO_01 | 4.10 | copia |  | CompressedInMemory Vorbis 0.7 |
| 10_Policia | POL_RADIO_AVISO_02 | 5.92 | copia |  | CompressedInMemory Vorbis 0.7 |
| 10_Policia | POL_RADIO_AVISO_03 | 6.84 | copia |  | CompressedInMemory Vorbis 0.7 |
| 10_Policia | POL_RADIO_AVISO_04 | 12.32 | copia |  | Streaming |
| 10_Policia | POL_RADIO_DESPACHO_01 | 3.98 | copia |  | CompressedInMemory Vorbis 0.7 |
| 10_Policia | POL_RADIO_DESPACHO_02 | 6.46 | copia |  | CompressedInMemory Vorbis 0.7 |
| 10_Policia | POL_SIRENA_CORTA | 1.83 | copia |  | DecompressOnLoad ADPCM |
| 10_Policia | POL_SIRENA_LOOP | 11.05 | copia | loop | Streaming |
