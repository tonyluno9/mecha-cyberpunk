# T-081 Fachadas con detalle
Dueño: Alan | Rama: alan/T-081-fachadas-detalle | Modelo: Sonnet 5.5, medium
Depende de: T-076 (Fachada_Modular), T-079 (ambiente). Mejor después de T-080 (mismo creador, menos conflictos).

## Objetivo
Los edificios dejan de ser cajas lisas: en planta baja tienen locales con cortina de metal (la fachada modular del kit) y arriba
ventanas, algunas prendidas con luz cálida o de neón, con cornisa y detalles. Desde la calle el barrio se ve denso y habitado, de
noche y de día, sin que cueste demasiado dibujarlo.

## Contexto
- Lo que hay (Mundo/Editor/CreadorBarrio.cs, `EdificioPlano`): cada edificio es un "Cuerpo" (cubo de w × altura × d, 8 a 30 m de
  alto) más un piso extra, una lámina (pobres) o un tanque, y a veces un letrero greybox. Los edificios se tocan entre sí: solo
  los lados que dan a una calle, al callejón o a la banqueta se ven.
- Fachada_Modular (T-076): pieza de 4 m de ancho, 6 m de alto y 0.4 m de fondo, con local en planta baja (cortina), pilastras y
  cornisa; el frente mira a +Z.
- Truco barato para las ventanas: una textura generada por código (no pintada) con una cuadrícula de 4 × 4 ventanas en 12 × 12 m
  y su mapa de emisión con algunas ventanas prendidas; se pone en quads pegados a los lados visibles (de 6 m hasta la azotea) con
  la repetición ajustada al tamaño de cada quad (`MaterialPropertyBlock` con `_BaseMap_ST`), así cada ventana mide lo mismo
  en todos los edificios.
- Diseño: estetica-hud.md (low-poly, neón azul, magenta, naranja y turquesa). Megaciudad inspirada en México: zona pobre (casa,
  Manzanas 1 y 4) con block, lámina y varilla; centro (bar, clandestino, casino) más alto y con más luz.

## Qué hacer
1. Texturas generadas (`Mundo/Texturas/Fachada_<variante>.png` y `_Emision.png`, creadas con `Texture2D` y `EncodeToPNG` por el
   creador, importadas sin compresión que borre las ventanas): 3 variantes (pobre, media, alta) con distinto color de muro,
   marco y patrón de ventanas prendidas (cálidas la mayoría, algunas cian o magenta). Materiales `Fachada_*` URP/Lit con emisión.
2. Archivo nuevo `Mundo/Editor/VestirFachadas.cs`, llamado por CreadorBarrio antes de guardar el prefab: para cada edificio,
   detecta qué lados se ven (los que no tocan a otro edificio) y en esos lados:
   - planta baja: una fila de Fachada_Modular a lo ancho (lo que sobre de menos de 4 m, con un cubo del mismo material);
   - de 6 m a la azotea: un quad de ventanas con la variante de la manzana;
   - una cornisa delgada en la orilla de la azotea.
   Los lugares (`Lugar_*`) no se tocan: su puerta, letrero y entrada se quedan como están.
3. La Fachada_Modular no sobresale más de 0.45 m del muro (no estorba la franja de caminar de T-080) y sin collider propio (el
   "Cuerpo" ya choca).
4. Regenera con `CreadorTodo.Crear` y al final rehornea los reflejos (`CreadorAmbiente.HornearReflejos()`, T-079).

## Carpetas permitidas
Assets/_Proyecto/Mundo/, Assets/_Proyecto/Escenas/Ciudad.unity, Assets/_Proyecto/Escenas/Pruebas_Alan.unity,
Assets/_Proyecto/Tests/EditMode/Mundo/ y Assets/_Proyecto/Tests/PlayMode/Mundo/
No tocar: Core/, Assets/Arte/, Vehiculos/, Clandestino.unity, las carpetas de Tony.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (incluidas KitCallePruebas, BarrioPruebas y RecorridoCarroPruebas)
- [ ] EditMode: las 3 texturas y sus mapas de emisión existen, son de al menos 256 × 256, y el mapa de emisión tiene entre 15 % y
      45 % de ventanas prendidas
- [ ] PlayMode: todo lado de edificio que da a una calle tiene al menos una Fachada_Modular y un quad de ventanas (rayo desde la
      calle hacia el muro, a 2 m y a 7 m de alto, en el centro de cada lado visible, choca con fachada o ventana); ningún `Lugar_*`
      tiene fachada encima de su puerta
- [ ] PlayMode: en cada quad de ventanas, el `_BaseMap_ST` hace que una ventana mida 3 m (±0.1 m) en ancho y alto
- [ ] PlayMode: la franja de caminar de T-080 sigue libre (misma prueba)
- [ ] Medido en el editor (eval), anotado en la bitácora: triángulos de Barrio.prefab ≤ 2 000 000 y promedio de
      `Time.unscaledDeltaTime` en 300 frames en V1 a las 22:00 en el editor vivo (comparado con la medición antes de la tarea)
- [ ] Capturas de las vistas V1, V2, V3 y V4 a las 22:00 (docs/capturas/T-081-1.png a -4.png) y V1 a las 13:00 (T-081-5.png)
- [ ] Consola sin errores

## Criterios de sensación (para un humano)
- El barrio se ve denso y habitado, no de cajas: de noche hay ventanas prendidas aquí y allá, de día se lee el block y los locales.
- La zona pobre y el centro se distinguen a simple vista.

## Pruebas automáticas
- EditMode (Tests/EditMode/Mundo/): FachadasTexturasPruebas.
- PlayMode (Tests/PlayMode/Mundo/): FachadasPruebas (lados vestidos, lugares intactos, tamaño de ventana).

## Fuera de alcance
- Letreros de neón (T-082); interiores visibles por las ventanas; edificios con forma distinta a una caja
- Grafitis, anuncios corporativos con tu alias (A-F4 y después)
