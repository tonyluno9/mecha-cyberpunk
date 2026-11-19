# T-1396: Puerto, ronda 2 (K15 y A15)

Script: `Assets/Arte/Editor/ZonaPuertoT1396.cs` (`MechaCyberpunk.Arte.Editor.ZonaPuertoT1396`). Corre DESPUES de `ZonaPuertoT1387.Aplicar`.
Orden: `Aplicar` (Logs/t1396-aplicar.txt), `Validar` (Logs/t1396-validar.txt) y `Fotos` en otra corrida (docs/capturas/t-1396/, diagnostico en Logs/t1396-cielo.txt).

Escrito sin Unity (sin Library): pendiente de compilar y correr en la carpeta principal.

1. PU08: rehace los contenedores con grafiti (quita los de T-1387); lee la textura para saber en que cara larga esta el grafiti y lo orienta al norte/sur; fuerza PU08 en la cara norte de la primera columna y fila (lo que ve K15).
2. Bodegas: alero de lamina (M_T08), neon magenta/turquesa con Point Light, 2 lamparas de sodio (N01 + Spot chico) y tinaco sobre el techo, asentados con rayos contra la malla de la bodega. Tope 30 luces nuevas.
3. Gruas: balizas rojas sobre vertices reales de la malla (no en la caja); barco movido a x ~195 para quedar de frente al fondo del pasillo.
4. Arco del cielo: es el domo de nubes (CieloNubes), que en el editor no seguia a la camara de Foto; Fotos lo coloca en la camara (en el juego ya la sigue).
5. Piso: tarimas, tambos, huacales, montacargas y basura pegados a las caras de los bloques; postes de madera con lampara y cables que cruzan el pasillo hasta las bodegas.
6. Bodegas que rozaban el arroyo: se apartan hacia atras y, si no cabe, se comprime su profundidad (margen 0.5 m).
7. Fotos K15 y A15 en docs/capturas/t-1396/.
