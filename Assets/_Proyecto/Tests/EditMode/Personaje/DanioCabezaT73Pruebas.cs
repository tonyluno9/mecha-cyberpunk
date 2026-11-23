using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1707 (T73): la cabeza hace x2 (x4 con el francotirador). Pandillero aguanta 1 tiro a la cabeza con la pistola y muere con 2, policía 2-3, Nova más; el francotirador mata de uno.</summary>
    public class DanioCabezaT73Pruebas
    {
        private const string Datos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_";

        private static float Vida(string nombre)
        {
            Object o = AssetDatabase.LoadAssetAtPath<Object>(Datos + nombre + ".asset");
            Assert.IsNotNull(o, nombre);
            return new SerializedObject(o).FindProperty("vidaMaxima").floatValue;
        }

        private static float Danio(string arma)
        {
            DatosArma d = AssetDatabase.LoadAssetAtPath<DatosArma>("Assets/_Proyecto/Personaje/Datos/Armas/DatosArma_" + arma + "_0.asset");
            Assert.IsNotNull(d, arma);
            return d.Danio;
        }

        private static int Tiros(float vida, float danioPorTiro) => (int)System.Math.Ceiling(vida / danioPorTiro);

        [Test]
        public void Pistola_Pandillero_AguantaUnaCabezaYMuereConDos()
        {
            float cabeza = Danio("pistola") * ReglaZonaDanio.Multiplicador(true, ArmaAPie.Pistola);
            foreach (string tipo in new[] { "Base", "Calaveras", "Viboras" })
            {
                Assert.AreEqual(2, Tiros(Vida(tipo), cabeza), tipo);
            }
        }

        [Test]
        public void Pistola_Policia_DosATresCabezas_NovaMas()
        {
            float cabeza = Danio("pistola") * ReglaZonaDanio.Multiplicador(true, ArmaAPie.Pistola);
            int policia = Tiros(Vida("Policia"), cabeza);
            Assert.GreaterOrEqual(policia, 2);
            Assert.LessOrEqual(policia, 3);
            Assert.Greater(Tiros(Vida("Nova"), cabeza), policia);
        }

        [Test]
        public void Francotirador_MataDeUnTiroALaCabeza_PandilleroYPolicia()
        {
            for (int nivel = 0; nivel < 4; nivel++)
            {
                DatosArma d = AssetDatabase.LoadAssetAtPath<DatosArma>("Assets/_Proyecto/Personaje/Datos/Armas/DatosArma_sniper_" + nivel + ".asset");
                float cabeza = d.Danio * ReglaZonaDanio.Multiplicador(true, ArmaAPie.Sniper);
                Assert.GreaterOrEqual(cabeza, Vida("Base"), "pandillero nivel " + nivel);
                Assert.GreaterOrEqual(cabeza, Vida("Policia"), "policia nivel " + nivel);
                Assert.Less(d.Danio, Vida("Base"), "al cuerpo no mata, nivel " + nivel);
            }
        }
    }
}
