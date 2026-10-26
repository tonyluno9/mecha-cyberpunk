# T-187 Usar los favores: descuento en el mercado negro, escondite contra el calor e información para el soplo
Dueño: Tony | Rama: tony/T-187-usar-favores | Modelo: Sonnet 5.5, medium
Depende de: T-182, T-184, T-185, T-186

## Objetivo
Los favores que da el barrio se usan, cada uno en su sitio: el DESCUENTO rebaja 30 % la siguiente compra del mercado negro, el ESCONDITE borra
el calor en un escondite del barrio (y cumple el paso de escapar) y la INFORMACIÓN abre EL SOPLO en el bar. El teléfono tiene la pestaña
FAVORES. Con esto se cumple "Una misión de barrio sube la reputación del barrio y da un favor usable".

## Contexto
- Diseño: economia-misiones.md ("Las misiones del barrio pagan poco o nada en efectivo, pero dan reputación y favores: descuentos, información y
  escondites"), ciudad.md ("El calor ... baja si te escondes en el barrio o llegas a casa").
- Decisiones: D-tony-81 (mostradores), D-tony-146 (favores: máximo 3 por tipo, un uso cada uno).
- Código: `Economia/Tiendas/TiendaBasica.cs`, `UI/Tiendas/MostradorTienda.cs` y `PantallaTienda.cs`, `UI/Prefabs/MostradorMercadoNegro.prefab`
  (y su creador `UI/Editor/CreadorMostradorTienda.cs`); `ServicioFavores` (T-182); `ServicioMisiones` y el paso `Escapar` (T-184, T-185);
  `PantallaTelefono` y `PantallaMisiones` (T-186); `ICalor.Borrar`; `IPuntoMision` de tipo `Escondite` (Mision_Escondite_Azotea y
  Mision_Escondite_Lavanderia, ya en Pruebas_Misiones).

## Qué hacer
1. Economia: `TiendaBasica.PrecioCon(oferta, rebaja)` (rebaja de 0 a 1; redondea al peso más cercano; un precio mayor que 0 nunca baja de $1) y
   `Comprar(oferta, cartera, rebaja)` (cobra el precio rebajado; `Comprar(oferta, cartera)` sigue igual, así que `ITienda` no cambia).
2. UI de la tienda: `MostradorTienda` gana `aceptaFavorDescuento` (true solo en `MostradorMercadoNegro.prefab`, puesto por su creador). Con un
   DESCUENTO en `ServicioFavores`, la pestaña COMPRAR muestra "$80 → $56" y una línea "FAVOR · −30 % EN ESTA COMPRA · TE QUEDAN 1"; la compra
   cobra el rebajado y gasta el favor solo si se cobró (sin dinero no se gasta); la siguiente ya cuesta completo. Vender no usa favores.
3. `Progresion/Favores/Escondites.cs` (en `ServiciosProgresion.prefab`, con el creador): un `IInteractuable` por cada `IPuntoMision` de tipo
   `Escondite`. Con calor mayor que 0 y un ESCONDITE: "E · ESCONDERTE (FAVOR)" → `ICalor.Borrar()`, gasta el favor y, si la misión está en
   `Escapar`, lo cumple en ese momento; con calor y sin favor: "ESCONDITE · NECESITAS UN FAVOR" (sin E); sin calor no se ofrece. Llama
   `Activar(true)` a los escondites mientras hay calor. Solo a pie.
4. INFORMACIÓN: EL SOPLO (que ya pide y gasta el favor al aceptar, T-183/T-184) muestra en la `PantallaMisiones` "USA 1 INFORMACIÓN"; si el
   favor no alcanza sigue en gris. Comprueba que abandonar EL SOPLO no devuelve el favor (D-tony-146).
5. Pestaña FAVORES del teléfono: los tres con "N/3", su descripción y dónde se usan ("MERCADO NEGRO", "ESCONDITES DEL BARRIO", "CONTACTO DEL
   BAR"). Si una misión da un favor que ya está en 3, el aviso de fin dice "FAVOR PERDIDO: YA TIENES 3".
6. Pruebas:
   - EditMode `Tests/EditMode/Economia/` (casos nuevos en la prueba de tienda): `PrecioCon($80, 0.3)` = $56; `PrecioCon($1, 0.9)` = $1; precio 0
     sigue gratis; `Comprar` sin dinero para el rebajado devuelve `SinDinero`.
   - PlayMode `Tests/PlayMode/Progresion/Misiones/FavoresPruebas.cs` en Pruebas_Misiones con entrada simulada
     (atajos anotados: favores dados por código salvo en a); `AvanzarHasta` por código para cambiar de día o de franja):
     a) con 1 DESCUENTO (dado por `barrio_tiendita` cumplida, no por código): mercado negro, comprar 2 MISILES: el primero cobra $56 y el segundo
        $80; FAVORES dice 0/3;
     b) `robo_bodega`: recoger (calor 50) y, con 1 ESCONDITE, ir al escondite de la azotea y E: calor 0, la misión se cumple ahí, ESCONDITE 0/3;
        repetir sin favor: el escondite dice "NECESITAS UN FAVOR" y E no hace nada;
     c) con 1 INFORMACIÓN, en el bar de noche aceptar EL SOPLO: INFORMACIÓN 0/3; cumplirla da 12 chatarra;
     d) con 3 DESCUENTOS, cumplir `barrio_tiendita`: sigue en 3 y el aviso dice "FAVOR PERDIDO";
     e) F5, cambiar los favores por código, F9: vuelven los conteos guardados.
     Consola sin errores; `MedidorFrames` sin tirones.

