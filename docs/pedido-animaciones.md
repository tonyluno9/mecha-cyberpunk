# Animaciones de escalera (T-1204)

## Estado al 17-nov

Ya están en el juego las cinco animaciones de escalera hechas en Blender sobre PJ01:

| Archivo (en `imageassets/blender/pj01_clips/`) | Cuándo se juega |
| --- | --- |
| `PJ01_Escalera_Entrar.fbx` | Al engancharse a la escalera desde el piso. |
| `PJ01_Escalera_Subir.fbx` | Subiendo. Avanza con los metros que sube: si se detiene, la animación se detiene. |
| `PJ01_Escalera_Bajar.fbx` | Bajando, igual que la de subir. |
| `PJ01_Escalera_SalirArriba.fbx` | Al pasar de la escalera al techo. El paso dura lo que el clip. |
| `PJ01_Escalera_SalirAbajo.fbx` | Al soltarse abajo, al llegar al piso bajando. |

- Se importan igual que los demás clips de PJ01: Humanoid con el avatar de PJ01 y Scale Factor 1.8367. Se copian a `Assets/Arte/Personajes/PJ01/Clips/`.
- Subir y Bajar se repiten en bucle; Entrar, SalirArriba y SalirAbajo no.
- Una vuelta completa de Subir o Bajar equivale a 0.9 m de escalera. Se ajusta en
  `Assets/_Proyecto/Personaje/Datos/Resources/ConfigAnimacionEscalera.asset` (campo Metros por ciclo).

## Si cambias o agregas un clip

- Guarda el archivo nuevo con el mismo nombre en `imageassets/blender/pj01_clips/`.
- Al abrir Unity, si falta algún clip en la config, se importa solo. Para reimportar los que ya estaban, usa el menú
  Mecha Cyberpunk > T-1204 > Importar animaciones de escalera.
- También acepta animaciones de Mixamo para subir y bajar: `imageassets/animaciones/Escalera_Subir.fbx` y `Escalera_Bajar.fbx`
  (FBX Binary, Without Skin, 30 fps, "In Place"). Si existen los de PJ01, se usan los de PJ01.

## Bajar desde el techo

Párate en el techo donde te deja la escalera (o junto a su orilla), mirando hacia la calle, y presiona W. El jugador se engancha a la
escalera por arriba y baja mientras sostengas W. Al soltar W, la escalera vuelve a lo normal: W sube y S baja. Al llegar al piso se suelta.

## Pendiente de revisar jugando

- Durante SalirArriba el cuerpo pasa por encima del pretil (`docs/capturas/t1204/saliendo-arriba.png`). Revisa si se ve bien en tu techo favorito.
