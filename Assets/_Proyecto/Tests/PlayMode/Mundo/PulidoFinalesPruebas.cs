using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Pruebas;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Pulido de calles y finales (T-098): decorado sin collider, autopista con carros que se mueven, y capturas de los finales.</summary>
    public class PulidoFinalesPruebas
    {
        private const float MitadBarrioZ = 87f;
        private const int MinimoRayas = 100;
        private const float SegundosMovimiento = 1f;
        private const float AlturaOjos = 1.7f;
        private const float SegundosFisica = 0.3f;
        private const float MinimoMovimiento = 5f;

        private GameObject camaraObjeto;

        [TearDown]
        public void TearDown()
        {
            if (camaraObjeto != null)
            {
                Object.DestroyImmediate(camaraObjeto);
            }
            EscenaLugares.Descargar();
        }

        private static Transform Grupo()
        {
            GameObject go = GameObject.Find("PulidoFinales");
            Assert.IsNotNull(go, "El barrio trae el grupo PulidoFinales");
            return go.transform;
        }

        [UnityTest]
        public IEnumerator Pulido_NoTieneColliders_ParaNoCambiarPorDondePasaElCarro()
        {
            yield return EscenaLugares.CargarCiudad();
            Transform grupo = Grupo();
            Assert.AreEqual(0, grupo.GetComponentsInChildren<Collider>(true).Length, "Todo el pulido es decorado");
            Assert.GreaterOrEqual(grupo.Find("PasosDeCebra").childCount, MinimoRayas, "Hay pasos de cebra en los cruces");
            Assert.Greater(grupo.Find("Alcantarillas").childCount, 5);
            Assert.Greater(grupo.Find("Grietas").childCount, 20);
        }

        [UnityTest]
        public IEnumerator Pulido_LaAutopistaEstaAlNorteDelBarrio_YSusCarrosSeMueven()
        {
            yield return EscenaLugares.CargarCiudad();
            Transform autopista = Grupo().Find("Autopista");
            Assert.IsNotNull(autopista);
            Assert.Greater(autopista.Find("Losa").position.z - autopista.Find("Losa").localScale.z / 2f, MitadBarrioZ, "La losa queda fuera del barrio");
            Transform carro = autopista.Find("Carros/Carro_0");
            Assert.IsNotNull(carro);
            float antes = carro.localPosition.x;
            yield return EscenaLugares.Esperar(SegundosMovimiento);
            Assert.Greater(Mathf.Abs(carro.localPosition.x - antes), MinimoMovimiento, "El carro de la autopista avanzó");
        }

        [UnityTest]
        public IEnumerator Pulido_Capturas_CruceAutopistaYObra()
        {
            yield return EscenaLugares.CargarCiudad();
            EscenaLugares.RelojPausado().AvanzarHasta(EscenaLugares.HoraNoche);
            EscenaLugares.ColocarFrenteA(IdLugar.Casa, out camaraObjeto);
            CamaraLibre camara = camaraObjeto.GetComponent<CamaraLibre>();
            (Vector3 desde, Vector3 hacia, string ruta)[] tomas =
            {
                (new Vector3(47f, AlturaOjos, -20f), new Vector3(47f, 1.5f, 10f), "docs/capturas/T-098-1.png"),
                (new Vector3(20f, 18f, 60f), new Vector3(20f, 11f, 103f), "docs/capturas/T-098-2.png"),
                (new Vector3(-100f, AlturaOjos, 0f), new Vector3(-150f, 14f, -10f), "docs/capturas/T-098-3.png")
            };
            foreach (var toma in tomas)
            {
                camara.Colocar(new Pose(toma.desde, Quaternion.LookRotation(toma.hacia - toma.desde, Vector3.up)));
                yield return EscenaLugares.Calentar();
                yield return EscenaLugares.Esperar(SegundosFisica);
                CapturaPruebas.Guardar(camara.Camara, toma.ruta);
            }
        }
    }
}
