using MechaCyberpunk.Vehiculos.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-1336: la moto estacionada no tiene por delante nada bajo (barril, bolardo, barrera) que la ataje; el barrido ve lo que el de los carros no veía.</summary>
    public class MotoEstacionadaT1336Pruebas
    {
        private const string RutaV08 = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V08.prefab";
        private GameObject moto;
        private GameObject estorbo;

        [TearDown]
        public void Limpiar()
        {
            if (moto != null) { Object.DestroyImmediate(moto); }
            if (estorbo != null) { Object.DestroyImmediate(estorbo); }
        }

        private void Armar(float altoEstorbo)
        {
            moto = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaV08));
            moto.transform.SetPositionAndRotation(new Vector3(5000f, 0f, 5000f), Quaternion.identity);
            if (altoEstorbo > 0f)
            {
                estorbo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                estorbo.transform.localScale = new Vector3(2f, altoEstorbo, 0.4f);
                estorbo.transform.position = new Vector3(5000f, altoEstorbo * 0.5f, 5004f);
            }
        }

        [Test]
        public void AdelanteLibre_VeUnBarrilBajoALos4Metros()
        {
            Armar(0.9f);
            Assert.IsFalse(MotoEstacionadaT1336.AdelanteLibre(moto.transform, out string choque));
            StringAssert.Contains("m", choque);
        }

        [Test]
        public void AdelanteLibre_SinEstorbo_EstaLibre()
        {
            Armar(0f);
            Assert.IsTrue(MotoEstacionadaT1336.AdelanteLibre(moto.transform, out _));
        }

        [Test]
        public void CarroceriaDelV08_QuedaSobreElFondoDeLasRuedas()
        {
            Armar(0f);
            BoxCollider caja = moto.transform.Find("Carroceria").GetComponent<BoxCollider>();
            float fondo = caja.transform.TransformPoint(caja.center).y - caja.size.y * caja.transform.lossyScale.y * 0.5f;
            Assert.GreaterOrEqual(fondo - moto.transform.position.y, 0.3f, "El cuerpo de la moto no roza el piso");
        }

        [Test]
        public void Ciudad_CadaMotoEstacionada_TieneDiezMetrosLibresAlFrente()
        {
            SceneSetup[] antes = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EditorSceneManager.OpenScene(MotoEstacionadaT1336.RutaCiudad, OpenSceneMode.Single);
                var motos = MotoEstacionadaT1336.Motos();
                Assert.IsNotEmpty(motos, "La ciudad trae al menos una moto estacionada");
                foreach (var m in motos)
                {
                    Assert.IsTrue(MotoEstacionadaT1336.AdelanteLibre(m.transform, out string choque), m.name + " tiene por delante: " + choque);
                }
            }
            finally
            {
                if (antes.Length > 0) { EditorSceneManager.RestoreSceneManagerSetup(antes); }
            }
        }
    }
}
