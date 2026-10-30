# T-417 Personajes de apoyo (los profes) como NPC de prueba, el CIC, y peatones al mínimo
Dueño: Tony | Rama: tony/T-417-profes-peatones | Modelo: Sonnet 5.5, medium (mecánico) | MODO RÁPIDO (docs/plan-noche.md, paso 2)
Depende de: T-414 (I+D+I, EL FOX, VIVACOIN, Biits) y T-416 (las mejoras de las 5 armas, que vende el CIC).

## Objetivo
En la ciudad hay cinco personajes de apoyo con su nombre y su función, como muñecos de prueba hasta que llegue su modelo: EL INVENTOR (TINO) en el CIC
mejora armas; EL DOC (JOEL) en I+D+I te cura; EL HACKER (SERGIO) te da información; EL MECÁNICO (CANEPA) arregla tu carro; EL DIRECTOR (ESTRADA) es
neutral y solo platica. Y hay muy pocos peatones, sin que la calle se vea vacía.

## Contexto
- Interacción: `Core/Jugador/IInteractuable.cs` (`Texto`, `PuedeUsar`, `Usar()`) que lee `Personaje/Interaccion/InteraccionAPie.cs` y muestra el HUD
  ("E · …"). Ejemplos: `IA/Aliados/Reclutable.cs` y `Personaje/Combate/ArmaEnPiso.cs`.
- Curar: `Personaje/Vida/VidaAPie` (lo usa ya `Progresion/Prologo/Clinica/DoctorClinica.cs`, que te levanta en `Clinica_Camilla` cuando caes; no se
  cambia). Dinero: `ICartera` (`IntentarPagar`, motivo `MotivoDinero.Clinica` y `Otro`). Calor: `ICalor.Borrar()`. Tienda: `UI/Tiendas/MostradorTienda` +
  `PantallaTienda` con un `DatosTienda` (como `MostradorMercadoNegro.prefab`).
- Subtítulos: `UI/Historia/PantallaHistoriaPandillas.MostrarSubtitulo(texto, segundos)` (la usa DirectorPandillas).
- Lugares: la clínica está dentro de El Mercado (`EntradaClinica`, de Alan en Barrio.prefab); El Fox es `Lugar_Bar` (Tianguis); VivaCoin es `Lugar_Casino`
  (Neón Alto); tu casa es `Casa_Puerta` (Suburbios, `Manzana_1` y la de junto). El CIC no existe: se arma aquí con primitivas.
- Peatones (Alan, autorizado): `Trafico/Datos/ConfigPeatones_Trafico.asset` (`peatonesDeDia` 12, `peatonesDeNoche` 24) dentro de Barrio.prefab
  (`GrupoPeatones` crea `config.Maximo` al arrancar). Cambia el asset por código; **no regeneres Barrio.prefab**.
- Decisiones: D-tony-255 (peatones) y D-tony-256 (profes y CIC). 60 FPS de noche en la calle principal es obligatorio (docs/plan-noche.md).

## Carpetas permitidas
Progresion/Apoyo/ (nuevo: código, Datos/ y Editor/ si hace falta), Progresion/Prologo/Editor/CreadorCiudadPandillas.cs, Economia/Datos/ (DatosTienda_CIC, por
código), Trafico/Datos/ConfigPeatones_Trafico.asset (por código) y Trafico/Editor/CreadorPeatones.cs (solo los valores por defecto), Escenas/Ciudad_Prologo.unity
(solo con código), Tests/EditMode/Progresion/, Tests/PlayMode/Progresion/ (y ajustar las pruebas de Trafico que esperen 12/24, si hay), docs de Tony.
No tocar: Radio/, Barrio.prefab, el arte de T-250, DoctorClinica (su caída y la camilla siguen igual).

## Qué hacer
1. **`Progresion/Apoyo/`**: `ServicioApoyo` (enum: Mejoras, Curar, Informacion, Taller, Platica), `DatosPersonajeApoyo` (ScriptableObject: id, título
   "EL INVENTOR", nombre "TINO", función "MEJORA TUS ARMAS", color, servicio, costo, 3 frases) y `ReglaPersonajeApoyo` (pura: qué dice el aviso y si se
   puede usar según el servicio, la vida, el calor y el dinero). `PersonajeApoyo` (MonoBehaviour, `IInteractuable`) con `event Action<string> Usado`
   (el id; lo escuchará la historia en T-418) y `Regalo` (una acción de una vez que reemplaza al servicio la siguiente vez; lo usa T-418).
