using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// Pose de conductor de moto sin clip (T-562): el cuerpo se sienta en el asiento, las manos van al manubrio y los pies a los estribos por IK,
    /// y se inclina al girar. Subir y bajar se mezclan en un fundido corto (peso del IK y posición). Va en el mismo objeto que el Animator
    /// (con IK Pass en la capa base), bajo VisualPersonaje. No maneja la moto: quien la maneje llama a <see cref="Montar"/>,
    /// <see cref="Bajar"/> y alimenta <see cref="Giro"/> y <see cref="Velocidad"/>. Al bajar, el visual vuelve a su sitio de a pie.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Animator))]
    public class PoseMoto : MonoBehaviour
    {
        private Animator animador;
        private Transform raizVisual;
        private PuntosMoto moto;
        private float avance;
        private float inclinacion;
        private bool bajando;
        private float alturaCadera;
        private Vector3 localAntes;
        private Quaternion rotacionAntes;
        private bool usaInicioFijo;
        private Vector3 inicioMundo;
        private Quaternion giroInicioMundo;

        /// <summary>Giro normalizado del manubrio (-1 izquierda, 1 derecha).</summary>
        public float Giro { get; set; }

        /// <summary>Velocidad de la moto (m/s).</summary>
        public float Velocidad { get; set; }

        /// <summary>0 = a pie, 1 = sentado del todo.</summary>
        public float Avance => avance;

        /// <summary>Inclinación actual del conductor (°, positiva a la derecha).</summary>
        public float Inclinacion => inclinacion;

        public bool Montado => moto != null;

        public PuntosMoto Moto => moto;

        private void Awake()
        {
            animador = GetComponent<Animator>();
            VisualPersonaje visual = GetComponentInParent<VisualPersonaje>();
            raizVisual = visual != null ? visual.transform : transform;
        }

        /// <summary>
        /// Sube a la moto. Devuelve false si a la moto le faltan puntos. Con <paramref name="desdeMundo"/> el fundido de subir arranca en ese punto del mundo
        /// (donde estaba parado el jugador) aunque la raíz del jugador ya haya saltado a la moto (S1, T-660).
        /// </summary>
        public bool Montar(PuntosMoto destino, Vector3? desdeMundo = null)
        {
            if (destino == null || !destino.Completo)
            {
                return false;
            }
            if (moto != null)
            {
                Terminar();
            }
            moto = destino;
            bajando = false;
            avance = 0f;
            Transform huesoCadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            alturaCadera = huesoCadera != null ? Mathf.Max(0f, (huesoCadera.position - raizVisual.position).y) : 0f;
            localAntes = raizVisual.localPosition;
            rotacionAntes = raizVisual.localRotation;
            usaInicioFijo = desdeMundo.HasValue;
            inicioMundo = desdeMundo ?? raizVisual.position;
            giroInicioMundo = raizVisual.rotation;
            if (moto.Caballete != null)
            {
                moto.Caballete.SetActive(false);
            }
            return true;
        }

        /// <summary>Baja de la moto con el mismo fundido corto; el caballete vuelve al terminar.</summary>
        public void Bajar()
        {
            if (moto != null)
            {
                bajando = true;
            }
        }

        private void LateUpdate()
        {
            if (moto == null)
            {
                return;
            }
            avance = ReglasMoto.Avance(avance, !bajando, Time.deltaTime);
            float t = ReglasMoto.Suave(avance);
            inclinacion = Mathf.Lerp(inclinacion, ReglasMoto.Inclinacion(Giro, Velocidad), 1f - Mathf.Exp(-8f * Time.deltaTime));
            Transform asiento = moto.Asiento;
            Quaternion rotMoto = asiento.rotation * Quaternion.Euler(moto.InclinacionTorso, 0f, -inclinacion);
            Transform padre = raizVisual.parent;
            Vector3 mundoAntes = padre != null ? padre.TransformPoint(localAntes) : localAntes;
            Quaternion giroAntes = padre != null ? padre.rotation * rotacionAntes : rotacionAntes;
            if (usaInicioFijo && !bajando)
            {
                mundoAntes = inicioMundo;
                giroAntes = giroInicioMundo;
            }
            raizVisual.SetPositionAndRotation(Vector3.Lerp(mundoAntes, asiento.position - rotMoto * (Vector3.up * alturaCadera), t), Quaternion.Slerp(giroAntes, rotMoto, t));
            if (bajando && avance <= 0f)
            {
                Terminar();
            }
        }

        private void Terminar()
        {
            if (moto != null && moto.Caballete != null)
            {
                moto.Caballete.SetActive(true);
            }
            raizVisual.localPosition = localAntes;
            raizVisual.localRotation = rotacionAntes;
            moto = null;
            bajando = false;
            avance = 0f;
        }

        private void OnAnimatorIK(int capa)
        {
            if (animador == null || capa != 0 || moto == null)
            {
                return;
            }
            float peso = ReglasMoto.Suave(avance);
            Poner(AvatarIKGoal.LeftHand, moto.ManubrioIzquierdo, peso);
            Poner(AvatarIKGoal.RightHand, moto.ManubrioDerecho, peso);
            Poner(AvatarIKGoal.LeftFoot, moto.EstriboIzquierdo, peso);
            Poner(AvatarIKGoal.RightFoot, moto.EstriboDerecho, peso);
        }

        private void Poner(AvatarIKGoal meta, Transform punto, float peso)
        {
            if (punto == null || peso <= 0.001f)
            {
                return;
            }
            animador.SetIKPositionWeight(meta, peso);
            animador.SetIKRotationWeight(meta, peso * 0.5f);
            animador.SetIKPosition(meta, punto.position);
            animador.SetIKRotation(meta, punto.rotation);
        }

        private void OnDisable()
        {
            if (moto != null)
            {
                Terminar();
            }
        }
    }
}
