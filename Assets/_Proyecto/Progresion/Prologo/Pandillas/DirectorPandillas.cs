using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Aliados;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI.Historia;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Prologo.Pandillas
{
    /// <summary>
    /// Lleva la historia corta del juego de pandillas (T-406, D-tony-223): despiertas en tu casa, defiendes a DOÑA LUPE a puños, recoges la pistola,
    /// reclutas al CHATO, conquistas EL TIANGUIS y sale "CONTINUARÁ". Las reglas están en <see cref="MaquinaHistoriaPandillas"/>. Revisa cada cuadro
    /// lo que pasa en el mundo y avisa a la máquina; cada paso fija su objetivo. Guarda su paso con la clave <c>historia_pandillas</c>.
    /// F6 salta un paso (solo fuera de la demo final).
    /// </summary>
    public class DirectorPandillas : MonoBehaviour, IGuardable
    {
        public const string Clave = "historia_pandillas";
        public const string TextoSalir = "SAL A LA CALLE";
        public const string TextoDefender = "DEFIENDE A DOÑA LUPE";
        public const string TextoPistola = "RECOGE LA PISTOLA (E)";
        public const string TextoChato = "HABLA CON EL CHATO";
        public const string TextoJefe = "VENCE AL JEFE DE LOS CALAVERAS";
        public const string NombreChato = "EL CHATO";
        private const float VidaProteccion = 25f;
        private const float SegundosProteccion = 3f;
        private const int CuadrosEspera = 3;

        [SerializeField] private Transform puertaCasa;
        [SerializeField] private Transform donaLupe;
        [SerializeField] private PandilleroAPie[] calaveras = System.Array.Empty<PandilleroAPie>();
        [SerializeField] private Reclutable chato;
        [Tooltip("Los grupos de Calaveras del tianguis: apagados hasta el paso del tianguis.")]
        [SerializeField] private GameObject[] gruposTianguis = System.Array.Empty<GameObject>();
        [SerializeField] private string idCama = "Casa_Cama";
        [SerializeField] private string zona = "tianguis";
        [Min(1)] [SerializeField] private int respetoJefe = 70;
        [Min(0.5f)] [SerializeField] private float radioSalida = 2.5f;
        [Min(0f)] [SerializeField] private float segundosContinuara = 4f;

        private readonly MaquinaHistoriaPandillas maquina = new MaquinaHistoriaPandillas();
        private PantallaHistoriaPandillas pantalla;
        private ArmaEnPiso arma;
        private Vector3 ultimaCaida;
        private bool hayCaida;
        private float proteccionHasta = -1f;
        private float inicioContinuara = -1f;
        private int respetoMostrado = -1;
        private bool arrancado;
        private bool cargadoAntes;

        public PasoHistoria Paso => maquina.Paso;
        public bool Iniciado => arrancado;
        public PantallaHistoriaPandillas Pantalla => pantalla;
        public string ClaveGuardado => Clave;

        private void Awake()
        {
            pantalla = gameObject.AddComponent<PantallaHistoriaPandillas>();
            pantalla.ContinuaraCerrada += AlCerrarContinuara;
            foreach (GameObject grupo in gruposTianguis)
            {
                if (grupo != null)
                {
                    grupo.SetActive(false);
                }
            }
            foreach (PandilleroAPie c in calaveras)
            {
                if (c != null)
                {
                    c.gameObject.SetActive(false);
                    c.Vencido += AlVencerCalavera;
                }
            }
#if DEMO_FINAL
            // La demo final no lleva teclas de prueba (calor, guardado).
            foreach (MechaCyberpunk.Progresion.Pruebas.CalorDePrueba p in FindObjectsByType<MechaCyberpunk.Progresion.Pruebas.CalorDePrueba>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Destroy(p);
            }
            foreach (MechaCyberpunk.Progresion.Pruebas.GuardadoDePrueba p in FindObjectsByType<MechaCyberpunk.Progresion.Pruebas.GuardadoDePrueba>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Destroy(p);
            }
#endif
        }

        private void OnEnable() => Registro<IGuardable>.Agregar(this);

        private void OnDisable() => Registro<IGuardable>.Quitar(this);

        private void OnDestroy()
        {
            foreach (PandilleroAPie c in calaveras)
            {
                if (c != null)
                {
                    c.Vencido -= AlVencerCalavera;
                }
            }
            if (pantalla != null)
            {
                pantalla.ContinuaraCerrada -= AlCerrarContinuara;
            }
            if (arma != null)
            {
                arma.Tomada -= AlTomarArma;
            }
        }

        private IEnumerator Start()
        {
            // Unos cuadros: el jugador se coloca y las partidas cargan antes de empezar.
            for (int i = 0; i < CuadrosEspera; i++)
            {
                yield return null;
            }
            float limite = Time.realtimeSinceStartup + 3f;
            while (Registro<IJugador>.Actual == null && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            arrancado = true;
            if (!cargadoAntes && maquina.Paso == PasoHistoria.Despierta)
            {
                ColocarEnLaCama();
            }
            Aplicar();
        }

        private void ColocarEnLaCama()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null)
            {
                return;
            }
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto != null && punto.Id == idCama)
                {
                    jugador.Colocar(punto.Pose);
                    return;
                }
            }
            Debug.LogWarning("[Progresion] DirectorPandillas no encontró el punto " + idCama + "; el jugador empieza donde está.", this);
        }

        // ---------- Cada cuadro ----------

        private void Update()
        {
            if (!arrancado)
            {
                return;
            }
#if !DEMO_FINAL
            Keyboard teclado = Keyboard.current;
            if (teclado != null && teclado.f6Key.wasPressedThisFrame && maquina.Saltar())
            {
                Aplicar();
            }
#endif
            switch (maquina.Paso)
            {
                case PasoHistoria.Despierta:
                    if (puertaCasa != null && DistanciaAlJugador(puertaCasa.position) <= radioSalida)
                    {
                        Avisar(AvisoHistoria.SalioDeCasa);
                    }
                    break;
                case PasoHistoria.PrimeraPelea:
                    Proteger();
                    if (!HayCalaverasVivas())
                    {
                        Avisar(AvisoHistoria.PeleaGanada);
                    }
                    break;
                case PasoHistoria.Chato:
                    IPandillaJugador pandilla = Registro<IPandillaJugador>.Actual;
                    if (pandilla != null && Tiene(pandilla, NombreChato))
                    {
                        Avisar(AvisoHistoria.ChatoUnido);
                    }
                    break;
                case PasoHistoria.Tianguis:
                    ActualizarRespeto();
                    ITerritorio territorio = Registro<ITerritorio>.Actual;
                    if (territorio != null && territorio.JefeDisponible(zona))
                    {
                        Avisar(AvisoHistoria.JefeDisponible);
                    }
                    break;
                case PasoHistoria.Jefe:
                    ITerritorio t = Registro<ITerritorio>.Actual;
                    if (t != null && t.Dueno(zona) == IdPandilla.Jugador)
                    {
                        Avisar(AvisoHistoria.TianguisConquistado);
                    }
                    break;
                case PasoHistoria.Continuara:
                    if (inicioContinuara >= 0f && Time.time - inicioContinuara >= segundosContinuara && !pantalla.ContinuaraVisible)
                    {
                        pantalla.MostrarContinuara();
                    }
                    break;
            }
        }

        private static bool Tiene(IPandillaJugador pandilla, string nombre)
        {
            for (int i = 0; i < pandilla.Cantidad; i++)
            {
                if (pandilla.Nombre(i) == nombre)
                {
                    return true;
                }
            }
            return false;
        }

        private static float DistanciaAlJugador(Vector3 punto)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return float.MaxValue;
            }
            Vector3 d = jugador.Raiz.position - punto;
            d.y = 0f;
            return d.magnitude;
        }

        private bool HayCalaverasVivas()
        {
            foreach (PandilleroAPie c in calaveras)
            {
                if (c != null && c.gameObject.activeSelf && !c.EstaKO)
                {
                    return true;
                }
            }
            return false;
        }

        // No pueden matarte: con menos de 25 de vida dejan de pegar 3 s.
        private void Proteger()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            IVida vida = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<IVida>() : null;
            if (vida != null && vida.Actual < VidaProteccion)
            {
                proteccionHasta = Time.time + SegundosProteccion;
            }
            bool quietos = Time.time < proteccionHasta;
            foreach (PandilleroAPie c in calaveras)
            {
                if (c != null && c.gameObject.activeSelf && c.enabled == quietos)
                {
                    c.enabled = !quietos;
                }
            }
        }

        private void AlVencerCalavera(PandilleroAPie vencido)
        {
            ultimaCaida = vencido.transform.position;
            hayCaida = true;
        }

        private void ActualizarRespeto()
        {
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            int respeto = territorio != null ? territorio.Respeto(zona) : 0;
            if (respeto != respetoMostrado)
            {
                respetoMostrado = respeto;
                FijarObjetivo("CONQUISTA EL TIANGUIS · RESPETO " + respeto + "/" + respetoJefe, PosicionTianguis());
            }
        }

        private Vector3 PosicionTianguis()
        {
            foreach (GameObject g in gruposTianguis)
            {
                if (g != null)
                {
                    return g.transform.position;
                }
            }
            return Vector3.zero;
        }

        // ---------- Avance ----------

        private void Avisar(AvisoHistoria aviso)
        {
            if (maquina.Avisar(aviso))
            {
                Aplicar();
            }
        }

        private void AlCerrarContinuara() => Avisar(AvisoHistoria.ContinuaraCerrado);

        // Efectos del paso actual: objetivo, textos y qué hay en el mundo.
        private void Aplicar()
        {
            PasoHistoria paso = maquina.Paso;
            respetoMostrado = -1;
            foreach (PandilleroAPie c in calaveras)
            {
                if (c != null)
                {
                    bool activa = paso == PasoHistoria.PrimeraPelea && !c.EstaKO;
                    if (c.gameObject.activeSelf != activa)
                    {
                        c.gameObject.SetActive(activa);
                    }
                }
            }
            if (paso != PasoHistoria.Libre)
            {
                foreach (GameObject g in gruposTianguis)
                {
                    bool activo = paso >= PasoHistoria.Tianguis;
                    if (g != null && g.activeSelf != activo)
                    {
                        g.SetActive(activo);
                    }
                }
            }
            if (chato != null)
            {
                chato.LiberadoPorHistoria = paso == PasoHistoria.Chato;
            }
            inicioContinuara = paso == PasoHistoria.Continuara ? Time.time : -1f;

            switch (paso)
            {
                case PasoHistoria.Despierta:
                    FijarObjetivo(TextoSalir, puertaCasa != null ? puertaCasa.position : Vector3.zero);
                    break;
                case PasoHistoria.PrimeraPelea:
                    FijarObjetivo(TextoDefender, donaLupe != null ? donaLupe.position : Vector3.zero);
                    pantalla.MostrarSubtitulo("—¡Ya déjenla!", 4f);
                    break;
                case PasoHistoria.Pistola:
                    SoltarPistola();
                    FijarObjetivo(TextoPistola, arma != null ? arma.transform.position : Vector3.zero);
                    break;
                case PasoHistoria.Chato:
                    pantalla.MostrarAyuda("CLIC DERECHO APUNTA · CLIC IZQUIERDO DISPARA", 8f);
                    FijarObjetivo(TextoChato, chato != null ? chato.Posicion : Vector3.zero);
                    break;
                case PasoHistoria.Tianguis:
                    pantalla.MostrarSubtitulo("—Los Calaveras se creen dueños del Tianguis", 5f);
                    pantalla.MostrarAyuda("PUEDES ROBAR UN CARRO CON E\nEN EL MERCADO NEGRO VENDEN ESCOPETAS", 10f);
                    ActualizarRespeto();
                    break;
                case PasoHistoria.Jefe:
                    FijarObjetivo(TextoJefe, PosicionJefe());
                    break;
                default:
                    QuitarObjetivo();
                    break;
            }
        }

        private Vector3 PosicionJefe()
        {
            foreach (JefePandilla j in FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (j.Zona == zona)
                {
                    return j.transform.position;
                }
            }
            return PosicionTianguis();
        }

        private void SoltarPistola()
        {
            if (arma != null)
            {
                return;
            }
            Vector3 punto = hayCaida ? ultimaCaida : (donaLupe != null ? donaLupe.position : transform.position);
            GameObject objeto = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objeto.name = "PistolaEnPiso";
            Destroy(objeto.GetComponent<Collider>());
            objeto.transform.position = punto + Vector3.up * 0.15f;
            objeto.transform.localScale = new Vector3(0.12f, 0.12f, 0.3f);
            Renderer r = objeto.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(1f, 0.85f, 0.2f);
            }
            arma = objeto.AddComponent<ArmaEnPiso>();
            arma.Tomada += AlTomarArma;
        }

        private void AlTomarArma(ArmaEnPiso tomada)
        {
            arma = null;
            Avisar(AvisoHistoria.PistolaTomada);
        }

        private static void FijarObjetivo(string texto, Vector3 posicion)
        {
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Fijar(new Objetivo(texto, posicion));
        }

        private static void QuitarObjetivo()
        {
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Quitar();
        }

        // ---------- Guardado ----------

        [System.Serializable]
        private class DatosGuardados
        {
            public string paso = nameof(PasoHistoria.Despierta);
        }

        public string Guardar() => JsonUtility.ToJson(new DatosGuardados { paso = maquina.Paso.ToString() });

        public void Cargar(string datos)
        {
            DatosGuardados leidos = null;
            try
            {
                leidos = JsonUtility.FromJson<DatosGuardados>(datos);
            }
            catch (System.Exception)
            {
                // se avisa abajo
            }
            if (leidos == null || !MaquinaHistoriaPandillas.TryLeer(leidos.paso, out PasoHistoria paso))
            {
                Debug.LogWarning("[Progresion] DirectorPandillas: el guardado no se entiende; no se cambia nada.");
                return;
            }
            maquina.Iniciar(paso);
            cargadoAntes = true;
            if (arrancado)
            {
                Aplicar();
            }
        }
    }
}
