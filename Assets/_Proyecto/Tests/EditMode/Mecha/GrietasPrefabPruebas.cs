using System.Collections.Generic;
using MechaCyberpunk.Mecha.Cabina;
using MechaCyberpunk.Mecha.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>T-105: las grietas del prefab del jugador dejan libre el centro del cristal y el creador es determinista.</summary>
    public class GrietasPrefabPruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaCopia = "Assets/_Proyecto/Tests/EditMode/Mecha/Temp_MechaJugadorCopia.prefab";
        private const float MitadCentroLibre = 0.2f;
        private const float PasoMuestreo = 0.01f;

        private GameObject raiz;

        [SetUp]
        public void SetUp()
        {
            raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
        }

        [TearDown]
        public void TearDown()
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }

        private CristalCuarteado Cristal()
        {
            return raiz.GetComponentInChildren<CristalCuarteado>(true);
        }

        // Todos los puntos de todas las líneas de un grupo, en coordenadas locales del cristal.
        private static List<Vector3[]> Lineas(Transform cristal, string grupo)
        {
            List<Vector3[]> resultado = new List<Vector3[]>();
            foreach (LineRenderer linea in cristal.Find(grupo).GetComponentsInChildren<LineRenderer>(true))
            {
                Vector3[] puntos = new Vector3[linea.positionCount];
                for (int i = 0; i < puntos.Length; i++)
                {
                    Vector3 mundo = linea.transform.TransformPoint(linea.GetPosition(i));
                    puntos[i] = cristal.InverseTransformPoint(mundo);
                }
                resultado.Add(puntos);
            }
            return resultado;
        }

        private static bool EnCentro(Vector3 p)
        {
            return Mathf.Abs(p.x) < MitadCentroLibre && Mathf.Abs(p.y) < MitadCentroLibre;
        }

        [Test]
        public void Prefab_TieneCristalCuarteadoConLosDosGrupos()
        {
            CristalCuarteado cristal = Cristal();
            Assert.IsNotNull(cristal, "MechaJugador lleva CristalCuarteado");
            Assert.IsNotNull(cristal.transform.Find("Grietas1"));
            Assert.IsNotNull(cristal.transform.Find("Grietas2"));
            Assert.GreaterOrEqual(Lineas(cristal.transform, "Grietas1").Count, 3);
            Assert.GreaterOrEqual(Lineas(cristal.transform, "Grietas2").Count, 5);
        }

        [Test]
        public void Grietas_NingunVerticeNiTramoEntraAlCentro()
        {
            Transform cristal = Cristal().transform;
            foreach (string grupo in new[] { "Grietas1", "Grietas2" })
            {
                foreach (Vector3[] linea in Lineas(cristal, grupo))
                {
                    Assert.GreaterOrEqual(linea.Length, 2, "Una grieta necesita dos puntos");
                    for (int i = 0; i < linea.Length; i++)
                    {
                        Assert.IsFalse(EnCentro(linea[i]), grupo + ": un vértice cae en el centro: " + linea[i]);
                        if (i == 0)
                        {
                            continue;
                        }
                        int pasos = Mathf.CeilToInt(Vector3.Distance(linea[i - 1], linea[i]) / PasoMuestreo);
                        for (int k = 0; k <= pasos; k++)
                        {
                            Vector3 punto = Vector3.Lerp(linea[i - 1], linea[i], pasos == 0 ? 0f : (float)k / pasos);
                            Assert.IsFalse(EnCentro(punto), grupo + ": un tramo cruza el centro en " + punto);
                        }
                    }
                }
            }
        }

        [Test]
        public void Grietas_QuedanDelLadoDelPilotoDelCristal()
        {
            Transform cristal = Cristal().transform;
            foreach (Vector3[] linea in Lineas(cristal, "Grietas2"))
            {
                foreach (Vector3 punto in linea)
                {
                    Assert.Less(punto.z, 0f, "Las grietas van delante del cristal, hacia el piloto");
                }
            }
        }

        [Test]
        public void Creador_CorridoDosVeces_DejaLosMismosVertices()
        {
            // Trabaja sobre una copia temporal: correr las pruebas no debe reescribir el prefab versionado.
            AssetDatabase.DeleteAsset(RutaCopia);
            Assert.IsTrue(AssetDatabase.CopyAsset(RutaPrefab, RutaCopia), "No se pudo copiar el prefab");
            try
            {
                CreadorDanioCabina.Crear(RutaCopia);
                string antes = Huella(RutaCopia);
                CreadorDanioCabina.Crear(RutaCopia);
                CreadorDanioCabina.Crear(RutaCopia);
                Assert.AreEqual(antes, Huella(RutaCopia), "El creador cambió las grietas al correr otra vez");
            }
            finally
            {
                AssetDatabase.DeleteAsset(RutaCopia);
            }
        }

        // Todos los vértices de los dos grupos, en orden, como texto.
        private static string Huella(string ruta)
        {
            GameObject copia = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                Transform cristal = copia.GetComponentInChildren<CristalCuarteado>(true).transform;
                System.Text.StringBuilder texto = new System.Text.StringBuilder();
                foreach (string grupo in new[] { "Grietas1", "Grietas2" })
                {
                    foreach (Vector3[] linea in Lineas(cristal, grupo))
                    {
                        foreach (Vector3 p in linea)
                        {
                            texto.Append(p.x.ToString("R")).Append(',').Append(p.y.ToString("R")).Append(',').Append(p.z.ToString("R")).Append(';');
                        }
                        texto.Append('|');
                    }
                }
                return texto.ToString();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(copia);
            }
        }
    }
}
