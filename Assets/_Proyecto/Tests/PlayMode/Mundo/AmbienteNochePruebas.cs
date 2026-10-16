using System.Collections;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>T-079 en Pruebas_Alan: peso de los Volumes de posproceso por hora, sondas de reflejo y vistas de referencia.</summary>
    public class AmbienteNochePruebas
    {
        private const string RutaPruebas = "Assets/_Proyecto/Escenas/Pruebas_Alan.unity";
        private const float HoraNoche = 23f;
        private const float HoraDia = 13f;
        private const float InicioAnochecer = 18.5f;
        private const float FinAnochecer = 19.5f;
        private const float InicioAmanecer = 6.5f;
        private const float FinAmanecer = 7.5f;
        private const float MinutoEnHoras = 1f / 60f;
        private const float MargenHora = 0.0001f;
        private const float SaltoMaximo = 0.1f;
        private const float PesoCasiCero = 0.05f;
        private const float PesoCasiUno = 0.95f;
        private const int SondasMinimas = 4;
        private const int VistaAerea = 4;
        private const float AlturaOjos = 1.75f;
        private const float AlturaAerea = 40f;
        private const float Tolerancia = 0.05f;
        private const float ToleranciaAltura = 0.15f;

        private RelojMundo reloj;
        private CicloDiaNoche ciclo;

        [UnitySetUp]
        public IEnumerator CargarEscena()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaPruebas, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            reloj = Object.FindFirstObjectByType<RelojMundo>();
            ciclo = Object.FindFirstObjectByType<CicloDiaNoche>();
            Assert.IsNotNull(reloj, "Pruebas_Alan debe traer un RelojMundo");
            Assert.IsNotNull(ciclo, "Pruebas_Alan debe traer un CicloDiaNoche");
            reloj.Pausar(true);
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.DestroyImmediate(raiz);
            }
        }

        [UnityTest]
        public IEnumerator Volumes_HayDosGlobalesConSusPerfiles()
        {
            Assert.IsNotNull(ciclo.VolumeNoche, "El ciclo necesita el Volume de noche");
            Assert.IsNotNull(ciclo.VolumeDia, "El ciclo necesita el Volume de día");
            Assert.IsTrue(ciclo.VolumeNoche.isGlobal);
            Assert.IsTrue(ciclo.VolumeDia.isGlobal);
            Assert.AreEqual("PerfilNoche", ciclo.VolumeNoche.sharedProfile.name);
            Assert.AreEqual("PerfilDia", ciclo.VolumeDia.sharedProfile.name);
            Assert.AreEqual(2, Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Length, "Solo los dos Volumes del ciclo");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pesos_A23ElDeNocheEs1_YA13ElDeDiaEs1()
        {
            reloj.AvanzarHasta(HoraNoche);
            yield return null;
            Assert.AreEqual(1f, ciclo.VolumeNoche.weight, 0.001f, "A las 23:00 manda la noche");
            Assert.AreEqual(0f, ciclo.VolumeDia.weight, 0.001f);

            reloj.AvanzarHasta(HoraDia);
            yield return null;
            Assert.AreEqual(1f, ciclo.VolumeDia.weight, 0.001f, "A las 13:00 manda el día");
            Assert.AreEqual(0f, ciclo.VolumeNoche.weight, 0.001f);
        }

        [UnityTest]
        public IEnumerator Pesos_EntreLas1830YLas1930_CambianSinSaltos()
        {
            reloj.AvanzarHasta(InicioAnochecer);
            yield return null;
            float primero = ciclo.VolumeNoche.weight;
            float anterior = primero;
            for (float hora = InicioAnochecer + MinutoEnHoras; hora <= FinAnochecer + MargenHora; hora += MinutoEnHoras)
            {
                reloj.AvanzarHasta(hora);
                yield return null;
                float actual = ciclo.VolumeNoche.weight;
                Assert.Less(Mathf.Abs(actual - anterior), SaltoMaximo, "Salto del peso a las " + hora.ToString("F3"));
                Assert.GreaterOrEqual(actual, anterior - MargenHora, "El peso de noche solo sube al anochecer");
                Assert.AreEqual(1f, actual + ciclo.VolumeDia.weight, 0.001f, "Los dos pesos suman 1");
                anterior = actual;
            }
            Assert.Less(primero, PesoCasiCero, "A las 18:30 todavía es de día");
            Assert.Greater(anterior, PesoCasiUno, "A las 19:30 ya es de noche");
        }

        [UnityTest]
        public IEnumerator Pesos_AlAmanecer_TambienCambianSinSaltos()
        {
            reloj.AvanzarHasta(InicioAmanecer);
            yield return null;
            float anterior = ciclo.VolumeNoche.weight;
            Assert.Greater(anterior, PesoCasiUno, "A las 06:30 todavía es de noche");
            for (float hora = InicioAmanecer + MinutoEnHoras; hora <= FinAmanecer + MargenHora; hora += MinutoEnHoras)
            {
                reloj.AvanzarHasta(hora);
                yield return null;
                float actual = ciclo.VolumeNoche.weight;
                Assert.Less(Mathf.Abs(actual - anterior), SaltoMaximo, "Salto del peso a las " + hora.ToString("F3"));
                anterior = actual;
            }
            Assert.Less(anterior, PesoCasiCero, "A las 07:30 ya es de día");
        }

        [UnityTest]
        public IEnumerator Sondas_HayAlMenosCuatroConTexturaHorneadaYProyeccionDeCaja()
        {
            ReflectionProbe[] sondas = Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
            Assert.GreaterOrEqual(sondas.Length, SondasMinimas);
            foreach (ReflectionProbe sonda in sondas)
            {
                Assert.IsNotNull(sonda.customBakedTexture, sonda.name + " necesita su textura horneada");
                Assert.IsTrue(sonda.boxProjection, sonda.name + " debe usar proyección de caja");
                Assert.IsInstanceOf<Cubemap>(sonda.customBakedTexture, sonda.name + ": la textura debe ser un cubemap");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vistas_V1EstaEnLaEsquinaMirandoAlEsteYV4EsAerea()
        {
            Assert.IsNotNull(Object.FindFirstObjectByType<VistasReferencia>(), "Pruebas_Alan necesita VistasReferencia");
            CamaraLibre libre = Object.FindFirstObjectByType<CamaraLibre>();
            Assert.IsTrue(VistasReferencia.Ir(1, 22f));
            yield return null;
            Vector3 ojos = libre.Camara.transform.position;
            Assert.AreEqual(-44f, ojos.x, Tolerancia);
            Assert.AreEqual(AlturaOjos, ojos.y, ToleranciaAltura); // el CharacterController se asienta sobre el suelo con su margen de piel
            Assert.AreEqual(-4f, ojos.z, Tolerancia);
            Assert.Greater(Vector3.Dot(libre.Camara.transform.forward, Vector3.right), 0.99f, "V1 mira al este");
            Assert.AreEqual(22f, reloj.Hora, 0.01f);

            Assert.IsTrue(VistasReferencia.Ir(VistaAerea, 22f));
            yield return null;
            Assert.AreEqual(AlturaAerea, libre.Camara.transform.position.y, Tolerancia);
            Assert.IsTrue(libre.EstaVolando, "La aérea vuela");
            Assert.Less(libre.Camara.transform.forward.y, -0.3f, "La aérea mira hacia abajo");
            Assert.IsFalse(VistasReferencia.Ir(VistaAerea + 1, 22f), "No hay quinta vista");
        }
    }
}
