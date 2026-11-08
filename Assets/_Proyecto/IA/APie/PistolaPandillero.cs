using System;
using MechaCyberpunk.Combate.Efectos;
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
        // T-676: sin rayos láser; el aviso es un hilo finísimo. T-745: el trazo de la bala lo dibuja TrazadorBala.
        private const float Grosor = 0.006f;
        private static readonly Color ColorAviso = new Color(1f, 0.2f, 0.15f, 0.35f);

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
            laser.startWidth = Grosor;
            laser.endWidth = Grosor;
            laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            laser.receiveShadows = false;
            if (materialLaser != null)
            {
                laser.sharedMaterial = materialLaser;
            }
            else
            {
                Shader sprite = Shader.Find("Sprites/Default");
                if (sprite != null)
                {
                    laser.sharedMaterial = new Material(sprite);
                }
            }
            PonerColor(ColorAviso);
            laser.enabled = false;
        }

        private void PonerColor(Color color)
        {
            laser.startColor = color;
            laser.endColor = color;
            laser.startWidth = Grosor;
            laser.endWidth = Grosor;
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

        /// <summary>Fracción final del aviso en la que se ve el destello del hilo (T-676: un destello breve justo antes del tiro, no una línea que se queda).</summary>
        public const float ProgresoDestelloAviso = 0.8f;

        /// <summary>
        /// Aviso de disparo: suena al empezar y, solo en el último tramo (<paramref name="progreso"/> de 0 a 1 desde <see cref="ProgresoDestelloAviso"/>),
        /// muestra un hilo finísimo de la boca al objetivo.
        /// </summary>
        public void Apuntar(Vector3 origen, Vector3 objetivo, float progreso = 1f)
        {
            AsegurarLaser();
            if (!avisando)
            {
                Sonidos.ReproducirEn("ARM_LASER_AVISO", origen);
            }
            avisando = true;
            if (progreso < ProgresoDestelloAviso)
            {
                laser.enabled = Time.time < finTrazo;
                return;
            }
            PonerColor(ColorAviso);
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
            EventosRuido.PublicarDisparo(origen);
            Vector3 hacia = pecho - origen;
            float distancia = hacia.magnitude;
            Vector3 direccion = distancia > 0.001f ? hacia / distancia : transform.forward;
            Vector3 punto = pecho;
            bool libre = lineaVerificada || HayLinea(origen, direccion, distancia, objetivo, out punto);
            // T-421: si el jugador está agachado tras una cubierta del otro lado, la bala pega en ella.
            if (libre && objetivo is IProtegeDeBalas protegido && protegido.ProtegeDe(origen, out Vector3 puntoCubierta))
            {
                libre = false;
                punto = puntoCubierta;
            }
            bool acierta = libre && Azar() <= probabilidad;
            // T-745: el trazo termina donde pega la bala. Si falla en campo abierto sigue hasta el muro o el alcance máximo.
            if (libre && !acierta)
            {
                TrazadorBala.TrazarDireccion(origen, direccion, transform);
            }
            else
            {
                TrazadorBala.Trazar(origen, punto, true);
            }
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
            // T-676: si la boca quedó dentro o detrás de un muro (el tirador pegado a él), el rayo que sale de ahí no lo ve: se revisa también del cuerpo a la boca.
            Vector3 cuerpo = new Vector3(transform.position.x, origen.y, transform.position.z);
            Vector3 haciaBoca = origen - cuerpo;
            if (haciaBoca.sqrMagnitude > 0.0001f)
            {
                int n = Physics.RaycastNonAlloc(cuerpo, haciaBoca.normalized, impactos, haciaBoca.magnitude, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    Collider c = impactos[i].collider;
                    if (c.transform.IsChildOf(transform) || c.GetComponentInParent<PandilleroAPie>() != null)
                    {
                        continue;
                    }
                    punto = impactos[i].point;
                    return false;
                }
            }
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
