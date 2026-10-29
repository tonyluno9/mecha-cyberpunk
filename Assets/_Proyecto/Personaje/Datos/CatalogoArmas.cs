using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;

namespace MechaCyberpunk.Personaje
{
    /// <summary>Lista de las 16 armas a pie (4 armas por 4 niveles) (T-403).</summary>
    [CreateAssetMenu(menuName = "Mecha Cyberpunk/Catálogo de armas", fileName = "CatalogoArmas_Nuevo")]
    public class CatalogoArmas : ScriptableObject
    {
        [SerializeField] private List<DatosArma> armas = new List<DatosArma>();

        public IReadOnlyList<DatosArma> Armas => armas;

        /// <summary>El arma en ese nivel, o null.</summary>
        public DatosArma Buscar(ArmaAPie arma, int nivel)
        {
            foreach (DatosArma a in armas)
            {
                if (a != null && a.Arma == arma && a.Nivel == nivel) return a;
            }
            return null;
        }

        /// <summary>Crea un catálogo en memoria (para pruebas).</summary>
        public static CatalogoArmas Crear(params DatosArma[] lista)
        {
            var c = CreateInstance<CatalogoArmas>();
            c.armas = new List<DatosArma>(lista);
            return c;
        }
    }
}
