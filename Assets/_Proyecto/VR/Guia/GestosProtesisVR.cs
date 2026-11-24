using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.VR.Menus;
using UnityEngine;

namespace MechaCyberpunk.VR.Guia
{
    /// <summary>
    /// VR20/VR45/VR31b: gestos de las prótesis (en VR ya no hay rueda de prótesis: solo gestos). Con la prótesis instalada y los dos grips
    /// apretados ~0.5 s: las dos manos abajo, de la cadera a la rodilla, activan PIERNAS; los brazos abiertos en cruz activan BRAZOS; las dos
    /// manos a los lados de la cabeza activan la VISIÓN (habilidad CABEZA) (<see cref="ReglaGestosProtesisVR"/>). Llama a
    /// <see cref="HabilidadesProtesis.Pulsar"/> (mismo efecto, sonido y recarga que en PC); al activarse vibran los dos controles, y la visión,
    /// que en PC no tiene sonido propio, suena como las otras (servos). Sin la prótesis la pose no cuenta (ni suena error). Con pausa, menú o
    /// guía abiertos no hace nada. <see cref="EnPose"/> dice si las dos manos están en la pose de una prótesis instalada CON los dos grips
    /// apretados, para que el agarre, la recarga y el gesto de armas no usen esos grips (VR45: con la zona de piernas más grande, una mano
    /// colgando sin grip ya no bloquea sacar la pistola). Se agrega solo al jugador cuando el rig VR está armado (<see cref="AdaptadorVR.Listo"/>).
    /// </summary>
    [DefaultExecutionOrder(140)]
    public class GestosProtesisVR : MonoBehaviour
    {
        private const float AmplitudActivar = 0.8f;
        private const float SegundosVibrar = 0.25f;

        private readonly DetectorGestoProtesisVR detector = new DetectorGestoProtesisVR();
        private AdaptadorVR adaptador;
        private HabilidadesProtesis habilidades;

        /// <summary>
        /// True mientras las dos manos están en la pose de una prótesis instalada y los dos grips están apretados. El agarre con implante, la
        /// recarga y el gesto de armas no toman el grip si esto es true (corre antes que AgarreImplanteVR: orden 140 contra 150).
        /// </summary>
        public static bool EnPose { get; private set; }

        /// <summary>La pose de este cuadro, aunque no tenga la prótesis (para depurar).</summary>
        public GestoProtesisVR PoseActual { get; private set; }

        /// <summary>Cuántas veces el gesto activó una prótesis.</summary>
        public int Activaciones { get; private set; }

        /// <summary>La última prótesis que el gesto intentó activar.</summary>
        public GestoProtesisVR UltimoGesto { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            EnPose = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Suscribir()
        {
            AdaptadorVR.Listo -= AlListo;
            AdaptadorVR.Listo += AlListo;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RevisarActual()
        {
            if (AdaptadorVR.Actual != null && AdaptadorVR.Actual.Armado)
            {
                AlListo(AdaptadorVR.Actual);
            }
        }

        private static void AlListo(AdaptadorVR rig)
        {
            if (rig != null && rig.GetComponent<GestosProtesisVR>() == null)
            {
                rig.gameObject.AddComponent<GestosProtesisVR>();
            }
        }

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            habilidades = GetComponent<HabilidadesProtesis>();
            if (adaptador == null)
            {
                Debug.LogError("[VR] GestosProtesisVR sin AdaptadorVR en " + name, this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            EnPose = false;
            detector.Reiniciar();
        }

        private void Update()
        {
            EnPose = false;
            PoseActual = GestoProtesisVR.Ninguno;
            if (!ModoVR.Activo || adaptador == null || !adaptador.Armado || adaptador.Camara == null
                || adaptador.ManoIzquierda == null || adaptador.ManoDerecha == null)
            {
                detector.Reiniciar();
                return;
            }
            if (habilidades == null)
            {
                habilidades = HabilidadesProtesis.Actual;
            }
            Transform cabeza = adaptador.Camara.transform;
            Vector3 izq = ReglaGestosProtesisVR.EnMarcoCabeza(cabeza.position, cabeza.forward, cabeza.up, adaptador.ManoIzquierda.transform.position);
            Vector3 der = ReglaGestosProtesisVR.EnMarcoCabeza(cabeza.position, cabeza.forward, cabeza.up, adaptador.ManoDerecha.transform.position);
            GestoProtesisVR pose = ReglaGestosProtesisVR.Pose(izq, der);
            PoseActual = pose;
            if (pose != GestoProtesisVR.Ninguno && !Tiene(pose))
            {
                pose = GestoProtesisVR.Ninguno; // sin la prótesis la pose no cuenta
            }
            bool bloqueado = Pausa.Activa || MenusVR.MenuAbierto || GuiaControlesVR.Abierta;
            if (bloqueado)
            {
                detector.Reiniciar();
                return;
            }
            bool dosGrips = EntradaVR.Sostenido(EntradaVR.GripIzquierdo) && EntradaVR.Sostenido(EntradaVR.GripDerecho);
            EnPose = pose != GestoProtesisVR.Ninguno && dosGrips;
            GestoProtesisVR disparo = detector.Avanzar(pose, dosGrips, Time.deltaTime);
            if (disparo != GestoProtesisVR.Ninguno)
            {
                Activar(disparo);
            }
        }

        private bool Tiene(GestoProtesisVR gesto) => habilidades != null && habilidades.Nivel(Habilidad(gesto)) > 0;

        /// <summary>La habilidad de prótesis de cada gesto (VR31b: la visión es la habilidad CABEZA).</summary>
        public static HabilidadProtesis Habilidad(GestoProtesisVR gesto)
        {
            switch (gesto)
            {
                case GestoProtesisVR.Brazos: return HabilidadProtesis.Brazos;
                case GestoProtesisVR.Vision: return HabilidadProtesis.Cabeza;
                default: return HabilidadProtesis.Piernas;
            }
        }

        private void Activar(GestoProtesisVR gesto)
        {
            UltimoGesto = gesto;
            HabilidadProtesis habilidad = Habilidad(gesto);
            // La misma API que la tecla de PC: mismo efecto, sonido y recarga.
            if (habilidades == null || !habilidades.Pulsar(habilidad))
            {
                return; // recargando o ya activa: Pulsar ya sonó el error
            }
            if (ReglaSonidoExo.AlActivar(habilidad) == null)
            {
                Sonidos.ReproducirEn(ReglaSonidoExo.Servos, transform.position); // VR31b: la visión suena como las otras
            }
            Activaciones++;
            adaptador.ManoIzquierda.Vibrar(AmplitudActivar, SegundosVibrar);
            adaptador.ManoDerecha.Vibrar(AmplitudActivar, SegundosVibrar);
            Debug.Log("[VR] Gesto de prótesis: " + gesto + " activada.");
        }
    }
}
