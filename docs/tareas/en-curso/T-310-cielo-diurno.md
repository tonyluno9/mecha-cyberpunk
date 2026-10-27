# T-310 Cielo y atmósfera diurna legibles
Dueño: Alan | Rama: alan/T-310-cielo-diurno | Modelo: Sonnet 5.5, medium
Depende de: T-079 y T-099 (ya hechas: ciclo día/noche, perfiles y vistas de referencia). No depende de T-084, T-085, T-092 ni T-093, y no las toca.

## Objetivo
A las 12:00 el cielo es blanco-gris plano y la niebla casi no tiene color (docs/capturas/T-099-V1_12.png), así que el cyberpunk solo se lee de noche. Dar color y profundidad al día sin cambiar la noche de las 22:00.

## Qué hacer
1. `ConfigCicloDia`: niebla y ambiente de día más azul violáceos, densidad de niebla de día mayor, y tres datos nuevos del cielo (tinte, exposición, disco del sol). De noche el tinte del cielo es el color de la niebla.
2. `CicloDiaNoche`: si tiene un material de cielo (Skybox/Procedural), usa una copia y le pone tinte, exposición y sol según la hora, y pone la cámara principal en Skybox. Sin material, todo queda como antes (Flashback y demás escenas no cambian).
3. `CreadorAmbiente`: crea `Cielo_Procedural.mat`, lo asigna al `CicloDiaNoche` de Ciudad y Pruebas_Alan, y vuelve a copiar el Config del ciclo (una sola vez, hasta que tenga `colorCielo`).
4. Capturas con `VistasReferencia` a las 12:00 y a las 22:00, antes y después.

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Tests/EditMode/Mundo/, escenas de Alan (Ciudad, Pruebas_Alan), docs/capturas/, docs de Alan.

## Criterios de aceptación
- [ ] Compila sin errores y `bash herramientas/verificar.sh todo` pasa (EditMode y PlayMode).
- [ ] EditMode `CieloDiurnoPruebas`: a las 12:00 la saturación (HSV) de la niebla y del cielo es ≥ 0.2, ambos con azul > rojo, y la densidad de niebla ≥ 0.005.
- [ ] EditMode: a las 22:00 siguen iguales la densidad de niebla (0.02), el color de la niebla, el ambiente y el sol sin disco.
- [ ] Captura `docs/capturas/T-310-12.png` (vista V1 a las 12:00): el cielo ya no es blanco-gris; medido en píxeles de la franja del cielo, saturación media ≥ 0.15 y brillo medio ≤ 0.9.
- [ ] Captura `docs/capturas/T-310-22.png` comparada con T-099-V1_22: sin cambios visibles del cielo ni de la niebla de noche (revisión de la imagen).
- [ ] Reflejos de las sondas y rendimiento sin regresión (el cielo procedural es un solo pase de skybox).
- [ ] Límite honesto: que el día "se vea cyberpunk" es subjetivo; queda para revisión humana con las capturas lado a lado. Las paredes pastel de los edificios (materiales) no se cambian aquí.

## Estado
WIP sin Unity: código y pruebas escritos, sin compilar ni ejecutar. Falta ejecutar el creador (Crear T-079) para generar el .mat, las escenas y los .meta (incluido el del archivo de pruebas nuevo), y tomar las capturas.
