using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// Un grupo de SKULLS (<see cref="PandillaAPie"/> en el mismo objeto) que molesta a un <see cref="CivilAcosado"/> (S2, 14-nov).
    /// Acosando: en círculo alrededor del civil, mirándolo, con burlas y empujones; no persiguen a nadie. Te ven (cono, línea de vista,
    /// inclinación) o te oyen de cerca: alerta y pelean con la IA normal. Si les pegas sin que te vean: sorpresa, buscan un rato y luego alerta.
    /// Al vencerlos a todos el civil huye y el grupo suelta un bono donde estaba. Lo decide <see cref="ReglaAcoso"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(PandillaAPie))]
    public class GrupoAcoso : MonoBehaviour
    {
        [SerializeField] private DatosAcoso datos;
        [Tooltip("El civil al que molestan (hijo del grupo).")]
        [SerializeField] private CivilAcosado civil;

        private PandillaAPie grupo;
        private PandilleroAPie[] pandilleros;
        private ReglaAcoso regla;
        private Func<float> azar = () => UnityEngine.Random.value;
        private float proximaDeteccion;
        private bool detectado;
        private bool danioPendiente;
        private bool danioDesdeArriba;
        private Vector3 centroCivil;

        public ReglaAcoso.Fase Fase => regla != null ? regla.Actual : ReglaAcoso.Fase.Acosando;
        public CivilAcosado Civil => civil;
        public DatosAcoso Datos => datos;

        /// <summary>Para pruebas: cambia el azar (antes del primer Update).</summary>
        public void InyectarAzar(Func<float> fuente)
        {
            azar = fuente ?? azar;
            Crear();
        }

        private void Awake()
        {
            grupo = GetComponent<PandillaAPie>();
            pandilleros = GetComponentsInChildren<PandilleroAPie>(true);
            if (datos == null || civil == null || pandilleros.Length == 0)
            {
                Debug.LogError("[IA] Faltan referencias en GrupoAcoso de " + name, this);
                enabled = false;
                return;
            }
            // Siempre están (de día y de noche) y no se retiran: el orden de ejecución deja esto antes del OnEnable de la pandilla.
            grupo.ModoMision = true;
            grupo.TerritorioSuprimido = true;
            foreach (PandilleroAPie p in pandilleros)
            {
                p.Acosando = true;
                p.DanioRecibido += AlRecibirDanio;
            }
            centroCivil = civil.transform.position;
            Crear();
        }

        private void Crear()
        {
            if (datos == null)
            {
                return;
            }
            regla = new ReglaAcoso(datos.PeriodoGestoMinimo, datos.PeriodoGestoMaximo, datos.ProbabilidadEmpujon, datos.ProbabilidadCaida,
                datos.RetrasoEmboscada, datos.RetrasoEmboscadaDesdeArriba, azar);
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
                    p.DanioRecibido -= AlRecibirDanio;
                }
            }
        }

        private void AlRecibirDanio(PandilleroAPie quien, InfoDanio info)
        {
            if (regla == null || regla.Actual != ReglaAcoso.Fase.Acosando)
            {
                return;
            }
            danioPendiente = true;
            IJugador jugador = Registro<IJugador>.Actual;
            danioDesdeArriba = jugador != null && jugador.Raiz != null
                && ReglaAcoso.EstaDesdeArriba(jugador.Raiz.position.y, quien.transform.position.y, datos.AlturaDesdeArriba);
        }

        private void Update()
        {
            if (regla == null || grupo == null || civil == null)
            {
                return;
            }
            float dt = Time.deltaTime;
            bool mirando = regla.Actual == ReglaAcoso.Fase.Acosando || regla.Actual == ReglaAcoso.Fase.Sorpresa;
            if (mirando && Time.time >= proximaDeteccion)
            {
                proximaDeteccion = Time.time + datos.PeriodoDeteccion;
                // Mientras buscan (sorpresa) solo te delata el oído: si no, ver al jugador anularía el retraso de la emboscada.
                detectado = JugadorDetectado(regla.Actual == ReglaAcoso.Fase.Sorpresa);
            }
            ReglaAcoso.Entrada entrada = new ReglaAcoso.Entrada
            {
                JugadorDetectado = detectado,
                DanioRecibido = danioPendiente,
                JugadorDesdeArriba = danioDesdeArriba,
                TodosVencidos = grupo.TodosVencidos
            };
            danioPendiente = false;
            ReglaAcoso.Salida salida = regla.Avanzar(dt, entrada);

            if (salida.EntroSorpresa)
            {
                PandilleroAPie voz = PrimeroEnPie();
                if (voz != null)
                {
                    voz.Gritar(datos.GritoSorpresa);
                }
            }
            if (salida.EntroAlerta)
            {
                EntrarEnAlerta();
            }
            if (salida.EntroVencidos)
            {
                Vencer();
            }
            switch (regla.Actual)
            {
                case ReglaAcoso.Fase.Acosando:
                    MirarAlCivil(dt);
                    HacerGesto(salida);
                    break;
                case ReglaAcoso.Fase.Sorpresa:
                    Buscar(dt);
                    break;
            }
        }

        // Otros sistemas (o las pruebas) pueden destruir a un pandillero: se cuenta como que ya no está.
        private static bool EnPie(PandilleroAPie p) => p != null && p.gameObject.activeSelf && !p.EstaKO;

        private PandilleroAPie PrimeroEnPie()
        {
            foreach (PandilleroAPie p in pandilleros)
            {
                if (EnPie(p))
                {
                    return p;
                }
            }
            return null;
        }

        private bool JugadorDetectado(bool soloOido)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null || jugador.Modo != ModoJugador.APie)
            {
                return false;
            }
            Vector3 objetivo = jugador.Raiz.position + Vector3.up * datos.AlturaPechoJugador;
            foreach (PandilleroAPie p in pandilleros)
            {
                if (!EnPie(p))
                {
                    continue;
                }
                Vector3 ojos = p.transform.position + Vector3.up * datos.AlturaOjos;
                ReglaAcoso.Vista vista = ReglaAcoso.Evaluar(ojos, p.transform.forward, objetivo, datos.DistanciaVista, datos.AnguloVista,
                    datos.InclinacionMaximaVista, datos.DistanciaOido);
                if (vista == ReglaAcoso.Vista.Oido)
                {
                    return true;
                }
                if (!soloOido && vista == ReglaAcoso.Vista.Ojos && HayLinea(ojos, objetivo, jugador.Raiz))
                {
                    return true;
                }
            }
            return false;
        }

        // Linecast pecho a pecho sin triggers: tapan los muros, no el propio jugador ni el grupo.
        private bool HayLinea(Vector3 ojos, Vector3 objetivo, Transform raizJugador)
        {
            RaycastHit[] golpes = Physics.RaycastAll(ojos, objetivo - ojos, (objetivo - ojos).magnitude, ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit g in golpes)
            {
                if (g.transform.IsChildOf(raizJugador) || g.transform.IsChildOf(transform))
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        private void MirarAlCivil(float dt)
        {
            foreach (PandilleroAPie p in pandilleros)
            {
                if (!EnPie(p))
                {
                    continue;
                }
                Vector3 hacia = civil.transform.position - p.transform.position;
                hacia.y = 0f;
                if (hacia.sqrMagnitude > 1e-6f)
                {
                    p.transform.rotation = Quaternion.RotateTowards(p.transform.rotation, Quaternion.LookRotation(hacia.normalized, Vector3.up), datos.VelocidadGiro * dt);
                }
            }
        }

        private void HacerGesto(ReglaAcoso.Salida salida)
        {
            if (salida.Gesto == ReglaAcoso.Gesto.Ninguno || civil.Huyendo || civil.Terminado)
            {
                return;
            }
            if (salida.Gesto == ReglaAcoso.Gesto.Burla)
            {
                PandilleroAPie quien = ElegirAlAzar();
                if (quien != null && quien.Visual != null)
                {
                    quien.Visual.Burlar();
                }
                return;
            }
            // Empujón: el que está más cerca del civil lo golpea (no hay clip de empujón: usa Golpear) y el civil retrocede.
            PandilleroAPie cercano = null;
            float menor = float.MaxValue;
            foreach (PandilleroAPie p in pandilleros)
            {
                if (!EnPie(p))
                {
                    continue;
                }
                float d = (p.transform.position - civil.transform.position).sqrMagnitude;
                if (d < menor)
                {
                    menor = d;
                    cercano = p;
                }
            }
            if (cercano == null)
            {
                return;
            }
            if (cercano.Visual != null)
            {
                cercano.Visual.Golpear(false);
            }
            cercano.Gritar(datos.GritoEmpujon);
            civil.Mirar(cercano.transform.position);
            civil.Empujar(civil.transform.position - cercano.transform.position, salida.EmpujonTira);
        }

        private PandilleroAPie ElegirAlAzar()
        {
            int vivos = 0;
            foreach (PandilleroAPie p in pandilleros)
            {
                if (EnPie(p))
                {
                    vivos++;
                }
            }
            if (vivos == 0)
            {
                return null;
            }
            int elegido = Mathf.Min(vivos - 1, Mathf.FloorToInt(azar() * vivos));
            foreach (PandilleroAPie p in pandilleros)
            {
                if (EnPie(p) && elegido-- == 0)
                {
                    return p;
                }
            }
            return null;
        }

        // Sorpresa: giran sobre sí mismos mirando alrededor (unos para un lado, otros para el otro).
        private void Buscar(float dt)
        {
            for (int i = 0; i < pandilleros.Length; i++)
            {
                PandilleroAPie p = pandilleros[i];
                if (!EnPie(p))
                {
                    continue;
                }
                float signo = i % 2 == 0 ? 1f : -1f;
                p.transform.Rotate(0f, signo * datos.GiroBusqueda * dt, 0f);
            }
        }

        private void EntrarEnAlerta()
        {
            grupo.TerritorioSuprimido = false;
            foreach (PandilleroAPie p in pandilleros)
            {
                p.Acosando = false;
            }
            PandilleroAPie origen = PrimeroEnPie();
            if (origen != null)
            {
                grupo.Avisar(origen, datos.GritoAlerta, true);
            }
            foreach (PandilleroAPie p in pandilleros)
            {
                if (EnPie(p))
                {
                    p.Alerta = true;
                }
            }
            Debug.Log("[IA] Acoso: el grupo de " + name + " pasa a alerta.");
        }

        private void Vencer()
        {
            grupo.TerritorioSuprimido = false;
            Vector3 donde = civil != null ? civil.transform.position : centroCivil;
            if (datos.BonoCivil > 0)
            {
                EventosBotin.Publicar(new InfoBotin(donde, datos.BonoCivil, ArmaBotin.Ninguna));
            }
            IJugador jugador = Registro<IJugador>.Actual;
            Vector3 desde = jugador != null && jugador.Raiz != null ? jugador.Raiz.position : donde - civil.transform.forward;
            civil.Huir(desde, datos.GritoGracias);
            Debug.Log("[IA] Acoso: grupo de " + name + " vencido; el civil huye y deja un bono de " + datos.BonoCivil);
        }
    }
}
