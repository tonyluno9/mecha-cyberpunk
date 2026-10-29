using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// Pruebas jugables de T-402: JugadorAPie real sobre un piso, un grupo de 2 Calaveras armados, entrada simulada con la pistola.
    /// El azar de los disparos se fija para que la prueba no dependa de la suerte.
    /// </summary>
    public class PandillaArmadaPruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const string RutaDron = "Assets/_Proyecto/Trafico/Prefabs/Dron.prefab";
        private const string RutaPatrulla = "Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab";
        private const float DistanciaGrupo = 50f;
        private const float DistanciaEnTerritorio = 15f;
        private const float SegundosSinDisparar = 3f;
        private const float LimiteDisparo = 4f;
        private const float LimiteDrones = 5f;
        private const float DanoMatar = 1000f;
        private const int DineroEsperado = 80;
        private const float CalorExtra = 10f;

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject jugador;
        private GameObject grupo;
        private GameObject gestorObjeto;
        private PandillaAPie pandilla;
        private System.Func<float> azarAnterior;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 0f;
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);

            // Grupo de 2 en +Z, a 50 m. Se activa después de poner los datos.
            grupo = new GameObject("GrupoPrueba");
            grupo.SetActive(false);
            grupo.transform.position = new Vector3(0f, 0f, DistanciaGrupo);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero);
            DatosPandilleroAPie datos = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            for (int i = 0; i < 2; i++)
            {
                GameObject p = Object.Instantiate(prefab, grupo.transform);
                p.transform.position = new Vector3(i * 2f - 1f, 0.05f, DistanciaGrupo);
                SerializedObject soP = new SerializedObject(p.GetComponent<PandilleroAPie>());
                soP.FindProperty("datos").objectReferenceValue = datos;
                soP.ApplyModifiedPropertiesWithoutUndo();
            }
            pandilla = grupo.AddComponent<PandillaAPie>();
            SerializedObject so = new SerializedObject(pandilla);
            so.FindProperty("datos").objectReferenceValue = datos;
            so.FindProperty("idPandilla").enumValueIndex = (int)IdPandilla.Calaveras;
            so.ApplyModifiedPropertiesWithoutUndo();
            grupo.SetActive(true);
        }

        public override void TearDown()
        {
            PistolaPandillero.Azar = azarAnterior;
            Object.Destroy(gestorObjeto);
            Object.Destroy(grupo);
            Object.Destroy(jugador);
            Object.Destroy(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private int Disparos()
        {
            int total = 0;
            foreach (PistolaPandillero pistola in grupo.GetComponentsInChildren<PistolaPandillero>(true))
            {
                total += pistola.Disparos;
            }
            return total;
        }

        private IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TeDisparanEnSuTerritorio()
        {
            yield return Esperar(0.5f);
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            float maxima = vida.Maxima;
            Assert.AreEqual(2, pandilla.Presentes);

            // A 50 m del centro: fuera del territorio, nadie dispara.
            yield return Esperar(SegundosSinDisparar);
            Assert.AreEqual(0, Disparos(), "No debe disparar fuera del territorio.");
            Assert.AreEqual(maxima, vida.Actual, 0.001f);

            // A 15 m: en menos de 4 s la vida baja.
            Registro<IJugador>.Actual.Colocar(new Pose(new Vector3(0f, 0.05f, DistanciaGrupo - DistanciaEnTerritorio), Quaternion.identity));
            float fin = Time.time + LimiteDisparo;
            while (Time.time < fin && vida.Actual >= maxima)
            {
                yield return null;
            }
            Assert.Less(vida.Actual, maxima, "El jugador en su territorio debe recibir disparos.");
            Assert.Greater(Disparos(), 0);
        }

        [UnityTest]
        public IEnumerator VencerlosPagaYTraeDrones()
        {
            yield return Esperar(0.5f);
            ICartera cartera = Registro<ICartera>.Actual;
            ICalor calor = Registro<ICalor>.Actual;
            Assert.IsNotNull(cartera, "El jugador trae la cartera.");
            Assert.IsNotNull(calor, "El jugador trae el calor.");

            gestorObjeto = new GameObject("Persecucion");
            gestorObjeto.SetActive(false);
            GestorPersecucion gestor = gestorObjeto.AddComponent<GestorPersecucion>();
            gestor.Configurar(AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfig),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaDron).GetComponent<DronMundo>(),
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaPatrulla).GetComponent<PatrullaMundo>());
            gestorObjeto.SetActive(true);
            yield return null;

            // Un tiro real con la pistola (2, clic derecho, clic): suma calor en la calle.
            int dineroAntes = cartera.Dinero;
            float calorAntes = calor.Valor;
            Press(teclado.digit2Key);
            yield return null;
            Release(teclado.digit2Key);
            Press(raton.rightButton);
            yield return null;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            Release(raton.rightButton);
            yield return null;
            Assert.Greater(calor.Valor, calorAntes, "Un disparo en la calle suma calor.");

            // Vence a los 2 pandilleros.
            foreach (PandilleroAPie p in pandilla.Pandilleros)
            {
                p.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, p.transform.position, Vector3.forward, null));
            }
            Assert.AreEqual(dineroAntes + DineroEsperado, cartera.Dinero);

            // 2 x 8 + el tiro no llegan al umbral de 25: se sube el calor como lo haría la tecla de prueba.
            calor.Sumar(CalorExtra, MotivoCalor.Otro);
            Assert.GreaterOrEqual(calor.Nivel, 1);
            float fin = Time.time + LimiteDrones;
            while (Time.time < fin && gestor.Drones < 1)
            {
                yield return null;
            }
            Assert.GreaterOrEqual(gestor.Drones, 1);
        }
    }
}
