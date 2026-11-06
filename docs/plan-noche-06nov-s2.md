# Noche 6-nov, sesión 2 (mundo, interiores, menús de taller y clínica)

Fuente: docs/playtest-06nov.md (SESIÓN 2) más lo que Tony agregó: pelea del tianguis < 200 s, pruebas de distancias de la ciudad
(se arregla el mundo, no las pruebas) y consultorio del Doc menos verde, como su referencia.

## Reglas para cada tarea
- Repo: `C:\Users\Angel Perez\Desktop\videojuego\mecha-cyberpunk` (carpeta principal, de la sesión 2). La sesión 1 trabaja en `../mecha-s1`:
  no toques esa carpeta. Su Unity es otro proyecto (no choca con el candado de este), pero `herramientas/unity-vivo.sh` se niega a
  arrancar si hay CUALQUIER editor abierto: en ese caso usa Unity en batch con `-executeMethod` (y `-quit`) o pruebas de PlayMode.
- Rama: sale de `tony/s2-mundo`, se llama `tony/T-8xx-nombre`, commit ahí (con su .meta). El orquestador hace los merges a s2-mundo y a main.
- No se tocan (son de la sesión 1): scripts del jugador (`Personaje/Jugador`, `Personaje/Movimiento`, `Personaje/Combate` existentes,
  `Personaje/Interaccion`), `JugadorAPie.prefab`, `MechaJugador`, armas (`PistolaAPie`, `ArsenalJugador`, `SensacionDisparo`),
  `Vehiculos/`, scripts de `Trafico/` (policía y tráfico), SKULLS (`IA/`, `DirectorPandillas`). Archivos NUEVOS que solo escuchan
  eventos públicos de esos scripts sí se pueden agregar; también cambiar assets de datos (materiales, DatosArma nuevos).
- Nunca `CreadorTodo`. Si se regenera Barrio.prefab: volver a correr `ColocarT250.Colocar`, `ColocarNoche.Colocar` (T-419) y las
  colocaciones T-50x. Lo nuevo va en código de editor idempotente en `Assets/Arte/Editor/ColocarS2n*.cs` (menú Mecha Cyberpunk/S2n),
  que rehace su propia raíz en `Ciudad_Prologo.unity`; nada de YAML a mano.
- Convenciones: `.claude/rules/unity-csharp.md`. Pruebas en `Tests/EditMode/<Área>/` o `Tests/PlayMode/<Área>/`.
- Verificación: `bash herramientas/verificar.sh compilar`, `editmode` y PlayMode de las áreas tocadas
  (`AREAS="Mundo Demo" bash herramientas/verificar.sh playmode-por-area`). Lo que toque la ruta de la demo: también `Demo`.
- Capturas lado a lado (referencia | juego) en `docs/capturas/s2n-<zona>.png` con `ColocarS2.Foto` + `ColocarS2.LadoALado`.
  Ábrelas y revísalas tú: si algo se ve encimado, flotando, fuera de escala o hecho con primitivos, arréglalo y repite.
- Si `ProjectSettings/ProjectSettings.asset` solo cambió en `SENTIS_ANALYTICS_ENABLED`, `git restore`; borra
  `ProjectSettings/SceneTemplateSettings.json` si aparece.
- Assets que llegan después (V09-V11 .zip): el importador `ImportarS2n` (menú Mecha Cyberpunk/S2n/Meter assets nuevos) los mete
  solo cuando aparezca el archivo en `imageassets/`. Ver docs/meter-assets-06nov.md.

## Tareas (en orden)
| ID | Qué |
| --- | --- |
| T-801 | Pruebas de distancias de la ciudad que fallan (arreglar el mundo) |
| T-802 | Pelea del tianguis en menos de 200 s |
| T-803 | Limpieza: bolsa de basura del cuarto, mochilas derechas, papel picado en todos lados, cruces/logos de las casas |
| T-804 | Puertas reales con E y bisagra; casa y cuarto sin teletransporte; taller del mecánico dentro de su edificio, visible desde la casa y en el mapa |
| T-805 | Calle infinita junto a la casa, callejones sin límite y partes rotas de la calle |
| T-806 | Cielo de día y de noche |
| T-807 | Líneas de la calle (M01/M02, o hechas en Unity) e importador |
| T-808 | Huecos de bala (D01 concreto, D02 metal, o hechos en Unity) |
| T-809 | Puentes y pasarelas subibles; el puente de Nova conectado o quitado |
| T-810 | Variedad de carros del tráfico (V09-V11 cuando lleguen) |
| T-811 | CIC de Tino |
| T-812 | I+D+I del Doc: Doc más grande, barril, frascos, menos verde |
| T-813 | Clínica: silla del Doc, sentarse y secuencia de curación |
| T-814 | Menú de mejora de armas con holograma (W01 3D, vista explotada) |
| T-815 | Menú de implantes con holograma del cuerpo |
| T-816 | Hologramas H01-H04 en la lista de armas y el ícono del HUD |
| T-817 | Barrido general (encimado, flotante, escala, primitivos) y capturas s2n de cada zona |
