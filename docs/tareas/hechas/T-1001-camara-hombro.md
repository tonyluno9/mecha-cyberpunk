# T-1001: cámara al hombro como CAM04 y CAM05 (playtest 16-nov, punto 1)

- Sin apuntar (CAM04): cámara 0.3 m a la derecha, 3.5 m atrás; el personaje queda casi centrado (centro X 0.444), cuerpo completo.
- Apuntando (CAM05): se acerca a 1.5 m y se corre 0.75 m al hombro derecho, FOV 45; el personaje queda a la izquierda,
  de la cintura para arriba, y la cruceta libre. Transición suave (suavizado de la config).
- Si hay pared a la derecha (SphereCast desde el punto de giro), la cámara pasa al hombro izquierdo con `CameraSide` de
  Cinemachine, suave, y vuelve cuando hay sitio. La colisión de Cinemachine sigue evitando paredes.
- Valores en `ConfigCamaraHombro_Base.asset` con `AjustarCamaraT1001.Ajustar`.
- Pruebas: `CamaraHombroT1001Pruebas` (2), `CamaraHombroT1001EditPruebas`; se ajustaron T-723, CamaraPared, CamaraEscena y
  ArmasMercado; se quitaron las de T-682 (pedían la cámara centrada al apuntar, que este punto reemplaza).
- Capturas revisadas: `docs/capturas/t1001/`.
