# T-674 Agachado se ve agachado
Dueño: Tony | Rama: tony/T-674-agachado-pose | Modelo: Sonnet 5.5
Depende de: T-673

## Objetivo
Arreglo de T-673: la captura mostraba al personaje casi de pie, inclinado y con los brazos atrás. Agachado quieto, caminando y apuntando deben verse agachados.

## Qué se hizo
- No hay clip de agachado quieto en Assets/Arte (solo Rifle_AgachadoCaminar), así que se armó una pose procedural: PoseAgachado (Personaje/Visual) baja la cadera, dobla las rodillas con cálculo de dos huesos (los pies se quedan donde los pone la animación), echa la cadera un poco atrás y endereza el torso al caminar.
- Se monta sobre cualquier animación: quieto usa Idle (brazos naturales), caminando AgachadoCaminar, apuntando la capa de arriba con la pose de apuntar (antes agacharse apagaba esa capa).
- VisualPersonaje agrega PoseAgachado sola al agacharse (sirve para cualquier personaje, incluso enemigos que se agachen).
- AnimacionJugador ya no usa la pose de cobertura para el agachado quieto.

## Criterios
- [x] Quieto agachado: cadera y cabeza bajan, torso derecho, rodillas dobladas, pies en el piso, cabeza coincide con la cápsula baja
- [x] Caminando y apuntando agachado la cabeza sigue baja; apuntando usa la capa de arriba
- [x] Capturas docs/capturas/s1n-T-674-*.png

## Pruebas
PlayMode Personaje: PoseAgachadoT674Pruebas (2), AgacharseT673Pruebas (actualizada). PersonajeVisual ControladorBaseVisualPruebas.
