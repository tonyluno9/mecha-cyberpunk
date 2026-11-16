# T-1018: las prótesis de ataque se ven en el cuerpo

Contexto: T-1017 dio el brazo láser y la hombrera de misiles (se tienen si están en el inventario) pero no se veían.

## Qué se hizo
- `ColocarProtesisT1018` (Personaje/Editor; `-executeMethod MechaCyberpunk.Personaje.Editor.ColocarProtesisT1018.Colocar`, se puede repetir): desempaca `IMP04_BrazoLaser.zip` e `IMP05_HombreraMisiles.zip` (imageassets/modelos3d/protesis/ del proyecto o de la carpeta principal) a `Assets/Arte/Protesis/<código>/` (fbx, texturas color/normal/metálico/rugosidad), crea el material URP Lit, arma el prefab (raíz = el hueso, `Modelo` con su pose en el hueso, `Punta` = de donde sale el rayo o los misiles) y lo asigna al campo `modelo` de `DatosProtesisAtaque_laser` y `_misiles`. La pose se mide con el esqueleto de PJ01 (JugadorAPie): el brazo a lo largo del antebrazo derecho hacia la mano (0.46 m), la hombrera derecha sobre el hombro izquierdo (0.30 m).
- `ProtesisVisiblesAPie` (Personaje/Habilidades, lo agrega `HabilidadesProtesis`): cada cuadro revisa el catálogo y el inventario; si hay la prótesis cuelga su modelo del hueso Humanoid (RightLowerArm el brazo; LeftShoulder la hombrera) y lo activa, si no lo desactiva. Aparece al comprarla sin reiniciar y sigue la animación.
- `ProtesisAtaqueAPie`: el rayo sale de `PuntaLaser` y los misiles de `SalidaMisiles` si el modelo está; si no, de la mano y el hombro como antes.

## Pruebas
PlayMode `ProtesisEnElCuerpoT1018Pruebas` (6): sin prótesis no hay modelo activo; con ella el modelo está activo, hijo de su hueso, de 0.15 a 0.7 m, cerca del hueso y sin tapar la cámara (fuera de 0.25 m); al quitarla se apaga; sigue al hueso al caminar; el rayo nace en la punta del brazo; capturas con CAPTURAR_S1=1 en docs/capturas/t1018/ (cuerpo de frente y de espalda, de cerca el brazo y el hombro, modelos sueltos). Con `ProtesisAtaqueT1017Pruebas` y `HabilidadesProtesisT992Pruebas`: 21/21. EditMode Personaje 220/220.

## Notas
- La orientación de los modelos de Tripo se decidió mirando sus renders sueltos (modelo-*.png): el brazo viene colgando (cañón arriba, mano abajo) y se pone con la mano en la muñeca; la hombrera se gira 90 grados para que el depósito quede atrás. Es una decisión estética que conviene que Tony vea.
- Las prótesis solo se ven con la cámara a pie; en el carro o el mecha no cambia nada.

## Revisión (Opus)
- La hombrera IMP05 es un arnés (dos hombreras, correas y el lanzador arriba), no una sola hombrera: ahora va en el pecho (hueso Chest), a lo ancho de los hombros (0.5 m), derecha, con el lanzador sobre el hombro izquierdo. Ejes revisados con `EjesProtesisT1018.Render` (la raíz del .fbx trae 90° en X).
