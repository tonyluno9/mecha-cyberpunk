using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1643 (T23): los peatones no atraviesan paredes, postes ni carros que cruzan su ruta, los rodean o dan la vuelta, y no aparecen dentro de objetos.</summary>
    public class NpcsColisionT1643Pruebas
    {
        private static readonly Vector3[] Ruta = { new Vector3(0f, 0f, 0f), new Vector3(200f, 0f, 0f) };

        private GameObject raiz;
        private ConfigPeatones config;

        [SetUp]
        public void Preparar()
        {
            EventosRuido.LimpiarSuscriptores();
            config = ScriptableObject.CreateInstance<ConfigPeatones>();
            raiz = new GameObject("Peatones_T1643");
        }

        [TearDown]
        public void Limpiar()
        {
            EventosRuido.LimpiarSuscriptores();
            Object.Destroy(raiz);
            Object.Destroy(config);
        }

        private PeatonMundo CrearPeaton(float metros)
        {
            GameObject objeto = new GameObject("Peaton");
            objeto.transform.SetParent(raiz.transform, false);
            objeto.SetActive(false);
            PeatonMundo peaton = objeto.AddComponent<PeatonMundo>();
            peaton.Preparar(Ruta, config, new System.Random(1), () => (Vector3?)null, metros / config.Velocidad);
            objeto.SetActive(true);
            return peaton;
        }

        private BoxCollider CrearCaja(Vector3 centro, Vector3 tamano)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.name = "Obstaculo_T1643";
            caja.transform.SetParent(raiz.transform, false);
            caja.transform.position = centro;
            caja.transform.localScale = tamano;
            Physics.SyncTransforms();
            return caja.GetComponent<BoxCollider>();
        }

        // Cuántos cuadros se solapó el cuerpo del peatón con el obstáculo mientras corren 'segundos'.
        private IEnumerator Observar(PeatonMundo peaton, Collider obstaculo, float segundos, System.Action<int> alTerminar)
        {
            CapsuleCollider cuerpo = peaton.GetComponent<CapsuleCollider>();
            int cuadrosDentro = 0;
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
                Physics.SyncTransforms();
                if (Physics.ComputePenetration(cuerpo, cuerpo.transform.position, cuerpo.transform.rotation,
                        obstaculo, obstaculo.transform.position, obstaculo.transform.rotation, out _, out float profundidad) && profundidad > 0.05f)
                {
                    cuadrosDentro++;
                }
            }
            alTerminar(cuadrosDentro);
        }

        [UnityTest]
        public IEnumerator Pared_EnLaRuta_NoLaAtraviesa()
        {
            PeatonMundo peaton = CrearPeaton(50f);
            BoxCollider pared = CrearCaja(new Vector3(54f, 1.5f, 0f), new Vector3(0.5f, 3f, 6f));
            int dentro = -1;
            yield return Observar(peaton, pared, 6f, n => dentro = n);
            Assert.AreEqual(0, dentro, "El cuerpo nunca se metió en la pared");
            Assert.Less(peaton.transform.position.x, 53.75f, "No pasó al otro lado de la pared");
            Assert.GreaterOrEqual(peaton.Vueltas, 1, "Dio la vuelta ante la pared");
        }

        [UnityTest]
        public IEnumerator PosteEstrecho_EnLaRuta_LoRodeaSinAtravesarlo()
        {
            PeatonMundo peaton = CrearPeaton(50f);
            BoxCollider poste = CrearCaja(new Vector3(54f, 1f, 0f), new Vector3(0.5f, 2f, 0.5f));
            int dentro = -1;
            yield return Observar(peaton, poste, 9f, n => dentro = n);
            Assert.AreEqual(0, dentro, "El cuerpo nunca se metió en el poste");
            Assert.Greater(peaton.Bloqueos, 0, "Detectó el poste por delante");
            Assert.Greater(peaton.transform.position.x, 55f, "Rodeó el poste y siguió su camino (está en " + peaton.transform.position + ")");
        }

        [UnityTest]
        public IEnumerator CarroEstacionado_EnLaRuta_NoLoAtraviesa()
        {
            PeatonMundo peaton = CrearPeaton(50f);
            BoxCollider carro = CrearCaja(new Vector3(54f, 0.8f, 0f), new Vector3(4.5f, 1.6f, 2f));
            int dentro = -1;
            yield return Observar(peaton, carro, 9f, n => dentro = n);
            Assert.AreEqual(0, dentro, "El cuerpo nunca se metió en el carro");
        }

        [UnityTest]
        public IEnumerator AparecerDentroDeUnaCaja_SeReubicaFuera()
        {
            BoxCollider caja = CrearCaja(new Vector3(50f, 1.5f, 0f), new Vector3(4f, 3f, 4f));
            PeatonMundo peaton = CrearPeaton(50f);
            yield return null;
            Assert.Greater(peaton.Reubicaciones, 0, "Se adelantó por la ruta porque apareció dentro de la caja");
            Assert.IsTrue(MovimientoSeguro.Libre(peaton.transform.position, peaton.transform), "Terminó en un sitio libre (" + peaton.transform.position + ")");
            Assert.IsFalse(caja.bounds.Contains(peaton.transform.position + Vector3.up), "No quedó dentro de la caja");
        }

        [UnityTest]
        public IEnumerator Libre_IgnoraPisoYTriggers_PeroNoParedes()
        {
            CrearCaja(new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 20f)); // piso: su cara de arriba queda a y = 0
            BoxCollider gatillo = CrearCaja(new Vector3(5f, 1f, 0f), new Vector3(2f, 2f, 2f));
            gatillo.isTrigger = true;
            CrearCaja(new Vector3(-5f, 1f, 0f), new Vector3(2f, 2f, 2f));
            yield return null;
            Physics.SyncTransforms();
            Assert.IsTrue(MovimientoSeguro.Libre(new Vector3(0f, 0f, 0f), null), "El piso no cuenta");
            Assert.IsTrue(MovimientoSeguro.Libre(new Vector3(5f, 0f, 0f), null), "Un trigger no cuenta");
            Assert.IsFalse(MovimientoSeguro.Libre(new Vector3(-5f, 0f, 0f), null), "Una caja sólida sí cuenta");
        }
    }
}
