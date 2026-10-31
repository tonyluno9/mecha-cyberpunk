# T-424 Apuntado asistido suave
Dueño: Tony | Rama: tony/T-424-apuntado-asistido | Modelo: Sonnet 5.5 | MODO RÁPIDO (docs/plan-noche.md, paso 5; mejora 4 de docs/mejoras-pendientes.md)
Depende de: T-411. Tony autorizó tocar todas las áreas y Core.

## Objetivo
Al apuntar con clic derecho, la cámara se jala suave hacia un enemigo (pandillero o jefe) cerca de la retícula y con línea de vista; la retícula cambia de color. Graduable en OPCIONES; mínimo con el sniper con zoom.

## Qué hacer
1. Regla pura `ReglaApuntadoAsistido` (EditMode), `DatosApuntadoAsistido`, componente `ApuntadoAsistido`, `ControladorAPie.Empujar`.
2. `IOpciones.ApuntadoAsistido` (Core), ServicioOpciones, fila en PantallaOpciones, color de mira en HudAPie.
3. Pruebas: EditMode ReglaApuntadoAsistidoPruebas, PlayMode ApuntadoAsistidoPruebas.

## Carpetas permitidas
Core/Opciones, Personaje, Progresion/Opciones, UI, Tests, docs de Tony.

## Bitácora
- Hecha: EditMode 10/10 (ReglaApuntadoAsistido, ReglaOpciones, MiradaAPie), PlayMode ApuntadoAsistidoPruebas 3/3 + OpcionesPruebas + TituloCreacionPruebas pasan (6/6 en la corrida); OpcionesPruebas ajustada por la fila nueva. Captura docs/capturas/T-424-1.png (retícula roja sobre un enemigo). D-tony-289, D-tony-290.
