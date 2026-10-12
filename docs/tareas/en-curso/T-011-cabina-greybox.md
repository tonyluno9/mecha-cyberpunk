# T-011 Cabina greybox
Dueño: Tony | Rama: tony/T-011-cabina-greybox | Modelo: Sonnet 5.5, medium
Depende de: T-001, T-010

## Objetivo
La cámara queda dentro de una cabina hecha con cubos: se ven el marco del cristal, el tablero y los dos brazos del mecha.
Es lo que hace que el juego se sienta "desde la cabina" y es donde T-012 anima los golpes y T-013 pone la pantalla de vida.

## Contexto
- Diseño: docs/diseno/combate.md (brazos de precisión) y docs/diseno/estetica-hud.md (en el mecha todo es diegético,
  neón azul, magenta, naranja y turquesa).
- Decisiones: D-011 (pantallas diegéticas), D-014 (escala).
- Parte de Mecha/Prefabs/MechaJugador.prefab y Escenas/Pruebas_Tony.unity, de T-010.
- La cabina va fija al cuerpo del mecha: al mirar arriba o abajo se mueve la cabeza del piloto, no la cabina.

## Qué hacer
1. Dentro del prefab, bajo el cuerpo (no bajo la cámara), crea `Cabina` con cubos y planos: marco del cristal con
   postes delgados, techo, paneles laterales y un tablero abajo. Deja libre la vista central hacia adelante.
2. Pon un cristal: un quad transparente con un tinte azul muy leve (material URP/Lit transparente en Mecha/Materiales/).
3. Agrega una o dos tiras de luz de neón (material emisivo turquesa o magenta) en el marco para que se lea como cabina.
4. Crea dos brazos greybox, `BrazoIzquierdo` y `BrazoDerecho`, cada uno con el pivote en el hombro y tres partes:
   brazo, antebrazo y `Puno` (un cubo). En reposo se ven en la parte baja de la vista, uno a cada lado.
   T-012 los anima moviendo esos pivotes, así que deja los nombres y la jerarquía así.
5. Deja en el tablero un objeto vacío `AnclaPantallaVida` en el centro y un quad oscuro como marco. T-013 pone ahí la pantalla.
6. Ajusta la cámara: plano cercano de unos 0.05 m para que el marco no se recorte al girar. Quita las sombras que
   proyectan las piezas de la cabina si ensucian la vista.
7. Si algún valor visual necesita ajustarse en el editor, que sea un campo serializado o un `Config*` en Mecha/Datos/,
   no un número en el código. Esta tarea casi no necesita código.

## Carpetas permitidas
Assets/_Proyecto/Mecha/ y Assets/_Proyecto/Escenas/Pruebas_Tony.unity
No tocar: Core/, Combate/, UI/, Arena/, IA/, otras escenas ni ProjectSettings/.

## Criterios de aceptación (se comprueban jugando)
- [ ] Al dar Play en Pruebas_Tony.unity veo el marco de la cabina alrededor de la vista y el centro despejado
- [ ] Veo los dos brazos abajo, uno a cada lado, y se mueven con el mecha al caminar y girar
- [ ] Al mirar arriba veo el techo de la cabina y al mirar abajo el tablero; la cabina no se inclina con la vista
- [ ] Al girar rápido, ninguna pieza de la cabina parpadea ni se recorta
- [ ] Se ve hacia afuera a través del cristal con un tinte leve
- [ ] El neón del marco se distingue del resto de la cabina
- [ ] El movimiento de T-010 sigue funcionando igual
- [ ] No aparecen errores en la consola

## Pruebas automáticas
- Ninguna: esta tarea no tiene lógica pura. Corre las pruebas de EditMode existentes para confirmar que siguen pasando.

## Fuera de alcance
- Animar los brazos (T-012), pantalla de vida y sacudida (T-013)
- LED rojo y cristal cuarteado con vida baja
- Encendido de 3 pasos, palancas y botones de la cabina
- Modelos finales
