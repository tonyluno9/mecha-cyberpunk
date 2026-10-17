# T-143 Escena Pruebas_Economia
Dueño: Tony | Rama: tony/T-143-escena-pruebas-economia | Modelo: Sonnet 5.5, medium
Depende de: T-140, T-142

## Objetivo
Existe `Escenas/Tony/Pruebas_Economia.unity`, una escena greybox chica y rápida de recorrer donde se prueba toda la economía después de la
demo sin pasar por el prólogo: taller con el mecha de la historia y su banco, mostrador de peleas del clandestino (de noche, historia en
`Libre`), un sitio para el mostrador del mercado negro y las teclas de prueba de dinero, inventario y reloj. Las tareas T-145 a T-151 la usan
para sus pruebas con entrada simulada y para las pruebas de sensación.

## Contexto
- Decisiones: D-029 (escenas de Tony en Escenas/Tony/), D-068 y D-tony-51 (dobles de prueba: TallerDePrueba, RelojDePrueba), D-tony-66
  (`MostradorPeleas` solo de noche y en `PeleaCasera` o `Libre`), D-tony-65 (`EntradaArena`). Propuesta nueva: D-tony-87.
- Ejemplo a seguir (y reutilizar en lo posible): `Progresion/Prologo/Editor/CreadorEscenaPrologo.cs` (`ArmarTaller`, `ArmarJugadorYServicios`,
  `ArmarEntradaArena`, `ArmarMostradorPeleas`, `ArmarLugar` con `LugarDePrueba`). Deuda conocida: un creador que reordena objetos rompe pruebas
  que dependen del orden (T-111); usa métodos idempotentes y no dependas del orden de la jerarquía.
- La historia del jugador vive en `ServicioHistoria` (dentro de `ServiciosProgresion.prefab`); `Avanzar(EtapaPrologo)` la mueve.

## Qué hacer
1. `Progresion/Pruebas/HistoriaLibreDePrueba.cs`: al arrancar, si la `IHistoria` registrada está en `Ninguna`, la deja en `Libre` (con
   `ServicioHistoria.Avanzar`); con otra etapa no hace nada. Solo va en escenas de pruebas.
2. `Progresion/Prologo/Editor/CreadorEscenaEconomia.cs` (menú "Mecha Cyberpunk/Crear escena Pruebas_Economia") que arma, en unos 40 × 30 m:
   piso y muros greybox; `JugadorAPie.prefab` en una aparición "Economia_Inicio"; `MechaEstacionado.prefab` con `TallerDePrueba` (el banco
   del taller queda a 3 m del mecha, como en Pruebas_Calle); el mostrador de peleas (`MostradorPeleas.prefab` con su `EntradaArena` y una
   `ArenaDePrueba` lejos del resto, y la aparición "Clandestino_Salida" junto al mostrador, como en Pruebas_Prologo) a unos 15 m del inicio; un vacío "Sitio_Mercado" a unos 10 m del inicio para el mostrador de T-146, con un
   `LugarDePrueba` `MercadoNegro` siempre abierto alrededor; `RelojDePrueba` a las 21:00 (de noche); `DineroDePrueba`, `InventarioDePrueba`
   (T-142) y `HistoriaLibreDePrueba`; luz y cámara como Pruebas_Prologo. Letreros de texto 3D o cubos de color para que una persona sepa
   qué es cada cosa (TALLER, PELEAS, MERCADO).
3. Crear la escena con el creador (nunca a mano) y anotar en la bitácora las posiciones de cada sitio y las teclas de prueba
   (7/8 dinero, 9 brazo de chatarra, 0 kit, J reloj, F5/F9 guardado si están en la escena).
4. PlayMode `Tests/PlayMode/Progresion/EscenaEconomiaPruebas.cs` (carga la escena, entrada simulada).

## Carpetas permitidas
Assets/_Proyecto/Progresion/Prologo/Editor/, Assets/_Proyecto/Progresion/Pruebas/, Assets/_Proyecto/Escenas/Tony/Pruebas_Economia.unity,
Assets/_Proyecto/Tests/PlayMode/Progresion/, docs/capturas/
No tocar: Core/, otras escenas, los prefabs existentes (solo se colocan), todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los 9 fallos conocidos de Alan en Arena)
- [ ] EscenaEconomiaPruebas: al cargar hay exactamente 1 `ICartera`, 1 `ServicioInventario`, 1 `IRenta`, 1 `IRelojMundo` (de noche), 1 mecha
      montable estacionado en su taller y la `IHistoria` en `Libre`; consola sin errores en 5 s
- [ ] EscenaEconomiaPruebas: con W y ratón simulados el jugador llega del inicio al mostrador de peleas en menos de 15 s de juego y el HUD dice
      "E · Pelea casera · premio $600"; las teclas 7, 9 y 0 cambian dinero e inventario como dice T-142
- [ ] Correr el creador dos veces deja la misma lista de objetos en la escena (prueba de EditMode o comparación en la bitácora)
- [ ] Captura docs/capturas/T-143-1.png: vista aérea con taller, mostrador de peleas y sitio del mercado legibles

## Criterios de sensación (para un humano)
- Pruebas_Economia, Play: ¿se entiende sin explicación dónde está el taller, el mostrador de peleas y el mercado? ¿Se recorre en segundos?

## Pruebas automáticas
- PlayMode: EscenaEconomiaPruebas.

## Fuera de alcance
- El mostrador del mercado negro (T-146) y la pantalla de apuesta (T-145). Ciudad.unity (Alan).
