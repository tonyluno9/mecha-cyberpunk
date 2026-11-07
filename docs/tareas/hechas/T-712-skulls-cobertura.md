# T-712 SKULLS en cobertura, agachados
Dueño: Tony (S1) | Rama: tony/T-712-skulls-cobertura | Modelo: Sonnet 5.5
Depende de: T-423, T-683, T-711

## Objetivo
Los SKULLS se cubren agachados detrás de algo sólido (muro, carro, contenedor): corren a la cobertura, se agachan, asoman, disparan y se esconden. Antes se cubrían parados.

## Qué se hizo
- PandilleroAPie: al llegar a su puesto de cobertura (modo Cubrirse) pone VisualPersonaje.Agachado (estado AgachadoQuieto con la pose baja de Cobertura_Entrar, de T-683); al asomarse (Asomado) se para, apunta y dispara; tras disparar vuelve a esconderse. Corriendo hacia la cobertura va de pie.
- Los SKULLS armados (no el jefe) corren a cubrirse en cuanto ven al jugador y tienen una cobertura cerca (antes solo después del primer disparo; los policías ya lo hacían).
- La cobertura debe medir 1.3 m o más: agachado la cabeza llega a 1.33 m (medido con la malla horneada), así que un muro de 1.4 m lo tapa y uno de 1 m no (no se agacha en campo abierto).
- Cada uno toma su propio puesto detrás de la cobertura (T-711), y solo uno desde el que alcance a disparar al jugador (a 12 m o menos): si no, se quedaba escondido sin pelear (lo vio la prueba de T-714 con el carro estacionado a 12 m).

## Criterios
- [x] Corre a la cobertura del lado contrario al jugador
- [x] Llegado, el Animator juega AgachadoQuieto y la cabeza queda por debajo del muro
- [x] Agachado no dispara; asomado se para y dispara; se repite (2 ciclos)
- [x] Un muro bajo (1 m) no sirve: no se agacha en campo abierto
- [x] Capturas docs/capturas/s1c-T-712-*.png (agachado y asomando)

## Pruebas
- EditMode IA: CicloCubiertaT712Pruebas (3)
- PlayMode IA: CubiertaSkullsT712Pruebas (3), con el jugador real

## Fuera de alcance
- Animación nueva de agachado (la pose agachada es la de T-683, con la cabeza a 1.33 m; Crouch Idle sigue pendiente en docs/animaciones-faltan.md)
