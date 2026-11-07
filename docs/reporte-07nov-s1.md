# Reporte sesión 1 — 7 de noviembre (carpeta mecha-s1)

Tony pidió parar después de T-720. Todo se integró en `tony/s1-07nov` y quedó en main con push (merge 113685b).
Build de prueba con main: Builds/Demo/CyberCity.exe de mecha-s1, generado el 7-nov a las 14:20.
Detalle de cada tarea en docs/tareas/hechas/T-xxx-*.md y capturas en docs/capturas/s1c-T-xxx-*.png.

## Hecho

| Tarea | Qué quedó |
| --- | --- |
| T-698 | El juego se llama CYBERCITY: menú, carga, créditos, Product Name (lo fija el build) y `CyberCity.exe`. Logo provisional de texto con glitch; si existe `UI/Resources/LogoCybercity.png`, se usa ese. |
| T-699 | Cámara en tercera persona como H10: personaje centrado y abajo, mirando a lo largo de la calle (altura 3.4, distancia 7, FOV 65, apuntando 45). Bajo techos la cámara baja. |
| T-700 / T-701 | HUD nuevo como H10_HUD.png con Rajdhani (OFL). Escudo, vida con número, estrellas y dinero arriba a la izquierda; minimapa cuadrado arriba a la derecha con el objetivo debajo; ayudas con teclas reales del Input System; muñequito con los implantes que se pone rojo donde hay daño. El minimapa dibuja calles a partir de los edificios de la escena. |
| T-702 | Pruebas viejas actualizadas: frase nueva del Doc leída de los datos, CYBERCITY en el título, mensajes del teléfono sin el guardado real. |
| T-703 | Un carro del tráfico chocado se detiene y pita; si el choque es fuerte, el conductor se baja enojado. Cada carro va a su velocidad (8 a 12 m/s). |
| T-704 | Los carros nacen y desaparecen fuera de la vista y a más de 45 m. Pegados al suelo (antes flotaban 0.45 m). |
| T-705 | Robar con conductor: lo sacas, cae, se levanta asustado y llama con Inter_Telefono si te quedas cerca. |
| T-706 | El aliado sube y baja por la puerta del copiloto con animación (en moto no sube). |
| T-707 | Patrulla con la física de los carros, sólida y robable. En patrulla robada la policía te ubica siempre (GPS). |
| T-708 | Las patrullas nacen fuera de la vista y llegan manejando por las calles (unos 12 s). |
| T-709 | Bajan 2 policías (C01), se cubren tras la patrulla en esquinas distintas, asoman y disparan. Dispararles sube la búsqueda; reaccionan y mueren con la caída de siempre. |
| T-710 | El dron dispara de verdad: trazo fino, daño real y sonido de bala rozando (WAV provisional). |
| T-711 | En la ciudad real, las balas de SKULLS, policía y dron no atraviesan casas, edificios ni carros. |
| T-712 | SKULLS en cobertura: se agachan, asoman, disparan y se esconden. |
| T-713 | Bug de los SKULLS: eran un grupo normal (de día salía solo una parte y reaparecían). Ahora en la misión salen los 7 a la vez, no reaparecen y la misión se completa. |
| T-714 | Refuerzos de SKULLS en carro: llegan desde fuera de la vista y se bajan 2. |
| T-715 | Los guardias de Nova hacen daño: avisan con luz roja 0.6 s y pegan 12 (barrida de 20 cada tercer golpe). |
| T-716 | Objetivo siempre visible (frase y metros) en el HUD, el mapa y el minimapa. Una prueba recorre todos los pasos de la historia. |
| T-717 | Historia del dinero: cinemática al salir de casa (Enter la salta), el asaltante huye en carro, persecución, llanta ponchada o choque, pelea y el dinero vuelve. |
| T-718 | Tutorial completo con 16 avisos y las teclas reales. Arreglado: el golpe con puños contaba como disparo, y se ofrecía apuntar sin arma. |
| T-719 | Las expendedoras leen su posición de la escena (Lugar_Casa y Lugar_Bar con un desplazamiento), no de coordenadas fijas. |
| T-720 | El Mercado ya vendía las tres armas; ahora vende balas de escopeta, fusil y sniper, con reserva por arma, "NO TE ALCANZA" y tope. Pantalla de tienda reacomodada para 13 filas. |

Pruebas al cierre: compilación OK en main después del merge. EditMode 2046/2047 (la roja es CarrosS2nPruebas.Importador_SinZip, de la sesión 2).
PlayMode se corrió por área y con filtros, no completo (memoria justa con dos Unity).

## Falta

- T-721: los dos nombres encimados junto al ícono del mecánico en el mapa, y dibujar en el mapa y el minimapa las calles de a pie nuevas
  de la sesión 2 (docs/cambios-mapa-07nov.md).
- T-722:
  - Ocultar el HUD en las cinemáticas: en la del asalto se ve debajo de las barras (s1c-T-717-cinematica-asaltante.png).
  - Cámara dentro de interiores chicos: en el cuarto de la casa se ve casi solo el techo (s1c-T-716-Doc.png).
  - Teclas reales en Opciones > Controles (PanelOpcionesInicio tiene texto fijo).
- Pruebas rojas sin arreglar:
  - DestinosMapaPruebas (minimapa) y ZonaHudPruebas (título de zona): parecen del HUD de T-701.
  - ColisionCochesT580Pruebas con Carro_V08.
  - HeadshotPandilleroT677Pruebas.
  - SonidoArmasT593 (espera variación de tono que T-676 quitó).
  - PrologoCiudadPruebas: obsoleta, busca DirectorPrologo, que está apagado a propósito desde T-400. Hay que rehacerla o retirarla.
  - RecorridoRonda2Pruebas.MercadoTianguisEscopeta y RecorridoAPiePruebas (Bar).
- Pruebas que dependen del orden: TelefonoMensajesPruebas, HudH10Pruebas.Arma_BalasGrandes y MarcadorObjetivoPruebas después de
  HistoriaPandillasPruebas. Pasan solas.
- No se corrió RecorridoDemoPruebas completo, ni Builds/Juego.
- Los rectángulos de zona (Progresion/Datos/Territorio/DatosZona_*) son del mapa viejo; hay que corregirlos cuando la ciudad nueva quede.

## Para revisar o decidir

- Las pruebas en batch sobrescribieron el guardado automático real (`%LOCALAPPDATA%Low\DefaultCompany\CYBERFIX\partida_auto.json`).
  En su lugar quedó una copia de partida_1.json (paso Tianguis). Si al continuar quedas en un punto raro, borra ese archivo.
  Las pruebas que guardan ya usan un guardado temporal. Con el nombre nuevo, el build guarda en otra carpeta (CyberCity).
- Logo: hay 6 opciones en imageassets/logo (LG01 a LG06). Falta elegir una y ponerla como `UI/Resources/LogoCybercity.png`.
- Policías: solo disparan al jugador a pie, se bajan sin animación y la patrulla no lleva conductor visible.
- El aliado va oculto dentro del carro.
- Para la sesión 2: marcadores `Marcador_Expendedora_Casa`, `_Tianguis` y `_Nova` en la ciudad (anotado en decisiones-pendientes-tony.md).
- Restos de corridas batch sin commit (prefabs JugadorAPie, Carro_V09/V10, materiales, capturas viejas): guardados en un stash de
  mecha-s1 ("restos de corridas batch 07-nov"), no borrados. Siguen los stash viejos de T-684 y T-706, y ProjectSettings/SceneTemplateSettings.json
  sin versionar.
