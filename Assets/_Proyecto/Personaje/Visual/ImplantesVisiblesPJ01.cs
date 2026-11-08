using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual
{
    /// <summary>
    /// T-740 (A9): muestra en PJ01 la pieza de cada implante instalado según su nivel en <see cref="IImplantes"/> (Core): IMP01 piernas (Piernas), IMP02 brazos (Brazos) e IMP03 visor de cabeza
    /// (Ojos), cada uno con su nivel 1 y 2; sistema nervioso no tiene pieza. Solo se ve la pieza del nivel instalado (la del nivel 2 reemplaza a la del 1). Las piezas son SkinnedMesh con
    /// los mismos 78 huesos de PJ01 (ver CreadorImplantesPJ01T740), así que siguen cualquier clip y salen en el espejo como el resto del cuerpo. Lee el mismo servicio y en el mismo
    /// momento que HudImplantes, así que el muñequito del HUD y el cuerpo van en sincronía.
    /// </summary>
    public class ImplantesVisiblesPJ01 : MonoBehaviour
    {
        [Serializable]
        public struct Pieza
        {
            public EspacioImplante espacio;
            public int nivel;
            public GameObject objeto;
        }

        [SerializeField] private Pieza[] piezas = Array.Empty<Pieza>();

        private int[] aplicados = new int[4];

        public Pieza[] Piezas => piezas;

        /// <summary>Para los creadores: las piezas del cuerpo.</summary>
        public void Configurar(Pieza[] nuevas) => piezas = nuevas;

        private void OnEnable()
        {
            for (int i = 0; i < aplicados.Length; i++) { aplicados[i] = -1; }
            Aplicar();
        }

        private void Update() => Aplicar();

        private void Aplicar()
        {
            IImplantes servicio = Registro<IImplantes>.Actual;
            for (int e = 0; e < aplicados.Length; e++)
            {
                int nivel = servicio != null ? servicio.Nivel((EspacioImplante)e) : 0;
                if (nivel == aplicados[e]) { continue; }
                aplicados[e] = nivel;
                foreach (Pieza p in piezas)
                {
                    if ((int)p.espacio == e && p.objeto != null) { p.objeto.SetActive(p.nivel == nivel); }
                }
            }
        }
    }
}
