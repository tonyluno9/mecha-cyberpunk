# T-513 Paso 5 del plan de personajes y carros: fondo de la ciudad
Dueño: Tony | Rama: tony/T-513-fondo | Modelo: Opus 5.5 | docs/plan-personajes-carros.md, paso 5
Hecho: colocador idempotente ColocarT513.Colocar (menú Mecha Cyberpunk/T-513/Fondo de la ciudad, Assets/Arte/Editor/), raíz T513_Fondo en Ciudad_Prologo. Las tres pinturas de imageassets/fondos/ (6336x2688) se copian a Assets/Arte/Fondos/Texturas (importadas a 4096, comprimidas, con mipmaps) y van en un cilindro de 560 m de radio (dentro del plano lejano de 600 m de las cámaras más cortas) y 373 m de alto, en 4 tramos de 90°: fondo-nova al norte, fondo-neon-alto al este, fondo-suburbios al sur y al oeste (el oeste en espejo para que la unión con el sur no se note). Mallas en Assets/Arte/Fondos/Mallas, materiales en Fondos/Materiales.
Shader nuevo MechaCyberpunk/FondoCiudad (Assets/Arte/Fondos/Shaders): sin luces, sin sombras y sin la niebla del juego; arriba se vuelve transparente (de 55 % a 85 % de la altura) y deja ver el cielo real, así se funde con el cielo de día y de noche. De noche la pintura va con brillo 2 (el posproceso de noche de T-507 la oscurece); de día se aclara, pierde la mitad del color y se mezcla con una bruma azul (45 %): se ve como una ciudad lejana con calima. Sigue la global _T501Noche de VolumenSigueNoche.
Torres: las 48 torres de horizonte de T-501 (T501_Suburbios/Horizonte, a 340-820 m) quedan apagadas porque el fondo las cubre; las 14 de Neón Alto (T505_NeonAlto/Torres) se quedan.
Código: Arte/Editor/ColocarT513.cs, Arte/Fondos/ (shader, texturas, materiales, mallas), Ciudad_Prologo.
Pruebas: EditMode Mundo 179/179 (FondoCiudadT513Pruebas 5 nuevas: 4 tramos con su pintura y sin sombras, cada pintura hacia su lado, dentro del plano lejano, shader sin niebla y transparente arriba, torres lejanas apagadas y las de Neón Alto puestas; SuburbiosT501Pruebas espera el horizonte apagado). PlayMode SuburbiosT501Play y NeonAltoT505Play 7/7. RecorridoDemoPruebas 2/2 (ver bitácora).
Capturas (editor vivo, Logs/cap513.sh): docs/capturas/T-513-<vista>-comparacion.png (antes | después) para azotea-este (Neón Alto), azotea-norte (Nova), suburbios-oeste (calle), neon-norte, azotea-sur-dia y azotea-este-dia; y sus -antes/-despues. El "antes" se tomó en la misma sesión apagando el fondo y prendiendo las torres.

## FPS (build de medición, 1920x1080, Medio, hora 23, Logs/med513/medicion.txt)
| Punto | FPS | 1% bajo |
| --- | --- | --- |
| calle | 162.3 | 110.4 |
| tianguis | 162.8 | 94.5 |
| carros | 155.2 | 88.2 |
| neon_calle | 259.1 | 183.5 |
| suburbios | 347.7 | 248.5 |
Todos muy por encima de 60 (sin las 48 torres, Suburbios sube de 301.8 a 347.7).

## Bitácora
- 2026-11-02: tres vueltas de ajuste: 8 tramos de 45° y brillo 0.2 (de noche no se veía y el horizonte quedaba a ~7°, tapado por los edificios); brillo 1 (débil); 4 tramos de 90°, base a -40 m y brillo 2 (se ven el skyline verde y magenta al este y los haces de Nova al norte). Queda una costura visible donde se juntan Nova y Neón Alto (noreste).
- 2026-11-02: RecorridoDemoPruebas 2/2, pero con 2 bugs anotados en El Tianguis (KO del bot y pelea que pasa de 240 s; el rescate de la prueba la terminó): el fallo intermitente que arregla el paso 6. Builds/Demo y Builds/Juego regenerados (Result: Success).
