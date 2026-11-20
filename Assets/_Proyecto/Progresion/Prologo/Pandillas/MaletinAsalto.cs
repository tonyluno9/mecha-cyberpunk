using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Pandillas
{
    /// <summary>
    /// T-987: el maletín que sueltan los asaltantes al detenerse su carro. El modelo es el MA01 (Resources) con un brillo tenue para verlo de noche;
    /// que cae un momento hasta el piso. Es un <see cref="IInteractuable"/>: el jugador a pie lo recoge con E. <see cref="AsaltoDinero"/> lo crea, lo vigila y paga.
    /// </summary>
    public class MaletinAsalto : MonoBehaviour, IInteractuable
    {
        public const string TextoRecoger = "Recoger maletín";
        public const string Nombre = "MaletinAsalto";
        /// <summary>Prefab del modelo MA01 en Resources (lo crea ImportarMaletinT1499 en Arte/Editor). Sin él no hay modelo (T-1523 quitó el respaldo de cubos).</summary>
        public const string RecursoModelo = "MaletinAsalto";

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
            GameObject modelo = Resources.Load<GameObject>(RecursoModelo);
            if (modelo != null)
            {
                // T-1499: el maletín MA01 (0.45 m, con su collider). Solo se agrega el brillo para verlo de noche.
                GameObject instancia = Instantiate(modelo, transform);
                instancia.name = "Modelo";
                instancia.transform.localPosition = Vector3.zero;
                instancia.transform.localRotation = Quaternion.identity;
                AgregarBrillo();
                return;
            }
            // Sin el prefab no se inventa un cubo (T-1523): solo queda el brillo y un aviso, y el maletín se puede recoger igual.
            Debug.LogError("[Progresion] Falta Resources/" + RecursoModelo + " (MA01): el maletín no tiene modelo.", this);
            AgregarBrillo();
        }

        private void AgregarBrillo()
        {
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
            Sonidos.ReproducirEn("INT_MALETIN_CERRAR", transform.position); // T-1474
            Recogido?.Invoke();
        }
    }
}
