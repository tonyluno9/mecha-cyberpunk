# T-1363 · Malecón como K11 y K12

Rama: tony/T-1363-malecon-k11 (sale de f41c439). Carpetas: Assets/Arte/Editor/ColocarT1354*.cs, su prueba EditMode y docs/.
Estado: código escrito SIN correr (no hay Library en este entorno). Falta correr `ColocarT1354Malecon.Aplicar` y `.Fotos`, y las pruebas.

## Qué cambia
1. Fachadas: se quitó la segunda hilera de cajas lisas. Frente de la avenida del kit con 3 a 5 pisos, cortinas, toldos (50 %), aires y letreros
   en bandera con luz propia (2.2 / 6 m). Nuevas fachadas del kit en las dos paredes de las bocas de U1 y U2 (z -178 a -208).
2. T530: `ApagarRellenoT530` apaga (SetActive false, no borra) los `Edificio` y `Ventanas` de T530_Afueras/Salida_4 y Salida_5 con centro a menos de
   60 m de la avenida y z < -178 (`ColocarT1354Curva.RellenoT530Apagable`). Los nombres salen en Logs/t1354-malecon.txt.
3. Otra orilla: losa oscura z -690 a -900, torres T20-T23 de T-541 (4 a 16 pisos de 12 m, pantallas T22), franja emisiva en el borde y luces
   puntuales fuertes (rango 240) en la base de una de cada tres torres para las estelas del shader Mar. Sin colliders ni sombras, LOD que corta al 0.3 %.
4. Luz: Volume local `VolumenMalecon` (isGlobal false, BoxCollider trigger x -305..165, z -300..-205, mezcla 15 m, prioridad 10), perfil
   `Assets/Arte/Mundo/Zonas/PerfilMalecon_T1363.asset` (balance +30, tinte +6, saturación -6, filtro sodio, bloom 1.4). La mezcla llega a z = -190.
   Faroles ML01_Farol x1.6 de intensidad y x1.25 de rango.
5. K12: puesto de mariscos a escala 1.8 con foco cálido 30 y halo. Hieleras y bancos de plástico siguen como Hueco.
6. Fotos: K11 a 1.7 m del paseo (y = 1.9), K12 junto al puesto, A11 aérea.

## Pruebas
MaleconT1354Pruebas: Volumen_CubreLaAvenidaYElPaseo, Volumen_NoAlcanzaLosBarriosViejos, Volumen_MezclaDe15mYPrioridadAlta,
RellenoT530_SoloSeApagaDetrasDeLasFachadasYCercaDeLaAvenida.

## Dudas
- El volumen no lleva VolumenSigueNoche (su `origen` es un objeto de la escena y el prefab no puede referenciarlo): también se nota de día.
- ColocarS2cLook.Foto tiene el far clip en 600 m: las torres más lejanas pueden salir cortadas en las capturas.
