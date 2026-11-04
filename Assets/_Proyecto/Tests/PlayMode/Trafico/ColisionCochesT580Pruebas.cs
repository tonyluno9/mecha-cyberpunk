using System.Collections;
using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-580: medimos si el jugador a pie y un carro empujado de verdad chocan con los carros (de tráfico cinemáticos y estacionados) o los atraviesan.
    /// Cada prefab de carro se prueba por separado.
    /// </summary>
    public class ColisionCochesT580Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string CarpetaTrafico = "Assets/_Proyecto/Trafico/Prefabs/CarroTrafico_";
        private const string CarpetaCarros = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_";
        private static readonly string[] Codigos = { "V01a", "V01b", "V02", "V03", "V04", "V05", "V06", "V08" };

        private const float VelocidadLanzada = 35f;

        private Keyboard teclado;
        private GameObject piso;
        private readonly List<GameObject> extras = new List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            teclado = InputSystem.AddDevice<Keyboard>();
            Mouse raton = InputSystem.AddDevice<Mouse>();
            MapasDePruebaTrafico.Reiniciar(teclado, raton);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
        }

        public override void TearDown()
        {
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    Object.Destroy(extra);
                }
            }
            extras.Clear();
            Object.Destroy(piso);
            Registros.LimpiarTodos();
            MapasDePruebaTrafico.AntesDelTearDown();
            base.TearDown();
            MapasDePruebaTrafico.DespuesDelTearDown();
        }


        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static string Colliders(GameObject carro)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Collider c in carro.GetComponentsInChildren<Collider>(true))
            {
                sb.Append(c.GetType().Name).Append(c.isTrigger ? "(trigger)" : "").Append(c.enabled ? "" : "(off)").Append('@').Append(c.gameObject.name).Append(' ');
            }
            return sb.Length == 0 ? "SIN COLLIDERS" : sb.ToString();
        }

        // Extremo cercano del carro en z (el jugador viene desde z negativa).
        private static float BordeCercano(GameObject carro)
        {
            float menor = float.MaxValue;
            foreach (Collider c in carro.GetComponentsInChildren<Collider>())
            {
                if (!c.isTrigger && c.enabled)
                {
                    menor = Mathf.Min(menor, c.bounds.min.z);
                }
            }
            return menor;
        }

        private IEnumerator CaminarContra(string ruta, string nombre)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, "Falta " + ruta);
            GameObject carro = Object.Instantiate(prefab, new Vector3(0f, 0.3f, 8f), Quaternion.identity);
            extras.Add(carro);
            GameObject jugadorObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador));
            extras.Add(jugadorObjeto);
            yield return null;
            yield return null;
            IJugador jugador = Registro<IJugador>.Actual;
            Debug.Log("[T-580] " + nombre + " jugador listo");
            Assert.IsNotNull(jugador, "Hay jugador");
            jugador.Colocar(new Pose(new Vector3(0f, 0.1f, 0f), Quaternion.identity));
            yield return Esperar(0.5f);
            Physics.SyncTransforms();
            float borde = BordeCercano(carro);
            Assert.Less(borde, float.MaxValue, nombre + ": el carro no tiene collider sólido. " + Colliders(carro));
            Debug.Log("[T-580] " + nombre + " camina, timeScale " + Time.timeScale);
            Press(teclado.wKey);
            yield return Esperar(4f);
            Release(teclado.wKey);
            float z = jugador.Raiz.position.z;
            Assert.Greater(z, 1f, nombre + ": el jugador sí caminó (z=" + z.ToString("F2") + ")");
            Assert.Less(z, borde + 0.05f, nombre + ": el jugador atravesó el carro (z=" + z.ToString("F2") + ", borde del carro z=" + borde.ToString("F2") + "). " + Colliders(carro));
        }

        private IEnumerator Limpiar()
        {
            foreach (GameObject extra in extras)
            {
                Object.Destroy(extra);
            }
            extras.Clear();
            Registros.LimpiarTodos();
            yield return null;
        }

        [UnityTest]
        [Timeout(150000)]
        public IEnumerator JugadorAPie_NoAtraviesaNingunCarroDeTrafico()
        {
            foreach (string codigo in Codigos)
            {
                yield return CaminarContra(CarpetaTrafico + codigo + ".prefab", "CarroTrafico_" + codigo);
                yield return Limpiar();
            }
        }

        [UnityTest]
        [Timeout(150000)]
        public IEnumerator JugadorAPie_NoAtraviesaNingunCarroEstacionado()
        {
            foreach (string codigo in Codigos)
            {
                yield return CaminarContra(CarpetaCarros + codigo + ".prefab", "Carro_" + codigo);
                yield return Limpiar();
            }
        }

        // Carro de verdad (dinámico) lanzado contra uno de tráfico (cinemático): no deben traslaparse.
        [UnityTest]
        [Timeout(150000)]
        public IEnumerator CarroEmpujado_NoAtraviesaUnCarroDeTrafico()
        {
            GameObject quieto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaTrafico + "V02.prefab"), new Vector3(0f, 0.3f, 40f), Quaternion.identity);
            GameObject lanzado = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaCarros + "V03.prefab"), new Vector3(0f, 0.3f, 0f), Quaternion.identity);
            extras.Add(quieto);
            extras.Add(lanzado);
            yield return null;
            Rigidbody cuerpo = lanzado.GetComponent<Rigidbody>();
            Assert.IsFalse(cuerpo.isKinematic);
            float fin = Time.time + 3f;
            while (Time.time < fin)
            {
                cuerpo.linearVelocity = new Vector3(0f, cuerpo.linearVelocity.y, VelocidadLanzada);
                yield return null;
            }
            float frente = float.MinValue;
            foreach (Collider c in lanzado.GetComponentsInChildren<Collider>())
            {
                if (!c.isTrigger)
                {
                    frente = Mathf.Max(frente, c.bounds.max.z);
                }
            }
            float borde = BordeCercano(quieto);
            Assert.Less(frente, borde + 0.3f, "El carro lanzado atravesó al de tráfico (frente z=" + frente.ToString("F2") + ", borde z=" + borde.ToString("F2") + "). " + Colliders(quieto) + "| " + Colliders(lanzado));
        }
    }
}
