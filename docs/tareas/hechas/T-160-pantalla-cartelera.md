# T-160 Pantalla de la cartelera: elegir la pelea en el mostrador
Dueño: Tony | Rama: tony/T-160-pantalla-cartelera | Modelo: Sonnet 5.5, medium
Depende de: T-159

## Objetivo
En el mostrador de peleas, después de la demo, E abre la cartelera del clandestino: se ve en qué liga vas, cuántos campeones llevas y las
peleas que puedes tomar (el siguiente rival, el jefe marcado, revanchas a mitad de premio, la casera y la liga que sigue bloqueada). Con las
flechas eliges, Enter pasa a la pantalla de apuesta de siempre y de ahí a la pelea; Esc sale sin cobrar.

## Contexto
- Diseño: estetica-hud.md (a pie, HUD mínimo; neón azul, magenta, naranja y turquesa), personaje-progresion.md (ligas).
- Decisiones: D-tony-64 (pantallas con teclado; el control del jugador vuelve un cuadro después de cerrar, D-tony-67), D-tony-80 y T-145
  (pantalla de apuesta: ←/→ monto, Enter, Esc). Propuesta nueva: D-tony-99 (textos y orden de la cartelera).
- Código: `UI/Peleas/PantallaApuesta.cs` y `SeleccionApuesta.cs` (patrón de pantalla con selección pura + MonoBehaviour), `UI/Datos/ConfigPantallaApuesta.cs`,
  `UI/Editor/` (creador de `PantallaApuesta.prefab`), `Progresion/Prologo/MostradorPeleas.cs` (`Opciones`, `Retar`, `AliasDe`, `PremioDe`, T-159),
  `Progresion/Prologo/Editor/CreadorMostradorPeleas.cs` (anida `PantallaApuesta` en el mostrador).
- Pruebas de ejemplo: `Tests/PlayMode/Progresion/ApuestaMostradorPruebas.cs` (E, flechas, Enter y Esc simulados) y `Tests/PlayMode/UI/`.

## Qué hacer
1. `UI/Peleas/SeleccionCartelera.cs` (C# puro): filas con texto, premio y si se puede elegir; ↑/↓ mueven sin dar la vuelta y saltan las filas
   que no se pueden elegir; empieza en la primera elegible.
2. `UI/Peleas/PantallaCartelera.cs` + `UI/Datos/ConfigPantallaCartelera.cs` (asset `_Base`) + `UI/Prefabs/PantallaCartelera.prefab` (creador en
   UI/Editor/). `Mostrar(encabezado, filas, alElegir(int), alSalir)` y `Ocultar`. Textos en el config (D-tony-99):
   - encabezado: "PELEAS DEL CLANDESTINO · LIGA {liga} · CAMPEONES {n}/{total}";
   - siguiente: "LIGA {liga} {i}/{total} · {rival} · {premio}"; jefe: "JEFE DE LA LIGA {liga} · {rival} · {premio}" (en naranja);
   - revancha: "REVANCHA · {rival} · {premio}"; casera: "PELEA CASERA · {rival} · {premio}";
   - bloqueada: "LIGA {liga} · BLOQUEADA · VENCE A {rival}" (gris, no se elige);
   - ayuda: "↑ ↓ ELEGIR · ENTER PELEAR · ESC SALIR".
3. `MostradorPeleas`: campo opcional `PantallaCartelera pantallaCartelera` (hija). Si hay pantalla y `Opciones` trae algo más que la casera, E
   abre la cartelera (congela el control como la apuesta); Enter llama a `Retar` con la opción elegida (que abre la apuesta de siempre); Esc
   cierra y devuelve el control un cuadro después. Sin pantalla o con solo la casera, E hace lo de hoy. El texto del mostrador en ese caso es
   "Peleas del clandestino" (en el componente, editable); de día, en otra etapa o con el mecha no listo, los textos de hoy.
4. `MostradorPeleas.prefab`: el creador anida `PantallaCartelera.prefab` y la asigna (idempotente).
5. Ajusta las pruebas de Tony que en `Libre` pulsaban E y esperaban la apuesta directa (ApuestaMostradorPruebas, RecorridoEconomiaPruebas y las
   que salgan): ahora eligen "PELEA CASERA" en la cartelera con ↓ y Enter. Sin cambiar lo que comprueban.
6. EditMode `Tests/EditMode/UI/SeleccionCarteleraPruebas.cs` y PlayMode `Tests/PlayMode/Progresion/CarteleraMostradorPruebas.cs` (entrada simulada).

## Carpetas permitidas
Assets/_Proyecto/UI/ (Peleas/, Datos/, Editor/, Prefabs/), Assets/_Proyecto/Progresion/Prologo/ (y su Editor/ y Prefabs/ solo con el creador),
Assets/_Proyecto/Tests/EditMode/UI/, Assets/_Proyecto/Tests/PlayMode/UI/, Assets/_Proyecto/Tests/PlayMode/Progresion/, docs/capturas/
No tocar: Core/, Pelea/, Progresion/Ligas/ (si falta algo, se anota), escenas (el mostrador llega por su prefab), todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena) y el área Progresion no sale "LIMITE"
- [ ] SeleccionCarteleraPruebas: ↑ en la primera y ↓ en la última no se mueven; las filas bloqueadas se saltan; empieza en la primera elegible
- [ ] CarteleraMostradorPruebas (Pruebas_Economia, de noche, `Libre`): E abre la cartelera con el encabezado "LIGA SÓTANO · CAMPEONES 0/4" y las
      filas [TORNILLO, casera, CALLE bloqueada]; Enter abre la apuesta con TORNILLO y el premio de su cartel; Esc en la apuesta cierra todo sin
      cobrar; ↓ y Enter eligen la casera y la apuesta muestra EL CHATARRAS y $600
- [ ] CarteleraMostradorPruebas: con `Poner(liga 1, siguiente 5)` la primera fila es "JEFE DE LA LIGA SÓTANO · DON FIERRO · $2,000" en el color de
      jefe; con siguiente 3 aparecen dos revanchas con la mitad del premio
- [ ] Mientras la cartelera está abierta, WASD y el ratón no mueven al jugador; al cerrar con Esc, el control vuelve y la misma E no la reabre
- [ ] En la etapa `PeleaCasera`, E sigue abriendo la apuesta de la casera sin cartelera; el prólogo (RecorridoPrologoPruebas) sigue en verde
- [ ] Capturas docs/capturas/T-160-1.png (cartelera al empezar) y T-160-2.png (cartelera con el jefe y dos revanchas)

## Criterios de sensación (para un humano)
- Pruebas_Economia, de noche: E en el mostrador. ¿Se entiende de un vistazo en qué liga vas, a quién te toca y qué pasa si ganas? ¿El jefe se
  distingue? ¿La fila bloqueada motiva ("vence a DON FIERRO") o estorba?

## Pruebas automáticas
- EditMode: SeleccionCarteleraPruebas. PlayMode: CarteleraMostradorPruebas (y las ajustadas).

## Fuera de alcance
- Textos de jefe en la arena y avisos de subir de liga (T-161). Ratón en la cartelera. Retratos de los rivales.
