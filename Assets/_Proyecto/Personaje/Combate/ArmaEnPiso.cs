using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Un arma tirada en el piso (T-406): con E la recoges y pasa a tu inventario en su nivel base (0), así <see cref="ArsenalJugador"/> la reconoce.
    /// Se registra como <see cref="IInteractuable"/> mientras está en la escena.
    /// T-509: brilla como objeto para recoger: su modelo (los hijos) gira y flota sobre una luz pequeña del color del neón.
    /// </summary>
    public class ArmaEnPiso : MonoBehaviour, IInteractuable
    {
        [SerializeField] private ArmaAPie arma = ArmaAPie.Pistola;
        [SerializeField] private string nombre = "la pistola";
        [Tooltip("Color de la luz de objeto para recoger.")]
        [SerializeField] private Color colorBrillo = new Color(0.2f, 1f, 0.9f);
        [Tooltip("Intensidad de la luz de objeto para recoger.")]
        [SerializeField, Min(0f)] private float intensidadBrillo = 4f;
        [Tooltip("Alcance de la luz (m).")]
        [SerializeField, Min(0f)] private float alcanceBrillo = 2.2f;
        [Tooltip("Giro del modelo (°/s).")]
        [SerializeField, Min(0f)] private float giroPorSegundo = 90f;
        [Tooltip("Altura a la que flota el modelo (m).")]
        [SerializeField, Min(0f)] private float alturaFlotar = 0.25f;
        [Tooltip("Cuánto sube y baja al flotar (m).")]
        [SerializeField, Min(0f)] private float amplitudFlotar = 0.06f;
        [Tooltip("Vueltas por segundo del sube y baja (Hz).")]
        [SerializeField, Min(0f)] private float frecuenciaFlotar = 1.2f;

        private const string NombrePivote = "Brillo_Recoger";
        private Transform pivote;

        private bool tomada;

        public ArmaAPie Arma => arma;
        public bool FueTomada => tomada;
        public Vector3 Posicion => transform.position;
        public string Texto => "Recoger " + nombre;
        public bool PuedeUsar => !tomada;

        /// <summary>Se dispara una vez al recogerla.</summary>
        public event Action<ArmaEnPiso> Tomada;

        private void Awake()
        {
            // El modelo pasa a un pivote que gira y flota; la raíz no se mueve (es el punto de interacción).
            pivote = new GameObject(NombrePivote).transform;
            pivote.SetParent(transform, false);
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform hijo = transform.GetChild(i);
                if (hijo != pivote)
                {
                    hijo.SetParent(pivote, false);
                }
            }
            Light luz = new GameObject("Luz").AddComponent<Light>();
            luz.transform.SetParent(transform, false);
            luz.transform.localPosition = Vector3.up * alturaFlotar;
            luz.type = LightType.Point;
            luz.color = colorBrillo;
            luz.intensity = intensidadBrillo;
            luz.range = alcanceBrillo;
            luz.shadows = LightShadows.None;
        }

        private void Update()
        {
            float fase = Time.time * frecuenciaFlotar * 2f * Mathf.PI;
            pivote.localPosition = Vector3.up * (alturaFlotar + Mathf.Sin(fase) * amplitudFlotar);
            pivote.Rotate(0f, giroPorSegundo * Time.deltaTime, 0f, Space.Self);
        }

        private void OnEnable() => Registro<IInteractuable>.Agregar(this);

        private void OnDisable() => Registro<IInteractuable>.Quitar(this);

        public void Usar()
        {
            if (tomada)
            {
                return;
            }
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            ServicioInventario inventario = Registro<ServicioInventario>.Actual;
            DatosArma datos = arsenal != null && arsenal.Catalogo != null ? arsenal.Catalogo.Buscar(arma, 0) : null;
            if (datos == null || inventario == null)
            {
                Debug.LogWarning("[Personaje] ArmaEnPiso: falta el arsenal, el catálogo o el inventario; no se puede recoger.", this);
                return;
            }
            inventario.Agregar(datos, 1);
            Sonidos.ReproducirEn("ECO_ARMA_RECOGER", transform.position);
            tomada = true;
            Tomada?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
