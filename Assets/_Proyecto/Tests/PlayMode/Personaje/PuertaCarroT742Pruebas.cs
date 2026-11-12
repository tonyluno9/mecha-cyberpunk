using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-742 y T-972: subir y bajar de V01a, V01b, V05 y la patrulla sin puertas (decisión del 9-nov): el jugador llega a manejar (fase Dentro) y baja,
    /// por los dos lados, y el carro no tiene objetos Puerta_*. Entrada simulada, jugador real y capturas con CAPTURAR_S1=1 (docs/capturas/t972/).
    /// </summary>
    public class PuertaCarroT742Pruebas : BaseJugadorAPie
    {
        private static readonly string[] Codigos = { "V01a", "V01b", "V05" };
        private const float SegundosLimite = 6f;
        private IMontable carro;
        private GameObject carroObj;

        public override void TearDown()
        {
            if (carroObj != null)
            {
                Object.Destroy(carroObj);
            }
            base.TearDown();
        }

        private IEnumerator PrepararCarro(string codigo)
        {
            yield return Calentar();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab");
            carroObj = Object.Instantiate(prefab, new Vector3(0f, 0.3f, 6f), Quaternion.identity);
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m is Component c && c.gameObject == carroObj) { carro = m; }
            }
            Assert.IsNotNull(carro, "El carro se registra");
            yield return Esperar(1f);
        }

        private IEnumerator Tecla()
        {
            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
        }

        private void PonerJugadorAfuera(bool izquierda)
        {
            Vector3 punto = carro.PuntoSubida;
            if (!izquierda)
            {
                punto.x = 2f * carroObj.transform.position.x - punto.x;
            }
            Vector3 hacia = izquierda ? Vector3.right : Vector3.left;
            Vector3 inicio = punto - hacia * 1f;
            inicio.y = 0.05f;
            jugadorAPie.Colocar(new Pose(inicio, Quaternion.LookRotation(hacia)));
        }

        // Foto del carro por el lado del conductor (izquierda), con el jugador sentado. Solo con CAPTURAR_S1=1.
        private void FotoConductor(string nombre)
        {
            Vector3 c = carroObj.transform.position;
            CapturaMundoS1.Foto(nombre, c + new Vector3(-4.2f, 1.5f, -1.2f), c + new Vector3(0f, 0.9f, 0.0f), 42f, false, 1280, 720);
        }

        private IEnumerator Subir(bool izquierda)
        {
            PonerJugadorAfuera(izquierda);
            yield return Esperar(0.3f);
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            yield return Tecla();
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "La E sube");
            float limite = Time.time + SegundosLimite;
            while (anim.Fase == AnimacionCarro.FaseCarro.Entrando && Time.time < limite)
            {
                yield return null;
            }
            Assert.AreEqual(AnimacionCarro.FaseCarro.Dentro, anim.Fase, "Terminó de entrar: ya maneja");
        }

        private IEnumerator Bajar()
        {
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            yield return Tecla();
            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo, "La E baja");
            float limite = Time.time + SegundosLimite;
            while (anim.Fase == AnimacionCarro.FaseCarro.Saliendo && Time.time < limite)
            {
                yield return null;
            }
            Assert.AreEqual(AnimacionCarro.FaseCarro.Fuera, anim.Fase, "Terminó de bajar");
        }

        private void SinPuertas()
        {
            foreach (Transform t in carroObj.GetComponentsInChildren<Transform>(true))
            {
                StringAssert.DoesNotStartWith("Puerta_", t.name, "El carro no tiene objetos de puerta (" + t.name + ")");
            }
        }

        [UnityTest]
        public IEnumerator SubirYBajar_SinPuertas_ElJugadorLlegaADentroYBaja([ValueSource(nameof(Codigos))] string codigo)
        {
            yield return PrepararCarro(codigo);
            SinPuertas();
            yield return Subir(true);
            yield return Esperar(0.5f);
            if (codigo == "V01a")
            {
                FotoConductor("t972/v01a-jugador-dentro");
            }
            yield return Bajar();
            SinPuertas();
        }

        [UnityTest]
        public IEnumerator SubirYBajar_PorElLadoDelCopiloto_SinPuertas()
        {
            yield return PrepararCarro("V01a");
            yield return Subir(false);
            yield return Bajar();
        }

        [UnityTest]
        public IEnumerator SubirYBajar_Patrulla_SinPuertas()
        {
            yield return Calentar();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/Patrulla.prefab");
            carroObj = Object.Instantiate(prefab, new Vector3(0f, 0.3f, 6f), Quaternion.identity);
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m is Component c && c.gameObject == carroObj) { carro = m; }
            }
            Assert.IsNotNull(carro, "La patrulla se puede montar");
            yield return Esperar(1f);
            SinPuertas();
            yield return Subir(true);
            yield return Bajar();
        }

        [UnityTest]
        public IEnumerator Subir_ElJugadorVaDentroSinVidrio_LaCabezaQuedaDentroDeLaSilueta()
        {
            // Sin vidrio: el conductor sentado queda entre el techo y la línea de la carrocería y nada tapa su modelo.
            yield return PrepararCarro("V01a");
            yield return Subir(true);
            yield return Esperar(0.9f);
            MechaCyberpunk.Personaje.Visual.VisualPersonaje visual = jugador.GetComponentInChildren<MechaCyberpunk.Personaje.Visual.VisualPersonaje>(true);
            Assert.IsTrue(visual.gameObject.activeInHierarchy, "El modelo del conductor se ve");
            Transform cabeza = visual.Animador.GetBoneTransform(HumanBodyBones.Head);
            Bounds todo = new Bounds(carroObj.transform.position, Vector3.zero);
            foreach (Renderer r in carroObj.GetComponentsInChildren<Renderer>())
            {
                todo.Encapsulate(r.bounds);
            }
            Assert.IsTrue(todo.Contains(cabeza.position), "La cabeza del conductor está dentro del volumen del carro: " + cabeza.position + " vs " + todo);
            foreach (Renderer r in carroObj.GetComponentsInChildren<Renderer>())
            {
                foreach (Material m in r.sharedMaterials)
                {
                    StringAssert.DoesNotContain("Vidrio", m.name, "Ninguna pieza del carro es de vidrio");
                }
            }
        }
    }
}
