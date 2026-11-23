using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// T-1722 (T79): un maniquí del campo de tiro. Usa SOLO el visual de un enemigo de Nova (sin IA ni NavMesh: se le quitan los scripts), con una caja de
    /// colisión. Recibe el daño de las balas del jugador por <see cref="IRecibeDanio"/>, muestra el número, cae al llegar a 0 y reaparece a los pocos segundos.
    /// </summary>
    public class ObjetivoTiro : MonoBehaviour, IRecibeDanio
    {
        private const float SegundosCaida = 0.4f;
        private const float GradosCaida = 85f;
        private const float AlturaNumero = 0.3f;
        private const float AnchoMinimoCaja = 0.4f;

        private float vida;
        private float segundosCaido;
        private bool listo;
        private bool caido;
        private Quaternion giroBase;
        private Quaternion giroCaido;
        private BoxCollider caja;
        private Transform visual;

        public TipoObjetivoTiro Tipo { get; private set; }
        public float Vida => vida;
        public float VidaMaxima => ReglaCampoTiro.Vida(Tipo);
        public bool EstaListo => listo;
        public bool EstaCaido => caido;

        /// <summary>Un objetivo recibió daño: (objetivo, daño aplicado, causó caída).</summary>
        public static event Action<ObjetivoTiro, float, bool> Golpeado;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Golpeado = null;

        /// <summary>Crea el objetivo (el visual aparece al cuadro siguiente). <paramref name="prefabVisual"/> puede ser null: entonces es una cápsula.</summary>
        public static ObjetivoTiro Crear(Transform padre, TipoObjetivoTiro tipo, GameObject prefabVisual, Vector3 posicion, Quaternion rotacion)
        {
            GameObject objeto = new GameObject("Objetivo_" + tipo);
            objeto.transform.SetParent(padre, false);
            objeto.transform.SetPositionAndRotation(posicion, Quaternion.identity);
            ObjetivoTiro objetivo = objeto.AddComponent<ObjetivoTiro>();
            objetivo.Tipo = tipo;
            objetivo.vida = ReglaCampoTiro.Vida(tipo);
            objetivo.giroBase = rotacion;
            objetivo.StartCoroutine(objetivo.Armar(prefabVisual));
            return objetivo;
        }

        private IEnumerator Armar(GameObject prefabVisual)
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
            Bounds limites = MedirVisual();
            caja = gameObject.AddComponent<BoxCollider>();
            Vector3 tamano = limites.size;
            caja.size = new Vector3(Mathf.Max(AnchoMinimoCaja, tamano.x), Mathf.Max(AnchoMinimoCaja, tamano.y), Mathf.Max(AnchoMinimoCaja, tamano.z));
            caja.center = transform.InverseTransformPoint(limites.center);
            giroCaido = giroBase * Quaternion.Euler(-GradosCaida, 0f, 0f);
            transform.rotation = giroBase;
            listo = true;
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
            NumeroDanioFlotante.Crear(info.Punto + Vector3.up * AlturaNumero, Mathf.RoundToInt(info.Cantidad).ToString(), cae ? TrazoH10.Coral : TrazoH10.Amarillo);
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

        /// <summary>Lo levanta con la vida llena (reaparecer o REINICIAR OBJETIVOS).</summary>
        public void Reaparecer()
        {
            vida = VidaMaxima;
            caido = false;
            segundosCaido = 0f;
            if (listo)
            {
                transform.rotation = giroBase;
                caja.enabled = true;
            }
        }

        private void Update()
        {
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
