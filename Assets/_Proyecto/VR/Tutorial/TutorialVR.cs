using MechaCyberpunk.CampoTiro;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.Progresion.Prologo.Clinica;
using MechaCyberpunk.Progresion.Recorrido;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Rueda;
using MechaCyberpunk.UI.Titulo;
using MechaCyberpunk.VR.Guia;
using MechaCyberpunk.VR.Menus;
using UnityEngine;

namespace MechaCyberpunk.VR.Tutorial
{
    /// <summary>
    /// VR62: el tutorial de VR que se presenta en el momento justo. Cada control se enseña con una tarjeta (<see cref="TarjetaTutorialVR"/>:
    /// la imagen de la guía, la frase y APRENDIDO) cuando se va a usar, sin pausar el juego para que se pueda hacer el gesto; la tarjeta se
    /// cierra con APRENDIDO o sola al hacer el gesto. Cada una sale una sola vez por partida guardada: se marca en los avisos únicos de la
    /// partida (<see cref="AvisosUnicos"/>, clave <see cref="ReglaTutorialVR.Clave"/>), que el director de la historia guarda con la partida y
    /// limpia en partida nueva. Pausa → CONTROLES muestra todas (la guía completa).
    /// Momentos (detectados aquí, sondeando el estado del juego, sin tocar código de PC):
    /// <list type="bullet">
    /// <item>Pelea de los 3 SKULLS (<see cref="PasoRecorrido.PeleaSkulls"/>): MOVERSE Y GIRAR, RECENTRAR y GOLPE. Es pelea: mientras se ve la
    /// tarjeta el juego se detiene (los SKULLS esperan); el gesto se lee del control aunque el tiempo esté detenido.</item>
    /// <item>Al acercarse al Doc (<see cref="DoctorClinica"/> a menos de <see cref="DistanciaDoc"/> m): INTERACTUAR.</item>
    /// <item>Con cada prótesis instalada (piernas, brazos u ojos): su gesto; con BRAZOS, además AGARRAR Y LANZAR.</item>
    /// <item>Con la pistola o el revólver (la compra con Tino): SACAR PISTOLA.</item>
    /// <item>Dentro del campo de tiro: DISPARAR → RECARGAR → ARMA LARGA → RUEDA DE ARMAS → GRANADA, una al cerrar la anterior.</item>
    /// <item>Al tener por primera vez un carro para subir (o ya dentro): MANEJAR.</item>
    /// <item>Lo que se consigue antes sale la primera vez que se usa: arma en la mano → DISPARAR; recargar → RECARGAR; arma larga, dos armas
    /// (rueda) o granada → su tarjeta.</item>
    /// </list>
    /// Las tarjetas esperan a que no haya carga, título, pausa, pantallas, menús, avisos ni la guía; si se abre una pantalla con la tarjeta
    /// visible, se esconde y vuelve al cerrarla. Con el simulador de XR no sale salvo <see cref="MostrarEnSimulador"/> (para no tapar las
    /// pruebas). Se agrega solo al jugador cuando el rig VR está armado (<see cref="AdaptadorVR.Listo"/>).
    /// </summary>
    [DefaultExecutionOrder(170)]
    public class TutorialVR : MonoBehaviour
    {
        /// <summary>Al Doc se le enseña B a esta distancia (m).</summary>
        public const float DistanciaDoc = 4f;
        /// <summary>Cada cuánto se buscan el Doc y los componentes VR que se agregan después (s).</summary>
        public const float SegundosBuscar = 1f;
        /// <summary>Segundos de stick para dar por hecho MOVERSE Y GIRAR (s).</summary>
        public const float SegundosMoverse = 1f;
        /// <summary>Segundos del stick derecho abajo para RECENTRAR (s).</summary>
        public const float SegundosRecentrar = 1f;
        /// <summary>Rapidez de la mano que cuenta como golpe en la tarjeta (m/s).</summary>
        public const float RapidezGolpe = 1.5f;
        /// <summary>Segundos acelerando con el gatillo para MANEJAR (s).</summary>
        public const float SegundosManejar = 0.5f;
        /// <summary>La tarjeta no se da por hecha antes de esto (s): que se alcance a ver.</summary>
        public const float SegundosMinimos = 0.6f;

        private const float UmbralStick = 0.5f;
        private const float UmbralAbajo = -0.7f;
        private const float UmbralAcelerar = 0.3f;

