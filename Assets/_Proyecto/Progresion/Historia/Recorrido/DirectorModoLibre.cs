using System;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Guardado;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Partida;
using MechaCyberpunk.UI.Armas;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-historia-libre-23nov (escalada progresiva, 24-nov): el modo libre de venganza. Empieza sin aviso al salir de la tienda de Tino tras el campo de tiro
    /// (o al CONTINUAR/REINTENTAR una partida que ya pasó por ahí): objetivo pequeño "SOBREVIVE", fase 0 solo con los SKULLS y luego las fases 1 a 6 de
    /// <see cref="ParametrosRespuesta.FasesLibre"/> (<see cref="ReglaFasesLibre"/>), que se pasan al director de respuesta que ya existe
    /// (<see cref="DirectorRespuesta.PonerModoLibre"/>): él y sus generadores (GestorPersecucion, RespuestaNova) traen a la policía y a NOVA.
    /// Cuenta bajas de cada bando y el tiempo (<see cref="MarcadorModoLibre"/>), publica <see cref="FaseJuego"/> y, al morir, abre <see cref="PantallaMuerteLibre"/>.
    /// Guarda un punto de reintento (copia del autoguardado) al empezar y el mejor récord en PlayerPrefs.
    /// </summary>
    public class DirectorModoLibre : MonoBehaviour
    {
        public const string TextoObjetivo = "SOBREVIVE";
        public const string ClaveMejorTiempo = "modo_libre_mejor_tiempo";
        public const string ClaveMejorBajas = "modo_libre_mejor_bajas";
        private const string ExtensionReintento = ".reintento";
        private const float SegundosEntreRevisiones = 0.5f;
        private const float SegundosAntesDePantalla = 2f;

        public static DirectorModoLibre Actual { get; private set; }

        private ReglaFasesLibre regla;
        private ParametrosRespuesta numeros;
        private ReglaFaseSkulls reglaSkulls;
        private FaseModoLibre faseSkulls;
        private GoteoSkullsLibre goteo;
        private float proximaRevision;
        private bool pausadoPorCampo;
        private bool muerto;
        private float muertoDesde;
        private bool pantallaAbierta;
        private VidaAPie vida;
        private readonly HashSet<PandilleroAPie> vigilados = new HashSet<PandilleroAPie>();
        private readonly List<PandilleroAPie> limpiar = new List<PandilleroAPie>();

        /// <summary>Nivel de escalada: 0 (solo SKULLS, T94: goteo de grupos y sin policía) a 6.</summary>
        public int Nivel { get; private set; }

        public int SkullsMuertos { get; private set; }
        public int PoliciasMuertos { get; private set; }
        public int NovaMuertos { get; private set; }
        public float SegundosVivo { get; private set; }
        public int EstrellasMaximas { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => Actual = null;

        /// <summary>Arranca el modo libre (una vez por escena). <paramref name="guardarReintento"/>: guarda la partida y deja el punto de REINTENTAR aquí.</summary>
        public static DirectorModoLibre Iniciar(bool guardarReintento)
        {
            if (Actual != null)
            {
                return Actual;
            }
            DirectorModoLibre d = new GameObject("DirectorModoLibre").AddComponent<DirectorModoLibre>();
            d.Arrancar(guardarReintento);
            return d;
        }

        private void Arrancar(bool guardarReintento)
        {
            numeros = DirectorRespuesta.Parametros;
            regla = new ReglaFasesLibre(numeros.FasesLibre);
            Nivel = 0;
            // T94: fase 0 solo con SKULLS: una fase del director sin tipos (no llega la policía aunque tengas estrellas) y el goteo de SKULLS.
            reglaSkulls = new ReglaFaseSkulls(numeros);
            faseSkulls = new FaseModoLibre("SKULLS", 0f, 1, false, new TipoRespuesta[0], 0, null);
            goteo = gameObject.AddComponent<GoteoSkullsLibre>();
            goteo.Configurar(numeros);
            goteo.SkullVencido += AlVencerSkullGoteo;
            DirectorRespuesta.PonerModoLibre(faseSkulls);
            FaseJuego.Reiniciar(FaseJuego.Fase.Skulls, 0);
            (Registro<IObjetivos>.Actual as ServicioObjetivos)?.Fijar(new Objetivo(TextoObjetivo, IdLugar.Ninguno));
            MisionProtegida.Desactivar();
            GuardarPuntoReintento(guardarReintento);
            gameObject.AddComponent<MarcadorModoLibre>().Configurar(this);
            Debug.Log("[Historia] Empieza el modo libre (SOBREVIVE). Fases: " + regla.Total + ", fase 0 SKULLS de " + numeros.SegundosFaseSkulls + " s o "
                + numeros.SkullsFaseSkulls + " SKULLS (tope " + numeros.SegundosMaximosFaseSkulls + " s).");
        }

        private void Awake()
        {
            if (Actual != null && Actual != this)
            {
                Destroy(gameObject);
                return;
            }
            Actual = this;
        }

        private void OnEnable() => DirectorRespuesta.Baja += AlMorirUnidad;

        private void OnDisable()
        {
            DirectorRespuesta.Baja -= AlMorirUnidad;
            if (vida != null)
            {
                vida.KO -= AlMorirJugador;
            }
        }

        private void OnDestroy()
        {
            if (Actual == this)
            {
                Actual = null;
                DirectorRespuesta.PonerModoLibre(null);
            }
            foreach (PandilleroAPie p in vigilados)
            {
                if (p != null)
                {
                    p.Vencido -= AlVencerPandillero;
                }
            }
            vigilados.Clear();
            if (goteo != null)
            {
                goteo.SkullVencido -= AlVencerSkullGoteo;
            }
        }

        private void Update()
        {
            if (regla == null)
            {
                return;
            }
            if (muerto)
            {
                if (!pantallaAbierta && Time.unscaledTime - muertoDesde >= SegundosAntesDePantalla)
                {
                    pantallaAbierta = true;
                    PantallaMuerteLibre.Abrir(this);
                }
                return;
            }
            // En el campo de tiro (se puede volver con Tino) la escalada se pausa: la sala tiene su propia protección.
            bool enCampo = PestanasBancoTino.EnCampoDeTiro;
            if (enCampo != pausadoPorCampo)
            {
                pausadoPorCampo = enCampo;
                DirectorRespuesta.PonerModoLibre(enCampo ? null : FaseDelDirector);
            }
            if (enCampo || CinematicaEnCurso.Activa || Pausa.Activa)
            {
                return;
            }
            float dt = Time.deltaTime;
            SegundosVivo += dt;
            if (Nivel == 0)
            {
                AvanzarFaseSkulls(dt);
            }
            else if (regla.Avanzar(dt))
            {
                Nivel = regla.Nivel;
                AplicarFase();
            }
            if (Time.time >= proximaRevision)
            {
                proximaRevision = Time.time + SegundosEntreRevisiones;
                Revisar();
            }
        }

        /// <summary>T94: la fase que se le pasa al director de respuesta (la de SKULLS, sin tipos, en la fase 0).</summary>
        private FaseModoLibre FaseDelDirector => Nivel == 0 ? faseSkulls : regla.Fase;

        // T94: la fase 0: goteo de grupos de SKULLS (dentro del tope de enemigos del modo libre) hasta que la regla diga que llega la policía.
        private void AvanzarFaseSkulls(float dt)
        {
            reglaSkulls.Avanzar(dt);
            if (reglaSkulls.Terminada)
            {
                Debug.Log("[Historia] T94: termina la fase 0 SKULLS a los " + reglaSkulls.Segundos.ToString("F0") + " s con " + reglaSkulls.Muertes
                    + " SKULLS vencidos (" + goteo.Generados + " del goteo, " + goteo.GeneradosArmados + " armados).");
                Nivel = 1;
                regla.Iniciar(1);
                AplicarFase();
                return;
            }
            int espacio = numeros.TopeEnemigosLibreActual - DirectorRespuesta.VivosLibre;
            int cuantos = reglaSkulls.CuantosGenerar(goteo.Vivos, espacio, UnityEngine.Random.value);
            if (cuantos <= 0)
            {
                return;
            }
            if (goteo.Generar(cuantos, () => reglaSkulls.EsArmado(UnityEngine.Random.value)) > 0)
            {
                reglaSkulls.RegistrarGrupo();
            }
            else
            {
                reglaSkulls.RegistrarFallo();
            }
        }

        private void AlVencerSkullGoteo(PandilleroAPie p)
        {
            if (muerto)
            {
                return;
            }
            SkullsMuertos++;
            if (Nivel == 0 && !pausadoPorCampo)
            {
                reglaSkulls.RegistrarMuerte();
            }
        }

        private void AplicarFase()
        {
            FaseModoLibre fase = regla.Fase;
            DirectorRespuesta.PonerModoLibre(fase);
            FaseJuego.Fijar(fase.ModoNova ? FaseJuego.Fase.Nova : FaseJuego.Fase.Policia, Nivel);
            Debug.Log("[Historia] Modo libre: fase " + Nivel + " " + fase.Nombre + " a los " + SegundosVivo.ToString("F0") + " s.");
            SostenerEstrellas();
        }

        // Cada medio segundo: las estrellas de la fase, sin peleas protegidas, el jugador vigilado y los SKULLS nuevos.
        private void Revisar()
        {
            if (MisionProtegida.Activa)
            {
                MisionProtegida.Desactivar(); // las peleas protegidas ya no aplican en el modo libre
            }
            SostenerEstrellas();
            EstrellasMaximas = Mathf.Max(EstrellasMaximas, EstrellasAhora());
            VigilarJugador();
            VigilarSkulls();
        }

        private void SostenerEstrellas()
        {
            if (Nivel <= 0)
            {
                return;
            }
            ICalor calor = Registro<ICalor>.Actual;
            if (calor != null && EstrellasBusqueda.De(calor) < regla.Fase.Estrellas)
            {
                EstrellasBusqueda.SubirA(calor, regla.Fase.Estrellas);
            }
        }

        private static int EstrellasAhora() => EstrellasBusqueda.De(Registro<ICalor>.Actual);

        private void VigilarJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            VidaAPie nueva = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<VidaAPie>(true) : null;
            if (nueva == vida)
            {
                return;
            }
            if (vida != null)
            {
                vida.KO -= AlMorirJugador;
            }
            vida = nueva;
            if (vida != null)
            {
                vida.KO += AlMorirJugador;
            }
        }

        private void VigilarSkulls()
        {
            limpiar.Clear();
            foreach (PandilleroAPie p in vigilados)
            {
                if (p == null)
                {
                    limpiar.Add(p);
                }
            }
            foreach (PandilleroAPie p in limpiar)
            {
                vigilados.Remove(p);
            }
            foreach (PandilleroAPie p in PandilleroAPie.Activos)
            {
                if (p != null && !p.EstaKO && p.Pandilla == IdPandilla.Calaveras && !p.EsDeNova && !goteo.EsDelGoteo(p) && vigilados.Add(p))
                {
                    p.Vencido += AlVencerPandillero;
                }
            }
        }

        private void AlVencerPandillero(PandilleroAPie p)
        {
            if (p != null)
            {
                p.Vencido -= AlVencerPandillero;
                vigilados.Remove(p);
            }
            if (!muerto)
            {
                SkullsMuertos++;
                if (Nivel == 0 && !pausadoPorCampo)
                {
                    reglaSkulls.RegistrarMuerte(); // T94: los SKULLS del mapa también cuentan para terminar la fase 0
                }
            }
        }

        private void AlMorirUnidad(TipoRespuesta tipo)
        {
            if (muerto)
            {
                return;
            }
            if (ReglaEscaladaRespuesta.EsNova(tipo) || (tipo == TipoRespuesta.Dron && DirectorRespuesta.ModoNova))
            {
                NovaMuertos++;
            }
            else if (tipo == TipoRespuesta.PoliciaAPie)
            {
                PoliciasMuertos++;
            }
            if (Nivel > 0 && !pausadoPorCampo)
            {
                regla.RegistrarBaja(tipo);
            }
        }

        private void AlMorirJugador()
        {
            if (muerto)
            {
                return;
            }
            muerto = true;
            muertoDesde = Time.unscaledTime;
            EstrellasMaximas = Mathf.Max(EstrellasMaximas, EstrellasAhora());
            FaseJuego.Fijar(FaseJuego.Fase.Muerto, Nivel);
            GuardarRecord();
            IJugador jugador = Registro<IJugador>.Actual;
            ControladorAPie control = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponentInChildren<ControladorAPie>(true) : null;
            control?.ActivarControl(false);
            Debug.Log("[Historia] Modo libre: el jugador murió en la fase " + Nivel + " a los " + SegundosVivo.ToString("F0") + " s (SKULLS " + SkullsMuertos
                + ", policías " + PoliciasMuertos + ", NOVA " + NovaMuertos + ").");
        }

        // ---------- Récord y reintento ----------

        public int BajasTotales => SkullsMuertos + PoliciasMuertos + NovaMuertos;

        public static float MejorTiempo => PlayerPrefs.GetFloat(ClaveMejorTiempo, 0f);

        public static int MejoresBajas => PlayerPrefs.GetInt(ClaveMejorBajas, 0);

        /// <summary>True si esta partida superó el mejor tiempo guardado (se calcula al morir).</summary>
        public bool NuevoRecord { get; private set; }

        private void GuardarRecord()
        {
            NuevoRecord = SegundosVivo > MejorTiempo;
            if (NuevoRecord)
            {
                PlayerPrefs.SetFloat(ClaveMejorTiempo, SegundosVivo);
            }
            if (BajasTotales > MejoresBajas)
            {
                PlayerPrefs.SetInt(ClaveMejorBajas, BajasTotales);
            }
            PlayerPrefs.Save();
        }

        private static string RutaReintento()
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            return servicio != null ? servicio.RutaRanura(ServicioGuardado.RanuraAuto) + ExtensionReintento : null;
        }

        // Al empezar el modo libre (justo después del campo de tiro) se guarda y se copia el autoguardado: REINTENTAR vuelve a esa copia.
        private static void GuardarPuntoReintento(bool guardar)
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            string reintento = RutaReintento();
            if (servicio == null || reintento == null)
            {
                return;
            }
            try
            {
                if (guardar)
                {
                    servicio.Guardar();
                }
                string auto = servicio.RutaRanura(ServicioGuardado.RanuraAuto);
                if ((guardar || !File.Exists(reintento)) && File.Exists(auto))
                {
                    File.Copy(auto, reintento, true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Historia] No se pudo guardar el punto de reintento del modo libre: " + e.Message);
            }
        }

        /// <summary>REINTENTAR: vuelve al momento justo después del campo de tiro (arma, mejoras y dinero de entonces) y empieza otra vez el modo libre.</summary>
        public static void Reintentar()
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            string reintento = RutaReintento();
            try
            {
                if (servicio != null && reintento != null && File.Exists(reintento))
                {
                    File.Copy(reintento, servicio.RutaRanura(ServicioGuardado.RanuraAuto), true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Historia] No se pudo restaurar el punto de reintento: " + e.Message);
            }
            FaseJuego.Reiniciar(FaseJuego.Fase.Skulls, 0);
            FlujoPartida.IrACargar(ServicioGuardado.RanuraAuto);
        }

        /// <summary>MENÚ PRINCIPAL desde la pantalla HAS MUERTO.</summary>
        public static void IrAlMenu()
        {
            FaseJuego.Reiniciar(FaseJuego.Fase.Guiada, 0);
            FlujoPartida.IrAlTitulo();
        }

        /// <summary>Depuración (eval): fase, bajas y tiempo.</summary>
        public static string DepurarModoLibre()
        {
            DirectorModoLibre d = Actual;
            if (d == null)
            {
                return "[Historia] sin modo libre";
            }
            string fase0 = d.Nivel == 0 && d.reglaSkulls != null ? " SKULLS " + d.reglaSkulls.Segundos.ToString("F0") + "s bajas " + d.reglaSkulls.Muertes
                + "/" + d.numeros.SkullsFaseSkulls + " goteo vivos " + d.goteo.Vivos + " (" + d.goteo.GeneradosArmados + "/" + d.goteo.Generados + " armados)" : string.Empty;
            return "[Historia] modo libre fase " + d.Nivel + fase0 + (d.Nivel > 0 ? " " + d.regla.Fase.Nombre + " " + d.regla.SegundosEnFase.ToString("F0") + "s bajas "
                + d.regla.MuertesDeFase + "/" + d.regla.Fase.Muertes : string.Empty) + " | SKULLS " + d.SkullsMuertos + " policías " + d.PoliciasMuertos
                + " NOVA " + d.NovaMuertos + " | " + d.SegundosVivo.ToString("F0") + " s | vivos " + DirectorRespuesta.VivosLibre;
        }

        /// <summary>Depuración: salta a la fase dada (1 a 6).</summary>
        public static void DepurarSaltarAFase(int nivel)
        {
            DirectorModoLibre d = Actual;
            if (d == null)
            {
                return;
            }
            d.regla.Iniciar(nivel);
            d.Nivel = d.regla.Nivel;
            d.AplicarFase();
        }
    }
}
