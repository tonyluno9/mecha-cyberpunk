# T-voces-personajes-23nov: meter al juego las voces grabadas

## Qué es
El usuario grabó las voces de los personajes. Ya están cortadas, limpias, con efecto y con nombre en
`3d sonidos/19_Voces/`. Hay que copiarlas a `Assets/Arte/Audio/19_Voces/` y conectarlas para que suenen en el juego en su momento.

## Paso 0 (obligatorio)
Cerrar Unity y Unity Hub, confirmar que no queda ningún Unity.exe, borrar `Temp/UnityLockfile` y repetir si el candado vuelve a aparecer.

## Qué copiar
- Solo los .wav de cada carpeta. **No copiar** las subcarpetas `_sin_efecto` (son para comparar). `Policia/_megafono` es opcional (ver abajo).
- Cada asset con su .meta en el mismo commit. Importar como **Mono**; las frases largas (Jefe_SKULLS, Jefe_Nova, Doc) en **Streaming** o **Compressed in memory**.

| Carpeta | Archivos | Cuándo suena |
|---|---|---|
| Jugador | JUG_ESFUERZO (4), JUG_SALTO (3), JUG_DOLOR_LEVE (4), JUG_DOLOR_FUERTE (3), JUG_MUERTE (2), JUG_QUEMADO (2), JUG_TOS (3), JUG_RESPIRACION_CANSADO (loop) | Golpear o empujar; saltar; daño bajo; daño alto; morir; fuego; humo o explosión; correr mucho (loop con fade in/out) |
| SKULLS | SKU_AMBIENTE, SKU_PROVOCAR, SKU_ULTIMO_AVISO, SKU_GRITO_ALERTA, SKU_INSULTO, SKU_COBERTURA, SKU_RECARGANDO, SKU_LO_PERDIMOS, SKU_HUIR, SKU_GANAMOS, SKU_DOLOR, SKU_MUERTE, SKU_RISA_BURLA, SKU_QUEMADO | Orden: ambiente → provocar → último aviso → alerta → combate (insulto, cobertura, recargando, dolor) → lo perdimos / huir / ganamos. Detalle en `19_Voces/SKULLS/LEEME.md` |
| Maton | SKU_MATON_PROVOCAR, _ATAQUE, _RISA, _DOLOR, _MUERTE | El pandillero grandote del brazo robótico; ATAQUE al pegar con el brazo |
| Jefe_SKULLS | SKU_JEFE_MALETIN, _GUARDIAS, _CASTIGO, _ENFRENTAMIENTO, _DERROTA, _DOLOR | Escenas de la historia del maletín (ver historia-cybercity-v1 en el proyecto). Si la escena aún no existe, dejarlas listas en el catálogo de sonidos |
| Policia | POL_VOZ_ALTO, _RENDIRSE, _REFUERZOS, _OFICIAL_CAIDO, _ALTAVOZ, _LO_PERDIMOS, _DOLOR, _MUERTE | ALTAVOZ (con efecto megáfono) desde la patrulla en persecución. `_megafono/POL_MEGAFONO_*` son "Alto", "Rendirse" y "Lo perdimos" con megáfono, también para la patrulla (opcional) |
| Nova | NOV_VOZ_ALERTA, _ORDEN, _CUBRIRSE, _LO_PERDIMOS, _DOLOR, _MUERTE | Agentes con armadura (voz robótica con casco). CUBRIRSE cuando activan el escudo |
| Jefe_Nova | NOV_JEFE_FRASE, _REVELACION, _ATAQUE, _MUERTE | Jefe final; REVELACION solo en la escena final |
| Doc | DOC_VOZ_SALUDO, _SIENTATE, _EXPLICAR, _REVIVIR, _SUERO, _DESPEDIDA | Clínica del Doc: al entrar, al instalar implante, al revivir tras morir, al salir |

## Reglas
- Cada grupo con varias tomas (_01, _02…) se elige al azar sin repetir la última.
- Que una voz no se pise con otra del mismo personaje; una sola voz por NPC a la vez y un enfriamiento de unos segundos entre frases de ambiente o insulto.
- Volumen de voces por encima de los efectos; con distancia 3D (las del jugador y del Doc en 2D).
- Donde el juego ya usaba un sonido viejo o de librería para estos eventos (dolor, muerte de NPC, POL_RADIO_DESPACHO, etc.), reemplazarlo por el nuevo. Las voces de despachador (POL_RADIO_DESPACHO_*) se cambian por POL_RADIO_AVISO_* ("Mexican Police").
- Si falta el evento en el código (p. ej. no hay escena del maletín), no inventar la mecánica: dejar el ID en el catálogo y anotarlo en la revisión.

## Cómo comprobarlo
- Pruebas solo las necesarias: que los IDs nuevos existan en el catálogo y que cada evento pida el ID correcto.
- Lo importante es que el usuario pruebe jugando: dejar en la revisión una lista corta de qué hacer para oír cada grupo (pelear con SKULLS, que te persiga la policía, ir con el Doc, morir, etc.).

## Pendiente de grabar (no es parte de esta tarea)
Tino, Canepa, aliado, mesera robot, peatones y locutor de Nova Radio.
