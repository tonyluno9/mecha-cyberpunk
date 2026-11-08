# T-741 PJ01 verificación final (A7)
Dueño: Tony | Rama: tony/T-741-pj01-verificacion | Integrada en tony/s1-09nov

## Capturas pj01-verif-* (38 imágenes, 19 casos de frente y de lado; las abrí todas, de dos en dos)
apuntar, apuntar-lateral-derecha, apuntar-lateral-izquierda, apuntar-atras, agachado-sin-pistola, agachado-con-pistola, sacar, guardar, dano-frente, dano-espalda, dano-fuerte,
rodar-1, rodar-2, rodar-3, rodar-final, caminar-sin-implante, caminar-con-implante, correr-sin-implante, correr-con-implante. Cada una va con su comprobación del estado del Animator en VerificacionPJ01T741Pruebas (7 pruebas).
Lo que se ve: apuntar en los cuatro sentidos con la pistola al frente y dos manos; agachado con la pistola solo al apuntar; sacar y guardar con la pistola saliendo de la cadera apuntando al frente; los tres daños son un estremecimiento corto sobre la pose de apuntar (poco visible en foto fija);
la rodada completa (agacha, vuelta, queda en cuclillas); con implante los brazos van separados y los exoesqueletos siguen la pose sin atravesar el cuerpo.

## La pose de apuntar "rara" (pj01-implantes-todos-lvl2-apuntando-frente.png): es el retarget, no el código
Medido en Play (Medida_PoseDeApuntar_PJ01ContraElEsqueletoDeMixamo): con Pistola_Apuntar las manos quedan a 0.07 largos de brazo en el esqueleto de Mixamo (se tocan, agarre a dos manos) y a 0.44 en PJ01 con Humanoid (unos 25 cm de hueco);
la mano izquierda queda a 0.48 largos de la pistola y se ve más ancha (0.22 contra 0.15). Humanoid copia ángulos y PJ01 tiene hombros y brazos más anchos. El cañón sí apunta a la mira (menos de 1.3 grados, T-738); visto de frente se ve la boca del arma, de ahí lo de "casi vertical".
Se documentó en docs/pedido-blender-09nov.md (sección "Pose de apuntar con dos manos") con la medida y lo que se pide de Blender. Opción de código si no llega: IK de la mano izquierda al AgarreIzq de la pistola (no se hizo: se pidió dejarla como el clip).

## RecorridoDemoPruebas: causa y arreglo (era la prueba)
Síntoma: "El bot venció a los SKULLS y al jefe sin que la prueba los matara". Medido con registros nuevos en la prueba: en la pelea del Tianguis el bot apuntaba bien (error de 0.3 a 0.6 grados), pero el arma activa era Puños y no disparó ni un tiro
(disparos 0, balas siempre 10, 7 enemigos vivos) mientras perdía vida y los SKULLS no caían. El bot pide la pistola con la tecla 2 solo una vez al empezar la pelea; ArmasAPie ignora el cambio mientras hay un golpe en curso o el control no está activo, y el bot nunca la reintenta.
Es un comportamiento normal del juego (cambiar de arma durante un golpe se ignora), así que se corrigió la prueba: PelearConPistola vuelve a pedir la pistola cada segundo si no está en la mano (necesitó 3 intentos). Resultado: Tianguis de 202 s (agotado, con rescate) a 25 s; jefe de 182 s a 9 s; la prueba completa pasa sin rescate (2/2, incluida NavMesh_LugaresDeLaHistoria_TienenCamino).
La prueba además imprime disparos, impactos, error de apuntado, arma activa y estado del clic en sus líneas [T-476]. Nota: durante el recorrido la prueba reporta (sin fallar) BUG 1 a 3 de la tienda del CIC (no abre, no cobra, Esc no cierra) en el tramo 09: no se investigaron aquí.

## Pruebas
- EditMode 2134/2134. Compilación OK.
- PlayMode Personaje por área (ver el reporte): todas las clases de PJ01 (T-732 a T-741), reglas del Animator, eventos de sacar y guardar, daño aditivo y agarre.
- RecorridoDemoPruebas (explícita, se corre con -testFilter): pasa.
