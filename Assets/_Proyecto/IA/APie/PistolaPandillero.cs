using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// Arma del pandillero armado (T-402): avisa con un láser rojo y luego dispara un rayo al pecho del jugador. El acierto baja con la
    /// distancia (70 % a 10 m, 30 % a 30 m) y el rayo no atraviesa muros. Hace daño por <see cref="IRecibeDanio"/> con <see cref="TipoDanio.Disparo"/>.
    /// </summary>
    public class PistolaPandillero : MonoBehaviour
    {
        private const int MaxImpactos = 16;

        /// <summary>Fuente del azar (0 a 1). Las pruebas la cambian para que sea determinista.</summary>
        public static Func<float> Azar = () => UnityEngine.Random.value;

        [Tooltip("Línea del láser de aviso y del trazo del disparo. Se crea sola si falta.")]
        [SerializeField] private LineRenderer laser;
        [Tooltip("Material del láser (rojo, sin sombras).")]
        [SerializeField] private Material materialLaser;
        [Tooltip("Cuánto dura el trazo después de un disparo (s).")]
        [Min(0.01f)] [SerializeField] private float duracionTrazo = 0.08f;
        [Tooltip("Grosor del láser (m).")]
        [Min(0.005f)] [SerializeField] private float grosor = 0.03f;

        private readonly RaycastHit[] impactos = new RaycastHit[MaxImpactos];
        private float finTrazo = -1f;
        private bool avisando;

        public int Disparos { get; private set; }
        public int Aciertos { get; private set; }

        /// <summary>Probabilidad de acierto a esa distancia: lineal entre los dos puntos de referencia, recortada a ellos.</summary>
        public static float ProbabilidadAcierto(float distancia, float cerca, float lejos, float pCerca, float pLejos)
        {
            if (lejos <= cerca)
            {
                return pCerca;
            }
            float t = Mathf.Clamp01((distancia - cerca) / (lejos - cerca));
            return Mathf.Lerp(pCerca, pLejos, t);
        }

        private void Awake() => AsegurarLaser();

        private void AsegurarLaser()
        {
            if (laser != null)
            {
                return;
            }
            GameObject objeto = new GameObject("Laser");
            objeto.transform.SetParent(transform, false);
            laser = objeto.AddComponent<LineRenderer>();
            laser.positionCount = 2;
            laser.useWorldSpace = true;
            laser.startWidth = grosor;
            laser.endWidth = grosor;
            laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            laser.receiveShadows = false;
            if (materialLaser != null)
            {
                laser.sharedMaterial = materialLaser;
            }
            laser.startColor = Color.red;
            laser.endColor = Color.red;
            laser.enabled = false;
        }

        private void Update()
        {
            if (!avisando && laser != null && laser.enabled && Time.time >= finTrazo)
            {
                laser.enabled = false;
            }
        }

        private void OnDisable()
        {
            avisando = false;
            if (laser != null)
            {
                laser.enabled = false;
            }
        }

        /// <summary>Muestra el láser de aviso desde la boca hasta el objetivo.</summary>
        public void Apuntar(Vector3 origen, Vector3 objetivo)
        {
            AsegurarLaser();
            avisando = true;
            laser.SetPosition(0, origen);
            laser.SetPosition(1, objetivo);
            laser.enabled = true;
        }

        /// <summary>Apaga el láser de aviso (el trazo del disparo sigue su curso).</summary>
        public void QuitarAviso()
        {
            if (!avisando)
            {
                return;
            }
            avisando = false;
            if (laser != null && Time.time >= finTrazo)
            {
                laser.enabled = false;
            }
        }

        /// <summary>
        /// Dispara al pecho del jugador: devuelve true si el daño llegó. <paramref name="objetivo"/> es quien recibe el daño.
        /// Los muros bloquean el tiro; los demás pandilleros no.
        /// </summary>
        public bool Disparar(Vector3 origen, Vector3 pecho, IRecibeDanio objetivo, float danio, float probabilidad, bool lineaVerificada = false)
        {
            AsegurarLaser();
            avisando = false;
            Disparos++;
            Vector3 hacia = pecho - origen;
            float distancia = hacia.magnitude;
            Vector3 direccion = distancia > 0.001f ? hacia / distancia : transform.forward;
            Vector3 punto = pecho;
            bool libre = lineaVerificada || HayLinea(origen, direccion, distancia, objetivo, out punto);
            bool acierta = libre && Azar() <= probabilidad;
            laser.SetPosition(0, origen);
            laser.SetPosition(1, punto);
            laser.enabled = true;
            finTrazo = Time.time + duracionTrazo;
            if (!acierta)
            {
                return false;
            }
            Aciertos++;
            objetivo.RecibirDanio(new InfoDanio(danio, TipoDanio.Disparo, punto, direccion, null));
            return true;
        }

        /// <summary>True si el primer obstáculo hacia el objetivo es el propio jugador (no un muro). <paramref name="punto"/> es donde pega.</summary>
        public bool HayLinea(Vector3 origen, Vector3 direccion, float distancia, IRecibeDanio objetivo, out Vector3 punto)
        {
            punto = origen + direccion * distancia;
            int cantidad = Physics.RaycastNonAlloc(origen, direccion, impactos, distancia + 0.5f, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            bool llegaAlJugador = true;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit impacto = impactos[i];
                if (impacto.distance >= mejor || impacto.collider.GetComponentInParent<PandilleroAPie>() != null)
                {
                    continue;
                }
                mejor = impacto.distance;
                llegaAlJugador = ReferenceEquals(impacto.collider.GetComponentInParent<IRecibeDanio>(), objetivo);
                punto = impacto.point;
            }
            return llegaAlJugador;
        }
    }
}
