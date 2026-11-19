# T-1476 Sonidos de la policía

Rama: tony/T-1476-policia-sonidos. Sin compilar ni correr pruebas (worktree sin Library): lo compila el orquestador.

## Evento -> ID

| Evento | ID | Dónde |
| --- | --- | --- |
| Una patrulla llega y baja a sus policías; llegan los refuerzos de 5 estrellas (enfriamiento 15 s) | POL_RADIO_AVISO (4 variantes) | RadioPolicia.Sonar, ConfigPersecucion.IdSonidoRadio |
| Sube el nivel de búsqueda (la central manda unidades; enfriamiento propio de 15 s) | POL_RADIO_DESPACHO (2 variantes) | RadioPolicia.SonarDespacho, GestorPersecucion.Reconciliar, ConfigPersecucion.IdSonidoDespacho (campo nuevo) |
| Patrulla en persecución (al salir con sirena o al dejar la ronda) | POL_SIRENA_LOOP, bucle que sigue a la patrulla; se detiene al retirarse o al desactivarse/destruirse | SonidosPolicia.IniciarSirena/DetenerSirena, PatrullaMundo |
| Misma patrulla empieza a perseguir | POL_SIRENA_CORTA (bloop 3D) | SonidosPolicia.IniciarSirena |
| Llega la respuesta de Nova (más de 5 estrellas) | POL_NOVA_AVISO (2D) | ConfigRespuestaNova.SonidoLlegada |

Se reemplazó CAL_PATRULLA_SIRENA por POL_SIRENA_LOOP (T-1471 ya mapea el viejo). CAL_PATRULLA_MEGAFONO queda igual.

## Métodos de editor (en este orden, tras compilar)
1. MechaCyberpunk.Trafico.Editor.AplicarSonidosPoliciaT1476.Aplicar
2. MechaCyberpunk.IA.Editor.AplicarSonidoNovaT1476.Aplicar

(Los valores por defecto del código ya son los nuevos; los métodos ajustan los .asset existentes.)

## Pruebas
- Tests/EditMode/Trafico: SonidosPoliciaT1476Pruebas (radio sin spam, despacho, sirena arranca y se detiene).
- Tests/EditMode/IA: NovaSonidoT1476Pruebas.
- EstrellasT1322Pruebas: la prueba de config ya no espera la radio vacía (espera POL_RADIO_AVISO).
- NovaT1323ReglasPruebas exige que SonidoLlegada esté encendido en ReglaMezclaSonidos: pasa solo cuando T-1471 encienda POL_NOVA_AVISO.

## Falta
- Sirena de ambulancia, altavoz/megáfono nuevo, rotores de dron, reflector, estática de radio.
- El máximo de ~3 sirenas a la vez lo pone la mezcla de T-1471.
- Por revisar a oído: POL_NOVA_AVISO al llegar Nova; que POL_SIRENA_CORTA más el bucle no se pisen; la frecuencia del despacho.
