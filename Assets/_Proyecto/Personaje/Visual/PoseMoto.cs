using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// Conductor de moto (T-562; T-963 con los clips de Blender). Si el Animator tiene PJ01_Moto_Subir, PJ01_Moto_Manejar y PJ01_Moto_Bajar, subir y bajar juegan esos clips
    /// (estados MotoSubir, MotoManejar y MotoBajar) y la raíz del visual sigue el camino que la cadera hace en el clip, con la moto como referencia (como en Blender, donde el
    /// personaje tiene su origen en el de la moto); sentado, la cadera del clip queda en el asiento. Las manos van al manubrio y los pies a los estribos por IK, que entra al final
    /// de subir y sale al empezar a bajar, y el conductor se inclina al girar. Sin esos clips queda la pose de antes (fundido corto y todo por IK).
    /// Va en el mismo objeto que el Animator (con IK Pass en la capa base), bajo VisualPersonaje. No maneja la moto: quien la maneje llama a <see cref="Montar"/>,
    /// <see cref="Bajar"/> y alimenta <see cref="Giro"/> y <see cref="Velocidad"/>. Al bajar, el visual vuelve a su sitio de a pie.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Animator))]
    public class PoseMoto : MonoBehaviour
    {
        public const string ClipSubir = "PJ01_Moto_Subir";
        public const string ClipManejar = "PJ01_Moto_Manejar";
        public const string ClipBajar = "PJ01_Moto_Bajar";
        private const int Muestras = 24;
        /// <summary>Parte de subir (y de bajar, al final) en la que el cuerpo pasa de donde estaba parado al camino del clip.</summary>
        private const float FraccionMezclaPie = 0.25f;
        /// <summary>Desde qué parte de subir entra el IK de manos y pies (antes el clip va solo), y hasta qué parte de bajar sigue.</summary>
        private const float InicioIKSubir = 0.6f;
        private const float FinIKBajar = 0.4f;
        /// <summary>T-963: la InclinacionTorso de la C19, la moto con la que se hicieron los clips en Blender (°).</summary>
        public const float InclinacionTorsoClips = 22f;
        /// <summary>T-963: grados que se endereza el cuerpo por cada grado de InclinacionTorso menos que la C19 (medido en la V07: con 1 la cabeza quedaba bajo el manubrio; con 2 queda 2.5 cm encima y más no la sube: gira en la cadera).</summary>
        private const float EnderezarPorGrado = 2f;

        private Animator animador;
        private VisualPersonaje visual;
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
        private bool conClips;
        private float tiempo;
        private float segundosSubir = ReglasMoto.SegundosSubir;
        private float segundosBajar = ReglasMoto.SegundosSubir;
        private Vector3 caderaSentado;
        private Vector3[] caminoSubir;
        private Vector3[] caminoBajar;

        /// <summary>T-687: la mano derecha suelta el manubrio (saca la pistola); la izquierda sigue en el manubrio.</summary>
        public bool ManoDerechaLibre { get; set; }

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

        /// <summary>T-963: true si sube, maneja y baja con los clips de Blender.</summary>
        public bool ConClips => conClips;

        /// <summary>T-963: dónde queda la cadera sentada según el clip, en el espacio del visual (m).</summary>
        public Vector3 CaderaSentado => caderaSentado;

        /// <summary>Lo que tarda en subir (s): el clip de subir, o el fundido corto sin clips.</summary>
        public float SegundosSubir => segundosSubir;

        /// <summary>Lo que tarda en bajar (s).</summary>
        public float SegundosBajar => segundosBajar;

        /// <summary>
        /// T-976: dónde queda parado el cuerpo al terminar el clip de bajar (en el mundo, a la altura del asiento sentado; quien lo use pone la altura del piso) y mirando
        /// hacia donde apunta la moto. False si no va montado o no usa los clips.
        /// </summary>
        public bool PoseFinalBajar(out Vector3 posicion, out Quaternion giro)
        {
            posicion = Vector3.zero;
            giro = Quaternion.identity;
            if (moto == null || !conClips || caminoBajar == null)
            {
                return false;
            }
            Transform asiento = moto.Asiento;
            Vector3 raizSentado = asiento.position - asiento.rotation * caderaSentado;
            posicion = raizSentado + asiento.rotation * (caminoBajar[caminoBajar.Length - 1] - caminoBajar[0]);
            Vector3 frente = Vector3.ProjectOnPlane(asiento.forward, Vector3.up);
            giro = frente.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(frente, Vector3.up) : Quaternion.identity;
            return true;
        }

        private void Awake()
        {
            animador = GetComponent<Animator>();
            visual = GetComponentInParent<VisualPersonaje>();
            raizVisual = visual != null ? visual.transform : transform;
        }

        /// <summary>
        /// Sube a la moto. Devuelve false si a la moto le faltan puntos. Con <paramref name="desdeMundo"/> el cuerpo arranca en ese punto del mundo
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
            tiempo = 0f;
            Transform huesoCadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            alturaCadera = huesoCadera != null ? Mathf.Max(0f, (huesoCadera.position - raizVisual.position).y) : 0f;
            localAntes = raizVisual.localPosition;
            rotacionAntes = raizVisual.localRotation;
            usaInicioFijo = desdeMundo.HasValue;
            inicioMundo = desdeMundo ?? raizVisual.position;
            giroInicioMundo = raizVisual.rotation;
            PrepararClips();
            if (moto.Caballete != null)
            {
                moto.Caballete.SetActive(false);
            }
            if (conClips && visual != null)
            {
                visual.Manejando = true;
                visual.Gestear(GestoVisual.MotoSubir);
            }
            return true;
        }

        /// <summary>Baja de la moto (con su clip, o con el fundido corto); el caballete vuelve al terminar.</summary>
        public void Bajar()
        {
            if (moto == null || bajando)
            {
                return;
            }
            bajando = true;
            tiempo = 0f;
            if (conClips && visual != null)
            {
                visual.Manejando = false;
                visual.Gestear(GestoVisual.MotoBajar);
            }
        }

        // ---------- Clips ----------

        private void PrepararClips()
        {
            AnimationClip subir = Buscar(ClipSubir);
            AnimationClip manejar = Buscar(ClipManejar);
            AnimationClip bajar = Buscar(ClipBajar);
            conClips = subir != null && manejar != null && bajar != null && animador.GetBoneTransform(HumanBodyBones.Hips) != null;
            if (!conClips)
            {
                segundosSubir = ReglasMoto.SegundosSubir;
                segundosBajar = ReglasMoto.SegundosSubir;
                return;
            }
            segundosSubir = subir.length / ReglasAnimacion.VelocidadEstado("MotoSubir");
            segundosBajar = bajar.length / ReglasAnimacion.VelocidadEstado("MotoBajar");
            caderaSentado = MedirConAnimador("MotoManejar", 0f, 1)[0];
            // Los caminos son solo el avance de la raíz: la pose en el sitio ya la pone el Animator.
            caminoSubir = MedirConAnimador("MotoSubir", segundosSubir, Muestras, true);
            caminoBajar = MedirConAnimador("MotoBajar", segundosBajar, Muestras, true);
        }

        /// <summary>
        /// Mide con el propio Animator (Play y Update, igual que en el juego) un estado de la capa base en <paramref name="n"/> tiempos iguales a lo largo de
        /// <paramref name="segundos"/>: la cadera en el espacio del visual o, con <paramref name="soloAvance"/>, lo que avanza la raíz del clip (deltaPosition acumulado, en el
        /// plano). Con SampleAnimation no sirve: el clip se centra en su centro de masa y la cadera sale hasta 35 cm corrida. Quien llama vuelve a lanzar el estado que toca.
        /// </summary>
        private Vector3[] MedirConAnimador(string estado, float segundos, int n, bool soloAvance = false)
        {
            Transform cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            var puntos = new Vector3[n];
            Vector3 avance = Vector3.zero;
            animador.Play(estado, 0, 0f);
            animador.Update(0f);
            for (int i = 0; i < n; i++)
            {
                if (i > 0)
                {
                    animador.Update(segundos / (n - 1));
                    Vector3 paso = raizVisual.InverseTransformVector(animador.deltaPosition);
                    avance += new Vector3(paso.x, 0f, paso.z);
                }
                puntos[i] = soloAvance ? avance : raizVisual.InverseTransformPoint(cadera.position);
            }
            return puntos;
        }

        private AnimationClip Buscar(string nombre)
        {
            if (animador.runtimeAnimatorController == null)
            {
                return null;
            }
            foreach (AnimationClip c in animador.runtimeAnimatorController.animationClips)
            {
                if (c != null && c.name == nombre)
                {
                    return c;
                }
            }
            return null;
        }

        private static Vector3 En(Vector3[] camino, float fraccion)
        {
            float x = Mathf.Clamp01(fraccion) * (camino.Length - 1);
            int i = Mathf.Min((int)x, camino.Length - 2);
            return Vector3.Lerp(camino[i], camino[i + 1], x - i);
        }

        // ---------- Pose ----------

        private void LateUpdate()
        {
            if (moto == null)
            {
                return;
            }
            tiempo += Time.deltaTime;
            float fase = Mathf.Clamp01(tiempo / Mathf.Max(0.01f, bajando ? segundosBajar : segundosSubir));
            avance = conClips ? (bajando ? 1f - fase : fase) : ReglasMoto.Avance(avance, !bajando, Time.deltaTime);
            inclinacion = Mathf.Lerp(inclinacion, ReglasMoto.Inclinacion(Giro, Velocidad), 1f - Mathf.Exp(-8f * Time.deltaTime));
            Transform asiento = moto.Asiento;
            Transform padre = raizVisual.parent;
            Vector3 mundoAntes = padre != null ? padre.TransformPoint(localAntes) : localAntes;
            Quaternion giroAntes = padre != null ? padre.rotation * rotacionAntes : rotacionAntes;
            if (usaInicioFijo && !bajando)
            {
                mundoAntes = inicioMundo;
                giroAntes = giroInicioMundo;
            }
            if (conClips)
            {
                ColocarConClips(asiento, fase, mundoAntes, giroAntes);
            }
            else
            {
                float t = ReglasMoto.Suave(avance);
                Quaternion rotMoto = asiento.rotation * Quaternion.Euler(moto.InclinacionTorso, 0f, -inclinacion);
                raizVisual.SetPositionAndRotation(Vector3.Lerp(mundoAntes, asiento.position - rotMoto * (Vector3.up * alturaCadera), t), Quaternion.Slerp(giroAntes, rotMoto, t));
            }
            if (bajando && fase >= 1f)
            {
                Terminar();
            }
        }

        // T-963: la raíz sigue el camino de la cadera del clip respecto del asiento (el clip va en el sitio: su avance no mueve el cuerpo), y al principio de subir y al final de
        // bajar se mezcla con donde el jugador está parado.
        private void ColocarConClips(Transform asiento, float fase, Vector3 mundoAntes, Quaternion giroAntes)
        {
            bool sentado = !bajando && fase >= 1f;
            // T-976: al bajar, el enderezado y la inclinación se van con el IK (antes se cortaban en el primer cuadro y la cadera saltaba).
            float pesoInclinacion = bajando ? 1f - ReglasMoto.Suave(fase / FinIKBajar) : ReglasMoto.Suave(Mathf.InverseLerp(InicioIKSubir, 1f, fase));
            // Los clips se hicieron en la C19 (deportiva): en una moto de manubrio alto (reparto) el cuerpo se endereza lo que su InclinacionTorso es menor que la de la C19.
            float cabeceo = (moto.InclinacionTorso - InclinacionTorsoClips) * EnderezarPorGrado * pesoInclinacion;
            Quaternion rotMoto = asiento.rotation * Quaternion.Euler(cabeceo, 0f, -inclinacion * pesoInclinacion);
            Vector3 raizSentado = asiento.position - rotMoto * caderaSentado;
            Vector3 desvio = Vector3.zero;
            if (!sentado)
            {
                desvio = bajando ? En(caminoBajar, fase) - caminoBajar[0] : En(caminoSubir, fase) - caminoSubir[caminoSubir.Length - 1];
            }
            Vector3 objetivo = raizSentado + asiento.rotation * desvio;
            float pie = bajando ? ReglasMoto.Suave(Mathf.InverseLerp(1f - FraccionMezclaPie, 1f, fase)) : 1f - ReglasMoto.Suave(fase / FraccionMezclaPie);
            raizVisual.SetPositionAndRotation(Vector3.Lerp(objetivo, mundoAntes, pie), Quaternion.Slerp(rotMoto, giroAntes, pie));
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
            tiempo = 0f;
        }

        /// <summary>Peso del IK de manos y pies: con clips entra al final de subir y sale al empezar a bajar; sin clips sigue al fundido.</summary>
        private float PesoIK()
        {
            if (!conClips)
            {
                return ReglasMoto.Suave(avance);
            }
            float fase = Mathf.Clamp01(tiempo / Mathf.Max(0.01f, bajando ? segundosBajar : segundosSubir));
            return bajando ? 1f - ReglasMoto.Suave(fase / FinIKBajar) : ReglasMoto.Suave(Mathf.InverseLerp(InicioIKSubir, 1f, fase));
        }

        private void OnAnimatorIK(int capa)
        {
            if (animador == null || capa != 0 || moto == null)
            {
                return;
            }
            float peso = PesoIK();
            Poner(AvatarIKGoal.LeftHand, moto.ManubrioIzquierdo, peso);
            if (!ManoDerechaLibre)
            {
                Poner(AvatarIKGoal.RightHand, moto.ManubrioDerecho, peso);
            }
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
