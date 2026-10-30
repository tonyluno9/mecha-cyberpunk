using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-407: variantes sin repetir, el final gana al provisional, un ID faltante avisa una sola vez, secciones por prefijo y volúmenes iniciales.</summary>
    public class CatalogoSonidosPruebas
    {
        private static AudioClip Clip(string nombre)
        {
            return AudioClip.Create(nombre, 100, 1, 44100, false);
        }

        [Test]
        public void VarianteFinalYFaltante()
        {
            CatalogoSonidos catalogo = ScriptableObject.CreateInstance<CatalogoSonidos>();
            AudioClip a = Clip("a"), b = Clip("b"), c = Clip("c"), final = Clip("final"), prov = Clip("prov");
            catalogo.Fijar(new List<CatalogoSonidos.Entrada>
            {
                new CatalogoSonidos.Entrada { id = "ARM_PIS_DISPARO", finales = new[] { a, b, c } },
                new CatalogoSonidos.Entrada { id = "ECO_COMPRA", finales = new[] { final }, provisionales = new[] { prov } },
                new CatalogoSonidos.Entrada { id = "UI_MOVER", provisionales = new[] { prov } },
            });

            HashSet<AudioClip> usados = new HashSet<AudioClip>();
            AudioClip anterior = null;
            for (int i = 0; i < 60; i++)
            {
                AudioClip actual = catalogo.Elegir("ARM_PIS_DISPARO");
                Assert.IsNotNull(actual);
                Assert.AreNotSame(anterior, actual, "No repite la variante dos veces seguidas.");
                usados.Add(actual);
                anterior = actual;
            }
            Assert.AreEqual(3, usados.Count, "Con 3 variantes se usan todas.");

            Assert.AreSame(final, catalogo.Elegir("ECO_COMPRA"), "Con final y provisional gana el final.");
            Assert.AreSame(prov, catalogo.Elegir("UI_MOVER"), "Sin final suena el provisional.");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("NO_EXISTE"));
            for (int i = 0; i < 10; i++)
            {
                Assert.IsNull(catalogo.Elegir("NO_EXISTE"));
            }
            Assert.AreEqual(1, catalogo.IdsFaltantesAvisados, "Pedir 10 veces un ID faltante avisa una sola vez.");
            LogAssert.NoUnexpectedReceived();
            Object.DestroyImmediate(catalogo);
        }

        [Test]
        public void SeccionYVolumenInicial()
        {
            Assert.AreEqual(SeccionSonido.Voces, ReglaCatalogoSonidos.SeccionDe("PAN_RIVAL_ALERTA"));
            Assert.AreEqual(SeccionSonido.Interfaz, ReglaCatalogoSonidos.SeccionDe("UI_ACEPTAR"));
            Assert.AreEqual(SeccionSonido.Ambiente, ReglaCatalogoSonidos.SeccionDe("AMB_CALLE_NOCHE"));
            Assert.AreEqual(SeccionSonido.Efectos, ReglaCatalogoSonidos.SeccionDe("ARM_ESC_DISPARO"));
            Assert.AreEqual(0f, ReglaCatalogoSonidos.VolumenInicial(SeccionSonido.Ambiente));
            Assert.AreEqual(0f, ReglaCatalogoSonidos.VolumenInicial(SeccionSonido.Musica));
            Assert.AreEqual(1f, ReglaCatalogoSonidos.VolumenInicial(SeccionSonido.Efectos));
        }

        [Test]
        public void SinGestor_LaFachadaNoHaceNadaNiAvisa()
        {
            Registros.LimpiarTodos();
            Sonidos.Reproducir("ARM_VACIO");
            Sonidos.ReproducirEn("ARM_VACIO", Vector3.zero);
            Assert.AreEqual(Sonidos.SinBucle, Sonidos.ReproducirBucle("VEH_ALARMA_ROBO", null));
            Sonidos.DetenerBucle(3);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
