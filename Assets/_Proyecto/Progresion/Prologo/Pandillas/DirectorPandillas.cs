using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.UI.Historia;
using MechaCyberpunk.Progresion.Partida;
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
        public const string TextoPistola = "RECOGE LA PISTOLA"; // T-718: sin la tecla escrita: el HUD muestra la real en el aviso de interacción
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
        [Tooltip("T-545: modelo de la pistola que suelta el SKULL (W01_Pistola); vacío arma una de piezas.")]
        [SerializeField] private GameObject modeloPistola;
        private Vector3 ultimaCaida;
        private bool hayCaida;
        private float proteccionHasta = -1f;
        private float continuaraEn = -1f;
        private float finRevelacion = -1f;
        private bool textosTianguisPendientes;
        private int respetoMostrado = -1;
        private bool arrancado;
        private bool cargadoAntes;
        private AsaltoDinero asalto;
        private int dineroRobadoGuardado;

        public PasoHistoria Paso => maquina.Paso;
        public bool Iniciado => arrancado;
        public PantallaHistoriaPandillas Pantalla => pantalla;
        public string ClaveGuardado => Clave;

        /// <summary>El objetivo del paso del Tianguis: un SKULL vencido son 10 de respeto ("VENCE A LOS SKULLS 3/7").</summary>
        public static string TextoTianguis(int vencidos, int total) => PrefijoTianguis + Mathf.Clamp(vencidos, 0, total) + "/" + total;

        /// <summary>Todos los objetivos de la historia, para revisar que quepan en <see cref="LargoMaximoObjetivo"/> (D-tony-240).</summary>
        public static string[] TextosObjetivo => new[]
        {
            TextoSalir, AsaltoDinero.TextoRobarCarro, AsaltoDinero.TextoPerseguir, AsaltoDinero.TextoMaletin, AsaltoDinero.TextoMaletinEnCarro, TextoDefender, TextoPistola, TextoChato, TextoDoc, TextoInventor, TextoTianguis(7, 7), TextoJefe, TextoHacker
        };

        /// <summary>T-716: el lugar de Core al que apunta el objetivo si el destino de la escena falta. Nunca una coordenada fija.</summary>
        public const IdLugar LugarRespaldo = IdLugar.Casa;

        /// <summary>T-716: la frase del objetivo de un paso (vacía en Continuara y Libre, que no llevan objetivo). El Tianguis usa <see cref="TextoTianguis"/>.</summary>
        public static string TextoDelPaso(PasoHistoria paso)
        {
            switch (paso)
            {
                case PasoHistoria.Despierta: return TextoSalir;
                case PasoHistoria.Asalto: return AsaltoDinero.TextoRobarCarro;
                case PasoHistoria.PrimeraPelea: return TextoDefender;
                case PasoHistoria.Pistola: return TextoPistola;
                case PasoHistoria.Chato: return TextoChato;
                case PasoHistoria.Doc: return TextoDoc;
                case PasoHistoria.Inventor: return TextoInventor;
                case PasoHistoria.Tianguis: return TextoTianguis(0, 7);
                case PasoHistoria.Jefe: return TextoJefe;
                case PasoHistoria.NeonAlto: return TextoHacker;
                default: return string.Empty;
            }
        }

        /// <summary>T-716: true si el paso lleva objetivo en el HUD y los mapas (todos menos Continuara y Libre).</summary>
        public static bool LlevaObjetivo(PasoHistoria paso) => TextoDelPaso(paso).Length > 0;

        private void Awake()
        {
            pantalla = gameObject.AddComponent<PantallaHistoriaPandillas>();
            pantalla.ContinuaraCerrada += AlCerrarContinuara;
            asalto = gameObject.AddComponent<AsaltoDinero>(); // T-717: la historia del dinero
            asalto.Configurar(pantalla);
            asalto.Terminado += AlTerminarAsalto;
            foreach (GameObject grupo in gruposTianguis)
            {
                if (grupo != null)
                {
                    grupo.SetActive(false);
                    PandillaAPie pandilla = grupo.GetComponent<PandillaAPie>();
                    if (pandilla != null)
                    {
                        pandilla.ModoMision = true; // T-713: salen todos a la vez, no reaparecen
                    }
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
            if (asalto != null)
            {
                asalto.Terminado -= AlTerminarAsalto;
            }
            if (arma != null)
            {
                arma.Tomada -= AlTomarArma;
            }
        }

        private IEnumerator Start()
        {
            // T-413: en el título la historia espera (EMPEZAR o CONTINUAR recargan la escena).
            while (FlujoPartida.EnTitulo)
            {
                yield return null;
            }
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
            // T-449: los mensajes del teléfono saben cuándo terminó la historia de la demo (esta máquina, no IHistoria).
            Registro<ServicioMensajes>.Actual?.FijarHistoriaTerminada(maquina.Paso == PasoHistoria.Libre);
#if !DEMO_FINAL
            Keyboard teclado = Keyboard.current;
            if (teclado != null && !Pausa.Activa && teclado.f6Key.wasPressedThisFrame && maquina.Saltar())
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
                    // T-930: ya no hay aliados que reclutar; el paso del Chato avanza solo.
                    Avisar(AvisoHistoria.ChatoUnido);
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
                    CompletarRespetoSiCayeronTodos(territorio);
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

        // T-713: si ya cayeron todos los SKULLS del Tianguis (en modo misión no reaparecen) y el respeto no llegó al umbral del jefe (alguno cayó fuera de la
        // zona, o hay menos que 7), se completa: la misión no se puede trabar por falta de SKULLS.
        private void CompletarRespetoSiCayeronTodos(ITerritorio territorio)
        {
            if (territorio == null || gruposTianguis.Length == 0)
            {
                return;
            }
            foreach (GameObject g in gruposTianguis)
            {
                PandillaAPie pandilla = g != null ? g.GetComponent<PandillaAPie>() : null;
                if (pandilla == null || !pandilla.ModoMision || !pandilla.TodosVencidos)
                {
                    return;
                }
            }
            TryPosicionTianguis(out Vector3 punto);
            int seguridad = 20;
            while (!territorio.JefeDisponible(zona) && territorio.Dueno(zona) != IdPandilla.Jugador && seguridad-- > 0)
            {
                territorio.RegistrarVencido(punto, IdPandilla.Calaveras, false);
            }
        }

        private void ActualizarRespeto()
        {
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            int respeto = territorio != null ? territorio.Respeto(zona) : 0;
            if (respeto != respetoMostrado)
            {
                respetoMostrado = respeto;
                FijarObjetivo(TextoTianguis(respeto / RespetoPorSkull, respetoJefe / RespetoPorSkull), TryPosicionTianguis(out Vector3 tianguis), tianguis);
            }
        }

        private bool TryPosicionTianguis(out Vector3 posicion)
        {
            foreach (GameObject g in gruposTianguis)
            {
                if (g != null)
                {
                    posicion = g.transform.position;
                    return true;
                }
            }
            posicion = Vector3.zero;
            return false;
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

        private void AlTerminarAsalto() => Avisar(AvisoHistoria.DineroRecuperado);

        /// <summary>T-717: el asalto de la historia del dinero (para las pruebas).</summary>
        public AsaltoDinero Asalto => asalto;

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
            if (paso != PasoHistoria.Asalto && asalto != null)
            {
                asalto.Cancelar(); // otro paso: se quita la cinemática y el dinero vuelve si seguía en manos del ladrón
            }
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
                    FijarObjetivo(TextoSalir, puertaCasa != null, puertaCasa != null ? puertaCasa.position : default);
                    break;
                case PasoHistoria.Asalto:
                    int reponer = dineroRobadoGuardado; // T-728: un guardado viejo (T-717) a medio asalto: el dinero que te habían quitado vuelve
                    dineroRobadoGuardado = 0;
                    asalto.Iniciar(reponer); // pone su propio objetivo (robarte un carro y luego perseguirlos)
                    break;
                case PasoHistoria.PrimeraPelea:
                    FijarObjetivo(TextoDefender, donaLupe != null, donaLupe != null ? donaLupe.position : default);
                    pantalla.MostrarSubtitulo("—¡Ya déjenla!", 4f);
                    break;
                case PasoHistoria.Pistola:
                    SoltarPistola();
                    FijarObjetivo(TextoPistola, arma != null, arma != null ? arma.transform.position : default);
                    break;
                case PasoHistoria.Chato:
                    QuitarObjetivo(); // T-930: paso de paso, sin aliado que buscar
                    pantalla.MostrarAyuda(AyudaApuntarYDisparar(), 8f);
                    break;
                case PasoHistoria.Doc:
                    FijarObjetivo(TextoDoc, doc != null, doc != null ? doc.Posicion : default);
                    break;
                case PasoHistoria.Inventor:
                    FijarObjetivo(TextoInventor, inventor != null, inventor != null ? inventor.Posicion : default);
                    break;
                case PasoHistoria.Tianguis:
                    textosTianguisPendientes = true;
                    ActualizarRespeto();
                    break;
                case PasoHistoria.Jefe:
                    FijarObjetivo(TextoJefe, TryPosicionJefe(out Vector3 jefe), jefe);
                    break;
                case PasoHistoria.NeonAlto:
                    FijarObjetivo(TextoHacker, hacker != null, hacker != null ? hacker.Posicion : default);
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

        private bool TryPosicionJefe(out Vector3 posicion)
        {
            foreach (JefePandilla j in FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (j.Zona == zona)
                {
                    posicion = j.transform.position;
                    return true;
                }
            }
            return TryPosicionTianguis(out posicion);
        }

        private void SoltarPistola()
        {
            if (arma != null)
            {
                return;
            }
            Vector3 punto = hayCaida ? ultimaCaida : (donaLupe != null ? donaLupe.position : transform.position);
            GameObject objeto = new GameObject("PistolaEnPiso");
            objeto.transform.position = punto + Vector3.up * 0.1f;
            objeto.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            if (modeloPistola != null)
            {
                // T-545: la pistola W01 de Tony, acostada de lado en el piso.
                GameObject w = Instantiate(modeloPistola, objeto.transform);
                w.name = "W01";
                w.transform.localPosition = Vector3.up * 0.03f;
                w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            else
            {
                ArmarModeloPistola(objeto.transform);
            }
            arma = objeto.AddComponent<ArmaEnPiso>();
            arma.Tomada += AlTomarArma;
        }

        private const float LargoCorredera = 0.26f;
        private const float EmisionPistola = 2.5f;

        /// <summary>T-507: pistola de piezas (corredera, cañón, empuñadura y tira de neón) con material de arma, en vez de un cubo amarillo.</summary>
        private static void ArmarModeloPistola(Transform raiz)
        {
            Shader sombreador = Shader.Find("Universal Render Pipeline/Lit");
            Material metal = new Material(sombreador) { name = "PistolaMetal" };
            metal.SetColor("_BaseColor", new Color(0.14f, 0.15f, 0.18f));
            metal.SetFloat("_Metallic", 0.85f);
            metal.SetFloat("_Smoothness", 0.7f);
            Material grip = new Material(sombreador) { name = "PistolaEmpunadura" };
            grip.SetColor("_BaseColor", new Color(0.22f, 0.13f, 0.09f));
            grip.SetFloat("_Smoothness", 0.3f);
            Material neon = new Material(sombreador) { name = "PistolaNeon" };
            neon.SetColor("_BaseColor", new Color(0.05f, 0.4f, 0.4f));
            neon.EnableKeyword("_EMISSION");
            neon.SetColor("_EmissionColor", new Color(0.1f, 0.9f, 0.85f) * EmisionPistola);

            AgregarPieza(raiz, PrimitiveType.Cube, "Corredera", new Vector3(0f, 0.07f, 0.02f), new Vector3(0.05f, 0.055f, LargoCorredera), Vector3.zero, metal);
            AgregarPieza(raiz, PrimitiveType.Cylinder, "Canon", new Vector3(0f, 0.065f, LargoCorredera * 0.5f + 0.04f), new Vector3(0.025f, 0.04f, 0.025f), new Vector3(90f, 0f, 0f), metal);
            AgregarPieza(raiz, PrimitiveType.Cube, "Empunadura", new Vector3(0f, 0.0f, -0.07f), new Vector3(0.045f, 0.13f, 0.06f), new Vector3(-14f, 0f, 0f), grip);
            AgregarPieza(raiz, PrimitiveType.Cube, "TiraNeon", new Vector3(0.027f, 0.075f, 0.02f), new Vector3(0.006f, 0.012f, LargoCorredera * 0.8f), Vector3.zero, neon);
        }

        private static void AgregarPieza(Transform raiz, PrimitiveType tipo, string nombre, Vector3 pos, Vector3 escala, Vector3 giro, Material material)
        {
            GameObject pieza = GameObject.CreatePrimitive(tipo);
            pieza.name = nombre;
            Destroy(pieza.GetComponent<Collider>());
            pieza.transform.SetParent(raiz, false);
            pieza.transform.localPosition = pos;
            pieza.transform.localEulerAngles = giro;
            pieza.transform.localScale = escala;
            pieza.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void AlTomarArma(ArmaEnPiso tomada)
        {
            arma = null;
            Avisar(AvisoHistoria.PistolaTomada);
        }

        // T-716: el destino sale de la escena; si falta, el objetivo apunta al lugar de Core de respaldo, nunca a (0, 0, 0).
        /// <summary>T-718: "CLIC DER. APUNTA · CLIC DISPARA" con las teclas reales del Input System (si el jugador las cambió, ve las suyas).</summary>
        public static string AyudaApuntarYDisparar()
        {
            string apuntar = MechaCyberpunk.UI.Hud.TeclasHud.Texto(MechaCyberpunk.UI.Hud.TeclasHud.Buscar("Apuntar"), "Clic der.");
            string disparar = MechaCyberpunk.UI.Hud.TeclasHud.Texto(MechaCyberpunk.UI.Hud.TeclasHud.Buscar("Atacar"), "Clic");
            return (apuntar + " APUNTA · " + disparar + " DISPARA").ToUpperInvariant();
        }

        private static void FijarObjetivo(string texto, bool hayPosicion, Vector3 posicion)
        {
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Fijar(hayPosicion
                ? new Objetivo(texto, posicion)
                : new Objetivo(texto, LugarRespaldo));
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
            public int dineroRobado; // T-717: dinero que tiene el asaltante en un guardado a medio asalto (solo se escribe si hay)
        }

        [System.Serializable]
        private class DatosPaso
        {
            public string paso;
        }

        public string Guardar()
        {
            // T-728: el asalto ya no te quita dinero: solo se guarda el paso (el campo dineroRobado de guardados viejos se sigue leyendo).
            return JsonUtility.ToJson(new DatosPaso { paso = maquina.Paso.ToString() });
        }

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
            dineroRobadoGuardado = paso == PasoHistoria.Asalto ? Mathf.Max(0, leidos.dineroRobado) : 0;
            cargadoAntes = true;
            if (arrancado)
            {
                Aplicar();
            }
        }
    }
}
