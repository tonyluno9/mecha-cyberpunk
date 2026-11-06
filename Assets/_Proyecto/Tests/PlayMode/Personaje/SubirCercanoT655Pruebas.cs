using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-655: con E el jugador sube al carro más cercano si está a 2.5 m o menos de cualquier puerta, sin importar hacia dónde mira.</summary>
    public class SubirCercanoT655Pruebas : BaseJugadorAPie
    {
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const float Alcance = 2.5f;
        private readonly List<GameObject> carros = new List<GameObject>();

        public override void TearDown()
        {
            foreach (GameObject c in carros) { if (c != null) { Object.Destroy(c); } }
            carros.Clear();
            base.TearDown();
        }

        private IMontable CrearCarro(Vector3 posicion, float yaw = 0f)
        {
            GameObject obj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), posicion, Quaternion.Euler(0f, yaw, 0f));
            carros.Add(obj);
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m is Component c && c.gameObject == obj) { return m; }
            }
            Assert.Fail("El carro no se registró");
            return null;
        }

        private IEnumerator Tecla(KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private IEnumerator Colocar(Vector3 pos, Vector3 mira)
        {
            jugadorAPie.Colocar(new Pose(pos, Quaternion.LookRotation(mira)));
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Subir_ADosMetrosDeLaPuertaMirandoParaOtroLado_Sube()
        {
            yield return Calentar();
            IMontable carro = CrearCarro(new Vector3(0f, 0.3f, 6f));
            yield return null;
            Vector3 puerta = carro.PuntoSubida;
            Vector3 donde = puerta + new Vector3(-2.3f, 0f, 0f);
            donde.y = 0.05f;
            yield return Colocar(donde, Vector3.left);
            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo);
            Assert.LessOrEqual(Vector2.Distance(new Vector2(donde.x, donde.z), new Vector2(puerta.x, puerta.z)), Alcance);

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "Sube aunque mire en sentido contrario al carro");
            Assert.IsTrue(carro.Ocupado);
        }

        [UnityTest]
        public IEnumerator Subir_DesdeAtrasDelCarroACercaDeUnaPuerta_Sube()
        {
            yield return Calentar();
            IMontable carro = CrearCarro(new Vector3(0f, 0.3f, 6f));
            yield return null;
            Vector3 puerta = carro.PuntoSubida;
            Vector3 donde = new Vector3(puerta.x + 1f, 0.05f, puerta.z - 2.2f);
            yield return Colocar(donde, Vector3.back);

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "Desde atrás, mirando al lado contrario, también sube");
        }

        [UnityTest]
        public IEnumerator Subir_DesdeElLadoDelCopiloto_SubeAlMismoCarro()
        {
            yield return Calentar();
            IMontable carro = CrearCarro(new Vector3(0f, 0.3f, 6f));
            yield return null;
            Assert.IsTrue(PuertasMontable.TryEspejo(carro, out Vector3 opuesta));
            Vector3 donde = opuesta + new Vector3(1.5f, 0f, 0f);
            donde.y = 0.05f;
            Assert.Greater(Vector2.Distance(new Vector2(donde.x, donde.z), new Vector2(carro.PuntoSubida.x, carro.PuntoSubida.z)), Alcance,
                "Del lado del copiloto la puerta del conductor queda fuera de alcance");
            yield return Colocar(donde, Vector3.right);

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "La puerta del copiloto también cuenta");
            Assert.IsTrue(carro.Ocupado);
        }

        [UnityTest]
        public IEnumerator Subir_MasLejosDe2Punto5MDeTodasLasPuertas_NoSube()
        {
            yield return Calentar();
            IMontable carro = CrearCarro(new Vector3(0f, 0.3f, 6f));
            yield return null;
            Vector3 donde = carro.PuntoSubida + new Vector3(-Alcance - 0.4f, 0f, 0f);
            donde.y = 0.05f;
            yield return Colocar(donde, Vector3.right);

            yield return Tecla(teclado.eKey);

            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo);
            Assert.IsFalse(carro.Ocupado);
        }

        [UnityTest]
        public IEnumerator Subir_ConDosCarros_SubeAlMasCercano()
        {
            yield return Calentar();
            IMontable lejano = CrearCarro(new Vector3(0f, 0.3f, 12f));
            IMontable cercano = CrearCarro(new Vector3(0f, 0.3f, 8f));
            yield return null;
            Vector3 donde = cercano.PuntoSubida + new Vector3(-1f, 0f, -1f);
            donde.y = 0.05f;
            yield return Colocar(donde, Vector3.back);

            yield return Tecla(teclado.eKey);

            Assert.IsTrue(cercano.Ocupado, "Sube al más cercano");
            Assert.IsFalse(lejano.Ocupado);
        }
    }
}
