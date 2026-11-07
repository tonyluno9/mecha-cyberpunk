import json, sys
OUT = sys.argv[1]  # raíz imageassets

BASE = ("Stylized low-poly 3D game asset: chunky simplified shapes, large clean bevels, flat colors with soft hand-painted gradients, "
"no photorealistic textures, no fine noise, readable from a distance. Single object, centered, fully visible with empty space around it, "
"isolated on a plain flat light grey background, soft even studio lighting, no cast shadow on the ground, no text, no letters, no numbers, "
"no logos, no people. Nothing thin or dangling: antennas, railings, ladders and cables are thick and chunky.")
NEON = ("Rich cyberpunk business district: clean dark charcoal and gunmetal grey structure with polished dark blue-black glass, "
"small glowing acid green and magenta accents, thin teal light strips. Very light wear, clean and expensive-looking.")
NOVA = ("Cold villain megacorporation: pristine white and light grey panels, matte black glass, thin cold white light strips, "
"sharp angular minimalist forms, very clean, imposing and sterile.")
REF = ("Corporate industrial refinery: white, light grey and galvanized grey metal, yellow-and-black safety stripes on edges and feet, "
"red warning lights, a little grime and rust streaks near the bottom, heavy and solid.")
MID = ("Middle-class Mexican cyberpunk neighborhood: painted concrete in faded teal and warm cream with some newer dark grey metal panels, "
"a few small glowing teal and magenta accents, light wear.")
MOD = ("Modular building section for a game kit, shaped as a box with a perfectly square footprint (equal width and depth). The top face and "
"the bottom face are perfectly flat and horizontal so identical modules stack exactly on top of each other. Thick square corner pillars at "
"all four corners. Windows are flat smooth opaque dark blue-black glass panels: no reflections, no interiors, no curtains, no light behind them. "
"Nothing protrudes more than a hand's width from the facade unless stated.")
VIEW_BOX = "View: 3/4 view at 45 degrees showing the front face and the right face equally, camera at the height of the module, slight perspective."
REFT21 = ("Use the attached image as the exact reference for the footprint width and depth, corner pillars, window grid position and spacing, "
"materials, colors and art style, so this module stacks perfectly with it. ")
REFT24 = REFT21

items = []
def add(**k): items.append(k)

# ---- Torres neón alto
add(id="T21", nombre="PisoOficinas", carpeta="torres", fase=1, endpoint="text-to-image", refs=[], aspecto="16:9",
    medida="12 × 12 m de planta, 4 m de alto", modo="Smart Mesh P2", caras=10000, tex="2K",
    prompt=BASE+" "+NEON+" "+MOD+" Subject: one single floor module of a rich corporate office tower, a wide low square block three times wider than tall: on each of the four sides the same glass curtain wall grid of exactly 4 equal tall window panels separated by thick charcoal vertical mullions, a thick charcoal horizontal band along the top edge and the bottom edge, and a thin glowing teal light strip running along the top and bottom edges of every face. All four sides identical. "+VIEW_BOX,
    cc="Prefab `T21_PisoOficinas`: `Visual` escalado a 12 × 12 m de planta y 4 m de alto (si la proporción no coincide, escala X/Z a 12 m y Y a 4 m y avísame cuánto se deformó), pivote en el centro de la cara inferior. Vidrios con `VentanaLuz` (40–60% encendidas, frías y cálidas), franjas turquesa con `MascaraEmision`. BoxCollider.")
add(id="T22", nombre="PisoPantalla", carpeta="torres", fase=2, endpoint="edit", refs=["torres/T21.png"], aspecto="16:9",
    medida="12 × 12 m de planta, 4 m de alto", modo="Smart Mesh P2", caras=10000, tex="2K",
    prompt=REFT21+BASE+" "+NEON+" "+MOD+" Subject: the same office tower floor module as the reference, identical on three sides, but on the front face a large flat matte black advertising screen (screen OFF, plain black, no image) covering most of the face, flush with the facade, with a thick gunmetal frame and a thin glowing magenta LED edge. "+VIEW_BOX,
    cc="Prefab `T22_PisoPantalla`: igual que `T21` (12 × 12 × 4 m, mismo pivote). Quad `Pantalla` sobre la pantalla con `PantallaAnuncio`. BoxCollider.")
