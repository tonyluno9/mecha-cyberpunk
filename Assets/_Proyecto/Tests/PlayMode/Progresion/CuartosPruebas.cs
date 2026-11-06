using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Progresion.Prologo.Cuartos;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-550 / T-804: la habitación. Desde T-804 está dentro de la casa, detrás de la puerta de la calle (antes era un cuarto aparte a 1000 m y se
    /// cruzaba con fundido). Estas pruebas antes llevaban al jugador al trigger de cada PuertaCuarto; ahora comprueban que ya no hay ninguna PuertaCuarto
    /// de la casa, que la puerta cerrada no deja pasar y que se sale caminando por las dos puertas (atajos anotados en <see cref="SalidaCasaPruebas"/>).
    /// </summary>
    public class CuartosPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreCuarto = "Cuarto_Habitacion";
        private const float AlturaMinimaDelCuarto = 2.5f;

        private Keyboard teclado;
        private IJugador jugador;
        private Transform cuarto;
        private Bounds volumenCuarto;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
        }

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
            cuarto = GameObject.Find(NombreCuarto).transform;
            volumenCuarto = new Bounds(cuarto.position, Vector3.zero);
            foreach (Renderer r in cuarto.Find("Piso").GetComponents<Renderer>().Concat(cuarto.Find("Techo").GetComponents<Renderer>()))
            {
                volumenCuarto.Encapsulate(r.bounds);
            }
        }

        [Test]
        public void Cuarto_Mide3x4x2_7YLaPuertaYLaCamaSonDeEscalaHumana()
        {
            Bounds piso = cuarto.Find("Piso").GetComponent<Renderer>().bounds;
            Assert.AreEqual(3f, piso.size.x, 0.01f);
            Assert.AreEqual(4f, piso.size.z, 0.01f);
            Assert.AreEqual(2.7f, cuarto.Find("Techo").GetComponent<Renderer>().bounds.max.y - piso.min.y, 0.01f);
            Bounds puerta = Limites(cuarto.Find("Puerta_Cuarto/Hoja"));
            Assert.That(puerta.size.y, Is.InRange(2f, 2.3f), "Puerta de unos 2.1 m");
            Bounds escritorio = Limites(cuarto.Find("I02"));
            Assert.That(escritorio.size.y, Is.InRange(0.7f, 1.3f), "Escritorio con su monitor");
            Assert.Greater(volumenCuarto.size.y, AlturaMinimaDelCuarto);
        }

        [Test]
        public void Cuarto_TieneTodosSusMueblesYSuLuzCalida()
        {
            Bounds dentro = volumenCuarto;
            dentro.Expand(new Vector3(0.1f, 0.2f, 0.1f));
            foreach (string mueble in new[] { "I01", "I02", "I03", "I04", "I05", "H02_Cama", "H11_Buro" })
            {
                Transform m = cuarto.Find(mueble);
                Assert.IsNotNull(m, mueble);
                Bounds b = Limites(m);
                Assert.That(dentro.Contains(b.center), mueble + " dentro del cuarto");
                Assert.That(b.min.y, Is.InRange(cuarto.position.y - 0.05f, cuarto.position.y + 1.1f), mueble + " ni enterrado ni flotando");
            }
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                Assert.IsFalse(t.name.StartsWith("Banco") && dentro.Contains(t.position), "Sin banco de taller en el cuarto: " + t.name);
            }
            Light[] luces = cuarto.Find("Luces").GetComponentsInChildren<Light>();
            Assert.IsTrue(luces.Any(l => l.name == "FocoColgante_Luz" && l.color.r > l.color.b), "Foco cálido");
            Assert.IsTrue(luces.Any(l => l.name.StartsWith("NeonVentana")), "Neón por la ventana");
        }

        [Test]
        public void Casa_SinTeletransporte_YLaCamaDentroDeLaCasa()
        {
            PuertaCuarto[] cruces = Object.FindObjectsByType<PuertaCuarto>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsFalse(cruces.Any(p => p.Cuarto != null && (p.Cuarto.Lugar == IdLugar.Casa || p.Cuarto.Lugar == IdLugar.Taller)), "Ya no hay PuertaCuarto (fundido y teletransporte) de la casa ni del taller");
            ILugar casa = Casa();
            Vector3 cama = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(m => m.GetType().Name == "CamaJugador").transform.position;
            Assert.Less(Vector3.Distance(cama, casa.PosicionEntrada), 12f, "La cama está en la casa de la ciudad, no a 1000 m");
            // La casa (frente x -57, de z -83.7 a -70.3, fondo hasta x -69) contiene el cuarto y la cama.
            Bounds volumenCasa = new Bounds();
            volumenCasa.SetMinMax(new Vector3(-69f, -0.5f, -83.7f), new Vector3(-56.9f, 7.2f, -70.3f));
            Assert.IsTrue(volumenCasa.Contains(cama), "La cama está dentro del volumen de la casa: " + cama);
            Assert.IsTrue(volumenCasa.Contains(volumenCuarto.min) && volumenCasa.Contains(volumenCuarto.max), "El cuarto entero cabe en la casa");
            Assert.AreEqual(2, Registro<IPuerta>.Todos.Count(p => p.Lugar == IdLugar.Casa), "La casa tiene dos puertas reales: la de la calle y la del cuarto");
        }

        [UnityTest]
        public IEnumerator Salir_PorLaPuertaDelCuartoYLaDeLaCalle_Caminando_SinTeletransporte()
        {
            IPuerta delCuarto = SalidaCasaPruebas.PuertaDeLaCasa(true);
            IPuerta deLaCalle = SalidaCasaPruebas.PuertaDeLaCasa(false);
            Assert.IsFalse(delCuarto.EstaAbierta);
            Assert.IsFalse(deLaCalle.EstaAbierta);
            // El director de la historia pone al jugador en la cama al arrancar: se espera a que lo haga antes de moverlo.
            yield return EsperarDirector();
            Assert.IsTrue(Casa().JugadorDentro, "En el cuarto, el jugador está dentro de la casa");

            // Cerrada, la puerta del cuarto no deja pasar.
            yield return SalidaCasaPruebas.Caminar(jugador, delCuarto.Posicion + Vector3.forward * SalidaCasaPruebas.FrenteCuarto + Vector3.left * 0.9f);
            yield return SalidaCasaPruebas.Caminar(jugador, delCuarto.Posicion + Vector3.forward * SalidaCasaPruebas.FrenteCuarto);
            CharacterController cc = ((Component)jugador).GetComponent<CharacterController>();
            for (int i = 0; i < 40; i++)
            {
                cc.Move(Vector3.back * 0.05f + Vector3.down * 0.02f);
                yield return null;
            }
            Assert.Greater(jugador.Raiz.position.z, delCuarto.Posicion.z, "Con la puerta cerrada no se cruza (no hay trigger de fundido)");

            List<Vector3> recorrido = new List<Vector3>();
            bool siguiendo = true;
            IEnumerator Seguir()
            {
                while (siguiendo)
                {
                    recorrido.Add(jugador.Raiz.position);
                    yield return null;
                }
            }
            ((MonoBehaviour)jugador).StartCoroutine(Seguir());
            yield return SalidaCasaPruebas.Salir();
            siguiendo = false;
            Assert.IsFalse(Casa().JugadorDentro, "En la banqueta ya salió de la casa");
            Assert.IsFalse(PuertaCuarto.HayCruce, "Sin fundido");
            for (int i = 1; i < recorrido.Count; i++)
            {
                Assert.Less(Vector3.Distance(recorrido[i], recorrido[i - 1]), 0.6f, "Sin saltos (teletransporte) entre cuadros: " + recorrido[i - 1] + " -> " + recorrido[i]);
            }
            Assert.Greater(recorrido.Count, 30, "Se siguió el recorrido");
        }

        private static IEnumerator EsperarDirector()
        {
            float fin = Time.realtimeSinceStartup + 20f;
            MechaCyberpunk.Progresion.Prologo.Pandillas.DirectorPandillas director = null;
            while ((director == null || !director.Iniciado) && Time.realtimeSinceStartup < fin)
            {
                director = Object.FindFirstObjectByType<MechaCyberpunk.Progresion.Prologo.Pandillas.DirectorPandillas>();
                yield return null;
            }
            yield return Esperar(0.5f);
        }

        private static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static ILugar Casa()
        {
            return Registro<ILugar>.Todos.First(l => l.Id == IdLugar.Casa);
        }

        private static Bounds Limites(Transform t)
        {
            return t.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
        }
    }
}
