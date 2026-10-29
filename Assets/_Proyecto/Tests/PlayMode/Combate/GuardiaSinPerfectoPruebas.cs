using System.Collections;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Combate
{
    /// <summary>T-201: GuardiaMecha en mando por código y sin bloqueo perfecto; con el modo normal todo sigue igual.</summary>
    public class GuardiaSinPerfectoPruebas
    {
        private const float Danio = 10f;
        private const float ReduccionBloqueo = 0.7f;
        private const float VentanaPerfecta = 0.3f;
        private const float TiempoSubir = 0.1f;
        private const float EsperaMaxima = 3f;
        private const float Tolerancia = 1e-3f;

        private GameObject mecha;
        private DatosMecha datos;
        private VidaMecha vida;
        private GuardiaMecha guardia;

        [SetUp]
        public void SetUp()
        {
            EventosPelea.LimpiarSuscriptores();
        }

        [TearDown]
        public void TearDown()
        {
            if (mecha != null)
            {
                Object.Destroy(mecha);
            }
            if (datos != null)
            {
                Object.Destroy(datos);
            }
            EventosPelea.LimpiarSuscriptores();
        }

        private void Armar(bool permitePerfecto)
        {
            datos = ScriptableObject.CreateInstance<DatosMecha>();
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("vidaMaxima").floatValue = 1000f;
            so.FindProperty("tiempoSubirGuardia").floatValue = TiempoSubir;
            so.FindProperty("ventanaBloqueoPerfecto").floatValue = VentanaPerfecta;
            so.FindProperty("reduccionBloqueo").floatValue = ReduccionBloqueo;
            so.ApplyModifiedPropertiesWithoutUndo();
            mecha = new GameObject("MechaPrueba");
            mecha.SetActive(false);
            vida = mecha.AddComponent<VidaMecha>();
            vida.Configurar(datos);
            guardia = mecha.AddComponent<GuardiaMecha>();
            SerializedObject soGuardia = new SerializedObject(guardia);
            soGuardia.FindProperty("mandoPorCodigo").boolValue = true;
            soGuardia.FindProperty("permitePerfecto").boolValue = permitePerfecto;
            soGuardia.ApplyModifiedPropertiesWithoutUndo();
            mecha.SetActive(true);
        }

        private IEnumerator HastaArriba()
        {
            guardia.Sostener(true);
            float limite = Time.time + EsperaMaxima;
            while (!guardia.EstaArriba && Time.time < limite)
            {
                yield return null;
            }
            Assert.IsTrue(guardia.EstaArriba, "La guardia subió");
        }

        [UnityTest]
        public IEnumerator SinPerfecto_GuardiaRecienSubida_EsNormal()
        {
            Armar(false);
            Assert.IsFalse(guardia.PermitePerfecto);
            yield return HastaArriba();
            Assert.Less(guardia.SegundosArriba, VentanaPerfecta, "Recién subida, dentro de la ventana perfecta");
            LogAssert.Expect(LogType.Log, "[Combate] Bloqueo normal");
            ResultadoDanio r = vida.RecibirDanio(new InfoDanio(Danio, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            Assert.AreEqual(TipoBloqueo.Normal, r.Bloqueo);
            Assert.AreEqual(Danio * (1f - ReduccionBloqueo), r.DanioAplicado, Tolerancia);
        }

        [UnityTest]
        public IEnumerator ConPerfecto_GuardiaRecienSubida_EsPerfecto()
        {
            Armar(true);
            Assert.IsTrue(guardia.PermitePerfecto);
            yield return HastaArriba();
            Assert.Less(guardia.SegundosArriba, VentanaPerfecta);
            LogAssert.Expect(LogType.Log, "[Combate] Bloqueo perfecto");
            ResultadoDanio r = vida.RecibirDanio(new InfoDanio(Danio, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            Assert.AreEqual(TipoBloqueo.Perfecto, r.Bloqueo);
            Assert.AreEqual(0f, r.DanioAplicado, Tolerancia);
        }

        [UnityTest]
        public IEnumerator MandoPorCodigo_SostenerSubeYSoltarBaja_SinAccion()
        {
            Armar(false);
            Assert.IsFalse(guardia.Levantada);
            yield return HastaArriba();
            Assert.IsTrue(guardia.Pedida);
            guardia.Sostener(false);
            float limite = Time.time + EsperaMaxima;
            while (guardia.Levantada && Time.time < limite)
            {
                yield return null;
            }
            Assert.IsFalse(guardia.Levantada, "Al soltar baja del todo");
            Assert.IsFalse(guardia.Pedida);
        }

        [UnityTest]
        public IEnumerator MandoPorCodigo_NoDaErrorSinInputActionReference()
        {
            Armar(false);
            yield return null;
            Assert.IsTrue(guardia.enabled, "Sin acción de entrada sigue encendida en mando por código");
        }
    }
}
