using System.Collections;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>
    /// Carro estacionado que se puede robar (T-401). Va junto a un <see cref="CarroViejo"/>. La primera vez que el jugador sube
    /// suena la cerradura forzada, suma calor una sola vez y el carro queda como suyo. T-696: ya no hay luces de alarma ni sonido de carro robado.
    /// </summary>
    [RequireComponent(typeof(CarroViejo))]
    public class CarroEstacionado : MonoBehaviour
    {
        private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");
        private static readonly int IdColor = Shader.PropertyToID("_Color");

        [SerializeField] private float calorAlRobar = 30f;
        [Tooltip("Color de la carrocería (alfa 0 deja el original).")]
        [SerializeField] private Color tinte = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private bool esTuyo;

        private CarroViejo carro;

        /// <summary>True si ya es del jugador (no vuelve a sumar calor).</summary>
        public bool EsTuyo => esTuyo;

        /// <summary>Color de la carrocería; se aplica con un MaterialPropertyBlock.</summary>
        public void PonerTinte(Color color)
        {
            tinte = color;
            AplicarTinte();
        }

        private void Awake()
        {
            carro = GetComponent<CarroViejo>();
            AplicarTinte();
        }

        private void OnEnable()
        {
            carro.OcupadoCambio += AlCambiarOcupado;
        }

        private void OnDisable()
        {
            if (carro != null)
            {
                carro.OcupadoCambio -= AlCambiarOcupado;
            }
        }

        private void AplicarTinte()
        {
            if (tinte.a <= 0f)
            {
                return;
            }
            foreach (Renderer renderizador in GetComponentsInChildren<Renderer>(true))
            {
                if (renderizador.name != "Carroceria")
                {
                    continue;
                }
                MaterialPropertyBlock bloque = new MaterialPropertyBlock();
                renderizador.GetPropertyBlock(bloque);
                bloque.SetColor(IdColorBase, tinte);
                bloque.SetColor(IdColor, tinte);
                renderizador.SetPropertyBlock(bloque);
            }
        }

        // T-704: un carro parado no debe flotar. Se mide qué tan arriba del suelo quedan sus ruedas (la parte más baja de su modelo) con un rayo
        // hacia abajo, y si flota más que la tolerancia se baja hasta tocar. Al nacer (en la escena) y al estacionar (el jugador lo deja).
        public const float ToleranciaFlotar = 0.15f;
        private const float MaximoQueFlota = 4f;
        private const float SegundosParaRevisarAlEstacionar = 1.5f;
        private readonly RaycastHit[] golpesSuelo = new RaycastHit[8];

        private void Start()
        {
            PegarAlSuelo();
        }

        /// <summary>Cuánto flota el carro (m) sobre el suelo bajo él; 0 si no hay suelo debajo o no tiene modelo visible.</summary>
        public float MedirFlotacion()
        {
            bool hay = false;
            Bounds limites = new Bounds(transform.position, Vector3.zero);
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !(r is MeshRenderer || r is SkinnedMeshRenderer))
                {
                    continue;
                }
                if (!hay)
                {
                    limites = r.bounds;
                    hay = true;
                }
                else
                {
                    limites.Encapsulate(r.bounds);
                }
            }
            if (!hay)
            {
                return 0f;
            }
            Vector3 origen = new Vector3(limites.center.x, limites.min.y + 0.3f, limites.center.z);
            int n = Physics.RaycastNonAlloc(origen, Vector3.down, golpesSuelo, MaximoQueFlota, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float menor = float.MaxValue;
            float sueloY = float.NaN;
            for (int i = 0; i < n; i++)
            {
                Collider c = golpesSuelo[i].collider;
                if (c == null || c.transform.IsChildOf(transform) || c.attachedRigidbody != null)
                {
                    continue;
                }
                if (golpesSuelo[i].distance < menor)
                {
                    menor = golpesSuelo[i].distance;
                    sueloY = golpesSuelo[i].point.y;
                }
            }
            return float.IsNaN(sueloY) ? 0f : Mathf.Max(0f, limites.min.y - sueloY);
        }

        /// <summary>Si las ruedas flotan más que <see cref="ToleranciaFlotar"/>, baja el carro hasta el suelo. Devuelve si lo movió.</summary>
        public bool PegarAlSuelo()
        {
            if (carro == null)
            {
                carro = GetComponent<CarroViejo>();
            }
            float flota = MedirFlotacion();
            if (flota <= ToleranciaFlotar)
            {
                return false;
            }
            Rigidbody cuerpo = GetComponent<Rigidbody>();
            if (cuerpo != null)
            {
                cuerpo.position += Vector3.down * flota;
            }
            transform.position += Vector3.down * flota;
            Physics.SyncTransforms();
            return true;
        }

        private IEnumerator RevisarAlEstacionar()
        {
            yield return new WaitForSeconds(SegundosParaRevisarAlEstacionar);
            if (carro != null && !carro.Ocupado && carro.Rapidez < 0.3f)
            {
                PegarAlSuelo();
            }
        }

        private void AlCambiarOcupado(bool ocupado)
        {
            // T-450: la puerta suena al subir y al bajar; la primera vez (robo) también la cerradura forzada.
            Sonidos.ReproducirEn(ocupado ? "VEH_PUERTA_ABRIR" : "VEH_PUERTA_CERRAR", transform.position);
            if (!ocupado)
            {
                StartCoroutine(RevisarAlEstacionar());
            }
            if (!ocupado || esTuyo)
            {
                return;
            }
            Sonidos.ReproducirEn("VEH_FORZAR_PUERTA", transform.position);
            esTuyo = true;
            ICalor calor = Registro<ICalor>.Actual;
            if (calor != null)
            {
                calor.Sumar(calorAlRobar, MotivoCalor.Otro);
            }
        }
    }
}
