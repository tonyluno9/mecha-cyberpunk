using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1304: con 4 estrellas de búsqueda llega Nova: guardias robot armados y un guardia pesado aparecen a 22-34 m del jugador (en el piso), van por él y lo
    /// atacan; se reponen por oleadas. Con 5 estrellas son más y llega también el jefe final (una vez por búsqueda). Bajo 4 estrellas unos segundos, Nova se
    /// retira. Arranca sola en cada partida (si existe IA/Resources/ConfigRespuestaNova) y solo actúa donde hay policía (IZonaBusqueda registrada).
    /// </summary>
    public class RespuestaNova : MonoBehaviour
    {
        private const float SegundosRevisar = 0.5f;
        private const int Intentos = 24;
        private const float AlturaSonda = 1.6f;
        private const float MismoPiso = 1.5f;
        private const float CaidaMaxima = 40f;
        private const float RadioCapsula = 0.4f;
        private const float GravedadAgente = 20f; // la misma de PandilleroAPie (m/s²)
        private const float AlturaAgente = 2.4f; // lo que mide el jefe final más un margen (m)
        private const float MargenCaida = 0.5f;
        private const float AlturaMinimaCaida = 3f; // con menos de esto encima no vale la pena caer: aparecen en el piso
        private const float AlturaDestelloSalida = 1f;

        /// <summary>Las pruebas que no son de Nova pueden apagarla para que no aparezca nadie en sus escenas.</summary>
        public static bool Apagada { get; set; }
        public static RespuestaNova Actual { get; private set; }

        private ConfigRespuestaNova config;
        private readonly List<PandilleroAPie> guardias = new List<PandilleroAPie>();
        private readonly List<PandilleroAPie> pesados = new List<PandilleroAPie>();
        private PandilleroAPie jefe;
        private bool jefeSalio;
        private float proximaRevision;
        private float proximaOleada;
        private float segundosSinNova;
        private bool anunciada;
        private readonly HashSet<PandilleroAPie> vistos = new HashSet<PandilleroAPie>();

        /// <summary>T-1323: la búsqueda llegó al tope (5 estrellas) y suena el aviso especial; Nova baja del cielo.</summary>
        public event System.Action LlegadaEspecial;

        public int Estrellas { get; private set; }
        public int GuardiasVivos => Vivos(guardias);
        public int PesadosVivos => Vivos(pesados);
        public bool HayJefe => jefe != null && !jefe.EstaKO;
        public PandilleroAPie Jefe => jefe;
        public IReadOnlyList<PandilleroAPie> Guardias => guardias;
        public IReadOnlyList<PandilleroAPie> Pesados => pesados;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEstatico()
        {
            Apagada = false;
            Actual = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
            if (Actual != null)
            {
                return;
            }
            ConfigRespuestaNova cfg = Resources.Load<ConfigRespuestaNova>(ConfigRespuestaNova.Recurso);
            if (cfg == null)
            {
                return;
            }
            GameObject objeto = new GameObject("RespuestaNova");
            DontDestroyOnLoad(objeto);
            objeto.AddComponent<RespuestaNova>().Configurar(cfg);
        }

        public void Configurar(ConfigRespuestaNova cfg) => config = cfg;

        private void OnEnable()
        {
            Actual = this;
            EventosJugador.Desperto += RetirarTodos; // T-1328: al despertar con el Doc, Nova se va
        }

        private void OnDisable()
        {
            EventosJugador.Desperto -= RetirarTodos;
            if (Actual == this)
            {
                Actual = null;
            }
        }

        private void OnDestroy() => RetirarTodos();

        private void Update()
        {
            if (Time.time < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.time + SegundosRevisar;
            IJugador jugador = Registro<IJugador>.Actual;
            bool hayPolicia = Registro<IZonaBusqueda>.Actual != null;
            Estrellas = config == null || Apagada || !hayPolicia ? 0 : EstrellasBusqueda.De(Registro<ICalor>.Actual);
            guardias.RemoveAll(p => p == null);
            pesados.RemoveAll(p => p == null);
            if (jugador == null || jugador.Raiz == null || !EstrellasBusqueda.EsNova(Estrellas))
            {
                anunciada = false;
                segundosSinNova += SegundosRevisar;
                if (Apagada || config == null || Estrellas <= 0 || jugador == null || jugador.Raiz == null)
                {
                    // Sin búsqueda (el Doc, un arresto, otra escena): se van todos, hayan visto al jugador o no.
                    if (Apagada || config == null || segundosSinNova >= (config != null ? config.SegundosParaRetirarse : 0f))
                    {
                        RetirarTodos();
                    }
                    return;
                }
                // T-1323: con 1 a 3 estrellas se retiran los que nunca vieron al jugador; los que ya lo vieron lo siguen persiguiendo.
                if (segundosSinNova >= config.SegundosParaRetirarse)
                {
                    RetirarNoVistos();
                }
                Vector3 aqui = jugador.Raiz.position;
                MarcarVistos(aqui);
                Azuzar(aqui);
                return;
            }
            segundosSinNova = 0f;
            Vector3 donde = jugador.Raiz.position;
            if (Estrellas >= EstrellasBusqueda.Total && !anunciada)
            {
                // T-1323: al llegar al tope suena el aviso y Nova baja del cielo (la oleada de abajo).
                anunciada = true;
                Sonidos.Reproducir(config.SonidoLlegada);
                LlegadaEspecial?.Invoke();
                proximaOleada = Time.time; // la oleada baja en ese momento, no a la siguiente vuelta
            }
            if (Time.time >= proximaOleada)
            {
                proximaOleada = Time.time + config.SegundosEntreOleadas;
                Reponer(guardias, config.Guardia, config.Guardias(Estrellas), donde, "Nova_Guardia_");
                Reponer(pesados, config.GuardiaPesado, config.Pesados(Estrellas), donde, "Nova_GuardiaPesado_");
            }
            if (Estrellas >= EstrellasBusqueda.Total && !jefeSalio && config.JefeFinal != null)
            {
                jefe = Aparecer(config.JefeFinal, donde, "Nova_JefeFinal");
                jefeSalio = jefe != null;
            }
            MarcarVistos(donde);
            Azuzar(donde);
        }

        /// <summary>T-1323: marca como "ya te vio" a los agentes de Nova en pie que están a <see cref="ConfigRespuestaNova.DistanciaVista"/> o menos del jugador.</summary>
        private void MarcarVistos(Vector3 donde)
        {
            float vista = config.DistanciaVista;
            MarcarVistos(guardias, donde, vista);
            MarcarVistos(pesados, donde, vista);
            if (jefe != null && !jefe.EstaKO)
            {
                MarcarVisto(jefe, donde, vista);
            }
        }

        private void MarcarVistos(List<PandilleroAPie> lista, Vector3 donde, float vista)
        {
            foreach (PandilleroAPie p in lista)
            {
                MarcarVisto(p, donde, vista);
            }
        }

        private void MarcarVisto(PandilleroAPie p, Vector3 donde, float vista)
        {
            if (p == null || p.EstaKO || vistos.Contains(p))
            {
                return;
            }
            Vector3 d = p.transform.position - donde;
            d.y = 0f;
            if (d.sqrMagnitude <= vista * vista)
            {
                vistos.Add(p);
            }
        }

        /// <summary>True si este agente de Nova ya vio al jugador (y lo sigue persiguiendo aunque baje la búsqueda).</summary>
        public bool YaVioAlJugador(PandilleroAPie p) => vistos.Contains(p);

        // Todos van por el jugador: su punto (la correa) es donde está el jugador y están en alerta; el pesado y el jefe, además, en modo pelea.
        private void Azuzar(Vector3 donde)
        {
            foreach (PandilleroAPie p in guardias)
            {
                Azuzar(p, donde);
            }
            foreach (PandilleroAPie p in pesados)
            {
                if (p == null || p.EstaKO)
                {
                    continue;
                }
                Azuzar(p, donde);
                ConductaGuardiaPesado pesado = p.GetComponent<ConductaGuardiaPesado>();
                if (pesado != null)
                {
                    pesado.FijarPuesto(donde, donde - p.transform.position);
                    pesado.Provocar();
                }
            }
            if (jefe != null && !jefe.EstaKO)
            {
                Azuzar(jefe, donde);
                ConductaJefeFinalNova conducta = jefe.GetComponent<ConductaJefeFinalNova>();
                if (conducta != null)
                {
                    conducta.Activar();
                }
            }
        }

        private static void Azuzar(PandilleroAPie p, Vector3 donde)
        {
            if (p == null || p.EstaKO)
            {
                return;
            }
            p.Configurar(donde, null);
            p.Alerta = true;
        }

        private void Reponer(List<PandilleroAPie> lista, GameObject prefab, int cantidad, Vector3 donde, string nombre)
        {
            if (prefab == null)
            {
                return;
            }
            for (int i = Vivos(lista); i < cantidad; i++)
            {
                PandilleroAPie nuevo = Aparecer(prefab, donde, nombre + (lista.Count + 1));
                if (nuevo == null)
                {
                    return;
                }
                lista.Add(nuevo);
            }
        }

        private PandilleroAPie Aparecer(GameObject prefab, Vector3 jugador, string nombre)
        {
            if (!BuscarPunto(jugador, out Vector3 punto))
            {
                return null;
            }
            Vector3 mira = jugador - punto;
            mira.y = 0f;
            Quaternion giro = mira.sqrMagnitude > 0.01f ? Quaternion.LookRotation(mira) : Quaternion.identity;
            float altura = AlturaLibreArriba(punto);
            Vector3 inicio = punto + Vector3.up * altura;
            GameObject objeto = Instantiate(prefab, inicio, giro);
            objeto.name = nombre;
            PandilleroAPie p = objeto.GetComponent<PandilleroAPie>();
            if (p == null)
            {
                Destroy(objeto);
                return null;
            }
            if (prefab == config.GuardiaPesado && !Mathf.Approximately(config.EscalaPesado, 1f))
            {
                // T-1323: el pesado se veía más chico que los otros agentes de Nova.
                Transform modelo = objeto.transform.Find("Modelo");
                if (modelo != null)
                {
                    modelo.localScale *= config.EscalaPesado;
                }
            }
            objeto.AddComponent<ZonaCabezaNova>();
            Azuzar(p, jugador);
            // T-1323: llegan cayendo (la gravedad del agente y su animación de caída hacen el resto) con un destello al salir y otro al aterrizar.
            DestelloLlegadaNova.Lanzar(inicio + Vector3.up * AlturaDestelloSalida, config, 0f);
            if (altura > 0f)
            {
                DestelloLlegadaNova.Lanzar(punto, config, SegundosDeCaida(altura));
            }
            return p;
        }

        /// <summary>Cuánto se puede subir desde <paramref name="punto"/> (hasta <see cref="ConfigRespuestaNova.AlturaCaida"/>) sin topar con un techo: desde ahí caen.</summary>
        private float AlturaLibreArriba(Vector3 punto)
        {
            float maxima = config.AlturaCaida;
            if (maxima <= 0f)
            {
                return 0f;
            }
            float libre = maxima;
            if (Physics.Raycast(punto + Vector3.up * AlturaSonda, Vector3.up, out RaycastHit techo, maxima + MargenCaida, ~0, QueryTriggerInteraction.Ignore))
            {
                libre = Mathf.Max(0f, techo.distance + AlturaSonda - AlturaAgente);
            }
            return Mathf.Min(maxima, libre) >= AlturaMinimaCaida ? Mathf.Min(maxima, libre) : 0f;
        }

        /// <summary>Segundos que tarda en caer <paramref name="altura"/> metros con la gravedad de <see cref="PandilleroAPie"/>.</summary>
        public static float SegundosDeCaida(float altura) => Mathf.Sqrt(2f * altura / GravedadAgente);

        /// <summary>Un punto libre en el piso a 22-34 m del jugador: primero a su misma altura (su calle o su techo); si no hay, en el piso de abajo.</summary>
        public bool BuscarPunto(Vector3 jugador, out Vector3 punto)
        {
            float min = config != null ? config.DistanciaMinima : 22f;
            float max = config != null ? config.DistanciaMaxima : 34f;
            for (int i = 0; i < Intentos; i++)
            {
                bool estricto = i < Intentos / 2;
                float angulo = Random.Range(0f, Mathf.PI * 2f);
                float d = Random.Range(min, max);
                Vector3 arriba = jugador + new Vector3(Mathf.Cos(angulo) * d, AlturaSonda, Mathf.Sin(angulo) * d);
                if (!Physics.Raycast(arriba, Vector3.down, out RaycastHit golpe, CaidaMaxima, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                if ((estricto && Mathf.Abs(golpe.point.y - jugador.y) > MismoPiso) || golpe.normal.y < 0.7f)
                {
                    continue;
                }
                Vector3 pie = golpe.point + Vector3.up * (RadioCapsula + 0.05f);
                if (Physics.CheckCapsule(pie, pie + Vector3.up * 1.2f, RadioCapsula, ~0, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                punto = golpe.point + Vector3.up * 0.05f;
                return true;
            }
            punto = jugador;
            return false;
        }

        /// <summary>Nova se va: quita a los suyos que siguen de pie (los caídos se quedan tirados) y la próxima búsqueda alta vuelve a traer al jefe.</summary>
        public void RetirarTodos()
        {
            Quitar(guardias);
            Quitar(pesados);
            if (jefe != null && !jefe.EstaKO)
            {
                Destroy(jefe.gameObject);
            }
            jefe = null;
            jefeSalio = false;
            proximaOleada = 0f;
            anunciada = false;
            vistos.Clear();
        }

        /// <summary>T-1323: quita a los que nunca vieron al jugador; los que ya lo vieron se quedan persiguiéndolo.</summary>
        private void RetirarNoVistos()
        {
            QuitarNoVistos(guardias);
            QuitarNoVistos(pesados);
            if (jefe != null && !jefe.EstaKO && !vistos.Contains(jefe))
            {
                Destroy(jefe.gameObject);
                jefe = null;
                jefeSalio = false;
            }
        }

        private void QuitarNoVistos(List<PandilleroAPie> lista)
        {
            for (int i = lista.Count - 1; i >= 0; i--)
            {
                PandilleroAPie p = lista[i];
                if (p != null && !p.EstaKO && !vistos.Contains(p))
                {
                    Destroy(p.gameObject);
                    lista.RemoveAt(i);
                }
            }
        }

        private static void Quitar(List<PandilleroAPie> lista)
        {
            foreach (PandilleroAPie p in lista)
            {
                if (p != null && !p.EstaKO)
                {
                    Destroy(p.gameObject);
                }
            }
            lista.Clear();
        }

        private static int Vivos(List<PandilleroAPie> lista)
        {
            int n = 0;
            foreach (PandilleroAPie p in lista)
            {
                if (p != null && !p.EstaKO)
                {
                    n++;
                }
            }
            return n;
        }
    }
}
