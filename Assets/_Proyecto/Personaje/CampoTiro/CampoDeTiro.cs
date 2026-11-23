using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Armas;
using MechaCyberpunk.UI.Kit;
using UnityEngine;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// T-1722 (T79): el campo de tiro del banco de Tino. Entrar lleva al jugador (con fundido) a una sala blanca aislada, lejos de la ciudad, con todas las
    /// armas prestadas, munición infinita, sin policía ni calor y sin perder vida; Salir lo devuelve exactamente a donde estaba y deja el arsenal como era.
    /// Esta clase es la API para las pruebas; el trabajo por cuadro lo hace <see cref="CorredorCampoTiro"/> y el menú <see cref="MenuCampoTiro"/>.
    /// El botón CAMPO DE TIRO se agrega solo a la barra del banco (<see cref="PestanasBancoTino.RegistrarBotonExtra"/>).
    /// </summary>
    public static class CampoDeTiro
    {
        public const string EtiquetaBoton = "CAMPO DE TIRO";
        public const string MotivoProteccion = "campo_tiro";
        /// <summary>Centro de la sala, muy arriba de la ciudad y fuera de cualquier calle (m).</summary>
        public static readonly Vector3 Centro = new Vector3(0f, 800f, 0f);
        private const float SegundosFundido = 0.35f;
        private const int CuadrosEsperaBanco = 2;
        private const float SegundosInvulnerable = 1f;

        private static CorredorCampoTiro corredor;
        private static MenuCampoTiro menu;
        private static GameObject sala;
        private static DatosCampoTiro datos;
        private static readonly List<ObjetivoTiro> objetivos = new List<ObjetivoTiro>();
        private static PistolaAPie pistolaEscuchada;
        private static JugadorAPie jugador;
        private static ControladorAPie controlador;
        private static ArmasAPie armas;
        private static VidaAPie vida;
        private static ArsenalJugador arsenal;

        private static Vector3 posicionPrevia;
        private static Quaternion rotacionPrevia;
        private static float giroPrevio;
        private static float inclinacionPrevia;
        private static ArmaAPie armaPrevia;
        private static bool nieblaPrevia;
        private static Color colorNieblaPrevio;
        private static FogMode modoNieblaPrevio;
        private static float densidadNieblaPrevia;
        private static readonly Color ColorNiebla = new Color(0.01f, 0.02f, 0.04f, 1f);
        private const float DensidadNiebla = 0.035f;
        private static bool volverAlBanco;
        private static bool proteccionMia;
        private static CursorLockMode cursorPrevio;
        private static bool cursorVisiblePrevio;
        private static int golpesEnDisparo;


        public static bool EstaDentro
        {
            get => PestanasBancoTino.EnCampoDeTiro;
            private set => PestanasBancoTino.EnCampoDeTiro = value; // la historia (T78) no avanza ni avisa mientras estás en el campo
        }
        public static bool EnTransicion { get; private set; }
        public static bool MenuAbierto { get; private set; }
        public static ArmaAPie ArmaElegida { get; private set; } = ArmaAPie.Pistola;
        public static TipoObjetivoTiro TipoElegido { get; private set; } = TipoObjetivoTiro.Centinela;
        public static int CuantosElegidos { get; private set; } = 3;
        public static DistanciaTiro DistanciaElegida { get; private set; } = DistanciaTiro.Media;
        public static float DanioTotal { get; private set; }
        public static int Disparos { get; private set; }
        public static int Aciertos { get; private set; }
        public static IReadOnlyList<ObjetivoTiro> Objetivos => objetivos;
        public static GameObject Sala => sala;
        public static float Precision => ReglaCampoTiro.Precision(Aciertos, Disparos);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            corredor = null;
            menu = null;
            sala = null;
            datos = null;
            objetivos.Clear();
            pistolaEscuchada = null;
            jugador = null;
            controlador = null;
            armas = null;
            vida = null;
            arsenal = null;
            EstaDentro = false;
            EnTransicion = false;
            MenuAbierto = false;
            volverAlBanco = false;
            proteccionMia = false;
            ArmaElegida = ArmaAPie.Pistola;
            TipoElegido = TipoObjetivoTiro.Centinela;
            CuantosElegidos = 3;
            DistanciaElegida = DistanciaTiro.Media;
            DanioTotal = 0f;
            Disparos = 0;
            Aciertos = 0;
            golpesEnDisparo = 0;
        }

        // El botón va a la barra del banco de Tino. Se registra antes de cargar la escena (PestanasBancoTino limpia su lista al iniciar el subsistema).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegistrarBoton()
        {
            PestanasBancoTino.RegistrarBotonExtra(EtiquetaBoton, () => Entrar(), () => !EstaDentro);
        }

        /// <summary>
        /// Entra al campo de tiro: cierra el banco de Tino si está abierto (aunque la historia no deje salir), hace el fundido, guarda dónde estás y te coloca
        /// en la sala. <see cref="EstaDentro"/> es true desde que se llama; <see cref="EnTransicion"/> es true hasta que termina el fundido. False si no se puede.
        /// </summary>
        public static bool Entrar(bool conFundido = true)
        {
            if (EstaDentro || EnTransicion)
            {
                return false;
            }
            jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (jugador == null || jugador.Modo != ModoJugador.APie)
            {
                return false;
            }
            controlador = jugador.GetComponent<ControladorAPie>();
            armas = jugador.GetComponent<ArmasAPie>();
            vida = jugador.GetComponent<VidaAPie>();
            arsenal = Registro<ArsenalJugador>.Actual;
            if (controlador == null || armas == null || arsenal == null)
            {
                Debug.LogWarning("[CampoTiro] Faltan ControladorAPie, ArmasAPie o ArsenalJugador: no se puede entrar.");
                return false;
            }
            volverAlBanco = PestanasBancoTino.Abierto;
            if (volverAlBanco)
            {
                CerrarBancoSinBloqueo();
            }
            AsegurarCorredor();
            EstaDentro = true;
            EnTransicion = true;
            corredor.StartCoroutine(RutinaEntrar(conFundido));
            return true;
        }

        /// <summary>Sale del campo: fundido, vuelves exactamente a donde estabas, el arsenal queda como era y se reabre el banco si venías de él.</summary>
        public static bool Salir(bool conFundido = true)
        {
            if (!EstaDentro || EnTransicion)
            {
                return false;
            }
            EnTransicion = true;
            corredor.StartCoroutine(RutinaSalir(conFundido));
            return true;
        }

        /// <summary>Pone el arma en la mano. False si no se está dentro o el arma no pudo equiparse.</summary>
        public static bool ElegirArma(ArmaAPie arma)
        {
            if (!EstaDentro || EnTransicion || armas == null)
            {
                return false;
            }
            ArmaElegida = arma;
            bool puesta = armas.Equipar(arma);
            menu?.Refrescar();
            return puesta;
        }

        /// <summary>Quita los objetivos de ahora y pone <paramref name="cuantos"/> (1 a 5) del tipo a la distancia, de pie y con la vida llena.</summary>
        public static bool PonerObjetivos(TipoObjetivoTiro tipo, int cuantos, DistanciaTiro distancia)
        {
            if (!EstaDentro || sala == null)
            {
                return false;
            }
            TipoElegido = tipo;
            CuantosElegidos = ReglaCampoTiro.LimitarCuantos(cuantos);
            DistanciaElegida = distancia;
            foreach (ObjetivoTiro viejo in objetivos)
            {
                if (viejo != null)
                {
                    Object.Destroy(viejo.gameObject);
                }
            }
            objetivos.Clear();
            GameObject prefab = datos != null ? datos.Visual(tipo) : null;
            foreach (Vector3 local in ReglaCampoTiro.Posiciones(CuantosElegidos, distancia))
            {
                Vector3 base3 = sala.transform.position + local + Vector3.up * ReglaCampoTiro.AlturaPlataforma(tipo);
                objetivos.Add(ObjetivoTiro.Crear(sala.transform, tipo, prefab, base3, Quaternion.LookRotation(Vector3.back, Vector3.up), datos));
            }
            menu?.Refrescar();
            return true;
        }

        /// <summary>REINICIAR OBJETIVOS: todos de pie y con la vida llena, y el marcador en cero.</summary>
        public static void ReiniciarObjetivos()
        {
            foreach (ObjetivoTiro objetivo in objetivos)
            {
                if (objetivo != null)
                {
                    objetivo.Reaparecer();
                }
            }
            DanioTotal = 0f;
            Disparos = 0;
            Aciertos = 0;
            menu?.Refrescar();
        }

        /// <summary>Abre el menú de entrenamiento (la tecla E): cursor libre y control congelado.</summary>
        public static bool AbrirMenu()
        {
            if (!EstaDentro || EnTransicion || MenuAbierto || menu == null)
            {
                return false;
            }
            MenuAbierto = true;
            cursorPrevio = Cursor.lockState;
            cursorVisiblePrevio = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (controlador != null)
            {
                controlador.ActivarControl(false);
            }
            KitUI.AsegurarEventSystem();
            PilaPantallas.Abrir(menu);
            menu.Mostrar(true);
            return true;
        }

        /// <summary>Cierra el menú (Esc o clic fuera). El control vuelve un cuadro después para que la tecla no dispare nada.</summary>
        public static bool CerrarMenu()
        {
            if (!MenuAbierto)
            {
                return false;
            }
            MenuAbierto = false;
            Cursor.lockState = cursorPrevio;
            Cursor.visible = cursorVisiblePrevio;
            if (menu != null)
            {
                PilaPantallas.Cerrar(menu);
                menu.Mostrar(false);
            }
            if (corredor != null && !EnTransicion)
            {
                corredor.StartCoroutine(DevolverControlDespues());
            }
            return true;
        }

        /// <summary>Una línea con todo el estado, para las pruebas y los registros.</summary>
        public static string Estado()
        {
            int listos = 0;
            int caidos = 0;
            foreach (ObjetivoTiro o in objetivos)
            {
                if (o != null && o.EstaListo)
                {
                    listos++;
                }
                if (o != null && o.EstaCaido)
                {
                    caidos++;
                }
            }
            return "dentro=" + EstaDentro + " transicion=" + EnTransicion + " menu=" + MenuAbierto + " arma=" + (armas != null ? armas.ArmaActiva : ArmaElegida) +
                   " objetivos=" + ReglaCampoTiro.Nombre(TipoElegido) + " x" + objetivos.Count + " " + DistanciaElegida + " listos=" + listos + " caidos=" + caidos +
                   " danio=" + Mathf.RoundToInt(DanioTotal) + " disparos=" + Disparos + " aciertos=" + Aciertos + " precision=" + Mathf.RoundToInt(Precision * 100f) + "%" +
                   " clip=" + (objetivos.Count > 0 && objetivos[0] != null ? objetivos[0].ClipActual : "") + " letrero=" + (SalaCampoTiro.UltimaTerminal != null && SalaCampoTiro.UltimaTerminal.LetreroVisible);
        }

        /// <summary>El texto del marcador de la esquina.</summary>
        public static string TextoMarcador()
        {
            return "DAÑO TOTAL  " + Mathf.RoundToInt(DanioTotal) + "\nDISPAROS  " + Disparos + "   ACIERTOS  " + Aciertos + "   PRECISIÓN  " + Mathf.RoundToInt(Precision * 100f) + "%";
        }

        // ---------- por cuadro (lo llama el corredor) ----------

        internal static void Actualizar()
        {
            if (!EstaDentro)
            {
                return;
            }
            // Si el jugador desapareció (cambio de escena) se limpia todo sin fundido.
            if (jugador == null || Registro<IJugador>.Actual as JugadorAPie != jugador)
            {
                Abandonar();
                return;
            }
            if (vida != null)
            {
                vida.DarInvulnerabilidad(SegundosInvulnerable);
            }
            if (EnTransicion)
            {
                return;
            }
            Vector3 local = jugador.transform.position - Centro;
            if (ReglaCampoTiro.EstaLimitado(local))
            {
                jugador.Colocar(new Pose(Centro + ReglaCampoTiro.LimitarAlJugador(local), jugador.transform.rotation));
            }
            menu?.PonerMarcador(TextoMarcador());
        }

        internal static void AlFinalDelCuadro() => golpesEnDisparo = 0;

        internal static void AlGolpearObjetivo(ObjetivoTiro objetivo, float danio, bool causoCaida)
        {
            DanioTotal += danio;
            golpesEnDisparo++;
        }

        private static void AlDisparar()
        {
            Disparos++;
            if (golpesEnDisparo > 0)
            {
                Aciertos++;
            }
            golpesEnDisparo = 0;
        }

        // ---------- rutinas ----------

        private static IEnumerator RutinaEntrar(bool conFundido)
        {
            // Si venía del banco, su control vuelve un cuadro después de cerrarse: se espera para que no deshaga el congelado de aquí.
            for (int i = 0; i < CuadrosEsperaBanco; i++)
            {
                yield return null;
            }
            controlador.ActivarControl(false);
            if (conFundido)
            {
                yield return corredor.Fundir(1f, SegundosFundido);
            }
            posicionPrevia = jugador.transform.position;
            rotacionPrevia = jugador.transform.rotation;
            giroPrevio = controlador.GiroCamara;
            inclinacionPrevia = controlador.Inclinacion;
            armaPrevia = armas.ArmaActiva;
            nieblaPrevia = RenderSettings.fog;
            colorNieblaPrevio = RenderSettings.fogColor;
            modoNieblaPrevio = RenderSettings.fogMode;
            densidadNieblaPrevia = RenderSettings.fogDensity;
            // Niebla oscura suave: la cuadrícula holográfica se pierde a lo lejos.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = ColorNiebla;
            RenderSettings.fogDensity = DensidadNiebla;
            JugadorAPie.GuardadoFijo = (posicionPrevia, rotacionPrevia.eulerAngles.y, armaPrevia);
            if (datos == null)
            {
                datos = Resources.Load<DatosCampoTiro>(DatosCampoTiro.RutaResources);
                if (datos == null)
                {
                    Debug.LogWarning("[CampoTiro] No hay DatosCampoTiro en Resources: corre AplicarCampoTiro.Aplicar. Se usan materiales y cápsulas por omisión.");
                }
            }
            if (sala == null)
            {
                sala = SalaCampoTiro.Construir(datos, Centro);
            }
            // Antes de prestar las armas y de colocarse: la protección contra la policía y NOVA.
            if (string.IsNullOrEmpty(MisionProtegida.Motivo))
            {
                MisionProtegida.Activar(MotivoProteccion, false);
                proteccionMia = true;
            }
            arsenal.PrestarTodas();
            pistolaEscuchada = jugador.GetComponent<PistolaAPie>();
            if (pistolaEscuchada != null)
            {
                pistolaEscuchada.Disparo += AlDisparar;
            }
            ObjetivoTiro.Golpeado += AlGolpearObjetivo;
            DanioTotal = 0f;
            Disparos = 0;
            Aciertos = 0;
            jugador.Colocar(new Pose(Centro + new Vector3(0f, 0.1f, ReglaCampoTiro.ZInicioJugador), Quaternion.identity));
            controlador.Empujar(Mathf.DeltaAngle(controlador.GiroCamara, 0f), -controlador.Inclinacion);
            yield return null; // las armas prestadas arman sus cargadores en el siguiente Update
            armas.Equipar(ArmaAPie.Pistola);
            ArmaElegida = ArmaAPie.Pistola;
            PonerObjetivos(TipoElegido, CuantosElegidos, DistanciaElegida);
            menu.Mostrar(false);
            menu.MostrarMarcador(true);
            corredor.Destello(); // aparición holográfica del jugador
            if (conFundido)
            {
                yield return corredor.Fundir(0f, SegundosFundido);
            }
            controlador.ActivarControl(true);
            EnTransicion = false;
            Debug.Log("[CampoTiro] Dentro del campo de tiro.");
        }

        private static IEnumerator RutinaSalir(bool conFundido)
        {
            if (MenuAbierto)
            {
                CerrarMenu();
            }
            controlador.ActivarControl(false);
            if (conFundido)
            {
                yield return corredor.Fundir(1f, SegundosFundido);
            }
            armas.Equipar(armaPrevia);
            LimpiarEstadoDelCampo();
            jugador.Colocar(new Pose(posicionPrevia, rotacionPrevia));
            // La red de seguridad pudo guardar la sala (y=800) como punto seguro: al bajar a la calle creería que el jugador se cayó y lo regresaría.
            RedSeguridadJugador red = jugador.GetComponent<RedSeguridadJugador>();
            if (red != null)
            {
                red.FijarSeguro(new Pose(posicionPrevia, rotacionPrevia));
            }
            controlador.Empujar(Mathf.DeltaAngle(controlador.GiroCamara, giroPrevio), inclinacionPrevia - controlador.Inclinacion);
            EstaDentro = false;
            menu.MostrarMarcador(false);
            controlador.ActivarControl(true);
            if (volverAlBanco)
            {
                // El banco congela el control a pie mientras está abierto y lo devuelve al cerrarse; si la historia lo bloquea, sigue valiendo.
                PestanasBancoTino.Abrir();
            }
            volverAlBanco = false;
            if (conFundido)
            {
                yield return corredor.Fundir(0f, SegundosFundido);
            }
            EnTransicion = false;
            Debug.Log("[CampoTiro] De vuelta en la tienda.");
        }

        private static IEnumerator DevolverControlDespues()
        {
            yield return null;
            if (controlador != null && !MenuAbierto && !EnTransicion && EstaDentro)
            {
                controlador.ActivarControl(true);
            }
        }

        // Quita todo lo que el campo puso: armas prestadas, protección, eventos, sala y objetivos.
        private static void LimpiarEstadoDelCampo()
        {
            if (arsenal != null)
            {
                arsenal.DevolverPrestadas();
            }
            else
            {
                CargadorPistola.MunicionInfinita = false;
            }
            if (pistolaEscuchada != null)
            {
                pistolaEscuchada.Disparo -= AlDisparar;
            }
            pistolaEscuchada = null;
            ObjetivoTiro.Golpeado -= AlGolpearObjetivo;
            if (proteccionMia)
            {
                MisionProtegida.Desactivar();
                proteccionMia = false;
            }
            JugadorAPie.GuardadoFijo = null;
            RenderSettings.fog = nieblaPrevia;
            RenderSettings.fogColor = colorNieblaPrevio;
            RenderSettings.fogMode = modoNieblaPrevio;
            RenderSettings.fogDensity = densidadNieblaPrevia;
            foreach (ObjetivoTiro objetivo in objetivos)
            {
                if (objetivo != null)
                {
                    Object.Destroy(objetivo.gameObject);
                }
            }
            objetivos.Clear();
            if (sala != null)
            {
                Object.Destroy(sala);
                sala = null;
            }
            if (MenuAbierto)
            {
                CerrarMenu();
            }
        }

        private static void Abandonar()
        {
            LimpiarEstadoDelCampo();
            EstaDentro = false;
            EnTransicion = false;
            volverAlBanco = false;
            if (menu != null)
            {
                menu.MostrarMarcador(false);
            }
        }

        private static void CerrarBancoSinBloqueo()
        {
            System.Func<string> bloqueo = PestanasBancoTino.BloqueoSalida;
            PestanasBancoTino.BloqueoSalida = null;
            PestanasBancoTino.Cerrar();
            PestanasBancoTino.BloqueoSalida = bloqueo;
        }

        private static void AsegurarCorredor()
        {
            if (corredor != null)
            {
                return;
            }
            GameObject objeto = new GameObject("CampoDeTiro_Corredor");
            corredor = objeto.AddComponent<CorredorCampoTiro>();
            menu = objeto.AddComponent<MenuCampoTiro>();
        }
    }
}
