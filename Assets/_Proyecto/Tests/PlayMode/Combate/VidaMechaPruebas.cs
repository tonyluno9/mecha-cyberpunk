using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Combate
{
    public class VidaMechaPruebas
    {
        private const float VidaInicial = 100f;

        private GameObject objeto;
        private DatosMecha datos;
        private VidaMecha vida;
        private PeleadorMecha peleador;
        private ComponenteControlado controlado;
        private int impactos;
        private IPeleador ultimoObjetivo;
        private ResultadoDanio ultimoResultado;

        private class ComponenteControlado : MonoBehaviour
        {
        }

        [SetUp]
        public void SetUp()
        {
            EventosPelea.LimpiarSuscriptores();
            datos = ScriptableObject.CreateInstance<DatosMecha>();
            SerializedObject so = new SerializedObject(datos);
            so.FindProperty("nombre").stringValue = "Mecha de prueba";
            so.FindProperty("vidaMaxima").floatValue = VidaInicial;
            so.ApplyModifiedPropertiesWithoutUndo();

            objeto = new GameObject("Mecha de prueba");
            objeto.SetActive(false);
            vida = objeto.AddComponent<VidaMecha>();
            vida.Configurar(datos);
            controlado = objeto.AddComponent<ComponenteControlado>();
            peleador = objeto.AddComponent<PeleadorMecha>();
            SerializedObject soPeleador = new SerializedObject(peleador);
            SerializedProperty lista = soPeleador.FindProperty("controlados");
            lista.arraySize = 1;
            lista.GetArrayElementAtIndex(0).objectReferenceValue = controlado;
            soPeleador.ApplyModifiedPropertiesWithoutUndo();
            objeto.SetActive(true);

            impactos = 0;
            EventosPelea.ImpactoRecibido += (objetivo, info, resultado) =>
            {
                impactos++;
                ultimoObjetivo = objetivo;
                ultimoResultado = resultado;
            };
        }

        [TearDown]
        public void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            Object.DestroyImmediate(objeto);
            Object.DestroyImmediate(datos);
        }

        private static InfoDanio Golpe(float cantidad)
        {
            return new InfoDanio(cantidad, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null);
        }

        [Test]
        public void RecibirDanio_Normal_BajaLaVidaYPublicaUnImpacto()
        {
            ResultadoDanio resultado = vida.RecibirDanio(Golpe(30f));

            Assert.AreEqual(VidaInicial - 30f, vida.Actual, 1e-4f);
            Assert.AreEqual(30f, resultado.DanioAplicado, 1e-4f);
            Assert.IsFalse(resultado.CausoKO);
            Assert.AreEqual(1, impactos);
            Assert.AreSame(peleador, ultimoObjetivo);
            Assert.AreEqual(30f, ultimoResultado.DanioAplicado, 1e-4f);
        }

        [Test]
        public void RecibirDanio_Cero_IgnoradoYNoPublica()
        {
            ResultadoDanio resultado = vida.RecibirDanio(Golpe(0f));

            Assert.AreEqual(0f, resultado.DanioAplicado);
            Assert.AreEqual(VidaInicial, vida.Actual);
            Assert.AreEqual(0, impactos);
        }

        [Test]
        public void RecibirDanio_EnKO_IgnoradoYNoPublica()
        {
            vida.RecibirDanio(Golpe(VidaInicial));
            Assert.IsTrue(vida.EstaKO);
            impactos = 0;

            ResultadoDanio resultado = vida.RecibirDanio(Golpe(10f));

            Assert.AreEqual(0f, resultado.DanioAplicado);
            Assert.IsFalse(resultado.CausoKO);
            Assert.AreEqual(0f, vida.Actual);
            Assert.AreEqual(0, impactos);
        }

        [Test]
        public void RecibirDanio_Letal_CausaKOUnaVezYAplicaSoloLoQueQueda()
        {
            int avisosKO = 0;
            vida.KO += () => avisosKO++;
            vida.RecibirDanio(Golpe(90f));

            ResultadoDanio resultado = vida.RecibirDanio(Golpe(50f));
            vida.RecibirDanio(Golpe(50f));

            Assert.IsTrue(resultado.CausoKO);
            Assert.AreEqual(10f, resultado.DanioAplicado, 1e-4f);
            Assert.AreEqual(1, avisosKO);
            Assert.AreEqual(2, impactos);
        }

        [Test]
        public void Cambio_AvisaEnCadaDanioAplicado()
        {
            int cambios = 0;
            vida.Cambio += () => cambios++;
            vida.RecibirDanio(Golpe(10f));
            vida.RecibirDanio(Golpe(0f));
            vida.RecibirDanio(Golpe(10f));
            Assert.AreEqual(2, cambios);
        }

        [Test]
        public void Restaurar_DespuesDeKO_VuelveAlMaximoYSeRecibeDanioOtraVez()
        {
            vida.RecibirDanio(Golpe(VidaInicial));
            Assert.IsTrue(vida.EstaKO);

            vida.Restaurar();

            Assert.AreEqual(VidaInicial, vida.Actual);
            Assert.IsFalse(vida.EstaKO);
            Assert.AreEqual(10f, vida.RecibirDanio(Golpe(10f)).DanioAplicado, 1e-4f);
        }

        [Test]
        public void Configurar_ConOtraVidaMaxima_CambiaElMaximo()
        {
            DatosMecha otros = ScriptableObject.CreateInstance<DatosMecha>();
            SerializedObject so = new SerializedObject(otros);
            so.FindProperty("vidaMaxima").floatValue = 50f;
            so.ApplyModifiedPropertiesWithoutUndo();

            vida.Configurar(otros);

            Assert.AreEqual(50f, vida.Maxima);
            Assert.AreEqual(50f, vida.Actual);
            Object.DestroyImmediate(otros);
        }

        [Test]
        public void Peleador_DatosPorDefecto_NombreLadoYVida()
        {
            Assert.AreEqual("Mecha de prueba", peleador.Nombre);
            Assert.AreEqual(LadoPelea.Jugador, peleador.Lado);
            Assert.AreSame(vida, peleador.Vida);
            Assert.AreSame(objeto.transform, peleador.Raiz);
            Assert.IsTrue(peleador.ControlActivo);
        }

        [Test]
        public void Peleador_AlLlegarAKO_QuitaElControl()
        {
            Assert.IsTrue(controlado.enabled);

            vida.RecibirDanio(Golpe(VidaInicial));

            Assert.IsFalse(peleador.ControlActivo);
            Assert.IsFalse(controlado.enabled);
        }

        [Test]
        public void Peleador_ActivarControl_ActivaYDesactivaLaLista()
        {
            peleador.ActivarControl(false);
            Assert.IsFalse(controlado.enabled);
            Assert.IsFalse(peleador.ControlActivo);

            peleador.ActivarControl(true);
            Assert.IsTrue(controlado.enabled);
            Assert.IsTrue(peleador.ControlActivo);
        }

        [Test]
        public void Peleador_ActivarControlEnKO_NoLoDevuelve()
        {
            vida.RecibirDanio(Golpe(VidaInicial));
            peleador.ActivarControl(true);
            Assert.IsFalse(peleador.ControlActivo);
            Assert.IsFalse(controlado.enabled);
        }

        [Test]
        public void Peleador_PrepararPelea_RestauraColocaYDejaSinControl()
        {
            vida.RecibirDanio(Golpe(VidaInicial));
            Pose pose = new Pose(new Vector3(5f, 0f, -3f), Quaternion.Euler(0f, 90f, 0f));

            peleador.PrepararPelea(pose);

            Assert.AreEqual(VidaInicial, vida.Actual);
            Assert.IsFalse(vida.EstaKO);
            Assert.AreEqual(0f, Vector3.Distance(pose.position, objeto.transform.position), 1e-4f);
            Assert.AreEqual(0f, Quaternion.Angle(pose.rotation, objeto.transform.rotation), 1e-3f);
            Assert.IsFalse(peleador.ControlActivo);
            Assert.IsFalse(controlado.enabled);
        }

        [Test]
        public void Peleador_PrepararPelea_ConCharacterController_LoMueveYLoDejaActivo()
        {
            CharacterController controlador = objeto.AddComponent<CharacterController>();
            Pose pose = new Pose(new Vector3(8f, 1f, 2f), Quaternion.identity);

            peleador.PrepararPelea(pose);

            Assert.IsTrue(controlador.enabled);
            Assert.AreEqual(0f, Vector3.Distance(pose.position, objeto.transform.position), 1e-3f);
        }
    }
}
