using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Pruebas
{
    /// <summary>
    /// Doble de prueba de una puerta de Alan (solo para escenas y pruebas de Tony). Sigue el orden de decisión de
    /// <see cref="IPuerta.IntentarAbrir"/>; al abrir, la hoja se desliza a un lado.
    /// </summary>
    public class PuertaDePrueba : MonoBehaviour, IPuerta
    {
        [SerializeField] private LugarDePrueba lugar;
        [Tooltip("Hoja de la puerta (con collider). Se desliza en su eje X local al abrir.")]
        [SerializeField] private Transform hoja;
        [Tooltip("Cuánto se desliza la hoja al abrir (m).")]
        [Min(0.1f)] [SerializeField] private float desplazamiento = 1.3f;
        [Tooltip("Tiempo que tarda en abrir o cerrar (s).")]
        [Min(0.01f)] [SerializeField] private float duracion = 0.3f;

        private Vector3 posicionCerrada;
        private Vector3 posicionAbierta;

        public IdLugar Lugar => lugar != null ? lugar.Id : IdLugar.Ninguno;
        public bool EstaAbierta { get; private set; }
        public Vector3 Posicion => transform.position;

        /// <summary>Cuántas veces se llamó a IntentarAbrir (para las pruebas).</summary>
        public int IntentosAbrir { get; private set; }

        public event Action<bool> Cambio;

        private void Awake()
        {
            if (lugar == null || hoja == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en PuertaDePrueba de " + name, this);
                enabled = false;
                return;
            }
            posicionCerrada = hoja.localPosition;
            posicionAbierta = posicionCerrada + Vector3.right * desplazamiento;
        }

        private void OnEnable()
        {
            lugar.EstadoCambio += AlCambiarLugar;
            Registro<IPuerta>.Agregar(this);
        }

        private void OnDisable()
        {
            lugar.EstadoCambio -= AlCambiarLugar;
            Registro<IPuerta>.Quitar(this);
        }

        private void Update()
        {
            Vector3 objetivo = EstaAbierta ? posicionAbierta : posicionCerrada;
            float paso = desplazamiento / duracion * Time.deltaTime;
            hoja.localPosition = Vector3.MoveTowards(hoja.localPosition, objetivo, paso);
        }

        public ResultadoPuerta IntentarAbrir()
        {
            IntentosAbrir++;
            if (EstaAbierta) return ResultadoPuerta.YaAbierta;
            if (lugar.JugadorDentro)
            {
                Abrir();
                return ResultadoPuerta.Abierta;
            }
            if (lugar.CerradoEnDemo) return ResultadoPuerta.Bloqueada;
            if (!lugar.AbiertoPorHorario) return ResultadoPuerta.CerradaPorHorario;
            Abrir();
            return ResultadoPuerta.Abierta;
        }

        public void Cerrar()
        {
            if (!EstaAbierta) return;
            EstaAbierta = false;
            Cambio?.Invoke(false);
        }

        private void Abrir()
        {
            EstaAbierta = true;
            Cambio?.Invoke(true);
        }

        // El lugar cierra su puerta al cerrar, salvo que el jugador siga dentro (ahí la puerta debe poder usarse para salir).
        private void AlCambiarLugar(bool abierto)
        {
            if (!abierto && !lugar.JugadorDentro) Cerrar();
        }
    }
}