add(id="T20", nombre="BaseTorre", carpeta="torres", fase=2, endpoint="edit", refs=["torres/T21.png"], aspecto="16:9",
    medida="12 × 12 m de planta, 6 m de alto (marquesina sale hasta 3 m)", modo="Smart Mesh P2", caras=12000, tex="2K",
    prompt=REFT21+BASE+" "+NEON+" "+MOD+" Subject: the ground floor base module of the same rich office tower, one and a half times taller than the reference: same corner pillars and same window grid on the sides and back; on the front face a tall glass entrance with double glass doors in the center between two thick dark columns, a thick cantilevered canopy marquee over the entrance with an acid green glowing edge (the canopy may stick out about a quarter of the width), and a blank flat sign panel above the canopy (no text). A dark stone plinth along the bottom. Bottom face flat. "+VIEW_BOX,
    cc="Prefab `T20_BaseTorre`: `Visual` 12 × 12 m de planta y 6 m de alto (sin contar la marquesina), pivote en el centro de la cara inferior. `LetreroNeon` en el panel (nombre ficticio), luz de la marquesina, vidrios con `VentanaLuz`. BoxCollider dejando libre la entrada si se puede entrar; si no, collider completo.")
add(id="T23", nombre="RemateTorre", carpeta="torres", fase=2, endpoint="edit", refs=["torres/T21.png"], aspecto="1:1",
    medida="12 × 12 m de planta, 5 m de cuerpo + antena ~6 m arriba", modo="Smart Mesh P2", caras=12000, tex="2K",
    prompt=REFT21+BASE+" "+NEON+" Subject: the roof crown module of the same rich office tower: same square footprint and corner pillars as the reference; a short band of the same window grid at the bottom, then a solid charcoal parapet; on the flat roof a circular landing pad with a simple painted acid green ring (no letters), three boxy air-conditioning units with big fans, a boxy water tank, and one thick tall antenna mast with a glowing red light on top (the mast may rise well above the roof). The bottom face is flat to sit on top of the floor modules. View: 3/4 view at 45 degrees from slightly above so the roof is visible, the whole antenna inside the image.",
    cc="Prefab `T23_RemateTorre`: `Visual` 12 × 12 m de planta, cuerpo de 5 m (la antena queda por encima), pivote en el centro de la cara inferior. Luz roja de la antena parpadeando. BoxCollider del cuerpo.")
add(id="T25", nombre="EdificioMedio", carpeta="torres", fase=1, endpoint="text-to-image", refs=[], aspecto="1:1",
    medida="12 × 12 m de planta, ~10 m de alto (3 pisos); balcones salen hasta 1 m", modo="Smart Mesh P2", caras=12000, tex="2K",
    prompt=BASE+" "+MID+" Subject: a medium three-story apartment building with a square footprint, the visual bridge between a poor neighborhood and a rich district: painted concrete facades, rows of windows mixing barred old windows and newer dark blue-black glass windows, small balconies with thick solid railings and a couple of plants, a ground floor with two rolled-down metal shutters and an entrance door, a few boxy air conditioners, thin glowing teal light strips on the balcony edges, a flat roof with a low parapet and a water tank. Bottom face flat. View: 3/4 view at 45 degrees showing the front and right facades, camera at mid height.",
    cc="Prefab `T25_EdificioMedio`: `Visual` 12 × 12 m de planta, ~10 m de alto, pivote en el centro de la cara inferior. Ventanas con `VentanaLuz`, BoxCollider. Úsalo entre el barrio y el distrito de torres.")
# ---- Nova
add(id="T24b", nombre="PisoNova", carpeta="torres", fase=1, endpoint="text-to-image", refs=[], aspecto="16:9",
    medida="16 × 16 m de planta, 4 m de alto", modo="Smart Mesh P2", caras=10000, tex="2K",
    prompt=BASE+" "+NOVA+" "+MOD+" Subject: one single floor module of an imposing megacorporation tower, a wide low square block four times wider than tall: on each of the four sides the same grid of exactly 5 equal window panels of matte black glass separated by thick white vertical panels, thick white horizontal bands along the top and bottom edges, and a thin glowing cold white light strip along the top edge of every face. All four sides identical. "+VIEW_BOX,
    cc="Prefab `T24b_PisoNova`: `Visual` 16 × 16 m de planta y 4 m de alto, pivote en el centro de la cara inferior. Vidrios con `VentanaLuz` (luz blanca fría, pocas encendidas), franja blanca emisiva. BoxCollider.")
