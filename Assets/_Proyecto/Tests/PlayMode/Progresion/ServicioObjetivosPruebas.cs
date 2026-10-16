using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    public class ServicioObjetivosPruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";

        private class LugarFalso : ILugar
        {
            public IdLugar Id { get; set; }
            public string Nombre => "Falso";
            public HorarioLugar Horario => default;
            public bool EstaAbierto => true;
            public bool JugadorDentro => false;
            public Vector3 PosicionEntrada { get; set; }
            public event System.Action<bool> EstadoCambio { add { } remove { } }
        }

        private class JugadorFalso : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => default;
            public Camera Camara => null;
            public event System.Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) { }
        }

        private GameObject raiz;
        private ServicioObjetivos servicio;
        private GameObject jugadorGo;
        private JugadorFalso jugador;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            raiz = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab));
            servicio = raiz.GetComponent<ServicioObjetivos>();
            jugadorGo = new GameObject("JugadorFalso");
            jugador = new JugadorFalso { Raiz = jugadorGo.transform };
        }

        public override void TearDown()
        {
            Object.Destroy(raiz);
            Object.Destroy(jugadorGo);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            base.TearDown();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin) yield return null;
        }

        [UnityTest]
        public IEnumerator Prefab_Instanciado_ElServicioEsElActualDelRegistro()
        {
            yield return null;
            Assert.IsNotNull(servicio);
            Assert.AreSame(servicio, Registro<IObjetivos>.Actual);
        }

        [UnityTest]
        public IEnumerator Prefab_Desactivado_SaleDelRegistroYAvisaQuitadoUnaVez()
        {
            yield return null;
            int quitados = 0;
            Registro<IObjetivos>.Quitado += _ => quitados++;
            raiz.SetActive(false);
            Assert.IsNull(Registro<IObjetivos>.Actual);
            Assert.AreEqual(1, quitados);
        }

        [UnityTest]
        public IEnumerator Prefab_Reactivado_VuelveAlRegistroYAvisaAgregadoUnaVez()
        {
            yield return null;
            raiz.SetActive(false);
            int agregados = 0;
            Registro<IObjetivos>.Agregado += _ => agregados++;
            raiz.SetActive(true);
            Assert.AreSame(servicio, Registro<IObjetivos>.Actual);
            Assert.AreEqual(1, agregados);
        }

        [UnityTest]
        public IEnumerator Fijar_ObjetivoDeLugar_AvisaCambioConEseObjetivo()
        {
            yield return null;
            int avisos = 0;
            Objetivo recibido = default;
            servicio.Cambio += o => { avisos++; recibido = o; };
            servicio.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(IdLugar.Bar, recibido.Lugar);
            Assert.AreEqual("Ve al bar", recibido.Texto);
            Assert.IsTrue(servicio.HayObjetivo);
            Assert.AreEqual(IdLugar.Bar, servicio.Actual.Lugar);
        }

        [UnityTest]
        public IEnumerator Fijar_LugarRegistrado_LaPosicionEsLaEntradaDelLugar()
        {
            var lugar = new LugarFalso { Id = IdLugar.Bar, PosicionEntrada = new Vector3(12f, 0f, -7f) };
            Registro<ILugar>.Agregar(lugar);
            Registro<IJugador>.Agregar(jugador);
            servicio.Fijar(new Objetivo("Bar", IdLugar.Bar));
            yield return null;
            yield return null;
            Assert.IsTrue(servicio.PosicionResuelta);
            Assert.AreEqual(lugar.PosicionEntrada, servicio.Posicion);
        }

        [UnityTest]
        public IEnumerator Fijar_LugarNoRegistrado_NoResuelveLaPosicion()
        {
            Registro<IJugador>.Agregar(jugador);
            servicio.Fijar(new Objetivo("Bar", IdLugar.Bar));
            yield return null;
            yield return null;
            Assert.IsFalse(servicio.PosicionResuelta);
        }

        [UnityTest]
        public IEnumerator Fijar_Ninguno_EquivaleAQuitar()
        {
            yield return null;
            servicio.Fijar(new Objetivo("Bar", IdLugar.Bar));
            int avisos = 0;
            Objetivo recibido = new Objetivo("x", IdLugar.Bar);
            servicio.Cambio += o => { avisos++; recibido = o; };
            servicio.Fijar(Objetivo.Ninguno);
            Assert.AreEqual(1, avisos);
            Assert.IsFalse(servicio.HayObjetivo);
            Assert.IsFalse(recibido.TienePosicion);
            Assert.AreEqual(IdLugar.Ninguno, recibido.Lugar);
            servicio.Quitar();
            Assert.AreEqual(1, avisos, "Quitar sin objetivo no avisa");
        }

        [UnityTest]
        public IEnumerator Jugador_SeMueveLejosYRegresa_SeAlejaSePrendeSeApagaYLlegadaAvisaUnaVez()
        {
            Registro<IJugador>.Agregar(jugador);
            int llegadas = 0;
            servicio.Seguimiento.Llegada += () => llegadas++;
            servicio.Fijar(new Objetivo("Punto", Vector3.zero));
            jugadorGo.transform.position = new Vector3(50f, 0f, 0f);
            yield return null;
            yield return null;
            Assert.IsFalse(servicio.SeAleja);
            Assert.AreEqual(50f, servicio.Distancia, 0.01f);

            jugadorGo.transform.position = new Vector3(100f, 0f, 0f);
            yield return Esperar(1.8f);
            Assert.IsTrue(servicio.SeAleja);

            jugadorGo.transform.position = new Vector3(55f, 0f, 0f);
            yield return null;
            yield return null;
            Assert.IsFalse(servicio.SeAleja);
            Assert.AreEqual(0, llegadas);

            jugadorGo.transform.position = new Vector3(2f, 0f, 0f);
            yield return null;
            yield return null;
            jugadorGo.transform.position = new Vector3(30f, 0f, 0f);
            yield return null;
            jugadorGo.transform.position = new Vector3(1f, 0f, 0f);
            yield return null;
            Assert.IsTrue(servicio.Llego);
            Assert.AreEqual(1, llegadas);
        }

        [UnityTest]
        public IEnumerator Jugador_NoRegistrado_NoAvanzaElSeguimiento()
        {
            servicio.Fijar(new Objetivo("Punto", Vector3.zero));
            yield return null;
            yield return null;
            Assert.IsFalse(servicio.Llego);
            Assert.AreEqual(0f, servicio.Distancia);
        }

        [UnityTest]
        public IEnumerator JugadorEntro_AlLugarDelObjetivo_AvisaLlegadaAunqueEsteLejos()
        {
            Registro<IJugador>.Agregar(jugador);
            Registro<ILugar>.Agregar(new LugarFalso { Id = IdLugar.Bar, PosicionEntrada = new Vector3(500f, 0f, 0f) });
            int llegadas = 0;
            servicio.Seguimiento.Llegada += () => llegadas++;
            servicio.Fijar(new Objetivo("Bar", IdLugar.Bar));
            yield return null;
            yield return null;
            Assert.AreEqual(0, llegadas);
            EventosLugares.PublicarEntro(IdLugar.Bar);
            Assert.AreEqual(1, llegadas);
            Assert.IsTrue(servicio.Llego);
        }

        [UnityTest]
        public IEnumerator JugadorEntro_AOtroLugar_NoAvisaLlegada()
        {
            yield return null;
            int llegadas = 0;
            servicio.Seguimiento.Llegada += () => llegadas++;
            servicio.Fijar(new Objetivo("Bar", IdLugar.Bar));
            EventosLugares.PublicarEntro(IdLugar.Casino);
            Assert.AreEqual(0, llegadas);
            Assert.IsFalse(servicio.Llego);
        }

        [UnityTest]
        public IEnumerator Fijador_DosObjetivosYAvanzarAlLlegar_PasaDelPrimeroAlSegundo()
        {
            Registro<IJugador>.Agregar(jugador);
            GameObject destinoA = new GameObject("DestinoA");
            GameObject destinoB = new GameObject("DestinoB");
            destinoA.transform.position = Vector3.zero;
            destinoB.transform.position = new Vector3(200f, 0f, 0f);
            jugadorGo.transform.position = new Vector3(30f, 0f, 0f);

            var go = new GameObject("Fijador");
            go.SetActive(false);
            var fijador = go.AddComponent<FijadorObjetivoDePrueba>();
            var so = new SerializedObject(fijador);
            so.FindProperty("avanzarAlLlegar").boolValue = true;
            SerializedProperty lista = so.FindProperty("objetivos");
            lista.arraySize = 2;
            lista.GetArrayElementAtIndex(0).FindPropertyRelative("texto").stringValue = "A";
            lista.GetArrayElementAtIndex(0).FindPropertyRelative("destino").objectReferenceValue = destinoA.transform;
            lista.GetArrayElementAtIndex(1).FindPropertyRelative("texto").stringValue = "B";
            lista.GetArrayElementAtIndex(1).FindPropertyRelative("destino").objectReferenceValue = destinoB.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(true);

            yield return null;
            yield return null;
            Assert.AreEqual("A", servicio.Actual.Texto);
            Assert.AreEqual(0, fijador.Indice);

            jugadorGo.transform.position = new Vector3(1f, 0f, 0f);
            yield return null;
            yield return null;
            Assert.AreEqual("B", servicio.Actual.Texto);
            Assert.AreEqual(1, fijador.Indice);
            Assert.IsTrue(servicio.Actual.TienePosicion);
            Assert.AreEqual(destinoB.transform.position, servicio.Actual.Posicion);

            Object.Destroy(go);
            Object.Destroy(destinoA);
            Object.Destroy(destinoB);
        }

        [UnityTest]
        public IEnumerator Fijador_TeclaO_PasaAlSiguienteObjetivo()
        {
            var teclado = InputSystem.AddDevice<Keyboard>();
            var go = new GameObject("Fijador");
            go.SetActive(false);
            var fijador = go.AddComponent<FijadorObjetivoDePrueba>();
            var so = new SerializedObject(fijador);
            SerializedProperty lista = so.FindProperty("objetivos");
            lista.arraySize = 2;
            lista.GetArrayElementAtIndex(0).FindPropertyRelative("texto").stringValue = "A";
            lista.GetArrayElementAtIndex(0).FindPropertyRelative("lugar").enumValueIndex = (int)IdLugar.Bar;
            lista.GetArrayElementAtIndex(1).FindPropertyRelative("texto").stringValue = "B";
            lista.GetArrayElementAtIndex(1).FindPropertyRelative("lugar").enumValueIndex = (int)IdLugar.Casino;
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(true);

            yield return null;
            Assert.AreEqual("A", servicio.Actual.Texto);
            Press(teclado.oKey);
            yield return null;
            Release(teclado.oKey);
            yield return null;
            Assert.AreEqual("B", servicio.Actual.Texto);
            Assert.AreEqual(IdLugar.Casino, servicio.Actual.Lugar);

            Object.Destroy(go);
        }
    }
}
