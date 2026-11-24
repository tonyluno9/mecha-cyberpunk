using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR.Menus;
using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR5/VR19: el stick derecho en vertical. Desde VR19 ya NO cambia de arma (las armas van en la rueda: mantener X). Mantener el stick derecho
    /// abajo 1 s recentra la vista (<see cref="AdaptadorVR.Recentrar"/>); el horizontal es el giro (<see cref="AdaptadorVR"/>). Las reglas están en
    /// <see cref="ReglaStickArmaVR"/>. Con un menú abierto o en pausa no hace nada. Lo agrega <see cref="ArranqueCombateVR"/> (el nombre se queda
    /// por compatibilidad).
    /// </summary>
    public class CambioArmaStickVR : MonoBehaviour
    {
        private readonly ReglaStickArmaVR regla = new ReglaStickArmaVR();
        private AdaptadorVR adaptador;

        /// <summary>Cuántas veces pidió recentrar (para las pruebas).</summary>
        public int Recentrados { get; private set; }

        /// <summary>Cambios de arma con el stick: siempre 0 desde VR19 (queda para las pruebas).</summary>
        public int Cambios => 0;

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            if (adaptador == null || GetComponent<ArmasAPie>() == null)
            {
                Debug.LogWarning("[VR] CambioArmaStickVR sin AdaptadorVR o ArmasAPie en " + name + ".", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (adaptador == null)
            {
                Destroy(this);
                return;
            }
            if (!adaptador.Armado)
            {
                return;
            }
            if (MenusVR.MenuAbierto || Pausa.Activa)
            {
                regla.Cancelar();
                return;
            }
            if (regla.Avanzar(EntradaVR.Eje(EntradaVR.StickDerecho), Time.unscaledDeltaTime) == GestoStickVR.Recentrar)
            {
                Recentrados++;
                AdaptadorVR.Actual?.Recentrar();
            }
        }
    }
}
