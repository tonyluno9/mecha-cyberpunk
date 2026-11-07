# T-705 Robar un carro con conductor
Dueño: Tony | Rama: tony/T-705-robar-con-conductor | Modelo: Sonnet 5.5

## Objetivo
Al robar un carro del tráfico, al conductor lo sacan del asiento, cae al suelo y se levanta asustado. Es un testigo: puede llamar a la policía (T-661, T-663) con el teléfono (Inter_Telefono). Si nadie llama ni hay patrulla que lo vea, no hay persecución.

## Decisiones
- El conductor ya no es "ConductorHuyendo" sin collider: es el prefab Peaton con PeatonMundo (testigo) y el componente nuevo ConductorRobado (nombre del objeto: ConductorRobado). Fases: lo sacan del asiento a la puerta (0.35 s) y cae (visual.Caido, 0.9 s tirado), se levanta (1.8 s) y queda asustado. De pie, si sigue llamando se queda parado mirando al jugador con el teléfono; si ya no llama (terminó, se canceló) sale corriendo (ConductorHuye, como antes) y desaparece. Si lo matan queda tirado y desaparece a los 10 s.
- Cómo cuenta como testigo: TestigosDelito, antes de decidir si un delito suma calor, pide a los carros de tráfico que están siendo robados que saquen a su conductor (CarroTrafico.SacarConductorRobado, idempotente), así no depende del orden en que reaccionen CarroEstacionado y CarroTrafico. Entonces el conductor es un civil más: ve al jugador, llama y, si termina la llamada, suma el calor.
- Para que "robar sin patrulla cerca sin que te persigan" siga siendo lo normal: el conductor pierde de vista al jugador a 25 m (PeatonMundo.RadioAbandonoPropio; los demás civiles siguen con 60 m de la config). Si te vas manejando, en 4 s ya estás lejos, la llamada se cancela y no hay calor. Si te quedas cerca de él hasta que termine de llamar (o una patrulla o cámara te ve), sí hay persecución.
- Teléfono (T-663): PeatonMundo.IniciarLlamada ahora juega Inter_Telefono (ya estaba en el controlador base como el gesto Telefono): lo saca al llamar y lo vuelve a sacar cada vez que termina el gesto mientras dura la llamada. Vale para cualquier civil que llama, no solo el conductor.
- Trafico.asmdef ya referenciaba Personaje.Visual desde T-703; los tests PlayMode de Trafico también.
- TraficoRobarPruebas (T-511) se actualizó: el conductor ahora primero cae y se levanta, y recién entonces huye.

## Criterios
- [x] Al robar, el conductor sale, cae (Tirado) y se levanta asustado (PlayMode)
- [x] Se queda en el suelo, no vuela (altura máxima < 0.35 m)
- [x] Si el jugador se queda, llama con Inter_Telefono y hay calor (PlayMode)
- [x] Si el jugador se va o lo matan, no llama y no hay calor (PlayMode)
- [x] El civil de T-663 juega Inter_Telefono al llamar (PlayMode)
- [x] Capturas: docs/capturas/s1c-T-705-conductor-en-el-suelo.png (cayendo) y s1c-T-705-conductor-asustado-llamando.png

## Pruebas
- EditMode: ConductorRobadoT705Pruebas (2). PlayMode Trafico: RobarConConductorT705Pruebas (5) y TraficoRobarPruebas ajustada.
