# Sonidos: qué prender después (sugerencias, en orden)
Desde el 5-nov el juego tiene solo los sonidos necesarios (ver docs/plan-noche-05nov.md, Paso 1). Estos se prenden uno por uno desde el ScriptableObject de volumen, probando cada vez que no se amontone. Ya existen los archivos.

1. **AMB_CALLE_NOCHE** — un solo ambiente bajito de ciudad de noche; da vida sin estorbar. (Luego, si funciona: AMB_TIANGUIS en El Tianguis y AMB_INTERIOR_CASA en la habitación.)
2. **VEH_PUERTA_ABRIR / VEH_PUERTA_CERRAR** — al subir y bajar del carro; ahora que hay animación se siente.
3. **ARM_BALA_IMPACTO_CONCRETO** — cuando fallas el tiro; ayuda a saber dónde pegó.
4. **PAN_RIVAL_ALERTA** — un "¡Ey!" cuando un SKULL te descubre; avisa del peligro.
5. **PAN_JEFE_ENTRADA** — la entrada del jefe de los SKULLS; momento importante de la demo.
6. **UI_OBJETIVO_NUEVO / UI_OBJETIVO_CUMPLIDO** — guía al jugador sin leer.
7. **VEH_FRENO_DERRAPE** — al frenar fuerte o dar vuelta rápido.
8. **JUG_PASO_ASFALTO** — pasos, pero bajitos y solo del jugador (nunca de todos los NPC).
9. **CAL_SUBE_NIVEL** — aviso de que sube el calor.
10. **ECO_DINERO_RECOGER** — al recoger Biits.
11. **MUN_PUERTA_CASA_ABRIR / CERRAR** — puertas de los interiores.
12. **AMB_CASINO** dentro de VivaCoin y **AMB_CLINICA** en I+D+I.
No volver a prender sin probar: voces de NPC, jadeos, claxon del tráfico, hologramas, neón, perros, aire acondicionado, disparos lejanos y UI_TEXTO (son los que hacían spam).
