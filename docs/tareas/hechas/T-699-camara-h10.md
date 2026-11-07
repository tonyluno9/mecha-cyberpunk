# T-699 Cámara como H10
Dueño: Tony | Rama: tony/T-699-camara-h10 | Modelo: Sonnet 5.5

## Objetivo
Cámara en tercera persona como H10_HUD.png: personaje centrado abajo, ~1/4 del alto, cámara alta mirando a lo largo de la calle.

## Qué se hizo
- ConfigCamaraHombro_Base (AjustarCamaraH10T699): lateral 0, altura 3.4, distancia 7, FOV 65; apuntando mismo encuadre, FOV 45.
- Medido en Ciudad_Prologo frente a la casa (BakeMesh, cabeza a pies): fracción 0.269, centrado 0.496, pies 0.10, cabeza 0.37.
- CamaraHombro: la altura del pivote se recorta bajo techos (casas), para que la cámara alta no salga por el techo.
- Pruebas ajustadas: CamaraApuntarT682 (Edit y Play), CamaraPared (+ prueba de techo), ApuntadoAsistido (ángulo desde la cámara), ArmasMercado (FOV 65/45, blanco a la altura de la mira). Nueva: CamaraH10T699Pruebas.
- Capturas: s1c-T-699-calle.png y s1c-T-699-comparacion.png (H10 a la izquierda).

## Criterios
- [x] Fracción 0.2-0.32, centrado, pies abajo, cámara a 3.4 m y 7 m atrás
