using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Pruebas;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Pruebas jugables de la cámara libre: prefab real, piso y pared de prueba, entrada simulada.</summary>
    public class CamaraLibrePruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mundo/Prefabs/CamaraLibre.prefab";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float DistanciaPared = 6f;

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject pared;
        private GameObject camaraObjeto;
        private CamaraLibre camara;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();

            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);

            // Pared de 8 m de alto y 1 m de grosor; su cara cercana está en z = 5.5.
            pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pared.transform.position = new Vector3(0f, 4f, DistanciaPared);
            pared.transform.localScale = new Vector3(40f, 8f, 1f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            camaraObjeto = Object.Instantiate(prefab, new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            camara = camaraObjeto.GetComponent<CamaraLibre>();
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(camaraObjeto);
            Object.DestroyImmediate(pared);
            Object.DestroyImmediate(piso);
            base.TearDown();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        // Los primeros frames de PlayMode son lentos: se espera a que deltaTime se estabilice.
        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private void QuitarPared()
        {
            Object.DestroyImmediate(pared);
            pared = null;
        }

        [UnityTest]
        public IEnumerator Caminar_WDosSegundos_AvanzaEntre7y10Metros()
        {
            QuitarPared();
            yield return Calentar();
            Vector3 inicio = camara.transform.position;
            Press(teclado.wKey);
            yield return Esperar(2f);
            Release(teclado.wKey);
            float avance = Vector3.Distance(inicio, camara.transform.position);
            Assert.That(avance, Is.InRange(7f, 10f));
        }

        [UnityTest]
        public IEnumerator Correr_ShiftYW_AvanzaMasQueCaminando()
        {
            QuitarPared();
            yield return Calentar();
            Vector3 inicio = camara.transform.position;
            Press(teclado.leftShiftKey);
            Press(teclado.wKey);
            yield return Esperar(2f);
            Release(teclado.wKey);
            Release(teclado.leftShiftKey);
            float avance = Vector3.Distance(inicio, camara.transform.position);
            Assert.Greater(avance, 10f, "Corriendo debe avanzar más que los 10 m máximos de caminar");
        }

        [UnityTest]
        public IEnumerator Mirar_MoverElRaton_GiraLaVista()
        {
            yield return Calentar();
            float yawAntes = camara.Yaw;
            Set(raton.delta, new Vector2(100f, 0f));
            yield return null;
            yield return null;
            Assert.Greater(camara.Yaw - yawAntes, 5f, "Mover el ratón a la derecha debe girar la vista a la derecha");

            Set(raton.delta, new Vector2(0f, -100f));
            yield return null;
            yield return null;
            Assert.Greater(camara.Inclinacion, 5f, "Mover el ratón hacia abajo debe inclinar la vista hacia abajo");
        }

        [UnityTest]
        public IEnumerator Caminar_ContraUnaPared_QuedaFueraDeSuCollider()
        {
            yield return Calentar();
            Press(teclado.wKey);
            yield return Esperar(4f);
            Release(teclado.wKey);
            Collider colliderPared = pared.GetComponent<Collider>();
            Vector3 ojos = camara.Camara.transform.position;
            Assert.IsFalse(colliderPared.bounds.Contains(ojos), "La cámara no debe quedar dentro de la pared");
            Assert.Less(camara.transform.position.z, DistanciaPared - 0.5f, "La cámara debe quedar antes de la pared");
        }

        [UnityTest]
        public IEnumerator Volar_ConFYEspacio_SubeYAtraviesaLaPared()
        {
            yield return Calentar();
            PressAndRelease(teclado.fKey);
            yield return null;
            yield return null;
            Assert.IsTrue(camara.EstaVolando, "F debe activar el vuelo");

            Press(teclado.spaceKey);
            yield return Esperar(0.2f);
            Release(teclado.spaceKey);
            Assert.That(camara.transform.position.y, Is.InRange(2f, 7f), "Espacio debe subir sin pasar por encima de la pared de 8 m");

            Press(teclado.wKey);
            yield return Esperar(1f);
            Release(teclado.wKey);
            Assert.Greater(camara.transform.position.z, DistanciaPared + 1f, "Volando debe atravesar la pared sin chocar");
        }

        [UnityTest]
        public IEnumerator Volar_ConFOtraVez_VuelveACaminar()
        {
            yield return Calentar();
            PressAndRelease(teclado.fKey);
            yield return null;
            yield return null;
            Press(teclado.spaceKey);
            yield return Esperar(0.3f);
            Release(teclado.spaceKey);
            float alturaVolando = camara.transform.position.y;
            Assert.Greater(alturaVolando, 2f);

            PressAndRelease(teclado.fKey);
            yield return Esperar(2f);
            Assert.IsFalse(camara.EstaVolando, "F otra vez debe volver a caminar");
            Assert.Less(camara.transform.position.y, 0.5f, "Al caminar debe caer hasta el piso");

            Vector3 antes = camara.transform.position;
            QuitarPared();
            Press(teclado.sKey);
            yield return Esperar(1f);
            Release(teclado.sKey);
            Assert.Greater(Vector3.Distance(antes, camara.transform.position), 3f, "Caminando debe volver a moverse");
        }

        [UnityTest]
        public IEnumerator Registro_ConLaCamaraEnEscena_EsElJugadorActual()
        {
            yield return Calentar();
            Assert.AreSame(camara, Registro<IJugador>.Actual);
            Assert.AreEqual(ModoJugador.APie, Registro<IJugador>.Actual.Modo);
            Assert.AreSame(camara.Camara, Registro<IJugador>.Actual.Camara);
        }

        [UnityTest]
        public IEnumerator Colocar_ConUnaPose_MueveSinAtravesarElPiso()
        {
            QuitarPared();
            yield return Calentar();
            Pose pose = new Pose(new Vector3(30f, 0.05f, -20f), Quaternion.Euler(0f, 90f, 0f));
            Registro<IJugador>.Actual.Colocar(pose);
            Assert.AreEqual(30f, camara.transform.position.x, 0.01f);
            Assert.AreEqual(-20f, camara.transform.position.z, 0.01f);
            Assert.AreEqual(90f, camara.transform.eulerAngles.y, 0.5f);
            Assert.IsTrue(camaraObjeto.GetComponent<CharacterController>().enabled, "El CharacterController debe quedar prendido");

            yield return Esperar(1f);
            Assert.GreaterOrEqual(camara.transform.position.y, -0.05f, "No debe atravesar el piso");
            Assert.AreEqual(30f, camara.transform.position.x, 0.05f);
        }
    }
}
