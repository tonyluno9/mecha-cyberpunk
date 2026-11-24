# VR Quest 3 (PCVR por Quest Link) · tareas 23-nov

Pedido de Tony: el juego en primera persona en Meta Quest 3, corriendo en su PC (RTX 5070 Laptop, 32 GB, Windows 11) por Quest Link / Air Link.
Rama aparte `vr/quest3`, carpeta aparte `C:\Users\Angel Perez\Desktop\videojuego\wtVR`. El juego normal (main) NO se toca.
Objetivo de tiempo: 3–4 horas. Lo indispensable primero; lo demás básico.

## VR1 · Base VR (primero, bloquea a las demás)
- Paquetes: XR Plugin Management + OpenXR (perfil Meta Quest Touch Plus / Oculus Touch), XR Interaction Toolkit con Starter Assets. Nada del Meta SDK.
- Modo VR activable sin romper el modo normal: flag `ModoVR` (si no hay visor, el juego sigue en tercera persona).
- XR Origin en el jugador: la cámara es la cabeza, a la altura de PJ01 (1.84 m, modo de seguimiento Floor). Cuerpo de PJ01 oculto para la cámara o solo manos/antebrazos visibles.
- Caminar con stick izquierdo a la velocidad de caminar/trote actuales (1.62 / 4.53, clic del stick = correr), hacia donde mira la cabeza. Giro por pasos de 45° con stick derecho. Viñeta anti-mareo al moverse.
- Quitar en VR: temblores de cámara, sacudidas por golpes, empujones/derribos/levantarse del jugador (en su lugar, fundido a negro corto si hace falta), cámaras de cinemática que muevan la cabeza (las cinemáticas se ven en una pantalla virtual o se saltan).
- Calidad para VR: bajar postproceso pesado, sombras y escala de render para 72–90 fps estables en la RTX 5070.
- Comprobar: en Play con el visor, caminar y girar por la ciudad sin mareo ni temblor.

## VR2 · Pistola en la mano y golpes
- La pistola actual (W de ArmasAPie) va en la mano derecha (control derecho), apuntando hacia adelante del control.
- Disparar con el gatillo derecho: el raycast/bala sale del cañón en la dirección del arma; mismo daño, munición y sonido que hoy. Sin mira de pantalla (opcional: láser rojo tenue).
- Recarga: botón A (simplificada).
- Golpes sin implante: colisionador en cada mano; si la mano choca a un enemigo con velocidad > 1.5 m/s hace el daño de puño actual (más daño cuanto más rápido) + vibración del control + sonido de golpe.
- Comprobar: disparar y golpear a un SKULL en el campo de tiro o en la calle y que le baje la vida.

## VR3 · Implante: agarrar y lanzar
- Solo con el implante de brazos instalado (el mismo que hoy da más fuerza).
- Botón de agarre (grip) con la mano cerca de un enemigo (< 0.4 m): lo sujeta; el enemigo pasa a cuerpo físico (ragdoll si tiene; si no, su Rigidbody/cápsula sin IA) pegado a la mano.
- Al soltar el grip, sale lanzado con la velocidad de la mano × multiplicador del implante; daño al chocar contra paredes/suelo/otros.
- Golpe con la otra mano mientras lo sostienes: lo suelta y lo manda a volar en la dirección del golpe.
- Sin implante: el grip no agarra enemigos.
- Después de volar: el enemigo vuelve a su IA o queda derrotado según su vida.
- Comprobar: con implante, agarrar a un SKULL, lanzarlo contra una pared; golpearlo sosteniéndolo y que salga volando.

## VR4 · Menús básicos (al final, sin pulir)
- Todos los Canvas que hoy son de pantalla se muestran en VR como panel en el espacio a ~1.5 m frente a la cabeza; se eligen con láser del control derecho + gatillo.
- Menú/pausa con el botón Menú del control izquierdo. Rueda de Tab con botón Y. HUD (vida, dinero) en la muñeca izquierda.
- Tienda de Tino, máquinas, Doc: mismo flujo con láser.
- Carros: fuera en VR (no se pueden manejar; aviso "NO DISPONIBLE EN VR").

## Entrega
- Build: `Builds/VR/CyberCity.exe` (solo PC, con OpenXR). Tony lo abre con Quest Link activo.
- Reporte en `docs/reporte-vr-23nov.md` con qué quedó, qué no y qué debe probar Tony con el visor.