add(id="T24a", nombre="BaseNova", carpeta="torres", fase=2, endpoint="edit", refs=["torres/T24b.png"], aspecto="16:9",
    medida="16 × 16 m de planta, 8 m de alto", modo="Smart Mesh P2", caras=12000, tex="2K",
    prompt=REFT24+BASE+" "+NOVA+" "+MOD+" Subject: the ground floor base module of the same megacorporation tower, twice as tall as the reference: same corner pillars and same window grid on the sides and back; on the front face a monumental tall pointed arch entrance with dark glass doors, framed by thick white angular pilasters, cold white light lines tracing the arch, two small boxy security cameras with red lights, a white stone plinth with a few wide steps. No text, no logo. Bottom face flat. "+VIEW_BOX,
    cc="Prefab `T24a_BaseNova`: `Visual` 16 × 16 m de planta y 8 m de alto, pivote en el centro de la cara inferior. Luces blancas frías en el arco (Spot Lights hacia abajo), cámaras con luz roja. BoxCollider.")
add(id="T24c", nombre="RemateNova", carpeta="torres", fase=2, endpoint="edit", refs=["torres/T24b.png"], aspecto="1:1",
    medida="16 × 16 m de planta, ~10 m de alto", modo="Smart Mesh P2", caras=12000, tex="2K",
    prompt=REFT24+BASE+" "+NOVA+" Subject: the crown of the same megacorporation tower: the bottom has exactly the same square footprint and corner pillars as the reference so it sits on top of it, then sharp white angular faceted fins rise and taper inward like a cold crown, with thin cold white light lines along the edges and one glowing red aviation light at the very top. Bottom face flat. View: 3/4 view at 45 degrees from slightly below, the whole crown inside the image.",
    cc="Prefab `T24c_RemateNova`: `Visual` 16 × 16 m de planta, ~10 m de alto, pivote en el centro de la cara inferior. Luz roja parpadeando, líneas blancas emisivas. BoxCollider.")
# ---- Refinería
add(id="RF01", nombre="Tanque", carpeta="refineria", fase=1, endpoint="text-to-image", refs=[], aspecto="1:1",
    medida="10 m de diámetro, ~8 m de alto", modo="HD", caras=15000, tex="2K",
    prompt=BASE+" "+REF+" Subject: a large cylindrical industrial storage tank, a bit wider than tall, white with light grey panel seams and bolts, a thick external spiral staircase wrapping around the outside with a thick solid railing, a slightly domed roof with a thick solid railing ring around its edge, a wide yellow-and-black striped band at the base, a red warning light on top, a few short thick pipe stubs at the base cut flat, grime near the bottom. View: 3/4 view from slightly above, the whole tank visible.",
    cc="Prefab `RF01_Tanque`: `Visual` escalado a 10 m de diámetro, pivote en el centro de la base. MeshCollider convexo o CapsuleCollider; luz roja parpadeando.")
add(id="RF02", nombre="Tuberias", carpeta="refineria", fase=1, endpoint="text-to-image", refs=[], aspecto="16:9",
    medida="8 m de largo, ~3 m de alto", modo="HD", caras=8000, tex="2K",
    prompt=BASE+" "+REF+" Subject: a straight section of an industrial pipe rack: three thick parallel horizontal pipes (two white, one grey) with chunky flanges and one red valve wheel, resting on two sturdy steel H-frame supports with yellow-and-black striped feet bolted to small concrete pads. The pipes are cut perfectly flat at both ends so identical sections line up end to end. View: 3/4 view from the front left, slightly from above.",
    cc="Prefab `RF02_Tuberias`: `Visual` 8 m de largo, pivote en el centro de la base, los extremos alineados al eje X para encadenar copias cada 8 m. BoxColliders en soportes y tubos.")
add(id="RF03", nombre="Pasarela", carpeta="refineria", fase=1, endpoint="text-to-image", refs=[], aspecto="16:9",
    medida="6 m de largo, 1.5 m de ancho, piso a 4 m", modo="HD", caras=8000, tex="2K",
    prompt=BASE+" "+REF+" Subject: an elevated industrial metal catwalk section: a solid steel deck (no grating holes) with thick toe boards, thick solid safety railings on both sides with a top rail, a mid rail and thick posts painted safety yellow, supported by two sturdy grey steel legs with thick cross bracing standing on small concrete pads. Both ends of the deck are cut flat so identical sections line up end to end. View: 3/4 view from the front left, slightly from below.",
    cc="Prefab `RF03_Pasarela`: `Visual` 6 m de largo con el piso a 4 m, pivote en el centro entre las patas a nivel de suelo, extremos alineados al eje X. Collider de piso caminable + barandales.")
