# T-676 Disparo limpio
Dueño: Tony | Rama: tony/T-676-disparo-limpio | Modelo: Sonnet 5.5

## Objetivo
Destello chico y brevísimo, sonido de disparo siempre igual, sin sonidos de impacto, sin rayos láser y sin disparar sin línea de vista.

## Qué se hizo
- Destello del cañón: quad aditivo chico (0.14 a 0.26 m según el arma) de 0.04 s con una chispa procedural; DatosSensacion.texturaDestello queda listo para F01_Destello.png (menú Mecha Cyberpunk > Ajustar T-676 destello F01 la asigna si ya está importada). La luz del cañón se limitó a intensidad 1.2 y alcance 2.5 (bañaba de naranja al personaje).
- Sonido: el disparo no varía el tono y el catálogo siempre elige el primer clip de *_DISPARO. Ya no suenan los impactos en suelo, pared, carro ni carne, ni la voz PAN_RIVAL_DANIO al herir al enemigo.
- Láser: el jugador no tenía rayos de bala. Los SKULLS: trazo de 6 mm y 0.03 s (corto); el aviso es un destello del hilo en el último 20 % del aviso (antes una línea durante todo el aviso).
- Línea de vista: los pandilleros no avisan ni disparan sin ella (también si su boca quedó dentro o tras un muro); el dron comprueba línea de vista; el jugador no dispara si entre su cuerpo y la boca del arma hay un muro (la bala pega ahí).
- No hay policía a pie todavía; la regla vale para el dron.

## Criterios
- [x] Destello de 2 o 3 cuadros y chico
- [x] Solo suena ARM_*_DISPARO (y el casquillo) al disparar a pared o persona
- [x] Sin muro ni barricada no pasa la bala (jugador, SKULLS, dron)
- [x] Trazo de bala de SKULLS finísimo y que no cruza la calle

## Pruebas
PlayMode: DisparoLimpioT676Pruebas (5), BalasT676Pruebas (2, IA), DronLineaDeVistaT676Pruebas (2, Tráfico). EditMode: DisparoSiempreIgualT676Pruebas (8).
