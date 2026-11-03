# Interiores: lo que falta (T-546, paso 4 del plan de la noche 3-nov)

Los cinco interiores se armaron solo con assets que ya había (`Assets/Arte/Editor/ColocarT546.cs`, raíz `T546_Interiores`
en Ciudad_Prologo). Lo que no había se dejó como hueco limpio, sin cubos. Comparaciones con cada referencia en
`docs/capturas/interior-<nombre>-comparacion.png` (izquierda la referencia, derecha el juego).

## Habitación (referencia: docs/referencias-interiores/habitacion.png)

Hay: cama H02, buró H11 con su lámpara, foco colgante cálido (la A05 del clandestino a 0.4), luz de neón magenta y turquesa por la
ventana, tres pósters planos con textura generada y la tele H06 sobre el banco de trabajo, que hace de escritorio. Se quitaron cocina,
sofá, mesa y tubos de taller; el techo bajó a 3 m.

Falta:
- **El cuarto es mucho más grande que el de la referencia** (es la caja del greybox): la referencia es un cuarto chico de 3 × 4 m.
  Achicarlo pide mover las paredes de la casa; con eso se vería como la referencia.
- Ropero de madera (alto, dos puertas).
- Escritorio de verdad con computadora vieja (monitor CRT y teclado). El banco de trabajo con tablero de herramientas que hace de
  escritorio todavía se ve de taller.
- Silla de escritorio.
- Ventana con barrotes (la luz de neón entra, pero no hay marco ni barrotes).
- Mochila, ropa tirada.

## Consultorio del Doc (I+D+I) (referencia: consultorio-doc.png)

Hay: luz turquesa, camilla (la cama H02 rayada hace de camilla), monitores, estantes, El Doc junto a la camilla, servidor.

Falta:
- **Brazo robótico de cirugía** colgando del techo (lo que más se nota en la referencia).
- Camilla de verdad (sillón reclinable de cirugía).
- Vitrinas con implantes (manos, ojos) en frascos.
- Lavabos.
- Cables por el piso y papel picado turquesa.

## Taller de Tino (CIC) (referencia: taller-tino.png)

Hay: bancos T03, tablero T01, repisa T08, drones CY07b colgando, series de focos, pizarrón con textura generada, Tino junto al banco.

Falta:
- Estantes con drones a medio armar (solo hay repisa genérica).
- Lámpara de brazo sobre el banco.

## Casino (referencia: casino-interior.png)

Hay: alfombra estampada generada (roja con grecas doradas), 18 tragamonedas en hileras (copias de adorno; la que cobra es la de
siempre), barra, mesa del Hacker al fondo, papel picado, luces doradas y magenta.

Falta:
- Mesas de póker / blackjack con paño verde y sus sillas.
- Candil (chandelier) de neón al centro.
- Arcos y columnas doradas, botellas iluminadas detrás de la barra, bancos de barra.
- Animación de sentado para El Hacker (está de pie junto a la mesa).
- El techo del casino es la caja del greybox: la referencia tiene tuberías y focos en hilera.

## Entrada de armas de El Mercado (referencia: entrada-armas-mercado.png)

Hay: mostrador, cajas, tambos, reja, papel picado, series de focos.

Falta:
- Vitrina de armas (mostrador con cristal y pistolas dentro).
- Tablero de pared con rifles colgados.
- Reja corrediza de tijera (la que hay es cortina).
- Letrero de neón de arma (el de la referencia es un rifle dibujado en neón).
- Toldo de lona sobre la entrada.

## Nota técnica

Al armar el casino salió que los materiales URP/Lit creados por código con `globalIlluminationFlags = None` pierden `_EMISSION` en cuanto
Unity los reimporta (el postprocesador de URP solo deja la palabra si el GI es emisivo). Les pasaba a los letreros L10-L14 del paso 2:
ya están arreglados (`RealtimeEmissive`). Otros creadores viejos (Mundo, Arena, Casino) también usan `None`; si algún neón suyo deja de
brillar tras reimportar, es esto.
