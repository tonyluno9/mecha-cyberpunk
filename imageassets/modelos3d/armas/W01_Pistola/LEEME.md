# W01 Pistola — modelo y animaciones (Tony, 2-nov-2026)
Origen: C:\Users\Angel Perez\Downloads\pistola (copiado aquí tal cual).
- W01_Pistola.fbx / W01_Pistola.blend: la pistola v2, separada en piezas (24 partes de Tripo: corredera, cargador, etc.) para poder animarla.
- W01_Pistola_Disparo.blend: animación de disparo (corredera atrás y adelante).
- W01_Pistola_Recarga.blend: animación de recarga (sale y entra el cargador).
- W01_Pistola_original_tripo.fbx + texturas_tripo/: el modelo original de Tripo y sus texturas (basecolor, normal, metallic, roughness por parte).

Para Claude Code:
1. Exporta las dos animaciones a FBX con Blender en línea de comandos (blender -b <archivo>.blend --python-expr que exporte FBX con bake de animación, escala 1, Y arriba), o importa los .blend directo si Unity tiene Blender configurado. Revisa los nombres de las acciones.
2. Importa a Assets/Arte/Armas/W01_Pistola/: modelo + clips "Disparo" y "Recarga" (Generic, sin bucle). Material URP Lit con las texturas.
3. Reemplaza la pistola de piezas sencillas: en la mano del jugador y de los SKULLS, y la que se tira en el piso (con giro y brillo). Que el clip Disparo suene con ARM_PIS_DISPARO y el destello, y Recarga dure lo que dura la recarga del juego (ajusta la velocidad del clip) con ARM_PIS_RECARGA.
4. Tamaño real: unos 20 cm de largo en la mano de una persona de 1.8 m; agarre en la mano derecha.
5. Captura en la mano, en el piso y en el panel del arma del HUD.
