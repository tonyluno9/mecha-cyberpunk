using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR.Menus;
using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR5: el stick derecho en vertical cambia de arma entre las que tiene el jugador (<see cref="ArmasAPie"/>): arriba la siguiente al instante,
    /// abajo la anterior al soltar (toque de menos de 0.5 s). Mantener abajo 1 s recentra la vista (<see cref="AdaptadorVR.Recentrar"/>) y no
    /// cambia de arma. Las reglas están en <see cref="ReglaStickArmaVR"/>. Con un menú abierto o en pausa no hace nada. Lo agrega
    /// <see cref="ArranqueCombateVR"/>.
    /// </summary>
    public class CambioArmaStickVR : MonoBehaviour
    {
        private readonly ReglaStickArmaVR regla = new ReglaStickArmaVR();
        private AdaptadorVR adaptador;
        private ArmasAPie armas;

        /// <summary>Cuántas veces pidió recentrar (para las pruebas).</summary>
        public int Recentrados { get; private set; }

        /// <summary>Cuántas veces cambió de arma con el stick (para las pruebas).</summary>
        public int Cambios { get; private set; }

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            armas = GetComponent<ArmasAPie>();
            if (adaptador == null || armas == null)
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
            GestoStickVR gesto = regla.Avanzar(EntradaVR.Eje(EntradaVR.StickDerecho), Time.unscaledDeltaTime);
            switch (gesto)
            {
                case GestoStickVR.ArmaSiguiente:
                    Cambiar(1);
                    break;
                case GestoStickVR.ArmaAnterior:
                    Cambiar(-1);
                    break;
                case GestoStickVR.Recentrar:
                    Recentrados++;
                    AdaptadorVR.Actual?.Recentrar();
                    break;
            }
        }

        private void Cambiar(int paso)
        {
            if (!armas.PuedeCambiar)
            {
                return;
            }
            ArmaAPie siguiente = ReglaStickArmaVR.Siguiente(armas.ArmaActiva, paso, armas.TieneArma);
            if (siguiente != armas.ArmaActiva && armas.Equipar(siguiente))
            {
                Cambios++;
            }
        }
    }
}
