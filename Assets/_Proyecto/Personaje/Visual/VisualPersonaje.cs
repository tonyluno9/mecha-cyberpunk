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

        [Header("Pies sin deslizarse (T-520)")]
        [Tooltip("Velocidad (m/s) a la que avanza el clip Caminar tal como viene. 0 = no ajustar la rapidez de la animación.")]
        [Min(0f)] [SerializeField] private float velocidadClipCaminar;
        [Tooltip("Velocidad (m/s) a la que avanza el clip Correr tal como viene. 0 = no ajustar la rapidez de la animación.")]
        [Min(0f)] [SerializeField] private float velocidadClipCorrer;
        [Tooltip("Rapidez mínima de la animación al ajustarla a la velocidad real (1 = normal).")]
        [Min(0.1f)] [SerializeField] private float rapidezMinima = 0.6f;
        [Tooltip("Rapidez máxima de la animación al ajustarla a la velocidad real (1 = normal).")]
        [Min(1f)] [SerializeField] private float rapidezMaxima = 2.2f;
        [Header("Caída (sin animación de caer)")]
        [Tooltip("Tiempo que tarda el cuerpo en tumbarse al quedar KO (s). 0 = no se tumba.")]
        [Min(0f)] [SerializeField] private float segundosTumbarse = 0.35f;
        [Tooltip("Cuánto sube el cuerpo al estar tumbado para no hundirse en el piso (m).")]
        [Min(0f)] [SerializeField] private float alturaTumbado = 0.15f;

        // El modelo de Tripo mira a -Z local (nodo Giro de 180°): cae de espaldas con +90° sobre su eje X.
        private const float GradosTumbado = 90f;
        private const float VelocidadVerticalEnAire = 1.5f;
        private static readonly int HashCaminar = Animator.StringToHash("Caminar");
        private static readonly int HashCorrer = Animator.StringToHash("Correr");
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

        /// <summary>0 = de pie, 1 = tumbado (KO).</summary>
        public float Tumbado => tumbado;

        /// <summary>Para los creadores: las velocidades de los clips tal como vienen (m/s).</summary>
        public void ConfigurarClips(float caminar, float correr)
        {
            velocidadClipCaminar = caminar;
            velocidadClipCorrer = correr;
        }

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
        private float tumbado;
        private bool ejeGuardado;
        private Quaternion rotacionBase;
        private Vector3 posicionBase;

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
            AjustarRapidez();
        }

        private void LateUpdate()
        {
            float objetivo = caido && segundosTumbarse > 0f ? 1f : 0f;
            if (Mathf.Approximately(tumbado, objetivo) || animador == null)
            {
                return;
            }
            tumbado = segundosTumbarse > 0f ? Mathf.MoveTowards(tumbado, objetivo, Time.deltaTime / segundosTumbarse) : objetivo;
            // Se tumba el nodo Giro (padre del Modelo) y no el Modelo: CubiertaYRodada reescribe la rotación del Modelo cada cuadro.
            Transform eje = animador.transform.parent != null && animador.transform.parent != transform ? animador.transform.parent : animador.transform;
            if (!ejeGuardado)
            {
                ejeGuardado = true;
                rotacionBase = eje.localRotation;
                posicionBase = eje.localPosition;
            }
            eje.localRotation = rotacionBase * Quaternion.Euler(GradosTumbado * tumbado, 0f, 0f);
            eje.localPosition = posicionBase + new Vector3(0f, alturaTumbado * tumbado, 0f);
        }

        /// <summary>Acelera o frena Caminar y Correr para que los pies sigan el suelo a la velocidad real.</summary>
        private void AjustarRapidez()
        {
            if (animador == null || (velocidadClipCaminar <= 0f && velocidadClipCorrer <= 0f))
            {
                return;
            }
            int estado = animador.GetCurrentAnimatorStateInfo(0).shortNameHash;
            float referencia = estado == HashCaminar ? velocidadClipCaminar : estado == HashCorrer ? velocidadClipCorrer : 0f;
            animador.speed = referencia > 0f ? Mathf.Clamp(velocidad / referencia, rapidezMinima, rapidezMaxima) : 1f;
        }

        private void LeerEstadoDelPadre(float dt)
        {
            Transform padre = transform.parent != null ? transform.parent : transform;
            float objetivo;
            if (controlador != null && controlador.enabled)
            {
                Vector3 v = controlador.velocity;
                objetivo = ReglasVisual.VelocidadHorizontal(v.x, v.z);
                // Un CharacterController quieto que no recibe Move cada cuadro reporta isGrounded falso: solo cuenta como aire si de verdad sube o cae.
                enSuelo = controlador.isGrounded || Mathf.Abs(v.y) < VelocidadVerticalEnAire;
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
