# Pedido de arte: subir a los techos (S2 techos B)

Todo esto hoy es greybox hecho por código (`Assets/Arte/Editor/ColocarS2Techos.cs`, mallas en `Assets/Arte/Mundo/S2b/`). Hay que sustituirlo por arte final.

## Modelos

1. **Escalera marina de servicio** (la usan las 17 escaleras `EscaleraGreybox` y las 6 `EscaleraDeLaBarda`).
   - Modular por metro, para que el colocador la repita según la altura (de 2,5 a 11 m).
   - Dos largueros metálicos, peldaños cada 0,3 m, separada de la pared 0,13 m con soportes cada 1,5 m. Variantes con jaula y sin jaula.
   - Pieza de remate que sobresale unos 0,9 m sobre el techo para agarrarse.
   - Sin collider sólido: el trigger lo pone `PuntoTrepar`. Origen: pie de la pared, centrada, el frente mira a la calle.
2. **Tubo de desagüe con abrazaderas** (los 5 `TuboGreybox`).
   - Tubo de 0,12 m de diámetro, modular por metro, con abrazaderas cada 1,2 m y un codo o canalón arriba. Sin collider sólido.
3. **Tablón o puente de azotea** (los 2 `Tablon_*`).
   - Tabla o pasarela de 0,5 m de ancho por 0,1 m de grueso y entre 4 y 7 m de largo, que se apoya 0,5 m en cada techo. Con collider de caja.

## Clips de animación que faltan (jugador a pie)

- `Trepar_Escalera` (subida en bucle, manos y pies alternados).
- `Trepar_Tubo` (abrazado al tubo, subida más lenta).
- `Agarre_Borde` (colgado de una mano o dos del borde y subida al techo).
- `Salto_Largo` (salto corriendo de unos 3,5 m, para saltar entre azoteas).

## Notas

- El contenedor L01 y la barda MT01 de las rutas de parkour ya son arte del kit; la barda se escala a 3,5 m de alto. Si se quiere una barda baja propia (3,5 m) conviene un modelo nuevo.
- `Barrio/Edificio_7` no tiene un techo de 3,8 m: esa pieza es un paso elevado (de 3,8 a 9,0 m), así que no se hizo la ruta con contenedor solo.
