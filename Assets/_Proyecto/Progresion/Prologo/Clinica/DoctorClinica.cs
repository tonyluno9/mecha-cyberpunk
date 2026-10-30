using System;
using System.Collections;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Historia;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Progresion.Prologo.Clinica
{
    /// <summary>
    /// Caer en la calle (T-196, D-tony-170): si el jugador cae en KO a pie, o su mecha cae en KO fuera de una pelea del flujo, y la historia
    /// está en Libre, espera unos segundos, funde a negro y despierta en la camilla de la clínica horas después: el doctor cobra a crédito
    /// (el dinero puede quedar en negativo), el calor se borra, la misión en curso falla, el mecha que andaba en la calle vuelve solo al
    /// taller muy dañado y el objetivo manda a casa. Una sola secuencia por caída. Durante la secuencia el jugador no tiene control
    /// ni llega a ninguna cama, así que no se puede guardar. Va en DoctorClinica.prefab.
    /// </summary>
    public class DoctorClinica : MonoBehaviour
    {
        [SerializeField] private DatosDoctor datos;

        private VidaAPie vidaJugador;
        private VidaMecha vidaMecha;
        private MechaPersistente persistente;
        private LlamadoMecha llamado;
        private ControladorAPie controladorJugador;
        private float proximaBusqueda;
        private int peleasDelFlujo;
        private bool objetivoPuesto;
        private ServicioMisiones misionesSuscritas;
        private Coroutine rutinaSubtitulo;

        /// <summary>True desde que cae el jugador hasta que se le devuelve el control. Mientras tanto no se atiende otra caída.</summary>
        public bool Ocupado { get; private set; }

        /// <summary>Cuántas caídas ha atendido el doctor.</summary>
        public int Caidas { get; private set; }

        /// <summary>El texto del último subtítulo del doctor.</summary>
        public string UltimoTexto { get; private set; } = string.Empty;

        public DatosDoctor Datos => datos;

        /// <summary>Se avisa cuando termina cada secuencia (el jugador ya despertó y tiene el control).</summary>
        public event Action Terminada;

        private void Awake()
        {
            if (datos == null)
            {
                Debug.LogError("[Progresion] Falta DatosDoctor en DoctorClinica de " + name, this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            EventosPelea.PeleaIniciada += AlIniciarPeleaDelFlujo;
            EventosPelea.PeleaTerminada += AlTerminarPeleaDelFlujo;
            EventosLugares.JugadorEntro += AlEntrarLugar;
            peleasDelFlujo = 0;
        }

        private void OnDisable()
        {
            EventosPelea.PeleaIniciada -= AlIniciarPeleaDelFlujo;
            EventosPelea.PeleaTerminada -= AlTerminarPeleaDelFlujo;
            EventosLugares.JugadorEntro -= AlEntrarLugar;
            Desvincular();
            if (misionesSuscritas != null)
            {
                misionesSuscritas.Aceptada -= AlAceptarMision;
                misionesSuscritas = null;
            }
            StopAllCoroutines();
            Ocupado = false;
            rutinaSubtitulo = null;
        }

        private void Update()
        {
            // El jugador y el mecha pueden aparecer después que el doctor: se buscan cada cierto tiempo, no cada cuadro.
            if (Time.unscaledTime < proximaBusqueda)
            {
                return;
            }
            proximaBusqueda = Time.unscaledTime + datos.SegundosBusqueda;
            VincularJugador();
            VincularMecha();
            if (misionesSuscritas == null)
            {
                misionesSuscritas = Registro<ServicioMisiones>.Actual;
                if (misionesSuscritas != null)
                {
                    misionesSuscritas.Aceptada += AlAceptarMision;
                }
            }
        }

        // ---------- Vínculos ----------

        private void VincularJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            VidaAPie vida = jugador != null && jugador.Raiz != null ? jugador.Raiz.GetComponent<VidaAPie>() : null;
            if (vida == vidaJugador)
            {
                return;
            }
            if (vidaJugador != null)
            {
                vidaJugador.KO -= AlCaerElJugador;
            }
            vidaJugador = vida;
            controladorJugador = vida != null ? vida.GetComponent<ControladorAPie>() : null;
            if (vidaJugador != null)
            {
                vidaJugador.KO += AlCaerElJugador;
            }
        }

        private void VincularMecha()
        {
            if (persistente != null)
            {
                return;
            }
            MechaPersistente encontrado = Object.FindFirstObjectByType<MechaPersistente>();
            if (encontrado == null)
            {
                return;
            }
            persistente = encontrado;
            vidaMecha = encontrado.GetComponent<VidaMecha>();
            llamado = encontrado.GetComponent<LlamadoMecha>();
            if (vidaMecha != null)
            {
                vidaMecha.KO += AlCaerElMecha;
            }
        }

        private void Desvincular()
        {
            if (vidaJugador != null)
            {
                vidaJugador.KO -= AlCaerElJugador;
                vidaJugador = null;
            }
            if (vidaMecha != null)
            {
                vidaMecha.KO -= AlCaerElMecha;
                vidaMecha = null;
            }
            persistente = null;
        }

        // ---------- Disparadores ----------

        private void AlIniciarPeleaDelFlujo() => peleasDelFlujo++;

        private void AlTerminarPeleaDelFlujo(ResultadoPelea resultado) => peleasDelFlujo = Mathf.Max(0, peleasDelFlujo - 1);

        private void AlCaerElJugador() => Atender(false);

        private void AlCaerElMecha() => Atender(true);

        private void Atender(bool cayoElMecha)
        {
            if (Ocupado || !isActiveAndEnabled)
            {
                return;
            }
            IHistoria historia = Registro<IHistoria>.Actual;
            bool enFlujo = peleasDelFlujo > 0 || (llamado != null && llamado.Estado == EstadoLlamado.EnArena);
            if (!ReglaDoctor.Actua(historia != null, historia != null ? historia.Etapa : EtapaPrologo.Libre, cayoElMecha, enFlujo))
            {
                return;
            }
            Ocupado = true;
            Caidas++;
            StartCoroutine(Secuencia());
        }

        // ---------- La secuencia ----------

        private IEnumerator Secuencia()
        {
            Debug.Log("[Progresion] El doctor atiende una caída en la calle.");
            yield return EsperarSinEscala(datos.SegundosEspera);

            PantallaFundido fundido = PantallaFundido.Actual;
            if (fundido != null)
            {
                yield return fundido.Fundir(1f, fundido.Config.SegundosSalida);
            }

            // Con la pantalla en negro.
            BajarDelMecha();
            FallarMisionEnCurso();
            LlevarElMechaAlTaller();
            Registro<ICalor>.Actual?.Borrar();
            DormirHoras();
            ColocarEnLaCamilla();
            if (vidaJugador != null)
            {
                vidaJugador.Restaurar();
            }
            // Un par de cuadros con el control encendido para que la cámara se levante; luego se congela hasta destapar.
            yield return null;
            yield return null;
            if (controladorJugador != null)
            {
                controladorJugador.ActivarControl(false);
            }
            yield return EsperarSinEscala(datos.SegundosNegro);

            int dineroDespues = Cobrar();
            FijarObjetivoCasa();
            MostrarSubtitulo(ReglaDoctor.TextoDoctor(dineroDespues, datos.TextoDoctor, datos.TextoDeuda));
            if (fundido != null)
            {
                yield return fundido.Fundir(0f, fundido.Config.SegundosEntrada);
            }
            if (controladorJugador != null)
            {
                controladorJugador.ActivarControl(true);
            }
            Ocupado = false;
            Terminada?.Invoke();
        }

        private static IEnumerator EsperarSinEscala(float segundos)
        {
            float transcurrido = 0f;
            while (transcurrido < segundos)
            {
                yield return null;
                transcurrido += Time.unscaledDeltaTime;
            }
        }

        // Si iba montado: baja por el camino de siempre; si no puede (apagado, en movimiento o sin lugar), por la bajada forzada.
        private void BajarDelMecha()
        {
            JugadorAPie jugador = Registro<IJugador>.Actual as JugadorAPie;
            if (jugador == null || jugador.MontableActual == null)
            {
                return;
            }
            if (jugador.Bajar())
            {
                return;
            }
            MontajeMecha montable = jugador.MontableActual as MontajeMecha;
            if (montable != null)
            {
                montable.ForzarSiguienteBajada();
                if (jugador.Bajar())
                {
                    return;
                }
            }
            Debug.LogWarning("[Progresion] El doctor no pudo bajar al jugador de " + jugador.MontableActual.Nombre + ".", this);
        }

        private static void FallarMisionEnCurso()
        {
            ServicioMisiones misiones = Registro<ServicioMisiones>.Actual;
            if (misiones != null && misiones.HayMision)
            {
                misiones.FallarExterno(MotivoFalloMision.KO);
            }
        }

        // Fuera del taller (calle o arena): vuelve solo con la menor vida entre la suya y la del doctor. En el taller no se toca.
        private void LlevarElMechaAlTaller()
        {
            if (persistente == null || llamado == null || vidaMecha == null || llamado.Estado == EstadoLlamado.EnTaller)
            {
                return;
            }
            float fraccionActual = vidaMecha.Maxima > 0f ? vidaMecha.Actual / vidaMecha.Maxima : 0f;
            float fraccion = ReglaDoctor.FraccionMecha(fraccionActual, datos.FraccionVidaMecha, true);
            llamado.VolverAlTallerYa();
            persistente.FijarFraccionVida(fraccion);
        }

        private void DormirHoras()
        {
            IRelojMundo reloj = Registro<IRelojMundo>.Actual;
            if (reloj != null)
            {
                reloj.AvanzarHasta(ReglaDoctor.HoraDeDespertar(reloj.Hora, datos.HorasInconsciente));
            }
            Sonidos.Reproducir("ECO_CLINICA_DESPERTAR");
        }

        // Camilla, o la cama de casa, o donde cayó (con un aviso).
        private void ColocarEnLaCamilla()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null)
            {
                return;
            }
            IPuntoAparicion punto = BuscarAparicion(datos.IdAparicionCamilla) ?? BuscarAparicion(datos.IdAparicionCasa);
            if (punto == null)
            {
                Debug.LogWarning("[Progresion] El doctor no encontró '" + datos.IdAparicionCamilla + "' ni '" + datos.IdAparicionCasa
                    + "': el jugador despierta donde cayó.", this);
                return;
            }
            jugador.Colocar(punto.Pose);
        }

        private int Cobrar()
        {
            ICartera cartera = Registro<ICartera>.Actual;
            if (cartera == null)
            {
                Debug.LogWarning("[Progresion] El doctor no encontró la cartera: no cobra.", this);
                return 0;
            }
            if (datos.Cobro > 0)
            {
                cartera.CargarDeuda(datos.Cobro, MotivoDinero.Clinica);
            }
            return cartera.Dinero;
        }

        private void FijarObjetivoCasa()
        {
            ServicioObjetivos objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            if (objetivos == null)
            {
                return;
            }
            objetivos.Fijar(new Objetivo(datos.TextoObjetivo, datos.LugarCasa));
            objetivoPuesto = true;
        }

        private void MostrarSubtitulo(string texto)
        {
            UltimoTexto = texto;
            PantallaSubtitulos subtitulos = Object.FindFirstObjectByType<PantallaSubtitulos>();
            if (subtitulos == null)
            {
                Debug.LogWarning("[Progresion] El doctor no encontró los subtítulos de la escena.", this);
                return;
            }
            subtitulos.Mostrar(texto);
            if (rutinaSubtitulo != null)
            {
                StopCoroutine(rutinaSubtitulo);
            }
            rutinaSubtitulo = StartCoroutine(OcultarSubtitulo(subtitulos));
        }

        private IEnumerator OcultarSubtitulo(PantallaSubtitulos subtitulos)
        {
            yield return EsperarSinEscala(datos.SegundosSubtitulo);
            if (subtitulos != null)
            {
                subtitulos.Ocultar();
            }
            rutinaSubtitulo = null;
        }

        // ---------- El objetivo de volver a casa ----------

        private void AlEntrarLugar(IdLugar lugar)
        {
            if (lugar == datos.LugarCasa)
            {
                QuitarObjetivoPropio();
            }
        }

        private void AlAceptarMision(DatosMision mision) => QuitarObjetivoPropio();

        // Solo quita el objetivo si sigue siendo el del doctor: uno nuevo (una misión) ya lo reemplazó.
        private void QuitarObjetivoPropio()
        {
            if (!objetivoPuesto)
            {
                return;
            }
            objetivoPuesto = false;
            ServicioObjetivos objetivos = Registro<IObjetivos>.Actual as ServicioObjetivos;
            if (objetivos != null && objetivos.HayObjetivo && objetivos.Actual.Texto == datos.TextoObjetivo)
            {
                objetivos.Quitar();
            }
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
    }
}