        private readonly ReglaTutorialVR regla = new ReglaTutorialVR(AvisosUnicos.YaVisto);
        private AdaptadorVR adaptador;
        private InteraccionAPie interaccion;
        private ArmasAPie armas;
        private HabilidadesProtesis habilidades;
        private GolpesManosVR golpes;
        private AgarreImplanteVR agarre;
        private ArmasEspecialesVR especiales;
        private CambioArmaStickVR stick;
        private RecargaManualVR recarga;
        private DoctorClinica[] doctores = new DoctorClinica[0];
        private float buscarEn;

        private TarjetaTutorialVR tarjeta;
        private float abiertaDesde;
        private bool pausando;
        private float acumulado;
        private ArmaAPie armaAlAbrir;
        private int golpesAlAbrir;
        private int lanzadasAlAbrir;
        private int recentradosAlAbrir;
        private int metidosAlAbrir;
        private int aperturasRuedaAlAbrir;
        private int disparosCampoAlAbrir;
        private bool ruedaAbiertaEnTarjeta;
        private Vector3 manoIzqAntes;
        private Vector3 manoDerAntes;
        private bool hayManosAntes;
        private bool estabaEnCampo;

        /// <summary>Con el simulador de XR el tutorial no sale (las pruebas lo encienden para probarlo).</summary>
        public static bool MostrarEnSimulador { get; set; }

        /// <summary>El tutorial del jugador VR (null si no hay).</summary>
        public static TutorialVR Actual { get; private set; }

        /// <summary>La regla con la fila de tarjetas.</summary>
        public ReglaTutorialVR Regla => regla;

        /// <summary>La tarjeta que se ve (o espera tras una pantalla); null si no hay.</summary>
        public TarjetaTutorialVR Tarjeta => tarjeta;

        /// <summary>True mientras una tarjeta detiene el juego (en la pelea).</summary>
        public bool Pausando => pausando;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            MostrarEnSimulador = false;
            Actual = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Suscribir()
        {
            AdaptadorVR.Listo -= AlListo;
            AdaptadorVR.Listo += AlListo;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RevisarActual()
        {
            if (AdaptadorVR.Actual != null && AdaptadorVR.Actual.Armado)
            {
                AlListo(AdaptadorVR.Actual);
            }
        }

        private static void AlListo(AdaptadorVR rig)
        {
            if (rig != null && rig.GetComponent<TutorialVR>() == null)
            {
                rig.gameObject.AddComponent<TutorialVR>();
            }
        }

        /// <summary>Marca la tarjeta como aprendida en esta partida (se guarda con la partida).</summary>
        public static void MarcarAprendida(PaginaGuiaVR pagina) => AvisosUnicos.Marcar(ReglaTutorialVR.Clave(pagina));

        /// <summary>True si la tarjeta ya se aprendió en esta partida.</summary>
        public static bool Aprendida(PaginaGuiaVR pagina) => AvisosUnicos.YaVisto(ReglaTutorialVR.Clave(pagina));

        /// <summary>Pide una tarjeta (sale cuando haya calma). False si ya se aprendió o ya espera.</summary>
        public bool Pedir(PaginaGuiaVR pagina) => (tarjeta == null || tarjeta.Pagina != pagina) && regla.Pedir(pagina);

        private void PedirVarias(PaginaGuiaVR[] paginas)
        {
            foreach (PaginaGuiaVR p in paginas)
            {
                Pedir(p);
            }
        }

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            interaccion = GetComponent<InteraccionAPie>();
            armas = GetComponent<ArmasAPie>();
            habilidades = GetComponent<HabilidadesProtesis>();
            if (adaptador == null)
            {
                Debug.LogError("[VR] TutorialVR sin AdaptadorVR en " + name, this);
                enabled = false;
                return;
            }
            Actual = this;
        }

        private void OnDisable()
        {
            SoltarPausa();
            if (tarjeta != null)
            {
                tarjeta.Mostrar(false);
            }
        }

        private void OnDestroy()
        {
            SoltarPausa();
            if (tarjeta != null)
            {
                tarjeta.Cerrada -= AlCerrarTarjeta;
                tarjeta.Cerrar(false);
                tarjeta = null;
            }
            if (Actual == this)
            {
                Actual = null;
            }
        }

