# T-311 Fachadas y entradas con neón
Dueño: Alan | Rama: alan/T-311-fachadas-neon (base alan/fase-5) | Modelo: Sonnet 5.5, medium
Estado: **hecha y verificada** (corrida completa limpia de `verificar.sh todo`, 2026-10-27).

## Objetivo
Que la entrada de cada destino se reconozca desde la calle de día y de noche (auditoría visual: puertas "rectángulo negro", muros lisos), con geometría low-poly y neón acorde a docs/diseno/estetica-hud.md. No cambia la lógica de acceso.

## Alcance
- Casa, Clandestino (abiertos: neón brillante + 1 luz puntual sin sombras cada uno) y Casino, Mercado negro (cerrados: neón tenue, sin luz).
- Por destino: marco de acero (2 jambas + dintel), cajón de luz, 2 neones de jamba, 2 laterales, 2 zócalos de concreto, toldo inclinado con filo de neón. Sin colliders.
- Omitidos: Bar y Taller (interiores/locales ligados a otras tareas), fachadas del recorrido del prólogo (T-093 no integrada), tráfico y carro (T-084/T-085), asfalto y cielo de día (no son fachadas).

## Archivos
- `Mundo/Editor/VestirEntradas.cs` (nuevo; paso en `CreadorTodo` tras VestirCerrados). Materiales `Entrada_*.mat` los genera el creador.
- `Tests/PlayMode/Mundo/EntradasNeonPruebas.cs`.

## Criterios de aceptación
- [x] Entrada reconocible en capturas de día y de noche de los 4 destinos: docs/capturas/T-311-<Lugar>-dia|noche.png, revisadas
- [x] Nada sobre la `Entrada` ni la puerta; sin colliders nuevos; nada sobresale más de 0.9 m de la fachada (prueba)
- [x] Casino y mercado siguen cerrados a cualquier hora (prueba)
- [x] Coste: ≤ 2 luces nuevas en total, sin sombras; ≤ 6 materiales distintos (prueba)
- [x] `CreadorTodo.Crear` repetido deja el prefab igual
- [x] `bash herramientas/verificar.sh todo` pasa

## Evidencia (Unity 6000.3.25f1, 2026-10-27)
- Merge de origin/alan/fase-5 (T-093, T-310, T-301, T-302) sin conflictos.
- `VestirEntradas.Crear` corre sin errores; genera Barrio.prefab y 6 materiales `Entrada_*` (Marco, Zocalo, 4 neones) con sus .meta. Regenerar no duplica piezas (conteo de luces y piezas en prueba) aunque el prefab cambia en bytes por los fileID nuevos, como los demás creadores.
- Compilar OK. EditMode 952/952.
- PlayMode completo: 911/922, 2 fallos:
  1. `CicloDiaNochePruebas.Luces_A22...`: causa mía (las luces de entrada seguían prendidas a las 12:00). Corregido: llevan `LuzNocturna`. Ahora pasa.
  2. `RecorridoPrologoPruebas.Prologo_DeLaCreacionAContinuara...` ("la interacción ofrece la cama"): es del prólogo de Tony, no toca fachadas; pasa al correrla sola. No se reprodujo; no se probó en la base sin T-311.
- Corridas dirigidas después de la corrección: CicloDiaNoche, EntradasNeon, Cerrados, Lugares, Puertas, RecorridoPrologo: 13/13 y 29/29 pasan.
- Criterios medidos por pruebas: 0 colliders nuevos, nada sobre la Entrada, saliente ≤ 0.9 m, 2 luces nuevas (Casa y Clandestino, sin sombras, solo de noche), 6 materiales, casino y mercado siguen cerrados.
- Capturas revisadas (docs/capturas/T-311-<Lugar>-dia|noche.png, 8): marco, neón y letrero legibles y sin solapes con marquesina del casino ni cortina del mercado. El mercado de noche es oscuro a propósito (cerrado, neón tenue). La primera captura del mercado salió dentro de un muro (6 m en un callejón de 4.5 m): corregido a 3.2 m.

## Límites
- T-303 (interior del casino, pendiente) vuelve el casino abierto de noche: entonces su neón (hoy tenue y sin luz) deberá subirse; no se tocó aquí.
- Bar, Taller y fachadas del prólogo no se vistieron.

## Corrida completa final (commit 488eeba, 2026-10-27)
- `bash herramientas/verificar.sh todo`: compilar OK; EditMode 952/952 (0 fallos); PlayMode 922 casos: 913 pasan, 0 fallan, 9 omitidas por `[Ignore]` (capturas de T-134, T-148, T-149, T-170, T-169 y T-177, no de T-311). Sin corridas Unity sobre este worktree durante la prueba.
- Los dos fallos de la corrida anterior ya no aparecen (el del prólogo no se reprodujo).
- Capturas de T-311 revisadas de nuevo (8): entrada reconocible de día y de noche en los 4 destinos.
- La corrida regeneró capturas viejas, `Vidrio_Interior.mat`, `Flashback_Ventana.mat` y `ProjectSettings.asset`; no se commitearon.
