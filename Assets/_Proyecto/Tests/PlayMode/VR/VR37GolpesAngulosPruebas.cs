using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR37: el golpe cuenta desde cualquier dirección y altura (de frente, de lado, de arriba y al cuerpo) contra un SKULL real, y la regla de la mano
    /// que entra en el enemigo ya frenando.
    /// </summary>
    public class VR37GolpesAngulosPruebas : BaseJugadorVR
    {
        private const string RutaSkull = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";

        private PandilleroAPie skull;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            AgarreImplanteVR.ForzarImplante = false;
        }

        public override void TearDown()
        {
            AgarreImplanteVR.ForzarImplante = null;
            if (skull != null)
            {
                Object.Destroy(skull.gameObject);
            }
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        [Test]
        public void GolpeaAl_ManoQueEntraFrenando_CuentaYTocarDespacioNo()
        {
            Assert.AreEqual(0.13f, ReglaGolpeManoVR.RadioNudillos, 1e-5f);
            Assert.AreEqual(ReglaGolpeManoVR.RadioNudillos, GolpesManosVR.Radio, 1e-5f);
            Assert.IsTrue(ReglaGolpeManoVR.GolpeaAl(0.9f, 0.9f, false), "A más de 0.8 m/s siempre cuenta.");
            Assert.IsTrue(ReglaGolpeManoVR.GolpeaAl(0.3f, 1.5f, true), "La mano que entra tras ir rápida cuenta aunque ya frene.");
            Assert.IsFalse(ReglaGolpeManoVR.GolpeaAl(0.3f, 1.5f, false), "Ya dentro y frenada no vuelve a contar.");
            Assert.IsFalse(ReglaGolpeManoVR.GolpeaAl(0.5f, 0.53f, true), "Tocar despacio no es golpe.");
            Assert.IsFalse(ReglaGolpeManoVR.ValeLaPenaBarrer(0.2f, 0.4f));
            Assert.IsTrue(ReglaGolpeManoVR.ValeLaPenaBarrer(0.2f, 0.7f));
        }

        [UnityTest]
        public IEnumerator DeFrenteDeLadoDeArribaYAlCuerpo_TodosLeBajanVida()
        {
            yield return ArmarVR();
            GolpesManosVR golpes = jugador.GetComponent<GolpesManosVR>();
            Assert.IsNotNull(golpes, "GolpesManosVR debe agregarse solo al armarse el rig.");
            GameObject molde = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkull);
            Assert.IsNotNull(molde, "Falta el prefab " + RutaSkull);
            skull = Object.Instantiate(molde, new Vector3(0f, 0.05f, 0.75f), Quaternion.Euler(0f, 180f, 0f)).GetComponent<PandilleroAPie>();
            Assert.IsNotNull(skull);
            yield return Cuadros(3);
            skull.enabled = false; // quieto: solo recibe los golpes
            PonerMano(false, new Vector3(-0.3f, 1.0f, 0.1f), Quaternion.identity);

            (string nombre, Vector3 desde, Vector3 hasta)[] golpesPrueba =
            {
                ("de frente", new Vector3(0.15f, 1.35f, 0.1f), new Vector3(0.05f, 1.35f, 0.65f)),
                ("de lado (gancho)", new Vector3(0.6f, 1.35f, 0.6f), new Vector3(-0.1f, 1.35f, 0.6f)),
                ("de arriba", new Vector3(0.05f, 2.2f, 0.6f), new Vector3(0.05f, 1.6f, 0.65f)),
                ("al cuerpo", new Vector3(0.2f, 0.9f, 0.1f), new Vector3(0.05f, 1.0f, 0.6f)),
            };
            foreach ((string nombre, Vector3 desde, Vector3 hasta) g in golpesPrueba)
            {
                if (skull.EstaKO)
                {
                    break;
                }
                PonerMano(true, g.desde, Quaternion.identity);
                yield return Segundos(0.45f);
                int antes = golpes.Golpes;
                float vidaAntes = skull.Actual;
                yield return MoverMano(true, g.desde, g.hasta, 0.15f, Quaternion.identity);
                yield return Cuadros(2);
                Debug.Log("[VR] VR37 golpe " + g.nombre + ": " + (golpes.Golpes - antes) + " golpes, vida " + vidaAntes + " -> " + skull.Actual);
                Assert.Greater(golpes.Golpes, antes, "El golpe " + g.nombre + " debe contar.");
                Assert.IsTrue(skull.Actual < vidaAntes || skull.EstaKO, "El golpe " + g.nombre + " debe bajarle vida.");
                Assert.IsTrue(golpes.UltimoConDerecha);
            }
        }
    }
}
