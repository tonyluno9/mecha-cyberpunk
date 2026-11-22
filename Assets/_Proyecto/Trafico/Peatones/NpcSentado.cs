using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MechaCyberpunk.Trafico.Peatones
{
    /// <summary>
    /// T-1632 (T18): un civil sentado en una banca con el clip de sentado del jugador (PJ01_Sentado, Humanoid, retargeteado). Va en la raíz de un Visual_* (con su Animator)
    /// que NO es un <see cref="PeatonMundo"/>, así que no cuenta en el pool de peatones. Se alinea solo: mide la cadera en la pose del clip (PlayableGraph evaluado a mano)
    /// y mueve la raíz para que la cadera quede sobre el asiento. Al oír un disparo (<see cref="EventosRuido.DisparoConRadio"/>) o una muerte cercana se asusta, se levanta
    /// (no hay clip de levantarse: salta al clip de huir) y corre en dirección contraria; al terminar la huida se apaga y no vuelve.
    /// </summary>
    public class NpcSentado : MonoBehaviour
    {
        /// <summary>Cuánto sube la cadera (hueso Hips) sobre la superficie del asiento (m).</summary>
        public const float CaderaSobreAsiento = 0.08f;
        /// <summary>Altura de la superficie del asiento de PZ01_Banca y Banca_Sentable sobre su origen (m).</summary>
        public const float AlturaAsientoBanca = 0.41f;
        /// <summary>Radio con el que oyen una muerte (m), como PeatonMundo.RadioVerMuerte.</summary>
        public const float RadioMuerte = 15f;
        public const float VelocidadHuida = 4.2f;
        public const float SegundosHuida = 8f;
        public const float GradosMirarseEnBanca = 25f;
        private const int MuestrasMedicion = 6;
        private const float SegundosSubirAlPiso = 0.3f;

        public enum Fase { Sentado, Susto, Huyendo, Terminado }

        [Tooltip("Transform del asiento: el origen va al piso, el frente es -forward (como PZ01_Banca y Banca_Sentable).")]
        [SerializeField] private Transform ancla;
        [Tooltip("Corrimiento a lo largo del asiento desde el ancla (m, + a la derecha).")]
        [SerializeField] private float lateral;
        [Tooltip("Altura de la superficie del asiento sobre el origen del ancla (m). Bancas: 0.41.")]
        [SerializeField] private float alturaAsiento = AlturaAsientoBanca;
        [Tooltip("True: el frente es -forward del ancla (bancas). False: +forward.")]
        [SerializeField] private bool frenteMenosZ = true;
        [Tooltip("El objeto del asiento interactuable (AsientoInteractuable) para marcarlo ocupado; opcional.")]
        [SerializeField] private GameObject asientoOcupable;
        [SerializeField] private AnimationClip clipSentado;
        [SerializeField] private AnimationClip clipSusto;
        [SerializeField] private AnimationClip clipHuir;
        [Tooltip("Dónde empieza el clip de sentado (0 a 1 de su duración): así no respiran todos igual.")]
        [Range(0f, 1f)] [SerializeField] private float desfase;
        [Tooltip("Rapidez del clip de sentado (1 = normal).")]
        [SerializeField] private float rapidez = 1f;
        [Tooltip("Giro extra sobre el frente del asiento (grados).")]
        [SerializeField] private float giroExtra;
        [Tooltip("Quien comparte la banca: se miran un poco.")]
        [SerializeField] private NpcSentado companero;

        private Animator animador;
        private PlayableGraph grafo;
        private AnimationClipPlayable playable;
        private AnimationClip clipActual;
        private Fase fase = Fase.Sentado;
        private bool alineado;
        private bool reaccionPendiente;
        private float reaccionEn;
        private Vector3 ruidoPos;
        private float faseDesde;
        private Vector3 direccionHuida;
        private bool huyendoCorrer;
        private float alturaInicioSubida;
        private float spreadCadera;

        public Fase FaseActual => fase;
        public bool EstaSentado => fase == Fase.Sentado;
        public Transform Ancla => ancla;
        public NpcSentado Companero => companero;
        /// <summary>Cuánto se movió la cadera entre las muestras del clip al alinear (m): si es grande, el clip tiene movimiento de más.</summary>
        public float VariacionCaderaClip => spreadCadera;

        /// <summary>Superficie del asiento donde se sienta (mundo).</summary>
        public Vector3 PosicionAsiento => ancla != null
            ? ancla.position + ancla.right * lateral + Vector3.up * alturaAsiento
            : transform.position;

        /// <summary>Hacia dónde mira quien se sienta (horizontal).</summary>
        public Vector3 FrenteAsiento
        {
            get
            {
                Vector3 f = ancla != null ? Vector3.ProjectOnPlane(frenteMenosZ ? -ancla.forward : ancla.forward, Vector3.up) : Vector3.forward;
                return f.sqrMagnitude < 1e-6f ? Vector3.forward : f.normalized;
            }
        }

        /// <summary>Cadera actual (hueso Hips) en el mundo, o null sin Animator humanoide.</summary>
        public Vector3? PosicionCadera
        {
            get
            {
                Transform cadera = animador != null && animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.Hips) : null;
                return cadera != null ? cadera.position : (Vector3?)null;
            }
        }

        /// <summary>Distancia (m) de la cadera al punto que debería ocupar sobre el asiento. -1 si no hay cadera.</summary>
        public float DiferenciaCadera
        {
            get
            {
                Vector3? c = PosicionCadera;
                return c.HasValue ? Vector3.Distance(c.Value, PosicionAsiento + Vector3.up * CaderaSobreAsiento) : -1f;
            }
        }

        /// <summary>Altura (m sobre el piso del asiento) del pie más bajo; null sin Animator humanoide.</summary>
        public float? AlturaPieMasBajo
        {
            get
            {
                if (animador == null || !animador.isHuman || ancla == null)
                {
                    return null;
                }
                Transform izq = animador.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform der = animador.GetBoneTransform(HumanBodyBones.RightFoot);
                if (izq == null || der == null)
                {
                    return null;
                }
                return Mathf.Min(izq.position.y, der.position.y) - ancla.position.y;
            }
        }

        /// <summary>Para el editor y las pruebas: deja todo configurado (los clips pueden ser null: queda sin animar pero alineado por la tabla).</summary>
        public void Configurar(Transform elAncla, float elLateral, bool elFrenteMenosZ, GameObject asiento, AnimationClip sentado, AnimationClip susto, AnimationClip huir,
            float elDesfase, float laRapidez, float elGiro)
        {
            ancla = elAncla;
            lateral = elLateral;
            frenteMenosZ = elFrenteMenosZ;
            asientoOcupable = asiento;
            clipSentado = sentado;
            clipSusto = susto;
            clipHuir = huir;
            desfase = Mathf.Clamp01(elDesfase);
            rapidez = Mathf.Max(0.5f, laRapidez);
            giroExtra = elGiro;
        }

        public void Acompanar(NpcSentado otro) => companero = otro;

        /// <summary>Altura de la superficie del asiento sobre el origen del ancla (taburetes y sillas; las bancas usan 0.41).</summary>
        public void FijarAlturaAsiento(float altura) => alturaAsiento = Mathf.Max(0.05f, altura);

        /// <summary>Altura de la superficie del asiento sobre el origen del ancla (m).</summary>
        public float AlturaAsiento => alturaAsiento;

        private void OnEnable()
        {
            EventosRuido.DisparoConRadio += AlOirDisparo;
            EventosRuido.Muerte += AlOirMuerte;
            if (alineado && fase == Fase.Sentado)
            {
                IniciarClip(clipSentado, rapidez, desfase * (clipSentado != null ? clipSentado.length : 0f));
            }
        }

        private void OnDisable()
        {
            EventosRuido.DisparoConRadio -= AlOirDisparo;
            EventosRuido.Muerte -= AlOirMuerte;
            DestruirGrafo();
        }

        private void Start()
        {
            animador = GetComponentInChildren<Animator>(true);
            if (ancla == null)
            {
                return;
            }
            Alinear();
            if (asientoOcupable != null)
            {
                asientoOcupable.SendMessage("Ocupar", gameObject, SendMessageOptions.DontRequireReceiver);
            }
        }

        /// <summary>Gira hacia el frente del asiento (y hacia el compañero) y baja o sube la raíz hasta que la cadera quede sobre el asiento.</summary>
        public void Alinear()
        {
            if (animador == null)
            {
                animador = GetComponentInChildren<Animator>(true);
            }
            Vector3 frente = FrenteAsiento;
            float giro = giroExtra;
            if (companero != null && companero.ancla != null)
            {
                float angulo = Vector3.SignedAngle(frente, Vector3.ProjectOnPlane(companero.PosicionAsiento - PosicionAsiento, Vector3.up), Vector3.up);
                giro = angulo >= 0f ? GradosMirarseEnBanca : -GradosMirarseEnBanca;
            }
            transform.rotation = Quaternion.AngleAxis(giro, Vector3.up) * Quaternion.LookRotation(frente, Vector3.up);
            // Aproximación por la tabla (asiento 0.46 m sobre el origen del clip NPC_Sentado_Banca) por si no hay clip o Animator.
            transform.position = PosicionAsiento - Vector3.up * 0.46f;
            float t0 = clipSentado != null ? desfase * clipSentado.length : 0f;
            if (clipSentado != null && animador != null)
            {
                IniciarClip(clipSentado, rapidez, t0);
                if (animador.isHuman)
                {
                    MedirYCorregir(t0);
                }
            }
            alineado = true;
            faseDesde = Time.time;
        }

        private void MedirYCorregir(float t0)
        {
            Transform cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            if (cadera == null || !grafo.IsValid())
            {
                return;
            }
            Vector3 suma = Vector3.zero;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int k = 0; k < MuestrasMedicion; k++)
            {
                playable.SetTime(clipSentado.length * k / MuestrasMedicion);
                grafo.Evaluate(0f);
                Vector3 p = cadera.position;
                suma += p;
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }
            spreadCadera = maxY - minY;
            Vector3 media = suma / MuestrasMedicion;
            transform.position += PosicionAsiento + Vector3.up * CaderaSobreAsiento - media;
            playable.SetTime(t0);
            grafo.Evaluate(0f);
        }

        private void IniciarClip(AnimationClip clip, float velocidad, float tiempo)
        {
            if (animador == null || clip == null)
            {
                return;
            }
            DestruirGrafo();
            animador.applyRootMotion = false;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            playable = AnimationPlayableUtilities.PlayClip(animador, clip, out grafo);
            playable.SetSpeed(Mathf.Max(0.01f, velocidad));
            playable.SetTime(tiempo);
            clipActual = clip;
        }

        private void DestruirGrafo()
        {
            if (grafo.IsValid())
            {
                grafo.Destroy();
            }
            clipActual = null;
        }

        /// <summary>Oye un disparo en <paramref name="posicion"/> con ese radio de oído (T11): si está dentro, se asusta y huye.</summary>
        public void AlOirDisparo(Vector3 posicion, float radio) => Oir(posicion, radio);

        public void AlOirMuerte(Vector3 posicion) => Oir(posicion, RadioMuerte);

        private void Oir(Vector3 posicion, float radio)
        {
            if (fase != Fase.Sentado || reaccionPendiente || !isActiveAndEnabled)
            {
                return;
            }
            if ((posicion - transform.position).sqrMagnitude > radio * radio)
            {
                return;
            }
            reaccionPendiente = true;
            ruidoPos = posicion;
            // Que no se levanten todos en el mismo cuadro: 0.05 a 0.5 s según su posición.
            float azar = Mathf.Abs(Mathf.Sin(transform.position.x * 12.9898f + transform.position.z * 78.233f));
            reaccionEn = Time.time + 0.05f + azar * 0.45f;
        }

        private void Update()
        {
            switch (fase)
            {
                case Fase.Sentado:
                    Repetir(clipActual);
                    if (reaccionPendiente && Time.time >= reaccionEn)
                    {
                        EmpezarSusto();
                    }
                    break;
                case Fase.Susto:
                    SubirAlPiso();
                    if (Time.time - faseDesde >= (clipSusto != null ? Mathf.Min(clipSusto.length, 0.7f) : 0.3f))
                    {
                        EmpezarHuida();
                    }
                    break;
                case Fase.Huyendo:
                    Huir();
                    break;
            }
        }

        private void Repetir(AnimationClip clip)
        {
            if (clip != null && grafo.IsValid() && playable.IsValid() && playable.GetTime() >= clip.length)
            {
                playable.SetTime(playable.GetTime() % clip.length);
            }
        }

        private void EmpezarSusto()
        {
            fase = Fase.Susto;
            faseDesde = Time.time;
            alturaInicioSubida = transform.position.y;
            if (asientoOcupable != null)
            {
                asientoOcupable.SendMessage("Liberar", SendMessageOptions.DontRequireReceiver);
            }
            Vector3 haciaRuido = Vector3.ProjectOnPlane(ruidoPos - transform.position, Vector3.up);
            if (haciaRuido.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(haciaRuido.normalized, Vector3.up);
            }
            Vector3 lejos = Vector3.ProjectOnPlane(transform.position - ruidoPos, Vector3.up);
            direccionHuida = lejos.sqrMagnitude > 0.01f ? lejos.normalized : FrenteAsiento;
            if (clipSusto != null)
            {
                IniciarClip(clipSusto, 1f, 0f);
            }
        }

        private void EmpezarHuida()
        {
            fase = Fase.Huyendo;
            faseDesde = Time.time;
            huyendoCorrer = clipHuir != null;
            if (huyendoCorrer)
            {
                IniciarClip(clipHuir, 1f, 0f);
            }
        }

        private float PisoBase => ancla != null ? ancla.position.y : transform.position.y;

        private void SubirAlPiso()
        {
            float u = Mathf.Clamp01((Time.time - faseDesde) / SegundosSubirAlPiso);
            Vector3 p = transform.position;
            p.y = Mathf.Lerp(alturaInicioSubida, PisoBase, u);
            transform.position = p;
        }

        private void Huir()
        {
            Repetir(clipActual);
            Vector3 direccion = LibreDeObstaculos(direccionHuida);
            Quaternion meta = Quaternion.LookRotation(direccion, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, meta, 540f * Time.deltaTime);
            Vector3 p = transform.position + transform.forward * (VelocidadHuida * Time.deltaTime);
            float piso = PisoBase;
            Vector3 horizontal = Vector3.ProjectOnPlane(p - (ancla != null ? ancla.position : p), Vector3.up);
            if (horizontal.magnitude > 1.5f && Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out RaycastHit golpe, 3f, ~0, QueryTriggerInteraction.Ignore))
            {
                piso = golpe.point.y;
            }
            p.y = Mathf.MoveTowards(p.y, piso, 6f * Time.deltaTime);
            transform.position = p;
            if (Time.time - faseDesde >= SegundosHuida)
            {
                fase = Fase.Terminado;
                gameObject.SetActive(false); // no vuelve: otro civil ya habrá pasado por ahí
            }
        }

        private Vector3 LibreDeObstaculos(Vector3 deseada)
        {
            float[] giros = { 0f, 45f, -45f, 90f, -90f, 135f, -135f };
            Vector3 origen = transform.position + Vector3.up * 0.9f;
            foreach (float g in giros)
            {
                Vector3 d = Quaternion.AngleAxis(g, Vector3.up) * deseada;
                if (!Physics.Raycast(origen, d, 0.9f, ~0, QueryTriggerInteraction.Ignore))
                {
                    return d;
                }
            }
            return deseada;
        }
    }
}
