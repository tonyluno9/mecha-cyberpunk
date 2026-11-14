# Pedido de arte: SKULLS que molestan a un civil (S2 pandillas)

Hoy los cinco grupos (`S2_PandillasAcoso`, colocados por `Assets/Arte/Editor/ColocarS2Pandillas.cs`) usan los clips que ya existen. Faltan estos para que se vea bien.

## Clips de animación que faltan

- `Skull_Empujon` (SKULL): empujón con las dos manos al frente, corto (0,5 s). Hoy se usa `Golpear(false)` (puño) y el civil retrocede con `Herir`.
- `Civil_Asustado_Bucle` (civil): encogido, con las manos arriba y la cabeza girando de un lado a otro, en bucle. Hoy se achica la escala vertical al 92 % y se queda en Idle.
- `Civil_Suplicar` (civil): manos juntas pidiendo piedad, de unos 2 s. Hoy no hay nada.
- `Civil_Huir` (civil): correr asustado, con los brazos pegados al cuerpo y mirando hacia atrás. Hoy usa el clip de correr normal (`Loco_Correr` o el que tenga el visual); si ese clip se ve bien para huir, este no hace falta.
- `Civil_Caer_Empujon` (civil): tropieza hacia atrás y cae sentado, y `Civil_Levantarse`. Hoy se usan `Tumbar()` y `Levantar()` (el mismo clip que al quedar KO).

## Notas

- Los SKULLS se ven casi negros en las capturas de noche (su material con el color de los datos). No es de este cambio, pero conviene revisar el material de `Visual_SkullHombre` con luz baja.
- El grito sobre la cabeza ("¿¡Quién fue!?", "¡Ya cállate!", "¡Gracias!") es texto 3D de prueba; faltan sonidos de voz para esos tres momentos.
