using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>Qué hace una persona de fondo en la banqueta (T-510).</summary>
    public enum ModoGente
    {
        /// <summary>De pie: el Idle respira (el pecho, la cabeza y la cadera se mecen un poco, con fase y ritmo propios).</summary>
        Quieto,
        /// <summary>De pie platicando: reproduce un clip en bucle (asentir o discutir) con fase y rapidez propias.</summary>
        Platicar,
        /// <summary>Camina de un punto a otro de la banqueta y vuelve (se detiene un momento en cada extremo).</summary>
        Caminar,
    }

    /// <summary>
    /// Vida para la gente de banqueta de T-508, que antes estaba congelada. Va en la raíz del visual (junto a <see cref="VisualPersonaje"/>).
    /// Caminar mueve al padre (el objeto que envuelve al visual) entre dos puntos del mundo, y <see cref="VisualPersonaje"/> pone Caminar al leer
    /// su velocidad. Nada de esto usa colliders ni física; solo trabaja mientras el cuerpo se ve (60 FPS de noche).
    /// </summary>
    [DisallowMultipleComponent]
    public class GenteDeBanqueta : MonoBehaviour
    {
        [SerializeField] private ModoGente modo = ModoGente.Quieto;
        [Tooltip("Clip en bucle de Platicar (asentir o enojo); cualquier clip Humanoid sirve.")]
        [SerializeField] private AnimationClip clipPlatica;
        [Tooltip("Caminar: extremos del recorrido en el mundo.")]
        [SerializeField] private Vector3 puntoA;
        [SerializeField] private Vector3 puntoB;
        [Tooltip("Caminar: rapidez (m/s).")]
        [Min(0.1f)] [SerializeField] private float velocidad = 1f;
        [Tooltip("Caminar: segundos que se queda parado en cada extremo.")]
        [Min(0f)] [SerializeField] private float pausa = 2f;
        [Tooltip("Platicar: rapidez del clip. Quieto: ritmo de la respiración.")]
        [Min(0.05f)] [SerializeField] private float ritmo = 1f;
        [Tooltip("Fase inicial 0 a 1: desfasa unos de otros.")]
        [Range(0f, 1f)] [SerializeField] private float fase;
        [Tooltip("Quien se mueve al caminar (el objeto que envuelve al visual); vacío = el padre.")]
        [SerializeField] private Transform mover;

        private const float GradosPorSegundoGiro = 220f;
        private const float AmplitudPecho = 1.6f;
        private const float AmplitudCabeza = 2.2f;
        private const float AmplitudCadera = 1.2f;

        private Animator animador;
        private SkinnedMeshRenderer cuerpo;
        private Transform pecho;
        private Transform cabeza;
        private Transform cadera;
        private PlayableGraph grafo;
        private bool hayGrafo;
        private bool haciaB = true;
        private float espera;
        private float t;

        public ModoGente Modo => modo;
        public float Velocidad => velocidad;
        public Vector3 PuntoA => puntoA;
        public Vector3 PuntoB => puntoB;
        public float Fase => fase;
        public float Ritmo => ritmo;
        public AnimationClip ClipPlatica => clipPlatica;
        public bool Platicando => hayGrafo;

        /// <summary>Para los creadores de escena.</summary>
        public void Configurar(ModoGente nuevoModo, float nuevaFase, float nuevoRitmo, AnimationClip clip, Vector3 a, Vector3 b, float rapidez, float pausaExtremos, Transform quienSeMueve)
        {
            modo = nuevoModo;
            fase = Mathf.Repeat(nuevaFase, 1f);
            ritmo = Mathf.Max(0.05f, nuevoRitmo);
            clipPlatica = clip;
            puntoA = a;
            puntoB = b;
            velocidad = Mathf.Max(0.1f, rapidez);
            pausa = pausaExtremos;
            mover = quienSeMueve;
            if (Application.isPlaying && isActiveAndEnabled)
            {
                // Se configuró con el objeto ya vivo (pruebas): se vuelve a arrancar con lo nuevo.
                OnDisable();
                OnEnable();
            }
        }

        private void Awake()
        {
            animador = GetComponentInChildren<Animator>(true);
            cuerpo = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (mover == null)
            {
                mover = transform.parent;
            }
            if (animador != null && animador.isHuman)
            {
                pecho = animador.GetBoneTransform(HumanBodyBones.Chest);
                if (pecho == null)
                {
                    pecho = animador.GetBoneTransform(HumanBodyBones.Spine);
                }
                cabeza = animador.GetBoneTransform(HumanBodyBones.Head);
                cadera = animador.GetBoneTransform(HumanBodyBones.Hips);
            }
        }

        private void OnEnable()
        {
            t = fase * 40f;
            if (modo == ModoGente.Platicar)
            {
                IniciarPlatica();
            }
            else if (modo == ModoGente.Caminar && mover != null)
            {
                // Arranca en un punto cualquiera del recorrido y tras una espera distinta: ninguno camina igual que otro.
                mover.position = Vector3.Lerp(puntoA, puntoB, fase);
                haciaB = fase < 0.5f;
                espera = fase * pausa;
                Vector3 d = (haciaB ? puntoB : puntoA) - mover.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.01f)
                {
                    mover.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
                }
            }
        }

        private void OnDisable()
        {
            if (hayGrafo)
            {
                grafo.Destroy();
                hayGrafo = false;
            }
        }

        private void IniciarPlatica()
        {
            if (animador == null || clipPlatica == null || hayGrafo)
            {
                return;
            }
            VisualPersonaje visual = GetComponent<VisualPersonaje>();
            if (visual != null)
            {
                visual.enabled = false; // el Animator ya no usa el controlador: lo maneja este clip
            }
            grafo = PlayableGraph.Create("Gente_" + name);
            grafo.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            AnimationPlayableOutput salida = AnimationPlayableOutput.Create(grafo, "Platica", animador);
            AnimationClipPlayable reproductor = AnimationClipPlayable.Create(grafo, clipPlatica);
            reproductor.SetTime(fase * clipPlatica.length);
            reproductor.SetSpeed(ritmo);
            salida.SetSourcePlayable(reproductor);
            grafo.Play();
            hayGrafo = true;
        }

        private void Update()
        {
            if (modo == ModoGente.Caminar && mover != null)
            {
                Caminar(Time.deltaTime);
            }
        }

        private void Caminar(float dt)
        {
            if (espera > 0f)
            {
                espera -= dt;
                return;
            }
            Vector3 meta = haciaB ? puntoB : puntoA;
            Vector3 hacia = meta - mover.position;
            hacia.y = 0f;
            float distancia = hacia.magnitude;
            if (distancia < 0.05f)
            {
                haciaB = !haciaB;
                espera = pausa;
                return;
            }
            Vector3 dir = hacia / distancia;
            Quaternion mira = Quaternion.LookRotation(dir, Vector3.up);
            mover.rotation = Quaternion.RotateTowards(mover.rotation, mira, GradosPorSegundoGiro * dt);
            if (Quaternion.Angle(mover.rotation, mira) > 40f)
            {
                return; // primero gira, luego avanza
            }
            mover.position += dir * Mathf.Min(distancia, velocidad * dt);
        }

        private void LateUpdate()
        {
            if (modo != ModoGente.Quieto || animador == null || cuerpo == null || (!cuerpo.isVisible && animador.cullingMode != AnimatorCullingMode.AlwaysAnimate))
            {
                return;
            }
            // Respira: el reposo es una pose fija, así que el Animator la reescribe cada cuadro y esto no se acumula.
            t += Time.deltaTime * ritmo;
            float respiro = Mathf.Sin(t * Mathf.PI * 2f * 0.25f);
            float vaiven = Mathf.Sin(t * Mathf.PI * 2f * 0.09f + 1.7f);
            if (pecho != null)
            {
                pecho.localRotation *= Quaternion.Euler(respiro * AmplitudPecho, 0f, 0f);
            }
            if (cabeza != null)
            {
                cabeza.localRotation *= Quaternion.Euler(-respiro * 0.5f * AmplitudPecho, vaiven * AmplitudCabeza, 0f);
            }
            if (cadera != null)
            {
                cadera.localRotation *= Quaternion.Euler(0f, 0f, vaiven * AmplitudCadera);
            }
        }
    }
}
