using System;
using System.Collections;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// T-1722 (T79): un maniquí del campo de tiro. Usa SOLO el visual de un enemigo de Nova (sin IA ni NavMesh: se le quitan los scripts), con una caja de
    /// colisión. Recibe el daño de las balas del jugador por <see cref="IRecibeDanio"/>, cae al llegar a 0 y reaparece a los pocos segundos.
    /// T86: muestra su barra de vida (<see cref="BarraVidaObjetivo"/>) y ningún número de daño.
    /// T85: toca su idle (el AnimatorController del Nova, cuyo estado por defecto es Idle), se ve holográfico y aparece con un destello que se asienta en 0.5 s.
    /// </summary>
    public class ObjetivoTiro : MonoBehaviour, IRecibeDanio
    {
        private const float SegundosCaida = 0.4f;
        private const float GradosCaida = 85f;
        private const float SegundosAparicion = 0.5f;
        private const float AlturaExtraBarra = 0.35f;
        private const float AnchoMinimoCaja = 0.4f;
        private static readonly Color ColorDestello = new Color(0.85f, 1f, 1f, 1f);

        private float vida;
        private float segundosCaido;
        private bool listo;
        private bool caido;
        private Quaternion giroBase;
        private Quaternion giroCaido;
        private BoxCollider caja;
        private Transform visual;
        private Animator animador;
        private Material materialHolo;
        private Color colorHolo;
        private float edadAparicion = 99f;
        private GameObject plataforma;
        private BarraVidaObjetivo barra;

        public TipoObjetivoTiro Tipo { get; private set; }
        public float Vida => vida;
        public float VidaMaxima => ReglaCampoTiro.Vida(Tipo);
        public bool EstaListo => listo;
        public bool EstaCaido => caido;
        /// <summary>Altura (m) de la plataforma bajo el objetivo (0 si no lleva).</summary>
        public float AlturaPlataforma { get; private set; }
        /// <summary>Nombre del clip que suena ahora en el Animator (el idle del Nova); vacío si no hay Animator con controlador.</summary>
        public string ClipActual
        {
            get
            {
                if (animador == null || animador.runtimeAnimatorController == null)
                {
                    return string.Empty;
                }
                AnimatorClipInfo[] clips = animador.GetCurrentAnimatorClipInfo(0);
                return clips.Length > 0 && clips[0].clip != null ? clips[0].clip.name : string.Empty;
            }
        }
        public bool TieneAnimador => animador != null && animador.runtimeAnimatorController != null;
        /// <summary>True si el visual usa el material holográfico.</summary>
        public bool Holografico => materialHolo != null;
        /// <summary>Opacidad actual del material holográfico (0 a 1): empieza en 1 (destello) y se asienta en la del material.</summary>
        public float AlfaHolo => materialHolo != null ? materialHolo.GetColor("_BaseColor").a : 0f;
        public bool BarraVisible => barra != null && barra.Visible;
        public float FraccionBarra => barra != null ? barra.Fraccion : 0f;

        /// <summary>Un objetivo recibió daño: (objetivo, daño aplicado, causó caída).</summary>
        public static event Action<ObjetivoTiro, float, bool> Golpeado;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Golpeado = null;

        /// <summary>Crea el objetivo (el visual aparece al cuadro siguiente). <paramref name="prefabVisual"/> puede ser null: entonces es una cápsula.</summary>
        public static ObjetivoTiro Crear(Transform padre, TipoObjetivoTiro tipo, GameObject prefabVisual, Vector3 posicion, Quaternion rotacion, DatosCampoTiro datos = null)
        {
            GameObject objeto = new GameObject("Objetivo_" + tipo);
            objeto.transform.SetParent(padre, false);
            objeto.transform.SetPositionAndRotation(posicion, Quaternion.identity);
            ObjetivoTiro objetivo = objeto.AddComponent<ObjetivoTiro>();
            objetivo.Tipo = tipo;
            objetivo.vida = ReglaCampoTiro.Vida(tipo);
            objetivo.giroBase = rotacion;
            objetivo.AlturaPlataforma = ReglaCampoTiro.AlturaPlataforma(tipo);
            if (objetivo.AlturaPlataforma > 0f)
            {
                // Plataforma holográfica aparte (no hija: el objetivo gira al caer). Los bajos, como el perro, quedan a la vista por encima de los costales.
                objetivo.plataforma = SalaCampoTiro.CrearPlataforma(padre, posicion - Vector3.up * objetivo.AlturaPlataforma, objetivo.AlturaPlataforma, datos);
            }
            objetivo.StartCoroutine(objetivo.Armar(prefabVisual, datos));
            return objetivo;
        }

        private IEnumerator Armar(GameObject prefabVisual, DatosCampoTiro datos)
        {
            GameObject sosten = new GameObject("Sosten");
            sosten.SetActive(false);
            if (prefabVisual != null)
            {
                GameObject copia = Instantiate(prefabVisual, sosten.transform);
                copia.name = "Visual";
                visual = copia.transform;
                // Sin scripts: es un maniquí. Con el sostén apagado no corre ni su Awake; los scripts se destruyen al final del cuadro.
                foreach (MonoBehaviour guion in copia.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    Destroy(guion);
                }
                foreach (Collider choque in copia.GetComponentsInChildren<Collider>(true))
                {
                    Destroy(choque);
                }
            }
            else
            {
                GameObject capsula = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsula.transform.SetParent(sosten.transform, false);
                capsula.transform.localPosition = Vector3.up;
                Destroy(capsula.GetComponent<Collider>());
                visual = capsula.transform;
            }
            yield return null;
            if (this == null)
            {
                Destroy(sosten);
                yield break;
            }
            visual.SetParent(transform, false);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            Destroy(sosten);
            PonerIdle(datos);
            PonerHolo(datos);
            Bounds limites = MedirVisual();
            caja = gameObject.AddComponent<BoxCollider>();
            Vector3 tamano = limites.size;
            caja.size = new Vector3(Mathf.Max(AnchoMinimoCaja, tamano.x), Mathf.Max(AnchoMinimoCaja, tamano.y), Mathf.Max(AnchoMinimoCaja, tamano.z));
            caja.center = transform.InverseTransformPoint(limites.center);
            giroCaido = giroBase * Quaternion.Euler(-GradosCaida, 0f, 0f);
            transform.rotation = giroBase;
            barra = BarraVidaObjetivo.Crear(transform, Mathf.Max(0.8f, limites.max.y - transform.position.y) + AlturaExtraBarra);
            barra.Poner(1f, true);
            listo = true;
        }

        // Su idle: el AnimatorController del Nova tiene Idle como estado por defecto. Sin esto el Animator queda sin controlador y el maniquí en pose T.
        private void PonerIdle(DatosCampoTiro datos)
        {
            RuntimeAnimatorController control = datos != null ? datos.Control(Tipo) : null;
            animador = visual.GetComponentInChildren<Animator>(true);
            if (animador == null)
            {
                return;
            }
            if (control != null)
            {
                animador.runtimeAnimatorController = control;
            }
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animador.applyRootMotion = false;
            animador.enabled = true;
        }

        // Material holográfico propio (para animar el destello) en todos los dibujos del visual. Se queda así: el objetivo es un holograma.
        private void PonerHolo(DatosCampoTiro datos)
        {
            if (datos == null || datos.MaterialHolo == null)
            {
                return;
            }
            materialHolo = new Material(datos.MaterialHolo);
            colorHolo = datos.MaterialHolo.GetColor("_BaseColor");
            foreach (Renderer dibujo in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (!(dibujo is MeshRenderer) && !(dibujo is SkinnedMeshRenderer))
                {
                    continue;
                }
                Material[] lista = new Material[Mathf.Max(1, dibujo.sharedMaterials.Length)];
                for (int i = 0; i < lista.Length; i++)
                {
                    lista[i] = materialHolo;
                }
                dibujo.sharedMaterials = lista;
                dibujo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                dibujo.receiveShadows = false;
            }
            edadAparicion = 0f;
            AplicarAparicion(0f);
        }

        // t=0: destello casi blanco y opaco; t=1: cian translúcido asentado.
        private void AplicarAparicion(float t)
        {
            if (materialHolo == null)
            {
                return;
            }
            float suave = 1f - (1f - t) * (1f - t);
            Color color = Color.Lerp(ColorDestello, colorHolo, suave);
            color.a = Mathf.Lerp(1f, colorHolo.a, suave);
            materialHolo.SetColor("_BaseColor", color);
        }

        private void OnDestroy()
        {
            if (materialHolo != null)
            {
                Destroy(materialHolo);
            }
            if (plataforma != null)
            {
                Destroy(plataforma);
            }
        }

        private Bounds MedirVisual()
        {
            Renderer[] dibujos = visual.GetComponentsInChildren<Renderer>(true);
            Bounds limites = new Bounds(transform.position + Vector3.up, new Vector3(0.6f, 2f, 0.6f));
            bool primero = true;
            foreach (Renderer dibujo in dibujos)
            {
                if (!(dibujo is MeshRenderer) && !(dibujo is SkinnedMeshRenderer))
                {
                    continue;
                }
                if (primero)
                {
                    limites = dibujo.bounds;
                    primero = false;
                }
                else
                {
                    limites.Encapsulate(dibujo.bounds);
                }
            }
            return limites;
        }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (!listo || caido || info.Cantidad <= 0f)
            {
                return ResultadoDanio.Ignorado;
            }
            float aplicado = Mathf.Min(vida, info.Cantidad);
            vida -= aplicado;
            bool cae = vida <= 0f;
            ResultadoDanio resultado = new ResultadoDanio(aplicado, TipoBloqueo.Ninguno, cae);
            if (barra != null)
            {
                barra.Poner(vida / VidaMaxima, !cae);
            }
            EventosPelea.PublicarImpacto(null, info, resultado);
            Golpeado?.Invoke(this, aplicado, cae);
            if (cae)
            {
                Caer();
            }
            return resultado;
        }

        private void Caer()
        {
            caido = true;
            segundosCaido = 0f;
            if (caja != null)
            {
                caja.enabled = false;
            }
        }

        /// <summary>Lo levanta con la vida llena (reaparecer o REINICIAR OBJETIVOS), con el destello holográfico otra vez.</summary>
        public void Reaparecer()
        {
            vida = VidaMaxima;
            caido = false;
            segundosCaido = 0f;
            if (listo)
            {
                transform.rotation = giroBase;
                caja.enabled = true;
                barra?.Poner(1f, true);
                edadAparicion = 0f;
            }
        }

        private void Update()
        {
            if (materialHolo != null && edadAparicion < SegundosAparicion)
            {
                edadAparicion += Time.deltaTime;
                AplicarAparicion(Mathf.Clamp01(edadAparicion / SegundosAparicion));
            }
            if (!caido)
            {
                return;
            }
            segundosCaido += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(giroBase, giroCaido, Mathf.Clamp01(segundosCaido / SegundosCaida));
            if (ReglaCampoTiro.DebeReaparecer(segundosCaido))
            {
                Reaparecer();
            }
        }
    }
}
