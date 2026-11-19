# T-1394 Colonia del Cerro, ronda 2

Script: Assets/Arte/Editor/ZonaCerroT1394.cs. Corre DESPUÉS de ZonaCerroT1385. Métodos: ZonaCerroT1394.Aplicar, .Validar, .Fotos (corrida aparte).
Sin verificar en Unity todavía (escrito sin Library): el orquestador corre y devuelve errores.

- Casas existentes del kit cambiadas por CC07_a, CC06_b, CC01 y CC02 (una de cada tres, hasta 90), con tinaco, antena, tendedero y lavadero.
- Escalones de concreto y tablones entre azoteas vecinas (0 a 3 m de desnivel), con collider; cada grupo unido recibe escalera de servicio (PuntoTrepar).
- Hasta 26 neones chicos con su Point Light (LuzNocturna, sin sombras) junto a las escaleras CE01 y en fachadas cerca de K13.
- K13 en la calle base (x -303.5, z -12) mirando al oeste-noroeste hacia la escalera del z = 0; carros cercanos quitados; casas que tapan el este se quitan (tope 14).
- Arregla T1385: quita las casas que invadían la calle y rehace sus escaleras de servicio (tenían la salida al revés).
- Lo quitado de lo viejo se guarda desactivado en T1394_Cerro/Reemplazados con su lugar en T1394_Cerro/Registro; Aplicar lo devuelve antes de rehacer.
