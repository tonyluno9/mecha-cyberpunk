using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-650: agarre de las armas en la mano derecha, punto de la mano izquierda y corrección del cañón al apuntar.</summary>
    public class AgarresArmasT650Pruebas
    {
        [TestCase("Pistola")]
        [TestCase("Fusil")]
        [TestCase("Escopeta")]
        [TestCase("Sniper")]
        public void Agarre_ConocidoParaCadaArma_ConElCanonPorLosDedos(string arma)
        {
            Assert.IsTrue(AgarresArmas.TryObtener(arma, "RightHand", out AgarreArma a));
            // Modelos mixamorig: con -90° en X el +Z del arma (cañón) cae en el +Y de la mano (los dedos) y la corredera (+Y) en -Z (hacia el pulgar).
            Quaternion r = Quaternion.Euler(a.Rotacion);
            Assert.AreEqual(1f, Vector3.Dot(r * Vector3.forward, Vector3.up), 0.001f, "cañón = dedos");
            Assert.AreEqual(1f, Vector3.Dot(r * Vector3.up, Vector3.back), 0.001f, "corredera = lado del pulgar");
            Assert.Less(a.Posicion.magnitude, 0.2f, "El arma queda cerca de la palma (m)");
        }

        [Test]
        public void Agarre_PJ01_DeLaPistola_EsElCasoADeAgarreTxt()
        {
            Assert.IsTrue(AgarresArmas.TryObtener("Pistola", AgarresArmas.ManoDerechaPJ01, out AgarreArma a));
            Assert.AreEqual(0f, Vector3.Distance(new Vector3(0.0129f, 0.1181f, 0.0080f), a.Posicion), 0.0001f);
            // El nodo Pistola * giro del modelo (180° sobre Y) * inverso del giro del FBX de Tony = el cuaternión de agarre.txt.
            Quaternion nodo = Quaternion.Euler(a.Rotacion);
            Quaternion compuesto = nodo * AgarresArmas.GiroModeloPistola * Quaternion.Inverse(AgarresArmas.GiroFbxDeTony);
            Assert.Less(Quaternion.Angle(AgarresArmas.CuaternionTony, compuesto), 0.3f, "El agarre compuesto es el cuaternión de Tony");
            Assert.AreEqual(-0.05021f, AgarresArmas.CuaternionTony.x, 0.00001f);
            Assert.AreEqual(0.14534f, AgarresArmas.CuaternionTony.w, 0.00001f);
            // El nodo conserva los ejes de siempre: con la pistola nivelada su X es horizontal; aquí basta que sea un giro propio sin escala.
            Assert.AreEqual(1f, nodo.x * nodo.x + nodo.y * nodo.y + nodo.z * nodo.z + nodo.w * nodo.w, 0.001f);
        }

        [TestCase("Fusil")]
        [TestCase("Escopeta")]
        [TestCase("Sniper")]
        public void Agarre_PJ01_DeLasArmasLargas_SigueSiendoElDeSiempre(string arma)
        {
            Assert.IsTrue(AgarresArmas.TryObtener(arma, AgarresArmas.ManoDerechaPJ01, out AgarreArma a));
            Assert.AreEqual(0f, Vector3.Distance(new Vector3(-0.025f, 0.06f, -0.035f), a.Posicion), 0.0001f);
            Assert.AreEqual(0f, Vector3.Distance(new Vector3(-90f, 0f, 0f), a.Rotacion), 0.0001f);
        }

        [Test]
        public void Agarre_ArmaDesconocida_NoExiste()
        {
            Assert.IsFalse(AgarresArmas.TryObtener("Lanzallamas", out _));
        }

        [Test]
        public void CorreccionDeArma_CanonTorcidoAlaIzquierda_LoLlevaAlFrenteConservandoLaInclinacion()
        {
            Quaternion arma = Quaternion.LookRotation(new Vector3(-0.5f, 0.2f, 0.85f).normalized, Vector3.up);
            Quaternion c = AgarresArmas.CorreccionDeArma(arma, Vector3.forward, 1f, 90f);
            Vector3 canon = c * arma * Vector3.forward;
            Assert.AreEqual(0f, canon.x, 0.01f, "Mira al frente");
            Assert.AreEqual(0.2f / new Vector3(-0.5f, 0.2f, 0.85f).magnitude, canon.y, 0.01f, "Conserva la inclinación");
            Assert.Greater(canon.z, 0.9f);
        }

        [Test]
        public void CorreccionDeArma_ConInclinacion_LaFija_YNivelaLaCorredera()
        {
            Quaternion arma = Quaternion.LookRotation(new Vector3(0.7f, 0.7f, 0f).normalized, Vector3.up) * Quaternion.Euler(0f, 0f, 30f);
            Quaternion c = AgarresArmas.CorreccionDeArma(arma, Vector3.forward, 1f, 150f, -5f);
            Quaternion final = c * arma;
            Vector3 canon = final * Vector3.forward;
            Assert.AreEqual(Mathf.Sin(-5f * Mathf.Deg2Rad), canon.y, 0.01f, "Inclinación fija");
            Assert.Greater(canon.z, 0.95f, "Al frente");
            Assert.AreEqual(0f, (final * Vector3.right).y, 0.01f, "Sin ladearse");
        }

        [Test]
        public void CorreccionDeArma_SinPeso_NoGira()
        {
            Quaternion arma = Quaternion.LookRotation(Vector3.left, Vector3.up);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, AgarresArmas.CorreccionDeArma(arma, Vector3.forward, 0f, 90f)), 0.001f);
        }

        [Test]
        public void CorreccionDeArma_LimitaElGiro()
        {
            Quaternion arma = Quaternion.LookRotation(Vector3.right, Vector3.up);
            float grados = Quaternion.Angle(Quaternion.identity, AgarresArmas.CorreccionDeArma(arma, Vector3.forward, 1f, 30f));
            Assert.LessOrEqual(grados, 30.5f);
        }

        [Test]
        public void PrefabDelJugador_TieneLasArmasConSuAgarre_YElPuntoDeLaManoIzquierda()
        {
            GameObject contenido = PrefabUtility.LoadPrefabContents(AplicarAgarresArmas.RutaJugador);
            try
            {
                ApuntadoIK ik = contenido.GetComponentInChildren<ApuntadoIK>(true);
                Assert.IsNotNull(ik);
                foreach (string nombre in new[] { "Pistola", "Fusil", "Escopeta", "Sniper" })
                {
                    Transform arma = ik.SocketArma.Find(nombre);
                    Assert.IsNotNull(arma, nombre);
                    AgarresArmas.TryObtener(nombre, out AgarreArma a);
                    Assert.AreEqual(0f, Vector3.Distance(a.Posicion, arma.localPosition), 0.001f, nombre + " posición");
                    Assert.AreEqual(0f, Quaternion.Angle(Quaternion.Euler(a.Rotacion), arma.localRotation), 0.1f, nombre + " rotación");
                    Transform izq = arma.Find(AgarresArmas.NombreAgarreIzquierdo);
                    Assert.IsNotNull(izq, nombre + " tiene AgarreIzq");
                    Assert.AreEqual(0f, Vector3.Distance(a.PosicionIzquierda, izq.localPosition), 0.001f);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contenido);
            }
        }

        [Test]
        public void Controlador_LaCapaArriba_TieneIKPass_ParaLaManoIzquierda()
        {
            var c = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ConstructorControladorBase.RutaControlador);
            Assert.IsTrue(c.layers[0].iKPass);
            Assert.IsTrue(c.layers[1].iKPass);
        }
    }
}
