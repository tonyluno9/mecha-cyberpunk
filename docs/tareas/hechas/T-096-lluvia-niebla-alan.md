# T-096 Lluvia y niebla
Dueño: Alan | Rama: alan/T-096-lluvia-niebla | Modelo: Sonnet 5.5, medium
Depende de: T-079 (posproceso y asfalto mojado).

## Objetivo
Lluvia fina que sigue al jugador y niebla baja entre los edificios, de noche, sin costar fotogramas.

## Qué hacer
1. `Mundo/Ambiente/LluviaSigueJugador.cs`: un `ParticleSystem` en caja de 30 × 15 × 30 m que sigue a `Registro<IJugador>.Actual` (o a la cámara principal), apagado dentro de interiores (`ZonaInterior.JugadorDentro`) y de día si la config lo pide. Sus números salen de `ConfigLluvia` (Datos/).
2. Niebla: subir la densidad de noche con la hora (ConfigCicloDia ya trae colores) y una franja de niebla baja con partículas en las calles principales.
3. Un interruptor `Lluvia.Activa` para el director del prólogo (Tony lo puede llamar sin contrato nuevo: solo un MonoBehaviour en la escena).

## Carpetas permitidas
Assets/_Proyecto/Mundo/ (Ambiente/, Datos/, Editor/), Tests de Mundo, docs/capturas/.

## Criterios de aceptación
- [ ] Con la cámara dentro del bar o la casa la lluvia está apagada; afuera, encendida (prueba)
- [ ] Medición de fotogramas del recorrido (MedidorFotogramas): sin frames de más de 100 ms
- [ ] Capturas de la calle con lluvia: docs/capturas/T-096-1..2.png
- [ ] `bash herramientas/verificar.sh todo` pasa
