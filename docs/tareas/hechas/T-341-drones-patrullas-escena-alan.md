# T-341 Drones y patrullas con calor (A-F5)
Dueño: Alan | Rama: alan/T-341-drones-patrullas | Modelo: Sonnet 5.5, medium
Estado: **hecha y verificada** (2026-10-28), integrada en alan/fase-6 (EditMode 1143/1143, PlayMode 1028 pasaron, 0 fallos, 9 omitidas; pruebas de Trafico y capturas T-341-1/2 en docs/estado-alan.md).
Depende de: T-340, T-077 (calles), `ICalor` e `IJugador` en main.

## Objetivo
Con calor te persiguen drones (vuelan sobre ti con una luz) y, en nivel 3, una patrulla (carro con luces rojas y azules por las calles). Al llegar a casa el calor se borra y se van.

## Qué hacer
1. `Trafico/Persecucion/DronMundo.cs` y `PatrullaMundo.cs`, con prefabs creados por `Trafico/Editor/CreadorPersecucion.cs`; `GestorPersecucion` escucha `ICalor.NivelCambio` y crea o retira perseguidores con `CalculoPersecucion`.
2. Llegar a la casa: el lugar Casa llama a `ICalor.Borrar()` cuando el jugador entra (`EventosLugares`).
3. Los drones no tienen colliders que estorben; la patrulla sigue las calles con waypoints y no sale de ellas.

## Carpetas permitidas
Assets/_Proyecto/Trafico/, Assets/_Proyecto/Mundo/ (colgar el grupo en el barrio y el aviso de la casa), Tests de Trafico y Mundo, docs/capturas/. No tocar Core/ ni carpetas de Tony.

## Criterios de aceptación
- [ ] Con un doble de `ICalor`: nivel 0 sin perseguidores; subir el nivel los crea; bajar a 0 los retira
- [ ] Entrar a la casa borra el calor y retira a los perseguidores
- [ ] La patrulla no sale de la calle en 60 s; sin errores en consola; capturas: docs/capturas/T-341-1..2.png
- [ ] `bash herramientas/verificar.sh todo` pasa

## Fuera de alcance
Daño del dron al jugador, choques, persecuciones con mecha (Tony).
