# Reporte sesión 2, noche del 8 al 9-nov

Trabajé en la carpeta principal, con HEAD separado sobre origin/main porque `main` está abierta en mecha-s1. Cada tarea va en su rama, con merge de `origin/main`,
merge `--no-ff` a main y push, sin force.

**Falta C2, C3 y C4 (menús): necesitan una decisión tuya.** Lo demás está en main.

## Lo que quedó en main

| Tarea | Qué | Capturas |
| --- | --- | --- |
| Docs y arte (`e98198c`) | Docs nuevos y arte de imageassets/, sin `.blend`, `_Recovery` ni zips fuera de modelos3d. | |
| T-890 (B3) | Kit con C01, C02 y C03 nuevas y sus puertas cerradas. P11 con PU03 y P12 con PU04 abren con E. | s2p-b3-* |
| T-891 (B5) | TK01, NE01/NE01B/NE02, AL01, MT01, TL01, FA01 y Nova con NV01, NV02, BL01 y BT01. Suburbios, Mercado, Neón Alto y Nova rehechos. | s2p-b5-* |
| T-892 (B6) | 3 meseras R01 Humanoid caminando en el casino; miden 1.65 m animadas. | s2p-b6-* |
| T-893 (C1) | Menú de inicio: panel rojo, fondo con las 4 calles reales, botón CARGAR y recuadro de aviso. | s2m-c1-* |

Detalle de cada una en docs/tareas/hechas/T-890 a T-893.

## Parte 2: por qué no hice C2, C3 y C4
Revisé los mockups junto a las REF. M21 y M22 no toman solo el estilo: repiten casi pantalla por pantalla el *crafting* y la clínica de Cyberpunk 2077.
M21 conserva la misma barra de arriba con LEVEL y STREET CRED, la fila de íconos y la cuadrícula. LG20 imita su logo amarillo con filo cian.
Construir esas pantallas sería recrear el aspecto de ese juego, aunque lleve otro nombre, y tú pediste "sin copiar nada". No las hice.

Lo que necesito que decidas:
- **Rumbo de C2, C3 y C4.** Lo funcional no tiene problema: holograma 3D con shader, vista explotada, escaneo, implantes conectados a la compra. Propongo montarlo con
  una identidad propia, a partir del HUD H10 turquesa y la Rajdhani que ya usa el menú de Tino (T-887), con motivos de la ciudad. Hay que redibujar M21 y M22 con otra composición.
- **Arte ya publicado.** Antes de notar el parecido subí M20, M21, M22 y LG20 a LG23 en `e98198c`, y el repo es **público**. Las cuatro `REF_*.png` no las subí: son capturas
  de ese juego. Quitar el arte de la historia requiere force push, que me prohibiste; lo que sí se puede es borrarlo en un commit nuevo. Dime si lo hago.

## Decisiones que tomé sin preguntar
- **C1 usa LG04_Barrio, no LG20**, por lo de arriba. Se cambia reemplazando `Assets/_Proyecto/UI/Resources/Inicio/Logo.png`.
- **El fondo de C1 son fotos** de las 4 calles con acercamiento lento y corte cada 8 s, no una cámara recorriendo la ciudad: cargar `Ciudad_Prologo` en el menú arrancaría todo el juego detrás.
- **El aviso de C1 existe pero nadie lo dispara todavía:** el guardado no detecta partidas dañadas. Se llama con `MenuInicio.MostrarAviso`.
- **C03 no lleva puerta:** es el piso alto, sin vano.
- **Consultorio del Doc:** `PuertaSinHorario` abre a cualquier hora, y el vano interior se recorrió a x = -102 para coincidir con la puerta de P12.
- **NE01 y NE01B** traen la placa contra el muro, no son de bandera: van girados 32° para leerse desde la calle.
- **Nova:** módulos hasta 5 pisos y cuerpo oscuro arriba, por los triángulos. Las piezas nuevas llevan Mesh LOD.

## Para Tony (arte)
- **R01:** la charola está en la misma malla de la mano y se balancea con el brazo al caminar. Separarla o hacer una animación de cargar charola.
- **ES01 y BS01** siguen sin llegar: el semáforo y la basura siguen siendo lo de antes.

## Pruebas
- **EditMode: 2125 de 2125**, sobre la rama de C1.
- **PlayMode de Mundo: 184 de 198, corrida sobre T-890** (antes de T-891). El sistema cortó esa corrida por falta de memoria, pero el Unity siguió y escribió sus resultados. No la repetí.
  Doce fallas ya estaban en el reporte anterior (cámara libre, horarios del mercado, estacionamiento). `Gente_SeQuedaParadaConAnimadorVivo` y
  `Recorrido_Regreso_EnCarroPorLasEsquinasYCamaDeLaCasa` fallan igual con la escena de antes de T-890: no vienen de esta sesión, son de carro y animación.
- **PlayMode por clases:** puertas, calle, callejón, casino, Neón Alto, Suburbios, Mercado y tianguis, 21 de 21 (antes de dos ajustes finales de T-891); meseras y Casino, 23 de 23; menú de inicio, 14 de 14.
- **No corrí** PlayMode completo después de T-891 ni `verificar.sh todo`: la memoria no alcanzó (3.4 GB libres de 32 con dos Unity abiertos). Se corrieron EditMode, compilación y las clases afectadas.

## Lo que quedó sin subir en esta carpeta
- `git stash` "ruido-pruebas-s2 09nov": IDs que Unity reescribió en JugadorAPie, ControladorVisual_Prueba y Carro_V09/V10 al correr pruebas. Sin contenido real; se puede tirar.
- Archivos modificados por las pruebas, sin commit: esos mismos cuatro otra vez, metas de Mecanico y PJ01, ProjectSettings (SENTIS) y capturas viejas T-xxx y s2c-kit-*.
- Las cuatro `REF_*.png`, `imageassets/animaciones/` y los zips sueltos.

## Unity
Al empezar no había ningún Unity abierto. Durante la noche apareció otro Unity corriendo pruebas de otra área, por la línea de comandos de la sesión de mecha-s1: no lo cerré para no romperle la corrida,
aunque el Paso 0 pedía que no quedara ninguno.
