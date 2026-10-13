# Estado de Alan (modo automático)

Fase A-F2 (Carro, docs/hoja-de-ruta.md), rama de integración: alan/fase-2 (sale de alan/fase-1).
A-F1 (T-020, T-040 a T-045) está hecha y revisada en alan/fase-1; espera la aprobación de Alan para pasar a main.

| Tarea | Estado |
| --- | --- |
| T-074 Carro viejo: física y manejo | hecha (sin revisar) |
| T-075 Subir y bajar del carro, y sus cámaras | pendiente (depende de T-074) |
| T-076 Arte de Blender a Unity: FBX, materiales y prefabs | hecha (sin revisar) |
| T-077 Finales sólidos y retenes | pendiente (depende de T-074) |
| T-078 El carro en el barrio, cierre de H3 | pendiente (depende de T-075 y T-077) |
| T-079 Noche cyberpunk: posproceso y asfalto mojado | pendiente (depende de T-076) |
| T-080 La calle vestida con el kit urbano | pendiente (depende de T-076 y T-079) |
| T-081 Fachadas con detalle | pendiente (depende de T-076 y T-079) |
| T-082 Letreros de neón | pendiente (depende de T-076 y T-081) |
| T-083 El bar con los props de Blender y mejor luz | pendiente (depende de T-076 y T-079) |
| T-084 Tráfico mínimo de fachada | pendiente (depende de T-077 y T-078) |
| T-085 Modelo del carro viejo | pendiente (depende de T-075 y T-076) |
| T-060, T-061 (de Tony) | bloqueadas: siguen esperando que A-F1 y A-F2 lleguen a main (D-alan-22) |

Orden: T-074, T-075, T-076, T-077, T-078, T-079, T-080, T-081, T-082, T-083, T-084, T-085, una tras otra (D-alan-21).
Las fuentes de Blender están en Assets/Arte/Fuentes~/ (D-alan-18).
Qué sigue: crear alan/fase-2 desde alan/planificacion-fase-2 (ya contiene alan/fase-1 y estas tareas; main todavía no tiene A-F1)
y empezar con T-074. Ojo: docs/estado.md todavía dice A-F1 para Alan y la skill trabajar crea `<persona>/fase-N` desde main;
hasta que A-F1 se apruebe, la fase de Alan es A-F2 y su rama alan/fase-2 sale de esta rama, no de main.
