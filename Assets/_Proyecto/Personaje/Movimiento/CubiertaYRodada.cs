using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Movimiento
{
    /// <summary>
    /// Cubrirse y rodar a pie (T-421). Lo crea <see cref="ControladorAPie"/>. Q pega al jugador a la cubierta más cercana (pared, carro, caja) y lo
    /// deja agachado; al apuntar se asoma (de pie, sin protección) y puede disparar; sale con Q o caminando lejos de ella. CTRL rueda en la dirección
    /// del movimiento con unos segundos de invulnerabilidad y un enfriamiento. Sin animaciones nuevas: baja la cápsula y la cámara y encoge y gira el muñeco.
    /// Los números salen de <see cref="DatosCubierta"/> (Resources/DatosCubierta).
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class CubiertaYRodada : MonoBehaviour
    {
        private const int Direcciones = 8;
        private const float AlturaRayo = 0.5f;
        private const float NormalMaximaVertical = 0.5f;
        private const float HolguraPared = 0.2f;
        private const float SuavizadoVisual = 14f;
        private const float RapidezMinimaDeSalida = 0.5f;

        private ControladorAPie controlador;
        private CharacterController cuerpo;
        private DatosCubierta datos;
        private InputAction accionCubrirse;
        private InputAction accionRodar;
        private readonly RodadaAPie rodada = new RodadaAPie();
        private Collider cubierta;
        private Vector3 normal;
        private Vector3 direccionRodada;
        private Transform modelo;
        private float alturaOriginal;
        private Vector3 centroOriginal;
        private float ajusteCamara;
        private float escalaActual = 1f;

        public DatosCubierta Datos => datos;
        public bool Cubierto => cubierta != null;
        /// <summary>True si está pegado a la cubierta y agachado (no asomado).</summary>
        public bool Agachado => Cubierto && !controlador.Apuntando;
        public bool Rodando => rodada.Activa;
        public bool Invulnerable => rodada.Invulnerable;
        public bool RodadaLista => rodada.Lista;
        public Collider Cubierta => cubierta;
        public Vector3 NormalCubierta => normal;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            cuerpo = GetComponent<CharacterController>();
            datos = Resources.Load<DatosCubierta>("DatosCubierta");
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosCubierta>();
            }
            alturaOriginal = cuerpo.height;
            centroOriginal = cuerpo.center;
            Animator animador = GetComponentInChildren<Animator>(true);
            modelo = animador != null ? animador.transform : null;
            accionCubrirse = new InputAction("Cubrirse", InputActionType.Button, "<Keyboard>/q");
            accionRodar = new InputAction("Rodar", InputActionType.Button, "<Keyboard>/leftCtrl");
        }

        private readonly SeguidorImplantes implantes = new SeguidorImplantes();

        private void OnEnable()
        {
            implantes.Activar();
            accionCubrirse.Enable();
            accionRodar.Enable();
        }

        private void OnDisable()
        {
            implantes.Desactivar();
            accionCubrirse.Disable();
            accionRodar.Disable();
            cubierta = null;
            Reiniciar();
        }

        private void OnDestroy()
        {
            accionCubrirse?.Dispose();
            accionRodar?.Dispose();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            rodada.Avanzar(dt);
            if (!controlador.ControlActivo)
            {
                cubierta = null;
                Reiniciar();
                return;
            }

            if (rodada.Activa)
            {
                controlador.FactorVelocidad = 0f;
                controlador.DesplazamientoExterno = direccionRodada * datos.RapidezRodada;
                ActualizarVisual(dt);
                return;
            }
            if (!Cubierto && cuerpo.height != alturaOriginal)
            {
                Reiniciar();
            }

            if (accionRodar.WasPressedThisFrame())
            {
                Rodar();
                return;
            }
            if (accionCubrirse.WasPressedThisFrame())
            {
                if (Cubierto)
                {
                    Soltar();
                }
                else
                {
                    BuscarCubierta();
                }
            }

            if (Cubierto)
            {
                MantenerCubierta();
            }
            else
            {
                controlador.FactorVelocidad = 1f;
                controlador.DesplazamientoExterno = Vector3.zero;
            }
            ActualizarVisual(dt);
        }

        /// <summary>Intenta rodar ya (CTRL). Devuelve false si está en enfriamiento.</summary>
        public bool Rodar()
        {
            if (!rodada.Iniciar(datos.DuracionRodada, datos.Invulnerabilidad,
                EfectosAPie.EnfriamientoRodada(datos.EnfriamientoRodada, implantes.Efectos)))
            {
                return false;
            }
            cubierta = null;
            Quaternion marco = Quaternion.Euler(0f, controlador.GiroCamara, 0f);
            Vector3 mundo = marco * new Vector3(controlador.Velocidad.x, 0f, controlador.Velocidad.y);
            direccionRodada = mundo.sqrMagnitude > 0.04f ? mundo.normalized : transform.forward;
            direccionRodada.y = 0f;
            direccionRodada.Normalize();
            transform.rotation = Quaternion.LookRotation(direccionRodada, Vector3.up);
            PonerAltura(datos.AlturaRodando);
            Sonidos.ReproducirEn("JUG_SALTO", transform.position);
            return true;
        }

        /// <summary>Busca la cubierta más cercana y se pega a ella. Devuelve false si no hay ninguna.</summary>
        public bool BuscarCubierta()
        {
            Vector3 origen = transform.position + Vector3.up * AlturaRayo;
            float mejor = float.MaxValue;
            Collider elegida = null;
            Vector3 normalElegida = Vector3.zero;
            for (int i = 0; i < Direcciones; i++)
            {
                Vector3 direccion = Quaternion.Euler(0f, i * 360f / Direcciones, 0f) * Vector3.forward;
                RaycastHit[] golpes = Physics.RaycastAll(origen, direccion, datos.DistanciaBuscar, ~0, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit golpe in golpes)
                {
                    if (golpe.distance >= mejor || !EsCubierta(golpe))
                    {
                        continue;
                    }
                    mejor = golpe.distance;
                    elegida = golpe.collider;
                    normalElegida = golpe.normal;
                }
            }
            if (elegida == null)
            {
                return false;
            }
            normalElegida.y = 0f;
            cubierta = elegida;
            normal = normalElegida.normalized;
            PonerAltura(datos.AlturaAgachado);
            return true;
        }

        /// <summary>Suelta la cubierta y se levanta.</summary>
        public void Soltar()
        {
            if (cubierta == null)
            {
                return;
            }
            cubierta = null;
            if (!rodada.Activa)
            {
                Reiniciar();
            }
        }

        /// <summary>True si una bala disparada desde <paramref name="origen"/> pega en la cubierta (raycast contra su collider).</summary>
        public bool ProtegeDe(Vector3 origen, out Vector3 punto)
        {
            punto = Vector3.zero;
            if (!Agachado)
            {
                return false;
            }
            if (!ReglaCubierta.Protege(origen - transform.position, normal, datos.CosenoProteccion))
            {
                return false;
            }
            Vector3 blanco = transform.position + Vector3.up * (datos.AlturaAgachado * 0.6f);
            Vector3 hacia = blanco - origen;
            float distancia = hacia.magnitude;
            if (distancia < 0.001f)
            {
                return false;
            }
            if (cubierta.Raycast(new Ray(origen, hacia / distancia), out RaycastHit golpe, distancia))
            {
                punto = golpe.point;
                return true;
            }
            return false;
        }

        private bool EsCubierta(RaycastHit golpe)
        {
            Collider c = golpe.collider;
            if (c.transform.root == transform.root || Mathf.Abs(golpe.normal.y) > NormalMaximaVertical)
            {
                return false;
            }
            if (c.GetComponentInParent<IRecibeDanio>() != null || c.GetComponentInParent<CharacterController>() != null)
            {
                return false;
            }
            return c.bounds.max.y >= transform.position.y + datos.AlturaMinimaCubierta;
        }

        private void MantenerCubierta()
        {
            Quaternion marco = Quaternion.Euler(0f, controlador.GiroCamara, 0f);
            Vector3 movimiento = marco * new Vector3(controlador.Velocidad.x, 0f, controlador.Velocidad.y);
            if (controlador.Velocidad.magnitude > RapidezMinimaDeSalida && ReglaCubierta.DebeSalir(movimiento, normal, datos.CosenoSalida))
            {
                Soltar();
                controlador.FactorVelocidad = 1f;
                controlador.DesplazamientoExterno = Vector3.zero;
                return;
            }
            controlador.FactorVelocidad = datos.FactorVelocidad;
            // Se queda pegado a la cubierta: si se separó, camina hacia ella.
            Vector3 origen = transform.position + Vector3.up * AlturaRayo;
            Vector3 pegar = Vector3.zero;
            if (cubierta.Raycast(new Ray(origen, -normal), out RaycastHit golpe, datos.DistanciaBuscar + 0.5f)
                && golpe.distance > cuerpo.radius + HolguraPared)
            {
                pegar = -normal * datos.RapidezPegarse;
            }
            controlador.DesplazamientoExterno = pegar;
        }

        private void PonerAltura(float altura)
        {
            cuerpo.height = altura;
            cuerpo.center = new Vector3(centroOriginal.x, altura / 2f, centroOriginal.z);
        }

        private void Reiniciar()
        {
            if (cuerpo != null)
            {
                cuerpo.height = alturaOriginal;
                cuerpo.center = centroOriginal;
            }
            if (controlador != null)
            {
                controlador.FactorVelocidad = 1f;
                controlador.DesplazamientoExterno = Vector3.zero;
                controlador.AjusteAlturaCamara = 0f;
            }
            ajusteCamara = 0f;
            if (modelo != null)
            {
                modelo.localScale = Vector3.one;
                modelo.localRotation = Quaternion.identity;
                modelo.localPosition = Vector3.zero;
            }
            escalaActual = 1f;
        }

        // Cámara y muñeco: baja la cámara y encoge el muñeco agachado; al rodar lo gira hacia adelante alrededor de su centro.
        private void ActualizarVisual(float dt)
        {
            float paso = 1f - Mathf.Exp(-SuavizadoVisual * dt);
            bool agachado = Agachado || rodada.Activa;
            ajusteCamara = Mathf.Lerp(ajusteCamara, agachado ? datos.BajadaCamara : 0f, paso);
            controlador.AjusteAlturaCamara = ajusteCamara;
            if (modelo == null)
            {
                return;
            }
            escalaActual = Mathf.Lerp(escalaActual, agachado ? datos.EscalaAgachado : 1f, paso);
            modelo.localScale = new Vector3(1f, escalaActual, 1f);
            if (rodada.Activa)
            {
                Vector3 centro = new Vector3(0f, datos.AlturaRodando / 2f, 0f);
                Quaternion giro = Quaternion.Euler(rodada.Progreso * 360f, 0f, 0f);
                modelo.localRotation = giro;
                modelo.localPosition = centro - giro * centro;
            }
            else
            {
                modelo.localRotation = Quaternion.identity;
                modelo.localPosition = Vector3.zero;
            }
        }
    }
}
