# T-1005: el Doc y el exo de piernas gratis

Playtest 16-nov: el Doc ya no regala el exo de piernas directo; lo eliges y lo compras gratis. El nivel II de cualquier prótesis se compra en cuanto tienes el I.

- `RegaloImplanteDoc`: ya no instala. Mientras no tengas implantes, al hablar con el Doc dice "Elige tu prótesis, te regalo la de piernas. Siéntate en la silla." (constante `TextoFrase`; la frase vieja serializada en Ciudad_Prologo queda ignorada). La propiedad `Regalado` pasó a `Avisado`.
- `DatosImplante_implante_piernas_1`: precio 0. Todos los nivel II: ligaMinima 1 (script `ImplantesT1005.Aplicar`; `CreadorImplantes` quedó con los mismos valores).
- `ServicioImplantes.Instalar`: un costo 0 no cobra (la cartera no deja "pagar" 0). `ReglaImplantes.Oferta`: lo gratis se ofrece aun con deuda.
- Clínica (`PantallaClinica`, `ConfigPantallaClinica`): con costo 0 muestra GRATIS en la fila, la etiqueta, el botón y "¿INSTALAR GRATIS?".
- `ServicioImplantes.Regalar` queda sin uso en el juego (solo existía para el regalo); no se borró.
- Pruebas: RegaloImplanteS2tPruebas, ServicioImplantesGuardadoPruebas, BalanceImplantesPruebas (EditMode); ClinicaPruebas, ImplantesDocPruebas, RecorridoImplantesPruebas, HistoriaDemoPruebas (PlayMode).
