using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-595: veinte claxons seguidos de tráfico no se encimen, varían de tono y tienen alcance corto.</summary>
    public class SonidoCarrosT595Pruebas
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
            catalogo.Fijar(new List<CatalogoSonidos.Entrada>
            {
                new CatalogoSonidos.Entrada { id = "VEH_CLAXON_TRAFICO", finales = new[] { AudioClip.Create("claxon", 44100 * 3, 1, 44100, false) } },
            });
            objeto = new GameObject("GestorSonidos");
            GestorSonidos g = objeto.AddComponent<GestorSonidos>();
            g.Catalogo = catalogo;
            return g;
        }

        [UnityTest]
        public IEnumerator ClaxonsEnRafaga_NoPasanDeDosVocesYTienenAlcanceCorto()
        {
            GestorSonidos g = Crear();
            yield return null;
            for (int i = 0; i < 20; i++)
            {
                g.ReproducirEn("VEH_CLAXON_TRAFICO", new Vector3(i, 0, 0));
                Assert.LessOrEqual(g.VocesActivas(ReglaMezclaSonidos.GrupoClaxon), 2);
            }
            Assert.AreEqual(20, g.Pedidos("VEH_CLAXON_TRAFICO"));
            Assert.AreEqual(25f, g.UltimaDistanciaMaxima, 0.01f);
        }

        [UnityTest]
        public IEnumerator ClaxonsEspaciados_VarianDeTono()
        {
            GestorSonidos g = Crear();
            g.FijarSemillaTono(3);
            yield return null;
            HashSet<int> tonos = new HashSet<int>();
            for (int i = 0; i < 6; i++)
            {
                g.ReproducirEn("VEH_CLAXON_TRAFICO", Vector3.zero);
                tonos.Add(Mathf.RoundToInt(g.UltimoTono * 1000f));
                yield return new WaitForSecondsRealtime(0.85f);
            }
            Assert.GreaterOrEqual(tonos.Count, 4, "Cada claxon suena con otro tono.");
            foreach (int t in tonos) Assert.That(t, Is.InRange(879, 1121));
        }
    }
}
