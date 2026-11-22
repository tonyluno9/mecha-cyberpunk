using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T25: cualquier carro que pega a buena velocidad a un NPC (peatón, pandillero, policía) lo arrolla: recibe daño según la rapidez y sale volando (<see cref="ImpulsoNpc"/>).
    /// Un solo objeto vigila a todos los carros registrados (IMontable en modo Carro); se crea solo al empezar el juego. Al jugador no lo toca (eso es <c>AtropelloJugador</c>).
    /// La rapidez sale del Rigidbody o, si es cinemático (tráfico), de lo que se mueve el carro.
    /// </summary>
    public class AtropelloNpcs : MonoBehaviour
    {
        private const string NombreObjeto = "AtropelloNpcs_T25";
        private const float SegundosEnfriamiento = 4f;
        private const float MargenDelante = 0.35f;
        private const float AlturaCaja = 0.9f;
        private const float MediaAnchoPorOmision = 1.1f;
        private const float MediaLargoPorOmision = 2.3f;
        private const float MaximoMedia = 4f;
        private const int MaxCandidatos = 24;

        private static AtropelloNpcs instancia;

        private readonly Collider[] candidatos = new Collider[MaxCandidatos];
        private readonly Dictionary<Transform, Vector3> posicionesAntes = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, Vector3> medidas = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, float> enfriamiento = new Dictionary<Transform, float>();
        private readonly List<Transform> borrar = new List<Transform>();

        /// <summary>Cuántos NPC se han arrollado desde que empezó el juego (para las pruebas).</summary>
        public static int Arrollados { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            instancia = null;
            Arrollados = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Crear()
        {
            if (instancia != null)
            {
                return;
            }
            GameObject objeto = new GameObject(NombreObjeto);
            objeto.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(objeto);
            instancia = objeto.AddComponent<AtropelloNpcs>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            IReadOnlyList<IMontable> carros = Registro<IMontable>.Todos;
            IJugador jugador = Registro<IJugador>.Actual;
            Transform raizJugador = jugador != null ? jugador.Raiz : null;
            for (int i = 0; i < carros.Count; i++)
            {
                if (!(carros[i] is Component componente) || componente == null || !componente.gameObject.activeInHierarchy || carros[i].Modo != ModoJugador.Carro)
                {
                    continue;
                }
                Transform raiz = componente.transform;
                Vector3 velocidad = Velocidad(componente, dt);
                if (velocidad.magnitude >= ReglaImpulsoAtropello.RapidezMinima)
                {
                    Revisar(raiz, velocidad, raizJugador);
                }
            }
            LimpiarEnfriamientos();
        }

        private Vector3 Velocidad(Component carro, float dt)
        {
            Transform raiz = carro.transform;
            Rigidbody cuerpo = carro.GetComponent<Rigidbody>();
            Vector3 velocidad = Vector3.zero;
            if (cuerpo != null && !cuerpo.isKinematic)
            {
                velocidad = cuerpo.linearVelocity;
            }
            else if (posicionesAntes.TryGetValue(raiz, out Vector3 antes) && dt > 0f)
            {
                velocidad = (raiz.position - antes) / dt;
            }
            posicionesAntes[raiz] = raiz.position;
            return velocidad;
        }

        private void Revisar(Transform carro, Vector3 velocidad, Transform raizJugador)
        {
            Vector3 media = Medidas(carro);
            Vector3 direccion = new Vector3(velocidad.x, 0f, velocidad.z).normalized;
            Vector3 centro = carro.position + Vector3.up * AlturaCaja + direccion * MargenDelante;
            Quaternion giro = Quaternion.LookRotation(direccion, Vector3.up);
            int n = Physics.OverlapBoxNonAlloc(centro, media, candidatos, giro, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = candidatos[i];
                if (c.transform.IsChildOf(carro))
                {
                    continue;
                }
                Transform npc = RaizDeNpc(c, raizJugador);
                if (npc == null || (enfriamiento.TryGetValue(npc, out float hasta) && Time.time < hasta))
                {
                    continue;
                }
                if (ReglaImpulsoAtropello.Arrolla(velocidad, npc.position - carro.position))
                {
                    Arrollar(npc, velocidad);
                }
            }
        }

        /// <summary>La raíz del NPC al que pertenece el collider, o null si no es un NPC arrollable (carro, objeto inerte, el jugador, algo sin visual de personaje).</summary>
        private static Transform RaizDeNpc(Collider c, Transform raizJugador)
        {
            IRecibeDanio recibe = c.GetComponentInParent<IRecibeDanio>();
            if (!(recibe is Component comp) || comp == null)
            {
                return null;
            }
            Transform raiz = comp.transform;
            if (raiz == raizJugador || (raizJugador != null && raiz.IsChildOf(raizJugador)) || comp.GetComponent<IObjetoInerte>() != null
                || raiz.GetComponentInParent<IMontable>() != null || raiz.GetComponent<ImpulsoNpc>() != null)
            {
                return null;
            }
            return raiz.GetComponentInChildren<VisualPersonaje>(true) != null ? raiz : null;
        }

        private Vector3 Medidas(Transform carro)
        {
            if (medidas.TryGetValue(carro, out Vector3 guardada))
            {
                return guardada;
            }
            Vector3 media = new Vector3(MediaAnchoPorOmision, 0.8f, MediaLargoPorOmision);
            Collider[] colliders = carro.GetComponentsInChildren<Collider>();
            bool hay = false;
            Bounds local = new Bounds();
            foreach (Collider col in colliders)
            {
                if (col.isTrigger)
                {
                    continue;
                }
                Bounds b = col.bounds;
                for (int k = 0; k < 8; k++)
                {
                    Vector3 esquina = b.center + Vector3.Scale(b.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1));
                    Vector3 p = carro.InverseTransformPoint(esquina);
                    if (!hay)
                    {
                        local = new Bounds(p, Vector3.zero);
                        hay = true;
                    }
                    else
                    {
                        local.Encapsulate(p);
                    }
                }
            }
            if (hay)
            {
                Vector3 escala = carro.lossyScale;
                media = new Vector3(Mathf.Clamp(local.extents.x * Mathf.Abs(escala.x), 0.6f, MaximoMedia), 0.8f, Mathf.Clamp(local.extents.z * Mathf.Abs(escala.z), 1f, MaximoMedia));
            }
            medidas[carro] = media;
            return media;
        }

        private void LimpiarEnfriamientos()
        {
            if (enfriamiento.Count == 0)
            {
                return;
            }
            borrar.Clear();
            foreach (KeyValuePair<Transform, float> par in enfriamiento)
            {
                if (par.Key == null || Time.time >= par.Value)
                {
                    borrar.Add(par.Key);
                }
            }
            foreach (Transform t in borrar)
            {
                enfriamiento.Remove(t);
            }
        }

        private void Arrollar(Transform npc, Vector3 velocidadCarro)
        {
            enfriamiento[npc] = Time.time + SegundosEnfriamiento;
            Vector3 direccion = new Vector3(velocidadCarro.x, 0f, velocidadCarro.z).normalized;
            bool murio = false;
            IRecibeDanio recibe = npc.GetComponentInChildren<IRecibeDanio>();
            if (recibe == null)
            {
                recibe = npc.GetComponentInParent<IRecibeDanio>();
            }
            if (recibe != null)
            {
                ResultadoDanio resultado = recibe.RecibirDanio(new InfoDanio(ReglaImpulsoAtropello.Danio(velocidadCarro), TipoDanio.Golpe,
                    npc.position + Vector3.up, direccion, null));
                murio = resultado.CausoKO;
            }
            ImpulsoNpc impulso = npc.gameObject.AddComponent<ImpulsoNpc>();
            impulso.Lanzar(ReglaImpulsoAtropello.VelocidadDeVuelo(velocidadCarro), direccion, murio);
            Arrollados++;
            Debug.Log("[IA] T25: " + npc.name + " arrollado a " + new Vector3(velocidadCarro.x, 0f, velocidadCarro.z).magnitude.ToString("F1") + " m/s" + (murio ? " (murió)" : ""));
        }

        // ---- Depuración para probar en Play por eval ----

        /// <summary>Arrolla ahora al NPC <paramref name="npc"/> como si un carro lo golpeara con esa velocidad (m/s). Devuelve false si no es un NPC.</summary>
        public static bool ForzarAtropello(Transform npc, Vector3 velocidadCarro)
        {
            if (npc == null)
            {
                return false;
            }
            Crear();
            Collider colision = npc.GetComponentInChildren<Collider>();
            Transform raiz = (colision != null ? RaizDeNpc(colision, null) : null) ?? npc;
            if (raiz.GetComponent<ImpulsoNpc>() != null || instancia == null)
            {
                return false;
            }
            instancia.Arrollar(raiz, velocidadCarro);
            return true;
        }

        /// <summary>Arrolla al NPC más cercano al jugador (o a la cámara) dentro de <paramref name="radio"/> m, empujándolo hacia donde mira el jugador a 12 m/s. Devuelve su nombre o "" si no hay.</summary>
        public static string ForzarAtropelloCercano(float radio = 25f)
        {
            Crear();
            IJugador jugador = Registro<IJugador>.Actual;
            Transform raizJugador = jugador != null ? jugador.Raiz : null;
            Vector3 origen = raizJugador != null ? raizJugador.position : (Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            Vector3 adelante = raizJugador != null ? raizJugador.forward : Vector3.forward;
            Collider[] cerca = Physics.OverlapSphere(origen, radio, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Transform mejor = null;
            float mejorDistancia = float.MaxValue;
            foreach (Collider c in cerca)
            {
                Transform npc = RaizDeNpc(c, raizJugador);
                if (npc == null || npc.GetComponent<ImpulsoNpc>() != null)
                {
                    continue;
                }
                float d = Vector3.Distance(origen, npc.position);
                if (d < mejorDistancia)
                {
                    mejorDistancia = d;
                    mejor = npc;
                }
            }
            if (mejor == null || instancia == null)
            {
                return "";
            }
            instancia.Arrollar(mejor, adelante.normalized * 12f);
            return mejor.name;
        }

        /// <summary>Cuántos NPC están volando o tirados por un atropello ahora.</summary>
        public static int ContarEnImpulso() => FindObjectsByType<ImpulsoNpc>(FindObjectsSortMode.None).Length;
    }
}
