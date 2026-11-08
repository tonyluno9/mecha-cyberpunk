# T-891: lo hecho en código por los modelos de imageassets/modelos3d/calle/ (B5)

Rama `tony/T-891-calle-modelos`. Importador: `ImportarS2p.ImportarCalle`. Colocación: `ColocarS2pPuertas.TodoCalle`.

## Qué quedó
- **TK01:** base de ladrillo del tinaco, a 1.5 x 0.7 m (el modelo medía 1 m de alto).
- **NE01 / NE01B / NE02:** neones magenta, turquesa y de pared. Los NE01 traen la placa contra el muro y la cara al frente: no son de bandera como pedía
  el pedido, así que van girados 32° para leerse desde la calle.
- **Emisión:** los tubos vienen pintados en la textura. Se genera `<código>_emision.png` con lo que pasa de un umbral de brillo: brilla el tubo, no la caja.
- **AL01:** rejilla en el arroyo, en lugar de la copia de la tapa redonda.
- **MT01:** 7 módulos en la cara norte del callejón, en lugar del muro liso.
- **TL01:** toldo en la mitad de las puertas de vivienda.
- **FA01:** FARMACIA a lo largo del muro y cruz verde.
- **Nova:** edificios de módulos, NV02 al centro de la planta baja y NV01 en lo demás, hasta 5 pisos; arriba sigue un cuerpo oscuro, que es el que choca.
  Sin rendijas (T-888). BL01 en lugar de las barreras y BT01 en lugar de los basureros.
- Mesh LOD activado en todas las piezas nuevas de T-890 y T-891; instancing en NV01 y NV02.
- El fondo de la calle de K01 se recorrió 1 m atrás para que los toldos no pasen de la línea de la calle.

## Evidencia
- EditMode: 2117 de 2117.
- PlayMode por clases (puertas, calle, callejón, casino, Neón Alto, Suburbios, Mercado, tianguis): 21 de 21. Se corrió antes de los últimos dos ajustes
  (material del cuerpo de Nova y fondo de la calle); después solo EditMode.
- Capturas revisadas: docs/capturas/s2p-b5-k01, k05, k06, callejon.
