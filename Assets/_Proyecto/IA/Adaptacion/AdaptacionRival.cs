using System;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using UnityEngine;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>
    /// El rival observa cómo pelea el jugador y lo recuerda (T-200, D-tony-191 a D-tony-195). Cuenta puños lanzados, golpes del rival que
    /// llegaron (y cuáles venían cubiertos) y golpes del jugador que conectaron (y cuáles castigaban); al terminar la pelea actualiza su
    /// memoria con <see cref="ReglaAdaptacion"/> y avisa lo que aprendió u olvidó. Todavía no cambia cómo pelea (T-201 y T-202).
    /// Va junto a RivalBasico y fuera de su lista de controlados, para seguir oyendo cuando el rival cae en KO.
    /// </summary>
    [RequireComponent(typeof(RivalBasico))]
    [RequireComponent(typeof(PeleadorMecha))]
    public class AdaptacionRival : MonoBehaviour, ILecturaAdaptacion
    {
        private const int Tipos = 3;
        private const float SegundosPorMinuto = 60f;
        private const float PorcentajeTotal = 100f;

        [Tooltip("Umbrales y números de la adaptación de este rival (DatosAdaptacionRival_Base o _Avanzado).")]
        [SerializeField] private DatosAdaptacionRival datos;

        private RivalBasico rival;
        private PeleadorMecha peleador;
        private MemoriaRival memoriaPropia;
        private MemoriaRivales memoriaEnlazada;
        private readonly int[] niveles = new int[Tipos];
        private PerfilEstilo perfil;
        private bool armado;
        private bool observando;
        private bool golpeAMi;
        private GameObject objetivoConocido;
        private GolpeMecha golpeObjetivo;
        private IPeleador peleadorObjetivo;
        private bool suscrito;

        /// <summary>Los números de la adaptación de este rival (T-201 lee de aquí la GUARDIA).</summary>
        public DatosAdaptacionRival Datos => datos;

        /// <summary>Nombre del asset DatosRival: la llave de su memoria.</summary>
        public string IdRival => rival != null && rival.Datos != null ? rival.Datos.name : name;

        /// <summary>Lo que ha visto en la pelea en curso; null fuera de pelea.</summary>
        public PerfilEstilo PerfilActual => armado ? perfil : null;

        public string Nombre => peleador != null ? peleador.Nombre : name;
        public Transform Raiz => transform;

        public int Activas
        {
            get
            {
                int activas = 0;
                for (int i = 0; i < Tipos; i++)
                {
                    if (niveles[i] > 0)
                    {
                        activas++;
                    }
                }
                return activas;
            }
        }

        public event Action<TipoAdaptacion> Usada;
        public event Action<IReadOnlyList<CambioAdaptacion>> Aprendio;
        public event Action NivelesCambiados;

        public int Nivel(TipoAdaptacion tipo)
        {
            int i = (int)tipo;
            return i >= 0 && i < Tipos ? niveles[i] : 0;
        }

        /// <summary>Avisa que el rival usó una adaptación (lo llamarán T-201 y T-202).</summary>
        public void AvisarUso(TipoAdaptacion tipo)
        {
            Usada?.Invoke(tipo);
        }

        /// <summary>Recalcula los niveles desde la memoria (para la herramienta de T-204).</summary>
        public void Releer()
        {
            Recalcular();
        }

        private void Awake()
        {
            rival = GetComponent<RivalBasico>();
            peleador = GetComponent<PeleadorMecha>();
            if (datos == null || datos.UmbralesNivel == null || datos.UmbralesNivel.Count < DatosAdaptacionRival.Niveles)
            {
                Debug.LogError("[IA] Falta el DatosAdaptacionRival en AdaptacionRival de " + name, this);
                enabled = false;
                return;
            }
            // En Awake y se quita en OnDestroy, no en OnEnable/OnDisable: sigue visible aunque el rival se quede sin control.
            Registro<ILecturaAdaptacion>.Agregar(this);
        }

        private void OnDestroy()
        {
            Registro<ILecturaAdaptacion>.Quitar(this);
        }

        private void OnEnable()
        {
            peleador.PeleaPreparada += AlPrepararPelea;
            EventosPelea.PeleaIniciada += AlIniciarPelea;
            EventosPelea.PeleaTerminada += AlTerminarPelea;
            EventosPelea.ImpactoRecibido += AlRecibirImpacto;
            Registro<MemoriaRivales>.Agregado += AlCambiarMemorias;
            Registro<MemoriaRivales>.Quitado += AlCambiarMemorias;
            suscrito = true;
            Enlazar();
        }

        private void OnDisable()
        {
            if (suscrito)
            {
                peleador.PeleaPreparada -= AlPrepararPelea;
                EventosPelea.PeleaIniciada -= AlIniciarPelea;
                EventosPelea.PeleaTerminada -= AlTerminarPelea;
                EventosPelea.ImpactoRecibido -= AlRecibirImpacto;
                Registro<MemoriaRivales>.Agregado -= AlCambiarMemorias;
                Registro<MemoriaRivales>.Quitado -= AlCambiarMemorias;
                suscrito = false;
            }
            Desenlazar();
            SoltarObjetivo();
        }

        private void Start()
        {
            Recalcular();
        }

        // Solo compara la referencia: el GetComponent ocurre una vez por cambio de objetivo.
        private void Update()
        {
            if (rival.Objetivo != objetivoConocido)
            {
                ResolverObjetivo();
            }
        }

        // ---- Memoria ----

        private MemoriaRival Memoria()
        {
            MemoriaRivales servicio = MemoriaRivales.Actual;
            if (servicio != null)
            {
                return servicio.Buscar(IdRival);
            }
            return memoriaPropia;
        }

        private MemoriaRival MemoriaParaEscribir()
        {
            MemoriaRivales servicio = MemoriaRivales.Actual;
            if (servicio != null)
            {
                return servicio.Obtener(IdRival);
            }
            if (memoriaPropia == null)
            {
                memoriaPropia = new MemoriaRival { idRival = IdRival };
            }
            return memoriaPropia;
        }

        private void AlCambiarMemorias(MemoriaRivales servicio)
        {
            Enlazar();
            Recalcular();
        }

        private void Enlazar()
        {
            MemoriaRivales actual = MemoriaRivales.Actual;
            if (actual == memoriaEnlazada)
            {
                return;
            }
            Desenlazar();
            memoriaEnlazada = actual;
            if (memoriaEnlazada != null)
            {
                memoriaEnlazada.Cambio += Recalcular;
            }
        }

        private void Desenlazar()
        {
            if (memoriaEnlazada != null)
            {
                memoriaEnlazada.Cambio -= Recalcular;
                memoriaEnlazada = null;
            }
        }

        // Nunca a media pelea: los niveles quedan fijos desde que se prepara hasta que termina.
        private void Recalcular()
        {
            if (armado || datos == null)
            {
                return;
            }
            MemoriaRival memoria = Memoria();
            bool cambio = false;
            for (int i = 0; i < Tipos; i++)
            {
                int nuevo = ReglaAdaptacion.Nivel(memoria, (TipoAdaptacion)i, datos);
                if (nuevo != niveles[i])
                {
                    niveles[i] = nuevo;
                    cambio = true;
                }
            }
            if (cambio)
            {
                NivelesCambiados?.Invoke();
            }
        }

        // ---- Objetivo ----

        private void ResolverObjetivo()
        {
            SoltarObjetivo();
            objetivoConocido = rival.Objetivo;
            if (objetivoConocido == null)
            {
                return;
            }
            peleadorObjetivo = objetivoConocido.GetComponent<IPeleador>();
            golpeObjetivo = objetivoConocido.GetComponent<GolpeMecha>();
            if (golpeObjetivo != null)
            {
                golpeObjetivo.Impacto += AlLanzarGolpe;
                golpeObjetivo.Conecto += AlConectarGolpe;
            }
        }

        private void SoltarObjetivo()
        {
            if (golpeObjetivo != null)
            {
                golpeObjetivo.Impacto -= AlLanzarGolpe;
                golpeObjetivo.Conecto -= AlConectarGolpe;
            }
            golpeObjetivo = null;
            peleadorObjetivo = null;
            objetivoConocido = null;
        }

        // ---- Pelea ----

        private void AlPrepararPelea()
        {
            armado = false;
            Recalcular();
            Armar();
        }

        private void Armar()
        {
            perfil = new PerfilEstilo();
            armado = true;
            observando = false;
            golpeAMi = false;
        }

        private void AlIniciarPelea()
        {
            if (rival.Objetivo != objetivoConocido)
            {
                ResolverObjetivo();
            }
            // Un rival recién creado por el flujo nunca recibe PeleaPreparada; el que pelea es el que tiene el control puesto.
            if (!armado && rival.enabled && rival.Objetivo != null)
            {
                Armar();
            }
            observando = armado;
        }

        private void AlTerminarPelea(ResultadoPelea resultado)
        {
            bool habiaObservado = observando;
            PerfilEstilo visto = perfil;
            observando = false;
            armado = false;
            perfil = null;
            if (!habiaObservado)
            {
                return;
            }

            int[] antes = (int[])niveles.Clone();
            bool[] vistos = new bool[Tipos];
            for (int i = 0; i < Tipos; i++)
            {
                vistos[i] = ReglaEstilo.Vio((TipoAdaptacion)i, visto, resultado.DuracionSegundos, datos);
            }
            List<CambioAdaptacion> cambios = ReglaAdaptacion.Aprender(MemoriaParaEscribir(), vistos,
                resultado.Ganador == LadoPelea.Jugador, datos);
            Recalcular();
            MemoriaRivales servicio = MemoriaRivales.Actual;
            if (servicio != null)
            {
                servicio.AvisarCambio();
            }
            RegistrarEnConsola(visto, resultado.DuracionSegundos, antes, cambios);
            Aprendio?.Invoke(cambios);
        }

        private void RegistrarEnConsola(PerfilEstilo visto, float segundos, int[] antes, List<CambioAdaptacion> cambios)
        {
            float porMinuto = segundos > 0f ? visto.GolpesLanzados * SegundosPorMinuto / segundos : 0f;
            float cubierto = visto.GolpesRival > 0 ? PorcentajeTotal * visto.GolpesRivalCubiertos / visto.GolpesRival : 0f;
            float castigando = visto.GolpesConectados > 0 ? PorcentajeTotal * visto.ConectadosCastigando / visto.GolpesConectados : 0f;
            Debug.Log("[IA] " + Nombre + " observó: lanzados " + visto.GolpesLanzados + ", " + porMinuto.ToString("0.0")
                + " golpes/min, " + cubierto.ToString("0") + "% cubierto, " + castigando.ToString("0") + "% castigando; niveles antes "
                + Texto(antes) + " despues " + Texto(niveles));
            foreach (CambioAdaptacion cambio in cambios)
            {
                string nombre = cambio.Tipo.ToString().ToUpperInvariant();
                Debug.Log(cambio.Subio
                    ? "[IA] " + Nombre + " aprendió: " + nombre + " " + Romano(cambio.NivelDespues)
                    : "[IA] " + Nombre + " olvidó: " + nombre + " " + Romano(cambio.NivelAntes));
            }
        }

        private static string Texto(int[] valores)
        {
            return "G" + valores[(int)TipoAdaptacion.Guardia] + " F" + valores[(int)TipoAdaptacion.Fintas]
                + " R" + valores[(int)TipoAdaptacion.Retirada];
        }

        private static string Romano(int nivel) => nivel >= 2 ? "II" : "I";

        // ---- Qué cuenta ----

        private void AlLanzarGolpe()
        {
            golpeAMi = false;
            if (observando)
            {
                perfil.RegistrarGolpeLanzado();
            }
        }

        // Conecto llega después del impacto recibido (que marca golpeAMi) y antes de Impacto.
        private void AlConectarGolpe(ResultadoDanio resultado)
        {
            if (observando && golpeAMi)
            {
                perfil.RegistrarConectado(rival.Fase == FaseGolpe.Recuperando);
            }
            golpeAMi = false;
        }

        private void AlRecibirImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            if (!observando || objetivo == null || info.Tipo != TipoDanio.Golpe)
            {
                return;
            }
            if (ReferenceEquals(objetivo, peleador) && peleadorObjetivo != null && ReferenceEquals(info.Atacante, peleadorObjetivo))
            {
                golpeAMi = true;
            }
            else if (peleadorObjetivo != null && ReferenceEquals(objetivo, peleadorObjetivo) && ReferenceEquals(info.Atacante, peleador))
            {
                perfil.RegistrarGolpeRival(resultado.Bloqueo);
            }
        }
    }
}
