# Rueda de armas y objetos, y habilidades de prótesis (15-nov, Sesión A)

Diseño de Opus para T-991 (rueda) y T-992 (habilidades). Estilo H10 provisional hasta los mockups M30 (rueda) y M31 (barra de habilidades) de `imageassets/referencias/menus/`.

## Teclas nuevas a pie

| Tecla | Antes | Ahora |
| --- | --- | --- |
| Tab (mantener) | Siguiente objeto | Rueda de armas y objetos |
| 1 | Puños | [1] PIERNAS |
| 2 | Pistola | [2] BRAZOS |
| 3 | Escopeta | [3] CABEZA |
| 4, 5 | Fusil, francotirador | Nada |
| 6–0 | Objetos directos | Nada |
| F | Usar objeto elegido | Nada (los objetos se usan desde la rueda) |
| Rueda del mouse | Cambiar arma | Igual |

Las teclas de prueba (`DineroDePrueba`, `CalorDePrueba`, `ImplantesDePrueba`, `InventarioDePrueba`) no cambian: solo viven en escenas de prueba.

## Rueda (T-991)

- **Abrir**: al mantener Tab, solo a pie, vivo, con control, sin menú ni pausa abiertos.
- **Lugares**: 10 casillas en anillo, de 36° cada una, con los ángulos medidos en el sentido del reloj desde arriba.
  - Armas, en la mitad de arriba, de izquierda a derecha: Puños (288°), Pistola (324°), Escopeta (0°), Fusil (36°) y Francotirador (72°).
  - Objetos, en la mitad de abajo, de derecha a izquierda: Bebida (108°), Papitas (144°), Venda (180°), Café (216°) y Batería (252°).
  - Puños siempre está. Un arma que no tienes, o un objeto con 0 piezas, sale apagado y no se puede elegir.
  - Cada objeto muestra cuántos llevas (`x2`). Cada arma de fuego muestra sus balas.
- **Elegir**: el movimiento del mouse mueve un cursor virtual que nace en el centro, con un radio máximo de 160 px de referencia.
  - Fuera de la zona muerta (40 px), se resalta la casilla que señala el ángulo, si se puede elegir.
  - En el centro salen el nombre de lo resaltado y su detalle: balas, o lo que hace el objeto.
- **Soltar Tab**:
  - Con un arma resaltada, la equipa, igual que antes con su número.
  - Con un objeto resaltado, lo usa en el acto, igual que antes con 6–0.
  - Sin nada resaltado, solo cierra.
- **Mientras está abierta**:
  - `Time.timeScale = 0.25` y `fixedDeltaTime` escalado.
  - El fondo se oscurece con una capa negra al 55 %.
  - El jugador no mira con el mouse ni dispara.
  - Al cerrar se devuelve la escala que había. Si la pausa toma el control, la rueda se cierra sin elegir y no pisa la escala de la pausa.
- **Se cierra sola** si mueres, subes a un carro, entra una cinemática o se abre un menú.
- La lógica va en una regla pura (`ReglaRuedaArmas`: ángulo → casilla, disponibilidad, resultado al soltar) con pruebas de EditMode.
- **Opciones**: "Rueda de armas y objetos" se puede reasignar. Se quitan de la lista las teclas viejas de armas (1–5) y de objetos.

## Habilidades de prótesis (T-992)

Tres casillas fijas. Cada una tiene un medidor de energía:
- **Lleno**: lista.
- **Se vacía** durante la duración: activa.
- **Se llena** durante la recarga: no se puede usar.

Solo se activa si tienes el implante del espacio (`IImplantes.Nivel(espacio) > 0`). Con el nivel II dura más.

| Tecla | Casilla | Implante | Efecto mientras dura | Duración I / II | Recarga |
| --- | --- | --- | --- | --- | --- |
| 1 | PIERNAS | Exo de piernas (`EspacioImplante.Piernas`) | Correr ×1.5 y caminar ×1.3; súper salto: altura ×2.2 | 6 s / 10 s | 20 s |
| 2 | BRAZOS | Exo de brazos (`EspacioImplante.Brazos`) | Golpe a pie ×2.5 de daño y más empuje; tiempo de recarga del arma ×0.4 | 6 s / 10 s | 25 s |
| 3 | CABEZA | Visor (`EspacioImplante.Ojos`) | Ver enemigos a través de paredes (VisorRayosX, 45 m) | 8 s / 14 s | 30 s |

- Los multiplicadores se suman encima de los efectos pasivos que ya dan los implantes; esos no cambian.
- **Cambio de diseño:** el visor ya no ve a través de paredes todo el tiempo. Ahora lo hace solo mientras la habilidad CABEZA está activa (antes, T-866, era siempre).
- **Sin el implante**, la casilla sale apagada con "—". Al pulsar la tecla, el HUD avisa "NECESITAS EXO DE PIERNAS", "NECESITAS EXO DE BRAZOS" o "NECESITAS EL VISOR". No hace nada más.
- **Solo a pie.** En el carro las teclas no hacen nada, pero los tiempos siguen corriendo. Al morir se cortan las habilidades activas y quedan en recarga.
- **Datos**: un ScriptableObject `ConfigHabilidadesProtesis` en `Personaje/Datos/`. **Lógica**: una regla pura con estado Lista, Activa o Recargando, y pruebas de EditMode.
- **HUD**: una barra de 3 casillas abajo al centro, con los colores y la fuente del H10.
  - Cada casilla lleva la tecla real (leída del Input System), el nombre y el medidor.
  - Si está activa, brilla. Si está apagada, sale gris.
- **Al activar**: un aviso corto ("PIERNAS ACTIVAS") y un sonido, si hay uno a mano en el proyecto.

## Lo que se actualiza en las dos tareas

- **Tutorial ligero**: los pasos de cambiar de arma y de usar objetos ahora dicen "Mantén [Tab]". Si tienes un implante, se agrega el paso de [1]/[2]/[3].
- **Opciones de teclas**: Rueda, Habilidad PIERNAS, Habilidad BRAZOS y Habilidad CABEZA.
- **Ayudas del HUD H10 y textos de controles del inicio** (`ReglaControlesInicio`): con las teclas nuevas.
