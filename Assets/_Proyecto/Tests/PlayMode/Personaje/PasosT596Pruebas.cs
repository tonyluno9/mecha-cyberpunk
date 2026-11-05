using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-596: caminar suena pasos al ritmo de la zancada, quieto no suena y el paso cambia en un interior.</summary>
    public class PasosT596Pruebas : BaseJugadorAPie
    {
        private GameObject objetoGestor;

        private GestorSonidos CrearGestor()
        {
            CatalogoSonidos catalogo = ScriptableObject.CreateInstance<CatalogoSonidos>();
            List<CatalogoSonidos.Entrada> entradas = new List<CatalogoSonidos.Entrada>();
            foreach (string id in new[] { "JUG_PASO_ASFALTO", "JUG_PASO_INTERIOR", "JUG_PASO_METAL", "JUG_PASO_ASFALTO_MOJADO" })
            {
                entradas.Add(new CatalogoSonidos.Entrada { id = id, finales = new[] { AudioClip.Create("p_" + id, 4410, 1, 44100, false) } });
            }
            catalogo.Fijar(entradas);
            objetoGestor = new GameObject("GestorSonidos");
            GestorSonidos g = objetoGestor.AddComponent<GestorSonidos>();
            g.Catalogo = catalogo;
            return g;
        }

        public override void TearDown()
        {
            if (objetoGestor != null) Object.Destroy(objetoGestor);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Pasos_CaminarConW_SuenanAlRitmoDeLaDistanciaRecorrida()
        {
            yield return Calentar();
            GestorSonidos g = CrearGestor();
            yield return null;
            int antes = controlador.PasosDados;
            Vector3 inicio = jugador.transform.position;
            Press(teclado.wKey);
            yield return Esperar(2f);
            Release(teclado.wKey);
            float avance = (jugador.transform.position - inicio).magnitude;
            int pasos = controlador.PasosDados - antes;
            float esperados = (avance + 0.7f) / 1.4f;
            Assert.That(pasos, Is.InRange(esperados - 1.01f, esperados + 1.01f), "Un paso por cada zancada de " + avance + " m");
            Assert.AreEqual(pasos, g.Pedidos("JUG_PASO_ASFALTO"), "Cada paso pide el sonido de asfalto.");
        }

        [UnityTest]
        public IEnumerator Pasos_QuietoNoSuenan()
        {
            yield return Calentar();
            GestorSonidos g = CrearGestor();
            yield return Esperar(0.5f);
            int antes = controlador.PasosDados;
            yield return Esperar(1f);
            Assert.AreEqual(antes, controlador.PasosDados);
        }

        [UnityTest]
        public IEnumerator Pasos_AlEntrarAUnLugarCerrado_SuenaElPasoDeInterior()
        {
            yield return Calentar();
            GestorSonidos g = CrearGestor();
            yield return null;
            EventosLugares.PublicarEntro(IdLugar.Casa);
            Press(teclado.wKey);
            yield return Esperar(1.5f);
            Release(teclado.wKey);
            Assert.Greater(g.Pedidos("JUG_PASO_INTERIOR"), 0);
            Assert.AreEqual(0, g.Pedidos("JUG_PASO_ASFALTO"));
            EventosLugares.PublicarSalio(IdLugar.Casa);
        }
    }
}
