using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR51/T91/T101, lógica pura de la mira del arma en VR (<see cref="MiraArmaVR"/>):
    /// - T101: <see cref="MiraDe"/> (qué armas apuntan con la mira y cuál lleva zoom), <see cref="Avanzar"/> (gatillo izquierdo, zoom del
    ///   francotirador con el stick derecho, fundido de entrada y salida de <see cref="SegundosFundido"/>) y <see cref="BloqueaGiro"/>.
    ///   El zoom es un AUMENTO (2× a 8× el francotirador, <see cref="AumentoFijo"/> las demás) que se aplica solo a la cámara de la mira:
    ///   <see cref="FovCamara"/> da el campo de visión de esa cámara para que el círculo de <see cref="AnguloCirculo"/> grados se vea con ese aumento.
    /// - <see cref="PosicionReticula"/> y <see cref="LadoAngular"/> (T91): el círculo pegado a la cabeza, sobre la línea ojo-punto de impacto.
    /// - <see cref="PuntoColimado"/>: dónde se ve la retícula de una mira holográfica (colimada): la línea que sale del ojo paralela al cañón cruza
    ///   el vidrio ahí. Así la retícula marca la dirección del cañón sin importar dónde esté el ojo.
    /// </summary>
    public sealed class ReglaMiraVR
    {
        /// <summary>T101: lo que abarca de la vista el círculo de la mira (°).</summary>
        public const float AnguloCirculo = 45f;
        /// <summary>T101: aumento fijo de las armas sin zoom variable (subfusil, fusil, escopeta, pistola, revólver).</summary>
        public const float AumentoFijo = 1.5f;
        /// <summary>T101: aumento mínimo del francotirador.</summary>
        public const float AumentoMinimo = 2f;
        /// <summary>T101: aumento máximo del francotirador.</summary>
        public const float AumentoMaximo = 8f;
        /// <summary>T101: aumento del francotirador la primera vez que se apunta (después recuerda el último).</summary>
        public const float AumentoInicial = 3f;
        /// <summary>Con el stick a fondo, el aumento se multiplica por e^(±esto) cada segundo (de 2× a 8× en ~1 s).</summary>
        public const float RapidezZoom = 1.4f;
        /// <summary>El stick por debajo de esto (0..1) no hace zoom.</summary>
        public const float ZonaStick = 0.25f;
        /// <summary>T101: segundos del fundido al apuntar y al soltar.</summary>
        public const float SegundosFundido = 0.1f;

        private float aumentoFrancotirador = AumentoInicial;

        /// <summary>La mira que se está usando (Ninguna si no se aprieta el gatillo izquierdo con un arma que apunta).</summary>
        public MiraArmaVR.TipoMira Activa { get; private set; }

        /// <summary>La mira que se dibuja: la activa o, mientras se desvanece al soltar, la última.</summary>
        public MiraArmaVR.TipoMira Dibujada { get; private set; }

        /// <summary>Opacidad de la mira (0 vista normal, 1 todo negro salvo el círculo).</summary>
        public float Alfa { get; private set; }

        /// <summary>True si la mira se ve aunque sea un poco (la cámara de la mira debe dibujar).</summary>
        public bool Visible => Alfa > 0f;

        /// <summary>Aumento actual de la mira dibujada (×).</summary>
        public float Aumento => Dibujada == MiraArmaVR.TipoMira.Zoom ? aumentoFrancotirador : AumentoFijo;

        /// <summary>Campo de visión (°) de la cámara de la mira para el aumento actual.</summary>
        public float Fov => FovCamara(AnguloCirculo, Aumento);

        /// <summary>True mientras se apunta con el francotirador: el stick derecho es el zoom y no gira ni recentra la vista.</summary>
        public bool BloqueaGiro => Activa == MiraArmaVR.TipoMira.Zoom;

        /// <summary>T101: qué mira lleva cada arma en VR. Lanzagranadas (arco) y plasma (carga) no: los hace <see cref="ArmasEspecialesVR"/>.</summary>
        public static MiraArmaVR.TipoMira MiraDe(ArmaAPie arma)
        {
            switch (arma)
            {
                case ArmaAPie.Pistola:
                case ArmaAPie.Revolver:
                case ArmaAPie.Subfusil:
                case ArmaAPie.Fusil:
                case ArmaAPie.Escopeta:
                    return MiraArmaVR.TipoMira.Fija;
                case ArmaAPie.Sniper:
                    return MiraArmaVR.TipoMira.Zoom;
                default:
                    return MiraArmaVR.TipoMira.Ninguna;
            }
        }

        /// <summary>
        /// Un cuadro de <paramref name="dt"/> s. <paramref name="tipo"/> es la mira del arma en la mano (Ninguna si no apunta o no se puede apuntar),
        /// <paramref name="gatillo"/> si el gatillo izquierdo está apretado y <paramref name="ejeY"/> el stick derecho vertical (-1..1, adelante +):
        /// con el francotirador, adelante acerca y atrás aleja.
        /// </summary>
        public void Avanzar(MiraArmaVR.TipoMira tipo, bool gatillo, float ejeY, float dt)
        {
            dt = Mathf.Max(0f, dt);
            Activa = gatillo ? tipo : MiraArmaVR.TipoMira.Ninguna;
            if (Activa != MiraArmaVR.TipoMira.Ninguna)
            {
                Dibujada = Activa;
            }
            if (Activa == MiraArmaVR.TipoMira.Zoom)
            {
                AvanzarZoom(ejeY, dt);
            }
            float objetivo = Activa != MiraArmaVR.TipoMira.Ninguna ? 1f : 0f;
            Alfa = Mathf.MoveTowards(Alfa, objetivo, SegundosFundido > 0f ? dt / SegundosFundido : 1f);
            if (Alfa <= 0f)
            {
                Dibujada = MiraArmaVR.TipoMira.Ninguna;
            }
        }

        /// <summary>Quita la mira de golpe (menú, pausa, sin arma).</summary>
        public void Cortar()
        {
            Activa = MiraArmaVR.TipoMira.Ninguna;
            Dibujada = MiraArmaVR.TipoMira.Ninguna;
            Alfa = 0f;
        }

        /// <summary>Un cuadro de zoom del francotirador con el stick vertical <paramref name="ejeY"/> (-1..1). Devuelve el aumento.</summary>
        public float AvanzarZoom(float ejeY, float dt)
        {
            if (Mathf.Abs(ejeY) < ZonaStick)
            {
                return aumentoFrancotirador;
            }
            aumentoFrancotirador = Mathf.Clamp(aumentoFrancotirador * Mathf.Exp(ejeY * RapidezZoom * Mathf.Max(0f, dt)), AumentoMinimo, AumentoMaximo);
            return aumentoFrancotirador;
        }

        /// <summary>Aumento guardado del francotirador (×).</summary>
        public float AumentoFrancotirador => aumentoFrancotirador;

        /// <summary>
        /// Campo de visión (°) de una cámara cuya imagen llena un círculo de <paramref name="anguloCirculo"/> grados de la vista para que se vea con
        /// <paramref name="aumento"/>: 2·atan(tan(ángulo/2)/aumento). Con aumento 1 es el mismo ángulo (sin zoom).
        /// </summary>
        public static float FovCamara(float anguloCirculo, float aumento)
        {
            float mitad = Mathf.Clamp(anguloCirculo, 0.1f, 179f) * 0.5f * Mathf.Deg2Rad;
            return 2f * Mathf.Atan(Mathf.Tan(mitad) / Mathf.Max(0.01f, aumento)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Dónde cruza el vidrio (plano en <paramref name="centroVidrio"/> con normal <paramref name="normalVidrio"/>) la línea que sale del
        /// <paramref name="ojo"/> en la dirección del <paramref name="canon"/>. False si el vidrio está detrás del ojo o de canto.
        /// </summary>
        public static bool PuntoColimado(Vector3 ojo, Vector3 canon, Vector3 centroVidrio, Vector3 normalVidrio, out Vector3 punto)
        {
            punto = centroVidrio;
            if (canon.sqrMagnitude < 1e-8f || normalVidrio.sqrMagnitude < 1e-8f)
            {
                return false;
            }
            Vector3 d = canon.normalized;
            Vector3 n = normalVidrio.normalized;
            float denominador = Vector3.Dot(d, n);
            if (Mathf.Abs(denominador) < 1e-4f)
            {
                return false;
            }
            float t = Vector3.Dot(centroVidrio - ojo, n) / denominador;
            if (t <= 0f)
            {
                return false;
            }
            punto = ojo + d * t;
            return true;
        }

        /// <summary>True si el punto (en unidades del vidrio, desde su centro) cae dentro de un vidrio redondo de ese radio.</summary>
        public static bool DentroDelVidrio(Vector2 enVidrio, float radio) => enVidrio.sqrMagnitude <= radio * radio;

        /// <summary>
        /// T91: dónde va la retícula pegada a la cabeza. <paramref name="objetivoEnCabeza"/> es el punto donde pega la bala, relativo al ojo y
        /// en los ejes de la cabeza (m, +z adelante). La retícula va sobre la línea ojo-objetivo, a la distancia del objetivo limitada entre
        /// <paramref name="distanciaMinima"/> y <paramref name="distanciaMaxima"/>: así su centro tapa justo el punto de impacto (y, a la
        /// distancia real, los dos ojos la ven en el mismo sitio). False si el objetivo queda detrás de la cabeza o pegado al ojo.
        /// </summary>
        public static bool PosicionReticula(Vector3 objetivoEnCabeza, float distanciaMinima, float distanciaMaxima, out Vector3 posicion)
        {
            posicion = Vector3.forward * distanciaMinima;
            float largo = objetivoEnCabeza.magnitude;
            if (largo < 1e-4f || objetivoEnCabeza.z <= 0f)
            {
                return false;
            }
            posicion = objetivoEnCabeza / largo * Mathf.Clamp(largo, distanciaMinima, Mathf.Max(distanciaMinima, distanciaMaxima));
            return true;
        }

        /// <summary>T91: lado (m) que ocupa <paramref name="grados"/> de la vista a <paramref name="distancia"/> m del ojo.</summary>
        public static float LadoAngular(float distancia, float grados) => 2f * Mathf.Max(0f, distancia) * Mathf.Tan(Mathf.Clamp(grados, 0f, 179f) * 0.5f * Mathf.Deg2Rad);
    }
}
