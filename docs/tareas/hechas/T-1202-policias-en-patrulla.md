# T-1202: los policías van dentro de la patrulla y se bajan como el jugador

Rama `tony/T-1202-policias-en-patrulla`. Pedido del 17-nov, punto 3.

## Antes
La patrulla iba vacía y, al llegar, los policías aparecían de golpe junto a las puertas (`Instantiate` en `BajarPolicias`).

## Qué se hizo
- `OcupantePatrulla` (Trafico/Persecucion): una figura del policía, hecha del mismo prefab `PoliciaAPie` pero sin sus scripts, colliders ni cuerpo rígido.
  - Se instancia bajo un padre apagado, así no corre ningún `Awake` de la IA.
  - Va sentada con la cadera en el asiento y juega `PJ01_Carro_Manejar` (con un PlayableGraph, porque Trafico no puede referenciar a Personaje ni a IA).
  - Al bajarse juega `PJ01_Carro_Salir` a 3× (como el jugador): del asiento a la puerta y de la puerta al piso, en 1.9 s.
- `PatrullaMundo`:
  - Al salir sienta a sus policías: el conductor en el asiento del jugador y el copiloto del otro lado; si hubiera más de 2, atrás.
  - `BajarPolicias` hace que cada figura se baje por su lado; al tocar el piso, en su lugar queda el policía de verdad (con su IA).
  - Si explota o la roban con ellos dentro, salen de golpe junto a su puerta.
- `CreadorPoliciasSentadosT1202` (editor) le pone a `Patrulla.prefab` los dos clips.
- Las rondas también llevan a sus policías dentro. Ya no hay otra forma de que aparezca un policía.

## Evidencia
- `PoliciasSentadosT1202Pruebas` 2/2:
  - Sentados: dentro de la carrocería, sin colliders ni IA, cabeza bajo el techo.
  - Al bajarse: hasta tocar el piso no hay ningún policía de verdad; luego quedan los 2 afuera, a más de 1.2 m de lado y en el piso.
- `PoliciaRondaT933` (ahora espera a que terminen de bajarse), `TestigosT661`, `PersecucionFisicaT986` y `PatrullaLlegaT708`: 16/16. EditMode 2577/2577.
- Capturas en `docs/capturas/t1202/`: sentados de lado y de frente (se ven por la ventana), bajándose y abajo.

## Notas
- Cuando la patrulla se retira después de bajar a sus policías, ellos se quedan; no vuelven a subir.
