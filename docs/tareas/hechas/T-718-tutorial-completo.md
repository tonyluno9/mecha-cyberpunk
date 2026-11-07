# T-718 Tutorial completo con las teclas reales
Dueño: Tony (S1) | Rama: tony/T-718-tutorial-completo | Modelo: Sonnet 5.5
Depende de: T-441, T-700, T-716, T-717

## Objetivo
El tutorial recorre de punta a punta lo que el jugador debe aprender (caminar, correr, agacharse, rodar, golpes, pistola y apuntado, carro, moto, teléfono, objetos, escudo, y la historia con el Doc, Tino y los SKULLS) y las teclas que muestra son las reales del Input System, no texto escrito.

## Qué había
- TutorialLigero (T-441) con 10 avisos y las teclas escritas en el texto ("W A S D", "CLIC DERECHO", "Q", "CTRL"): si el jugador cambiaba una tecla veía la vieja. Faltaban correr, agacharse, golpear, moto, objetos y escudo. El clic con los puños contaba como disparo (Disparar nunca se ofrecía). La ayuda del paso del CHATO y el objetivo de la pistola ("(E)") también llevaban las teclas escritas.

## Qué se hizo
- AccionTutorial/ReglaTutorial: 16 acciones (Moverse, Correr, Agacharse, Golpear, Apuntar, Disparar, Recargar, Cubrirse, Rodar, SubirAlCarro, Robar, Moto, Mapa, Misiones, Consumibles, Escudo) con su cadena de requisitos (todas llegan a Moverse, sin ciclos); `Texto(accion, resolver)`: las teclas las pide el texto a un resolver por nombre; sin resolver usa las de siempre.
- TutorialLigero.TeclaReal: cada nombre sale de GetBindingDisplayString (TeclasHud): acciones del mapa APie, TeclasHud.TextoMover para el compuesto W A S D, las acciones privadas de CubiertaYRodada (agacharse, cubrirse) y ConsumiblesJugador (elegir, usar) que ahora se exponen, y la tecla del mapa. Detecta correr (Shift con movimiento), agacharse, golpear (puños), objetos (Tab o F), escudo (IEscudo.Absorbio) y moto (montable ocupado con "moto" en el nombre). Apuntar, Disparar y Recargar solo se ofrecen con un arma de fuego en la mano.
- DirectorPandillas: la ayuda "APUNTA · DISPARA" del CHATO usa las teclas reales (AyudaApuntarYDisparar) y el objetivo de la pistola ya no trae "(E)".
- RecorridoDemoPruebas: tramo 03c que, con el jugador libre tras el asalto, espera cada aviso (moverse, correr, agacharse, golpear), comprueba que muestra las teclas reales y lo hace con entrada simulada.

## Criterios
- [x] Cada aviso muestra la tecla real; si el jugador cambia una tecla, el aviso muestra la nueva
- [x] Moverse, correr, agacharse, golpear, mapa, teléfono, objetos y escudo se cumplen con entrada simulada y se dan por vistos
- [x] Ningún paso del tutorial se traba: lo que no se puede hacer (sin carro, sin objetos) se salta
- [x] El tutorial ya no da por disparo el clic con los puños

## Pruebas
- EditMode UI: TutorialTeclasRealesT718Pruebas (7: texto sin teclas escritas, tecla cambiada, cadena sin ciclos, recorrido completo, recorrido sin acciones posibles, TextoMover, acciones del mapa APie), ReglaTutorialPruebas (actualizada)
- PlayMode UI: TutorialCompletoT718Pruebas (2: de moverse al escudo con teclas simuladas; tecla cambiada con ApplyBindingOverride). Capturas docs/capturas/s1c-T-718-aviso-*.png
- PlayMode Demo: RecorridoDemoPruebas con el tramo 03c (corrido con T474_HASTA=4, ver el resumen)

## Notas
- Apuntar, Disparar, Recargar, Cubrirse y Rodar se dan por hechos con Regla.Hecha en la prueba de UI (necesitan arma y cobertura); su texto con la tecla real sí se comprueba. Subir al carro/robar/moto no se simulan en esa prueba (necesitan un carro cerca): cubiertos por la cadena y por las pruebas de vehículos.
- PanelOpcionesInicio (pantalla de controles de las opciones) sigue con un texto fijo de controles: no es del tutorial; pendiente.
