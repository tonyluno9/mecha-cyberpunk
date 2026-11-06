using System.Collections;
using System.Linq;
using MechaCyberpunk.Core;
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
    /// S2 / T-804: el taller del mecánico (Canepa). Hasta T-804 era un cuarto aparte con fundido; ahora está dentro de la planta baja del Edificio_45,
    /// frente a la casa, con el portón abierto. Se comprueba lo que tiene (carro en el elevador, grúa, herramientas, luz cálida, letrero del kit), que se ve
    /// desde la calle frente a la casa, que el mapa apunta a su portón, que Canepa atiende ahí y que se entra caminando (sin PuertaCuarto).
    /// </summary>
    public class TallerMecanicoS2Pruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const string NombreTaller = "S2n_TallerMecanico";

        private Keyboard teclado;
        private IJugador jugador;
        private Transform taller;
        private Bounds zona;

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
            taller = GameObject.Find(NombreTaller).transform;
            zona = taller.Find("ZonaInterior").GetComponent<BoxCollider>().bounds;
        }

        [Test]
        public void Taller_TieneCarroEnElElevadorGruaHerramientasLetreroYLuzCalida_YCanepaAtiendeAhi()
        {
            Transform muebles = taller.Find("Muebles");
            foreach (string pieza in new[] { "Elevador/V01a_CarroViejito", "T07_GruaMotor", "T04_GabineteHerramientas", "T01_ParedTaller", "L04_Llantas", "C16_Tambo" })
            {
                Assert.IsNotNull(muebles.Find(pieza), pieza);
            }
            Bounds carro = Limites(muebles.Find("Elevador/V01a_CarroViejito"));
            Assert.Greater(carro.min.y, 1.2f, "El carro está levantado en el elevador");
            Assert.IsTrue(zona.Contains(carro.center), "El carro está dentro del taller");
            Assert.IsNotNull(taller.Find("Fachada/Letrero_Taller"), "Letrero de asset sobre el portón");
            Assert.IsTrue(taller.Find("Luces").GetComponentsInChildren<Light>().Any(l => l.enabled && l.color.r > l.color.b && l.intensity > 1f), "Tubos cálidos");
            Transform canepa = GameObject.Find("GTA_T417/Apoyo_CANEPA").transform;
            Assert.IsTrue(zona.Contains(canepa.position + Vector3.up), "Canepa atiende dentro de su taller: " + canepa.position);
            Transform lugar = GameObject.Find("Lugar_TallerMecanico").transform;
            Assert.Less(Vector3.Distance(Plano(lugar.position), Plano(new Vector3(zona.min.x, 0f, zona.center.z))), 1.5f, "El destino MECÁNICO del mapa apunta al portón");
            Assert.IsFalse(Object.FindObjectsByType<PuertaCuarto>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(p => p.Cuarto != null && p.Cuarto.Lugar == IdLugar.Taller), "Sin cuarto aparte ni fundido");
        }

        [Test]
        public void Taller_SeVeDesdeLaCalleFrenteALaCasa()
        {
            ILugar casa = Registro<ILugar>.Todos.First(l => l.Id == IdLugar.Casa);
            Vector3 ojo = casa.PosicionEntrada + Vector3.up * 1.6f;
            foreach (Vector3 punto in new[] { new Vector3(zona.center.x, 1.5f, zona.center.z), Limites(taller.Find("Muebles/Elevador/V01a_CarroViejito")).center })
            {
                bool tapado = Physics.Linecast(ojo, punto, out RaycastHit golpe, ~0, QueryTriggerInteraction.Ignore) && !golpe.collider.transform.IsChildOf(taller);
                Assert.IsFalse(tapado, "Desde la puerta de la casa se ve el interior del taller (" + punto + "); lo tapa " + (tapado ? golpe.collider.name : ""));
            }
        }

        [UnityTest]
        public IEnumerator Entrar_CaminandoPorElPortonAbierto_MarcaAlJugadorDentro()
        {
            // El director de la historia pone al jugador en la cama al arrancar: se espera a que lo haga antes de moverlo.
            float espera = Time.realtimeSinceStartup + 20f;
            MechaCyberpunk.Progresion.Prologo.Pandillas.DirectorPandillas director = null;
            while ((director == null || !director.Iniciado) && Time.realtimeSinceStartup < espera)
            {
                director = Object.FindFirstObjectByType<MechaCyberpunk.Progresion.Prologo.Pandillas.DirectorPandillas>();
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.5f);
            ILugar lugar = Registro<ILugar>.Todos.First(l => l.Id == IdLugar.Taller);
            Vector3 desde = lugar.PosicionEntrada + Vector3.up * 0.05f;
            Vector3 hacia = new Vector3(zona.center.x - 1.5f, desde.y, zona.max.z - 1.2f);
            jugador.Colocar(new Pose(desde, Quaternion.LookRotation(Plano(hacia - desde).normalized, Vector3.up)));
            yield return null;
            yield return null;
            Assert.IsFalse(lugar.JugadorDentro, "Empieza en la calle");
            // Se camina con el CharacterController del jugador (atajo anotado en SalidaCasaPruebas): el portón abierto deja pasar.
            yield return SalidaCasaPruebas.Caminar(jugador, hacia, () => lugar.JugadorDentro);
            Assert.IsTrue(lugar.JugadorDentro, "Caminando por el portón entra al taller (" + jugador.Raiz.position + ")");
            Assert.IsFalse(PuertaCuarto.HayCruce, "Sin fundido");
        }

        private static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private static Bounds Limites(Transform t)
        {
            return t.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
        }
    }
}
