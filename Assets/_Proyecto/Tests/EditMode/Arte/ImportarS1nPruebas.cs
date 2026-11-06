using System.IO;
using MechaCyberpunk.Arte.Editor;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Arte
{
    /// <summary>T-678: el importador de la sesión 1 mete cada archivo solo cuando aparece y correrlo otra vez no cambia nada.</summary>
    public class ImportarS1nPruebas
    {
        [Test]
        public void Buscar_ArchivoQueNoExiste_DevuelveNull()
        {
            Assert.IsNull(ImportarS1n.Buscar("modelos3d/NoExiste_XYZ.zip"));
        }

        [Test]
        public void Importador_DosVeces_LaSegundaNoCopiaNada()
        {
            Assume.That(ImportarS1n.Buscar("modelos3d/W02.zip"), Is.Not.Null, "Sin imageassets/ no hay nada que comprobar");
            ImportarS1n.MeterTodo();
            ImportarS1n.MeterTodo();
            string informe = ImportarS1n.Informe.ToString();
            StringAssert.DoesNotContain("copiado:", informe, "La segunda pasada no vuelve a copiar archivos iguales");
            StringAssert.DoesNotContain("ERROR", informe);
        }

        [Test]
        public void Armas_W02_W03_W04_TraenSuModeloYSuTextura()
        {
            foreach (ImportarS1n.Arma arma in ImportarS1n.Armas)
            {
                Assume.That(ImportarS1n.Buscar(arma.Zip) != null || File.Exists(arma.Fbx), arma.Codigo + " todavía no llega");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(arma.Fbx), "Modelo " + arma.Fbx);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(arma.Texturas + arma.Codigo + "_basecolor.JPEG"), "Color de " + arma.Codigo);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(arma.Texturas + arma.Codigo + "_normal.JPEG"), "Normal de " + arma.Codigo);
            }
        }

        [Test]
        public void Destello_F01_QuedaAsignadoAlasDatosDeSensacion()
        {
            Assume.That(File.Exists(ImportarS1n.RutaDestello), "F01 todavía no llega");
            DatosSensacion datos = AssetDatabase.LoadAssetAtPath<DatosSensacion>(ImportarS1n.RutaDatosSensacion);
            Assert.IsNotNull(datos);
            Assert.AreEqual(ImportarS1n.RutaDestello, AssetDatabase.GetAssetPath(datos.TexturaDestello));
        }

        [Test]
        public void Hologramas_H02_H04_EstanEnElProyectoYEnElHud()
        {
            foreach (string archivo in ImportarS2nHologramas.Archivos)
            {
                Assume.That(File.Exists(ImportarS2nHologramas.CarpetaDestino + archivo), archivo + " todavía no llega");
            }
            PiezasUI piezas = AssetDatabase.LoadAssetAtPath<PiezasUI>(ImportarS2nHologramas.RutaPiezas);
            Assert.IsNotNull(piezas);
            SerializedProperty lista = new SerializedObject(piezas).FindProperty("armasHolograma");
            Assert.GreaterOrEqual(lista.arraySize, 5, "Puños + 4 armas");
            for (int i = 1; i <= 4; i++)
            {
                Assert.IsNotNull(lista.GetArrayElementAtIndex(i).objectReferenceValue, "Holograma del arma " + i + " en el HUD");
            }
        }
    }
}
