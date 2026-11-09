# T-933 Policía y testigos

Rama: tony/T-933-policia-testigos. Playtest 09-nov.

Regla: el calor solo sube si lo ve la policía (una patrulla o un policía a pie de una patrulla, `ITestigoPolicia`); con nivel mayor que 0 suma sin preguntar. Hay patrullas de ronda sin sirena (`patrullasDeRonda`, sin tocar el número); si una ve a un criminal atacando (`ICriminalCalle`, lo implementa PandilleroAPie) va y baja a sus policías; al empezar la búsqueda una ronda pasa a perseguir. Un policía a pie no ataca al jugador con nivel 0.

Se borró (cámaras y civiles que llaman): CamaraSeguridad, CalculoCamaras, CalculoVistaCamara, ReglaLlamadaCivil, CreadorTestigos, IndicadorLlamada, el prefab CamaraSeguridad y sus materiales, Trafico_IconoLlamada.mat, los campos de cámara y civil de ConfigPersecucion y la llamada de PeatonMundo (Llamando, IniciarLlamada, TerminarLlamada, MantenerTelefono, Tumbado, RadioAbandonoPropio). El conductor robado ya no llama: al levantarse huye. VisualPersonaje.Telefonear se conserva (es del ensamblado Personaje.Visual).

Pruebas: borradas CamarasT662, CalculoCamaras, ReglaLlamadaCivil; reescritas TestigosT661 y RobarConConductorT705; nuevas PoliciaRondaT933 (Trafico) y PoliciaNivelCeroT933 (IA); las que usaban una cámara de testigo (PandillaArmada, TraficoRobar, CiudadCarros, PoliciasT709) usan un `ITestigoPolicia` de prueba; las que contaban patrullas ahora no cuentan las rondas.
