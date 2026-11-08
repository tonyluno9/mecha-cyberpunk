# Tareas: carros, motos y fachadas por piezas (8-nov)

Origen: los zips de Tripo están en `imageassets/modelos3d/piezas/` (copia de Descargas\MODELOSNUEVOS). Las puertas de carro
hechas en Blender (con su espejo) están en `imageassets/blender/puertas/`. **Paso 0 en cada tarea:** cerrar Unity y Unity Hub, confirmar
que no quede Unity.exe, borrar Temp/UnityLockfile y repetirlo si vuelve.

| Zip / archivo | Qué es |
| --- | --- |
| V01CARROZA.zip | Carrocería de V01a (carro viejito), sin puertas ni ruedas |
| V01BCARROZA.zip | Carrocería de V01b (carro pro) |
| V05CARROZA.zip | Carrocería de V05 (patrulla sedán) |
| blender/puertas/V01A_, V01B_, V05_Puerta_Izquierda/Derecha.fbx | Puertas delanteras de esos 3 carros |
| RU01COCHE.zip | La llanta única de TODOS los carros |
| RU02MOTO.zip | La llanta única de TODAS las motos |
| C19CUERPO.zip | Cuerpo de la moto C19 sin ruedas (confirmado por Tony; el nombre "drone" de las texturas lo pone Tripo, ignorarlo) |
| C01FACHADA.zip, C02FACHADA.zip, C03.zip | Fachadas C01, C02 y C03 con el hueco de la puerta vacío |
| P11FACHADA.zip | Entrada del edificio de la casa sin reja (para Hueco_Entrada_P11) |
| P12HOSPITAL.zip | Fachada del consultorio del Doc sin puerta |
| PU01.zip | Puerta metálica (vivienda / consultorio) |
| PU03.zip | Puerta de reja (entrada de la casa, P11) |
| PU04.zip | Puerta metálica del consultorio del Doc (P12) |
| GARAGEPUERTA.zip | Cortina/puerta de garaje de lamas (para locales y el taller) |
| PJ01.zip | El PJ01 de siempre (ya está en imageassets/modelos3d/personajes/PJ01): no duplicar |

## B1. Carros armados V01a, V01b y V05
- Un prefab por carro con hijos: `Carroceria`, `Puerta_Izq`, `Puerta_Der`, `Rueda_DI`, `Rueda_DD`, `Rueda_TI`, `Rueda_TD` (las 4 con RU01
  escalada al hueco de cada carro), más `Asiento_Conductor` y `Punto_Puerta`. Mismo tamaño y collider que el carro actual.
- Pivote de cada rueda en su centro y de cada puerta en su bisagra (si el FBX no lo trae, poner un padre vacío en la bisagra).
- Código: las ruedas giran según la velocidad y las delanteras doblan con la dirección; la puerta del lado por donde subes se abre y
  se cierra con DOTween sincronizada con Carro_Entrar / Carro_Salir. Cristales: sin vidrio, para ver al conductor.
- Reemplazar estos 3 carros en tráfico, estacionados y patrullas. Los demás carros siguen como están hasta que lleguen sus piezas.

## B2. Motos con RU02
- Las motos V07, V08 y C19 usan RU02 como ruedas separadas que giran (C19CUERPO es el cuerpo de C19).

## B3. Fachadas y puertas
- C01, C02 y C03 del kit con las fachadas nuevas y su puerta (PU01 o GARAGEPUERTA según sea vivienda o local) que se abre con E donde sea
  una entrada real; en las demás, la puerta queda cerrada.
- P11 en Hueco_Entrada_P11 con PU03 que se abre. P12 en el consultorio del Doc con PU04 que se abre.
- Actualizar el kit de manzanas (KitS2c) y volver a correr sus colocaciones.

## B4. Implantes (A9)
- Ya llegó `imageassets/blender/implantes/PJ01_Implantes.fbx`: hacer la tarea A9 de docs/tareas-pj01-animaciones.md.

## Pendiente de Tony (no esperar)
- Carrocería y puertas de V02, V03, V04, V06, V09 y V10; cuerpos de V07 y V08.
