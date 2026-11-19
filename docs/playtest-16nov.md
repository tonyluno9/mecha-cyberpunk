# Mejoras del 16-nov (Tony) — lista que se va llenando

## 1. Cámara al apuntar por encima del hombro (como GTA)
Referencias: `imageassets/referencias/look/CAM04_SinApuntar.png` y `CAM05_Apuntando_Hombro.png`.
- Sin apuntar (CAM04): el personaje casi centrado, un poco a la izquierda, cuerpo completo, con la mira chica visible delante.
- Al apuntar (CAM05): la cámara se acerca y se corre al lado derecho del personaje (hombro derecho, el personaje queda a la izquierda
  de la pantalla, de la cintura para arriba), para que el cuerpo NO tape hacia dónde apuntas y la cruceta quede libre en el centro.
  Transición suave, que no atraviese paredes; si hay pared a la derecha, cambia al hombro izquierdo.
- Reemplaza el "personaje centrado al apuntar" de antes.

## ESTA NOCHE

### Sesión A — jugador, combate, IA, vehículos, Doc
2. **El juego empieza de día** (nueva partida y la demo arrancan de día; el ciclo sigue normal).
3. **Golpes con auto-giro:** al apretar golpear, el personaje se gira solo hacia el oponente más cercano al frente (radio corto) y lo golpea.
4. **NPC con las mismas animaciones del jugador:** bandidos (SKULLS, SNAKES), peatones, policía, guardias de Nova, meseras, todos los
   personajes humanos usan los clips de caminar, correr, saltar, idle, etc. de PJ01 (Humanoid). Que nadie camine con clips viejos o raros.
5. **Doc:** ya no regala el exo de piernas directo. Te dice "Elige tu prótesis, te regalo la de piernas" y en el menú de la clínica el
   EXO DE PIERNAS I aparece con precio **GRATIS**; la compras tú. Quitar el bloqueo tipo "Liga 3": al comprar el nivel I de cualquier
   prótesis se puede comprar enseguida el nivel II.
6. **Patrullas** que a veces se quedan raras o atoradas: arreglar.
7. **Ruedas de todos los carros** (patrullas incluidas) al tamaño correcto de su hueco. Revisar que la corrección de la moto (clips de
   Blender y bajar sin tirones T-976) esté aplicada en el build.
8. **Drones de la policía:** vuelo natural (inercia, inclinación al girar, frenado suave), como si alguien lo manejara.
9. **Enemigo de Nova** (Capitán y guardias): si lo golpeas, se defiende y ataca fuerte; es poderoso.

### Sesión B — mundo, techos, casino
10. **Techos:** en puntos de las casas, escaleras de mano para subir y estar en los techos (verificar que funcione en el juego).
11. **Calle infinita** junto a la calle del Doc (assets repetidos): rehacerla con cuadras, esquinas, lámparas y casas armadas como el kit.
12. **Edificios lejanos nítidos:** las torres de Nova y los edificios grandes se ven borrosos o tapados de lejos; que se vean nítidos e
    impresionantes con sus luces y letreros (ajustar niebla/atenuación por distancia solo para ellos).
13. **Calle de Nova:** eliminar por completo las escaleras raras (y la pasarela si sin escaleras no tiene sentido).
14. **Casino:** la mesera del bar de cambio de fichas está al revés (mira la pared): girarla. Sergio (el programador/hacker): silla al
    revés y lejos de la computadora; moverlo a otra esquina, sentado bien frente a su compu. Las sillas de las mesas, de frente a su
    mesa. La puerta de entrada vista desde dentro tiene texturas feas: que tenga la misma textura que el resto del casino.
    (Lo demás del casino le encanta: no tocar; las lámparas del techo están bien.)

## PENDIENTES (no esta noche)
- Pantalla de carga tipo GTA con imágenes animadas.
- Mejorar la animación de la intro del asalto.
- Blender: cómo camina el personaje y la pose estática (idle).
- Sonidos por mejorar.
- Asset del maletín (chat de arte → Tripo).
- Asset de máquinas tragamonedas (chat de arte → Tripo).
- Animaciones de parkour.
- Algunas calles raras: generar referencias de una calle bien armada y rehacerlas.
- Mejorar la animación de robar un carro en algunas situaciones.
- Mejorar las pantallas de ARRESTADO y HAS MUERTO.
- Más letreros para los edificios (si Tony quiere).
- (de antes) Rueda de armas y habilidades con los mockups M30/M31; tienda de armas con imagen (M32); armas, prótesis y enemigos nuevos
  (pedido-arte-14nov); ícono H33.
