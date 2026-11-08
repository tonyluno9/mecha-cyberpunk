# Pedido a Blender (9-nov): que PJ01 apunte en Unity igual que en Blender (T-737)

Para Tony. La causa de que PJ01 se rompiera al apuntar ya está corregida (el código movía el torso y las manos encima del clip, ver
docs/tareas/hechas/T-737-pj01-apuntar.md). Ahora el jugador se ve EXACTAMENTE como el clip que Unity calcula. Lo que falta es que ese clip,
reproducido en Humanoid sobre PJ01, no queda igual que lo que se ve en Blender: la pistola sale unos 17-23° a la izquierda, 13-19° hacia arriba y
con la corredera ladeada (Blender dice 0-1° y sin ladeo). Es la diferencia normal del retargeting Humanoid (cada esqueleto traduce los giros a su
manera). Hay dos formas de cerrarla; pido la primera y, si no se puede, la segunda.

## Opción 1 (la mejor): los clips ya pasados a PJ01, sin retargeting

Exportar de Blender, con la animación ya aplicada al esqueleto de PJ01 (los mismos 78 huesos y los mismos nombres: Hip, Waist, Spine01, Spine02,
L_Clavicle, L_Upperarm, ..., R_Hand), un FBX por clip, tal como los pasaste para revisar el agarre:

- Clips: Pistola_Apuntar, Pistola_Disparo, Pistola_Sacar, Pistola_Guardar, Pistola_ApuntarLateral_Derecha, Pistola_ApuntarLateral_Izquierda,
  Pistola_ApuntarAtras, Pistola_Recargar, Pistola_RecargarCaminar, Pistola_Dano_Frente, Pistola_Dano_Espalda, Pistola_Dano_Fuerte.
- Esqueleto de PJ01 con el mismo nombre de huesos que el modelo (Armature > BoneRoot > Hip ...) y la malla NO incluida (solo esqueleto + animación).
- Escala: la misma con la que exportas PJ01 (0.98 m); Unity lo agranda con el Scale Factor.
- 30 fps, una toma por archivo con el nombre del clip, "Bake Animation" activado y "Add Leaf Bones" desactivado.
- Nombrar los archivos `PJ01_<NombreDelClip>.fbx`.

En Unity se importan como Generic con el avatar de PJ01 (no pasan por Humanoid, así que no hay retargeting). Eso hay que montarlo en el
Animator (los demás clips siguen en Humanoid); lo hago yo cuando lleguen.

## Opción 2: pose T de referencia para que Humanoid acierte

Si prefieres no exportar clips por modelo, necesito la pose T exacta de los dos esqueletos para fijar el avatar:

- `PJ01_Tpose.fbx`: PJ01 con los brazos exactamente horizontales (hoy los brazos caen unos 12° en el archivo: el Humanoid de PJ01 toma esa caída como
  su "T"), las palmas hacia abajo y las piernas rectas, sin animación.
- `mixamorig_Tpose.fbx`: el esqueleto mixamorig (65 huesos) de los clips en su pose T de Mixamo, sin animación.

## Qué se midió (para que lo compares en Blender)

Con la pistola W01 en R_Hand (caso A de agarre.txt) y el clip Pistola_Apuntar puro sobre PJ01 en Unity, en el espacio del cuerpo:

| Momento | Giro a un lado | Cabeceo | Ladeo de la corredera |
| --- | --- | --- | --- |
| Pistola_Apuntar en Unity (Humanoid) | 22° a la izquierda | 19° arriba | 65-79° |
| Pistola_Apuntar en Blender (agarre.txt) | 0-1° | 0° | < 2° |

(Los valores de Unity están en docs/tareas/hechas/T-737-pj01-apuntar.md, medidos en Play con docs/capturas/pj01-apuntar-*.)

## Pose de apuntar con dos manos (T-741, medido)

Al apuntar con los tres implantes (pj01-implantes-todos-lvl2-apuntando-frente.png) la pose se ve rara: la pistola alta, a la altura de la cara, y la mano izquierda abierta y separada. No es un fallo del código: es el mismo retargeting.
Medido en Play con Pistola_Apuntar parado, en largos de brazo (hombro a codo más codo a mano):

| Esqueleto | Manos separadas | Ancho de la mano izq. (pulgar a meñique) |
| --- | --- | --- |
| Mixamo, la fuente del clip, en su propio esqueleto | 0.07 (las manos se tocan: agarre a dos manos) | 0.15 |
| PJ01 en Unity con Humanoid | 0.44 (unos 25 cm de hueco) | 0.22 |

La mano izquierda queda a 0.48 largos de brazo de la pistola. Humanoid copia ángulos, no posiciones: PJ01 tiene hombros y brazos más anchos y gruesos que el esqueleto de Mixamo, y con los mismos ángulos las manos no se juntan.
La pistola se ve alta porque el hombro y el brazo derecho de PJ01 quedan por delante del rostro con ese ancho; el cañón sí apunta a la mira (menos de 1.3 grados, T-738), solo que visto de frente lo que se ve es la boca del arma.
Lo que sirve de Blender: los clips Pistola_Apuntar, Disparo, ApuntarLateral_* y ApuntarAtras ya aplicados a PJ01 (opción 1 de arriba), con la mano izquierda sobre la empuñadura como en tu prueba de agarre.
Alternativa del lado del código, solo si no llegan: IK de la mano izquierda a un punto de la empuñadura (AgarreIzq ya existe en la pistola). Se descartó por ahora porque se pidió dejar la mano izquierda como la pone el clip.
