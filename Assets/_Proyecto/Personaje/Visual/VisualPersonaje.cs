using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// Puente entre el juego y el Animator del modelo 3D de un personaje (T-473). Vive en la raíz del prefab Visual_&lt;Nombre&gt;.
    /// Con <see cref="leerDelPadre"/> activo calcula solo Velocidad y EnSuelo (del CharacterController o Rigidbody del padre, o de lo que se
    /// movió el objeto) sin referenciar los componentes del juego. Lo demás lo alimenta el código con los métodos públicos
    /// (Disparar, Recargar, Golpear, Herir, Rodar) y propiedades (Apuntando, TipoArma, Cubierto, Caido).
    /// Si el Animator no tiene un parámetro, lo ignora sin error.
    /// </summary>
    public class VisualPersonaje : MonoBehaviour
    {
        [Tooltip("Animator del modelo. Si está vacío se busca en los hijos.")]
        [SerializeField] private Animator animador;
        [Tooltip("Leer Velocidad y EnSuelo del CharacterController, Rigidbody o movimiento del padre.")]
        [SerializeField] private bool leerDelPadre = true;
        [Tooltip("Suavizado de la velocidad que ve el Animator (1/s). 0 = sin suavizar.")]
        [Min(0f)] [SerializeField] private float suavizadoVelocidad = 12f;

        private static readonly int IdVelocidad = Animator.StringToHash(ReglasVisual.Velocidad);
        private static readonly int IdEnSuelo = Animator.StringToHash(ReglasVisual.EnSuelo);
        private static readonly int IdApuntando = Animator.StringToHash(ReglasVisual.Apuntando);
        private static readonly int IdTipoArma = Animator.StringToHash(ReglasVisual.TipoArma);
        private static readonly int IdDisparar = Animator.StringToHash(ReglasVisual.Disparar);
        private static readonly int IdRecargar = Animator.StringToHash(ReglasVisual.Recargar);
        private static readonly int IdGolpe = Animator.StringToHash(ReglasVisual.Golpe);
        private static readonly int IdHerido = Animator.StringToHash(ReglasVisual.Herido);
        private static readonly int IdCaer = Animator.StringToHash(ReglasVisual.Caer);
        private static readonly int IdCubierto = Animator.StringToHash(ReglasVisual.Cubierto);
        private static readonly int IdRodar = Animator.StringToHash(ReglasVisual.Rodar);

        private CharacterController controlador;
        private Rigidbody cuerpo;
        private Vector3 posicionAnterior;
        private float velocidad;
        private bool enSuelo = true;

        public Animator Animador => animador;
        public bool LeerDelPadre { get => leerDelPadre; set => leerDelPadre = value; }

        /// <summary>Rapidez horizontal en m/s. Se escribe a mano o la calcula este componente si LeerDelPadre está activo.</summary>
        public float Velocidad
        {
            get => velocidad;
            set { velocidad = Mathf.Max(0f, value); Aplicar(); }
        }

        public bool EnSuelo
        {
            get => enSuelo;
            set { enSuelo = value; Aplicar(); }
        }

        public bool Apuntando
        {
            get => apuntando;
            set { apuntando = value; Aplicar(); }
        }

        public TipoArmaVisual TipoArma
        {
            get => tipoArma;
            set { tipoArma = value; Aplicar(); }
        }

        public bool Cubierto
        {
            get => cubierto;
            set { cubierto = value; Aplicar(); }
        }

        /// <summary>Tirado en el suelo (KO).</summary>
        public bool Caido
        {
            get => caido;
            set { caido = value; Aplicar(); }
        }

        private bool apuntando;
        private TipoArmaVisual tipoArma;
        private bool cubierto;
        private bool caido;

        private void Awake()
        {
            if (animador == null)
            {
                animador = GetComponentInChildren<Animator>(true);
            }
            if (animador == null)
            {
                Debug.LogError("[Personaje] Falta el Animator en VisualPersonaje de " + name, this);
                enabled = false;
                return;
            }
            Transform padre = transform.parent != null ? transform.parent : transform;
            controlador = padre.GetComponentInParent<CharacterController>();
            cuerpo = padre.GetComponentInParent<Rigidbody>();
            posicionAnterior = padre.position;
        }

        private void OnEnable()
        {
            Transform padre = transform.parent != null ? transform.parent : transform;
            posicionAnterior = padre.position;
        }

        private void Update()
        {
            if (leerDelPadre)
            {
                LeerEstadoDelPadre(Time.deltaTime);
            }
            Aplicar();
        }

        private void LeerEstadoDelPadre(float dt)
        {
            Transform padre = transform.parent != null ? transform.parent : transform;
            float objetivo;
            if (controlador != null && controlador.enabled)
            {
                Vector3 v = controlador.velocity;
                objetivo = ReglasVisual.VelocidadHorizontal(v.x, v.z);
                enSuelo = controlador.isGrounded;
            }
            else if (cuerpo != null)
            {
                Vector3 v = cuerpo.linearVelocity;
                objetivo = ReglasVisual.VelocidadHorizontal(v.x, v.z);
                enSuelo = Mathf.Abs(v.y) < 0.5f;
            }
            else
            {
                Vector3 delta = padre.position - posicionAnterior;
                objetivo = dt > 0f ? ReglasVisual.VelocidadHorizontal(delta.x, delta.z) / dt : 0f;
                enSuelo = true;
            }
            posicionAnterior = padre.position;
            velocidad = suavizadoVelocidad > 0f && dt > 0f
                ? Mathf.Lerp(velocidad, objetivo, 1f - Mathf.Exp(-suavizadoVelocidad * dt))
                : objetivo;
        }

        public void Disparar() => Disparo(IdDisparar);

        public void Recargar() => Disparo(IdRecargar);

        public void Golpear() => Disparo(IdGolpe);

        public void Herir() => Disparo(IdHerido);

        public void Rodar() => Disparo(IdRodar);

        private void Disparo(int id)
        {
            if (animador != null && Tiene(id, AnimatorControllerParameterType.Trigger))
            {
                animador.SetTrigger(id);
            }
        }

        private void Aplicar()
        {
            if (animador == null)
            {
                return;
            }
            Poner(IdVelocidad, velocidad);
            Poner(IdEnSuelo, enSuelo);
            Poner(IdApuntando, apuntando);
            Poner(IdTipoArma, (int)tipoArma);
            Poner(IdCaer, caido);
            Poner(IdCubierto, cubierto);
        }

        private void Poner(int id, float valor)
        {
            if (Tiene(id, AnimatorControllerParameterType.Float))
            {
                animador.SetFloat(id, valor);
            }
        }

        private void Poner(int id, bool valor)
        {
            if (Tiene(id, AnimatorControllerParameterType.Bool))
            {
                animador.SetBool(id, valor);
            }
        }

        private void Poner(int id, int valor)
        {
            if (Tiene(id, AnimatorControllerParameterType.Int))
            {
                animador.SetInteger(id, valor);
            }
        }

        private bool Tiene(int id, AnimatorControllerParameterType tipo)
        {
            if (animador.runtimeAnimatorController == null)
            {
                return false;
            }
            if (parametros == null || controladorCacheado != animador.runtimeAnimatorController)
            {
                controladorCacheado = animador.runtimeAnimatorController;
                parametros = new System.Collections.Generic.Dictionary<int, AnimatorControllerParameterType>();
                foreach (AnimatorControllerParameter p in animador.parameters)
                {
                    parametros[p.nameHash] = p.type;
                }
            }
            return parametros.TryGetValue(id, out AnimatorControllerParameterType encontrado) && encontrado == tipo;
        }

        private System.Collections.Generic.Dictionary<int, AnimatorControllerParameterType> parametros;
        private RuntimeAnimatorController controladorCacheado;
    }
}
