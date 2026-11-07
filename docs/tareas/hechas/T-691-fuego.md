# T-691 Quemarse
Dueño: Tony | Rama: tony/T-691-fuego | Modelo: Sonnet 5.5, medium
Depende de: T-684

## Objetivo
El jugador se prende con una explosión; el fuego baja la vida por ticks y se apaga solo (4 s), al rodar o al salir del área en llamas; nunca daña sin fin. Revisar el "daño progresivo misterioso" junto al robot de Nova.

## Qué hacer
1. Personaje/Vida: ReglaQuemadura (pura), QuemaduraJugador (llamas, luz, ticks) y VidaAPie (prende con daño Explosion, AplicarQuemadura, Restaurar apaga).
2. Medición: el jugador 30 s junto a un guardia de Nova (robot) sin atacarlo.

## Medición del daño misterioso
Resultado medido (MedicionDanioNovaT691Pruebas, Ciudad_Prologo, 3 guardias de Nova, 30 s a 2 m): 0 impactos, 0 vida perdida. Los GuardiaNova solo hacen gestos (Nova_Punch) y no tienen componente de daño. En el código el jugador solo pierde vida por impactos publicados (golpe de pandillero, pistola de pandillero, dron, explosión) y, desde esta tarea, por fuego. No hay ningún componente con daño periódico sobre el jugador. Lo más probable es que el "daño progresivo" fuera el dron de la policía (el guardia llama a la policía al oír un disparo) o el fuego del carro sin la regla de apagado. La prueba queda como guardia: si algún día baja la vida sin impacto publicado, falla.

## Criterios de aceptación
- [x] Explosión prende; baja vida poco a poco; se apaga sola a los 4 s; daño total con tope (20)
- [x] Rodar (Ctrl + W + Espacio simulados) lo apaga; alejarse 5 m del origen lo apaga
- [x] Sin fuente no hay daño junto al robot de Nova

## Pruebas automáticas
- EditMode ReglaQuemaduraPruebas; PlayMode FuegoT691Pruebas y MedicionDanioNovaT691Pruebas; captura docs/capturas/s1t-T-691-quemandose.png
