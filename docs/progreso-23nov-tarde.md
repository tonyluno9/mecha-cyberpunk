# Progreso 23-nov tarde (T77, T78, T79)
Paso 0: sin cambios nuevos del chat de Blender (los .blend >100 MB de _REVISION_TONY siguen fuera, como antes). main al día (8de11df).

| Tarea | Rama | Estado |
|---|---|---|
| T77 | tony/T-1720 | HECHA (merge en main) |
| T78 | tony/T-1721-candados-inicio | A MEDIAS (detenido, en su rama) |
| T79 | tony/T-1722 | pendiente |

## Detenido por Tony (23-nov tarde)
- T78 se detuvo a medias. Su avance está guardado en la rama tony/T-1721-candados-inicio (sin merge a main): CandadosMundo (Core),
  GestorCandados, cambios en DirectorRecorrido, clínica (prótesis gratis), banco de Tino, interacción, avisos y 2 pruebas EditMode.
  El Editor.log no tenía errores de compilación al cerrar. Ni las pruebas ni el flujo completo en Play quedaron comprobados; solo hay una
  captura del candado de robo de carros. Para seguir: checkout de esa rama, revisar, correr las pruebas por clase y comprobar en Play.
- T79 sin empezar. Builds y reporte final pendientes.
- Ruido del Play (overrides, ControladorBase, JugadorAPie, Visual_Prueba, ConfigArmasEspeciales) descartado con git restore.
