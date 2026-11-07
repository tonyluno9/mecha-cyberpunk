using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.IA.Aliados
{
    /// <summary>
    /// T-706: el aliado se sube a tu carro y se baja contigo con animación, sin volar. Camina hasta la puerta del copiloto, juega el clip de
    /// subir (el mismo del jugador, Carro_Entrar) deslizándose al asiento y va dentro (oculto) hasta que bajas; entonces sale por la misma puerta
    /// con Carro_Salir. En una moto no sube y si ya hay otro aliado en el asiento del copiloto tampoco: te sigue a pie.
    /// Lo agrega <see cref="AliadoAPie"/> y lo llama desde su Update.
    /// </summary>
    public class AliadoEnCarro : MonoBehaviour
    {
        public enum FaseCarro
        {
            Fuera,
            Acercando,
            Entrando,
            Dentro,
            Saliendo,
        }

        private const float InicioDeslizar = 0.5f;
        private const float FinDeslizar = 0.9f;
        private const float AlturaRayoPiso = 1.5f;
        private const float LargoRayoPiso = 4f;
        private const float SegundosMinimos = 0.6f;

        /// <summary>Los aliados vivos (para saber quién ocupa el asiento del copiloto).</summary>
        public static readonly List<AliadoEnCarro> Todos = new List<AliadoEnCarro>();

        private AliadoAPie aliado;
        private CharacterController controlador;
        private VisualPersonaje visual;
        private IMontable montable;
        private Transform marco;
        private Vector3 puertaLocal;
        private Vector3 asientoLocal;
        private Vector3 salidaLocal;
        private float t;
        private float duracion;
        private float segundosAcercando;
        private readonly RaycastHit[] golpes = new RaycastHit[8];

        public FaseCarro Fase { get; private set; }

        /// <summary>El carro al que se sube o en el que va.</summary>
        public IMontable Montable => montable;

        /// <summary>True mientras el aliado se mueve solo por la animación (entrando, dentro o saliendo): <see cref="AliadoAPie"/> no lo mueve.</summary>
        public bool Controla => Fase == FaseCarro.Entrando || Fase == FaseCarro.Dentro || Fase == FaseCarro.Saliendo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => Todos.Clear();

        private void Awake()
        {
            aliado = GetComponent<AliadoAPie>();
            controlador = GetComponent<CharacterController>();
            visual = GetComponentInChildren<VisualPersonaje>(true);
        }

        private void OnEnable() => Todos.Add(this);

        private void OnDisable()
        {
            Todos.Remove(this);
            Reiniciar();
        }

        /// <summary>El carro en el que va el jugador ahora (el montable ocupado de modo Carro), o null.</summary>
        public static IMontable MontableDelJugador()
        {
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m.Ocupado && m.Modo == ModoJugador.Carro)
                {
                    return m;
                }
            }
            return null;
        }

        /// <summary>True si el asiento es el de una moto (una sola plaza).</summary>
        public static bool EsMoto(IMontable carro) => carro.Asiento != null && carro.Asiento.GetComponentInParent<PuntosMoto>() != null;

        /// <summary>El aliado que ya va (o va a ir) en el asiento del copiloto de ese carro, o null.</summary>
        public static AliadoEnCarro PasajeroDe(IMontable carro, AliadoEnCarro excepto = null)
        {
            foreach (AliadoEnCarro a in Todos)
            {
                if (a != excepto && a.Fase != FaseCarro.Fuera && ReferenceEquals(a.montable, carro))
                {
                    return a;
                }
            }
            return null;
        }

        /// <summary>Rapidez del carro (m/s): la de su cuerpo.</summary>
        public static float RapidezDe(IMontable carro)
        {
            Rigidbody cuerpo = carro.Asiento != null ? carro.Asiento.GetComponentInParent<Rigidbody>() : null;
            return cuerpo != null ? cuerpo.linearVelocity.magnitude : 0f;
        }

        /// <summary>True si este aliado puede empezar a subirse a <paramref name="carro"/> desde donde está.</summary>
        public bool PuedeSubir(IMontable carro, bool siguiendo)
        {
            if (carro == null || carro.Asiento == null || Fase != FaseCarro.Fuera)
            {
                return false;
            }
            Vector3 d = carro.PuntoSubida - transform.position;
            d.y = 0f;
            return ReglaAbordarAliado.PuedeSubir(siguiendo, EsMoto(carro), PasajeroDe(carro, this) == null, d.magnitude, RapidezDe(carro));
        }

        /// <summary>Empieza a caminar hacia la puerta del copiloto.</summary>
        public void IniciarAcercar(IMontable carro)
        {
            montable = carro;
            marco = carro.Asiento.parent != null ? carro.Asiento.parent : carro.Asiento;
            Vector3 asiento = ReglaAbordarAliado.AsientoCopiloto(carro.Asiento.position, marco);
            Vector3 puerta = ReglaAbordarAliado.PuertaCopiloto(carro.PuntoSubida, marco);
            Vector3 puertaPegada = PisoBajo(ReglaAbordarAliado.PuntoPuerta(puerta, asiento));
            Vector3 salida = PisoBajo(ReglaAbordarAliado.PuntoSalida(puertaPegada, asiento));
            puertaLocal = carro.Asiento.InverseTransformPoint(puertaPegada);
            asientoLocal = carro.Asiento.InverseTransformPoint(asiento);
            salidaLocal = carro.Asiento.InverseTransformPoint(salida);
            segundosAcercando = 0f;
            Fase = FaseCarro.Acercando;
        }

        /// <summary>El punto junto a la puerta del copiloto en el mundo (sigue al carro).</summary>
        public Vector3 PuertaMundo => montable != null ? montable.Asiento.TransformPoint(puertaLocal) : transform.position;

        /// <summary>Mientras camina a la puerta: True si ya llegó (empieza el clip), False si sigue; se rinde sola si el carro arranca o tarda mucho.</summary>
        public bool AvanzarAcercando(float dt)
        {
            if (Fase != FaseCarro.Acercando)
            {
                return false;
            }
            segundosAcercando += dt;
            if (montable == null || ReglaAbordarAliado.DebeRendirse(RapidezDe(montable), segundosAcercando) || !montable.Ocupado)
            {
                Reiniciar();
                return false;
            }
            Vector3 d = PuertaMundo - transform.position;
            d.y = 0f;
            if (d.magnitude <= ReglaAbordarAliado.DistanciaPuerta)
            {
                IniciarEntrada();
                return true;
            }
            return false;
        }

        private void IniciarEntrada()
        {
            Fase = FaseCarro.Entrando;
            t = 0f;
            duracion = Mathf.Max(SegundosMinimos, LargoClip("Carro_Entrar") / ReglasAnimacion.VelocidadEstado("CarroEntrar"));
            if (controlador != null)
            {
                controlador.enabled = false;
            }
            if (visual != null)
            {
                visual.Manejando = true;
                visual.Gestear(GestoVisual.CarroEntrar);
            }
        }

        /// <summary>Empieza a bajar por la puerta del copiloto con Carro_Salir.</summary>
        public void IniciarSalida()
        {
            if (Fase != FaseCarro.Dentro && Fase != FaseCarro.Entrando)
            {
                return;
            }
            Fase = FaseCarro.Saliendo;
            t = 0f;
            duracion = Mathf.Max(SegundosMinimos, LargoClip("Carro_Salir") / ReglasAnimacion.VelocidadEstado("CarroSalir"));
            aliado.PonerAbordo(false);
            if (controlador != null)
            {
                controlador.enabled = false;
            }
            if (visual != null)
            {
                visual.Manejando = false;
                visual.Gestear(GestoVisual.CarroSalir);
            }
        }

        /// <summary>Un cuadro de la animación de subir, ir dentro o bajar. Entra con el modo actual del jugador.</summary>
        public void Avanzar(ModoJugador modoJugador, float dt)
        {
            if (!Controla)
            {
                return;
            }
            if (montable == null || marco == null)
            {
                Reiniciar();
                aliado.PonerAbordo(false);
                return;
            }
            Transform asiento = montable.Asiento;
            Vector3 puerta = asiento.TransformPoint(puertaLocal);
            Vector3 pies = asiento.TransformPoint(asientoLocal);
            t += dt;
            if (Fase == FaseCarro.Entrando)
            {
                float u = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(InicioDeslizar * duracion, FinDeslizar * duracion, t));
                Vector3 haciaAsiento = pies - puerta;
                haciaAsiento.y = 0f;
                Quaternion alPuerta = haciaAsiento.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(haciaAsiento, Vector3.up) : transform.rotation;
                transform.SetPositionAndRotation(Vector3.Lerp(puerta, pies, u), Quaternion.Slerp(alPuerta, Quaternion.Euler(0f, asiento.eulerAngles.y, 0f), u));
                if (t >= duracion)
                {
                    Fase = FaseCarro.Dentro;
                    aliado.PonerAbordo(true);
                }
                return;
            }
            if (Fase == FaseCarro.Dentro)
            {
                transform.SetPositionAndRotation(pies, Quaternion.Euler(0f, asiento.eulerAngles.y, 0f));
                aliado.PonerAbordo(true);
                if (modoJugador != ModoJugador.Carro || !montable.Ocupado)
                {
                    IniciarSalida();
                }
                return;
            }
            // Saliendo: del asiento a la puerta (pegado al carro) y de ahí al piso, afuera.
            Vector3 salida = asiento.TransformPoint(salidaLocal);
            Vector3 haciaFuera = salida - puerta;
            haciaFuera.y = 0f;
            Quaternion alFrente = haciaFuera.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(haciaFuera, Vector3.up) : transform.rotation;
            float f1 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / (InicioDeslizar * duracion)));
            if (t < InicioDeslizar * duracion)
            {
                transform.SetPositionAndRotation(Vector3.Lerp(pies, puerta, f1), Quaternion.Slerp(Quaternion.Euler(0f, asiento.eulerAngles.y, 0f), alFrente, f1));
                return;
            }
            float f2 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(InicioDeslizar * duracion, FinDeslizar * duracion, t));
            transform.SetPositionAndRotation(Vector3.Lerp(puerta, salida, f2), alFrente);
            if (t >= duracion)
            {
                Terminar(salida);
            }
        }

        private void Terminar(Vector3 salida)
        {
            Fase = FaseCarro.Fuera;
            montable = null;
            marco = null;
            aliado.Colocar(salida);
            if (controlador != null)
            {
                controlador.enabled = true;
            }
        }

        /// <summary>Vuelve a estar fuera del carro (se rindió, el carro desapareció o se apagó el aliado).</summary>
        public void Reiniciar()
        {
            if (Fase == FaseCarro.Fuera)
            {
                return;
            }
            Fase = FaseCarro.Fuera;
            montable = null;
            marco = null;
            if (visual != null)
            {
                visual.Manejando = false;
            }
            if (controlador != null && aliado != null && !aliado.Abordo)
            {
                controlador.enabled = true;
            }
        }

        // El punto bajado hasta el piso real (sin contar el carro) para que los pies no floten ni se hundan.
        private Vector3 PisoBajo(Vector3 punto)
        {
            int cantidad = Physics.RaycastNonAlloc(punto + Vector3.up * AlturaRayoPiso, Vector3.down, golpes, LargoRayoPiso,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            Transform carro = montable != null && montable.Asiento != null
                ? (montable.Asiento.GetComponentInParent<Rigidbody>() != null ? montable.Asiento.GetComponentInParent<Rigidbody>().transform : marco)
                : marco;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit g = golpes[i];
                if ((carro != null && g.collider.transform.IsChildOf(carro)) || g.collider.transform.IsChildOf(transform) || g.distance >= mejor)
                {
                    continue;
                }
                mejor = g.distance;
                punto.y = g.point.y;
            }
            return punto;
        }

        private float LargoClip(string nombre)
        {
            Animator animador = visual != null ? visual.Animador : null;
            if (animador == null || animador.runtimeAnimatorController == null)
            {
                return 0f;
            }
            foreach (AnimationClip c in animador.runtimeAnimatorController.animationClips)
            {
                if (c != null && c.name == nombre)
                {
                    return c.length;
                }
            }
            return 0f;
        }
    }
}
