using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// Apunta con las manos cuando el modelo no tiene animación de apuntar (T-520): con <see cref="VisualPersonaje.Apuntando"/> y un arma
    /// en la mano lleva las manos al frente del pecho por IK de Animator (necesita la capa base con IK Pass). Va en el mismo objeto que el Animator.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DefaultExecutionOrder(10)]
    public class ApuntadoIK : MonoBehaviour
    {
        [Tooltip("Visual del que se lee Apuntando y TipoArma. Si está vacío se busca en los padres.")]
        [SerializeField] private VisualPersonaje visual;
        [Tooltip("Qué tan al frente del pecho queda la mano de la pistola (m).")]
        [Min(0f)] [SerializeField] private float alcancePistola = 0.5f;
        [Tooltip("Qué tan al frente del pecho queda la mano que sostiene el arma larga (m).")]
        [Min(0f)] [SerializeField] private float alcanceArmaLarga = 0.62f;
        [Tooltip("Separación lateral de las manos respecto al pecho (m).")]
        [Min(0f)] [SerializeField] private float separacionManos = 0.1f;
        [Tooltip("Altura de las manos sobre el pecho (m); negativa = más abajo.")]
        [SerializeField] private float alturaManos = 0.06f;
        [Tooltip("Rapidez con la que sube o baja el arma (1/s).")]
        [Min(0.1f)] [SerializeField] private float rapidez = 10f;

        [Tooltip("Punto de la mano derecha donde van las armas (hijo del hueso de la mano). Su -Y apunta por el cañón: el arma lo gira al frente al apuntar. Vacío = no se mueve.")]
        [SerializeField] private Transform socketArma;
        [Tooltip("Inclinación del arma hacia abajo cuando no se apunta (°); 0 = horizontal, 90 = colgando.")]
        [Range(0f, 90f)] [SerializeField] private float inclinacionBajada = 60f;

        [Tooltip("Llevar las manos al frente por IK. Se apaga cuando el controlador ya trae las animaciones ApuntarPistola y ApuntarLarga de verdad.")]
        [SerializeField] private bool manosPorIK = true;

        [Tooltip("T-737: no tocar ningún hueso al apuntar (sin giro de torso, sin IK de manos): se ve el clip tal cual. El jugador lo enciende desde AnimacionJugador.")]
        [SerializeField] private bool clipPuro;

        private Animator animador;
        private float peso;

        // T-681: máximo que se gira el torso al apuntar para que el cañón dé justo en el punto de mira (°).
        private const float MaximoGiroTorso = 30f;

        [Header("Arma en la mano (S1)")]
        [Tooltip("Máximo que se gira la mano derecha al apuntar para que el cañón mire al frente (°).")]
        [Range(0f, 90f)] [SerializeField] private float maximoCorreccionCanon = 45f;
        [Tooltip("Inclinación del cañón de las armas largas (°, positiva hacia arriba): siempre se llevan listas al frente, no cruzadas sobre el pecho como en la animación.")]
        [Range(-45f, 45f)] [SerializeField] private float inclinacionArmaLarga = -5f;
        [Tooltip("Máximo que se gira el arma larga para dejarla al frente (°).")]
        [Range(0f, 180f)] [SerializeField] private float maximoCorreccionArmaLarga = 120f;
        [Tooltip("Rapidez con la que la mano izquierda se pega al arma o se suelta (1/s).")]
        [Min(0.1f)] [SerializeField] private float rapidezManoIzquierda = 8f;

        private static readonly int HashRecargarPistola = Animator.StringToHash("PistolaRecargar");
        private static readonly int HashRecargarPistolaCaminar = Animator.StringToHash("PistolaRecargarCaminar");
        private static readonly int HashRecargarLarga = Animator.StringToHash("LargaRecargar");
        private int capaArriba = -2;
        private float pesoIzquierda;
        private float pesoCorreccion;
        private Transform armaActiva;
        private Transform agarreIzquierdo;
        private Transform bocaActiva;
        private bool hayMira;
        private Vector3 puntoMira;

        /// <summary>Qué tanto está pegada la mano izquierda al arma (0 a 1).</summary>
        public float PesoManoIzquierda => pesoIzquierda;

        /// <summary>Qué tanto está levantada el arma, de 0 (bajada) a 1.</summary>
        public float Peso => peso;

        /// <summary>T-681: el punto del mundo al que apunta la mira (el centro de la cámara de juego). Mientras haya uno el cañón se lleva exactamente hacia él; sin punto mira al frente del cuerpo.</summary>
        public void PonerPuntoMira(Vector3 punto)
        {
            puntoMira = punto;
            hayMira = true;
        }

        /// <summary>T-681: quita el punto de mira; el cañón vuelve a mirar al frente del cuerpo.</summary>
        public void QuitarPuntoMira() => hayMira = false;

        /// <summary>T-738: hay un punto de mira puesto (lo lee AgarrePistolaPJ01).</summary>
        public bool HayMira => hayMira;

        /// <summary>T-738: el punto de mira puesto por <see cref="PonerPuntoMira"/>.</summary>
        public Vector3 PuntoMira => puntoMira;

        private void Awake()
        {
            animador = GetComponent<Animator>();
            if (visual == null)
            {
                visual = GetComponentInParent<VisualPersonaje>();
            }
        }

        /// <summary>Para los creadores: el socket del arma.</summary>
        public void ConfigurarSocket(Transform socket) => socketArma = socket;

        public Transform SocketArma => socketArma;

        public bool ManosPorIK { get => manosPorIK; set => manosPorIK = value; }

        /// <summary>
        /// T-737: con el clip puro este componente no escribe ningún hueso: ni el torso girado al punto de mira, ni la mano derecha corregida, ni la mano izquierda pegada al arma (AgarreIzq),
        /// ni las manos por IK. El jugador se ve exactamente como el clip de Tony (medido: 0.0° de diferencia en torso, brazos y cabeza). El punto de mira sigue guardado para quien lo lea.
        /// </summary>
        public bool ClipPuro { get => clipPuro; set => clipPuro = value; }

        private void Update()
        {
            if (visual == null)
            {
                return;
            }
            bool arriba = visual.Apuntando && visual.TipoArma != TipoArmaVisual.Ninguna && !visual.Caido;
            peso = Mathf.MoveTowards(peso, arriba ? 1f : 0f, rapidez * Time.deltaTime);
            ActualizarManoIzquierda();
        }


        // T-681: lo que el IK de la muñeca no alcanza (la muñeca tiene límites) se termina girando el torso entero, con los brazos, el arma y las manos juntos.
        private void AjustarTorsoAlPuntoDeMira()
        {
            if (clipPuro || manosPorIK || !hayMira || armaActiva == null || pesoCorreccion <= 0.001f || animador == null || !animador.isHuman)
            {
                return;
            }
            Transform columna = animador.GetBoneTransform(HumanBodyBones.Spine);
            if (columna == null)
            {
                return;
            }
            Vector3 boca = bocaActiva != null ? bocaActiva.position : armaActiva.position;
            Quaternion resto = AgarresArmas.CorreccionHaciaDireccion(armaActiva.rotation, puntoMira - boca, pesoCorreccion, MaximoGiroTorso);
            columna.rotation = resto * columna.rotation;
        }

        private void LateUpdate()
        {
            AjustarTorsoAlPuntoDeMira();
            if (clipPuro || socketArma == null || visual == null || !manosPorIK)
            {
                return;
            }
            // Con el arma arriba el cañón apunta al frente; abajo, inclinado hacia el piso.
            Vector3 frente = visual.transform.forward;
            Vector3 bajada = Quaternion.AngleAxis(inclinacionBajada, visual.transform.right) * frente;
            Vector3 canon = Vector3.Slerp(bajada, frente, peso).normalized;
            // La -Y local del socket es la dirección del cañón (las armas del jugador cuelgan de la mano con -Y).
            socketArma.rotation = Quaternion.LookRotation(canon, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
        }

        // El arma que se ve en la mano (hija del socket y activa) y su punto de agarre de la mano izquierda.
        private void BuscarArmaActiva()
        {
            armaActiva = null;
            agarreIzquierdo = null;
            bocaActiva = null;
            if (socketArma == null)
            {
                return;
            }
            foreach (Transform hijo in socketArma)
            {
                if (hijo.gameObject.activeInHierarchy && hijo.Find(AgarresArmas.NombreAgarreIzquierdo) != null)
                {
                    armaActiva = hijo;
                    agarreIzquierdo = hijo.Find(AgarresArmas.NombreAgarreIzquierdo);
                    bocaActiva = hijo.Find("Boca");
                    return;
                }
            }
        }

        private bool Recargando()
        {
            if (capaArriba == -2)
            {
                capaArriba = animador.GetLayerIndex(ReglasAnimacion.CapaArriba);
            }
            if (capaArriba < 0)
            {
                return false;
            }
            AnimatorStateInfo actual = animador.GetCurrentAnimatorStateInfo(capaArriba);
            AnimatorStateInfo siguiente = animador.GetNextAnimatorStateInfo(capaArriba);
            bool EsRecarga(AnimatorStateInfo s) => s.shortNameHash == HashRecargarPistola || s.shortNameHash == HashRecargarPistolaCaminar || s.shortNameHash == HashRecargarLarga;
            return EsRecarga(actual) || (animador.IsInTransition(capaArriba) && EsRecarga(siguiente));
        }

        // Arma a dos manos solo al apuntar (sin apuntar el jugador camina normal con el arma colgando, T-671); recargando la mano izquierda trabaja sola (T-650).
        private void ActualizarManoIzquierda()
        {
            float objetivo = 0f;
            float objetivoCorreccion = 0f;
            if (!clipPuro && !manosPorIK && animador != null && !visual.Caido && visual.TipoArma != TipoArmaVisual.Ninguna && !Recargando())
            {
                objetivo = peso;
                objetivoCorreccion = peso;
            }
            pesoIzquierda = Mathf.MoveTowards(pesoIzquierda, objetivo, rapidezManoIzquierda * Time.deltaTime);
            pesoCorreccion = Mathf.MoveTowards(pesoCorreccion, objetivoCorreccion, rapidezManoIzquierda * Time.deltaTime);
        }

        private void AplicarIKConArma()
        {
            BuscarArmaActiva();
            if (armaActiva == null)
            {
                return;
            }
            Transform manoDerecha = animador.GetBoneTransform(HumanBodyBones.RightHand);
            Transform manoIzquierda = animador.GetBoneTransform(HumanBodyBones.LeftHand);
            if (manoDerecha == null || manoIzquierda == null)
            {
                return;
            }
            // Dentro de OnAnimatorIK los huesos traen la pose de la animación de este cuadro, sin IK. Las metas de IK de las manos no están en
            // el hueso (llevan un giro y una distancia propios), así que se mueven en relación con ellas: meta nueva = meta animada + lo que hay que mover el hueso.
            // Al apuntar el cañón mira al frente: se gira la mano derecha por IK (la animación apunta algo a un lado) y la izquierda la sigue.
            bool larga = visual.TipoArma == TipoArmaVisual.ArmaLarga;
            float maximo = larga ? maximoCorreccionArmaLarga : maximoCorreccionCanon;
            Quaternion correccion = CorreccionDelCanon(manoDerecha.position, larga, maximo);
            if (pesoCorreccion > 0.001f)
            {
                animador.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
                animador.SetIKRotation(AvatarIKGoal.RightHand, correccion * animador.GetIKRotation(AvatarIKGoal.RightHand));
            }
            if (pesoIzquierda > 0.001f && agarreIzquierdo != null)
            {
                Vector3 destino = manoDerecha.position + correccion * (agarreIzquierdo.position - manoDerecha.position);
                Quaternion giroDestino = correccion * agarreIzquierdo.rotation;
                animador.SetIKPositionWeight(AvatarIKGoal.LeftHand, pesoIzquierda);
                animador.SetIKRotationWeight(AvatarIKGoal.LeftHand, pesoIzquierda);
                animador.SetIKPosition(AvatarIKGoal.LeftHand, animador.GetIKPosition(AvatarIKGoal.LeftHand) + (destino - manoIzquierda.position));
                animador.SetIKRotation(AvatarIKGoal.LeftHand, giroDestino * Quaternion.Inverse(manoIzquierda.rotation) * animador.GetIKRotation(AvatarIKGoal.LeftHand));
            }
        }

        // T-681: con punto de mira el cañón va de la boca a ese punto (se afina una vez con la boca ya girada); sin él, al frente del cuerpo.
        private Quaternion CorreccionDelCanon(Vector3 posicionMano, bool larga, float maximo)
        {
            if (!hayMira)
            {
                return AgarresArmas.CorreccionDeArma(armaActiva.rotation, visual.transform.forward, pesoCorreccion, maximo, larga ? inclinacionArmaLarga : float.NaN);
            }
            Vector3 boca = bocaActiva != null ? bocaActiva.position : armaActiva.position;
            Quaternion giro = AgarresArmas.CorreccionHaciaDireccion(armaActiva.rotation, puntoMira - boca, 1f, maximo);
            Vector3 bocaGirada = posicionMano + giro * (boca - posicionMano);
            giro = AgarresArmas.CorreccionHaciaDireccion(armaActiva.rotation, puntoMira - bocaGirada, 1f, maximo);
            return Quaternion.Slerp(Quaternion.identity, giro, pesoCorreccion);
        }

        private void OnAnimatorIK(int capa)
        {
            if (animador == null || visual == null || clipPuro)
            {
                return;
            }
            if (!manosPorIK)
            {
                if (capa == 0)
                {
                    AplicarIKConArma();
                }
                return;
            }
            Transform pecho = animador.GetBoneTransform(HumanBodyBones.UpperChest);
            if (pecho == null)
            {
                pecho = animador.GetBoneTransform(HumanBodyBones.Chest);
            }
            if (pecho == null)
            {
                return;
            }
            Transform referencia = visual.transform;
            Vector3 frente = referencia.forward;
            Vector3 derecha = referencia.right;
            bool larga = visual.TipoArma == TipoArmaVisual.ArmaLarga;
            Vector3 centro = pecho.position + Vector3.up * alturaManos;
            Vector3 manoDer = centro + frente * alcancePistola + derecha * separacionManos;
            Vector3 manoIzq = larga
                ? centro + frente * alcanceArmaLarga - derecha * separacionManos * 0.5f
                : centro + frente * alcancePistola - derecha * (separacionManos * 0.4f);
            Quaternion giro = Quaternion.LookRotation(frente, Vector3.up);
            Poner(AvatarIKGoal.RightHand, manoDer, giro);
            Poner(AvatarIKGoal.LeftHand, manoIzq, giro);
        }

        private void Poner(AvatarIKGoal mano, Vector3 posicion, Quaternion giro)
        {
            animador.SetIKPositionWeight(mano, peso);
            animador.SetIKRotationWeight(mano, peso * 0.6f);
            animador.SetIKPosition(mano, posicion);
            animador.SetIKRotation(mano, giro);
            AvatarIKHint codo = mano == AvatarIKGoal.RightHand ? AvatarIKHint.RightElbow : AvatarIKHint.LeftElbow;
            float lado = mano == AvatarIKGoal.RightHand ? 1f : -1f;
            animador.SetIKHintPositionWeight(codo, peso);
            animador.SetIKHintPosition(codo, posicion - visual.transform.forward * 0.25f + visual.transform.right * lado * 0.35f - Vector3.up * 0.15f);
        }
    }
}
