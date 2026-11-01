# Plan: ciudad como las referencias, personajes e interfaz (31-oct-2026)
Modo demo: solo lo que se ve en la demo (ver plan maestro, "Solo lo que se ve en la demo"). No trabajes en nada descartado.
Hay dos partes. Si hay dos sesiones: la PARTE A la hace la computadora de Tony y la PARTE B la de Alan, en paralelo. Si hay una sola sesión: Paso 0, luego A1, B1, B2, A2.

## Reglas
- Sonnet para todo; Opus solo para comparar capturas con las referencias si Sonnet no ve la diferencia, o si algo falla 2 veces.
- Una rama por tarea desde tony/fase-g (Parte A IDs T-500 a T-519; Parte B T-520 a T-539). Antes de cada merge a main: git fetch, mezcla origin/main, compila; conflicto en archivo de la otra parte = quédate con origin/main. Merge a main con push al terminar cada paso (yo, Tony, lo autorizo). Nunca force push.
- Pruebas solo de lo que tocas + RecorridoDemoPruebas al final de cada paso. 60 FPS de noche es obligatorio.
- No corras CreadorTodo. Si regeneras Barrio.prefab, vuelve a correr ColocarT250.Colocar y la colocación de T-419.
- Después de cada paso: regenera Builds/Demo y Builds/Juego y 3 líneas de qué probar en docs/guia-para-jugar.md. Si se acaba el límite, espera y continúa.

## Paso 0 (computadora de Tony, antes que todo) — importar
Origen: imageassets/modelos3d/ y imageassets/ui/. Descomprime en Temp/ (fuera de git):
- N01–N08 con el flujo de T-250/T-419 (Assets/Arte/<Zona>/<Código>/, prefab con hijo Visual, URP, LOD si pasa de 15 mil caras). RF01 es de la Refinería, que se descartó: no lo importes.
- man.zip, woman.zip, woman2.zip (personajes de Tripo YA con esqueleto y animaciones): a Assets/Arte/Personajes/<man|woman|woman2>/ con su carpeta .fbm de texturas.
- imageassets/ui/*.png a Assets/Arte/UI/Fuentes~/ (originales).
Commit y push a main. Así la otra computadora los recibe con git pull.

## PARTE A — Ciudad como las referencias (Mundo/, Arte de ciudad, escenas)
Referencias: docs/referencias-ciudad/*.png y fuentes.md (qué assets sale en cada una).
Método por zona: captura desde el mismo punto y ángulo que la referencia → ajusta → nueva captura → compárala con la referencia (composición, densidad, luz, color, niebla, reflejos) → máximo 3 vueltas. Guarda docs/capturas/ref-<zona>-antes.png, -despues.png y una imagen lado a lado con la referencia.
A1. **Suburbios primero** (calle-suburbios-noche, cruce-dia-lluvia, azotea-vista-ciudad): calles que se sientan angostas y encerradas (fachadas más juntas a la banqueta y edificios de 2–3 pisos con el kit de pisos, tinacos en todas las azoteas), muchos cables en catenaria entre postes y fachadas (LineRenderer, 6–10 por cuadra), N01 lámpara de jaula con luz cálida junto a cada puerta, letreros chicos de neón magenta y turquesa, charcos con reflejo (reflection probes o SSR), niebla azul que esconde el fondo de la calle, luz naranja de postes, orillas con tambos, huacales, N06 basura. Horizonte por código: 30–60 torres de fondo (siluetas con ventanas encendidas, pantallas verdes y magenta usando E03/CY02, antenas con luz roja) — el kit de torres T20–T23 aún no llega; cuando llegue, se cambian las cercanas.
A2. Callejón (callejon-noche): magenta, N04 tuberías, N05 ventilas con vapor, contenedor, llantas, tambos, escalera, reja al fondo, niebla rosa, muy oscuro con pocos puntos de luz.
A3. El Tianguis (tianguis-noche): N03a/N03b puestos con lona en hileras, series de focos cálidos en zigzag sobre la calle, vapor en los puestos de comida, expendedora, proyectores holo, hologramas de comida.
A4. Entrada de El Mercado (entrada-mercado-noche): E02 portón con luz naranja adentro, papel picado de colores colgando (planos con textura generada), C18 barreras con franja turquesa, cámaras, servidor, puesto de implantes, plataforma de dron.
A5. Neón Alto por fuera (neon-alto-noche): N02 autopista elevada cruzando, N07 vallas, N08 y CY02 anuncios verticales, E03 pantallas gigantes, E01 entrada del casino, torres, verde ácido + magenta, lluvia y neblina verde.
Al final: tabla en docs/estado-tony.md con FPS por zona y las capturas lado a lado.

## PARTE B — Personajes e interfaz (Personaje/, IA/ prefabs de NPC, UI/)
B1. **Personajes** (usa el sistema de Personaje/Visual/ y docs/como-meter-personajes.md):
- Importa man, woman y woman2 como Humanoid (avatar desde su esqueleto de Tripo); si el avatar falla, Generic con sus propias animaciones. Revisa qué animaciones trae cada FBX y usa las que sirvan (quieto, caminar, correr...).
- Asignación: creación de personaje = elegir entre woman (protagonista) y man; woman2 = primer aliado. NPC con variantes de material del mismo cuerpo (no modelos nuevos): SKULLS = negro con magenta, jefe SKULLS = más grande y con brillo magenta, vecino y vendedor = colores apagados, peatones = 4 variantes de color, El Doc / El Inventor / El Hacker = variante con un color propio y su nombre.
- Animaciones que falten (apuntar, disparar pistola y arma larga, recargar, golpe, recibir golpe, caer, cubrirse, rodar): usa capas y IK de la parte de arriba para apuntar con lo que haya; escribe en docs/animaciones-faltan.md la lista exacta para que Tony las baje de Mixamo (subiendo el FBX) y cómo dejarlas para que entren solas.
- Que el modelo encaje en el CharacterController (1.8 m), pies en el piso, sin deslizarse; capturas de cada uno.
B2. **Interfaz** (piezas en Assets/Arte/UI/Fuentes~/ y estilo de imageassets/referencias/mockup-hud.png y mockup-menu.png):
- Quita el fondo a las piezas (rembg o por color), recorta las hojas (armas, estados, mapa, insignias, retículas) en PNG sueltos, márcalos como Sprite; UI01/UI02 como 9-slice.
- Fuentes Teko (títulos y números) y Rajdhani (texto) de Google Fonts, como TextMesh Pro con acentos y ñ.
- Rehaz el HUD como el mockup con estos cambios: la vida en turquesa que pasa a rojo cuando baja (el naranja solo para marcos y selección), todo en español (CALOR, RESPETO), símbolo propio de Biits (UI05), sin etiqueta "HUD", 5 casillas de arma, aliados compactos junto al minimapa, paneles más transparentes.
- Rehaz menú principal (UI13 logo + UI14 fondo, fondo más oscuro detrás del menú), pausa, opciones, tienda de El Mercado, CIC, mapa, créditos y aviso de zona conquistada con el mismo estilo. Lo descartado (teléfono, guarida, negocios) no se toca.
- Capturas de cada pantalla a 1080p y 1440p, comparadas con los mockups.
