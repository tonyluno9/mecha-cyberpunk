using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-470: el gestor toca un ID con archivo, uno sin archivo no da error, y el AMB de un lugar suena al entrar.</summary>
    public class GestorSonidosAmbientePruebas
    {
        private GameObject objeto;

        [TearDown]
        public void Limpiar()
        {
            if (objeto != null) Object.Destroy(objeto);
            Registros.LimpiarTodos();
        }

        private GestorSonidos Crear()
        {
            Registros.LimpiarTodos();
            CatalogoSonidos catalogo = ScriptableObject.CreateInstance<CatalogoSonidos>();
            AudioClip largo = AudioClip.Create("largo", 44100 * 2, 1, 44100, false);
            catalogo.Fijar(new List<CatalogoSonidos.Entrada>
            {
                new CatalogoSonidos.Entrada { id = "ARM_PIS_DISPARO", finales = new[] { largo } },
                new CatalogoSonidos.Entrada { id = "AMB_MERCADO", finales = new[] { largo } },
                new CatalogoSonidos.Entrada { id = "AMB_CALLE_NOCHE", finales = new[] { largo } },
            });
            objeto = new GameObject("GestorSonidos");
            GestorSonidos g = objeto.AddComponent<GestorSonidos>();
            g.Catalogo = catalogo;
            return g;
        }

        [UnityTest]
        public IEnumerator TocaUnIdConArchivo_YUnoSinArchivoNoDaError()
        {
            GestorSonidos g = Crear();
            yield return null;
            g.Reproducir("ARM_PIS_DISPARO");
            Assert.AreEqual(1, g.Pedidos("ARM_PIS_DISPARO"));
            bool suena = false;
            foreach (AudioSource a in objeto.GetComponentsInChildren<AudioSource>())
            {
                suena |= a.isPlaying && a.clip != null && a.clip.name == "largo" && a.volume > 0.5f;
            }
            Assert.IsTrue(suena, "El disparo suena a buen volumen.");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("ID_QUE_NO_EXISTE"));
            for (int i = 0; i < 5; i++) g.ReproducirEn("ID_QUE_NO_EXISTE", Vector3.one);
            Assert.AreEqual(5, g.Pedidos("ID_QUE_NO_EXISTE"));
        }

        [UnityTest]
        public IEnumerator ElAmbienteDelLugarSuenaAlEntrar_YSeMezclaBajo()
        {
            GestorSonidos g = Crear();
            yield return null;
            EventosLugares.PublicarEntro(IdLugar.MercadoNegro);
            yield return new WaitForSecondsRealtime(1.3f);
            Assert.AreEqual("AMB_MERCADO", g.AmbienteDeseado);
            AudioSource ambiente = null;
            foreach (AudioSource a in objeto.GetComponentsInChildren<AudioSource>())
            {
                if (a.name.StartsWith("Ambiente_") && a.isPlaying) ambiente = a;
            }
            Assert.IsNotNull(ambiente, "Suena una fuente de ambiente.");
            Assert.Greater(ambiente.volume, 0.05f, "El ambiente se oye aunque la opcion arranque en 0.");
            Assert.Less(ambiente.volume, 0.5f, "El ambiente queda bajo.");

            EventosLugares.PublicarSalio(IdLugar.MercadoNegro);
            yield return null;
            Assert.AreEqual("AMB_CALLE_NOCHE", g.AmbienteDeseado);
        }
    }
}
