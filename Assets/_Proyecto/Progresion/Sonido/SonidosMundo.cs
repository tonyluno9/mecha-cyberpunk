using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Sonido
{
    /// <summary>
    /// T-1535: los sonidos de ambiente que viven en un lugar del mundo (interiores, transformadores, aires, cámaras, refrigeradores, expendedoras, tele, bocina).
    /// Recorre la escena una vez (y otra más tarde, para lo que se coloca en ejecución) buscando por nombre las anclas de <see cref="TablaEmisoresMundo"/>,
    /// pone un punto de sonido en cada una y, cada medio segundo, deja sonar solo las más cercanas al jugador (<see cref="ReglaFuentesMundo"/>): así
    /// los cientos de postes, aires y cámaras de la ciudad no llenan las 12 voces del gestor. No toca la escena guardada: todo se crea en ejecución.
    /// La crea <see cref="ArranqueSonidosT1535"/> sola cuando hay un gestor de sonidos.
    /// </summary>
    public class SonidosMundo : MonoBehaviour
    {
        private const int AnclasPorCuadro = 4000;
        private const float RadioPorDefecto = 25f; // m: para los IDs sin distancia en su política (la bocina de la fiesta)

        private class Emisor
        {
            public Transform punto;
            public string id;
            public int idNumero;
            public TablaEmisoresMundo.Definicion definicion;
            public int bucle = Sonidos.SinBucle;
            public float proximoGolpe;
        }

        private readonly List<Emisor> emisores = new List<Emisor>();
        private readonly HashSet<Transform> conocidas = new HashSet<Transform>();
        private readonly Dictionary<string, int> numerosDeId = new Dictionary<string, int>();
        private readonly List<IdLugar> lugares = new List<IdLugar>();
        private System.Random azar = new System.Random();
        private float proximaRevision;

        /// <summary>Cuántas fuentes (una por ancla y por ID) hay registradas.</summary>
        public int Emisores => emisores.Count;

        /// <summary>Cuántas fuentes de bucle suenan ahora.</summary>
        public int BuclesSonando
        {
            get
            {
                int n = 0;
                foreach (Emisor e in emisores)
                {
                    if (e.bucle != Sonidos.SinBucle) n++;
                }
                return n;
            }
        }

        /// <summary>Si el jugador está dentro de un lugar cerrado (casa, taller, bar, casino, mercado, clínica, clandestino).</summary>
        public bool EnLugarCerrado => lugares.Count > 0;

        public void FijarSemilla(int semilla) => azar = new System.Random(semilla);

        private void OnEnable()
        {
            EventosLugares.JugadorEntro += AlEntrar;
            EventosLugares.JugadorSalio += AlSalir;
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        private void OnDisable()
        {
            EventosLugares.JugadorEntro -= AlEntrar;
            EventosLugares.JugadorSalio -= AlSalir;
            SceneManager.sceneLoaded -= AlCargarEscena;
            DetenerTodo();
        }

        private void Start() => StartCoroutine(Recorrer());

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            StopAllCoroutines();
            StartCoroutine(Recorrer());
        }

        private void AlEntrar(IdLugar lugar)
        {
            lugares.Remove(lugar);
            if (lugar != IdLugar.Calle && lugar != IdLugar.Ninguno) lugares.Add(lugar);
        }

        private void AlSalir(IdLugar lugar) => lugares.Remove(lugar);

        private IEnumerator Recorrer()
        {
            yield return new WaitForSecondsRealtime(TablaEmisoresMundo.SegundosAntesDeRecorrer);
            yield return Buscar("primera pasada");
            yield return new WaitForSecondsRealtime(TablaEmisoresMundo.SegundosSegundaPasada - TablaEmisoresMundo.SegundosAntesDeRecorrer);
            yield return Buscar("segunda pasada");
        }

        // Recorre todos los transforms de la escena por tandas (no se atora un cuadro) y registra los que coinciden con una definición.
        private IEnumerator Buscar(string pasada)
        {
            Transform[] todos = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int[] porDefinicion = new int[TablaEmisoresMundo.Definiciones.Length];
            HashSet<Transform> coincidentes = new HashSet<Transform>();
            int nuevos = 0;
            for (int i = 0; i < todos.Length; i++)
            {
                Transform t = todos[i];
                if (t != null && !conocidas.Contains(t))
                {
                    int d = TablaEmisoresMundo.Buscar(t.name);
                    if (d >= 0 && !TieneAncestroIgual(t, d, coincidentes))
                    {
                        coincidentes.Add(t);
                        conocidas.Add(t);
                        porDefinicion[d]++;
                        nuevos += Registrar(t, TablaEmisoresMundo.Definiciones[d]);
                    }
                }
                if ((i + 1) % AnclasPorCuadro == 0) yield return null;
            }
            LimpiarDestruidos();
            string detalle = string.Empty;
            for (int d = 0; d < porDefinicion.Length; d++)
            {
                if (porDefinicion[d] > 0) detalle += TablaEmisoresMundo.Definiciones[d].Nombre + "=" + porDefinicion[d] + " ";
            }
            Debug.Log("[T-1535] Sonidos del mundo, " + pasada + ": " + todos.Length + " objetos revisados, " + nuevos + " fuentes nuevas (" + emisores.Count + " en total). Anclas: " + (detalle.Length > 0 ? detalle.TrimEnd() : "ninguna"));
        }

        private static bool TieneAncestroIgual(Transform t, int definicion, HashSet<Transform> coincidentes)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                if (coincidentes.Contains(p) && TablaEmisoresMundo.Buscar(p.name) == definicion) return true;
            }
            return false;
        }

        private int Registrar(Transform ancla, TablaEmisoresMundo.Definicion d)
        {
            Vector3 donde = d.Centrado ? CentroDe(ancla) : ancla.position;
            int n = 0;
            foreach (string id in d.Ids)
            {
                GameObject go = new GameObject("Sonido_" + id);
                go.transform.SetParent(ancla, true);
                go.transform.position = donde;
                if (!numerosDeId.TryGetValue(id, out int numero))
                {
                    numero = numerosDeId.Count;
                    numerosDeId[id] = numero;
                }
                emisores.Add(new Emisor { punto = go.transform, id = id, idNumero = numero, definicion = d, proximoGolpe = 0f });
                n++;
            }
            return n;
        }

        private static Vector3 CentroDe(Transform ancla)
        {
            Renderer[] renderers = ancla.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0) return ancla.position;
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b.center;
        }

        private void LimpiarDestruidos()
        {
            for (int i = emisores.Count - 1; i >= 0; i--)
            {
                if (emisores[i].punto == null) emisores.RemoveAt(i);
            }
            conocidas.RemoveWhere(t => t == null);
        }

        private static float RadioDe(string id) => MapaSonidos20nov.TryFila(id, out TablaMezcla20nov.Fila fila) && fila.Distancia > 0f ? fila.Distancia : RadioPorDefecto;

        private void Update()
        {
            if (emisores.Count == 0 || Time.unscaledTime < proximaRevision) return;
            proximaRevision = Time.unscaledTime + TablaEmisoresMundo.SegundosRevision;
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                DetenerTodo();
                return;
            }
            Revisar(jugador.Raiz.position, jugador.Modo == ModoJugador.Carro);
        }

        private void Revisar(Vector3 jugador, bool enCarro)
        {
            LimpiarDestruidos();
            List<ReglaFuentesMundo.Candidato> candidatos = new List<ReglaFuentesMundo.Candidato>(emisores.Count);
            bool lamina = ReglaClimaSonido.SuenaLamina(ClimaSonoro.Lluvia, enCarro, EnLugarCerrado);
            foreach (Emisor e in emisores)
            {
                bool permitido = e.definicion.Grupo != GrupoEmisorMundo.Lamina || lamina;
                candidatos.Add(new ReglaFuentesMundo.Candidato
                {
                    Grupo = (int)e.definicion.Grupo,
                    Id = e.idNumero,
                    Distancia = permitido ? Vector3.Distance(jugador, e.punto.position) : float.MaxValue,
                    Radio = RadioDe(e.id),
                });
            }
            bool[] activos = new bool[candidatos.Count];
            ReglaFuentesMundo.Elegir(candidatos, TablaEmisoresMundo.TopePorGrupo, TablaEmisoresMundo.TopePorId, activos);
            float ahora = Time.unscaledTime;
            for (int i = 0; i < emisores.Count; i++)
            {
                Emisor e = emisores[i];
                if (e.definicion.EsPuntual) Golpear(e, activos[i], ahora);
                else Encender(e, activos[i]);
            }
        }

        private static void Encender(Emisor e, bool debeSonar)
        {
            if (debeSonar && e.bucle == Sonidos.SinBucle)
            {
                e.bucle = Sonidos.ReproducirBucle(e.id, e.punto);
            }
            else if (!debeSonar && e.bucle != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(e.bucle);
                e.bucle = Sonidos.SinBucle;
            }
        }

        // Los que no son bucle (expendedora, tele): suenan de vez en cuando mientras el jugador está cerca.
        private void Golpear(Emisor e, bool cerca, float ahora)
        {
            if (!cerca)
            {
                e.proximoGolpe = 0f;
                return;
            }
            if (e.proximoGolpe <= 0f)
            {
                e.proximoGolpe = ahora + ReglaFuentesMundo.SegundosHastaOtro(0f, e.definicion.MinimoSegundos, (float)azar.NextDouble());
                return;
            }
            if (ahora < e.proximoGolpe) return;
            Sonidos.ReproducirEn(e.id, e.punto.position);
            e.proximoGolpe = ahora + ReglaFuentesMundo.SegundosHastaOtro(e.definicion.MinimoSegundos, e.definicion.MaximoSegundos, (float)azar.NextDouble());
        }

        private void DetenerTodo()
        {
            foreach (Emisor e in emisores)
            {
                if (e.bucle != Sonidos.SinBucle)
                {
                    Sonidos.DetenerBucle(e.bucle);
                    e.bucle = Sonidos.SinBucle;
                }
            }
        }
    }
}
