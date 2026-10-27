# T-189 Correcciones del bloque 5: aviso de reputación al cargar y letrero del grafiti
Dueño: Tony | Rama: tony/T-189-correcciones-bloque-5 | Modelo: Sonnet 5.5, medium
Depende de: T-186, T-188

## Objetivo
Dos errores que encontró el recorrido del bloque 5 (T-186 y T-188) quedan corregidos antes de mezclar en main.

## Qué hacer
1. `UI/HudAPie/HudAPie.cs`: al cargar una partida (F9 o al arrancar con guardado), el aviso de reputación del HUD muestra los valores
   guardados como si fueran un cambio. Ignorar los cambios de reputación que llegan durante la carga (o los que no vienen de jugar). Prueba
   de PlayMode en UI o Progresion.Misiones: guardar con barrio ≠ 0, cargar, el aviso no aparece; un cambio real después sí aparece.
2. `Progresion/Prologo/Editor/CreadorEscenaMisiones.ArmarMuroReputacion`: el TextMesh "GRAFITI: <ALIAS>" cuelga del muro escalado
   (0.4 × 5 × 7) y su escala de mundo queda deformada. Ponerlo bajo un padre sin escala, pegado a la cara del muro, legible desde la calle.
   Correr el creador (dos veces, mismo md5 de la escena) y regenerar docs/capturas/T-188-3.png (o T-189-1.png) revisada a ojo.
   Prueba: la escala de mundo del letrero es uniforme.

## Carpetas permitidas
Assets/_Proyecto/UI/HudAPie/, Assets/_Proyecto/Progresion/Prologo/Editor/CreadorEscenaMisiones.cs,
Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity (solo con el creador), Assets/_Proyecto/Tests/PlayMode/UI/,
Assets/_Proyecto/Tests/PlayMode/Progresion/Misiones/, docs/capturas/
No tocar: Core/, lo de Alan, otras escenas.

## Criterios de aceptación
- [ ] Pruebas nuevas en verde 3 corridas; UI y Progresion.Misiones en verde.
- [ ] Captura del muro con el letrero sin deformar, revisada a ojo.
