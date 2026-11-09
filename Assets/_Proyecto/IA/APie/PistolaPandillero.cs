using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// Arma del pandillero armado (T-402): avisa con un sonido y luego dispara un rayo invisible al pecho del jugador. El acierto baja con la
    /// distancia (70 % a 10 m, 30 % a 30 m) y el rayo no atraviesa muros. Hace daño por <see cref="IRecibeDanio"/> con <see cref="TipoDanio.Disparo"/>.
    /// </summary>
    public class PistolaPandillero : MonoBehaviour
    {
        private const int MaxImpactos = 16;

        /// <summary>Fuente del azar (0 a 1). Las pruebas la cambian para que sea determinista.</summary>
        public static Func<float> Azar = () => UnityEngine.Random.value;

        private readonly RaycastHit[] impactos = new RaycastHit[MaxImpactos];
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

        /// <summary>Fracción final del aviso en la que se ve el destello del hilo (T-676: un destello breve justo antes del tiro, no una línea que se queda).</summary>
        public const float ProgresoDestelloAviso = 0.8f;

        /// <summary>Aviso de disparo: suena una vez al empezar a apuntar. T-931: sin hilo ni láser visible.</summary>
        public void Apuntar(Vector3 origen, Vector3 objetivo, float progreso = 1f)
        {
            if (!avisando)
            {
                Sonidos.ReproducirEn("ARM_LASER_AVISO", origen);
            }
            avisando = true;
        }

        /// <summary>Termina el aviso de disparo.</summary>
        public void QuitarAviso()
        {
            avisando = false;
        }

        /// <summary>
        /// Dispara al pecho del jugador: devuelve true si el daño llegó. <paramref name="objetivo"/> es quien recibe el daño.
        /// Los muros bloquean el tiro; los demás pandilleros no.
        /// </summary>
        public bool Disparar(Vector3 origen, Vector3 pecho, IRecibeDanio objetivo, float danio, float probabilidad, bool lineaVerificada = false)
        {
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
