using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// Apuntar desde el carro y la moto (T-687). Mientras se mantiene el clic derecho manejando, la cámara deja de seguir al vehículo y
    /// apunta libre con el ratón desde la cabeza del conductor; el jugador gira en el asiento, saca la pistola (en la moto, con una mano,
    /// la otra sigue en el manubrio) y el cañón mira donde mira la cámara. El disparo con el clic izquierdo ya lo hace PistolaAPie hacia
    /// donde mira la cámara. Al soltar el clic, todo vuelve a como estaba (manejar, reversa y subir y bajar no se tocan).
    /// Corre después de la cámara del vehículo y de la pose del conductor para tener la última palabra del cuadro.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class ApuntarDesdeVehiculo : MonoBehaviour
    {
        private const float GradosPorPixel = 0.12f;
        private const float InclinacionMin = -40f;
        private const float InclinacionMax = 55f;
        private const float GiroMaximoCarro = 100f;
        private const float GiroMaximoMoto = 90f;
        private const float AlturaCabezaPorDefecto = 1.15f;
        private const float DistanciaPuntoMira = 40f;
        private const float AlturaSobreCabeza = 0.55f;
        private const float DistanciaDetras = 2.4f;
        private const float RadioCamara = 0.25f;
        private readonly RaycastHit[] impactos = new RaycastHit[8];

        private readonly InputAction apuntar = new InputAction("ApuntarVehiculo", InputActionType.Button, "<Mouse>/rightButton");
        private readonly InputAction mirar = new InputAction("MirarVehiculo", InputActionType.Value, "<Mouse>/delta");

        private JugadorAPie jugador;
        private AnimacionCarro animacion;
        private ArmasAPie armas;
        private VidaAPie vida;
        private VisualPersonaje visual;
        private ApuntadoIK ik;
        private PoseMoto poseMoto;
        private Transform cabeza;
        private bool ikPrevio;
        private bool apuntandoPrevio;
        private TipoArmaVisual tipoPrevio;
        private float yaw;
        private float pitch;

        /// <summary>True mientras se apunta libre desde el vehículo.</summary>
        public bool Apuntando { get; private set; }

        /// <summary>True si el vehículo es una moto (pistola con una mano).</summary>
        public bool EnMoto { get; private set; }

        public float Yaw => yaw;
        public float Inclinacion => pitch;

        /// <summary>Hacia dónde apunta ahora (vector unitario en el mundo).</summary>
        public Vector3 Direccion => Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;

        private void Awake()
        {
            jugador = GetComponent<JugadorAPie>();
            animacion = GetComponent<AnimacionCarro>();
            armas = GetComponent<ArmasAPie>();
            vida = GetComponent<VidaAPie>();
        }

        private void OnEnable()
        {
            apuntar.Enable();
            mirar.Enable();
        }

        private void OnDisable()
        {
            apuntar.Disable();
            mirar.Disable();
            if (Apuntando)
            {
                Terminar();
            }
        }

        private bool Puede()
        {
            if (jugador == null || animacion == null || armas == null || vida == null || Pausa.Activa)
            {
                return false;
            }
            return jugador.Modo == ModoJugador.Carro && jugador.MontableActual != null && jugador.MontableActual.Asiento != null
                && animacion.Fase == AnimacionCarro.FaseCarro.Dentro && jugador.Camara != null && !vida.EstaKO
                && armas.TieneArma(ArmaAPie.Pistola);
        }

        private void LateUpdate()
        {
            bool quiere = Puede() && apuntar.IsPressed();
            if (quiere && !Apuntando)
            {
                Empezar();
            }
            else if (!quiere && Apuntando)
            {
                Terminar();
            }
            if (Apuntando)
            {
                Aplicar();
            }
        }

        private void Empezar()
        {
            visual = GetComponentInChildren<VisualPersonaje>(true);
            Animator animador = visual != null ? visual.Animador : null;
            ik = animador != null ? animador.GetComponent<ApuntadoIK>() : null;
            poseMoto = animador != null ? animador.GetComponent<PoseMoto>() : null;
            cabeza = animador != null && animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.Head) : null;
            EnMoto = poseMoto != null && poseMoto.Montado;
            // Arranca mirando hacia donde mira la cámara del vehículo (sin saltos), a la altura del horizonte.
            yaw = jugador.Camara.transform.eulerAngles.y;
            pitch = 0f;
            if (visual != null)
            {
                apuntandoPrevio = visual.Apuntando;
                tipoPrevio = visual.TipoArma;
                visual.TipoArma = TipoArmaVisual.Pistola;
                visual.Apuntando = true;
            }
            if (ik != null)
            {
                ikPrevio = ik.enabled;
                ik.enabled = true;
            }
            if (EnMoto)
            {
                poseMoto.ManoDerechaLibre = true;
            }
            Apuntando = true;
        }

        private void Terminar()
        {
            Apuntando = false;
            if (visual != null)
            {
                visual.Apuntando = apuntandoPrevio;
                visual.TipoArma = tipoPrevio;
            }
            if (ik != null)
            {
                ik.QuitarPuntoMira();
                ik.enabled = ikPrevio;
            }
            if (poseMoto != null)
            {
                poseMoto.ManoDerechaLibre = false;
            }
        }

        private void Aplicar()
        {
            Vector2 delta = mirar.ReadValue<Vector2>();
            float sensibilidad = Registro<IOpciones>.Actual != null ? Registro<IOpciones>.Actual.Sensibilidad : 1f;
            bool invertir = Registro<IOpciones>.Actual != null && Registro<IOpciones>.Actual.InvertirY;
            yaw = Mathf.Repeat(yaw + delta.x * GradosPorPixel * sensibilidad, 360f);
            pitch = Mathf.Clamp(pitch + (invertir ? delta.y : -delta.y) * GradosPorPixel * sensibilidad, InclinacionMin, InclinacionMax);

            Transform asiento = jugador.MontableActual.Asiento;
            Transform raiz = visual != null ? visual.transform : null;
            if (raiz != null)
            {
                // El conductor gira en el asiento hacia donde apunta (el resto lo completa el IK del brazo y el torso).
                float limite = EnMoto ? GiroMaximoMoto : GiroMaximoCarro;
                float relativo = Mathf.Clamp(Mathf.DeltaAngle(asiento.eulerAngles.y, yaw), -limite, limite);
                Quaternion giro = Quaternion.AngleAxis(relativo, Vector3.up);
                raiz.SetPositionAndRotation(asiento.position + giro * (raiz.position - asiento.position), giro * raiz.rotation);
            }

            Vector3 posicionCabeza = cabeza != null ? cabeza.position : asiento.position + Vector3.up * AlturaCabezaPorDefecto;
            Transform camara = jugador.Camara.transform;
            Quaternion giroCamara = Quaternion.Euler(pitch, yaw, 0f);
            if (EnMoto)
            {
                // Moto: sin carrocería que estorbe, la cámara va en la cabeza del conductor.
                camara.SetPositionAndRotation(posicionCabeza + Vector3.up * 0.05f, giroCamara);
            }
            else
            {
                // Carro: la carrocería taparía todo desde dentro; la cámara se pone detrás del hombro, sobre el techo, mirando hacia donde se apunta.
                camara.SetPositionAndRotation(CamaraDetras(posicionCabeza + Vector3.up * AlturaSobreCabeza, giroCamara, asiento.root), giroCamara);
            }
            if (ik != null)
            {
                ik.PonerPuntoMira(camara.position + camara.forward * DistanciaPuntoMira);
            }
        }

        // Posición de la cámara a DistanciaDetras del foco, en sentido contrario a donde mira; los muros (no el vehículo ni el jugador) la acercan.
        private Vector3 CamaraDetras(Vector3 foco, Quaternion giro, Transform raizVehiculo)
        {
            Vector3 atras = giro * Vector3.back;
            int cantidad = Physics.SphereCastNonAlloc(foco, RadioCamara, atras, impactos, DistanciaDetras, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float libre = DistanciaDetras;
            for (int i = 0; i < cantidad; i++)
            {
                Transform t = impactos[i].collider.transform;
                if (impactos[i].distance <= 0f || t.IsChildOf(raizVehiculo) || t.IsChildOf(transform))
                {
                    continue;
                }
                libre = Mathf.Min(libre, impactos[i].distance);
            }
            return foco + atras * Mathf.Max(0.3f, libre);
        }

        // Mira (cruz con punto) en el centro de la pantalla mientras se apunta: el HUD a pie no se ve en el vehículo.
        private void OnGUI()
        {
            if (!Apuntando || Event.current.type != EventType.Repaint)
            {
                return;
            }
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            Color antes = GUI.color;
            GUI.color = new Color(0.2f, 1f, 0.9f, 0.9f);
            GUI.DrawTexture(new Rect(cx - 2f, cy - 2f, 4f, 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy - 19f, 2f, 10f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1f, cy + 9f, 2f, 10f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 19f, cy - 1f, 10f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + 9f, cy - 1f, 10f, 2f), Texture2D.whiteTexture);
            GUI.color = antes;
        }
    }
}
