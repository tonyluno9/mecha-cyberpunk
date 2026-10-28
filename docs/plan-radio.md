# Plan de Radio (3 estaciones)

Estado: **borrador de planificación** (2026-10-28, rama `alan/plan-radio`). No hay código ni assets de Radio todavía.
Fuentes: hoja-de-ruta.md (T-F7, bloque 7 "radio", que Tony tiene en su lista desde T-198), docs/diseno/estetica-hud.md,
D-052 (la canción del HUD espera a la radio), `Assets/_Proyecto/Radio/` (solo el asmdef, dueño Tony) y `Assets/Arte/Audio/`
(4 ambientes .wav de la ciudad, de Alan; no son estaciones).

## Qué pide el diseño (sin inventar más)
- 3 estaciones que se cambian con una tecla: cumbia electrónica, electrónica oscura, hip hop o rap callejero.
- La radio son los audífonos del protagonista y **siempre suena, también en las peleas**.
- Canciones = covers del equipo, solo uso escolar, no se suben a internet; al publicar se cambian por temas originales.
- Los audífonos y el audio de la pelea deben oírse juntos sin taparse (propuesta, sin valores).
- El HUD a pie ya tiene `HudAPie.MostrarCancion(string)`; sin radio muestra "RADIO · sin señal".

## Reparto
| Quién | Qué | Dónde |
| --- | --- | --- |
| Tony | Lógica y datos: `Radio/` (reproductor, estaciones como ScriptableObject, cambio por tecla, lista de canciones, volumen y mezcla con la pelea), entrada, aviso al HUD, guardado de la última estación, pruebas | `Assets/_Proyecto/Radio/`, `Tests/*/Radio/`, la parte de UI/HUD y Progresion solo si su tarea lo pide |
| Alan | **Solo audio y ambiente del mundo**: las canciones/covers como assets finales (`Assets/Arte/Audio/Radio/`, Git LFS) y, si Tony lo pide por contrato, fuentes de radio diegéticas en locales (bar, casino, carro) | `Assets/Arte/Audio/Radio/`, `Mundo/`, `Vehiculos/`, `Casino/` |
| Nadie en este plan | Core, ProjectSettings, escenas compartidas | — |

Alan no toca `Radio/` ni lo que sea de Tony; Tony no toca `Arte/` salvo que la tarea lo diga. La radio nunca referencia
`Mundo/`, `Vehiculos/` ni `Casino/` (dueños distintos): se hablan por Core.

## Contratos mínimos (tarea de Core, la hace Tony)
1. `Core/Radio/IRadio` (solo lo que otros necesitan): `EstacionActual`, `CancionActual`, `Silenciada`,
   `event CancionCambiada(string titulo)`, `SiguienteEstacion()`.
2. `Registro<IRadio>` para encontrarla sin referenciar el área (patrón ya usado, `Core/Servicios/Registro.cs`).
3. Para locales de Alan (opcional, fase tardía): `Core/Radio/IFuenteRadioLocal` (`Posicion`, `Radio`, `Volumen`) que Alan implementa en
   bar/casino/carro; la radio baja los audífonos al entrar en una. **Decisión pendiente de Alan** (ver abajo); si no, se omite.
4. Evento de pelea: la radio escucha `Core/Pelea/EventosPelea` (ya existe) para ajustar la mezcla; no hace falta contrato nuevo.

## Orden de implementación
| Paso | Tarea (IDs por confirmar con Tony; él numera el bloque 7 desde T-198) | Dueño | Depende de | Resultado |
| --- | --- | --- | --- | --- |
| 0 | **Decidir lista de canciones** (títulos, 3 estaciones, cuántas por estación, origen del cover) | Alan + Tony | — | Tabla en este archivo (ver "Decisiones") |
| 1 | Contrato `IRadio` en Core | Tony | — | Compila; sin comportamiento |
| 2 | Datos y reproductor: `EstacionRadio` (SO con lista de `AudioClip`), reproductor con cola/aleatorio, 3 estaciones con clips de relleno (tonos) | Tony | 1 | EditMode: avanzar canción, ciclo de estaciones; PlayMode: suena y cambia con tecla simulada |
| 3 | Aviso al HUD (`MostrarCancion`) y cartel de estación | Tony | 2, T-056 | PlayMode: al cambiar sale el título en el HUD |
| 4 | Mezcla con la pelea (bajar un poco al empezar, nunca callar) y volumen | Tony | 2, T-F7 peleas | PlayMode: durante una pelea sigue sonando y los SFX se distinguen (medir volumen relativo, captura de la mezcla) |
| 5 | **Assets de canciones/covers**: importar, loop points, compresión, `.meta`, LFS | Alan | 0 | `Assets/Arte/Audio/Radio/<estación>/*.ogg|wav`; prueba de que cada clip carga y dura > 0 |
| 6 | Conectar clips reales a las 3 estaciones (solo datos) | Tony | 2, 5 | Reemplaza los de relleno; PlayMode de las 3 estaciones |
| 7 | Guardado de la última estación | Tony | 2, guardado | Guardar y cargar conserva la estación |
| 8 | (Opcional) Radios locales diegéticas en bar/casino/carro | Alan | `IFuenteRadioLocal`, Mundo/Casino/Vehiculos | La radio baja al entrar y vuelve al salir |

Se puede empezar 0 y 5 en paralelo a 1-4 (Alan no necesita código de Tony; Tony usa clips de relleno hasta que lleguen los reales).

## Pruebas (resumen)
- EditMode (Tony): ciclo 1→2→3→1, canción siguiente sin repetir la anterior, estación persistida.
- PlayMode con entrada simulada (Tony): tecla de cambio de estación, `MostrarCancion` llamado, la radio sigue activa durante una pelea y en carro.
- Datos (Alan): cada clip importa sin error, formato y duración documentados; ningún clip fuera de `Assets/Arte/Audio/Radio/`.
- Evidencia: valor medido o captura de la mezcla; "suena bien" a oído queda para revisión humana (H7).

## Decisiones creativas que son de Alan (no inventadas aquí)
1. **Qué canciones/covers** van en cada estación, cuántas, y quién los hace (Alan, el equipo, o relleno hasta que haya). Los géneros vienen del diseño; los títulos no.
2. ¿Hay **radios en el mundo** (bar, casino, carro) o solo los audífonos? Define si existe el paso 8.
3. ¿Los anuncios/locutores o frases entre canciones? Hoy el diseño no los pide: se asume que no.
4. Nombre y arte de cada estación (para el cartel del HUD).

## Pendiente de Tony
- Confirmar los IDs de las tareas del bloque 7 (T-198 en adelante) y mover estos pasos a docs/tareas/pendientes/ cuando él planifique el bloque.
- Confirmar si `Radio` referencia `UI` (permitido solo si la tarea lo pide) o usa `Registro<IRadio>` + evento `CancionCambiada` (recomendado: así no hay dependencia).
- Tecla de cambio de estación (no definida en el diseño).
