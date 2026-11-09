# T-936 Disparar agachado

Rama: tony/T-936-disparar-agachado (sale de T-935). Playtest 09-nov.

- PistolaAPie: agachado, la pulsación de disparar ya no se descarta: el jugador se pone de pie (AlternarAgachado) y dispara con esa misma pulsación. Si hay techo encima y no cabe de pie, dispara agachado.
- Prueba nueva AgacharseT673Pruebas.Agachado_UnaSolaPulsacionDeDisparar_SePoneDePieYHaceDanioReal: un MunecoAPie recibe daño con un solo clic estando agachado. AnimatorPJ01T733Pruebas ya no dice "agachado no dispara": ahora comprueba que la primera pulsación dispara (resta una bala) y se para.
- Resultados PlayMode (Personaje): AgacharseT673, EventosPistolaT734, AnimatorPJ01T733: 21/21.
