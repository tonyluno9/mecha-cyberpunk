using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.VR.Menus;
using UnityEngine;

namespace MechaCyberpunk.VR.Guia
{
    /// <summary>
    /// VR20: gestos de las prótesis. Con la prótesis instalada, las dos manos sobre los muslos con los dos grips ~0.5 s activan PIERNAS;
    /// los dos brazos abiertos a los lados con los dos grips ~0.5 s activan BRAZOS (<see cref="ReglaGestosProtesisVR"/>). Llama a la misma
    /// API que la rueda de prótesis (<see cref="HabilidadesProtesis.Pulsar"/>: mismo efecto, sonido de servos y recarga); al activarse vibran
    /// los dos controles. Sin la prótesis la pose no cuenta (ni suena error). Con pausa, menú o rueda abiertos no hace nada.
    /// <see cref="EnPose"/> dice si las dos manos están en la pose de una prótesis instalada, para que el agarre (VR/Agarre) no tome a nadie
    /// con esos grips. Se agrega solo al jugador cuando el rig VR está armado (<see cref="AdaptadorVR.Listo"/>).
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
        /// True mientras las dos manos están en la pose de una prótesis instalada (aunque aún no aprieten los grips). El agarre con implante
        /// debería no agarrar al apretar el grip si esto es true (corre antes que AgarreImplanteVR: orden 140 contra 150).
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
            EnPose = pose != GestoProtesisVR.Ninguno;
            bool dosGrips = EntradaVR.Sostenido(EntradaVR.GripIzquierdo) && EntradaVR.Sostenido(EntradaVR.GripDerecho);
            GestoProtesisVR disparo = detector.Avanzar(pose, dosGrips, Time.deltaTime);
            if (disparo != GestoProtesisVR.Ninguno)
            {
                Activar(disparo);
            }
        }

        private bool Tiene(GestoProtesisVR gesto) => habilidades != null && habilidades.Nivel(Habilidad(gesto)) > 0;

        /// <summary>La habilidad de prótesis de cada gesto.</summary>
        public static HabilidadProtesis Habilidad(GestoProtesisVR gesto) =>
            gesto == GestoProtesisVR.Brazos ? HabilidadProtesis.Brazos : HabilidadProtesis.Piernas;

        private void Activar(GestoProtesisVR gesto)
        {
            UltimoGesto = gesto;
            // La misma API que la rueda de prótesis (RuedaHabilidadesExo): mismo efecto, sonido y recarga.
            if (habilidades == null || !habilidades.Pulsar(Habilidad(gesto)))
            {
                return; // recargando o ya activa: Pulsar ya sonó el error
            }
            Activaciones++;
            adaptador.ManoIzquierda.Vibrar(AmplitudActivar, SegundosVibrar);
            adaptador.ManoDerecha.Vibrar(AmplitudActivar, SegundosVibrar);
            Debug.Log("[VR] Gesto de prótesis: " + gesto + " activada.");
        }
    }
}
