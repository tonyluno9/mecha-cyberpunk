using System.Collections.Generic;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Reloj;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T-1545: la luz de la ciudad como las referencias (K20 a K34) y con presupuesto. Se crea sola en las escenas de la ciudad (RuntimeInitializeOnLoad).
    /// 1) Al cargar: las farolas (luces cálidas bajo un poste o farola) pasan a luz de sodio naranja, y las luces de punto chicas de neón
    ///    (magenta y turquesa) suben su intensidad y su alcance un poco, para que el neón pinte la fachada y el piso mojado.
    /// 2) Cada <c>segundosEntreRevisiones</c> s: deja prendidas solo las luces de punto y de cono más cercanas a la cámara (tope y radio), porque
    ///    la ciudad tiene cientos y URP descarta las que pasan de su límite por cámara sin criterio.
    /// No toca las luces que mueven otros sistemas (interiores, torretas, semáforos, destellos, carros, personajes) ni las que tienen sombra (son pocas).
    /// </summary>
    public class LucesCiudad : MonoBehaviour
    {
        private static readonly string[] EscenasCiudad = { "Ciudad_Prologo", "Ciudad" };
        private static readonly string[] NombresFarola = { "farol", "poste", "lampara" };
        private static readonly string[] NombresAjenos = { "semaforo", "torreta", "faros", "destello", "flash", "muzzle", "explosion", "llama" };
        private const int NivelesDePadres = 4;

        [Header("Farolas de sodio")]
        [Tooltip("Color de la luz de las farolas (sodio de alta presión, naranja cálido).")]
        [SerializeField] private Color colorSodio = new Color(1f, 0.6f, 0.22f);

        [Tooltip("Un cono con al menos esta intensidad bajo un poste es el foco de la calle: siempre pasa a sodio.")]
        [SerializeField, Min(1f)] private float intensidadMinimaFoco = 50f;

        [Header("Neón")]
        [Tooltip("Múltiplo de la intensidad de las luces de punto de neón (turquesa y magenta, de alcance chico).")]
        [SerializeField, Min(0.1f)] private float multiploIntensidadNeon = 4f;
        [Tooltip("Alcance mínimo (m) de esas luces: pintan la fachada y la banqueta mojada de enfrente.")]
        [SerializeField, Min(1f)] private float alcanceNeon = 5f;
        [Tooltip("Las luces de neón con más intensidad que esto ya son luces de local, no letreros: no se tocan.")]
        [SerializeField, Min(0.1f)] private float intensidadMaximaNeon = 2f;

        [Header("Presupuesto de luces")]
        [Tooltip("Máximo de luces de punto y de cono prendidas a la vez (las más cercanas a la cámara).")]
        [SerializeField, Min(8)] private int maximoPrendidas = 96;
        [Tooltip("Distancia (m) más allá del alcance de la luz a la que se apaga.")]
        [SerializeField, Min(0f)] private float distanciaExtra = 45f;
        [Tooltip("Margen (m) para volver a prenderla, y que no parpadee en el límite.")]
        [SerializeField, Min(0f)] private float histeresis = 8f;
        [Tooltip("Cada cuántos segundos se revisa cuáles prender.")]
        [SerializeField, Min(0.05f)] private float segundosEntreRevisiones = 0.25f;

        private readonly List<Light> luces = new List<Light>();
        private Vector3[] posiciones = new Vector3[0];
        private float[] radios = new float[0];
        private bool[] elegible = new bool[0];
        private bool[] estabaPrendida = new bool[0];
        private bool[] prendida = new bool[0];
        private bool[] apagadaPorMi = new bool[0];
        private float[] memoria = new float[0];
        private Camera[] camaras = new Camera[4];
        private Camera camara;
        private float proximaRevision;

        /// <summary>Cuántas luces administra el presupuesto.</summary>
        public int Administradas => luces.Count;

        /// <summary>Cuántas quedaron prendidas en la última revisión.</summary>
        public int Prendidas { get; private set; }

        /// <summary>Cuántas farolas pasaron a sodio.</summary>
        public int Farolas { get; private set; }

        /// <summary>Cuántas luces de neón se reforzaron.</summary>
        public int Neones { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CrearEnLaCiudad()
        {
            string escena = SceneManager.GetActiveScene().name;
            if (System.Array.IndexOf(EscenasCiudad, escena) < 0 || FindFirstObjectByType<LucesCiudad>() != null)
            {
                return;
            }
            new GameObject("LucesCiudad").AddComponent<LucesCiudad>();
        }

        private void Start()
        {
            Preparar();
        }

        /// <summary>Recorre la escena: recolora farolas, refuerza neón y arma la lista del presupuesto. Una vez por carga.</summary>
        public void Preparar()
        {
            luces.Clear();
            Farolas = 0;
            Neones = 0;
            HashSet<Light> ajenas = LucesAjenas();
            Dictionary<Light, LuzNocturna> nocturnas = new Dictionary<Light, LuzNocturna>();
            foreach (LuzNocturna n in FindObjectsByType<LuzNocturna>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (n.Luz != null)
                {
                    nocturnas[n.Luz] = n;
                }
            }
            foreach (Light luz in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (luz == null || luz.type == LightType.Directional || luz.type == LightType.Rectangle || luz.type == LightType.Disc
                    || ajenas.Contains(luz) || !EsFija(luz))
                {
                    continue;
                }
                AjustarColor(luz, nocturnas);
                if (luz.shadows == LightShadows.None)
                {
                    luces.Add(luz);
                }
            }
            Reservar(luces.Count);
            for (int i = 0; i < luces.Count; i++)
            {
                posiciones[i] = luces[i].transform.position;
                radios[i] = CalculoLucesCiudad.RadioPrendida(luces[i].range, distanciaExtra);
                estabaPrendida[i] = luces[i].enabled;
            }
            Debug.Log("[Mundo] LucesCiudad: " + luces.Count + " luces con presupuesto de " + maximoPrendidas + ", " + Farolas + " farolas a sodio, " + Neones + " neones reforzados");
        }

        private void Update()
        {
            if (Time.unscaledTime < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.unscaledTime + segundosEntreRevisiones;
            if (camara == null || !camara.isActiveAndEnabled)
            {
                camara = BuscarCamara();
            }
            if (camara != null)
            {
                Revisar(camara.transform.position);
            }
        }

        // La principal; si no hay (la cámara de la medición no lleva la etiqueta), la activa de mayor profundidad que dibuja en pantalla.
        private Camera BuscarCamara()
        {
            Camera principal = Camera.main;
            if (principal != null)
            {
                return principal;
            }
            if (camaras.Length < Camera.allCamerasCount)
            {
                camaras = new Camera[Camera.allCamerasCount * 2];
            }
            int cantidad = Camera.GetAllCameras(camaras);
            Camera mejor = null;
            for (int i = 0; i < cantidad; i++)
            {
                if (camaras[i].targetTexture == null && (mejor == null || camaras[i].depth > mejor.depth))
                {
                    mejor = camaras[i];
                }
                camaras[i] = null;
            }
            return mejor;
        }

        /// <summary>Un paso del presupuesto con la cámara en <paramref name="posicionCamara"/> (las pruebas lo llaman a mano).</summary>
        public void Revisar(Vector3 posicionCamara)
        {
            int n = luces.Count;
            for (int i = 0; i < n; i++)
            {
                Light luz = luces[i];
                if (luz == null)
                {
                    elegible[i] = false;
                    continue;
                }
                if (apagadaPorMi[i] && luz.enabled)
                {
                    apagadaPorMi[i] = false; // otro sistema la volvió a prender (el reloj al anochecer): se juzga de nuevo
                }
                // Prendida por su dueño, o apagada por mí y que de noche tiene intensidad. Una apagada por otro (de día) no se toca.
                elegible[i] = luz.enabled || (apagadaPorMi[i] && luz.intensity > 0.01f);
                estabaPrendida[i] = luz.enabled;
            }
            Prendidas = CalculoLucesCiudad.Seleccionar(posicionCamara, posiciones, radios, elegible, estabaPrendida, n, maximoPrendidas, histeresis, prendida, memoria);
            for (int i = 0; i < n; i++)
            {
                Light luz = luces[i];
                if (luz == null || !elegible[i])
                {
                    continue;
                }
                if (prendida[i] && apagadaPorMi[i])
                {
                    luz.enabled = true;
                    apagadaPorMi[i] = false;
                }
                else if (!prendida[i] && luz.enabled)
                {
                    luz.enabled = false;
                    apagadaPorMi[i] = true;
                }
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < luces.Count && i < apagadaPorMi.Length; i++)
            {
                if (apagadaPorMi[i] && luces[i] != null && luces[i].intensity > 0.01f)
                {
                    luces[i].enabled = true;
                }
            }
        }

        private void Reservar(int cantidad)
        {
            posiciones = new Vector3[cantidad];
            radios = new float[cantidad];
            elegible = new bool[cantidad];
            estabaPrendida = new bool[cantidad];
            prendida = new bool[cantidad];
            apagadaPorMi = new bool[cantidad];
            memoria = new float[cantidad];
        }

        private void AjustarColor(Light luz, Dictionary<Light, LuzNocturna> nocturnas)
        {
            if (luz.type == LightType.Point && luz.range <= CalculoLucesCiudad.AlcanceMaximoNeon && luz.intensity <= intensidadMaximaNeon
                && CalculoLucesCiudad.EsColorNeon(luz.color))
            {
                if (nocturnas.TryGetValue(luz, out LuzNocturna nocturna))
                {
                    nocturna.EscalarIntensidad(multiploIntensidadNeon);
                }
                else
                {
                    luz.intensity *= multiploIntensidadNeon;
                }
                luz.range = Mathf.Max(luz.range, alcanceNeon);
                Neones++;
                return;
            }
            // El foco de la calle (cono fuerte bajo un poste) es naranja aunque hoy salga blanco azulado (los Luz_S2t); una lámpara chica solo si ya era cálida.
            bool focoDeCalle = luz.type == LightType.Spot && luz.intensity >= intensidadMinimaFoco && EsDeFarola(luz.transform);
            bool lamparaCalida = (luz.type == LightType.Spot || luz.type == LightType.Point) && CalculoLucesCiudad.EsColorCalido(luz.color) && EsDeFarola(luz.transform);
            if (focoDeCalle || lamparaCalida)
            {
                luz.color = colorSodio;
                Farolas++;
            }
        }

        private static bool EsDeFarola(Transform t)
        {
            for (int i = 0; t != null && i < NivelesDePadres; i++, t = t.parent)
            {
                string nombre = t.name.ToLowerInvariant();
                foreach (string clave in NombresFarola)
                {
                    if (nombre.Contains(clave))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Luces que ya maneja otro sistema: no se les cambia el estado.
        private static HashSet<Light> LucesAjenas()
        {
            HashSet<Light> ajenas = new HashSet<Light>();
            foreach (ZonaInterior zona in FindObjectsByType<ZonaInterior>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (zona.Luces == null)
                {
                    continue;
                }
                foreach (Light luz in zona.Luces)
                {
                    if (luz != null)
                    {
                        ajenas.Add(luz);
                    }
                }
            }
            foreach (LuzTorreta torreta in FindObjectsByType<LuzTorreta>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (Light luz in torreta.GetComponentsInChildren<Light>(true))
                {
                    ajenas.Add(luz);
                }
            }
            foreach (SemaforoCiclo semaforo in FindObjectsByType<SemaforoCiclo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                foreach (Light luz in semaforo.GetComponentsInChildren<Light>(true))
                {
                    ajenas.Add(luz);
                }
            }
            return ajenas;
        }

        // Una luz con un script propio (parpadeo, destello, apagar) la maneja ese script; la excepción es el reloj nocturno, que convive con el presupuesto.
        private static bool TieneOtroComponente(Light luz)
        {
            foreach (MonoBehaviour m in luz.GetComponents<MonoBehaviour>())
            {
                if (m != null && !(m is LuzNocturna))
                {
                    return true;
                }
            }
            return false;
        }

        // Solo luces que no se mueven ni son de algo vivo (carros, personajes, armas) ni de un efecto.
        private static bool EsFija(Light luz)
        {
            Transform t = luz.transform;
            if (t.GetComponentInParent<Rigidbody>(true) != null || t.GetComponentInParent<CharacterController>(true) != null
                || t.GetComponentInParent<Animator>(true) != null || t.GetComponentInParent<Camera>(true) != null
                || TieneOtroComponente(luz))
            {
                return false;
            }
            for (int i = 0; t != null && i < NivelesDePadres; i++, t = t.parent)
            {
                string nombre = t.name.ToLowerInvariant();
                foreach (string clave in NombresAjenos)
                {
                    if (nombre.Contains(clave))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
