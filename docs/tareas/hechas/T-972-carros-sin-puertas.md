# T-972: carros sin puertas

B3 de docs/tareas-integrar-blender-10nov.md. Decisión de Tony (9-nov): V01a, V01b, V05 y la patrulla van sin puertas ni animación de puerta.

- Prefabs (Carro_V01a/V01b/V05, Patrulla): borrados Puerta_Izq y Puerta_Der (2 objetos cada una) y el componente PuertasCarro, con `Vehiculos/Editor/QuitarPuertasT972.cs` (idempotente).
- Código: borrado `PuertasCarro.cs`; `AnimacionCarro` ya no manda mensajes de puerta (subir, manejar y bajar sin cambios). Los creadores T-742 ya no arman puertas.
- Assets `Assets/Arte/Vehiculos/Piezas/V*/_Puerta*`: se dejan, `Menu_Calles.unity` (copias sueltas de carros) aún los usa. Pendiente si se quieren borrar junto con ese menú.
- Pruebas: EditMode (sin puertas ni scripts faltantes, sin referencias al script borrado) y PlayMode (subir y bajar por los dos lados y la patrulla).
- Capturas: docs/capturas/t972/.
