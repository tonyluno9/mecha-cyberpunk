using System;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Mecha.Estado
{
    /// <summary>
    /// Hace que el daño del mecha del jugador se quede entre peleas y se guarde (clave <c>mecha</c>).
    /// Si la vida queda por debajo del piso (o en KO), la siguiente pelea lo levanta a ese piso.
    /// </summary>
    [RequireComponent(typeof(VidaMecha))]
    [RequireComponent(typeof(PeleadorMecha))]
    public class MechaPersistente : MonoBehaviour, IGuardable
    {
        public const string Clave = "mecha";

        [Serializable]
        private class DatosGuardados
        {
            public float vida;
            public float vidaMaxima;
        }

        [SerializeField] private ConfigMechaDaniado config;

        private VidaMecha vida;
        private PeleadorMecha peleador;
        private bool registrado;

        public string ClaveGuardado => Clave;

        /// <summary>Vida máxima menos la actual.</summary>
        public float Danio => vida != null ? vida.Maxima - vida.Actual : 0f;

        /// <summary>Avisa cuando <see cref="Reparar"/> deja la vida completa.</summary>
        public event Action Reparado;

        private void Awake()
        {
            vida = GetComponent<VidaMecha>();
            peleador = GetComponent<PeleadorMecha>();
            if (config == null)
            {
                Debug.LogError("[Mecha] MechaPersistente sin ConfigMechaDaniado en " + name, this);
                enabled = false;
                return;
            }
            peleador.ConservarVidaAlPreparar = true;
        }

        private void OnEnable()
        {
            if (config == null)
            {
                return;
            }
            peleador.PeleaPreparada += AlPrepararPelea;
            foreach (IGuardable otro in Registro<IGuardable>.Todos)
            {
                if (otro.ClaveGuardado == Clave)
                {
                    Debug.LogWarning("[Mecha] Ya hay un mecha registrado para el guardado; " + name + " no se registra.", this);
                    return;
                }
            }
            Registro<IGuardable>.Agregar(this);
            registrado = true;
        }

        private void OnDisable()
        {
            if (peleador != null)
            {
                peleador.PeleaPreparada -= AlPrepararPelea;
            }
            if (registrado)
            {
                Registro<IGuardable>.Quitar(this);
                registrado = false;
            }
        }

        private void AlPrepararPelea()
        {
            float objetivo = ReglaVidaPersistente.AlPreparar(vida.Actual, vida.Maxima, config.PisoVidaPersistente);
            if (vida.EstaKO || objetivo > vida.Actual)
            {
                vida.FijarVida(objetivo);
            }
        }

        /// <summary>Deja la vida completa y quita el KO.</summary>
        public void Reparar()
        {
            vida.Restaurar();
            Reparado?.Invoke();
        }

        public string Guardar()
        {
            return JsonUtility.ToJson(new DatosGuardados { vida = vida.Actual, vidaMaxima = vida.Maxima });
        }

        public void Cargar(string datos)
        {
            DatosGuardados leidos = null;
            try
            {
                leidos = string.IsNullOrWhiteSpace(datos) ? null : JsonUtility.FromJson<DatosGuardados>(datos);
            }
            catch (ArgumentException)
            {
                leidos = null;
            }
            if (leidos == null || leidos.vidaMaxima <= 0f)
            {
                Debug.LogWarning("[Mecha] No se entendió el estado guardado del mecha; no cambia nada.");
                return;
            }
            float objetivo = ReglaVidaPersistente.AlCargar(leidos.vida, leidos.vidaMaxima, vida.Maxima, config.PisoVidaPersistente);
            vida.FijarVida(objetivo);
        }
    }
}
