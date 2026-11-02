# T-509 Paso 3 del plan de personajes y carros: cierre del pulido (puesto de tacos, pistola, Neón Alto verde)
Dueño: Tony | Rama: tony/T-509-cierre-pulido | Modelo: Opus 5.5 | docs/plan-personajes-carros.md, paso 3
Hecho: ColocarT509.Colocar (menú Mecha Cyberpunk/T-509/Cierre del pulido, idempotente). (1) La "caja gris colgante" del pasillo era la plataforma de dron CY07a de T-504 colgada a media altura: se apaga (la cámara CY08 se queda). Los 4 puestos de tacos del pasillo de El Mercado (malla puesto_tacos de bloques: trompos naranjas, mostrador gris, bancos rosas y garrafón blanco) pasan al modelo C12 (hijo Visual_T509, sin collider propio; la malla vieja queda apagada y conserva su collider). (2) Todo modelo del kit con material liso (no solo primitivas: tinacos, letreros, transformadores, mobiliario) recibe el material con textura de T-506: 123 retexturizados, 0 renderers sin arte en la escena. (3) Neón Alto verde azulado como docs/referencias-ciudad/neon-alto-noche.png: perfil PerfilNeonAlto (filtro verde azulado suave, tinte -28, saturación 12, sombras verde azulado, luces altas magenta), 8 farolas verde frío, niebla verde azulada, charcos verdes y magenta, pantallas con emisión más verde, 10 luces de calle verde y magenta sin sombra. La primera vuelta (filtro verde puro, tinte -80) volvía la imagen monocroma; la segunda conserva los acentos magenta y amarillos.
Pistola que suelta el SKULL: ya era pequeña (corredera de 26 cm, T-507); ArmaEnPiso ahora la hace brillar como objeto para recoger: el modelo gira (90°/s) y flota (25 cm, ±6 cm) sobre una luz puntual turquesa sin sombra; el punto de interacción no se mueve y el HUD dice "E · Recoger la pistola".
Código: Arte/Editor/ColocarT509.cs, Personaje/Combate/ArmaEnPiso.cs, materiales de Assets/Arte/Exteriores (perfil, niebla, charcos, brillos), Ciudad_Prologo.
Pruebas: EditMode Mundo 167/167 (SinGreyboxT506Pruebas + 3 nuevas: ninguna malla con material liso, puestos de tacos con C12 y sin caja colgada, perfil de Neón Alto verde azulado sin saturar de más; LuzColorT507 acepta farolas verdes solo en Neón Alto; MercadoT504 espera la plataforma de dron apagada). PlayMode Mundo 177/188: las 11 rojas no tocan lo de T-509 (horario del mercado, que ahora abre 24 horas: EntradasNeon, Lugares x2, Puertas; cámara libre de Alan: CamaraLibre x2, CamaraLibreCarro x4; RecorridoPrologoCiudad, el carro no llega a la esquina 2); no se comprobaron en main. PlayMode ArmaEnPisoBrilloPruebas 1/1 (luz, gira, flota, punto fijo, "Recoger") y HistoriaPandillasPruebas 2/2 (el SKULL suelta la pistola y se recoge).
Capturas (build de medición, noche): docs/capturas/T-509-neon-alto-comparacion.png (referencia | antes | después), T-509-mercado-comparacion.png (antes | después), y sus -antes/-despues.

## FPS (build de medición, 1920x1080, Medio, hora 23, Logs/med509/medicion.txt)
| Punto | FPS | 1% bajo |
| --- | --- | --- |
| calle | 170.1 | 93.0 |
| tianguis | 176.7 | 99.2 |
| mercado | 269.4 | 191.1 |
| neon_calle (Neón Alto) | 269.6 | 182.8 |
| neon | 251.5 | 124.0 |
Todos muy por encima de 60.

## Bitácora
- 2026-11-01: WIP 425edce (Neón Alto más verde, ColocarT509 a medio escribir).
- 2026-11-02: main (T-510 y T-511) traído a la rama; la escena se tomó de main y se volvió a correr ColocarT509.Colocar. Neón Alto ajustado en una segunda vuelta (menos verde) tras comparar con la referencia.
