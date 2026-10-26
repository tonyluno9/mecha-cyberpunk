# T-186 Dónde salen las misiones: teléfono de día, contacto del bar de noche y encargos en la calle; reputación a la vista
Dueño: Tony | Rama: tony/T-186-telefono-bar-encargos | Modelo: Sonnet 5.5, medium
Depende de: T-184 (puede ir en paralelo con T-185)

## Objetivo
El jugador encuentra y acepta las misiones como dice el diseño: las corporativas por teléfono (tecla T, de día), las ilegales y los robos con
el contacto del bar (de noche) y las del barrio con los vecinos en la calle (E en el encargo). El teléfono también muestra la misión en curso
(con abandonar) y las dos reputaciones con su nivel, y el HUD avisa cada cambio ("BARRIO +8 · CORPORACIONES −5"), así que aceptar un contrato
corporativo baja el barrio de forma visible.

## Contexto
- Diseño: economia-misiones.md ("Las misiones del barrio salen en la calle, las corporativas por teléfono de día y las ilegales y robos en el bar
  de noche"), concepto-historia.md (criterio "Aceptar un contrato corporativo baja la reputación del barrio de forma visible"), estetica-hud.md.
- Decisiones: D-tony-81 (mostradores con E, Alan coloca el prefab), D-tony-147, D-tony-151. Propuesta: el teléfono es la tecla T a pie.
- Código modelo: `UI/Tiendas/MostradorTienda.cs` y `PantallaTienda.cs` (IInteractuable, abierto según el `ILugar`, congela `ControladorAPie`,
  ← → pestañas, ↑ ↓ elegir, Enter, Esc), `UI/Peleas/PantallaCartelera.cs`, `UI/HudAPie/HudAPie.cs` (avisos de dinero y de liga con tiempo),
  `UI/HudAPie/FormatoHud.cs` (texto por `MotivoDinero`), `Personaje/Editor/CreadorJugadorAPie.cs` (acciones del mapa `APie`; anida
  `HudAPie.prefab` en `JugadorAPie.prefab` por ruta), `UI/Editor/CreadorMostradorTienda.cs`.
- Contratos y servicios: `ServicioMisiones.Ofertas/Motivo/Aceptar/Abandonar` y sus eventos (T-184), `IReputacion` (T-181), `IRelojMundo`,
  `ILugar` (Bar).

## Qué hacer
1. Acción `Telefono` (`<Keyboard>/t`) en el mapa `APie` de `ControlesAPie.inputactions`, agregada con el creador de Personaje sin regenerar lo
   demás (las acciones que ya existen conservan sus ids).
2. `UI/Misiones/PantallaMisiones.cs` (Canvas Overlay, se reusa en los tres orígenes): título, lista de ofertas (↑ ↓) con su título, pago,
   chatarra, efecto en reputación ("BARRIO +8 · CORP. 0") y favor; las no disponibles en gris con su motivo en palabras ("DE NOCHE",
   "NECESITAS SER CONOCIDO EN EL BARRIO", "YA LO HICISTE HOY", "LIGA 2", "NECESITAS 1 INFORMACIÓN", "YA TIENES UNA MISIÓN"); Enter acepta la
   elegida si está disponible; Esc cierra. Congela el control a pie mientras está abierta (como la tienda). Una sola, anidada en `JugadorAPie.prefab`; el teléfono,
   el contacto y los encargos la encuentran con `Registro<PantallaMisiones>`.
3. `UI/Misiones/PantallaTelefono.cs` (tecla T, solo a pie, no con otra pantalla abierta), anidada en `JugadorAPie.prefab` como `HudAPie`.
   Pestañas (← →): CONTRATOS (de día, las corporativas con la `PantallaMisiones`; de noche "NADIE CONTESTA · LLAMA DE DÍA"; con corporaciones en
   −2 "YA NO TE LLAMAN"), MISIÓN (título, paso actual, tiempo restante si tiene límite, "Enter · ABANDONAR" con confirmación; sin misión
   "SIN MISIÓN"), REPUTACIÓN (dos barras de −100 a 100 con el valor y el nombre del nivel). T o Esc cierra.
4. `UI/Misiones/ContactoBar.cs` + `UI/Prefabs/ContactoBar.prefab` (IInteractuable; Alan lo coloca en su bar): "E · HABLAR CON EL CONTACTO"
   abre la `PantallaMisiones` con las de origen `Bar`. Abierto según el `ILugar` Bar (sin ese lugar, abierto) y solo de noche (de día, aviso
   "EL CONTACTO LLEGA DE NOCHE", E no hace nada).
5. `UI/Misiones/EncargosCalle.cs` (es UI, así que va junto al teléfono en `JugadorAPie.prefab`, no en `ServiciosProgresion`): un `IInteractuable` por cada `IPuntoMision` de tipo `Encargo` que tenga una misión de barrio que
   ofrecer; texto "E · AYUDAR: <TÍTULO>" (o el motivo si no está disponible, sin E); abre la `PantallaMisiones` con esa misión. Llama
   `Activar(true)` al punto mientras tiene algo que ofrecer.
6. HUD a pie (`HudAPie`): aviso de reputación al cambiar ("BARRIO +8 · CORPORACIONES −5", 3 s; con cambio de nivel, una segunda línea "AHORA
   ERES CONOCIDO EN EL BARRIO"); aviso al terminar una misión ("MISIÓN CUMPLIDA · +$50 · DESCUENTO" o "MISIÓN FALLIDA"); el aviso de dinero dice
   "MISIÓN" con `MotivoDinero.Mision`; la misión en curso usa el objetivo con flecha que ya existe.
7. `CreadorEscenaMisiones`: coloca `ContactoBar.prefab` en el bar de prueba.
8. Pruebas PlayMode `Tests/PlayMode/Progresion/Misiones/OrigenMisionesPruebas.cs` en Pruebas_Misiones, todo con entrada simulada (WASD, E, T,
   flechas, Enter, Esc, J; J lleva a las 8:00 y para volver a la noche se usa `AvanzarHasta(21)` por código, atajo anotado):
   a) de noche, T → CONTRATOS dice "NADIE CONTESTA"; J (día) → lista las 3 corporativas, LIMPIEZA DE ZONA en gris con su motivo; Enter acepta
      ENTREGA URGENTE; MISIÓN muestra su paso y el tiempo; ABANDONAR con confirmación la cancela (corporaciones −3; el aviso lo dice);
   b) E en Doña Lupe → la pantalla muestra LA TIENDITA con "BARRIO +8" y DESCUENTO; Enter acepta y el objetivo apunta a la farmacia;
   c) de día, E en el contacto del bar dice "EL CONTACTO LLEGA DE NOCHE"; de noche lista CARGA CALIENTE, PELEA SIN JAULA, BODEGA, y EL SOPLO en
      gris ("NECESITAS 1 INFORMACIÓN");
   d) cumplir COBRO DE ADEUDO: el HUD avisa "BARRIO −12 · CORPORACIONES +10" y la pestaña REPUTACIÓN muestra −12 en barrio (el criterio del diseño);
   e) con una pantalla abierta el jugador no camina; con la tienda abierta, T no abre el teléfono.
   Consola sin errores; `MedidorFrames` sin tirones.