add(id="RF04", nombre="Chimenea", carpeta="refineria", fase=1, endpoint="text-to-image", refs=[], aspecto="9:16",
    medida="20 m de alto, ~2.5 m de diámetro", modo="HD", caras=15000, tex="2K",
    prompt=BASE+" "+REF+" Subject: a tall industrial chimney stack, very tall and slender cylinder, white and light grey with two thick red and white bands near the top, three thick maintenance ring platforms with solid railings at different heights, one thick enclosed ladder tube running up the side, red warning lights at the top, a wide reinforced concrete base with bolts and yellow-and-black stripes. View: 3/4 view from slightly below, the whole chimney from base to top fully inside the image with empty space above and below.",
    cc="Prefab `RF04_Chimenea`: `Visual` 20 m de alto, pivote en el centro de la base, CapsuleCollider, luces rojas parpadeando y humo con partículas saliendo de la punta.")
add(id="RF05", nombre="CasetaNova", carpeta="refineria", fase=1, endpoint="text-to-image", refs=[], aspecto="4:3",
    medida="caseta 4 × 3 × 3 m + brazo de barrera 5 m", modo="HD", caras=10000, tex="2K",
    prompt=BASE+" "+NOVA+" Subject: a corporate security checkpoint: a small boxy white and light grey guard booth with matte black glass windows and a thin cold white light strip, a boxy terminal with a glowing teal screen showing no text, a red beacon light on the roof, and next to it a thick boom barrier arm striped red and white resting horizontally on its support post. Everything sits on one flat concrete base slab with yellow-and-black curb edges. View: 3/4 view from the front left, at eye level.",
    cc="Prefab `RF05_CasetaNova`: `Visual` con la caseta de 4 m de ancho, pivote en el centro de la losa. Si el brazo de la barrera sale como pieza separable, déjalo como hijo `Barrera` con pivote en el poste para animarlo (subir/bajar); si no, déjalo fijo. Colliders en caseta y brazo.")
add(id="RF06a", nombre="ContenedorBlanco", carpeta="refineria", fase=1, endpoint="text-to-image", refs=[], aspecto="16:9",
    medida="6.1 × 2.4 × 2.6 m", modo="Smart Mesh P2", caras=8000, tex="2K",
    prompt=BASE+" "+REF+" Subject: a standard 20-foot shipping container painted white, corrugated walls, thick doors on the end with chunky vertical locking bars, reinforced corner castings, a few small hazard decals shown as plain yellow and red shapes with no text, dirt and rust streaks near the bottom. View: 3/4 view from the front left (door end), slightly from above.",
    cc="Prefab `RF06a_ContenedorBlanco`: `Visual` 6.1 × 2.4 × 2.6 m, pivote centro de la base, BoxCollider. Apilable de 2 o 3.")
add(id="RF06b", nombre="ContenedorGris", carpeta="refineria", fase=1, endpoint="text-to-image", refs=[], aspecto="16:9",
    medida="6.1 × 2.4 × 2.6 m", modo="Smart Mesh P2", caras=8000, tex="2K",
    prompt=BASE+" "+REF+" Subject: a standard 20-foot shipping container painted medium grey, corrugated walls, thick doors on the end with chunky vertical locking bars, reinforced corner castings, a few small hazard decals shown as plain yellow and red shapes with no text, dirt and rust streaks near the bottom. View: 3/4 view from the front left (door end), slightly from above.",
    cc="Prefab `RF06b_ContenedorGris`: igual que `RF06a`.")
add(id="RF07", nombre="Reflector", carpeta="refineria", fase=1, endpoint="text-to-image", refs=[], aspecto="9:16",
    medida="8 m de alto", modo="HD", caras=8000, tex="2K",
    prompt=BASE+" "+REF+" Subject: an industrial floodlight tower: a thick grey steel pole on a concrete footing with bolts and yellow-and-black stripes, at the top a cluster of four large boxy floodlights glowing cold white all facing the same direction, a small square maintenance platform under them with a thick solid railing, and a boxy junction box with a red light at mid height. View: 3/4 view from slightly below, the whole tower fully inside the image.",
    cc="Prefab `RF07_Reflector`: `Visual` 8 m de alto, pivote centro de la base, Spot Light blanco frío potente con sombras apuntando a donde miran los focos. CapsuleCollider.")

