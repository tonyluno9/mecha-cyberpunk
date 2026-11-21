using UnityEngine;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Dinero soltado por un enemigo (T-686): un fajo que gira y flota; se recoge solo con pasar cerca (sin tecla) y abona a la cartera.
    /// Modelo provisional hecho en código (un fajo brillante); para arte: cambiarlo por un billete o ficha real.
    /// </summary>
    public class DineroEnPiso : MonoBehaviour
    {
        /// <summary>Prefab en Resources (lo crea ModelosPickupsT1523 en Personaje/Editor).</summary>
        public const string RecursoModelo = "DineroEnPiso_Modelo";
        private const float RadioRecoger = 1.3f;
        private const float AlturaFlotar = 0.35f;
        private const float AmplitudFlotar = 0.06f;
        private const float GiroPorSegundo = 120f;
        // Un momento después de caer ya se puede recoger: se alcanza a ver caer el dinero aunque el jugador esté pegado.
        private const float SegundosAntesDeRecoger = 0.6f;

        private int cantidad;
        private Transform modelo;
        private bool tomado;
        private float recogibleDesde;

        public int Cantidad => cantidad;
        public bool FueTomado => tomado;

        /// <summary>Crea un fajo con esa cantidad en el piso.</summary>
        public static DineroEnPiso Crear(Vector3 posicionPiso, int cantidad)
        {
            var raiz = new GameObject("DineroEnPiso_" + cantidad);
            raiz.transform.position = posicionPiso;
            var dinero = raiz.AddComponent<DineroEnPiso>();
            dinero.cantidad = cantidad;
            dinero.recogibleDesde = Time.time + SegundosAntesDeRecoger;
            dinero.Armar();
            return dinero;
        }

        private void Armar()
        {
            Color verde = new Color(0.2f, 1f, 0.55f);
            modelo = new GameObject("Fajo").transform;
            modelo.SetParent(transform, false);
            GameObject prefab = Resources.Load<GameObject>(RecursoModelo);
            if (prefab != null)
            {
                // T-1523: modelo real (el maletín MA01 a escala de pickup; no hay un modelo de billetes en Assets/Arte).
                GameObject instancia = Instantiate(prefab, modelo, false);
                instancia.name = "Maletin";
                foreach (Collider c in instancia.GetComponentsInChildren<Collider>(true))
                {
                    Destroy(c);
                }
            }
            else
            {
                ArmarRespaldo(verde);
            }
            Light luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(transform, false);
            luz.transform.localPosition = Vector3.up * AlturaFlotar;
            luz.type = LightType.Point;
            luz.color = verde;
            luz.intensity = 0.5f;
            luz.range = 1.2f;
            luz.shadows = LightShadows.None;
        }

        /// <summary>Respaldo si falta el prefab en Resources: el fajo hecho de cubos (solo debería verse si alguien borró el recurso).</summary>
        private void ArmarRespaldo(Color verde)
        {
            Shader sombreador = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(sombreador) { name = "DineroEnPiso" };
            material.SetColor("_BaseColor", verde * 0.5f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", verde * 1.2f);
            for (int i = 0; i < 3; i++)
            {
                GameObject billete = GameObject.CreatePrimitive(PrimitiveType.Cube);
                billete.name = "Billete" + i;
                Destroy(billete.GetComponent<Collider>());
                billete.transform.SetParent(modelo, false);
                billete.transform.localPosition = new Vector3(0f, 0.025f * i, 0f);
                billete.transform.localRotation = Quaternion.Euler(0f, 12f * i, 0f);
                billete.transform.localScale = new Vector3(0.28f, 0.02f, 0.14f);
                billete.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private void Update()
        {
            if (tomado)
            {
                return;
            }
            if (modelo != null)
            {
                modelo.localPosition = Vector3.up * (AlturaFlotar + Mathf.Sin(Time.time * 4f) * AmplitudFlotar);
                modelo.Rotate(0f, GiroPorSegundo * Time.deltaTime, 0f, Space.Self);
            }
            IJugador jugador = Registro<IJugador>.Actual;
            if (Time.time < recogibleDesde || jugador == null || jugador.Raiz == null)
            {
                return;
            }
            Vector3 d = jugador.Raiz.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude <= RadioRecoger * RadioRecoger)
            {
                Recoger();
            }
        }

        /// <summary>Abona el dinero a la cartera y desaparece. Se llama solo al pasar cerca.</summary>
        public void Recoger()
        {
            if (tomado)
            {
                return;
            }
            ICartera cartera = Registro<ICartera>.Actual;
            if (cartera == null)
            {
                return;
            }
            tomado = true;
            cartera.Abonar(cantidad, MotivoDinero.Otro);
            Sonidos.ReproducirEn("INT_RECOGER_DINERO", transform.position); // T-1535: el sonido propio de recoger dinero (T-1474 usaba el de recoger objeto)
            Destroy(gameObject);
        }
    }
}
