# T74: zonas de glitch puestas a propósito (2)

Se agregó ZonaGlitch (zumbido de transformador en loop 3D y parpadeo leve) con ColocarZonasGlitch / AddComponent; no se movió ningún objeto.

| Zona | Objeto | Posición | Por qué | Captura |
| --- | --- | --- | --- | --- |
| Elevador del taller El Canepa | S2n_TallerMecanico/Elevador | (-29.0, 2.0, -74.5) | Lift elevado a propósito (3.4 m) dentro del taller que se ve desde la calle de la casa, a 25 m de ella. | glitch-elevador-taller.png |
| Candil del casino | I13 (S2o_Casino) | (70.5, 4.2, 50.5) | Candil colgante del salón del casino, que el jugador visita; se ve bien como luz que parpadea. | glitch-candil-casino.png |

No se usó: torre de Nova, mercado, muro ni el fondo T513 (cascarón sin piso).
Nota: las capturas son estáticas; el parpadeo dura 0.05 a 0.15 s cada 1 a 4 s y el zumbido solo se oye en Play.

## Verificacion en Play (23-nov, verificador A)

- Parpadeo: ocurre. `fin`/`proximo` avanzan cada 1 a 4 s en las dos zonas (muestras en t=1155 a 1168), Elevador con 14 piezas movibles e I13 con 1.
- Zumbido: hay un AudioSource OBJ_TRANSFORMADOR en loop, reproduciendo, spatialBlend 1, min/max 2/12, volumen 0.029 por zona.
- FALLA de posicion: el zumbido del Elevador suena en (0, 0, 0) (centro del Tianguis), no en (-29, 2, -74.5), porque el GameObject `Elevador` (padre Muebles) tiene su transform en el origen y sus renderers estan a (-29, 2.5, -73). Se oye (a 12 m) en el Tianguis y no en el taller. Arreglo: en ZonaGlitch.OnEnable usar el centro de los renderers (Bounds) como ancla del bucle (hijo vacio en ese punto) o colocar el componente en un hijo en el centro. I13 esta bien (70.5, 4.2, 50.5).
- Volumen 0.029 es muy bajo; revisar a oido.
