using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-593: los sonidos de las armas no se encimen (una voz por arma), con volumen y tono controlados.</summary>
    public class SonidoArmasT593Pruebas
    {
        private GameObject objeto;

        [TearDown]
        public void Limpiar()
        {
            if (objeto != null) Object.Destroy(objeto);
            Registros.LimpiarTodos();
        }

        private GestorSonidos Crear(params string[] ids)
        {
            Registros.LimpiarTodos();
            CatalogoSonidos catalogo = ScriptableObject.CreateInstance<CatalogoSonidos>();
            List<CatalogoSonidos.Entrada> entradas = new List<CatalogoSonidos.Entrada>();
            foreach (string id in ids)
            {
                entradas.Add(new CatalogoSonidos.Entrada { id = id, finales = new[] { AudioClip.Create("clip_" + id, 44100 * 2, 1, 44100, false) } });
            }
            catalogo.Fijar(entradas);
            objeto = new GameObject("GestorSonidos");
            GestorSonidos g = objeto.AddComponent<GestorSonidos>();
            g.Catalogo = catalogo;
            return g;
        }

        [UnityTest]
        public IEnumerator DisparosSeguidosDelFusil_NuncaSuenanDosAlaVez()
        {
            GestorSonidos g = Crear("ARM_FUS_DISPARO");
            yield return null;
            for (int i = 0; i < 12; i++)
            {
                g.ReproducirEn("ARM_FUS_DISPARO", Vector3.zero);
                Assert.AreEqual(1, g.VocesActivas(ReglaMezclaSonidos.GrupoArma), "Una sola voz de arma tras el disparo " + i);
            }
            Assert.AreEqual(12, g.Pedidos("ARM_FUS_DISPARO"));
        }

        [UnityTest]
        public IEnumerator CambiarDeArmaCortaLaRecargaAnterior()
        {
            GestorSonidos g = Crear("ARM_ESC_RECARGA_CARTUCHO", "ARM_CAMBIAR", "ARM_PIS_DISPARO");
            yield return null;
            g.Reproducir("ARM_ESC_RECARGA_CARTUCHO");
            AudioSource recarga = null;
            foreach (AudioSource a in objeto.GetComponentsInChildren<AudioSource>())
            {
                if (a.clip != null && a.clip.name == "clip_ARM_ESC_RECARGA_CARTUCHO") recarga = a;
            }
            Assert.IsNotNull(recarga);
            g.Reproducir("ARM_CAMBIAR");
            Assert.IsFalse(recarga.isPlaying && recarga.clip.name == "clip_ARM_ESC_RECARGA_CARTUCHO", "La recarga vieja se corta al cambiar de arma.");
            Assert.AreEqual(1, g.VocesActivas(ReglaMezclaSonidos.GrupoArma));
        }

        [UnityTest]
        public IEnumerator ElDisparoSuenaMasBajoQueElMaximo_YSiempreConElMismoTono()
        {
            GestorSonidos g = Crear("ARM_SNI_DISPARO", "ARM_PIS_DISPARO");
            g.FijarSemillaTono(7);
            yield return null;
            float minimo = 10f, maximo = 0f;
            for (int i = 0; i < 40; i++)
            {
                g.Reproducir("ARM_PIS_DISPARO");
                minimo = Mathf.Min(minimo, g.UltimoTono);
                maximo = Mathf.Max(maximo, g.UltimoTono);
                Assert.Less(g.UltimoVolumenRelativo, 1f);
                yield return new WaitForSecondsRealtime(0.11f); // T-620: el mismo ID no se repite antes de 0.1 s
            }
            // T-676 (y T-977): el disparo suena siempre igual, sin variar el tono.
            Assert.AreEqual(1f, minimo, 1e-4f);
            Assert.AreEqual(1f, maximo, 1e-4f, "El tono del disparo no varía.");
        }

        [UnityTest]
        public IEnumerator ElCasquilloNoSuenaDosVecesEnElMismoInstante()
        {
            GestorSonidos g = Crear("ARM_CASQUILLO");
            yield return null;
            g.Reproducir("ARM_CASQUILLO");
            g.Reproducir("ARM_CASQUILLO");
            Assert.AreEqual(1, g.VocesActivas(ReglaMezclaSonidos.GrupoArmaAccesorio));
            Assert.AreEqual(2, g.Pedidos("ARM_CASQUILLO"));
        }
    }
}
