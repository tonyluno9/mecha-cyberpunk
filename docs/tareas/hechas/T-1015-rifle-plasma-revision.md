# T-1015: revisión del rifle de plasma, la granada y el lanzagranadas en la calle (playtest 16-nov, punto 16)

- Capturas en Ciudad_Prologo de día (`CapturasArmasT1013Pruebas`, con CAPTURAR_T991=1): `docs/capturas/t1013/plasma-cargando.png`
  (bola de energía cian en la boca), `plasma-rayo.png` (rayo a la mira), `granada-arco.png`, `lanzagranadas-explosion.png`.
- Arreglos tras revisarlas:
  - Las armas especiales disparan y dibujan en `LateUpdate` (orden 1000): en `Update` la boca aún está donde la dejó el cuadro anterior.
  - La bola de carga lleva su propio material teñido del color del rayo (era blanca).
  - El material del rayo es blanco HDR; el color lo pone cada rayo (cian el plasma, rojo el láser de T-1017).
- La prueba comprueba que la bola está en la boca del rifle mientras carga.
