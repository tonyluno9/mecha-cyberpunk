# T-020 Arena greybox del clandestino
Dueño: Compañero | Rama: compa/T-020-arena-clandestino | Modelo: Sonnet 5.5, medium
Depende de: T-001

## Objetivo
Existe la jaula del clandestino en greybox: piso, reja que no se puede cruzar, gradas de relleno y luces básicas con el
neón del juego. Es el lugar donde se pelea la fase 1 y la base de la escena Clandestino.unity.

## Contexto
- Diseño: docs/diseno/ciudad.md (el clandestino abre de noche, frente al bar) y docs/diseno/estetica-hud.md
  (low-poly, niebla, neón azul, magenta, naranja y turquesa).
- Decisiones: D-003 (escenas), D-009 (sin capas ni tags), D-011 (pantalla de la arena), D-014 (escala: mecha de 4 m,
  jaula de unos 24 × 24 m, peleadores a unos 12 m).
- Esta tarea casi no lleva código. No hay contratos de Core que usar todavía; depende de T-001 para que main ya tenga Core.

## Qué hacer
1. Crea el prefab `Arena/Prefabs/JaulaClandestino.prefab` con:
   - Piso de unos 24 × 24 m (cubo plano), oscuro y un poco brillante, como concreto mojado.
   - Reja alrededor: postes y barras de cubos, de unos 6 m de alto, con colliders que cubran todo el perímetro.
   - Fuera de la reja, gradas de relleno con cubos escalonados y unas siluetas de público (cápsulas), sin colliders
     que estorben dentro de la jaula.
   - Dos objetos vacíos `InicioJugador` e `InicioRival`, a unos 12 m uno del otro, mirándose.
   - Un quad grande y oscuro en lo alto de un lado de la reja, `PantallaArena`, visible desde el centro de la jaula a la
     altura de la cabina (unos 3.5 m). T-022 pone ahí el conteo y el resultado.
2. Luces: luz ambiental baja, 4 a 6 luces de punto o de foco con los colores del neón sobre la jaula, y una luz
   principal cenital blanca y fría para que la pelea se lea bien. Materiales en Arena/Materiales/.
3. Crea `Escenas/Clandestino.unity` con la jaula, sus luces y niebla leve (Lighting > Environment).
4. Crea `Escenas/Pruebas_Compa.unity` con la jaula y una cámara a 3.5 m de alto en `InicioJugador` mirando al centro.
5. En las dos escenas, quita la cámara que trae la escena si sobra.

## Carpetas permitidas
Assets/_Proyecto/Arena/, Assets/_Proyecto/Escenas/Clandestino.unity y Assets/_Proyecto/Escenas/Pruebas_Compa.unity
No tocar: Core/, Mecha/, Combate/, UI/, IA/, Pruebas_Tony.unity ni ProjectSettings/ (tampoco Lighting global del proyecto
ni capas o tags).

## Criterios de aceptación (se comprueban jugando)
- [ ] Al dar Play en Pruebas_Compa.unity, desde la cámara a 3.5 m veo la jaula completa, la reja, las gradas y la pantalla
- [ ] Las luces de neón se distinguen (al menos tres colores del juego) y aun así el centro de la jaula se ve claro
- [ ] Hay niebla leve en Clandestino.unity y las gradas del fondo se pierden un poco en ella
- [ ] Un cubo con Rigidbody soltado dentro de la jaula cae al piso y no lo atraviesa; empujado en el editor contra la reja, no la cruza
- [ ] La escala se siente de mecha: la reja es más alta que una cápsula de 4 m puesta junto a ella
- [ ] No aparecen errores en la consola en ninguna de las dos escenas

## Pruebas automáticas
- Ninguna: esta tarea no tiene lógica pura. Corre las pruebas de EditMode existentes para confirmar que siguen pasando.

## Fuera de alcance
- Rival (T-021), flujo y textos de la pantalla (T-022)
- Presentador, público animado, sonido
- Exterior del clandestino, la calle, el bar
- Iluminación horneada y postprocesado
