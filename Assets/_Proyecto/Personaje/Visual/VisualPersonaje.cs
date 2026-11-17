using System;
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
        [Header("Controlador base (T-561)")]
        [Tooltip("Rapidez con la que la capa de arriba (torso) se apaga al cubrirse, agacharse o caer (1/s).")]
        [Min(0.1f)] [SerializeField] private float rapidezCapaArriba = 8f;
        [Tooltip("Suavizado de la dirección local y del giro (1/s).")]
        [Min(0.1f)] [SerializeField] private float suavizadoDireccion = 14f;

        [Header("Caída (sin animación de caer)")]
        [Tooltip("Tiempo que tarda el cuerpo en tumbarse al quedar KO (s). 0 = no se tumba.")]
        [Min(0f)] [SerializeField] private float segundosTumbarse = 0.35f;
        [Tooltip("Cuánto sube el cuerpo al estar tumbado para no hundirse en el piso (m).")]
        [Min(0f)] [SerializeField] private float alturaTumbado = 0.15f;

        // T-640: el modelo de Tripo ya mira a +Z local (el nodo Giro quedó en 0°): cae de espaldas con -90° sobre su eje X.
        private const float GradosTumbado = -90f;
        private const float VelocidadVerticalEnAire = 1.5f;
        private static readonly int HashIdle = Animator.StringToHash("Idle");
        private static readonly int HashCaminar = Animator.StringToHash("Caminar");
        private static readonly int HashAgachadoCaminar = Animator.StringToHash("AgachadoCaminar");
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
        private static readonly int IdVelX = Animator.StringToHash(ReglasAnimacion.VelX);
        private static readonly int IdVelZ = Animator.StringToHash(ReglasAnimacion.VelZ);
        private static readonly int IdApuntarX = Animator.StringToHash(ReglasAnimacion.ApuntarX);
        private static readonly int IdApuntarZ = Animator.StringToHash(ReglasAnimacion.ApuntarZ);
        private static readonly int IdVelY = Animator.StringToHash(ReglasAnimacion.VelY);
        private static readonly int IdGiro = Animator.StringToHash(ReglasAnimacion.Giro);
        private static readonly int IdAgachado = Animator.StringToHash(ReglasAnimacion.Agachado);
        private static readonly int IdDanoTipo = Animator.StringToHash(ReglasAnimacion.DanoTipo);
        private static readonly int IdMuerteTipo = Animator.StringToHash(ReglasAnimacion.MuerteTipo);
        private static readonly int IdGolpeTipo = Animator.StringToHash(ReglasAnimacion.GolpeTipo);
        private static readonly int IdRapidezGolpe = Animator.StringToHash(ReglasAnimacion.RapidezGolpe);
        private static readonly int IdCoberturaTipo = Animator.StringToHash(ReglasAnimacion.CoberturaTipo);
        private static readonly int IdDisparoCobertura = Animator.StringToHash(ReglasAnimacion.DisparoCobertura);
        private static readonly int IdFrenar = Animator.StringToHash(ReglasAnimacion.Frenar);
        private static readonly int IdEnGuardia = Animator.StringToHash(ReglasAnimacion.EnGuardia);
        private static readonly int IdGesto = Animator.StringToHash(ReglasAnimacion.Gesto);
        private static readonly int IdGestoTipo = Animator.StringToHash(ReglasAnimacion.GestoTipo);
        private static readonly int IdManejando = Animator.StringToHash(ReglasAnimacion.Manejando);
        private static readonly int IdViendoTelefono = Animator.StringToHash(ReglasAnimacion.ViendoTelefono);

        private CharacterController controlador;
        private Rigidbody cuerpo;
        private Vector3 posicionAnterior;
        private float velocidad;
        private bool enSuelo = true;
        /// <summary>S2: con true el visual cuenta al jugador como en el suelo (trepando o agarrándose de un borde, sin animación de caída).</summary>
        public bool ForzarSuelo { get; set; }

        public Animator Animador => animador;
        public bool LeerDelPadre
        {
            get => leerDelPadre;
            set
            {
                if (value && !leerDelPadre)
                {
                    // T-976: al volver a leer del padre se parte de donde está ahora. Con lo viejo (de antes de subir a la moto) el giro acumulado
                    // hacía que el cuerpo jugara GirarIzq/GirarDer en el sitio al quedar de pie.
                    Transform padre = transform.parent != null ? transform.parent : transform;
                    posicionAnterior = padre.position;
                    yawAnterior = transform.eulerAngles.y;
                    giro = 0f;
                }
                leerDelPadre = value;
            }
        }

        private float rapidezGolpes = 1f;

        /// <summary>Rapidez de los golpes de puños (1 = la del clip). El jugador la sube para que la animación siga el ritmo del golpe del juego (T-652).</summary>
        public float RapidezGolpes { get => rapidezGolpes; set => rapidezGolpes = Mathf.Max(0.1f, value); }

        /// <summary>T-944: rapidez de los clips de caer muerto (1 = tal como vienen).</summary>
        public float RapidezMuerte { get; set; } = 1f;

        /// <summary>0 = de pie, 1 = tumbado (KO).</summary>
        public float Tumbado => tumbado;

        /// <summary>Para los creadores: las velocidades de los clips tal como vienen (m/s).</summary>
        public void ConfigurarClips(float caminar, float correr)
        {
            velocidadClipCaminar = caminar;
            velocidadClipCorrer = correr;
        }

        /// <summary>Para los creadores: las velocidades de los clips y la rapidez máxima de la animación (T-562).</summary>
        public void ConfigurarClips(float caminar, float correr, float rapidezMax)
        {
            ConfigurarClips(caminar, correr);
            rapidezMaxima = Mathf.Max(1f, rapidezMax);
        }

        /// <summary>
        /// Variación de un NPC (T-562): salta el idle a <paramref name="desfase"/> (fracción del ciclo, 0 a 1) y lo juega a esa rapidez (1 = normal).
        /// Solo afecta al estado Idle: al caminar la rapidez sigue a la velocidad real.
        /// </summary>
        public void Variar(float desfase, float rapidez)
        {
            rapidezIdle = Mathf.Clamp(rapidez, 0.5f, 1.5f);
            if (animador != null && animador.runtimeAnimatorController != null && animador.GetCurrentAnimatorStateInfo(0).shortNameHash == HashIdle)
            {
                animador.Play(HashIdle, 0, Mathf.Repeat(desfase, 1f));
                animador.Update(0f);
            }
        }

        /// <summary>Rapidez con la que se juega el idle de este personaje.</summary>
        public float RapidezIdle => rapidezIdle;

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

        /// <summary>Segundos que el arma sigue "levantada" (pose de arma en el torso) tras disparar o recargar sin apuntar (T-671).</summary>
        public const float SegundosArmaTrasAccion = 1.5f;

        private float armaLevantadaHasta;

        /// <summary>El tipo de arma que ve el Animator: solo al apuntar (o justo después de disparar o recargar) se usan las poses de arma; si no, camina y corre normal (T-671).</summary>
        public TipoArmaVisual TipoArmaAnimador => apuntando || Time.time < armaLevantadaHasta ? tipoArma : TipoArmaVisual.Ninguna;

        /// <summary>
        /// T-733: la pistola solo está en la mano mientras se apunta (o justo después de disparar o recargar): es cuando el Animator
        /// juega Pistola_Sacar y no Pistola_Guardar. Es lo que se quiere; <see cref="ArmaEnMano"/> es lo que ya se ve.
        /// </summary>
        public bool ArmaLevantada => TipoArmaAnimador != TipoArmaVisual.Ninguna;

        /// <summary>
        /// T-734: el arma ya está en la mano. La prende el evento de Pistola_Sacar (0.30 s) o un disparo o recarga al instante, y la apaga el evento de
        /// Pistola_Guardar (0.53 s). Lo lee ArmasAPie para mostrar u ocultar la pistola del jugador.
        /// </summary>
        public bool ArmaEnMano { get; private set; }

        /// <summary>Avisa cuando cambia <see cref="ArmaEnMano"/>.</summary>
        public event Action<bool> CambioArmaEnMano;

        /// <summary>La usa <see cref="ReceptorEventosArma"/>; también para pruebas.</summary>
        public void PonerArmaEnMano(bool enMano)
        {
            if (ArmaEnMano == enMano)
            {
                return;
            }
            ArmaEnMano = enMano;
            CambioArmaEnMano?.Invoke(enMano);
        }

        // Red de seguridad: si un evento no llegó (el estado se cortó, la capa quedó sin peso, se cambió el modelo) el arma sigue lo que juega el torso.
        private void CorregirArmaEnMano()
        {
            int capa = IndiceCapaArriba();
            if (capa < 0 || animador.IsInTransition(capa))
            {
                return;
            }
            AnimatorStateInfo estado = animador.GetCurrentAnimatorStateInfo(capa);
            if (ArmaEnMano && !ArmaLevantada && estado.shortNameHash == HashVacio)
            {
                PonerArmaEnMano(false);
            }
            else if (!ArmaEnMano && ArmaLevantada && estado.shortNameHash != HashVacio && !estado.IsName("PistolaSacar") && !estado.IsName("PistolaGuardar"))
            {
                PonerArmaEnMano(true);
            }
        }

        private void LevantarArma()
        {
            armaLevantadaHasta = Time.time + SegundosArmaTrasAccion;
            Aplicar();
            // Disparar o recargar al instante: la pistola está en la mano ya, sin esperar el evento de Pistola_Sacar.
            PonerArmaEnMano(true);
        }

        public bool Cubierto
        {
            get => cubierto;
            set { cubierto = value; Aplicar(); }
        }

        /// <summary>Tirado en el suelo (KO). Al caer elige cómo según de dónde vino el último golpe (<see cref="Herir"/>).</summary>
        public bool Caido
        {
            get => caido;
            set
            {
                if (value && !caido)
                {
                    ElegirMuerte();
                }
                caido = value;
                Aplicar();
            }
        }

        /// <summary>Agachado caminando (con rifle).</summary>
        public bool Agachado
        {
            get => agachado;
            set
            {
                agachado = value;
                Aplicar();
            }
        }

        /// <summary>Sentado al volante de un carro (T-622): el estado CarroManejar. Quitarlo lo para de nuevo.</summary>
        public bool Manejando
        {
            get => manejando;
            set { manejando = value; Aplicar(); }
        }

        /// <summary>
        /// T-695: congela la animación en el cuadro actual (la pose se sigue moviendo por IK y por quien la coloque). Para el conductor de una moto,
        /// que va quieto: el idle respira y mueve la cabeza unos centímetros, y eso se veía como tambaleo.
        /// </summary>
        public bool Congelado
        {
            get => congelado;
            set
            {
                congelado = value;
                if (!value && animador != null)
                {
                    animador.speed = 1f;
                }
            }
        }

        private bool congelado;

        /// <summary>De pie viendo el celular (T-623), en bucle.</summary>
        public bool ViendoTelefono
        {
            get => viendoTelefono;
            set { viendoTelefono = value; Aplicar(); }
        }

        /// <summary>
        /// Lanza un gesto de una vez (carro, interacción, burla...). No hace nada si el personaje está caído o el Animator no tiene el gesto.
        /// Mientras dura apaga la capa de arriba (torso) para que no se encime un arma.
        /// </summary>
        public void Gestear(GestoVisual gesto)
        {
            if (caido || gesto == GestoVisual.Ninguno || animador == null || !Tiene(IdGesto, AnimatorControllerParameterType.Trigger))
            {
                return;
            }
            if (Tiene(IdGestoTipo, AnimatorControllerParameterType.Int))
            {
                animador.SetInteger(IdGestoTipo, (int)gesto);
            }
            animador.SetTrigger(IdGesto);
            if (gesto == GestoVisual.Pitar)
            {
                segundosPitarPendiente = 0.15f;
            }
            else
            {
                segundosGestoPendiente = 0.15f;
            }
        }

        public void Pitar() => Gestear(GestoVisual.Pitar);

        public void Burlar() => Gestear(GestoVisual.Burla);

        public void Presumir() => Gestear(GestoVisual.Presumir);

        // T-705: los NPC de Trafico (que no pueden referenciar este ensamblado) mandan estos mensajes con BroadcastMessage("Tumbar" / "Levantar" /
        // "Telefonear" / "Burlar"); solo llega al modelo activo. Son lo que un personaje hace al caer, levantarse y llamar por teléfono.

        /// <summary>Lo tira al suelo (igual que <see cref="Caido"/> = true).</summary>
        public void Tumbar() => Caido = true;

        /// <summary>Lo levanta (igual que <see cref="Caido"/> = false).</summary>
        public void Levantar() => Caido = false;

        /// <summary>Saca el teléfono (gesto Inter_Telefono).</summary>
        public void Telefonear() => Gestear(GestoVisual.Telefono);

        /// <summary>T-964: avisa cuando el gesto del teléfono lo lleva al oído (evento de PJ01_Inter_Telefono).</summary>
        public event System.Action TelefonoEnOreja;

        /// <summary>T-964: cuántas veces llegó el teléfono al oído (para las pruebas).</summary>
        public int VecesTelefonoEnOreja { get; private set; }

        /// <summary>La usa <see cref="ReceptorEventosArma"/>.</summary>
        public void AvisarTelefonoEnOreja()
        {
            VecesTelefonoEnOreja++;
            TelefonoEnOreja?.Invoke();
        }

        /// <summary>True mientras la capa base juega un gesto, manejar o el celular (los estados con la etiqueta Gesto).</summary>
        public bool EnGesto
        {
            get
            {
                if (animador == null || animador.runtimeAnimatorController == null)
                {
                    return false;
                }
                return segundosGestoPendiente > 0f || animador.GetCurrentAnimatorStateInfo(0).IsTag(ReglasAnimacion.EtiquetaGesto)
                    || (animador.IsInTransition(0) && animador.GetNextAnimatorStateInfo(0).IsTag(ReglasAnimacion.EtiquetaGesto));
            }
        }

        /// <summary>True mientras suena el claxon (la capa de arriba juega Pitar).</summary>
        public bool Pitando => animador != null && capaArriba >= 0 && (segundosPitarPendiente > 0f || animador.GetCurrentAnimatorStateInfo(capaArriba).IsName("Pitar")
            || (animador.IsInTransition(capaArriba) && animador.GetNextAnimatorStateInfo(capaArriba).IsName("Pitar")));

        private float segundosPitarPendiente;
        private float segundosGestoPendiente;
        private bool manejando;
        private bool viendoTelefono;

        /// <summary>Cómo cayó (o caerá) el personaje.</summary>
        public MuerteVisual Muerte { get; private set; }

        /// <summary>Fuente del azar (0 a menos de 1) para elegir la muerte entre las válidas. Para pruebas.</summary>
        public void InyectarAzar(Func<float> fuente) => azar = fuente ?? (() => UnityEngine.Random.value);

        /// <summary>Elige la muerte entre las válidas para la dirección del último golpe y la deja lista en el Animator.</summary>
        public MuerteVisual ElegirMuerte()
        {
            Muerte = ReglasAnimacion.ElegirMuerte(ultimaDireccionGolpe, transform.forward, azar());
            if (animador != null && Tiene(IdMuerteTipo, AnimatorControllerParameterType.Int))
            {
                animador.SetInteger(IdMuerteTipo, (int)Muerte);
            }
            return Muerte;
        }

        /// <summary>Reacción al daño: de frente, de espalda o fuerte según la dirección del golpe (del atacante al personaje).</summary>
        public void Herir(Vector3 direccionGolpe, bool fuerte = false)
        {
            ultimaDireccionGolpe = direccionGolpe;
            if (animador != null && Tiene(IdDanoTipo, AnimatorControllerParameterType.Int))
            {
                animador.SetInteger(IdDanoTipo, (int)ReglasAnimacion.ElegirDano(direccionGolpe, transform.forward, fuerte));
            }
            Disparo(IdHerido);
        }

        /// <summary>Guarda de dónde vino el último golpe sin mostrar reacción (el golpe que mata: decide cómo cae).</summary>
        public void RegistrarGolpe(Vector3 direccionGolpe) => ultimaDireccionGolpe = direccionGolpe;

        /// <summary>Golpe de puños: alterna izquierda y derecha, o fuerte.</summary>
        public void Golpear(bool fuerte)
        {
            ultimoGolpe = Time.time;
            CortarReaccion(); // T-675 con la capa aditiva de T-735: un golpe de puños corta el estremecimiento, no se encima
            golpeAlterno = fuerte ? 2 : 1 - golpeAlterno % 2;
            if (animador != null && Tiene(IdGolpeTipo, AnimatorControllerParameterType.Int))
            {
                animador.SetInteger(IdGolpeTipo, golpeAlterno);
            }
            Disparo(IdGolpe);
        }

        /// <summary>
        /// T-985: si es true, tras un golpe de puños el personaje se queda en guardia (PJ01_Punos_Guardia) <see cref="ReglasAnimacion.SegundosGuardiaPunos"/> segundos.
        /// Solo lo enciende el jugador (AnimacionJugador): los enemigos conservan su pose al terminar el golpe.
        /// </summary>
        public bool GuardiaTrasGolpe { get; set; }

        /// <summary>True mientras el personaje está en guardia de puños: con <see cref="GuardiaTrasGolpe"/>, sin arma levantada y poco después de su último golpe.</summary>
        public bool EnGuardia => GuardiaTrasGolpe && !caido && TipoArmaAnimador == TipoArmaVisual.Ninguna && Time.time - ultimoGolpe < ReglasAnimacion.SegundosGuardiaPunos;

        // T-675: el siguiente golpe espera a que acabe la animación del anterior; la reacción al daño se corta con un golpe o un esquive.
        private const float EsperaDisparador = 0.08f;
        private const float DuracionMaximaGolpe = 1.5f;
        private static readonly int HashVacio = Animator.StringToHash("Vacio");
        private float ultimoGolpe = float.NegativeInfinity;

        private int IndiceCapaArriba()
        {
            if (animador == null || animador.runtimeAnimatorController == null)
            {
                return -1;
            }
            if (capaArriba == -2)
            {
                capaArriba = animador.GetLayerIndex(ReglasAnimacion.CapaArriba);
            }
            return capaArriba;
        }

        private static bool EsGolpe(AnimatorStateInfo e) => e.IsName("GolpeIzq") || e.IsName("GolpeDer") || e.IsName("GolpeFuerte");

        private static bool EsDano(AnimatorStateInfo e) => e.IsName("DanoFrente") || e.IsName("DanoEspalda") || e.IsName("DanoFuerte");

        /// <summary>True mientras suena la animación de un golpe de puños (desde que se pidió hasta que termina, con un tope de seguridad).</summary>
        public bool GolpeEnCurso
        {
            get
            {
                float desde = Time.time - ultimoGolpe;
                if (desde > DuracionMaximaGolpe)
                {
                    return false;
                }
                if (desde < EsperaDisparador)
                {
                    return true;
                }
                int capa = IndiceCapaArriba();
                if (capa < 0)
                {
                    return false;
                }
                if (animador.IsInTransition(capa))
                {
                    return EsGolpe(animador.GetNextAnimatorStateInfo(capa));
                }
                AnimatorStateInfo actual = animador.GetCurrentAnimatorStateInfo(capa);
                return EsGolpe(actual) && actual.normalizedTime < 1f;
            }
        }

        // T-735: la reacción al daño vive en su propia capa aditiva, no en la de arriba.
        private int IndiceCapaDano()
        {
            if (animador == null || animador.runtimeAnimatorController == null)
            {
                return -1;
            }
            if (capaDano == -2)
            {
                capaDano = animador.GetLayerIndex(ReglasAnimacion.CapaDano);
            }
            return capaDano;
        }

        /// <summary>Peso actual de la capa aditiva de daño (0 si está caído o manejando), de 0 a 1.</summary>
        public float PesoCapaDano => pesoDano;

        /// <summary>True mientras suena la reacción a un golpe recibido.</summary>
        public bool ReaccionEnCurso
        {
            get
            {
                int capa = IndiceCapaDano();
                if (capa < 0)
                {
                    return false;
                }
                return EsDano(animador.GetCurrentAnimatorStateInfo(capa)) || (animador.IsInTransition(capa) && EsDano(animador.GetNextAnimatorStateInfo(capa)));
            }
        }

        /// <summary>Corta la reacción al daño al instante (esquive): el torso vuelve a su pose normal.</summary>
        public void CortarReaccion()
        {
            int capa = IndiceCapaDano();
            if (capa < 0)
            {
                return;
            }
            if (Tiene(IdHerido, AnimatorControllerParameterType.Trigger))
            {
                animador.ResetTrigger(IdHerido);
            }
            if (ReaccionEnCurso)
            {
                animador.CrossFadeInFixedTime(HashVacio, 0.06f, capa);
            }
        }

        private float velX;
        private float velZ;
        private float velY;
        private float giro;
        private float rapidezIdle = 1f;
        private bool rapidezAgachadoAplicada;
        private float pesoArriba = 1f;
        private float velocidadAnterior;
        private float yawAnterior;
        private int capaArriba = -2;
        private int capaDano = -2;
        private float pesoDano = 1f;
        private bool agachado;
        private int golpeAlterno = 1;
        private int coberturaAlterna;
        private Vector3 ultimaDireccionGolpe;
        private Func<float> azar = () => UnityEngine.Random.value;
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
            if (animador.GetComponent<ReceptorEventosArma>() == null)
            {
                animador.gameObject.AddComponent<ReceptorEventosArma>();
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
            CorregirArmaEnMano();
            if (congelado)
            {
                animador.speed = 0f;
            }
            else
            {
                AjustarRapidez();
            }
            ActualizarCapas(Time.deltaTime);
        }

        private void ActualizarCapas(float dt)
        {
            if (animador == null || animador.runtimeAnimatorController == null)
            {
                return;
            }
            if (capaArriba == -2)
            {
                capaArriba = animador.GetLayerIndex(ReglasAnimacion.CapaArriba);
            }
            // T-735: la capa aditiva de daño va con peso 1; solo se apaga caído (la muerte manda) o manejando (el volante manda).
            int dano = IndiceCapaDano();
            if (dano >= 0)
            {
                pesoDano = Mathf.MoveTowards(pesoDano, caido || manejando ? 0f : 1f, rapidezCapaArriba * dt);
                animador.SetLayerWeight(dano, pesoDano);
            }
            if (capaArriba < 0)
            {
                return;
            }
            segundosGestoPendiente = Mathf.Max(0f, segundosGestoPendiente - dt);
            segundosPitarPendiente = Mathf.Max(0f, segundosPitarPendiente - dt);
            // Sentado al volante la capa de arriba solo se usa para pitar (los brazos al claxon); si no, taparía el volante con la pose del arma.
            float objetivo = cubierto || caido || (agachado && !apuntando) || viendoTelefono || EnGesto ? 0f : 1f;
            if (manejando)
            {
                objetivo = Pitando ? 1f : 0f;
            }
            pesoArriba = Mathf.MoveTowards(pesoArriba, objetivo, rapidezCapaArriba * dt);
            animador.SetLayerWeight(capaArriba, pesoArriba);

        }

        /// <summary>Peso actual de la capa de arriba (torso), de 0 a 1.</summary>
        public float PesoCapaArriba => pesoArriba;

        private void LateUpdate()
        {
            float objetivo = caido && segundosTumbarse > 0f && !TieneMuerteAnimada() ? 1f : 0f;
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

        private bool TieneMuerteAnimada() => animador != null && Tiene(IdMuerteTipo, AnimatorControllerParameterType.Int);

        /// <summary>Escala del modelo contra la que se midieron las velocidades de los clips (1 = el clip tal como viene): el paso crece con el modelo (T-670).</summary>
        public float EscalaClips => animador != null ? Mathf.Max(0.1f, animador.transform.lossyScale.y) : 1f;

        /// <summary>Acelera o frena Caminar y Correr para que los pies sigan el suelo a la velocidad real.</summary>
        private static readonly int HashMuerteAdelante = Animator.StringToHash("MuerteAdelante");
        private static readonly int HashMuerteAtras = Animator.StringToHash("MuerteAtras");
        private static readonly int HashMuerteDesmayo = Animator.StringToHash("MuerteDesmayo");
        private bool rapidezMuerteAplicada;

        private void AjustarRapidez()
        {
            // T-944: la caída al morir puede ir más rápida (solo el jugador lo pide).
            if (animador != null && !Mathf.Approximately(RapidezMuerte, 1f))
            {
                int actual = animador.GetCurrentAnimatorStateInfo(0).shortNameHash;
                if (actual == HashMuerteAdelante || actual == HashMuerteAtras || actual == HashMuerteDesmayo)
                {
                    animador.speed = RapidezMuerte;
                    rapidezMuerteAplicada = true;
                    return;
                }
                if (rapidezMuerteAplicada)
                {
                    animador.speed = 1f;
                    rapidezMuerteAplicada = false;
                }
            }
            // T-683: agachado caminando la rapidez del clip sube con la velocidad (con Shift los pies van más rápido, no solo deslizan).
            if (animador != null && animador.GetCurrentAnimatorStateInfo(0).shortNameHash == HashAgachadoCaminar)
            {
                animador.speed = ReglasAnimacion.RapidezAgachado(velocidad, EscalaClips);
                rapidezAgachadoAplicada = true;
                return;
            }
            if (rapidezAgachadoAplicada && animador != null)
            {
                animador.speed = 1f;
                rapidezAgachadoAplicada = false;
            }
            if (animador == null || (velocidadClipCaminar <= 0f && velocidadClipCorrer <= 0f && Mathf.Approximately(rapidezIdle, 1f)))
            {
                return;
            }
            int estado = animador.GetCurrentAnimatorStateInfo(0).shortNameHash;
            if (estado == HashIdle)
            {
                animador.speed = rapidezIdle;
                return;
            }
            float referencia = estado == HashCaminar ? velocidadClipCaminar : estado == HashCorrer ? velocidadClipCorrer : 0f;
            // T-640: caminar normal (4.5 m/s) ya no es Correr: Caminar se acelera menos que Correr para no verse apresurado.
            float tope = estado == HashCaminar ? Mathf.Min(rapidezMaxima, ReglasAnimacion.RapidezMaximaCaminar) : rapidezMaxima;
            animador.speed = ReglasAnimacion.EscalaVelocidad(velocidad, referencia * EscalaClips, rapidezMinima, tope);
        }

        private const float HolguraPisoCerca = 0.25f;
        private static readonly RaycastHit[] golpesPiso = new RaycastHit[8];

        // Un rayo corto bajo los pies; no cuenta lo propio ni los triggers.
        private bool HayPisoCerca()
        {
            Bounds b = controlador.bounds;
            int n = Physics.RaycastNonAlloc(b.center, Vector3.down, golpesPiso, b.extents.y + HolguraPisoCerca, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (!golpesPiso[i].collider.transform.IsChildOf(controlador.transform))
                {
                    return true;
                }
            }
            return false;
        }

        private void LeerEstadoDelPadre(float dt)
        {
            Transform padre = transform.parent != null ? transform.parent : transform;
            float objetivo;
            Vector3 mundo = Vector3.zero;
            if (controlador != null && controlador.enabled)
            {
                Vector3 v = controlador.velocity;
                mundo = v;
                objetivo = ReglasVisual.VelocidadHorizontal(v.x, v.z);
                // Un CharacterController quieto que no recibe Move cada cuadro reporta isGrounded falso: solo cuenta como aire si de verdad sube o cae.
                // T-944: en la cima de un salto la velocidad vertical también es casi cero: solo cuenta como suelo si de verdad hay piso abajo.
                enSuelo = controlador.isGrounded || (Mathf.Abs(v.y) < VelocidadVerticalEnAire && HayPisoCerca());
            }
            else if (cuerpo != null)
            {
                Vector3 v = cuerpo.linearVelocity;
                mundo = v;
                objetivo = ReglasVisual.VelocidadHorizontal(v.x, v.z);
                enSuelo = Mathf.Abs(v.y) < 0.5f;
            }
            else
            {
                Vector3 delta = padre.position - posicionAnterior;
                mundo = dt > 0f ? delta / dt : Vector3.zero;
                objetivo = dt > 0f ? ReglasVisual.VelocidadHorizontal(delta.x, delta.z) / dt : 0f;
                enSuelo = true;
            }
            // S2: al trepar o agarrarse de un borde el controlador está apagado o quieto: no se pone la animación de caída.
            if (ForzarSuelo)
            {
                enSuelo = true;
            }
            posicionAnterior = padre.position;
            float paso = dt > 0f ? 1f - Mathf.Exp(-suavizadoDireccion * dt) : 1f;
            Vector2 local = ReglasAnimacion.DireccionLocal(mundo, transform.forward);
            velX = Mathf.Lerp(velX, local.x, paso);
            velZ = Mathf.Lerp(velZ, local.y, paso);
            velY = mundo.y;
            float yaw = transform.eulerAngles.y;
            float grados = dt > 0f ? Mathf.DeltaAngle(yawAnterior, yaw) / dt : 0f;
            yawAnterior = yaw;
            giro = Mathf.Lerp(giro, grados, paso);
            velocidad = suavizadoVelocidad > 0f && dt > 0f
                ? Mathf.Lerp(velocidad, objetivo, 1f - Mathf.Exp(-suavizadoVelocidad * dt))
                : objetivo;
            if (enSuelo && ReglasAnimacion.DebeFrenar(velocidadAnterior, velocidad))
            {
                Disparo(IdFrenar);
            }
            velocidadAnterior = velocidad;
        }

        /// <summary>Disparo: desde cobertura usa la animación de asomarse a disparar (alterna las dos).</summary>
        public void Disparar()
        {
            if (cubierto && animador != null && Tiene(IdDisparoCobertura, AnimatorControllerParameterType.Trigger))
            {
                coberturaAlterna = 1 - coberturaAlterna;
                if (Tiene(IdCoberturaTipo, AnimatorControllerParameterType.Int))
                {
                    animador.SetInteger(IdCoberturaTipo, coberturaAlterna);
                }
                animador.SetTrigger(IdDisparoCobertura);
                return;
            }
            LevantarArma();
            Disparo(IdDisparar);
        }

        public void Recargar()
        {
            LevantarArma();
            Disparo(IdRecargar);
        }

        public void Golpear() => Golpear(false);

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
            Poner(IdTipoArma, (int)TipoArmaAnimador);
            Poner(IdCaer, caido);
            Poner(IdCubierto, cubierto);
            Poner(IdAgachado, agachado);
            Poner(IdManejando, manejando);
            Poner(IdViendoTelefono, viendoTelefono);
            Poner(IdEnGuardia, EnGuardia);
            Poner(IdRapidezGolpe, rapidezGolpes);
            if (leerDelPadre)
            {
                Poner(IdVelX, velX);
                Poner(IdVelZ, velZ);
                // T-733: el árbol de apuntar camina hacia donde va; quien no calcula la dirección deja ApuntarX/Z en 0 y apunta quieto.
                Poner(IdApuntarX, velX);
                Poner(IdApuntarZ, velZ);
                Poner(IdVelY, velY);
                Poner(IdGiro, giro);
            }
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
