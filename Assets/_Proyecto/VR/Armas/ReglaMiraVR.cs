using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR51/T91, lógica pura de la mira del arma en VR (<see cref="MiraArmaVR"/>):
    /// - <see cref="PosicionReticula"/> y <see cref="LadoAngular"/> (T91): la retícula pegada a la cabeza, sobre la línea ojo-punto de impacto.
    /// - <see cref="PuntoColimado"/>: dónde se ve la retícula de una mira holográfica (colimada): la línea que sale del ojo paralela al cañón cruza
    ///   el vidrio ahí. Así la retícula marca la dirección del cañón sin importar dónde esté el ojo.
    /// - Zoom del visor del francotirador con el stick derecho en vertical: arriba acerca (baja el campo de visión), abajo aleja; bajo
    ///   <see cref="ZonaStick"/> no hace nada. Entre <see cref="FovMinimo"/> y <see cref="FovMaximo"/>.
    /// </summary>
    public sealed class ReglaMiraVR
    {
        /// <summary>Campo de visión del visor más cerrado (° , el zoom máximo).</summary>
        public const float FovMinimo = 3f;
        /// <summary>Campo de visión del visor más abierto (°, el zoom mínimo).</summary>
        public const float FovMaximo = 20f;
        /// <summary>Campo de visión del visor al empezar (°).</summary>
        public const float FovInicial = 8f;
        /// <summary>Con el stick a fondo, el campo de visión se multiplica por e^(±esto) cada segundo.</summary>
        public const float RapidezZoom = 1.6f;
        /// <summary>El stick por debajo de esto (0..1) no hace zoom.</summary>
        public const float ZonaStick = 0.25f;

        /// <summary>El campo de visión del visor (°).</summary>
        public float Fov { get; private set; } = FovInicial;

        /// <summary>Un cuadro de zoom con el stick vertical <paramref name="ejeY"/> (-1..1) en <paramref name="dt"/> s. Devuelve el campo de visión.</summary>
        public float AvanzarZoom(float ejeY, float dt)
        {
            if (Mathf.Abs(ejeY) < ZonaStick)
            {
                return Fov;
            }
            Fov = Mathf.Clamp(Fov * Mathf.Exp(-ejeY * RapidezZoom * Mathf.Max(0f, dt)), FovMinimo, FovMaximo);
            return Fov;
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
