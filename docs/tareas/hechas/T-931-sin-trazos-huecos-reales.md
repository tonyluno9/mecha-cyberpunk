# T-931 Sin trazos de bala y huecos solo sobre superficies reales

Rama: tony/T-931-sin-trazos-huecos-reales (sale de T-930). Playtest 09-nov.

- Trazos: borrados Combate/Efectos (TrazadorBala y ReglaTrazo), la copia en Vehiculos/Efectos, el material Trazo_URP, el LineRenderer "Laser" de PistolaPandillero (y su material PistolaPandillero_Laser) y los usos en PistolaAPie, PistolaPandillero y DronMundo. Se queda el fogonazo y los sonidos (el aviso del pandillero sigue sonando, ya sin hilo).
- Regla pura nueva en ReglaHuecosBala: HayHueco (no trigger, no persona, hay superficie visible), PuntoEnCaja (margen 5 cm) y SeMovio (2 cm). SuperficieImpacto.Visible aplica la regla a un collider: TerrainCollider, Renderer propio activo, o Renderer activo de padres/hijos cuyos límites ampliados contienen el punto (ignora partículas, líneas y estelas).
- HuecosBala.SimularImpacto y PistolaAPie.PonerMarca usan la regla. El hueco guarda la superficie y su punto local; cada cuadro se apaga si la superficie se destruyó o apagó, o si (solo si no es estática y el hueco no va pegado a un cuerpo rígido) el punto se movió más de 2 cm.
- Decisión: los huecos de metal de un carro siguen pegados a su Rigidbody (no flotan), así que no se vigila su movimiento; solo si el carro se destruye o apaga.
- Pruebas: EditMode 2252/2252. PlayMode: HuecosBalaPruebas 3/3 (dos nuevas), EfectosCombateT745Pruebas 7/7, DronDisparaT710Pruebas 2/2, BalasT676Pruebas 2/2.
