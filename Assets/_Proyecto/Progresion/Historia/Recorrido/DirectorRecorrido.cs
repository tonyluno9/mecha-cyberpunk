using System;
using System.Collections;
using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.UI.Armas;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion.Opciones;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using MechaCyberpunk.UI.Clinica;
using MechaCyberpunk.UI.Creditos;
using MechaCyberpunk.UI.Tiendas;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1625 (T10): lleva la historia nueva de 19 pasos (tabla 10.2 de docs/tarea-historia-en-juego.md) y reemplaza el recorrido viejo de
    /// <see cref="DirectorPandillas"/> (que queda en la escena desconectado, no borrado; sin aliados: EL CHATO, LA GÜERA y EL TOPO no entran).
    /// Se activa solo con NUEVA PARTIDA o al cargar una partida que ya traiga la clave <c>historia_cybercity</c>; cualquier otra forma de abrir la
    /// ciudad (pruebas viejas, Play directo) conserva el recorrido viejo. Reutiliza de la escena lo del viejo: el Doc, el Inventor, el Hacker, los
    /// grupos del Tianguis y de Neón Alto, la clínica, la tienda y el recinto del jefe final. Las cinemáticas salen de <see cref="CinematicaPendiente"/>
    /// y las peleas de los pasos 3, 12, 13, 15 (Nova permitida) y 17 activan <see cref="MisionProtegida"/>.
    /// Para saltar a un paso en Play: <c>DirectorRecorrido.SaltarAPaso(12)</c> (F8 pasa al siguiente fuera de la demo final).
    /// </summary>
    public class DirectorRecorrido : MonoBehaviour, IGuardable
    {
        public const string Clave = "historia_cybercity";
        private const float RadioMolestia = 14f;
        private const float RadioRanura = 4f;
        private const float RadioTorre = 35f;
        private const float DistanciaAlejarseInventor = 14f;
        private const float MetrosAtrasDeLaRanura = 7f;
        private const float SegundosEntreNaves = 50f;
        private const float SegundosPrimeraNave = 4f;
        private const int EstrellasNave = 5;
        private const float HoraNoche = ReglaNocheFija.HoraNoche; // T-1685: la noche fija usa la misma hora
        private const float LluviaNoche = 0.7f;
        private const float SegundosFin = 5f;
        private const float SegundosEnNegroLevantarse = 1.8f;
        private const int RespetoPorSkull = 10;
        private const float AlturaRanura = 3f;
        private const float AnchoRanura = 3.2f;
        private const float GrosorRanura = 0.5f;
        private static readonly Color ColorTapia = new Color(0.12f, 0.13f, 0.15f);

        [Serializable]
        private class DatosGuardados
        {
            public string paso;
            public bool hambre;
            public string avisos;
        }

        public static DirectorRecorrido Actual { get; private set; }

        /// <summary>Pruebas: simula la build Demo (con DEMO_FINAL ya lo es).</summary>
        public static bool ForzarDemo { get; set; }

        public static bool EsDemo
        {
            get
            {
#if DEMO_FINAL
                return true;
#else
                return ForzarDemo;
#endif
            }
        }

        public PasoRecorrido Paso => paso;

        /// <summary>VR16: true si la última vez que se entró a "Lo molestan" se saltó por estar en VR (lo leen las pruebas).</summary>
        public bool MolestiaSaltadaEnVR { get; private set; }
        public bool Iniciado => iniciado;
        public string ClaveGuardado => Clave;

        /// <summary>Se avisa con el paso nuevo cada vez que cambia (pruebas y depuración).</summary>
        public event Action<PasoRecorrido> PasoCambio;

        private PasoRecorrido paso = PasoRecorrido.Ninguno;
        private int sub;
        private bool modoNueva;
        private bool cargada;
        private bool iniciado;
        private bool saltado;
        private DatosRecorrido datos;
        private DirectorPandillas viejo;
        private ServicioHistoria historia;
        private MolestiaSkulls molestia;
        private PeleaInicioSkulls pelea; // VR24
        private float inicioDespierta = float.MaxValue;
        // T-1656: los SKULLS salen poco después de que despierta (ya de pie), para esperarlo a media calle antes de que se aleje.
        private const float SegundosHastaMolestia = 6f;
        private const float MetrosEntraProteccionTianguis = 80f;
        private const float MetrosSaleProteccionTianguis = 110f;
        private GameObject ranuraCerrada;
        private RecintoJefe recinto;
        private bool jefeMostrado;
        private int implantesAntes;
        private float proximaNave;
        private Vector3 inicioPos;
        private Vector3 inicioAdelante = Vector3.forward;
        private bool inicioConocido;
        private bool sinMarcadorAvisado;
        // T-1662 (T44): el paso del hambre (comprar en una máquina antes de las peleas con pandilleros) y el aviso de la rueda de armas.
        private bool hambreResuelta;
        private bool ruedaAvisada;
        private Expendedora maquinaHambre;
        private IRelojMundo relojVigilado;
        // T-78: aviso suave si se aleja del Doc y protección del Tianguis por cercanía.
        private float proximoAvisoLejos;
        private float distanciaDocAntes;
        private float proximaMuestraDoc;
        private bool tianguisProtegido;

        // ---------- Arranque ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Actual = null;
            ForzarDemo = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SuscribirEscena()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AlArrancar() => Intentar();

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo) => Intentar();

        // Después de Awake y antes de Start de la escena: el director viejo todavía no arrancó y se puede apagar.
        private static void Intentar()
        {
            if (Actual != null || FlujoPartida.EnTitulo || Object.FindFirstObjectByType<DirectorPandillas>() == null)
            {
                return;
            }
            AccionPartida pendiente = FlujoPartida.Pendiente;
            bool nueva = pendiente == AccionPartida.Nueva;
            bool carga = pendiente == AccionPartida.Cargar && ArchivoTieneElRecorrido(FlujoPartida.RanuraPendiente);
            if (nueva || carga)
            {
                Asegurar().modoNueva = nueva;
            }
        }

        private static bool ArchivoTieneElRecorrido(int ranura)
        {
            try
            {
                ServicioGuardado servicio = ServicioGuardado.Actual;
                if (servicio == null)
                {
                    return false;
                }
                string ruta = servicio.RutaRanura(ranura);
                return File.Exists(ruta) && File.ReadAllText(ruta).Contains(Clave);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Historia] No se pudo mirar si la partida trae el recorrido nuevo: " + e.Message);
                return false;
            }
        }

        /// <summary>Crea el director si no existe (apaga el recorrido viejo). Lo usan el arranque y la depuración.</summary>
        public static DirectorRecorrido Asegurar()
        {
            if (Actual != null)
            {
                return Actual;
            }
            return new GameObject("DirectorRecorrido").AddComponent<DirectorRecorrido>();
        }

        /// <summary>Depuración: salta al paso <paramref name="numero"/> (1 a 19, 20 = juego libre). Crea el director si hace falta.</summary>
        public static bool SaltarAPaso(int numero)
        {
            if (!Enum.IsDefined(typeof(PasoRecorrido), numero) || numero <= 0)
            {
                Debug.LogWarning("[Historia] No existe el paso " + numero);
                return false;
            }
            Asegurar().SaltarA((PasoRecorrido)numero);
            return true;
        }

        /// <summary>T-1662: depuración. Paso actual, objetivo, distancia al objetivo (invocable por reflexión).</summary>
        public static string DepurarHistoria()
        {
            DirectorRecorrido d = Actual;
            if (d == null)
            {
                return "[Historia] sin director (recorrido viejo o título)";
            }
            IObjetivos objetivos = Registro<IObjetivos>.Actual;
            string objetivo = objetivos != null && objetivos.HayObjetivo ? objetivos.Actual.Texto : "(ninguno)";
            return "[Historia] paso=" + (int)d.paso + " " + d.paso + " sub=" + d.sub + " objetivo=\"" + objetivo + "\" hambreResuelta=" + d.hambreResuelta
                + " ruedaAvisada=" + d.ruedaAvisada
                + " avisoTutorial=\"" + (AvisoTutorialHistoria.Actual != null && AvisoTutorialHistoria.Actual.Visible ? AvisoTutorialHistoria.Actual.TextoActual : "") + "\"";
        }

        /// <summary>T-1662: depuración de las máquinas expendedoras: cuántas hay, si venden, dónde y a qué distancia del jugador.</summary>
        public static string DepurarExpendedora()
        {
            Transform jugador = JugadorRaiz();
            StringBuilder sb = new StringBuilder("[Expendedoras]");
            Expendedora[] todas = FindObjectsByType<Expendedora>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            sb.Append(" venden=").Append(todas.Length);
            foreach (Expendedora e in todas)
            {
                sb.Append(" | ").Append(e.name).Append(" en ").Append(e.transform.position.ToString("F0"));
                if (jugador != null)
                {
                    sb.Append(" a ").Append(Plano(jugador.position, e.Posicion).ToString("F0")).Append(" m");
                }
                sb.Append(" ofertas=").Append(e.EnVenta.Count).Append(" abierta=").Append(e.PantallaAbierta);
            }
            if (DirectorRecorrido.Actual != null && Actual.maquinaHambre != null)
            {
                sb.Append(" | maquina del hambre: ").Append(Actual.maquinaHambre.name);
            }
            return sb.ToString();
        }

        private void Awake()
        {
            if (Actual != null && Actual != this)
            {
                Destroy(gameObject);
                return;
            }
            Actual = this;
            viejo = FindFirstObjectByType<DirectorPandillas>(FindObjectsInactive.Include);
            if (viejo != null)
            {
                viejo.enabled = false; // el recorrido viejo queda desconectado
            }
            DirectorPrologo prologo = FindFirstObjectByType<DirectorPrologo>(FindObjectsInactive.Include);
            if (prologo != null)
            {
                prologo.enabled = false;
            }
            // T-51: sin línea de ruta en el piso (se quitó la guía); solo queda el aviso del tutorial.
            if (GetComponent<AvisoTutorialHistoria>() == null)
            {
                gameObject.AddComponent<AvisoTutorialHistoria>();
            }
            datos = Resources.Load<DatosRecorrido>(DatosRecorrido.NombreRecurso);
            if (datos == null)
            {
                Debug.LogWarning("[Historia] Falta Resources/DatosRecorrido: se usan las cinemáticas de la tabla y no hay SKULLS en el paso 3 (corre CreadorRecorrido.Aplicar).");
            }
        }

        private void OnEnable()
        {
            Registro<IGuardable>.Agregar(this);
            AvisosUnicos.Cambio += GuardarPartida; // T-62: un aviso nuevo (o el tutorial de prótesis completo) se guarda en la partida
            CandadosMundo.Negado += AvisarNegado;
        }

        private void OnDisable()
        {
            Registro<IGuardable>.Quitar(this);
            AvisosUnicos.Cambio -= GuardarPartida;
            CandadosMundo.Negado -= AvisarNegado;
        }

        private void OnDestroy()
        {
            if (Actual == this)
            {
                Actual = null;
                CandadosMundo.Liberar(); // T-78: sin recorrido (título, otra partida) el mundo no queda con candados
                PestanasBancoTino.BloqueoSalida = null;
                PestanasBancoTino.TutorialGuiado = false; // T-84
            }
            DejarDeVigilarReloj();
            Cancelar();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }
            float limite = Time.realtimeSinceStartup + 3f;
            while (Registro<IJugador>.Actual == null && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            if (saltado)
            {
                yield break;
            }
            historia = Registro<IHistoria>.Actual as ServicioHistoria;
            historia?.Avanzar(EtapaPrologo.Libre); // el mundo ya no sigue las etapas viejas del prólogo
            iniciado = true;
            if (paso == PasoRecorrido.Ninguno)
            {
                if (modoNueva)
                {
                    AvisosUnicos.Reiniciar();
                    FaseJuego.Reiniciar(FaseJuego.Fase.Guiada); // T-historia-libre-23nov
                    IrA(PasoRecorrido.Recuerdo);
                }
                yield break;
            }
            Reanudar();
        }

        private void Reanudar()
        {
            PasoRecorrido p = paso;
            if (p == PasoRecorrido.Recuerdo)
            {
                p = PasoRecorrido.Despierta;
            }
            else if (GestorCandados.DesconectadoPorModoLibre(p))
            {
                p = PasoRecorrido.Libre; // T-historia-libre-23nov: una partida guardada en la historia vieja (chica en adelante) sigue en el modo libre
            }
            if (p != PasoRecorrido.Libre)
            {
                FaseJuego.Reiniciar(FaseJuego.Fase.Guiada);
            }
            Debug.Log("[Historia] Reanuda el recorrido en el paso " + (int)p + " (" + p + ")");
            reanudando = true;
            try
            {
                IrA(p);
            }
            finally
            {
                reanudando = false;
            }
        }

        // Al reanudar una partida guardada se vuelve a entrar al paso. VR24: el dinero del maletín ya solo se da al vencer a los SKULLS del inicio.
        private bool reanudando;

        /// <summary>True mientras se vuelve a entrar al paso de una partida cargada (depuración y pruebas).</summary>
        public bool Reanudando => reanudando;

        // ---------- Guardado ----------

        public string Guardar() => JsonUtility.ToJson(new DatosGuardados { paso = paso.ToString(), hambre = hambreResuelta, avisos = AvisosUnicos.Serializar() });

        public void Cargar(string texto)
        {
            DatosGuardados leidos = null;
            try
            {
                leidos = string.IsNullOrWhiteSpace(texto) ? null : JsonUtility.FromJson<DatosGuardados>(texto);
            }
            catch (ArgumentException)
            {
                leidos = null;
            }
            if (leidos == null || !Enum.TryParse(leidos.paso, false, out PasoRecorrido leido) || !Enum.IsDefined(typeof(PasoRecorrido), leido))
            {
                Debug.LogWarning("[Historia] El recorrido no entendió el paso guardado; empieza de nuevo.");
                return;
            }
            paso = leido;
            cargada = true;
            hambreResuelta = leidos.hambre;
            AvisosUnicos.Cargar(leidos.avisos); // T-62: los avisos del tutorial que ya salieron no vuelven
            if (iniciado && !saltado)
            {
                Reanudar();
            }
        }

        private static void GuardarPartida()
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            if (servicio != null)
            {
                servicio.Guardar();
            }
        }

        // ---------- Cambio de paso ----------

        private void SaltarA(PasoRecorrido nuevo)
        {
            saltado = true;
            iniciado = true;
            if (historia == null)
            {
                historia = Registro<IHistoria>.Actual as ServicioHistoria;
                historia?.Avanzar(EtapaPrologo.Libre);
            }
            Cancelar();
            if (CinematicaPendiente.EnCurso)
            {
                CinematicaPendiente.SaltarActual();
            }
            IrA(nuevo);
        }

        private void Cancelar()
        {
            if (molestia != null)
            {
                molestia.Terminada -= AlTerminarMolestia;
                molestia.Cancelar();
                Destroy(molestia);
                molestia = null;
            }
            QuitarPelea();
            Expendedora.Compra -= AlComprarEnMaquina;
            maquinaHambre = null;
            StopAllCoroutines();
            MisionProtegida.Desactivar();
        }

        private void IrA(PasoRecorrido nuevo)
        {
            paso = nuevo;
            sub = 0;
            jefeMostrado = false;
            tianguisProtegido = false;
            PestanasBancoTino.BloqueoSalida = null;
            PestanasBancoTino.TutorialGuiado = false; // T-84
            GestorCandados.Aplicar(nuevo); // T-78: los candados del mundo según el paso (también al cargar la partida: Reanudar pasa por aquí)
            // T-1658: de recibir el dinero del maletín a salir del menú de prótesis del suero, la camilla del Doc no se usa; al terminar sale el tutorial de prótesis.
            if (nuevo < PasoRecorrido.Dinero || nuevo == PasoRecorrido.PeleaSkulls)
            {
                // T-80: antes del Doc no hay prótesis ni tutorial de Tab. Limpia lo que quedara de otra partida jugada sin cerrar el juego.
                MechaCyberpunk.UI.Clinica.PrimeraVisitaClinica.Fijar(false, false);
            }
            else if (nuevo >= PasoRecorrido.Dinero && nuevo <= PasoRecorrido.Suero)
            {
                MechaCyberpunk.UI.Clinica.PrimeraVisitaClinica.Iniciar();
            }
            else
            {
                MechaCyberpunk.UI.Clinica.PrimeraVisitaClinica.Terminar();
            }
            Debug.Log("[Historia] Paso " + (int)nuevo + ": " + nuevo);
            PasoCambio?.Invoke(nuevo);
            ActualizarMundo();
            Entrar();
        }

        // Lo que queda del mundo según el paso (se repite al reanudar): la ranura cerrada y la noche.
        private void ActualizarMundo()
        {
            if (paso >= PasoRecorrido.Dinero)
            {
                CerrarRanura();
            }
            if (NocheFijaAhora())
            {
                PonerNoche();
            }
            else if (paso > PasoRecorrido.Decision)
            {
                Reloj()?.Pausar(false);
            }
            if (paso > PasoRecorrido.Decision)
            {
                ClimaMundo.Lluvia = 0f;
            }
        }

        private void Entrar()
        {
            switch (paso)
            {
                case PasoRecorrido.Recuerdo: EntrarRecuerdo(); break;
                case PasoRecorrido.Despierta: EntrarDespierta(); break;
                case PasoRecorrido.Molestan: EntrarMolestan(); break;
                case PasoRecorrido.Maletin: EntrarMaletin(); break;
                case PasoRecorrido.Ranura: EntrarRanura(); break;
                case PasoRecorrido.Decision: EntrarDecision(); break;
                case PasoRecorrido.PeleaSkulls: EntrarPeleaSkulls(); break;
                case PasoRecorrido.Dinero: EntrarDinero(); break;
                case PasoRecorrido.Suero: EntrarSuero(); break;
                case PasoRecorrido.UsarProtesis: EntrarUsarProtesis(); break;
                case PasoRecorrido.Armas: EntrarArmas(); break;
                case PasoRecorrido.Inventor: EntrarInventor(); break;
                case PasoRecorrido.Chica: EntrarChica(); break;
                case PasoRecorrido.GuerraSkulls: EntrarGuerraSkulls(); break;
                case PasoRecorrido.GuerraSnakes: EntrarGuerraSnakes(); break;
                case PasoRecorrido.Hacker: EntrarHacker(); break;
                case PasoRecorrido.Robots: EntrarRobots(); break;
                case PasoRecorrido.Hermano: EntrarHermano(); break;
                case PasoRecorrido.JefeFinal: EntrarJefeFinal(); break;
                case PasoRecorrido.Puertas: EntrarPuertas(); break;
                case PasoRecorrido.Fin: EntrarFin(); break;
                case PasoRecorrido.Libre: EntrarLibre(); break;
            }
        }

        private void Cinematica(string id, Action enNegro, Action alTerminar)
        {
            DatosCinematica d = datos != null ? datos.Buscar(id) : null;
            if (d == null)
            {
                d = TablaRecorrido.CrearRespaldo(id);
            }
            CinematicaPendiente.Reproducir(d, enNegro, alTerminar);
        }

        // ---------- Pasos ----------

        // 1. C01 EL RECUERDO, antes de todo. En negro: de noche y el jugador tumbado frente a su casa.
        private void EntrarRecuerdo()
        {
            Cinematica("C01", () =>
            {
                PonerNoche();
                ColocarEnElInicio();
                PegarJugadorAlSuelo();
                VisualPersonaje visual = VisualDelJugador();
                ControlDelJugador()?.ActivarControl(false);
                if (visual != null)
                {
                    DejarTiradoEnElSuelo(visual);
                }
                paso = PasoRecorrido.Despierta; // el paso 2 ya cuenta mientras se levanta
            }, () =>
            {
                IrA(PasoRecorrido.Despierta);
                StartCoroutine(Levantarse());
            });
        }

        // 2. Despierta en la calle, frente a su casa de Suburbios (sin cuarto, decisión de Tony).
        private void EntrarDespierta()
        {
            inicioDespierta = Time.time;
            Fijar(TablaRecorrido.ObjetivoSalir, PosicionMolestia());
            GuardarPartida();
        }

        // 3. Dos SKULLS le salen al paso. Pelea protegida.
        private void EntrarMolestan()
        {
            MolestiaSaltadaEnVR = false;
            if (GestorCandados.SeSalta(PasoRecorrido.Molestan))
            {
                // VR16/VR24: no hay SKULLS que empujan ni aviso (en PC tampoco): se da por jugado y sigue igual que al terminar la escena (Maletín).
                Debug.Log("[Historia] VR24: se salta el paso 3 (Lo molestan).");
                Quitar();
                MolestiaSaltadaEnVR = true;
                AlTerminarMolestia();
                return;
            }
            MisionProtegida.Activar("historia: los SKULLS lo molestan", false);
            Quitar();
            if (molestia != null)
            {
                // T-1720 (T77): una sola escena de SKULLS a la vez: si ya había una, se quita antes de armar otra.
                molestia.Terminada -= AlTerminarMolestia;
                molestia.Cancelar();
                Destroy(molestia);
            }
            molestia = gameObject.AddComponent<MolestiaSkulls>();
            molestia.Terminada += AlTerminarMolestia;
            float distancia = datos != null ? datos.DistanciaAparicionSkulls : 10f;
            if (datos == null || !molestia.Iniciar(datos.PrefabsSkull, distancia, PosicionCallejonSkulls(), PosicionEsperaSkulls()))
            {
                Debug.LogWarning("[Historia] No se pudo armar la escena de los SKULLS (faltan los prefabs en DatosRecorrido o el jugador): se sigue con la historia.");
                AlTerminarMolestia();
            }
        }

        private void AlTerminarMolestia()
        {
            if (paso != PasoRecorrido.Molestan)
            {
                return;
            }
            if (molestia != null)
            {
                molestia.Terminada -= AlTerminarMolestia;
                Destroy(molestia);
                molestia = null;
            }
            MisionProtegida.Desactivar();
            IrA(PasoRecorrido.Maletin);
        }

        // 4. C02 EL MALETÍN.
        private void EntrarMaletin()
        {
            Cinematica("C02", () =>
            {
                PonerNoche();
                Reloj()?.Pausar(true);
                ClimaMundo.Lluvia = LluviaNoche;
            }, () => IrA(GestorCandados.Siguiente(PasoRecorrido.Maletin))); // VR24: directo a C03 (ya no se camina a la ranura)
        }

        // 5. VR24: ya no se juega (ir a la ranura del muro). Una partida guardada aquí sigue en C03 LA DECISIÓN.
        private void EntrarRanura()
        {
            Debug.Log("[Historia] VR24: el paso 5 (Ranura) ya no se juega: sigue en C03.");
            Quitar();
            IrA(GestorCandados.Reemplazo(PasoRecorrido.Ranura));
        }

        // 6. C03 LA DECISIÓN. VR24: en negro el jugador vuelve a su punto de inicio (donde aparece al empezar) y la ranura se cierra para el resto del juego.
        private void EntrarDecision()
        {
            Quitar();
            Cinematica("C03", () =>
            {
                ColocarEnElInicio();
                CerrarRanura();
            }, () => IrA(PasoRecorrido.PeleaSkulls));
        }

        // 6b. VR24: en el punto de inicio, 3 SKULLS lo encaran y pelean a puños (pelea protegida). Al vencerlos: Ƀ20,000 en el banco y VE CON EL DOC.
        private void EntrarPeleaSkulls()
        {
            ClimaMundo.Lluvia = 0f;
            MisionProtegida.Activar("historia: pelea con los SKULLS del inicio", false);
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Fijar(new Objetivo(TablaRecorrido.ObjetivoPeleaSkulls, IdLugar.Ninguno));
            QuitarPelea();
            ConocerInicio();
            Transform j = JugadorRaiz();
            Vector3 desde = j != null ? j.position : inicioPos;
            Vector3 adelante = j != null ? Vector3.ProjectOnPlane(j.forward, Vector3.up) : inicioAdelante;
            pelea = gameObject.AddComponent<PeleaInicioSkulls>();
            pelea.Terminada += AlTerminarPelea;
            if (datos == null || !pelea.Iniciar(datos.PrefabsSkull, desde, adelante))
            {
                Debug.LogWarning("[Historia] VR24: no se pudo armar la pelea de los SKULLS (faltan los prefabs en DatosRecorrido o el jugador): se sigue con la historia.");
                AlTerminarPelea();
                return;
            }
            GuardarPartida();
        }

        private void AlTerminarPelea()
        {
            if (paso != PasoRecorrido.PeleaSkulls)
            {
                return;
            }
            QuitarPelea(false);
            MisionProtegida.Desactivar();
            // El dinero del maletín se da aquí y solo aquí (EntrarDinero ya no lo da): una sola vez.
            Registro<ICartera>.Actual?.Abonar(TablaRecorrido.DineroMaletin, MotivoDinero.Mision);
            Avisar(string.Format(TablaRecorrido.AvisoBanco, Moneda.Texto(TablaRecorrido.DineroMaletin)), MechaCyberpunk.UI.Hud.TipoAviso.Dinero);
            IrA(GestorCandados.Siguiente(PasoRecorrido.PeleaSkulls));
        }

        // Quita el componente de la pelea; con <quitarSkulls> también a los SKULLS (al terminar se quedan tirados y se limpian solos).
        private void QuitarPelea(bool quitarSkulls = true)
        {
            if (pelea == null)
            {
                return;
            }
            pelea.Terminada -= AlTerminarPelea;
            if (quitarSkulls)
            {
                pelea.Cancelar();
            }
            else
            {
                pelea.Soltar();
            }
            Destroy(pelea);
            pelea = null;
        }

        // 7. A ver al Doc (los Ƀ20,000 del maletín llegan antes, al vencer a los SKULLS del inicio: VR24).
        private void EntrarDinero()
        {
            ClimaMundo.Lluvia = 0f;
            if (!NocheFijaAhora())
            {
                Reloj()?.Pausar(false);
            }
            // VR24: el dinero del maletín ya se dio al vencer a los SKULLS del inicio (AlTerminarPelea); aquí no, para no darlo dos veces.
            PersonajeApoyo doc = viejo != null ? viejo.Doc : null;
            if (doc == null)
            {
                Debug.LogWarning("[Historia] No hay Doc en la escena: se salta el paso 7.");
                IrA(PasoRecorrido.Suero);
                return;
            }
            Fijar(TablaRecorrido.ObjetivoDoc, doc.Posicion);
            doc.DarRegalo(() => IrA(PasoRecorrido.Suero));
            distanciaDocAntes = 0f;
            proximoAvisoLejos = 0f;
            proximaMuestraDoc = 0f;
            GuardarPartida();
        }

        // 8. C04 EL SUERO y luego la pantalla de mejoras.
        private void EntrarSuero()
        {
            PersonajeApoyo doc = viejo != null ? viejo.Doc : null;
            doc?.DarRegalo(null);
            IImplantes implantes = Registro<IImplantes>.Actual;
            implantesAntes = implantes != null ? implantes.Cantidad : 0;
            Cinematica("C04", () => VidaDelJugador()?.Restaurar(), () =>
            {
                sub = 1;
                if (doc != null)
                {
                    Fijar(TablaRecorrido.ObjetivoMejora, doc.Posicion);
                }
                if (Registro<IImplantes>.Actual == null || Registro<IImplantes>.Actual.Nivel(EspacioImplante.Piernas) > 0)
                {
                    IrA(PasoRecorrido.UsarProtesis); // sin servicio de implantes, o ya tiene las piernas (partida vieja): no hay nada que instalar
                    return;
                }
                MostradorClinica clinica = FindFirstObjectByType<MostradorClinica>();
                if (clinica != null && clinica.PuedeAbrirDesdeDoc)
                {
                    clinica.AbrirDesdeDoc();
                }
            });
        }

        // 9. T-59: con Tino, en el CIC, comprar el arma (su banco tiene COMPRAR y MEJORAR). En cuanto la tienes, sales a la calle y sigue la chica.
        // T-78: es obligatorio comprar un arma Y ponerle una mejora; el banco no deja salir hasta tener las dos (PestanasBancoTino.BloqueoSalida).
        private void EntrarArmas()
        {
            sub = TieneArma() && TieneMejoraDeArma() ? 1 : 0;
            PersonajeApoyo inventor = viejo != null ? viejo.Inventor : null;
            if (sub == 0)
            {
                Fijar(TablaRecorrido.ObjetivoTino, inventor != null ? inventor.Posicion : PosicionGrupos(viejo != null ? viejo.GruposTianguis : null));
                PestanasBancoTino.BloqueoSalida = FaltaEnElBanco;
                PestanasBancoTino.TutorialGuiado = true; // T-84: primera visita a Tino guiada paso a paso
                GuardarPartida();
                return;
            }
            IrA(PasoRecorrido.Libre); // T-historia-libre-23nov
        }

        private string FaltaEnElBanco() => paso == PasoRecorrido.Armas ? GestorCandados.FaltaEnElBanco(TieneArma(), TieneMejoraDeArma()) : string.Empty;

        // T-78: tras instalar las piernas del Doc, usarlas: Tab, PIERNAS y correr. La ventana del tutorial (TutorialProtesis, con ENTENDIDO) sale al
        // pisar la calle (IrA llama a PrimeraVisitaClinica.Terminar); el paso avanza solo cuando las piernas están activas y corre.
        private void EntrarUsarProtesis()
        {
            Quitar();
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Fijar(new Objetivo(ModoVR.Activo ? TablaRecorrido.ObjetivoUsarProtesisVR : TablaRecorrido.ObjetivoUsarProtesis, IdLugar.Ninguno));
            GuardarPartida();
        }

        private void ActualizarUsarProtesis(Transform jugador)
        {
            HabilidadesProtesis habilidades = HabilidadesProtesis.Actual;
            ControladorAPie controlador = jugador.GetComponentInChildren<ControladorAPie>();
            if (habilidades == null || controlador == null)
            {
                return;
            }
            bool activas = habilidades.Estado(HabilidadProtesis.Piernas) == EstadoHabilidad.Activa;
            if (GestorCandados.UsoPiernasCompleto(activas, controlador.PidiendoCorrer, controlador.Velocidad.magnitude))
            {
                IrA(PasoRecorrido.Armas);
            }
        }

        // T-78: el aviso suave "VE CON EL DOC" si se aleja mucho del Doc antes de recibir el suero. Sin paredes.
        private void ActualizarDinero(Transform jugador)
        {
            PersonajeApoyo doc = viejo != null ? viejo.Doc : null;
            if (doc == null || Time.time < proximaMuestraDoc)
            {
                return;
            }
            proximaMuestraDoc = Time.time + 1f;
            float distancia = Plano(jugador.position, doc.Posicion);
            if (Time.time >= proximoAvisoLejos && distanciaDocAntes > 0f && GestorCandados.DebeAvisarLejosDelDoc(distancia, distanciaDocAntes))
            {
                proximoAvisoLejos = Time.time + 8f;
                Avisar(GestorCandados.AvisoDoc, MechaCyberpunk.UI.Hud.TipoAviso.Neutro);
            }
            distanciaDocAntes = distancia;
        }

        // T-78: el candado le negó algo al jugador (carro, tienda...): aviso suave en el HUD.
        private static void AvisarNegado(string texto) => Avisar(texto, MechaCyberpunk.UI.Hud.TipoAviso.Neutro);

        // T-62: acabas de comprar el arma: sale el aviso de la rueda de armas (una sola vez; espera a que cierres el banco).
        private void AvisarRueda()
        {
            ruedaAvisada = true;
            AvisoTutorialHistoria aviso = AvisoTutorialHistoria.Actual;
            if (aviso != null)
            {
                string tecla = MechaCyberpunk.UI.Hud.TeclasHud.Texto(MechaCyberpunk.UI.Hud.TeclasHud.Buscar("Rueda"), "RUEDA (clic)");
                if (string.IsNullOrWhiteSpace(tecla))
                {
                    tecla = "RUEDA (clic)";
                }
                aviso.Mostrar(AvisosUnicos.Rueda, string.Format(TablaRecorrido.AvisoRueda, tecla.ToUpperInvariant()));
            }
        }

        // 10. Ya no hay cinemática del Inventor (T-59): Tino vende y mejora en su banco. Una partida vieja guardada aquí pasa directo a la chica.
        private void EntrarInventor()
        {
            viejo?.Inventor?.DarRegalo(null);
            IrA(PasoRecorrido.Libre); // T-historia-libre-23nov: la chica y lo demás quedan desconectados
        }

        // 11. C06 LA CHICA al salir del CIC la primera vez (al alejarse del Inventor).
        private void EntrarChica()
        {
            Quitar();
            sub = 0;
        }

        private void ReproducirChica()
        {
            sub = 1;
            Cinematica("C06", null, () => IrA(PasoRecorrido.GuerraSkulls));
        }

        // 12. Guerra con los SKULLS: liberar el Tianguis. Pelea protegida; la otra pandilla (Neón Alto) sigue apagada.
        private void EntrarGuerraSkulls()
        {
            // T-78: el mundo ya es libre; la pelea protegida solo se activa al acercarse al Tianguis (ActualizarProteccionTianguis).
            // VR47 / T-historia-libre-23nov: sin el aviso "YA ERES LIBRE" (Tony no quiere avisos; la historia termina con Tino).
            Prender(viejo != null ? viejo.GruposNeon : null, false);
            if (!hambreResuelta && IniciarHambre())
            {
                Prender(viejo != null ? viejo.GruposTianguis : null, false); // antes de la pelea: "Tengo hambre..."
                GuardarPartida();
                return;
            }
            ComenzarGuerraSkulls();
        }

        private void ComenzarGuerraSkulls()
        {
            sub = 1;
            hambreResuelta = true;
            Prender(viejo != null ? viejo.GruposTianguis : null, true);
            Fijar(TablaRecorrido.ObjetivoTianguis, PosicionGrupos(viejo != null ? viejo.GruposTianguis : null));
            if (!ruedaAvisada)
            {
                AvisarRueda(); // por si llegó a la guerra sin ver el aviso; AvisosUnicos evita repetirlo
            }
            GuardarPartida();
        }

        // T-1662: el paso del hambre. false si no hay ninguna máquina que venda (entonces se sigue sin él).
        private bool IniciarHambre()
        {
            Transform jugador = JugadorRaiz();
            Expendedora cerca = null;
            float mejor = float.MaxValue;
            foreach (Expendedora e in FindObjectsByType<Expendedora>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                float d = jugador != null ? Plano(jugador.position, e.Posicion) : 0f;
                if (d < mejor)
                {
                    mejor = d;
                    cerca = e;
                }
            }
            if (cerca == null)
            {
                Debug.LogWarning("[Historia] No hay máquinas expendedoras que vendan en la escena: se salta el paso del hambre.");
                return false;
            }
            sub = 0;
            maquinaHambre = cerca;
            Expendedora.Compra -= AlComprarEnMaquina;
            Expendedora.Compra += AlComprarEnMaquina;
            Fijar(TablaRecorrido.ObjetivoComer, cerca.Posicion);
            Vector3 boca = (jugador != null ? jugador.position : cerca.Posicion) + Vector3.up * 1.6f;
            Subtitulos.Publicar("YO", TablaRecorrido.PensamientoHambre, boca, 3.5f, new Color(0.7f, 1f, 0.95f));
            AvisoTutorialHistoria aviso = AvisoTutorialHistoria.Actual;
            if (aviso != null)
            {
                string tecla = MechaCyberpunk.UI.Hud.TeclasHud.Texto(MechaCyberpunk.UI.Hud.TeclasHud.Buscar("Interactuar"), "E");
                if (string.IsNullOrWhiteSpace(tecla))
                {
                    tecla = "E"; // T-1757: sin texto de la tecla salía "PULSA ."
                }
                aviso.Mostrar(AvisosUnicos.Maquina, string.Format(TablaRecorrido.AvisoMaquina, tecla.ToUpperInvariant()));
            }
            return true;
        }

        private void AlComprarEnMaquina(Expendedora maquina)
        {
            Expendedora.Compra -= AlComprarEnMaquina;
            if (paso != PasoRecorrido.GuerraSkulls || hambreResuelta)
            {
                return;
            }
            maquinaHambre = null;
            AvisoTutorialHistoria.Actual?.Quitar();
            ComenzarGuerraSkulls();
        }

        // 13. Guerra con los SNAKES: Neón Alto. Pelea protegida; los SKULLS ya cayeron y no se meten.
        private void EntrarGuerraSnakes()
        {
            MisionProtegida.Activar("historia: guerra con los SNAKES", false);
            GameObject[] neon = viejo != null ? viejo.GruposNeon : null;
            Prender(neon, true);
            Fijar(TablaRecorrido.ObjetivoNeon, PosicionGrupos(neon));
            GuardarPartida();
        }

        // 14. El Hacker: al hablarle sale C07. En la demo, después sale CONTINUARÁ.
        private void EntrarHacker()
        {
            PersonajeApoyo hacker = viejo != null ? viejo.Hacker : null;
            if (hacker == null)
            {
                Debug.LogWarning("[Historia] No hay Hacker en la escena: se salta a la cinemática C07.");
                ReproducirHacker();
                return;
            }
            Fijar(TablaRecorrido.ObjetivoHacker, hacker.Posicion);
            hacker.DarRegalo(() =>
            {
                hacker.DarRegalo(null);
                ReproducirHacker();
            });
            GuardarPartida();
        }

        private void ReproducirHacker()
        {
            Cinematica("C07", null, () =>
            {
                if (EsDemo)
                {
                    MostrarContinuara();
                }
                else
                {
                    IrA(PasoRecorrido.Robots);
                }
            });
        }

        private void MostrarContinuara()
        {
            Quitar();
            PantallaNegraRecorrido.Mostrar(TablaRecorrido.TituloContinuara, NombreJuego.Nombre, TablaRecorrido.TeclasContinuara, 0f, () => FlujoPartida.IrAlTitulo());
        }

        // 15. Llegan los robots de NOVA. Protegida contra la policía normal (NOVA sí ataca).
        private void EntrarRobots()
        {
            MisionProtegida.Activar("historia: los robots de NOVA", true);
            Fijar(TablaRecorrido.ObjetivoTorre, PosicionTorre());
            proximaNave = Time.time + SegundosPrimeraNave;
            GuardarPartida();
        }

        // 16. C08 EL HERMANO. En negro se pasa a la protección del jefe final (sin refuerzos de NOVA).
        private void EntrarHermano()
        {
            Cinematica("C08", () => MisionProtegida.Activar("historia: el jefe final", false), () => IrA(PasoRecorrido.JefeFinal));
        }

        // 17. Jefe final: pelea protegida contra E14.
        private void EntrarJefeFinal()
        {
            MisionProtegida.Activar("historia: el jefe final", false);
            recinto = FindFirstObjectByType<RecintoJefe>();
            Fijar(TablaRecorrido.ObjetivoJefeNova, recinto != null ? recinto.transform.position : PosicionTorre());
            if (recinto == null)
            {
                Debug.LogWarning("[Historia] No hay RecintoJefe en la escena: el paso 17 no puede terminar solo (usa SaltarAPaso(18)).");
            }
            GuardarPartida();
        }

        // 18. C09 LAS PUERTAS.
        private void EntrarPuertas()
        {
            Cinematica("C09", () => MisionProtegida.Desactivar(), () => IrA(PasoRecorrido.Fin));
        }

        // 19. Fin: negro, "CYBER CITY", "¿YA SALISTE?", créditos y vuelta al título. CONTINUAR = juego libre.
        private void EntrarFin()
        {
            MisionProtegida.Desactivar();
            Quitar();
            paso = PasoRecorrido.Libre; // lo que se guarda: al CONTINUAR la partida sigue en juego libre
            PrepararLibre();
            GuardarPartida();
            ControladorAPie control = ControlDelJugador();
            control?.ActivarControl(false);
            PantallaNegraRecorrido.Mostrar(TablaRecorrido.TituloFin, TablaRecorrido.LineaFin, string.Empty, SegundosFin, () =>
                PantallaCreditos.Abrir(() => FlujoPartida.IrAlTitulo()));
        }

        // T-historia-libre-23nov: al CONTINUAR o REINTENTAR (partida cargada en Libre) el modo libre empieza ya; si se llega desde Tino, empieza al salir de su tienda.
        private void EntrarLibre()
        {
            PrepararLibre();
            if (reanudando)
            {
                EmpezarModoLibre(false);
                return;
            }
            sub = 0;
            FaseJuego.Reiniciar(FaseJuego.Fase.Guiada);
            GuardarPartida();
        }

        // Sin aviso: el objetivo pequeño SOBREVIVE y la escalada (DirectorModoLibre). Con guardar, aquí queda el punto de REINTENTAR.
        private void EmpezarModoLibre(bool guardar)
        {
            sub = 1;
            DirectorModoLibre.Iniciar(guardar);
        }

        // T-historia-libre-23nov: el modo libre empieza la primera vez que sales de la tienda de Tino (banco cerrado, fuera del campo de tiro y lejos de él).
        private void ActualizarSalidaTino(Transform jugador)
        {
            if (sub != 0 || PestanasBancoTino.AbiertoOEnCampo)
            {
                return;
            }
            PersonajeApoyo inventor = viejo != null ? viejo.Inventor : null;
            if (inventor == null || Plano(jugador.position, inventor.Posicion) >= DistanciaAlejarseInventor)
            {
                EmpezarModoLibre(true);
            }
        }

        private void PrepararLibre()
        {
            MisionProtegida.Desactivar();
            Prender(viejo != null ? viejo.GruposTianguis : null, true);
            Prender(viejo != null ? viejo.GruposNeon : null, true);
            viejo?.Doc?.DarRegalo(null);
            viejo?.Inventor?.DarRegalo(null);
            viejo?.Hacker?.DarRegalo(null);
            Quitar();
        }

        // ---------- Cada cuadro ----------

        private void Update()
        {
#if !DEMO_FINAL
            Keyboard teclado = Keyboard.current;
            if (teclado != null && teclado.f8Key.wasPressedThisFrame && !Pausa.Activa && paso != PasoRecorrido.Libre && paso != PasoRecorrido.Ninguno)
            {
                SaltarA(GestorCandados.Siguiente(paso));
                return;
            }
#endif
            MantenerNoche();
            if (!iniciado || CinematicaEnCurso.Activa || FlujoPartida.EnTitulo)
            {
                return;
            }
            Transform jugador = JugadorRaiz();
            if (jugador == null)
            {
                return;
            }
            switch (paso)
            {
                case PasoRecorrido.Despierta:
                    if (Time.time - inicioDespierta >= SegundosHastaMolestia
                        || Plano(jugador.position, PosicionMolestia()) <= RadioMolestia
                        || (inicioConocido && Plano(jugador.position, inicioPos) >= MetrosParaMolestia()))
                    {
                        IrA(PasoRecorrido.Molestan);
                    }
                    break;
                case PasoRecorrido.Ranura:
                    if (Plano(jugador.position, PosicionRanura()) <= RadioRanura)
                    {
                        IrA(PasoRecorrido.Decision);
                    }
                    break;
                case PasoRecorrido.Suero:
                    ActualizarSuero();
                    break;
                case PasoRecorrido.Dinero:
                    ActualizarDinero(jugador);
                    break;
                case PasoRecorrido.UsarProtesis:
                    ActualizarUsarProtesis(jugador);
                    break;
                case PasoRecorrido.Armas:
                    if (sub == 0 && TieneArma())
                    {
                        if (!ruedaAvisada)
                        {
                            AvisarRueda(); // el aviso espera a que cierres el banco (AvisoTutorialHistoria)
                        }
                        if (TieneMejoraDeArma() && !PestanasBancoTino.AbiertoOEnCampo)
                        {
                            IrA(PasoRecorrido.Libre); // T-historia-libre-23nov: la historia termina con Tino; lo de después queda desconectado
                        }
                    }
                    break;
                case PasoRecorrido.Chica:
                    ActualizarChica(jugador);
                    break;
                case PasoRecorrido.Libre:
                    ActualizarSalidaTino(jugador);
                    break;
                case PasoRecorrido.GuerraSkulls:
                    if (sub == 0 && !hambreResuelta)
                    {
                        break; // esperando a que compre algo en la máquina
                    }
                    ActualizarProteccionTianguis(jugador);
                    if (ActualizarConquista(viejo != null ? viejo.ZonaTianguis : "tianguis", viejo != null ? viejo.GruposTianguis : null,
                        TablaRecorrido.ObjetivoJefeSkulls, IdPandilla.Calaveras))
                    {
                        MisionProtegida.Desactivar();
                        IrA(PasoRecorrido.GuerraSnakes);
                    }
                    break;
                case PasoRecorrido.GuerraSnakes:
                    if (ActualizarConquista(TablaRecorrido.ZonaNeon, viejo != null ? viejo.GruposNeon : null,
                        TablaRecorrido.ObjetivoJefeSnakes, IdPandilla.Viboras))
                    {
                        MisionProtegida.Desactivar();
                        IrA(PasoRecorrido.Hacker);
                    }
                    break;
                case PasoRecorrido.Robots:
                    ActualizarRobots(jugador);
                    break;
                case PasoRecorrido.JefeFinal:
                    if (recinto != null && recinto.Jefe != null && recinto.Jefe.Vencido)
                    {
                        MisionProtegida.Desactivar();
                        IrA(PasoRecorrido.Puertas);
                    }
                    break;
            }
        }

        // T-78: la misión protegida de la guerra con los SKULLS vale solo cerca del Tianguis (con un margen para no prender y apagar).
        private void ActualizarProteccionTianguis(Transform jugador)
        {
            float distancia = Plano(jugador.position, PosicionGrupos(viejo != null ? viejo.GruposTianguis : null));
            bool cerca = distancia <= (tianguisProtegido ? MetrosSaleProteccionTianguis : MetrosEntraProteccionTianguis);
            if (cerca == tianguisProtegido)
            {
                return;
            }
            tianguisProtegido = cerca;
            if (cerca)
            {
                MisionProtegida.Activar("historia: guerra con los SKULLS", false);
            }
            else
            {
                MisionProtegida.Desactivar();
            }
        }

        private void ActualizarSuero()
        {
            if (sub != 1)
            {
                return;
            }
            IImplantes implantes = Registro<IImplantes>.Actual;
            if (implantes != null && implantes.Cantidad > implantesAntes)
            {
                MostradorClinica clinica = FindFirstObjectByType<MostradorClinica>();
                if (clinica != null && (clinica.Operando || clinica.PantallaAbierta))
                {
                    return; // que termine la operación (fundido y subtítulo) antes de salir a la calle
                }
                IrA(PasoRecorrido.UsarProtesis);
            }
        }

        private void ActualizarChica(Transform jugador)
        {
            if (sub != 0)
            {
                return;
            }
            PersonajeApoyo inventor = viejo != null ? viejo.Inventor : null;
            if (inventor == null || Plano(jugador.position, inventor.Posicion) >= DistanciaAlejarseInventor)
            {
                ReproducirChica();
            }
        }

        private void ActualizarRobots(Transform jugador)
        {
            if (Plano(jugador.position, PosicionTorre()) <= RadioTorre)
            {
                IrA(PasoRecorrido.Hermano);
                return;
            }
            if (Time.time >= proximaNave)
            {
                proximaNave = Time.time + SegundosEntreNaves;
                // Lo que hoy hace llegar a NOVA: una nave que baja soldados cerca del jugador (RespuestaNova, T-1498).
                RespuestaNova nova = RespuestaNova.Actual;
                if (nova != null)
                {
                    nova.LlamarNave(jugador.position, EstrellasNave);
                }
            }
        }

        // Cuenta de la conquista de una zona; true cuando la zona ya es del jugador. Como el director viejo, no se puede trabar por falta de SKULLS.
        private bool ActualizarConquista(string zona, GameObject[] grupos, string textoJefe, IdPandilla rival)
        {
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            if (territorio == null)
            {
                return false;
            }
            if (territorio.Dueno(zona) == IdPandilla.Jugador)
            {
                return true;
            }
            CompletarSiCayeronTodos(territorio, zona, grupos, rival);
            if (!jefeMostrado && territorio.JefeDisponible(zona))
            {
                jefeMostrado = true;
                Vector3 donde = PosicionJefe(zona, grupos);
                Fijar(textoJefe, donde);
            }
            return false;
        }

        private void CompletarSiCayeronTodos(ITerritorio territorio, string zona, GameObject[] grupos, IdPandilla rival)
        {
            if (grupos == null || grupos.Length == 0)
            {
                return;
            }
            foreach (GameObject g in grupos)
            {
                PandillaAPie pandilla = g != null ? g.GetComponent<PandillaAPie>() : null;
                if (pandilla == null || !pandilla.ModoMision || !pandilla.TodosVencidos)
                {
                    return;
                }
            }
            Vector3 punto = PosicionGrupos(grupos);
            int seguridad = 20;
            while (!territorio.JefeDisponible(zona) && territorio.Dueno(zona) != IdPandilla.Jugador && seguridad-- > 0)
            {
                territorio.RegistrarVencido(punto, rival, false);
            }
        }

        // ---------- Mundo ----------

        private static void Prender(GameObject[] grupos, bool activo)
        {
            if (grupos == null)
            {
                return;
            }
            foreach (GameObject g in grupos)
            {
                if (g == null || g.activeSelf == activo)
                {
                    continue;
                }
                if (activo)
                {
                    PandillaAPie pandilla = g.GetComponent<PandillaAPie>();
                    if (pandilla != null)
                    {
                        pandilla.EsDeGuion = true;
                        pandilla.ModoMision = true;
                    }
                }
                g.SetActive(activo);
            }
        }

        private void PonerNoche()
        {
            IRelojMundo reloj = Reloj();
            // T68: con MOMENTO DEL DÍA = Día la historia también transcurre de día; con Noche (por defecto) todo igual que antes.
            MomentoDelDia momento = ReglaMomentoDia.Actual;
            bool volver = reloj != null && (momento == MomentoDelDia.Noche
                ? !reloj.EsDeNoche || ReglaNocheFija.HayQueVolver(reloj.Hora)
                : ReglaMomentoDia.HayQueVolver(momento, reloj.Hora, reloj.HoraAmanecer, reloj.HoraAnochecer));
            if (volver)
            {
                reloj.AvanzarHasta(momento == MomentoDelDia.Noche ? HoraNoche : ReglaMomentoDia.HoraDe(momento));
            }
            if (NocheFijaAhora())
            {
                reloj?.Pausar(true);
            }
        }

        // T-1685 (T36): del paso 2 al 11 (ReglaNocheFija) el reloj se queda a las 23:00 y en pausa. Sin la regla, la noche de los pasos 2 a 6
        // sigue como antes (la historia la pide).
        private bool NocheFijaAhora()
        {
            // T68: con el momento en Día no se fija la noche (AplicadorMomentoDia mantiene el reloj de día).
            return ReglaMomentoDia.Actual == MomentoDelDia.Noche
                && (ReglaNocheFija.Aplica(paso) || (paso >= PasoRecorrido.Despierta && paso <= PasoRecorrido.Decision));
        }

        // T-1685: cada frame, y en cuanto el reloj avisa que cambió de franja, lo regresa a la noche si algo lo movió (dormir, la clínica, cargar
        // una partida de día, una herramienta de prueba). AvanzarHasta suma los días que cruce, así que la renta y los negocios siguen contando.
        private void MantenerNoche()
        {
            IRelojMundo actual = Reloj();
            if (actual != relojVigilado)
            {
                if (relojVigilado != null)
                {
                    relojVigilado.FranjaCambio -= AlCambiarFranja;
                }
                relojVigilado = actual;
                if (relojVigilado != null)
                {
                    relojVigilado.FranjaCambio += AlCambiarFranja;
                }
            }
            if (actual == null || !iniciado || !NocheFijaAhora())
            {
                return;
            }
            if (!actual.EsDeNoche || ReglaNocheFija.HayQueVolver(actual.Hora) || !actual.Pausado)
            {
                PonerNoche();
            }
        }

        private void AlCambiarFranja(FranjaDia franja)
        {
            if (franja == FranjaDia.Dia && iniciado && NocheFijaAhora())
            {
                PonerNoche();
            }
        }

        private void DejarDeVigilarReloj()
        {
            if (relojVigilado != null)
            {
                relojVigilado.FranjaCambio -= AlCambiarFranja;
                relojVigilado = null;
            }
        }

        private static IRelojMundo Reloj() => Registro<IRelojMundo>.Actual;

        private void CerrarRanura()
        {
            if (ranuraCerrada != null)
            {
                return;
            }
            Pose p = PoseRanura();
            ranuraCerrada = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ranuraCerrada.name = "RanuraCerrada";
            Transform t = ranuraCerrada.transform;
            t.SetPositionAndRotation(p.position + Vector3.up * (AlturaRanura * 0.5f), Quaternion.Euler(0f, p.rotation.eulerAngles.y, 0f));
            t.localScale = new Vector3(AnchoRanura, AlturaRanura, GrosorRanura);
            Renderer r = ranuraCerrada.GetComponent<Renderer>();
            Shader sombreador = Shader.Find("Universal Render Pipeline/Lit");
            if (r != null && sombreador != null)
            {
                Material m = new Material(sombreador) { name = "TapiaRanura" };
                m.SetColor("_BaseColor", ColorTapia);
                m.SetFloat("_Smoothness", 0.15f);
                r.sharedMaterial = m;
            }
        }

        // T-1656 (T30): tras C01 el jugador ya está TIRADO (la pose final de la caída, sin verla caer) y se levanta con el gesto de siempre.
        // Se tumba y se manda el animador directo al último cuadro del estado de caída (boca arriba si es posible).
        internal static void DejarTiradoEnElSuelo(VisualPersonaje visual)
        {
            visual.Caido = true;
            Animator animador = visual.Animador;
            if (animador == null)
            {
                return;
            }
            string estado;
            switch (visual.Muerte)
            {
                case MuerteVisual.Adelante:
                    estado = "MuerteAdelante";
                    break;
                case MuerteVisual.Desmayo:
                    estado = "MuerteDesmayo";
                    break;
                default:
                    estado = "MuerteAtras";
                    break;
            }
            int hash = Animator.StringToHash(estado);
            if (animador.HasState(0, hash))
            {
                animador.Play(hash, 0, 0.999f);
                animador.Update(0f);
            }
        }

        // T-1656 (T30): pegado al piso con raycast (ignorando al propio jugador) para que no haya caída desde el aire.
        private static void PegarJugadorAlSuelo()
        {
            Transform j = JugadorRaiz();
            if (j == null)
            {
                return;
            }
            RaycastHit[] golpes = Physics.RaycastAll(j.position + Vector3.up * 3f, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            Vector3 punto = j.position;
            bool hay = false;
            foreach (RaycastHit g in golpes)
            {
                if (g.collider.transform.IsChildOf(j) || g.distance >= mejor)
                {
                    continue;
                }
                mejor = g.distance;
                punto = g.point;
                hay = true;
            }
            if (hay)
            {
                Registro<IJugador>.Actual?.Colocar(new Pose(punto + Vector3.up * 0.03f, j.rotation));
            }
        }

        private IEnumerator Levantarse()
        {
            ControladorAPie control = ControlDelJugador();
            VisualPersonaje visual = VisualDelJugador();
            if (visual == null)
            {
                yield break;
            }
            control?.ActivarControl(false);
            visual.Caido = true;
            yield return new WaitForSecondsRealtime(ReglasAnimacion.RetardoLevantarse);
            visual.Caido = false;
            yield return new WaitForSecondsRealtime(SegundosEnNegroLevantarse);
            control?.ActivarControl(true);
        }

        private static bool TieneMejoraDeArma()
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            if (arsenal == null)
            {
                return false;
            }
            foreach (ArmaAPie arma in Enum.GetValues(typeof(ArmaAPie)))
            {
                if (arma != ArmaAPie.Punos && arma != ArmaAPie.Granada && arsenal.Nivel(arma) >= 1)
                {
                    return true;
                }
            }
            return false;
        }

        private bool TieneArma()
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            if (arsenal == null)
            {
                return false;
            }
            foreach (ArmaAPie arma in Enum.GetValues(typeof(ArmaAPie)))
            {
                if (arma != ArmaAPie.Punos && arma != ArmaAPie.Granada && arsenal.Tiene(arma))
                {
                    return true;
                }
            }
            return false;
        }

        // ---------- Posiciones ----------

        private float MetrosParaMolestia() => datos != null ? datos.MetrosParaMolestia : 22f;

        private void ConocerInicio()
        {
            if (inicioConocido)
            {
                return;
            }
            IPuntoAparicion punto = BuscarAparicion(viejo != null ? viejo.IdPuntoInicio : "Inicio_Calle") ?? BuscarAparicion(DirectorPandillas.IdInicioRespaldo);
            if (punto != null)
            {
                inicioPos = punto.Pose.position;
                inicioAdelante = Vector3.ProjectOnPlane(punto.Pose.rotation * Vector3.forward, Vector3.up).normalized;
                inicioConocido = true;
                return;
            }
            Transform j = JugadorRaiz();
            if (j != null)
            {
                inicioPos = j.position;
                inicioAdelante = Vector3.ProjectOnPlane(j.forward, Vector3.up).normalized;
                inicioConocido = true;
            }
        }

        private void ColocarEnElInicio()
        {
            ConocerInicio();
            IPuntoAparicion punto = BuscarAparicion(viejo != null ? viejo.IdPuntoInicio : "Inicio_Calle") ?? BuscarAparicion(DirectorPandillas.IdInicioRespaldo);
            if (punto == null)
            {
                Debug.LogWarning("[Historia] No hay punto de aparición Inicio_Calle: el jugador empieza donde está.");
                return;
            }
            Registro<IJugador>.Actual?.Colocar(punto.Pose);
        }

        private Vector3 PosicionMolestia()
        {
            if (MarcadorHistoria.TryBuscar(MarcadorHistoria.IdMolestia, out Pose pose))
            {
                return pose.position;
            }
            AvisarSinMarcador(MarcadorHistoria.IdMolestia);
            ConocerInicio();
            return Ajustar(inicioPos + inicioAdelante * MetrosParaMolestia());
        }

        // T-1652: de dónde salen los SKULLS: el callejón junto a la casa (marcador); respaldo, un hueco libre a 15-20 m del Inicio_Calle a un lado de la calle;
        // null si no hay nada (salen adelante del jugador como antes).
        private Vector3? PosicionCallejonSkulls()
        {
            if (MarcadorHistoria.TryBuscar(MarcadorHistoria.IdCallejonSkulls, out Pose pose))
            {
                return pose.position;
            }
            AvisarSinMarcador(MarcadorHistoria.IdCallejonSkulls);
            ConocerInicio();
            if (inicioConocido && MolestiaSkulls.BuscarSalidaRespaldo(inicioPos, inicioAdelante, out Vector3 punto))
            {
                return punto;
            }
            return null;
        }

        // T-1656: dónde esperan los SKULLS, a media calle frente a la casa: marcador Historia_EsperaSkulls; respaldo, 5-10 m adelante del Inicio_Calle; null si no hay hueco.
        private Vector3? PosicionEsperaSkulls()
        {
            if (MarcadorHistoria.TryBuscar(MarcadorHistoria.IdEsperaSkulls, out Pose pose))
            {
                return pose.position;
            }
            AvisarSinMarcador(MarcadorHistoria.IdEsperaSkulls);
            ConocerInicio();
            if (inicioConocido && MolestiaSkulls.BuscarEsperaRespaldo(inicioPos, inicioAdelante, out Vector3 punto))
            {
                return punto;
            }
            return null;
        }

        private Pose PoseRanura()
        {
            if (MarcadorHistoria.TryBuscar(MarcadorHistoria.IdRanura, out Pose pose))
            {
                return pose;
            }
            AvisarSinMarcador(MarcadorHistoria.IdRanura);
            ConocerInicio();
            float metros = datos != null ? datos.MetrosHastaRanura : 45f;
            Vector3 donde = Ajustar(inicioPos + inicioAdelante * metros);
            return new Pose(donde, Quaternion.LookRotation(inicioAdelante, Vector3.up));
        }

        private Vector3 PosicionRanura() => PoseRanura().position;

        private Vector3 PosicionTorre()
        {
            if (MarcadorHistoria.TryBuscar(MarcadorHistoria.IdTorre, out Pose pose))
            {
                return pose.position;
            }
            RecintoJefe r = FindFirstObjectByType<RecintoJefe>();
            if (r != null)
            {
                return r.transform.position;
            }
            AvisarSinMarcador(MarcadorHistoria.IdTorre);
            Transform j = JugadorRaiz();
            return j != null ? j.position + j.forward * 200f : Vector3.zero;
        }

        private Vector3 PosicionJefe(string zona, GameObject[] grupos)
        {
            foreach (JefePandilla j in FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (j.Zona == zona)
                {
                    return j.transform.position;
                }
            }
            return PosicionGrupos(grupos);
        }

        private static Vector3 PosicionGrupos(GameObject[] grupos)
        {
            if (grupos != null)
            {
                foreach (GameObject g in grupos)
                {
                    if (g != null)
                    {
                        return g.transform.position;
                    }
                }
            }
            return Vector3.zero;
        }

        private void AvisarSinMarcador(string id)
        {
            if (!sinMarcadorAvisado)
            {
                sinMarcadorAvisado = true;
                Debug.LogWarning("[Historia] Falta el marcador de historia '" + id + "' en la escena: se usa una posición de respaldo (ver docs).");
            }
        }

        private static Vector3 Ajustar(Vector3 punto)
        {
            if (NavMesh.SamplePosition(punto, out NavMeshHit golpe, 30f, NavMesh.AllAreas))
            {
                return golpe.position;
            }
            return Suelo(punto);
        }

        private static Vector3 Suelo(Vector3 punto)
        {
            if (Physics.Raycast(punto + Vector3.up * 8f, Vector3.down, out RaycastHit golpe, 20f, ~0, QueryTriggerInteraction.Ignore))
            {
                return golpe.point;
            }
            return punto;
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static IPuntoAparicion BuscarAparicion(string id)
        {
            foreach (IPuntoAparicion punto in Registro<IPuntoAparicion>.Todos)
            {
                if (punto != null && punto.Id == id)
                {
                    return punto;
                }
            }
            return null;
        }

        // ---------- Jugador, objetivos y avisos ----------

        private static Transform JugadorRaiz()
        {
            IJugador j = Registro<IJugador>.Actual;
            return j != null ? j.Raiz : null;
        }

        private static ControladorAPie ControlDelJugador()
        {
            Transform raiz = JugadorRaiz();
            return raiz != null ? raiz.GetComponentInChildren<ControladorAPie>(true) : null;
        }

        private static VisualPersonaje VisualDelJugador()
        {
            Transform raiz = JugadorRaiz();
            return raiz != null ? raiz.GetComponentInChildren<VisualPersonaje>(true) : null;
        }

        private static VidaAPie VidaDelJugador()
        {
            Transform raiz = JugadorRaiz();
            return raiz != null ? raiz.GetComponentInChildren<VidaAPie>(true) : null;
        }

        private static void Fijar(string texto, Vector3 posicion)
        {
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Fijar(new Objetivo(texto, posicion));
        }

        private static void Quitar()
        {
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Quitar();
        }

        private static void Avisar(string texto, MechaCyberpunk.UI.Hud.TipoAviso tipo)
        {
            MechaCyberpunk.UI.Hud.ColaAvisos.Publicar(texto, MechaCyberpunk.UI.Hud.PrioridadAviso.Normal, tipo, 4f);
        }
    }
}
