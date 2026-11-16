using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1013: dispara las armas especiales a pie, que <see cref="PistolaAPie"/> le pasa cada cuadro con su cargador:
    /// - Lanzagranadas: cada clic gasta un tiro y lanza una granada en arco hacia la mira, que explota al chocar.
    /// - Granada: al apuntar se ve el arco por donde va a volar; cada clic lanza una granada con la mano que rebota y explota al acabarse la mecha.
    /// - Rifle de plasma: mantener el disparo carga 3 s (una bola de energía crece en la boca); luego sale un rayo de 5 s que sigue a la mira, atraviesa a la
    ///   gente y daña todo lo que toca (para en las paredes). La lógica está en <see cref="ReglaRayoPlasma"/> y <see cref="ReglaBalistica"/>.
    /// También hace girar el tambor del revólver y del lanzagranadas en cada tiro (<see cref="TamborArma"/>).
    /// </summary>
    [RequireComponent(typeof(PistolaAPie))]
    public class ArmasEspecialesAPie : MonoBehaviour
    {
        private const float TicDanioRayo = 0.1f;
        private const int MaxToques = 32;
        private const float AlturaManoGranada = 1.45f;
        private const float AdelanteManoGranada = 0.35f;
        private const float LatidoRayo = 18f;

        private ControladorAPie controlador;
        private PistolaAPie pistola;
        private ArmasAPie armas;
        private ConfigArmasEspeciales cfg;
        private ReglaRayoPlasma plasma;
        private DatosArma datosPlasma;
        private bool pulsadoAntes;
        private LineRenderer arco;
        private LineRenderer rayo;
        private Transform bolaCarga;
        private Light luzRayo;
        private Vector3[] puntosArco;
        private float siguienteZumbido;
        private float acumuladoTic;
        private Vector3 finRayo;
        private readonly RaycastHit[] toques = new RaycastHit[MaxToques];
        private readonly HashSet<IRecibeDanio> tocados = new HashSet<IRecibeDanio>();
        private readonly List<TamborArma> tambores = new List<TamborArma>();

        /// <summary>En qué va el rifle de plasma.</summary>
        public EstadoPlasma EstadoPlasma => plasma != null ? plasma.Estado : EstadoPlasma.Reposo;

        /// <summary>Carga del rifle de plasma (0 a 1).</summary>
        public float CargaPlasma => plasma != null ? plasma.Carga : 0f;

        /// <summary>Segundos de rayo que quedan.</summary>
        public float RestanteRayo => plasma != null ? plasma.RestanteRayo : 0f;

        /// <summary>Donde termina el rayo ahora (pared o alcance).</summary>
        public Vector3 FinRayo => finRayo;

        /// <summary>True si se está dibujando el arco de la granada.</summary>
        public bool ArcoVisible => arco != null && arco.enabled;

        /// <summary>La última granada o proyectil lanzado (para las pruebas).</summary>
        public ProyectilAPie UltimoProyectil { get; private set; }

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            pistola = GetComponent<PistolaAPie>();
            armas = GetComponent<ArmasAPie>();
            cfg = ConfigArmasEspeciales.Actual;
            arco = Linea("ArcoGranada", cfg.AnchoArco, cfg.ColorArco);
            rayo = Linea("RayoPlasma", cfg.AnchoRayo, cfg.ColorRayo);
            rayo.numCapVertices = 4;
            puntosArco = new Vector3[Mathf.Max(2, cfg.PuntosArco)];
            GameObject bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(bola.GetComponent<Collider>());
            bola.name = "CargaPlasma";
            bola.transform.SetParent(transform, false);
            Renderer r = bola.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (cfg.MaterialRayo != null)
            {
                r.sharedMaterial = cfg.MaterialRayo;
            }
            bolaCarga = bola.transform;
            bolaCarga.gameObject.SetActive(false);
            luzRayo = new GameObject("LuzPlasma").AddComponent<Light>();
            luzRayo.transform.SetParent(transform, false);
            luzRayo.type = LightType.Point;
            luzRayo.color = cfg.ColorRayo;
            luzRayo.range = 4f;
            luzRayo.enabled = false;
        }

        private void Start()
        {
            // Los tambores del revólver y del lanzagranadas (un hijo "Tambor" en su visual).
            foreach (ArmaAPie arma in new[] { ArmaAPie.Revolver, ArmaAPie.Lanzagranadas })
            {
                GameObject visual = armas != null ? armas.Visual(arma) : null;
                Transform tambor = visual != null ? Buscar(visual.transform, TamborArma.NombreTambor) : null;
                if (tambor != null)
                {
                    TamborArma t = tambor.GetComponent<TamborArma>();
                    if (t == null)
                    {
                        t = tambor.gameObject.AddComponent<TamborArma>();
                    }
                    t.Iniciar(arma, visual.transform);
                    tambores.Add(t);
                }
            }
        }

        private void OnEnable()
        {
            if (pistola != null)
            {
                pistola.Fogonazo += AlDisparar;
            }
        }

        private void OnDisable()
        {
            if (pistola != null)
            {
                pistola.Fogonazo -= AlDisparar;
            }
            Soltar();
        }

        private void OnDestroy()
        {
            if (arco != null) Destroy(arco.gameObject);
            if (rayo != null) Destroy(rayo.gameObject);
        }

        private void AlDisparar(ArmaAPie arma, Vector3 boca)
        {
            foreach (TamborArma t in tambores)
            {
                if (t != null && t.Arma == arma)
                {
                    DatosArma d = pistola.DatosActuales;
                    t.Girar(d != null && d.Cargador > 0 ? d.Cargador : TamborArma.RecamarasPorOmision, cfg.SegundosGiroTambor);
                }
            }
        }

        private static Transform Buscar(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            return null;
        }

        private LineRenderer Linea(string nombre, float ancho, Color color)
        {
            LineRenderer l = new GameObject(nombre).AddComponent<LineRenderer>();
            l.useWorldSpace = true;
            l.widthMultiplier = ancho;
            l.startColor = color;
            l.endColor = color;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.sharedMaterial = cfg.MaterialRayo != null ? cfg.MaterialRayo : new Material(Shader.Find("Sprites/Default"));
            l.enabled = false;
            return l;
        }

        /// <summary>
        /// Un cuadro con un arma especial en la mano: <paramref name="cargador"/> es su cargador (gasta un tiro por granada o por rayo) y
        /// <paramref name="pulsado"/> si el botón de disparar está abajo.
        /// </summary>
        public void Actualizar(ArmaAPie arma, CargadorPistola cargador, DatosArma d, bool pulsado)
        {
            bool flanco = pulsado && !pulsadoAntes;
            pulsadoAntes = pulsado;
            if (d.TipoDisparo != TipoDisparoArma.Rayo && plasma != null && plasma.Estado != EstadoPlasma.Reposo)
            {
                plasma.Cancelar();
            }
            switch (d.TipoDisparo)
            {
                case TipoDisparoArma.Proyectil:
                    ApagarPlasma();
                    DibujarArco(arma, d);
                    if (flanco && cargador.IntentarDisparar())
                    {
                        LanzarProyectil(arma, d);
                    }
                    break;
                case TipoDisparoArma.Lanzada:
                    ApagarPlasma();
                    DibujarArco(arma, d);
                    if (flanco && cargador.IntentarDisparar())
                    {
                        LanzarGranada(arma, d);
                    }
                    break;
                case TipoDisparoArma.Rayo:
                    arco.enabled = false;
                    Plasma(arma, cargador, d, pulsado);
                    break;
            }
        }

        /// <summary>Sin arma especial en la mano: se esconde el arco y se corta el plasma.</summary>
        public void Soltar()
        {
            pulsadoAntes = false;
            if (arco != null)
            {
                arco.enabled = false;
            }
            plasma?.Cancelar();
            ApagarPlasma();
        }

        // ---- Lanzagranadas y granada ----

        private Vector3 OrigenLanzada()
        {
            Vector3 adelante = controlador.Camara != null ? Vector3.ProjectOnPlane(controlador.Camara.forward, Vector3.up).normalized : transform.forward;
            return transform.position + Vector3.up * AlturaManoGranada + adelante * AdelanteManoGranada + transform.right * 0.2f;
        }

        private Vector3 VelocidadDe(ArmaAPie arma, DatosArma d, out Vector3 origen)
        {
            if (d.TipoDisparo == TipoDisparoArma.Lanzada)
            {
                origen = OrigenLanzada();
                Vector3 mira = controlador.Camara != null ? controlador.Camara.forward : transform.forward;
                return ReglaBalistica.Elevar(mira, cfg.ElevacionLanzada) * d.VelocidadProyectil;
            }
            Transform boca = pistola.BocaDe(arma);
            origen = boca != null ? boca.position : transform.position + Vector3.up * AlturaManoGranada;
            Vector3 punto = pistola.PuntoDeMira(d.Alcance);
            ReglaBalistica.DireccionHacia(origen, punto, d.VelocidadProyectil, Physics.gravity.y, out Vector3 direccion);
            return direccion * d.VelocidadProyectil;
        }

        private void LanzarProyectil(ArmaAPie arma, DatosArma d)
        {
            Vector3 velocidad = VelocidadDe(arma, d, out Vector3 origen);
            controlador.MirarAlDisparar();
            Sonidos.ReproducirEn(PistolaAPie.IdDisparo(arma), origen);
            UltimoProyectil = ProyectilAPie.Lanzar(origen, velocidad, d.RadioExplosion, d.Danio, true, 0f, 0f, transform.root);
            pistola.AvisarDisparoEspecial(arma, origen);
        }

        private void LanzarGranada(ArmaAPie arma, DatosArma d)
        {
            Vector3 velocidad = VelocidadDe(arma, d, out Vector3 origen);
            controlador.MirarAlDisparar();
            Sonidos.ReproducirEn("ARM_CAMBIAR", origen);
            UltimoProyectil = ProyectilAPie.Lanzar(origen, velocidad, d.RadioExplosion, d.Danio, false, cfg.MechaGranada, cfg.Rebote, transform.root);
            pistola.AvisarDisparoEspecial(arma, origen);
        }

        // Al apuntar se ve el arco hasta lo primero que toca.
        private void DibujarArco(ArmaAPie arma, DatosArma d)
        {
            if (!controlador.Apuntando)
            {
                arco.enabled = false;
                return;
            }
            Vector3 velocidad = VelocidadDe(arma, d, out Vector3 origen);
            ReglaBalistica.Arco(origen, velocidad, Physics.gravity, cfg.SegundosArco, puntosArco);
            int usados = puntosArco.Length;
            for (int i = 1; i < puntosArco.Length; i++)
            {
                Vector3 tramo = puntosArco[i] - puntosArco[i - 1];
                float largo = tramo.magnitude;
                if (largo > 0.0001f && Physics.Raycast(puntosArco[i - 1], tramo / largo, out RaycastHit golpe, largo, ~0, QueryTriggerInteraction.Ignore)
                    && golpe.collider.transform.root != transform.root)
                {
                    puntosArco[i] = golpe.point;
                    usados = i + 1;
                    break;
                }
            }
            arco.positionCount = usados;
            arco.SetPositions(puntosArco);
            arco.enabled = true;
        }

        // ---- Rifle de plasma ----

        private void Plasma(ArmaAPie arma, CargadorPistola cargador, DatosArma d, bool pulsado)
        {
            if (plasma == null || datosPlasma != d)
            {
                plasma = new ReglaRayoPlasma(d.SegundosCarga, d.SegundosRayo);
                datosPlasma = d;
            }
            EstadoPlasma antes = plasma.Estado;
            bool empezo = plasma.Avanzar(Time.deltaTime, pulsado, cargador.IntentarDisparar);
            Transform boca = pistola.BocaDe(arma);
            Vector3 origen = boca != null ? boca.position : transform.position + Vector3.up * AlturaManoGranada;
            if (plasma.Estado == EstadoPlasma.Cargando)
            {
                if (antes != EstadoPlasma.Cargando)
                {
                    Sonidos.ReproducirEn("ARM_LASER_AVISO", origen);
                }
                float c = plasma.Carga;
                bolaCarga.gameObject.SetActive(true);
                bolaCarga.position = origen;
                bolaCarga.localScale = Vector3.one * (cfg.TamanoCarga * c * (0.9f + 0.1f * Mathf.Sin(Time.time * LatidoRayo)));
                luzRayo.enabled = true;
                luzRayo.transform.position = origen;
                luzRayo.intensity = cfg.IntensidadLuzRayo * c;
                rayo.enabled = false;
                return;
            }
            if (plasma.Estado == EstadoPlasma.Rayo)
            {
                if (empezo)
                {
                    Sonidos.ReproducirEn(PistolaAPie.IdDisparo(ArmaAPie.Sniper), origen);
                    pistola.AvisarDisparoEspecial(arma, origen);
                    siguienteZumbido = Time.time;
                    acumuladoTic = TicDanioRayo;
                }
                controlador.MirarAlDisparar();
                Rayo(origen, d);
                return;
            }
            ApagarPlasma();
        }

        private void Rayo(Vector3 origen, DatosArma d)
        {
            Vector3 punto = pistola.PuntoDeMira(d.Alcance);
            Vector3 hacia = punto - origen;
            Vector3 direccion = hacia.sqrMagnitude > 0.0001f ? hacia.normalized : transform.forward;
            float largo = d.Alcance;
            tocados.Clear();
            int n = Physics.SphereCastNonAlloc(origen, cfg.RadioRayo, direccion, toques, d.Alcance, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(toques, 0, n, Comparador);
            for (int i = 0; i < n; i++)
            {
                RaycastHit t = toques[i];
                if (t.collider.transform.root == transform.root)
                {
                    continue;
                }
                IRecibeDanio objetivo = t.collider.GetComponentInParent<IRecibeDanio>();
                bool esAlguien = objetivo != null && !(objetivo is IObjetoInerte);
                if (objetivo != null)
                {
                    tocados.Add(objetivo);
                }
                if (!esAlguien)
                {
                    // Las paredes (y los objetos) paran el rayo; la gente no.
                    largo = t.distance > 0f ? t.distance : 0f;
                    break;
                }
            }
            finRayo = origen + direccion * largo;
            acumuladoTic += Time.deltaTime;
            if (acumuladoTic >= TicDanioRayo)
            {
                float danio = d.Danio * acumuladoTic;
                acumuladoTic = 0f;
                foreach (IRecibeDanio objetivo in tocados)
                {
                    objetivo.RecibirDanio(new InfoDanio(danio, TipoDanio.Disparo, finRayo, direccion, null));
                }
            }
            if (Time.time >= siguienteZumbido)
            {
                siguienteZumbido = Time.time + cfg.SegundosZumbido;
                Sonidos.ReproducirEn("ARM_BALA_ZUMBIDO", origen);
            }
            rayo.enabled = true;
            rayo.positionCount = 2;
            rayo.SetPosition(0, origen);
            rayo.SetPosition(1, finRayo);
            rayo.widthMultiplier = cfg.AnchoRayo * (0.85f + 0.15f * Mathf.Sin(Time.time * LatidoRayo));
            bolaCarga.gameObject.SetActive(true);
            bolaCarga.position = origen;
            bolaCarga.localScale = Vector3.one * cfg.TamanoCarga * 0.7f;
            luzRayo.enabled = true;
            luzRayo.transform.position = finRayo - direccion * 0.3f;
            luzRayo.intensity = cfg.IntensidadLuzRayo;
        }

        private static readonly IComparer<RaycastHit> Comparador = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        private void ApagarPlasma()
        {
            if (rayo != null)
            {
                rayo.enabled = false;
            }
            if (bolaCarga != null)
            {
                bolaCarga.gameObject.SetActive(false);
            }
            if (luzRayo != null)
            {
                luzRayo.enabled = false;
            }
        }
    }
}
