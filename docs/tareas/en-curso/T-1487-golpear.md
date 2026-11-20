# T-1487: golpear a pie se siente bien

Rama: tony/T-1487-golpear. Estado: código listo, sin compilar ni correr pruebas (el worktree no tiene Library).

## Qué cambió
- **Asistencia de objetivo** (ya existía como auto-giro T-1003/T-1327): ahora sus valores salen de `ConfigGolpeAPie`: alcance 3 m, cono 70°, giro 900 °/s (suave, y al impactar se termina de girar), acercamiento máx. 1.2 m a 7 m/s, se detiene a 1 m. Se mueve con el CharacterController: respeta colisiones.
- **Combo** (`ReglaComboGolpe`, pura): 3 golpes, buffer de clic 0.3 s, el siguiente golpe arranca en cuanto la recuperación pasa del 45 %, sin esperar a Listo ni a que acabe la animación (`GolpeAPie.Update`). El tercero es el final (animación GolpeFuerte vía `AnimacionJugador`, solo la línea `visual.Golpear(golpe.EsGolpeFinal)`), no encadena. Sin golpear 0.7 s: vuelve al primero. KO, derribo o cambio de arma: cancela combo y buffer.
- **Al conectar** (`SensacionGolpe`, en el jugador; `GolpeAPie` lo agrega solo): hitstop 50 ms (80 ms el final) congelando `Animator.speed` del atacante y del golpeado con tiempo real (`TemporizadorHitstop`; no toca Time.timeScale; se restaura en OnDisable y con timeScale 0); impulso de cámara con `TemblorCamara.Preparar/Generar` (API de T-1485; fuerza 0.8, final 1.5, dirección del golpe); sonido MEL_GOLPE_CARNE (el mezclador ya varía el tono ±5 %) y MEL_GOLPE extra en el final; empuje al golpeado (`EmpujeReceptorGolpe` + `EmpujeLineal`): 2.5 m/s normal, 6 m/s final, 0.25 s. Rigidbody: lo sigue empujando BRAZOS. La reacción de golpe recibido (`visual.Herir`) ya la hacían los enemigos; no se tocó IA/.
- Valores en `Personaje/Datos/ConfigGolpeAPie.cs`; sin asset usa `ConfigGolpeAPie.PorDefecto`.

## Métodos de editor
- `MechaCyberpunk.Personaje.Editor.CrearConfigGolpeT1487.Crear`: crea `Datos/ConfigGolpeAPie_Base.asset` y se lo asigna a `GolpeAPie` en JugadorAPie.prefab. Opcional (sin él funciona igual con los valores de código).
- Los .meta de los archivos nuevos los genera Unity al abrir/compilar; van en el commit de integración.

## Pruebas
- EditMode (Tests/EditMode/Personaje): ReglaComboGolpeT1487Pruebas, TemporizadorHitstopT1487Pruebas, EmpujeLinealT1487Pruebas.
- PlayMode: SensacionGolpeT1487Pruebas (hitstop restaura, con timeScale 0 y al desactivar; el final empuja más). Falta prueba con entrada simulada de GolpeAPie completo: requiere el prefab y Library.

## Qué probar en juego
- Tres clics rápidos: combo seguido sin volver a guardia; el tercero más fuerte; sin clic 0.7 s empieza otra vez.
- Enemigo a 3 m en el cono: el cuerpo gira y se acerca sin atravesarlo.
- Se siente el hitstop, el temblor (requiere el listener de T-1485) y el empuje.
- Verificar en el Animator que GolpeIzq/GolpeDer/GolpeFuerte se pueden interrumpir entre sí sin pasar por idle; si no, ajustar transiciones (no se tocó el controller).

## Animaciones que faltan
Ver sección T-1487 en docs/animaciones-faltan.md (todo opcional).
