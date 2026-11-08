using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Lugares
{
    /// <summary>
    /// T-890: puerta que se abre con E a cualquier hora (el consultorio del Doc: antes no tenía puerta y se entraba siempre; lo que cierra de
    /// noche es el mostrador, no el cuarto). Cumple <c>IPuerta</c> y mueve su <see cref="HojaPuerta"/>.
    /// </summary>
    public class PuertaSinHorario : MonoBehaviour, IPuerta
    {
        [SerializeField] private IdLugar lugar = IdLugar.Clinica;
        [SerializeField] private HojaPuerta hoja;

        public IdLugar Lugar => lugar;
        public bool EstaAbierta => hoja.EstaAbierta;
        public Vector3 Posicion => transform.position;

        public event Action<bool> Cambio;

        private void Awake()
        {
            if (hoja == null)
            {
                Debug.LogError("[Mundo] PuertaSinHorario en '" + name + "' necesita su hoja.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (hoja != null)
            {
                hoja.Cambio += AlCambiarHoja;
            }
            Registro<IPuerta>.Agregar(this);
        }

        private void OnDisable()
        {
            if (hoja != null)
            {
                hoja.Cambio -= AlCambiarHoja;
            }
            Registro<IPuerta>.Quitar(this);
        }

        public ResultadoPuerta IntentarAbrir()
        {
            if (hoja.EstaAbierta)
            {
                return ResultadoPuerta.YaAbierta;
            }
            hoja.Abrir();
            return ResultadoPuerta.Abierta;
        }

        public void Cerrar()
        {
            hoja.Cerrar();
        }

        private void AlCambiarHoja(bool abierta)
        {
            Cambio?.Invoke(abierta);
        }
    }
}
