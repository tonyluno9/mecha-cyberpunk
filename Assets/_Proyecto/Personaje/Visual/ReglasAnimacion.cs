using System;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>Cómo cae un personaje al quedar KO (parámetro MuerteTipo).</summary>
    public enum MuerteVisual
    {
        Adelante = 0,
        Atras = 1,
        Desmayo = 2,
        /// <summary>T-1540: sale volando hacia atrás por una explosión (PJ01_Muerte_Explosion).</summary>
        Explosion = 3,
        /// <summary>T-1540: lo arrolla un carro (PJ01_Muerte_Atropello).</summary>
        Atropello = 4,
        /// <summary>T-1540: cae de una altura mortal (PJ01_Muerte_CaidaAltura, boca abajo).</summary>
        CaidaAltura = 5,
    }

    /// <summary>T-1540: qué causó el golpe que tumba al personaje; decide el clip de muerte (<see cref="MuerteVisual"/>).</summary>
    public enum MotivoDanioVisual
    {
        Normal = 0,
        Explosion = 1,
        Atropello = 2,
        CaidaAltura = 3,
    }

    /// <summary>T-1540: cómo reacciona a una explosión que lo alcanza (<see cref="ReglasAnimacion.ReaccionAExplosion"/>).</summary>
    public enum ReaccionExplosion
    {
        /// <summary>Daño chico: la reacción normal de herido.</summary>
        Ninguna = 0,
        /// <summary>Explosión cercana que no lo tumba: tambalea (PJ01_Dano_ExplosionCercana).</summary>
        Tambaleo = 1,
        /// <summary>Lo derriba y se levanta (PJ01_Derribo_Explosion, luego PJ01_Dano_Levantarse_BocaArriba).</summary>
        Derribo = 2,
        /// <summary>Lo mata la explosión: sale volando (PJ01_Muerte_Explosion).</summary>
        Muerte = 3,
    }

    /// <summary>Qué reacción de daño se muestra (parámetro DanoTipo).</summary>
    public enum DanoVisual
    {
        Frente = 0,
        Espalda = 1,
        Fuerte = 2,
        /// <summary>T-1515: el golpe pega por el lado izquierdo del jugador (PJ01_Dano_Izquierda).</summary>
        Izquierda = 3,
        /// <summary>T-1515: el golpe pega por el lado derecho del jugador (PJ01_Dano_Derecha).</summary>
        Derecha = 4,
    }

    /// <summary>T-1515: valor del parámetro Escalones.</summary>
    public enum EscalonesVisual
    {
        Ninguno = 0,
        Subiendo = 1,
        Bajando = 2,
    }

    /// <summary>Gestos de una sola vez de la capa base (parámetro GestoTipo): carro, interacción y burla/presumir de enemigos (T-622 a T-624).</summary>
    public enum GestoVisual
    {
        Ninguno = 0,
        CarroEntrar = 1,
        CarroSalir = 2,
        Pitar = 3,
        AbrirPuerta = 4,
        Boton = 5,
        Recoger = 6,
        Telefono = 7,
        Burla = 8,
        Presumir = 9,
        /// <summary>T-963: subir a la moto (PJ01_Moto_Subir) y quedarse manejando (PJ01_Moto_Manejar) mientras Manejando siga prendido.</summary>
        MotoSubir = 10,
        /// <summary>T-963: bajar de la moto (PJ01_Moto_Bajar).</summary>
        MotoBajar = 11,
        /// <summary>T-1540: empujón de un maleante (PJ01_Dano_Empujon, retrocede ~0.55 m).</summary>
        Empujon = 12,
        /// <summary>T-1540: explosión cercana que no lo tumba (PJ01_Dano_ExplosionCercana).</summary>
        ExplosionCercana = 13,
        /// <summary>T-1540: la explosión lo derriba (PJ01_Derribo_Explosion) y se levanta boca arriba.</summary>
        DerriboExplosion = 14,
    }

    /// <summary>
    /// Nombres y reglas del controlador base humanoide (T-561). Lógica pura, sin Animator: la usan VisualPersonaje, el constructor de editor
    /// y las pruebas. Los parámetros de ReglasVisual (Velocidad, EnPiso, Arma, KO...) se conservan; aquí están los nuevos.
    /// </summary>
    public static class ReglasAnimacion
    {
        public const string VelX = "VelX";
        public const string VelZ = "VelZ";
        public const string VelY = "VelY";
        public const string Giro = "Giro";
        public const string Agachado = "Agachado";
        public const string DanoTipo = "DanoTipo";
        public const string MuerteTipo = "MuerteTipo";
        public const string GolpeTipo = "GolpeTipo";
        public const string CoberturaTipo = "CoberturaTipo";
        public const string DisparoCobertura = "DisparoCobertura";
        public const string Frenar = "Frenar";
        /// <summary>T-1515: 0 = nada, 1 = subiendo escalones, 2 = bajando escalones (<see cref="EscalonesVisual"/>). Lo pone VisualPersonaje del jugador desde DetectorEscalones.</summary>
        public const string Escalones = "Escalones";
        /// <summary>T-1515: rapidez (x) de los estados EscalonesSubir y EscalonesBajar según la velocidad real (pies sin patinar).</summary>
        public const string RapidezEscalones = "RapidezEscalones";
        /// <summary>T-1505: trigger del aterrizaje fuerte (PJ01_Aterrizaje_Fuerte); lo dispara VisualPersonaje del jugador al tocar el piso tras una caída de <see cref="VelocidadAterrizajeFuerte"/>.</summary>
        public const string AterrizajeFuerte = "AterrizajeFuerte";
        /// <summary>T-1550: trigger de la rodada al caer corriendo (PJ01_Salto_Correr_Rodada, sin bloquear el movimiento); lo dispara VisualPersonaje del jugador al tocar el piso según <see cref="TipoAterrizaje"/>.</summary>
        public const string AterrizajeRodada = "AterrizajeRodada";
        /// <summary>
        /// T-1550: true mientras el jugador pidió saltar y aún no despega (los 3 cuadros, 0.1 s, de la agachada de PJ01_Salto_Despegue: el impulso sale en el cuadro 4). Lo pone ControladorAPie en VisualPersonaje;
        /// con él el salto empieza en el piso (SaltoInicio o SaltoCorrerInicio) y no espera a dejar el suelo.
        /// </summary>
        public const string PreparandoSalto = "PreparandoSalto";
        /// <summary>T-985: puños en guardia (estado PunosGuardia de la capa de arriba, PJ01_Punos_Guardia). Lo pone VisualPersonaje unos segundos después de cada golpe de puños, solo en quien tiene <c>GuardiaTrasGolpe</c> (el jugador).</summary>
        public const string EnGuardia = "EnGuardia";
        /// <summary>T-1310: el jugador lleva un arma larga (aunque no apunte). Con ella, cubrirse usa PJ01_CoberturaRifle_* en vez de los de pistola.</summary>
        public const string ArmaLargaEquipada = "ArmaLargaEquipada";
        /// <summary>T-985: segundos que el jugador sigue en guardia tras su último golpe de puños.</summary>
        public const float SegundosGuardiaPunos = 2.5f;
        /// <summary>Tiempo normalizado fijo del estado CoberturaMantener (la última pose de entrar).</summary>
        public const string PoseCobertura = "PoseCobertura";

        /// <summary>Trigger que lanza un gesto; el tipo va en GestoTipo (<see cref="GestoVisual"/>).</summary>
        public const string Gesto = "Gesto";
        public const string GestoTipo = "GestoTipo";
        /// <summary>Sentado al volante (estado CarroManejar). Se pone junto con el gesto CarroEntrar y se quita al bajar.</summary>
        public const string Manejando = "Manejando";
        /// <summary>Viendo el celular de pie (estado VerTelefono, en bucle).</summary>
        public const string ViendoTelefono = "ViendoTelefono";
        /// <summary>Etiqueta de los estados de la capa base que apagan el torso (gestos, manejar, celular).</summary>
        public const string EtiquetaGesto = "Gesto";

        public const string CapaBase = "Base";
        public const string CapaArriba = "Arriba";
        /// <summary>
        /// T-735: capa ADITIVA con la máscara del torso donde suenan Pistola_Dano_Frente, Espalda y Fuerte: el torso se estremece encima de lo que
        /// ya juegue (apuntar, disparar, caminar) sin quitarle la pose. Los tres clips empiezan y terminan en Pistola_Apuntar, que es su pose de referencia.
        /// </summary>
        public const string CapaDano = "Dano";
        /// <summary>
        /// T-735: un golpe de esta fracción de la vida máxima (o más) se muestra como daño fuerte (Pistola_Dano_Fuerte); menos, de frente o de espalda
        /// según de dónde viene. Es el umbral de <c>AnimacionJugador</c>.
        /// </summary>
        public const float FraccionDanoFuerte = 0.25f;
        /// <summary>T-733: dirección local con la que se apunta caminando (el árbol 2D de la capa de arriba). Es VelX/VelZ de quien calcula la dirección y 0 en quien no.</summary>
        public const string ApuntarX = "ApuntarX";
        public const string ApuntarZ = "ApuntarZ";
        /// <summary>T-739: 0 = caminar y correr sin implante (Loco_*_SinImplante en el jugador), 1 = con implante de piernas o brazos (Loco_*_ConImplante). Los demás papeles lo dejan en 0 y siguen con Loco_Caminar y Loco_Correr.</summary>
        public const string ConImplante = "ConImplante";
        /// <summary>T-733: dispara la rodada (Loco_Rodar, 1 s). La distancia (2.2 m) la mueve el controlador del jugador.</summary>
        public const string Rodar = ReglasVisual.Rodar;
        /// <summary>T-733: velocidad (m/s) a la que se mezcla del clip de pistola agachada quieta al de caminando (la del jugador agachado, ~1.6).</summary>
        public const float VelocidadAgachadoCaminando = 1.6f;
        /// <summary>T-733: duración (s) y distancia (m) de la rodada: son las de Loco_Rodar.</summary>
        public const float DuracionRodada = 1f;
        public const float DistanciaRodada = 2.2f;

        /// <summary>T-733: velocidad (m/s) a la que avanza Loco_AgachadoCaminar tal como viene (1.07 m en un ciclo de 2 s, agarre.txt).</summary>
        public const float VelocidadClipAgachado = 0.535f;
        /// <summary>T-683: cuánto más rápido que el clip se deja deslizar a los pies al caminar agachado (el jugador va a 1.6 m/s y el clip a 0.4: sin este tope los pies correrían).</summary>
        public const float DeslizamientoAgachado = 2.5f;
        /// <summary>T-683: rapidez mínima y máxima (x) del clip de caminar agachado.</summary>
        public const float RapidezMinimaAgachado = 0.8f;
        public const float RapidezMaximaAgachado = 2.8f;

        /// <summary>
        /// T-683: rapidez (x) de Rifle_AgachadoCaminar para la velocidad real: sube con ella, así con Shift los pies se mueven más rápido y no solo deslizan.
        /// <paramref name="escalaClips"/> es la escala del modelo contra la que se midió el clip.
        /// </summary>
        public static float RapidezAgachado(float velocidadReal, float escalaClips)
        {
            return EscalaVelocidad(velocidadReal, VelocidadClipAgachado * DeslizamientoAgachado * Mathf.Max(0.1f, escalaClips), RapidezMinimaAgachado, RapidezMaximaAgachado);
        }

        /// <summary>Velocidad (m/s) a la que avanza el clip Loco_Caminar tal como viene, medida con MedicionClips (T-562; T-733: el Loco_Caminar ajustado de Tony mide 0.997).</summary>
        public const float VelocidadClipCaminar = 0.997f;
        /// <summary>Velocidad (m/s) a la que avanza el clip Loco_Correr tal como viene, medida con MedicionClips (T-562; T-736: el Loco_Correr ajustado de Tony mide 2.51).</summary>
        public const float VelocidadClipCorrer = 2.51f;
        /// <summary>Rapidez máxima de la animación: deja que Correr iguale los 7.5 m/s del jugador sin toparse (7.5 / 2.51 = 2.99).</summary>
        public const float RapidezMaximaLocomocion = 3f;
        /// <summary>T-739: velocidad (m/s) a la que avanzan Loco_Caminar_SinImplante / ConImplante y Loco_Correr_SinImplante / ConImplante tal como vienen, medida con MedicionClips (la del jugador va con Animator.speed 1, T-724).</summary>
        public const float VelocidadClipCaminarSinImplante = 1.293f, VelocidadClipCaminarConImplante = 1.284f, VelocidadClipCorrerSinImplante = 3.19f, VelocidadClipCorrerConImplante = 3.052f;
        /// <summary>Rapidez máxima de Caminar antes de pasar a Correr (a 2.2x el clip aún no se ve acelerado de más).</summary>
        public const float RapidezMaximaCaminar = 2.2f;
        /// <summary>Velocidad (m/s) normal de caminar del jugador y los aliados (Caminar en los árboles de arma).</summary>
        public const float VelocidadAndar = 4.5f;
        /// <summary>Velocidad (m/s) de correr (Shift) del jugador y los aliados (Correr en los árboles de arma).</summary>
        public const float VelocidadCarrera = 7.5f;
        /// <summary>
        /// Velocidad (m/s) a partir de la cual el controlador base pasa de Caminar a Correr (T-640): entre andar (4.5) y correr (7.5),
        /// para que caminar normal sea Caminar y solo Shift sea Correr.
        /// </summary>
        public const float UmbralCorrer = 5.5f;
        /// <summary>Fracción del umbral con la que se vuelve de Correr a Caminar (evita parpadeo en el umbral).</summary>
        public const float HisteresisCorrer = 0.9f;
        /// <summary>Segundos de mirada al jugador en rango antes de que la cabeza voltee.</summary>
        public const float RetardoMirada = 1f;
        /// <summary>Segundos que el jugador queda boca arriba o abajo después de despertar, antes de levantarse (el fundido a negro dura ~1 s).</summary>
        public const float RetardoLevantarse = 0.8f;
        /// <summary>Caída más rápida que esto (m/s hacia abajo) se muestra como Caida y no como salto.</summary>
        public const float VelocidadCaidaLibre = 7f;
        /// <summary>Giro (°/s) a partir del cual el personaje quieto gira en el sitio.</summary>
        public const float UmbralGiro = 70f;
        /// <summary>Velocidad desde la que soltar el avance se muestra como frenar.</summary>
        public const float VelocidadFrenar = 5f;

        // ---------- Rapidez de los estados (T-652) ----------

        /// <summary>Parámetro de rapidez de los golpes de puños del jugador (los enemigos no lo cambian, se quedan en 1).</summary>
        public const string RapidezGolpe = "RapidezGolpe";
        /// <summary>
        /// Rapidez de los tres clips de puños del jugador (x). El golpe del juego conecta a los 0.12 s y se recupera a los 0.57 s; los clips de Mixamo
        /// tardan 1.8 a 2.2 s y conectan a los 0.5 a 1.1 s. Con 3.3 conectan a 0.15 a 0.33 s y terminan a 0.55 a 0.67 s.
        /// T-962: los PJ01_Punos_* de Blender duran 1.43 a 1.83 s (0.8 de los de Mixamo, con el impacto en la misma fracción del clip): con 2.7 conectan a los 0.13 a 0.21 s
        /// y terminan a los 0.53 a 0.68 s, como antes.
        /// </summary>
        public const float RapidezGolpeJugador = 2.7f;
        /// <summary>T-944: qué tan rápido cae el jugador al morir (1 = el clip tal como viene). T-962: los PJ01_Muerte_* de Blender ya vienen 1.7 veces más rápidos que los de Mixamo.</summary>
        public const float RapidezMuerteJugador = 1f;

        /// <summary>
        /// Rapidez (x) del estado del controlador base con ese nombre; 1 si no se acelera. Los clips de Mixamo traen mucha espera antes y después de la acción:
        /// subir al carro dura 5.5 s en el clip y en el juego es un momento. Disparo y recarga de arma no se tocan.
        /// </summary>
        public static float VelocidadEstado(string estado)
        {
            switch (estado)
            {
                case "CarroEntrar": case "CarroSalir": return 3f;
                case "AbrirPuerta": return 4f;
                case "Boton": case "Recoger": return 2.5f;
                case "SaltoInicio": return 2.5f;
                case "SaltoAterrizaje": return 2f;
                case "GirarDer": case "GirarIzq": return 1.5f;
                case "CoberturaDisparo1": case "CoberturaDisparo2": return 1.5f;
                case "LevantarseBocaArriba": case "LevantarseBocaAbajo": return 2.5f;
                // T-963: los clips de moto de Blender duran 1.77 s; subir y bajar en 1.2 s.
                case "MotoSubir": case "MotoBajar": return 1.5f;
                default: return 1f;
            }
        }

        /// <summary>Estados de la capa base del controlador base (el nombre es el del estado).</summary>
        public static readonly string[] EstadosBase =
        {
            "Idle", "Caminar", "Correr", "GirarDer", "GirarIzq", "Frenar", "SaltoInicio", "SaltoAire", "SaltoAterrizaje", "Caida",
            "AgachadoCaminar", "AgachadoQuieto", "Agacharse", "Pararse", "Rodar", "CoberturaEntrar", "CoberturaMantener", "CoberturaAsomarse", "CoberturaRegresar", "CoberturaDisparo1", "CoberturaDisparo2", "GirarApuntarDer", "GirarApuntarIzq",
            "CoberturaRifleEntrar", "CoberturaRifleIdle", "CoberturaRifleDisparo",
            "MuerteAdelante", "MuerteAtras", "MuerteDesmayo", "LevantarseBocaArriba", "LevantarseBocaAbajo",
            "CarroEntrar", "CarroManejar", "CarroSalir", "AbrirPuerta", "Boton", "Recoger", "Telefono", "VerTelefono", "Burla", "Presumir",
            "MotoSubir", "MotoManejar", "MotoBajar", "AterrizajeFuerte", "EscalonesSubir", "EscalonesBajar",
            // T-1540: muertes por explosión, atropello y caída; derribo por explosión; empujón y explosión cercana.
            "MuerteExplosion", "MuerteAtropello", "MuerteCaidaAltura", "DerriboExplosion", "DanoEmpujon", "DanoExplosionCercana",
            // T-1540: salto corriendo (despegue, aire, aterrizaje y la rodada del aterrizaje fuerte corriendo), aprobados el 20-nov.
            "SaltoCorrerInicio", "SaltoCorrerAire", "SaltoCorrerAterrizaje", "SaltoCorrerRodada",
        };

        /// <summary>T-1540: estados del salto corriendo -> clip PJ01. Se entra desde Correr (Velocidad arriba de <see cref="UmbralCorrer"/> x <see cref="HisteresisCorrer"/>) al dejar el piso; la rodada, con el aterrizaje fuerte corriendo.</summary>
        public static readonly (string Estado, string Clip)[] EstadosSaltoCorrerT1540 =
        {
            ("SaltoCorrerInicio", "PJ01_Salto_Correr_Despegue"), ("SaltoCorrerAire", "PJ01_Salto_Correr_Aire"),
            ("SaltoCorrerAterrizaje", "PJ01_Salto_Correr_Aterrizaje"), ("SaltoCorrerRodada", "PJ01_Salto_Correr_Rodada"),
        };

        /// <summary>
        /// T-1540: multiplicadores de la rapidez de SaltoInicio y SaltoAterrizaje (el estado trae 2.5x y 2x, de los clips viejos). El jugador usa los clips Callejero del 20-nov (Despegue 8 cuadros, Aterrizaje 14),
        /// que van a 1x (BITACORA: "SaltoInicio 2.5 a 1, SaltoAterrizaje 2 a 1"): su parámetro vale 1 / la rapidez del estado. Los demás se quedan en 1 (siguen con los clips viejos a su rapidez).
        /// </summary>
        public const string RapidezSaltoInicio = "RapidezSaltoInicio", RapidezSaltoAterrizaje = "RapidezSaltoAterrizaje";

        public static float AjusteSaltoJugador(string estado) => 1f / Mathf.Max(0.01f, VelocidadEstado(estado));

        /// <summary>T-1540: estado de la capa base -> (clip PJ01, tipo de gesto o -1 si es una muerte). Lo usan el constructor del controlador, el diagnóstico y las pruebas.</summary>
        public static readonly (string Estado, string Clip, int Gesto)[] EstadosT1540 =
        {
            ("MuerteExplosion", "PJ01_Muerte_Explosion", -1), ("MuerteAtropello", "PJ01_Muerte_Atropello", -1), ("MuerteCaidaAltura", "PJ01_Muerte_CaidaAltura", -1),
            ("DerriboExplosion", "PJ01_Derribo_Explosion", (int)GestoVisual.DerriboExplosion),
            ("DanoEmpujon", "PJ01_Dano_Empujon", (int)GestoVisual.Empujon),
            ("DanoExplosionCercana", "PJ01_Dano_ExplosionCercana", (int)GestoVisual.ExplosionCercana),
        };

        /// <summary>T-1540: una explosión que quita esta fracción de la vida máxima (o más) derriba; entre <see cref="FraccionTambaleoExplosion"/> y esta, tambalea.</summary>
        public const float FraccionDerriboExplosion = 0.3f;
        /// <summary>T-1540: fracción mínima de la vida máxima de una explosión para que tambalee (menos es un herido normal).</summary>
        public const float FraccionTambaleoExplosion = 0.1f;

        /// <summary>
        /// T-1540: qué muestra el personaje al recibir una explosión. <paramref name="direccionGolpe"/> va del origen de la explosión al personaje. La muerte y el derribo lo lanzan hacia atrás
        /// (como el clip), así que solo valen si la explosión viene del frente o de un costado; si le explota por la espalda tambalea o cae como siempre.
        /// </summary>
        public static ReaccionExplosion ReaccionAExplosion(float cantidad, float vidaMaxima, bool murio, Vector3 direccionGolpe, Vector3 frente)
        {
            bool porLaEspalda = GolpeDeEspalda(direccionGolpe, frente);
            if (murio)
            {
                return porLaEspalda ? ReaccionExplosion.Ninguna : ReaccionExplosion.Muerte;
            }
            float fraccion = vidaMaxima > 0f ? cantidad / vidaMaxima : 1f;
            if (fraccion >= FraccionDerriboExplosion && !porLaEspalda)
            {
                return ReaccionExplosion.Derribo;
            }
            return fraccion >= FraccionTambaleoExplosion ? ReaccionExplosion.Tambaleo : ReaccionExplosion.Ninguna;
        }

        /// <summary>
        /// T-1540: la muerte para el motivo del golpe. Explosión (si no viene por la espalda), atropello y caída tienen clip propio; lo demás sale de <see cref="ElegirMuerte(Vector3, Vector3, float)"/>.
        /// </summary>
        public static MuerteVisual ElegirMuerte(Vector3 direccionGolpe, Vector3 frente, float azar, MotivoDanioVisual motivo)
        {
            switch (motivo)
            {
                case MotivoDanioVisual.Explosion:
                    if (!GolpeDeEspalda(direccionGolpe, frente)) { return MuerteVisual.Explosion; }
                    break;
                case MotivoDanioVisual.Atropello:
                    return MuerteVisual.Atropello;
                case MotivoDanioVisual.CaidaAltura:
                    return MuerteVisual.CaidaAltura;
            }
            return ElegirMuerte(direccionGolpe, frente, azar);
        }

        /// <summary>T-1540: un golpe cuya dirección va casi recto hacia abajo (la caída) es de caída de altura.</summary>
        public const float CosenoCaidaAltura = 0.9f;

        /// <summary>T-1540: el motivo del golpe para la animación: explosión, caída (golpe vertical hacia abajo) o normal.</summary>
        public static MotivoDanioVisual MotivoDe(bool esExplosion, bool esGolpe, Vector3 direccionGolpe)
        {
            if (esExplosion)
            {
                return MotivoDanioVisual.Explosion;
            }
            if (esGolpe && direccionGolpe.sqrMagnitude > 1e-6f && Vector3.Dot(direccionGolpe.normalized, Vector3.down) >= CosenoCaidaAltura)
            {
                return MotivoDanioVisual.CaidaAltura;
            }
            return MotivoDanioVisual.Normal;
        }

        /// <summary>Estados de la capa aditiva de daño (T-735).</summary>
        public static readonly string[] EstadosDano = { "Vacio", "DanoFrente", "DanoEspalda", "DanoFuerte", "DanoIzquierda", "DanoDerecha" };

        /// <summary>Estados de la capa de arriba (torso).</summary>
        public static readonly string[] EstadosArriba =
        {
            "Vacio", "GolpeIzq", "GolpeDer", "GolpeFuerte",
            "PunosGuardia", "PistolaIdle", "PistolaSacar", "PistolaGuardar", "PistolaApuntar", "PistolaAgachado", "PistolaDisparo", "PistolaRecargar", "PistolaRecargarCaminar",
            "LargaIdle", "LargaApuntar", "LargaDisparo", "LargaRecargar", "Pitar",
        };

        public static readonly string[] ParametrosFloat = { ReglasVisual.Velocidad, VelX, VelZ, VelY, Giro, PoseCobertura, RapidezGolpe, RapidezEscalones, ApuntarX, ApuntarZ, ConImplante, RapidezSaltoInicio, RapidezSaltoAterrizaje };
        public static readonly string[] ParametrosBool = { ReglasVisual.EnSuelo, ReglasVisual.Apuntando, ReglasVisual.Caer, ReglasVisual.Cubierto, Agachado, Manejando, ViendoTelefono, EnGuardia, ArmaLargaEquipada, PreparandoSalto };
        public static readonly string[] ParametrosInt = { ReglasVisual.TipoArma, DanoTipo, MuerteTipo, GolpeTipo, CoberturaTipo, GestoTipo, Escalones };
        public static readonly string[] ParametrosTrigger =
        {
            ReglasVisual.Disparar, ReglasVisual.Recargar, ReglasVisual.Golpe, ReglasVisual.Herido, DisparoCobertura, Frenar, Gesto, Rodar, AterrizajeFuerte, AterrizajeRodada,
        };

        /// <summary>
        /// Velocidad del Animator para que los pies no patinen: real / la del clip, limitada. Sin velocidad de clip (0) no se ajusta (1).
        /// </summary>
        /// <summary>
        /// T-1505: velocidad (m/s) a la que avanzan los clips de Blender del jugador (BITACORA, pedido 19-nov noche). T-1550 (decisión de Tony del 20-nov): WASD = caminar
        /// (PJ01_Caminar 1.62), Shift = trote con el correr viejo (PJ01_Trote 4.53) y la velocidad rápida (implante o consumible) = PJ01_CorrerRapido 6.25, que reemplaza al
        /// PJ01_Sprint de 7.25 (el archivo se queda, ya no se usa). Trote de lado y hacia atrás 4.00; los de caminar de lado (1.61) y atrás (1.04) quedan en el extremo lento de la mezcla.
        /// </summary>
        public const float VelocidadPJ01Caminar = 1.62f, VelocidadPJ01Trote = 4.53f, VelocidadPJ01CorrerRapido = 6.25f, VelocidadPJ01TroteLado = 4.0f, VelocidadPJ01TroteAtras = 4.0f;
        public const float VelocidadPJ01CaminarLado = 1.61f, VelocidadPJ01CaminarAtras = 1.04f;
        /// <summary>T-1550: velocidad (m/s) a la que avanza PJ01_CaminarApuntando_Derecha (antes 2.0): el caminar de lado de siempre con los brazos sujetando la pistola. El de la izquierda se queda como estaba.</summary>
        public const float VelocidadPJ01CaminarApuntandoDerecha = 1.57f;
        /// <summary>T-1550: velocidad (m/s) real del jugador con la velocidad rápida activa (implante de piernas, habilidad de piernas o consumible): la del clip PJ01_CorrerRapido, para que los pies no patinen.</summary>
        public const float VelocidadRapida = VelocidadPJ01CorrerRapido;
        /// <summary>T-1505: rango de Animator.speed del jugador al igualar los pies con el cuerpo.</summary>
        public const float RapidezMinimaJugador = 0.85f, RapidezMaximaJugador = 1.15f;

        /// <summary>
        /// T-1505: velocidad (m/s) con la que avanza la mezcla 1D por Velocidad (caminar lento, trote) del estado Caminar del jugador: interpola la velocidad
        /// de los dos clips con el mismo peso que usa el árbol (umbrales <paramref name="umbralBajo"/> y <paramref name="umbralAlto"/>).
        /// </summary>
        public static float VelocidadClipMezcla(float velocidad, float umbralBajo, float umbralAlto, float clipBajo, float clipAlto)
        {
            if (umbralAlto <= umbralBajo)
            {
                return clipAlto;
            }
            return Mathf.Lerp(clipBajo, clipAlto, Mathf.InverseLerp(umbralBajo, umbralAlto, velocidad));
        }

        /// <summary>T-1515: velocidad (m/s) hacia adelante a la que avanzan PJ01_Escaleras_Subir (0.47) y PJ01_Escaleras_Bajar (0.49), de la tabla de Blender.</summary>
        public const float VelocidadClipEscalones = 0.48f;
        /// <summary>T-1515: rapidez (x) mínima y máxima de los clips de escalones: arriba de la velocidad máxima se pasa al trote normal (el IK de pies cuida el desnivel).</summary>
        public const float RapidezMinimaEscalones = 1f, RapidezMaximaEscalones = 2.6f;
        /// <summary>T-1515: velocidad (m/s) hasta la que se usan los clips de escalones (andar tranquilo); arriba de ella se queda el trote normal, que el IK de pies ya adapta al desnivel.</summary>
        public const float VelocidadMaximaEscalones = 3.2f;

        /// <summary>T-1515: rapidez (x) de los clips de escalones para la velocidad real (m/s).</summary>
        public static float RapidezEscalonesPara(float velocidadReal) =>
            EscalaVelocidad(velocidadReal, VelocidadClipEscalones, RapidezMinimaEscalones, RapidezMaximaEscalones);

        /// <summary>T-1505: velocidad vertical de llegada (m/s hacia abajo) desde la que el jugador hace el aterrizaje fuerte: ~2.5 m con gravedad 16; arriba de ~4 m rueda (CaidaJugador).</summary>
        public const float VelocidadAterrizajeFuerte = 9f;

        /// <summary>T-1505: true si la velocidad vertical más negativa del aire (m/s) pide el aterrizaje fuerte.</summary>
        public static bool EsAterrizajeFuerte(float velocidadVerticalMinima, float metrosCaidos) => velocidadVerticalMinima <= -VelocidadAterrizajeFuerte && metrosCaidos >= CaidaMinimaAterrizajeFuerte;

        /// <summary>T-1550: qué aterrizaje se muestra al tocar el piso (<see cref="TipoAterrizaje"/>).</summary>
        public enum AterrizajeVisual
        {
            /// <summary>El de siempre: SaltoAterrizaje, SaltoCorrerAterrizaje o la caída en bucle.</summary>
            Normal = 0,
            /// <summary>Cayó corriendo desde alto: PJ01_Salto_Correr_Rodada, rueda y se levanta corriendo (no bloquea).</summary>
            Rodada = 1,
            /// <summary>Cayó desde muy alto sin correr: PJ01_Aterrizaje_Fuerte, tres puntos (bloquea hasta terminar).</summary>
            Fuerte = 2,
        }

        /// <summary>T-1550: velocidad horizontal (m/s) desde la que un salto o una caída son "corriendo" (PJ01_Salto_Correr_*): ~3 m/s, entre el caminar (1.6) y el trote (4.5).</summary>
        public const float VelocidadSaltoCorriendo = 3f;

        /// <summary>T-1550: VelZ mínimo (dirección local hacia adelante, 0 a 1) para el salto corriendo: de lado o hacia atrás se usa el salto parado (el clip de correr solo mira adelante).</summary>
        public const float AdelanteMinimoSaltoCorriendo = 0.5f;

        /// <summary>T-1550: velocidad vertical de llegada (m/s hacia abajo) desde la que caer corriendo es una rodada (~1.1 m con gravedad 16); abajo de ella el aterrizaje corriendo es el normal.</summary>
        public const float VelocidadAterrizajeRodada = 6f;

        /// <summary>T-1550: caída mínima (m) de la rodada (evita dispararla por una velocidad vertical espuria al arrancar o teletransportar).</summary>
        public const float CaidaMinimaRodada = 1f;

        /// <summary>
        /// T-1550: el aterrizaje que toca para una caída de <paramref name="velocidadVerticalMinima"/> m/s (negativa, la más baja del aire) y <paramref name="metrosCaidos"/> m, con la
        /// <paramref name="velocidadHorizontal"/> (m/s) con que toca el piso. Corriendo (&gt;= <see cref="VelocidadSaltoCorriendo"/>) desde ~6 m/s de caída es la rodada (también por arriba de 9: rueda en vez de
        /// quedarse en los tres puntos); sin correr, desde 9 m/s y 2 m es el aterrizaje fuerte; lo demás es normal.
        /// </summary>
        public static AterrizajeVisual TipoAterrizaje(float velocidadVerticalMinima, float metrosCaidos, float velocidadHorizontal)
        {
            bool corriendo = velocidadHorizontal >= VelocidadSaltoCorriendo;
            if (corriendo && velocidadVerticalMinima <= -VelocidadAterrizajeRodada && metrosCaidos >= CaidaMinimaRodada)
            {
                return AterrizajeVisual.Rodada;
            }
            return EsAterrizajeFuerte(velocidadVerticalMinima, metrosCaidos) ? AterrizajeVisual.Fuerte : AterrizajeVisual.Normal;
        }

        /// <summary>T-1505: caída mínima (m) para el aterrizaje fuerte: evita dispararlo por una velocidad vertical espuria al arrancar o al teletransportar.</summary>
        public const float CaidaMinimaAterrizajeFuerte = 2f;

        /// <summary>
        /// T-1484: velocidad a la que avanza la mezcla direccional de un árbol 2D (adelante, atrás, lados) para la dirección local
        /// (<paramref name="x"/> derecha, <paramref name="z"/> adelante). Sin dirección cuenta como adelante.
        /// </summary>
        public static float VelocidadClipDireccional(float x, float z, float adelante, float atras, float lado)
        {
            float pa = Mathf.Max(0f, z), pt = Mathf.Max(0f, -z), pl = Mathf.Abs(x);
            float suma = pa + pt + pl;
            if (suma < 1e-4f)
            {
                return adelante;
            }
            if (atras <= 0f) atras = adelante;
            if (lado <= 0f) lado = adelante;
            return (pa * adelante + pt * atras + pl * lado) / suma;
        }

        /// <summary>T-1484: desnivel (m) por debajo del cual el pie se deja como viene en el clip (piso plano, piel del CharacterController).</summary>
        public const float DesnivelMinimo = 0.03f;

        /// <summary>
        /// T-1484: cuánto hay que subir (+) o bajar (-) un pie para que llegue al piso que tiene debajo, recortado a ±<paramref name="maximo"/>.
        /// Menos de <see cref="DesnivelMinimo"/> no se corrige.
        /// </summary>
        public static float Desnivel(float alturaPiso, float alturaBase, float maximo)
        {
            float d = alturaPiso - alturaBase;
            if (Mathf.Abs(d) < DesnivelMinimo)
            {
                return 0f;
            }
            return Mathf.Clamp(d, -maximo, maximo);
        }

        /// <summary>T-1484: la cadera baja lo que baje el pie más bajo (para que alcance el escalón); si los dos suben, no se mueve.</summary>
        public static float BajadaCadera(float desnivelIzq, float desnivelDer)
        {
            return Mathf.Min(0f, Mathf.Min(desnivelIzq, desnivelDer));
        }

        /// <summary>T-1484: fracción de la inclinación física (atan(v·ω/g)) que se usa y su tope (°): se nota al dar vuelta corriendo sin verse exagerada.</summary>
        public const float FraccionInclinacion = 0.3f, InclinacionMaxima = 10f;

        /// <summary>
        /// T-1484: inclinación del cuerpo hacia dentro de la curva (°, positiva hacia la derecha) a <paramref name="velocidad"/> m/s
        /// girando <paramref name="giroGradosSeg"/> °/s.
        /// </summary>
        public static float InclinacionEnGiro(float velocidad, float giroGradosSeg, float gravedad = 9.81f)
        {
            if (velocidad <= 0.5f || gravedad <= 0f)
            {
                return 0f;
            }
            float fisica = Mathf.Atan(velocidad * giroGradosSeg * Mathf.Deg2Rad / gravedad) * Mathf.Rad2Deg;
            return Mathf.Clamp(fisica * FraccionInclinacion, -InclinacionMaxima, InclinacionMaxima);
        }

        /// <summary>
        /// T-1484: siguiente ángulo (°) del cuerpo hacia <paramref name="objetivo"/>: se acerca con suavizado exponencial (arranca rápido y llega suave)
        /// sin pasar de <paramref name="maximoGradosSeg"/>. A menos de medio grado llega.
        /// </summary>
        public static float GirarSuave(float actual, float objetivo, float dt, float maximoGradosSeg, float suavizado)
        {
            float delta = Mathf.DeltaAngle(actual, objetivo);
            if (Mathf.Abs(delta) < 0.5f || dt <= 0f)
            {
                return Mathf.Abs(delta) < 0.5f ? objetivo : actual;
            }
            float paso = delta * (1f - Mathf.Exp(-suavizado * dt));
            float tope = maximoGradosSeg * dt;
            return actual + Mathf.Clamp(paso, -tope, tope);
        }

        public static float EscalaVelocidad(float velocidadReal, float velocidadClip, float minima, float maxima)
        {
            if (velocidadClip <= 0f)
            {
                return 1f;
            }
            return Mathf.Clamp(velocidadReal / velocidadClip, minima, maxima);
        }

        /// <summary>
        /// Velocidad local normalizada (VelX derecha, VelZ adelante) a partir de la velocidad del mundo y el frente del personaje.
        /// Por debajo de la zona muerta sale (0, 0).
        /// </summary>
        public static Vector2 DireccionLocal(Vector3 velocidadMundo, Vector3 frente, float zonaMuerta = 0.1f)
        {
            Vector3 plano = new Vector3(velocidadMundo.x, 0f, velocidadMundo.z);
            Vector3 f = new Vector3(frente.x, 0f, frente.z);
            if (plano.magnitude <= zonaMuerta || f.sqrMagnitude < 1e-6f)
            {
                return Vector2.zero;
            }
            f.Normalize();
            Vector3 derecha = Vector3.Cross(Vector3.up, f);
            Vector3 dir = plano.normalized;
            return new Vector2(Vector3.Dot(dir, derecha), Vector3.Dot(dir, f));
        }

        /// <summary>
        /// De dónde viene el golpe respecto al frente del personaje. La dirección es la del golpe (del atacante al objetivo): si va contra el frente
        /// del objetivo, le pegaron de frente. Sin dirección cuenta como de frente.
        /// </summary>
        public static bool GolpeDeEspalda(Vector3 direccionGolpe, Vector3 frente)
        {
            Vector3 d = new Vector3(direccionGolpe.x, 0f, direccionGolpe.z);
            Vector3 f = new Vector3(frente.x, 0f, frente.z);
            if (d.sqrMagnitude < 1e-6f || f.sqrMagnitude < 1e-6f)
            {
                return false;
            }
            return Vector3.Dot(d.normalized, f.normalized) > 0.3f;
        }

        public static DanoVisual ElegirDano(Vector3 direccionGolpe, Vector3 frente, bool fuerte)
        {
            if (fuerte)
            {
                return DanoVisual.Fuerte;
            }
            return GolpeDeEspalda(direccionGolpe, frente) ? DanoVisual.Espalda : DanoVisual.Frente;
        }

        /// <summary>T-1515: coseno mínimo (contra el costado del jugador) para que un golpe cuente como de lado: de 53° a 90° de cada costado, el resto es de frente o de espalda.</summary>
        public const float CosenoGolpeLateral = 0.6f;

        /// <summary>
        /// T-1515: de qué costado pega el golpe: -1 izquierda, 1 derecha, 0 si viene de frente, de espalda o sin dirección. La dirección es la del golpe (del atacante al personaje):
        /// una bala que va hacia la derecha del jugador le pega por su izquierda.
        /// </summary>
        public static int LadoDelGolpe(Vector3 direccionGolpe, Vector3 frente)
        {
            Vector3 d = new Vector3(direccionGolpe.x, 0f, direccionGolpe.z);
            Vector3 f = new Vector3(frente.x, 0f, frente.z);
            if (d.sqrMagnitude < 1e-6f || f.sqrMagnitude < 1e-6f)
            {
                return 0;
            }
            float haciaDerecha = Vector3.Dot(d.normalized, Vector3.Cross(Vector3.up, f.normalized));
            if (Mathf.Abs(haciaDerecha) < CosenoGolpeLateral)
            {
                return 0;
            }
            return haciaDerecha > 0f ? -1 : 1;
        }

        /// <summary>T-1515: igual que <see cref="ElegirDano(Vector3, Vector3, bool)"/> pero con <paramref name="lateral"/> los golpes por un costado salen como Izquierda o Derecha.</summary>
        public static DanoVisual ElegirDano(Vector3 direccionGolpe, Vector3 frente, bool fuerte, bool lateral)
        {
            if (fuerte)
            {
                return DanoVisual.Fuerte;
            }
            if (lateral)
            {
                int lado = LadoDelGolpe(direccionGolpe, frente);
                if (lado != 0)
                {
                    return lado < 0 ? DanoVisual.Izquierda : DanoVisual.Derecha;
                }
            }
            return ElegirDano(direccionGolpe, frente, false);
        }

        /// <summary>Muertes válidas según de dónde viene el golpe: de frente cae de espaldas o se desmaya; de espalda cae de frente o se desmaya.</summary>
        public static MuerteVisual[] MuertesValidas(Vector3 direccionGolpe, Vector3 frente)
        {
            bool sinDireccion = new Vector3(direccionGolpe.x, 0f, direccionGolpe.z).sqrMagnitude < 1e-6f;
            if (sinDireccion)
            {
                return new[] { MuerteVisual.Adelante, MuerteVisual.Atras, MuerteVisual.Desmayo };
            }
            return GolpeDeEspalda(direccionGolpe, frente)
                ? new[] { MuerteVisual.Adelante, MuerteVisual.Desmayo }
                : new[] { MuerteVisual.Atras, MuerteVisual.Desmayo };
        }

        /// <summary>Elige entre las muertes válidas con <paramref name="azar"/> (0 a menos de 1), que se inyecta para poder probarlo.</summary>
        public static MuerteVisual ElegirMuerte(Vector3 direccionGolpe, Vector3 frente, float azar)
        {
            MuerteVisual[] validas = MuertesValidas(direccionGolpe, frente);
            int i = Mathf.Clamp((int)(Mathf.Clamp01(azar) * validas.Length), 0, validas.Length - 1);
            return validas[i];
        }

        /// <summary>Cae de espaldas (o se desmaya) se levanta boca arriba; cae de frente, boca abajo.</summary>
        public static bool LevantarseBocaArriba(MuerteVisual muerte) => muerte != MuerteVisual.Adelante && muerte != MuerteVisual.CaidaAltura;

        /// <summary>Giro en el sitio: -1 izquierda, 0 nada, 1 derecha, según la velocidad angular (°/s, positiva a la derecha) y si está quieto.</summary>
        public static int GiroEnSitio(float gradosPorSegundo, float velocidad, float umbral = UmbralGiro, float velocidadQuieto = 0.1f)
        {
            if (velocidad > velocidadQuieto || Mathf.Abs(gradosPorSegundo) < umbral)
            {
                return 0;
            }
            return gradosPorSegundo > 0f ? 1 : -1;
        }

        /// <summary>True cuando deja de correr: venía arriba de <paramref name="desde"/> y ahora está casi quieto.</summary>
        public static bool DebeFrenar(float velocidadAntes, float velocidadAhora, float desde = VelocidadFrenar, float hasta = 1.5f)
        {
            return velocidadAntes >= desde && velocidadAhora <= hasta;
        }

        /// <summary>Tipo de arma del Animator según el arma del jugador: puños 0, pistola 1, escopeta/fusil/sniper 2.</summary>
        public static TipoArmaVisual TipoPorArma(bool desarmado, bool esPistola)
        {
            if (desarmado)
            {
                return TipoArmaVisual.Ninguna;
            }
            return esPistola ? TipoArmaVisual.Pistola : TipoArmaVisual.ArmaLarga;
        }
    
        // ---------- Variación de NPC (T-562) ----------

        /// <summary>Rapidez del idle más lenta y más rápida entre NPC (1 = normal).</summary>
        public const float RapidezIdleMinima = 0.9f;
        public const float RapidezIdleMaxima = 1.1f;

        private static uint Mezclar(int semilla, uint sal)
        {
            unchecked
            {
                uint h = (uint)semilla * 2654435761u + sal;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                h *= 3266489917u;
                h ^= h >> 16;
                return h;
            }
        }

        /// <summary>Desfase de tiempo del idle (0 a menos de 1, fracción del ciclo) para que dos NPC no respiren al unísono. Determinista por semilla.</summary>
        public static float DesfaseIdle(int semilla) => (Mezclar(semilla, 17u) & 0xFFFFFF) / (float)0x1000000;

        /// <summary>Rapidez del idle de este NPC, entre <see cref="RapidezIdleMinima"/> y <see cref="RapidezIdleMaxima"/>. Determinista por semilla.</summary>
        public static float RapidezIdle(int semilla)
        {
            float u = (Mezclar(semilla, 91u) & 0xFFFFFF) / (float)0x1000000;
            return Mathf.Lerp(RapidezIdleMinima, RapidezIdleMaxima, u);
        }

        /// <summary>Temporizador de mirar al jugador: suma mientras está en rango y se reinicia al salir.</summary>
        public static float AvanzarMirada(float tiempoEnRango, bool enRango, float dt) => enRango ? tiempoEnRango + dt : 0f;

        /// <summary>True cuando ya pasó el retardo y la cabeza debe voltear.</summary>
        public static bool MiradaLista(float tiempoEnRango, float retardo = RetardoMirada) => tiempoEnRango >= retardo;

        /// <summary>
        /// El jugador despierta: el visual sigue tumbado hasta que pasa el retardo desde que dejó de estar en KO, y entonces se levanta.
        /// </summary>
        public static bool DebeLevantarse(bool enKO, float segundosSinKO, float retardo = RetardoLevantarse) => !enKO && segundosSinKO >= retardo;
    }
}