## Carpetas permitidas
Assets/_Proyecto/Economia/Tiendas/TiendaBasica.cs, Assets/_Proyecto/UI/Tiendas/, Assets/_Proyecto/UI/Misiones/, Assets/_Proyecto/UI/Editor/,
Assets/_Proyecto/UI/Prefabs/ (solo con creadores), Assets/_Proyecto/Progresion/Favores/, Assets/_Proyecto/Progresion/Misiones/ (solo lo de
`Escapar` con escondite), Assets/_Proyecto/Progresion/Editor/CreadorDatosProgresion.cs, Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab
(solo con el creador), Assets/_Proyecto/Progresion/Prologo/Editor/CreadorEscenaMisiones.cs, Assets/_Proyecto/Escenas/Tony/Pruebas_Misiones.unity
(solo con el creador), Assets/_Proyecto/Tests/EditMode/Economia/, Assets/_Proyecto/Tests/PlayMode/Progresion/Misiones/, docs/capturas/
No tocar: Core/ (`ITienda` no cambia), Mecha/ (el taller no tiene descuento en este bloque), Pelea/, las otras escenas, todo lo de Alan.

## Criterios de aceptación (los comprueba el verificador)
- [ ] `bash herramientas/verificar.sh todo` pasa (salvo los fallos conocidos de Alan en Arena)
- [ ] FavoresPruebas pasa 3 corridas seguidas con los valores del paso 6; las pruebas de la tienda, la venta y el recorrido de economía y de
      habilidades que ya existían siguen en verde (sin favores, todo cuesta lo mismo que antes)
- [ ] Correr los creadores dos veces no cambia prefabs ni escena (md5 igual)
- [ ] Capturas docs/capturas/T-187-1.png (mercado negro con "$80 → $56" y la línea del favor) y T-187-2.png (escondite con "E · ESCONDERTE
      (FAVOR)" y el calor en nivel 2), revisadas a ojo

## Criterios de sensación (para un humano)
- ¿Los favores se sienten como algo que el barrio te debe y que vale la pena guardar, o se olvidan en el bolsillo? ¿El escondite te salvó de
  verdad en un robo? ¿30 % de descuento en una sola compra se siente generoso o tacaño?

## Pruebas automáticas
- EditMode: casos de `TiendaBasica`. PlayMode: FavoresPruebas.

## Fuera de alcance
- Descuentos en el taller o la clínica (pregunta abierta, D-tony-152). Escondites con modelo y animación (Alan, con `IPuntoMision`).
