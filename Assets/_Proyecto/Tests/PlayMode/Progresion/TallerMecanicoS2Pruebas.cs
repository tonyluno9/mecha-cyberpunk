using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Prologo.Cuartos;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// S2 (playtest 5-nov): el taller del mecánico (Canepa, frente a la casa) es un cuarto aparte con fundido, como la habitación. Ida y vuelta por su
    /// puerta de la calle, y que Canepa sigue afuera, junto a la puerta.
    /// </summary>
    public class TallerMecanicoS2Pruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreCuarto = "Cuarto_TallerMecanico";
        private const float SegundosMaximosDeCruce = 6f;
        private const float DistanciaMaximaAfuera = 1.5f;
        private const float GradosMaximosHaciaLaCalle = 45f;
        private const float SegundosEntreCruces = 1f;
        private const float AlturaMinimaDelCuarto = 2.5f;

        private IJugador jugador;
        private PuertaCuarto puertaAfuera;
        private PuertaCuarto puertaAdentro;
        private Bounds volumenCuarto;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 30 && Registro<IJugador>.Actual == null; i++)
            {
                yield return null;
            }
            jugador = Registro<IJugador>.Actual;
            Assert.IsNotNull(jugador, "Ciudad_Prologo trae al jugador");
            PuertaCuarto[] puertas = Object.FindObjectsByType<PuertaCuarto>(FindObjectsSortMode.None);
            puertaAfuera = puertas.Single(p => !p.EsSalida && p.Cuarto.Lugar == IdLugar.Taller);
            puertaAdentro = puertas.Single(p => p.EsSalida && p.Cuarto.Lugar == IdLugar.Taller);
            Transform cuarto = puertaAdentro.Cuarto.transform;
            Assert.AreEqual(NombreCuarto, cuarto.name);
            volumenCuarto = new Bounds(cuarto.position, Vector3.zero);
            foreach (Renderer r in cuarto.GetComponentsInChildren<Renderer>())
            {
                volumenCuarto.Encapsulate(r.bounds);
            }
        }

        [Test]
        public void Taller_TieneCarroGruaHerramientasYLuzCalida_YCanepaSigueAfuera()
        {
            Transform cuarto = puertaAdentro.Cuarto.transform;
            foreach (string pieza in new[] { "V01a_CarroViejito", "T07_GruaMotor", "T04_GabineteHerramientas", "L04_Llantas", "C16_Tambo", "C01_FachadaLocal_cy" })
            {
                Assert.IsNotNull(cuarto.Find(pieza), pieza);
            }
            Assert.IsTrue(cuarto.Find("Luces").GetComponentsInChildren<Light>().Any(l => l.color.r > l.color.b && l.intensity > 1f), "Tubos cálidos");
            Transform canepa = GameObject.Find("GTA_T417/Apoyo_CANEPA").transform;
            Assert.Less(Vector3.Distance(canepa.position, puertaAfuera.transform.position), 4f, "Canepa atiende junto a la puerta de su taller");
            Assert.IsFalse(volumenCuarto.Contains(canepa.position), "Canepa sigue en la calle (arregla el carro afuera)");
        }

        private IEnumerator Cruzar(PuertaCuarto puerta)
        {
            yield return new WaitForSecondsRealtime(SegundosEntreCruces); // la puerta no deja cruzar dos veces seguidas (cooldown de 0.75 s)
            BoxCollider caja = puerta.GetComponent<BoxCollider>();
            jugador.Colocar(new Pose(caja.bounds.center - Vector3.up * (caja.bounds.extents.y - 0.1f), jugador.Raiz.rotation));
            float limite = Time.realtimeSinceStartup + SegundosMaximosDeCruce;
            yield return new WaitForFixedUpdate();
            yield return null;
            while (Time.realtimeSinceStartup < limite && !PuertaCuarto.HayCruce)
            {
                yield return null;
            }
            while (Time.realtimeSinceStartup < limite && PuertaCuarto.HayCruce)
            {
                yield return null;
            }
            Assert.IsFalse(PuertaCuarto.HayCruce, "El cruce terminó");
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        private ILugar Taller()
        {
            return Registro<ILugar>.Todos.First(l => l.Id == IdLugar.Taller);
        }

        [UnityTest]
        public IEnumerator Cruzar_AlEntrar_ElJugadorQuedaDentroDelCuarto()
        {
            Assert.IsFalse(volumenCuarto.Contains(jugador.Raiz.position), "Empieza afuera");
            yield return Cruzar(puertaAfuera);
            Vector3 p = jugador.Raiz.position;
            Assert.That(volumenCuarto.Contains(p), "Dentro del volumen del cuarto: " + p + " vs " + volumenCuarto);
            Assert.Less(Vector3.Distance(p, puertaAdentro.Cuarto.PoseInterior.position), 0.6f, "Junto a su puerta interior");
            Assert.IsTrue(Taller().JugadorDentro, "El lugar marca al jugador dentro");
            Assert.Greater(volumenCuarto.size.y, AlturaMinimaDelCuarto);
        }

        [UnityTest]
        public IEnumerator Cruzar_IdaYVuelta_AlSalirQuedaFrenteALaPuertaDeAfuera()
        {
            yield return Cruzar(puertaAfuera);
            Assert.IsTrue(Taller().JugadorDentro);
            yield return Cruzar(puertaAdentro);
            Pose afuera = puertaAdentro.Cuarto.PoseExterior;
            Vector3 p = jugador.Raiz.position;
            Vector3 plano = p - afuera.position;
            plano.y = 0f;
            Assert.Less(plano.magnitude, DistanciaMaximaAfuera, "Frente a la puerta de la calle: " + p + " vs " + afuera.position);
            Assert.IsFalse(volumenCuarto.Contains(p), "Ya no está dentro del cuarto");
            Assert.IsFalse(Taller().JugadorDentro, "El lugar ya no lo marca dentro");
            Assert.Less(Vector3.Angle(Vector3.ProjectOnPlane(jugador.Raiz.forward, Vector3.up), Vector3.ProjectOnPlane(afuera.rotation * Vector3.forward, Vector3.up)), GradosMaximosHaciaLaCalle, "Mira hacia la calle");
        }
    }
}
