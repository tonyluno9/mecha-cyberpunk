using MechaCyberpunk.Trafico.Peatones;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// T-705: el conductor al que le roban el carro. Lo sacan del asiento a la puerta, cae al suelo y se levanta asustado. Ya de pie huye
    /// (<see cref="PeatonMundo"/>), sin llamar a nadie (T-933): al levantarse sale corriendo (<see cref="ConductorHuye"/>). Si lo matan se queda tirado y desaparece.
    /// </summary>
    public class ConductorRobado : MonoBehaviour
    {
        public enum FaseConductor
        {
            Sacado,
            Caido,
            Levantandose,
            Asustado,
            Huyendo,
        }

        /// <summary>Lo que tarda en salir del asiento a la puerta (s).</summary>
        public const float SegundosSacar = 0.35f;
        /// <summary>Lo que se queda tirado antes de levantarse (s).</summary>
        public const float SegundosCaido = 0.9f;
        /// <summary>Lo que tarda en levantarse (s).</summary>
        public const float SegundosLevantarse = 1.8f;
        private const float SegundosCuerpo = 10f;
        private const float AlturaBusquedaPiso = 2f;

        private Vector3 desde;
        private Vector3 hasta;
        private Vector3 amenaza;
        private float t;
        private PeatonMundo peaton;
        private Collider capsula;

        public FaseConductor Fase { get; private set; } = FaseConductor.Sacado;

        /// <summary>True mientras está tirado en el piso (lo sacan y cae, hasta que empieza a levantarse).</summary>
        public bool Tirado => peaton != null && !peaton.Muerto && (Fase == FaseConductor.Sacado || Fase == FaseConductor.Caido);

        /// <summary>
        /// Lo saca de <paramref name="asiento"/> a <paramref name="puerta"/> (en el piso) y lo deja caer. <paramref name="haciaQuien"/> es el jugador:
        /// de ahí se asusta y a ese lado mira (o de él huye).
        /// </summary>
        public void Iniciar(Vector3 asiento, Vector3 puerta, Vector3 haciaQuien)
        {
            peaton = GetComponent<PeatonMundo>();
            // T-937: nace dentro del volumen del carro (en el asiento): sin colisión mientras sale, para no empujar al carro ni a sí mismo.
            capsula = GetComponent<Collider>();
            if (capsula != null)
            {
                capsula.enabled = false;
            }
            amenaza = haciaQuien;
            hasta = PegarAlPiso(puerta);
            desde = new Vector3(asiento.x, hasta.y, asiento.z);
            transform.position = desde;
            Vector3 afuera = hasta - desde;
            afuera.y = 0f;
            if (afuera.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(afuera.normalized, Vector3.up);
            }
            // Mensajes de Unity: los atiende VisualPersonaje del modelo activo (Trafico no referencia ese ensamblado).
            BroadcastMessage("Tumbar", SendMessageOptions.DontRequireReceiver);
        }

        private void Update()
        {
            if (peaton != null && peaton.Muerto)
            {
                t += Time.deltaTime;
                if (t >= SegundosCuerpo)
                {
                    Destroy(gameObject);
                }
                return;
            }
            t += Time.deltaTime;
            switch (Fase)
            {
                case FaseConductor.Sacado:
                    transform.position = Vector3.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / SegundosSacar));
                    if (t >= SegundosSacar)
                    {
                        Fase = FaseConductor.Caido;
                        if (capsula != null)
                        {
                            capsula.enabled = true; // ya está afuera, junto a la puerta
                        }
                    }
                    break;
                case FaseConductor.Caido:
                    if (t >= SegundosCaido)
                    {
                        Fase = FaseConductor.Levantandose;
                        BroadcastMessage("Levantar", SendMessageOptions.DontRequireReceiver);
                    }
                    break;
                case FaseConductor.Levantandose:
                    if (t >= SegundosCaido + SegundosLevantarse)
                    {
                        Fase = FaseConductor.Asustado;
                    }
                    break;
                case FaseConductor.Asustado:
                    Huir();
                    break;
            }
        }

        // Ya no llama (terminó o se canceló): sale corriendo lejos del jugador.
        private void Huir()
        {
            Fase = FaseConductor.Huyendo;
            ConductorHuye huida = gameObject.AddComponent<ConductorHuye>();
            huida.Iniciar(amenaza);
            enabled = false;
        }

        /// <summary>Hacia dónde mira ahora (el jugador). Se actualiza mientras dura la llamada.</summary>
        public void PonerAmenaza(Vector3 posicion) => amenaza = posicion;

        private static Vector3 PegarAlPiso(Vector3 punto)
        {
            // El suelo es lo primero sólido que no sea un carro (cuerpo con Rigidbody) ni gente.
            RaycastHit[] golpes = Physics.RaycastAll(punto + Vector3.up * AlturaBusquedaPiso, Vector3.down, AlturaBusquedaPiso * 2f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float menor = float.MaxValue;
            foreach (RaycastHit g in golpes)
            {
                if (g.collider.attachedRigidbody == null && g.collider.GetComponentInParent<PeatonMundo>() == null && g.distance < menor)
                {
                    menor = g.distance;
                    punto.y = g.point.y;
                }
            }
            return punto;
        }
    }
}
