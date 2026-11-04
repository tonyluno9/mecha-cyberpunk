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

        public const string CapaBase = "Base";
        public const string CapaArriba = "Arriba";

        /// <summary>Velocidad (m/s) a partir de la cual el controlador base pasa de Caminar a Correr (entre 4.5 y 7.5 del jugador).</summary>
        public const float UmbralCorrer = 6f;
        /// <summary>Caída más rápida que esto (m/s hacia abajo) se muestra como Caida y no como salto.</summary>
        public const float VelocidadCaidaLibre = 7f;
        /// <summary>Giro (°/s) a partir del cual el personaje quieto gira en el sitio.</summary>
        public const float UmbralGiro = 70f;
        /// <summary>Velocidad desde la que soltar el avance se muestra como frenar.</summary>
        public const float VelocidadFrenar = 5f;

        /// <summary>Estados de la capa base del controlador base (el nombre es el del estado).</summary>
        public static readonly string[] EstadosBase =
        {
            "Idle", "Caminar", "Correr", "GirarDer", "GirarIzq", "Frenar", "SaltoInicio", "SaltoAire", "SaltoAterrizaje", "Caida",
            "AgachadoCaminar", "CoberturaEntrar", "CoberturaMantener", "CoberturaDisparo1", "CoberturaDisparo2",
            "MuerteAdelante", "MuerteAtras", "MuerteDesmayo", "LevantarseBocaArriba", "LevantarseBocaAbajo",
        };

        /// <summary>Estados de la capa de arriba (torso).</summary>
        public static readonly string[] EstadosArriba =
        {
            "Vacio", "GolpeIzq", "GolpeDer", "GolpeFuerte", "DanoFrente", "DanoEspalda", "DanoFuerte",
            "PistolaIdle", "PistolaApuntar", "PistolaDisparo", "PistolaRecargar", "PistolaRecargarCaminar",
            "LargaIdle", "LargaApuntar", "LargaDisparo", "LargaRecargar",
        };

        public static readonly string[] ParametrosFloat = { ReglasVisual.Velocidad, VelX, VelZ, VelY, Giro, PoseCobertura };
        public static readonly string[] ParametrosBool = { ReglasVisual.EnSuelo, ReglasVisual.Apuntando, ReglasVisual.Caer, ReglasVisual.Cubierto, Agachado };
        public static readonly string[] ParametrosInt = { ReglasVisual.TipoArma, DanoTipo, MuerteTipo, GolpeTipo, CoberturaTipo };
        public static readonly string[] ParametrosTrigger =
        {
            ReglasVisual.Disparar, ReglasVisual.Recargar, ReglasVisual.Golpe, ReglasVisual.Herido, DisparoCobertura, Frenar,
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
    }
}
