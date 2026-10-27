# T-311 Fachadas y entradas con neón
Dueño: Alan | Rama: alan/T-311-fachadas-neon (base alan/fase-5) | Modelo: Sonnet 5.5, medium
Estado: **WIP, sin verificar** (Unity ocupado por otra sesión; falta compilar y correr pruebas y capturas).

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
- [ ] Entrada reconocible en capturas de día y de noche de los 4 destinos: docs/capturas/T-311-<Lugar>-dia|noche.png, revisadas
- [ ] Nada sobre la `Entrada` ni la puerta; sin colliders nuevos; nada sobresale más de 0.9 m de la fachada (prueba)
- [ ] Casino y mercado siguen cerrados a cualquier hora (prueba)
- [ ] Coste: ≤ 2 luces nuevas en total, sin sombras; ≤ 6 materiales distintos (prueba)
- [ ] `CreadorTodo.Crear` repetido deja el prefab igual
- [ ] `bash herramientas/verificar.sh todo` pasa

## Pendiente (Unity)
1. Correr `VestirEntradas.Crear` / `CreadorTodo.Crear`; commitear Barrio.prefab, `Entrada_*.mat` y todos los `.meta` nuevos (incluidos los de VestirEntradas.cs y EntradasNeonPruebas.cs, que aún no existen).
2. Compilar, EditMode, PlayMode; revisar las 8 capturas y ajustar (posible solape del marco con la cortina del mercado, `Guia`, y con la marquesina del casino).
3. Aviso de solape: el Casino ya tiene marquesina y tubos (T-094); si el toldo nuevo se ve duplicado, quitarlo allí.
