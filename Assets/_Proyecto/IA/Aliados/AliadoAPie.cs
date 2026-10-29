using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using UnityEngine;

namespace MechaCyberpunk.IA.Aliados
{
    /// <summary>
    /// Aliado a pie (T-405): muñeco naranja con pistola. Te sigue a 3 a 5 m (corre si te alejas más de 8 m), espera donde está si
    /// te subes a un carro o si le ordenas esperar, y dispara al rival que elige <see cref="ReglaAliado"/> con aviso láser y recarga.
    /// Si cae, se desactiva y <see cref="PandillaJugador"/> lo trae de vuelta a los 45 s.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class AliadoAPie : MonoBehaviour, IVida, IRecibeDanio
    {
        public static readonly List<AliadoAPie> Activos = new List<AliadoAPie>();

        private const float Gravedad = -20f;
        private const float AlturaBoca = 1.4f;
        private const float AlturaPecho = 1.2f;
        private const float AdelanteBoca = 0.4f;
        private const float DistanciaMax = 5f;
        private const float DistanciaCorrer = 8f;
        private const float VelocidadCaminar = 4.5f;
        private const float VelocidadCorrer = 7.5f;
        private const float GiroGrados = 540f;
        private const float AvisoTiro = 0.5f;
        private const float RecargaTiro = 1.2f;
        private const float AciertoCerca = 0.7f;
        private const float AciertoLejos = 0.3f;
        private const float DistCerca = 10f;
        private const float DistLejos = 30f;
        private static readonly int IdColor = Shader.PropertyToID("_BaseColor");
        private static readonly int IdVelocidad = Animator.StringToHash("Velocidad");
        private static readonly int IdEnPiso = Animator.StringToHash("EnPiso");
        private static readonly int IdApuntando = Animator.StringToHash("Apuntando");
        private static readonly int IdArma = Animator.StringToHash("Arma");
        private static readonly int IdDisparo = Animator.StringToHash("Disparo");

        [SerializeField] private Color colorAliado = new Color(1f, 0.5f, 0.1f);

        private CharacterController controlador;
        private PistolaPandillero pistola;
        private Animator animador;
        private ModeloVida modelo;
        private int nivel = 1;
        private float velocidadVertical;
        private float finAviso = -1f;
        private float finRecarga;
        private readonly RaycastHit[] impactos = new RaycastHit[16];
        private PandilleroAPie objetivo;
        private readonly List<Vector3> posiciones = new List<Vector3>();
        private readonly List<PandilleroAPie> candidatos = new List<PandilleroAPie>();

        public int Nivel => nivel;
        public bool Siguiendo { get; set; } = true;
        public float Actual => modelo.Actual;
        public float Maxima => modelo.Maxima;
        public bool EstaKO => modelo.EstaKO;

        public event Action Cambio;
        public event Action KO;
        /// <summary>Venció a un rival con un disparo suyo.</summary>
        public event Action<AliadoAPie> Vencio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => Activos.Clear();

        /// <summary>El aliado vivo más cercano a <paramref name="desde"/> a menos de <paramref name="maxima"/> m (plano), o null.</summary>
        public static AliadoAPie MasCercano(Vector3 desde, float maxima)
        {
            AliadoAPie mejor = null;
            float menor = maxima;
            foreach (AliadoAPie a in Activos)
            {
                if (a.EstaKO)
                {
                    continue;
                }
                Vector3 d = a.transform.position - desde;
                d.y = 0f;
                if (d.magnitude < menor)
                {
                    menor = d.magnitude;
                    mejor = a;
                }
            }
            return mejor;
        }

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            pistola = GetComponent<PistolaPandillero>();
            if (pistola == null)
            {
                pistola = gameObject.AddComponent<PistolaPandillero>();
            }
            animador = GetComponentInChildren<Animator>(true);
            PonerNivel(1);
            Pintar();
        }

        private void OnEnable() => Activos.Add(this);

        private void OnDisable()
        {
            Activos.Remove(this);
            if (pistola != null)
            {
                pistola.QuitarAviso();
            }
        }

        /// <summary>Fija el nivel y llena la vida a su máximo.</summary>
        public void PonerNivel(int nuevo)
        {
            nivel = Mathf.Clamp(nuevo, 1, ReglaAliado.NivelMaximo);
            if (modelo != null)
            {
                modelo.Cambio -= AlCambiar;
                modelo.KO -= AlLlegarAKO;
            }
            modelo = new ModeloVida(ReglaAliado.Vida(nivel));
            modelo.Cambio += AlCambiar;
            modelo.KO += AlLlegarAKO;
            Cambio?.Invoke();
        }

        /// <summary>Lo pone en esa posición con la vida llena y lo activa.</summary>
        public void Revivir(Vector3 posicion)
        {
            gameObject.SetActive(true);
            PonerNivel(nivel);
            Colocar(posicion);
        }

