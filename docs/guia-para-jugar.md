# Guía para jugar el build provisional (Tony)

Build de Windows con solo escenas de Tony (Alan aún no mezcla DirectorPrologo en Ciudad.unity; T-136/T-137 bloqueadas).
No está en git (`Builds/` está ignorado): se regenera con un script de editor temporal. Actualizado al bloque 5 (misiones y reputación, T-181 a T-189).

## Cómo abrirlo
- `Builds/Provisional/MechaCyberpunk.exe` (en la raíz del repo). Doble clic. Arranca en CreacionPersonaje (título y creación de personaje).
- `Builds/Provisional_Misiones/MechaCyberpunk.exe`: las mismas 9 escenas, pero arranca directo en Pruebas_Misiones. Úsalo para probar el bloque 5.

## Escenas incluidas (en el orden del build)
| # | Escena | Qué es |
| --- | --- | --- |
| 0 | CreacionPersonaje | Título y creación de personaje; arranca aquí |
| 1 | Pruebas_Prologo | El prólogo (ciudad de prueba de Tony) |
| 2 | Pruebas_Flashback | El flashback |
| 3 | Pruebas_Tony | Banco de pruebas del mecha y la pelea |
| 4 | Pruebas_Economia | Taller, banco, mercado negro, mostrador de peleas |
| 5 | Pruebas_Calle | Calle de prueba con rival |
| 6 | Pruebas_APie | Jugador a pie |
| 7 | Pruebas_Tutorial | Tutorial |
| 8 | Pruebas_Misiones | Misiones, reputación, teléfono y favores (el bloque 5); en Provisional_Misiones es la escena 0 |

Cómo moverse entre ellas: solo el flujo del juego cambia de escena (CambioEscena): título -> prólogo -> flashback -> prólogo, siguiendo la historia.
No hay menú para saltar a las demás; las escenas 3 a 8 están en el build pero no hay forma de llegar a ellas desde el juego. Para probarlas
una por una, cambia el orden en el script de build (la primera escena es la que arranca) o usa el editor.

## Controles
A pie (ControlesAPie): 
| Acción | Tecla |
| --- | --- |
| Moverse | W A S D |
| Mirar | Ratón |
| Correr | Shift izquierdo |
| Saltar | Espacio |
| Interactuar (subir/bajar del mecha, bancos, mostradores) | E |
| Atacar | Clic izquierdo |
| Puños / pistola | 1 / 2 |
| Recargar | R |
| Soltar/recuperar cursor | Esc |

En el mecha (ControlesMecha):
| Acción | Tecla |
| --- | --- |
| Moverse / mirar | W A S D / ratón |
| Golpear / bloquear | Clic izquierdo / clic derecho |
| Soltar cursor | Esc |
| Confirmar | F |
| Levantar | Espacio |
| Cristal | Q |
| Llamar al mecha | M |
| Habilidad del brazo / del torso (solo en pelea) | 1 / 2 |

Habilidades: el brazo izquierdo y el torso con habilidad (BRAZO IZQ. MILITAR I, TORSO TECH I...) gastan una munición por uso y se enfrían. La munición
(misiles, celdas, cargas, combustible) se compra solo en el mercado negro, pestaña COMPRAR; no se vende. Sin munición solo quedan los brazos.

Pantallas (menús de piezas, apuestas, tienda): flechas y Enter para elegir, Esc para cerrar. Las ayudas de controles (T-134) salen solas
en pantalla según lo que hagas. Teclas de prueba (solo en escenas de prueba): P iniciar pelea, H hacerse daño, G usar kit, T entrar a la arena,
F5/F9 guardar/cargar, 8 y 9 y 0 cargan deuda/brazo/kit o calor, J saltar reloj, L vencer rival de la liga, O siguiente objetivo.

## 10 cosas que Tony debe probar
1. El título y crear un personaje: que se pueda completar y pase al prólogo.
2. Que el prólogo avance al flashback y regrese sin quedarse trabado.
3. Caminar, correr y saltar a pie: sensación y cámara.
4. Subir al mecha con E, encenderlo y bajar; llamarlo con M.
5. Golpear y bloquear en una pelea (P para iniciarla): peso y daño.
6. Los kits de reparación en plena pelea (G) y reparar en el banco.
7. Comprar y vender piezas en el mercado negro (flechas y Enter).
8. Apostar en el mostrador de peleas y ver el pago.
9. Guardar con F5, cerrar el juego, abrir y cargar con F9.
10. Fluidez: que no haya tirones ni errores al cambiar de escena.

