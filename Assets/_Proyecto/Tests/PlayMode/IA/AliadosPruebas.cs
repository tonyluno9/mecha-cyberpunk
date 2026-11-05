using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Aliados;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>T-405: reclutar al CHATO con E, que te siga y que dispare a un pandillero a 12 m.</summary>
    public class AliadosPruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";

        private class TerritorioFalso : ITerritorio
        {
            public IReadOnlyList<string> Zonas => new[] { "fosa" };
            public string ZonaEn(Vector3 posicion) => "fosa";
            public string Nombre(string zona) => "SUBURBIOS";
            public IdPandilla Dueno(string zona) => IdPandilla.Jugador;
            public int Respeto(string zona) => 10;
            public bool JefeDisponible(string zona) => false;
            public void RegistrarVencido(Vector3 posicion, IdPandilla pandilla, bool jefe) { }
            public void RegistrarCaida(Vector3 posicion) { }
            public event Action<string> Cambio { add { } remove { } }
            public event Action<string, IdPandilla> Conquistada { add { } remove { } }
        }

        private Keyboard teclado;
        private GameObject piso;
        private GameObject jugador;
        private GameObject servicios;
        private GameObject pandillero;
        private Func<float> azarAnterior;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 0f;
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            Registro<ITerritorio>.Agregar(new TerritorioFalso());

            servicios = new GameObject("ServiciosAliados");
            servicios.SetActive(false);
            PandillaJugador pandilla = servicios.AddComponent<PandillaJugador>();
            SerializedObject so = new SerializedObject(pandilla);
            so.FindProperty("prefabAliado").objectReferenceValue = CreadorAliadoParaPruebas();
            so.ApplyModifiedPropertiesWithoutUndo();
            GameObject chato = new GameObject("Chato");
            chato.transform.SetParent(servicios.transform);
            chato.transform.position = new Vector3(0f, 0f, 1.5f);
            chato.AddComponent<Reclutable>().Configurar("EL CHATO", 0, 10, "fosa");
            servicios.SetActive(true);
        }

        private static GameObject CreadorAliadoParaPruebas() => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/IA/Prefabs/Aliado.prefab");

        public override void TearDown()
        {
            PistolaPandillero.Azar = azarAnterior;
            foreach (AliadoAPie a in AliadoAPie.Activos.ToArray())
            {
                UnityEngine.Object.Destroy(a.gameObject);
            }
            UnityEngine.Object.Destroy(pandillero);
            UnityEngine.Object.Destroy(servicios);
            UnityEngine.Object.Destroy(jugador);
            UnityEngine.Object.Destroy(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
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
        public IEnumerator ReclutarSeguirYPelear()
        {
            yield return Esperar(0.5f);
            IPandillaJugador pandilla = Registro<IPandillaJugador>.Actual;
            Assert.IsNotNull(pandilla);
            Assert.AreEqual(0, pandilla.Cantidad);

            PressAndRelease(teclado.eKey);
            yield return Esperar(0.5f);
            Assert.AreEqual(1, pandilla.Cantidad, "E junto al CHATO lo recluta.");
            Assert.AreEqual("EL CHATO", pandilla.Nombre(0));

            Press(teclado.wKey);
            yield return Esperar(4f);
            Release(teclado.wKey);
            yield return Esperar(0.5f);
            Transform raiz = Registro<IJugador>.Actual.Raiz;
            Assert.AreEqual(1, pandilla.Aliados.Count);
            Transform aliado = pandilla.Aliados[0];
            Vector3 d = aliado.position - raiz.position;
            d.y = 0f;
            Assert.Less(d.magnitude, 8f, "El aliado te sigue.");

            // Un pandillero a 12 m del jugador, en reposo.
            pandillero = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero),
                raiz.position + raiz.forward * 12f + Vector3.up * 0.05f, Quaternion.identity);
            SerializedObject so = new SerializedObject(pandillero.GetComponent<PandilleroAPie>());
            so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            so.ApplyModifiedPropertiesWithoutUndo();
            PandilleroAPie rival = pandillero.GetComponent<PandilleroAPie>();
            yield return null;
            float vidaAntes = rival.Actual;
            float fin = Time.time + 6f;
            while (Time.time < fin && rival.Actual >= vidaAntes)
            {
                yield return null;
            }
            Assert.Less(rival.Actual, vidaAntes, "El aliado le dispara al pandillero.");
        }

        private class JugadorEnvuelto : IJugador
        {
            private readonly IJugador real;
            public ModoJugador ModoFalso = ModoJugador.APie;
            public JugadorEnvuelto(IJugador real) { this.real = real; }
            public Transform Raiz => real.Raiz;
            public ModoJugador Modo => ModoFalso;
            public Camera Camara => real.Camara;
            public event Action<ModoJugador> ModoCambio { add { } remove { } }
            public void Colocar(Pose pose) => real.Colocar(pose);
        }

        // T-630: con F esperan, con F otra vez siguen, y al subirte a un carro van contigo (ocultos) y bajan a tu lado.
        [UnityTest]
        public IEnumerator EsperarSeguirYSubirseAlCarro()
        {
            yield return Esperar(0.5f);
            PressAndRelease(teclado.eKey);
            yield return Esperar(0.5f);
            IPandillaJugador pandilla = Registro<IPandillaJugador>.Actual;
            Assert.AreEqual(1, pandilla.Cantidad);
            IJugador real = Registro<IJugador>.Actual;
            Transform raiz = real.Raiz;
            AliadoAPie aliado = AliadoAPie.Activos[0];

            PressAndRelease(teclado.fKey);
            yield return Esperar(0.3f);
            Assert.IsFalse(pandilla.Siguiendo, "F lo manda a esperar.");
            Vector3 donde = aliado.transform.position;
            raiz.GetComponent<CharacterController>().enabled = false;
            raiz.position += new Vector3(0f, 0f, 20f);
            raiz.GetComponent<CharacterController>().enabled = true;
            yield return Esperar(1.5f);
            Assert.Less((aliado.transform.position - donde).magnitude, 0.5f, "Esperando no se mueve aunque te alejes.");

            PressAndRelease(teclado.fKey);
            yield return Esperar(4f);
            Assert.IsTrue(pandilla.Siguiendo, "F otra vez: sígueme.");
            Vector3 d = aliado.transform.position - raiz.position;
            d.y = 0f;
            Assert.Less(d.magnitude, 8f, "Te alcanza.");

            JugadorEnvuelto falso = new JugadorEnvuelto(real) { ModoFalso = ModoJugador.Carro };
            Registro<IJugador>.Quitar(real);
            Registro<IJugador>.Agregar(falso);
            raiz.GetComponent<CharacterController>().enabled = false;
            raiz.position += new Vector3(30f, 0f, 0f);
            raiz.GetComponent<CharacterController>().enabled = true;
            yield return Esperar(0.5f);
            Assert.IsTrue(aliado.Abordo, "Al subirte al carro, se sube contigo.");
            d = aliado.transform.position - raiz.position;
            d.y = 0f;
            Assert.Less(d.magnitude, 1f, "Va en tu posición dentro del carro.");

            falso.ModoFalso = ModoJugador.APie;
            yield return Esperar(0.5f);
            Assert.IsFalse(aliado.Abordo, "Al bajar, aparece.");
            d = aliado.transform.position - raiz.position;
            d.y = 0f;
            Assert.Less(d.magnitude, 5f, "Baja a tu lado.");
            Registro<IJugador>.Quitar(falso);
            Registro<IJugador>.Agregar(real);
        }
    }
}
