# T-737 PJ01 se rompe al apuntar
Dueño: Tony | Rama: tony/T-737-pj01-apuntar | Integrada en tony/s1-09nov

## Causa real (con evidencia)
El avatar Humanoid de PJ01 y el import NO eran el problema. Era ApuntadoIK, que con el ControladorBase (manosPorIK = false) escribía huesos ENCIMA del clip:
1. LateUpdate: giraba la columna (Spine) hasta 30 grados hacia el punto de mira (AjustarTorsoAlPuntoDeMira).
2. OnAnimatorIK: giraba la mano derecha (SetIKRotation) para llevar el cañón al frente.
3. OnAnimatorIK: pegaba la mano izquierda al AgarreIzq de la pistola (SetIKPosition y SetIKRotation, peso 1), pensado para la pistola vieja.
Medido en Play (ApuntarPJ01T737Pruebas): giro local de cada hueso contra el MISMO clip sobre una copia limpia de PJ01 (sin controlador ni scripts).
| Hueso | Antes (grados) | Después |
| --- | --- | --- |
| Spine | 31.5 | 0.0 |
| LeftUpperArm / LeftLowerArm / LeftHand | 50.4 / 76.3 / 38.8 | 0.0 |
| RightLowerArm / RightHand | 19.0 / 34.5 | 0.0 |
| Chest, UpperChest, Head, hombros, RightUpperArm | 0.0 | 0.0 |
(Pistola_Apuntar quieto; Disparo: máximo 47.8 antes y 0.0 después; caminar de lado: 38.7 antes y 0.0 después.)
Con ApuntadoIK apagado la diferencia es 0.0 en todos; la capa Dano (aditiva, sin clip en reposo) no mueve nada; IKPiesYMirada no mueve brazos (Pies ya estaba apagado).

Avatar de PJ01 revisado: 78 huesos, mapeo Hip/Waist/Spine01/Spine02(UpperChest)/Head/Clavicle/Upperarm/Forearm/Hand correcto (prueba EditMode), pose de referencia en T con brazos
casi horizontales (caen 12 grados), Translation DoF apagado, Arm/Forearm Twist 0.5, Feet Spacing 0. Falta el hueso Neck (el rig no lo trae; Head cuelga de NeckTwist). Máscaras: Arriba = Body, Head, brazos, dedos; Dano aditiva.
Con los músculos en cero, los brazos de PJ01 y de Loco_Idle quedan igual (0.63,-0.66,-0.41) frente a (0.63,-0.64,-0.43): el mapeo es coherente.

## Qué cambió
- ApuntadoIK: propiedad ClipPuro. Encendida, no escribe ningún hueso (ni torso, ni mano derecha, ni mano izquierda, ni manos por IK). El punto de mira se sigue guardando.
- AnimacionJugador.ClipsDeTony enciende ClipPuro al jugador (igual que ya apagaba el IK de pies). Aliados y enemigos no cambian.
- El tiro sigue saliendo de la boca hacia la mira (PistolaAPie no se tocó).
- Pruebas que medían las correcciones del IK en el jugador las encienden con ConIKDeApuntadoEncendido (BaseJugadorAPie); AgarrePJ01T736 pide solo que el cañón mire hacia delante.

## Lo que queda distinto de Blender (pendiente, docs/pedido-blender-09nov.md)
El clip puro en Humanoid deja la pistola (agarre T-736) así, en el espacio del cuerpo: quieto 22 grados a la izquierda y 19 arriba; disparo 14 a la derecha y 22 arriba; lateral 16 a la izquierda y 9 arriba;
corredera ladeada 65-79 grados. Blender dice 0-1 grados. Es retargeting Humanoid; se intentó cambiar la descripción de origen del clip y la pose de referencia de PJ01 y no acerca (un experimento descartado).
El pedido a Blender: clips ya aplicados a PJ01 (Generic) o poses T exactas.

## Pruebas
- EditMode ApuntarPJ01T737EditPruebas 5/5 (avatar, import, T, máscaras, ClipPuro). EditMode total 2116/2117 (la roja es CarrosS2nPruebas.Importador_SinZip, de la sesión 2).
- PlayMode Personaje (filtro): ApuntarPJ01T737Pruebas 5/5 (quieto, disparo y lateral contra el clip puro, máximo 0.0 grados), más T-681, ArmaEnMano, T-736, ArmasNuevas, JugadorVisual, T-733, T-734, T-735, T-732 y T-724 en verde.
- Capturas: docs/capturas/pj01-apuntar-{quieto,disparo,lateral}-{frente,lado}-{antes,despues,clip}.png (clip = el clip puro sin materiales).