        public void Colocar(Vector3 posicion)
        {
            controlador.enabled = false;
            transform.position = posicion;
            controlador.enabled = true;
            velocidadVertical = 0f;
        }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            if (modelo.EstaKO || info.Cantidad <= 0f)
            {
                return ResultadoDanio.Ignorado;
            }
            float aplicado = modelo.Aplicar(info.Cantidad);
            return new ResultadoDanio(aplicado, TipoBloqueo.Ninguno, modelo.EstaKO);
        }

        private void AlCambiar() => Cambio?.Invoke();

        private void AlLlegarAKO()
        {
            KO?.Invoke();
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (modelo.EstaKO)
            {
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return;
            }
            Vector3 posJugador = jugador.Raiz.position;
            BuscarObjetivo(jugador.Raiz);

            Vector3 boca = transform.position + Vector3.up * AlturaBoca + transform.forward * AdelanteBoca;
            bool puedeTirar = false;
            Vector3 pecho = Vector3.zero;
            float distObjetivo = 0f;
            if (objetivo != null)
            {
                pecho = objetivo.transform.position + Vector3.up * AlturaPecho;
                Vector3 hacia = pecho - boca;
                distObjetivo = hacia.magnitude;
                puedeTirar = HayLinea(boca, hacia.normalized, distObjetivo, jugador.Raiz);
            }
            bool disparando = false;
            if (objetivo != null && puedeTirar)
            {
                if (finAviso < 0f && Time.time >= finRecarga)
                {
                    finAviso = Time.time + AvisoTiro;
                }
                if (finAviso >= 0f)
                {
                    disparando = true;
                    pistola.Apuntar(boca, pecho);
                    if (Time.time >= finAviso)
                    {
                        Disparar(boca, pecho, distObjetivo);
                    }
                }
            }
            else
            {
                finAviso = -1f;
                pistola.QuitarAviso();
            }

            Vector3 deseada = Vector3.zero;
            Vector3 haciaJugador = posJugador - transform.position;
            haciaJugador.y = 0f;
            float dist = haciaJugador.magnitude;
            if (Siguiendo && jugador.Modo == ModoJugador.APie && dist > DistanciaMax)
            {
                deseada = haciaJugador.normalized * (dist > DistanciaCorrer ? VelocidadCorrer : VelocidadCaminar);
            }
            Vector3 mirada = objetivo != null && puedeTirar ? objetivo.transform.position - transform.position : deseada;
            mirada.y = 0f;
            if (mirada.sqrMagnitude > 0.0001f)
            {
                Quaternion meta = Quaternion.LookRotation(mirada.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, meta, GiroGrados * Time.deltaTime);
            }
            velocidadVertical = controlador.isGrounded ? -1f : velocidadVertical + Gravedad * Time.deltaTime;
            Vector3 mover = deseada;
            mover.y = velocidadVertical;
            controlador.Move(mover * Time.deltaTime);

            if (animador != null)
            {
                animador.SetFloat(IdVelocidad, deseada.magnitude);
                animador.SetBool(IdEnPiso, true);
                animador.SetBool(IdApuntando, disparando);
                animador.SetInteger(IdArma, 1);
            }
        }

        // Solo los muros bloquean: el jugador, los aliados y los pandilleros no.
        private bool HayLinea(Vector3 origen, Vector3 direccion, float distancia, Transform jugador)
        {
            int cantidad = Physics.RaycastNonAlloc(origen, direccion, impactos, distancia, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cantidad; i++)
            {
                Collider c = impactos[i].collider;
                if (c.transform.IsChildOf(jugador) || c.GetComponentInParent<AliadoAPie>() != null
                    || c.GetComponentInParent<PandilleroAPie>() != null)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        private void Disparar(Vector3 boca, Vector3 pecho, float distancia)
        {
            finAviso = -1f;
            finRecarga = Time.time + RecargaTiro;
            bool estabaKO = objetivo.EstaKO;
            float p = PistolaPandillero.ProbabilidadAcierto(distancia, DistCerca, DistLejos, AciertoCerca, AciertoLejos);
            pistola.Disparar(boca, pecho, objetivo, ReglaAliado.Danio(nivel), p, true);
            if (animador != null)
            {
                animador.SetTrigger(IdDisparo);
            }
            if (!estabaKO && objetivo.EstaKO)
            {
                Vencio?.Invoke(this);
            }
        }

        private void BuscarObjetivo(Transform jugador)
        {
            candidatos.Clear();
            posiciones.Clear();
            int quienDispara = -1;
            foreach (PandilleroAPie p in PandilleroAPie.Activos)
            {
                if (p.EstaKO)
                {
                    continue;
                }
                if (p.ObjetivoActual == jugador && (p.Estado == EstadoPandillero.Preparando || p.Estado == EstadoPandillero.Recargando)
                    && quienDispara < 0)
                {
                    quienDispara = candidatos.Count;
                }
                candidatos.Add(p);
                posiciones.Add(p.transform.position);
            }
            int indice = ReglaAliado.ElegirObjetivo(jugador.position, posiciones, quienDispara);
            objetivo = indice >= 0 ? candidatos[indice] : null;
        }

        private void Pintar()
        {
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                if (r is LineRenderer)
                {
                    continue;
                }
                r.GetPropertyBlock(bloque);
                bloque.SetColor(IdColor, colorAliado);
                r.SetPropertyBlock(bloque);
            }
        }
    }
}
