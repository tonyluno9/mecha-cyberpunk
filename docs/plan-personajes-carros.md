# Plan: personajes nuevos, carros reales, fondo y cierre del pulido (1-nov-2026)
Modo demo: solo lo que se ve en la demo. Reglas de siempre: Sonnet, una rama por tarea desde tony/fase-g (IDs T-510 a T-529), pruebas de lo que tocas + RecorridoDemoPruebas al final de cada paso, merge a main con push al terminar cada paso (yo, Tony, lo autorizo), regenera Builds/Demo y Builds/Juego, 3 líneas en docs/guia-para-jugar.md, 60 FPS de noche. No corras CreadorTodo. Nunca force push. Si se acaba el límite, espera y continúa.
Antes de empezar: borra los .whl sueltos de la raíz (no se suben).

## Paso 1 — Personajes nuevos (imageassets/modelos3d/, ignora los "- copia")
- Importa SkullHombre, Doctor, Inventor, Hacker, RobotHombre y GuardiaNova con CreadorVisualTripo (Humanoid, 1.8 m; jefe 1.95 m; robots 1.85 m). Mecanico: impórtalo pero no lo uses (se descartó).
- Papeles: SkullHombre = jefe de los SKULLS; Doctor = El Doc; Inventor = El Inventor; Hacker = El Hacker; RobotHombre = peatón robot (mezclado con la gente, 1 de cada 4 aprox.); GuardiaNova = guardias parados en Neón Alto (2 a 4, ambiente, no pelean).
- Arregla que "man" se vea negro (textura o material mal importado) y úsalo también como base de NPC hombres.
- Los demás NPC (pandilleros SKULLS y SNAKES comunes, vecino, vendedor, peatones) siguen saliendo de man/woman/woman2 con colores distintos.
- Animaciones: usa las que hay (caminar, correr, agree/afraid/angry como quieto y variaciones). Que la gente de banqueta no esté congelada: unos caminan por la banqueta y otros están de pie con quieto que respira; que no todos se muevan igual (desfase aleatorio). Robots con las mismas animaciones.
- Cuando existan los clips de Mixamo en Assets/Arte/Personajes/Animaciones/ (docs/animaciones-faltan.md), corre CreadorVisualTripo.CrearTodos para conectarlos.

## Paso 2 — Carros reales y todos robables
- Importa V02, V03, V04, V05, V06 y V08 (y V01a, V01b ya están) con el flujo de T-250: prefab con Visual, URP, LOD, BoxCollider.
- Quita TODOS los carros decorativos que no se manejan: cada carro estacionado de la ciudad es un carro manejable y robable (con alarma y calor), usando los 8 modelos repartidos al azar. Los carros del tráfico también usan los modelos reales; si el jugador se acerca a uno detenido y pulsa E, el conductor se baja y huye y el carro es tuyo (sube el calor).
- Ruedas: si el modelo trae las ruedas pegadas, oculta las Rueda_* de greybox (que sigan girando invisibles para la física) y que no se vean cilindros por fuera.
- Cámara del carro ajustada a la altura de cada modelo. Revisa que no se atoren en banquetas ni postes.

## Paso 3 — Terminar T-509 (rama tony/T-509-cierre-pulido)
- Puesto de tacos del pasillo de El Mercado: cambia trompos, garrafón, mostrador, bancos y caja colgada de bloques por props reales (C12, huacales, tambos, CY06) o quítalos.
- Pistola que suelta el SKULL: si no hay modelo, que sea pequeña y con brillo de objeto para recoger (y el texto "E recoger").
- Neón Alto verde como la referencia (con captura).

## Paso 4 — Iluminación por zona (comparar con docs/referencias-ciudad/)
- Callejón y El Mercado: más luz (focos N01, neón magenta, luz naranja del portón) sin perder lo oscuro; que se vean los objetos.
- El Tianguis: más puestos (N03a/N03b, C12, C13) en hileras a los dos lados.
- Lluvia también de día (ligera) y charcos.
- Azotea: techos menos oscuros (luz de luna y rebote).
- Capturas lado a lado nuevas por zona.

## Paso 5 — Fondo de la ciudad
- Si existe imageassets/fondos/ (fondo-suburbios, fondo-neon-alto, fondo-nova): pon un fondo lejano tipo matte painting en un cilindro grande alrededor de la ciudad (sin iluminación, que no proyecte sombra ni reciba niebla de cerca, fundido con el color del cielo arriba): fondo-suburbios hacia el oeste y sur, fondo-neon-alto hacia el este, fondo-nova hacia el norte. Deja solo las torres 3D cercanas (las 14 de Neón Alto) y quita las 48 lejanas si el fondo las cubre.
- Si no existe, salta este paso y anótalo.

## Paso 6 — Bot del recorrido
- Arregla el bot de RecorridoDemoPruebas en la pelea de El Tianguis (que use cubrirse, el aliado y el apuntado asistido, o que el modo prueba le dé ventaja sin cambiar el juego) para que llegue al jefe, El Hacker y CONTINUARÁ sin ayuda. No cambies la dificultad del juego.

Al terminar: reporte corto en docs/estado-tony.md (qué quedó, capturas, FPS, qué falta).
