using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-509: el arma tirada (la pistola que suelta el SKULL) se ve como objeto para recoger: luz pequeña, modelo que gira y flota,
    /// el punto de interacción quieto en el piso y el texto "Recoger ...".
    /// </summary>
    public class ArmaEnPisoBrilloPruebas
    {
        private GameObject raiz;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.Destroy(raiz);
            Registros.LimpiarTodos();
        }

        [UnityTest]
        public IEnumerator ArmaEnPiso_Brilla_GiraYFlota_SinMoverSuPuntoDeInteraccion()
        {
            raiz = new GameObject("PistolaEnPiso");
            raiz.transform.position = new Vector3(3f, 0.1f, 2f);
            GameObject pieza = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pieza.name = "Corredera";
            pieza.transform.SetParent(raiz.transform, false);
            pieza.transform.localScale = new Vector3(0.05f, 0.05f, 0.26f);
            ArmaEnPiso arma = raiz.AddComponent<ArmaEnPiso>();
            yield return null;

            Light luz = raiz.GetComponentInChildren<Light>();
            Assert.IsNotNull(luz, "Tiene luz de objeto para recoger");
            Assert.AreEqual(LightType.Point, luz.type);
            Assert.AreEqual(LightShadows.None, luz.shadows, "La luz no proyecta sombra (barata)");
            Assert.Greater(luz.intensity, 0f);
            Assert.AreNotEqual(raiz.transform, pieza.transform.parent, "El modelo cuelga de un pivote que gira y flota");

            Vector3 posicionRaiz = raiz.transform.position;
            Quaternion giroAntes = pieza.transform.rotation;
            float alturaMinima = float.MaxValue;
            float alturaMaxima = float.MinValue;
            float fin = Time.time + 1f;
            while (Time.time < fin)
            {
                yield return null;
                alturaMinima = Mathf.Min(alturaMinima, pieza.transform.position.y);
                alturaMaxima = Mathf.Max(alturaMaxima, pieza.transform.position.y);
            }
            Assert.Greater(Quaternion.Angle(giroAntes, pieza.transform.rotation), 20f, "El modelo gira");
            Assert.Greater(alturaMaxima - alturaMinima, 0.03f, "El modelo sube y baja");
            Assert.Greater(alturaMinima, posicionRaiz.y, "Flota sobre el piso");
            Assert.AreEqual(posicionRaiz, raiz.transform.position, "El punto de interacción no se mueve");
            Assert.AreEqual(posicionRaiz, arma.Posicion);
            StringAssert.StartsWith("Recoger", arma.Texto);
            Assert.IsTrue(arma.PuedeUsar);
            Assert.IsTrue(System.Linq.Enumerable.Contains(Registro<IInteractuable>.Todos, arma), "Se registra como interactuable (E recoger)");
        }
    }
}
