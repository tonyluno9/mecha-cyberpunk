# T-148 Kits de reparación en plena pelea
Dueño: Tony | Rama: tony/T-148-kits-reparacion | Modelo: Sonnet 5.5, medium
Depende de: T-142, T-143 (conviene después de T-146, que los vende)

## Objetivo
Dentro de una pelea, con la tecla G el jugador gasta un kit de reparación de su inventario y su mecha recupera un poco de vida en un
segundo y medio. Hay un tope por pelea y la cabina dice cuántos kits quedan. Da a los kits del mercado negro su razón de existir y una
decisión táctica (curarte a media pelea cuesta más que reparar en el taller).

## Contexto
- Diseño: mecha-taller.md ("Hay kits de reparación limitados del mercado negro para curar un poco en plena pelea"), combate.md (en PC las
  acciones de cabina van a teclas; el centro de la vista queda libre).
- Decisiones: D-011 (nada tapa el centro de la vista en la cabina), D-tony-42 y D-tony-59 (vida persistente del mecha de la historia),
  D-tony-49 (Mecha referencia Economia). Propuesta nueva: D-tony-83 (reglas y tecla) y D-tony-86 (valores).
- `VidaMecha.FijarVida(float)` cambia la vida sin publicar impacto ni KO; `EventosPelea.PeleaIniciada` y `PeleaTerminada` marcan la pelea.
- Ejemplos: `UI/Cabina/PantallaVidaCabina.cs` y `PantallaCalorCabina.cs` (pantallas de cabina), `Economia/Pruebas/DineroDePrueba.cs`
  (InputAction con tecla por defecto en un campo), `Tests/PlayMode/Mecha/MechaDaniadoPruebas.cs`. Usa el helper de mapas de acciones de T-140.

## Qué hacer
1. Regla pura `Mecha/Kits/ReglaKitReparacion.cs`: decide si se puede usar un kit (en pelea, mecha no en KO, vida menor que la máxima, kits en
   el inventario > 0, usados en esta pelea < tope, sin otro kit en curso) y cuánto cura (fracción de la vida máxima, sin pasar del máximo).
2. `Mecha/Kits/UsoKitReparacion.cs` en `MechaJugador.prefab` (con el creador del prefab; si `MechaEstacionado.prefab` es otro prefab, también):
   acción "UsarKit" en G (campo `InputAction` con `<Keyboard>/g`), activa solo con el jugador piloteando. Al usar: quita el kit del inventario
   al instante y sube la vida de forma pareja durante `segundosCuracion` con `VidaMecha.FijarVida`; si el mecha cae en KO a la mitad, la
   curación se corta. El contador de usos se reinicia con cada `PeleaIniciada`. Valores en `ConfigKitReparacion` (Mecha/Datos/): cura 20 % de la
   máxima, 1.5 s, tope 2 por pelea (D-tony-86).
3. Pantalla de cabina `UI/Cabina/PantallaKitsCabina.cs` (o una línea en una pantalla existente, fuera del 40 % central): "KITS 2 · G",
   "REPARANDO…" mientras cura, "SIN KITS" con 0 y "LÍMITE DE KITS" al llegar al tope. Sin `ServicioInventario` en la escena, no se muestra.
4. PlayMode `Tests/PlayMode/Mecha/KitReparacionPruebas.cs`: pelea con `ArenaDePrueba`, `MechaJugador` y un rival al que se le quita el control
   tras el conteo (para que la vida medida no cambie por golpes), `ServiciosEconomia.prefab` instanciado y kits dados por código; tecla G simulada.

## Carpetas permitidas
Assets/_Proyecto/Mecha/ (Kits/, Datos/, Editor/, Prefabs/ solo con su creador), Assets/_Proyecto/UI/ (Cabina/, Datos/, Editor/),
Assets/_Proyecto/Tests/EditMode/Mecha/, Assets/_Proyecto/Tests/PlayMode/Mecha/, docs/capturas/
No tocar: Core/, Economia/ (solo se usa), Combate/, Pelea/, escenas que no sean de Tony, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] ReglaKitReparacionPruebas (EditMode): fuera de pelea, en KO, con vida completa, sin kits o al tope no se usa; con vida 95 de 100 cura
      solo 5
- [ ] KitReparacionPruebas a): con 3 kits y la vida en 50/100 tras "¡PELEA!", G: kits 2 al instante y la vida llega a 70 (±0.5) entre 1.4 y 1.7 s
      después, subiendo en al menos 5 cuadros distintos (no de golpe)
- [ ] b) segundo G (al terminar el primero): kits 1, vida 90; tercer G en la misma pelea: no cambia nada y la cabina dice "LÍMITE DE KITS";
      en la pelea siguiente (revancha o nueva) se puede volver a usar
- [ ] c) G durante el conteo, con 0 kits o en KO: no cambia vida ni inventario
- [ ] d) la pantalla de kits queda fuera del 40 % × 40 % central de la vista (medido como en T-134) en 16:9 y 4:3
- [ ] Captura docs/capturas/T-148-1.png: la cabina con "KITS 2 · G" y el mecha dañado

## Criterios de sensación (para un humano)
- Pruebas_Economia: compra 2 kits, pelea la casera y usa G con vida baja. ¿Curarte se siente como un respiro en plena pelea y no como trampa?
- ¿1.5 s de curación da tiempo a que el rival te castigue lo justo? ¿"KITS 2 · G" se ve sin buscarlo?

## Pruebas automáticas
- EditMode: ReglaKitReparacionPruebas. PlayMode: KitReparacionPruebas.

## Fuera de alcance
- Kits a pie o en la calle fuera de una pelea. Animación o partículas de reparación. Sonido. La ayuda de controles (T-134) no cambia.