2. **Servicios**:
   - EL INVENTOR · TINO (CIC): "E · MEJORAS DE ARMAS" abre la tienda `DatosTienda_CIC` (solo las 15 mejoras de T-416, mismos precios que El Mercado).
   - EL DOC · JOEL (I+D+I, junto a `EntradaClinica`): "E · CURARTE · Ƀ50": vida al máximo y cobra Ƀ50; con la vida llena dice "ESTÁS BIEN, MIJO";
     sin dinero, "NO TE ALCANZA". Los implantes quedan para el punto 10 de docs/mejoras-pendientes.md: su segunda línea dice "IMPLANTES: PRONTO".
   - EL HACKER · SERGIO (entrada de VivaCoin): "E · INFORMACIÓN": un subtítulo con la pista que toque (cuánto respeto te falta en la zona donde estás,
     dónde está el jefe si ya apareció, o una frase suya si no hay nada).
   - EL MECÁNICO · CANEPA (Suburbios, junto al carro estacionado más cercano a tu casa): "E · PINTAR TU CARRO · Ƀ100": borra el calor (`ICalor.Borrar`);
     sin calor dice "TU CARRO ESTÁ LIMPIO". Arreglar daño llega con el punto 14 (carros que se dañan).
   - EL DIRECTOR · ESTRADA (puerta de El Fox): "E · PLATICAR": una de sus 3 frases, sin repetir la última. Nunca pelea.
3. **Muñeco de prueba**: cápsula de 1.8 m con el color del personaje (Inventor naranja, Doc blanco, Hacker turquesa, Mecánico amarillo, Director gris),
   o el `ModeloJugador.prefab` teñido si es fácil; mira hacia la calle; sin collider que estorbe al pasar (radio 0.4). Arriba, a 2.2 m, un letrero
   que siempre mira a la cámara: el título en blanco y el nombre en su color ("EL INVENTOR" / "TINO"), visible a menos de 15 m.
4. **El CIC**: bajo `GTA_T417/Lugar_CIC`, en la banqueta de Suburbios a 10–25 m de `Casa_Puerta` sin tapar ninguna puerta: mesa de trabajo, repisa con
   cajas, una luz fría y un letrero de neón "CIC" turquesa. El Inventor está detrás de la mesa. `T417()` (sumado a `Todo()`) pone los cinco personajes,
   el CIC y la tienda.
5. **Peatones**: `peatonesDeDia` 4 y `peatonesDeNoche` 6 (D-tony-255). Si la calle principal se ve vacía en la captura, sube a 6 y 8 y anótalo.

## Criterios de aceptación
- [ ] Hay exactamente 5 `PersonajeApoyo` en Ciudad_Prologo, cada uno con su título, su nombre y su servicio; los 5 están sobre el piso (raycast) y a
      menos de 3 m de su lugar (CIC, `EntradaClinica`, `Lugar_Casino`, el carro de Suburbios, `Lugar_Bar`).
- [ ] Con vida 40 y Ƀ200, E con EL DOC deja la vida al máximo y Ƀ150; con calor nivel 2 y Ƀ200, E con EL MECÁNICO deja el calor en 0 y Ƀ100;
      E con EL INVENTOR abre una tienda con 15 mejoras y ninguna arma.
- [ ] De noche hay 6 peatones prendidos como máximo y de día 4.
- [ ] 60 FPS o más de noche en la calle principal (valor medido en el build o en el editor, anotado en la bitácora con el lugar y la cámara).
- [ ] Capturas revisadas a ojo y corregidas si se ven mal: docs/capturas/T-417-1.png (el CIC con EL INVENTOR y su letrero) y T-417-2.png (la calle
      principal de noche con los peatones que quedan), en 1920x1080.

## Pruebas (solo estas 2 nuevas)
- EditMode `ReglaPersonajeApoyoPruebas.AvisoYServicio`: el aviso de cada servicio con y sin dinero, con vida llena, sin calor, y que la Platica no
  repite la última frase.
- PlayMode `PersonajesApoyoPruebas.RecorridoProfes` (InputTestFixture): Ciudad_Prologo en Libre con Ƀ500, vida 40 y calor nivel 2; el jugador aparece
  frente a cada personaje y pulsa E: el Doc cura y cobra, el Mecánico borra el calor y cobra, el Inventor abre la tienda (Esc la cierra), el Hacker y el
  Director sacan un subtítulo; cuenta los peatones prendidos de noche (≤ 6). Toma las dos capturas.

## Al terminar
- Bitácora de una línea con los FPS medidos, fila de docs/estado-tony.md en "hecha" y merge --no-ff a tony/fase-g (el cierre del paso 2 lo hace T-418).
  Nunca force push; `git restore` a lo que Unity cambia solo.

## Sensación (Tony)
- ¿Encuentras a cada profe? ¿Se entiende qué hace cada uno con solo leer su letrero? ¿La calle se siente viva con 6 peatones o vacía?

## Fuera de alcance
- Los modelos de los profes (llegan después), los implantes a pie (punto 10), el daño de carros (punto 14), asaltar peatones (punto 13, fuera) y
  diálogos con opciones.
