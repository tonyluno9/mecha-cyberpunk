# Pedido de animaciones: escalera (T-1204)

Hoy, cuando el jugador sube una escalera, no hay animación: el personaje va pegado a la escalera con la pose de estar parado.
El código ya está listo para usar dos animaciones en cuanto existan. No inventamos animaciones: faltan estos dos archivos.

## Archivos que hacen falta

Se guardan en la carpeta `imageassets/animaciones/` con estos nombres exactos:

- `imageassets/animaciones/Escalera_Subir.fbx`
- `imageassets/animaciones/Escalera_Bajar.fbx`

Si solo llega uno, se usa para subir y para bajar. Lo ideal son los dos.

## Cómo bajarlas de Mixamo

1. Entra a Mixamo. Puedes usar cualquier personaje humanoide, o subir el PJ01.
2. Busca "Climbing Ladder" (subir) y "Climbing Down Ladder" o parecido (bajar).
3. Ajustes de descarga:
   - Format: FBX Binary (.fbx)
   - Skin: Without Skin
   - Frames per second: 30
   - Keyframe Reduction: none
   - "In Place": márcalo si la animación lo ofrece.
4. Que sea un solo ciclo de peldaño (una mano y un pie) que se pueda repetir en bucle.

## Qué pasa cuando los pongas

- Al abrir Unity, el proyecto los importa solo como animaciones Humanoid y los conecta. Si Unity ya estaba abierto, usa el menú
  Mecha Cyberpunk > T-1204 > Importar animaciones de escalera.
- La animación avanza con los metros que sube el jugador, no con el tiempo: si se detiene en la escalera, la animación se detiene.
  Una vuelta completa del clip equivale a 0,9 m de subida; se ajusta en `ConfigAnimacionEscalera`
  (`Assets/_Proyecto/Personaje/Datos/Resources/ConfigAnimacionEscalera.asset`, campo Metros por ciclo).
- El personaje sigue pegado a la escalera y arriba sale al techo como hasta ahora.

## Bajar desde el techo

Esto ya funciona sin animaciones: camina hasta la orilla del techo justo encima de una escalera, mirando hacia la calle, y presiona W.
El jugador se engancha a la escalera por arriba; con W sigue bajando hasta que sueltes la tecla, y después W sube y S baja como siempre.
Al llegar al piso con S (o al piso mismo mientras desciende) se suelta.
