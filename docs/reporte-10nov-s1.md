# Reporte 10-nov, sesión 1

Trabajo en `.sesiones/s1`. Todo publicado en `main` con merge `--no-ff`.

## T-749: clips de Blender en PJ01 (merge 81d317a)
**Se queda Humanoid con el avatar de PJ01.** No hace falta Generic. Medido contra `imageassets/blender/pistola/W01_Pistola_agarre.blend`:

- Cuatro causas, ninguna era retargeting: el agarre (R_Hand en Unity va girado 180° en Y respecto a agarre.txt), el cuerpo de los clips miraba a -Z, la cadera venía de la capa base y giraba el torso hasta 46° (`Root` entró en `MascaraTorso`), y el retroceso buscaba el nombre viejo del clip.
- Clip Humanoid contra el mismo clip Generic: peor cañón 0.46°, peor manos 0.13 cm (10 clips, 4 tiempos).
- En juego, sin compensación: cañón a la mira 0.7-3°, ladeo hasta 2.7°, manos juntas, pies en el piso.
- Capturas revisadas: docs/capturas/pj01-t749-*.png. Diagnóstico `DiagPiesT749` quitado. Clips que faltan: docs/pedido-blender-10nov.md (no bloquean).

## B7 (T-910, rama tony/T-910-b7-piezas-calle)
Semáforo ES01 con ciclo (verde 8 s, ámbar 2 s, rojo 10 s, cruces en contrafase), basura BS01a-e, pasarela PS01 como K07 con colisión, puerta de cristal PC01 corrediza en el casino, lata O01 en la bandeja de la expendedora. Capturas revisadas: docs/capturas/t910-*.png.

## B2
Ya estaba hecho en T-743: Carro_V07, V08 y C19 llevan `Ruedas_T743` con RU02; MotosPorPiezasT743Pruebas 28/28. No hubo código nuevo.

## Pruebas
- Tras los merges a main: EditMode 2267/2267; PlayMode de T-749 11/11 y de T-910 4/4 (`Retarget`, `ApuntarClips`, `PiezasCalleT910`, `CasinoPuertaS2o`). Corridos con el resto del área aparte.
- PlayMode completo sobre T-749: pasan completas Arena, Casino, Combate, Core, Creditos, Demo, Economia, Mecha, ModoVideo, Pelea, PersonajeVisual y Progresion.Misiones. **Con fallos:** Personaje 351/398 (45), IA 85/87, Mundo 187/200, Progresion.Implantes 30/40, Progresion 140/149, Sonido 15/16, Trafico 97/98, UI 195/224, Vehiculos 111/116.
- Personaje se comparó con la base `f5749cb`: la base falla 47 y T-749 falla 45. Lo único nuevo es `ApuntarClipsPJ01T749Pruebas`, que pasa aislada y cae dentro del grupo que depende del orden de las pruebas (como `AgarreFinalPJ01T738`, que aislada también pasa y en el área completa falla en la base). T-749 arregla 3. **Las demás áreas con fallos no se compararon contra la base**: por reportes anteriores coinciden con fallos ya conocidos (cámara libre, horarios del mercado, recorridos, HUD), pero no está medido en esta corrida.
- T-910, Mundo 190/203; las 13 son el mismo grupo ya conocido.

## Por revisar a mano
- Probar subir y bajar la escalera de la pasarela con el jugador: la colisión se comprobó con rayos, no caminando.
- La escalera queda pegada a la entrada del casino VivaCoin y deja la entrada libre, pero se nota en cámara.
- Las pruebas que fallan en el área completa y pasan solas (Personaje): el proceso de ese orden no se investigó.
