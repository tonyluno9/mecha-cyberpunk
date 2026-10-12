# T-053 Pistola a pie
Dueño: Tony | Rama: tony/T-053-pistola | Modelo: Sonnet 5.5, medium
Depende de: T-050 en main (`TipoDanio.Disparo`), T-052 (golpe, arma activa y muñeco)

## Objetivo
El jugador a pie cambia entre puños (1) y pistola (2) y dispara con clic izquierdo: tiro instantáneo al centro de la vista, con
cargador, recarga y cadencia. Completa el combate a pie básico de combate.md.

## Contexto
- Diseño: docs/diseno/combate.md (a pie: pistola y golpes simples). La munición limitada que se compra es de las habilidades del
  mecha, no de la pistola.
- Decisiones: D-009 (ignorar el propio `transform.root`, sin capas). Propuestas: D-tony-15 (daño a pie con `TipoDanio.Disparo`),
  D-tony-19 (pistola) en docs/decisiones-pendientes-tony.md.
- Las acciones ArmaPunos (1), ArmaPistola (2), Atacar (clic izquierdo) y Recargar (R) ya están en ControlesAPie.inputactions (T-051).
- Muñeco de prueba: Personaje/Pruebas/MunecoAPie.prefab (T-052).

## Qué hacer
1. `Personaje/Datos/DatosPistola.cs` (ScriptableObject, la pistola se podrá mejorar): daño (25 puntos), alcance (60 m), tiempo
   entre disparos (0.25 s), balas por cargador (8), tiempo de recarga (1.4 s). Asset `DatosPistola_Base.asset`.
   En esta fase la reserva de balas es infinita (solo el cargador se acaba).
2. Lógica pura `Personaje/Combate/CargadorPistola.cs`: balas en el cargador, `IntentarDisparar()` (solo con balas, sin recargar y
   pasado el tiempo entre disparos), `EmpezarRecarga()` (no si está lleno o ya recarga), `Avanzar(dt)`, `Recargando`, y avisos de
   disparo, clic vacío y recarga terminada.
3. `Personaje/Combate/ArmasAPie.cs`: arma activa (Puños o Pistola) con 1 y 2; Atacar va al golpe o a la pistola según el arma;
   no se cambia de arma durante la preparación de un golpe. Expone el arma activa, las balas, el tamaño del cargador y si recarga
   (el HUD los lee en T-056), y un evento al cambiar de arma o de balas.
4. `Personaje/Combate/PistolaAPie.cs`: con la pistola en la mano, Atacar dispara un `Physics.RaycastNonAlloc` desde la cámara al
   centro de la vista, hasta el alcance, ignorando triggers y el propio root; el primer collider que no es del jugador decide:
   si tiene `IRecibeDanio` en sus padres recibe `InfoDanio` con `TipoDanio.Disparo`; si no, el tiro se queda en la pared.
   R recarga; con el cargador vacío, Atacar empieza la recarga sola. Visual greybox: pistola de cubos en `Manos`, una luz
   breve en la boca del cañón y un retroceso corto; una marca pequeña donde pegó que se borra en 2 s (máximo 16 marcas).
5. Creador: agrega `ArmasAPie` y `PistolaAPie` al prefab, y en Pruebas_APie un muñeco a 20 m y otro a 80 m. Regenera.

## Carpetas permitidas
Assets/_Proyecto/Personaje/, Assets/_Proyecto/Escenas/Tony/Pruebas_APie.unity, Assets/_Proyecto/Tests/EditMode/Personaje/ y
Assets/_Proyecto/Tests/PlayMode/Personaje/
No tocar: Core/, Combate/, Mecha/, las carpetas y escenas de Alan, Pruebas_Tony.unity.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa
- [ ] EditMode CargadorPistola: 8 disparos y el noveno es clic vacío; no dispara antes del tiempo entre disparos; la recarga tarda
      lo de los datos y llena el cargador; no se puede disparar recargando; recargar lleno no hace nada
- [ ] PlayMode con entrada simulada: con 2 y un clic frente al muñeco a 20 m, el muñeco recibe el daño de la pistola con
      `TipoDanio.Disparo` (comprobado escuchando `ImpactoRecibido` o leyendo su vida)
- [ ] PlayMode con entrada simulada: el muñeco a 80 m (fuera de alcance) no recibe daño; un muro entre el jugador y el muñeco
      lo protege
- [ ] PlayMode con entrada simulada: 20 clics seguidos en 1 s disparan como máximo 5 veces (cadencia 0.25 s); después de 8 disparos
      el siguiente clic no hace daño y empieza la recarga; tras 1.4 s vuelve a haber 8 balas
- [ ] PlayMode con entrada simulada: con 1 el clic vuelve a dar golpes (T-052) y no dispara; con el jugador en KO no dispara
- [ ] PlayMode: disparando al piso a sus pies, el jugador no se hace daño (ignora su propio root)
- [ ] Captura del disparo con la luz del cañón y la marca en un muro: docs/capturas/T-053-1.png
- [ ] Consola sin errores en Pruebas_APie disparando 3 cargadores seguidos

## Criterios de sensación (para un humano)
- Disparar se siente seco y claro: se nota el tiro, el retroceso y dónde pegó.
- Cambiar con 1 y 2 y recargar con R se siente natural; el clic vacío se entiende.

## Pruebas automáticas
- EditMode (Tests/EditMode/Personaje/): CargadorPistolaPruebas.
- PlayMode (Tests/PlayMode/Personaje/): PistolaAPiePruebas con InputTestFixture; muñecos y muros creados en la prueba.

## Fuera de alcance
- Comprar balas o pistolas (T-F7), apuntar con la mira, dispersión, armas automáticas
- Sonido de disparo (pasa a la tarea de audio); HUD de balas (T-056)
- Que el mecha reciba disparos de pistola de forma especial (si un tiro pega a un mecha, recibe el daño normal)
