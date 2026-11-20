# Progreso: pulir cómo se siente jugar (19-nov, noche)

Pedido de Tony: caminar, correr, disparar, golpear y manejar sin una sola falla; lo prueba en el build.
Merge --no-ff a main y push autorizados por Tony. Una sola instancia de Unity; nada de PlayMode completo (pruebas por clase);
no correr CreadorTodo; nada de animaciones hechas a mano (solo PJ01_*, Loco_*, Mixamo); lo que falte va en docs/animaciones-faltan.md.
No grabar videos. Al final: builds Demo y Juego uno por uno, docs/reporte-gamefeel.md, cerrar Unity.

## Dónde se trabaja
- Integración: `.sesiones/s2` (tiene Library), rama `integ/gamefeel` (sale de main 3b2a58a).
- Tareas en worktrees `.sesiones/gf-T-148x` (sin binarios LFS: los subagentes no compilan ni tocan binarios). Se integran
  en `integ/gamefeel` y se compilan y prueban en s2. Al terminar se borran los worktrees.

## Tareas
| Tarea | Qué | Estado |
| --- | --- | --- |
| T-1484 | Caminar y correr: velocidad real de cada clip, blend tree por velocidad, Loco_Caminar vs PJ01, arranque/frenado, inclinación, foot IK, escaleras | fase 1 integrada (MedirLocomocionT1484.Medir); fase 2 espera medidas |
| T-1485 | Cámara a pie: suavizado, sin atravesar paredes, balanceo al correr, ajuste al apuntar, FOV al correr | integrada, EditMode 10/10 |
| T-1486 | Disparar: peso por arma, retroceso, temblor, destello, casquillo, cañón a la mira, hitmarker y marca de muerte, reacción, apuntar/recargar | integrada; falta AjustarSensacionT1486.Ajustar y pruebas |
| T-1487 | Golpear: asistencia al más cercano con acercamiento, congelamiento 50-80 ms, temblor, sonido, reacción/empuje, combo | integrada; falta CrearConfigGolpeT1487.Crear y pruebas |
| T-1488 | Manejar: peso por carro, frenos, derrape, suspensión, cámara por velocidad y FOV, temblor al chocar, motor, subir/bajar/robar alineado, motos estables | en curso (subagente Sonnet) |
| T-1489 | Detalles: polvo al caer, salpicadura en charcos, temblor e indicador de dirección del daño, viñeta roja con vida baja | integrada; falta compilar y pruebas |
| Final | Integrar, compilar, pruebas por clase, reporte, builds | pendiente |

## Bitácora
- 19-nov: paso 0 hecho (Unity cerrado, main 3b2a58a, 16 GB RAM libres, 154 GB de disco). Worktrees creados.
- 6 subagentes lanzados. Temblor de cámara: contrato común CinemachineImpulseSource -> listener en las cámaras (T-1485 a pie, T-1488 carro).
- Integradas T-1484 fase 1, T-1485, T-1486, T-1487, T-1489 en integ/gamefeel (4b6f913). Temblor: TemblorCamara en MechaCyberpunk.Personaje.Movimiento.
