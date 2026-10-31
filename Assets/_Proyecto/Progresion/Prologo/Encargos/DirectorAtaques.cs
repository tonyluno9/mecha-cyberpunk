using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Historia;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Encargos
{
    /// <summary>
    /// Los ataques rivales a tus zonas (T-427, D-tony-297): en el juego libre, cada cierto tiempo (al azar, más seguido con más zonas y más calor) los SNAKES
    /// o los SKULLS atacan una zona que conquistaste. Avisa en el HUD y marca la zona en el minimapa y el mapa (el objetivo de T-408/T-409); hay un tiempo
    /// para llegar y otro para vencer la oleada de 4 rivales. Defender paga Biits y respeto; no llegar o perder baja el respeto de la zona y, bajo el umbral,
    /// pasa al rival. Nunca ocurre en la historia ni durante un encargo (T-426); SUBURBIOS no se ataca. Guarda la espera con la clave <c>ataques</c>.
    /// </summary>
    public class DirectorAtaques : MonoBehaviour, IGuardable
    {
        public const string Clave = "ataques";
        public const string PrefijoIr = "VE A DEFENDER ";
        public const string PrefijoDefender = "DEFIENDE ";
        public const int LargoMaximoObjetivo = 32;
        public const int RivalesOleada = 4;
        private const float RadioLlegada = 25f;
        private const float RadioAbandono = 120f;
        private const float RadioAnillo = 7f;
        private const float SegundosAviso = 6f;

        private enum Fase { Ninguna, Camino, Oleada }

        [System.Serializable]
        private struct PuntoAtaque
        {
            public string zona;
            public Transform punto;
        }

        [SerializeField] private DirectorPandillas historia;
        [SerializeField] private DirectorEncargos encargos;
        [SerializeField] private DatosAtaquesZonas datos;
        [SerializeField] private PuntoAtaque[] puntos = System.Array.Empty<PuntoAtaque>();
        [Tooltip("Rivales a puños, apagados: se prenden al llegar a la zona atacada.")]
        [SerializeField] private PandilleroAPie[] rivales = System.Array.Empty<PandilleroAPie>();

        private readonly ReglaAtaques regla = new ReglaAtaques();
        private Fase fase;
        private string zona = string.Empty;
        private IdPandilla atacante;
        private float limite;
        private int vencidosOleada;

        /// <summary>True mientras hay un ataque en curso (el tablero de encargos no ofrece nada).</summary>
        public static bool EnCurso { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => EnCurso = false;

        public ReglaAtaques Regla => regla;
        public bool HayAtaque => fase != Fase.Ninguna;
        public string ZonaAtacada => zona;
        public bool EnOleada => fase == Fase.Oleada;
        public string ClaveGuardado => Clave;

        private bool HistoriaLibre => historia != null && historia.Iniciado && historia.Paso == PasoHistoria.Libre;
        private PantallaHistoriaPandillas Pantalla => historia != null ? historia.Pantalla : null;

        public static string TextoIr(string nombreZona) => PrefijoIr + nombreZona;

        public static string TextoDefender(string nombreZona, int vencidos, int total) => PrefijoDefender + nombreZona + " " + vencidos + "/" + total;

        private void OnEnable() => Registro<IGuardable>.Agregar(this);

        private void OnDisable()
        {
            Registro<IGuardable>.Quitar(this);
            EnCurso = false;
        }

        private void Awake()
        {
            foreach (PandilleroAPie r in rivales)
            {
                if (r != null)
                {
                    r.gameObject.SetActive(false);
                }
            }
        }

        // Zonas tuyas que se pueden atacar: con dueño Jugador, con punto de ataque y que no sean la protegida (SUBURBIOS).
        private List<string> ZonasAtacables()
        {
            List<string> lista = new List<string>();
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            if (territorio == null)
            {
                return lista;
            }
            string protegida = datos != null ? datos.ZonaProtegida : "fosa";
            foreach (string z in territorio.Zonas)
            {
                if (z != protegida && territorio.Dueno(z) == IdPandilla.Jugador && Punto(z) != null)
                {
                    lista.Add(z);
                }
            }
            return lista;
        }

        private Transform Punto(string z)
        {
            foreach (PuntoAtaque p in puntos)
            {
                if (p.zona == z)
                {
                    return p.punto;
                }
            }
            return null;
        }

        private int NivelCalor => Registro<ICalor>.Actual != null ? Registro<ICalor>.Actual.Nivel : 0;

        private bool EncargoEnCurso => encargos != null && encargos.HayEncargo;

        private void Update()
        {
            if (datos == null)
            {
                return;
            }
            if (fase == Fase.Ninguna)
            {
                Esperar();
            }
            else
            {
                Avanzar();
            }
        }

        private void Esperar()
        {
            List<string> atacables = ZonasAtacables();
            if (!ReglaAtaques.Permitido(HistoriaLibre, EncargoEnCurso, false, atacables.Count))
            {
                return;
            }
            if (regla.Espera < 0f)
            {
                regla.Fijar(ReglaAtaques.Intervalo(datos.SegundosMinimo, datos.SegundosMaximo, atacables.Count + 1, NivelCalor, Random.value, datos.MasPorZona, datos.MasPorCalor));
            }
            regla.Avanzar(Time.deltaTime);
            if (regla.Listo)
            {
                Atacar(atacables[ReglaAtaques.Elegir(atacables.Count, Random.value)]);
            }
        }

        /// <summary>Empieza un ataque a una zona tuya (la que se pida, o una al azar). False si no se permite ahora.</summary>
        public bool Atacar(string zonaAtacada = null)
        {
            List<string> atacables = ZonasAtacables();
            if (datos == null || !ReglaAtaques.Permitido(HistoriaLibre, EncargoEnCurso, HayAtaque, atacables.Count))
            {
                return false;
            }
            if (string.IsNullOrEmpty(zonaAtacada))
            {
                zonaAtacada = atacables[ReglaAtaques.Elegir(atacables.Count, Random.value)];
            }
            if (!atacables.Contains(zonaAtacada))
            {
                return false;
            }
            zona = zonaAtacada;
            atacante = ReglaAtaques.Atacante(zona);
            fase = Fase.Camino;
            EnCurso = true;
            vencidosOleada = 0;
            limite = Time.time + datos.SegundosParaLlegar;
            string nombre = Registro<ITerritorio>.Actual.Nombre(zona);
            Objetivo(TextoIr(nombre), Punto(zona).position);
            Pantalla?.MostrarAyuda(ReglaAtaques.Aviso(atacante, nombre), SegundosAviso);
            Sonidos.Reproducir("ZON_CONQUISTADA");
            return true;
        }

        private void Avanzar()
        {
            if (!HistoriaLibre || EncargoEnCurso)
            {
                // Si la historia vuelve a un paso (cargar una partida) el ataque se deja sin castigo.
                Cerrar();
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return;
            }
            Vector3 yo = jugador.Raiz.position;
            Vector3 destino = Punto(zona).position;
            string nombre = Registro<ITerritorio>.Actual.Nombre(zona);
            if (fase == Fase.Camino)
            {
                if (Distancia(yo, destino) <= RadioLlegada)
                {
                    EmpezarOleada(destino, nombre);
                }
                else if (Time.time >= limite)
                {
                    Resolver(false, "NO LLEGASTE A TIEMPO");
                }
                return;
            }
            int total = Mathf.Min(rivales.Length, RivalesOleada);
            int vencidos = 0;
            for (int i = 0; i < total; i++)
            {
                if (rivales[i] != null && rivales[i].EstaKO)
                {
                    vencidos++;
                }
            }
            if (vencidos != vencidosOleada)
            {
                vencidosOleada = vencidos;
                Objetivo(TextoDefender(nombre, vencidos, total), destino);
            }
            if (vencidos >= total)
            {
                Resolver(true, null);
            }
            else if (Time.time >= limite)
            {
                Resolver(false, "PERDISTE " + nombre);
            }
            else if (Distancia(yo, destino) > RadioAbandono)
            {
                Resolver(false, "ABANDONASTE " + nombre);
            }
        }

        private void EmpezarOleada(Vector3 destino, string nombre)
        {
            fase = Fase.Oleada;
            limite = Time.time + datos.SegundosOleada;
            int total = Mathf.Min(rivales.Length, RivalesOleada);
            for (int i = 0; i < total; i++)
            {
                float angulo = i * Mathf.PI * 2f / total;
                DirectorEncargos.ColocarRival(rivales[i], destino + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * RadioAnillo);
            }
            Objetivo(TextoDefender(nombre, 0, total), destino);
        }

        private static float Distancia(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return (a - b).magnitude;
        }

        private void Resolver(bool defendida, string aviso)
        {
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            ServicioTerritorio servicio = Registro<ServicioTerritorio>.Actual;
            string zonaAnterior = zona;
            string nombre = territorio.Nombre(zonaAnterior);
            IdPandilla rival = atacante;
            Cerrar();
            regla.Terminar(defendida);
            if (defendida)
            {
                Registro<ICartera>.Actual?.Abonar(datos.PremioBiits, MotivoDinero.Mision);
                servicio?.AjustarRespeto(zonaAnterior, datos.PremioRespeto);
                Pantalla?.MostrarAyuda("ZONA DEFENDIDA · +" + Moneda.Texto(datos.PremioBiits) + " · +RESPETO", SegundosAviso);
                return;
            }
            if (servicio != null && ReglaAtaques.PierdeZona(territorio.Respeto(zonaAnterior), datos.CastigoRespeto, datos.UmbralPerder))
            {
                servicio.Ceder(zonaAnterior, rival, datos.RespetoAlPerder);
                Pantalla?.MostrarAyuda(nombre + " AHORA ES DE LOS " + ReglaAtaques.NombrePandilla(rival), SegundosAviso);
            }
            else
            {
                servicio?.AjustarRespeto(zonaAnterior, -datos.CastigoRespeto);
                Pantalla?.MostrarAyuda(aviso + " · −RESPETO", SegundosAviso);
            }
        }

        // Quita lo del ataque (marca, rivales) sin premio ni castigo.
        private void Cerrar()
        {
            fase = Fase.Ninguna;
            EnCurso = false;
            foreach (PandilleroAPie r in rivales)
            {
                if (r != null && r.gameObject.activeSelf)
                {
                    r.gameObject.SetActive(false);
                }
            }
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Quitar();
        }

        private static void Objetivo(string texto, Vector3 posicion)
        {
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Fijar(new Objetivo(texto, posicion));
        }

        public string Guardar() => regla.Guardar();

        public void Cargar(string texto)
        {
            if (!regla.Cargar(texto))
            {
                Debug.LogWarning("[Progresion] DirectorAtaques: el guardado no se entiende; no se cambia nada.");
            }
        }
    }
}
