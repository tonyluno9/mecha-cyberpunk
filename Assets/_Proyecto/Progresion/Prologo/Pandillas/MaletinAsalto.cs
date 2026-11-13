using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Pandillas
{
    /// <summary>
    /// T-987: el maletín que sueltan los asaltantes al detenerse su carro. Un modelo simple hecho con primitivas (caja café oscura con asa y un brillo tenue para verlo de noche)
    /// que cae un momento hasta el piso. Es un <see cref="IInteractuable"/>: el jugador a pie lo recoge con E. <see cref="AsaltoDinero"/> lo crea, lo vigila y paga.
    /// </summary>
    public class MaletinAsalto : MonoBehaviour, IInteractuable
    {
        public const string TextoRecoger = "Recoger maletín";
        public const string Nombre = "MaletinAsalto";

        private static readonly Vector3 Medidas = new Vector3(0.45f, 0.12f, 0.33f);
        private const float AlturaCaida = 0.6f;
        private const float SegundosCaida = 0.45f;
        private const float DistanciaMaximaBajoElPiso = 30f;

        private bool registrado;
        private bool recogido;
        private float inicioCaida;
        private float alturaFinal;

        /// <summary>Se dispara una sola vez cuando el jugador lo recoge con E.</summary>
        public event Action Recogido;

        public Vector3 Posicion => transform.position;
        public string Texto => TextoRecoger;
        public bool PuedeUsar => !recogido;
        public bool FueRecogido => recogido;

        /// <summary>Un punto del piso (en el mundo) bajo <paramref name="punto"/> sin contar los colliders de <paramref name="ignorar"/>. False si no hay piso.</summary>
        public static bool BuscarPiso(Vector3 punto, Transform ignorar, out Vector3 piso)
        {
            piso = punto;
            RaycastHit[] golpes = Physics.RaycastAll(punto + Vector3.up * 2f, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            bool hay = false;
            foreach (RaycastHit g in golpes)
            {
                if (g.distance >= mejor || (ignorar != null && g.collider.transform.root == ignorar.root))
                {
                    continue;
                }
                mejor = g.distance;
                piso = g.point;
                hay = true;
            }
            return hay;
        }

        /// <summary>Crea un maletín que cae hasta <paramref name="piso"/> (un punto del suelo).</summary>
        public static MaletinAsalto Crear(Vector3 piso, float giroY)
        {
            GameObject raiz = new GameObject(Nombre);
            raiz.transform.SetPositionAndRotation(piso + Vector3.up * AlturaCaida, Quaternion.Euler(0f, giroY, 0f));
            MaletinAsalto m = raiz.AddComponent<MaletinAsalto>();
            m.alturaFinal = piso.y;
            m.inicioCaida = Time.time;
            m.Armar();
            return m;
        }

        /// <summary>True si el maletín está muy por debajo de la referencia (cayó fuera del mundo).</summary>
        public bool EstaPerdido(Vector3 referencia)
        {
            return float.IsNaN(transform.position.y) || transform.position.y < referencia.y - DistanciaMaximaBajoElPiso;
        }

        private void Armar()
        {
            Shader sombreador = Shader.Find("Universal Render Pipeline/Lit");
            if (sombreador == null)
            {
                sombreador = Shader.Find("Standard");
            }
            Material cafe = new Material(sombreador) { name = "MaletinAsalto_Cafe" };
            Color color = new Color(0.22f, 0.11f, 0.05f);
            cafe.SetColor("_BaseColor", color);
            cafe.EnableKeyword("_EMISSION");
            cafe.SetColor("_EmissionColor", new Color(0.55f, 0.25f, 0.04f));
            Material metal = new Material(sombreador) { name = "MaletinAsalto_Metal" };
            metal.SetColor("_BaseColor", new Color(0.6f, 0.55f, 0.4f));
            metal.EnableKeyword("_EMISSION");
            metal.SetColor("_EmissionColor", new Color(0.5f, 0.4f, 0.1f));

            Parte("Caja", new Vector3(0f, Medidas.y * 0.5f, 0f), Medidas, cafe);
            float yAsa = Medidas.y + 0.02f;
            Parte("AsaIzquierda", new Vector3(-0.07f, yAsa, 0f), new Vector3(0.025f, 0.05f, 0.025f), metal);
            Parte("AsaDerecha", new Vector3(0.07f, yAsa, 0f), new Vector3(0.025f, 0.05f, 0.025f), metal);
            Parte("AsaBarra", new Vector3(0f, yAsa + 0.03f, 0f), new Vector3(0.17f, 0.025f, 0.025f), metal);
            Parte("Broche", new Vector3(0f, Medidas.y, Medidas.z * 0.5f), new Vector3(0.06f, 0.03f, 0.02f), metal);

            // Una luz puntual pequeña para que se vea de noche.
            GameObject luzObjeto = new GameObject("Brillo");
            luzObjeto.transform.SetParent(transform, false);
            luzObjeto.transform.localPosition = Vector3.up * 0.35f;
            Light luz = luzObjeto.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.7f, 0.3f);
            luz.range = 3.5f;
            luz.intensity = 1.2f;
        }

        private void Parte(string nombre, Vector3 local, Vector3 escala, Material material)
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = nombre;
            Collider c = cubo.GetComponent<Collider>();
            if (c != null)
            {
                Destroy(c); // sin collider: no estorba ni se interpone al disparar
            }
            cubo.transform.SetParent(transform, false);
            cubo.transform.localPosition = local;
            cubo.transform.localScale = escala;
            cubo.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void OnEnable()
        {
            Registro<IInteractuable>.Agregar(this);
            registrado = true;
        }

        private void OnDisable()
        {
            if (registrado)
            {
                Registro<IInteractuable>.Quitar(this);
                registrado = false;
            }
        }

        private void Update()
        {
            float t = Mathf.Clamp01((Time.time - inicioCaida) / SegundosCaida);
            Vector3 p = transform.position;
            p.y = alturaFinal + AlturaCaida * (1f - t * t);
            transform.position = p;
        }

        public void Usar()
        {
            if (recogido)
            {
                return;
            }
            recogido = true;
            Recogido?.Invoke();
        }
    }
}
