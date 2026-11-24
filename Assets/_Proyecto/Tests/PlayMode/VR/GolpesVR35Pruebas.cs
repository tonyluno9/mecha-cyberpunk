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
    /// VR35: golpes con las manos fiables (nudillos, todas las capas y triggers, desde 0.8 m/s) y sujetar a un SKULL con el grip izquierdo sin
    /// implante y pegarle con la derecha hasta derribarlo (al caer se suelta solo).
    /// </summary>
    public class GolpesVR35Pruebas : BaseJugadorVR
    {
        private const string RutaSkull = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab";
        private const int CapaIgnoreRaycast = 2;
        private static readonly Vector3 ManoIzqInicio = new Vector3(-0.2f, 1.2f, 0.3f);

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

        [UnityTest]
        public IEnumerator GolpeAUnMetroPorSegundo_ContraUnTriggerEnIgnoreRaycast_LeBajaVida()
        {
            yield return ArmarVR();
            GolpesManosVR golpes = jugador.GetComponent<GolpesManosVR>();
            Assert.IsNotNull(golpes, "GolpesManosVR debe agregarse solo al armarse el rig.");
            PonerMano(false, ManoIzqInicio, Quaternion.identity);
            yield return Segundos(0.3f);

            // Un blanco que solo es un trigger en la capa Ignore Raycast (antes la máscara y el filtro de triggers lo dejaban fuera).
            GameObject blanco = new GameObject("BlancoVR35");
            extras.Add(blanco);
            blanco.layer = CapaIgnoreRaycast;
            blanco.transform.position = adaptador.ManoIzquierda.Nudillos + jugador.transform.forward * 0.45f;
            BoxCollider caja = blanco.AddComponent<BoxCollider>();
            caja.size = Vector3.one * 0.3f;
            caja.isTrigger = true;
            BlancoDanioVR35 vida = blanco.AddComponent<BlancoDanioVR35>();
            Physics.SyncTransforms();
            yield return Cuadros(2);

            // 0.5 m en 0.5 s: 1 m/s (el umbral es 0.8 m/s).
            yield return MoverMano(false, ManoIzqInicio, ManoIzqInicio + new Vector3(0f, 0f, 0.5f), 0.5f, Quaternion.identity);
            yield return Cuadros(2);
            Debug.Log("[VR] VR35 golpe a 1 m/s: " + golpes.Golpes + " golpes, " + golpes.UltimaVelocidad.ToString("0.00") + " m/s, daño " + vida.Danio.ToString("0.0"));
            Assert.GreaterOrEqual(golpes.Golpes, 1, "Un golpe a 1 m/s contra el blanco cuenta.");
            Assert.Greater(vida.Danio, 0f, "El golpe le baja vida.");
            Assert.IsFalse(golpes.UltimoConDerecha);
        }

        [UnityTest]
        public IEnumerator SinImplante_SujetarConGripIzquierdo_YGolpearConLaDerecha_LoDerribaYSeSuelta()
        {
            yield return ArmarVR();
            AgarreImplanteVR agarre = jugador.GetComponent<AgarreImplanteVR>();
            GolpesManosVR golpes = jugador.GetComponent<GolpesManosVR>();
            Assert.IsNotNull(agarre);
            Assert.IsNotNull(golpes);
            GameObject molde = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSkull);
            Assert.IsNotNull(molde, "Falta el prefab " + RutaSkull);
            skull = Object.Instantiate(molde, new Vector3(-0.2f, 0.05f, 0.75f), Quaternion.Euler(0f, 180f, 0f)).GetComponent<PandilleroAPie>();
            Assert.IsNotNull(skull);
            PonerMano(false, ManoIzqInicio, Quaternion.identity);
            yield return Cuadros(4);

            Mantener(controlIzq.gripButton);
            yield return Cuadros(2);
            CuerpoAgarradoVR cuerpo = agarre.Sostenido(false);
            Assert.IsNotNull(cuerpo, "Sin implante el grip izquierdo cerca del SKULL lo sujeta.");
            Assert.IsFalse(skull.enabled, "Su IA se pausa.");

            Vector3 desde = new Vector3(0.3f, 1.3f, 0.2f);
            Vector3 hasta = new Vector3(-0.05f, 1.3f, 0.85f);
            for (int intento = 0; intento < 20 && !skull.EstaKO; intento++)
            {
                PonerMano(true, desde, Quaternion.identity);
                yield return Segundos(0.35f);
                yield return MoverMano(true, desde, hasta, 0.15f, Quaternion.identity);
                yield return Cuadros(2);
            }
            Debug.Log("[VR] VR35 sujetar y golpear: " + golpes.Golpes + " golpes, vida " + skull.Actual + ", KO " + skull.EstaKO);
            Assert.IsTrue(skull.EstaKO, "Golpeándolo con la derecha mientras se sujeta con la izquierda cae.");
            Assert.IsTrue(golpes.UltimoASostenido, "Los golpes fueron al sostenido.");
            yield return Cuadros(2);
            Assert.IsNull(agarre.Sostenido(false), "Al caer se suelta solo.");
            Soltar(controlIzq.gripButton);
            yield return null;
        }
    }

    /// <summary>VR35, para pruebas: blanco que solo suma el daño que recibe.</summary>
    public class BlancoDanioVR35 : MonoBehaviour, IRecibeDanio
    {
        public float Danio { get; private set; }

        public ResultadoDanio RecibirDanio(InfoDanio info)
        {
            Danio += info.Cantidad;
            return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
        }
    }
}
