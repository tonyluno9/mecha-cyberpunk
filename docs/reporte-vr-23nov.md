# Reporte VR Quest 3 · 23-nov

Rama `vr/quest3` (subida, sin merge a main). Carpeta: `C:\Users\Angel Perez\Desktop\videojuego\wtVR`.
Build: `wtVR\Builds\VR\CyberCity.exe` (PC, OpenXR, mismas escenas que ConstruirJuego).

## Cómo se abre
1. Prende Quest Link (o Air Link) y deja el visor en el escritorio de Link.
2. Abre `CyberCity.exe`. Si hay visor, entra en VR solo. Si no hay visor, el juego sale normal (tercera persona).
3. `CyberCity.exe -novr` fuerza el modo normal. `-vrsim` usa el simulador de XR sin visor.

## Botones
| Acción | Botón |
| --- | --- |
| Caminar (1.62 m/s, hacia donde miras) | Stick izquierdo |
| Correr (4.53 m/s) | Mantener el clic del stick izquierdo |
| Girar 45° | Stick derecho a un lado (soltar para el siguiente) |
| Saltar | X |
| Interactuar (la E: Tino, máquinas, Doc, terminales) | B |
| Disparar | Gatillo derecho |
| Recargar | A |
| Golpear | Mover la mano rápido (> 1.5 m/s) contra el enemigo |
| Agarrar enemigo (solo con implante de brazos) | Grip, con la mano a < 0.4 m; soltar = lanzar |
| Elegir en menús | Láser del control derecho + gatillo |
| Pausa | Botón Menú del control izquierdo |
| Rueda (Tab) | Mantener Y, elegir con el láser |
| Vida y dinero | Mira la muñeca izquierda |

## Qué quedó
- **VR1 base**: OpenXR con perfiles Oculus Touch y Meta Quest Touch Plus. La cámara del jugador pasa a ser la cabeza, con los ojos a 1.72 m (PJ01 de 1.84) sea cual sea tu estatura. El cuerpo solo da sombra y se ven los controles de los Starter Assets. Hay viñeta al moverte y al girar. Si caminas en el cuarto, la cápsula te sigue y choca con las paredes. En VR se quitaron derribos, empujones, la rodada al caer, los temblores (Cinemachine apagado) y las cámaras de cinemática (se ven en la ventana del PC, no en el visor). Calidad: sombra a 30 m con 1 cascada, MSAA 2x, y fuera desenfoque, motion blur, aberración, grano y reflejos de espejos y charcos. El bloom y el color se quedan.
- **VR2**: la pistola real va en la mano derecha. La bala sale del cañón con el mismo daño, munición, sonido y trazos de siempre. A recarga. Las manos golpean con más daño cuanto más rápido, y vibran y suenan.
- **VR3**: con el implante de brazos, el grip agarra a SKULLs, pandilleros y objetivos del campo de tiro. Al soltar sale lanzado con la velocidad de tu mano ×3 (×4 con nivel 2) y se daña al chocar. Si lo golpeas con la otra mano mientras lo sostienes, sale volando. Después vuelve a su IA, o queda KO. Sin implante no agarra.
- **VR4**: los menús salen en un panel a 1.5 m frente a ti y se eligen con el láser y el gatillo. La pausa va en Menú, la rueda en Y y el HUD en la muñeca. Los carros y motos están fuera: sale el aviso "NO DISPONIBLE EN VR".

**Pruebas automáticas con el simulador:** las 29 de la tanda normal pasan. La de la ciudad real (`CiudadVRPruebas`) va en tanda aparte y también pasa. La partida `partida_auto.json` no se tocó: el md5 es el mismo antes y después.

## Qué no quedó / limitaciones
- Nada se probó con el visor puesto: solo con el simulador.
- Lanzagranadas, granada y rifle de plasma no disparan en VR. Solo disparan las armas de bala.
- El menú de Inicio usa un mini rig propio, que no está probado en Inicio.
- Los avisos "B interactuar" del HUD de pantalla no se ven en VR.
- Correr es manteniendo el clic del stick, no alterna.
- No hay recentrado manual de altura: la altura se calibra sola al arrancar.

## Qué debe probar Tony con el visor puesto
1. Abre el exe con Link. ¿Entra en VR? ¿El menú de inicio se ve frente a ti y puedes elegir con el láser?
2. En la ciudad, ¿la altura se siente de adulto alto (1.84)? ¿Ves tus controles y no tu cuerpo?
3. Camina con el stick hacia donde miras, corre con el clic y gira 45° con el stick derecho. ¿Marea? ¿La viñeta ayuda o estorba? ¿Hay temblor?
4. ¿Los fps se ven fluidos (72–90) en la calle con neón y tráfico?
5. ¿La pistola en la mano apunta derecho con el control? Si sale torcida, dime cuántos grados. Dispara a un SKULL en el campo de tiro y en la calle: ¿le baja la vida? ¿A recarga?
6. Dale puñetazos rápidos a un SKULL. ¿Le quita vida? ¿Vibra el control?
7. Con el implante de brazos, agarra a un SKULL con el grip y lánzalo contra una pared. Golpéalo con la otra mano mientras lo sostienes. Sin implante, comprueba que no agarra.
8. Botón Menú: ¿sale la pausa y la cierras con el láser? Y: ¿sale la rueda y eliges con el láser? ¿Se lee el HUD de la muñeca?
9. Entra con B a Tino, a una máquina y al Doc, y úsalos con el láser.
10. Acércate a un carro y pulsa B. Debe salir "NO DISPONIBLE EN VR" y no subir.
11. Ve una cinemática (por ejemplo la silla del Doc). En el visor debes seguir viendo desde tu cabeza, sin que la cámara te mueva.
