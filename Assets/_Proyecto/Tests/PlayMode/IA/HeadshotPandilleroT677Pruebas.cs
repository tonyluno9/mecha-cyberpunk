using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-677: un tiro a la cabeza de un pandillero armado hace x3 el daño de un tiro al pecho.</summary>
    public class HeadshotPandilleroT677Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject jugador;
        private GameObject plataforma;
        private GameObject pandillero;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(pandillero);
            Object.DestroyImmediate(plataforma);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private float MiraX => jugador.GetComponent<ControladorAPie>().Camara.position.x;

        private PandilleroAPie Crear(float alturaPlataforma)
        {
            if (alturaPlataforma > 0f)
            {
                plataforma = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plataforma.transform.position = new Vector3(MiraX, alturaPlataforma / 2f, 12f);
                plataforma.transform.localScale = new Vector3(2f, alturaPlataforma, 2f);
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero);
            prefab.SetActive(false);
            pandillero = Object.Instantiate(prefab, new Vector3(MiraX, alturaPlataforma + 0.05f, 12f), Quaternion.identity);
            prefab.SetActive(true);
            SerializedObject so = new SerializedObject(pandillero.GetComponent<PandilleroAPie>());
            so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            so.ApplyModifiedPropertiesWithoutUndo();
            pandillero.SetActive(true);
            Physics.SyncTransforms();
            return pandillero.GetComponent<PandilleroAPie>();
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Pandillero_TiroALaCabezaHaceTresVecesElDelPecho()
        {
            yield return Esperar(0.6f);
            jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola); // T-991: ya no hay tecla 2; el arma se pone como lo hace la rueda
            float limite = Time.time + 3f;
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            while (armas.ArmaActiva != ArmaAPie.Pistola && Time.time < limite)
            {
                yield return null;
            }
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva);
            yield return Esperar(0.3f);
            float alturaMira = jugador.GetComponent<ControladorAPie>().Camara.position.y;
            ImpactoBala ultimo = default;
            jugador.GetComponent<PistolaAPie>().BalaImpacto += (arma, impacto) => ultimo = impacto;

            // Pecho: el pandillero sobre una plataforma deja la mira a mitad de su cuerpo.
            PandilleroAPie p = Crear(alturaMira - 1.0f);
            float vidaAntes = p.Actual;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.3f);
            float danioPecho = vidaAntes - p.Actual;
            Assert.IsTrue(ultimo.EnPersona, "El tiro pegó al pandillero");
            Assert.IsFalse(ultimo.Cabeza, "Al pecho no es cabeza");
            Assert.Greater(danioPecho, 0f);
            Object.DestroyImmediate(pandillero);
            Object.DestroyImmediate(plataforma);
            plataforma = null;
            yield return Esperar(0.4f);

            // Cabeza: el pandillero en el piso, con la mira a la altura de su cabeza.
            p = Crear(0f);
            vidaAntes = p.Actual;
            ultimo = default;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.3f);
            float danioCabeza = vidaAntes - p.Actual;
            Assert.IsTrue(ultimo.Cabeza, "A la altura de la cabeza se marca el headshot");
            Assert.AreEqual(Mathf.Min(danioPecho * ReglaZonaDanio.MultiplicadorCabeza, vidaAntes), danioCabeza, 0.01f, "La cabeza hace x3 (" + danioCabeza + " contra " + danioPecho + ")");
        }
    }
}
