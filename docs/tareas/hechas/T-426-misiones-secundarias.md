# T-426 Misiones secundarias repetibles en el juego libre
Dueño: Tony | Rama: tony/T-426-misiones-secundarias | Modelo: Sonnet 5.5 | MODO RÁPIDO (docs/plan-noche.md, paso 5; mejora 6 de docs/mejoras-pendientes.md)
Tony autorizó tocar todas las áreas y Core; no hizo falta Core.

## Objetivo
Encargos repetibles en el juego libre, con marcador de objetivo y marca en el mapa (T-408/T-409), premio en Biits y respeto, enfriamiento y guardado mínimo.

## Qué se hizo
1. Tres tipos (Progresion/Prologo/Encargos/): COBRAR CUOTA (3 locales de SUBURBIOS, 50 Ƀ cada uno + 100 Ƀ; a veces un local se resiste y manda un moroso a puños), DEFENDER ZONA (oleada de 4 rivales a puños en un punto de SUBURBIOS, 250 Ƀ) y ROBAR CARRO (un CarroRobable_N marcado; se sube y se lleva a CANEPA, 300 Ƀ). Cada uno suma respeto a SUBURBIOS.
2. Se piden con E en el TABLERO DE ENCARGOS junto a la puerta de tu casa; ofrece el tipo que toca (rota) y E durante un encargo lo cancela. Solo con la historia en el juego libre (en cualquier paso de la historia el tablero dice "TERMINA LA HISTORIA" y un encargo en curso se deja sin castigo).
3. Enfriamiento: 300 s tras éxito, 60 s tras fallo, por tipo. Guardado con la clave `encargos` (enfriamientos, contadores y rotación) por el sistema de T-412.
4. Reglas puras en ReglaEncargos; DirectorEncargos lleva las fases; el creador T426 (CreadorCiudadPandillasT426, parte de Todo()) pone la raíz GTA_T426 en Ciudad_Prologo.
5. Pendiente: escoltar aliado y carreras con checkpoints.

## Pruebas
EditMode ReglaEncargosPruebas (9) y PlayMode EncargosPruebas (2: cuota y defender con entrada simulada; robar y entregar). Captura docs/capturas/T-426-1.png.