## Qué probar del bloque 5 (en Provisional_Misiones)
1. Teléfono (T): ¿se siente rápido o estorba? Cambia de pestaña con las flechas y ciérralo con Esc o T.
2. Misión de barrio con Doña Lupe (E junto a ella): ¿la flecha y el texto del HUD te llevan sin perderte? ¿Recoger y entregar con E se siente natural o falta algo?
3. Los tres vecinos (Doña Lupe, el taquero, la vecindad): ¿se sienten de barrio?
4. Contrato corporativo por teléfono de día: ¿el aviso "BARRIO −12" te hace pensar dos veces antes de aceptar otro? Prueba también de noche ("NADIE CONTESTA").
5. De noche, el contacto del bar (E): ¿se siente escondido y de noche? Acepta el robo de la bodega y escapa: ¿es tensión real o solo esperar a que baje el calor?
6. La pandilla del callejón: ¿se siente como defender al taquero? ¿Vender la chatarra después se siente como el cierre del robo?
7. Favores: ¿se sienten como algo que el barrio te debe? ¿El escondite te salvó en un robo? ¿30 % de descuento en una compra es generoso o tacaño?
8. Reputación: ¿JUSTICIERO y ENEMIGO suenan al juego? ¿Sentiste el jalón entre barrio y corporaciones? Mira el grafiti GRAFITI del muro oeste.
9. F5 y F9 con una misión en curso: la misión se cancela sin castigo y no sale un aviso falso de reputación.
10. En general, 20 minutos de un día y una noche: ¿las misiones dan ganas de hacerlas entre peleas o son relleno?

## Misiones y reputación (Pruebas_Misiones)
Teléfono: T (contratos de día, misión, reputación, favores). Flechas ← → cambian de pestaña, ↑ ↓ y Enter eligen, Esc o T cierran. CONTRATOS lista las
corporativas solo de día (de noche "NADIE CONTESTA"); MISIÓN muestra la que llevas y permite abandonar (Enter dos veces, castiga); REPUTACIÓN muestra
las dos barras (barrio y corporaciones); FAVORES muestra DESCUENTO, INFORMACIÓN y ESCONDITE (máximo 3 de cada uno).
Dónde se aceptan: los encargos de barrio con E junto a los vecinos (Doña Lupe, el taquero, la vecindad), las corporativas por teléfono de día, y las
ilegales y los robos con E en el contacto del bar, de noche. Una misión a la vez; la flecha y el texto del HUD te llevan paso a paso (E para recoger y entregar).
Los favores: DESCUENTO baja 30 % tu siguiente compra en el mercado negro; INFORMACIÓN abre EL SOPLO con el contacto del bar; ESCONDITE se usa con E
en un escondite del barrio (azotea, lavandería) cuando hay calor: lo borra y cumple el paso de escapar. Pruebas_Misiones es la escena para probarlo todo
(J pasa al día siguiente a las 8:00; F5 guarda y F9 carga: la misión en curso se cancela sin castigo).



## Implantes y la clínica (Pruebas_Implantes)
Escena para probarlos: `Pruebas_Implantes` (Play, de día a las 10:00; hay pista de 30 m, cajas de salto, muñecos, espejo, taller, mostrador de peleas y casa).
- **Clínica**: de día (8:00 a 20:00) camina al mostrador "CLÍNICA" y pulsa E. Flechas arriba/abajo eligen la pieza (OJOS, BRAZOS, PIERNAS, SISTEMA NERVIOSO), Enter pide confirmar, Enter otra vez instala, Esc sale. Cada operación cuesta dinero y 2 horas. Los de nivel 2 se abren en la liga 3 y cobran solo la diferencia. De noche dice "CERRADA".
- **Qué hace cada uno**: OJOS muestra la vida del rival y avisa su próximo golpe (en la cabina y a pie); BRAZOS pega más fuerte a pie; PIERNAS corre más rápido y salta más alto; SISTEMA NERVIOSO acelera tu golpe y tu guardia en el mecha. En el espejo de la casa (E) los ves puestos.
- **Pandilla de noche**: desde las 21:00 hay 3 pandilleros a pie en el sitio de la pandilla; persiguen, nunca pegan más de 2 a la vez y pagan al caer. De día no están.
- **Si caes en la calle** (a pie, de noche): despiertas en la camilla de la clínica a las 05:00, el doc te cobra $500 (puedes quedar en deuda), el calor se borra y el objetivo dice VUELVE A CASA; el mecha, si estaba en la calle, vuelve dañado al taller.
- **Teclas de prueba** (`ImplantesDePrueba`): 3 instala los 4 implantes de nivel 1 sin cobrar, 4 los de nivel 2, 5 los quita. 1 y 2 cambian entre puños y pistola; F5 guarda y F9 carga.
