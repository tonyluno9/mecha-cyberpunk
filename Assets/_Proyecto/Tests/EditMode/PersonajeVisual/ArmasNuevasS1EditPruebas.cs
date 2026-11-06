using System.IO;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Arte
{
    /// <summary>T-679: las tres armas nuevas tienen prefab de mano y de piso, y están registradas en el catálogo y en los datos del banco de Tino.</summary>
    public class ArmasNuevasS1EditPruebas
    {
        [Test]
        public void Prefabs_DeManoYDePiso_ConLaBocaEnLaPunta()
        {
            foreach (ColocarS1nArmas.Medida m in ColocarS1nArmas.Medidas)
            {
                Assume.That(File.Exists(m.RutaFbx), m.Codigo + " todavía no llega");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(m.RutaPrefab);
                Assert.IsNotNull(prefab, m.RutaPrefab);
                Transform boca = prefab.transform.Find("Boca");
                Assert.IsNotNull(boca, "Boca en " + m.Codigo);
                Assert.Greater(boca.localPosition.z, 0.5f, "El cañón apunta a +Z y mide más de medio metro desde la empuñadura");
                GameObject piso = AssetDatabase.LoadAssetAtPath<GameObject>(m.RutaPiso);
                Assert.IsNotNull(piso, m.RutaPiso);
                ArmaEnPiso enPiso = piso.GetComponent<ArmaEnPiso>();
                Assert.IsNotNull(enPiso);
                Assert.AreEqual(m.Arma, enPiso.Arma);
                Assert.IsTrue(enPiso.Texto.Contains(m.Articulo.Split(' ')[1]), "Texto de recoger: " + enPiso.Texto);
            }
        }

        [Test]
        public void Catalogo_TieneLasCuatroArmasConSusCuatroNiveles()
        {
            CatalogoArmas catalogo = AssetDatabase.LoadAssetAtPath<CatalogoArmas>("Assets/_Proyecto/Personaje/Datos/CatalogoArmas_Base.asset");
            Assert.IsNotNull(catalogo);
            foreach (ArmaAPie arma in new[] { ArmaAPie.Pistola, ArmaAPie.Escopeta, ArmaAPie.Fusil, ArmaAPie.Sniper })
            {
                for (int nivel = 0; nivel < 4; nivel++)
                {
                    Assert.IsNotNull(catalogo.Buscar(arma, nivel), arma + " nivel " + nivel);
                }
            }
        }

        [Test]
        public void BancoDeTino_TieneLasCuatroArmasConIconoEImagenHolografica()
        {
            ConfigMenuArmas config = AssetDatabase.LoadAssetAtPath<ConfigMenuArmas>("Assets/_Proyecto/UI/Datos/ConfigMenuArmas_Base.asset");
            Assert.IsNotNull(config);
            Assert.AreEqual(4, config.Armas.Count);
            ArmaAPie[] esperadas = { ArmaAPie.Pistola, ArmaAPie.Escopeta, ArmaAPie.Fusil, ArmaAPie.Sniper };
            for (int i = 0; i < esperadas.Length; i++)
            {
                Assert.AreEqual(esperadas[i], config.Armas[i].Arma);
                Assert.IsNotNull(config.Armas[i].Icono, "Icono de " + esperadas[i]);
                Assert.IsNotNull(config.Armas[i].Imagen, "Imagen de " + esperadas[i]);
            }
        }
    }
}
