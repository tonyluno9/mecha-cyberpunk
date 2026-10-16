using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Pruebas
{
    /// <summary>
    /// Herramienta de pruebas: fija una lista de objetivos en el <see cref="ServicioObjetivos"/>. El primero al empezar;
    /// con la tecla configurada (O) o al llegar (si se pide) pasa al siguiente.
    /// </summary>
    public class FijadorObjetivoDePrueba : MonoBehaviour
    {
        [Serializable]
        public struct EntradaObjetivo
        {
            [Tooltip("Texto que ve el jugador.")]
            public string texto;
            [Tooltip("Lugar del objetivo. Se ignora si hay destino.")]
            public IdLugar lugar;
            [Tooltip("Si existe, el objetivo es su posición (m, mundo) al momento de fijarlo.")]
            public Transform destino;
        }

        [SerializeField] private List<EntradaObjetivo> objetivos = new List<EntradaObjetivo>();
        [Tooltip("Si está activo, al llegar al objetivo pasa al siguiente.")]
        [SerializeField] private bool avanzarAlLlegar;
        [SerializeField] private InputAction siguiente = new InputAction("Siguiente", InputActionType.Button, "<Keyboard>/o");

        private ServicioObjetivos servicio;
        private int indice = -1;

        public int Indice => indice;

        private void OnEnable()
        {
            siguiente.performed += AlPulsarSiguiente;
            siguiente.Enable();
            Registro<IObjetivos>.Agregado += AlAgregarServicio;
        }

        private void OnDisable()
        {
            siguiente.performed -= AlPulsarSiguiente;
            siguiente.Disable();
            Registro<IObjetivos>.Agregado -= AlAgregarServicio;
            Desconectar();
        }

        private void Start()
        {
            Conectar(Registro<IObjetivos>.Actual);
        }

        private void AlAgregarServicio(IObjetivos nuevo) => Conectar(nuevo);

        private void Conectar(IObjetivos candidato)
        {
            if (servicio != null || !(candidato is ServicioObjetivos encontrado)) return;
            servicio = encontrado;
            servicio.Seguimiento.Llegada += AlLlegar;
            if (indice < 0) Pasar(0);
        }

        private void Desconectar()
        {
            if (servicio == null) return;
            if (servicio.Seguimiento != null) servicio.Seguimiento.Llegada -= AlLlegar;
            servicio = null;
        }

        private void AlPulsarSiguiente(InputAction.CallbackContext contexto) => Siguiente();

        private void AlLlegar()
        {
            if (avanzarAlLlegar) Siguiente();
        }

        /// <summary>Pasa al siguiente objetivo de la lista; en el último se queda.</summary>
        public void Siguiente()
        {
            if (indice + 1 < objetivos.Count) Pasar(indice + 1);
        }

        private void Pasar(int nuevo)
        {
            if (servicio == null || nuevo < 0 || nuevo >= objetivos.Count) return;
            indice = nuevo;
            EntradaObjetivo entrada = objetivos[nuevo];
            servicio.Fijar(entrada.destino != null
                ? new Objetivo(entrada.texto, entrada.destino.position)
                : new Objetivo(entrada.texto, entrada.lugar));
        }
    }
}
