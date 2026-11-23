# Progreso 23-nov tarde (T77, T78, T79)
Paso 0: sin cambios nuevos del chat de Blender (los .blend >100 MB de _REVISION_TONY siguen fuera, como antes). main al día (8de11df).

| Tarea | Rama | Estado |
|---|---|---|
| T77 | tony/T-1720 | HECHA (merge en main) |
| T78 | tony/T-1721-candados-inicio | HECHA (merge en main 8f4ef40; correr con la prótesis: Tony a mano) |
| T79 | tony/T-1722-campo-tiro | HECHA (merge en main) |

## Detenido por Tony (23-nov tarde)
- T78 se detuvo a medias. Su avance está guardado en la rama tony/T-1721-candados-inicio (sin merge a main): CandadosMundo (Core),
  GestorCandados, cambios en DirectorRecorrido, clínica (prótesis gratis), banco de Tino, interacción, avisos y 2 pruebas EditMode.
  El Editor.log no tenía errores de compilación al cerrar. Ni las pruebas ni el flujo completo en Play quedaron comprobados; solo hay una
  captura del candado de robo de carros. Para seguir: checkout de esa rama, revisar, correr las pruebas por clase y comprobar en Play.
- T79 sin empezar. Builds y reporte final pendientes.
- Ruido del Play (overrides, ControladorBase, JugadorAPie, Visual_Prueba, ConfigArmasEspeciales) descartado con git restore.

## Retomado (23-nov noche)
- Respaldo de las partidas antes del Play: %TEMP%/respaldo_saves/CYBERCITY_1452 (el Play del editor escribe partida_auto.json).
- Teclado simulado NO llega al editor (QueueStateEvent ni keybd_event): pasos que piden teclas se prueban llamando métodos (Usar, Pulsar,
  ExecuteEvents de clic) o con F8 (SaltarAPaso).
- Pendiente al final: prueba de punta a punta + guardar/Continuar a mitad (candados restaurados), builds uno por uno, reporte-23nov-tarde.md.
