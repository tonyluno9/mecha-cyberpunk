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
    }

    /// <summary>Qué reacción de daño se muestra (parámetro DanoTipo).</summary>
    public enum DanoVisual
    {
        Frente = 0,
        Espalda = 1,
        Fuerte = 2,
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
            "AgachadoCaminar", "AgachadoQuieto", "Agacharse", "Pararse", "Rodar", "CoberturaEntrar", "CoberturaMantener", "CoberturaDisparo1", "CoberturaDisparo2",
            "MuerteAdelante", "MuerteAtras", "MuerteDesmayo", "LevantarseBocaArriba", "LevantarseBocaAbajo",
            "CarroEntrar", "CarroManejar", "CarroSalir", "AbrirPuerta", "Boton", "Recoger", "Telefono", "VerTelefono", "Burla", "Presumir",
            "MotoSubir", "MotoManejar", "MotoBajar",
        };

        /// <summary>Estados de la capa aditiva de daño (T-735).</summary>
        public static readonly string[] EstadosDano = { "Vacio", "DanoFrente", "DanoEspalda", "DanoFuerte" };

        /// <summary>Estados de la capa de arriba (torso).</summary>
        public static readonly string[] EstadosArriba =
        {
            "Vacio", "GolpeIzq", "GolpeDer", "GolpeFuerte",
            "PistolaIdle", "PistolaSacar", "PistolaGuardar", "PistolaApuntar", "PistolaAgachado", "PistolaDisparo", "PistolaRecargar", "PistolaRecargarCaminar",
            "LargaIdle", "LargaApuntar", "LargaDisparo", "LargaRecargar", "Pitar",
        };

        public static readonly string[] ParametrosFloat = { ReglasVisual.Velocidad, VelX, VelZ, VelY, Giro, PoseCobertura, RapidezGolpe, ApuntarX, ApuntarZ, ConImplante };
        public static readonly string[] ParametrosBool = { ReglasVisual.EnSuelo, ReglasVisual.Apuntando, ReglasVisual.Caer, ReglasVisual.Cubierto, Agachado, Manejando, ViendoTelefono };
        public static readonly string[] ParametrosInt = { ReglasVisual.TipoArma, DanoTipo, MuerteTipo, GolpeTipo, CoberturaTipo, GestoTipo };
        public static readonly string[] ParametrosTrigger =
        {
            ReglasVisual.Disparar, ReglasVisual.Recargar, ReglasVisual.Golpe, ReglasVisual.Herido, DisparoCobertura, Frenar, Gesto, Rodar,
        };

        /// <summary>
        /// Velocidad del Animator para que los pies no patinen: real / la del clip, limitada. Sin velocidad de clip (0) no se ajusta (1).
        /// </summary>
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
        public static bool LevantarseBocaArriba(MuerteVisual muerte) => muerte != MuerteVisual.Adelante;

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