        private void Update()
        {
            if (!ModoVR.Activo || !adaptador.Armado || (ModoVR.Simulado && !MostrarEnSimulador))
            {
                SoltarPausa();
                if (tarjeta != null)
                {
                    tarjeta.Mostrar(false);
                }
                return;
            }
            if (Time.unscaledTime >= buscarEn)
            {
                buscarEn = Time.unscaledTime + SegundosBuscar;
                Buscar();
            }
            Detectar();
            bool calma = EnCalma();
            if (tarjeta != null)
            {
                tarjeta.Mostrar(calma);
                if (calma)
                {
                    RevisarGesto();
                }
            }
            else if (calma && regla.Siguiente(out PaginaGuiaVR siguiente))
            {
                Abrir(siguiente);
            }
            bool pausar = tarjeta != null && calma && EnPelea();
            if (pausar && !pausando)
            {
                ControlTiempo.Pedir(this, 0f);
                pausando = true;
            }
            else if (!pausar)
            {
                SoltarPausa();
            }
        }

        private void SoltarPausa()
        {
            if (pausando)
            {
                ControlTiempo.Soltar(this);
                pausando = false;
            }
        }

        // Los componentes que otras partes agregan al rig después, y el Doc de la escena (cada SegundosBuscar, no en cada cuadro).
        private void Buscar()
        {
            if (interaccion == null) interaccion = GetComponent<InteraccionAPie>();
            if (armas == null) armas = GetComponent<ArmasAPie>();
            if (habilidades == null) habilidades = GetComponent<HabilidadesProtesis>();
            if (habilidades == null) habilidades = HabilidadesProtesis.Actual;
            if (golpes == null) golpes = GetComponent<GolpesManosVR>();
            if (agarre == null) agarre = GetComponent<AgarreImplanteVR>();
            if (especiales == null) especiales = GetComponent<ArmasEspecialesVR>();
            if (stick == null) stick = GetComponent<CambioArmaStickVR>();
            if (recarga == null) recarga = GetComponent<RecargaManualVR>();
            doctores = FindObjectsByType<DoctorClinica>(FindObjectsSortMode.None);
        }

        private bool EnPelea()
        {
            DirectorRecorrido d = DirectorRecorrido.Actual;
            return d != null && d.Paso == PasoRecorrido.PeleaSkulls;
        }

        private bool EnCarro => adaptador.Jugador != null && adaptador.Jugador.Modo == ModoJugador.Carro;

        private bool EnCalma()
        {
            bool control = (adaptador.Controlador != null && adaptador.Controlador.ControlActivo) || EnCarro;
            return control && !FlujoPartida.EnTitulo && !PantallaCargando.Visible && !Pausa.Activa && PilaPantallas.Vacia
                && MenuCyberfix.Abiertos == 0 && !VentanaAviso.Abierta && !GuiaControlesVR.Abierta && !MenusVR.MenuAbierto;
        }

        // ---- Los momentos ----

        private void Detectar()
        {
            if (EnPelea())
            {
                PedirVarias(ReglaTutorialVR.Inicio);
            }
            if (adaptador.Jugador != null && adaptador.Jugador.Modo == ModoJugador.APie && CercaDelDoc())
            {
                Pedir(PaginaGuiaVR.Interactuar);
            }
            if (habilidades != null)
            {
                if (habilidades.Nivel(HabilidadProtesis.Piernas) > 0) Pedir(PaginaGuiaVR.ProtesisPiernas);
                if (habilidades.Nivel(HabilidadProtesis.Brazos) > 0)
                {
                    Pedir(PaginaGuiaVR.ProtesisBrazos);
                    Pedir(PaginaGuiaVR.AgarrarLanzar);
                }
                if (habilidades.Nivel(HabilidadProtesis.Cabeza) > 0) Pedir(PaginaGuiaVR.ProtesisVision);
                if (habilidades.Nivel(HabilidadProtesis.Piernas) > 0 || habilidades.Nivel(HabilidadProtesis.Brazos) > 0)
                {
                    CallarAvisosProtesisPC();
                }
            }
            bool enCampo = CampoDeTiro.EstaDentro;
            if (enCampo && !estabaEnCampo)
            {
                PedirVarias(ReglaTutorialVR.CampoTiro);
            }
            estabaEnCampo = enCampo;
            if (armas != null)
            {
                if (armas.TieneArma(ArmaAPie.Pistola) || armas.TieneArma(ArmaAPie.Revolver)) Pedir(PaginaGuiaVR.SacarPistola);
                ArmaAPie activa = armas.ArmaActiva;
                if (activa != ArmaAPie.Punos && activa != ArmaAPie.Granada) Pedir(PaginaGuiaVR.Disparar);
                if (armas.Recargando) Pedir(PaginaGuiaVR.Recargar);
                if (TieneArmaLarga()) Pedir(PaginaGuiaVR.ArmaLarga);
                if (CuantasArmas() >= 2) Pedir(PaginaGuiaVR.RuedaArmas);
                if (armas.TieneArma(ArmaAPie.Granada)) Pedir(PaginaGuiaVR.Granada);
            }
            if (EnCarro || (interaccion != null && interaccion.TipoCandidato == TipoCandidatoAPie.Montable && interaccion.MontableCandidato != null
                && interaccion.MontableCandidato.Modo == ModoJugador.Carro))
            {
                Pedir(PaginaGuiaVR.Manejar);
            }
        }

