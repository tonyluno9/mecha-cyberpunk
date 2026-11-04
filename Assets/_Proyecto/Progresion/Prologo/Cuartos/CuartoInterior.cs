using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Cuartos
{
    /// <summary>
    /// T-550: los datos de un cuarto de interior aparte (lejos de la ciudad) y de su lugar. Dice a qué lugar pertenece y dónde se aparece al entrar
    /// y al salir. Lo usan las dos <see cref="PuertaCuarto"/> (la de afuera y la interior). Un cuarto nuevo solo es armar el cuarto y conectar estos dos puntos.
    /// </summary>
    public class CuartoInterior : MonoBehaviour
    {
        [Tooltip("Lugar de la ciudad al que pertenece el cuarto (su LugarMundo con ese Id manda si abre o no).")]
        [SerializeField] private IdLugar lugar = IdLugar.Casa;
        [Tooltip("Punto dentro del cuarto, junto a su puerta interior; su +Z (azul) mira hacia dentro del cuarto.")]
        [SerializeField] private Transform aparicionInterior;
        [Tooltip("Punto afuera, frente a la puerta de la calle; su +Z (azul) mira hacia la calle.")]
        [SerializeField] private Transform salidaExterior;

        public IdLugar Lugar => lugar;
        public Pose PoseInterior => new Pose(aparicionInterior.position, aparicionInterior.rotation);
        public Pose PoseExterior => new Pose(salidaExterior.position, salidaExterior.rotation);

        private void Awake()
        {
            if (lugar == IdLugar.Ninguno || aparicionInterior == null || salidaExterior == null)
            {
                Debug.LogError("[Progresion] CuartoInterior en '" + name + "' necesita su lugar y sus dos puntos de aparición.", this);
                enabled = false;
            }
        }
    }
}
