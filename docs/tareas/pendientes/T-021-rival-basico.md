# T-021 Rival básico
Dueño: Compañero | Rama: compa/T-021-rival-basico | Modelo: Sonnet 5.5, medium
Depende de: T-001, T-020

## Objetivo
Existe un rival greybox que se acerca a su objetivo, avisa cada golpe antes de darlo, pega por el contrato de Core,
recibe daño y cae al llegar a 0. Es la otra mitad de la meta de la fase 1: un rival que te golpea.

## Contexto
- Diseño: docs/diseno/combate.md y docs/diseno/personaje-progresion.md (el rival de la liga sótano es el más fácil).
- Decisiones: D-005 (datos), D-006 (ModeloVida), D-009 (ignorar el propio `transform.root`), D-012 (la preparación del
  golpe es el aviso visible que permite bloquear), D-015 (sin NavMesh).
- Contratos de Core: `IRecibeDanio`, `InfoDanio`, `ResultadoDanio`, `IVida`, `ModeloVida`, `IPeleador`, `LadoPelea`,
  `EventosPelea.PublicarImpacto`, `DatosMecha` y `DatosRival`. Lee el contrato escrito en los comentarios de
  `IRecibeDanio` y `IPeleador` antes de implementarlos.
- El jugador real (prefab de Tony) llega en T-013. Mientras, pruebas contra un `PeleadorDePrueba` que creas aquí.
  El rival solo conoce a su objetivo por `IPeleador` e `IRecibeDanio`, así funcionará igual con el jugador real.
- Ensamblado: IA/ solo referencia Core.

## Qué hacer
1. Crea `IA/Datos/DatosMecha_RivalPrueba.asset` (como los valores iniciales, pero `preparacionGolpe` de 0.6 s para que el
   aviso se pueda leer) y `IA/Datos/DatosRival_Prueba.asset` que lo use.
2. Lógica pura en `IA/Rival/CerebroRival.cs`: con la distancia al objetivo, si el objetivo está en KO, si el rival tiene
   control, el tiempo transcurrido (`dt`) y los `DatosRival`, decide la acción: Esperar, Acercarse o EmpezarGolpe.
   Se acerca si está más lejos que `DistanciaAtaque`; empieza un golpe cuando está en distancia y ya pasó la espera;
   después de cada golpe elige una espera nueva al azar entre la mínima y la máxima (azar inyectado);
   sin control o con el objetivo en KO, espera.
3. `IA/Rival/RivalBasico.cs` (MonoBehaviour con CharacterController, `IPeleador` e `IRecibeDanio`):
   - Recibe su objetivo como `GameObject` en el inspector y saca `IPeleador` e `IRecibeDanio` en `Awake`.
   - Camina hacia el objetivo con `VelocidadMaxima` y `Aceleracion`, gira hacia él sin pasar de `VelocidadGiroMaxima`
     y nunca se acerca más que `DistanciaMinima`.
   - Golpe: durante `PreparacionGolpe` echa atrás su brazo y lo pinta de naranja emisivo (el aviso); en el impacto, si el
     objetivo sigue dentro de `AlcanceGolpe` y enfrente, le manda un `InfoDanio` con `DanioGolpe`; luego espera `RecargaGolpe`.
   - Vida con `ModeloVida`: al recibir daño parpadea en blanco una fracción de segundo y publica `ImpactoRecibido`.
   - En KO deja de moverse y de atacar y se cae al piso inclinándose en alrededor de un segundo.
   - `ActivarControl(false)` lo deja quieto; `PrepararPelea` lo levanta, lo restaura y lo coloca.
   - Colores, duración del parpadeo y de la caída en `IA/Datos/ConfigPresentacionRival.cs` con su asset.
4. Prefab `IA/Prefabs/RivalBasico.prefab`: cápsula de unos 4 m con un brazo de cubos al frente, de otro color que el piso.
5. `IA/Pruebas/PeleadorDePrueba.cs`: una cápsula de 4 m que implementa `IPeleador` e `IRecibeDanio` con `ModeloVida`,
   escribe en la consola el daño que recibe y, con una `InputAction` del inspector (tecla F), le hace un daño configurable
   al rival si está a menos de 3 m. No se mueve sola: se mueve en la vista de escena durante Play.
   T-022 también la usa.
6. En Pruebas_Compa.unity: pon el PeleadorDePrueba en `InicioJugador`, el rival en `InicioRival` con el PeleadorDePrueba
   como objetivo, y deja la cámara detrás del PeleadorDePrueba. Para probar `ActivarControl`, agrega en
   `IA/Pruebas/` un componente con una `InputAction` (tecla C) que alterna el control del rival.

## Carpetas permitidas
Assets/_Proyecto/IA/, Assets/_Proyecto/Tests/EditMode/IA/ y Assets/_Proyecto/Escenas/Pruebas_Compa.unity
No tocar: Core/, Mecha/, Combate/, UI/, Arena/ (salvo usar el prefab de la jaula), Clandestino.unity, Pruebas_Tony.unity
ni ProjectSettings/.

## Criterios de aceptación (se comprueban jugando)
- [ ] Al dar Play en Pruebas_Compa.unity, el rival camina hacia el PeleadorDePrueba y se detiene a distancia de golpe sin atravesarlo
- [ ] Antes de cada golpe, el brazo del rival se echa atrás y se pone naranja por más de medio segundo; luego golpea
- [ ] Cada golpe que conecta aparece en la consola con 10 de daño
- [ ] Si muevo el PeleadorDePrueba a otro lado de la jaula, el rival lo sigue y gira hacia él poco a poco, no de golpe
- [ ] Entre golpes el rival espera un tiempo que cambia de un golpe a otro
- [ ] Con F cerca del rival, el rival parpadea en blanco y su vida baja (se ve en el inspector)
- [ ] Al llegar a 0, el rival se cae al piso, deja de moverse y de atacar, y más F no hace nada
- [ ] Con C el rival se queda quieto y deja de atacar; con C otra vez sigue
- [ ] No aparecen errores en la consola en 3 minutos de prueba

## Pruebas automáticas
EditMode, en Tests/EditMode/IA/:
- CerebroRival: lejos se acerca; en distancia y con la espera cumplida empieza un golpe; en distancia pero esperando, espera;
  con el objetivo en KO o sin control, espera; la espera nueva siempre queda entre la mínima y la máxima (con un azar fijo).

## Fuera de alcance
- Bloqueo, esquivas y adaptación del rival
- Varios rivales, ligas, nombres finales
- Flujo de pelea y revancha (T-022)
- Mostrar la vida del rival en pantalla (D-011)
