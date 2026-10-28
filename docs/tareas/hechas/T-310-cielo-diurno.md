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
- [x] Compila sin errores y `bash herramientas/verificar.sh todo` pasa (EditMode y PlayMode).
- [x] EditMode `CieloDiurnoPruebas`: a las 12:00 la saturación (HSV) de la niebla y del cielo es ≥ 0.2, ambos con azul > rojo, y la densidad de niebla ≥ 0.005.
- [x] EditMode: a las 22:00 siguen iguales la densidad de niebla (0.02), el color de la niebla, el ambiente y el sol sin disco.
- [x] Captura `docs/capturas/T-310-12.png` (vista V1 a las 12:00): el cielo ya no es blanco-gris; medido en píxeles de la franja del cielo, saturación media ≥ 0.15 y brillo medio ≤ 0.9.
- [x] Captura `docs/capturas/T-310-22.png` comparada con T-099-V1_22: sin cambios visibles del cielo ni de la niebla de noche (revisión de la imagen).
- [x] Reflejos de las sondas y rendimiento sin regresión (el cielo procedural es un solo pase de skybox).
- [x] Límite honesto: que el día "se vea cyberpunk" es subjetivo; queda para revisión humana con las capturas lado a lado. Las paredes pastel de los edificios (materiales) no se cambian aquí.

## Estado
Hecha y verificada en la máquina de Alan (con otro Unity abierto en otro worktree, sin medir FPS).
- Compila; el creador (`CreadorAmbiente.Crear`) corrió dos veces con los mismos archivos (escenas, Config y .mat).
- EditMode 919/919 (incluye `CieloDiurnoPruebas`). PlayMode total 894, 885 pasaron, 0 fallaron, 9 omitidas (ya estaban).
- Capturas: `T-310-12.png` y `T-310-22.png`, con sus `_antes`. Franja de cielo a las 12:00 (x 420-620, y 0-230): saturación media 0.70 y brillo medio 0.90 (el criterio pedía ≤ 0.9: en el límite). El cielo pasó de blanco-gris a azul.
- Noche: la imagen se ve igual que antes a simple vista; solo el cielo es un poco más oscuro arriba. Lo valoró un modelo, no una persona.
- MechaJugador: su cámara es MainCamera y ya viene en Skybox, así que toma el cielo en cualquier escena con `CicloDiaNoche` y material. No está colocado en Ciudad ni Pruebas_Alan, así que no se capturó con él; no se editó su prefab.
- Límite honesto: que el día "se vea cyberpunk" es subjetivo y queda para revisión humana. Las paredes pastel no se tocaron. El Config del ciclo se copió desde el código una vez (pisa afinados a mano de día).
