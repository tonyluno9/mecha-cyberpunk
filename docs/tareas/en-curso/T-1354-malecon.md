# T-1354: el Malecón

Rama: tony/T-1354-malecon (desde 9220518). Dueño: Tony. Plano: docs/plan-zonas-18nov.md. Referencias: K11, K12, A11, M12.

## Qué hace
`MechaCyberpunk.Arte.Editor.ColocarT1354Malecon.Aplicar()` rehace la raíz `T1354_Malecon` de Ciudad_Prologo (borra y rehace solo esa raíz); `.Fotos()` saca las tres capturas lado a lado sin guardar.
Las cuentas puras (curva, puntos de unión, espaciado) están en `ColocarT1354Curva` y se prueban en EditMode.

- Avenida de 16 m (2 carriles por sentido) por z = -226 (recta de x -110 a 60) con curva suave a U3 (-300, -205) y a U5 (160, -215). Radio mínimo en planta: ~165 m. Plana (y = 0).
- Perfil: banqueta de ciudad 3 m, arroyo, banqueta del mar 8 m (y 0.2), muro hasta y -2.2, playa hasta la orilla en z = -262 (y -3), fondo a -6.
- CalleZona "Malecon": avenida, U1 y U2 (rectas de z -201 a -226 por x = ±47), U3 (Hermite por (-330, -150) a (-305, -120)). U5 es el extremo este de la avenida.
- Barandal ML01, escaleras ML02 cada 80 m, palmeras y faroles alternados cada 12 m, bancas, escollera ML05 (tramo oeste y espigón este con el faro ML04), muelle ML03/ML04 con 5 lanchas de colores y puesto de mariscos.
- Fachadas de 3 a 5 pisos con el kit S2c (neón, aires), postes C07 con transformador y cables, rayas y 18 charcos.
- Mar con `CreadorMarT1351.CrearPlanoMar/CrearOrilla` por reflexión (si no está: `Hueco_Mar`). 2 sondas de reflejo. Máximo 10 Light reales (faro, puesto y postes repartidos). Estático y LOD (culling al 1.5 %) en las casas.

## Criterios
- [ ] Compila y MaleconT1354Pruebas (EditMode) pasa. (No se pudo correr Unity al escribirlo: el worktree no tiene Library.)
- [ ] `Aplicar()` corre sin errores y el informe Logs/t1354-malecon.txt lista los huecos.
- [ ] Capturas docs/capturas/zonas/malecon-k11.png, -k12.png, -a11.png revisadas contra las referencias.

## Decisiones
- El faro va al final este (plan); K12 lo ve al oeste, así que la toma de K12 mira al este-sureste (espejo).
- U3: los puntos del plano obligan a girar ~68 grados en 40 m; su radio mínimo queda ~19 m (las demás calles piden 40).
- Ronda 2: sin tope de Light (faroles con spot de sodio como ColocarS2oLuz, postes, focos y neones con su luz); playa de 30 m con el agua por debajo hasta el muro; fachadas de 3-5 pisos con cortinas, neones y aires; postes a los dos lados con cables cruzando; muelle largo con 6 lanchas; faro sobre montículo de rocas con haz aditivo (HaloLuz) que gira con GiroHazFaro (Mundo/Ambiente); cruces U1/U2 libres 16 m; mar de x -700 a 330 (el este es del Puerto). Encuadres de fotos nuevos. Balcones: no hay pieza en el kit.
- La arena usa color liso porque no hay material de T04_Arena en Assets.
- Skyline: no se pudo comprobar sin Unity.