lote = {"version":1,"nota":"Fase 1 primero (T21, T24b y los independientes). Fase 2 (T20, T22, T23, T24a, T24c) usa como referencia T21/T24b ya generados.",
 "resolution":"1k","variantes_por_defecto":1,
 "trabajos":[{"id":it["id"],"archivo":it["id"],"salida":f"imageassets/{it['carpeta']}/{it['id']}.png","fase":it["fase"],
  "endpoint":"google/nano-banana-2.1/"+it["endpoint"],"referencias":[f"imageassets/{r}" for r in it["refs"]],
  "aspect_ratio":it["aspecto"],"resolution":"1k","variantes":1,"prompt":it["prompt"],
  "tripo":{"modo":it["modo"],"caras":it["caras"],"textura":it["tex"]},"medida":it["medida"]} for it in items]}
json.dump(lote, open(f"{OUT}/lote_torres.json","w",encoding="utf-8"), ensure_ascii=False, indent=1)

md=["\n\n---\n\n# Parte 8 — Torres de neón alto, torre de Nova y refinería\n",
"Lote: `imageassets/lote_torres.json` (17 imágenes). Se guardan en `imageassets/torres/<código>.png` e `imageassets/refineria/<código>.png`.\n",
"**Orden:** primero la **fase 1** (T21, T24b, T25 y toda la refinería). Cuando T21 y T24b estén aprobados, la **fase 2** (T20, T22, T23, T24a, T24c), que los usa como referencia para que la planta, los pilares y la rejilla de ventanas coincidan al apilar.\n",
"**Regla del kit:** T20–T23 miden 12 × 12 m de planta; T24a–c miden 16 × 16 m. Cara de arriba y de abajo planas. Rejilla de ventanas igual en todos. Vidrio oscuro liso (la luz la pone Unity con `VentanaLuz`).\n",
"**Códigos:** la refinería usa **RF01–RF07** (no R01–R07) para no chocar con los códigos de personajes R02/R03.\n"]
for it in items:
    md.append(f"\n## {it['id']} — {it['nombre']}\n- **Medida real:** {it['medida']} · **Fase:** {it['fase']} · **Aspecto:** {it['aspecto']} · **Endpoint:** `{it['endpoint']}`"+(f" · **Adjuntar:** `imageassets/{it['refs'][0]}`" if it['refs'] else "")+f"\n\n```\n{it['prompt']}\n```\n")
    md.append(f"\n**Tripo:** Image to 3D · {it['modo']} · {it['caras']:,} caras · textura {it['tex']} · Delight apagado · sin rig → `{it['id']}_{it['nombre']}.fbx`\n")
    md.append(f"\n**Claude Code:**\n> Importa `{it['id']}_{it['nombre']}.fbx` en `Assets/Arte/{'Torres' if it['carpeta']=='torres' else 'Refineria'}/{it['id']}/`. {it['cc']} Material URP Lit (Smoothness 0.3–0.5, Metallic 0.2) con `MascaraEmision`. Captura de noche, commit y push en tu rama.\n")
md.append("\n## Armado de torres y refinería (Claude Code, cuando estén los FBX)\n> Crea `ArmarTorre`: dado un tipo (NeonAlto o Nova) y un número de pisos, apila base + pisos + remate alineando centros y caras (NeonAlto: `T20` + N×(`T21` con ~25% `T22`, nunca dos `T22` seguidos) + `T23`; Nova: `T24a` + N×`T24b` + `T24c`), con altura total a partir de los 4 m por piso. Si al apilar la rejilla no coincide entre módulos, ajusta escala X/Z de cada módulo para que los pilares de las esquinas queden alineados y avísame. Coloca 4–6 torres NeonAlto de 6–14 pisos en el distrito rico (con `E01`, `E03`, `CY02`, `V01b`, `C19`, `CY10`), 1 torre Nova de ~20 pisos dominando el fondo, y `T25` como transición entre el barrio y las torres. Arma la refinería de Nova: 3–4 `RF01` en fila, `RF02` encadenadas entre tanques, `RF03` uniendo los tanques en alto, 2 `RF04` con humo, `RF05` en el acceso, `RF06a/b` apilados, `RF07` en el perímetro con luz blanca fría y niebla densa. Usa LOD y GPU Instancing; las torres lejanas pueden ser solo silueta. Mide FPS y toma capturas de noche desde la calle y desde una azotea. Commit y push en tu rama.\n")
with open(f"{OUT}/PROMPTS_ASSETS.md","a",encoding="utf-8") as f: f.write("".join(md))
print(len(items))
