# T-440 Botón CRÉDITOS en el título
Dueño: Tony | Rama: tony/T-440-boton-creditos | Modelo: Sonnet 5.5 | Sesión A (bug de docs/bugs.md, T-471 de la Sesión B)
Hecho: el título tiene el botón CRÉDITOS entre OPCIONES y SALIR. Sin ciclo de ensamblados: PantallaTitulo expone el evento estático `PidioCreditos` y el ensamblado nuevo `MechaCyberpunk.UI.TituloCreditos` (UI/TituloCreditos/, depende de UI y UI.Creditos) lo conecta a `PantallaCreditos.Abrir`. Esc vuelve al título con el botón elegido. Prefab del título regenerado con `CreadorPantallaTitulo.Crear`.
Pruebas: PlayMode UI 133/139 (6 omitidas ya antes), 0 fallos; `TituloCreacionPruebas.ContinuarConRaton` ahora hace clic en CRÉDITOS y Esc.
Nota: docs/bugs.md es de la Sesión B (solo lectura para la Sesión A): el bug "Botón CRÉDITOS en el título" queda resuelto aquí.
