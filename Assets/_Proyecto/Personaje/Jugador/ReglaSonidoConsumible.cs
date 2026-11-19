using System.Collections.Generic;
using MechaCyberpunk.Core;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// T-1474: qué sonidos pide un consumible al usarse, según su id. Lógica pura (sin MonoBehaviour): <see cref="Ids"/> devuelve la lista en orden
    /// y <see cref="Sonar"/> la pide por la fachada <c>Sonidos</c> en 2D (es el propio jugador).
    /// </summary>
    public static class ReglaSonidoConsumible
    {
        public const string Beber = "CON_BEBER";
        public const string LataAbrir = "CON_LATA_ABRIR";
        public const string PapitasComer = "CON_PAPITAS_COMER";
        public const string Venda = "CON_VENDA";
        public const string Curacion = "CON_CURACION";

        private const string IdBebidaEnergetica = "bebida_energetica";
        private const string IdCafe = "cafe";
        private const string IdPapitas = "papitas";
        private const string FragmentoBotiquin = "botiquin";
        private const string FragmentoVenda = "venda";

        /// <summary>Los sonidos del consumible, en orden: la lata se abre antes de beber. Id vacío: ninguno.</summary>
        public static IReadOnlyList<string> Ids(string idConsumible)
        {
            var lista = new List<string>();
            if (string.IsNullOrEmpty(idConsumible))
            {
                return lista;
            }
            string id = idConsumible.ToLowerInvariant();
            if (id == IdBebidaEnergetica)
            {
                lista.Add(LataAbrir);
                lista.Add(Beber);
            }
            else if (id == IdCafe)
            {
                lista.Add(Beber);
            }
            else if (id == IdPapitas)
            {
                lista.Add(PapitasComer);
            }
            else if (id.Contains(FragmentoVenda) || id.Contains(FragmentoBotiquin))
            {
                lista.Add(Venda);
            }
            else
            {
                lista.Add(Curacion); // batería de escudo y cualquier otro: el sonido de curación (por revisar a oído)
            }
            return lista;
        }

        /// <summary>Pide los sonidos del consumible en 2D.</summary>
        public static void Sonar(string idConsumible)
        {
            foreach (string id in Ids(idConsumible))
            {
                Sonidos.Reproducir(id);
            }
        }
    }
}
