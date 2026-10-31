using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T-425: al cargar la ciudad, los objetos de decorado chicos (menos de <c>tamanioMaximo</c> m y sin LODGroup, sin física ni personajes)
    /// reciben un LODGroup de un solo nivel que los corta a <c>distanciaCorte</c> m. Baja miles de llamadas de dibujo en la calle sin
    /// tocar el arte. Se crea solo en las escenas de la ciudad (RuntimeInitializeOnLoad), no hay que ponerlo a mano.
    /// </summary>
    public class CulingDecorado : MonoBehaviour
    {
        private static readonly string[] EscenasCiudad = { "Ciudad_Prologo", "Ciudad" };

        [Tooltip("Objetos cuyo lado mayor es menor o igual a esto (m) se cortan por distancia.")]
        [SerializeField, Min(0.1f)] private float tamanioMaximo = 2.5f;
        [Tooltip("Distancia (m) a la que desaparece el decorado chico.")]
        [SerializeField, Min(10f)] private float distanciaCorte = 70f;
        [Tooltip("Campo de visión vertical de referencia (°).")]
        [SerializeField, Range(20f, 110f)] private float campoVision = 60f;

        /// <summary>Cuántos objetos recibieron corte por distancia.</summary>
        public int Cortados { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CrearEnLaCiudad()
        {
            string escena = SceneManager.GetActiveScene().name;
            if (System.Array.IndexOf(EscenasCiudad, escena) < 0 || FindFirstObjectByType<CulingDecorado>() != null)
            {
                return;
            }
            new GameObject("CulingDecorado").AddComponent<CulingDecorado>();
        }

        private void Start()
        {
            Aplicar();
        }

        /// <summary>Recorre la escena y agrega los LODGroup. Se puede llamar una vez por carga.</summary>
        public void Aplicar()
        {
            Cortados = 0;
            foreach (MeshRenderer r in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!Elegible(r))
                {
                    continue;
                }
                Vector3 tamanio = r.bounds.size;
                float mayor = Mathf.Max(tamanio.x, Mathf.Max(tamanio.y, tamanio.z));
                if (!CalculoCortePorDistancia.EsChico(mayor, tamanioMaximo))
                {
                    continue;
                }
                LODGroup grupo = r.gameObject.AddComponent<LODGroup>();
                grupo.SetLODs(new[] { new LOD(CalculoCortePorDistancia.AlturaDeCorte(mayor, distanciaCorte, campoVision), new Renderer[] { r }) });
                grupo.RecalculateBounds();
                Cortados++;
            }
        }

        private static bool Elegible(MeshRenderer r)
        {
            if (r == null || !r.enabled || r.GetComponent<TextMesh>() != null || r.GetComponent<LODGroup>() != null)
            {
                return false;
            }
            MeshFilter filtro = r.GetComponent<MeshFilter>();
            if (filtro == null || filtro.sharedMesh == null)
            {
                return false;
            }
            // Nada que se mueva, tenga vida o ya tenga LOD: carros, personajes, puertas animadas, pisos con LODGroup.
            return r.GetComponentInParent<LODGroup>(true) == null
                && r.GetComponentInParent<Rigidbody>(true) == null
                && r.GetComponentInParent<CharacterController>(true) == null
                && r.GetComponentInParent<Animator>(true) == null;
        }
    }
}
