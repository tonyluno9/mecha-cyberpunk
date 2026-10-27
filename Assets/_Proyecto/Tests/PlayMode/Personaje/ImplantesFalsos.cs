using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using UnityEditor;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>Doble de IImplantes para pruebas a pie: se le dice qué niveles tiene y qué efectos suman (desde los assets reales).</summary>
    public class ImplantesFalsos : IImplantes
    {
        private const string RutaDatos = "Assets/_Proyecto/Progresion/Datos/Implantes/DatosImplante_implante_";
        private readonly int[] niveles = new int[4];
        private EfectosImplantes efectos = EfectosImplantes.Ninguno;

        public int Cantidad { get { int n = 0; foreach (int x in niveles) if (x > 0) n++; return n; } }
        public EfectosImplantes Efectos => efectos;
        public event Action Cambio;
        public event Action<EspacioImplante, int> Instalado;

        public int Nivel(EspacioImplante espacio) => niveles[(int)espacio];
        public string Nombre(EspacioImplante espacio) => niveles[(int)espacio] > 0 ? "FALSO" : string.Empty;

        /// <summary>Asset real del implante de ese espacio y nivel.</summary>
        public static DatosImplante Datos(EspacioImplante espacio, int nivel)
        {
            string nombre = espacio.ToString().ToLowerInvariant();
            return AssetDatabase.LoadAssetAtPath<DatosImplante>(RutaDatos + nombre + "_" + nivel + ".asset");
        }

        /// <summary>Instala (nivel 0 quita) y combina los efectos de lo instalado, con los valores de los assets.</summary>
        public void Poner(EspacioImplante espacio, int nivel)
        {
            niveles[(int)espacio] = nivel;
            efectos = EfectosImplantes.Ninguno;
            for (int i = 0; i < niveles.Length; i++)
            {
                if (niveles[i] > 0) efectos = EfectosImplantes.Combinar(efectos, Datos((EspacioImplante)i, niveles[i]).Efectos);
            }
            Cambio?.Invoke();
            if (nivel > 0) Instalado?.Invoke(espacio, nivel);
        }
    }
}
