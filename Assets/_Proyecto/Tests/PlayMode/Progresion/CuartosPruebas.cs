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
    /// T-550: el cruce de ida y vuelta de la habitación (cuarto aparte). Se lleva al jugador al trigger de cada puerta, que es lo que hace caminar hacia ella:
    /// al entrar queda dentro del volumen del cuarto (y el lugar lo marca como "dentro"); al salir queda frente a la puerta de la calle, mirando a la calle.
    /// </summary>
    public class CuartosPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreCuarto = "Cuarto_Habitacion";
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
            puertaAfuera = puertas.Single(p => !p.EsSalida && p.Cuarto.Lugar == IdLugar.Casa);
            puertaAdentro = puertas.Single(p => p.EsSalida && p.Cuarto.Lugar == IdLugar.Casa);
            Transform cuarto = puertaAdentro.Cuarto.transform;
            Assert.AreEqual(NombreCuarto, cuarto.name);
            volumenCuarto = new Bounds(cuarto.position, Vector3.zero);
            foreach (Renderer r in cuarto.GetComponentsInChildren<Renderer>())
            {
                volumenCuarto.Encapsulate(r.bounds);
            }
        }

        [Test]
        public void Cuarto_Mide3x4x2_7YLaPuertaYLaCamaSonDeEscalaHumana()
        {
            Transform cuarto = puertaAdentro.Cuarto.transform;
            Bounds piso = cuarto.Find("Piso").GetComponent<Renderer>().bounds;
            Assert.AreEqual(3f, piso.size.x, 0.01f);
            Assert.AreEqual(4f, piso.size.z, 0.01f);
            Assert.AreEqual(2.7f, cuarto.Find("Techo").GetComponent<Renderer>().bounds.max.y - piso.min.y, 0.01f);
            Bounds puerta = cuarto.Find("PuertaInterior").GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Assert.That(puerta.size.y, Is.InRange(2f, 2.3f), "Puerta de unos 2.1 m");
            Bounds escritorio = cuarto.Find("I02").GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Assert.That(escritorio.size.y, Is.InRange(0.7f, 1.3f), "Escritorio con su monitor");
        }

        [Test]
        public void Cuarto_TieneTodosSusMueblesYSuLuzCalida()
        {
            Transform cuarto = puertaAdentro.Cuarto.transform;
            foreach (string mueble in new[] { "I01", "I02", "I03", "I04", "I05", "H02_Cama", "H11_Buro" })
            {
                Transform m = cuarto.Find(mueble);
                Assert.IsNotNull(m, mueble);
                Bounds b = m.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
                Assert.That(volumenCuarto.Contains(b.center), mueble + " dentro del cuarto");
                Assert.That(b.min.y, Is.InRange(cuarto.position.y - 0.05f, cuarto.position.y + 1.1f), mueble + " ni enterrado ni flotando");
            }
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                Assert.IsFalse(t.name.StartsWith("Banco") && volumenCuarto.Contains(t.position), "Sin banco de taller en el cuarto: " + t.name);
            }
            Light[] luces = cuarto.Find("Luces").GetComponentsInChildren<Light>();
            Assert.IsTrue(luces.Any(l => l.name == "FocoColgante_Luz" && l.color.r > l.color.b), "Foco cálido");
            Assert.IsTrue(luces.Any(l => l.name.StartsWith("NeonVentana")), "Neón por la ventana");
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

        private ILugar Casa()
        {
            return Registro<ILugar>.Todos.First(l => l.Id == IdLugar.Casa);
        }

        [UnityTest]
        public IEnumerator Cruzar_AlEntrar_ElJugadorQuedaDentroDelCuarto()
        {
            Assert.IsFalse(volumenCuarto.Contains(jugador.Raiz.position), "Empieza afuera");
            yield return Cruzar(puertaAfuera);
            Vector3 p = jugador.Raiz.position;
            Assert.That(volumenCuarto.Contains(p), "Dentro del volumen del cuarto: " + p + " vs " + volumenCuarto);
            Assert.Less(Vector3.Distance(p, puertaAdentro.Cuarto.PoseInterior.position), 0.6f, "Junto a su puerta interior");
            Assert.IsTrue(Casa().JugadorDentro, "El lugar marca al jugador dentro");
            Assert.Greater(volumenCuarto.size.y, AlturaMinimaDelCuarto);
        }

        [UnityTest]
        public IEnumerator Cruzar_IdaYVuelta_AlSalirQuedaFrenteALaPuertaDeAfuera()
        {
            yield return Cruzar(puertaAfuera);
            Assert.IsTrue(Casa().JugadorDentro);
            yield return Cruzar(puertaAdentro);
            Pose afuera = puertaAdentro.Cuarto.PoseExterior;
            Vector3 p = jugador.Raiz.position;
            Vector3 plano = p - afuera.position;
            plano.y = 0f;
            Assert.Less(plano.magnitude, DistanciaMaximaAfuera, "Frente a la puerta de la calle: " + p + " vs " + afuera.position);
            Assert.IsFalse(volumenCuarto.Contains(p), "Ya no está dentro del cuarto");
            Assert.IsFalse(Casa().JugadorDentro, "El lugar ya no lo marca dentro");
            Assert.Less(Vector3.Angle(Vector3.ProjectOnPlane(jugador.Raiz.forward, Vector3.up), Vector3.ProjectOnPlane(afuera.rotation * Vector3.forward, Vector3.up)), GradosMaximosHaciaLaCalle, "Mira hacia la calle");
        }
    }
}
