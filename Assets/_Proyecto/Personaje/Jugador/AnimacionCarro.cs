using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// T-622: el modelo del jugador sube, maneja y baja de un carro con los clips de Mixamo. Al subir arranca junto a la puerta (donde estaba el jugador)
    /// con Carro_Entrar y se desliza al asiento; sentado juega Carro_Manejar (visible por la ventana, sin cabeza para no estorbar la cámara
    /// de primera persona); al bajar juega Carro_Salir hasta la puerta. Con H suena el claxon (Honking Horn en los brazos).
    /// Lo agrega JugadorAPie solo; no maneja el carro ni mueve la raíz del jugador (esa sigue al asiento).
    /// </summary>
    public class AnimacionCarro : MonoBehaviour
    {
        public enum FaseCarro
        {
            Fuera,
            Entrando,
            Dentro,
            Saliendo,
        }

        // Cuánto cae la cadera sentada respecto del punto del asiento del carro (m) y cuánto se echa para atrás.
        private const float CaidaCadera = 0.2f;
        private const float RetrasoCadera = 0.05f;
        private const float InicioDeslizarEntrada = 0.5f;
        private const float FinDeslizarEntrada = 0.9f;
        private const float FinDeslizarSalida = 0.5f;
        private const float SegundosMinimos = 0.3f;

        private readonly InputAction claxon = new InputAction("Claxon", InputActionType.Button, "<Keyboard>/h");
        private VisualPersonaje visual;
        private Animator animador;
        private Transform raizVisual;
        private Transform cabeza;
        private IKPiesYMirada ik;
        private bool ikPrevio;
        private Vector3 posicionOriginal;
        private Quaternion rotacionOriginal;
        private Vector3 inicioLocal;
        private float inicioYaw;
        private Vector3 asientoLocal;
        private Vector3 asientoMundo;
        private float duracion;
        private float transcurrido;

        public FaseCarro Fase { get; private set; }

        /// <summary>True mientras el modelo es visible dentro del carro (entrando, manejando o saliendo).</summary>
        public bool ModeloVisible => Fase != FaseCarro.Fuera;

        /// <summary>Posición local del modelo sentado, respecto de la raíz del jugador (que sigue al asiento).</summary>
        public Vector3 AsientoLocal => asientoLocal;

        private void OnEnable() => claxon.Enable();

        private void OnDisable() => claxon.Disable();

        /// <summary>Sube: el jugador estaba en <paramref name="posicionPrevia"/> mirando a <paramref name="frentePrevio"/>. Devuelve false si no hay modelo animable.</summary>
        public bool Subir(IMontable montable, Vector3 posicionPrevia, Vector3 frentePrevio)
        {
            if (!Preparar() || montable.Asiento == null)
            {
                return false;
            }
            raizVisual.gameObject.SetActive(true);
            posicionOriginal = raizVisual.localPosition;
            rotacionOriginal = raizVisual.localRotation;
            asientoLocal = CalcularAsientoLocal();
            // La raíz del jugador saltará al asiento en LateUpdate: el inicio se mide respecto del asiento.
            inicioLocal = montable.Asiento.InverseTransformPoint(posicionPrevia);
            inicioLocal.y = Mathf.Min(inicioLocal.y, asientoLocal.y);
            Vector3 frente = Vector3.ProjectOnPlane(frentePrevio, Vector3.up);
            inicioYaw = frente.sqrMagnitude > 1e-4f ? Vector3.SignedAngle(montable.Asiento.forward, frente, Vector3.up) : 0f;
            ApagarIK(true);
            duracion = Mathf.Max(SegundosMinimos, LargoClip("Carro_Entrar") / ReglasAnimacion.VelocidadEstado("CarroEntrar"));
            transcurrido = 0f;
            Fase = FaseCarro.Entrando;
            visual.Manejando = true;
            visual.Gestear(GestoVisual.CarroEntrar);
            Colocar(0f);
            return true;
        }

        /// <summary>Baja: llamar justo antes de colocar la raíz del jugador en la salida. Juega Carro_Salir de vuelta a la puerta.</summary>
        public void Bajar()
        {
            if (Fase == FaseCarro.Fuera || raizVisual == null)
            {
                return;
            }
            asientoMundo = raizVisual.position;
            visual.Manejando = false;
            visual.Gestear(GestoVisual.CarroSalir);
            duracion = Mathf.Max(SegundosMinimos, LargoClip("Carro_Salir") / ReglasAnimacion.VelocidadEstado("CarroSalir"));
            transcurrido = 0f;
            Fase = FaseCarro.Saliendo;
            RestaurarCabeza();
            ApagarIK(false);
        }

        /// <summary>Suena el claxon: los brazos tocan el claxon sentado al volante.</summary>
        public void Pitar()
        {
            if (Fase != FaseCarro.Dentro)
            {
                return;
            }
            visual.Pitar();
            Sonidos.ReproducirEn("VEH_CLAXON_VIEJO", transform.position);
        }

        private bool Preparar()
        {
            visual = GetComponentInChildren<VisualPersonaje>(true);
            if (visual == null || visual.Animador == null || visual.Animador.runtimeAnimatorController == null)
            {
                return false;
            }
            animador = visual.Animador;
            raizVisual = visual.transform;
            cabeza = animador.GetBoneTransform(HumanBodyBones.Head);
            ik = animador.GetComponent<IKPiesYMirada>();
            return true;
        }

        private void Update()
        {
            if (Fase == FaseCarro.Fuera || visual == null)
            {
                return;
            }
            if (Fase == FaseCarro.Dentro)
            {
                if (claxon.WasPressedThisFrame())
                {
                    Pitar();
                }
                return;
            }
            transcurrido += Time.deltaTime;
            if (transcurrido >= duracion)
            {
                Terminar();
            }
        }

        private void LateUpdate()
        {
            if (Fase == FaseCarro.Fuera || raizVisual == null)
            {
                return;
            }
            if (Fase == FaseCarro.Entrando)
            {
                Colocar(Mathf.InverseLerp(InicioDeslizarEntrada * duracion, FinDeslizarEntrada * duracion, transcurrido));
            }
            else if (Fase == FaseCarro.Saliendo)
            {
                float f = Mathf.Clamp01(transcurrido / (FinDeslizarSalida * duracion));
                raizVisual.position = Vector3.Lerp(asientoMundo, transform.position, f);
                raizVisual.localRotation = Quaternion.identity;
            }
            else
            {
                raizVisual.localPosition = asientoLocal;
                raizVisual.localRotation = Quaternion.identity;
                if (cabeza != null)
                {
                    cabeza.localScale = Vector3.zero;
                }
            }
        }

        // f = 0 junto a la puerta mirando como estaba el jugador; f = 1 sentado en el asiento mirando al frente del carro.
        private void Colocar(float f)
        {
            f = Mathf.SmoothStep(0f, 1f, f);
            raizVisual.localPosition = Vector3.Lerp(inicioLocal, asientoLocal, f);
            raizVisual.localRotation = Quaternion.Euler(0f, Mathf.LerpAngle(inicioYaw, 0f, f), 0f);
        }

        private void Terminar()
        {
            if (Fase == FaseCarro.Entrando)
            {
                Fase = FaseCarro.Dentro;
                visual.Manejando = true;
                return;
            }
            Fase = FaseCarro.Fuera;
            raizVisual.localPosition = posicionOriginal;
            raizVisual.localRotation = rotacionOriginal;
            RestaurarCabeza();
            ApagarIK(false);
        }

        // La cadera sentada (Carro_Manejar) cae donde dice el asiento: se mide con la pose del clip sobre el propio modelo.
        private Vector3 CalcularAsientoLocal()
        {
            Vector3 respaldo = raizVisual.localPosition;
            Quaternion giro = raizVisual.localRotation;
            raizVisual.localPosition = Vector3.zero;
            raizVisual.localRotation = Quaternion.identity;
            AnimationClip manejar = BuscarClip("Carro_Manejar_Bucle");
            Vector3 cadera = Vector3.zero;
            Transform huesoCadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            if (manejar != null && huesoCadera != null)
            {
                manejar.SampleAnimation(animador.gameObject, 0f);
                cadera = transform.InverseTransformPoint(huesoCadera.position);
            }
            raizVisual.localPosition = respaldo;
            raizVisual.localRotation = giro;
            return new Vector3(-cadera.x, -cadera.y - CaidaCadera, -cadera.z - RetrasoCadera);
        }

        private AnimationClip BuscarClip(string nombre)
        {
            foreach (AnimationClip c in animador.runtimeAnimatorController.animationClips)
            {
                if (c != null && c.name == nombre)
                {
                    return c;
                }
            }
            return null;
        }

        private float LargoClip(string nombre)
        {
            AnimationClip c = BuscarClip(nombre);
            return c != null ? c.length : 0f;
        }

        private void RestaurarCabeza()
        {
            if (cabeza != null)
            {
                cabeza.localScale = Vector3.one;
            }
        }

        private void ApagarIK(bool apagar)
        {
            if (ik == null)
            {
                return;
            }
            if (apagar)
            {
                ikPrevio = ik.enabled;
                ik.enabled = false;
            }
            else
            {
                ik.enabled = ikPrevio || ik.enabled;
            }
        }
    }
}
