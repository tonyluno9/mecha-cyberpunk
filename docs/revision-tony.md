# Revisión de Tony

Rama para probar: `tony/fase-2`. Turno de día en curso (2026-10-12).

## Por tarea

### T-030 Carpetas y ensamblados por dueño
- Verificado automáticamente: 6 de 7 criterios (EditMode 59/59, PlayMode 64/64); el séptimo es de juicio humano.
- Para ti: que las referencias de Mundo, Vehiculos, Casino y Trafico le alcancen a Alan para A-F1 y A-F2 (¿TextMeshPro?).
- Decisiones del agente: la regla "Core no referencia nada" solo aplica a MechaCyberpunk.Core.
- Se quitó del commit ProjectSettings/SceneTemplateSettings.json (lo generó el editor).
- Integrada a main porque Alan la necesita.

### T-031 Contratos de arena y jugador
- Verificado automáticamente: todos los criterios (EditMode 68/68, PlayMode 64/64).
- Para ti (o Alan): abrir Assets/_Proyecto/Core/Arena/IArenaPelea.cs y confirmar que los comentarios bastan para armar la jaula y orientar la pantalla (+Z) sin preguntar.
- Decisiones del agente: Registros.Apuntar público pero documentado como interno; Quitar(null) no hace nada.
- Sugerencia: prueba de PlayMode de Registro<T> con un MonoBehaviour real, en una tarea posterior.
- Integrada a main porque Alan la necesita.

### T-032 Contratos de reloj, lugares y puertas
- Verificado automáticamente: 6 de 6 criterios (EditMode 94/94, PlayMode 64/64).
- Para ti (o Alan): leer los /// de IRelojMundo, ILugar e IPuerta en Core/Mundo/ y confirmar que bastan para T-041 y T-042.
- Decisiones del agente: HorarioLugar normaliza abre/cierra (24 = 0); nombres PublicarEntro / PublicarSalio / PublicarEstadoLugar.
- Integrada a main porque Alan la necesita.

### T-021 Rival básico
- Verificado automáticamente: todos los criterios (EditMode 105/105, PlayMode 80/80; captura docs/capturas/T-021-1.png). La tecla C real no se probó en vivo (editor sin ventana); la cubre una prueba con teclado simulado.
- Para ti: en Pruebas_Tony, tecla C. ¿El aviso naranja da tiempo de subir la guardia (clic derecho) y bloquear en la raya se siente justo? ¿El rival se siente pesado y la caída en KO se entiende? Clic izquierdo para pegarle.
- Observación: el AtacanteDePrueba de la escena mata al jugador en unos 30 s y estorba; conviene apagarlo al probar.
- Decisiones del agente: límite de DistanciaMinima por distancia; espera entre golpes corre solo en Listo; RivalBasico.Start quita el control (T-022 lo activa un frame después).
