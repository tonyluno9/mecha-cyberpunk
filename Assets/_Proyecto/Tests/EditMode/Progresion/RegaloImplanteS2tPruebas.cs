using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-864 (playtest 6-nov tarde): el Doc regala un implante sin cobrar ni pedir liga (ServicioImplantes.Regalar).</summary>
    public class RegaloImplanteS2tPruebas
    {
        private const string RutaRegalo = "Assets/_Proyecto/Progresion/Datos/Implantes/DatosImplante_implante_piernas_1.asset";

        [Test]
        public void Regalar_SinCartera_LoInstalaUnaVez()
        {
            GameObject go = new GameObject("Implantes");
            try
            {
                ServicioImplantes s = go.AddComponent<ServicioImplantes>();
                DatosImplante d = AssetDatabase.LoadAssetAtPath<DatosImplante>(RutaRegalo);
                Assert.IsNotNull(d);
                int avisos = 0;
                s.Instalado += (e, n) => avisos++;
                Assert.AreEqual(ResultadoImplante.Instalado, s.Regalar(d));
                Assert.AreEqual(1, s.Nivel(EspacioImplante.Piernas));
                Assert.AreEqual(1, s.Cantidad);
                Assert.AreEqual(ResultadoImplante.YaInstalado, s.Regalar(d), "no se repite");
                Assert.AreEqual(1, avisos);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
