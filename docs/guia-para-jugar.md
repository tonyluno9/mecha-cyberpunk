# Guía para jugar el build provisional (Tony)

Build de Windows con solo escenas de Tony (Alan aún no mezcla DirectorPrologo en Ciudad.unity; T-136/T-137 bloqueadas).
No está en git (`Builds/` está ignorado): se regenera con un script de editor temporal.

## Cómo abrirlo
`Builds/Provisional/MechaCyberpunk.exe` (en la raíz del repo). Doble clic. Arranca en CreacionPersonaje (título y creación de personaje).

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

Cómo moverse entre ellas: solo el flujo del juego cambia de escena (CambioEscena): título -> prólogo -> flashback -> prólogo, siguiendo la historia.
No hay menú para saltar a las demás; las escenas 3 a 7 están en el build pero no hay forma de llegar a ellas desde el juego. Para probarlas
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

(Una sección por bloque se agregará después.)
