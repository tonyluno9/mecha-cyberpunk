# T-683 Agachado con animación real
Dueño: Tony | Rama: tony/T-683-agachado-animado | Modelo: Sonnet 5.5
Depende de: T-673, T-674

## Objetivo
Agachado usa animación de Mixamo y no la pose procedural de T-674; sin arma no se ve pose de arma; con Shift los pies se mueven más rápido; la rodada es lenta, natural, sin hundirse y termina agachado.

## Qué se hizo
- Se quitó PoseAgachado (la pose procedural) y sus pruebas.
- Controlador base (ConstructorControladorBase, ya reconstruido): estado AgachadoQuieto (la pose más baja de Cobertura_Entrar, fija con el parámetro PoseAgachadoQuieto) y AgachadoCaminar (Rifle_AgachadoCaminar); y una capa Brazos (máscara del torso, clip Loco_Idle) que VisualPersonaje prende solo agachado sin apuntar, así no se ve la pose de rifle ni brazos hacia atrás. Apuntando manda la capa Arriba como siempre.
- AgachadoCaminar: la rapidez del clip sube con la velocidad (ReglasAnimacion.RapidezAgachado): con Shift los pies van más rápido (0.8x a 2.8x; el clip avanza a 0.41 m/s y el jugador va a 1.6 o más, por eso el tope).
- Rodada: 0.8 s, 3.2 m, invulnerable 0.5 s (DatosCubierta, AjustarAgachadoT683); el muñeco se encoge hasta 70 % mientras gira (no se hunde), arranque y final suaves y el giro acaba al 85 %: el resto se reacomoda agachado.
- Faltan en Mixamo (anotados en docs/animaciones-faltan.md): Crouch Idle, Crouched Walking y Forward Roll. No hay clip de rodada en el proyecto, así que el giro sigue siendo procedural.

## Criterios
- [x] Quieto agachado: estado AgachadoQuieto, cabeza baja, manos colgando, sin pose de arma
- [x] Agachado con arma sin apuntar: brazos sueltos; apuntando: pose de arma y cañón al punto de mira
- [x] Con Shift el clip avanza al menos 1.2x más ciclos por segundo
- [x] Rodada de 0.8 s, el punto más bajo no baja de -0.12 m, termina agachado y derecho
- [x] Capturas docs/capturas/s1t-T-683-*.png
- [x] Caminar, correr y el cambio no cambian (ControladorBaseJugador, VelocidadesAnimacionT652, AnimacionDireccionT640)

## Pruebas
PlayMode Personaje: AgachadoAnimadoT683Pruebas (4), AgacharseT673Pruebas y ArmaEnManoS1Pruebas actualizadas.
