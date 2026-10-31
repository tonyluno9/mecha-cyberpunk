# Bugs y pendientes para la Sesión A

- **T-470 Ambiente apagado por las opciones**: `Progresion/Opciones/ReglaOpciones.VolumenInicial` deja Ambiente y Música en 0 y `ServicioOpciones.AplicarVolumenes` lo aplica al gestor. Para que el AMB suene hoy, `CatalogoSonidos.pisoOpcionAmbiente` (0.8) sube el ambiente al menos a ese nivel. Cuando cambien el defecto de Ambiente a 0.8 (y ajusten `RecorridoRonda2Pruebas`, que espera `Volumen(Ambiente) == 0`), poner `pisoOpcionAmbiente` en 0 en CatalogoSonidos.asset para que el slider mande.
- **T-470 ARM_SUB_* vs ARM_FUS_***: PistolaAPie pide ARM_SUB_DISPARO/RECARGA para el fusil y los archivos se llaman ARM_FUS_*. El catálogo los redirige (alias); renombrar el ID en `PistolaAPie.IdDisparo/IdRecarga`.
