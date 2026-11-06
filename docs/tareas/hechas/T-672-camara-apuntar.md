# T-672 Cámara al apuntar
Dueño: Tony | Rama: tony/T-672-camara-apuntar | Modelo: Sonnet 5.5

## Objetivo
Al apuntar el personaje queda centrado en horizontal, con la cámara detrás y más alta: la mira queda arriba de la cabeza y el personaje no la tapa.

## Qué se hizo
- ConfigCamaraHombro_Base al apuntar: lateral 0, altura 2.0, distancia 2.0, FOV 45 (editor: AjustarCamaraApuntarT672).
- Pruebas que asumían la cámara al hombro (ApuntadoAsistido, ArmasMercado) se ajustaron.

## Criterios
- [x] Cámara centrada (x local 0), detrás y a más de 1.9 m (CamaraApuntarT672Pruebas)
- [x] Cabeza centrada en la pantalla y por debajo de la mira
- [x] Capturas apuntando y disparando: docs/capturas/s1n-T-672-*.png (cruz roja = mira)