        /// <summary>
        /// VR45: el tutorial de prótesis de PC (UI/HudAPie/TutorialProtesis) saca cada paso en una ventana de solo texto al centro una vez por
        /// partida, con la clave "protesis_paso_" + paso (su constante privada PrefijoPaso). En VR lo reemplazan las tarjetas con imagen: se
        /// marcan esos pasos como vistos para que la ventana no salga (la lógica de sus pasos y de la historia sigue igual).
        /// </summary>
        public const string PrefijoPasoProtesisPC = "protesis_paso_";
        private const int PasosProtesisPC = 3;

        private static void CallarAvisosProtesisPC()
        {
            for (int i = 0; i < PasosProtesisPC; i++)
            {
                AvisosUnicos.Marcar(PrefijoPasoProtesisPC + i);
            }
        }

        private bool CercaDelDoc()
        {
            Vector3 aqui = transform.position;
            foreach (DoctorClinica doc in doctores)
            {
                if (doc != null && doc.isActiveAndEnabled)
                {
                    Vector3 d = doc.transform.position - aqui;
                    d.y = 0f;
                    if (d.sqrMagnitude <= DistanciaDoc * DistanciaDoc)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool EsLarga(ArmaAPie arma) => arma != ArmaAPie.Punos && arma != ArmaAPie.Granada && !ReglasArmaAPie.UsaPoseDePistola(arma);

        private bool TieneArmaLarga()
        {
            foreach (ArmaAPie a in System.Enum.GetValues(typeof(ArmaAPie)))
            {
                if (EsLarga(a) && armas.TieneArma(a))
                {
                    return true;
                }
            }
            return false;
        }

        private int CuantasArmas()
        {
            int n = 0;
            foreach (ArmaAPie a in System.Enum.GetValues(typeof(ArmaAPie)))
            {
                if (a != ArmaAPie.Punos && armas.TieneArma(a))
                {
                    n++;
                }
            }
            return n;
        }

        // ---- La tarjeta ----

        private void Abrir(PaginaGuiaVR pagina)
        {
            regla.Quitar(pagina);
            tarjeta = TarjetaTutorialVR.Crear(pagina);
            tarjeta.Cerrada += AlCerrarTarjeta;
            abiertaDesde = Time.unscaledTime;
            acumulado = 0f;
            armaAlAbrir = armas != null ? armas.ArmaActiva : ArmaAPie.Punos;
            golpesAlAbrir = golpes != null ? golpes.Golpes : 0;
            lanzadasAlAbrir = especiales != null ? especiales.Lanzadas : 0;
            recentradosAlAbrir = stick != null ? stick.Recentrados : 0;
            metidosAlAbrir = recarga != null ? recarga.Metidos : 0;
            aperturasRuedaAlAbrir = RuedaArmasObjetos.Aperturas;
            disparosCampoAlAbrir = CampoDeTiro.Disparos;
            ruedaAbiertaEnTarjeta = false;
            hayManosAntes = false;
        }

        private void AlCerrarTarjeta(TarjetaTutorialVR cerrada, bool porGesto)
        {
            cerrada.Cerrada -= AlCerrarTarjeta;
            MarcarAprendida(cerrada.Pagina);
            Debug.Log("[VR] Tutorial: " + cerrada.Pagina + (porGesto ? " aprendida con el gesto." : " aprendida (APRENDIDO)."));
            if (tarjeta == cerrada)
            {
                tarjeta = null;
            }
            SoltarPausa();
        }

        private void RevisarGesto()
        {
            if (tarjeta.Lograda || Time.unscaledTime - abiertaDesde < SegundosMinimos)
            {
                MedirManos(); // que la rapidez de la mano no dé un salto al empezar
                return;
            }
            if (Hecho(tarjeta.Pagina))
            {
                tarjeta.Lograr();
            }
        }

        // Si el jugador hizo lo que enseña la tarjeta (leído del control o del estado del juego; vale con el tiempo detenido en la pelea).
        private bool Hecho(PaginaGuiaVR pagina)
        {
            float dt = Time.unscaledDeltaTime;
            switch (pagina)
            {
                case PaginaGuiaVR.MoverseGirar:
                {
                    bool mueve = EntradaVR.Eje(EntradaVR.StickIzquierdo).magnitude > UmbralStick
                        || Mathf.Abs(EntradaVR.Eje(EntradaVR.StickDerecho).x) > UmbralStick;
                    return Sostener(mueve, dt, SegundosMoverse);
                }
                case PaginaGuiaVR.Recentrar:
                    return (stick != null && stick.Recentrados > recentradosAlAbrir)
                        || Sostener(EntradaVR.Eje(EntradaVR.StickDerecho).y < UmbralAbajo, dt, SegundosRecentrar);
                case PaginaGuiaVR.Golpe:
                    return (golpes != null && golpes.Golpes > golpesAlAbrir) || MedirManos() > RapidezGolpe;
                case PaginaGuiaVR.Interactuar:
                    return EntradaVR.Pulso(EntradaVR.BotonB);
                case PaginaGuiaVR.SacarPistola:
                    return armas != null && armas.ArmaActiva != armaAlAbrir && ReglasArmaAPie.UsaPoseDePistola(armas.ArmaActiva)
                        && armas.ArmaActiva != ArmaAPie.Granada;
                case PaginaGuiaVR.ArmaLarga:
                    return armas != null && armas.ArmaActiva != armaAlAbrir && EsLarga(armas.ArmaActiva);
                case PaginaGuiaVR.Disparar:
                    return CampoDeTiro.Disparos > disparosCampoAlAbrir
                        || (armas != null && armas.ArmaActiva != ArmaAPie.Punos && !TarjetaTutorialVR.BotonApuntado
                            && EntradaVR.Pulso(EntradaVR.GatilloDerecho));
                case PaginaGuiaVR.Recargar:
                    return (recarga != null && recarga.Metidos > metidosAlAbrir) || (armas != null && armas.Recargando);
                case PaginaGuiaVR.RuedaArmas:
                    if (RuedaArmasObjetos.Abierta)
                    {
                        ruedaAbiertaEnTarjeta = true;
                    }
                    return ruedaAbiertaEnTarjeta && !RuedaArmasObjetos.Abierta && RuedaArmasObjetos.Aperturas > aperturasRuedaAlAbrir;
                case PaginaGuiaVR.Granada:
                    return especiales != null && especiales.Lanzadas > lanzadasAlAbrir;
                case PaginaGuiaVR.AgarrarLanzar:
                    return agarre != null && agarre.Agarrado != null;
                case PaginaGuiaVR.ProtesisPiernas:
                    return Activa(HabilidadProtesis.Piernas);
                case PaginaGuiaVR.ProtesisBrazos:
                    return Activa(HabilidadProtesis.Brazos);
                case PaginaGuiaVR.ProtesisVision:
                    return Activa(HabilidadProtesis.Cabeza);
                case PaginaGuiaVR.Manejar:
                    return Sostener(EnCarro && (EntradaVR.Sostenido(EntradaVR.GatilloDerecho) || ValorGatillo() > UmbralAcelerar), dt, SegundosManejar);
                default:
                    return false;
            }
        }

        private static float ValorGatillo()
        {
            UnityEngine.InputSystem.InputAction a = EntradaVR.ValorGatilloDerecho;
            return a != null && a.enabled ? a.ReadValue<float>() : 0f;
        }

        private bool Activa(HabilidadProtesis h) => habilidades != null && habilidades.Estado(h) == EstadoHabilidad.Activa;

        // Suma tiempo mientras se cumple (no se reinicia al soltar: basta un rato en total).
        private bool Sostener(bool cumple, float dt, float segundos)
        {
            if (cumple)
            {
                acumulado += dt;
            }
            return acumulado >= segundos;
        }

        // La rapidez mayor de las dos manos respecto al rig (m/s), con el tiempo real: vale con el juego detenido.
        private float MedirManos()
        {
            if (adaptador.Rig == null || adaptador.ManoIzquierda == null || adaptador.ManoDerecha == null)
            {
                return 0f;
            }
            Vector3 izq = adaptador.Rig.InverseTransformPoint(adaptador.ManoIzquierda.transform.position);
            Vector3 der = adaptador.Rig.InverseTransformPoint(adaptador.ManoDerecha.transform.position);
            float dt = Time.unscaledDeltaTime;
            float rapidez = 0f;
            if (hayManosAntes && dt > 1e-4f)
            {
                rapidez = Mathf.Max((izq - manoIzqAntes).magnitude, (der - manoDerAntes).magnitude) / dt;
            }
            manoIzqAntes = izq;
            manoDerAntes = der;
            hayManosAntes = true;
            return rapidez;
        }
    }
}
