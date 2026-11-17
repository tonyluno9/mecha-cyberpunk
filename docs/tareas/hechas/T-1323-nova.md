# T-1323 Enemigos de Nova (playtest 17-nov, sección 5 y sonido de 5 estrellas)

Rama: tony/T-1323-nova. Escrito sin abrir Unity (no compilado ni probado por el implementador).

## Qué se hizo y por qué
1. **Disparo a la cabeza sin daño.** Causa probable (por lectura de código, falta medir en Unity): la cápsula del CharacterController mide 1.8 m y los modelos E12 (2.05 m) y
   E14 (2.3 m) sobresalen; la bala (`PistolaAPie.BuscarImpacto`, ignora triggers) pasa por encima de la cabeza sin tocar nada. Arreglo: `IA/APie/ZonaCabezaNova.cs`
   pone una esfera sólida (r 0.18 m) en el hueso Head, hija del agente, así que `GetComponentInParent<IRecibeDanio>` llega al PandilleroAPie y `ReglaZonaDanio` la cuenta
   como cabeza (x3). `RespuestaNova` la agrega a cada agente al aparecer (sin editar prefabs).
2. **Persecución.** Antes `RespuestaNova` los destruía a los 6 s bajo 4 estrellas aunque ya te vieran. Ahora el agente que estuvo a <= `DistanciaVista` (40 m) ya "te vio"
   (`YaVioAlJugador`) y sigue persiguiéndote con 1-3 estrellas; solo se van cuando la búsqueda es 0 (Doc, arresto), los que nunca te vieron se retiran como antes.
   Reaparición rara de un guardia: no pude aislar una causa única; lo más probable era la reposición por oleada (aparecía de la nada a 22-34 m). Ahora cae del cielo con destello.
3. **Caída desde arriba.** `Aparecer` instancia a `AlturaCaida` (12 m, o lo que deje libre un techo; menos de 3 m = en el piso) sobre el punto; la gravedad del PandilleroAPie
   y su Animator (EnPiso/VelY: Loco_Caida y aterrizaje que ya existían) hacen la caída. `DestelloLlegadaNova.cs`: luz azul + chispas (material existente) al salir y al aterrizar.
4. **5 estrellas.** Al llegar a `EstrellasBusqueda.Total` suena `config.SonidoLlegada` (una vez por búsqueda; evento `LlegadaEspecial`) y la oleada baja del cielo.
5. **Escudo chico.** `ConfigRespuestaNova.EscalaPesado` (1.15) escala el hijo `Modelo` del pesado al aparecer. El valor es una estimación: ajustar viendo al pesado junto a un guardia.

## Aplicar (idempotente)
`MechaCyberpunk.IA.Editor.AplicarNovaT1323.Aplicar` (-executeMethod): pone `Mat_ChispaEscudoNova` como material del destello en `IA/Resources/ConfigRespuestaNova.asset`.
Sin correrlo solo hay luz (sin chispas); no falla.

## Pruebas
- PlayMode `NovaT1323Pruebas`: caída + destello + aterrizaje, cabeza (collider, EsCabeza, daño), persecución tras bajar estrellas y retiro sin búsqueda, aviso único de 5
  estrellas, escala del pesado. EditMode `NovaT1323ReglasPruebas`: fórmula de caída, config por omisión, constantes de la cabeza.
- `RespuestaNovaT1304Pruebas`: el último paso pasa de 2 estrellas a 0 (la nueva regla de persecución).

## Decisiones
- "Pasó de 5 estrellas" = `Estrellas >= EstrellasBusqueda.Total`; no toqué las reglas de la policía (T-1322).
- Sonido del aviso: se usa `PAN_JEFE_ENTRADA` (ya existe y está encendido) en `ConfigRespuestaNova.SonidoLlegada`; no se agregó ID nuevo.

## Assets que faltan
- Sonido propio de llegada de Nova / 5 estrellas (hueco: `SonidoLlegada`, hoy la entrada de jefe). Sonidos de enemigos nuevos (Nova, escudo).
- Arma especial de Nova que dispare: NO hecha (falta el modelo).
- Naves/helicópteros que bajan enemigos con sogas (fuera de esta tarea).
