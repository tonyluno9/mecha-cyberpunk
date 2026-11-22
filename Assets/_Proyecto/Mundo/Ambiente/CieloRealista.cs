using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T14 (T-1605): el domo del shader MechaCyberpunk/CieloRealista sigue a la cámara, recibe el peso de noche del ciclo día/noche (ConfigCicloDia,
    /// con la hora del reloj del mundo) y tiñe las nubes según el barrio donde está la cámara (naranja Suburbios, magenta Neón Alto, azul frío Nova).
    /// No toca RenderSettings ni el ciclo: solo lee la hora. Sin reloj queda de noche, como CieloNubes.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class CieloRealista : MonoBehaviour
    {
        private static readonly int IdNoche = Shader.PropertyToID("_Noche");
        private static readonly int IdTinte = Shader.PropertyToID("_TinteNubes");
        private static readonly int IdLuna = Shader.PropertyToID("_DirLuna");

        [SerializeField] private ConfigRealismo config;
        [SerializeField] private ConfigCicloDia ciclo;

        private MeshRenderer domo;
        private MaterialPropertyBlock bloque;
        private Camera camara;
        private Color tinteActual;
        private bool tinteInicializado;

        /// <summary>Barrio donde estaba la cámara en la última actualización.</summary>
        public BarrioCielo Barrio { get; private set; }

        /// <summary>Tinte de nubes que se está usando (ya suavizado).</summary>
        public Color TinteActual => tinteActual;

        private void Awake()
        {
            domo = GetComponent<MeshRenderer>();
            if (config == null || ciclo == null || domo.sharedMaterial == null)
            {
                Debug.LogError("[Mundo] CieloRealista en '" + name + "' necesita config, ciclo y material.", this);
                enabled = false;
                return;
            }
            bloque = new MaterialPropertyBlock();
            transform.localScale = Vector3.one * (config.RadioDomo * 2f);
        }

        private void LateUpdate()
        {
            if (camara == null || !camara.isActiveAndEnabled)
            {
                camara = Camera.main;
            }
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            Actualizar(camara, reloj != null ? (float?)reloj.Hora : null, Time.deltaTime);
        }

        /// <summary>Tinte objetivo de un barrio (sin suavizar).</summary>
        public Color TinteDe(BarrioCielo barrio)
        {
            switch (barrio)
            {
                case BarrioCielo.Suburbios: return config.TinteSuburbios;
                case BarrioCielo.NeonAlto: return config.TinteNeonAlto;
                case BarrioCielo.Nova: return config.TinteNova;
                default: return config.TinteMercado;
            }
        }

        /// <summary>
        /// Pone el domo en la cámara, el peso de noche de esa hora (sin hora: noche completa) y el tinte del barrio, acercándolo con <paramref name="dt"/>.
        /// Lo usa LateUpdate y el código de editor de las fotos, que no corre Awake.
        /// </summary>
        public void Actualizar(Camera camaraDomo, float? hora, float dt)
        {
            if (domo == null)
            {
                domo = GetComponent<MeshRenderer>();
            }
            if (config == null || ciclo == null || domo == null)
            {
                return;
            }
            if (bloque == null)
            {
                bloque = new MaterialPropertyBlock();
                transform.localScale = Vector3.one * (config.RadioDomo * 2f);
            }
            Vector3 centro = transform.position;
            if (camaraDomo != null)
            {
                centro = camaraDomo.transform.position;
                transform.position = centro;
            }
            Barrio = CalculoCieloRealista.BarrioEn(centro.x, centro.z, config.LimiteSuburbiosX, config.LimiteEsteX, config.LimiteNovaZ);
            Color objetivo = TinteDe(Barrio);
            tinteActual = tinteInicializado ? CalculoCieloRealista.Suavizar(tinteActual, objetivo, config.VelocidadTinte, dt) : objetivo;
            tinteInicializado = true;

            float peso = hora.HasValue ? ciclo.PesoNoche(hora.Value) : 1f;
            domo.GetPropertyBlock(bloque);
            bloque.SetFloat(IdNoche, peso);
            bloque.SetColor(IdTinte, tinteActual);
            bloque.SetVector(IdLuna, config.DireccionLuna());
            domo.SetPropertyBlock(bloque);
        }
    }
}
