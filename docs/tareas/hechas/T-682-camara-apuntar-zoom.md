# T-682 Cámara al apuntar: mismo encuadre, solo zoom
Dueño: Tony | Rama: tony/T-682-camara-apuntar-zoom | Modelo: Sonnet 5.5

## Objetivo
Al apuntar la cámara conserva el encuadre de caminar (misma altura y distancia) y solo hace zoom; no sube (se deshace la subida de T-672).

## Qué se hizo
- ConfigCamaraHombro_Base al apuntar: altura 1.6 y distancia 1.9 (iguales a caminar), FOV 40 (caminando 50), lateral 0 (se conserva el centrado de T-672). Editor: AjustarCamaraApuntarT682 (reemplaza al de T-672).
- ApuntadoAsistido no dependía de la altura de la cámara (mide desde el pecho); ArmasMercado: blanco otra vez de 1.8 m y FOV 40.
- Decisión: al caminar la cámara va al hombro (lateral 0.45); al apuntar queda centrada detrás (lateral 0) porque así se pidió conservar el centrado.

## Criterios
- [x] Misma altura y distancia que caminando, centrada, FOV menor (CamaraApuntarT682Pruebas, EditMode y PlayMode)
- [x] ApuntadoAsistido, ArmasMercado y CamaraPared siguen pasando
- [x] Capturas docs/capturas/s1t-T-682-caminando|apuntando|disparando.png