## Carpetas permitidas
Assets/_Proyecto/UI/Misiones/ (nueva), Assets/_Proyecto/UI/HudAPie/, Assets/_Proyecto/UI/Datos/ (configs nuevas), Assets/_Proyecto/UI/Editor/,
Assets/_Proyecto/UI/Prefabs/ (nuevos y `HudAPie.prefab`, solo con creadores), Assets/_Proyecto/Personaje/Entrada/ControlesAPie.inputactions (solo
con el creador), Assets/_Proyecto/Personaje/Editor/CreadorJugadorAPie.cs, Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab (solo con el creador),
Assets/_Proyecto/Progresion/Prologo/Editor/CreadorEscenaMisiones.cs, Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity (solo con el creador),
Assets/_Proyecto/Tests/PlayMode/Progresion/Misiones/, Assets/_Proyecto/Tests/PlayMode/UI/ (si una prueba del HUD cambia), docs/capturas/
No tocar: Core/, Progresion/Misiones/ (si falta algo del servicio, se anota y se pide), Pelea/, las otras escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena)
- [ ] OrigenMisionesPruebas pasa 3 corridas seguidas con los textos y valores del paso 8
- [ ] Las pruebas de HUD, tienda, cartelera y jugador a pie que ya existían siguen en verde (la acción nueva no cambia las teclas viejas)
- [ ] Correr los creadores dos veces no cambia prefabs, escena ni `ControlesAPie.inputactions` (md5 igual)
- [ ] Capturas docs/capturas/T-186-1.png (teléfono, pestaña CONTRATOS de día), T-186-2.png (pestaña REPUTACIÓN después del cobro de adeudo) y
      T-186-3.png (pantalla del contacto del bar de noche con EL SOPLO en gris), revisadas a ojo

## Criterios de sensación (para un humano)
- Pruebas_Misiones: ¿el teléfono con T se siente rápido o estorba? ¿El aviso "BARRIO −12" después de un contrato corporativo te hace pensar dos
  veces antes de aceptar otro? ¿El contacto del bar se siente como algo de noche y escondido?

## Pruebas automáticas
- PlayMode: OrigenMisionesPruebas (y las de HUD que cambien).

## Fuera de alcance
- Pestaña FAVORES y usar favores (T-187). El contacto y los vecinos con modelo, voz o animación (Alan o un pase de arte; aquí son el punto y el
  letrero). Llamadas entrantes que suenan solas.
