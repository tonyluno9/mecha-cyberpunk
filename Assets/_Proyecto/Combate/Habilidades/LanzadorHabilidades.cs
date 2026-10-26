using System;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Datos;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Combate.Habilidades
{
    /// <summary>
    /// Lanza las habilidades especiales de un mecha (T-175): proyectil, cono, escudo y embestida, con la potencia de
    /// <see cref="ReglaHabilidad.Potencia"/>. No lleva teclas, munición ni enfriamiento (eso lo lleva quien lo usa).
    /// Nunca daña a su propia raíz.
    /// </summary>
    [RequireComponent(typeof(VidaMecha))]
    public class LanzadorHabilidades : MonoBehaviour
    {
        private const int MaxCercanos = 32;
        // Velocidad que mantiene a la embestida pegada al piso (m/s) y qué tan inclinado puede ser un choque para contar como "de lado".
        private const float VelocidadEnPiso = -2f;
        private const float MaxInclinacionContacto = 0.5f;

        [Tooltip("De dónde salen los efectos del brazo (tecla 1). Si falta, del centro del mecha.")]
        [SerializeField] private Transform origenBrazo;
        [Tooltip("De dónde salen los efectos del torso (tecla 2). Si falta, del centro del mecha.")]
        [SerializeField] private Transform origenTorso;
        [Tooltip("Tamaños, duraciones y colores de la presentación. Si falta usa los valores de fábrica.")]
        [SerializeField] private ConfigEfectosHabilidad config;

        private readonly Collider[] cercanos = new Collider[MaxCercanos];
        private readonly List<Transform> raicesGolpeadas = new List<Transform>();

        private VidaMecha vida;
        private IPeleador peleador;
        private CharacterController controlador;
        private ConfigEfectosHabilidad presentacion;

        private bool escudoActivo;
        private float finEscudo;
        private EscudoVisual escudoVisual;

        private bool embistiendo;
        private DatosHabilidad embestida;
        private float potenciaEmbestida;
        private Vector3 direccionEmbestida;
        private float recorridoEmbestida;
        private float tiempoEmbestida;
        private Collider contacto;
        private Vector3 puntoContacto;

        public bool EscudoActivo => escudoActivo;
        public bool Embistiendo => embistiendo;

        /// <summary>Se dispara cuando una habilidad se lanzó de verdad.</summary>
        public event Action<DatosHabilidad> Lanzada;

        private void Awake()
        {
            vida = GetComponent<VidaMecha>();
            peleador = GetComponent<IPeleador>();
            controlador = GetComponent<CharacterController>();
            presentacion = config;
            if (presentacion == null)
            {
                presentacion = ScriptableObject.CreateInstance<ConfigEfectosHabilidad>();
                presentacion.hideFlags = HideFlags.DontSave;
            }
        }

        private void OnEnable()
        {
            vida.KO += AlLlegarAKO;
        }

        private void OnDisable()
        {
            vida.KO -= AlLlegarAKO;
            TerminarEscudo();
            embistiendo = false;
        }

        /// <summary>
        /// Ejecuta el efecto de <paramref name="habilidad"/> con la potencia de <paramref name="nivel"/>. Devuelve false, sin efecto,
        /// si el mecha está en KO, ya está embistiendo o la habilidad es nula.
        /// </summary>
        public bool Lanzar(DatosHabilidad habilidad, int nivel, bool desdeTorso)
        {
            if (habilidad == null || vida.EstaKO || embistiendo)
            {
                return false;
            }
            float potencia = ReglaHabilidad.Potencia(nivel);
            Vector3 origen = Origen(desdeTorso, out Vector3 frente);
            switch (habilidad.Efecto)
            {
                case TipoEfectoHabilidad.Proyectil:
                    LanzarProyectiles(habilidad, potencia, origen, frente);
                    break;
                case TipoEfectoHabilidad.Cono:
                    LanzarCono(habilidad, potencia, origen, frente);
                    break;
                case TipoEfectoHabilidad.Escudo:
                    ActivarEscudo(habilidad, potencia);
                    break;
                case TipoEfectoHabilidad.Embestida:
                    EmpezarEmbestida(habilidad, potencia);
                    break;
            }
            Lanzada?.Invoke(habilidad);
            return true;
        }

        private Vector3 Origen(bool desdeTorso, out Vector3 frente)
        {
            Transform origen = desdeTorso ? origenTorso : origenBrazo;
            if (origen != null)
            {
                frente = origen.forward;
                return origen.position;
            }
            frente = transform.forward;
            return controlador != null ? transform.TransformPoint(controlador.center) : transform.position;
        }

        private void Update()
        {
            if (escudoActivo && Time.time >= finEscudo)
            {
                TerminarEscudo();
            }
            if (embistiendo)
            {
                AvanzarEmbestida(Time.deltaTime);
            }
        }

        private void AlLlegarAKO()
        {
            TerminarEscudo();
            embistiendo = false;
        }

        // Proyectil

        private void LanzarProyectiles(DatosHabilidad habilidad, float potencia, Vector3 origen, Vector3 frente)
        {
            float[] angulos = ReglaEfectosHabilidad.AngulosSalva(habilidad.Cantidad, habilidad.Dispersion);
            foreach (float angulo in angulos)
            {
                Vector3 direccion = Quaternion.AngleAxis(angulo, Vector3.up) * frente;
                ProyectilHabilidad.Crear(origen, direccion, habilidad, habilidad.Danio * potencia, transform, presentacion);
            }
        }

        // Cono

        private void LanzarCono(DatosHabilidad habilidad, float potencia, Vector3 origen, Vector3 frente)
        {
            DestelloCono.Crear(origen, frente, habilidad.AlcanceCono, habilidad.Angulo, habilidad.ColorRama, presentacion);
            float danio = habilidad.Danio * potencia;
            raicesGolpeadas.Clear();
            int cantidad = Physics.OverlapSphereNonAlloc(origen, habilidad.AlcanceCono, cercanos, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cantidad; i++)
            {
                Collider colision = cercanos[i];
                Transform raiz = colision.transform.root;
                if (raiz == transform.root || raicesGolpeadas.Contains(raiz))
                {
                    continue;
                }
                IRecibeDanio recibe = colision.GetComponentInParent<IRecibeDanio>();
                Vector3 cercano = colision.bounds.ClosestPoint(origen);
                if (recibe == null || !ReglaEfectosHabilidad.EnCono(origen, frente, cercano, habilidad.AlcanceCono, habilidad.Angulo))
                {
                    continue;
                }
                raicesGolpeadas.Add(raiz);
                AplicarConoA(habilidad, danio, origen, cercano, recibe, raiz);
            }
        }

        private void AplicarConoA(DatosHabilidad habilidad, float danio, Vector3 origen, Vector3 punto, IRecibeDanio recibe, Transform raiz)
        {
            Vector3 hacia = punto - origen;
            hacia.y = 0f;
            Vector3 direccion = hacia.sqrMagnitude > 1e-6f ? hacia.normalized : transform.forward;
            InfoDanio info = new InfoDanio(danio, TipoDanio.Golpe, punto, direccion, peleador);
            if (habilidad.RompeGuardia && recibe is VidaMecha vidaGolpeada)
            {
                vidaGolpeada.RecibirDanioSinGuardia(info);
            }
            else
            {
                recibe.RecibirDanio(info);
            }
            if (habilidad.Aturdir > 0f)
            {
                PeleadorMecha golpeado = raiz.GetComponentInChildren<PeleadorMecha>();
                if (golpeado != null)
                {
                    golpeado.Aturdir(habilidad.Aturdir);
                }
            }
            EmpujeMecha.Aplicar(raiz.gameObject, direccion, habilidad.Empuje, presentacion.DuracionEmpuje);
        }

        // Escudo

        private void ActivarEscudo(DatosHabilidad habilidad, float potencia)
        {
            vida.FactorEscudo = 1f - habilidad.Reduccion;
            finEscudo = Mathf.Max(escudoActivo ? finEscudo : 0f, Time.time + habilidad.Duracion * potencia);
            escudoActivo = true;
            if (escudoVisual == null)
            {
                Vector3 centro = controlador != null ? controlador.center : Vector3.up;
                escudoVisual = EscudoVisual.Crear(transform, centro, presentacion);
            }
        }

        private void TerminarEscudo()
        {
            if (vida != null)
            {
                vida.FactorEscudo = 1f;
            }
            escudoActivo = false;
            if (escudoVisual != null)
            {
                Destroy(escudoVisual.gameObject);
                escudoVisual = null;
            }
        }

        // Embestida

        private void EmpezarEmbestida(DatosHabilidad habilidad, float potencia)
        {
            embestida = habilidad;
            potenciaEmbestida = potencia;
            direccionEmbestida = transform.forward;
            direccionEmbestida.y = 0f;
            direccionEmbestida = direccionEmbestida.sqrMagnitude > 1e-6f ? direccionEmbestida.normalized : Vector3.forward;
            recorridoEmbestida = 0f;
            tiempoEmbestida = 0f;
            contacto = null;
            embistiendo = true;
        }

        // El controlador avisa cada vez que Move choca. Solo cuentan los choques de lado con algo que no sea el propio mecha.
        private void OnControllerColliderHit(ControllerColliderHit choque)
        {
            if (!embistiendo || contacto != null || Mathf.Abs(choque.normal.y) > MaxInclinacionContacto
                || choque.collider.transform.root == transform.root)
            {
                return;
            }
            contacto = choque.collider;
            puntoContacto = choque.point;
        }

        private void AvanzarEmbestida(float dt)
        {
            if (controlador == null || !controlador.enabled)
            {
                embistiendo = false;
                return;
            }
            tiempoEmbestida += dt;
            float paso = ReglaEfectosHabilidad.PasoEmbestida(embestida.Distancia, embestida.DuracionEmbestida, recorridoEmbestida, dt);
            controlador.Move(direccionEmbestida * paso + Vector3.up * (VelocidadEnPiso * dt));
            recorridoEmbestida += paso;

            if (contacto != null)
            {
                IRecibeDanio recibe = contacto.GetComponentInParent<IRecibeDanio>();
                if (recibe != null)
                {
                    GolpearConEmbestida(recibe, contacto, puntoContacto);
                }
                contacto = null;
                embistiendo = false;
                return;
            }
            if (recorridoEmbestida >= embestida.Distancia - 1e-4f || tiempoEmbestida >= embestida.DuracionEmbestida)
            {
                embistiendo = false;
            }
        }

        private void GolpearConEmbestida(IRecibeDanio recibe, Collider golpeado, Vector3 punto)
        {
            InfoDanio info = new InfoDanio(embestida.Danio * potenciaEmbestida, TipoDanio.Golpe, punto, direccionEmbestida, peleador);
            recibe.RecibirDanio(info);
            EmpujeMecha.Aplicar(golpeado.transform.root.gameObject, direccionEmbestida, embestida.Empuje, presentacion.DuracionEmpuje);
        }
    }
}
