using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Aliados;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.UI.Historia;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Prologo.Pandillas
{
    /// <summary>
    /// Lleva la historia corta del juego de pandillas (T-406, D-tony-223): despiertas en tu casa, defiendes a DOÑA LUPE a puños, recoges la pistola,
    /// reclutas al CHATO, EL DOC te cura, EL INVENTOR te mejora la pistola, conquistas EL TIANGUIS, EL HACKER descubre a NOVA en NEÓN ALTO
    /// y sale "CONTINUARÁ" (T-418). Los SNAKES de Neón Alto quedan apagados hasta el juego libre. Las reglas están en <see cref="MaquinaHistoriaPandillas"/>. Revisa cada cuadro
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
        public const string TextoJefe = "VENCE AL JEFE DE LOS SKULLS";
        public const string TextoDoc = "VE CON EL DOC A I+D+I";
        public const string TextoInventor = "VE CON EL INVENTOR AL CIC";
        public const string TextoHacker = "VE CON EL HACKER A NEÓN ALTO";
        public const string PrefijoTianguis = "VENCE A LOS SKULLS ";
        public const int LargoMaximoObjetivo = 32;
        public const string AyudaInicio = "MEGALOPOLIS · SUBURBIOS";
        public const string SubtituloDoc = "—Aguanta, mijo. Esto no te va a doler… mucho.";
        public const string SubtituloInventor = "—Esa pistola está de museo. Ya quedó.";
        public const string FinAyudaInventor = ": MÁS DAÑO Y MÁS BALAS";
        public const string SubtituloTianguis = "—Los SKULLS se creen dueños del Tianguis";
        public const string AyudaTianguis = "EN EL MERCADO VENDEN ARMAS · EL CIC LAS MEJORA";
        public const string PrefijoTianguisTuyo = "EL TIANGUIS ES DE ";
        public const string IdDoc = "doc";
        public const string IdInventor = "inventor";
        public const string IdHacker = "hacker";
        public static readonly string[] SubtitulosHacker =
        {
            "—Rastreé el dinero de los SNAKES.",
            "—Les paga NOVA. Y también les da las armas.",
            "—Si NOVA está detrás, esto apenas empieza."
        };
        private const float SegundosAyudaInicio = 4f;
        private const float SegundosSubtituloHacker = 4f;
        private const float SegundosAviso = 5f;
        private const int RespetoPorSkull = 10;
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
        [Tooltip("Los grupos de SNAKES de Neón Alto: apagados hasta el juego libre (T-418).")]
        [SerializeField] private GameObject[] gruposNeon = System.Array.Empty<GameObject>();
        [Tooltip("Los profes de la historia (T-418): EL DOC cura gratis, EL INVENTOR mejora la pistola gratis, EL HACKER descubre a NOVA.")]
        [SerializeField] private PersonajeApoyo doc;
        [SerializeField] private PersonajeApoyo inventor;
        [SerializeField] private PersonajeApoyo hacker;
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
        private float continuaraEn = -1f;
        private float finRevelacion = -1f;
        private bool textosTianguisPendientes;
        private int respetoMostrado = -1;
        private bool arrancado;
        private bool cargadoAntes;

        public PasoHistoria Paso => maquina.Paso;
        public bool Iniciado => arrancado;
        public PantallaHistoriaPandillas Pantalla => pantalla;
        public string ClaveGuardado => Clave;

        /// <summary>El objetivo del paso del Tianguis: un SKULL vencido son 10 de respeto ("VENCE A LOS SKULLS 3/7").</summary>
        public static string TextoTianguis(int vencidos, int total) => PrefijoTianguis + Mathf.Clamp(vencidos, 0, total) + "/" + total;

        /// <summary>Todos los objetivos de la historia, para revisar que quepan en <see cref="LargoMaximoObjetivo"/> (D-tony-240).</summary>
        public static string[] TextosObjetivo => new[]
        {
            TextoSalir, TextoDefender, TextoPistola, TextoChato, TextoDoc, TextoInventor, TextoTianguis(7, 7), TextoJefe, TextoHacker
        };

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
            foreach (GameObject grupo in gruposNeon)
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
            foreach (PersonajeApoyo p in Profes)
            {
                if (p != null)
                {
                    p.Usado += AlUsarProfe;
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
            foreach (PersonajeApoyo p in Profes)
            {
                if (p != null)
                {
                    p.Usado -= AlUsarProfe;
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
            if (maquina.Paso == PasoHistoria.Despierta)
            {
                pantalla.MostrarAyuda(AyudaInicio, SegundosAyudaInicio);
            }
        }

        private PersonajeApoyo[] Profes => new[] { doc, inventor, hacker };

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
                    // Los textos del Tianguis esperan a que se lea lo que dijo EL INVENTOR.
                    if (textosTianguisPendientes && pantalla.Subtitulo.Length == 0 && pantalla.Ayuda.Length == 0)
                    {
                        textosTianguisPendientes = false;
                        pantalla.MostrarSubtitulo(SubtituloTianguis, 5f);
                        pantalla.MostrarAyuda(AyudaTianguis, 10f);
                    }
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
                        IPerfilJugador perfil = Registro<IPerfilJugador>.Actual;
                        string nombrePandilla = perfil != null && !string.IsNullOrEmpty(perfil.NombrePandilla) ? perfil.NombrePandilla : ColoresPandilla.NombrePredeterminado;
                        pantalla.MostrarAyuda(PrefijoTianguisTuyo + nombrePandilla.ToUpperInvariant(), SegundosAviso);
                        Avisar(AvisoHistoria.TianguisConquistado);
                    }
                    break;
                case PasoHistoria.Continuara:
                    if (continuaraEn >= 0f && Time.unscaledTime >= continuaraEn && !pantalla.ContinuaraVisible)
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
                FijarObjetivo(TextoTianguis(respeto / RespetoPorSkull, respetoJefe / RespetoPorSkull), PosicionTianguis());
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

        // El profe ya hizo su regalo (si tocaba); aquí solo avanza el paso que le toca.
        private void AlUsarProfe(string id)
        {
            switch (maquina.Paso)
            {
                case PasoHistoria.Doc when id == IdDoc:
                    Avisar(AvisoHistoria.DocUsado);
                    break;
                case PasoHistoria.Inventor when id == IdInventor:
                    Avisar(AvisoHistoria.InventorUsado);
                    break;
                case PasoHistoria.NeonAlto when id == IdHacker:
                    Avisar(AvisoHistoria.HackerUsado);
                    break;
            }
        }

        // EL DOC: te cura sin cobrar.
        private void RegaloDoc()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            VidaAPie vida = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<VidaAPie>() : null;
            if (vida != null)
            {
                vida.Restaurar();
            }
            pantalla.MostrarSubtitulo(SubtituloDoc, SegundosAviso);
        }

        // EL INVENTOR: la pistola sube a PISTOLA I sin cobrar.
        private void RegaloInventor()
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            DatosArma mejora = arsenal != null ? arsenal.DarMejora(ArmaAPie.Pistola) : null;
            if (mejora == null)
            {
                Debug.LogWarning("[Progresion] DirectorPandillas: EL INVENTOR no pudo mejorar la pistola (falta el arsenal o ya está al máximo).", this);
            }
            pantalla.MostrarSubtitulo(SubtituloInventor, SegundosAviso);
            pantalla.MostrarAyuda((mejora != null ? mejora.Nombre.ToUpperInvariant() : "PISTOLA I") + FinAyudaInventor, SegundosAviso);
        }

        // EL HACKER: tres subtítulos seguidos; CONTINUARÁ sale al terminar el último.
        private void RegaloHacker()
        {
            finRevelacion = Time.unscaledTime + SubtitulosHacker.Length * SegundosSubtituloHacker;
            StartCoroutine(Revelar());
        }

        private IEnumerator Revelar()
        {
            foreach (string linea in SubtitulosHacker)
            {
                pantalla.MostrarSubtitulo(linea, SegundosSubtituloHacker);
                yield return new WaitForSecondsRealtime(SegundosSubtituloHacker);
            }
        }

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
            Prender(gruposTianguis, paso >= PasoHistoria.Tianguis);
            Prender(gruposNeon, paso == PasoHistoria.Libre);
            if (chato != null)
            {
                chato.LiberadoPorHistoria = paso == PasoHistoria.Chato;
            }
            if (doc != null)
            {
                doc.DarRegalo(paso == PasoHistoria.Doc ? RegaloDoc : (System.Action)null);
            }
            if (inventor != null)
            {
                inventor.DarRegalo(paso == PasoHistoria.Inventor ? RegaloInventor : (System.Action)null);
            }
            if (hacker != null)
            {
                hacker.DarRegalo(paso == PasoHistoria.NeonAlto ? RegaloHacker : (System.Action)null);
            }
            continuaraEn = paso == PasoHistoria.Continuara ? Mathf.Max(finRevelacion, Time.unscaledTime + segundosContinuara) : -1f;

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
                case PasoHistoria.Doc:
                    FijarObjetivo(TextoDoc, doc != null ? doc.Posicion : Vector3.zero);
                    break;
                case PasoHistoria.Inventor:
                    FijarObjetivo(TextoInventor, inventor != null ? inventor.Posicion : Vector3.zero);
                    break;
                case PasoHistoria.Tianguis:
                    textosTianguisPendientes = true;
                    ActualizarRespeto();
                    break;
                case PasoHistoria.Jefe:
                    FijarObjetivo(TextoJefe, PosicionJefe());
                    break;
                case PasoHistoria.NeonAlto:
                    FijarObjetivo(TextoHacker, hacker != null ? hacker.Posicion : Vector3.zero);
                    break;
                default:
                    QuitarObjetivo();
                    break;
            }
        }

        private static void Prender(GameObject[] grupos, bool activo)
        {
            foreach (GameObject g in grupos)
            {
                if (g != null && g.activeSelf != activo)
                {
                    g.SetActive(activo);
                }
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
            if (leidos == null)
            {
                Debug.LogWarning("[Progresion] DirectorPandillas: el guardado no se entiende; no se cambia nada.");
                return;
            }
            if (!MaquinaHistoriaPandillas.TryLeer(leidos.paso, out PasoHistoria paso))
            {
                // Un paso que ya no existe (guardado viejo): la historia empieza de nuevo.
                Debug.LogWarning("[Progresion] DirectorPandillas: el paso '" + leidos.paso + "' no existe; empieza en Despierta.");
                paso = PasoHistoria.Despierta;
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
