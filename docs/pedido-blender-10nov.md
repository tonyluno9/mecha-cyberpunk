# Pedido a Blender (10-nov): clips de PJ01 que faltan

T-749 resolvió el pedido del 9-nov: los clips de `imageassets/blender/pj01_clips/` entran por **Humanoid con el avatar de PJ01** y la pistola y las manos quedan igual que en Blender (medido en Unity: cañón a menos de 0.5° y manos a menos de 0.2 cm del mismo clip Generic). No hace falta Generic.

Lo que sigue saliendo de Mixamo en el jugador, porque no vino clip de Blender. Con la pierna y la cadera de Mixamo la pistola ya no se tuerce (T-749 puso `Root` en la máscara de la capa de arriba), así que **ninguno bloquea**; sirven para que el cuerpo sea el de Blender:

- Locomoción: `Loco_Idle`, `Loco_CaminarAtras`, `Loco_CaminarDer`, `Loco_CaminarIzq`.
- Arma larga, carro, moto, interacciones, puños, muertes, cobertura y gestos.

Formato igual que los anteriores: `PJ01_<NombreDelClip>.fbx`, 78 huesos de PJ01, sin malla, 30 fps, "Bake Animation" y sin "Add Leaf Bones". Se dejan en `imageassets/blender/pj01_clips/`.
