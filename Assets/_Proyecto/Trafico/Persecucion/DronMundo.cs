using System;
using MechaCyberpunk.Vehiculos.Efectos;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Un dron de la policía (T-341): vuela sobre el jugador y se le acerca hasta la distancia de seguimiento, con su luz apuntando hacia abajo.
    /// T-710: dispara de verdad: suena el disparo, deja un trazo de la bala (T-745: termina donde pega o se desvanece al máximo alcance), si falla y la bala
    /// pasa cerca del jugador suena el silbido de la bala rozando, y si acierta hace daño real por <see cref="IRecibeDanio"/>.
    /// T-730: se puede derribar a balazos (tiene una esfera de impacto chica en el aire, donde no estorba): al quedarse sin vida deja de disparar, cae girando y explota
    /// en chico (EfectosExplosion, con un radio chico); al derribarlo baja la búsqueda (lo hace el gestor con <see cref="Derribado"/>).
    /// </summary>
    public class DronMundo : MonoBehaviour, IRecibeDanio, IObjetoInerte
    {
        private const float DistanciaMetaRetiro = 50f; // T-1008: al retirarse apunta a un punto así de lejos, nunca llega
        private CalculoVueloDron vuelo;

        /// <summary>Fuente del azar (0 a 1) de los disparos. Las pruebas la cambian.</summary>
        public static Func<float> Azar = () => UnityEngine.Random.value;

        private const float AlturaPecho = 1.2f;
        // T-710: trazo de la bala (hilo finísimo y cortísimo, como el del pandillero) y silbido de la bala que pasa cerca.
        public const string IdDisparo = "ARM_PIS_DISPARO";
        public const string IdRoce = "ARM_BALA_ROZA";
        public const string IdZumbido = "IMP_BALA_ZUMBIDO";
        private const float BajoElDron = 0.2f;

        private Light[] luces;
        private Color[] coloresLuces;
        private float proximoTiro;
        private float finAviso = -1f;
        private CalculoPersecucion calculo;
        private ConfigPersecucion config;
        private Func<Vector3?> posicionJugador;
        // T-938: cada dron tiene su punto propio alrededor del jugador y se aparta de los demás.
        private static readonly System.Collections.Generic.List<DronMundo> ActivosLista = new System.Collections.Generic.List<DronMundo>();
        private static readonly System.Collections.Generic.List<Vector3> OtrasPosiciones = new System.Collections.Generic.List<Vector3>();
        private int indiceOrbita;
        private int totalOrbita = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarActivos() => ActivosLista.Clear();

        private void OnEnable()
        {
            ActivosLista.Add(this);
            if (GetComponent<MarcadorDronHud>() == null)
            {
                gameObject.AddComponent<MarcadorDronHud>(); // T-1492: el marcador en pantalla del dron que te dispara
            }
        }

        /// <summary>T-1492: segundos que el marcador sigue en pantalla después de un disparo.</summary>
        public const float SegundosMarcaTrasTiro = 1.5f;

        private float ultimoTiro = -1f;

        /// <summary>T-1492: True mientras avisa (luz roja) o acaba de disparar: el HUD lo marca.</summary>
        public bool Amenazando => !EstaCayendo && CalculoMarcadorDron.Amenazando(finAviso >= 0f, Time.time, ultimoTiro, SegundosMarcaTrasTiro);

        private void OnDisable()
        {
            ActivosLista.Remove(this);
            PararMotor();
        }

        /// <summary>T-1309b: zumbido del motor en bucle, 3D, pegado al dron mientras vuela.</summary>
        public const string IdMotor = "CAL_DRON_ZUMBIDO";
        private const float SegundosReintentoMotor = 1f;
        private int bucleMotor = Sonidos.SinBucle;
        private float reintentoMotor;

        /// <summary>True mientras suena el motor (para pruebas).</summary>
        public bool MotorSonando => bucleMotor != Sonidos.SinBucle;

        // El servicio de sonido puede llegar después que el dron: se intenta cada segundo hasta que suene.
        private void MantenerMotor()
        {
            if (EstaCayendo)
            {
                PararMotor();
                return;
            }
            if (bucleMotor != Sonidos.SinBucle || Time.time < reintentoMotor)
            {
                return;
            }
            reintentoMotor = Time.time + SegundosReintentoMotor;
            bucleMotor = Sonidos.ReproducirBucle(IdMotor, transform);
        }

        private void PararMotor()
        {
            if (bucleMotor != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucleMotor);
                bucleMotor = Sonidos.SinBucle;
            }
        }

        /// <summary>T-938: el gestor le dice qué número es de cuántos drones, para que cada uno tenga su punto.</summary>
        public void AsignarOrbita(int indice, int total)
        {
            indiceOrbita = indice;
            totalOrbita = Mathf.Max(1, total);
        }

        /// <summary>T-938: la separación que le toca moverse este cuadro para no quedar a menos de la distancia mínima de otro dron.</summary>
        private Vector3 SepararDeOtros()
        {
            OtrasPosiciones.Clear();
            foreach (DronMundo otro in ActivosLista)
            {
                if (otro != this && !otro.EstaCayendo)
                {
                    OtrasPosiciones.Add(otro.transform.position);
                }
            }
            Vector3 aparte = CalculoSeparacionDrones.PuntoEnCirculo(Vector3.zero, indiceOrbita, totalOrbita, 1f, 0f);
            return CalculoSeparacionDrones.Repulsion(transform.position, OtrasPosiciones, config.DistanciaMinimaEntreDrones, aparte);
        }

        public void Preparar(CalculoPersecucion reglas, ConfigPersecucion numeros, Func<Vector3?> dondeEstaElJugador)
        {
            calculo = reglas;
            config = numeros;
            posicionJugador = dondeEstaElJugador;
            vida = numeros != null ? numeros.VidaDron : -1f;
        }


        // ---- T-1498: variantes de combate (DR04, DR02, E13) ----

        public const string NombreVisualVariante = "Visual_Variante";
        private const string NombreVisualBase = "Visual_T1397";

        private VarianteDronCombate variante;

        /// <summary>La variante que se le aplicó (null: es el DR01 de siempre).</summary>
        public VarianteDronCombate Variante => variante;

        private float Danio => variante != null && variante.Danio > 0f ? variante.Danio : config.DanioDron;
        private float SegundosEntreTiros => variante != null && variante.SegundosEntreTiros > 0f ? variante.SegundosEntreTiros : config.SegundosEntreTirosDron;
        private float Acierto => variante != null && variante.Acierto > 0f ? variante.Acierto : config.AciertoDron;
        private float AlturaOrbita => variante != null && variante.Altura > 0f ? variante.Altura : config.AlturaDron;

        /// <summary>T-1498: vuelve a este dron una variante: su vida, daño, cadencia, altura y modelo. Se llama después de <see cref="Preparar"/>.</summary>
        public void AplicarVariante(VarianteDronCombate datos)
        {
            variante = datos;
            if (datos == null)
            {
                return;
            }
            if (datos.Vida > 0f)
            {
                vida = datos.Vida;
            }
            if (datos.RadioImpacto > 0f)
            {
                SphereCollider esfera = GetComponent<SphereCollider>();
                if (esfera != null)
                {
                    esfera.radius = datos.RadioImpacto;
                }
            }
            if (datos.Modelo == null)
            {
                return;
            }
            Transform viejo = transform.Find(NombreVisualBase);
            if (viejo != null)
            {
                viejo.gameObject.SetActive(false);
            }
            GameObject visual = Instantiate(datos.Modelo, transform);
            visual.name = NombreVisualVariante;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * (datos.Escala > 0f ? datos.Escala : 1f);
        }

        // ---- T-730: derribar al dron ----

        /// <summary>Radio (m) de la esfera donde le pegan las balas.</summary>
        public const float RadioImpacto = 0.6f;

        /// <summary>Radio (m) de la explosión chica al estrellarse.</summary>
        public const float RadioExplosion = 2.5f;

        public const string IdExplosion = MechaCyberpunk.Vehiculos.Efectos.LimiteSonidoExplosion.IdDronRobot; // T-1532: antes EXP_ONDA
        private const float GravedadCaida = 14f;
        private const float GiroCaida = 540f;
        private const float SegundosMaximosCayendo = 4f;
        private const float AlturaDeSuelo = 0.4f;

        /// <summary>Se avisa una vez por cada dron que cae (el gestor de la persecución baja la búsqueda). Se vacía al entrar a Play.</summary>
        public static event Action<DronMundo> Derribado;

        private float vida = -1f;
        private float velocidadCaida;
        private float segundosCayendo;

        /// <summary>Vida que le queda (puntos de daño). Se llena al prepararlo con <see cref="ConfigPersecucion.VidaDron"/>.</summary>
        public float Vida => vida;

        /// <summary>True desde que se queda sin vida: ya no dispara ni persigue, solo cae.</summary>
        public bool EstaCayendo { get; private set; }

        /// <summary>True una vez que se estrelló y explotó (el objeto se destruye en ese cuadro).</summary>
        public bool Exploto { get; private set; }

        /// <summary>La explosión chica de la caída (para las pruebas; null hasta que explota).</summary>
        public InstanciaExplosion Explosion { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEventos() => Derribado = null;

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (EstaCayendo || info.Cantidad <= 0f || config == null)
            {
                return ResultadoDanio.Ignorado;
            }
            if (vida < 0f)
            {
                vida = config.VidaDron;
            }
            float aplicado = Mathf.Min(info.Cantidad, vida);
            vida -= aplicado;
            if (vida <= 0f)
            {
                Derribar();
            }
            return new ResultadoDanio(aplicado, TipoBloqueo.Ninguno, vida <= 0f);
        }

        private void Derribar()
        {
            EstaCayendo = true;
            Retirando = false;
            finAviso = -1f;
            PonerLuces(false);
            velocidadCaida = 0f;
            segundosCayendo = 0f;
            Derribado?.Invoke(this);
        }

        private void Caer(float dt)
        {
            segundosCayendo += dt;
            velocidadCaida += GravedadCaida * dt;
            Vector3 paso = Vector3.down * (velocidadCaida * dt);
            bool toca = Physics.Raycast(transform.position, Vector3.down, out RaycastHit golpe, paso.magnitude + AlturaDeSuelo, ~0, QueryTriggerInteraction.Ignore)
                && !golpe.collider.transform.IsChildOf(transform);
            transform.Rotate(Vector3.up, GiroCaida * dt, Space.World);
            transform.Rotate(Vector3.forward, GiroCaida * 0.4f * dt, Space.Self);
            if (toca || segundosCayendo >= SegundosMaximosCayendo)
            {
                Vector3 donde = toca ? golpe.point + Vector3.up * AlturaDeSuelo : transform.position;
                Estrellar(donde);
                return;
            }
            transform.position += paso;
        }

        private void Estrellar(Vector3 donde)
        {
            Exploto = true;
            Explosion = EfectosExplosion.Explosion(donde, RadioExplosion);
            MechaCyberpunk.Vehiculos.Efectos.LimiteSonidoExplosion.Sonar(MechaCyberpunk.Vehiculos.Efectos.TipoExplosionSonido.DronRobot, donde); // T-1472 / T-1532: EXP_DRON_ROBOT con tope en cadena
            Destroy(gameObject);
        }

        private void Awake()
        {
            if (GetComponentInChildren<Collider>() == null)
            {
                SphereCollider esfera = gameObject.AddComponent<SphereCollider>();
                esfera.radius = RadioImpacto;
                esfera.isTrigger = false;
            }
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
            proximoTiro = Time.time + SegundosEntreTiros;
            ultimoTiro = Time.time;
            Disparos++;
            Vector3 origen = transform.position + Vector3.down * BajoElDron;
            Vector3 pecho = jugador.Raiz.position + Vector3.up * AlturaPecho;
            Vector3 direccion = (pecho - origen).normalized;
            EventosRuido.PublicarDisparo(origen);
            Sonidos.ReproducirEn(IdDisparo, origen);
            bool acierta = Azar() <= Acierto;
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
            if (acierta)
            {
                Aciertos++;
                objetivo.RecibirDanio(new InfoDanio(Danio, TipoDanio.Disparo, pecho, direccion, null));
                return;
            }
            float cerca = CalculoTiroDron.DistanciaALaBala(origen, direccion, pecho, out Vector3 masCercano);
            if (CalculoTiroDron.Roza(cerca, CalculoTiroDron.RadioRoce))
            {
                Rozaduras++;
                Sonidos.ReproducirEn(IdRoce, masCercano);
                Sonidos.ReproducirEn(IdZumbido, masCercano); // T-1472: la bala enemiga que pasa cerca del jugador
            }
        }

        /// <summary>T-710: cuántos disparos ha hecho.</summary>
        public int Disparos { get; private set; }

        /// <summary>T-710: cuántos le pegaron al jugador.</summary>
        public int Aciertos { get; private set; }

        /// <summary>T-710: cuántas balas pasaron lo bastante cerca del jugador como para silbar.</summary>
        public int Rozaduras { get; private set; }

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
            float rumboRetiro = RumboActual();
            Volar(transform.position + direccion.normalized * DistanciaMetaRetiro, rumboRetiro, dt);
            bool distante = !jugador.HasValue || Vector3.Distance(transform.position, jugador.Value) >= config.DistanciaRetiro;
            if ((distante && !ReglaVistaPolicia.LoVe(Camera.main, transform.position, RetiroAlcanceCamara)) || segundosRetirandose >= config.SegundosMaximosRetiro)
            {
                Destroy(gameObject);
            }
        }

        private const float RetiroAlcanceCamara = 400f;

        private void Update()
        {
            MantenerMotor(); // T-1309b
            Vector3? jugador = posicionJugador?.Invoke();
            if (calculo == null)
            {
                return;
            }
            if (EstaCayendo)
            {
                Caer(Time.deltaTime);
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
            Vector3 meta = CalculoSeparacionDrones.PuntoEnCirculo(jugador.Value, indiceOrbita, totalOrbita, config.RadioOrbitaDron, AlturaOrbita);
            Vector3 haciaJugador = jugador.Value - transform.position;
            haciaJugador.y = 0f;
            float rumbo = haciaJugador.sqrMagnitude > 0.001f ? Mathf.Atan2(haciaJugador.x, haciaJugador.z) * Mathf.Rad2Deg : RumboActual();
            Volar(PuntoDeParada(meta), rumbo, Time.deltaTime);
            transform.position += SepararDeOtros();
            Disparar();
        }

        // T-1008: vuelo con inercia: acelera, frena suave al llegar, gira el rumbo con límite y se inclina con la aceleración.
        // Como antes: no se acerca a menos de la distancia de seguimiento de su punto; dentro de ella se queda flotando donde está.
        private Vector3 PuntoDeParada(Vector3 meta)
        {
            Vector3 hacia = meta - transform.position;
            float distancia = hacia.magnitude;
            float seguimiento = calculo.DistanciaSeguimiento;
            return distancia <= seguimiento ? transform.position : transform.position + hacia / distancia * (distancia - seguimiento);
        }

        private float RumboActual() => vuelo != null ? vuelo.Rumbo : transform.eulerAngles.y;

        private void Volar(Vector3 meta, float rumboDeseado, float dt)
        {
            if (vuelo == null)
            {
                vuelo = new CalculoVueloDron(config.ParametrosVuelo, transform.eulerAngles.y);
            }
            transform.position += vuelo.Paso(transform.position, meta, rumboDeseado, dt);
            transform.rotation = vuelo.Rotacion;
        }
    }
}
