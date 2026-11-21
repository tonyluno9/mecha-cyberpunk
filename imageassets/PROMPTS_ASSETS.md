# Prompts de assets — Mecha Cyberpunk
Generado el 29-oct-2026 por el chat de arte 3D. Todo lo pendiente **menos coches y exos**, en orden de prioridad.
El mismo contenido está en `lote_prompts.json` para generarlo en automático con WaveSpeed.

## Cómo usar este archivo
- **Personajes fuera del lote:** el público (P04) y la Parte 6 (personajes por piezas) están aquí solo como referencia; **no** están en `lote_prompts.json`, así que el script no los genera.
- **Imágenes de referencia:** las de `edit` se adjuntan desde esta carpeta (ya están, con su nombre).
- **Nano Banana 2.1:** `text-to-image` cuando no hay referencia; `edit` cuando hay que adjuntar. Resolución 1K, una sola generación por asset (si falla, se regenera solo ese).
- **Tripo (por defecto):** Image to 3D · Delight apagado · PBR apagado · triángulos · sin rig · exportar FBX (preset Unity) con el nombre `<ID>_<Nombre>.fbx`. Las piezas de personajes van sin rig: se riggean al armar.
- **Revisión general:** nada flotando, base plana, atrás cerrado, sin letras ni logos, bordes rectos en módulos de pared.

## Paso 0 — Una sola vez en Claude Code (antes de meter los CY)
> Crea en el área de arte: (1) un script de editor `MascaraEmision` que, para un material URP Lit, genere un mapa de emisión a partir de los píxeles brillantes turquesa, magenta y ámbar de la textura base (umbral configurable) y lo asigne con intensidad HDR ajustable; (2) un shader graph `PantallaAnuncio` (emisivo, rejilla de píxeles sutil, líneas de escaneo, desplazamiento de anuncios, parpadeo ocasional) y un componente `AnuncioPantalla` con una lista de texturas de anuncios ficticios en español sin marcas reales; (3) el shader `HoloAnuncio` (aditivo, ambos lados, scanlines, glitch, fresnel) y el componente `AnuncioHolo`; (4) un ajuste global: bajar ~15% la saturación de los materiales ocre/crema del kit viejo para que domine el neón. Captura de prueba de noche.

---

# Parte 1 — Capa cyberpunk de la calle

## CY01 — Proyector de anuncio holográfico
- **Medida:** 2.5 × 1.6 m, fondo 0.4 m · **Aspecto:** 16:9 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a wall-mounted holographic advertisement projector, as a separate object with no wall behind it: a wide rectangular open frame made of thick dark gunmetal metal beams with beveled edges, the inside of the frame completely empty and open (the hologram will be added later). Along the bottom beam, a row of four chunky projector emitters with glowing teal lenses pointing up; at the four corners, chunky bolted brackets with small glowing magenta status lights; a boxy black power unit with cooling vents and an amber indicator attached to one side, connected by a thick ribbed conduit running along the frame. A flat thick mounting plate at the back of the bottom beam to attach it to a wall. Everything thick and solid. View: front view rotated about 25 degrees to the left, slightly from below, as seen from the street, the whole piece visible.
```

**Revisar:** El marco vacío por dentro (si dibuja holograma: agregar `the frame is completely empty, you can see the grey background through it`). Sin pared detrás, sin letras.

**Tripo:** Smart Mesh P2 · 5,000 caras · textura 2K → `CY01_ProyectorHolo.fbx`

**Claude Code:**
> Importa `CY01_ProyectorHolo.fbx` en `Assets/Arte/Calle/CY01/`. Prefab `CY01_ProyectorHolo`: hijo `Visual` escalado a 2.5 m de ancho sin deformar, pivote en el centro de la placa trasera, frente a +Z; material URP Lit (Max Size 2048, Smoothness 0.4, Metallic 0.3) con emisión por `MascaraEmision`. Hijo `Holograma`: quad dentro del marco con el shader `HoloAnuncio` y el componente `AnuncioHolo` (rota anuncios cada ~6 s con glitch). Pega 3 en fachadas de pisos altos de la calle de prueba. Captura antes/después de noche. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## CY02 — Pantalla publicitaria vertical
- **Medida:** 1.6 × 4.0 m, fondo 0.4 m · **Aspecto:** 9:16 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a tall vertical wall-mounted LED advertising screen, as a separate object with no wall behind it: a thick dark gunmetal frame with beveled edges around a flat smooth matte black screen surface (the screen is OFF, plain black, no image, no text). Chunky bolted mounting brackets on the back at the top and bottom, a boxy black control unit with cooling vents and small teal and amber status lights under the frame, a thick ribbed conduit running from the control unit up one side. A glowing magenta LED strip along the outer edge of the frame. View: front view rotated about 25 degrees to the left, slightly from below, the whole piece visible.
```

**Revisar:** Pantalla negra lisa, sin imagen. Marco completo y sin pared detrás.

**Tripo:** Smart Mesh P2 · 4,000 caras · textura 2K → `CY02_PantallaVertical.fbx`

**Claude Code:**
> Importa `CY02_PantallaVertical.fbx` en `Assets/Arte/Calle/CY02/`. Prefab: `Visual` escalado a 4.0 m de alto, pivote en el centro de la cara trasera, frente a +Z, URP Lit + `MascaraEmision`. Hijo `Pantalla`: quad sobre la pantalla con el shader `PantallaAnuncio` (reusa los anuncios de CY01). Colócalas en esquinas y fachadas altas: 4–6 en la calle. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## CY03 — Mazo de cables vertical (apilable)
- **Medida:** 0.6 × 3.0 m (un piso), fondo 0.3 m · **Aspecto:** 9:16 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a vertical bundle of thick cables and conduits for a building facade, as a separate object with no wall behind it: six thick rubber cables and ribbed conduits of different thickness in black, dark grey, one teal and one burnt orange, running perfectly straight and parallel from the top edge to the bottom edge, held together by three chunky metal wall clamps bolted to a flat back strip, one small boxy junction box with a glowing amber light in the middle. The cables are cut flat and clean at the top and bottom so the piece can be stacked vertically and continue seamlessly. Thick and solid, no thin wires. View: straight front view rotated about 20 degrees to the left, the whole piece visible.
```

**Revisar:** Cortes rectos arriba y abajo. Cables rectos y paralelos, gruesos.

**Tripo:** Smart Mesh P2 · 3,000 caras · textura 1K → `CY03_MazoCables.fbx`

**Claude Code:**
> Importa `CY03_MazoCables.fbx` en `Assets/Arte/Calle/CY03/`. Prefab: `Visual` escalado a 3.0 m de alto, pivote en el centro inferior de la cara trasera. En `ColocarKitPisos`, apila una columna desde la planta baja hasta la cornisa en ~30% de los edificios, pegada junto a una pilastra. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## CY04 — Antena repetidora de azotea
- **Medida:** 1.2 × 3.5 m · **Aspecto:** 9:16 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a rooftop telecom repeater mast for a cyberpunk slum: a thick square dark gunmetal pole on a heavy bolted base plate with two concrete ballast blocks, three chunky rectangular panel antennas mounted around the top, one small satellite dish on a thick arm halfway up, a boxy equipment cabinet strapped to the pole with cooling vents and teal status lights, a red aviation light on the very top, a thick ribbed conduit running down the pole into the cabinet. All parts thick and chunky, no lattice, no thin wires. View: 3/4 view from slightly below, the whole mast visible from base to top.
```

**Revisar:** Poste derecho, base plana. Antenas y parabólica pegadas, sin celosías delgadas.

**Tripo:** Smart Mesh P2 · 5,000 caras · textura 2K → `CY04_AntenaAzotea.fbx`

**Claude Code:**
> Importa `CY04_AntenaAzotea.fbx`. Prefab: `Visual` escalado a 3.5 m de alto, pivote en el centro de la base, BoxCollider. Luz roja que parpadea cada 1.5 s. En `ColocarKitPisos`, ponla en ~25% de las azoteas, detrás de `C22`. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## CY05 — Caja de servidor callejero (pared)
- **Medida:** 1.0 × 1.4 m, fondo 0.5 m · **Aspecto:** 3:4 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a wall-mounted street server cabinet hacked into the neighborhood network, as a separate object with no wall behind it: a boxy dark gunmetal metal cabinet with beveled edges and a closed door with a vented grille, two big round cooling fans on the side with a faint teal glow, a row of small teal, magenta and amber status lights, a small cracked screen showing a glowing teal bar graph with no text, a sticker shown as a plain magenta rectangle, a heavy padlock, rust and scratches, three thick ribbed conduits coming out of the top and cut flat at the top edge, a flat mounting plate on the back. View: 3/4 view from the front left, at chest height.
```

**Revisar:** Sin texto en la pantalla. Conduits cortados rectos arriba.

**Tripo:** Smart Mesh P2 · 4,000 caras · textura 1K → `CY05_ServidorCallejero.fbx`

**Claude Code:**
> Importa `CY05_ServidorCallejero.fbx`. Prefab: `Visual` escalado a 1.4 m de alto, pivote en el centro de la cara trasera, a 1.2 m del suelo. Luces que parpadean al azar, ventiladores con giro si se pueden separar, AudioSource con zumbido (rango 5 m). Uno cada ~3 edificios, más en el callejón. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## CY06 — Máquina expendedora cyber
- **Medida:** 1.1 × 2.0 m, fondo 0.9 m · **Aspecto:** 3:4 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a cyberpunk street vending machine for a Mexican slum: a tall boxy dark charcoal machine with beveled edges, a large front display window with chunky shelves of simple colorful snack and drink packages (plain colors, no text, no logos), a glowing teal touch screen and a payment scanner with an amber light on the side panel, a magenta neon strip around the front edge, a chunky dispensing slot at the bottom, dents and scratches, stickers shown as plain colored shapes, a thick power conduit going out the back. View: 3/4 view from the front left, at chest height.
```

**Revisar:** Productos sin letras ni logos. Base plana.

**Tripo:** Smart Mesh P2 · 6,000 caras · textura 2K → `CY06_Expendedora.fbx`

**Claude Code:**
> Importa `CY06_Expendedora.fbx`. Prefab: `Visual` escalado a 2.0 m de alto, pivote en el centro de la base, frente a +Z, BoxCollider, emisión por `MascaraEmision` + Point Light turquesa tenue al frente. 3 en la calle y 1 junto a la entrada del clandestino. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## CY07a — Plataforma de dron de reparto (pared)
- **Medida:** 1.6 × 0.6 m, fondo 1.2 m · **Aspecto:** 16:9 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a wall-mounted delivery drone docking pad, as a separate object with no wall behind it: a thick square dark gunmetal landing platform cantilevered on two chunky triangular brackets, with a glowing teal landing circle marking painted on top, small amber lights on the corners, a boxy charging unit with a magenta light at the back edge and a flat mounting plate at the back. Empty pad, no drone. View: 3/4 view from the front left, slightly from above so the landing pad top is visible.
```

**Revisar:** Sin dron encima. Ménsulas pegadas.

**Tripo:** Smart Mesh P2 · 4,000 caras · textura 1K → `CY07a_PlataformaDron.fbx`

**Claude Code:**
> Importa `CY07a_PlataformaDron.fbx`. Prefab: `Visual` escalado a 1.6 m de ancho, pivote en el centro del borde trasero. 3 en fachadas altas. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## CY07b — Dron de reparto
- **Medida:** 0.9 m de ancho · **Aspecto:** 1:1 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a chunky cyberpunk delivery drone: a boxy dark charcoal body with beveled edges, four thick short arms with round protective rotor rings, the rotors shown as solid flat discs, a boxy burnt-orange cargo box fixed tight under the body, a glowing teal front sensor, small red and green navigation lights, scratches. Compact and chunky, no thin parts. View: 3/4 view from the front left, slightly from above.
```

**Revisar:** Rotores como discos sólidos. Caja de carga pegada al cuerpo.

**Tripo:** Smart Mesh P2 · 5,000 caras · textura 1K → `CY07b_Dron.fbx`

**Claude Code:**
> Importa `CY07b_Dron.fbx`. Prefab `Dron`: `Visual` de 0.9 m, discos de rotor girando (si se separan; si no, quads con blur), componente `DronVuelo` que viaja entre `CY07a` y puntos al azar a 10–15 m de altura con balanceo suave. 3 drones en la calle. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## CY08 — Cámara de vigilancia corporativa
- **Medida:** 0.8 m · **Aspecto:** 1:1 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a corporate surveillance camera for a cyberpunk city, mounted on a short thick arm with a square wall plate: a sleek boxy black camera housing with a chrome visor, a big dark lens with a glowing red ring, a small antenna stub, a magenta LED line along its side, a blank plate shown as a plain white rectangle. Chunky and solid. The back of the wall plate is flat. View: 3/4 view from slightly below.
```

**Revisar:** Placa trasera plana. Sin letras.

**Tripo:** Smart Mesh P2 · 3,000 caras · textura 1K → `CY08_CamaraCorporativa.fbx`

**Claude Code:**
> Importa `CY08_CamaraCorporativa.fbx`. Prefab: `Visual` de 0.8 m, pivote en el centro de la placa trasera. Script que gira la cámara ±30° lento; aro rojo pulsando. ~8 en postes y esquinas. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## CY09 — Puesto de reparación de implantes
- **Medida:** 2.5 × 2.0 m, alto 2.5 m · **Aspecto:** 4:3 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a street stall for repairing cybernetic implants in a Mexican cyberpunk slum, shaped like a street food cart: a boxy dark charcoal and burnt-orange metal counter on small chunky wheels, a thick tight canopy roof made of a magenta and teal striped tarp on a thick metal frame, with a blank flat sign board on the front edge of the canopy (no text). On the counter: a chunky cybernetic arm being repaired lying on a padded mat, a boxy diagnostic machine with a glowing teal screen, a swing-arm magnifier lamp with a warm amber glow, a tray of chunky tools, a small stack of boxy spare parts. Under the counter a boxy battery generator with an amber light. Everything chunky, nothing thin or dangling. View: 3/4 view from slightly above, the front of the counter facing the camera at an angle.
```

**Revisar:** Letrero liso. Brazo y herramientas pegados al mostrador.

**Tripo:** HD · 10,000 caras · textura 2K → `CY09_PuestoImplantes.fbx`

**Claude Code:**
> Importa `CY09_PuestoImplantes.fbx`. Prefab como `C12_PuestoTacos` (2.5 m de ancho, pivote centro de la base, BoxCollider del mostrador), Point Light ámbar bajo la lona, `LetreroNeon` "IMPLANTES" magenta. Ponlo en la calle cerca de los tacos. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## CY10 — Cargador eléctrico de calle
- **Medida:** 0.6 × 1.8 m · **Aspecto:** 9:16 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a street electric vehicle charging post for a cyberpunk slum: a tall boxy dark gunmetal pillar with beveled edges on a small concrete base, a glowing teal vertical charge indicator bar, a small screen showing a simple teal battery icon with no text, a thick charging cable coiled tight on a hook on the side ending in a chunky connector plug, scratches, a sticker shown as a plain orange rectangle, a magenta LED strip on top. View: 3/4 view from the front left, at chest height.
```

**Revisar:** Cable enrollado pegado, no colgando. Sin texto.

**Tripo:** Smart Mesh P2 · 4,000 caras · textura 1K → `CY10_CargadorCalle.fbx`

**Claude Code:**
> Importa `CY10_CargadorCalle.fbx`. Prefab: `Visual` de 1.8 m de alto, pivote centro de la base, CapsuleCollider. 4 junto a lugares de estacionamiento. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## C18 — Barrera de banqueta con LED
- **Medida:** 2.0 × 1.0 m, fondo 0.5 m · **Aspecto:** 16:9 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a street barrier block for a cyberpunk slum sidewalk: a heavy concrete jersey-style barrier with beveled edges, painted with faded black-and-amber hazard stripes, scratches and grime, a thick glowing teal LED light strip embedded along the top edge, a small boxy warning beacon with an amber light bolted on one end, a few abstract colorful graffiti splashes with no letters. Flat ends so several can line up. View: 3/4 view from the front left, slightly from above.
```

**Revisar:** Extremos planos. Grafiti sin letras.

**Tripo:** Smart Mesh P2 · 4,000 caras · textura 1K → `C18_BarreraCalle.fbx`

**Claude Code:**
> Importa `C18_BarreraCalle.fbx`. Prefab: `Visual` de 2.0 m de largo, pivote centro de la base, BoxCollider. Reemplaza los bloques amarillos/naranjas de greybox junto a la banqueta (mantén sus colliders si tienen función). Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

---

# Parte 2 — Retrofit cyberpunk del kit de edificios (endpoint edit)

## C20acy — C20a con tecnología
- **Medida:** igual que el original · **Aspecto:** 4:3 · **Endpoint:** `edit` · **Adjuntar:** `C20a_PisoVentana.png`

```
Use the attached image as the exact base. Keep EXACTLY the same object: same shape, silhouette, size, proportions, edges, pilasters, camera angle, framing and background, and keep its existing colors and wear. Only ADD cheap futuristic cyberpunk technology bolted onto it, in the same stylized 3D game art style with soft hand-painted gradients: a thin glowing teal LED strip around the window frame; a small flat black advertising screen panel (screen off, plain black) bolted to the wall next to the window; a vertical bundle of three thick black and teal cables running from the top edge to the bottom edge near one pilaster, held by chunky clamps; a small boxy cyber air conditioner with a magenta light under the window. All additions are thick, chunky and flat against the surface, nothing sticks out more than 30 cm, nothing thin or dangling, no text, no letters, no logos. The left, right, top and bottom edges stay exactly where they are, straight and flat, so the piece still tiles with the original. Plain flat light grey background, soft even studio lighting, no cast shadow, no people.
```

**Revisar:** Misma silueta y bordes que el original (encímalas al 50%). Agregados pegados a la pared, sin letras.

**Tripo:** Smart Mesh P2 · 5,000 caras · textura 2K → `C20a_PisoVentana_cy.fbx`

**Claude Code:**
> Importa `C20a_PisoVentana_cy.fbx` en `Assets/Arte/Calle/C20a/`. Prefab `C20a_PisoVentana_cy` igual que el original (`C20a`): mismas medidas, pivote, orientación, variantes de tinte y collider; emisión por `MascaraEmision`; si trae pantalla, agrega quad `Pantalla` con `PantallaAnuncio`. En `ColocarKitPisos` / colocación de fachadas usa la versión `_cy` en ~40% de los módulos de `C20a`. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## C20bcy — C20b con tecnología
- **Medida:** igual que el original · **Aspecto:** 4:3 · **Endpoint:** `edit` · **Adjuntar:** `C20b_PisoDosVentanas_v2.png`

```
Use the attached image as the exact base. Keep EXACTLY the same object: same shape, silhouette, size, proportions, edges, pilasters, camera angle, framing and background, and keep its existing colors and wear. Only ADD cheap futuristic cyberpunk technology bolted onto it, in the same stylized 3D game art style with soft hand-painted gradients: thin glowing magenta LED strips along the windowsills; a small boxy network router with a short thick antenna and blinking teal lights mounted between the windows next to the electrical box; abstract neon-painted graffiti shapes in teal and magenta on the lower part of the wall, no letters. All additions are thick, chunky and flat against the surface, nothing sticks out more than 30 cm, nothing thin or dangling, no text, no letters, no logos. The left, right, top and bottom edges stay exactly where they are, straight and flat, so the piece still tiles with the original. Plain flat light grey background, soft even studio lighting, no cast shadow, no people.
```

**Revisar:** Misma silueta y bordes que el original (encímalas al 50%). Agregados pegados a la pared, sin letras.

**Tripo:** Smart Mesh P2 · 5,000 caras · textura 2K → `C20b_PisoDosVentanas_cy.fbx`

**Claude Code:**
> Importa `C20b_PisoDosVentanas_cy.fbx` en `Assets/Arte/Calle/C20b/`. Prefab `C20b_PisoDosVentanas_cy` igual que el original (`C20b`): mismas medidas, pivote, orientación, variantes de tinte y collider; emisión por `MascaraEmision`; si trae pantalla, agrega quad `Pantalla` con `PantallaAnuncio`. En `ColocarKitPisos` / colocación de fachadas usa la versión `_cy` en ~40% de los módulos de `C20b`. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## C21cy — C21 con tecnología
- **Medida:** igual que el original · **Aspecto:** 4:3 · **Endpoint:** `edit` · **Adjuntar:** `C21_PisoLiso.png`

```
Use the attached image as the exact base. Keep EXACTLY the same object: same shape, silhouette, size, proportions, edges, pilasters, camera angle, framing and background, and keep its existing colors and wear. Only ADD cheap futuristic cyberpunk technology bolted onto it, in the same stylized 3D game art style with soft hand-painted gradients: a large flat black advertising screen panel (screen off, plain black) with a thick gunmetal frame and a magenta LED edge mounted on the center of the wall; two thick black cables running down from the screen into a boxy junction box with an amber light; abstract neon-painted graffiti shapes in teal and magenta near the bottom, no letters. All additions are thick, chunky and flat against the surface, nothing sticks out more than 30 cm, nothing thin or dangling, no text, no letters, no logos. The left, right, top and bottom edges stay exactly where they are, straight and flat, so the piece still tiles with the original. Plain flat light grey background, soft even studio lighting, no cast shadow, no people.
```

**Revisar:** Misma silueta y bordes que el original (encímalas al 50%). Agregados pegados a la pared, sin letras.

**Tripo:** Smart Mesh P2 · 4,000 caras · textura 2K → `C21_PisoLiso_cy.fbx`

**Claude Code:**
> Importa `C21_PisoLiso_cy.fbx` en `Assets/Arte/Calle/C21/`. Prefab `C21_PisoLiso_cy` igual que el original (`C21`): mismas medidas, pivote, orientación, variantes de tinte y collider; emisión por `MascaraEmision`; si trae pantalla, agrega quad `Pantalla` con `PantallaAnuncio`. En `ColocarKitPisos` / colocación de fachadas usa la versión `_cy` en ~40% de los módulos de `C21`. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## C01cy — C01 con tecnología
- **Medida:** igual que el original · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `C01_FachadaLocal.png`

```
Use the attached image as the exact base. Keep EXACTLY the same object: same shape, silhouette, size, proportions, edges, pilasters, camera angle, framing and background, and keep its existing colors and wear. Only ADD cheap futuristic cyberpunk technology bolted onto it, in the same stylized 3D game art style with soft hand-painted gradients: a glowing teal LED strip along the top of the roll-up shutter frame; a small boxy card-reader security panel with a magenta light next to the shutter; a short thick vertical bundle of cables from the top edge into a junction box; the blank sign panel gets a thin glowing amber LED border (the panel stays blank, no text). All additions are thick, chunky and flat against the surface, nothing sticks out more than 30 cm, nothing thin or dangling, no text, no letters, no logos. The left, right, top and bottom edges stay exactly where they are, straight and flat, so the piece still tiles with the original. Plain flat light grey background, soft even studio lighting, no cast shadow, no people.
```

**Revisar:** Misma silueta y bordes que el original (encímalas al 50%). Agregados pegados a la pared, sin letras.

**Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K → `C01_FachadaLocal_cy.fbx`

**Claude Code:**
> Importa `C01_FachadaLocal_cy.fbx` en `Assets/Arte/Calle/C01/`. Prefab `C01_FachadaLocal_cy` igual que el original (`C01`): mismas medidas, pivote, orientación, variantes de tinte y collider; emisión por `MascaraEmision`; si trae pantalla, agrega quad `Pantalla` con `PantallaAnuncio`. En `ColocarKitPisos` / colocación de fachadas usa la versión `_cy` en ~40% de los módulos de `C01`. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## C02cy — C02 con tecnología
- **Medida:** igual que el original · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `C02_FachadaVivienda.png`

```
Use the attached image as the exact base. Keep EXACTLY the same object: same shape, silhouette, size, proportions, edges, pilasters, camera angle, framing and background, and keep its existing colors and wear. Only ADD cheap futuristic cyberpunk technology bolted onto it, in the same stylized 3D game art style with soft hand-painted gradients: a smart lock panel with a glowing teal keypad screen next to the door; a thin magenta LED strip around the window bars; a small boxy security camera with a red light above the door; a short thick bundle of cables running from the top edge to the electric meter. All additions are thick, chunky and flat against the surface, nothing sticks out more than 30 cm, nothing thin or dangling, no text, no letters, no logos. The left, right, top and bottom edges stay exactly where they are, straight and flat, so the piece still tiles with the original. Plain flat light grey background, soft even studio lighting, no cast shadow, no people.
```

**Revisar:** Misma silueta y bordes que el original (encímalas al 50%). Agregados pegados a la pared, sin letras.

**Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K → `C02_FachadaVivienda_cy.fbx`

**Claude Code:**
> Importa `C02_FachadaVivienda_cy.fbx` en `Assets/Arte/Calle/C02/`. Prefab `C02_FachadaVivienda_cy` igual que el original (`C02`): mismas medidas, pivote, orientación, variantes de tinte y collider; emisión por `MascaraEmision`; si trae pantalla, agrega quad `Pantalla` con `PantallaAnuncio`. En `ColocarKitPisos` / colocación de fachadas usa la versión `_cy` en ~40% de los módulos de `C02`. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## C03cy — C03 con tecnología
- **Medida:** igual que el original · **Aspecto:** 4:3 · **Endpoint:** `edit` · **Adjuntar:** `C03_FachadaPisoAlto.png`

```
Use the attached image as the exact base. Keep EXACTLY the same object: same shape, silhouette, size, proportions, edges, pilasters, camera angle, framing and background, and keep its existing colors and wear. Only ADD cheap futuristic cyberpunk technology bolted onto it, in the same stylized 3D game art style with soft hand-painted gradients: the old air conditioner becomes a boxy modern cyber cooling unit with a glowing teal fan ring; thin amber LED strips along the window frames; a small satellite dish with a magenta light mounted flat on the wall; a vertical bundle of thick cables along one pilaster from the top edge to the bottom edge. All additions are thick, chunky and flat against the surface, nothing sticks out more than 30 cm, nothing thin or dangling, no text, no letters, no logos. The left, right, top and bottom edges stay exactly where they are, straight and flat, so the piece still tiles with the original. Plain flat light grey background, soft even studio lighting, no cast shadow, no people.
```

**Revisar:** Misma silueta y bordes que el original (encímalas al 50%). Agregados pegados a la pared, sin letras.

**Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K → `C03_FachadaPisoAlto_cy.fbx`

**Claude Code:**
> Importa `C03_FachadaPisoAlto_cy.fbx` en `Assets/Arte/Calle/C03/`. Prefab `C03_FachadaPisoAlto_cy` igual que el original (`C03`): mismas medidas, pivote, orientación, variantes de tinte y collider; emisión por `MascaraEmision`; si trae pantalla, agrega quad `Pantalla` con `PantallaAnuncio`. En `ColocarKitPisos` / colocación de fachadas usa la versión `_cy` en ~40% de los módulos de `C03`. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## C22cy — C22 con tecnología
- **Medida:** igual que el original · **Aspecto:** 21:9 · **Endpoint:** `edit` · **Adjuntar:** `C22_Cornisa.png`

```
Use the attached image as the exact base. Keep EXACTLY the same object: same shape, silhouette, size, proportions, edges, pilasters, camera angle, framing and background, and keep its existing colors and wear. Only ADD cheap futuristic cyberpunk technology bolted onto it, in the same stylized 3D game art style with soft hand-painted gradients: a thin glowing teal LED strip along the underside of the concrete cap; a small chunky antenna array and a compact satellite dish on top; a boxy power unit with an amber light on the parapet. All additions are thick, chunky and flat against the surface, nothing sticks out more than 30 cm, nothing thin or dangling, no text, no letters, no logos. The left, right, top and bottom edges stay exactly where they are, straight and flat, so the piece still tiles with the original. Plain flat light grey background, soft even studio lighting, no cast shadow, no people.
```

**Revisar:** Misma silueta y bordes que el original (encímalas al 50%). Agregados pegados a la pared, sin letras.

**Tripo:** Smart Mesh P2 · 4,000 caras · textura 2K → `C22_Cornisa_cy.fbx`

**Claude Code:**
> Importa `C22_Cornisa_cy.fbx` en `Assets/Arte/Calle/C22/`. Prefab `C22_Cornisa_cy` igual que el original (`C22`): mismas medidas, pivote, orientación, variantes de tinte y collider; emisión por `MascaraEmision`; si trae pantalla, agrega quad `Pantalla` con `PantallaAnuncio`. En `ColocarKitPisos` / colocación de fachadas usa la versión `_cy` en ~40% de los módulos de `C22`. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

---

# Parte 3 — Pendientes del barrio

## C16 — Tambo
- **Medida:** 0.9 m de alto · **Aspecto:** 1:1 · **Endpoint:** `text-to-image`

```
Stylized low-poly 3D game asset in the style of a cozy industrial sci-fi prop kit: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Color palette: worn ochre yellow, burnt orange, cream and warm beige panels, dark charcoal grey, red accents on pipes and handles, small glowing teal screens and amber indicator lights. Light wear: chipped paint on edges, a few rust streaks, readable from a distance. Working-class Mexican cyberpunk neighborhood feel. No thin dangling objects, no loose hanging cables. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a standard metal oil drum barrel, faded burnt-orange paint with two thick raised rings around the body, a closed flat lid with a small round cap, a wide hand-painted ochre stripe, chipped paint and rust patches near the bottom rim, a small dent on one side, a small glowing teal tracking tag bolted on the side. No text, no hazard symbols. View: 3/4 view from slightly above.
```

**Revisar:** Sin letras ni símbolos. Tapa cerrada.

**Tripo:** Smart Mesh P2 · 3,000 caras · textura 1K → `C16_Tambo.fbx`

**Claude Code:**
> Importa `C16_Tambo.fbx`. Prefab: `Visual` de 0.9 m, pivote centro de la base, CapsuleCollider. Grupitos junto al puesto de tacos, callejón y taller. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## C17 — Pila de huacales
- **Medida:** 1.0 m de alto · **Aspecto:** 1:1 · **Endpoint:** `text-to-image`

```
Stylized low-poly 3D game asset in the style of a cozy industrial sci-fi prop kit: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Color palette: worn ochre yellow, burnt orange, cream and warm beige panels, dark charcoal grey, red accents on pipes and handles, small glowing teal screens and amber indicator lights. Light wear: chipped paint on edges, a few rust streaks, readable from a distance. Working-class Mexican cyberpunk neighborhood feel. No thin dangling objects, no loose hanging cables. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a small stack of three wooden produce crates (Mexican huacales), two crates on the bottom side by side and one on top slightly rotated, made of fewer, thick chunky wooden slats in warm faded beige and light brown with narrow gaps, every slat thick and solid. A few chunky round oranges and limes visible inside the top crate. The crates touch each other as one compact group sitting flat on the ground. View: 3/4 view from slightly above.
```

**Revisar:** Huacales juntos, sin flotar.

**Tripo:** Smart Mesh P2 · 3,000 caras · textura 1K → `C17_Huacales.fbx`

**Claude Code:**
> Importa `C17_Huacales.fbx`. Prefab: `Visual` de 1.0 m, pivote centro de la base, BoxCollider. Junto a puestos y cortinas de C01. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## C20d — Macetas en repisa
- **Medida:** 1.4 × 0.5 m, fondo 0.3 m · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `C20a_PisoVentana.png`

```
Use the attached image ONLY as the reference for the art style, colors, materials and level of wear. Stylized low-poly 3D game asset in the style of a cozy industrial sci-fi prop kit: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Color palette: worn ochre yellow, burnt orange, cream and warm beige panels, dark charcoal grey, red accents on pipes and handles, small glowing teal screens and amber indicator lights. Light wear: chipped paint on edges, a few rust streaks, readable from a distance. Working-class Mexican cyberpunk neighborhood feel. No thin dangling objects, no loose hanging cables. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a small wall-mounted window planter shelf, as a separate object with no wall behind it: a sturdy rectangular dark charcoal metal shelf frame with a thick front rail and two thick L-shaped wall brackets at the back, chipped paint and rust. On the shelf, a row of four mismatched chunky pots touching each other: two round terracotta clay pots, one bright turquoise painted metal can and one cut plastic container in faded yellow, each with simple bold chunky plants (a spiky aloe, a round cactus with a pink flower, a bushy green geranium with red flowers, a small leafy herb). Plants as solid chunky shapes, no thin leaves or stems. The back side is flat and straight to mount against a wall. View: 3/4 view from the front left and slightly from below, as seen from the street.
```

**Revisar:** Sin pared detrás. Plantas gruesas pegadas a las macetas.

**Tripo:** Smart Mesh P2 · 3,000 caras · textura 1K → `C20d_Macetas.fbx`

**Claude Code:**
> Importa `C20d_Macetas.fbx`. Prefab: `Visual` de 1.4 m, pivote centro del borde trasero, sin collider. Bajo ~25% de las ventanas de C20a/C20b sin balcón ni AC. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## C23 — Franja entre pisos
- **Medida:** 4.0 × 0.3 m · **Aspecto:** 21:9 · **Endpoint:** `edit` · **Adjuntar:** `C20a_PisoVentana.png`

```
Stylized low-poly 3D game asset in the style of a cozy industrial sci-fi prop kit: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Use the attached image as the exact reference for width, materials, level of wear and art style. Subject: a long thin modular horizontal concrete molding band for a building facade, the same width as the reference and about thirteen times wider than tall, like a thick decorative ledge between floors: a chunky protruding concrete ledge with a beveled top slope and a small groove underneath, weathered grey concrete with dark rain stains, rust streaks dripping from it and a few chipped spots, and a thin glowing teal LED strip tucked under the ledge. Short thick block ends on the left and right, aligned with the pilasters of the reference. Left and right ends straight and flat so it tiles; the back side flat. Nothing sticks out more than 30 cm. View: front view rotated about 20 degrees to the left, camera slightly below. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no people.
```

**Revisar:** Larga y delgada (si sale como pared: `only a thin horizontal ledge`).

**Tripo:** Smart Mesh P2 · 2,000 caras · textura 1K → `C23_Franja.fbx`

**Claude Code:**
> Importa `C23_Franja.fbx`. Prefab: `Visual` 4.0 × 0.3 m, pivote esquina inferior izquierda de la cara trasera, sin tinte, sin collider, emisión del LED. En `ColocarKitPisos`, en la unión entre planta baja y primer piso. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

---

# Parte 4 — Clandestino

## A11a — Entrada del clandestino: portal con cortina enrollada
- **Medida:** 3.4 × 3.8 m, fondo 0.5 m · **Aspecto:** 1:1 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a heavy entrance portal for an underground exoskeleton fight club, as a separate object with no wall around it: a thick dark gunmetal metal frame with beveled edges and diagonal amber-and-black hazard stripes on the sides, a big roll-up shutter box at the top with the steel shutter fully rolled up inside it, so the opening below is completely empty and open, thick bolts, two chunky caged floodlights with a warm amber glow on the upper corners, a boxy security panel with a magenta light on one side, a blank flat sign panel above the shutter box (no text), magenta LED strips along the inner edges of the opening. View: straight front view rotated about 20 degrees to the left, at chest height.
```

**Revisar:** Hueco vacío (se ve el fondo gris a través). Panel liso.

**Tripo:** HD · 8,000 caras · textura 2K → `A11a_PortalSotano.fbx`

**Claude Code:**
> Importa `A11a_PortalSotano.fbx` en `Assets/Arte/Clandestino/A11/`. Prefab: `Visual` escalado al ancho del hueco greybox de la entrada (~3.4 m), pivote centro inferior, frente a la calle. Colliders solo en el marco. Point Lights ámbar en los focos, `LetreroNeon` "COMBATE DE MECHAS — SÓTANO" naranja en el panel. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## A11b — Entrada del clandestino: escalera al sótano
- **Medida:** 2.8 m ancho × 4 m largo, baja 1.5 m · **Aspecto:** 4:3 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a solid block of concrete stairs going down into an underground basement, as a separate object: six thick concrete steps descending, enclosed by two short solid side walls, worn amber-and-black hazard stripes on each step edge, a thick metal pipe handrail fixed on both side walls, small amber LED strips along each step edge, grime and stains, everything solid and closed underneath. View: 3/4 view from above, looking down the stairs from the top.
```

**Revisar:** Escalones sólidos. Barandal pegado a los muros.

**Tripo:** HD · 6,000 caras · textura 2K → `A11b_EscaleraSotano.fbx`

**Claude Code:**
> Importa `A11b_EscaleraSotano.fbx`. Colócala dentro del hueco de la entrada detrás de `A11a`, bajando hacia el fondo; recorta/oculta el piso de la calle solo ahí; quad negro al fondo para la oscuridad; rampa de collider; hijo `PuntoEntrada` en el último escalón. Captura de noche de la entrada. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## A08 — Marcador colgante
- **Medida:** 3.0 × 1.8 m, fondo 1.0 m · **Aspecto:** 16:9 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a hanging scoreboard for an underground exoskeleton fight club: a chunky four-sided box made of dark gunmetal metal with beveled edges, each side with a big flat black screen (screens off, plain black), thick bolts, amber and magenta LED strips around the bottom edge, a heavy mounting frame on top with four short thick mounting rods (no chains), small caged spotlights on the bottom corners. Chunky and solid. View: 3/4 view from slightly below.
```

**Revisar:** Pantallas negras lisas. Sin cadenas.

**Tripo:** Smart Mesh P2 · 6,000 caras · textura 2K → `A08_Marcador.fbx`

**Claude Code:**
> Importa `A08_Marcador.fbx`. Prefab: `Visual` de 3.0 m de ancho, pivote en el centro de la parte superior, colgado sobre el centro de la jaula a ~7 m con barras/cadenas hechas en Unity. Quads `Pantalla` en las 4 caras con RenderTexture de una cámara UI (nombres de los peleadores y vida, placeholder sin conectar). Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## P04a — Público del clandestino (P04a)
- **Medida:** 1.75 m de pie · **Aspecto:** 3:4 · **Endpoint:** `edit` · **Adjuntar:** `P03_Referencia.png`

```
Use the attached image ONLY as the reference for the art style, rendering, shading, level of detail, stylized proportions, camera framing and background: a stylized 3D rendered game character with soft 3D shading, NOT a 2D drawing, no black ink outlines. Draw a completely DIFFERENT character, not the one in the image: do not copy his face, hair, clothes or colors. New character: a young man from the neighborhood crowd, light brown skin, short buzzed hair, a cheap teal LED visor glasses, a black sleeveless hoodie with a magenta print shown as abstract shapes, cargo pants, sneakers. Pose: standing, both fists raised in the air cheering, mouth open shouting. Full body visible, 3/4 front view, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no logos, no other people.
```

**Revisar:** Cuerpo completo, estilo 3D (sin contornos). Que no se parezca al protagonista.

**Tripo:** Smart Mesh P2 · 8,000 caras · textura 1K → `P04a_PublicoA.fbx`

## P04b — Público del clandestino (P04b)
- **Medida:** 1.75 m de pie · **Aspecto:** 3:4 · **Endpoint:** `edit` · **Adjuntar:** `P03_Referencia.png`

```
Use the attached image ONLY as the reference for the art style, rendering, shading, level of detail, stylized proportions, camera framing and background: a stylized 3D rendered game character with soft 3D shading, NOT a 2D drawing, no black ink outlines. Draw a completely DIFFERENT character, not the one in the image: do not copy his face, hair, clothes or colors. New character: a young woman from the neighborhood crowd, medium brown skin, dark hair in two chunky buns, a cropped burnt-orange jacket over a black top, dark jeans, chunky boots, a small glowing teal implant on her temple. Pose: sitting on an invisible bench, knees bent at 90 degrees, feet flat on the ground, one arm raised pointing forward, excited. Full body visible, 3/4 front view, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no logos, no other people.
```

**Revisar:** Cuerpo completo, estilo 3D (sin contornos). Que no se parezca al protagonista.

**Tripo:** Smart Mesh P2 · 8,000 caras · textura 1K → `P04b_PublicoB.fbx`

## P04c — Público del clandestino (P04c)
- **Medida:** 1.75 m de pie · **Aspecto:** 3:4 · **Endpoint:** `edit` · **Adjuntar:** `P03_Referencia.png`

```
Use the attached image ONLY as the reference for the art style, rendering, shading, level of detail, stylized proportions, camera framing and background: a stylized 3D rendered game character with soft 3D shading, NOT a 2D drawing, no black ink outlines. Draw a completely DIFFERENT character, not the one in the image: do not copy his face, hair, clothes or colors. New character: a heavy middle-aged man from the neighborhood crowd, dark brown skin, a thick mustache, a worn baseball cap, a faded grey work jacket with a teal reflective stripe, dark pants, work boots, holding a chunky drink can. Pose: sitting on an invisible bench, knees bent at 90 degrees, feet flat on the ground, leaning forward, one hand holding the can, the other a fist. Full body visible, 3/4 front view, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no logos, no other people.
```

**Revisar:** Cuerpo completo, estilo 3D (sin contornos). Que no se parezca al protagonista.

**Tripo:** Smart Mesh P2 · 8,000 caras · textura 1K → `P04c_PublicoC.fbx`

**Claude Code:**
> Importa `P04a/P04b/P04c_Publico*.fbx` en `Assets/Arte/Clandestino/Publico/` (sin rig). Reemplaza las ~200 cápsulas moradas del público: de pie (P04a) en los pasillos y sentados (P04b, P04c) en las gradas, usando GPU Instancing, color de ropa al azar con MaterialPropertyBlock (tinte), escala 0.95–1.05 y rotación hacia la jaula ±15°. Animación barata: rebote vertical y balanceo con un script o en vertex shader, con desfase al azar; más intenso cuando hay un golpe fuerte (evento público `ReaccionPublico(intensidad)`, sin conectar). LOD: a más de 25 m usa una versión más simple. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

---

# Parte 5 — Exteriores especiales y extras de calle

## E01 — Entrada del casino
- **Medida:** 8 × 5 m · **Aspecto:** 16:9 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: the grand entrance facade section of a flashy cyberpunk casino in a Mexican megacity, as a flat wall slab: a wide entrance with tall dark tinted glass double doors (opaque, no reflections), a huge chunky gold-and-black art deco frame around the doors, a big glowing marquee canopy above them with rows of round amber bulbs and thick magenta neon bands, a large blank flat sign panel above the canopy (no text), polished black stone walls with gold trim, two chunky gold pillars, a few red carpeted steps. Straight flat edges; it is a thin wall, not a whole building. View: straight front view rotated about 20 degrees to the left, from street level.
```

**Revisar:** Vidrios opacos. Panel sin texto, bordes rectos.

**Tripo:** HD · 12,000 caras · textura 4K → `E01_EntradaCasino.fbx`

**Claude Code:**
> Importa `E01_EntradaCasino.fbx`. Prefab: `Visual` de 8 m de ancho, pivote esquina inferior izquierda frontal. Colócalo en la planta baja del edificio del casino y arriba usa `C21_PisoLiso_cy`/`C20a_cy` con material oscuro; bombillas emisivas con parpadeo en secuencia; `LetreroNeon` con nombre ficticio (p. ej. "CASINO LA ESTRELLA"). Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## E02 — Portón del mercado negro
- **Medida:** 6 × 4.5 m · **Aspecto:** 4:3 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: the gated entrance of a black market in a Mexican cyberpunk slum: a big improvised arch made of stacked shipping container pieces and welded metal plates, a heavy sliding metal gate half open (the opening is empty), a thick bar of amber lights along the arch, stacked boxes and crates on both sides, a blank magenta neon sign frame on top (no text), two boxy security cameras with red lights, graffiti shown as abstract shapes with no letters. Chunky, no thin wires. View: front view rotated about 20 degrees to the left, from street level.
```

**Revisar:** Hueco del portón abierto. Sin letras.

**Tripo:** HD · 12,000 caras · textura 2K → `E02_PortonMercadoNegro.fbx`

**Claude Code:**
> Importa `E02_PortonMercadoNegro.fbx`. Prefab: `Visual` de 6 m de ancho, pivote centro inferior; colliders en el arco y el portón (el hueco cerrado con un collider invisible: solo se ve por fuera). Luces ámbar, `LetreroNeon` "MERCADO" magenta. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## E03 — Marco de pantalla gigante
- **Medida:** 6 × 4 m · **Aspecto:** 3:2 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a giant building-mounted advertising screen: a huge thick dark gunmetal frame with beveled edges around a flat matte black screen (off, plain black), chunky structural brackets and beams on the back that bolt to a building, a maintenance catwalk platform with a solid railing along the bottom, small amber warning lights on the corners, thick conduits running into a boxy power unit. View: 3/4 view from below, from the street.
```

**Revisar:** Pantalla negra lisa.

**Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K → `E03_PantallaGigante.fbx`

**Claude Code:**
> Importa `E03_PantallaGigante.fbx`. Prefab: `Visual` de 6 m, pivote centro de la cara trasera; quad `Pantalla` con `PantallaAnuncio` a mayor brillo + luz de área que tiñe la calle. 2 en esquinas de edificios altos. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## C13 — Puesto de electrónica/ciber
- **Medida:** 2.5 × 2.0 m · **Aspecto:** 4:3 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a street stall selling used electronics in a Mexican cyberpunk slum, shaped like a street food cart: a boxy dark charcoal and teal metal counter on small chunky wheels, a thick tight canopy made of an orange and black striped tarp on a thick metal frame with a blank flat sign board (no text), on the counter chunky stacks of old phones, boxy tablets, coiled thick cables tied in bundles, a small box of glowing chips, a glowing teal screen showing a simple price bar with no text, two small glowing magenta speakers. Everything chunky. View: 3/4 view from slightly above.
```

**Revisar:** Letrero liso. Objetos pegados al mostrador.

**Tripo:** HD · 10,000 caras · textura 2K → `C13_PuestoCiber.fbx`

**Claude Code:**
> Importa `C13_PuestoCiber.fbx`. Prefab como `C12_PuestoTacos`, `LetreroNeon` "CELULARES" turquesa. En la calle, lejos del puesto de implantes. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## C19 — Moto cyber estacionada (prop)
- **Medida:** 2.0 m de largo · **Aspecto:** 16:9 · **Endpoint:** `text-to-image`

```
Stylized 3D game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic textures, no fine noise. Gritty Mexican cyberpunk slum: cheap futuristic technology bolted onto old worn materials. Color palette: dark charcoal, gunmetal grey, black plastic and worn chrome, with bright glowing neon accents in teal, magenta and amber, and small touches of burnt orange. Light wear: scratches, grime, a few rust streaks. No thin dangling cables or wires: all cables are thick bundled conduits fixed to surfaces. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a parked cyberpunk street motorcycle, original design that does not resemble any real brand, low aggressive faceted bodywork in dark charcoal with burnt-orange panels and glowing teal light strips, chunky thick wheels with glowing teal rims, a boxy battery pack, a boxy cargo box on the back, scratches and patched panels, standing on its side kickstand. View: 3/4 view from the front left, slightly from above.
```

**Revisar:** Sin logos. Apoyada en el soporte.

**Tripo:** HD · 10,000 caras · textura 2K → `C19_Moto.fbx`

**Claude Code:**
> Importa `C19_Moto.fbx`. Prefab: `Visual` de 2.0 m, pivote centro de la base, BoxCollider; emisión de tiras y rines. 4–6 estacionadas en la calle. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

---

# Parte 6 — Personajes por piezas

Las piezas se generan **sin rig**. Al terminar todas las piezas de un personaje, usa el prompt de armado del final de esta parte.

## P03-cuerpo — Protagonista — cuerpo base sin cabeza
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `P03_Referencia.png`

```
Use the attached image as the exact reference for body proportions, skin color and style. Draw ONLY the bare body of this character as a standalone game base mesh, large and centered, filling most of the frame: same T-pose with arms straight out to the sides, palms down, fingers apart, feet shoulder-width apart, perfectly symmetrical. Medium brown skin, stylized slightly chunky athletic build, simple fitted plain dark charcoal base shorts, bare torso, bare arms, bare hands, bare feet. NO head: the body ends at a short clean neck stub. No jacket, no shirt, no pants, no gloves, no boots, no implant. Straight front orthographic view. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 15,000 caras · textura 4K → `P03_Cuerpo.fbx`

## P03-cabeza — Protagonista — cabeza con cuello
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `P03_Referencia.png`

```
Use the attached image as the exact reference. Isolate and redraw ONLY the head and neck of this character as a separate standalone 3D bust, large and centered, filling most of the frame. Identical face, hair, skin color. The neck ends in a clean flat horizontal cut at the base. No body, no shoulders, no jacket, no collar. Straight front orthographic view, looking straight ahead, neutral expression. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 15,000 caras · textura 4K → `P03_Cabeza.fbx`

## P03-chamarra — Protagonista — chamarra con playera
- **Medida:** — · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `P03_Referencia.png`

```
Use the attached image as the exact reference for design, colors, materials and wear. Isolate and redraw ONLY the burnt-orange work jacket together with the cream t-shirt underneath as one separate standalone object, shown on an invisible mannequin in a T-pose, as a close-up hero product shot. The jacket is HUGE in the image: the sleeve ends almost touch the left and right edges, the collar and the bottom hem almost touch the top and bottom edges. Same collar, rolled sleeves, shoulder patches, pockets and wear. The neck, sleeve and bottom openings are empty inside. No head, no neck, no skin, no hands, no gloves, no implant, no pants. Straight front orthographic view. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 15,000 caras · textura 4K → `P03_Chaqueta.fbx`

## P03-pantalon — Protagonista — pantalón con rodilleras
- **Medida:** — · **Aspecto:** 3:4 · **Endpoint:** `edit` · **Adjuntar:** `P03_Referencia.png`

```
Use the attached image as the exact reference for design, colors, materials and wear. Isolate and redraw ONLY the dark charcoal cargo pants of this character as a separate standalone object, large and centered, filling most of the frame, on an invisible mannequin standing straight with legs slightly apart: same cargo pockets, knee pads and straps. Waist opening and leg openings empty inside. No body, no skin, no jacket, no boots. Straight front orthographic view. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 10,000 caras · textura 4K → `P03_Pantalon.fbx`

## P03-bota — Protagonista — bota derecha
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `P03_Referencia.png`

```
Use the attached image as the exact reference for design, colors, materials and wear. Isolate and redraw ONLY ONE single boot, the RIGHT chunky ochre work boot of this character, as a separate standalone object, large and centered, filling most of the frame: same laces, thick rugged sole, toe cap and wear, standing upright, the top opening empty inside. Only one boot, not a pair. No leg, no pants, no skin. 3/4 view from the front outer side, slightly from above. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 10,000 caras · textura 4K → `P03_Bota.fbx`

## P03-guante — Protagonista — guante derecho
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `P03_Referencia.png`

```
Use the attached image as the exact reference for design, colors, materials and wear. Isolate and redraw ONLY ONE single glove, the RIGHT hand fingerless charcoal work glove of this character, as a separate standalone object, large and centered, filling most of the frame: standing upright with the fingers pointing up, back of the hand facing the camera, same knuckle padding and stitching, short finger tubes open at the tips, wrist opening empty inside. Only one glove. No hand, no skin, no arm, no sleeve. Straight front view, slightly angled. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 8,000 caras · textura 4K → `P03_Guante.fbx`

## P03-implante — Protagonista — implante de antebrazo (rediseñado)
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `P03_Referencia.png`

```
Use the attached image only as the reference for the art style, color palette and level of wear. Design and draw ONLY a detailed cybernetic forearm implant as a separate standalone object, large and centered, filling most of the frame: a thick armored bracer shaped like a tube that wraps fully around a forearm from wrist to elbow, closed all the way around, with an empty round opening at each end. Layered overlapping armor plates in worn bronze-grey and dark charcoal metal with large beveled edges; two thick chrome-grey hydraulic pistons along the outer side, attached at both ends; thick glowing teal light seams between the plates; a small inset screen on top showing a glowing teal bar graph with no text; chunky heat vent slats with a faint amber glow; thick ribbed dark rubber conduits embedded flat along the underside; hex bolts. One plate replaced with a mismatched burnt-orange plate, welded seams, a band of worn red electrical tape at one end, scratches. Everything thick, nothing dangling. No arm, no skin, no glove. 3/4 view from slightly above. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 15,000 caras · textura 4K → `P03_Implante.fbx`

### Armado de P03 (Claude Code, cuando estén todas sus piezas)
> Con el MCP de Blender, arma el personaje `P03` con las piezas `P03_*.fbx` de `Assets/Arte/Personajes/P03/`: 1) escala todo a 1.75–1.80 m usando el cuerpo como referencia y acomoda cada pieza en su lugar (cabeza en el cuello, prendas sobre el cuerpo, bota y guante derechos + copias espejadas para el lado izquierdo, implantes y accesorios), corrigiendo a ojo con capturas del viewport hasta que no haya huecos ni piel atravesando la ropa; 2) riggea el cuerpo como humanoide (Tripo Rigging v3.0 o Rigify) y transfiere pesos del cuerpo a cada prenda (Data Transfer, vertex weights); las piezas rígidas (máscara, implantes, bolso, tiras) se emparentan al hueso correspondiente; 3) oculta las caras del cuerpo que quedan siempre tapadas por la ropa para evitar que atraviesen; 4) exporta un FBX para Unity (Humanoid) con cada prenda como malla separada, para poder intercambiarlas. En Unity crea el prefab con un componente `Vestuario` que activa/desactiva prendas. Muéstrame capturas de frente, espalda y una pose de prueba antes de cerrar.

## R02-cuerpo — Pandillero — cuerpo base sin cabeza
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R02_Referencia.png`

```
Use the attached image as the exact reference for body proportions, skin color and art style. Draw ONLY the bare body of this character as a standalone game base mesh, large and centered, filling most of the frame: same lean athletic build with defined arms, same T-pose with arms straight out to the sides, palms down, fingers apart, feet shoulder-width apart, perfectly symmetrical. Light brown skin, simple fitted plain dark charcoal base shorts, bare torso, arms, hands and feet. NO head: the body ends at a short clean neck stub. No vest, no hoodie, no pants, no straps, no gloves, no boots, no mask. Straight front orthographic view. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 15,000 caras · textura 4K → `R02_Cuerpo.fbx`

## R02-cabeza — Pandillero — cabeza sin máscara
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R02_Referencia.png`

```
Use the attached image as the exact reference for the character and for the rendering style: a stylized 3D rendered game character with soft 3D shading and volume, exactly like the reference, NOT a 2D drawing, no black ink outlines, no flat illustration. Isolate and redraw ONLY the head and neck of this character as a separate standalone 3D bust, large and centered: same slicked-back black hair, eyes, eyebrows, skin color and stylized proportions, but WITHOUT the respirator mask: his full face uncovered, lean angular jaw, short thin black stubble, straight nose, slightly cocky serious mouth. The magenta streak is just a colored strand painted within the black hair, lying flat along the hair mass, not a separate piece sticking out. The neck ends in a clean flat horizontal cut. No body, no hood, no mask. Straight front orthographic view. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 15,000 caras · textura 4K → `R02_Cabeza.fbx`

## R02-mascara — Pandillero — máscara respirador
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R02_Referencia.png`

```
Use the attached image as the exact reference for design, colors, materials and wear. Isolate and redraw ONLY the black respirator mask of this character as a separate standalone object, large and centered, filling most of the frame: the same chunky black respirator with its two side filter cartridges with glowing magenta vents, the central glowing magenta grille and the thick strap, closed and solid, the inner side hollow where the face goes. No face, no head, no skin, no hair. 3/4 view from the front, slightly from the side. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 10,000 caras · textura 4K → `R02_Mascara.fbx`

## R02-chaleco — Pandillero — chaleco con sudadera
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R02_Referencia.png`

```
Use the attached image as the exact reference for design, colors, materials and wear. Isolate and redraw ONLY the black armored vest together with the magenta sleeveless hoodie underneath as one separate standalone object, shown on an invisible mannequin, as a close-up hero product shot, HUGE in the image, almost touching the edges: same riveted metal plates, quilted padding, magenta graffiti shapes with no letters, the hood down behind the neck with its drawstrings, the torn sleeveless armholes. Neck opening, armholes and bottom opening empty inside. No head, no arms, no skin, no mask, no pants. Straight front orthographic view. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 15,000 caras · textura 4K → `R02_Chaleco.fbx`

## R02-pantalon — Pandillero — pantalón sin tiras
- **Medida:** — · **Aspecto:** 3:4 · **Endpoint:** `edit` · **Adjuntar:** `R02_Referencia.png`

```
Use the attached image as the exact reference for design, colors, materials and wear. Isolate and redraw ONLY the black cargo pants of this character as a separate standalone object, large and centered, filling most of the frame, on an invisible mannequin standing straight with legs slightly apart: same side pockets, the dark round armored knee pads with their brown straps, same wear and dirt, but WITHOUT the long hanging straps. Waist and leg openings empty inside. No body, no skin, no vest, no boots. Straight front orthographic view. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 10,000 caras · textura 4K → `R02_Pantalon.fbx`

## R02-tira — Pandillero — tira colgante
- **Medida:** — · **Aspecto:** 9:16 · **Endpoint:** `edit` · **Adjuntar:** `R02_Referencia.png`

```
Use the attached image as the exact reference for design, colors, materials and wear. Isolate and redraw ONLY ONE of the long black hanging straps of this character as a separate standalone object, large and centered, filling most of the frame: one thick wide black canvas strap hanging straight down in a loop, with a chunky dark metal buckle clip at the top. Thick and solid like a heavy belt. Only one strap. No pants, no body. Straight front view. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 5,000 caras · textura 1K → `R02_Tira.fbx`

## R02-guante — Pandillero — guante derecho
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R02_Referencia.png`

```
Use the attached image as the exact reference for design, colors, materials and wear. Isolate and redraw ONLY ONE single glove, the RIGHT hand black fingerless glove of this character, as a separate standalone object, large and centered: same metal knuckle plates and bolts, standing upright with the fingers pointing up, back of the hand facing the camera, short finger tubes open at the tips, wrist opening empty inside. Only one glove. No hand, no skin. Straight front view, slightly angled. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 8,000 caras · textura 4K → `R02_Guante.fbx`

## R02-bota — Pandillero — bota derecha
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R02_Referencia.png`

```
Use the attached image as the exact reference for design, colors, materials and wear. Isolate and redraw ONLY ONE single boot, the RIGHT heavy black combat boot of this character, as a separate standalone object, large and centered: same thick laces, wide straps, rugged thick sole and scuffed wear, standing upright, the top opening empty inside. Only one boot. No leg, no pants. 3/4 view from the front outer side, slightly from above. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 10,000 caras · textura 4K → `R02_Bota.fbx`

### Armado de R02 (Claude Code, cuando estén todas sus piezas)
> Con el MCP de Blender, arma el personaje `R02` con las piezas `R02_*.fbx` de `Assets/Arte/Personajes/R02/`: 1) escala todo a 1.75–1.80 m usando el cuerpo como referencia y acomoda cada pieza en su lugar (cabeza en el cuello, prendas sobre el cuerpo, bota y guante derechos + copias espejadas para el lado izquierdo, implantes y accesorios), corrigiendo a ojo con capturas del viewport hasta que no haya huecos ni piel atravesando la ropa; 2) riggea el cuerpo como humanoide (Tripo Rigging v3.0 o Rigify) y transfiere pesos del cuerpo a cada prenda (Data Transfer, vertex weights); las piezas rígidas (máscara, implantes, bolso, tiras) se emparentan al hueso correspondiente; 3) oculta las caras del cuerpo que quedan siempre tapadas por la ropa para evitar que atraviesen; 4) exporta un FBX para Unity (Humanoid) con cada prenda como malla separada, para poder intercambiarlas. En Unity crea el prefab con un componente `Vestuario` que activa/desactiva prendas. Muéstrame capturas de frente, espalda y una pose de prueba antes de cerrar.

## R03-cabeza — Netrunner — cabeza sin cabello
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R03_Referencia.png`

```
Use the attached image as the exact reference for this character's face, skin and makeup. Draw ONLY her head and neck as a separate standalone 3D bust, large and centered, filling most of the frame, with no long hair: the left side of the scalp painted with a short dark shaved buzz-cut texture, the rest of the scalp smooth. Same face, same eyes with winged magenta eyeliner, same dark berry lips, same nose stud, same ears with the same silver piercings. No neck implant. The neck ends in a clean flat horizontal cut. Straight front orthographic view, looking straight ahead. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 15,000 caras · textura 4K → `R03_Cabeza.fbx`

## R03-cabello — Netrunner — mechón largo (sin parte rapada)
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R03_Referencia.png`

```
Use the attached image as the exact reference for this character's hairstyle and colors. Draw ONLY the long part of her hair as a separate standalone 3D game asset, like a hair piece, large and centered, filling most of the frame: the long straight dark hair that starts at the top of the head along the parting line, is swept over the top and falls down the right side to the collarbone in thick chunky locks, with the ends fading into bright teal. Do NOT include the shaved side: the hair piece ends in a clean edge along the parting line. The inner side that touches the scalp is hollow and curved. No head, no face, no skin, no ears. Thick solid hair shapes, no thin strands. 3/4 view from the front right, slightly from above. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 15,000 caras · textura 4K → `R03_Cabello.fbx`

## R03-guante — Netrunner — guante derecho (sin dispositivo)
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R03_Referencia.png`

```
Use the attached image as the exact reference for design, colors and wear. Draw ONLY ONE single glove, the RIGHT hand black fingerless glove of this character, as a separate standalone object, large and centered: plain black fingerless glove with a thick ribbed cuff, standing upright with fingers pointing up, back of the hand facing the camera, short finger tubes open at the tips, wrist opening empty inside, no screen and no device on it. Only one glove. No hand, no skin. Straight front view, slightly angled. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 8,000 caras · textura 4K → `R03_Guante.fbx`

## R03-implanteMano — Netrunner — dispositivo del guante
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R03_Referencia.png`

```
Use the attached image as the exact reference for design, colors and wear. Draw ONLY the small cybernetic device mounted on the back of her left glove as a separate standalone object, large and centered: a chunky rectangular dark gunmetal casing with beveled edges, a glowing teal screen on top showing a simple bar graph with no text, two small buttons on the side, and a slightly curved flat bottom plate that sits on the back of a hand. No glove, no hand. 3/4 view from slightly above. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 5,000 caras · textura 1K → `R03_ImplanteMano.fbx`

## R03-chamarra — Netrunner — chamarra bomber
- **Medida:** — · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `R03_Referencia.png`

```
Use the attached image as the exact reference for design, colors and wear. Draw ONLY the oversized cropped burnt-orange bomber jacket of this character as a separate standalone clothing asset, on an invisible mannequin in a T-pose, as a close-up hero product shot, HUGE in the image, sleeves almost touching the left and right edges: same puffy sleeves, chunky charcoal ribbed cuffs, collar, patches and wear, worn slipping off the shoulders. Neck, sleeve and bottom openings empty inside. No head, no body, no skin, no top, no hands. Straight front orthographic view. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 15,000 caras · textura 4K → `R03_Chamarra.fbx`

## R03-cuerpo — Netrunner — cuerpo con top y mallas
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R03_Referencia.png`

```
Use the attached image as the exact reference for body proportions, skin, outfit and art style. Draw ONLY her body without head as a standalone game base mesh, large and centered, in the same T-pose with arms straight out, palms down, fingers apart, feet shoulder-width apart, perfectly symmetrical. Same slim curvy athletic figure, fair skin, the thin glowing teal circuit tattoo lines on the shoulders and arms, wearing ONLY the fitted dark charcoal square-neck crop top with glowing teal trim, simple fitted black base underwear shorts, and the ripped black fishnet tights covering the legs down to bare feet. Bare arms and hands. NO head: the body ends at a short clean neck stub. No jacket, no gloves, no shorts, no belt, no pouch, no boots, no neck implant. Straight front orthographic view. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 15,000 caras · textura 4K → `R03_Cuerpo.fbx`

## R03-short — Netrunner — short con cinturón
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R03_Referencia.png`

```
Use the attached image as the exact reference for design, colors and wear. Draw ONLY her black cargo shorts with the wide grey-green belt as a separate standalone clothing asset, large and centered, on an invisible mannequin: same short black cargo shorts with rolled hems and the wide belt with its square buckle, WITHOUT the chain and WITHOUT the hip pouch. Waist and leg openings empty inside. No body, no skin, no legs, no tights. Straight front orthographic view. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 10,000 caras · textura 4K → `R03_Short.fbx`

## R03-bolso — Netrunner — bolso de cadera
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R03_Referencia.png`

```
Use the attached image as the exact reference for design, colors and wear. Draw ONLY the boxy grey-green tactical hip pouch of this character as a separate standalone object, large and centered: same chunky rectangular pouch with its flap, small glowing teal light, buckles and the short thick strap loop and clip on top that attaches to a belt. No shorts, no body. 3/4 view from slightly above. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 8,000 caras · textura 2K → `R03_Bolso.fbx`

## R03-bota — Netrunner — bota derecha
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R03_Referencia.png`

```
Use the attached image as the exact reference for design, colors and wear. Draw ONLY ONE single boot, the RIGHT chunky black platform combat boot of this character, as a separate standalone object, large and centered: same thick platform sole, ochre laces and toe shape, standing upright, the top opening empty inside. Only one boot. No leg, no tights, no skin. 3/4 view from the front outer side, slightly from above. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 10,000 caras · textura 4K → `R03_Bota.fbx`

## R03-implanteCuello — Netrunner — implante del cuello
- **Medida:** — · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `R03_Referencia.png`

```
Use the attached image as the exact reference for design and colors. Draw ONLY the chrome cybernetic data port implant on the side of her neck as a separate standalone object, large and centered: a chunky vertical chrome plate with beveled edges and three glowing teal dots in a column, slightly curved, with a curved flat back to sit on the side of a neck. No neck, no skin, no head. 3/4 view. Stylized 3D rendered game asset with soft 3D shading, the same art style as the attached image, NOT a 2D drawing, no black ink outlines. Isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos.
```

**Revisar:** Pieza sola, grande y centrada. Estilo 3D, sin contornos negros.

**Tripo:** HD · 5,000 caras · textura 1K → `R03_ImplanteCuello.fbx`

### Armado de R03 (Claude Code, cuando estén todas sus piezas)
> Con el MCP de Blender, arma el personaje `R03` con las piezas `R03_*.fbx` de `Assets/Arte/Personajes/R03/`: 1) escala todo a 1.75–1.80 m usando el cuerpo como referencia y acomoda cada pieza en su lugar (cabeza en el cuello, prendas sobre el cuerpo, bota y guante derechos + copias espejadas para el lado izquierdo, implantes y accesorios), corrigiendo a ojo con capturas del viewport hasta que no haya huecos ni piel atravesando la ropa; 2) riggea el cuerpo como humanoide (Tripo Rigging v3.0 o Rigify) y transfiere pesos del cuerpo a cada prenda (Data Transfer, vertex weights); las piezas rígidas (máscara, implantes, bolso, tiras) se emparentan al hueso correspondiente; 3) oculta las caras del cuerpo que quedan siempre tapadas por la ropa para evitar que atraviesen; 4) exporta un FBX para Unity (Humanoid) con cada prenda como malla separada, para poder intercambiarlas. En Unity crea el prefab con un componente `Vestuario` que activa/desactiva prendas. Muéstrame capturas de frente, espalda y una pose de prueba antes de cerrar.

---

# Parte 7 — Interiores extra (casa y taller)

## H11 — Buró / mesa de noche
- **Medida:** 0.5 × 0.6 m · **Aspecto:** 1:1 · **Endpoint:** `text-to-image`

```
Stylized low-poly 3D game asset in the style of a cozy industrial sci-fi prop kit: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Color palette: worn ochre yellow, burnt orange, cream and warm beige panels, dark charcoal grey, red accents on pipes and handles, small glowing teal screens and amber indicator lights. Light wear: chipped paint on edges, a few rust streaks, readable from a distance. Working-class Mexican cyberpunk neighborhood feel. No thin dangling objects, no loose hanging cables. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a small humble bedside nightstand: a chunky wooden box with one drawer with a round knob, faded light blue paint with chipped edges, on top a chunky old alarm clock with an amber glowing face, a small clay cup and a cheap phone charger dock with a teal light. View: 3/4 view from slightly above.
```

**Revisar:** Objetos pegados, nada flotando. Sin letras.

**Tripo:** Smart Mesh P2 · 3,000 caras · textura 1K → `H11_Buro.fbx`

**Claude Code:**
> Importa `H11_Buro.fbx` en `Assets/Arte/Casa/H11/`. Prefab: `Visual` escalado a 0.5 × 0.6 m, pivote centro de la base (o de la cara trasera si va en pared), URP Lit + `MascaraEmision`, collider ajustado. Colócalo en la casa/taller de prueba. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## H06 — Tele vieja sobre mueble
- **Medida:** 1.2 m · **Aspecto:** 4:3 · **Endpoint:** `text-to-image`

```
Stylized low-poly 3D game asset in the style of a cozy industrial sci-fi prop kit: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Color palette: worn ochre yellow, burnt orange, cream and warm beige panels, dark charcoal grey, red accents on pipes and handles, small glowing teal screens and amber indicator lights. Light wear: chipped paint on edges, a few rust streaks, readable from a distance. Working-class Mexican cyberpunk neighborhood feel. No thin dangling objects, no loose hanging cables. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: an old boxy CRT television on a low wooden TV cabinet in a humble home, the screen is flat dark grey (off, no image), a chunky antenna on top, a small boxy cyber streaming box with a teal light next to it, a doily under the TV, the cabinet with two doors. View: 3/4 view from slightly above.
```

**Revisar:** Objetos pegados, nada flotando. Sin letras.

**Tripo:** Smart Mesh P2 · 5,000 caras · textura 1K → `H06_TeleVieja.fbx`

**Claude Code:**
> Importa `H06_TeleVieja.fbx` en `Assets/Arte/Casa/H06/`. Prefab: `Visual` escalado a 1.2 m, pivote centro de la base (o de la cara trasera si va en pared), URP Lit + `MascaraEmision`, collider ajustado. Colócalo en la casa/taller de prueba. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## H07 — Sofá
- **Medida:** 2.0 m · **Aspecto:** 16:9 · **Endpoint:** `text-to-image`

```
Stylized low-poly 3D game asset in the style of a cozy industrial sci-fi prop kit: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Color palette: worn ochre yellow, burnt orange, cream and warm beige panels, dark charcoal grey, red accents on pipes and handles, small glowing teal screens and amber indicator lights. Light wear: chipped paint on edges, a few rust streaks, readable from a distance. Working-class Mexican cyberpunk neighborhood feel. No thin dangling objects, no loose hanging cables. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a humble worn three-seat sofa covered by a thick striped Mexican sarape blanket in burnt orange, cream and teal tucked over the back as one solid shape, two chunky cushions, short thick wooden legs. View: 3/4 view from the front left, slightly from above.
```

**Revisar:** Objetos pegados, nada flotando. Sin letras.

**Tripo:** Smart Mesh P2 · 5,000 caras · textura 1K → `H07_Sofa.fbx`

**Claude Code:**
> Importa `H07_Sofa.fbx` en `Assets/Arte/Casa/H07/`. Prefab: `Visual` escalado a 2.0 m, pivote centro de la base (o de la cara trasera si va en pared), URP Lit + `MascaraEmision`, collider ajustado. Colócalo en la casa/taller de prueba. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## H08 — Estufa y tarja
- **Medida:** 1.5 m · **Aspecto:** 16:9 · **Endpoint:** `text-to-image`

```
Stylized low-poly 3D game asset in the style of a cozy industrial sci-fi prop kit: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Color palette: worn ochre yellow, burnt orange, cream and warm beige panels, dark charcoal grey, red accents on pipes and handles, small glowing teal screens and amber indicator lights. Light wear: chipped paint on edges, a few rust streaks, readable from a distance. Working-class Mexican cyberpunk neighborhood feel. No thin dangling objects, no loose hanging cables. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a humble kitchen counter unit: an old cream enamel stove with four burners and a dented clay pot on top, joined to a small concrete sink counter with a chunky faucet and a stack of plates, a small shelf with spice jars as plain colored shapes, a tiled backsplash in teal and white. View: 3/4 view from the front left, slightly from above.
```

**Revisar:** Objetos pegados, nada flotando. Sin letras.

**Tripo:** Smart Mesh P2 · 6,000 caras · textura 1K → `H08_EstufaTarja.fbx`

**Claude Code:**
> Importa `H08_EstufaTarja.fbx` en `Assets/Arte/Casa/H08/`. Prefab: `Visual` escalado a 1.5 m, pivote centro de la base (o de la cara trasera si va en pared), URP Lit + `MascaraEmision`, collider ajustado. Colócalo en la casa/taller de prueba. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## H09 — Repisa con veladoras
- **Medida:** 1.0 m · **Aspecto:** 16:9 · **Endpoint:** `text-to-image`

```
Stylized low-poly 3D game asset in the style of a cozy industrial sci-fi prop kit: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Color palette: worn ochre yellow, burnt orange, cream and warm beige panels, dark charcoal grey, red accents on pipes and handles, small glowing teal screens and amber indicator lights. Light wear: chipped paint on edges, a few rust streaks, readable from a distance. Working-class Mexican cyberpunk neighborhood feel. No thin dangling objects, no loose hanging cables. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a small wooden wall shelf with a row of chunky glass votive candles with warm amber glowing flames, small clay pots with simple marigold flowers, a few plain picture frames showing only soft abstract colors, and a small string of chunky paper decorations shown as solid colored shapes. The back is flat to mount on a wall. View: 3/4 view from the front left.
```

**Revisar:** Objetos pegados, nada flotando. Sin letras.

**Tripo:** Smart Mesh P2 · 4,000 caras · textura 1K → `H09_RepisaVeladoras.fbx`

**Claude Code:**
> Importa `H09_RepisaVeladoras.fbx` en `Assets/Arte/Casa/H09/`. Prefab: `Visual` escalado a 1.0 m, pivote centro de la base (o de la cara trasera si va en pared), URP Lit + `MascaraEmision`, collider ajustado. Colócalo en la casa/taller de prueba. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## T06 — Soldadora con carrete
- **Medida:** 1.0 m · **Aspecto:** 1:1 · **Endpoint:** `text-to-image`

```
Stylized low-poly 3D game asset in the style of a cozy industrial sci-fi prop kit: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Color palette: worn ochre yellow, burnt orange, cream and warm beige panels, dark charcoal grey, red accents on pipes and handles, small glowing teal screens and amber indicator lights. Light wear: chipped paint on edges, a few rust streaks, readable from a distance. Working-class Mexican cyberpunk neighborhood feel. No thin dangling objects, no loose hanging cables. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a chunky portable welding machine on small wheels: a boxy red metal body with a big round dial and an amber light, a thick coiled welding cable wound tight on a side hook ending in a chunky welding torch clipped to the body, a welding helmet resting on top. View: 3/4 view from slightly above.
```

**Revisar:** Objetos pegados, nada flotando. Sin letras.

**Tripo:** Smart Mesh P2 · 5,000 caras · textura 1K → `T06_Soldadora.fbx`

**Claude Code:**
> Importa `T06_Soldadora.fbx` en `Assets/Arte/Taller/T06/`. Prefab: `Visual` escalado a 1.0 m, pivote centro de la base (o de la cara trasera si va en pared), URP Lit + `MascaraEmision`, collider ajustado. Colócalo en la casa/taller de prueba. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## T07 — Grúa de motor
- **Medida:** 2.5 m · **Aspecto:** 1:1 · **Endpoint:** `text-to-image`

```
Stylized low-poly 3D game asset in the style of a cozy industrial sci-fi prop kit: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Color palette: worn ochre yellow, burnt orange, cream and warm beige panels, dark charcoal grey, red accents on pipes and handles, small glowing teal screens and amber indicator lights. Light wear: chipped paint on edges, a few rust streaks, readable from a distance. Working-class Mexican cyberpunk neighborhood feel. No thin dangling objects, no loose hanging cables. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a mechanic engine hoist crane: a thick yellow-ochre steel boom arm on a heavy base with four chunky caster wheels, a thick hydraulic piston, a short thick chain with a chunky hook at the end of the boom, chipped paint and oil stains. View: 3/4 view from the side, slightly from above.
```

**Revisar:** Objetos pegados, nada flotando. Sin letras.

**Tripo:** Smart Mesh P2 · 6,000 caras · textura 1K → `T07_GruaMotor.fbx`

**Claude Code:**
> Importa `T07_GruaMotor.fbx` en `Assets/Arte/Taller/T07/`. Prefab: `Visual` escalado a 2.5 m, pivote centro de la base (o de la cara trasera si va en pared), URP Lit + `MascaraEmision`, collider ajustado. Colócalo en la casa/taller de prueba. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

## T08 — Repisa del taller
- **Medida:** 2.0 m · **Aspecto:** 16:9 · **Endpoint:** `text-to-image`

```
Stylized low-poly 3D game asset in the style of a cozy industrial sci-fi prop kit: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Color palette: worn ochre yellow, burnt orange, cream and warm beige panels, dark charcoal grey, red accents on pipes and handles, small glowing teal screens and amber indicator lights. Light wear: chipped paint on edges, a few rust streaks, readable from a distance. Working-class Mexican cyberpunk neighborhood feel. No thin dangling objects, no loose hanging cables. Single object, centered, fully visible, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no logos, no people. Subject: a sturdy metal garage shelving unit with three thick shelves holding chunky boxes, oil cans as plain colored shapes, a coil of thick hose, spare exoskeleton parts like a chunky metal hand and a piston, a small radio with an amber dial. Everything chunky, touching the shelves. View: 3/4 view from the front left.
```

**Revisar:** Objetos pegados, nada flotando. Sin letras.

**Tripo:** Smart Mesh P2 · 6,000 caras · textura 1K → `T08_RepisaTaller.fbx`

**Claude Code:**
> Importa `T08_RepisaTaller.fbx` en `Assets/Arte/Taller/T08/`. Prefab: `Visual` escalado a 2.0 m, pivote centro de la base (o de la cara trasera si va en pared), URP Lit + `MascaraEmision`, collider ajustado. Colócalo en la casa/taller de prueba. Si no se indicó material: URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision` si tiene luces. Captura de noche en docs/capturas, commit y push en tu rama. No toques prefabs del juego salvo lo indicado.

---

# Para tu Claude Code: generar las imágenes en automático
> En la carpeta `imageassets` está `lote_prompts.json`. Escribe un script Python `generar_imagenes.py` en esa carpeta que: lea la API key de la variable de entorno `WAVESPEED_API_KEY` (nunca la escribas en archivos ni en el repo); por cada trabajo del lote (o solo los IDs que le pase con `--ids`, o una parte con `--parte`), llame a la API de WaveSpeed con el modelo de Nano Banana 2.1 que indique el campo `endpoint` (`google/nano-banana-2.1/text-to-image` o `google/nano-banana-2.1/edit`), revisando la documentación de WaveSpeed para los nombres exactos de los campos; para `edit`, suba primero las imágenes de `referencias` desde la carpeta (endpoint de subida de medios de WaveSpeed) y pase sus URLs; use `aspect_ratio` y resolución `2k`, formato png; espere el resultado consultando la predicción cada 2–5 s; descargue cada imagen a `imageassets/generadas/<archivo>_v<n>.png` (genera `--variantes` por trabajo, por defecto 2) y no sobreescriba archivos existentes; registre en `generadas/registro.csv` id, archivo, prompt, costo si viene en la respuesta y estado. Antes de lanzar todo, corre solo `CY01` como prueba y muéstrame el resultado.


---

# Parte 8 — Torres de neón alto (Nova y refinería CANCELADAS el 31-oct)
Lote: `imageassets/lote_torres.json` (16 imágenes). Se guardan en `imageassets/torres/<código>.png` e `imageassets/refineria/<código>.png`.
**Orden:** primero la **fase 1** (T21, T24b, T25 y toda la refinería). Cuando T21 y T24b estén aprobados, la **fase 2** (T20, T22, T23, T24a, T24c), que los usa como referencia para que la planta, los pilares y la rejilla de ventanas coincidan al apilar.
**Regla del kit:** T20–T23 miden 12 × 12 m de planta; T24a–c miden 16 × 16 m. Cara de arriba y de abajo planas. Rejilla de ventanas igual en todos. Vidrio oscuro liso (la luz la pone Unity con `VentanaLuz`).
**Códigos:** la refinería usa **RF01–RF07** (no R01–R07) para no chocar con los códigos de personajes R02/R03.

## T21 — PisoOficinas
- **Medida real:** 12 × 12 m de planta, 4 m de alto · **Fase:** 1 · **Aspecto:** 16:9 · **Endpoint:** `text-to-image`

```
Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: antennas, railings, ladders and cables are thick and chunky. Rich cyberpunk business district: clean dark charcoal and gunmetal grey structure with polished dark blue-black glass, small glowing acid green and magenta accents, thin teal light strips. Very light wear, clean and expensive-looking. Modular building section for a game kit, shaped as a box with a perfectly square footprint (equal width and depth). The top face and the bottom face are perfectly flat and horizontal so identical modules stack exactly on top of each other. Thick square corner pillars at all four corners. Windows are flat smooth opaque dark blue-black glass panels: no reflections, no interiors, no curtains, no light behind them. Nothing protrudes more than a hand's width from the facade unless stated. Subject: one single floor module of a rich corporate office tower, a wide low square block three times wider than tall: on each of the four sides the same glass curtain wall grid of exactly 4 equal tall window panels separated by thick charcoal vertical mullions, a thick charcoal horizontal band along the top edge and the bottom edge, and a thin glowing teal light strip running along the top and bottom edges of every face. All four sides identical. View: 3/4 view at 45 degrees showing the front face and the right face equally, camera at the height of the module, slight perspective.
```

**Tripo:** Image to 3D · Smart Mesh P2 · 10,000 caras · textura 2K · Delight apagado · sin rig → `T21_PisoOficinas.fbx`

**Claude Code:**
> Importa `T21_PisoOficinas.fbx` en `Assets/Arte/Torres/T21/`. Prefab `T21_PisoOficinas`: `Visual` escalado a 12 × 12 m de planta y 4 m de alto (si la proporción no coincide, escala X/Z a 12 m y Y a 4 m y avísame cuánto se deformó), pivote en el centro de la cara inferior. Vidrios con `VentanaLuz` (40–60% encendidas, frías y cálidas), franjas turquesa con `MascaraEmision`. BoxCollider. Material URP Lit (Smoothness 0.3–0.5, Metallic 0.2) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## T22 — PisoPantalla
- **Medida real:** 12 × 12 m de planta, 4 m de alto · **Fase:** 2 · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `imageassets/torres/T21.png`

```
Use the attached image as the exact reference for the footprint width and depth, corner pillars, window grid position and spacing, materials, colors and art style, so this module stacks perfectly with it. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: antennas, railings, ladders and cables are thick and chunky. Rich cyberpunk business district: clean dark charcoal and gunmetal grey structure with polished dark blue-black glass, small glowing acid green and magenta accents, thin teal light strips. Very light wear, clean and expensive-looking. Modular building section for a game kit, shaped as a box with a perfectly square footprint (equal width and depth). The top face and the bottom face are perfectly flat and horizontal so identical modules stack exactly on top of each other. Thick square corner pillars at all four corners. Windows are flat smooth opaque dark blue-black glass panels: no reflections, no interiors, no curtains, no light behind them. Nothing protrudes more than a hand's width from the facade unless stated. Subject: the same office tower floor module as the reference, identical on three sides, but on the front face a large flat matte black advertising screen (screen OFF, plain black, no image) covering most of the face, flush with the facade, with a thick gunmetal frame and a thin glowing magenta LED edge. View: 3/4 view at 45 degrees showing the front face and the right face equally, camera at the height of the module, slight perspective.
```

**Tripo:** Image to 3D · Smart Mesh P2 · 10,000 caras · textura 2K · Delight apagado · sin rig → `T22_PisoPantalla.fbx`

**Claude Code:**
> Importa `T22_PisoPantalla.fbx` en `Assets/Arte/Torres/T22/`. Prefab `T22_PisoPantalla`: igual que `T21` (12 × 12 × 4 m, mismo pivote). Quad `Pantalla` sobre la pantalla con `PantallaAnuncio`. BoxCollider. Material URP Lit (Smoothness 0.3–0.5, Metallic 0.2) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## T20 — BaseTorre
- **Medida real:** 12 × 12 m de planta, 6 m de alto (marquesina sale hasta 3 m) · **Fase:** 2 · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `imageassets/torres/T21.png`

```
Use the attached image as the exact reference for the footprint width and depth, corner pillars, window grid position and spacing, materials, colors and art style, so this module stacks perfectly with it. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: antennas, railings, ladders and cables are thick and chunky. Rich cyberpunk business district: clean dark charcoal and gunmetal grey structure with polished dark blue-black glass, small glowing acid green and magenta accents, thin teal light strips. Very light wear, clean and expensive-looking. Modular building section for a game kit, shaped as a box with a perfectly square footprint (equal width and depth). The top face and the bottom face are perfectly flat and horizontal so identical modules stack exactly on top of each other. Thick square corner pillars at all four corners. Windows are flat smooth opaque dark blue-black glass panels: no reflections, no interiors, no curtains, no light behind them. Nothing protrudes more than a hand's width from the facade unless stated. Subject: the ground floor base module of the same rich office tower, one and a half times taller than the reference: same corner pillars and same window grid on the sides and back; on the front face a tall glass entrance with double glass doors in the center between two thick dark columns, a thick cantilevered canopy marquee over the entrance with an acid green glowing edge (the canopy may stick out about a quarter of the width), and a blank flat sign panel above the canopy (no text). A dark stone plinth along the bottom. Bottom face flat. View: 3/4 view at 45 degrees showing the front face and the right face equally, camera at the height of the module, slight perspective.
```

**Tripo:** Image to 3D · Smart Mesh P2 · 12,000 caras · textura 2K · Delight apagado · sin rig → `T20_BaseTorre.fbx`

**Claude Code:**
> Importa `T20_BaseTorre.fbx` en `Assets/Arte/Torres/T20/`. Prefab `T20_BaseTorre`: `Visual` 12 × 12 m de planta y 6 m de alto (sin contar la marquesina), pivote en el centro de la cara inferior. `LetreroNeon` en el panel (nombre ficticio), luz de la marquesina, vidrios con `VentanaLuz`. BoxCollider dejando libre la entrada si se puede entrar; si no, collider completo. Material URP Lit (Smoothness 0.3–0.5, Metallic 0.2) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## T23 — RemateTorre
- **Medida real:** 12 × 12 m de planta, 5 m de cuerpo + antena ~6 m arriba · **Fase:** 2 · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `imageassets/torres/T21.png`

```
Use the attached image as the exact reference for the footprint width and depth, corner pillars, window grid position and spacing, materials, colors and art style, so this module stacks perfectly with it. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: antennas, railings, ladders and cables are thick and chunky. Rich cyberpunk business district: clean dark charcoal and gunmetal grey structure with polished dark blue-black glass, small glowing acid green and magenta accents, thin teal light strips. Very light wear, clean and expensive-looking. Subject: the roof crown module of the same rich office tower: same square footprint and corner pillars as the reference; a short band of the same window grid at the bottom, then a solid charcoal parapet; on the flat roof a circular landing pad with a simple painted acid green ring (no letters), three boxy air-conditioning units with big fans, a boxy water tank, and one thick tall antenna mast with a glowing red light on top (the mast may rise well above the roof). The bottom face is flat to sit on top of the floor modules. View: 3/4 view at 45 degrees from slightly above so the roof is visible, the whole antenna inside the image.
```

**Tripo:** Image to 3D · Smart Mesh P2 · 12,000 caras · textura 2K · Delight apagado · sin rig → `T23_RemateTorre.fbx`

**Claude Code:**
> Importa `T23_RemateTorre.fbx` en `Assets/Arte/Torres/T23/`. Prefab `T23_RemateTorre`: `Visual` 12 × 12 m de planta, cuerpo de 5 m (la antena queda por encima), pivote en el centro de la cara inferior. Luz roja de la antena parpadeando. BoxCollider del cuerpo. Material URP Lit (Smoothness 0.3–0.5, Metallic 0.2) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## T25 — EdificioMedio
- **Medida real:** 12 × 12 m de planta, ~10 m de alto (3 pisos); balcones salen hasta 1 m · **Fase:** 1 · **Aspecto:** 1:1 · **Endpoint:** `text-to-image`

```
Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: antennas, railings, ladders and cables are thick and chunky. Middle-class Mexican cyberpunk neighborhood: painted concrete in faded teal and warm cream with some newer dark grey metal panels, a few small glowing teal and magenta accents, light wear. Subject: a medium three-story apartment building with a square footprint, the visual bridge between a poor neighborhood and a rich district: painted concrete facades, rows of windows mixing barred old windows and newer dark blue-black glass windows, small balconies with thick solid railings and a couple of plants, a ground floor with two rolled-down metal shutters and an entrance door, a few boxy air conditioners, thin glowing teal light strips on the balcony edges, a flat roof with a low parapet and a water tank. Bottom face flat. View: 3/4 view at 45 degrees showing the front and right facades, camera at mid height.
```

**Tripo:** Image to 3D · Smart Mesh P2 · 12,000 caras · textura 2K · Delight apagado · sin rig → `T25_EdificioMedio.fbx`

**Claude Code:**
> Importa `T25_EdificioMedio.fbx` en `Assets/Arte/Torres/T25/`. Prefab `T25_EdificioMedio`: `Visual` 12 × 12 m de planta, ~10 m de alto, pivote en el centro de la cara inferior. Ventanas con `VentanaLuz`, BoxCollider. Úsalo entre el barrio y el distrito de torres. Material URP Lit (Smoothness 0.3–0.5, Metallic 0.2) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## Armado de torres (Claude Code, cuando estén los FBX)
> Crea `ArmarTorre`: apila `T20` + N×(`T21` con ~25% `T22`, nunca dos `T22` seguidos) + `T23`, alineando centros y caras, 4 m por piso. Si la rejilla no coincide entre módulos, ajusta escala X/Z para alinear los pilares de las esquinas y avísame. Coloca 4–6 torres de 6–14 pisos en el distrito rico (con `E01`, `E03`, `CY02`, `V01b`, `C19`, `CY10`, `N02`, `N07`, `N08`) y `T25` como transición entre barrio y torres. LOD y GPU Instancing; torres lejanas como silueta. Capturas de noche desde la calle y desde una azotea; commit y push.


---

# Parte 9 — Ciudad 2 (N01–N08)
Lote: `imageassets/lote_ciudad2.json` (9 imágenes). Salida: `imageassets/ciudad2/<código>.png`. Todas van por `edit` con 2 escenas de `docs/referencias-ciudad/` solo como referencia de estilo.

## N01 — LamparaJaula
- **Medida real:** 40 cm (brazo + jaula) · **Aspecto:** 1:1 · **Adjuntar:** `docs/referencias-ciudad/callejon-noche.png`, `docs/referencias-ciudad/calle-suburbios-noche.png`

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy the scenes. Draw only the single object described, isolated on a plain flat light grey background. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Gritty Mexican cyberpunk city. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: railings, wires, tubes and brackets are thick and chunky. View: 3/4 view from the front, slightly from above unless stated. Subject: a wall-mounted caged lamp: a short thick dark charcoal metal arm coming out of a square wall plate, holding a chunky round protective cage made of a few thick bars around a big warm amber glowing bulb, a small boxy junction cap, rust spots. The back of the wall plate is flat. View: 3/4 view from the side and slightly from below.
```

**Tripo:** Image to 3D · Smart Mesh P2 · 8,000 caras · textura 1K · Delight apagado · sin rig → `N01_LamparaJaula.fbx`

**Claude Code:**
> Importa `N01_LamparaJaula.fbx` en `Assets/Arte/Calle/N01/`. Prefab `N01_LamparaJaula`: `Visual` de 0.4 m de largo, pivote en el centro de la placa trasera, Point Light ámbar (rango ~5 m) en el foco, sin collider. Úsala en callejón, puertas traseras y fachadas. Material URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## N02 — AutopistaElevada
- **Medida real:** 12 m de largo × 8 m de alto (incluye columnas) · **Aspecto:** 16:9 · **Adjuntar:** `docs/referencias-ciudad/neon-alto-noche.png`, `docs/referencias-ciudad/azotea-vista-ciudad.png`

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy the scenes. Draw only the single object described, isolated on a plain flat light grey background. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Gritty Mexican cyberpunk city. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: railings, wires, tubes and brackets are thick and chunky. View: 3/4 view from the front, slightly from above unless stated. Subject: a straight section of an elevated concrete highway overpass: a thick flat concrete road deck with beveled edges, two very thick concrete columns underneath with wide capitals, a thick solid concrete safety barrier with a chunky metal top rail along both edges of the deck, a glowing teal light strip running along the underside edge of the deck, dark rain stains and a few abstract neon graffiti shapes on the columns with no letters. Both ends of the deck are cut perfectly flat so identical sections line up end to end. View: 3/4 view from the front left, from below at street level.
```

**Tripo:** Image to 3D · HD · 20,000 caras · textura 2K · Delight apagado · sin rig → `N02_AutopistaElevada.fbx`

**Claude Code:**
> Importa `N02_AutopistaElevada.fbx` en `Assets/Arte/Calle/N02/`. Prefab `N02_AutopistaElevada`: `Visual` de 12 m de largo y 8 m de alto, pivote en el centro entre las columnas a nivel de suelo, extremos del tablero alineados al eje X para encadenar copias cada 12 m. Colliders en columnas y tablero (caminable si se sube). Emisión de la franja turquesa con `MascaraEmision` + luces turquesa bajo el tablero cada ~6 m. Material URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## N03a — PuestoMagenta
- **Medida real:** 3 × 2 m de planta, ~2.4 m de alto · **Aspecto:** 4:3 · **Adjuntar:** `docs/referencias-ciudad/tianguis-noche.png`, `docs/referencias-ciudad/entrada-mercado-noche.png`

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy the scenes. Draw only the single object described, isolated on a plain flat light grey background. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Gritty Mexican cyberpunk city. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: railings, wires, tubes and brackets are thick and chunky. View: 3/4 view from the front, slightly from above unless stated. Subject: a Mexican street market stall (tianguis): a sturdy frame of thick grey metal tubes holding a tight flat roof made of a plain solid magenta tarp as one thick smooth shape, a long wooden table with a few stacks of chunky boxes of goods and a crate under it, a plastic stool, a small warm amber bulb hanging tight under the tarp. Everything thick and solid, nothing flapping. View: 3/4 view from the front left, slightly from above.
```

**Tripo:** Image to 3D · HD · 10,000 caras · textura 2K · Delight apagado · sin rig → `N03a_PuestoMagenta.fbx`

**Claude Code:**
> Importa `N03a_PuestoMagenta.fbx` en `Assets/Arte/Calle/N03a/`. Prefab `N03a_PuestoMagenta`: `Visual` de 3 m de ancho, pivote centro de la base, BoxCollider de la mesa, Point Light ámbar bajo la lona. Material URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## N03b — PuestoRayas
- **Medida real:** 3 × 2 m de planta, ~2.4 m de alto · **Aspecto:** 4:3 · **Adjuntar:** `docs/referencias-ciudad/tianguis-noche.png`, `docs/referencias-ciudad/entrada-mercado-noche.png`

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy the scenes. Draw only the single object described, isolated on a plain flat light grey background. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Gritty Mexican cyberpunk city. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: railings, wires, tubes and brackets are thick and chunky. View: 3/4 view from the front, slightly from above unless stated. Subject: a Mexican street market stall (tianguis): a sturdy frame of thick grey metal tubes holding a tight flat roof made of a tarp with bold orange and white stripes as one thick smooth shape, a long wooden table with a few stacks of chunky boxes of goods and a crate under it, a plastic stool, a small warm amber bulb hanging tight under the tarp. Everything thick and solid, nothing flapping. View: 3/4 view from the front left, slightly from above.
```

**Tripo:** Image to 3D · HD · 10,000 caras · textura 2K · Delight apagado · sin rig → `N03b_PuestoRayas.fbx`

**Claude Code:**
> Importa `N03b_PuestoRayas.fbx` en `Assets/Arte/Calle/N03b/`. Prefab `N03b_PuestoRayas`: igual que `N03a`. Material URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## N04 — TuberiasPared
- **Medida real:** 3 m de largo · **Aspecto:** 16:9 · **Adjuntar:** `docs/referencias-ciudad/callejon-noche.png`, `docs/referencias-ciudad/entrada-mercado-noche.png`

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy the scenes. Draw only the single object described, isolated on a plain flat light grey background. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Gritty Mexican cyberpunk city. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: railings, wires, tubes and brackets are thick and chunky. View: 3/4 view from the front, slightly from above unless stated. Subject: a wall-mounted pipe section: two thick parallel horizontal pipes (one rusty orange, one dark grey) with two chunky elbow bends going up at one end, a big red valve wheel, thick metal clamps bolting them to a flat back strip, rust and grime. Pipes cut flat at the ends so sections can continue. The back side is flat to mount against a wall. View: front view rotated about 25 degrees to the left.
```

**Tripo:** Image to 3D · Smart Mesh P2 · 8,000 caras · textura 1K · Delight apagado · sin rig → `N04_TuberiasPared.fbx`

**Claude Code:**
> Importa `N04_TuberiasPared.fbx` en `Assets/Arte/Calle/N04/`. Prefab `N04_TuberiasPared`: `Visual` de 3 m, pivote en el centro de la cara trasera, sin collider. Callejón y fachadas traseras. Material URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## N05 — Ventila
- **Medida real:** 1 m de ancho · **Aspecto:** 1:1 · **Adjuntar:** `docs/referencias-ciudad/callejon-noche.png`, `docs/referencias-ciudad/calle-suburbios-noche.png`

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy the scenes. Draw only the single object described, isolated on a plain flat light grey background. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Gritty Mexican cyberpunk city. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: railings, wires, tubes and brackets are thick and chunky. View: 3/4 view from the front, slightly from above unless stated. Subject: a rectangular wall exhaust vent: a chunky dark gunmetal metal box with a thick louvered grille on the front, a short thick square duct coming out of one side, bolts, rust streaks and a faint teal indicator light. The back side is flat to mount against a wall. View: 3/4 view from the front left.
```

**Tripo:** Image to 3D · Smart Mesh P2 · 8,000 caras · textura 1K · Delight apagado · sin rig → `N05_Ventila.fbx`

**Claude Code:**
> Importa `N05_Ventila.fbx` en `Assets/Arte/Calle/N05/`. Prefab `N05_Ventila`: `Visual` de 1 m de ancho, pivote centro de la cara trasera, sin collider. Partículas de vapor saliendo de la rejilla en algunas copias. Material URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## N06 — BolsasBasura
- **Medida real:** 1.5 m de ancho · **Aspecto:** 1:1 · **Adjuntar:** `docs/referencias-ciudad/callejon-noche.png`, `docs/referencias-ciudad/tianguis-noche.png`

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy the scenes. Draw only the single object described, isolated on a plain flat light grey background. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Gritty Mexican cyberpunk city. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: railings, wires, tubes and brackets are thick and chunky. View: 3/4 view from the front, slightly from above unless stated. Subject: a pile of trash on the ground: several chunky black plastic garbage bags tied at the top, piled together with a few flattened and folded cardboard boxes and one crushed boxy can, all touching as one compact group sitting flat on the ground. Glossy black bags with soft highlights so the shapes read clearly. View: 3/4 view from slightly above.
```

**Tripo:** Image to 3D · Smart Mesh P2 · 8,000 caras · textura 1K · Delight apagado · sin rig → `N06_BolsasBasura.fbx`

**Claude Code:**
> Importa `N06_BolsasBasura.fbx` en `Assets/Arte/Calle/N06/`. Prefab `N06_BolsasBasura`: `Visual` de 1.5 m, pivote centro de la base, BoxCollider bajo. Junto a basureros, callejón y puestos. Material URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## N07 — Valla
- **Medida real:** 3 m de largo, ~1.1 m de alto · **Aspecto:** 16:9 · **Adjuntar:** `docs/referencias-ciudad/neon-alto-noche.png`, `docs/referencias-ciudad/cruce-dia-lluvia.png`

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy the scenes. Draw only the single object described, isolated on a plain flat light grey background. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Gritty Mexican cyberpunk city. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: railings, wires, tubes and brackets are thick and chunky. View: 3/4 view from the front, slightly from above unless stated. Subject: a section of sturdy metal sidewalk fence: two thick dark charcoal square posts on small base plates, a thick top rail and bottom rail, and a few thick vertical bars between them, with a thin glowing teal accent strip along the top rail, scratches. Both ends end in posts so sections line up. View: 3/4 view from the front left, slightly from above.
```

**Tripo:** Image to 3D · HD · 8,000 caras · textura 1K · Delight apagado · sin rig → `N07_Valla.fbx`

**Claude Code:**
> Importa `N07_Valla.fbx` en `Assets/Arte/Calle/N07/`. Prefab `N07_Valla`: `Visual` de 3 m de largo, pivote centro de la base, BoxCollider delgado, emisión de la franja turquesa. Encadenada en banquetas del distrito rico. Material URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision`. Captura de noche, commit y push en tu rama.

## N08 — AnuncioPoste
- **Medida real:** pantalla 1 × 3 m sobre poste (total ~5 m) · **Aspecto:** 9:16 · **Adjuntar:** `docs/referencias-ciudad/neon-alto-noche.png`, `docs/referencias-ciudad/tianguis-noche.png`

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy the scenes. Draw only the single object described, isolated on a plain flat light grey background. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Gritty Mexican cyberpunk city. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no logos, no people. Nothing thin or dangling: railings, wires, tubes and brackets are thick and chunky. View: 3/4 view from the front, slightly from above unless stated. Subject: a vertical advertising sign on a pole: a thick dark gunmetal pole on a bolted base plate holding a tall vertical frame with a flat smooth dark screen (OFF, plain dark, no image), the frame edged with a glowing magenta light strip, a boxy control unit on the pole with a teal light. View: 3/4 view from the front left, slightly from below, the whole sign inside the image.
```

**Tripo:** Image to 3D · Smart Mesh P2 · 8,000 caras · textura 2K · Delight apagado · sin rig → `N08_AnuncioPoste.fbx`

**Claude Code:**
> Importa `N08_AnuncioPoste.fbx` en `Assets/Arte/Calle/N08/`. Prefab `N08_AnuncioPoste`: `Visual` con la pantalla de 1 × 3 m (total ~5 m), pivote centro de la base, CapsuleCollider del poste, quad `Pantalla` con `PantallaAnuncio`. Material URP Lit (Smoothness 0.2–0.4, Metallic 0) con `MascaraEmision`. Captura de noche, commit y push en tu rama.


---

# Parte 10 — Personajes de la demo (PJ01–PJ12)
Lote: `imageassets/lote_personajes.json`. Fase 1 = 12 frentes; fase 2 = 12 espaldas (adjuntando su frente). Salida: `imageassets/personajes/<Nombre>_frente.png` y `_espalda.png`.
Profes (Doc, Inventor, Mecánico, Hacker): fotos en `imageassets/personajes/profes/` + estilo de `referencias/man.png`; se respetan sus facciones y se transforman según su rol. NPC base, Skulls y robots: estilo sencillo y ropa pegada.

## Tripo (igual para todos)
Multiview to 3D: **frente en Front, espalda en Back** · **Smart Mesh P2.0** en **quads** · **20,000 caras** (15–25k) · textura 2K (4K para el jefe y los profes si se ven de cerca) · Delight apagado · **Rig: humanoide (biped), Rigging v3.0, esqueleto Unity/Mixamo**, sin animación · exportar FBX con rig y descargar el zip como `imageassets/modelos3d/personajes/<Nombre>.zip`.
Si el rig de Tripo falla: exportar sin rig y subir el FBX a Mixamo para que él ponga el esqueleto (pose T).

| Código | Personaje | Altura real | Tipo | Referencias del frente |
|---|---|---|---|---|
| PJ01 | BaseHombre | 1.80 m | npc | `man.png`, `woman2.png` |
| PJ02 | BaseMujer | 1.68 m | npc | `woman.png`, `woman2.png` |
| PJ03 | JefeSkulls | 1.95 m | villano | `man.png` |
| PJ04 | SkullHombre | 1.80 m | villano | `man.png` |
| ~~PJ05~~ | SkullMujer (cancelada) | — | — | — |
| PJ06 | Doc | 1.75 m | profe | `joel-doc.png`, `man.png` |
| PJ07 | Inventor | 1.72 m | profe | `inventor-justino.png`, `man.png` |
| PJ08 | Mecanico | 1.75 m | profe | `canepa-mecanico.png`, `canepa2-mecanico.png`, `man.png` |
| PJ09 | Hacker | 1.75 m | profe | `sergio-hacker.jpg`, `man.png` |
| PJ10 | RobotHombre | 1.85 m | robot | `man.png`, `woman2.png` |
| PJ11 | RobotMujer | 1.85 m | robot | `woman.png`, `woman2.png` |
| PJ12 | GuardiaNova | 1.85 m | robot | `man.png` |

## PJ01-frente — BaseHombre_frente
- **Altura:** 1.80 m · **Adjuntar:** `imageassets/personajes/referencias/man.png`, `imageassets/personajes/referencias/woman2.png`

```
Match the art style of the attached stylized game character reference images: stylized 3D rendered game character, chunky simplified shapes, soft hand-painted gradients, soft 3D shading, slightly exaggerated proportions, no photorealism, no thick black ink outlines. Do not copy their faces, hair or clothes. Character: a generic young adult Mexican man, base NPC character: medium brown skin, short neat black hair, neutral friendly face, average build. He wears a plain fitted light grey t-shirt with no print, plain fitted light grey jeans and simple light grey sneakers; all clothing in neutral light grey so it can be recolored later. No accessories. Rig-friendly design: clothes fitted close to the body, a single top garment, nothing hanging loose: no open jackets, no capes, no loose hoods, no long loose hair, no dangling chains, cords or straps. Small details like chains, logos, tattoos and patterns are painted flat on the texture. Pose: strict T-pose, arms straight out horizontally to the sides, palms facing down, fingers straight and slightly apart, legs straight with feet shoulder-width apart, standing straight, looking forward, perfectly symmetrical. Straight front orthographic view, full body visible from head to feet with empty space around, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## PJ02-frente — BaseMujer_frente
- **Altura:** 1.68 m · **Adjuntar:** `imageassets/personajes/referencias/woman.png`, `imageassets/personajes/referencias/woman2.png`

```
Match the art style of the attached stylized game character reference images: stylized 3D rendered game character, chunky simplified shapes, soft hand-painted gradients, soft 3D shading, slightly exaggerated proportions, no photorealism, no thick black ink outlines. Do not copy their faces, hair or clothes. Character: a generic young adult Mexican woman, base NPC character: medium brown skin, black hair pulled back in a tight bun, neutral friendly face, average slim build. She wears a plain fitted light grey t-shirt with no print, plain fitted light grey pants and simple light grey sneakers; all clothing in neutral light grey so it can be recolored later. No accessories. Rig-friendly design: clothes fitted close to the body, a single top garment, nothing hanging loose: no open jackets, no capes, no loose hoods, no long loose hair, no dangling chains, cords or straps. Small details like chains, logos, tattoos and patterns are painted flat on the texture. Pose: strict T-pose, arms straight out horizontally to the sides, palms facing down, fingers straight and slightly apart, legs straight with feet shoulder-width apart, standing straight, looking forward, perfectly symmetrical. Straight front orthographic view, full body visible from head to feet with empty space around, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## PJ03-frente — JefeSkulls_frente
- **Altura:** 1.95 m · **Adjuntar:** `imageassets/personajes/referencias/man.png`

```
Match the art style of the attached stylized game character reference images: stylized 3D rendered game character, chunky simplified shapes, soft hand-painted gradients, soft 3D shading, slightly exaggerated proportions, no photorealism, no thick black ink outlines. Do not copy their faces, hair or clothes. Character: the tall, broad-shouldered, imposing leader of the SKULLS street gang in a Mexican cyberpunk city: an elegant fitted black catrin suit with a short closed buttoned jacket (no coat tails), black shirt and a thin magenta tie, a black wide-brimmed hat fitted tight to the head, his whole face painted on the skin like a Day of the Dead sugar skull in white and black with magenta details around the eyes, black leather gloves, polished black boots, thin glowing magenta lines painted along the suit seams, a silver skull ring. Proud, menacing expression. Rig-friendly design: clothes fitted close to the body, a single top garment, nothing hanging loose: no open jackets, no capes, no loose hoods, no long loose hair, no dangling chains, cords or straps. Small details like chains, logos, tattoos and patterns are painted flat on the texture. Pose: strict T-pose, arms straight out horizontally to the sides, palms facing down, fingers straight and slightly apart, legs straight with feet shoulder-width apart, standing straight, looking forward, perfectly symmetrical. Straight front orthographic view, full body visible from head to feet with empty space around, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## PJ04-frente — SkullHombre_frente
- **Altura:** 1.80 m · **Adjuntar:** `imageassets/personajes/referencias/man.png`

```
Match the art style of the attached stylized game character reference images: stylized 3D rendered game character, chunky simplified shapes, soft hand-painted gradients, soft 3D shading, slightly exaggerated proportions, no photorealism, no thick black ink outlines. Do not copy their faces, hair or clothes. Character: a SKULLS street gang member, lean young man: light brown skin, shaved head with a skull pattern painted on the scalp, the lower half of his face painted like sugar skull teeth in white and black, a fitted black sleeveless top with a magenta skull graphic painted on it, fitted black cargo pants with magenta stitching, heavy black boots, black fingerless gloves, a magenta bandana tied tight around one upper arm. Tough, aggressive expression. Rig-friendly design: clothes fitted close to the body, a single top garment, nothing hanging loose: no open jackets, no capes, no loose hoods, no long loose hair, no dangling chains, cords or straps. Small details like chains, logos, tattoos and patterns are painted flat on the texture. Pose: strict T-pose, arms straight out horizontally to the sides, palms facing down, fingers straight and slightly apart, legs straight with feet shoulder-width apart, standing straight, looking forward, perfectly symmetrical. Straight front orthographic view, full body visible from head to feet with empty space around, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## PJ05-frente — SkullMujer_frente
- **Altura:** 1.68 m · **Adjuntar:** `imageassets/personajes/referencias/woman.png`

```
Match the art style of the attached stylized game character reference images: stylized 3D rendered game character, chunky simplified shapes, soft hand-painted gradients, soft 3D shading, slightly exaggerated proportions, no photorealism, no thick black ink outlines. Do not copy their faces, hair or clothes. Character: a SKULLS street gang member, young woman: medium brown skin, black hair in a tight braid pinned up around her head (not hanging), sugar skull face paint around the eyes in white and black with magenta details, a fitted black long-sleeve top with a magenta skull print painted on it, fitted black pants with a magenta side stripe, heavy black boots, black fingerless gloves. Fierce, confident expression. Rig-friendly design: clothes fitted close to the body, a single top garment, nothing hanging loose: no open jackets, no capes, no loose hoods, no long loose hair, no dangling chains, cords or straps. Small details like chains, logos, tattoos and patterns are painted flat on the texture. Pose: strict T-pose, arms straight out horizontally to the sides, palms facing down, fingers straight and slightly apart, legs straight with feet shoulder-width apart, standing straight, looking forward, perfectly symmetrical. Straight front orthographic view, full body visible from head to feet with empty space around, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## PJ06-frente — Doc_frente
- **Altura:** 1.75 m · **Adjuntar:** `imageassets/personajes/profes/joel-doc.png`, `imageassets/personajes/referencias/man.png`

```
The first attached image(s) are photos of a real person: use them ONLY to copy his facial features, face shape, hairline, facial hair, skin tone and body build so he is clearly recognizable, transformed into a stylized cyberpunk game character. Do not copy his clothes, the background or anything written on his clothes. The last attached image is only the art style reference. Match the art style of the attached stylized game character reference images: stylized 3D rendered game character, chunky simplified shapes, soft hand-painted gradients, soft 3D shading, slightly exaggerated proportions, no photorealism, no thick black ink outlines. Do not copy their faces, hair or clothes. Character: the kind neighborhood doctor, a man in his fifties with the same face as the person in the photo (wavy dark messy hair, short greying beard and mustache, slim build, medium brown skin). Cyberpunk doctor styling: a short white-and-teal lab coat fully closed and buttoned, dark fitted pants, simple dark shoes, round glasses with glowing teal lenses, teal medical gloves, a small boxy medical scanner clipped tight to his belt, a teal cross symbol painted on the chest. Warm, kind smile. Accessories are allowed but every accessory is fixed tight to the body or head, nothing dangling or flapping, no long loose hair, no open coat flaps; small details are painted flat on the texture. Pose: strict T-pose, arms straight out horizontally to the sides, palms facing down, fingers straight and slightly apart, legs straight with feet shoulder-width apart, standing straight, looking forward, perfectly symmetrical. Straight front orthographic view, full body visible from head to feet with empty space around, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## PJ07-frente — Inventor_frente
- **Altura:** 1.72 m · **Adjuntar:** `imageassets/personajes/profes/inventor-justino.png`, `imageassets/personajes/referencias/man.png`

```
The first attached image(s) are photos of a real person: use them ONLY to copy his facial features, face shape, hairline, facial hair, skin tone and body build so he is clearly recognizable, transformed into a stylized cyberpunk game character. Do not copy his clothes, the background or anything written on his clothes. The last attached image is only the art style reference. Match the art style of the attached stylized game character reference images: stylized 3D rendered game character, chunky simplified shapes, soft hand-painted gradients, soft 3D shading, slightly exaggerated proportions, no photorealism, no thick black ink outlines. Do not copy their faces, hair or clothes. Character: the eccentric inventor, an older man in his sixties with the same face as the person in the photo (mostly bald on top with short grey hair on the sides, glasses, grey mustache, heavy build with a round belly, light brown skin). Cyberpunk inventor styling: a closed grey-blue work overall with a short brown leather apron, brass welding goggles pushed up on his forehead above his glasses, his right arm entirely a chunky iron robotic arm with pistons, rivets and a glowing amber joint from shoulder to hand, small tool pouches tight on the belt. Clever, cheerful expression. Accessories are allowed but every accessory is fixed tight to the body or head, nothing dangling or flapping, no long loose hair, no open coat flaps; small details are painted flat on the texture. Pose: strict T-pose, arms straight out horizontally to the sides, palms facing down, fingers straight and slightly apart, legs straight with feet shoulder-width apart, standing straight, looking forward, perfectly symmetrical. Straight front orthographic view, full body visible from head to feet with empty space around, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## PJ08-frente — Mecanico_frente
- **Altura:** 1.75 m · **Adjuntar:** `imageassets/personajes/profes/canepa-mecanico.png`, `imageassets/personajes/profes/canepa2-mecanico.png`, `imageassets/personajes/referencias/man.png`

```
The first attached image(s) are photos of a real person: use them ONLY to copy his facial features, face shape, hairline, facial hair, skin tone and body build so he is clearly recognizable, transformed into a stylized cyberpunk game character. Do not copy his clothes, the background or anything written on his clothes. The last attached image is only the art style reference. Match the art style of the attached stylized game character reference images: stylized 3D rendered game character, chunky simplified shapes, soft hand-painted gradients, soft 3D shading, slightly exaggerated proportions, no photorealism, no thick black ink outlines. Do not copy their faces, hair or clothes. Character: the neighborhood mechanic, a man in his thirties with the same face as the person in the photos (messy short black hair, full dark beard, round face, wide warm smile, light-medium brown skin, sturdy build). Cyberpunk mechanic styling: a fitted dark green work shirt with the sleeves rolled to the elbows, grey cargo work pants, heavy work boots, a black cap worn backwards, grease smudges, a tool belt tight on the waist with a few chunky wrenches, a chunky cyber gauntlet on his left forearm with a small glowing teal screen. Friendly, confident expression. Accessories are allowed but every accessory is fixed tight to the body or head, nothing dangling or flapping, no long loose hair, no open coat flaps; small details are painted flat on the texture. Pose: strict T-pose, arms straight out horizontally to the sides, palms facing down, fingers straight and slightly apart, legs straight with feet shoulder-width apart, standing straight, looking forward, perfectly symmetrical. Straight front orthographic view, full body visible from head to feet with empty space around, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## PJ09-frente — Hacker_frente
- **Altura:** 1.75 m · **Adjuntar:** `imageassets/personajes/profes/sergio-hacker.jpg`, `imageassets/personajes/referencias/man.png`

```
The first attached image(s) are photos of a real person: use them ONLY to copy his facial features, face shape, hairline, facial hair, skin tone and body build so he is clearly recognizable, transformed into a stylized cyberpunk game character. Do not copy his clothes, the background or anything written on his clothes. The last attached image is only the art style reference. Match the art style of the attached stylized game character reference images: stylized 3D rendered game character, chunky simplified shapes, soft hand-painted gradients, soft 3D shading, slightly exaggerated proportions, no photorealism, no thick black ink outlines. Do not copy their faces, hair or clothes. Character: the hacker, a young man in his twenties with the same face as the person in the photo (short dark hair, rectangular glasses, clean-shaven, light-medium brown skin, slim build). Cyberpunk hacker styling: a fitted dark charcoal hoodie zipped closed with the hood UP and snug around his head, glowing teal circuit lines painted on the hoodie, chunky headphones over the hood, a slim glowing teal AR visor over his glasses, short thick data cables plugged into the back of his neck and fixed tight along the collar, fitted black pants, black sneakers, black fingerless gloves. Focused, slightly smug expression. Accessories are allowed but every accessory is fixed tight to the body or head, nothing dangling or flapping, no long loose hair, no open coat flaps; small details are painted flat on the texture. Pose: strict T-pose, arms straight out horizontally to the sides, palms facing down, fingers straight and slightly apart, legs straight with feet shoulder-width apart, standing straight, looking forward, perfectly symmetrical. Straight front orthographic view, full body visible from head to feet with empty space around, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## PJ10-frente — RobotHombre_frente
- **Altura:** 1.85 m · **Adjuntar:** `imageassets/personajes/referencias/man.png`, `imageassets/personajes/referencias/woman2.png`

```
Match the art style of the attached stylized game character reference images: stylized 3D rendered game character, chunky simplified shapes, soft hand-painted gradients, soft 3D shading, slightly exaggerated proportions, no photorealism, no thick black ink outlines. Do not copy their faces, hair or clothes. Character: a humanoid service android with male body proportions: a body of smooth white and light grey armor plates with visible dark grey mechanical joints at the neck, shoulders, elbows, wrists, hips and knees, thin glowing teal light lines along the limbs and chest, and a simple head with a smooth black face screen showing two glowing teal dot eyes. Clean, friendly, no clothes, no weapons. Rig-friendly design: clothes fitted close to the body, a single top garment, nothing hanging loose: no open jackets, no capes, no loose hoods, no long loose hair, no dangling chains, cords or straps. Small details like chains, logos, tattoos and patterns are painted flat on the texture. Pose: strict T-pose, arms straight out horizontally to the sides, palms facing down, fingers straight and slightly apart, legs straight with feet shoulder-width apart, standing straight, looking forward, perfectly symmetrical. Straight front orthographic view, full body visible from head to feet with empty space around, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## PJ11-frente — RobotMujer_frente
- **Altura:** 1.85 m · **Adjuntar:** `imageassets/personajes/referencias/woman.png`, `imageassets/personajes/referencias/woman2.png`

```
Match the art style of the attached stylized game character reference images: stylized 3D rendered game character, chunky simplified shapes, soft hand-painted gradients, soft 3D shading, slightly exaggerated proportions, no photorealism, no thick black ink outlines. Do not copy their faces, hair or clothes. Character: a humanoid service android with female body proportions, slimmer than a male android: a body of smooth white and light grey armor plates with visible dark grey mechanical joints at the neck, shoulders, elbows, wrists, hips and knees, thin glowing teal light lines along the limbs and chest, and a simple head with a smooth white mask face with two glowing teal eye slits. Clean, elegant, no clothes, no weapons. Rig-friendly design: clothes fitted close to the body, a single top garment, nothing hanging loose: no open jackets, no capes, no loose hoods, no long loose hair, no dangling chains, cords or straps. Small details like chains, logos, tattoos and patterns are painted flat on the texture. Pose: strict T-pose, arms straight out horizontally to the sides, palms facing down, fingers straight and slightly apart, legs straight with feet shoulder-width apart, standing straight, looking forward, perfectly symmetrical. Straight front orthographic view, full body visible from head to feet with empty space around, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## PJ12-frente — GuardiaNova_frente
- **Altura:** 1.85 m · **Adjuntar:** `imageassets/personajes/referencias/man.png`

```
Match the art style of the attached stylized game character reference images: stylized 3D rendered game character, chunky simplified shapes, soft hand-painted gradients, soft 3D shading, slightly exaggerated proportions, no photorealism, no thick black ink outlines. Do not copy their faces, hair or clothes. Character: an armored security robot of a cold villain megacorporation: bulky white and black armor plates, heavy rounded shoulders, thick armored chest and limbs, black mechanical joints, small cold white light strips on the armor, a helmet-like head with a single horizontal glowing red visor slit, empty open hands. Intimidating and sterile, no weapons, no text, no logos. Rig-friendly design: clothes fitted close to the body, a single top garment, nothing hanging loose: no open jackets, no capes, no loose hoods, no long loose hair, no dangling chains, cords or straps. Small details like chains, logos, tattoos and patterns are painted flat on the texture. Pose: strict T-pose, arms straight out horizontally to the sides, palms facing down, fingers straight and slightly apart, legs straight with feet shoulder-width apart, standing straight, looking forward, perfectly symmetrical. Straight front orthographic view, full body visible from head to feet with empty space around, plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## Espalda (fase 2, igual para todos; adjuntar `<Nombre>_frente.png`)
```
Use the attached image as the exact reference. Draw the exact same character, identical body proportions, face shape, skin, hair, clothes, colors, materials, accessories and art style, now seen from directly BEHIND: strict straight back orthographic view, camera at chest height, zero perspective, the same T-pose with arms straight out horizontally, palms down, fingers apart, feet shoulder-width apart, perfectly symmetrical. Same height and same size in the frame as the reference, full body visible from head to feet. The face is not visible. Plain flat light grey background, soft even studio lighting, no cast shadow, no text, no letters, no logos, no other people.
```

## Claude Code (cuando estén los zips)
> Para cada zip de `imageassets/modelos3d/personajes/`: importa el FBX en `Assets/Arte/Personajes/<Nombre>/` como **Humanoid** (revisa el Avatar y corrige el mapeo si hace falta), escala a su altura real (tabla de la Parte 10 de PROMPTS_ASSETS.md), material URP Lit con `MascaraEmision` para luces, y asigna el Animator con idle/caminar de Mixamo para probar. BaseHombre/BaseMujer: crea variantes de color de ropa con tinte (MaterialPropertyBlock) para peatones. Robots: emisión turquesa (guardia: roja). Captura de cada personaje en pose idle de frente y espalda en docs/capturas, commit y push en tu rama.


# Parte 11 — Carros básicos para la demo (V02–V04)

Lote: `lote_carros.json` · salida `imageassets/carros/` · referencia de estilo `V01a_CarroViejito.png` · 16:9 · edit.
Tripo: Smart Mesh P2 · 20,000 caras · textura 2K. Ruedas: si Tripo las deja pegadas, se quedan las ruedas greybox como en V01a.

## V02 — V02_SedanViejo
- **Medida:** 4.3 m largo · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `V01a_CarroViejito.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, proportions of the wheels and level of wear; do not copy its car shape. Draw only the single vehicle described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Poor gritty Mexican cyberpunk slum car: a cheap old car kept alive with mismatched replacement panels, riveted patches, rust streaks and scratches, with a few cheap futuristic add-ons bolted on. Wheels are big, chunky and clearly separated from the body with visible dark wheel arches. Windows are dark tinted solid glass, no interior visible. No thin parts: antennas, mirrors and bars are thick and chunky. Single vehicle, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no license plate text, no logos, no people. View: 3/4 view from the front left, slightly from above. Subject: a boxy old 4-door compact sedan from the 1990s with a short square trunk and flat hood, faded pale blue paint with one rear door in primer grey and the trunk lid in dull white, a dented bumper held with a thick strap, a thin glowing teal LED strip replacing one broken headlight while the other is an old round yellowish lamp, a chunky boxy battery pack bolted on the roof rack, small amber light on the roof pack, rust along the bottom edges.
```

## V03 — V03_PickupTrabajo
- **Medida:** 4.8 m largo · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `V01a_CarroViejito.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, proportions of the wheels and level of wear; do not copy its car shape. Draw only the single vehicle described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Poor gritty Mexican cyberpunk slum car: a cheap old car kept alive with mismatched replacement panels, riveted patches, rust streaks and scratches, with a few cheap futuristic add-ons bolted on. Wheels are big, chunky and clearly separated from the body with visible dark wheel arches. Windows are dark tinted solid glass, no interior visible. No thin parts: antennas, mirrors and bars are thick and chunky. Single vehicle, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no license plate text, no logos, no people. View: 3/4 view from the front left, slightly from above. Subject: a small old 2-door work pickup truck with a short cab and an open rear cargo bed, faded dull green paint with a mismatched rust-orange front fender and a sun-bleached hood, a chunky black tube bull bar, the cargo bed holds two plastic crates and a tied tarp bundle shaped as simple blocks, a thick boxy glowing magenta light bar on the cab roof, a chunky side exhaust pipe, mud and rust on the lower sides.
```

## V04 — V04_CombiVieja
- **Medida:** 4.6 m largo · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `V01a_CarroViejito.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, proportions of the wheels and level of wear; do not copy its car shape. Draw only the single vehicle described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Poor gritty Mexican cyberpunk slum car: a cheap old car kept alive with mismatched replacement panels, riveted patches, rust streaks and scratches, with a few cheap futuristic add-ons bolted on. Wheels are big, chunky and clearly separated from the body with visible dark wheel arches. Windows are dark tinted solid glass, no interior visible. No thin parts: antennas, mirrors and bars are thick and chunky. Single vehicle, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no license plate text, no logos, no people. View: 3/4 view from the front left, slightly from above. Subject: an old rounded boxy minivan like a classic cheap passenger van with a flat front, sliding side door and small windows, two-tone paint of faded cream on top and worn dull red on the bottom with a primer grey replacement front panel, a big cheap roof rack holding a chunky square cargo box, a glowing amber LED strip across the front grille, a hand-painted stripe shown as a plain teal band along the side (no text), rust spots and dents.
```

### Parte 11b — Patrullas y motos (V05–V08)

## V05 — V05_PatrullaSedan
- **Medida:** 4.6 m largo · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `V01a_CarroViejito.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, proportions of the wheels and level of wear; do not copy its car shape. Draw only the single vehicle described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Poor gritty Mexican cyberpunk slum car: a cheap old car kept alive with mismatched replacement panels, riveted patches, rust streaks and scratches, with a few cheap futuristic add-ons bolted on. Wheels are big, chunky and clearly separated from the body with visible dark wheel arches. Windows are dark tinted solid glass, no interior visible. No thin parts: antennas, mirrors and bars are thick and chunky. Single vehicle, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no license plate text, no logos, no people. View: 3/4 view from the front left, slightly from above. Subject: Police patrol vehicle of a fictional cyberpunk Mexican city police: better maintained than the slum cars, only light wear and a few scratches, two-tone livery of dark navy blue and off-white, a thick chunky glowing light bar with red and blue blocks, a thick push bumper, NO text, NO words, NO numbers, NO badges or emblems: the doors carry only a plain diagonal teal stripe and a plain white blank shield shape. a boxy mid-size 4-door police sedan with a straight hood and short trunk, the navy and off-white livery, roof light bar, a chunky front push bumper with two small spotlights, a short thick antenna, a small boxy cyber scanner pod on the roof next to the light bar glowing teal.
```

## V06 — V06_PatrullaCamioneta
- **Medida:** 5.0 m largo · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `V01a_CarroViejito.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, proportions of the wheels and level of wear; do not copy its car shape. Draw only the single vehicle described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Poor gritty Mexican cyberpunk slum car: a cheap old car kept alive with mismatched replacement panels, riveted patches, rust streaks and scratches, with a few cheap futuristic add-ons bolted on. Wheels are big, chunky and clearly separated from the body with visible dark wheel arches. Windows are dark tinted solid glass, no interior visible. No thin parts: antennas, mirrors and bars are thick and chunky. Single vehicle, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no license plate text, no logos, no people. View: 3/4 view from the front left, slightly from above. Subject: Police patrol vehicle of a fictional cyberpunk Mexican city police: better maintained than the slum cars, only light wear and a few scratches, two-tone livery of dark navy blue and off-white, a thick chunky glowing light bar with red and blue blocks, a thick push bumper, NO text, NO words, NO numbers, NO badges or emblems: the doors carry only a plain diagonal teal stripe and a plain white blank shield shape. a big boxy 4-door police pickup truck with a tall cab and an open rear bed with a thick roll bar, the navy and off-white livery, a long light bar on the cab roof, a chunky front push bumper, big off-road tires, two boxy equipment lockers in the bed, a teal glowing strip under the roll bar.
```

## V07 — V07_MotoReparto
- **Medida:** 1.9 m largo · **Aspecto:** 4:3 · **Endpoint:** `edit` · **Adjuntar:** `V01a_CarroViejito.png`, `C19_Moto_v1.png`

```
Use the attached reference images ONLY for the art style, rendering, colors, proportions and level of wear; do not copy its shape. Draw only the single motorcycle described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Poor gritty Mexican cyberpunk slum car: a cheap old motorcycle kept alive with mismatched replacement panels, riveted patches, rust streaks and scratches, with a few cheap futuristic add-ons bolted on. Wheels are big, chunky and clearly separated from the body. No thin parts: antennas, mirrors and bars are thick and chunky. Single vehicle, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no license plate text, no logos, no people. View: 3/4 view from the front left, slightly from above. Subject: a cheap small delivery motorcycle (underbone scooter style) used by street food couriers, original design that does not resemble any real brand, faded red plastic body panels with one replacement panel in primer grey, a big boxy insulated delivery box on a rear rack with a plain amber stripe and no text, a round old headlight plus a thin teal LED strip, thick chunky wheels, a small boxy battery bolted under the seat, standing on its side kickstand.
```

## V08 — V08_MotoPatrulla
- **Medida:** 2.2 m largo · **Aspecto:** 4:3 · **Endpoint:** `edit` · **Adjuntar:** `V01a_CarroViejito.png`, `C19_Moto_v1.png`

```
Use the attached reference images ONLY for the art style, rendering, colors, proportions and level of wear; do not copy its shape. Draw only the single motorcycle described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Wheels are big, chunky and clearly separated from the body. No thin parts: antennas, mirrors and bars are thick and chunky. Single vehicle, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, no license plate text, no logos, no people. View: 3/4 view from the front left, slightly from above. Subject: Police patrol vehicle of a fictional cyberpunk Mexican city police: better maintained than the slum cars, only light wear and a few scratches, two-tone livery of dark navy blue and off-white, a thick chunky glowing light bar with red and blue blocks, a thick push bumper, NO text, NO words, NO numbers, NO badges or emblems: the doors carry only a plain diagonal teal stripe and a plain white blank shield shape. a big heavy police patrol motorcycle, original design that does not resemble any real brand, chunky faceted fairing in the navy and off-white livery, a windshield of dark tinted solid glass, red and blue light blocks on the front and on two short thick rear poles, two boxy side cases with plain white blank shield shapes, thick chunky wheels with glowing teal rims, standing on its side kickstand.
```


# Parte 12 — Fondos de horizonte (matte painting)

Lote: `lote_fondos.json` · salida `imageassets/fondos/` · 21:9 · 4k (si no lo acepta, 2k) · edit.

## fondo-suburbios
- **Aspecto:** 21:9 · **Endpoint:** `edit` · **Adjuntar:** `calle-suburbios-noche.png`, `azotea-vista-ciudad.png`

```
Use the attached reference images ONLY for the art style, rendering, color mood and lighting; do not copy their scenes or camera angles. Stylized painted 3D game look matching the references: simplified chunky shapes, soft hand-painted gradients, no photorealistic noise. This is a distant skyline matte painting for a video game horizon backdrop. Show ONLY the far-away horizon: no foreground, no street, no nearby rooftops, no characters, no vehicles close to the camera. Camera at eye level looking straight at the horizon. The horizon line sits at one third of the image height from the bottom. The bottom third is filled edge to edge with dark building silhouettes that fade into atmospheric haze toward the horizon, with no empty gaps at the left or right edges. The upper two thirds are a smooth, plain, dark blue night sky with only a few faint soft clouds and an even gradient, getting slightly lighter near the horizon, so it blends seamlessly with a game skybox. No moon, no stars clusters, no lens flare. No text, no letters, no numbers, no logos, no readable signs anywhere; screens and signs show only abstract shapes and color. Wide panoramic composition. Subject: kilometers of a poor Mexican cyberpunk neighborhood seen from far away: an endless sea of low houses and small concrete buildings with water tanks and antennas on their roofs drawn as small silhouettes, thousands of tiny warm orange lit windows, rows of street lamps with orange light, warm orange haze rising from the streets, one or two distant magenta glowing signs, the city stretching to the edges of the image.
```

## fondo-neon-alto
- **Aspecto:** 21:9 · **Endpoint:** `edit` · **Adjuntar:** `neon-alto-noche.png`, `azotea-vista-ciudad.png`

```
Use the attached reference images ONLY for the art style, rendering, color mood and lighting; do not copy their scenes or camera angles. Stylized painted 3D game look matching the references: simplified chunky shapes, soft hand-painted gradients, no photorealistic noise. This is a distant skyline matte painting for a video game horizon backdrop. Show ONLY the far-away horizon: no foreground, no street, no nearby rooftops, no characters, no vehicles close to the camera. Camera at eye level looking straight at the horizon. The horizon line sits at one third of the image height from the bottom. The bottom third is filled edge to edge with dark building silhouettes that fade into atmospheric haze toward the horizon, with no empty gaps at the left or right edges. The upper two thirds are a smooth, plain, dark blue night sky with only a few faint soft clouds and an even gradient, getting slightly lighter near the horizon, so it blends seamlessly with a game skybox. No moon, no stars clusters, no lens flare. No text, no letters, no numbers, no logos, no readable signs anywhere; screens and signs show only abstract shapes and color. Wide panoramic composition. Subject: the rich downtown district seen from far away: tall tightly packed cyberpunk skyscrapers with many lit windows, a few giant glowing screens in acid green and magenta showing abstract shapes, small blinking red lights on top of the antennas, a long elevated highway crossing the lower part with lines of white and red car lights, acid green haze around the base of the towers.
```

## fondo-nova
- **Aspecto:** 21:9 · **Endpoint:** `edit` · **Adjuntar:** `refineria-nova-noche.png`, `neon-alto-noche.png`

```
Use the attached reference images ONLY for the art style, rendering, color mood and lighting; do not copy their scenes or camera angles. Stylized painted 3D game look matching the references: simplified chunky shapes, soft hand-painted gradients, no photorealistic noise. This is a distant skyline matte painting for a video game horizon backdrop. Show ONLY the far-away horizon: no foreground, no street, no nearby rooftops, no characters, no vehicles close to the camera. Camera at eye level looking straight at the horizon. The horizon line sits at one third of the image height from the bottom. The bottom third is filled edge to edge with dark building silhouettes that fade into atmospheric haze toward the horizon, with no empty gaps at the left or right edges. The upper two thirds are a smooth, plain, dark blue night sky with only a few faint soft clouds and an even gradient, getting slightly lighter near the horizon, so it blends seamlessly with a game skybox. No moon, no stars clusters, no lens flare. No text, no letters, no numbers, no logos, no readable signs anywhere; screens and signs show only abstract shapes and color. Wide panoramic composition. Subject: a cold corporate zone seen from far away: one enormous clean white monolithic tower in the center, much taller than everything else, with a single vertical stripe of bright white light running up its face, lower boxy corporate buildings around it with cool white and pale blue windows, several searchlight beams pointing up into the sky from the base, cold blue haze around the buildings, a clean and sterile mood contrasting with the rest of the city.
```


# Parte 13 — Kit de torres v2, letreros 3D, interiores y pantallas (02-nov)

## Torres v2 (`lote_torres2.json`, `imageassets/torres/`) — fase 1: T21 y N09; fase 2: T20, T22, T23 (adjuntan T21)

### T21 — T21_PisoOficinas (fase 1)
- **Medida:** 12 × 12 m de planta · 4 m de alto · **Tripo:** Smart Mesh P2 · 10,000 caras · textura 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people. Nothing thin or dangling: antennas, railings, poles and cables are thick and chunky. Rich cyberpunk business district, clearly different from a poor neighborhood: clean, new and expensive-looking, sleek straight lines, dark charcoal and gunmetal grey structure, polished dark blue-black glass, small glowing acid green and magenta accents, thin teal light strips, almost no wear, no rust, no patches, no water tanks, no hanging laundry, no graffiti. Modular skyscraper section for a game kit, shaped as a box with a perfectly square footprint (equal width and depth, 12 by 12 meters). The top face and the bottom face are perfectly flat and horizontal so modules stack exactly on top of each other. Thick square corner pillars at all four corners. The window grid is a regular grid of 4 equal window columns per face, aligned with the corner pillars, so it lines up between stacked modules. Windows are flat smooth opaque dark blue-black glass panels: no reflections, no interiors, no curtains, no light behind them. Nothing protrudes more than a hand's width from the facade unless stated. No text, no letters, no numbers, no logos. Subject: a 4 meter tall office floor module of a corporate skyscraper: a full curtain wall of glass in a regular grid on all four faces, each window framed by thick charcoal grey mullions, a thin glowing teal horizontal light strip running along the top edge and the bottom edge of the floor all around the building. View: 3/4 view at 45 degrees showing the front face and the right face equally, camera at the height of the module, slight perspective.
```

### N09 — N09_PosteRico (fase 1)
- **Medida:** 8 m de alto · brazo de ~2 m · **Tripo:** HD · 8,000 caras · textura 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people. Nothing thin or dangling: antennas, railings, poles and cables are thick and chunky. Rich cyberpunk business district, clearly different from a poor neighborhood: clean, new and expensive-looking, sleek straight lines, dark charcoal and gunmetal grey structure, polished dark blue-black glass, small glowing acid green and magenta accents, thin teal light strips, almost no wear, no rust, no patches, no water tanks, no hanging laundry, no graffiti. No text, no letters, no numbers, no logos. Subject: a modern street lamp post of the rich district, 8 meters tall: a sleek straight square-section dark gunmetal pole, slender-looking but thick and solid enough for 3D (about the thickness of a forearm, never a thin wire), on a small square base plate with bolts, at the top a long horizontal arm with an elongated flat lamp head glowing cold white underneath, a thin glowing acid green stripe along the side of the lamp head and a small green ring near the base of the pole. View: 3/4 view from the front, whole post visible from base to top.
```

### T20 — T20_BaseTorre (fase 2)
- **Medida:** 12 × 12 m de planta · 6 m de alto · marquesina sale 3 m · **Tripo:** Smart Mesh P2 · 12,000 caras · textura 2K

```
The FIRST attached image is the exact reference for the footprint width and depth, corner pillars, window grid position and spacing, materials and colors: this module must stack perfectly with it. The other attached images are ONLY for art style. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people. Nothing thin or dangling: antennas, railings, poles and cables are thick and chunky. Rich cyberpunk business district, clearly different from a poor neighborhood: clean, new and expensive-looking, sleek straight lines, dark charcoal and gunmetal grey structure, polished dark blue-black glass, small glowing acid green and magenta accents, thin teal light strips, almost no wear, no rust, no patches, no water tanks, no hanging laundry, no graffiti. Modular skyscraper section for a game kit, shaped as a box with a perfectly square footprint (equal width and depth, 12 by 12 meters). The top face and the bottom face are perfectly flat and horizontal so modules stack exactly on top of each other. Thick square corner pillars at all four corners. The window grid is a regular grid of 4 equal window columns per face, aligned with the corner pillars, so it lines up between stacked modules. Windows are flat smooth opaque dark blue-black glass panels: no reflections, no interiors, no curtains, no light behind them. Nothing protrudes more than a hand's width from the facade unless stated. No text, no letters, no numbers, no logos. Subject: the 6 meter tall ground floor base of the same skyscraper: same footprint and corner pillars as the reference, extra thick square columns, the front face is a tall glass lobby entrance with double glass doors in the middle, a thick flat cantilevered entrance canopy over the doors that sticks out about 3 meters with downlights glowing cold white underneath, above the canopy a large blank flat sign panel (plain dark, empty, no text) with a thin teal edge light, a short dark stone plinth around the bottom, the other faces have the same window grid as the reference but taller. View: 3/4 view at 45 degrees showing the front face and the right face equally, camera at the height of the module, slight perspective.
```

### T22 — T22_PisoPantalla (fase 2)
- **Medida:** 12 × 12 m de planta · 4 m de alto · pantalla ~10 × 3 m · **Tripo:** Smart Mesh P2 · 10,000 caras · textura 2K

```
The FIRST attached image is the exact reference for the footprint width and depth, corner pillars, window grid position and spacing, materials and colors: this module must stack perfectly with it. The other attached images are ONLY for art style. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people. Nothing thin or dangling: antennas, railings, poles and cables are thick and chunky. Rich cyberpunk business district, clearly different from a poor neighborhood: clean, new and expensive-looking, sleek straight lines, dark charcoal and gunmetal grey structure, polished dark blue-black glass, small glowing acid green and magenta accents, thin teal light strips, almost no wear, no rust, no patches, no water tanks, no hanging laundry, no graffiti. Modular skyscraper section for a game kit, shaped as a box with a perfectly square footprint (equal width and depth, 12 by 12 meters). The top face and the bottom face are perfectly flat and horizontal so modules stack exactly on top of each other. Thick square corner pillars at all four corners. The window grid is a regular grid of 4 equal window columns per face, aligned with the corner pillars, so it lines up between stacked modules. Windows are flat smooth opaque dark blue-black glass panels: no reflections, no interiors, no curtains, no light behind them. Nothing protrudes more than a hand's width from the facade unless stated. No text, no letters, no numbers, no logos. Subject: exactly the same 4 meter office floor module as the reference, identical on three sides, but on the front face a large flat smooth screen covering most of the face, flush with the facade, with a thick gunmetal frame and a thin glowing magenta edge; the screen is OFF and plain matte black with no image, so a video can be placed on it later. View: 3/4 view at 45 degrees showing the front face and the right face equally, camera at the height of the module, slight perspective.
```

### T23 — T23_RemateTorre (fase 2)
- **Medida:** 12 × 12 m de planta · 5 m de cuerpo + antena ~6 m · **Tripo:** Smart Mesh P2 · 12,000 caras · textura 2K

```
The FIRST attached image is the exact reference for the footprint width and depth, corner pillars, window grid position and spacing, materials and colors: this module must stack perfectly with it. The other attached images are ONLY for art style. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people. Nothing thin or dangling: antennas, railings, poles and cables are thick and chunky. Rich cyberpunk business district, clearly different from a poor neighborhood: clean, new and expensive-looking, sleek straight lines, dark charcoal and gunmetal grey structure, polished dark blue-black glass, small glowing acid green and magenta accents, thin teal light strips, almost no wear, no rust, no patches, no water tanks, no hanging laundry, no graffiti. Modular skyscraper section for a game kit, shaped as a box with a perfectly square footprint (equal width and depth, 12 by 12 meters). The top face and the bottom face are perfectly flat and horizontal so modules stack exactly on top of each other. Thick square corner pillars at all four corners. The window grid is a regular grid of 4 equal window columns per face, aligned with the corner pillars, so it lines up between stacked modules. Windows are flat smooth opaque dark blue-black glass panels: no reflections, no interiors, no curtains, no light behind them. Nothing protrudes more than a hand's width from the facade unless stated. No text, no letters, no numbers, no logos. Subject: the 5 meter tall rooftop crown of the same skyscraper: same footprint and corner pillars as the reference, the walls are the same glass grid but only the bottom half, the top is a flat roof with a low solid parapet, on the roof: one thick tall communications antenna mast (very thick, chunky segments) with a glowing red light at the top, one squat cylindrical tank, three boxy air conditioning units with big round fans and a small boxy machine room, a thin teal light strip along the parapet. View: 3/4 view at 45 degrees from slightly above so the roof equipment is visible.
```

## Letreros 3D (`lote_letreros.json`, `imageassets/letreros/`)

### L10 — L10_LetreroVivaCoin
- **Medida:** 6 × 2.5 m · fondo 0.6 m · **Tripo:** HD · 12,000 caras · textura 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, readable from a distance. A single 3D sign object, centered, seen straight from the front with a very slight angle so its depth is visible, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no people, no wall behind it, no extra text other than the text requested. The letters are thick, raised, chunky 3D letters, very bold and simple, and must be spelled EXACTLY as written, letter by letter. Subject: a big flashy casino marquee sign: a wide rectangular box with a thick gold frame and magenta panels, rows of round warm white light bulbs all around the border, in the center the word "VIVACOIN" in huge thick gold letters with a magenta glow, a big round gold coin shape on the left of the word, a curved crown-like top edge.
```

### L11 — L11_LetreroHotel
- **Medida:** 1.2 × 5 m · fondo 0.4 m · **Tripo:** HD · 10,000 caras · textura 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, readable from a distance. A single 3D sign object, centered, seen straight from the front with a very slight angle so its depth is visible, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no people, no wall behind it, no extra text other than the text requested. The letters are thick, raised, chunky 3D letters, very bold and simple, and must be spelled EXACTLY as written, letter by letter. Subject: a tall vertical blade sign of a cheap city hotel: a narrow tall dark metal box with a thick frame and two thick mounting brackets on its back edge, the word "HOTEL" written vertically with one letter under the other (H, O, T, E, L), in glowing pink-magenta neon tube letters, small teal neon stars between the letters, a few bulbs at the top.
```

### L12 — L12_LetreroElMercado
- **Medida:** 3 × 1.2 m · fondo 0.2 m · **Tripo:** HD · 10,000 caras · textura 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, readable from a distance. A single 3D sign object, centered, seen straight from the front with a very slight angle so its depth is visible, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no people, no wall behind it, no extra text other than the text requested. The letters are thick, raised, chunky 3D letters, very bold and simple, and must be spelled EXACTLY as written, letter by letter. Subject: a rough, tough sign for a clandestine gun shop: a heavy rusted steel plate with welded edges and big bolts, hung by two thick short chains from a thick bar, the words "EL MERCADO" in thick stencil-style dark letters with a weak amber glow behind them, and a big flat silhouette of a pistol next to the text, glowing faintly in amber, scratches and rust.
```

### L13 — L13_LetreroIDI
- **Medida:** 1.8 × 1 m · fondo 0.3 m · **Tripo:** HD · 8,000 caras · textura 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, readable from a distance. A single 3D sign object, centered, seen straight from the front with a very slight angle so its depth is visible, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no people, no wall behind it, no extra text other than the text requested. The letters are thick, raised, chunky 3D letters, very bold and simple, and must be spelled EXACTLY as written, letter by letter. Subject: a sign for an underground street clinic: a rounded rectangular light box with a dark gunmetal frame, on the left a big medical cross in glowing teal made of a cross shape filled with circuit board lines, on the right the text "I+D+I" in thick white letters with a teal glow, small bolts and light wear.
```

### L14 — L14_LetreroCIC
- **Medida:** 2.5 × 1.2 m · fondo 0.3 m · **Tripo:** HD · 8,000 caras · textura 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, readable from a distance. A single 3D sign object, centered, seen straight from the front with a very slight angle so its depth is visible, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow, no people, no wall behind it, no extra text other than the text requested. The letters are thick, raised, chunky 3D letters, very bold and simple, and must be spelled EXACTLY as written, letter by letter. Subject: a sign for a neighborhood mechanic workshop: a sturdy rectangular steel sign with a thick orange and charcoal frame, on the left a big chunky gear shape glowing amber, on the right the letters "CIC" in huge thick white letters with an orange glow, a few rivets, light grime and scratches.
```

## Referencias de interiores (`lote_interiores.json`, `docs/referencias-interiores/`)

### habitacion

```
Use the attached reference images ONLY for the art style, rendering, colors and lighting; do not copy their scenes. Stylized low-poly 3D game environment render: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Interior concept for a Mexican cyberpunk video game, used as a layout guide to model the room and its props. Camera at eye height (1.6 m), wide view showing most of the room, clear readable layout with props well separated. No people, no characters, no animals. No readable text, no letters, no logos: signs and screens show only abstract shapes and colors. Subject: a small bedroom of a young man in a poor Mexican cyberpunk neighborhood: a single bed with a wrinkled blanket, a small nightstand with a lamp, a worn wooden wardrobe, a desk with an old chunky computer with a glowing screen and a cheap chair, a poster on the wall with abstract shapes, a window with magenta and teal neon light from the street coming in, a bare bulb hanging from the ceiling with warm light, cracked plaster walls painted a faded color, light mess: clothes on the chair, a backpack on the floor, cans on the desk. It is a bedroom, NOT a workshop: no tools, no workbench.
```

### casino-interior

```
Use the attached reference images ONLY for the art style, rendering, colors and lighting; do not copy their scenes. Stylized low-poly 3D game environment render: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Interior concept for a Mexican cyberpunk video game, used as a layout guide to model the room and its props. Camera at eye height (1.6 m), wide view showing most of the room, clear readable layout with props well separated. No people, no characters, no animals. No readable text, no letters, no logos: signs and screens show only abstract shapes and colors. Subject: inside a flashy cyberpunk casino: long rows of chunky slot machines with glowing screens showing abstract symbols, several green card tables with chairs, a long bar with stools and glowing bottles behind it, a loud patterned carpet in dark red and gold, gold and magenta lights everywhere, a ceiling with rows of lights and a big chandelier-like light fixture, empty of people.
```

### consultorio-doc

```
Use the attached reference images ONLY for the art style, rendering, colors and lighting; do not copy their scenes. Stylized low-poly 3D game environment render: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Interior concept for a Mexican cyberpunk video game, used as a layout guide to model the room and its props. Camera at eye height (1.6 m), wide view showing most of the room, clear readable layout with props well separated. No people, no characters, no animals. No readable text, no letters, no logos: signs and screens show only abstract shapes and colors. Subject: a clandestine underground street clinic: a padded medical bed in the center, a big robotic surgery arm hanging from the ceiling over the bed, several monitors on stands showing abstract graphs, metal shelves with medicine boxes, jars and cybernetic implants (robotic hands, eyes, chips) in glass cases, a small sink, tiled walls a bit dirty, cold teal lighting with a few warm spots, cables bundled thick along the walls.
```

### taller-tino

```
Use the attached reference images ONLY for the art style, rendering, colors and lighting; do not copy their scenes. Stylized low-poly 3D game environment render: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Interior concept for a Mexican cyberpunk video game, used as a layout guide to model the room and its props. Camera at eye height (1.6 m), wide view showing most of the room, clear readable layout with props well separated. No people, no characters, no animals. No readable text, no letters, no logos: signs and screens show only abstract shapes and colors. Subject: the workshop of an inventor: big wooden and metal workbenches covered with disassembled weapons, tools and parts, half-built drones on the tables and hanging from hooks, a pegboard full of tools, chalkboards and whiteboards covered with blueprint drawings of machines (abstract lines, no text), shelves with boxes of parts, a desk lamp, warm orange workshop lighting with a few teal accents.
```

### taller-mecanico

```
Use the attached reference images ONLY for the art style, rendering, colors and lighting; do not copy their scenes. Stylized low-poly 3D game environment render: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Interior concept for a Mexican cyberpunk video game, used as a layout guide to model the room and its props. Camera at eye height (1.6 m), wide view showing most of the room, clear readable layout with props well separated. No people, no characters, no animals. No readable text, no letters, no logos: signs and screens show only abstract shapes and colors. Subject: a neighborhood car mechanic garage: an old car lifted on a hydraulic jack in the middle, a stack of tires against the wall, a red tool cabinet and tools on the walls, an engine hoist crane holding an engine, oil stains on the concrete floor, a roll-up metal door half open to the street at night, warm work lights and a few neon accents.
```

### entrada-armas-mercado

```
Use the attached reference images ONLY for the art style, rendering, colors and lighting; do not copy their scenes. Stylized low-poly 3D game environment render: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Interior concept for a Mexican cyberpunk video game, used as a layout guide to model the room and its props. Camera at eye height (1.6 m), wide view showing most of the room, clear readable layout with props well separated. No people, no characters, no animals. No readable text, no letters, no logos: signs and screens show only abstract shapes and colors. Subject: the entrance of a clandestine gun shop hidden inside a market: a heavy metal security grille gate open to the side, behind it a counter with glass display cases full of pistols and rifles, more rifles on wall racks, stacks of ammunition boxes and crates, a cash register, dim amber light and a flickering magenta neon, cramped and rough.
```

## Pantallas LED (`lote_pantallas.json`, `imageassets/pantallas/`)

### pantalla-h-nova (16:9)

```
Use the attached reference image ONLY for the color mood and style; do not copy the scene. Flat full-frame 2D digital advertisement artwork for a giant LED screen in a cyberpunk city, filling the entire image edge to edge, no frame, no border, no bezel, no screen device, no background around it. Bold simple graphic design, strong shapes, high contrast, glowing colors in acid green, magenta and teal on dark background, slightly stylized like the game style references. Invented brands only, no real logos. Any text must be spelled EXACTLY as written, in big bold simple letters, and no other text anywhere. Subject: an advertisement for the NOVA corporation: an enormous clean white monolithic tower with a vertical stripe of white light in the center rising into a dark sky, cold blue and teal glow, the word "NOVA" in huge thin elegant white letters on the right side.
```

### pantalla-h-bebida (16:9)

```
Use the attached reference image ONLY for the color mood and style; do not copy the scene. Flat full-frame 2D digital advertisement artwork for a giant LED screen in a cyberpunk city, filling the entire image edge to edge, no frame, no border, no bezel, no screen device, no background around it. Bold simple graphic design, strong shapes, high contrast, glowing colors in acid green, magenta and teal on dark background, slightly stylized like the game style references. Invented brands only, no real logos. Any text must be spelled EXACTLY as written, in big bold simple letters, and no other text anywhere. Subject: an advertisement for an energy drink: a big tall can glowing acid green with lightning bolts bursting around it, splashes of green liquid, magenta sparks, the word "RAYO" in huge bold slanted letters next to the can.
```

### pantalla-h-casino (16:9)

```
Use the attached reference image ONLY for the color mood and style; do not copy the scene. Flat full-frame 2D digital advertisement artwork for a giant LED screen in a cyberpunk city, filling the entire image edge to edge, no frame, no border, no bezel, no screen device, no background around it. Bold simple graphic design, strong shapes, high contrast, glowing colors in acid green, magenta and teal on dark background, slightly stylized like the game style references. Invented brands only, no real logos. Any text must be spelled EXACTLY as written, in big bold simple letters, and no other text anywhere. Subject: an advertisement for the VivaCoin casino: a cascade of big gold coins, a slot machine showing three sevens, magenta and gold light rays, the word "VIVACOIN" in huge bold gold letters.
```

### pantalla-v-implantes (9:16)

```
Use the attached reference image ONLY for the color mood and style; do not copy the scene. Flat full-frame 2D digital advertisement artwork for a giant LED screen in a cyberpunk city, filling the entire image edge to edge, no frame, no border, no bezel, no screen device, no background around it. Bold simple graphic design, strong shapes, high contrast, glowing colors in acid green, magenta and teal on dark background, slightly stylized like the game style references. Invented brands only, no real logos. Any text must be spelled EXACTLY as written, in big bold simple letters, and no other text anywhere. Subject: a vertical advertisement for cybernetic implants: a stylized human profile head with a glowing teal circuit pattern and a chrome robotic arm below it, magenta highlights, the text "CROMO+" in huge bold letters at the top.
```

### pantalla-v-ramen (9:16)

```
Use the attached reference image ONLY for the color mood and style; do not copy the scene. Flat full-frame 2D digital advertisement artwork for a giant LED screen in a cyberpunk city, filling the entire image edge to edge, no frame, no border, no bezel, no screen device, no background around it. Bold simple graphic design, strong shapes, high contrast, glowing colors in acid green, magenta and teal on dark background, slightly stylized like the game style references. Invented brands only, no real logos. Any text must be spelled EXACTLY as written, in big bold simple letters, and no other text anywhere. Subject: a vertical advertisement for a ramen shop: a big steaming bowl of ramen with noodles lifted by chopsticks, egg and green onions, glowing magenta and acid green steam, the text "RAMEN 24H" in big bold letters at the top.
```

### pantalla-v-robot (9:16)

```
Use the attached reference image ONLY for the color mood and style; do not copy the scene. Flat full-frame 2D digital advertisement artwork for a giant LED screen in a cyberpunk city, filling the entire image edge to edge, no frame, no border, no bezel, no screen device, no background around it. Bold simple graphic design, strong shapes, high contrast, glowing colors in acid green, magenta and teal on dark background, slightly stylized like the game style references. Invented brands only, no real logos. Any text must be spelled EXACTLY as written, in big bold simple letters, and no other text anywhere. Subject: a vertical advertisement showing the big friendly face of a robot smiling: a rounded chrome head with two big glowing teal eyes and a wide happy LED smile, magenta and acid green background shapes, the word "HOLA" in big bold letters at the bottom.
```


# Parte 14 — Assets de interiores (I01–I21) y texturas de pared (X01–X05)

Lote: `lote_interiores_assets.json` · salida `imageassets/interiores/` · edit con referencias de `docs/referencias-interiores/` · 1k.

## I01 — I01_Ropero
- **Medida:** 1 × 0.55 × 2 m · **Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K · **Adjuntar:** `habitacion.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a tall old wooden wardrobe with two doors, worn brown wood with chipped edges, chunky round knobs, a few colorful stickers on the doors shown as simple shapes (stars, a skull, a lightning bolt, a plain magenta circle), a small mirror panel on one door, a cardboard box on top. Doors closed. View: 3/4 view from the front left, slightly from above.
```

## I02 — I02_EscritorioPC
- **Medida:** 1.2 × 0.6 × 0.75 m (monitor sube a 1.2 m) · **Tripo:** Smart Mesh P2 · 10,000 caras · textura 2K · **Adjuntar:** `habitacion.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a cheap metal desk with a thick grey metal frame and a scratched laminate top, one drawer unit on the side, on top an old chunky CRT computer monitor with a glowing teal screen showing abstract shapes, a chunky beige keyboard, a mouse, two soda cans and a small pile of papers. View: 3/4 view from the front left, slightly from above.
```

## I03 — I03_SillaRopa
- **Medida:** 0.5 × 0.5 × 0.9 m · **Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K · **Adjuntar:** `habitacion.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a simple metal desk chair with a padded seat and backrest in worn dark vinyl, thick tube legs, a hoodie and a pair of jeans thrown over the backrest as soft chunky draped shapes. View: 3/4 view from the front left, slightly from above.
```

## I04 — I04_VentanaBarrotes
- **Medida:** 1.6 × 1.2 m · fondo 0.25 m · **Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K · **Adjuntar:** `habitacion.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a window for an interior wall seen from the inside: a thick worn painted metal frame with two glass panes (flat dark blue glass with a faint magenta and teal glow, no scene visible through it), thick vertical security bars in front of it, a wide windowsill with a small potted plant. The back is flat so it fits into a wall opening. View: straight from the front with a very slight angle.
```

## I05 — I05_MochilaRopa
- **Medida:** 0.9 × 0.6 × 0.4 m · **Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K · **Adjuntar:** `habitacion.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a small pile on the floor: a worn backpack in dark grey with a teal stripe, slumped, next to a crumpled t-shirt, a pair of sneakers and a cap, all chunky and simplified, grouped as one object. View: 3/4 view from the front left, slightly from above.
```

## I06 — I06_BrazoCirugia
- **Medida:** 2 m de largo · plato de 0.6 m · **Tripo:** HD · 12,000 caras · textura 2K · **Adjuntar:** `consultorio-doc.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a robotic surgery arm that hangs from the ceiling: a round flat ceiling mount plate at the top, a thick jointed industrial arm with three segments in white and gunmetal, chunky joints with teal glowing rings, at the end a surgical head with a ring light and three chunky tool tips (a drill, a laser, a gripper), thick bundled cables along the arm. Hanging pose, folded slightly downward. The top plate is flat. View: 3/4 view from the front, slightly from below.
```

## I07 — I07_SillonCirugia
- **Medida:** 2 × 0.7 × 1 m · **Tripo:** Smart Mesh P2 · 12,000 caras · textura 2K · **Adjuntar:** `consultorio-doc.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a reclining surgery chair for an underground clinic: a padded medical chair in worn teal-grey vinyl, reclined almost flat, with thick leather restraint straps with buckles on the arms and legs, a chunky hydraulic base with a pedal, an adjustable headrest, small scuffs and a stain. View: 3/4 view from the front left, slightly from above.
```

## I08 — I08_VitrinaImplantes
- **Medida:** 1.2 × 0.35 × 1.4 m · **Tripo:** HD · 10,000 caras · textura 2K · **Adjuntar:** `consultorio-doc.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a wall-mounted glass display cabinet with a thick gunmetal frame and three shelves, teal LED light inside, holding cybernetic implants: two chrome robotic hands, three robotic eyes with glowing teal irises in small stands, a chrome jaw piece and a few chips. The back is flat to hang on a wall. View: from the front with a slight angle.
```

## I09 — I09_Lavabo
- **Medida:** 0.6 × 0.45 m · espejo arriba, total 1.6 m · **Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K · **Adjuntar:** `consultorio-doc.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: an old wall-mounted sink: a chunky white ceramic basin with chips and rust stains, a thick old metal faucet with two cross handles, a chunky drain pipe under it going into the wall, a small cracked mirror above with a thick frame. The back is flat to fit on a wall. View: 3/4 view from the front left, slightly from above.
```

## I10 — I10_MonitorMedico
- **Medida:** 0.5 × 0.5 × 1.6 m · **Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K · **Adjuntar:** `consultorio-doc.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a medical monitor on a rolling stand: a thick pole on a five-wheel base, a chunky boxy monitor on top with a glowing screen showing abstract green heart-rate lines and teal bars, a small basket and a coiled thick cable. View: 3/4 view from the front left, slightly from above.
```

## I11 — I11_MesaBlackjack
- **Medida:** 2 × 1.1 × 0.8 m · **Tripo:** Smart Mesh P2 · 10,000 caras · textura 2K · **Adjuntar:** `casino-interior.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a casino blackjack table: a half-moon shaped table with green felt top, a padded dark red leather rail around the curved edge, a dark wood base, a chip tray on the straight side with stacks of colorful chips, a few cards face down, gold trim and a thin magenta light strip under the edge. View: 3/4 view from the front left, slightly from above.
```

## I12 — I12_SillaCasino
- **Medida:** 0.55 × 0.55 × 1 m · **Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K · **Adjuntar:** `casino-interior.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a casino chair: a padded round-back chair in dark red velvet with gold trim, chunky gold metal legs with a foot ring. View: 3/4 view from the front left, slightly from above.
```

## I13 — I13_CandilNeon
- **Medida:** 2 m de diámetro · 1.5 m de alto · **Tripo:** HD · 10,000 caras · textura 2K · **Adjuntar:** `casino-interior.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a big tiered ceiling chandelier made of neon: three stacked rings getting smaller toward the bottom, made of thick gold metal bands with glowing magenta and warm gold light tubes, hanging from a flat round ceiling plate by three thick rods. View: 3/4 view from slightly below.
```

## I14 — I14_BarraBar
- **Medida:** 3 m de tramo · barra 1.1 m alta · estante 2.4 m · **Tripo:** Smart Mesh P2 · 12,000 caras · textura 2K · **Adjuntar:** `casino-interior.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a straight section of a casino bar counter: a solid counter with a dark polished top and a glowing front panel in magenta and gold with a geometric pattern, a gold foot rail, and behind it a back shelf unit with three shelves full of glowing bottles of different colors and a mirror back. The two side ends are flat and clean so sections can be placed side by side. View: 3/4 view from the front left.
```

## I15 — I15_BancoBarra
- **Medida:** 0.45 × 0.45 × 0.8 m · **Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K · **Adjuntar:** `casino-interior.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a tall bar stool: a round padded seat in dark red velvet, a thick gold pole, a gold foot ring and a round heavy base. View: 3/4 view from the front left, slightly from above.
```

## I16 — I16_ArcoDorado
- **Medida:** 3 × 3.5 m · fondo 0.6 m · **Tripo:** Smart Mesh P2 · 12,000 caras · textura 2K · **Adjuntar:** `casino-interior.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a grand casino entrance arch: two thick gold columns with geometric art-deco details and magenta light strips, joined at the top by a thick decorated gold beam with a stepped crown shape and a row of round warm bulbs, the opening empty. Both faces look the same. View: 3/4 view from the front.
```

## I17 — I17_VitrinaPistolas
- **Medida:** 2 × 0.6 × 1 m · **Tripo:** HD · 12,000 caras · textura 2K · **Adjuntar:** `entrada-armas-mercado.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a gun shop display counter: a long counter with a thick scratched metal frame and glass top and front, inside on dark felt several pistols and a few magazines displayed in rows, a weak amber light inside, a cash tray on top, dents and grime. View: 3/4 view from the front left, slightly from above.
```

## I18 — I18_TableroRifles
- **Medida:** 2 × 1.2 m · fondo 0.3 m · **Tripo:** HD · 12,000 caras · textura 2K · **Adjuntar:** `entrada-armas-mercado.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a wall rack for rifles: a big metal pegboard panel with a thick frame, four rifles and two shotguns hanging horizontally on chunky hooks, a few boxes of ammunition on a small shelf at the bottom, a weak amber light strip on top. The back is flat to hang on a wall. View: from the front with a slight angle.
```

## I19 — I19_RejaTijera
- **Medida:** 2.5 × 2.2 m · fondo 0.15 m · **Tripo:** HD · 12,000 caras · textura 2K · **Adjuntar:** `entrada-armas-mercado.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a sliding scissor security gate (collapsible gate) fully extended: a diamond lattice of thick flat steel bars with chunky rivets at every joint, a thick top track and bottom track, a heavy padlock on one side, scratches and rust. View: from the front with a slight angle.
```

## I20 — I20_EstanteDrones
- **Medida:** 1.2 × 0.5 × 2 m · **Tripo:** HD · 12,000 caras · textura 2K · **Adjuntar:** `taller-tino.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a metal shelving unit in a workshop with four shelves holding half-built drones: open drone frames with exposed chunky parts, loose propellers, motors, battery packs, a toolbox and a coil of thick cable, one drone with a glowing teal eye. View: 3/4 view from the front left, slightly from above.
```

## I21 — I21_LamparaBrazo
- **Medida:** 0.7 m extendida · **Tripo:** Smart Mesh P2 · 8,000 caras · textura 2K · **Adjuntar:** `taller-tino.png`

```
Use the attached reference image ONLY for the art style, rendering, colors, lighting mood and level of wear; do not copy the room or its layout. Draw only the single object described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise, readable from a distance. Mexican cyberpunk video game interior prop. Single object, centered, fully visible with empty space around it, isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, straps, bars and handles are thick and chunky; cables are thick bundled conduits. Subject: a desk lamp with an articulated arm for a workbench: a heavy clamp base, two thick arm segments with chunky springs and a round metal shade glowing warm orange. View: 3/4 view from the front left, slightly from above.
```

## X01 — X01_ConcretoDescascarado (textura)
- **Aspecto:** 1:1 · no va a Tripo · **Adjuntar:** `habitacion.png`

```
Use the attached reference image ONLY for the art style, colors and level of wear; do not copy the room. A seamless tileable texture: a perfectly flat surface seen straight on, orthographic, filling the entire square image edge to edge, even and flat lighting with no shadows, no vignette, no perspective, no objects in front, no text, no letters, no logos. The left and right edges and the top and bottom edges must continue seamlessly so the image repeats without visible seams. Stylized hand-painted game texture, chunky simple shapes, soft painted gradients, no photorealistic noise. Subject: a concrete wall with peeling faded paint, patches of bare grey concrete, water stains and dark grime streaks running down.
```

## X02 — X02_AzulejoHospital (textura)
- **Aspecto:** 1:1 · no va a Tripo · **Adjuntar:** `consultorio-doc.png`

```
Use the attached reference image ONLY for the art style, colors and level of wear; do not copy the room. A seamless tileable texture: a perfectly flat surface seen straight on, orthographic, filling the entire square image edge to edge, even and flat lighting with no shadows, no vignette, no perspective, no objects in front, no text, no letters, no logos. The left and right edges and the top and bottom edges must continue seamlessly so the image repeats without visible seams. Stylized hand-painted game texture, chunky simple shapes, soft painted gradients, no photorealistic noise. Subject: a wall of square aqua green ceramic tiles in a regular grid with dirty grout lines, some cracked tiles, light stains and grime.
```

## X03 — X03_LadrilloYeso (textura)
- **Aspecto:** 1:1 · no va a Tripo · **Adjuntar:** `taller-tino.png`

```
Use the attached reference image ONLY for the art style, colors and level of wear; do not copy the room. A seamless tileable texture: a perfectly flat surface seen straight on, orthographic, filling the entire square image edge to edge, even and flat lighting with no shadows, no vignette, no perspective, no objects in front, no text, no letters, no logos. The left and right edges and the top and bottom edges must continue seamlessly so the image repeats without visible seams. Stylized hand-painted game texture, chunky simple shapes, soft painted gradients, no photorealistic noise. Subject: an old red brick wall in a regular running bond pattern, with patches of fallen white plaster partly covering the bricks, chips and stains.
```

## X04 — X04_ParedCasino (textura)
- **Aspecto:** 1:1 · no va a Tripo · **Adjuntar:** `casino-interior.png`

```
Use the attached reference image ONLY for the art style, colors and level of wear; do not copy the room. A seamless tileable texture: a perfectly flat surface seen straight on, orthographic, filling the entire square image edge to edge, even and flat lighting with no shadows, no vignette, no perspective, no objects in front, no text, no letters, no logos. The left and right edges and the top and bottom edges must continue seamlessly so the image repeats without visible seams. Stylized hand-painted game texture, chunky simple shapes, soft painted gradients, no photorealistic noise. Subject: a dark red casino wall panel with an elegant repeating art-deco golden fret pattern (greek key) as a border band and a subtle damask pattern in the red area, polished.
```

## X05 — X05_TechoVigasTubos (textura)
- **Aspecto:** 1:1 · no va a Tripo · **Adjuntar:** `taller-tino.png`

```
Use the attached reference image ONLY for the art style, colors and level of wear; do not copy the room. A seamless tileable texture: a perfectly flat surface seen straight on, orthographic, filling the entire square image edge to edge, even and flat lighting with no shadows, no vignette, no perspective, no objects in front, no text, no letters, no logos. The left and right edges and the top and bottom edges must continue seamlessly so the image repeats without visible seams. Stylized hand-painted game texture, chunky simple shapes, soft painted gradients, no photorealistic noise. Subject: a ceiling seen from below: parallel dark wooden beams running across, with grey metal pipes and a thick bundled conduit running between them along the beams, dark plaster between.
```


# Parte 15 — Pedido 06-nov (armas, hologramas, VFX, calle, policía y carros)

Lote: `lote_06nov.json` · script: `generar_06nov.ps1` (corre las fases 1→5 en orden).

## W02 — W02_Escopeta (fase 1)
- **Salida:** `imageassets/armas/W02_Escopeta.png` · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `W01_Pistola.png`
- **Medida:** 1.0 m de largo · **Tripo:** HD · 12000 · 2K

```
Use the attached pistol image as the exact reference for the art style, materials, colors, wear and level of detail: the new weapon must look like it belongs to the same set as that pistol. Do not copy the pistol's shape. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Mexican cyberpunk megacity street weapon, worn and patched: dark gunmetal and charcoal metal, sand-beige worn polymer parts, burnt orange accents, red cloth tape wrapped on the grip, small glowing teal and amber lights, scratches and rust spots. The whole weapon fully visible, no hands, no person, no text, no letters, no numbers, no logos. Single object, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow. Nothing thin or dangling: straps, sights and rails are thick and chunky. View: 3/4 view from the side, slightly from the front, showing the full length of the weapon. Subject: a pump-action shotgun: a thick short barrel with a magazine tube under it, a chunky ribbed pump grip in sand-beige polymer, a boxy receiver in gunmetal with a teal shell counter light, a solid stock with a rubber butt pad, red tape on the grip, a short thick rail on top.
```

## W03 — W03_Fusil (fase 1)
- **Salida:** `imageassets/armas/W03_Fusil.png` · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `W01_Pistola.png`
- **Medida:** 0.8 m de largo · **Tripo:** HD · 12000 · 2K

```
Use the attached pistol image as the exact reference for the art style, materials, colors, wear and level of detail: the new weapon must look like it belongs to the same set as that pistol. Do not copy the pistol's shape. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Mexican cyberpunk megacity street weapon, worn and patched: dark gunmetal and charcoal metal, sand-beige worn polymer parts, burnt orange accents, red cloth tape wrapped on the grip, small glowing teal and amber lights, scratches and rust spots. The whole weapon fully visible, no hands, no person, no text, no letters, no numbers, no logos. Single object, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow. Nothing thin or dangling: straps, sights and rails are thick and chunky. View: 3/4 view from the side, slightly from the front, showing the full length of the weapon. Subject: a compact assault rifle: a short barrel with a chunky muzzle brake, a boxy handguard with thick vents in sand-beige, a curved box magazine in burnt orange, a gunmetal receiver, a compact red dot sight on top with a glowing red dot lens, a collapsible stock, a small teal ammo counter on the side.
```

## W04 — W04_Francotirador (fase 1)
- **Salida:** `imageassets/armas/W04_Francotirador.png` · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `W01_Pistola.png`
- **Medida:** 1.25 m de largo · **Tripo:** HD · 12000 · 2K

```
Use the attached pistol image as the exact reference for the art style, materials, colors, wear and level of detail: the new weapon must look like it belongs to the same set as that pistol. Do not copy the pistol's shape. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Mexican cyberpunk megacity street weapon, worn and patched: dark gunmetal and charcoal metal, sand-beige worn polymer parts, burnt orange accents, red cloth tape wrapped on the grip, small glowing teal and amber lights, scratches and rust spots. The whole weapon fully visible, no hands, no person, no text, no letters, no numbers, no logos. Single object, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow. Nothing thin or dangling: straps, sights and rails are thick and chunky. View: 3/4 view from the side, slightly from the front, showing the full length of the weapon. Subject: a bolt-action sniper rifle: a long thick barrel with a chunky suppressor-like muzzle, a big long scope with thick rings and a teal glowing lens, a chunky bolt handle, a boxy magazine, a sand-beige stock with a cheek rest and red tape, a folded bipod under the front of the barrel (legs folded flat along the barrel).
```

## H01 — H01_Pistola (fase 2)
- **Salida:** `imageassets/ui/hologramas/H01_Pistola.png` · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `W01_Pistola.png`

```
Turn the weapon in the attached image into a holographic projection for a video game user interface. Keep EXACTLY the same weapon shape, silhouette, parts and proportions as the attached image, but shown perfectly from the side (pure side profile, orthographic, muzzle pointing to the right), centered and filling about 80% of the image width. Hologram look: translucent cyan-turquoise glowing body, bright glowing outlines and edge lines on every part, faint inner wireframe lines, thin horizontal scan lines across the whole weapon, a slight digital glitch offset on one or two horizontal slices, soft cyan glow around it. The background is PURE BLACK (#000000) everywhere, completely flat, no gradient, no floor, no grid, no particles outside the weapon, no text, no letters, no numbers, no logos, no hands.
```

## H02 — H02_Escopeta (fase 2)
- **Salida:** `imageassets/ui/hologramas/H02_Escopeta.png` · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `W02_Escopeta.png`

```
Turn the weapon in the attached image into a holographic projection for a video game user interface. Keep EXACTLY the same weapon shape, silhouette, parts and proportions as the attached image, but shown perfectly from the side (pure side profile, orthographic, muzzle pointing to the right), centered and filling about 80% of the image width. Hologram look: translucent cyan-turquoise glowing body, bright glowing outlines and edge lines on every part, faint inner wireframe lines, thin horizontal scan lines across the whole weapon, a slight digital glitch offset on one or two horizontal slices, soft cyan glow around it. The background is PURE BLACK (#000000) everywhere, completely flat, no gradient, no floor, no grid, no particles outside the weapon, no text, no letters, no numbers, no logos, no hands.
```

## H03 — H03_Fusil (fase 2)
- **Salida:** `imageassets/ui/hologramas/H03_Fusil.png` · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `W03_Fusil.png`

```
Turn the weapon in the attached image into a holographic projection for a video game user interface. Keep EXACTLY the same weapon shape, silhouette, parts and proportions as the attached image, but shown perfectly from the side (pure side profile, orthographic, muzzle pointing to the right), centered and filling about 80% of the image width. Hologram look: translucent cyan-turquoise glowing body, bright glowing outlines and edge lines on every part, faint inner wireframe lines, thin horizontal scan lines across the whole weapon, a slight digital glitch offset on one or two horizontal slices, soft cyan glow around it. The background is PURE BLACK (#000000) everywhere, completely flat, no gradient, no floor, no grid, no particles outside the weapon, no text, no letters, no numbers, no logos, no hands.
```

## H04 — H04_Francotirador (fase 2)
- **Salida:** `imageassets/ui/hologramas/H04_Francotirador.png` · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `W04_Francotirador.png`

```
Turn the weapon in the attached image into a holographic projection for a video game user interface. Keep EXACTLY the same weapon shape, silhouette, parts and proportions as the attached image, but shown perfectly from the side (pure side profile, orthographic, muzzle pointing to the right), centered and filling about 80% of the image width. Hologram look: translucent cyan-turquoise glowing body, bright glowing outlines and edge lines on every part, faint inner wireframe lines, thin horizontal scan lines across the whole weapon, a slight digital glitch offset on one or two horizontal slices, soft cyan glow around it. The background is PURE BLACK (#000000) everywhere, completely flat, no gradient, no floor, no grid, no particles outside the weapon, no text, no letters, no numbers, no logos, no hands.
```

## F01 — F01_Destello (fase 3)
- **Salida:** `imageassets/vfx/F01_Destello.png` · **Aspecto:** 1:1 · **Endpoint:** `text-to-image`

```
A single small compact muzzle flash of a pistol shot for a video game particle effect, seen from the side with the flash pointing to the right: a tight bright white-yellow core with a short burst of 4 to 5 sharp orange-yellow flame petals, very brief and compact, NOT a big explosion, NO smoke, NO sparks trailing far, no gun visible. Centered, filling about 60% of the image. Stylized and clean, soft glow. The background is PURE BLACK (#000000) everywhere, flat, no gradient. No text, no logos.
```

## D01 — D01_HuecoConcreto (fase 3)
- **Salida:** `imageassets/vfx/D01_HuecoConcreto.png` · **Aspecto:** 1:1 · **Endpoint:** `text-to-image`

```
A bullet hole decal in a concrete wall, seen perfectly straight on (orthographic, flat, no perspective): a small dark round crater in the center with chipped broken concrete edges, a few short radial cracks and scattered grey dust and chips around it, fading out softly to nothing toward the edges. Stylized hand-painted game texture, clean. Everything outside the damage is PURE WHITE (#FFFFFF), flat, no wall texture beyond the fading damage, no shadow, no text.
```

## D02 — D02_HuecoMetal (fase 3)
- **Salida:** `imageassets/vfx/D02_HuecoMetal.png` · **Aspecto:** 1:1 · **Endpoint:** `text-to-image`

```
A bullet hole decal in a painted car body metal sheet, seen perfectly straight on (orthographic, flat, no perspective): a small dark hole in the center with the metal dented inward, a dark burnt ring around it, chipped and peeled paint flakes showing bare silver metal around the hole, small scratches, fading out softly toward the edges. Stylized hand-painted game texture, clean. Everything outside the damage is PURE WHITE (#FFFFFF), flat, no shadow, no text.
```

## M01 — M01_LineasAmarillas (fase 4)
- **Salida:** `imageassets/calle/M01_LineasAmarillas.png` · **Aspecto:** 9:16 · **Endpoint:** `text-to-image`

```
Top-down orthographic view of road paint only: a double continuous yellow center line running vertically from the very top edge to the very bottom edge of the image, two parallel solid yellow stripes with a small gap between them, centered horizontally, worn and faded paint with chips, cracks and dirt so some dark spots show through. The pattern must tile seamlessly from top to bottom: the stripes are perfectly straight and identical at the top and bottom edges. Everything that is not paint is PURE BLACK (#000000), flat: no asphalt, no texture, no shadows, no text.
```

## M02 — M02_CrucePeatonal (fase 4)
- **Salida:** `imageassets/calle/M02_CrucePeatonal.png` · **Aspecto:** 16:9 · **Endpoint:** `text-to-image`

```
Top-down orthographic view of road paint only: a zebra pedestrian crosswalk, 8 thick parallel white stripes of equal width and equal spacing, running vertically across the image side by side, worn and faded paint with chips, cracks and dirt so some dark spots show through. Everything that is not paint is PURE BLACK (#000000), flat: no asphalt, no texture, no shadows, no text.
```

## C01 — C01_Policia (fase 5)
- **Salida:** `imageassets/personajes/C01_Policia.png` · **Aspecto:** 1:1 · **Endpoint:** `edit` · **Adjuntar:** `BaseHombre_frente.png`, `man.png`
- **Medida:** 1.78 m de alto · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
STYLE IS THE TOP PRIORITY: draw this character in EXACTLY the same art style as the attached stylized game character reference images, as if made by the same artist for the same game: same rendering, shading, hand-painted finish, head-to-body proportions and face style. Copy only the style, not their face or clothes. Character: an adult city police officer of a Mexican cyberpunk megacity, medium brown skin, short black hair, serious neutral face. Navy blue tactical uniform fitted close to the body, a light fitted ballistic vest in dark navy with reflective turquoise stripes, a thick utility belt with flat pouches, knee pads, black tactical boots, a navy cap with a visor fitted to the head. Hands empty and open, no weapon, no holster with a gun, no radio cords. No text, no badges with letters, no numbers, no logos: the chest patch is a plain blank shield shape. Rig-friendly design: nothing hanging loose, no capes, no dangling straps. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms facing down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow.
```

## V09 — V09_Taxi (fase 5)
- **Salida:** `imageassets/carros/V09_Taxi.png` · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `V01a_CarroViejito.png`
- **Medida:** 4.2 m de largo · **Tripo:** Smart Mesh P2 · 18000 · 2K

```
Use the attached reference image ONLY for the art style, rendering, proportions of the wheels and level of wear; do not copy its car shape. Draw only the single vehicle described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Poor gritty Mexican cyberpunk megacity vehicle: old and dented, mismatched replacement panels, rust streaks, scratches, a few cheap futuristic add-ons with small glowing teal and orange lights. The four wheels are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Windows are dark tinted solid glass, no interior visible. Nothing thin: mirrors, antennas and bars are thick and chunky. The whole vehicle fully visible, single object, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no license plate text, no logos, no people. View: 3/4 view from the front left, slightly from above. Subject: an old compact 4-door taxi sedan painted pink and white (pink lower body and doors, white roof and hood), dented doors and a crumpled bumper, a boxy taxi sign on the roof glowing warm white with NO text on it, a small teal LED strip in the grille.
```

## V10 — V10_Pickup (fase 5)
- **Salida:** `imageassets/carros/V10_Pickup.png` · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `V01a_CarroViejito.png`
- **Medida:** 4.8 m de largo · **Tripo:** Smart Mesh P2 · 18000 · 2K

```
Use the attached reference image ONLY for the art style, rendering, proportions of the wheels and level of wear; do not copy its car shape. Draw only the single vehicle described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Poor gritty Mexican cyberpunk megacity vehicle: old and dented, mismatched replacement panels, rust streaks, scratches, a few cheap futuristic add-ons with small glowing teal and orange lights. The four wheels are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Windows are dark tinted solid glass, no interior visible. Nothing thin: mirrors, antennas and bars are thick and chunky. The whole vehicle fully visible, single object, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no license plate text, no logos, no people. View: 3/4 view from the front left, slightly from above. Subject: an old 2-door pickup truck in faded washed-out turquoise paint with heavy rust patches, a dented tailgate, the open rear bed loaded with stacked wooden produce crates (huacales) tied with a thick strap, a chunky black tube bumper, an orange glowing light on the cab roof.
```

## V11 — V11_Van (fase 5)
- **Salida:** `imageassets/carros/V11_Van.png` · **Aspecto:** 16:9 · **Endpoint:** `edit` · **Adjuntar:** `V01a_CarroViejito.png`
- **Medida:** 5 m de largo · **Tripo:** Smart Mesh P2 · 18000 · 2K

```
Use the attached reference image ONLY for the art style, rendering, proportions of the wheels and level of wear; do not copy its car shape. Draw only the single vehicle described. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Poor gritty Mexican cyberpunk megacity vehicle: old and dented, mismatched replacement panels, rust streaks, scratches, a few cheap futuristic add-ons with small glowing teal and orange lights. The four wheels are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Windows are dark tinted solid glass, no interior visible. Nothing thin: mirrors, antennas and bars are thick and chunky. The whole vehicle fully visible, single object, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no license plate text, no logos, no people. View: 3/4 view from the front left, slightly from above. Subject: a square boxy delivery van with a flat front and a tall box body, painted cream on top and orange on the lower half, dents along the side, a big sliding side door slightly open showing only darkness, a small roof rack, an orange LED strip on the front, plain colored panels with no text.
```


# Parte 16 — Pedido 06-nov tarde (HUD, modelos nuevos, banqueta)

Lote: `lote_06nov_b.json` · script: `generar_06nov_b.ps1`.

## HUD_A — HUD_A_minimalista (fase 1)
- **Salida:** `imageassets/ui/hud-opciones/HUD_A_minimalista.png` · **Aspecto:** 16:9 · **Adjuntar:** `calle-suburbios-noche.png`, `H01_Pistola.png`

```
The FIRST attached image is the game world: use it as the 3D scene behind the interface (a third-person view at night on a street of a Mexican cyberpunk megacity, keep its art style), with a player character seen from behind in the lower center holding a pistol. The SECOND attached image is the weapon hologram used in the game: use it, small, as the weapon icon in the weapon panel. Draw a full 16:9 video game screenshot with a modern, polished heads-up display on top, like a high-budget commercial game. HUD elements: health bar labeled "VIDA", energy shield bar labeled "ESCUDO", money counter "$ 12,450", a police wanted level shown as 3 out of 5 lit icons, a weapon panel with the pistol hologram and the ammo count "12 / 48", a minimap in a corner, and a mission objective "Ve con el Doc" with a small distance "120 m". All HUD text must be in Spanish and spelled EXACTLY as written, no other words, no logos. The HUD keeps the center of the screen clear; elements sit in the corners and edges. HUD style: MINIMALIST: very thin white and soft turquoise lines, lots of empty space, small clean sans-serif text, tiny bars, no heavy panels or boxes, only the essentials, subtle and elegant.
```

## HUD_B — HUD_B_holografico (fase 1)
- **Salida:** `imageassets/ui/hud-opciones/HUD_B_holografico.png` · **Aspecto:** 16:9 · **Adjuntar:** `calle-suburbios-noche.png`, `H01_Pistola.png`

```
The FIRST attached image is the game world: use it as the 3D scene behind the interface (a third-person view at night on a street of a Mexican cyberpunk megacity, keep its art style), with a player character seen from behind in the lower center holding a pistol. The SECOND attached image is the weapon hologram used in the game: use it, small, as the weapon icon in the weapon panel. Draw a full 16:9 video game screenshot with a modern, polished heads-up display on top, like a high-budget commercial game. HUD elements: health bar labeled "VIDA", energy shield bar labeled "ESCUDO", money counter "$ 12,450", a police wanted level shown as 3 out of 5 lit icons, a weapon panel with the pistol hologram and the ammo count "12 / 48", a minimap in a corner, and a mission objective "Ve con el Doc" with a small distance "120 m". All HUD text must be in Spanish and spelled EXACTLY as written, no other words, no logos. The HUD keeps the center of the screen clear; elements sit in the corners and edges. HUD style: HOLOGRAPHIC: translucent cyan glass panels with glowing edges, faint horizontal scan lines, a subtle digital glitch on a few edges, slightly angled panels as if projected in front of the camera, soft cyan glow.
```

## HUD_C — HUD_C_tactico (fase 1)
- **Salida:** `imageassets/ui/hud-opciones/HUD_C_tactico.png` · **Aspecto:** 16:9 · **Adjuntar:** `calle-suburbios-noche.png`, `H01_Pistola.png`

```
The FIRST attached image is the game world: use it as the 3D scene behind the interface (a third-person view at night on a street of a Mexican cyberpunk megacity, keep its art style), with a player character seen from behind in the lower center holding a pistol. The SECOND attached image is the weapon hologram used in the game: use it, small, as the weapon icon in the weapon panel. Draw a full 16:9 video game screenshot with a modern, polished heads-up display on top, like a high-budget commercial game. HUD elements: health bar labeled "VIDA", energy shield bar labeled "ESCUDO", money counter "$ 12,450", a police wanted level shown as 3 out of 5 lit icons, a weapon panel with the pistol hologram and the ammo count "12 / 48", a minimap in a corner, and a mission objective "Ve con el Doc" with a small distance "120 m". All HUD text must be in Spanish and spelled EXACTLY as written, no other words, no logos. The HUD keeps the center of the screen clear; elements sit in the corners and edges. HUD style: TACTICAL: military-tech look, bars split into angular segments, big bold condensed numbers for ammo and money, corner brackets, small hazard ticks, dark grey panels with turquoise and orange accents, precise and readable.
```

## HUD_D — HUD_D_neon_callejero (fase 1)
- **Salida:** `imageassets/ui/hud-opciones/HUD_D_neon_callejero.png` · **Aspecto:** 16:9 · **Adjuntar:** `calle-suburbios-noche.png`, `H01_Pistola.png`

```
The FIRST attached image is the game world: use it as the 3D scene behind the interface (a third-person view at night on a street of a Mexican cyberpunk megacity, keep its art style), with a player character seen from behind in the lower center holding a pistol. The SECOND attached image is the weapon hologram used in the game: use it, small, as the weapon icon in the weapon panel. Draw a full 16:9 video game screenshot with a modern, polished heads-up display on top, like a high-budget commercial game. HUD elements: health bar labeled "VIDA", energy shield bar labeled "ESCUDO", money counter "$ 12,450", a police wanted level shown as 3 out of 5 lit icons, a weapon panel with the pistol hologram and the ammo count "12 / 48", a minimap in a corner, and a mission objective "Ve con el Doc" with a small distance "120 m". All HUD text must be in Spanish and spelled EXACTLY as written, no other words, no logos. The HUD keeps the center of the screen clear; elements sit in the corners and edges. HUD style: STREET NEON: elegant Mexican street neon signage look, glowing magenta and orange neon tube outlines, a bold stylized display font inspired by hand-painted Mexican street signs but clean and premium, small papel-picado-inspired geometric cut details on panel edges, dark backgrounds behind the text for readability.
```

## P10 — P10_PuertaCuarto (fase 2)
- **Salida:** `imageassets/modelos-nuevos/P10_PuertaCuarto.png` · **Aspecto:** 3:4 · **Adjuntar:** `habitacion.png`
- **Medida:** 0.9 × 2.1 m · marco 1.05 × 2.2 m · fondo 0.15 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. No text, no letters, no numbers, no logos. Subject: an interior apartment door of a poor building, with its door frame: painted wood with peeling faded turquoise paint and scratches, a dented round metal knob and a lock, colorful stickers shown as simple shapes (a star, a skull, a lightning bolt, a plain orange circle), a small scuff at the bottom. Door closed. The back of the frame is flat to fit in a wall. View: from the front with a slight angle.
```

## P11 — P11_EntradaCasa (fase 2)
- **Salida:** `imageassets/modelos-nuevos/P11_EntradaCasa.png` · **Aspecto:** 1:1 · **Adjuntar:** `calle-suburbios-noche.png`, `callejon-noche.png`
- **Medida:** 2.2 m ancho × 3 m alto · escalones salen 1 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. No text, no letters, no numbers, no logos. Subject: the street entrance of a cheap apartment building: a thick concrete door frame set in a short piece of painted wall, a metal grille security door with a thick lattice and a padlock, three worn concrete steps in front, a row of small dented metal mailboxes on the wall beside the door, a caged bulb above the door glowing warm orange, a thick cable conduit running up the wall, a small turquoise LED doorbell panel. The back is flat to fit against a building facade. View: 3/4 view from the front left.
```

## P12 — P12_FachadaConsultorio (fase 2)
- **Salida:** `imageassets/modelos-nuevos/P12_FachadaConsultorio.png` · **Aspecto:** 4:3 · **Adjuntar:** `callejon-noche.png`, `L13_LetreroIDI.png`
- **Medida:** 6 m ancho × 4.5 m alto · fondo 0.6 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. No text, no letters, no numbers, no logos. Subject: the ground floor facade module of a clandestine street clinic: a stained concrete wall section, a heavy reinforced metal door with rivets and a small speaker grille, a window covered from inside with metal sheets and taped cardboard, a glowing turquoise light strip and a small medical cross shape (plain, no letters) above the door, thick bundled cables and conduits running across the wall to a small junction box, an air conditioning unit, grime and rust streaks. Leave an empty flat area above the door for a separate sign. The back is flat. View: from the front with a slight 3/4 angle.
```

## L15 — L15_LetreroCanepa (fase 2)
- **Salida:** `imageassets/modelos-nuevos/L15_LetreroCanepa.png` · **Aspecto:** 16:9 · **Adjuntar:** `calle-suburbios-noche.png`, `L14_LetreroCIC.png`
- **Medida:** 3 × 1.5 m · fondo 0.3 m · **Tripo:** HD · 10000 · 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. The text must be spelled EXACTLY as written, letter by letter, and no other text. Subject: a hand-painted metal sheet workshop sign of a Mexican neighborhood car mechanic: a rectangular steel sheet with a thick welded frame, painted in bright colors with a hand-painted Mexican sign-painter (rotulista) style, on the top line the words "EL CANEPA" in huge bold letters, below it a smaller line "TALLER MECÁNICO" (with the accent on the A), a painted wrench and a gear as simple shapes, a row of round warm bulbs along the top edge, rust spots, chipped paint, two thick wall brackets on the back. View: from the front with a slight angle.
```

## R01 — R01_MeseraRobot (fase 2)
- **Salida:** `imageassets/modelos-nuevos/R01_MeseraRobot.png` · **Aspecto:** 1:1 · **Adjuntar:** `RobotMujer_frente.png`, `L10_LetreroVivaCoin.png`
- **Medida:** 1.70 m de alto · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo); la charola puede salir pegada a la mano

```
STYLE IS THE TOP PRIORITY: draw this character in EXACTLY the same art style as the first attached robot character image, as if made by the same artist for the same game; the second image is only for the casino gold and magenta colors. Character: a stylized feminine waitress robot of the VivaCoin casino: sleek glossy white shell plating with gold trim and gold joints, a small magenta glowing light on the chest, an elegant smooth face plate with two soft turquoise glowing eyes and no mouth, a small gold bow-tie shape on the neck and a short stylized white and gold skirt-shaped hip plate fitted close to the body, holding a round gold serving tray flat in its right hand. No text, no logos. Rig-friendly: everything fitted close to the body, nothing hanging loose. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, the tray held flat on the right palm, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow.
```

## IMP01 — IMP01_ExoPiernas (fase 2)
- **Salida:** `imageassets/modelos-nuevos/IMP01_ExoPiernas.png` · **Aspecto:** 3:4 · **Adjuntar:** `BaseHombre_frente.png`, `RobotHombre_frente.png`
- **Medida:** 1 m de alto (cadera a tobillo) · **Tripo:** HD · 10000 · 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. No text, no letters, no numbers, no logos. The attached characters show the style of the game's chrome and grey cyber parts. Subject: a wearable leg exoskeleton implant shown on its own with nobody wearing it, standing upright as if worn by an invisible person: a hip belt frame, thick external struts along the outside of both thighs and shins, chunky knee joints with small turquoise glowing cores, ankle cuffs and foot plates, small orange hydraulic pistons at the knees, gunmetal and chrome with worn paint. Hollow inside where the legs would go. View: from the front with a slight angle.
```

## IMP02 — IMP02_ExoBrazos (fase 2)
- **Salida:** `imageassets/modelos-nuevos/IMP02_ExoBrazos.png` · **Aspecto:** 4:3 · **Adjuntar:** `BaseHombre_frente.png`, `RobotHombre_frente.png`
- **Medida:** 0.8 m de ancho (hombro a hombro con brazos) · **Tripo:** HD · 10000 · 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. No text, no letters, no numbers, no logos. The attached characters show the style of the game's chrome and grey cyber parts. Subject: a wearable arm and shoulder exoskeleton implant shown on its own with nobody wearing it, as if worn by an invisible person in a slight A-pose: a back plate with a small turquoise power core, thick shoulder pads, external struts along the outside of both upper arms and forearms, chunky elbow joints with small orange hydraulic pistons, wrist cuffs, gunmetal and chrome with worn paint. Hollow inside where the body and arms would go. View: from the front with a slight angle.
```

## IMP03 — IMP03_VisorCabeza (fase 2)
- **Salida:** `imageassets/modelos-nuevos/IMP03_VisorCabeza.png` · **Aspecto:** 1:1 · **Adjuntar:** `BaseHombre_frente.png`, `RobotHombre_frente.png`
- **Medida:** 0.22 m de ancho · **Tripo:** HD · 8000 · 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. No text, no letters, no numbers, no logos. The attached characters show the style of the game's chrome and grey cyber parts. Subject: a head-mounted cyber visor implant shown on its own: a curved band that wraps around the head at eye level and covers the eyes and temples, a wide wraparound turquoise glowing lens in front, chunky temple modules with small orange lights and a thick short connector at the back, gunmetal and chrome. Hollow inside where the head would go. View: 3/4 view from the front left.
```

## O01 — O01_BebidaRayo (fase 2)
- **Salida:** `imageassets/modelos-nuevos/O01_BebidaRayo.png` · **Aspecto:** 3:4 · **Adjuntar:** `pantalla-h-bebida.png`
- **Medida:** 12 cm de alto · 6.6 cm de diámetro · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. Use the attached advertisement to copy the brand design of the drink. Subject: a single standard drink can of the energy drink, standing upright: glowing acid green and black design with a big lightning bolt, the word "RAYO" spelled exactly like that in bold slanted letters, and no other text, a metal pull tab on top. View: 3/4 view from the front, slightly from above.
```

## O02 — O02_Papitas (fase 2)
- **Salida:** `imageassets/modelos-nuevos/O02_Papitas.png` · **Aspecto:** 3:4 · **Adjuntar:** `tianguis-noche.png`
- **Medida:** 25 cm de alto · 18 cm de ancho · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. Subject: a single bag of potato chips of an invented brand, standing upright, puffy foil bag with crimped top and bottom edges, bright orange and turquoise design with a big cartoon chili pepper and a few chips drawn on it, the word "CRUNCHI" spelled exactly like that in big bold letters, and no other text. View: 3/4 view from the front.
```

## DR01 — DR01_Dron (fase 2)
- **Salida:** `imageassets/modelos-nuevos/DR01_Dron.png` · **Aspecto:** 4:3 · **Adjuntar:** `V05_PatrullaSedan.png`
- **Medida:** 0.9 m de ancho · **Tripo:** HD · 12000 · 2K · Activar segmentación por partes si está disponible; si no, separar hélices en Blender

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. No text, no letters, no numbers, no logos. Use the attached police car only for the police colors (navy blue and off-white with a teal stripe and red and blue lights). Subject: a police surveillance drone: a compact armored central body in navy blue and off-white, four thick arms each ending in a round propeller guard ring with a chunky propeller inside, the four propeller units clearly separated from the body by visible gaps at the end of each arm so they can be cut as separate pieces, a small red and blue light block on top, a glowing teal camera eye in front, and a small compact gun turret hanging under the body. View: 3/4 view from the front, slightly from above.
```

## T01B — T01_Banqueta (fase 3)
- **Salida:** `imageassets/calle/T01_Banqueta.png` · **Aspecto:** 1:1 · **Adjuntar:** `calle-suburbios-noche.png`

```
Use the attached reference image ONLY for the art style and colors. A seamless tileable texture of a concrete sidewalk seen straight from above (orthographic, flat, no perspective), filling the entire square image edge to edge: square concrete slabs in a regular grid (2 by 2 slabs) separated by dark joint lines, grey worn concrete with oil and gum stains, a few cracks and a small chipped corner, dirt in the joints. Even flat lighting, no shadows, no objects, no text. The left and right edges and the top and bottom edges must continue seamlessly. Stylized hand-painted game texture, no photorealistic noise.
```


# Parte 17 — Pedido 07-nov: look objetivo (zonas, barras, mapa) con HUD_A como referencia

Lote: `lote_07nov.json` · script: `generar_07nov.ps1`.

## K01 — K01_Barrio_HUDnuevo
- **Salida:** `imageassets/referencias/look/K01_Barrio_HUDnuevo.png` · 16:9 · **Adjuntar:** `HUD_A_minimalista.png`

```
Keep the attached image EXACTLY the same: the same street, houses, lights, character, camera and composition, change nothing in the scene. Only rearrange the HUD on top into this new layout, in the same thin minimalist style: Minimalist HUD on top, thin lines, thin clean sans-serif text, NO panels, NO boxes, NO frames: top-left a circular minimap made of thin lines with a small mission objective in thin text below it; top-right the money "$ 12,450" and a row of turquoise wanted-level stars; bottom-left two thin bars labeled "VIDA" (red-pink) and "ESCUDO" (turquoise); bottom-right a small cyan weapon hologram of a pistol with the ammo "12 / 48". The player character is seen from behind in third person like GTA, centered at the bottom of the screen, about one quarter of the screen height. All HUD text in Spanish, spelled EXACTLY as written, no other HUD text. The mission objective under the minimap reads "Ve con el Doc · 120 m".
```

## K02 — K02_Tianguis
- **Salida:** `imageassets/referencias/look/K02_Tianguis.png` · 16:9 · **Adjuntar:** `HUD_A_minimalista.png`, `tianguis-noche.png`

```
The FIRST attached image is the target look of the whole game: match its art style EXACTLY, as if it were another screenshot of the same game: the same illustrated painted rendering, the same blue night lighting with fog, the same wet reflective ground, the same house shapes, materials, colors and level of detail, and the same thin minimalist HUD typography and line style. The second image is only for the idea of the market stalls. Scene: the El Mercado neighborhood at night: a street closed to cars and filled with tianguis market stalls with colorful tarp roofs (pink, orange, turquoise, yellow) and strings of warm bulbs, piles of goods, many people walking and shopping as small stylized figures, a small plaza visible at the end of the street, and on one side a gun shop front behind a heavy metal grille with a dim amber light. Short city blocks: the next street corner is visible ahead, never an endless corridor. Thick cables strung between utility poles, puddles reflecting the lights. Minimalist HUD on top, thin lines, thin clean sans-serif text, NO panels, NO boxes, NO frames: top-left a circular minimap made of thin lines with a small mission objective in thin text below it; top-right the money "$ 12,450" and a row of turquoise wanted-level stars; bottom-left two thin bars labeled "VIDA" (red-pink) and "ESCUDO" (turquoise); bottom-right a small cyan weapon hologram of a pistol with the ammo "12 / 48". The player character is seen from behind in third person like GTA, centered at the bottom of the screen, about one quarter of the screen height. All HUD text in Spanish, spelled EXACTLY as written, no other HUD text. The mission objective under the minimap reads "Compra en el mercado · 60 m".
```

## K03 — K03_NeonAlto
- **Salida:** `imageassets/referencias/look/K03_NeonAlto.png` · 16:9 · **Adjuntar:** `HUD_A_minimalista.png`, `L10_LetreroVivaCoin.png`, `V09_Taxi.png`

```
The FIRST attached image is the target look of the whole game: match its art style EXACTLY, as if it were another screenshot of the same game: the same illustrated painted rendering, the same blue night lighting with fog, the same wet reflective ground, the same house shapes, materials, colors and level of detail, and the same thin minimalist HUD typography and line style. The second image is the casino sign and the third is the city taxi: include both in the scene, redrawn in the style of the first image. Scene: the Neon Alto district at night: a street intersection with the VivaCoin casino on the corner, its big gold and magenta sign glowing, buildings of 4 to 6 floors with big glowing screens with abstract images, much more magenta and turquoise neon than the poor neighborhood, two pink and white taxis on the street, a few people on the sidewalks. Short city blocks: the next street corner is visible ahead, never an endless corridor. Thick cables strung between utility poles, puddles reflecting the lights. Minimalist HUD on top, thin lines, thin clean sans-serif text, NO panels, NO boxes, NO frames: top-left a circular minimap made of thin lines with a small mission objective in thin text below it; top-right the money "$ 12,450" and a row of turquoise wanted-level stars; bottom-left two thin bars labeled "VIDA" (red-pink) and "ESCUDO" (turquoise); bottom-right a small cyan weapon hologram of a pistol with the ammo "12 / 48". The player character is seen from behind in third person like GTA, centered at the bottom of the screen, about one quarter of the screen height. All HUD text in Spanish, spelled EXACTLY as written, no other HUD text. The mission objective under the minimap reads "Entra al casino".
```

## K04 — K04_Barrio_Dia
- **Salida:** `imageassets/referencias/look/K04_Barrio_Dia.png` · 16:9 · **Adjuntar:** `HUD_A_minimalista.png`

```
Keep the same street, houses, camera, character and composition as the attached image, in the same illustrated art style, but change the time to a late sunny afternoon: warm low sun, long soft shadows across the street, a blue sky with a few white clouds, no fog, all neon signs and lights turned off, the ground dry with only one small puddle. Same HUD as the attached image, in the new layout: Minimalist HUD on top, thin lines, thin clean sans-serif text, NO panels, NO boxes, NO frames: top-left a circular minimap made of thin lines with a small mission objective in thin text below it; top-right the money "$ 12,450" and a row of turquoise wanted-level stars; bottom-left two thin bars labeled "VIDA" (red-pink) and "ESCUDO" (turquoise); bottom-right a small cyan weapon hologram of a pistol with the ammo "12 / 48". The player character is seen from behind in third person like GTA, centered at the bottom of the screen, about one quarter of the screen height. All HUD text in Spanish, spelled EXACTLY as written, no other HUD text. The mission objective under the minimap reads "Ve con el Doc · 120 m".
```

## B01 — B01_Lineas
- **Salida:** `imageassets/ui/barras/B01_Lineas.png` · 16:9 · **Adjuntar:** `HUD_A_minimalista.png`

```
Show only the bottom-left corner of the game screen: a 16:9 close crop of the bottom-left part of the street from the attached image (wet ground, fog, lights, same illustrated style), with only the health and shield HUD element in the bottom-left, large enough to see its design clearly, no other HUD elements, no character. Same thin minimalist style as the HUD in the attached image, no panels, no boxes. Health is red-pink and labeled "VIDA", shield is turquoise and labeled "ESCUDO"; all text spelled exactly as written. Design: two long thin horizontal lines, the health line on top of the shield line, each with its small thin label "VIDA" or "ESCUDO" just above its left end, the health line 70% full and the shield line 40% full, the empty part as a faint dim line.
```

## B02 — B02_Segmentos
- **Salida:** `imageassets/ui/barras/B02_Segmentos.png` · 16:9 · **Adjuntar:** `HUD_A_minimalista.png`

```
Show only the bottom-left corner of the game screen: a 16:9 close crop of the bottom-left part of the street from the attached image (wet ground, fog, lights, same illustrated style), with only the health and shield HUD element in the bottom-left, large enough to see its design clearly, no other HUD elements, no character. Same thin minimalist style as the HUD in the attached image, no panels, no boxes. Health is red-pink and labeled "VIDA", shield is turquoise and labeled "ESCUDO"; all text spelled exactly as written. Design: two bars made of 10 short separate segments each (each segment is 10 points), health with 7 lit segments and shield with 4 lit segments, unlit segments dim, a thin number next to each bar ("70" and "40") and small labels "VIDA" and "ESCUDO".
```

## B03 — B03_Arco
- **Salida:** `imageassets/ui/barras/B03_Arco.png` · 16:9 · **Adjuntar:** `HUD_A_minimalista.png`

```
Show only the bottom-left corner of the game screen: a 16:9 close crop of the bottom-left part of the street from the attached image (wet ground, fog, lights, same illustrated style), with only the health and shield HUD element in the bottom-left, large enough to see its design clearly, no other HUD elements, no character. Same thin minimalist style as the HUD in the attached image, no panels, no boxes. Health is red-pink and labeled "VIDA", shield is turquoise and labeled "ESCUDO"; all text spelled exactly as written. Design: a small circular icon with a thin outline of the player's head silhouette, surrounded by two thin concentric arcs: the inner red-pink health arc 70% full and the outer turquoise shield arc 40% full, small labels "VIDA" and "ESCUDO" next to the arcs.
```

## B04 — B04_Diagonal
- **Salida:** `imageassets/ui/barras/B04_Diagonal.png` · 16:9 · **Adjuntar:** `HUD_A_minimalista.png`

```
Show only the bottom-left corner of the game screen: a 16:9 close crop of the bottom-left part of the street from the attached image (wet ground, fog, lights, same illustrated style), with only the health and shield HUD element in the bottom-left, large enough to see its design clearly, no other HUD elements, no character. Same thin minimalist style as the HUD in the attached image, no panels, no boxes. Health is red-pink and labeled "VIDA", shield is turquoise and labeled "ESCUDO"; all text spelled exactly as written. Design: two slanted bars leaning slightly to the right, with both ends cut on a diagonal, health 70% full and shield 40% full, a big thin number next to each bar ("70" and "40") and small labels "VIDA" and "ESCUDO".
```

## M10 — M10_MapaCiudad
- **Salida:** `imageassets/referencias/look/M10_MapaCiudad.png` · 16:9 · **Adjuntar:** `HUD_A_minimalista.png`

```
The FIRST attached image is the target look of the whole game: match its art style EXACTLY, as if it were another screenshot of the same game: the same illustrated painted rendering, the same blue night lighting with fog, the same wet reflective ground, the same house shapes, materials, colors and level of detail, and the same thin minimalist typography. Draw the full-screen city map of the game, seen from directly above, illustrated like the pause-menu map of a GTA-style game, at night colors with the same fog-blue mood. A compact city of short blocks (about 60 by 80 meters each) with streets that bend, small plazas, one roundabout, short alleys, and an elevated highway along one edge. Four districts tinted in different colors: bottom-left SUBURBIOS in orange (small low houses), bottom-right EL MERCADO in yellow (market stalls and a plaza), top-right NEÓN ALTO in magenta (the casino and tall buildings), top-left NOVA in cold blue (towers and a refinery). Each district name written once in thin capital letters inside its area. Small round map icons with a thin label each, spelled exactly: "Casa", "I+D+I", "CIC", "Taller El Canepa", "Tianguis", "Mercado de armas", "Casino VivaCoin", "Torre Nova". No other text anywhere, no legend, no compass text.
```


# Parte 18 — Pedido 07-nov b: HUD final, esquina, calle Nova y mapa corregido (ref. K01, sin lluvia)

Lote: `lote_07nov_b.json` · script: `generar_07nov_b.ps1` (fase 1: H10, K05, M11; fase 2: K06, que adjunta H10).

## H10 — H10_HUD (fase 1)
- **Salida:** `imageassets/referencias/look/H10_HUD.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
Keep the attached image EXACTLY the same: the same street, houses, lights, character, camera and composition, change nothing in the scene, and keep it without rain. Remove the old HUD completely and draw only this new HUD on top: HUD with an original design of our own (do not copy graphics, fonts or icons from any existing game), thin lines, thin clean sans-serif text, NO background panels, NO boxes, all text in Spanish spelled EXACTLY as written and no other HUD text: TOP-LEFT: a short thin turquoise shield bar labeled "ESCUDO"; below it a long thin red-pink health bar labeled "VIDA" with the number "143 / 204"; below that a row of 5 small turquoise wanted stars with 3 lit, and the money "$ 12,450". TOP-RIGHT: a SQUARE minimap drawn with thin lines showing streets and a dotted route line, and below it the objective text "Ve con el Doc · 120 m". BOTTOM-LEFT: 3 small square item slots drawn with thin outlines: an energy drink can, a bag of chips and a bandage, each with its key "[1]", "[2]", "[3]" and a small quantity "x2", "x1", "x3". BOTTOM-RIGHT: a cyan hologram of the pistol seen from the side, the big thin ammo count "12 | 48", a small body silhouette icon next to it, and above them two small button hints "[R] Recargar" and "[F] Golpe". 
```

## K05 — K05_EsquinaBarrio (fase 1)
- **Salida:** `imageassets/referencias/look/K05_EsquinaBarrio.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
The FIRST attached image is the target look of the whole game: match its art style EXACTLY, as if it were another screenshot of the same game: the same illustrated painted rendering, the same blue night lighting with soft fog, the same wet reflective ground, the same house shapes, materials, colors and level of detail. NO rain, no falling raindrops, no rain streaks; the ground is wet with puddles but it is not raining. Scene with NO HUD, no interface, no text overlays, no player character: an intersection of two streets of the same poor neighborhood at night, seen from one corner at eye height. On the four corners, two-story houses painted in different faded colors with small businesses on the ground floor: a corner shop with a rolled-down metal shutter, a taco stand with a glowing grill and stools, a small pharmacy with a green glowing cross. Utility poles with orange street lamps and thick cables crossing diagonally over the intersection, a traffic light, a street name sign on a pole with no readable text, painted zebra crosswalks on the asphalt, puddles reflecting the lights, a few parked old cars. The next block clearly starts about 70 meters away down each street (short blocks, never an endless corridor).
```

## K06 — K06_CalleNova (fase 2)
- **Salida:** `imageassets/referencias/look/K06_CalleNova.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `H10_HUD.png`

```
The FIRST attached image is the target look of the whole game: match its art style EXACTLY, as if it were another screenshot of the same game: the same illustrated painted rendering, the same blue night lighting with soft fog, the same wet reflective ground, the same house shapes, materials, colors and level of detail. NO rain, no falling raindrops, no rain streaks; the ground is wet with puddles but it is not raining. The SECOND attached image shows the exact HUD design to draw on top: copy that HUD layout and style exactly. Scene: a wider, cleaner street of the district of the Nova corporation at night: office buildings of 6 to 10 floors with dark glass facades and cold white and blue light strips, wide clean sidewalks with metal bollards, security cameras on poles and walls, very few neon signs, cold blue fog, and at the end of the street the huge white Nova tower rising above everything with a bright light at its top. The player character seen from behind in third person, centered at the bottom, about one quarter of the screen height. Same HUD as the second image, with the objective "Llega a la torre Nova".
```

## M11 — M11_MapaCiudad (fase 1)
- **Salida:** `imageassets/referencias/look/M11_MapaCiudad.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `M10_MapaCiudad.png`

```
Use the FIRST attached image for the art style and the color mood of the game, and the SECOND attached image (the previous city map) for the illustrated top-down map style, but FIX its layout: there must be NO four square zones and NO district borders or boxes. Draw the full-screen city map of the game, seen from directly above, illustrated like the pause-menu map of a GTA-style game. One single continuous city of short blocks (about 60 by 80 meters each) across the whole map, streets that bend, short alleys, 2 or 3 small plazas, one roundabout, and an elevated highway along one edge. The neighborhoods are told apart ONLY by the color of their lights and the type of buildings, blending gradually into each other at the edges: Suburbios in warm orange (small low houses), El Mercado in yellow (one street of market stalls, no hanging bulbs), Neón Alto in magenta (the casino and taller buildings), Nova in cold blue (office towers and the big Nova tower, with only a small refinery in one corner, not half of the district). NO big district names written on the map. Small round map icons with a thin label each, spelled exactly and no other text anywhere: "Casa"; "Taller El Canepa" right across the street from Casa; "I+D+I" and "CIC" each one or two blocks from Casa, inside the orange Suburbios area; "Tianguis"; "Mercado de armas"; "Casino VivaCoin"; "Torre Nova". No legend, no compass text.
```


# Parte 16b — Implantes minimalistas (regenerados 07-nov): IMP01, IMP02, IMP03 → `_v2.png` (fase 9 de `lote_06nov_b.json`)

## IMP01 — imageassets/modelos-nuevos/IMP01_ExoPiernas_v2.png
- **Medida:** 1 m de alto (cadera a tobillo) · **Tripo:** HD · 10000 · 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. No text, no letters, no numbers, no logos. The attached characters show the style of the game's chrome and grey cyber parts. Subject: a MINIMALIST wearable leg exoskeleton implant shown on its own with nobody wearing it, standing upright as if worn by an invisible person: only a slim hip belt around the waist, and from each side of the belt one single straight rod running down the OUTSIDE of each leg to the ankle, two rods in total, each rod with one small round knee joint with a tiny turquoise glowing core and ending in a small ankle cuff. Nothing else: no thigh plates, no shin plates, no foot plates, no pistons, no armor. Gunmetal and chrome, light wear. The rods are slim-looking but solid and chunky enough for 3D (about the thickness of a thumb). Empty space where the legs would be. View: from the front with a slight angle.
```

## IMP02 — imageassets/modelos-nuevos/IMP02_ExoBrazos_v2.png
- **Medida:** 0.8 m de ancho (hombro a hombro con brazos) · **Tripo:** HD · 10000 · 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. No text, no letters, no numbers, no logos. The attached characters show the style of the game's chrome and grey cyber parts. Subject: a MINIMALIST wearable upper-body exoskeleton implant shown on its own with nobody wearing it, as if worn by an invisible person in a slight A-pose: a slim spine piece running down the back with a small turquoise power core, connected at the top to two small shoulder joints, and from each shoulder one single straight rod running along the OUTSIDE of each arm down to the wrist, with one small round elbow joint and a thin wrist cuff. Nothing else: no chest plate, no shoulder pads, no arm armor, no pistons. Gunmetal and chrome, light wear. The rods are slim-looking but solid and chunky enough for 3D (about the thickness of a thumb). Empty space where the body and arms would be. View: 3/4 view from the back left, so the spine piece and both arm rods are visible.
```

## IMP03 — imageassets/modelos-nuevos/IMP03_VisorCabeza_v2.png
- **Medida:** 6 cm de lente · 10 cm total · **Tripo:** HD · 8000 · 2K

```
Use the attached reference images ONLY for the art style, rendering, colors and level of wear; do not copy their scenes or shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures, no fine noise. Worn Mexican cyberpunk megacity, small turquoise and orange accents. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars, handles and straps are thick and chunky; cables are thick bundled conduits fixed to surfaces. No text, no letters, no numbers, no logos. The attached characters show the style of the game's chrome and grey cyber parts. Subject: a cybernetic monocle implant for ONE eye only, shown on its own: a single round lens with a glowing turquoise iris and a thin targeting ring, set in a chunky round chrome housing, attached to a small curved gunmetal piece that would rest on the temple and cheekbone beside one eye, with a small orange status light and a short thick connector going back toward the ear. Nothing covers the other eye: no band around the head, no visor. View: 3/4 view from the front left.
```


# Parte 19 — Logo CYBERCITY (6 opciones, fondo negro)

Lote: `lote_logo.json` · script: `generar_logo.ps1`.

## LG01 — LG01_Neon
- **Salida:** `imageassets/logo/LG01_Neon.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
Use the attached image ONLY for the art style and color palette of the game (illustrated painted look, blue night, magenta and turquoise neon, orange sodium light, Mexican cyberpunk neighborhood); do not copy the scene. Design a video game title logo. The logo text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter clear and legible, no extra letters, no other words, no tagline, no subtitle. The logo alone, centered, filling about 70% of the image width, on a PURE BLACK (#000000) flat background with nothing else around it: no scene, no frame, no gradient background, no watermark. A soft glow is allowed only right around the logo. Style: a real neon tube sign: the letters made of glowing bent glass tubes in two colors, "CYBER" in magenta and "CITY" in turquoise, thin dark metal mounting rails and small clips behind the tubes, one letter's tube flickering dimmer than the others, realistic neon glow halo.
```

## LG02 — LG02_Glitch
- **Salida:** `imageassets/logo/LG02_Glitch.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
Use the attached image ONLY for the art style and color palette of the game (illustrated painted look, blue night, magenta and turquoise neon, orange sodium light, Mexican cyberpunk neighborhood); do not copy the scene. Design a video game title logo. The logo text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter clear and legible, no extra letters, no other words, no tagline, no subtitle. The logo alone, centered, filling about 70% of the image width, on a PURE BLACK (#000000) flat background with nothing else around it: no scene, no frame, no gradient background, no watermark. A soft glow is allowed only right around the logo. Style: thick bold modern sans-serif capital letters in white, sliced by a few horizontal glitch cuts with the slices shifted sideways, cyan and magenta chromatic offset copies peeking out behind the letters, a few thin digital scan lines, energetic but the word stays perfectly readable.
```

## LG03 — LG03_Skyline
- **Salida:** `imageassets/logo/LG03_Skyline.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
Use the attached image ONLY for the art style and color palette of the game (illustrated painted look, blue night, magenta and turquoise neon, orange sodium light, Mexican cyberpunk neighborhood); do not copy the scene. Design a video game title logo. The logo text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter clear and legible, no extra letters, no other words, no tagline, no subtitle. The logo alone, centered, filling about 70% of the image width, on a PURE BLACK (#000000) flat background with nothing else around it: no scene, no frame, no gradient background, no watermark. A soft glow is allowed only right around the logo. Style: bold capital letters in warm off-white with a soft orange glow, and growing out of the top edge of the letters a dark silhouette of the city: small houses with water tanks on their roofs, utility poles with sagging cables between them, a few antennas, and one tall slim tower rising above the middle of the word with a small light at its top; a few tiny magenta and turquoise lit windows in the silhouette.
```

## LG04 — LG04_Barrio
- **Salida:** `imageassets/logo/LG04_Barrio.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
Use the attached image ONLY for the art style and color palette of the game (illustrated painted look, blue night, magenta and turquoise neon, orange sodium light, Mexican cyberpunk neighborhood); do not copy the scene. Design a video game title logo. The logo text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter clear and legible, no extra letters, no other words, no tagline, no subtitle. The logo alone, centered, filling about 70% of the image width, on a PURE BLACK (#000000) flat background with nothing else around it: no scene, no frame, no gradient background, no watermark. A soft glow is allowed only right around the logo. Style: Mexican hand-painted sign-painter (rotulista) lettering with bold rounded shadowed letters in bright colors (yellow, red and turquoise with a dark drop shadow), mixed with neon: a thin magenta neon tube outlining the word, and beside the word a short utility pole with a street lamp glowing orange and two cables running from it across the top of the logo.
```

## LG05 — LG05_Minimal
- **Salida:** `imageassets/logo/LG05_Minimal.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `H10_HUD.png`

```
Use the attached image ONLY for the art style and color palette of the game (illustrated painted look, blue night, magenta and turquoise neon, orange sodium light, Mexican cyberpunk neighborhood); do not copy the scene. Design a video game title logo. The logo text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter clear and legible, no extra letters, no other words, no tagline, no subtitle. The logo alone, centered, filling about 70% of the image width, on a PURE BLACK (#000000) flat background with nothing else around it: no scene, no frame, no gradient background, no watermark. A soft glow is allowed only right around the logo. Style: (the second attached image shows the game's HUD: copy its thin typography style) a thin, clean geometric sans-serif in white with wide letter spacing, very elegant and minimal, with a single thin horizontal line of turquoise light passing under the word and fading out at both ends.
```

## LG06 — LG06_Emblema
- **Salida:** `imageassets/logo/LG06_Emblema.png` · 1:1 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
Use the attached image ONLY for the art style and color palette of the game (illustrated painted look, blue night, magenta and turquoise neon, orange sodium light, Mexican cyberpunk neighborhood); do not copy the scene. Design a video game title logo. The logo text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter clear and legible, no extra letters, no other words, no tagline, no subtitle. The logo alone, centered, filling about 70% of the image width, on a PURE BLACK (#000000) flat background with nothing else around it: no scene, no frame, no gradient background, no watermark. A soft glow is allowed only right around the logo. Style: a game emblem badge: a bold angular shield shape with a dark metal frame and turquoise and magenta edge lights, inside the shield a large stylized monogram of the two letters "CC" interlocked, and below the shield the word "CYBERCITY" in bold clean capital letters. The only letters allowed are the monogram "CC" and the word "CYBERCITY". Works as an app icon.
```


# Parte 19b — Logo CYBERCITY ronda 2 (variantes de LG05)

Lote: `lote_logo2.json` · script: `generar_logo2.ps1` · adjuntar `LG05_Minimal.png` en todas.

## LG07 — LG07_DosTonos

```
Use the attached logo as the starting point and keep its base exactly: the same thin, clean, geometric, modern sans-serif capital letters with wide letter spacing, the same soft white glow, the same proportions and placement, centered on a PURE BLACK (#000000) flat background with nothing else. The text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter clear and perfectly legible, no extra letters, no other words, no tagline. Only change this: two tones: the letters "CYBER" in white and the letters "CITY" in turquoise with a soft turquoise glow, the same thin weight for all letters, and the thin turquoise light line appears only under "CITY", not under "CYBER".
```

## LG08 — LG08_Cortes

```
Use the attached logo as the starting point and keep its base exactly: the same thin, clean, geometric, modern sans-serif capital letters with wide letter spacing, the same soft white glow, the same proportions and placement, centered on a PURE BLACK (#000000) flat background with nothing else. The text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter clear and perfectly legible, no extra letters, no other words, no tagline. Only change this: add a few fine precision cuts to the letters, like a tech brand wordmark: a thin diagonal cut through each C, the Y drawn with its two arms not touching the stem (an open Y), and the middle bar of the E separated from its spine by a small gap. Keep only a few cuts so it stays clean and readable. Keep the thin turquoise line below.
```

## LG09 — LG09_Skyline

```
Use the attached logo as the starting point and keep its base exactly: the same thin, clean, geometric, modern sans-serif capital letters with wide letter spacing, the same soft white glow, the same proportions and placement, centered on a PURE BLACK (#000000) flat background with nothing else. The text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter clear and perfectly legible, no extra letters, no other words, no tagline. Only change this: the thin turquoise line under the word turns into a minimal city skyline drawn with a single very thin 1 pixel turquoise line: low houses with small water tanks on their roofs, utility poles with sagging cables between them, and one tall slim tower rising near the middle with a tiny light at its top, the line fading out at both ends.
```

## LG10 — LG10_Glitch

```
Use the attached logo as the starting point and keep its base exactly: the same thin, clean, geometric, modern sans-serif capital letters with wide letter spacing, the same soft white glow, the same proportions and placement, centered on a PURE BLACK (#000000) flat background with nothing else. The text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter clear and perfectly legible, no extra letters, no other words, no tagline. Only change this: a very subtle glitch only: one thin horizontal slice through the word shifted a little to the side, and a slight magenta and cyan color offset on only two or three letters. Everything else stays exactly like the attached logo, including the thin turquoise line below.
```

## LG11 — LG11_Peso

```
Use the attached logo as the starting point and keep its base exactly: the same thin, clean, geometric, modern sans-serif capital letters with wide letter spacing, the same soft white glow, the same proportions and placement, centered on a PURE BLACK (#000000) flat background with nothing else. The text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter clear and perfectly legible, no extra letters, no other words, no tagline. Only change this: contrast of weights in a single line: "CYBER" stays in the same thin weight in white, and "CITY" becomes a bold heavy weight of the same geometric typeface in white, with a turquoise glow only around "CITY". Keep the thin turquoise line below the whole word.
```

## LG12 — LG12_Firma

```
Use the attached logo as the starting point and keep its base exactly: the same thin, clean, geometric, modern sans-serif capital letters with wide letter spacing, the same soft white glow, the same proportions and placement, centered on a PURE BLACK (#000000) flat background with nothing else. The text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter clear and perfectly legible, no extra letters, no other words, no tagline. Only change this: one signature detail: the letter I in "CITY" becomes a slim street light pole drawn in the same thin white line, with a small lamp head curving over the top that glows warm orange and casts a soft orange glow; the I must still read clearly as the letter I. Keep the thin turquoise line below.
```


# Parte 20 — Pedidos 08-nov y 08-nov b (HUD por piezas, cuarto, pasarela Nova, personajes, calle, Nova)

Lote: `lote_08nov.json` · script: `generar_08nov.ps1` (fases 1–5).

## H20 — H20_BarrasVida (fase 1)
- **Salida:** `imageassets/ui/hud/H20_BarrasVida.png` · 16:9 · **Adjuntar:** `H10_HUD.png`, `K01_Barrio_HUDnuevo.png`

```
The FIRST attached image shows the game's HUD: match its thin minimalist line style, thin clean typography and colors exactly; the second image is the game's overall look. Original design of our own: do not copy graphics, fonts or icons from any existing game. Draw ONLY the single UI element described, flat, straight from the front, no perspective, centered with empty space around it, on a PURE BLACK (#000000) flat background, nothing else in the image, no scene, no other HUD elements. Thin lines, crisp edges, soft glow only on lit parts. Element: a health and shield block, with NO words (no "VIDA", no "ESCUDO", no labels): on the left a small original emblem icon (a minimal angular hexagon with a stylized heart-pulse line inside, thin turquoise line); to its right, on top, a short thin shield bar; below it, a long thin health bar with a thin frame, and the number "143" in thin digits right after the health bar. Both bars completely FULL: the shield bar filled in glowing turquoise, the health bar filled in glowing red-pink.
```

## H21 — H21_BarrasVacias (fase 1)
- **Salida:** `imageassets/ui/hud/H21_BarrasVacias.png` · 16:9 · **Adjuntar:** `H10_HUD.png`, `K01_Barrio_HUDnuevo.png`

```
The FIRST attached image shows the game's HUD: match its thin minimalist line style, thin clean typography and colors exactly; the second image is the game's overall look. Original design of our own: do not copy graphics, fonts or icons from any existing game. Draw ONLY the single UI element described, flat, straight from the front, no perspective, centered with empty space around it, on a PURE BLACK (#000000) flat background, nothing else in the image, no scene, no other HUD elements. Thin lines, crisp edges, soft glow only on lit parts. Element: a health and shield block, with NO words (no "VIDA", no "ESCUDO", no labels): on the left a small original emblem icon (a minimal angular hexagon with a stylized heart-pulse line inside, thin turquoise line); to its right, on top, a short thin shield bar; below it, a long thin health bar with a thin frame, and the number "143" in thin digits right after the health bar. Both bars completely EMPTY: only their thin frames and a very dark dim inner track are visible, no fill color, so they can be filled in later; the emblem and the number stay the same.
```

## H22 — H22_MarcoMinimapa (fase 1)
- **Salida:** `imageassets/ui/hud/H22_MarcoMinimapa.png` · 1:1 · **Adjuntar:** `H10_HUD.png`, `K01_Barrio_HUDnuevo.png`

```
The FIRST attached image shows the game's HUD: match its thin minimalist line style, thin clean typography and colors exactly; the second image is the game's overall look. Original design of our own: do not copy graphics, fonts or icons from any existing game. Draw ONLY the single UI element described, flat, straight from the front, no perspective, centered with empty space around it, on a PURE BLACK (#000000) flat background, nothing else in the image, no scene, no other HUD elements. Thin lines, crisp edges, soft glow only on lit parts. Element: a SQUARE minimap frame made of a single thin turquoise line, with slightly thicker L-shaped corner brackets marking the four corners and a tiny north tick at the top center; the inside of the square is completely empty pure black.
```

## H23 — H23_IconosObjetos (fase 1)
- **Salida:** `imageassets/ui/hud/H23_IconosObjetos.png` · 3:2 · **Adjuntar:** `H10_HUD.png`, `K01_Barrio_HUDnuevo.png`

```
The FIRST attached image shows the game's HUD: match its thin minimalist line style, thin clean typography and colors exactly; the second image is the game's overall look. Original design of our own: do not copy graphics, fonts or icons from any existing game. Draw ONLY the single UI element described, flat, straight from the front, no perspective, centered with empty space around it, on a PURE BLACK (#000000) flat background, nothing else in the image, no scene, no other HUD elements. Thin lines, crisp edges, soft glow only on lit parts. Element: a sheet of 6 separate item icons arranged in a clean grid of 3 columns and 2 rows with plenty of black space between them, each icon drawn in a single thin turquoise line, same line weight, same size, no text: an energy drink can, a bag of chips, a rolled bandage, a coffee cup with steam, a battery, a hand grenade.
```

## H24 — H24_Munequito (fase 1)
- **Salida:** `imageassets/ui/hud/H24_Munequito.png` · 3:4 · **Adjuntar:** `H10_HUD.png`, `K01_Barrio_HUDnuevo.png`

```
The FIRST attached image shows the game's HUD: match its thin minimalist line style, thin clean typography and colors exactly; the second image is the game's overall look. Original design of our own: do not copy graphics, fonts or icons from any existing game. Draw ONLY the single UI element described, flat, straight from the front, no perspective, centered with empty space around it, on a PURE BLACK (#000000) flat background, nothing else in the image, no scene, no other HUD elements. Thin lines, crisp edges, soft glow only on lit parts. Element: a full-body human silhouette seen from the front, standing straight with arms slightly away from the body, drawn as a thin cyan outline, divided into separate outlined parts with tiny gaps between them: head, torso, left arm, right arm, left leg, right leg, so each part can be colored separately. No face details, no text.
```

## H25 — H25_Estrella (fase 1)
- **Salida:** `imageassets/ui/hud/H25_Estrella.png` · 16:9 · **Adjuntar:** `H10_HUD.png`, `K01_Barrio_HUDnuevo.png`

```
The FIRST attached image shows the game's HUD: match its thin minimalist line style, thin clean typography and colors exactly; the second image is the game's overall look. Original design of our own: do not copy graphics, fonts or icons from any existing game. Draw ONLY the single UI element described, flat, straight from the front, no perspective, centered with empty space around it, on a PURE BLACK (#000000) flat background, nothing else in the image, no scene, no other HUD elements. Thin lines, crisp edges, soft glow only on lit parts. Element: two wanted-level stars side by side with space between them, same size, simple clean five-pointed star with slightly sharp geometric points: on the left a FILLED glowing turquoise star, on the right an EMPTY star drawn only as a thin turquoise outline.
```

## H26 — H26_CasillaObjeto (fase 1)
- **Salida:** `imageassets/ui/hud/H26_CasillaObjeto.png` · 1:1 · **Adjuntar:** `H10_HUD.png`, `K01_Barrio_HUDnuevo.png`

```
The FIRST attached image shows the game's HUD: match its thin minimalist line style, thin clean typography and colors exactly; the second image is the game's overall look. Original design of our own: do not copy graphics, fonts or icons from any existing game. Draw ONLY the single UI element described, flat, straight from the front, no perspective, centered with empty space around it, on a PURE BLACK (#000000) flat background, nothing else in the image, no scene, no other HUD elements. Thin lines, crisp edges, soft glow only on lit parts. Element: one empty square item slot drawn with a thin turquoise outline and slightly thicker corner marks, with a small separate square tab attached at its top-left corner where the key number will go; the slot and the tab are empty, no icon, no text, no numbers.
```

## R10 — R10_Cuarto (fase 2)
- **Salida:** `imageassets/referencias/look/R10_Cuarto.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `habitacion.png`

```
The FIRST attached image is the target look of the game: match its illustrated painted art style, blue night light, neon colors, materials and level of detail EXACTLY. The second image is the old version of this room: use it only as a content idea, redesign it. No rain, no people, no text, no logos. Scene: the player's bedroom in a poor Mexican cyberpunk neighborhood, larger than before (about 5 by 4 meters), seen from the doorway at eye height: a single bed with a messy blanket, a desk with an old chunky computer with a glowing screen, a worn wardrobe, a window with security bars letting in magenta and turquoise neon light from the street, a full-length mirror on the wall, a few posters with abstract designs, clothes on a chair and on the floor, a backpack, and the room door visible at one side. A bare hanging bulb with warm light mixing with the cold neon.
```

## K07 — K07_PuenteNova (fase 2)
- **Salida:** `imageassets/referencias/look/K07_PuenteNova.png` · 16:9 · **Adjuntar:** `K06_CalleNova.png`, `K01_Barrio_HUDnuevo.png`

```
Match EXACTLY the art style and the look of the Nova district in the FIRST attached image (same buildings, cold white and blue lights, blue fog, illustrated painted style); the second image is the game's overall look. No rain, no HUD, no text, no logos. Scene: the same kind of wide clean Nova street at night, with a pedestrian footbridge crossing over the street from one sidewalk to the other: on each sidewalk a straight staircase climbs up to the bridge deck, the deck spans the whole street at about 5 meters high so vehicles pass under it, with solid side railings and cold white light strips along the railings, a few people walking on it. It must clearly connect both sidewalks. The huge white Nova tower in the far background.
```

## PJ01 — PJ01_Protagonista (fase 3)
- **Salida:** `imageassets/modelos-nuevos/PJ01_Protagonista.png` · 1:1 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `BaseHombre_frente.png`
- **Medida:** 1.75 m de alto · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the FIRST attached image for the art style of the game (illustrated painted look, colors, level of detail) and the SECOND attached image only for the body proportions, the character sheet framing and the T-pose. Draw a new, original character. Rig-friendly design: clothes fitted close to the body, nothing hanging loose, no capes, no long dangling straps or chains. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms facing down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: the young male protagonist, about 19 years old, Mexican, medium brown skin, medium-short curly black hair on top with a taper fade on the sides, two thin shaved lines on the RIGHT side of the head, confident neutral face. A fitted sleeveless dark grey shirt with a thin turquoise trim, slightly toned arms, olive cargo pants with flat pockets, chunky sneakers in white and orange.
```

## AL10 — AL10_Aliado1 (fase 3)
- **Salida:** `imageassets/modelos-nuevos/AL10_Aliado1.png` · 1:1 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `BaseHombre_frente.png`
- **Medida:** 1.72 m de alto · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the FIRST attached image for the art style of the game (illustrated painted look, colors, level of detail) and the SECOND attached image only for the body proportions, the character sheet framing and the T-pose. Draw a new, original character. Rig-friendly design: clothes fitted close to the body, nothing hanging loose, no capes, no long dangling straps or chains. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms facing down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a male neighborhood ally, about 20 years old, Mexican, a fitted hoodie with the hood DOWN, a padded vest over it zipped closed, a cap worn forward, fitted joggers, sneakers, a small turquoise LED detail on the vest.
```

## AL11 — AL11_Aliado2 (fase 3)
- **Salida:** `imageassets/modelos-nuevos/AL11_Aliado2.png` · 1:1 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `BaseMujer_frente.png`
- **Medida:** 1.65 m de alto · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the FIRST attached image for the art style of the game (illustrated painted look, colors, level of detail) and the SECOND attached image only for the body proportions, the character sheet framing and the T-pose. Draw a new, original character. Rig-friendly design: clothes fitted close to the body, nothing hanging loose, no capes, no long dangling straps or chains. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms facing down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a female neighborhood ally, about 20 years old, Mexican, two braids tied close to the back of the head ending at the shoulders (not loose), a short cropped jacket fitted and zipped, a fitted top, fitted pants, sturdy boots, small orange earring studs.
```

## AL12 — AL12_Aliado3 (fase 3)
- **Salida:** `imageassets/modelos-nuevos/AL12_Aliado3.png` · 1:1 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `BaseHombre_frente.png`
- **Medida:** 1.85 m de alto · corpulento · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the FIRST attached image for the art style of the game (illustrated painted look, colors, level of detail) and the SECOND attached image only for the body proportions, the character sheet framing and the T-pose. Draw a new, original character. Rig-friendly design: clothes fitted close to the body, nothing hanging loose, no capes, no long dangling straps or chains. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms facing down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: an older, big and heavy male ally, about 50 years old, Mexican, short grey hair and a thick moustache, a plain fitted t-shirt, a simple chunky robotic left arm in gunmetal with an orange light at the elbow, a fitted leather workshop apron tied close to the body, work pants, work boots.
```

## TK01 — TK01_BaseTinaco (fase 4)
- **Salida:** `imageassets/modelos-nuevos/TK01_BaseTinaco.png` · 4:3 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** 1.5 × 1.9 m · 0.7 m de alto · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, worn Mexican cyberpunk neighborhood); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. No text, no letters, no numbers, no logos. Subject: a rooftop water tank base: a low rectangular platform made of worn red bricks with chipped mortar and a few patches of grey cement, flat top, one thick grey PVC pipe coming out of one side and running down. No tank on it. View: 3/4 view from the front left, slightly from above.
```

## NE01a — NE01_NeonBandera_magenta (fase 4)
- **Salida:** `imageassets/modelos-nuevos/NE01_NeonBandera_magenta.png` · 3:4 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** 0.6 × 0.8 m · fondo 0.15 m · **Tripo:** HD · 8000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, worn Mexican cyberpunk neighborhood); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. No text, no letters, no numbers, no logos. Subject: a small projecting blade sign that sticks out from a wall: a black metal box with a thick mounting bracket on its back edge, on its face a glowing MAGENTA neon tube forming a simple abstract design (a wave line and a lightning bolt), no letters. View: 3/4 view from the front so both the face and the side bracket are visible.
```

## NE01b — NE01_NeonBandera_turquesa (fase 4)
- **Salida:** `imageassets/modelos-nuevos/NE01_NeonBandera_turquesa.png` · 3:4 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** 0.6 × 0.8 m · fondo 0.15 m · **Tripo:** HD · 8000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, worn Mexican cyberpunk neighborhood); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. No text, no letters, no numbers, no logos. Subject: a small projecting blade sign that sticks out from a wall: a black metal box with a thick mounting bracket on its back edge, on its face a glowing TURQUOISE neon tube forming a simple abstract design (a wave line and a lightning bolt), no letters. View: 3/4 view from the front so both the face and the side bracket are visible.
```

## NE02 — NE02_NeonPared (fase 4)
- **Salida:** `imageassets/modelos-nuevos/NE02_NeonPared.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** 0.9 × 0.5 m · fondo 0.08 m · **Tripo:** HD · 8000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, worn Mexican cyberpunk neighborhood); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. No text, no letters, no numbers, no logos. Subject: a flat rectangular wall neon sign: a thin black backing plate with small mounting clips, a glowing turquoise neon tube drawing three simple icons side by side (a coffee cup, a light bulb, an eye), no letters. The back is flat. View: from the front with a slight angle.
```

## AL01 — AL01_Alcantarilla (fase 4)
- **Salida:** `imageassets/modelos-nuevos/AL01_Alcantarilla.png` · 4:3 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** 0.9 × 0.4 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, worn Mexican cyberpunk neighborhood); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. No text, no letters, no numbers, no logos. Subject: a rectangular cast iron storm drain grate set into a small piece of sidewalk next to the curb edge: thick parallel iron bars, a heavy iron frame, rust and dirt in the slots, a bit of the concrete curb. View: 3/4 view from above.
```

## BS01 — BS01_Basura (fase 4)
- **Salida:** `imageassets/modelos-nuevos/BS01_Basura.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** piezas de 5 a 30 cm · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, worn Mexican cyberpunk neighborhood); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. A small set of separate loose objects, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. No text, no letters, no numbers, no logos. Subject: a set of small street litter pieces laid out separately with space between them: three crumpled paper balls, one crushed soda can, one paper cup lying on its side, a flattened cardboard piece and a few dry leaves. View: 3/4 view from above.
```

## MT01 — MT01_MuroTrasero (fase 4)
- **Salida:** `imageassets/modelos-nuevos/MT01_MuroTrasero.png` · 3:4 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** 4 m ancho × 6 m alto · fondo 0.3 m · **Tripo:** Smart Mesh P2 · 10000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, worn Mexican cyberpunk neighborhood); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. No text, no letters, no numbers, no logos. Subject: the back wall of a house facing an alley: a flat wall section with dirty cracked plaster in a faded color, a small window with security bars, two thick drain pipes running down, an electricity meter box with a thick cable, a caged bulb glowing warm orange, stains and grime. The back is flat. View: straight from the front with a very slight angle.
```

## TL01 — TL01_Toldo (fase 4)
- **Salida:** `imageassets/modelos-nuevos/TL01_Toldo.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** 2.5 m ancho · sale 1 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, worn Mexican cyberpunk neighborhood); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. No text, no letters, no numbers, no logos. Subject: a small awning for a house doorway: a sloped sheet of corrugated metal painted yellow, worn and slightly dented, held by two thick angled metal brackets fixed to a flat wall plate. The back is flat to fix to a wall. View: 3/4 view from the front left, slightly from above.
```

## ES01 — ES01_Semaforo (fase 4)
- **Salida:** `imageassets/modelos-nuevos/ES01_Semaforo.png` · 3:4 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** 5 m de alto · brazo de 3 m · **Tripo:** HD · 10000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, worn Mexican cyberpunk neighborhood); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. No text, no letters, no numbers, no logos. Subject: a Mexican street corner traffic light: a thick grey metal pole on a concrete base, a horizontal arm reaching out with a black traffic light box with three lights (red lit, amber and green dim), and a blank white street name sign plate on the pole with NO text. View: 3/4 view from the front.
```

## FA01 — FA01_Farmacia (fase 4)
- **Salida:** `imageassets/modelos-nuevos/FA01_Farmacia.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** 2.4 × 0.7 m · fondo 0.2 m · **Tripo:** HD · 10000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, worn Mexican cyberpunk neighborhood); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. The text must be spelled EXACTLY as written, letter by letter, and no other text. Subject: a pharmacy neon sign for a street corner: a dark metal sign box with the word "FARMACIA" in glowing white neon letters and, on the left, a big glowing green neon cross. The back has two thick wall brackets. View: from the front with a slight angle.
```

## NV01 — NV01_FachadaOficinas (fase 5)
- **Salida:** `imageassets/modelos-nuevos/NV01_FachadaOficinas.png` · 16:9 · **Adjuntar:** `K06_CalleNova.png`, `K01_Barrio_HUDnuevo.png`
- **Medida:** 6 m ancho × 3.5 m alto · fondo 0.4 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, the clean corporate Nova district); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. No text, no letters, no numbers, no logos. Subject: a stackable facade module of an office building, one floor tall: a flat wall section with a row of dark glass panels in a grey metal frame, a thin cold white light strip running along the top edge, the top and bottom edges perfectly flat and straight so modules stack, the side edges flat so modules sit side by side. Very clean, almost no wear. View: from the front with a slight 3/4 angle.
```

## NV02 — NV02_PlantaBajaNova (fase 5)
- **Salida:** `imageassets/modelos-nuevos/NV02_PlantaBajaNova.png` · 16:9 · **Adjuntar:** `K06_CalleNova.png`, `K01_Barrio_HUDnuevo.png`
- **Medida:** 6 m ancho × 4 m alto · fondo 0.4 m · **Tripo:** Smart Mesh P2 · 10000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, the clean corporate Nova district); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. The only text allowed is "NOVA", spelled exactly like that. Subject: the ground floor facade module of a Nova office: a tall glass entrance door in the center with a grey metal frame, a glowing turquoise light outlining the door frame, dark glass panels on both sides, a small clean "NOVA" logo in thin white letters above the door, a thin cold white light strip on top. The top and side edges are flat so it lines up with the office facade modules above. View: from the front with a slight 3/4 angle.
```

## BL01 — BL01_Bolardo (fase 5)
- **Salida:** `imageassets/modelos-nuevos/BL01_Bolardo.png` · 3:4 · **Adjuntar:** `K06_CalleNova.png`, `K01_Barrio_HUDnuevo.png`
- **Medida:** 0.9 m alto · 0.2 m diámetro · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, the clean corporate Nova district); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. No text, no letters, no numbers, no logos. Subject: a steel bollard: a thick brushed steel cylinder with a rounded top and a reflective white band near the top, on a small round base plate. View: 3/4 view from the front.
```

## BT01 — BT01_BoteBasura (fase 5)
- **Salida:** `imageassets/modelos-nuevos/BT01_BoteBasura.png` · 3:4 · **Adjuntar:** `K06_CalleNova.png`, `K01_Barrio_HUDnuevo.png`
- **Medida:** 1 m alto · 0.5 m diámetro · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached image ONLY for the art style, materials, colors and level of wear of the game (illustrated painted look, the clean corporate Nova district); do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels and soft hand-painted gradients, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling: cables, bars and pipes are thick and chunky. No text, no letters, no numbers, no logos. Subject: a round metal public trash bin of a corporate sidewalk: a brushed steel cylinder with vertical slots, a domed lid with a front opening, a thin turquoise light ring under the lid, on a short base. View: 3/4 view from the front.
```


## PJ01b — PJ01_Protagonista_v2 (fase 8 de `lote_08nov.json`, edita PJ01_Protagonista.png)
- **Tripo:** Quads · 20,000 caras · 2K · rig Humanoid · 1.75 m

```
Edit the attached character image. Keep EXACTLY the same character: same face, same curly hair with taper fade, the two shaved lines on the RIGHT side of the head, same body, same art style, same strict T-pose, same framing and the same plain flat pure white background. Change ONLY these things: 1) a slightly LIGHTER skin tone (light-medium tan instead of medium brown), keeping the same face; 2) a small silver hoop earring in his RIGHT ear, the same side as the shaved lines; 3) the sleeveless shirt is now plain BLACK (fitted sleeveless shirt, no print, no logo); 4) BLACK cargo pants with a relaxed, loose straight fit (not tight, not skinny), with flat side pockets, the hems resting on the shoes; 5) white high-top retro basketball sneakers with a canvas-leather look, a white rubber sole and a bold BLUE curved stripe on the side of each shoe (an original stripe shape, not a real brand logo). No text, no brand logos. Nothing hanging loose.
```


# Parte 21 — Piezas separadas para Tripo (llantas, carrocerías y puertas, fachadas sin puerta, motos sin ruedas)

Lote: `lote_piezas.json` · script: `generar_piezas.ps1` · salida `imageassets/modelos-nuevos/piezas/`.

## RU01 — RU01_LlantaCarro (fase 1)
- **Medida:** 0.65 m de diámetro · 0.22 m de ancho · **Tripo:** HD · 6000 · 2K · **Adjuntar:** `V01a_CarroViejito.png`, `V05_PatrullaSedan.png`

```
Use the attached cars ONLY for the art style, wheel size and the look of their tires and rims; draw a generic wheel that fits all of them. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: ONE single car wheel on its own: a chunky dark rubber tire with simple tread and a simple plain steel rim in dark gunmetal with a few round holes and a center cap, light wear and dust. Show the same wheel twice side by side with space between them: on the left seen straight from the side (looking directly at the rim), on the right in a 3/4 view showing the tread and thickness.
```

## RU02 — RU02_LlantaMoto (fase 1)
- **Medida:** 0.55 m de diámetro · 0.12 m de ancho · **Tripo:** HD · 6000 · 2K · **Adjuntar:** `V07_MotoReparto.png`, `C19_Moto_v1.png`

```
Use the attached motorcycles ONLY for the art style, wheel size and look; draw a generic wheel that fits all of them. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: ONE single motorcycle wheel on its own: a chunky dark rubber tire with simple tread and a spoked rim with thick chunky spokes (about 12) and a small hub in dark gunmetal, light wear. Show the same wheel twice side by side with space between them: on the left seen straight from the side, on the right in a 3/4 view showing the tread and thickness.
```

## V01a-C — V01a_Carroceria (fase 2)
- **Medida:** 4.2 × 1.95 × 2.3 m (sin ruedas) · **Tripo:** HD · 18000 · 2K · **Adjuntar:** `V01a_CarroViejito.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same vehicle design, shape, proportions, paint colors, panels, patches, lights, details, wear and art style. Draw the same vehicle from the same 3/4 front left view, but as a body shell for a game with separate parts: 1) NO wheels: the four wheel arches are empty, with dark empty wheel wells and nothing inside; 2) NO front doors: both front door openings are empty holes with their door frames visible, showing the simple dark interior: the driver seat, the steering wheel and the dashboard; 3) NO glass at all: the windshield, side windows and rear window are open empty frames, no glass, no reflections. The rest of the vehicle stays identical (rear doors, if any, stay closed). Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## V01a-P — V01a_Puerta (fase 2)
- **Medida:** puerta delantera izq. (~1.1 × 1.1 m) · **Tripo:** Smart Mesh P2 · 6000 · 2K · **Adjuntar:** `V01a_CarroViejito.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same paint color, panels, patches, handle, wear and art style of its FRONT LEFT door. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: ONLY the front left door of this vehicle, removed and shown on its own, seen from the outside straight from the side, with a very slight angle so its thickness is visible: the outer door panel with its handle and its side mirror if the vehicle has one on that door, the window frame on top is empty and open (no glass), the inner edge shows a simple dark door frame thickness and hinge side. 
```

## V05-C — V05_Carroceria (fase 2)
- **Medida:** 4.6 m de largo (sin ruedas) · **Tripo:** HD · 18000 · 2K · **Adjuntar:** `V05_PatrullaSedan.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same vehicle design, shape, proportions, paint colors, panels, patches, lights, details, wear and art style. Draw the same vehicle from the same 3/4 front left view, but as a body shell for a game with separate parts: 1) NO wheels: the four wheel arches are empty, with dark empty wheel wells and nothing inside; 2) NO front doors: both front door openings are empty holes with their door frames visible, showing the simple dark interior: the driver seat, the steering wheel and the dashboard; 3) NO glass at all: the windshield, side windows and rear window are open empty frames, no glass, no reflections. The rest of the vehicle stays identical (rear doors, if any, stay closed). Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## V05-P — V05_Puerta (fase 2)
- **Medida:** puerta delantera izq. (~1.1 × 1.1 m) · **Tripo:** Smart Mesh P2 · 6000 · 2K · **Adjuntar:** `V05_PatrullaSedan.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same paint color, panels, patches, handle, wear and art style of its FRONT LEFT door. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: ONLY the front left door of this vehicle, removed and shown on its own, seen from the outside straight from the side, with a very slight angle so its thickness is visible: the outer door panel with its handle and its side mirror if the vehicle has one on that door, the window frame on top is empty and open (no glass), the inner edge shows a simple dark door frame thickness and hinge side. 
```

## V09-C — V09_Carroceria (fase 2)
- **Medida:** 4.2 m de largo (sin ruedas) · **Tripo:** HD · 18000 · 2K · **Adjuntar:** `V09_Taxi.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same vehicle design, shape, proportions, paint colors, panels, patches, lights, details, wear and art style. Draw the same vehicle from the same 3/4 front left view, but as a body shell for a game with separate parts: 1) NO wheels: the four wheel arches are empty, with dark empty wheel wells and nothing inside; 2) NO front doors: both front door openings are empty holes with their door frames visible, showing the simple dark interior: the driver seat, the steering wheel and the dashboard; 3) NO glass at all: the windshield, side windows and rear window are open empty frames, no glass, no reflections. The rest of the vehicle stays identical (rear doors, if any, stay closed). Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## V09-P — V09_Puerta (fase 2)
- **Medida:** puerta delantera izq. (~1.1 × 1.1 m) · **Tripo:** Smart Mesh P2 · 6000 · 2K · **Adjuntar:** `V09_Taxi.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same paint color, panels, patches, handle, wear and art style of its FRONT LEFT door. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: ONLY the front left door of this vehicle, removed and shown on its own, seen from the outside straight from the side, with a very slight angle so its thickness is visible: the outer door panel with its handle and its side mirror if the vehicle has one on that door, the window frame on top is empty and open (no glass), the inner edge shows a simple dark door frame thickness and hinge side. 
```

## V01b-C — V01b_Carroceria (fase 3)
- **Medida:** 4.5 × 1.5 × 2.4 m (sin ruedas) · **Tripo:** HD · 18000 · 2K · **Adjuntar:** `V01b_CarroPro.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same vehicle design, shape, proportions, paint colors, panels, patches, lights, details, wear and art style. Draw the same vehicle from the same 3/4 front left view, but as a body shell for a game with separate parts: 1) NO wheels: the four wheel arches are empty, with dark empty wheel wells and nothing inside; 2) NO front doors: both front door openings are empty holes with their door frames visible, showing the simple dark interior: the driver seat, the steering wheel and the dashboard; 3) NO glass at all: the windshield, side windows and rear window are open empty frames, no glass, no reflections. The rest of the vehicle stays identical (rear doors, if any, stay closed). Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## V01b-P — V01b_Puerta (fase 3)
- **Medida:** puerta delantera izq. (~1.1 × 1.1 m) · **Tripo:** Smart Mesh P2 · 6000 · 2K · **Adjuntar:** `V01b_CarroPro.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same paint color, panels, patches, handle, wear and art style of its FRONT LEFT door. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: ONLY the front left door of this vehicle, removed and shown on its own, seen from the outside straight from the side, with a very slight angle so its thickness is visible: the outer door panel with its handle and its side mirror if the vehicle has one on that door, the window frame on top is empty and open (no glass), the inner edge shows a simple dark door frame thickness and hinge side. 
```

## V02-C — V02_Carroceria (fase 3)
- **Medida:** 4.3 m de largo (sin ruedas) · **Tripo:** HD · 18000 · 2K · **Adjuntar:** `V02_SedanViejo.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same vehicle design, shape, proportions, paint colors, panels, patches, lights, details, wear and art style. Draw the same vehicle from the same 3/4 front left view, but as a body shell for a game with separate parts: 1) NO wheels: the four wheel arches are empty, with dark empty wheel wells and nothing inside; 2) NO front doors: both front door openings are empty holes with their door frames visible, showing the simple dark interior: the driver seat, the steering wheel and the dashboard; 3) NO glass at all: the windshield, side windows and rear window are open empty frames, no glass, no reflections. The rest of the vehicle stays identical (rear doors, if any, stay closed). Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## V02-P — V02_Puerta (fase 3)
- **Medida:** puerta delantera izq. (~1.1 × 1.1 m) · **Tripo:** Smart Mesh P2 · 6000 · 2K · **Adjuntar:** `V02_SedanViejo.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same paint color, panels, patches, handle, wear and art style of its FRONT LEFT door. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: ONLY the front left door of this vehicle, removed and shown on its own, seen from the outside straight from the side, with a very slight angle so its thickness is visible: the outer door panel with its handle and its side mirror if the vehicle has one on that door, the window frame on top is empty and open (no glass), the inner edge shows a simple dark door frame thickness and hinge side. 
```

## V03-C — V03_Carroceria (fase 3)
- **Medida:** 4.8 m de largo (sin ruedas) · **Tripo:** HD · 18000 · 2K · **Adjuntar:** `V03_PickupTrabajo.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same vehicle design, shape, proportions, paint colors, panels, patches, lights, details, wear and art style. Draw the same vehicle from the same 3/4 front left view, but as a body shell for a game with separate parts: 1) NO wheels: the four wheel arches are empty, with dark empty wheel wells and nothing inside; 2) NO front doors: both front door openings are empty holes with their door frames visible, showing the simple dark interior: the driver seat, the steering wheel and the dashboard; 3) NO glass at all: the windshield, side windows and rear window are open empty frames, no glass, no reflections. The rest of the vehicle stays identical (rear doors, if any, stay closed). Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## V03-P — V03_Puerta (fase 3)
- **Medida:** puerta delantera izq. (~1.1 × 1.1 m) · **Tripo:** Smart Mesh P2 · 6000 · 2K · **Adjuntar:** `V03_PickupTrabajo.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same paint color, panels, patches, handle, wear and art style of its FRONT LEFT door. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: ONLY the front left door of this vehicle, removed and shown on its own, seen from the outside straight from the side, with a very slight angle so its thickness is visible: the outer door panel with its handle and its side mirror if the vehicle has one on that door, the window frame on top is empty and open (no glass), the inner edge shows a simple dark door frame thickness and hinge side. 
```

## V04-C — V04_Carroceria (fase 3)
- **Medida:** 4.6 m de largo (sin ruedas) · **Tripo:** HD · 18000 · 2K · **Adjuntar:** `V04_CombiVieja.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same vehicle design, shape, proportions, paint colors, panels, patches, lights, details, wear and art style. Draw the same vehicle from the same 3/4 front left view, but as a body shell for a game with separate parts: 1) NO wheels: the four wheel arches are empty, with dark empty wheel wells and nothing inside; 2) NO front doors: both front door openings are empty holes with their door frames visible, showing the simple dark interior: the driver seat, the steering wheel and the dashboard; 3) NO glass at all: the windshield, side windows and rear window are open empty frames, no glass, no reflections. The rest of the vehicle stays identical (rear doors, if any, stay closed). Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## V04-P — V04_Puerta (fase 3)
- **Medida:** puerta delantera izq. (~1.1 × 1.1 m) · **Tripo:** Smart Mesh P2 · 6000 · 2K · **Adjuntar:** `V04_CombiVieja.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same paint color, panels, patches, handle, wear and art style of its FRONT LEFT door. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: ONLY the front left door of this vehicle, removed and shown on its own, seen from the outside straight from the side, with a very slight angle so its thickness is visible: the outer door panel with its handle and its side mirror if the vehicle has one on that door, the window frame on top is empty and open (no glass), the inner edge shows a simple dark door frame thickness and hinge side. 
```

## V06-C — V06_Carroceria (fase 3)
- **Medida:** 5.0 m de largo (sin ruedas) · **Tripo:** HD · 18000 · 2K · **Adjuntar:** `V06_PatrullaCamioneta.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same vehicle design, shape, proportions, paint colors, panels, patches, lights, details, wear and art style. Draw the same vehicle from the same 3/4 front left view, but as a body shell for a game with separate parts: 1) NO wheels: the four wheel arches are empty, with dark empty wheel wells and nothing inside; 2) NO front doors: both front door openings are empty holes with their door frames visible, showing the simple dark interior: the driver seat, the steering wheel and the dashboard; 3) NO glass at all: the windshield, side windows and rear window are open empty frames, no glass, no reflections. The rest of the vehicle stays identical (rear doors, if any, stay closed). Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## V06-P — V06_Puerta (fase 3)
- **Medida:** puerta delantera izq. (~1.1 × 1.1 m) · **Tripo:** Smart Mesh P2 · 6000 · 2K · **Adjuntar:** `V06_PatrullaCamioneta.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same paint color, panels, patches, handle, wear and art style of its FRONT LEFT door. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: ONLY the front left door of this vehicle, removed and shown on its own, seen from the outside straight from the side, with a very slight angle so its thickness is visible: the outer door panel with its handle and its side mirror if the vehicle has one on that door, the window frame on top is empty and open (no glass), the inner edge shows a simple dark door frame thickness and hinge side. 
```

## V10-C — V10_Carroceria (fase 3)
- **Medida:** 4.8 m de largo (sin ruedas) · **Tripo:** HD · 18000 · 2K · **Adjuntar:** `V10_Pickup.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same vehicle design, shape, proportions, paint colors, panels, patches, lights, details, wear and art style. Draw the same vehicle from the same 3/4 front left view, but as a body shell for a game with separate parts: 1) NO wheels: the four wheel arches are empty, with dark empty wheel wells and nothing inside; 2) NO front doors: both front door openings are empty holes with their door frames visible, showing the simple dark interior: the driver seat, the steering wheel and the dashboard; 3) NO glass at all: the windshield, side windows and rear window are open empty frames, no glass, no reflections. The rest of the vehicle stays identical (rear doors, if any, stay closed). Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## V10-P — V10_Puerta (fase 3)
- **Medida:** puerta delantera izq. (~1.1 × 1.1 m) · **Tripo:** Smart Mesh P2 · 6000 · 2K · **Adjuntar:** `V10_Pickup.png`

```
Use the attached vehicle as the exact reference: keep EXACTLY the same paint color, panels, patches, handle, wear and art style of its FRONT LEFT door. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: ONLY the front left door of this vehicle, removed and shown on its own, seen from the outside straight from the side, with a very slight angle so its thickness is visible: the outer door panel with its handle and its side mirror if the vehicle has one on that door, the window frame on top is empty and open (no glass), the inner edge shows a simple dark door frame thickness and hinge side. 
```

## C01F — C01_Fachada_SinPuerta (fase 4)
- **Medida:** igual que C01 · **Tripo:** Smart Mesh P2 · 10000 · 2K · **Adjuntar:** `C01_FachadaLocal.png`

```
Use the attached facade as the exact reference: keep EXACTLY the same design, size, proportions, materials, colors, signs, lights, cables, wear and art style. Draw the same facade straight from the front, with the only change that the door opening (the shop entrance with its rolling shutter) is now completely EMPTY: keep the door frame and the shutter box above it, but remove the shutter and any door, leaving a dark empty opening. Everything else stays identical. The back is flat. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## C02F — C02_Fachada_SinPuerta (fase 4)
- **Medida:** igual que C02 · **Tripo:** Smart Mesh P2 · 10000 · 2K · **Adjuntar:** `C02_FachadaVivienda.png`

```
Use the attached facade as the exact reference: keep EXACTLY the same design, size, proportions, materials, colors, signs, lights, cables, wear and art style. Draw the same facade straight from the front, with the only change that the house door opening is now completely EMPTY: keep the door frame, but remove the door, leaving a dark empty opening. Everything else stays identical. The back is flat. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## C03F — C03_Fachada_SinPuerta (fase 4)
- **Medida:** igual que C03 · **Tripo:** Smart Mesh P2 · 10000 · 2K · **Adjuntar:** `C03_FachadaPisoAlto.png`

```
Use the attached facade as the exact reference: keep EXACTLY the same design, size, proportions, materials, colors, signs, lights, cables, wear and art style. Draw the same facade straight from the front, with the only change that any ground floor door opening is now completely EMPTY: keep the door frame, but remove the door, leaving a dark empty opening. Everything else stays identical. The back is flat. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## P11F — P11_EntradaCasa_SinPuerta (fase 4)
- **Medida:** 2.2 × 3 m · **Tripo:** Smart Mesh P2 · 10000 · 2K · **Adjuntar:** `P11_EntradaCasa.png`

```
Use the attached facade as the exact reference: keep EXACTLY the same design, size, proportions, materials, colors, signs, lights, cables, wear and art style. Draw the same facade straight from the front, with the only change that the entrance opening is now completely EMPTY: keep the thick concrete door frame, the steps, the mailboxes, the bulb and the conduit, but remove the metal grille gate and any door, leaving a dark empty opening. Everything else stays identical. The back is flat. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## P12F — P12_FachadaConsultorio_SinPuerta (fase 4)
- **Medida:** 6 × 4.5 m · **Tripo:** Smart Mesh P2 · 10000 · 2K · **Adjuntar:** `P12_FachadaConsultorio.png`

```
Use the attached facade as the exact reference: keep EXACTLY the same design, size, proportions, materials, colors, signs, lights, cables, wear and art style. Draw the same facade straight from the front, with the only change that the door opening is now completely EMPTY: keep the door frame, the light strip and the cross above it, but remove the heavy metal door, leaving a dark empty opening. Everything else stays identical. The back is flat. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

## PU01 — PU01_PuertaVivienda (fase 4)
- **Medida:** 0.9 × 2.1 m · 5 cm de grueso · **Tripo:** Smart Mesh P2 · 6000 · 2K · **Adjuntar:** `C02_FachadaVivienda.png`, `C01_FachadaLocal.png`

```
Use the attached facade ONLY to match the door's style, colors, materials and wear to it. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: a single house front door on its own, closed, seen straight from the front with a very slight angle so its thickness shows: painted wood and metal sheet panels in a faded color, a chunky round knob and a lock, a small vent grille, scuffs and chipped paint. No frame, only the door leaf.
```

## PU02 — PU02_CortinaMetalica (fase 4)
- **Medida:** 2.4 × 2.4 m (ajustar al hueco de C01) · **Tripo:** Smart Mesh P2 · 6000 · 2K · **Adjuntar:** `C01_FachadaLocal.png`

```
Use the attached facade ONLY to match the door's style, colors, materials and wear to it. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: a single rolling metal shop shutter on its own, fully closed (rolled down): horizontal corrugated metal slats in grey with a faded paint, a thick bottom bar with a handle and a padlock loop, the rolled drum box on top, dents and grime. Seen straight from the front with a very slight angle.
```

## PU03 — PU03_RejaEntrada (fase 4)
- **Medida:** 1.1 × 2.2 m · 6 cm de grueso · **Tripo:** HD · 8000 · 2K · **Adjuntar:** `P11_EntradaCasa.png`

```
Use the attached facade ONLY to match the door's style, colors, materials and wear to it. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: the metal grille security gate of that building entrance, on its own, closed: a thick metal frame with the same lattice of chunky bars, a padlock and a handle, the same paint and rust. Only the gate leaf, no wall, no frame. Seen straight from the front with a very slight angle.
```

## PU04 — PU04_PuertaMetalica (fase 4)
- **Medida:** 1 × 2.2 m · 8 cm de grueso · **Tripo:** Smart Mesh P2 · 6000 · 2K · **Adjuntar:** `P12_FachadaConsultorio.png`

```
Use the attached facade ONLY to match the door's style, colors, materials and wear to it. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: the heavy reinforced metal door of that clinic, on its own, closed: thick riveted steel plate, the small speaker grille, a chunky handle and lock, the same paint, grime and rust. Only the door leaf, no wall, no frame. Seen straight from the front with a very slight angle.
```

## V07-B — V07_Cuerpo (fase 5)
- **Medida:** 1.9 m de largo (sin ruedas) · **Tripo:** HD · 12000 · 2K · **Adjuntar:** `V07_MotoReparto.png`

```
Use the attached motorcycle as the exact reference: keep EXACTLY the same design, shape, colors, panels, lights, cargo, details, wear and art style. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: the same motorcycle WITHOUT its two wheels: the front fork and the rear swingarm stay, with their empty axle ends where the wheels would go, nothing inside them; the motorcycle stands upright as if on an invisible stand. Show it twice side by side with space between them: on the left straight from the side, on the right in a 3/4 front view.
```

## V08-B — V08_Cuerpo (fase 5)
- **Medida:** 2.2 m de largo (sin ruedas) · **Tripo:** HD · 12000 · 2K · **Adjuntar:** `V08_MotoPatrulla.png`

```
Use the attached motorcycle as the exact reference: keep EXACTLY the same design, shape, colors, panels, lights, cargo, details, wear and art style. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: the same motorcycle WITHOUT its two wheels: the front fork and the rear swingarm stay, with their empty axle ends where the wheels would go, nothing inside them; the motorcycle stands upright as if on an invisible stand. Show it twice side by side with space between them: on the left straight from the side, on the right in a 3/4 front view.
```

## C19-B — C19_Cuerpo (fase 5)
- **Medida:** 2.0 m de largo (sin ruedas) · **Tripo:** HD · 12000 · 2K · **Adjuntar:** `C19_Moto_v1.png`

```
Use the attached motorcycle as the exact reference: keep EXACTLY the same design, shape, colors, panels, lights, cargo, details, wear and art style. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: the same motorcycle WITHOUT its two wheels: the front fork and the rear swingarm stay, with their empty axle ends where the wheels would go, nothing inside them; the motorcycle stands upright as if on an invisible stand. Show it twice side by side with space between them: on the left straight from the side, on the right in a 3/4 front view.
```


## Parte 21b — Fachadas sin puerta: hueco que deja ver el fondo blanco (corrige fase 4)

### C01F → `imageassets/modelos-nuevos/piezas/C01_Fachada_SinPuerta_v2.png`

```
Use the attached facade as the exact reference: keep EXACTLY the same design, size, proportions, materials, colors, signs, lights, cables, wear and art style. Draw the same facade straight from the front, with the only change that the door opening (the shop entrance with its rolling shutter) is now completely EMPTY: keep the door frame and the shutter box above it, but remove the shutter and any door, leaving a real see-through hole cut all the way through the wall: through the opening you see ONLY the same plain flat pure white background, exactly like the background around the object. Nothing inside the opening: no darkness, no shadow, no interior, no floor, no door.  Everything else stays identical. The wall is a single flat slab whose back is flat; the opening goes straight through it. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

### C02F → `imageassets/modelos-nuevos/piezas/C02_Fachada_SinPuerta_v2.png`

```
Use the attached facade as the exact reference: keep EXACTLY the same design, size, proportions, materials, colors, signs, lights, cables, wear and art style. Draw the same facade straight from the front, with the only change that the house door opening is now completely EMPTY: keep the door frame, but remove the door, leaving a real see-through hole cut all the way through the wall: through the opening you see ONLY the same plain flat pure white background, exactly like the background around the object. Nothing inside the opening: no darkness, no shadow, no interior, no floor, no door.  Everything else stays identical. The wall is a single flat slab whose back is flat; the opening goes straight through it. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

### C03F → `imageassets/modelos-nuevos/piezas/C03_Fachada_SinPuerta_v2.png`

```
Use the attached facade as the exact reference: keep EXACTLY the same design, size, proportions, materials, colors, signs, lights, cables, wear and art style. Draw the same facade straight from the front, with the only change that any ground floor door opening is now completely EMPTY: keep the door frame, but remove the door, leaving a real see-through hole cut all the way through the wall: through the opening you see ONLY the same plain flat pure white background, exactly like the background around the object. Nothing inside the opening: no darkness, no shadow, no interior, no floor, no door.  Everything else stays identical. The wall is a single flat slab whose back is flat; the opening goes straight through it. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

### P11F → `imageassets/modelos-nuevos/piezas/P11_EntradaCasa_SinPuerta_v2.png`

```
Use the attached facade as the exact reference: keep EXACTLY the same design, size, proportions, materials, colors, signs, lights, cables, wear and art style. Draw the same facade straight from the front, with the only change that the entrance opening is now completely EMPTY: keep the thick concrete door frame, the steps, the mailboxes, the bulb and the conduit, but remove the metal grille gate and any door, leaving a real see-through hole cut all the way through the wall: through the opening you see ONLY the same plain flat pure white background, exactly like the background around the object. Nothing inside the opening: no darkness, no shadow, no interior, no floor, no door.  Everything else stays identical. The wall is a single flat slab whose back is flat; the opening goes straight through it. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```

### P12F → `imageassets/modelos-nuevos/piezas/P12_FachadaConsultorio_SinPuerta_v2.png`

```
Use the attached facade as the exact reference: keep EXACTLY the same design, size, proportions, materials, colors, signs, lights, cables, wear and art style. Draw the same facade straight from the front, with the only change that the door opening is now completely EMPTY: keep the door frame, the light strip and the cross above it, but remove the heavy metal door, leaving a real see-through hole cut all the way through the wall: through the opening you see ONLY the same plain flat pure white background, exactly like the background around the object. Nothing inside the opening: no darkness, no shadow, no interior, no floor, no door.  Everything else stays identical. The wall is a single flat slab whose back is flat; the opening goes straight through it. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. 
```


### PU02b — PU02_CortinaMetalica_v2 (fase 7): solo la lámina, sin marco ni rollo

```
Use the attached facade ONLY to match the style, colors and wear. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Stylized game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients, no photorealistic noise. Subject: ONLY the slatted curtain sheet of a rolling metal shop shutter, on its own, as a flat rectangular panel: horizontal corrugated dark grey metal slats from top to bottom, a thick bottom bar with a small handle and a padlock loop, faded paint, dents and grime. NO frame, NO side guide rails, NO roll drum, NO box on top, NO wall: the top edge of the panel is just the last slat, straight and clean. Seen straight from the front with a very slight angle so its thin thickness is visible.
```


# Parte 22 — Pedido 09-nov: menús, logo agresivo e íconos con el look de las REF (solo estilo)

Lote: `lote_09nov.json` · script: `generar_09nov.ps1`.

## M20 — M20_MenuInicio (fase 1)
- **Salida:** `imageassets/referencias/menus/M20_MenuInicio.png` · 16:9 · **Adjuntar:** `REF_MenuInicio.png`, `K01_Barrio_HUDnuevo.png`

```
The attached screenshots marked as menu references are ONLY a style guide: take their general visual language (dark panels with thin red lines, corners cut on a diagonal, cyan accents for active elements, slots with colored borders by rarity, thin condensed uppercase typography, lots of empty space, a hologram in the center). Do NOT copy anything specific from them: no logos, no names, no icons, no texts, no layouts copied one to one. This is an ORIGINAL menu design for our own game CYBERCITY. Palette: coral red (#FF5A4E) for lines and titles, cyan (#3FE3E0) for active and selected elements, an almost black reddish background, rarity colors green, blue, purple and orange. All text in Spanish, spelled EXACTLY as written, no other words. Full 16:9 game menu screen, crisp and readable, like a polished commercial game. The second attached image is our game's city: use it, softly blurred and darkened, as the background. Screen: the main menu. On the left a tall vertical translucent dark red panel with thin coral red edge lines and a diagonal cut corner; at the top of the panel the game logo text "CYBERCITY" (spelled exactly C-Y-B-E-R-C-I-T-Y) in aggressive slanted brush-like letters; below it a vertical list of options in thin condensed uppercase: "CONTINUAR" (highlighted in cyan with a small marker, as selected), "NUEVA PARTIDA", "CARGAR", "OPCIONES", "CRÉDITOS", "SALIR". In the bottom-left corner a tiny version number "v0.9". The rest of the screen shows the blurred night street.
```

## M21 — M21_MejoraArmas (fase 1)
- **Salida:** `imageassets/referencias/menus/M21_MejoraArmas.png` · 16:9 · **Adjuntar:** `REF_MenuArmas1.png`, `REF_MenuArmas2.png`, `H01_Pistola.png`

```
The attached screenshots marked as menu references are ONLY a style guide: take their general visual language (dark panels with thin red lines, corners cut on a diagonal, cyan accents for active elements, slots with colored borders by rarity, thin condensed uppercase typography, lots of empty space, a hologram in the center). Do NOT copy anything specific from them: no logos, no names, no icons, no texts, no layouts copied one to one. This is an ORIGINAL menu design for our own game CYBERCITY. Palette: coral red (#FF5A4E) for lines and titles, cyan (#3FE3E0) for active and selected elements, an almost black reddish background, rarity colors green, blue, purple and orange. All text in Spanish, spelled EXACTLY as written, no other words. Full 16:9 game menu screen, crisp and readable, like a polished commercial game. The third attached image is our pistol hologram: use it as the big hologram in the center. Screen: the weapon upgrade bench of Tino. Top: two tabs "MEJORAS" (active, cyan underline) and "ARMAS". Left side: a column of small category icons in thin red line, and next to it a grid of weapon slots with colored borders by rarity (green, blue, purple, orange), each showing a small weapon silhouette. Center: the pistol as a large glowing cyan hologram seen from the side, with its magazine pulled slightly out below it and glowing brighter, as the selected part. Right side: the weapon name "PISTOLA W01", the rarity "RARA" in blue, and three stats with before and after values: "DAÑO 24 → 30", "CADENCIA 3.0 → 3.0", "CARGADOR 12 → 16", the improved values in cyan. Bottom: a row of 4 part slots labeled "CARGADOR" (selected, cyan border), "CAÑÓN", "MIRA", "EMPUÑADURA", each with a small cost like "$ 1,200", and a button "MEJORAR" with a cut corner.
```

## M22 — M22_ClinicaImplantes (fase 1)
- **Salida:** `imageassets/referencias/menus/M22_ClinicaImplantes.png` · 16:9 · **Adjuntar:** `REF_MenuImplantes.png`, `PJ01_Protagonista_v2.png`, `IMP01_ExoPiernas_v2.png`

```
The attached screenshots marked as menu references are ONLY a style guide: take their general visual language (dark panels with thin red lines, corners cut on a diagonal, cyan accents for active elements, slots with colored borders by rarity, thin condensed uppercase typography, lots of empty space, a hologram in the center). Do NOT copy anything specific from them: no logos, no names, no icons, no texts, no layouts copied one to one. This is an ORIGINAL menu design for our own game CYBERCITY. Palette: coral red (#FF5A4E) for lines and titles, cyan (#3FE3E0) for active and selected elements, an almost black reddish background, rarity colors green, blue, purple and orange. All text in Spanish, spelled EXACTLY as written, no other words. Full 16:9 game menu screen, crisp and readable, like a polished commercial game. The second attached image is our protagonist and the third is a leg exoskeleton implant: use them. Screen: the implant clinic of the Doc. Center: the full body of the protagonist standing as a hologram in red and orange tones with glowing vein-like light lines, a slightly see-through hologram, a thin glowing leg exoskeleton outlined on his legs. Thin lines go out from the body to groups of square implant slots on both sides: top-left group labeled "CABEZA / OJOS" with 2 slots, middle-left group "BRAZOS" with 3 slots, middle-right group "PIERNAS" with 3 slots (one selected with a cyan border), lower-right group "PIEL / ESCUDO" with 2 slots; slots have rarity-colored borders and small implant icons, some empty. A detail box for the selected implant: title "EXO DE PIERNAS II", a short description "Salto más alto y menos daño por caída.", the cost "$ 4,500" and a button "INSTALAR". Top-right corner: "I+D+I · CLÍNICA DEL DOC" and the money "$ 12,450".
```

## LG20 — LG20_Amarillo (fase 2)
- **Salida:** `imageassets/logo/LG20_Amarillo.png` · 16:9 · **Adjuntar:** `REF_MenuInicio.png`

```
Design an ORIGINAL video game title logo (do not imitate the logo of any existing game). The text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter legible, no other words. Lettering: aggressive slanted letters leaning to the right, drawn with a fast digital brush or marker stroke, sharp tapered stroke ends, energetic but readable. Under the word a fast underline stroke, and one signature detail of our own: a thin street light pole with a small lamp rising from the underline at the right end, and a tiny silhouette of a tall tower behind the letters. The logo alone, centered, filling about 70% of the width, on a PURE BLACK (#000000) flat background, nothing else. Use the attached menu reference ONLY for the general mood. Color: electric yellow letters with a thin cyan outline.
```

## LG21 — LG21_Rojo (fase 2)
- **Salida:** `imageassets/logo/LG21_Rojo.png` · 16:9 · **Adjuntar:** `REF_MenuInicio.png`

```
Design an ORIGINAL video game title logo (do not imitate the logo of any existing game). The text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter legible, no other words. Lettering: aggressive slanted letters leaning to the right, drawn with a fast digital brush or marker stroke, sharp tapered stroke ends, energetic but readable. Under the word a fast underline stroke, and one signature detail of our own: a thin street light pole with a small lamp rising from the underline at the right end, and a tiny silhouette of a tall tower behind the letters. The logo alone, centered, filling about 70% of the width, on a PURE BLACK (#000000) flat background, nothing else. Use the attached menu reference ONLY for the general mood. Color: coral red (#FF5A4E) letters with a soft red glow.
```

## LG22 — LG22_Cian (fase 2)
- **Salida:** `imageassets/logo/LG22_Cian.png` · 16:9 · **Adjuntar:** `REF_MenuInicio.png`

```
Design an ORIGINAL video game title logo (do not imitate the logo of any existing game). The text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter legible, no other words. Lettering: aggressive slanted letters leaning to the right, drawn with a fast digital brush or marker stroke, sharp tapered stroke ends, energetic but readable. Under the word a fast underline stroke, and one signature detail of our own: a thin street light pole with a small lamp rising from the underline at the right end, and a tiny silhouette of a tall tower behind the letters. The logo alone, centered, filling about 70% of the width, on a PURE BLACK (#000000) flat background, nothing else. Use the attached menu reference ONLY for the general mood. Color: cyan (#3FE3E0) letters with a slight red glitch offset on a few strokes.
```

## LG23 — LG23_Blanco (fase 2)
- **Salida:** `imageassets/logo/LG23_Blanco.png` · 16:9 · **Adjuntar:** `REF_MenuInicio.png`

```
Design an ORIGINAL video game title logo (do not imitate the logo of any existing game). The text must read exactly "CYBERCITY": one word, nine letters, C-Y-B-E-R-C-I-T-Y, spelled correctly, every letter legible, no other words. Lettering: aggressive slanted letters leaning to the right, drawn with a fast digital brush or marker stroke, sharp tapered stroke ends, energetic but readable. Under the word a fast underline stroke, and one signature detail of our own: a thin street light pole with a small lamp rising from the underline at the right end, and a tiny silhouette of a tall tower behind the letters. The logo alone, centered, filling about 70% of the width, on a PURE BLACK (#000000) flat background, nothing else. Use the attached menu reference ONLY for the general mood. Color: white letters with a coral red shadow copy offset down and to the right.
```

## H30 — H30_IconosImplantes (fase 3)
- **Salida:** `imageassets/ui/menus/H30_IconosImplantes.png` · 3:2 · **Adjuntar:** `REF_MenuImplantes.png`, `IMP01_ExoPiernas_v2.png`, `IMP02_ExoBrazos_v2.png`, `IMP03_VisorCabeza_v2.png`

```
Use the attached menu reference ONLY for the general style of the item icons (illustrated, slightly glowing, in dark square slots with thin colored borders). Original icons of our own, do not copy any icon from the reference. A clean icon sheet on a PURE BLACK (#000000) flat background: each icon centered inside its own square slot, slots in a neat grid with even spacing, all slots the same size, no text, no labels, no numbers. The other attached images are our implants: base the icons on them. 6 illustrated implant icons in a grid of 3 columns by 2 rows, slot borders by rarity: 1) leg exoskeleton level I (simple rods, green border), 2) leg exoskeleton level II (the same with extra glowing parts, blue border), 3) arm exoskeleton level I (green border), 4) arm exoskeleton level II (blue border), 5) a bionic eye monocle (purple border), 6) a goggles visor for the eyes (orange border).
```

## H31 — H31_IconosPiezasArma (fase 3)
- **Salida:** `imageassets/ui/menus/H31_IconosPiezasArma.png` · 3:2 · **Adjuntar:** `REF_MenuArmas1.png`, `H01_Pistola.png`

```
Use the attached menu reference ONLY for the general style of the item icons (illustrated, slightly glowing, in dark square slots with thin colored borders). Original icons of our own, do not copy any icon from the reference. A clean icon sheet on a PURE BLACK (#000000) flat background: each icon centered inside its own square slot, slots in a neat grid with even spacing, all slots the same size, no text, no labels, no numbers. 6 illustrated weapon part icons in a grid of 3 columns by 2 rows, slot borders in rarity colors (green, green, blue, blue, purple, orange): a pistol magazine, a gun barrel, a small scope sight, a pistol grip, a suppressor, a small glowing damage chip.
```

## H32 — H32_IconosCategorias (fase 3)
- **Salida:** `imageassets/ui/menus/H32_IconosCategorias.png` · 21:9 · **Adjuntar:** `REF_MenuArmas2.png`

```
Use the attached menu reference ONLY for the general line style. Original icons of our own. A single row of 8 category icons evenly spaced on a PURE BLACK (#000000) flat background, each drawn in a single thin coral red (#FF5A4E) line, same line weight and size, no slots, no text: a pistol, a shotgun, an assault rifle, a sniper rifle with scope, a combat knife, a consumable (a drink can and a small box), an implant (a robotic arm segment), a weapon part (a gear with a bullet).
```


# Parte 23 — Pedido 14-nov: rueda de armas, habilidades, tienda, armas W05–W08, prótesis IMP04–05, enemigos E10–E14, hologramas H05–H08

Lote: `lote_14nov.json` · script: `generar_14nov.ps1`.

## M30 — M30_RuedaArmas (fase 1)
- **Salida:** `imageassets/referencias/menus/M30_RuedaArmas.png` · 16:9 · **Adjuntar:** `H10_HUD.png`, `K01_Barrio_HUDnuevo.png`, `H01_Pistola.png`

```
The FIRST attached image is our game's HUD and the SECOND is our game's city look: match their style EXACTLY: thin lines, corners cut on a diagonal, turquoise with a yellow accent for the selected element, thin clean sans-serif text, no heavy panels. Original design of our own, nothing copied from other games. Full 16:9 game screenshot over the night street of the second image. All text in Spanish, spelled EXACTLY as written, no other words. The third image is our pistol hologram style for the weapon icons. Screen: the weapon wheel is open: the street behind is darkened and slightly blurred (slow motion), and in the center of the screen a radial wheel of 8 slots arranged in a circle, drawn in thin turquoise lines with cut corners: "PISTOLA", "ESCOPETA", "FUSIL", "FRANCOTIRADOR", "PUÑOS", "BEBIDA", "PAPITAS", "VENDA", each slot with a small cyan hologram icon of its weapon or item and its name in tiny thin text. The selected slot "FUSIL" is highlighted in yellow. In the center of the wheel: the selected name "FUSIL" and its ammo "30 / 120". The normal HUD is hidden.
```

## M31 — M31_HabilidadesProtesis (fase 1)
- **Salida:** `imageassets/referencias/menus/M31_HabilidadesProtesis.png` · 16:9 · **Adjuntar:** `H10_HUD.png`, `K01_Barrio_HUDnuevo.png`, `H33_Implantes_Nivel2_v2.png`

```
Keep the FIRST attached image EXACTLY the same: the same street, character, camera and the same HUD in every corner. The second image is our city look and the third image shows our implant icons. Add ONLY one new element in the bottom center of the screen, in the same thin HUD style: a bar of 3 square ability slots side by side with cut corners, each with its key and name above it in Spanish, spelled exactly: "[1] PIERNAS", "[2] BRAZOS", "[3] CABEZA"; each slot shows the small icon of its implant (leg exoskeleton, arm exoskeleton, visor) and a thin energy/recharge bar below it: PIERNAS full and ACTIVE, glowing yellow; BRAZOS half recharged in turquoise; CABEZA full in turquoise. No other new text.
```

## M32 — M32_TiendaArmas (fase 1)
- **Salida:** `imageassets/referencias/menus/M32_TiendaArmas.png` · 16:9 · **Adjuntar:** `H10_HUD.png`, `K01_Barrio_HUDnuevo.png`, `H04_Francotirador.png`

```
The FIRST attached image is our game's HUD and the SECOND is our game's city look: match their style EXACTLY: thin lines, corners cut on a diagonal, turquoise with a yellow accent for the selected element, thin clean sans-serif text, no heavy panels. Original design of our own, nothing copied from other games. Full 16:9 game screenshot over the night street of the second image. All text in Spanish, spelled EXACTLY as written, no other words. The third image is the weapon hologram to show big. Screen: the gun shop of El Mercado. The street is darkened behind a large translucent interface. Left side: a vertical list of weapons with thin separators, each row with a name and a price: "PISTOLA  $ 800", "ESCOPETA  $ 2,400", "FUSIL  $ 4,800", "FRANCOTIRADOR  $ 7,500" (selected, highlighted in yellow), "SUBFUSIL  $ 3,600", "REVÓLVER  $ 2,900". Right side: the selected weapon shown BIG as a glowing cyan hologram from the side, its name "FRANCOTIRADOR" above, and four stats as thin bars with numbers: "DAÑO 90", "CADENCIA 20", "CARGADOR 5", "ALCANCE 95". Bottom right: a button with a cut corner "COMPRAR" in yellow, and above the list the money "$ 12,450".
```

## W05 — W05_Subfusil (fase 2)
- **Salida:** `imageassets/armas/W05_Subfusil.png` · 16:9 · **Adjuntar:** `W01_Pistola.png`, `W03_Fusil.png`
- **Medida:** 0.55 m (culata plegada) · **Tripo:** HD · 12000 · 2K

```
Use the attached weapons as the exact reference for the art style, materials, colors, wear and level of detail: the new weapon must look like it belongs to the same set. Do not copy their shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures. Mexican cyberpunk megacity street weapon, worn and patched: dark gunmetal and charcoal metal, sand-beige worn polymer parts, burnt orange accents, red cloth tape, small glowing teal and amber lights, scratches and rust spots. The whole weapon fully visible, no hands, no person, no text, no letters, no numbers, no logos. Single object, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow. Nothing thin or dangling. View: 3/4 view from the side, slightly from the front, showing the full length. Subject: a compact burst-fire submachine gun: a short boxy body, a long straight magazine sticking down, a folding stock folded to the side, a short barrel with a vented shroud, a small teal burst-mode light, red tape on the front grip.
```

## W06 — W06_Revolver (fase 2)
- **Salida:** `imageassets/armas/W06_Revolver.png` · 16:9 · **Adjuntar:** `W01_Pistola.png`, `W03_Fusil.png`
- **Medida:** 0.38 m · **Tripo:** HD · 10000 · 2K

```
Use the attached weapons as the exact reference for the art style, materials, colors, wear and level of detail: the new weapon must look like it belongs to the same set. Do not copy their shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures. Mexican cyberpunk megacity street weapon, worn and patched: dark gunmetal and charcoal metal, sand-beige worn polymer parts, burnt orange accents, red cloth tape, small glowing teal and amber lights, scratches and rust spots. The whole weapon fully visible, no hands, no person, no text, no letters, no numbers, no logos. Single object, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow. Nothing thin or dangling. View: 3/4 view from the side, slightly from the front, showing the full length. Subject: a heavy large-caliber revolver: a long thick barrel with a top rib, a big cylinder with a glowing turquoise ring of light around it, a chunky hammer, a sand-beige grip wrapped in red tape.
```

## W07 — W07_RiflePlasma (fase 2)
- **Salida:** `imageassets/armas/W07_RiflePlasma.png` · 16:9 · **Adjuntar:** `W01_Pistola.png`, `W03_Fusil.png`
- **Medida:** 1.0 m · **Tripo:** HD · 12000 · 2K

```
Use the attached weapons as the exact reference for the art style, materials, colors, wear and level of detail: the new weapon must look like it belongs to the same set. Do not copy their shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures. Mexican cyberpunk megacity street weapon, worn and patched: dark gunmetal and charcoal metal, sand-beige worn polymer parts, burnt orange accents, red cloth tape, small glowing teal and amber lights, scratches and rust spots. The whole weapon fully visible, no hands, no person, no text, no letters, no numbers, no logos. Single object, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow. Nothing thin or dangling. View: 3/4 view from the side, slightly from the front, showing the full length. Subject: an energy plasma rifle: a long body with three thick copper-and-gunmetal coils wrapped around the barrel glowing turquoise between them, a boxy energy cell inserted in the side glowing bright turquoise, heat vents, a sand-beige stock, small amber warning lights.
```

## W08 — W08_Lanzagranadas (fase 2)
- **Salida:** `imageassets/armas/W08_Lanzagranadas.png` · 16:9 · **Adjuntar:** `W01_Pistola.png`, `W03_Fusil.png`
- **Medida:** 0.8 m · **Tripo:** HD · 12000 · 2K

```
Use the attached weapons as the exact reference for the art style, materials, colors, wear and level of detail: the new weapon must look like it belongs to the same set. Do not copy their shapes. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, no photorealistic textures. Mexican cyberpunk megacity street weapon, worn and patched: dark gunmetal and charcoal metal, sand-beige worn polymer parts, burnt orange accents, red cloth tape, small glowing teal and amber lights, scratches and rust spots. The whole weapon fully visible, no hands, no person, no text, no letters, no numbers, no logos. Single object, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow. Nothing thin or dangling. View: 3/4 view from the side, slightly from the front, showing the full length. Subject: a sturdy revolving-drum grenade launcher: a big round rotating drum with six thick chambers, a wide short barrel, a heavy frame, a top carry handle, a folding stock, orange hazard stripes, a small amber ready light.
```

## IMP04 — IMP04_BrazoLaser (fase 3)
- **Salida:** `imageassets/modelos-nuevos/IMP04_BrazoLaser.png` · 16:9 · **Adjuntar:** `IMP02_ExoBrazos.png`, `W01_Pistola.png`
- **Medida:** 0.45 m (codo a mano) · **Tripo:** HD · 12000 · 2K

```
Use the attached images ONLY for the art style, materials and colors of our cyber parts and weapons. Stylized low-poly 3D game asset, chunky simplified shapes, large clean bevels, soft hand-painted gradients. Single object, complete, centered, isolated on a plain flat pure white background, soft even lighting, no cast shadow, no person, no text, no logos. Show the same piece twice side by side with space between them: on the left from the front, on the right in a 3/4 view. Subject: a cybernetic RIGHT forearm prosthetic, from elbow to hand, in gunmetal and chrome with orange accents: its top armor plates are split open like petals and a short thick laser cannon with a glowing turquoise lens slides out over the back of the hand; the hand itself is a robotic hand with chunky fingers. Hollow socket at the elbow end where it attaches.
```

## IMP05 — IMP05_HombreraMisiles (fase 3)
- **Salida:** `imageassets/modelos-nuevos/IMP05_HombreraMisiles.png` · 16:9 · **Adjuntar:** `IMP02_ExoBrazos.png`, `W01_Pistola.png`
- **Medida:** 0.4 m de ancho · lanzador 0.3 m · **Tripo:** HD · 10000 · 2K

```
Use the attached images ONLY for the art style, materials and colors of our cyber parts and weapons. Stylized low-poly 3D game asset, chunky simplified shapes, large clean bevels, soft hand-painted gradients. Single object, complete, centered, isolated on a plain flat pure white background, soft even lighting, no cast shadow, no person, no text, no logos. Show the same piece twice side by side with space between them: on the left from the front, on the right in a 3/4 view. Subject: a shoulder pad armor piece with a compact missile launcher that rises above the shoulder: a curved gunmetal shoulder plate with straps shaped as solid bands, a small hinged arm, and on top a boxy launcher pod with 4 mini missile tubes in a 2 by 2 grid, the missile tips orange, a small red targeting light.
```

## E10 — E10_PandilleroSnakes (fase 4)
- **Salida:** `imageassets/modelos-nuevos/E10_PandilleroSnakes.png` · 1:1 · **Adjuntar:** `PJ01_Protagonista_v2.png`, `SkullHombre_frente.png`
- **Medida:** 1.78 m · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character. Rig-friendly: everything fitted close to the body, nothing hanging loose, no capes, no long loose coats tails flapping, no dangling chains. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a gang member of the SNAKES, the rival gang of the Skulls: an adult man, a fitted cropped jacket with a scale pattern in dark green and black, snake tattoos winding down both forearms, a cloth mask over the lower face in black with a green fang pattern, fitted black cargo pants, black boots, acid green accents.
```

## E11 — E11_JefeSnakes (fase 4)
- **Salida:** `imageassets/modelos-nuevos/E11_JefeSnakes.png` · 1:1 · **Adjuntar:** `PJ01_Protagonista_v2.png`, `SkullHombre_frente.png`
- **Medida:** 2.0 m · corpulento · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character. Rig-friendly: everything fitted close to the body, nothing hanging loose, no capes, no long loose coats tails flapping, no dangling chains. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: the boss of the SNAKES gang, a mini boss: a big, broad, tall man, a long dark green coat fitted closed at the waist and ending at the knees (no loose tails), a scale-pattern vest under it, a shaved head with a snake tattoo on the scalp, a heavy cybernetic LEFT arm in black and gunmetal with a long blade folded along the forearm and acid green lights, black boots.
```

## E12 — E12_GuardiaPesadoNova (fase 4)
- **Salida:** `imageassets/modelos-nuevos/E12_GuardiaPesadoNova.png` · 1:1 · **Adjuntar:** `PJ01_Protagonista_v2.png`, `GuardiaNova_frente.png`
- **Medida:** 2.0 m (con armadura) · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character. Rig-friendly: everything fitted close to the body, nothing hanging loose, no capes, no long loose coats tails flapping, no dangling chains. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a heavy Nova corporation guard in full white and blue armor: bulky chest plate, shoulder pads, armored arms and legs, a closed helmet with a dark visor and a thin turquoise light line, a large riot shield strapped flat to the LEFT forearm (kept close along the arm), clean and corporate, blue light accents.
```

## E13 — E13_DronAsalto (fase 4)
- **Salida:** `imageassets/modelos-nuevos/E13_DronAsalto.png` · 4:3 · **Adjuntar:** `DR01_Dron.png`, `V05_PatrullaSedan.png`
- **Medida:** 1.4 m de ancho · **Tripo:** HD · 12000 · 2K

```
Use the attached drone ONLY for the art style and level of detail; this is a bigger, more dangerous version in Nova corporation colors (white and blue). Stylized low-poly 3D game asset, chunky simplified shapes, soft hand-painted gradients. Single object, complete, centered, isolated on a plain flat pure white background, soft even lighting, no cast shadow, no text, no logos. Subject: a Nova assault drone: an armored white and blue central body, four thick arms each ending in a round rotor guard with a chunky rotor inside, the four rotor units clearly separated from the body by visible gaps so they can be cut as separate pieces, a heavy rotary machine gun hanging under the body, a glowing red eye light at the front. View: 3/4 view from the front, slightly from above.
```

## E14 — E14_JefeFinalNova (fase 4)
- **Salida:** `imageassets/modelos-nuevos/E14_JefeFinalNova.png` · 1:1 · **Adjuntar:** `PJ01_Protagonista_v2.png`, `GuardiaNova_frente.png`
- **Medida:** 3.0 m · **Tripo:** Quads · 25000 · 2K · Humanoid (luego Mixamo)

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character. Rig-friendly: everything fitted close to the body, nothing hanging loose, no capes, no long loose coats tails flapping, no dangling chains. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: the FINAL BOSS: the head of security of the Nova corporation inside a heavy powered exosuit about 3 meters tall: massive white and blue armored frame with gunmetal joints, a small protected cockpit-helmet on top where his stern face is visible behind a clear visor, broad shoulders, both forearms ending in big arm cannons with glowing blue barrels instead of hands, a round glowing blue energy shield emitter in the center of the chest, thick armored legs. Imposing and heroic proportions for a giant.
```

## H05 — H05_Subfusil (fase 5)
- **Salida:** `imageassets/ui/hologramas/H05_Subfusil.png` · 16:9 · **Adjuntar:** `W05_Subfusil.png`

```
Turn the weapon in the attached image into a holographic projection for a video game user interface. Keep EXACTLY the same weapon shape, silhouette, parts and proportions, but shown perfectly from the side (pure side profile, orthographic, muzzle pointing to the right), centered and filling about 80% of the width. Translucent cyan-turquoise glowing body, bright glowing outlines on every part, faint inner wireframe lines, thin horizontal scan lines, a slight digital glitch on one or two slices, soft cyan glow. PURE BLACK (#000000) background everywhere, no text, no hands.
```

## H06 — H06_Revolver (fase 5)
- **Salida:** `imageassets/ui/hologramas/H06_Revolver.png` · 16:9 · **Adjuntar:** `W06_Revolver.png`

```
Turn the weapon in the attached image into a holographic projection for a video game user interface. Keep EXACTLY the same weapon shape, silhouette, parts and proportions, but shown perfectly from the side (pure side profile, orthographic, muzzle pointing to the right), centered and filling about 80% of the width. Translucent cyan-turquoise glowing body, bright glowing outlines on every part, faint inner wireframe lines, thin horizontal scan lines, a slight digital glitch on one or two slices, soft cyan glow. PURE BLACK (#000000) background everywhere, no text, no hands.
```

## H07 — H07_RiflePlasma (fase 5)
- **Salida:** `imageassets/ui/hologramas/H07_RiflePlasma.png` · 16:9 · **Adjuntar:** `W07_RiflePlasma.png`

```
Turn the weapon in the attached image into a holographic projection for a video game user interface. Keep EXACTLY the same weapon shape, silhouette, parts and proportions, but shown perfectly from the side (pure side profile, orthographic, muzzle pointing to the right), centered and filling about 80% of the width. Translucent cyan-turquoise glowing body, bright glowing outlines on every part, faint inner wireframe lines, thin horizontal scan lines, a slight digital glitch on one or two slices, soft cyan glow. PURE BLACK (#000000) background everywhere, no text, no hands.
```

## H08 — H08_Lanzagranadas (fase 5)
- **Salida:** `imageassets/ui/hologramas/H08_Lanzagranadas.png` · 16:9 · **Adjuntar:** `W08_Lanzagranadas.png`

```
Turn the weapon in the attached image into a holographic projection for a video game user interface. Keep EXACTLY the same weapon shape, silhouette, parts and proportions, but shown perfectly from the side (pure side profile, orthographic, muzzle pointing to the right), centered and filling about 80% of the width. Translucent cyan-turquoise glowing body, bright glowing outlines on every part, faint inner wireframe lines, thin horizontal scan lines, a slight digital glitch on one or two slices, soft cyan glow. PURE BLACK (#000000) background everywhere, no text, no hands.
```


# Parte 24 — Componentes para Unity: íconos de línea (H40 armas, H41 prótesis)

Lote: `lote_16nov.json` · script: `generar_16nov.ps1`.

## H40 — H40_IconosArmasLinea
- **Salida:** `imageassets/ui/menus/H40_IconosArmasLinea.png` · 1:1 · **Adjuntar:** `M30_RuedaArmas.png`, `M32_TiendaArmas.png`

```
The attached images are our approved game menus: match their thin turquoise line icon style EXACTLY (single thin turquoise line, same line weight, clean geometric shapes, subtle glow, corners cut on a diagonal on the cells). Original icons of our own. A clean icon sheet for Unity on a PURE BLACK (#000000) flat background: each icon centered inside its own square cell outlined with a very thin dim turquoise line with one cut corner, all cells exactly the same size in a neat grid with even spacing, every icon the same visual size and line weight, no text, no labels, no numbers, no extra decoration. 9 weapon icons in a 3 by 3 grid, each weapon seen from the side pointing right, drawn as a thin turquoise outline, in this order left to right, top to bottom: 1) a pistol, 2) a pump shotgun, 3) a compact assault rifle with a red dot sight, 4) a bolt-action sniper rifle with a big scope, 5) a clenched fist (for fists), 6) a compact submachine gun with a long magazine, 7) a heavy revolver with a long barrel, 8) a plasma rifle with coils around the barrel and an energy cell, 9) a revolving-drum grenade launcher.
```

## H41 — H41_IconosProtesis
- **Salida:** `imageassets/ui/menus/H41_IconosProtesis.png` · 3:2 · **Adjuntar:** `M31_HabilidadesProtesis.png`, `H30_IconosImplantes.png`, `H33_Implantes_Nivel2_v2.png`

```
The attached images are our approved game menus: match their thin turquoise line icon style EXACTLY (single thin turquoise line, same line weight, clean geometric shapes, subtle glow, corners cut on a diagonal on the cells). Original icons of our own. A clean icon sheet for Unity on a PURE BLACK (#000000) flat background: each icon centered inside its own square cell outlined with a very thin dim turquoise line with one cut corner, all cells exactly the same size in a neat grid with even spacing, every icon the same visual size and line weight, no text, no labels, no numbers, no extra decoration. The second and third images show the shapes of our implants (level I and level II): use them only for the shapes, redrawn as thin turquoise line icons. 6 prosthetic icons in a grid of 3 columns by 2 rows. Top row = LEVEL I (minimal, thin): 1) legs: a thin hip ring with one rod down the outside of each leg with small knee joints, 2) arms: a spine piece with one rod along each arm, 3) head: a single round cyber monocle lens for one eye. Bottom row = LEVEL II (bigger, heavier, complete), same order: 4) legs: a full leg exoskeleton with hip belt, thigh plates, knee joints, shin plates and foot trays, 5) arms: a full upper-body exoskeleton with chest plate, shoulder pads, elbow hinges and gauntlets, 6) head: a wide goggle visor over both eyes. Level II icons visibly more complete and with thicker lines than level I, but the same cell size. Add one tiny marker in the top-right corner of each cell: one small bar for level I and two small bars for level II.
```


# Parte 25 — Pedido 16-nov: maletín, tragamonedas, pantallas de carga, ARRESTADO/HAS MUERTO, H42, calles K08–K10, neones LT01–LT04

Lote: `lote_16nov_b.json` · script: `generar_16nov_b.ps1`.

## MA01 — MA01_Maletin (fase 1)
- **Salida:** `imageassets/modelos-nuevos/MA01_Maletin.png` · 4:3 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `SkullHombre_frente.png`
- **Medida:** 0.5 × 0.15 × 0.38 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached image ONLY for the art style, colors and level of detail of our game; do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels, soft hand-painted gradients, thin outlines, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling. No text, no letters, no numbers, no logos. The second image shows the Skulls gang: use only their skull motif for the sticker. Subject: a closed metal money briefcase of robbers: scratched brushed aluminum with ribbed sides, two chunky latch locks with small number wheels shown as plain blank wheels, a thick handle, rounded corners, dents, and one sticker of a stylized magenta holographic skull (no text) on the lid. View: 3/4 view from the front left, slightly from above.
```

## TR01 — TR01_Tragamonedas (fase 1)
- **Salida:** `imageassets/modelos-nuevos/TR01_Tragamonedas.png` · 3:4 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `L10_LetreroVivaCoin.png`
- **Medida:** 0.7 × 0.6 × 1.8 m · **Tripo:** Smart Mesh P2 · 10000 · 2K

```
Use the attached image ONLY for the art style, colors and level of detail of our game; do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels, soft hand-painted gradients, thin outlines, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling. No text, no letters, no numbers, no logos. The second image is the VivaCoin casino sign: use its gold and magenta colors. Subject: a casino slot machine cabinet: tall rounded gold and magenta cabinet, a glowing screen showing three abstract symbols (a star, a coin, a diamond shape), a row of round warm bulbs around the top edge, a chunky lever on the right side with a red ball, a small button panel, a coin tray at the bottom. View: 3/4 view from the front left, slightly from above.
```

## TR02 — TR02_Tragamonedas (fase 1)
- **Salida:** `imageassets/modelos-nuevos/TR02_Tragamonedas.png` · 3:4 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `L10_LetreroVivaCoin.png`
- **Medida:** 0.7 × 0.65 × 1.9 m · **Tripo:** Smart Mesh P2 · 10000 · 2K

```
Use the attached image ONLY for the art style, colors and level of detail of our game; do not copy the scene. Stylized game asset with chunky simplified shapes, large clean bevels, soft hand-painted gradients, thin outlines, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people. Nothing thin or dangling. No text, no letters, no numbers, no logos. Subject: a modern casino slot machine cabinet variant: a sleek black cabinet with glowing turquoise light strips along its edges, a large CURVED screen showing abstract symbols, a panel with three big round glowing buttons in turquoise, magenta and yellow, a small stool-height footrest. View: 3/4 view from the front left, slightly from above.
```

## L01 — L01_Carga_Protagonista (fase 2)
- **Salida:** `imageassets/ui/carga/L01_Carga_Protagonista.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `PJ01_Protagonista_v2.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with turquoise and magenta neon and orange sodium lights, soft fog, wet reflective ground). The SECOND attached image is our protagonist: keep his face, curly hair with taper fade and the two shaved lines on the right side, silver hoop earring, black sleeveless shirt, black loose cargo pants and white high-top sneakers with a blue side stripe. A large cinematic 16:9 loading-screen illustration, dramatic composition with depth, NO HUD, NO text, NO letters, NO logos anywhere. Scene: a close-up of the protagonist from the waist up on a night street, holding our worn pistol low and ready, turquoise and magenta neon light on one side of his face and orange street light on the other, determined look, blurred street lights and wet reflections behind him.
```

## L02 — L02_Carga_Persecucion (fase 2)
- **Salida:** `imageassets/ui/carga/L02_Carga_Persecucion.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `PJ01_Protagonista_v2.png`, `V01a_CarroViejito.png`, `V05_PatrullaSedan.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with turquoise and magenta neon and orange sodium lights, soft fog, wet reflective ground). The SECOND attached image is our protagonist: keep his face, curly hair with taper fade and the two shaved lines on the right side, silver hoop earring, black sleeveless shirt, black loose cargo pants and white high-top sneakers with a blue side stripe. A large cinematic 16:9 loading-screen illustration, dramatic composition with depth, NO HUD, NO text, NO letters, NO logos anywhere. Scene: The third image is the player's car and the fourth is the police car. A car chase on a wet night street: in the front a boxy dark van of the Skulls gang with magenta skull-like markings shown as simple shapes (no text), right behind it the player's old orange car with the protagonist driving, sparks flying where they scrape, and further back a police patrol with red and blue lights, motion blur, low dramatic angle.
```

## L03 — L03_Carga_Doc (fase 2)
- **Salida:** `imageassets/ui/carga/L03_Carga_Doc.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `Doc_frente.png`, `consultorio-doc.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with turquoise and magenta neon and orange sodium lights, soft fog, wet reflective ground). The SECOND attached image is the Doc character (keep his look) and the THIRD is his clinic (use it as the setting). A large cinematic 16:9 loading-screen illustration, dramatic composition with depth, NO HUD, NO text, NO letters, NO logos anywhere. Scene: the Doc inside his underground I+D+I clinic, standing next to the reclining surgery chair, adjusting the big robotic surgery arm that hangs from the ceiling over the chair, monitors with abstract graphs, shelves with implants, cold turquoise light.
```

## L04 — L04_Carga_Casino (fase 2)
- **Salida:** `imageassets/ui/carga/L04_Carga_Casino.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `R01_MeseraRobot.png`, `L10_LetreroVivaCoin.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with turquoise and magenta neon and orange sodium lights, soft fog, wet reflective ground). The SECOND attached image is the casino waitress robot and the THIRD is the casino sign: include both, redrawn in our style. A large cinematic 16:9 loading-screen illustration, dramatic composition with depth, NO HUD, NO text, NO letters, NO logos anywhere. Scene: the entrance of the VivaCoin casino at night: the big gold and magenta sign glowing above the doors (draw it as a shape with no readable text), two white and gold waitress robots with trays standing by the entrance, red carpet, gold lights, people arriving as silhouettes, light reflecting on the wet street.
```

## L05 — L05_Carga_Nova (fase 2)
- **Salida:** `imageassets/ui/carga/L05_Carga_Nova.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `E12_GuardiaPesadoNova.png`, `E13_DronAsalto.png`, `K06_CalleNova.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with turquoise and magenta neon and orange sodium lights, soft fog, wet reflective ground). The SECOND image is the heavy Nova guard, the THIRD the Nova assault drone and the FOURTH the Nova district: include them, redrawn in our style. A large cinematic 16:9 loading-screen illustration, dramatic composition with depth, NO HUD, NO text, NO letters, NO logos anywhere. Scene: the huge white Nova tower at night seen from below, its light at the top, cold blue fog, a line of heavy armored Nova guards at its base, and assault drones with red lights flying around the tower, searchlights in the sky.
```

## L06 — L06_Carga_Techos (fase 2)
- **Salida:** `imageassets/ui/carga/L06_Carga_Techos.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `PJ01_Protagonista_v2.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with turquoise and magenta neon and orange sodium lights, soft fog, wet reflective ground). The SECOND attached image is our protagonist: keep his face, curly hair with taper fade and the two shaved lines on the right side, silver hoop earring, black sleeveless shirt, black loose cargo pants and white high-top sneakers with a blue side stripe. A large cinematic 16:9 loading-screen illustration, dramatic composition with depth, NO HUD, NO text, NO letters, NO logos anywhere. Scene: the protagonist seen from behind and slightly to the side, sitting on the edge of a rooftop of the poor neighborhood, looking at the city at night: rooftops with water tanks, antennas and cables in the foreground, the glowing districts in the distance with neon colors and the Nova tower far away on the horizon.
```

## M33 — M33_Arrestado (fase 3)
- **Salida:** `imageassets/referencias/menus/M33_Arrestado.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `H10_HUD.png`, `PJ01_Protagonista_v2.png`, `V05_PatrullaSedan.png`

```
The FIRST attached image is our game's look and the SECOND is our HUD: match their style EXACTLY. Original design of our own, nothing copied from other games. All text in Spanish, spelled EXACTLY as written, no other words. Full 16:9 game screen. The third image is the protagonist and the fourth the police car. Screen: ARRESTED: the protagonist handcuffed and pushed against the hood of a police car, red and blue police lights washing over the scene, the whole image desaturated with only the red and blue lights in color, the normal HUD hidden. Big thin title in the center: "ARRESTADO". Below it, smaller: "-25 %  $ 3,112" and under that "Vuelves a casa". Thin lines with cut corners framing the text, in the HUD style.
```

## M34 — M34_HasMuerto (fase 3)
- **Salida:** `imageassets/referencias/menus/M34_HasMuerto.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `H10_HUD.png`, `PJ01_Protagonista_v2.png`

```
The FIRST attached image is our game's look and the SECOND is our HUD: match their style EXACTLY. Original design of our own, nothing copied from other games. All text in Spanish, spelled EXACTLY as written, no other words. Full 16:9 game screen. The third image is the protagonist. Screen: YOU DIED: the protagonist lying fallen on the wet street at night, seen from above at an angle, a red vignette around the edges of the screen, a digital glitch distortion on some horizontal slices, the normal HUD hidden. Big thin title in the center: "HAS MUERTO" in red. Below it, smaller: "Despiertas en la clínica del Doc". Thin lines with cut corners framing the text, in the HUD style.
```

## H42 — H42_IconosProtesisAtaque (fase 4)
- **Salida:** `imageassets/ui/menus/H42_IconosProtesisAtaque.png` · 16:9 · **Adjuntar:** `H41_IconosProtesis.png`, `IMP04_BrazoLaser.png`, `IMP05_HombreraMisiles.png`

```
The FIRST attached image is our prosthetic icon sheet: match its style EXACTLY (single thin turquoise line, same line weight, cells with a very thin dim turquoise outline and one cut corner, subtle glow). The second and third images are the implants to draw. A small icon sheet on a PURE BLACK (#000000) flat background with 2 cells of the same size side by side: 1) the cybernetic forearm opened with the laser cannon sliding out, 2) the shoulder pad with the 4-tube mini missile launcher. Same cell size as the reference, no text, no numbers.
```

## K08 — K08_CalleSuburbios_Cuadra (fase 5)
- **Salida:** `imageassets/referencias/look/K08_CalleSuburbios_Cuadra.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with soft fog, turquoise and magenta neon, orange sodium street lights, wet reflective ground with puddles, thick cables between poles). No HUD, no player character, no rain, no readable text, no logos: signs show only abstract shapes. Wide view at eye height. Short city blocks: the next corner is visible. Scene: one complete block of the Suburbios neighborhood seen from the sidewalk across the street: a row of attached two-story houses painted in different faded colors with water tanks on the roofs, a small corner shop with a rolled-up shutter and a glowing fridge, a car repair workshop with an open garage, an empty lot with a low painted wall and weeds, utility poles with orange lamps and cables, parked old cars, the corner at each end of the block visible.
```

## K09 — K09_Plaza_Barrio (fase 5)
- **Salida:** `imageassets/referencias/look/K09_Plaza_Barrio.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with soft fog, turquoise and magenta neon, orange sodium street lights, wet reflective ground with puddles, thick cables between poles). No HUD, no player character, no rain, no readable text, no logos: signs show only abstract shapes. Wide view at eye height. Short city blocks: the next corner is visible. Scene: a small neighborhood plaza between blocks: a small round kiosk with a dome roof in the center, benches, a few trees with round crowns, a couple of food stalls with tarp roofs and warm bulbs, a few people as small figures, two-story houses and small shops surrounding the plaza on all sides, streets entering at the corners.
```

## K10 — K10_CalleMercado (fase 5)
- **Salida:** `imageassets/referencias/look/K10_CalleMercado.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K02_Tianguis.png`, `K03_NeonAlto.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with soft fog, turquoise and magenta neon, orange sodium street lights, wet reflective ground with puddles, thick cables between poles). No HUD, no player character, no rain, no readable text, no logos: signs show only abstract shapes. Wide view at eye height. Short city blocks: the next corner is visible. Scene: The second and third images are the El Mercado and Neon Alto districts: this street is the transition between them. A busy commercial street: ground floor shops with metal shutters, some open with glowing interiors, many projecting signs and neon with abstract shapes, buildings of 3 to 4 floors with balconies, a few screens, more neon toward the far end, people on the sidewalks.
```

## LT01 — LT01_NeonTaqueria (fase 6)
- **Salida:** `imageassets/modelos-nuevos/LT01_NeonTaqueria.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** 3 × 1 m · **Tripo:** HD · 10000 · 2K

```
Use the attached image ONLY for the art style and colors. Stylized game asset with chunky simplified shapes, soft hand-painted gradients. A single large neon sign for a building facade, seen straight from the front with a very slight angle, complete, centered, isolated on a plain flat pure white background, no wall, soft even lighting, no shadow, no people. Thick neon tubes on a dark metal backing frame with mounting brackets. The text must be spelled EXACTLY as written, letter by letter, and no other text. Subject: a taco restaurant sign: the word "TAQUERÍA" (with the accent on the I) in big warm orange and yellow neon letters, with a neon taco shape and a small chili pepper.
```

## LT02 — LT02_NeonFarmacia24 (fase 6)
- **Salida:** `imageassets/modelos-nuevos/LT02_NeonFarmacia24.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** 3 × 0.9 m · **Tripo:** HD · 10000 · 2K

```
Use the attached image ONLY for the art style and colors. Stylized game asset with chunky simplified shapes, soft hand-painted gradients. A single large neon sign for a building facade, seen straight from the front with a very slight angle, complete, centered, isolated on a plain flat pure white background, no wall, soft even lighting, no shadow, no people. Thick neon tubes on a dark metal backing frame with mounting brackets. The text must be spelled EXACTLY as written, letter by letter, and no other text. Subject: a pharmacy sign: the words "FARMACIA 24 H" in white and green neon letters with a big green neon cross on the left.
```

## LT03 — LT03_NeonHotel (fase 6)
- **Salida:** `imageassets/modelos-nuevos/LT03_NeonHotel.png` · 9:16 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`
- **Medida:** 1.2 × 5 m · **Tripo:** HD · 10000 · 2K

```
Use the attached image ONLY for the art style and colors. Stylized game asset with chunky simplified shapes, soft hand-painted gradients. A single large neon sign for a building facade, seen straight from the front with a very slight angle, complete, centered, isolated on a plain flat pure white background, no wall, soft even lighting, no shadow, no people. Thick neon tubes on a dark metal backing frame with mounting brackets. The text must be spelled EXACTLY as written, letter by letter, and no other text. Subject: a tall vertical hotel sign: the word "HOTEL" written vertically, one letter under the other, in magenta neon letters, with turquoise neon stars between the letters.
```

## LT04 — LT04_NeonVivaCoin (fase 6)
- **Salida:** `imageassets/modelos-nuevos/LT04_NeonVivaCoin.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `L10_LetreroVivaCoin.png`
- **Medida:** 4 × 1.5 m · **Tripo:** HD · 10000 · 2K

```
Use the attached image ONLY for the art style and colors. Stylized game asset with chunky simplified shapes, soft hand-painted gradients. A single large neon sign for a building facade, seen straight from the front with a very slight angle, complete, centered, isolated on a plain flat pure white background, no wall, soft even lighting, no shadow, no people. Thick neon tubes on a dark metal backing frame with mounting brackets. The text must be spelled EXACTLY as written, letter by letter, and no other text. Subject: an advertising sign for the casino: the word "VIVACOIN" in big gold neon letters with a magenta neon outline, a neon gold coin on the left and a magenta arrow pointing right.
```


# Parte 26 — Pedido 17-nov: policía/Nova, plaza, NPCs, texturas, caída Nova, rueda EXO e íconos de tecla

Lote: `lote_17nov.json` · script: `generar_17nov.ps1`.

## V11P — V11_CamionetaPolicia (fase 1)
- **Salida:** `imageassets/modelos-nuevos/V11_CamionetaPolicia.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `V06_PatrullaCamioneta.png`
- **Medida:** 6 × 2.5 × 2.8 m · **Tripo:** HD · 18000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients); do not copy their scenes. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, ropes and bars are thick and chunky. The second image is our police pickup: use its police colors. Subject: an armored SWAT police truck: a big boxy armored vehicle with a tall armored body, small armored windows with dark glass, navy blue paint with turquoise stripes along the sides (no text, no badges), a chunky red and blue light bar on the roof, a heavy push bumper, side steps, a rear door, big off-road tires. The four wheels are clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. View: 3/4 view from the front left, slightly from above.
```

## NV01T — NV01_NaveTransporte (fase 1)
- **Salida:** `imageassets/modelos-nuevos/NV01_NaveTransporte.png` · 16:9 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K06_CalleNova.png`, `E13_DronAsalto.png`
- **Medida:** 12 m de largo · 7 m con rotores · **Tripo:** HD · 18000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients); do not copy their scenes. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, ropes and bars are thick and chunky. The second and third images show the Nova corporation look (white and blue, cold lights): use it. Subject: a futuristic troop transport aircraft like a helicopter without exposed blades: a sleek white and blue armored fuselage with a cockpit of dark glass in front, two large ducted rotors (round fans inside thick rings) on short wings on each side, a big side door slid fully OPEN showing the dark cabin with two thick rope-drop bars above the opening and two thick coiled ropes hanging just a little, landing skids, blue running lights and a red light under the nose. View: 3/4 view from the front left, slightly from above.
```

## W09 — W10_ArmaNova (fase 1)
- **Salida:** `imageassets/armas/W10_ArmaNova.png` · 16:9 · **Adjuntar:** `W07_RiflePlasma.png`, `W03_Fusil.png`
- **Medida:** 0.95 m · **Tripo:** HD · 12000 · 2K

```
Use the attached weapons ONLY for the art style, proportions and level of detail of our weapon set; this weapon is NOT worn: it is clean and corporate. Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, soft hand-painted gradients. Single object, the whole weapon fully visible, no hands, no person, no text, no logos, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow. Subject: the special energy rifle of the Nova corporation guards: a sleek glossy white body with smooth rounded panels, light grey and gunmetal details, a long barrel with a cold blue glowing emitter line along it, a glowing blue energy cell in the side, a compact sight, a clean stock. View: 3/4 view from the side, slightly from the front, showing the full length.
```

## H09 — H09_ArmaNova (fase 2)
- **Salida:** `imageassets/ui/hologramas/H09_ArmaNova.png` · 16:9 · **Adjuntar:** `W10_ArmaNova.png`

```
Turn the weapon in the attached image into a holographic projection for a video game user interface. Keep EXACTLY the same weapon shape, silhouette, parts and proportions, but shown perfectly from the side (pure side profile, orthographic, muzzle pointing to the right), centered and filling about 80% of the width. Translucent cyan-turquoise glowing body, bright glowing outlines on every part, faint inner wireframe lines, thin horizontal scan lines, a slight digital glitch on one or two slices, soft cyan glow. PURE BLACK (#000000) background everywhere, no text, no hands.
```

## AR01 — AR01_Arbol (fase 3)
- **Salida:** `imageassets/modelos-nuevos/AR01_Arbol.png` · 3:4 · **Adjuntar:** `K09_Plaza_Barrio.png`, `K01_Barrio_HUDnuevo.png`
- **Medida:** 4.5 m de alto · jardinera 1 × 1 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients); do not copy their scenes. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, ropes and bars are thick and chunky. Subject: a city sidewalk tree: a slim straight trunk, a medium round crown of leaves made of chunky simplified clumps, planted in a square low concrete planter box with a bit of soil, slightly dusty. View: 3/4 view from the front left, slightly from above.
```

## AR02 — AR02_Arbol (fase 3)
- **Salida:** `imageassets/modelos-nuevos/AR02_Arbol.png` · 1:1 · **Adjuntar:** `K09_Plaza_Barrio.png`, `K01_Barrio_HUDnuevo.png`
- **Medida:** 8 m de alto · copa de 9 m · **Tripo:** Smart Mesh P2 · 10000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients); do not copy their scenes. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, ropes and bars are thick and chunky. Subject: a big plaza tree like an Indian laurel: a thick short trunk with roots showing at the base, a very wide dense rounded crown of leaves made of large chunky clumps, a small round curb ring around the base. View: 3/4 view from the front left, slightly from above.
```

## AR03 — AR03_Palmera (fase 3)
- **Salida:** `imageassets/modelos-nuevos/AR03_Palmera.png` · 9:16 · **Adjuntar:** `K09_Plaza_Barrio.png`, `K01_Barrio_HUDnuevo.png`
- **Medida:** 12 m de alto · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients); do not copy their scenes. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, ropes and bars are thick and chunky. Subject: a tall avenue palm tree: a very tall slightly curved trunk with ring textures, a crown of thick chunky palm fronds at the top, a small round soil patch at the base. View: 3/4 view from the front, whole palm visible.
```

## PZ01 — PZ01_Banca (fase 3)
- **Salida:** `imageassets/modelos-nuevos/PZ01_Banca.png` · 16:9 · **Adjuntar:** `K09_Plaza_Barrio.png`, `K01_Barrio_HUDnuevo.png`
- **Medida:** 1.8 × 0.6 × 0.85 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients); do not copy their scenes. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, ropes and bars are thick and chunky. Subject: a plaza bench: thick wrought iron side frames with simple curls, wooden slats for the seat and backrest with chipped green paint, worn and weathered. View: 3/4 view from the front left, slightly from above.
```

## PZ02 — PZ02_Kiosco (fase 3)
- **Salida:** `imageassets/modelos-nuevos/PZ02_Kiosco.png` · 1:1 · **Adjuntar:** `K09_Plaza_Barrio.png`, `K01_Barrio_HUDnuevo.png`
- **Medida:** 6 m de diámetro · 5 m de alto · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients); do not copy their scenes. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, ropes and bars are thick and chunky. Subject: a small neighborhood plaza kiosk (bandstand): an octagonal raised concrete platform with three steps, thick metal posts with a simple railing, a pointed octagonal roof of painted corrugated metal sheet, a string of warm bulbs around the roof edge, a light hanging in the center, worn paint. View: 3/4 view from the front left, slightly from above.
```

## PZ03 — PZ03_Jardinera (fase 3)
- **Salida:** `imageassets/modelos-nuevos/PZ03_Jardinera.png` · 16:9 · **Adjuntar:** `K09_Plaza_Barrio.png`, `K01_Barrio_HUDnuevo.png`
- **Medida:** 2 × 1 × 0.5 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients); do not copy their scenes. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. Single object, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background, soft even studio lighting, no cast shadow, no people, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, ropes and bars are thick and chunky. Subject: a rectangular concrete planter for a plaza with chipped edges and stains, filled with soil and a few round chunky green bushes. View: 3/4 view from the front left, slightly from above.
```

## NP01 — NP01_Senora (fase 4)
- **Salida:** `imageassets/modelos-nuevos/NP01_Senora.png` · 1:1 · **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png`
- **Medida:** 1.58 m · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character, a regular citizen of a Mexican cyberpunk neighborhood. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps, no cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a middle-aged woman, short wavy dark hair, a fitted floral blouse, a knee-length skirt, flat shoes, and a woven market shopping bag worn on her shoulder held close to the body by a short strap (hands stay empty).
```

## NP02 — NP02_Trabajador (fase 4)
- **Salida:** `imageassets/modelos-nuevos/NP02_Trabajador.png` · 1:1 · **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png`
- **Medida:** 1.72 m · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character, a regular citizen of a Mexican cyberpunk neighborhood. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps, no cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a construction worker man, a hard hat, a reflective vest in orange with silver stripes over a grey t-shirt, work pants, work boots, gloves tucked in a pocket.
```

## NP03 — NP03_Joven (fase 4)
- **Salida:** `imageassets/modelos-nuevos/NP03_Joven.png` · 1:1 · **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png`
- **Medida:** 1.70 m · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character, a regular citizen of a Mexican cyberpunk neighborhood. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps, no cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a young man around 18, a fitted hoodie with the hood down, over-ear headphones resting around his neck, slim joggers, sneakers, a small turquoise LED on the headphones.
```

## NP04 — NP04_Senor (fase 4)
- **Salida:** `imageassets/modelos-nuevos/NP04_Senor.png` · 1:1 · **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png`
- **Medida:** 1.68 m · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character, a regular citizen of a Mexican cyberpunk neighborhood. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps, no cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: an older man around 60, grey moustache, a worn brown suit jacket closed with one button, a plain shirt without a tie, worn trousers, old dress shoes, a flat cap.
```

## NP05 — NP05_Vendedor (fase 4)
- **Salida:** `imageassets/modelos-nuevos/NP05_Vendedor.png` · 1:1 · **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png`
- **Medida:** 1.70 m · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character, a regular citizen of a Mexican cyberpunk neighborhood. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps, no cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a street food vendor man around 40, a plain white t-shirt, a fitted striped apron tied close to the body, a cap worn backwards, jeans, sneakers.
```

## NP06 — NP06_ChicaPunk (fase 4)
- **Salida:** `imageassets/modelos-nuevos/NP06_ChicaPunk.png` · 1:1 · **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png`
- **Medida:** 1.63 m · **Tripo:** Quads · 20000 · 2K · Humanoid (luego Mixamo)

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character, a regular citizen of a Mexican cyberpunk neighborhood. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps, no cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a punk young woman around 20, short spiky hair dyed magenta on one side, a fitted short leather jacket with small metal studs, a band-style t-shirt with an abstract print (no text), ripped fitted jeans, combat boots, a small nose ring.
```

## T02 — T02_Techo (fase 5)
- **Salida:** `imageassets/calle/T02_Techo.png` · 1:1 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
Use the attached image ONLY for the art style and colors. A seamless tileable texture seen straight from above (orthographic, flat, no perspective), filling the entire square image edge to edge, even flat lighting with no shadows, no objects, no text. The left and right edges and the top and bottom edges must continue seamlessly. Stylized hand-painted game texture, no photorealistic noise. Subject: a flat concrete rooftop covered with worn red waterproofing paint, large faded and peeled patches showing grey concrete, dark water stains, a few cracks and dirt.
```

## T03 — T03_Asfalto (fase 5)
- **Salida:** `imageassets/calle/T03_Asfalto.png` · 1:1 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
Use the attached image ONLY for the art style and colors. A seamless tileable texture seen straight from above (orthographic, flat, no perspective), filling the entire square image edge to edge, even flat lighting with no shadows, no objects, no text. The left and right edges and the top and bottom edges must continue seamlessly. Stylized hand-painted game texture, no photorealistic noise. Subject: old dark grey asphalt with darker rectangular patches of repairs, a network of thin cracks, a few dark oil stains and small loose gravel. No painted lines.
```

## F02 — F02_CaidaNova (fase 6)
- **Salida:** `imageassets/vfx/F02_CaidaNova.png` · 1:1 · **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
Use the attached image ONLY for the color mood. A landing impact effect sprite for a video game: a bright vertical column of blue-white light coming down from the top to the center, and at its base a flat glowing shockwave ring spreading on the ground seen in perspective from slightly above, small sparks and a soft blue glow. Stylized and clean. PURE BLACK (#000000) background everywhere, flat, no ground texture, no text.
```

## H43 — H43_RuedaEXO (fase 7)
- **Salida:** `imageassets/ui/menus/H43_RuedaEXO.png` · 16:9 · **Adjuntar:** `H10_HUD.png`, `K01_Barrio_HUDnuevo.png`, `M30_RuedaArmas.png`, `H42_IconosProtesisAtaque.png`

```
The FIRST attached image is our HUD and the SECOND our city look: match their style EXACTLY (thin turquoise lines, cut corners, yellow accent for the selected element, thin text). The THIRD image is our approved weapon wheel: make this one consistent with it. The FOURTH shows our attack implant icons. Original design, all text in Spanish spelled EXACTLY as written. Screen: the EXO ability wheel is open over the darkened, slightly blurred night street: a radial wheel in the center with 4 slots, each with a thin turquoise line icon and its name: "PIERNAS" (leg exoskeleton), "ESCUDO" (energy shield), "LÁSER" (forearm laser cannon), "MISILES" (shoulder missile launcher). "LÁSER" is selected and highlighted in yellow, and the center of the wheel shows "LÁSER" and a thin energy bar. Normal HUD hidden.
```

## H44 — H44_IconosTecla (fase 7)
- **Salida:** `imageassets/ui/menus/H44_IconosTecla.png` · 16:9 · **Adjuntar:** `H10_HUD.png`, `H41_IconosProtesis.png`

```
The attached images are our HUD and icon sheet: match their thin turquoise line style EXACTLY. An icon sheet on a PURE BLACK (#000000) flat background with 4 key prompt icons in a row, evenly spaced, each drawn with a single thin turquoise line and a subtle glow: 1) a square keyboard key with the letter "E", 2) a square keyboard key with the letter "F", 3) a long wide keyboard key with the word "ESPACIO", 4) a computer mouse outline with its LEFT button highlighted in yellow. No other text.
```


# Parte 27 — Pedido 17-nov noche: zonas nuevas K11–K18 y mapa expandido M12

Lote: `lote_17nov_b.json` · script: `generar_17nov_b.ps1` (fase 1: K11, K13–K18; fase 2: K12 y M12).

## K11 — K11_Malecon (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with soft fog, orange sodium street lights, turquoise and magenta neon, wet ground with reflections, thick cables strung between utility poles. A Mexican cyberpunk megacity. Third-person view at street level, the camera about 2 meters high, as it would look in the game. NO HUD, NO interface, NO player character, no rain, no readable text, no logos: signs and screens show only abstract shapes and colors. Original design, nothing copied from other games. 16:9. Scene: a long four-lane coastal avenue (malecón) with a wide open curve, a low concrete balustrade along the sea side, a row of tall palm trees and old street lamps with orange light, the sea at night on one side with the city skyline and its neon reflected on the dark water, a dark beach below the balustrade, a sleek sports car driving along the avenue with light trails, people walking on the wide promenade.
```

## K12 — K12_Malecon_Muelle (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K11_Malecon.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with soft fog, orange sodium street lights, turquoise and magenta neon, wet ground with reflections, thick cables strung between utility poles. A Mexican cyberpunk megacity. Third-person view at street level, the camera about 2 meters high, as it would look in the game. NO HUD, NO interface, NO player character, no rain, no readable text, no logos: signs and screens show only abstract shapes and colors. Original design, nothing copied from other games. 16:9. Scene: the far end of the same coastal avenue: a wooden fishermen's pier going out into the dark sea, small painted fishing boats (lanchas) tied to it, a small seafood stand at the start of the pier with a tarp roof and a single hanging bulb, stools and coolers, nets and crates, and a lighthouse in the distance sweeping its beam over the water. The second image is the malecón: keep it consistent if it exists.
```

## K13 — K13_ColoniaCerro (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K08_CalleSuburbios_Cuadra.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with soft fog, orange sodium street lights, turquoise and magenta neon, wet ground with reflections, thick cables strung between utility poles. A Mexican cyberpunk megacity. Third-person view at street level, the camera about 2 meters high, as it would look in the game. NO HUD, NO interface, NO player character, no rain, no readable text, no logos: signs and screens show only abstract shapes and colors. Original design, nothing copied from other games. 16:9. Scene: a hillside neighborhood: colorful small concrete houses stacked in steps one above the other up a steep hill, long concrete staircases with simple railings climbing between them, steep narrow alleys, rooftops with water tanks, antennas and laundry lines with clothes as simple shapes, utility poles with tangled cables, and below and far away the whole city lit up with neon colors.
```

## K14 — K14_CentroHistorico (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K09_Plaza_Barrio.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with soft fog, orange sodium street lights, turquoise and magenta neon, wet ground with reflections, thick cables strung between utility poles. A Mexican cyberpunk megacity. Third-person view at street level, the camera about 2 meters high, as it would look in the game. NO HUD, NO interface, NO player character, no rain, no readable text, no logos: signs and screens show only abstract shapes and colors. Original design, nothing copied from other games. 16:9. Scene: the main square of the historic downtown: a big colonial cathedral of pinkish-grey carved stone (cantera) with two bell towers, colonial buildings around the square with stone arches at ground level and wrought-iron balconies above, modern neon signs and big glowing screens mounted on top of the old buildings, food stalls with tarp roofs and warm bulbs in the square, people walking.
```

## K15 — K15_Puerto (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with soft fog, orange sodium street lights, turquoise and magenta neon, wet ground with reflections, thick cables strung between utility poles. A Mexican cyberpunk megacity. Third-person view at street level, the camera about 2 meters high, as it would look in the game. NO HUD, NO interface, NO player character, no rain, no readable text, no logos: signs and screens show only abstract shapes and colors. Original design, nothing copied from other games. 16:9. Scene: a cargo port at night: tall stacks of colorful shipping containers (rust red, blue, green, orange) forming corridors, enormous gantry cranes with red and white lights over the water, large warehouses with rolled shutters and bright floodlights, a big cargo ship docked with its lights on, puddles on the concrete reflecting everything, a few forklifts.
```

## K16 — K16_Deshuesadero (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with soft fog, orange sodium street lights, turquoise and magenta neon, wet ground with reflections, thick cables strung between utility poles. A Mexican cyberpunk megacity. Third-person view at street level, the camera about 2 meters high, as it would look in the game. NO HUD, NO interface, NO player character, no rain, no readable text, no logos: signs and screens show only abstract shapes and colors. Original design, nothing copied from other games. 16:9. Scene: an industrial zone: large corrugated metal warehouses, a junkyard with high piles of scrapped and crushed cars behind a chain-link fence with lights, and in front a long straight street used for illegal street races: a crowd standing on both sides, two tuned cars with neon underglow and big spoilers lined up at a painted start line, someone in the middle about to give the start signal with a raised arm.
```

## K17 — K17_LasLomas (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K06_CalleNova.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with soft fog, orange sodium street lights, turquoise and magenta neon, wet ground with reflections, thick cables strung between utility poles. A Mexican cyberpunk megacity. Third-person view at street level, the camera about 2 meters high, as it would look in the game. NO HUD, NO interface, NO player character, no rain, no readable text, no logos: signs and screens show only abstract shapes and colors. Original design, nothing copied from other games. 16:9. Scene: a wealthy residential street: modern minimalist houses of white concrete, wood and glass behind tall clean walls, a guard booth with a barrier arm at the entrance of the street, manicured gardens lit with cold white light, security cameras on the walls, clean sidewalks, trimmed trees, a few luxury cars parked, everything quiet and spotless, cold light instead of orange sodium.
```

## K18 — K18_AutopistaElevada (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K06_CalleNova.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with soft fog, orange sodium street lights, turquoise and magenta neon, wet ground with reflections, thick cables strung between utility poles. A Mexican cyberpunk megacity. Third-person view at street level, the camera about 2 meters high, as it would look in the game. NO HUD, NO interface, NO player character, no rain, no readable text, no logos: signs and screens show only abstract shapes and colors. Original design, nothing copied from other games. 16:9. Scene: an elevated highway: a concrete on-ramp climbing up from a poor neighborhood street with houses and shops below, the highway on tall concrete pillars curving away and entering a lit tunnel cut into a hill, lines of car headlights and taillights, big highway lamps, and the white towers of the Nova corporation glowing in the background.
```

## M12 — M12_MapaExpandido (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `M11_MapaCiudad.png`, `M10_MapaCiudad.png`

```
Use the FIRST attached image for the art style and color mood of our game, and the SECOND and THIRD images (our previous city maps) for the illustrated top-down map style: draw the expanded city map in the same style. Full-screen city map seen from directly above, like the pause-menu map of an open-world game, at night colors. No district borders as boxes: one continuous city of short blocks, the zones told apart by the color of their lights and their building types, blending at the edges. Layout: in the CENTER the four current districts: NOVA top-left (cold blue, office towers and the big tower), NEÓN ALTO top-right (magenta, casino and tall buildings), SUBURBIOS bottom-left (warm orange, low houses), EL MERCADO bottom-right (yellow, market streets). To the WEST: COLONIA DEL CERRO (small houses stacked on a hill with stairs). To the EAST, from top to bottom: CENTRO HISTÓRICO (a big square with a cathedral), ZONA INDUSTRIAL (warehouses and a junkyard), PUERTO (containers, cranes and docks on the water). To the NORTH: the elevated highway crossing the whole map, and behind it LAS LOMAS (large houses with gardens, green). To the SOUTH: the whole MALECÓN, a long curved coastal avenue along the sea, with a lighthouse on a point. The ONLY text on the map is each zone name, written once in thin capital letters, spelled exactly: "NOVA", "NEÓN ALTO", "SUBURBIOS", "EL MERCADO", "COLONIA DEL CERRO", "CENTRO HISTÓRICO", "ZONA INDUSTRIAL", "PUERTO", "LAS LOMAS", "MALECÓN". No icons with labels, no legend, no compass text.
```


# Parte 28 — Pedido grande 17-nov noche: vehículos y assets de la expansión del mapa

Lote: `lote_17nov_c.json` · script: `generar_17nov_c.ps1`.

## V12 — V12_Deportivo (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `V01a_CarroViejito.png` · **Medida:** 4.5 m · **Tripo:** HD · 18000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels (or the two wheels of a motorcycle) are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a low and wide two-door sports car of our own design, glossy black with a thin glowing turquoise neon line along the sides, sharp angular body, big rear spoiler.
```

## V13 — V13_Sedan (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `V01a_CarroViejito.png` · **Medida:** 4.6 m · **Tripo:** HD · 18000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels (or the two wheels of a motorcycle) are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a family four-door sedan, silver grey paint, a few dents and scratches, plain everyday design.
```

## V14 — V14_CompactoViejo (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `V01a_CarroViejito.png` · **Medida:** 3.8 m · **Tripo:** HD · 18000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels (or the two wheels of a motorcycle) are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. an old small compact car with round bubbly shapes, faded yellow paint with rust patches, round headlights, a dented fender.
```

## V15 — V15_Lujo (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `V01a_CarroViejito.png` · **Medida:** 5.2 m · **Tripo:** HD · 18000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels (or the two wheels of a motorcycle) are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a long luxury sedan, pearl white, very clean and polished, thin cold white LED headlights and light strips.
```

## V16 — V16_Microbus (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `V01a_CarroViejito.png` · **Medida:** 7.5 m · **Tripo:** HD · 18000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels (or the two wheels of a motorcycle) are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a Mexican city passenger minibus (microbús): boxy tall body painted green and white, worn and dented, a row of side windows, a folding passenger door on the side, a small roof destination box with no text.
```

## V17 — V17_Camion (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `V01a_CarroViejito.png` · **Medida:** 7.5 m · **Tripo:** HD · 18000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels (or the two wheels of a motorcycle) are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a medium cargo truck with a closed box body, red cab and grey box, worn, a roll-up door at the back.
```

## V18 — V18_Trailer (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `V01a_CarroViejito.png` · **Medida:** 16 m (tracto + plataforma) · **Tripo:** HD · 18000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels (or the two wheels of a motorcycle) are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a tractor truck with a blue cab hauling a flatbed trailer carrying one shipping container, worn, chrome exhaust stacks.
```

## V19 — V19_Ambulancia (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `V01a_CarroViejito.png` · **Medida:** 6 m · **Tripo:** HD · 18000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels (or the two wheels of a motorcycle) are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. an ambulance van, white with turquoise stripes, a red and blue light bar on the roof, a plain medical cross shape on the side (no text).
```

## MT01M — MT01_MotoDeportiva (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `V07_MotoReparto.png` · **Medida:** 2.1 m · **Tripo:** HD · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels (or the two wheels of a motorcycle) are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a faired sport motorcycle of our own design, magenta and black fairing, aggressive shape, small windscreen. View: 3/4 side view from the left.
```

## MT02 — MT02_MotoChopper (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `V07_MotoReparto.png` · **Medida:** 2.5 m · **Tripo:** HD · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels (or the two wheels of a motorcycle) are big, chunky and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a chopper motorcycle with a long front fork, tall ape-hanger handlebars, chrome and black, low seat, big rear tire. View: 3/4 side view from the left.
```

## ML01 — ML01_Farol (fase 2)
- **Adjuntar:** `K11_Malecon.png`, `K12_Malecon_Muelle.png` · **Medida:** 4.5 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a tall coastal promenade lamp post of dark iron with simple curls at the base, an ornate lantern on top glowing warm orange.
```

## ML02 — ML02_Lancha (fase 2)
- **Adjuntar:** `K11_Malecon.png`, `K12_Malecon_Muelle.png` · **Medida:** 6 m · **Tripo:** Smart Mesh P2 · 10000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a small wooden fishing boat (lancha) painted in bright colors (turquoise hull with an orange stripe), worn paint, a small outboard motor, coiled thick rope and a net inside.
```

## ML03 — ML03_PuestoMariscos (fase 2)
- **Adjuntar:** `K11_Malecon.png`, `K12_Malecon_Muelle.png` · **Medida:** 2.5 × 1.5 × 2.4 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a seafood street stand: a metal frame with a striped tarp roof, a front counter with a glass case of seafood as simple shapes, a big cooler, a single hanging bulb glowing warm, three plastic stools in front, close together so it reads as one object.
```

## ML04 — ML04_Faro (fase 2)
- **Adjuntar:** `K11_Malecon.png`, `K12_Malecon_Muelle.png` · **Medida:** 18 m · **Tripo:** Smart Mesh P2 · 10000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a lighthouse of white and red painted concrete in horizontal bands, a small balcony with a thick railing near the top, a glass lantern room with a bright light, on a small rocky base.
```

## CC01 — CC01_CasaCerro (fase 3)
- **Adjuntar:** `K13_ColoniaCerro.png` · **Medida:** 6 × 6 × 6.5 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a two-story concrete block house painted pink and faded, small windows with bars, a water tank on the flat roof, an exterior concrete staircase on one side going up to the second floor, cables to the roof.
```

## CC02 — CC02_CasaCerro (fase 3)
- **Adjuntar:** `K13_ColoniaCerro.png` · **Medida:** 6 × 6 × 3.5 m · **Tripo:** Smart Mesh P2 · 10000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a one-story concrete house painted turquoise and faded, a flat roof with steel rebar sticking up from the columns (unfinished second floor), a metal door and a small barred window.
```

## CC03 — CC03_CasaCerro (fase 3)
- **Adjuntar:** `K13_ColoniaCerro.png` · **Medida:** 4 × 7 × 9 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a narrow three-story house painted yellow, a wrought-iron balcony on the second floor, small windows, a water tank on the roof.
```

## CC04 — CC04_Tendedero (fase 3)
- **Adjuntar:** `K13_ColoniaCerro.png` · **Medida:** 3 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a rooftop clothesline: two metal posts with three thick lines between them and clothes hanging as simple chunky shapes (shirts, pants, a towel).
```

## CC05 — CC05_Lavadero (fase 3)
- **Adjuntar:** `K13_ColoniaCerro.png` · **Medida:** 1.2 × 0.6 × 0.9 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a concrete laundry sink (lavadero) with a ribbed washboard surface, a water tap, and a plastic bucket beside it touching it.
```

## PU01G — PU01_Grua (fase 4)
- **Adjuntar:** `K15_Puerto.png` · **Medida:** 45 m alto · boom 60 m · **Tripo:** HD · 15000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a container gantry crane: a giant red steel frame on four legs with a long horizontal boom, an operator cab, red and white warning lights, thick structural beams (no thin cables).
```

## PU02B — PU02_Barco (fase 4)
- **Adjuntar:** `K15_Puerto.png` · **Medida:** 120 m (fondo) · **Tripo:** Smart Mesh P2 · 15000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a cargo ship loaded with stacks of colorful containers on the deck, a white bridge tower at the back with lights, dark hull with a red bottom.
```

## PU03B — PU03_Bodega (fase 4)
- **Adjuntar:** `K15_Puerto.png` · **Medida:** 20 × 30 × 9 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a large warehouse of rusty corrugated metal sheet with a pitched roof, a big rolled-down metal shutter door, floodlights on the facade.
```

## PU04B — PU04_Bodega (fase 4)
- **Adjuntar:** `K15_Puerto.png` · **Medida:** 20 × 30 × 8 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a concrete warehouse with a flat roof and a raised loading dock with a ramp, two loading doors, bumpers and floodlights.
```

## PU05 — PU05_Montacargas (fase 4)
- **Adjuntar:** `K15_Puerto.png` · **Medida:** 3 × 1.2 × 2.2 m · **Tripo:** HD · 10000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a yellow forklift with a protective cage, two forks lowered, black tires, worn paint.
```

## PU06 — PU06_Pallets (fase 4)
- **Adjuntar:** `K15_Puerto.png` · **Medida:** 1.2 × 1 × 1.5 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a stack of wooden pallets with a few cardboard boxes and a wrapped crate on top.
```

## AU01 — AU01_Luminaria (fase 5)
- **Adjuntar:** `K18_AutopistaElevada.png` · **Medida:** 12 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a highway street light: a tall grey steel pole with two arms reaching to both sides, each ending in a long flat lamp head glowing cold white.
```

## IN01 — IN01_Nave (fase 6)
- **Adjuntar:** `K16_Deshuesadero.png` · **Medida:** 25 × 40 × 10 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: an industrial warehouse building of corrugated metal sheet with a gable (two-slope) roof, a big sliding door, small high windows, rust streaks.
```

## IN02 — IN02_Chatarra (fase 6)
- **Adjuntar:** `K16_Deshuesadero.png` · **Medida:** 5 × 3 × 3 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a pile of crushed scrapped cars stacked on top of each other, flattened and rusty, faded paint colors.
```

## IN03 — IN03_Tanques (fase 6)
- **Adjuntar:** `K16_Deshuesadero.png` · **Medida:** 10 × 10 × 12 m · **Tripo:** HD · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a group of three industrial storage tanks (cylinders) connected by thick pipes, with thick ladders and a catwalk with a solid railing, warning stripes.
```

## CH01 — CH01_Catedral (fase 7)
- **Adjuntar:** `K14_CentroHistorico.png` · **Medida:** 40 × 60 × 35 m · **Tripo:** Smart Mesh P2 · 15000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: an INVENTED colonial cathedral of pinkish-grey carved stone (not a copy of any real cathedral): a front facade with a large arched doorway, carved stone details, two bell towers with domes, a central dome behind. View: 3/4 view from the front.
```

## CH02 — CH02_EdificioColonial (fase 7)
- **Adjuntar:** `K14_CentroHistorico.png` · **Medida:** 15 × 10 × 8 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a two-story colonial building: stone arches at ground level forming a covered arcade, wrought-iron balconies on the upper floor, painted plaster walls in a faded color, a flat roof with a stone cornice.
```

## CH03 — CH03_EdificioColonial (fase 7)
- **Adjuntar:** `K14_CentroHistorico.png` · **Medida:** 15 × 10 × 12 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a three-story colonial building painted ochre, arches at ground level, wrought-iron balconies, and a big glowing neon screen frame mounted on the roof showing abstract shapes.
```

## CH04 — CH04_FarolColonial (fase 7)
- **Adjuntar:** `K14_CentroHistorico.png` · **Medida:** poste 3.5 m · de pared 0.8 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: two colonial street lanterns shown side by side: on the left a wall-mounted wrought-iron lantern with a bracket, on the right the same lantern on a tall iron post; warm light.
```

## LO01 — LO01_CasaModerna (fase 8)
- **Adjuntar:** `K17_LasLomas.png` · **Medida:** 15 × 12 × 7 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a modern two-story luxury house: white concrete volumes with warm wood panels, large glass windows lit with cold white light, a flat roof.
```

## LO02 — LO02_CasaModerna (fase 8)
- **Adjuntar:** `K17_LasLomas.png` · **Medida:** 15 × 12 × 7 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a modern two-story house of exposed concrete with tall floor-to-ceiling windows and a cantilevered upper floor.
```

## LO03 — LO03_CasaModerna (fase 8)
- **Adjuntar:** `K17_LasLomas.png` · **Medida:** 16 × 12 × 7 m · **Tripo:** Smart Mesh P2 · 12000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a modern two-story house with an integrated garage with a wide door, a terrace with a glass railing on the upper floor, white and grey.
```

## LO04 — LO04_Caseta (fase 8)
- **Adjuntar:** `K17_LasLomas.png` · **Medida:** 2.5 × 2.5 × 2.8 m · pluma 4 m · **Tripo:** Smart Mesh P2 · 8000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a small modern security guard booth with glass windows and a flat roof, and a long barrier arm (pluma) with red and white stripes beside it, attached.
```

## LO05 — LO05_Camara (fase 8)
- **Adjuntar:** `K17_LasLomas.png` · **Medida:** 4 m · **Tripo:** Smart Mesh P2 · 6000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a security camera on a pole: a thick grey pole with an arm holding a boxy camera with a small red light.
```

## LO06 — LO06_Arbusto (fase 8)
- **Adjuntar:** `K17_LasLomas.png` · **Medida:** 1 m · **Tripo:** Smart Mesh P2 · 6000 · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents); do not copy their scenes. Original design: do not copy any real car, brand or building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails, ropes and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a neatly trimmed round bush in a modern square concrete planter.
```

## T04 — T04_Arena (fase 9)
- **Adjuntar:** `K11_Malecon.png`

```
Use the attached image ONLY for the art style and colors. A seamless tileable texture seen straight from above (orthographic, flat, no perspective), filling the entire square image edge to edge, even flat lighting with no shadows, no objects, no text. The left and right edges and the top and bottom edges must continue seamlessly. Stylized hand-painted game texture, no photorealistic noise. Subject: wet beach sand, slightly darker damp areas, small shells and pebbles scattered.
```

## T05 — T05_ConcretoCerro (fase 9)
- **Adjuntar:** `K13_ColoniaCerro.png`

```
Use the attached image ONLY for the art style and colors. A seamless tileable texture seen straight from above (orthographic, flat, no perspective), filling the entire square image edge to edge, even flat lighting with no shadows, no objects, no text. The left and right edges and the top and bottom edges must continue seamlessly. Stylized hand-painted game texture, no photorealistic noise. Subject: rough grooved concrete of a steep street with anti-slip lines, cracks and dirt.
```

## T06 — T06_Adoquin (fase 9)
- **Adjuntar:** `K14_CentroHistorico.png`

```
Use the attached image ONLY for the art style and colors. A seamless tileable texture seen straight from above (orthographic, flat, no perspective), filling the entire square image edge to edge, even flat lighting with no shadows, no objects, no text. The left and right edges and the top and bottom edges must continue seamlessly. Stylized hand-painted game texture, no photorealistic noise. Subject: worn stone pavers (cantera cobblestones) in a regular pattern, rounded edges, dirt in the joints.
```


# Parte 29 — Pedido extra 18-nov: carros de carreras (V20–V23)

Lote: `lote_18nov.json` · script: `generar_18nov.ps1` (fase 1: V20–V22; fase 2: V23, prompt por confirmar).

## V20 — V20_Tuneado (fase 1)
- **Medida:** 4.3 m · **Tripo:** HD · 18,000 caras · 2K · **Adjuntar:** `K16_Deshuesadero.png`

```
Use the attached image ONLY for the art style and the look of its tuned street-race cars (illustrated, thin outlines, soft hand-painted gradients, Mexican cyberpunk); do not copy the scene. ORIGINAL car design of our own: it must NOT resemble any real car model or brand. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single car alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos, no brand badges, no license plate text. The four wheels are big and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. View: 3/4 view from the front left, slightly from above. Subject: a heavily tuned compact 1990s-style Japanese-inspired coupe of our own design: a wide body kit with flared fenders, a big rear wing, thin multi-spoke rims, very low suspension, a glowing turquoise neon underglow, glossy black paint with an abstract magenta vinyl graphic (shapes only, no text).
```

## V21 — V21_Tuneado (fase 1)
- **Medida:** 4.0 m · **Tripo:** HD · 18,000 caras · 2K · **Adjuntar:** `K16_Deshuesadero.png`

```
Use the attached image ONLY for the art style and the look of its tuned street-race cars (illustrated, thin outlines, soft hand-painted gradients, Mexican cyberpunk); do not copy the scene. ORIGINAL car design of our own: it must NOT resemble any real car model or brand. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single car alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos, no brand badges, no license plate text. The four wheels are big and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. View: 3/4 view from the front left, slightly from above. Subject: a tuned hatchback of our own design: riveted fender flares, a hood scoop air intake, a side exit exhaust pipe under the door, a glowing magenta neon underglow, white paint with orange and turquoise racing stripes.
```

## V22 — V22_Superdeportivo (fase 1)
- **Medida:** 4.6 m · **Tripo:** HD · 18,000 caras · 2K · **Adjuntar:** `K16_Deshuesadero.png`

```
Use the attached image ONLY for the art style and the look of its tuned street-race cars (illustrated, thin outlines, soft hand-painted gradients, Mexican cyberpunk); do not copy the scene. ORIGINAL car design of our own: it must NOT resemble any real car model or brand. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single car alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos, no brand badges, no license plate text. The four wheels are big and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. View: 3/4 view from the front left, slightly from above. Subject: a mid-engine supercar, extremely low and wedge-shaped, scissor doors closed, side air intakes behind the doors, glowing turquoise light strips along its edges, matte black paint with chrome details.
```

## V23 — V23_Superdeportivo (fase 2)
- **Medida:** 4.8 m · **Tripo:** HD · 18,000 caras · 2K · **Adjuntar:** `K16_Deshuesadero.png`

```
Use the attached image ONLY for the art style and the look of its tuned street-race cars (illustrated, thin outlines, soft hand-painted gradients, Mexican cyberpunk); do not copy the scene. ORIGINAL car design of our own: it must NOT resemble any real car model or brand. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single car alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos, no brand badges, no license plate text. The four wheels are big and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. View: 3/4 view from the front left, slightly from above. Subject: a futuristic hypercar with a bubble-shaped glass canopy cockpit, a long sleek low body, wheels partly covered by smooth fender skirts but still clearly separated from the body by visible gaps, pearl white paint with glowing magenta light lines, a small rear fin.
```


# Parte 30 — Pedido 18-nov: vistas aéreas A11–A18 y modelos V14 (v2), CH01 (v2), V23, V24

Lote: `lote_18nov_b.json` · script: `generar_18nov_b.ps1`. Las versiones anteriores de V14 y CH01 quedaron como `_v1`.

## A11 — A11_Malecon_Aerea (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `K12_Malecon_Muelle.png`

```
The FIRST attached image is our game's look for this zone: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with soft fog, orange sodium lights, turquoise and magenta neon, wet reflective ground). Draw an AERIAL ISOMETRIC view at 45 degrees from about 60 meters high, showing clearly and completely how the zone is laid out: streets, sidewalks, buildings and objects, as a layout guide to build it in a game. 16:9. NO HUD, NO text, NO letters, NO logos, NO people. Original design, nothing copied from real places. Zone: (the second image shows the pier end of the same malecón) a 200 meter stretch of coastal malecón in a gentle curve: a four-lane avenue, a wide sidewalk on the sea side with palm trees and street lamps evenly spaced about every 15 meters, a continuous balustrade, staircases down to the beach about every 60 meters, a sandy beach, a breakwater of large rocks; on the other side of the avenue a row of three-story buildings with shops on the ground floor. At the far end an L-shaped wooden pier with small fishing boats, the seafood stand at the start of the pier, and the lighthouse on rocks.
```

## A13 — A13_ColoniaCerro_Aerea (fase 1)
- **Adjuntar:** `K13_ColoniaCerro.png`

```
The FIRST attached image is our game's look for this zone: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with soft fog, orange sodium lights, turquoise and magenta neon, wet reflective ground). Draw an AERIAL ISOMETRIC view at 45 degrees from about 60 meters high, showing clearly and completely how the zone is laid out: streets, sidewalks, buildings and objects, as a layout guide to build it in a game. 16:9. NO HUD, NO text, NO letters, NO logos, NO people. Original design, nothing copied from real places. Zone: a hillside with colorful houses stacked in terraces, one street climbing the hill in zigzag switchbacks for cars, long straight pedestrian staircases cutting straight up between the terraces, flat rooftops almost touching each other so one can jump between them, water tanks and laundry lines on the roofs, and the city lights below at the bottom of the hill.
```

## A15 — A15_Puerto_Aerea (fase 1)
- **Adjuntar:** `K15_Puerto.png`

```
The FIRST attached image is our game's look for this zone: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with soft fog, orange sodium lights, turquoise and magenta neon, wet reflective ground). Draw an AERIAL ISOMETRIC view at 45 degrees from about 60 meters high, showing clearly and completely how the zone is laid out: streets, sidewalks, buildings and objects, as a layout guide to build it in a game. 16:9. NO HUD, NO text, NO letters, NO logos, NO people. Original design, nothing copied from real places. Zone: a container yard with long rows of stacked colorful containers separated by straight aisles, two gantry cranes standing on rails along the dock edge, a cargo ship moored at the dock, a row of warehouses with rolled shutters, a few forklifts, and an entrance road with a guard booth and barrier.
```

## A18 — A18_Autopista_Aerea (fase 1)
- **Adjuntar:** `K18_AutopistaElevada.png`

```
The FIRST attached image is our game's look for this zone: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with soft fog, orange sodium lights, turquoise and magenta neon, wet reflective ground). Draw an AERIAL ISOMETRIC view at 45 degrees from about 60 meters high, showing clearly and completely how the zone is laid out: streets, sidewalks, buildings and objects, as a layout guide to build it in a game. 16:9. NO HUD, NO text, NO letters, NO logos, NO people. Original design, nothing copied from real places. Zone: an elevated highway on tall concrete columns passing over a poor neighborhood of low houses and streets, an on-ramp climbing up from one of the neighborhood streets, the highway making a 90 degree curve and then entering a tunnel cut into a hill.
```

## A16 — A16_Deshuesadero_Aerea (fase 1)
- **Adjuntar:** `K16_Deshuesadero.png`

```
The FIRST attached image is our game's look for this zone: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with soft fog, orange sodium lights, turquoise and magenta neon, wet reflective ground). Draw an AERIAL ISOMETRIC view at 45 degrees from about 60 meters high, showing clearly and completely how the zone is laid out: streets, sidewalks, buildings and objects, as a layout guide to build it in a game. 16:9. NO HUD, NO text, NO letters, NO logos, NO people. Original design, nothing copied from real places. Zone: an industrial zone with several large metal warehouses, a fenced junkyard full of piles of scrapped cars, and a straight drag-race strip about 300 meters long with a painted start line and improvised bleachers made of scaffolding and stacked tires along both sides.
```

## A14 — A14_Centro_Aerea (fase 1)
- **Adjuntar:** `K14_CentroHistorico.png`

```
The FIRST attached image is our game's look for this zone: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with soft fog, orange sodium lights, turquoise and magenta neon, wet reflective ground). Draw an AERIAL ISOMETRIC view at 45 degrees from about 60 meters high, showing clearly and completely how the zone is laid out: streets, sidewalks, buildings and objects, as a layout guide to build it in a game. 16:9. NO HUD, NO text, NO letters, NO logos, NO people. Original design, nothing copied from real places. Zone: a square main plaza with a small round kiosk in the center, trees and benches, an INVENTED colonial cathedral of pink carved stone on one side (one tall bell tower on one side, a tiled dome, a facade with a big arch; NOT the Mexico City cathedral and not any real church), colonial arcades (portales) with stone arches along the other three sides, cobblestone streets around the plaza, neon signs on top of the old buildings.
```

## A17 — A17_Lomas_Aerea (fase 1)
- **Adjuntar:** `K17_LasLomas.png`

```
The FIRST attached image is our game's look for this zone: match its illustrated art style EXACTLY (thin outlines, painted rendering, blue night with soft fog, orange sodium lights, turquoise and magenta neon, wet reflective ground). Draw an AERIAL ISOMETRIC view at 45 degrees from about 60 meters high, showing clearly and completely how the zone is laid out: streets, sidewalks, buildings and objects, as a layout guide to build it in a game. 16:9. NO HUD, NO text, NO letters, NO logos, NO people. Original design, nothing copied from real places. Zone: a private gated street: a guard booth with a barrier arm at the entrance, and on both sides of the street modern luxury houses, each with a front garden lit with cold light, a garage, and a tall perimeter wall, clean sidewalks with trimmed trees.
```

## V14 — V14_CompactoViejo (fase 2)
- **Adjuntar:** `K13_ColoniaCerro.png` · **Medida:** 3.5 m · **Tripo:** HD · 18,000 caras · 2K

```
Use the attached image ONLY for the art style and colors of our game (illustrated, thin outlines, soft hand-painted gradients). ORIGINAL design of our own: it must NOT resemble any real car model, brand or real building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. View: 3/4 view from the front left, slightly from above. Subject: The four wheels are big and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. an old small two-door compact car of our own design: short and BOXY with flat panels and square corners (NOT round, not beetle-like), a short flat hood, square headlights, a short upright rear, faded yellow paint with rust patches and a dented door.
```

## CH01 — CH01_Catedral (fase 2)
- **Adjuntar:** `K14_CentroHistorico.png` · **Medida:** 35 × 55 × 40 m (torre) · **Tripo:** Smart Mesh P2 · 15,000 caras · 2K

```
Use the attached image ONLY for the art style and colors of our game (illustrated, thin outlines, soft hand-painted gradients). ORIGINAL design of our own: it must NOT resemble any real car model, brand or real building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. View: 3/4 view from the front left, slightly from above. Subject: an INVENTED colonial cathedral of pink carved stone (cantera rosa), NOT the Mexico City cathedral and not any real church: an asymmetric facade with ONE single tall bell tower on the left side, a large dome covered in colorful glazed tiles (azulejo) behind, and the main facade dominated by one big arch over the entrance with carved stone details.
```

## V23 — V23_Superdeportivo (fase 2)
- **Adjuntar:** `K16_Deshuesadero.png` · **Medida:** 4.8 m · **Tripo:** HD · 18,000 caras · 2K

```
Use the attached image ONLY for the art style and colors of our game (illustrated, thin outlines, soft hand-painted gradients). ORIGINAL design of our own: it must NOT resemble any real car model, brand or real building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. View: 3/4 view from the front left, slightly from above. Subject: The four wheels are big and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a futuristic hypercar with a bubble-shaped glass canopy cockpit, a long sleek low body, wheels half covered by smooth fender skirts but still clearly separated from the body by visible gaps, taillights as one single continuous red line across the rear, pearl magenta paint.
```

## V24 — V24_Superdeportivo (fase 2)
- **Adjuntar:** `K16_Deshuesadero.png` · **Medida:** 4.9 m · **Tripo:** HD · 18,000 caras · 2K

```
Use the attached image ONLY for the art style and colors of our game (illustrated, thin outlines, soft hand-painted gradients). ORIGINAL design of our own: it must NOT resemble any real car model, brand or real building. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. View: 3/4 view from the front left, slightly from above. Subject: The four wheels are big and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a cyberpunk grand tourer with a very long hood, a two-seat cabin set far back, sharp angular lines, glowing yellow neon light strips, silver paint with exposed carbon fiber panels on the hood, roof and side skirts.
```


# Parte 31 — Pedido 18-nov mediodía: detalle de zonas, texturas, NPCs, enemigos y drones (2 variantes _a/_b)

Lote: `lote_18nov_c.json` · script: `generar_18nov_c.ps1`. La variante _b usa el mismo prompt más una instrucción de diseño distinto.

## ML05 — ML05_EdificioMalecon (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `K12_Malecon_Muelle.png` · **Medida:** 12 × 10 × 10 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a three-story seafront building: ground floor shops with rolled metal shutters and colorful awnings, balconies with simple railings on the upper floors, air conditioning units, a small neon sign with an abstract shape (no letters), faded paint and salt stains.
```

## ML06 — ML06_EdificioMalecon (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `K12_Malecon_Muelle.png` · **Medida:** 14 × 12 × 17 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: an old five-story seafront hotel: rows of windows, some lit with warm light, small balconies, a tall vertical neon sign on the corner made of abstract glowing shapes (no letters), faded pastel paint, salt stains.
```

## ML07 — ML07_Hielera (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `K12_Malecon_Muelle.png` · **Medida:** 0.7 × 0.5 × 0.5 m · **Tripo:** Smart Mesh P2 · 6,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a plastic beach cooler with a hinged lid next to a short stack of colorful plastic stools (red, blue, yellow), touching each other as one group.
```

## SK01 — SK01_Skyline (fase 1)
- **Adjuntar:** `K11_Malecon.png`, `K06_CalleNova.png`

```
Use the attached images ONLY for the art style and colors. A distant city skyline at night seen straight from the front as a flat backdrop: towers and buildings of different heights as dark silhouettes with many small lit windows, glowing neon signs in turquoise and magenta as abstract shapes, the white Nova tower taller than the rest with a light on top, filling the image width edge to edge, the bottom edge of the skyline flat. Everything else is PURE BLACK (#000000): the sky and the background are pure black, no gradient, no stars, no water, no text.
```

## PU07 — PU07_PlumaCaseta (fase 1)
- **Adjuntar:** `K15_Puerto.png` · **Medida:** 2.5 × 2.5 × 3 m · pluma 5 m · **Tripo:** Smart Mesh P2 · 8,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: the entrance booth of a cargo port: a small boxy guard booth with windows and a flat roof, a floodlight, and attached to it a long barrier arm with red and white stripes in the lowered position.
```

## PU08 — PU08_ContenedorGraffiti (fase 1)
- **Adjuntar:** `K15_Puerto.png` · **Medida:** 12.2 × 2.4 × 2.6 m · **Tripo:** Smart Mesh P2 · 8,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a 40-foot shipping container with corrugated walls, rust patches and dents, the doors closed with thick locking bars, covered on its long side with colorful graffiti made of abstract shapes and characters (no readable letters).
```

## CC06 — CC06_CasaCerro (fase 1)
- **Adjuntar:** `K13_ColoniaCerro.png` · **Medida:** 6 × 6 × 6.5 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a two-story house half under construction: unpainted grey concrete blocks, steel rebar sticking up from the columns on the roof, an unfinished second floor partly covered with a blue tarp, a simple metal door.
```

## CC07 — CC07_CasaCerro (fase 1)
- **Adjuntar:** `K13_ColoniaCerro.png` · **Medida:** 4 × 7 × 9.5 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a narrow three-story house painted green and faded, small barred windows, a roof terrace with a metal railing and a clothesline with clothes as chunky shapes, a water tank.
```

## CC08 — CC08_MuroContencion (fase 1)
- **Adjuntar:** `K13_ColoniaCerro.png` · **Medida:** 8 m de ancho × 4 m de alto · **Tripo:** Smart Mesh P2 · 10,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a hillside retaining wall section made of stacked stones and concrete with drainage holes, and a concrete staircase with a simple metal railing attached along its face climbing to the top.
```

## AU02 — AU02_BarreraConcreto (fase 1)
- **Adjuntar:** `K18_AutopistaElevada.png` · **Medida:** 3 × 0.6 × 0.8 m · **Tripo:** Smart Mesh P2 · 6,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a highway concrete barrier segment (New Jersey profile) with reflective yellow and white stripes, chipped edges and dirt.
```

## AU03 — AU03_ChatarraPuente (fase 1)
- **Adjuntar:** `K18_AutopistaElevada.png` · **Medida:** 8 × 5 × 2 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a pile of junk that gathers under a highway bridge: two wrecked old cars without wheels, tires, black trash bags, a broken shopping cart, cardboard and planks, all grouped together as one object.
```

## T07 — T07_AsfaltoMojado (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
Use the attached image ONLY for the art style and colors. A seamless tileable texture seen straight from above (orthographic, flat, no perspective), filling the entire square image edge to edge, even flat lighting, no objects, no text. The left and right edges and the top and bottom edges must continue seamlessly. Stylized hand-painted game texture, no photorealistic noise. Subject: dark wet asphalt with irregular puddles that reflect orange and magenta light as soft color patches, small cracks.
```

## T08 — T08_LaminaOxidada (fase 2)
- **Adjuntar:** `K16_Deshuesadero.png`

```
Use the attached image ONLY for the art style and colors. A seamless tileable texture seen straight from above (orthographic, flat, no perspective), filling the entire square image edge to edge, even flat lighting, no objects, no text. The left and right edges and the top and bottom edges must continue seamlessly. Stylized hand-painted game texture, no photorealistic noise. Subject: corrugated metal sheet seen straight on, vertical ridges, heavy rust and peeling paint in faded blue and white.
```

## T09 — T09_ConcretoManchado (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
Use the attached image ONLY for the art style and colors. A seamless tileable texture seen straight from above (orthographic, flat, no perspective), filling the entire square image edge to edge, even flat lighting, no objects, no text. The left and right edges and the top and bottom edges must continue seamlessly. Stylized hand-painted game texture, no photorealistic noise. Subject: sidewalk concrete slabs with joint lines, dark stains, cracks and flattened chewing gum spots.
```

## NP07 — NP07_Pescador (fase 3)
- **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png` · **Medida:** 1.70 m · **Tripo:** Quads · 20,000 caras · 2K

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character of a Mexican cyberpunk megacity. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps or cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a fisherman around 50, a cap, a worn shirt with rolled sleeves, waterproof bib pants, tall rubber boots.
```

## NP08 — NP08_Turista (fase 3)
- **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png` · **Medida:** 1.75 m · **Tripo:** Quads · 20,000 caras · 2K

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character of a Mexican cyberpunk megacity. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps or cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a tourist man, a bright flowery short-sleeve shirt, shorts, sandals, sunglasses on his head, a small camera hanging close on his chest with a short strap.
```

## NP09 — NP09_Corredor (fase 3)
- **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png` · **Medida:** 1.65 m · **Tripo:** Quads · 20,000 caras · 2K

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character of a Mexican cyberpunk megacity. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps or cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a young woman runner, fitted sportswear with thin glowing turquoise light strips, a headband, running shoes.
```

## NP10 — NP10_Elotero (fase 3)
- **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png` · **Medida:** 1.68 m · **Tripo:** Quads · 20,000 caras · 2K

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character of a Mexican cyberpunk megacity. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps or cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a street corn vendor man (elotero), a fitted apron, a cap, a plain t-shirt, jeans, sneakers.
```

## NP11 — NP11_Estibador (fase 3)
- **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png` · **Medida:** 1.78 m · **Tripo:** Quads · 20,000 caras · 2K

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character of a Mexican cyberpunk megacity. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps or cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a dock worker, an orange reflective vest, a hard hat, work gloves, work pants, boots.
```

## NP12 — NP12_Abuela (fase 3)
- **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png` · **Medida:** 1.52 m · **Tripo:** Quads · 20,000 caras · 2K

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character of a Mexican cyberpunk megacity. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps or cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: an elderly grandmother, a rebozo shawl wrapped tightly around her shoulders and tucked in, a long skirt, flat shoes, grey hair in a bun; her walking cane is strapped flat along her right forearm so the hands stay empty.
```

## NP13 — NP13_Repartidor (fase 3)
- **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png` · **Medida:** 1.72 m · **Tripo:** Quads · 20,000 caras · 2K

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character of a Mexican cyberpunk megacity. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps or cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a delivery rider, a jacket, an insulated delivery box worn as a backpack tight on his back, a cap, pants, sneakers.
```

## NP14 — NP14_Oficinista (fase 3)
- **Adjuntar:** `PJ01_Protagonista_v2.png`, `BaseMujer_frente.png` · **Medida:** 1.74 m · **Tripo:** Quads · 20,000 caras · 2K

```
Use the attached characters for the art style of our game (rendering, proportions, level of detail) and the T-pose framing; draw a NEW original character of a Mexican cyberpunk megacity. Rig-friendly: everything fitted close to the body, nothing hanging loose, no loose straps or cables. Hands EMPTY. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: an office worker, a fitted suit with a thin tie, slim augmented reality glasses with a faint turquoise glow, dress shoes.
```

## E15 — E15_CentinelaNova (fase 4)
- **Adjuntar:** `E12_GuardiaPesadoNova.png`, `K06_CalleNova.png` · **Medida:** 1.90 m · **Tripo:** Quads · 20,000 caras · 2K

```
Use the attached images for the art style of our game and the framing; draw a NEW original design. Nothing copied from other games. Rig-friendly: everything fitted close to the body, nothing hanging loose. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a humanoid security robot of the Nova corporation: slim clean white armored body with blue stripes, a smooth head with a single horizontal glowing blue visor line, gunmetal joints.
```

## E16 — E16_RobotPesadoNova (fase 4)
- **Adjuntar:** `E12_GuardiaPesadoNova.png`, `K06_CalleNova.png` · **Medida:** 3.0 m · **Tripo:** Quads · 25,000 caras · 2K

```
Use the attached images for the art style of our game and the framing; draw a NEW original design. Nothing copied from other games. Rig-friendly: everything fitted close to the body, nothing hanging loose. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a big heavy bipedal Nova robot about 3 meters tall: massive white and blue armored torso, short thick legs, a large riot shield built into the left forearm, a heavy cannon built into the right forearm, a small sensor head with a blue light.
```

## E17 — E17_PerroRobotNova (fase 4)
- **Adjuntar:** `E12_GuardiaPesadoNova.png`, `K06_CalleNova.png` · **Medida:** 1.2 m de largo · **Tripo:** HD · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a four-legged Nova patrol robot dog: white and blue armored body, four jointed legs standing, a head with a bright searchlight, a small compact turret mounted on its back, a blue light strip.
```

## E18 — E18_EliteNova (fase 4)
- **Adjuntar:** `E12_GuardiaPesadoNova.png`, `K06_CalleNova.png` · **Medida:** 1.90 m · **Tripo:** Quads · 20,000 caras · 2K

```
Use the attached images for the art style of our game and the framing; draw a NEW original design. Nothing copied from other games. Rig-friendly: everything fitted close to the body, nothing hanging loose. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: an elite Nova soldier in full white and blue armor, a closed helmet with a blue visor, armored arms and legs, and a compact jetpack fitted flat on the back with two short thrusters.
```

## E19 — E19_PandilleroSkulls (fase 4)
- **Adjuntar:** `SkullHombre_frente.png`, `JefeSkulls_frente.png` · **Medida:** 1.78 m · **Tripo:** Quads · 20,000 caras · 2K

```
Use the attached images for the art style of our game and the framing; draw a NEW original design. Nothing copied from other games. Rig-friendly: everything fitted close to the body, nothing hanging loose. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a SKULLS gang member: a fitted black leather jacket zipped, a skull mask (a magenta holographic skull face), fitted dark pants, boots, magenta details.
```

## E20 — E20_MatonSkulls (fase 4)
- **Adjuntar:** `SkullHombre_frente.png`, `JefeSkulls_frente.png` · **Medida:** 2.05 m · **Tripo:** Quads · 20,000 caras · 2K

```
Use the attached images for the art style of our game and the framing; draw a NEW original design. Nothing copied from other games. Rig-friendly: everything fitted close to the body, nothing hanging loose. No text, no logos. Pose: strict T-pose, facing the camera directly, arms straight out horizontally at shoulder height, palms down, fingers together, legs straight and slightly apart, full body visible from head to feet. Isolated on a plain flat pure white background, soft even lighting, no shadow. Character: a huge thug of the SKULLS gang: very broad and tall, a sleeveless fitted shirt, a rusty heavy robotic right arm, a skull tattoo on the chest, a bandana over the lower face with a skull print, cargo pants, boots.
```

## DR02 — DR02_DronPesado (fase 5)
- **Adjuntar:** `E13_DronAsalto.png`, `DR01_Dron.png` · **Medida:** 1.8 m de ancho · **Tripo:** HD · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. Subject: The rotors or propellers are clearly separated from the body by visible gaps so they can be cut off as separate pieces. View: 3/4 view from the front, slightly from above. a large heavy assault drone of Nova: an armored white and blue body, four big rotors in round guards on thick arms, two machine guns mounted under the body, a red eye light.
```

## DR03 — DR03_DronCarga (fase 5)
- **Adjuntar:** `E13_DronAsalto.png`, `DR01_Dron.png` · **Medida:** 3 m de ancho · **Tripo:** HD · 15,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. Subject: The rotors or propellers are clearly separated from the body by visible gaps so they can be cut off as separate pieces. View: 3/4 view from the front, slightly from above. a large cargo drone of Nova: a long white body with six rotors, carrying under it a closed armored drop capsule hanging from two thick short clamps.
```

## DR04 — DR04_DronExplorador (fase 5)
- **Adjuntar:** `E13_DronAsalto.png`, `DR01_Dron.png` · **Medida:** 0.4 m · **Tripo:** HD · 6,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. Subject: The rotors or propellers are clearly separated from the body by visible gaps so they can be cut off as separate pieces. View: 3/4 view from the front, slightly from above. a small fast scout drone for swarms: a compact rounded body with a single ducted propeller on top, a small camera eye glowing blue, two tiny fins.
```

## NV03 — NV03_NaveNova (fase 5)
- **Adjuntar:** `NV01_NaveTransporte.png`, `K06_CalleNova.png` · **Medida:** 10 m de largo · **Tripo:** HD · 18,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity; Nova is clean white with cold blue light); do not copy their scenes. Original design, nothing copied from real places, brands or other games. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. Subject: The rotors or propellers are clearly separated from the body by visible gaps so they can be cut off as separate pieces. View: 3/4 view from the front, slightly from above. a Nova combat aircraft: a sleek clean white armored body with NO visible cockpit (smooth armored nose), two ducted rotors on short wings, weapon pods under the wings, cold blue running lights.
```


# Parte 32 — Pedido 19-nov noche: vehículos, edificios, muro, cobertura y charcos (2 variantes _a/_b)

Lote: `lote_19nov.json` · script: `generar_19nov.ps1`.

## PA01 — PA01_Patrulla (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `V05_PatrullaSedan.png` · **Medida:** 4.7 m · **Tripo:** HD · 18,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels are big and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a city police patrol sedan of our own design: navy blue body with reflective turquoise stripes along the sides (no text, no badges), a light bar on the roof with red and blue blocks, a reinforced push bumper at the front, plain steel wheels.
```

## PA02 — PA02_PatrullaSUV (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `V06_PatrullaCamioneta.png` · **Medida:** 5.0 m · **Tripo:** HD · 18,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels are big and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a police patrol SUV of our own design: tall boxy navy blue body with the same reflective turquoise stripes (no text, no badges), a larger roof light bar with red and blue blocks, a reinforced push bumper, steel wheels.
```

## V22 — V22_Superdeportivo (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K16_Deshuesadero.png` · **Medida:** 4.6 m · **Tripo:** HD · 18,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: The four wheels are big and clearly separated from the body by visible dark gaps and deep wheel arches, so they can be cut off as separate pieces. Doors, hood and trunk have clearly marked panel cut lines. Windows are dark tinted solid glass. a mid-engine supercar of our own design, very low and wedge-shaped, with a SIMPLE CLEAN body made of few large smooth panels (no loose small parts, no separate wings or fins sticking out), matte black paint with thin glowing turquoise light strips along its edges.
```

## ED01 — ED01_CasaSuburbios (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png` · **Medida:** 7 × 8 × 6.5 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: A complete building: the full front facade, the roof and one side wall clearly visible so the 3D model can be closed; flat roof unless stated. a two-story Suburbios house with a flat roof, faded terracotta paint: a rolled-down metal shutter on the ground floor, a window with security bars, a water tank on a red brick base on the roof, an electricity meter and a caged wall lamp glowing orange.
```

## ED02 — ED02_TienditaEsquina (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png` · **Medida:** 8 × 8 × 6.5 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: A complete building: the full front facade, the roof and one side wall clearly visible so the 3D model can be closed; flat roof unless stated. a two-story corner shop building with a flat roof, faded turquoise and cream paint, cut corner entrance: a corrugated metal awning over the shop, a glowing soda refrigerator outside the door, a small neon sign with an abstract shape (no letters).
```

## ED03 — ED03_EdificioDeptos (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png` · **Medida:** 10 × 10 × 10 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: A complete building: the full front facade, the roof and one side wall clearly visible so the 3D model can be closed; flat roof unless stated. a three-story apartment building with a flat roof, faded sea-green paint: balconies with laundry as chunky shapes, air conditioning units, an exterior concrete staircase with a solid railing on the side.
```

## ED04 — ED04_LocalesMercado (fase 2)
- **Adjuntar:** `K02_Tianguis.png`, `K10_CalleMercado.png` · **Medida:** 15 × 8 × 7 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: A complete building: the full front facade, the roof and one side wall clearly visible so the 3D model can be closed; flat roof unless stated. a two-story market building with a row of three shops on the ground floor, each with a metal shutter half open and a colorful tarp awning (pink, yellow, turquoise), and windows on the second floor.
```

## ED05 — ED05_EdificioMercado (fase 2)
- **Adjuntar:** `K02_Tianguis.png`, `K10_CalleMercado.png` · **Medida:** 10 × 10 × 13 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: A complete building: the full front facade, the roof and one side wall clearly visible so the 3D model can be closed; flat roof unless stated. a four-story commercial building with hand-painted advertisements on its walls made of abstract shapes and pictures (no letters), thick bundled cables and antennas on the roof, shops on the ground floor.
```

## ED06 — ED06_EdificioNeon (fase 2)
- **Adjuntar:** `K03_NeonAlto.png` · **Medida:** 12 × 10 × 17 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: A complete building: the full front facade, the roof and one side wall clearly visible so the 3D model can be closed; flat roof unless stated. a five-story building of the Neon Alto district: glowing screens with abstract images on the facade, tall vertical neon signs in magenta and turquoise made of abstract shapes (no letters), balconies with warm light.
```

## ED07 — ED07_HotelNeon (fase 2)
- **Adjuntar:** `K03_NeonAlto.png` · **Medida:** 8 × 12 × 20 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: A complete building: the full front facade, the roof and one side wall clearly visible so the 3D model can be closed; flat roof unless stated. a narrow six-story hotel: an illuminated marquee canopy over the entrance with rows of bulbs, a tall vertical neon sign along the corner made of abstract shapes (no letters), rows of small windows.
```

## ED08 — ED08_OficinaNova (fase 2)
- **Adjuntar:** `K06_CalleNova.png` · **Medida:** 15 × 15 × 21 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: A complete building: the full front facade, the roof and one side wall clearly visible so the 3D model can be closed; flat roof unless stated. a six-story corporate office building of Nova: dark glass curtain walls framed by clean white panels with thin cold blue light stripes, very clean.
```

## ED09 — ED09_PlantaBajaNova (fase 2)
- **Adjuntar:** `K06_CalleNova.png` · **Medida:** 15 × 15 × 6 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: A complete building: the full front facade, the roof and one side wall clearly visible so the 3D model can be closed; flat roof unless stated. the ground floor base of a Nova tower: a tall glass lobby with doors, thick white columns, a flat canopy with cold white downlights, the top of the piece flat so a tower can be stacked on it.
```

## ED10 — ED10_Bodega (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K16_Deshuesadero.png` · **Medida:** 15 × 20 × 7 m · **Tripo:** Smart Mesh P2 · 10,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: A complete building: the full front facade, the roof and one side wall clearly visible so the 3D model can be closed; flat roof unless stated. a warehouse of rusty corrugated metal sheet with a low pitched roof, a big rolled-down metal shutter door and a small side door, a floodlight.
```

## ED11 — ED11_EdificioAbandonado (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png` · **Medida:** 10 × 10 × 10 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: A complete building: the full front facade, the roof and one side wall clearly visible so the 3D model can be closed; flat roof unless stated. an abandoned three-story building: windows boarded up with planks and metal sheets, cracked walls, stains, colorful abstract graffiti shapes (no readable letters), a broken door.
```

## ED12 — ED12_Estacionamiento (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K06_CalleNova.png` · **Medida:** 30 × 20 × 9 m · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: A complete building: the full front facade, the roof and one side wall clearly visible so the 3D model can be closed; flat roof unless stated. a three-level open concrete parking garage: thick columns and horizontal slabs with low parapet walls, a ramp visible on one side, a few lights; no cars.
```

## MU01 — MU01_MuroPerimetral (fase 3)
- **Adjuntar:** `K06_CalleNova.png`, `K16_Deshuesadero.png` · **Medida:** 8 m alto × 10 m de tramo · **Tripo:** Smart Mesh P2 · 10,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a modular section of a tall city perimeter wall, 8 meters high: concrete panels reinforced with metal beams, thick pipes running along it, amber warning lights, yellow and black hazard stripes at the base, and razor wire coils on top drawn as thick chunky coils. The left and right ends are flat and clean so sections connect side by side. View: 3/4 view from the front.
```

## MU02 — MU02_TorreControl (fase 3)
- **Adjuntar:** `K06_CalleNova.png`, `K16_Deshuesadero.png` · **Medida:** 14 m de alto · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a watchtower for the perimeter wall: a tall concrete and steel tower with a small enclosed cabin on top with dark windows, a big searchlight and a security camera mounted on the cabin, a ladder cage on one side, warning lights.
```

## MU03 — MU03_PuertaMuro (fase 3)
- **Adjuntar:** `K06_CalleNova.png`, `K16_Deshuesadero.png` · **Medida:** 12 m ancho × 9 m alto · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a huge closed gate of the perimeter wall: two massive reinforced steel doors between two thick concrete pillars, hazard stripes, warning lights, and warning signs with simple symbols only (an exclamation triangle, a hand), no text.
```

## CB01 — CB01_BarreraConcreto (fase 4)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png` · **Medida:** 2.5 × 0.6 × 1 m · **Tripo:** Smart Mesh P2 · 6,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a low worn concrete barrier block about 1 meter high, chipped edges, stains and a few faded painted stripes.
```

## CB02 — CB02_Costales (fase 4)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png` · **Medida:** 2 × 0.7 × 1 m · **Tripo:** Smart Mesh P2 · 8,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a low wall of stacked sandbags, chunky and slightly worn, about 1 meter high.
```

## CB03 — CB03_CajasMetal (fase 4)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png` · **Medida:** 1.6 × 0.8 × 1 m · **Tripo:** Smart Mesh P2 · 8,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a stack of industrial metal crates about 1 meter high, dented, with stripes and rivets.
```

## CB04 — CB04_BarricadaPolicia (fase 4)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `V05_PatrullaSedan.png` · **Medida:** 2.5 × 0.6 × 1.1 m · **Tripo:** Smart Mesh P2 · 6,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents; the Nova corporation is clean white with cold blue light); do not copy their scenes. Original design: nothing copied from real places, brands or cars. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a police barricade: a sturdy metal frame with a solid panel painted with diagonal navy blue and white stripes, a small red and blue light on top, no text.
```

## T10 — T10_Charcos (fase 5)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`

```
Use the attached image ONLY for the mood. A mask texture seen straight from above (orthographic, flat): several puddles of irregular natural shapes and different sizes scattered across the square, with soft feathered edges, the water drawn in white and light grey with subtle variation; everything that is not water is PURE BLACK (#000000). The pattern should tile seamlessly at the edges. No reflections of objects, no text.
```


# Parte 33 — Pedido 21-nov: granada W11 + holograma H11, entrada del mercado K19, EM01, EM02 (_a/_b)

Lote: `lote_21nov.json` · script: `generar_21nov.ps1` (fase 1: W11, K19; fase 2: H11 desde W11; fase 3: EM01, EM02 con K19).

## W11 — W11_Granada (fase 1)
- **Adjuntar:** `W01_Pistola.png`, `W03_Fusil.png` · **Medida:** 10 cm de alto · **Tripo:** HD · 6,000 caras · 2K

```
Use the attached weapons as the reference for the art style, materials, wear and level of detail so it matches our weapon set; use them ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents and magenta neon); do not copy their scenes. Original design, nothing copied. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a compact cyberpunk hand grenade: a rounded segmented body of dark gunmetal grey metal with a glowing turquoise band around the middle, the safety lever (spoon) along the side and the pull ring at the top clearly visible and chunky, small scratches and worn edges.
```

## K19 — K19_EntradaMercado (fase 1)
- **Adjuntar:** `K02_Tianguis.png`, `K01_Barrio_HUDnuevo.png`

```
The FIRST attached image is our El Mercado district and the SECOND our game's look: match their illustrated art style EXACTLY (thin outlines, painted rendering, blue night with soft fog, orange sodium lights, turquoise and magenta neon, wet reflective ground). At street level, as seen in the game, 16:9, NO HUD, NO text, NO letters, NO logos. Original design. Scene: the entrance to a neighborhood market at night: a big arch-shaped gateway made of a metal frame and corrugated metal sheets, with a neon sign on top made of abstract shapes (no letters), market stalls with colorful tarp roofs lining both sides and continuing inside, strings of hanging bulbs, people walking in, cars and motorcycles parked outside, utility poles with thick cables.
```

## H11 — H11_Granada (fase 2)
- **Adjuntar:** `W11_Granada_a.png`

```
Turn the grenade in the attached image into a holographic projection for a video game user interface. Keep EXACTLY the same shape, parts and proportions, but shown perfectly from the side (pure side profile, orthographic), centered and filling about 60% of the height. Translucent cyan-turquoise glowing body, bright glowing outlines on every part, faint inner wireframe lines, thin horizontal scan lines, a slight digital glitch on one or two slices, soft cyan glow. PURE BLACK (#000000) background everywhere, no text, no hands.
```

## EM01 — EM01_PortonMercado (fase 3)
- **Adjuntar:** `K02_Tianguis.png`, `K19_EntradaMercado_a.png` · **Medida:** 8 m ancho × 6 m alto · **Tripo:** Smart Mesh P2 · 12,000 caras · 2K

```
Use the attached images (our market district and the market entrance) ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents and magenta neon); do not copy their scenes. Original design, nothing copied. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: the market entrance structure alone: a large arch-shaped gateway about 8 meters wide and 6 meters high, made of a metal frame covered with corrugated metal sheets, an open passage in the middle to walk through, a neon sign on top made of abstract shapes (no letters), strings of bulbs and a few lamps. View: 3/4 view from the front.
```

## EM02 — EM02_PuestoEntrada (fase 3)
- **Adjuntar:** `K02_Tianguis.png`, `K19_EntradaMercado_a.png` · **Medida:** 2.5 × 1.5 × 2.4 m · **Tripo:** Smart Mesh P2 · 10,000 caras · 2K

```
Use the attached images ONLY for the art style, colors and level of detail of our game (illustrated, thin outlines, soft hand-painted gradients, worn Mexican cyberpunk megacity with turquoise and orange accents and magenta neon); do not copy their scenes. Original design, nothing copied. Stylized game asset with chunky simplified shapes, large clean bevels, no photorealistic noise. A single object alone, complete and fully visible with empty space around it, centered, isolated on a plain flat pure white background: no ground, no people, no other objects, soft even studio lighting, no cast shadow, no text, no letters, no numbers, no logos. Nothing thin or dangling: cables, rails and bars are thick and chunky. View: 3/4 view from the front left, slightly from above. Subject: a market stall: a metal frame with a colorful tarp roof, a wooden counter in front, crates of fruit and boxes of goods on and under the counter, a hanging bulb.
```


# Parte 34 — Pedido 21-nov: referencias de todo el mapa (K20–K34, A20–A24, M13)

Lote: `lote_21nov_b.json` · script: `generar_21nov_b.ps1` · salida `imageassets/referencias/look/mapa-final/`.

## K20 — K20_Suburbios_CallePrincipal (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png`, `K08_CalleSuburbios_Cuadra.png`, `L15_LetreroCanepa.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: the main street of the Suburbios district (warm orange light, attached colorful two-story houses), the street where the player's house is, and right across the street the neighborhood car workshop El Canepa with its open garage, a car lifted inside, tools and a hand-painted sign shown as colorful shapes.
```

## K21 — K21_Suburbios_Callejon (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png`, `SkullHombre_frente.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: the narrow alley of the SKULLS gang in Suburbios: high walls with magenta skull-themed graffiti as abstract shapes, trash bags and metal barrels, a fire glowing in one barrel, metal fire escape stairs on the walls, puddles, a few gang members as distant silhouettes.
```

## K22 — K22_Suburbios_ClinicaTaller (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png`, `K08_CalleSuburbios_Cuadra.png`, `P12_FachadaConsultorio.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: a block of Suburbios with the underground clinic of the Doc (a reinforced metal door, a covered window, a turquoise light and a medical cross shape over the door) and, a few doors down, the inventor's workshop of Tino (a wide open garage with workbenches, drones and parts inside, warm light).
```

## K23 — K23_Mercado_Tianguis (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K02_Tianguis.png`, `K10_CalleMercado.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: the closed street of the tianguis in El Mercado: rows of stalls with colorful tarp roofs on both sides, piles of goods, people shopping; NO hanging bulb strings over the street, the light comes from the stalls and the street lamps.
```

## K24 — K24_Mercado_Plaza (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K09_Plaza_Barrio.png`, `K02_Tianguis.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: the plaza of El Mercado: a small kiosk in the center, benches, round trees, food stalls around it, and on the corner the little corner shop (tiendita) with a metal awning and a glowing soda fridge outside.
```

## K25 — K25_Mercado_Armas (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K10_CalleMercado.png`, `L12_LetreroElMercado.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: the gun market street of El Mercado: a shop front behind a heavy metal grille with display cases of weapons glowing amber inside, crates of ammunition stacked outside, a rough rusty sign shown as a plate with a pistol silhouette, a couple of armed guards as silhouettes.
```

## K26 — K26_NeonAlto_Casino (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K03_NeonAlto.png`, `L10_LetreroVivaCoin.png`, `L11_LetreroHotel.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: the intersection of Neon Alto with the VivaCoin casino on the corner (a big gold and magenta sign as a glowing shape with no readable letters) and a hotel next to it with a tall vertical neon sign, buildings of 4 to 6 floors, lots of magenta and turquoise neon.
```

## K27 — K27_NeonAlto_Calle (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K03_NeonAlto.png`, `V09_Taxi.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: a street of Neon Alto: tall buildings of 4 to 6 floors with big glowing screens showing abstract images, projecting neon signs, pink and white taxis driving and parked, people on the sidewalks.
```

## K28 — K28_Nova_Avenida (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K06_CalleNova.png`, `K07_PuenteNova.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: the wide clean avenue of the Nova district right in front of the huge white Nova tower, office towers with white panels and cold blue light strips, bollards, cameras, almost no neon, white and blue light.
```

## K29 — K29_Nova_Refineria (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K06_CalleNova.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: the Nova refinery inside the Nova district: large clean white industrial structures, big storage tanks, thick pipes running along racks, catwalks with solid railings, steam rising and glowing in the cold blue light, red warning lights on the tops.
```

## K30 — K30_Cruce_Suburbios_Mercado (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K05_EsquinaBarrio.png`, `K02_Tianguis.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: the corner where Suburbios becomes El Mercado: on one side attached colorful two-story houses with warm orange light, on the other side shops with colorful tarps and stalls starting, the styles mixing gradually along the block.
```

## K31 — K31_Cruce_NeonAlto_Nova (fase 1)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K03_NeonAlto.png`, `K06_CalleNova.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: the corner where Neon Alto becomes Nova: on one side buildings covered in magenta and turquoise neon and screens, on the other side clean white office towers with cold blue light, the transition gradual across the intersection.
```

## K32 — K32_Muro_Calle (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `MU01_MuroPerimetral_a.png`, `K05_EsquinaBarrio.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: The wall is 8 meters tall, made of concrete panels and metal beams, with thick pipes, amber warning lights, yellow and black hazard stripes and coils of razor wire on top. Behind the wall there is NOTHING: only the night sky, at most a few very distant tiny lights. a neighborhood street that ends against the wall, with houses built right up against the wall (no empty land between the houses and the wall).
```

## K33 — K33_Muro_Puerta (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `MU03_PuertaMuro_a.png`, `MU02_TorreControl_a.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: The wall is 8 meters tall, made of concrete panels and metal beams, with thick pipes, amber warning lights, yellow and black hazard stripes and coils of razor wire on top. Behind the wall there is NOTHING: only the night sky, at most a few very distant tiny lights. one of the giant gates of the wall, closed, with a watchtower next to it with a sweeping searchlight, and the street leading straight to it.
```

## K34 — K34_Muro_Esquina (fase 2)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `MU02_TorreControl_a.png`, `MU01_MuroPerimetral_a.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. Street-level view at the height of the player (camera about 2 meters high), as it would look in the game. Scene: The wall is 8 meters tall, made of concrete panels and metal beams, with thick pipes, amber warning lights, yellow and black hazard stripes and coils of razor wire on top. Behind the wall there is NOTHING: only the night sky, at most a few very distant tiny lights. a corner of the wall where two sections meet at a watchtower, with the city buildings pressed right against the wall on the inside.
```

## A20 — A20_Suburbios_Aerea (fase 3)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K08_CalleSuburbios_Cuadra.png`, `K05_EsquinaBarrio.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. AERIAL ISOMETRIC view at 45 degrees from about 60 meters high, showing clearly how everything is laid out: streets, blocks, alleys and plazas, with no gaps or empty lots, as a layout guide. No people. Scene: the whole Suburbios district: attached colorful two-story houses in short blocks, narrow alleys, one small plaza, the player's house street with the El Canepa workshop, the clinic and Tino's workshop, warm orange light.
```

## A21 — A21_Mercado_Aerea (fase 3)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K02_Tianguis.png`, `K09_Plaza_Barrio.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. AERIAL ISOMETRIC view at 45 degrees from about 60 meters high, showing clearly how everything is laid out: streets, blocks, alleys and plazas, with no gaps or empty lots, as a layout guide. No people. Scene: the whole El Mercado district: the closed tianguis street with rows of colorful tarp stalls, the market plaza with its kiosk, the gun market street, the corner shop, shops with tarps on every block.
```

## A22 — A22_NeonAlto_Aerea (fase 3)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K03_NeonAlto.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. AERIAL ISOMETRIC view at 45 degrees from about 60 meters high, showing clearly how everything is laid out: streets, blocks, alleys and plazas, with no gaps or empty lots, as a layout guide. No people. Scene: the whole Neon Alto district: buildings of 4 to 6 floors in dense blocks, rooftops with neon signs and screens, the casino on a corner intersection, the hotel, magenta and turquoise light everywhere.
```

## A23 — A23_Nova_Aerea (fase 3)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `K06_CalleNova.png`, `K07_PuenteNova.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. AERIAL ISOMETRIC view at 45 degrees from about 60 meters high, showing clearly how everything is laid out: streets, blocks, alleys and plazas, with no gaps or empty lots, as a layout guide. No people. Scene: the whole Nova district: clean white office towers on wide streets, the huge Nova tower in a plaza, the refinery with tanks and pipes in one corner of the district, cold white and blue light.
```

## A24 — A24_Muro_Aerea (fase 3)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `MU01_MuroPerimetral_a.png`, `MU02_TorreControl_a.png`

```
The FIRST attached image is the target look of our game: match its illustrated art style EXACTLY, as if it were another screenshot of the same game: thin outlines, painted rendering, blue night with light fog, orange sodium street lights, magenta and turquoise neon, wet ground with reflections, thick cables between utility poles, a worn Mexican cyberpunk megacity. Any other attached images are our existing places: keep them consistent. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. 16:9, NO HUD, NO interface, no readable text, no letters, no logos: signs and screens show only abstract shapes. Original design, nothing copied from real places. AERIAL ISOMETRIC view at 45 degrees from about 60 meters high, showing clearly how everything is laid out: streets, blocks, alleys and plazas, with no gaps or empty lots, as a layout guide. No people. Scene: The wall is 8 meters tall, made of concrete panels and metal beams, with thick pipes, amber warning lights, yellow and black hazard stripes and coils of razor wire on top. Behind the wall there is NOTHING: only the night sky, at most a few very distant tiny lights. a section of the edge of the city: the dense blocks of the city pressed right against the inside of the wall, a watchtower on the wall, and on the outside of the wall absolutely nothing, only darkness.
```

## M13 — M13_MapaFinal (fase 4)
- **Adjuntar:** `K01_Barrio_HUDnuevo.png`, `M11_MapaCiudad.png`, `M10_MapaCiudad.png`

```
Use the FIRST attached image for the art style and color mood of our game, and the other images (our previous city maps) for the illustrated top-down map style. Our city has only 4 districts inside a tall wall that surrounds it completely; there are no empty lots or gaps between districts: blocks touch each other and the style changes gradually. Draw the final full-screen city map seen from directly above, like the pause-menu map of an open-world game, at night colors. Layout: NOVA top-left (cold blue, white towers, the big tower and a small refinery), NEÓN ALTO top-right (magenta, dense tall buildings and the casino), SUBURBIOS bottom-left (warm orange, small attached houses), EL MERCADO bottom-right (yellow, the tianguis street and a plaza). Short blocks touching each other everywhere with no gaps, streets, alleys and a few plazas. A thick wall surrounds the whole city with 6 gates spread along it and watchtowers at the four corners. OUTSIDE the wall there is nothing: plain dark. The ONLY text on the map, in thin capital letters for the districts and small thin labels with small round icons for the places, spelled exactly: "NOVA", "NEÓN ALTO", "SUBURBIOS", "EL MERCADO", "Casa", "Doc", "Tino", "El Canepa", "Tianguis", "Mercado de armas", "Casino VivaCoin", "Torre Nova". Casa, Doc, Tino and El Canepa are close to each other inside Suburbios (El Canepa across the street from Casa). No legend, no compass text, no other words.
```
