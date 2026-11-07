using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Un dron de la policía (T-341): vuela sobre el jugador y se le acerca hasta la distancia de seguimiento, con su luz apuntando hacia abajo.
    /// Sin colliders: no estorba ni empuja nada. T-710: dispara de verdad: suena el disparo, deja un trazo finísimo y cortísimo (como T-676), si falla y la bala
    /// pasa cerca del jugador suena el silbido de la bala rozando, y si acierta hace daño real por <see cref="IRecibeDanio"/>.
    /// </summary>
    public class DronMundo : MonoBehaviour
    {
        private const float GradosPorSegundoGiro = 180f;

        /// <summary>Fuente del azar (0 a 1) de los disparos. Las pruebas la cambian.</summary>
        public static Func<float> Azar = () => UnityEngine.Random.value;

        private const float AlturaPecho = 1.2f;
        // T-710: trazo de la bala (hilo finísimo y cortísimo, como el del pandillero) y silbido de la bala que pasa cerca.
        public const string IdDisparo = "ARM_PIS_DISPARO";
        public const string IdRoce = "ARM_BALA_ROZA";
        private const float DuracionTrazo = 0.03f;
        private const float GrosorTrazo = 0.006f;
        private const float LargoTrazo = 6f;
        private const float BajoElDron = 0.2f;
        private static readonly Color ColorTrazo = new Color(1f, 0.9f, 0.6f, 0.9f);
        private LineRenderer trazo;
        private float finTrazo = -1f;

        private Light[] luces;
        private Color[] coloresLuces;
        private float proximoTiro;
        private float finAviso = -1f;
        private CalculoPersecucion calculo;
        private ConfigPersecucion config;
        private Func<Vector3?> posicionJugador;

        public void Preparar(CalculoPersecucion reglas, ConfigPersecucion numeros, Func<Vector3?> dondeEstaElJugador)
        {
            calculo = reglas;
            config = numeros;
            posicionJugador = dondeEstaElJugador;
        }

        private void Awake()
        {
            luces = GetComponentsInChildren<Light>(true);
            coloresLuces = new Color[luces.Length];
            for (int i = 0; i < luces.Length; i++)
            {
                coloresLuces[i] = luces[i].color;
            }
        }

        // Con calor suficiente avisa 0.3 s con su luz en rojo y dispara al jugador a pie; a quien va en carro no le dispara.
        private void Disparar()
        {
            ICalor calor = Registro<ICalor>.Actual;
            IJugador jugador = Registro<IJugador>.Actual;
            bool puede = calor != null && calor.Nivel >= config.NivelDisparoDron && jugador != null && jugador.Raiz != null
                && jugador.Modo == ModoJugador.APie;
            IRecibeDanio objetivo = puede ? jugador.Raiz.GetComponentInChildren<IRecibeDanio>() : null;
            IVida vida = puede ? jugador.Raiz.GetComponentInChildren<IVida>() : null;
            if (objetivo == null || (vida != null && vida.EstaKO))
            {
                finAviso = -1f;
                PonerLuces(false);
                return;
            }
            if (!HayLineaDeVista(jugador.Raiz))
            {
                // T-676: sin línea de vista (muro, barricada, techo) no avisa ni dispara.
                finAviso = -1f;
                PonerLuces(false);
                return;
            }
            if (finAviso < 0f)
            {
                if (Time.time >= proximoTiro)
                {
                    finAviso = Time.time + config.AvisoDron;
                    PonerLuces(true);
                }
                return;
            }
            if (Time.time < finAviso)
            {
                return;
            }
            finAviso = -1f;
            PonerLuces(false);
            proximoTiro = Time.time + config.SegundosEntreTirosDron;
            Disparos++;
            Vector3 origen = transform.position + Vector3.down * BajoElDron;
            Vector3 pecho = jugador.Raiz.position + Vector3.up * AlturaPecho;
            Vector3 direccion = (pecho - origen).normalized;
            EventosRuido.PublicarDisparo(origen);
            Sonidos.ReproducirEn(IdDisparo, origen);
            bool acierta = Azar() <= config.AciertoDron;
            // T-421: si el jugador está agachado tras una cubierta, la bala pega en ella y no le llega.
            if (acierta && objetivo is IProtegeDeBalas protegido && protegido.ProtegeDe(origen, out Vector3 puntoCubierta))
            {
                acierta = false;
                direccion = (puntoCubierta - origen).normalized;
            }
            else if (!acierta)
            {
                // Falla: la bala pasa a un metro o menos del jugador (de ahí el silbido).
                Vector3 lado = Vector3.Cross(direccion, Vector3.up).normalized;
                float desvio = (Azar() < 0.5f ? -1f : 1f) * (0.4f + 0.8f * Azar());
                direccion = (pecho + lado * desvio - origen).normalized;
            }
            DibujarTrazo(origen, direccion, Vector3.Distance(origen, pecho));
            if (acierta)
            {
                Aciertos++;
                objetivo.RecibirDanio(new InfoDanio(config.DanioDron, TipoDanio.Disparo, pecho, direccion, null));
                return;
            }
            float cerca = CalculoTiroDron.DistanciaALaBala(origen, direccion, pecho, out Vector3 masCercano);
            if (CalculoTiroDron.Roza(cerca, CalculoTiroDron.RadioRoce))
            {
                Rozaduras++;
                Sonidos.ReproducirEn(IdRoce, masCercano);
            }
        }

        /// <summary>T-710: cuántos disparos ha hecho.</summary>
        public int Disparos { get; private set; }

        /// <summary>T-710: cuántos le pegaron al jugador.</summary>
        public int Aciertos { get; private set; }

        /// <summary>T-710: cuántas balas pasaron lo bastante cerca del jugador como para silbar.</summary>
        public int Rozaduras { get; private set; }

        /// <summary>T-710: True mientras se ve el trazo del último disparo.</summary>
        public bool TrazoVisible => trazo != null && trazo.enabled;

        /// <summary>T-710: el trazo del último disparo (para pruebas).</summary>
        public LineRenderer Trazo => trazo;

        private void DibujarTrazo(Vector3 origen, Vector3 direccion, float distancia)
        {
            if (trazo == null)
            {
                GameObject objeto = new GameObject("Trazo");
                objeto.transform.SetParent(transform, false);
                trazo = objeto.AddComponent<LineRenderer>();
                trazo.positionCount = 2;
                trazo.useWorldSpace = true;
                trazo.startWidth = GrosorTrazo;
                trazo.endWidth = GrosorTrazo;
                trazo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trazo.receiveShadows = false;
                Shader sprite = Shader.Find("Sprites/Default");
                if (sprite != null)
                {
                    trazo.sharedMaterial = new Material(sprite);
                }
                trazo.startColor = ColorTrazo;
                trazo.endColor = ColorTrazo;
            }
            trazo.SetPosition(0, origen);
            trazo.SetPosition(1, origen + direccion * Mathf.Min(LargoTrazo, Mathf.Max(0.1f, distancia)));
            trazo.enabled = true;
            finTrazo = Time.time + DuracionTrazo;
        }

        private readonly RaycastHit[] impactosLinea = new RaycastHit[16];

        // True si el primer obstáculo entre el dron y el pecho del jugador es el propio jugador (o nada).
        private bool HayLineaDeVista(Transform raizJugador)
        {
            Vector3 pecho = raizJugador.position + Vector3.up * AlturaPecho;
            Vector3 hacia = pecho - transform.position;
            float distancia = hacia.magnitude;
            if (distancia < 0.01f)
            {
                return true;
            }
            int cantidad = Physics.RaycastNonAlloc(transform.position, hacia / distancia, impactosLinea, distancia, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cantidad; i++)
            {
                Collider c = impactosLinea[i].collider;
                if (c.transform.IsChildOf(raizJugador) || c.transform.IsChildOf(transform) || c.transform.root == raizJugador.root)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        private void PonerLuces(bool rojo)
        {
            for (int i = 0; i < luces.Length; i++)
            {
                luces[i].color = rojo ? Color.red : coloresLuces[i];
            }
        }

        /// <summary>True si ya se va: sube y se aleja del jugador hasta salir de la vista (T-587).</summary>
        public bool Retirando { get; private set; }

        /// <summary>T-587: deja de perseguir; desaparece cuando ya no se ve y está lejos (o tras <see cref="ConfigPersecucion.SegundosMaximosRetiro"/> s).</summary>
        public void Retirar()
        {
            Retirando = true;
            segundosRetirandose = 0f;
            finAviso = -1f;
            PonerLuces(false);
        }

        private float segundosRetirandose;

        private void Retirada(Vector3? jugador, float dt)
        {
            segundosRetirandose += dt;
            Vector3 lejos = jugador.HasValue ? transform.position - jugador.Value : transform.forward;
            lejos.y = 0f;
            Vector3 direccion = (lejos.sqrMagnitude > 0.001f ? lejos.normalized : transform.forward) + Vector3.up * 0.25f;
            transform.position += direccion.normalized * (calculo.Paso(true, float.MaxValue, dt));
            bool distante = !jugador.HasValue || Vector3.Distance(transform.position, jugador.Value) >= config.DistanciaRetiro;
            if ((distante && !ReglaVistaPolicia.LoVe(Camera.main, transform.position, RetiroAlcanceCamara)) || segundosRetirandose >= config.SegundosMaximosRetiro)
            {
                Destroy(gameObject);
            }
        }

        private const float RetiroAlcanceCamara = 400f;

        private void Update()
        {
            if (trazo != null && trazo.enabled && Time.time >= finTrazo)
            {
                trazo.enabled = false;
            }
            Vector3? jugador = posicionJugador?.Invoke();
            if (calculo == null)
            {
                return;
            }
            if (Retirando)
            {
                Retirada(jugador, Time.deltaTime);
                return;
            }
            if (!jugador.HasValue)
            {
                return;
            }
            Vector3 meta = jugador.Value + Vector3.up * config.AlturaDron;
            Vector3 haciaMeta = meta - transform.position;
            float paso = calculo.Paso(true, haciaMeta.magnitude, Time.deltaTime);
            if (paso > 0f)
            {
                transform.position += haciaMeta.normalized * paso;
            }
            Disparar();
            Vector3 haciaJugador = jugador.Value - transform.position;
            haciaJugador.y = 0f;
            if (haciaJugador.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(haciaJugador, Vector3.up), GradosPorSegundoGiro * Time.deltaTime);
            }
        }
    }
}
