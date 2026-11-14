using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-986: un carro puede recibir la entrada de un conductor IA en vez del teclado (<see cref="CarroViejo.ConducirConIA"/>): usa la misma física
    /// (acelera, gira, frena), no se duerme mientras lo maneja la IA, y al subir el jugador la IA se suelta.
    /// </summary>
    public class CarroConductorIAT986Pruebas
    {
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const float SegundosDeCarrera = 3f;

        private GameObject piso;
        private GameObject carroObjeto;
        private CarroViejo carro;
        private Rigidbody cuerpo;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            carroObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), new Vector3(0f, 0.6f, 0f), Quaternion.identity);
            carro = carroObjeto.GetComponent<CarroViejo>();
            cuerpo = carroObjeto.GetComponent<Rigidbody>();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(carroObjeto);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ConIA_AceleraConLaFisicaDelCarro_SinHabilitarLosControlesDelJugador()
        {
            yield return Esperar(0.5f);
            Assert.IsFalse(carro.ConIA);
            carro.ConducirConIA(() => new EntradaCarro(1f, 0f, 0f, 0f));
            Assert.IsTrue(carro.ConIA);
            Assert.IsFalse(carro.TieneControl, "No toma el control del jugador");
            Assert.IsFalse(cuerpo.isKinematic);
            yield return Esperar(SegundosDeCarrera);
            Assert.Greater(Vector3.Dot(cuerpo.linearVelocity, carroObjeto.transform.forward), 5f, "Acelera hacia adelante con el motor del carro");
            Assert.AreEqual(1f, carro.EntradaActual.Acelerar, 0.001f);
            Assert.IsFalse(carro.EstaDormido);
        }

        [UnityTest]
        public IEnumerator ConIA_ElVolanteGiraAlCarro_YElFrenoDeManoLoDetiene()
        {
            carro.ConducirConIA(() => new EntradaCarro(1f, 0f, 1f, 0f));
            yield return Esperar(SegundosDeCarrera);
            Assert.Greater(Mathf.DeltaAngle(0f, carroObjeto.transform.eulerAngles.y), 20f, "Volante a la derecha: el carro gira a la derecha");
            Assert.AreNotEqual(0f, carro.Volante, "El volante se mueve suavizado como el del jugador");
            carro.ConducirConIA(() => new EntradaCarro(0f, 0f, 0f, 1f));
            yield return Esperar(SegundosDeCarrera);
            Assert.Less(cuerpo.linearVelocity.magnitude, 0.5f, "Con el freno de mano se detiene");
        }

        [UnityTest]
        public IEnumerator ConIA_NoSeDuermeAunqueEsteQuieto_YSinIASiSeDuerme()
        {
            carro.ConducirConIA(() => default);
            yield return Esperar(SegundosDeCarrera);
            Assert.IsFalse(carro.EstaDormido, "Mientras lo maneja la IA no se duerme");
            carro.SoltarIA();
            Assert.IsFalse(carro.ConIA);
            yield return Esperar(SegundosDeCarrera);
            Assert.IsTrue(carro.EstaDormido, "Soltado y quieto se duerme como un carro estacionado");
        }

        [UnityTest]
        public IEnumerator ConIA_SoloPuedeSubirElJugadorSiVaDetenido()
        {
            carro.ConducirConIA(() => new EntradaCarro(1f, 0f, 0f, 0f));
            yield return Esperar(SegundosDeCarrera);
            Assert.IsFalse(carro.PuedeSubir, "En marcha, con la IA al volante, no se puede subir");
            carro.ConducirConIA(() => new EntradaCarro(0f, 0f, 0f, 1f));
            yield return Esperar(SegundosDeCarrera);
            Assert.IsTrue(carro.PuedeSubir, "Detenido se puede subir (robar)");
        }

        [UnityTest]
        public IEnumerator AlSubirElJugador_LaIASeSuelta_YManda_ElJugador()
        {
            carro.ConducirConIA(() => new EntradaCarro(1f, 0f, 0f, 0f));
            carro.ConducirConIA(() => new EntradaCarro(0f, 0f, 0f, 1f));
            yield return Esperar(SegundosDeCarrera);
            Assert.IsTrue(carro.IntentarSubir(null), "El jugador sube");
            Assert.IsFalse(carro.ConIA, "La IA se soltó");
            Assert.IsTrue(carro.TieneControl, "Ahora manda el jugador");
            yield return Esperar(1f);
            Assert.AreEqual(0f, carro.EntradaActual.Acelerar, 0.001f, "Sin teclas, ya no recibe la entrada de la IA");
        }
    }
}
