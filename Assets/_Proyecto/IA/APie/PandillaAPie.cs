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

        private ConfrontacionGrupo confrontacion;
        private float proximaResolucionConfrontacion;

        /// <summary>
        /// T-1651 (T26): la confrontación con advertencias de este grupo si es de maleantes de calle (SKULLS o SNAKES de calle) y existe su
        /// ConfigConfrontacion en Resources; null en cualquier otro caso (guion, jefes, policía, Nova): ahí queda la IA de siempre.
        /// </summary>
        public ConfrontacionGrupo Confrontacion
        {
            get
            {
                if (confrontacion != null || pandilleros == null || Time.time < proximaResolucionConfrontacion)
                {
                    return confrontacion;
                }
                proximaResolucionConfrontacion = Time.time + 2f; // EsDeGuion se pone después del Awake: se reintenta un rato
                if (GetComponent<GrupoAcoso>() != null)
                {
                    return null; // los grupos de acoso (T12/T-1633) tienen su propia lógica y prestan pandilleros al bullying
                }
                bool calle = false;
                foreach (PandilleroAPie p in pandilleros)
                {
                    if (p != null && p.EsMaleanteDeCalle)
                    {
                        calle = true;
                        break;
                    }
                }
                if (!calle)
                {
                    return null;
                }
                string recurso = idPandilla == IdPandilla.Viboras ? ConfigConfrontacion.RecursoSnakes : ConfigConfrontacion.RecursoSkulls;
                ConfigConfrontacion config = Resources.Load<ConfigConfrontacion>(recurso);
                if (config != null)
                {
                    confrontacion = new ConfrontacionGrupo(this, config);
                }
                return confrontacion;
            }
        }

        /// <summary>T-1659: depuración (Play, por reflexión): lista todos los grupos con confrontación, su estado y quién es el que se acerca.</summary>
        public static string DepurarConfrontacion() => ConfrontacionGrupo.DepurarConfrontacion();

        /// <summary>Depuración (Play, por eval): el estado de la confrontación del grupo con confrontación más cercano al jugador (o a la cámara), o "sin grupos".</summary>
        public static string EstadoConfrontacionMasCercano()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Vector3 origen = jugador != null && jugador.Raiz != null ? jugador.Raiz.position : (Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            PandillaAPie mejor = null;
            float menor = float.MaxValue;
            foreach (PandillaAPie g in todas)
            {
                if (g == null || g.Confrontacion == null || g.Presentes == 0)
                {
                    continue;
                }
                float d = (g.centro - origen).sqrMagnitude;
                if (d < menor)
                {
                    menor = d;
                    mejor = g;
                }
            }
            return mejor != null ? mejor.Confrontacion.ToString() : "sin grupos";
        }

        /// <summary>S2: con true el territorio no enciende la alerta por sí solo (el <see cref="GrupoAcoso"/> decide cuándo te ven).</summary>
        public bool TerritorioSuprimido { get; set; }

        /// <summary>
        /// T-713: grupo de una misión. Salen todos los del grupo a la vez (de día o de noche), no se retiran al amanecer, y los que caen no vuelven a
        /// aparecer (ni por esperar, ni por irte y regresar). Hay que ponerlo antes de prender el grupo.
        /// </summary>
        public bool ModoMision { get; set; }

        /// <summary>T-1528: true si el grupo es de un guion de misión (prólogo, jefe SNAKES): sus pandilleros no provocan antes de atacar.</summary>
        public bool EsDeGuion { get; set; }

        /// <summary>T-713: cuántos de este grupo ya cayeron en modo misión (no vuelven).</summary>
        public int Vencidos => vencidos.Count;

        /// <summary>T-713: True si en modo misión ya cayeron todos los del grupo.</summary>
        public bool TodosVencidos => pandilleros != null && pandilleros.Length > 0 && vencidos.Count >= pandilleros.Length;

        private readonly System.Collections.Generic.HashSet<PandilleroAPie> vencidos = new System.Collections.Generic.HashSet<PandilleroAPie>();

        /// <summary>T-1528: el jugador respondió a la provocación: todos los de este grupo atacan. Con <paramref name="completo"/> (pruebas, guion) atacan ya a todo su poder y no se calman.</summary>
        public void HacerHostiles(bool completo = false)
        {
            if (pandilleros == null)
            {
                return;
            }
            foreach (PandilleroAPie p in pandilleros)
            {
                if (p != null)
                {
                    p.PonerHostil(completo); // también los que aún no salieron (están apagados)
                }
            }
        }

        /// <summary>Cuántos pandilleros hay en pie ahora.</summary>
        public int Presentes
        {
            get
            {
                int cuenta = 0;
                foreach (PandilleroAPie p in pandilleros)
                {
                    if (p != null && p.gameObject.activeSelf && !p.EstaKO)
                    {
                        cuenta++;
                    }
                }
                return cuenta;
            }
        }

        private static readonly System.Collections.Generic.List<PandillaAPie> todas = new System.Collections.Generic.List<PandillaAPie>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarRegistro() => todas.Clear();

        // T-1495: pandilleros en pie de todos los grupos dentro de ReglaTopePandilleros.RadioZona de un punto.
        private static int VivosCerca(Vector3 punto)
        {
            int cuenta = 0;
            float r2 = ReglaTopePandilleros.RadioZona * ReglaTopePandilleros.RadioZona;
            foreach (PandillaAPie g in todas)
            {
                if (g == null || g.pandilleros == null)
                {
                    continue;
                }
                foreach (PandilleroAPie p in g.pandilleros)
                {
                    if (p != null && p.gameObject.activeSelf && !p.EstaKO)
                    {
                        Vector3 d = p.transform.position - punto;
                        d.y = 0f;
                        if (d.sqrMagnitude <= r2)
                        {
                            cuenta++;
                        }
                    }
                }
            }
            return cuenta;
        }

        private void Awake()
        {
            pandilleros = GetComponentsInChildren<PandilleroAPie>(true);
            todas.Add(this);
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
            todas.Remove(this);
            confrontacion?.Olvidar();
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
                if (p != null && p.gameObject.activeSelf && !p.EstaKO)
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
                if (p == null || p == origen || !p.gameObject.activeSelf || p.EstaKO)
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
                if (p == null || !p.gameObject.activeSelf || p.EstaKO)
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
            // T-1651: los maleantes de calle te detectan a la distancia de su ConfigConfrontacion (no a todo el territorio) y te sueltan al alejarte más que la distancia de dejar de seguir.
            float radioDeteccion = datos.RadioTerritorio;
            float margenPersecucion = datos.MargenPersecucion;
            ConfrontacionGrupo conf = Confrontacion;
            if (conf != null)
            {
                radioDeteccion = conf.Config.DistanciaDeteccion;
                margenPersecucion = Mathf.Max(0f, conf.Config.DistanciaDejarDeSeguir - radioDeteccion);
            }
            if (!TerritorioSuprimido)
            {
                bool alertaAntes = alerta;
                bool valido = hayJugador && jugador.Modo == ModoJugador.APie && !jugadorEnInterior;
                if (!valido)
                {
                    alerta = false;
                }
                else if (!alerta && distancia <= radioDeteccion)
                {
                    alerta = true;
                }
                else if (alerta && distancia > radioDeteccion + margenPersecucion && Time.time >= forzadaHasta)
                {
                    alerta = false;
                }
                foreach (PandilleroAPie p in pandilleros)
                {
                    if (p != null)
                    {
                        p.Alerta = alerta;
                    }
                }
                if (alerta && !alertaAntes)
                {
                    // T-423: al ver al jugador, el primero en pie grita y avisa a los demás.
                    Avisar(PrimeroEnPie(), "¡Ahí está!", false);
                }
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
            if (!ModoMision && Time.time - vacioDesde >= datos.SegundosReaparicion && distancia > datos.DistanciaReaparicion)
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
            Confrontacion?.Avanzar(Time.deltaTime, pandilleros);
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
                retiroParcialPendiente = !ModoMision && !reloj.EsDeNoche;
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
            else if (ModoMision)
            {
                // T-713: un grupo de misión no se retira al amanecer.
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
            if (ModoMision || relojActual == null || relojActual.EsDeNoche)
            {
                return pandilleros.Length;
            }
            return Mathf.Clamp(Mathf.RoundToInt(pandilleros.Length * datos.FraccionDeDia), 1, pandilleros.Length);
        }

        private void Aparecer(int cuantos)
        {
            // T-1495: tope de pandilleros vivos por punto y por zona (las misiones piden un número fijo y quedan fuera).
            int cupo = ModoMision ? int.MaxValue : ReglaTopePandilleros.Cupo(Presentes, VivosCerca(centro));
            for (int i = 0; i < pandilleros.Length && i < cuantos; i++)
            {
                PandilleroAPie p = pandilleros[i];
                // T-713: en modo misión el que ya cayó no vuelve.
                if (ModoMision && vencidos.Contains(p))
                {
                    continue;
                }
                // Un pandillero que sigue en pie de la noche anterior no se reinicia a media pelea.
                if (p.gameObject.activeSelf && !p.EstaKO)
                {
                    continue;
                }
                if (cupo <= 0)
                {
                    break;
                }
                cupo--;
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
            if (ModoMision)
            {
                vencidos.Add(vencido);
            }
            // T-686: el dinero ya no se abona solo; cada pandillero lo suelta al caer y el jugador lo recoge.
            // T-729: si lo venció la policía, no es un delito del jugador: no suma búsqueda.
            if (datos.CalorAlVencer > 0f && !vencido.CayoPorLaPolicia)
            {
                Registro<ICalor>.Actual?.Sumar(datos.CalorAlVencer, MotivoCalor.Pelea);
            }
            Registro<ITerritorio>.Actual?.RegistrarVencido(vencido.transform.position, idPandilla, false);
            Debug.Log("[IA] Pandillero vencido: suelta " + datos.Dinero);
        }
    }
}
