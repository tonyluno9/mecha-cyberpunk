# T-1389b Personajes nuevos de Blender al juego

Rama: tony/T-1389b-personajes-nuevos. Carpetas: Assets/Arte/, Personaje/Visual/, IA/, Tests/EditMode/PersonajeVisual/.

## Qué se hizo (código escrito sin Unity; falta correrlo)
- Assets/Arte/Editor/PersonajesNuevosT1389b.cs: Aplicar (copia FBX, extrae texturas, visuales Humanoid, perro Generic, peatones, Nova, SKULLS, informe en Logs/t1389b-personajes.txt), EscenaSkulls (cambia los SKULLS de Ciudad_Prologo) y Fotos (docs/capturas/t-1389b/).
- CreadorVisualTripo: carpeta por código y un FBX por variante; alturas de los nuevos. PostprocesadorAssetsT250: los nuevos se importan como Humanoid; E17 como Generic.
- PeatonVariado: lista de visuales nuevos (se crea solo el elegido). Peaton.prefab los recibe (65 %). El sistema de peatones no sabe de zonas: van mezclados.
- ConfigRespuestaNova + RespuestaNova: guardias extra (E15, E18), pesados extra (E16) y perros (E17, 2 con 4 estrellas, 3 con 5).
- ColocarS2Pandillas: los SKULLS salen por turnos con E19/E20 si existen.
- Pruebas: PersonajesNuevosT1389bPruebas (EditMode).

## Pendiente de verificar con Unity
Compilar, correr Aplicar, EscenaSkulls y Fotos, y las pruebas. Revisar las capturas y Logs/t1389b-personajes.txt.
