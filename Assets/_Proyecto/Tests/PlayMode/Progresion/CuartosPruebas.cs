using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Prologo.Cuartos;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-1496 (playtest 19-nov): se eliminó el cuarto. Antes (T-550/T-804) esta clase probaba la habitación dentro de la casa; ahora comprueba que
    /// el cuarto no se usa (apagado, sin sus puertas) y que la partida nueva empieza afuera, frente a la puerta de la calle, con el tutorial ya en
    /// su primer objetivo (el paso SAL A LA CALLE se salta porque ya se está afuera).
    /// </summary>
    public class CuartosPruebas
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreCuarto = "Cuarto_Habitacion";
        private const float MetrosMaximosALaPuerta = 3f;

        [UnitySetUp]
        public IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            float fin = Time.realtimeSinceStartup + 20f;
            DirectorPandillas director = null;
            while ((director == null || !director.Iniciado) && Time.realtimeSinceStartup < fin)
            {
                director = Object.FindFirstObjectByType<DirectorPandillas>();
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.5f);
        }

        [Test]
        public void Cuarto_NoSeUsa_ApagadoYSinPuertas()
        {
            Assert.IsNull(GameObject.Find(NombreCuarto), "Cuarto_Habitacion está apagado (Find no encuentra los inactivos)");
            PuertaCuarto[] cruces = Object.FindObjectsByType<PuertaCuarto>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.IsFalse(cruces.Any(p => p.Cuarto != null && p.Cuarto.Lugar == IdLugar.Casa), "Sin PuertaCuarto activa de la casa");
            Assert.AreEqual(1, Registro<IPuerta>.Todos.Count(p => p.Lugar == IdLugar.Casa), "La casa solo tiene la puerta de la calle (la del cuarto ya no)");
        }

        [Test]
        public void PartidaNueva_JugadorApareceAfuera_FrenteALaPuertaDeLaCalle()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Assert.IsNotNull(jugador);
            IPuntoAparicion puerta = Registro<IPuntoAparicion>.Todos.First(p => p.Id == "Casa_Puerta");
            Vector3 dif = jugador.Raiz.position - puerta.Pose.position;
            dif.y = 0f;
            Assert.Less(dif.magnitude, MetrosMaximosALaPuerta, "El jugador empieza junto a la puerta de la calle: " + jugador.Raiz.position);
            ILugar casa = Registro<ILugar>.Todos.First(l => l.Id == IdLugar.Casa);
            Assert.IsFalse(casa.JugadorDentro, "El jugador empieza fuera de la casa");
        }

        [Test]
        public void PartidaNueva_TutorialAvanzaAlPrimerObjetivo_SinSalirDelCuarto()
        {
            DirectorPandillas director = Object.FindFirstObjectByType<DirectorPandillas>();
            Assert.AreEqual(PasoHistoria.Asalto, director.Paso, "El paso SAL A LA CALLE se salta: la historia abre en el asalto");
            Assert.AreEqual(DirectorPandillas.AyudaInicio, director.Pantalla.Ayuda, "Sigue el letrero de inicio");
            Assert.IsNotNull(Registro<IObjetivos>.Actual.Actual, "Hay un primer objetivo");
        }
    }
}
