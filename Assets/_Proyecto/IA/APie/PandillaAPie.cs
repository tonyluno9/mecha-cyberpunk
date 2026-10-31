using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// Un grupo de pandilleros a pie alrededor de su punto (T-195, D-tony-169). Solo de noche: aparecen al anochecer y se retiran al
    /// amanecer cuando nadie pelea. Reparte los turnos de ataque (como máximo N a la vez). Cada vencido suelta su dinero y vuelve la
    /// noche siguiente. Los pandilleros son hijos del prefab; sus puntos son donde están al arrancar.
    /// </summary>
    public class PandillaAPie : MonoBehaviour
    {
        [SerializeField] private DatosPandilleroAPie datos;
        [Tooltip("A qué pandilla pertenece el grupo.")]
        [SerializeField] private IdPandilla idPandilla = IdPandilla.Ninguna;

        private PandilleroAPie[] pandilleros;
        private TurnosPandillaAPie turnos;
        private IRelojMundo reloj;
        private bool retiroPendiente;
        private bool retiroParcialPendiente;
        private Vector3 centro;
        private bool alerta;
        private bool jugadorEnInterior;
        private float vacioDesde = -1f;
        private float forzadaHasta = -1f;

        public DatosPandilleroAPie Datos => datos;
        public IdPandilla Pandilla => idPandilla;
        /// <summary>Centro del territorio: el promedio de los puntos de los pandilleros.</summary>
        public Vector3 Centro => centro;
        public float RadioTerritorio => datos != null ? datos.RadioTerritorio : 0f;
        /// <summary>True mientras el grupo ataca al jugador por estar en su territorio.</summary>
        public bool EnAlerta => alerta;
        public TurnosPandillaAPie Turnos => turnos;
        public PandilleroAPie[] Pandilleros => pandilleros;

        /// <summary>Cuántos pandilleros hay en pie ahora.</summary>
        public int Presentes
        {
            get
            {
                int cuenta = 0;
                foreach (PandilleroAPie p in pandilleros)
                {
                    if (p.gameObject.activeSelf && !p.EstaKO)
                    {
                        cuenta++;
                    }
                }
                return cuenta;
            }
        }

        private void Awake()
        {
            pandilleros = GetComponentsInChildren<PandilleroAPie>(true);
            if (datos == null || pandilleros.Length == 0)
            {
                Debug.LogError("[IA] Faltan referencias en PandillaAPie de " + name, this);
                enabled = false;
                return;
            }
            turnos = new TurnosPandillaAPie(datos.MaxAtacando);
            foreach (PandilleroAPie p in pandilleros)
            {
                centro += p.transform.position / pandilleros.Length;
                p.Configurar(p.transform.position, turnos);
                p.Grupo = this;
                p.Vencido += AlVencerUno;
                p.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (!enabled)
            {
                return;
            }
            EventosLugares.JugadorEntro += AlEntrarLugar;
            EventosLugares.JugadorSalio += AlSalirLugar;
            if (datos.Siempre)
            {
                // T-447: aunque esté siempre, de día quedan menos (según el reloj, si lo hay) y de noche salen todos.
                Aparecer(CuantosAhora(Registro<IRelojMundo>.Actual));
            }
            Registro<IRelojMundo>.Agregado += AlAgregarReloj;
            Registro<IRelojMundo>.Quitado += AlQuitarReloj;
            Vincular(Registro<IRelojMundo>.Actual);
        }

        private void OnDisable()
        {
            EventosLugares.JugadorEntro -= AlEntrarLugar;
            EventosLugares.JugadorSalio -= AlSalirLugar;
            Registro<IRelojMundo>.Agregado -= AlAgregarReloj;
            Registro<IRelojMundo>.Quitado -= AlQuitarReloj;
            Desvincular();
        }

        private void OnDestroy()
        {
            if (pandilleros == null)
            {
                return;
            }
            foreach (PandilleroAPie p in pandilleros)
            {
                if (p != null)
                {
                    p.Vencido -= AlVencerUno;
                }
            }
        }

        private PandilleroAPie PrimeroEnPie()
        {
            foreach (PandilleroAPie p in pandilleros)
            {
                if (p.gameObject.activeSelf && !p.EstaKO)
                {
                    return p;
                }
            }
            return null;
        }

        /// <summary>
        /// T-423: <paramref name="origen"/> grita y los del grupo dentro del radio de refuerzos pasan a alerta. Con <paramref name="forzar"/>
        /// (recibió daño) la alerta se mantiene unos segundos aunque el jugador esté fuera del territorio.
        /// </summary>
        public void Avisar(PandilleroAPie origen, string grito, bool forzar)
        {
            if (origen == null)
            {
                return;
            }
            if (forzar)
            {
                forzadaHasta = Time.time + datos.SegundosRefuerzo;
                alerta = true;
            }
            origen.Gritar(grito);
            float radio2 = datos.RadioRefuerzos * datos.RadioRefuerzos;
            foreach (PandilleroAPie p in pandilleros)
            {
                if (p == origen || !p.gameObject.activeSelf || p.EstaKO)
                {
                    continue;
                }
                if ((p.transform.position - origen.transform.position).sqrMagnitude <= radio2)
                {
                    p.Alerta = true;
                }
            }
        }

        /// <summary>T-423: puesto de <paramref name="quien"/> entre los que siguen en pie (para repartirse alrededor del jugador).</summary>
        public void Repartir(PandilleroAPie quien, out int indice, out int total)
        {
            indice = 0;
            total = 0;
            foreach (PandilleroAPie p in pandilleros)
            {
                if (!p.gameObject.activeSelf || p.EstaKO)
                {
                    continue;
                }
                if (p == quien)
                {
                    indice = total;
                }
                total++;
            }
        }

        private void AlEntrarLugar(IdLugar lugar) => jugadorEnInterior = lugar != IdLugar.Ninguno && lugar != IdLugar.Calle;

        private void AlSalirLugar(IdLugar lugar) => jugadorEnInterior = false;

        // Territorio (T-402): ataca si el jugador a pie entra al radio y lo sigue hasta radio + margen. Reaparece a los N s si quedó vacío.
        private void ActualizarTerritorio()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            bool hayJugador = jugador != null && jugador.Raiz != null;
            float distancia = float.MaxValue;
            if (hayJugador)
            {
                Vector3 delta = jugador.Raiz.position - centro;
                delta.y = 0f;
                distancia = delta.magnitude;
            }
            bool alertaAntes = alerta;
            bool valido = hayJugador && jugador.Modo == ModoJugador.APie && !jugadorEnInterior;
            if (!valido)
            {
                alerta = false;
            }
            else if (!alerta && distancia <= datos.RadioTerritorio)
            {
                alerta = true;
            }
            else if (alerta && distancia > datos.RadioTerritorio + datos.MargenPersecucion && Time.time >= forzadaHasta)
            {
                alerta = false;
            }
            foreach (PandilleroAPie p in pandilleros)
            {
                p.Alerta = alerta;
            }
            if (alerta && !alertaAntes)
            {
                // T-423: al ver al jugador, el primero en pie grita y avisa a los demás.
                Avisar(PrimeroEnPie(), "¡Ahí está!", false);
            }

            if (Presentes > 0)
            {
                vacioDesde = -1f;
                return;
            }
            if (vacioDesde < 0f)
            {
                vacioDesde = Time.time;
            }
            if (ZonaEsDelJugador())
            {
                return;
            }
            if (Time.time - vacioDesde >= datos.SegundosReaparicion && distancia > datos.DistanciaReaparicion)
            {
                vacioDesde = -1f;
                Aparecer(CuantosAhora(reloj));
            }
        }

        // T-404: una zona conquistada ya no reaparece su grupo.
        private bool ZonaEsDelJugador()
        {
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            return territorio != null && territorio.Dueno(territorio.ZonaEn(centro)) == IdPandilla.Jugador;
        }

        private void Update()
        {
            if (datos.UsaPistola)
            {
                ActualizarTerritorio();
            }
            if (retiroParcialPendiente)
            {
                RetirarSobrantesDeDia();
            }
            if (!retiroPendiente)
            {
                return;
            }
            foreach (PandilleroAPie p in pandilleros)
            {
                if (p.gameObject.activeSelf && p.EnPelea)
                {
                    return;
                }
            }
            retiroPendiente = false;
            Retirar();
        }

        private void AlAgregarReloj(IRelojMundo nuevo) => Vincular(nuevo);

        private void AlQuitarReloj(IRelojMundo quitado)
        {
            if (ReferenceEquals(quitado, reloj))
            {
                Desvincular();
                Vincular(Registro<IRelojMundo>.Actual);
            }
        }

        private void Vincular(IRelojMundo nuevo)
        {
            if (nuevo == null || ReferenceEquals(nuevo, reloj))
            {
                return;
            }
            Desvincular();
            reloj = nuevo;
            reloj.FranjaCambio += AlCambiarFranja;
            if (datos.Siempre)
            {
                Aparecer(CuantosAhora(reloj));
                retiroParcialPendiente = !reloj.EsDeNoche;
            }
            else if (reloj.EsDeNoche)
            {
                Aparecer(pandilleros.Length);
            }
        }

        private void Desvincular()
        {
            if (reloj != null)
            {
                reloj.FranjaCambio -= AlCambiarFranja;
                reloj = null;
            }
        }

        private void AlCambiarFranja(FranjaDia franja)
        {
            if (franja == FranjaDia.Noche)
            {
                retiroPendiente = false;
                retiroParcialPendiente = false;
                Aparecer(pandilleros.Length);
            }
            else if (datos.Siempre)
            {
                // De día se quedan algunos y el resto se retira cuando no esté peleando.
                retiroParcialPendiente = true;
            }
            else
            {
                retiroPendiente = true;
            }
        }

        // T-447: cuántos pandilleros del grupo hay ahora: todos de noche (o sin reloj) y, de día, la fracción de los datos (al menos uno).
        private int CuantosAhora(IRelojMundo relojActual)
        {
            if (relojActual == null || relojActual.EsDeNoche)
            {
                return pandilleros.Length;
            }
            return Mathf.Clamp(Mathf.RoundToInt(pandilleros.Length * datos.FraccionDeDia), 1, pandilleros.Length);
        }

        private void Aparecer(int cuantos)
        {
            for (int i = 0; i < pandilleros.Length && i < cuantos; i++)
            {
                PandilleroAPie p = pandilleros[i];
                // Un pandillero que sigue en pie de la noche anterior no se reinicia a media pelea.
                if (p.gameObject.activeSelf && !p.EstaKO)
                {
                    continue;
                }
                p.gameObject.SetActive(true);
                p.Reaparecer();
            }
        }

        // Retira a los que sobran de día (los que están más allá de la fracción) en cuanto no estén peleando.
        private void RetirarSobrantesDeDia()
        {
            int quedan = CuantosAhora(reloj);
            bool faltan = false;
            for (int i = quedan; i < pandilleros.Length; i++)
            {
                PandilleroAPie p = pandilleros[i];
                if (!p.gameObject.activeSelf)
                {
                    continue;
                }
                if (p.EnPelea)
                {
                    faltan = true;
                    continue;
                }
                turnos.Soltar(p);
                p.gameObject.SetActive(false);
            }
            retiroParcialPendiente = faltan;
        }

        private void Retirar()
        {
            foreach (PandilleroAPie p in pandilleros)
            {
                turnos.Soltar(p);
                p.gameObject.SetActive(false);
            }
        }

        private void AlVencerUno(PandilleroAPie vencido)
        {
            ICartera cartera = Registro<ICartera>.Actual;
            cartera?.Abonar(datos.Dinero, MotivoDinero.Otro);
            if (datos.CalorAlVencer > 0f)
            {
                Registro<ICalor>.Actual?.Sumar(datos.CalorAlVencer, MotivoCalor.Pelea);
            }
            Registro<ITerritorio>.Actual?.RegistrarVencido(vencido.transform.position, idPandilla, false);
            Debug.Log("[IA] Pandillero vencido: +" + datos.Dinero);
        }
    }
}
