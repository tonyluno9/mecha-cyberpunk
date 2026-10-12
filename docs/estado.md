# Estado del proyecto

Actualizado: 2026-10-11, sesión T-000b (preparación del repo y planeación de la fase 1).

## Fase actual: 1, pelea greybox
Meta: puedo golpear a un rival que me golpea, y uno de los dos gana. Todo con cubos y cápsulas, sin modelos finales.
Prueba de cierre: cada uno juega 3 peleas completas en Clandestino.unity sin errores en la consola.

## Tareas de la fase 1

| ID | Tarea | Dueño | Depende de | Estado |
| --- | --- | --- | --- | --- |
| T-001 | Contratos de Core | Tony (revisa el compañero) | Nada | Pendiente |
| T-010 | Movimiento del mecha en primera persona | Tony | T-001 | Pendiente |
| T-011 | Cabina greybox | Tony | T-001, T-010 | Pendiente |
| T-013 | Vida del mecha y recibir daño | Tony | T-001, T-011 | Pendiente |
| T-012 | Golpe y bloqueo | Tony | T-001, T-011, T-013 | Pendiente |
| T-020 | Arena greybox del clandestino | Compañero | T-001 | Pendiente |
| T-021 | Rival básico | Compañero | T-001, T-020 | Pendiente |
| T-022 | Flujo de pelea | Compañero | T-001, T-020, T-021, T-013 (paso 6) | Pendiente |

## Orden recomendado
1. T-001 primero. Nadie empieza otra tarea hasta que esté en main.
2. Después, en paralelo: Tony hace T-010, T-011, T-013 y T-012; el compañero hace T-020, T-021 y T-022.
3. T-013 va antes que T-012 porque el bloqueo cambia el daño que recibe la vida.
4. El paso 6 de T-022 (integrar el mecha de Tony en Clandestino.unity) necesita T-013 en main.

## Qué hay en main
- Repo preparado: .gitignore, Git LFS, carpetas de Assets/_Proyecto con sus asmdefs, CLAUDE.md, reglas de C#,
  skill `/tarea`, subagente `revisor`, diseño separado por bloque, plantillas y tareas de la fase 1.
- Todavía no hay código del juego.
- Las pruebas de EditMode corren en batchmode (0 pruebas por ahora).

## Pendiente de Tony
- Confirmar o cambiar las decisiones marcadas "Propuesta" en docs/decisiones.md (D-011 a D-016) antes de que empiecen
  las tareas que las usan.
- Dar acceso al repo al compañero. Él clona, corre `git lfs install` y abre el proyecto con Unity 6000.3.25f1.
