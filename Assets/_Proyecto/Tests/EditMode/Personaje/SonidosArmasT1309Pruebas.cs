using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1309: cada arma de fuego pide un sonido de disparo distinto, el catálogo les da clips distintos y el rayo de plasma zigzaguea sin soltar sus extremos.</summary>
    public class SonidosArmasT1309Pruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";

        private static readonly ArmaAPie[] ArmasDeFuego =
        {
            ArmaAPie.Pistola, ArmaAPie.Escopeta, ArmaAPie.Fusil, ArmaAPie.Sniper, ArmaAPie.Subfusil,
            ArmaAPie.Revolver, ArmaAPie.Lanzagranadas, ArmaAPie.RiflePlasma
        };

        [Test]
        public void IdDisparo_CadaArmaDeFuego_PideUnIdDistinto()
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (ArmaAPie arma in ArmasDeFuego)
            {
                Assert.IsTrue(ids.Add(PistolaAPie.IdDisparo(arma)), "ID repetido para " + arma + ": " + PistolaAPie.IdDisparo(arma));
            }
        }

        [Test]
        public void IdDisparo_ElRevolverConservaSuClipDeSiempre()
        {
            // T-1472: ahora cada arma pide su ID nuevo (W06 revólver, W04 francotirador); T-1471 los mapea a los archivos.
            Assert.AreEqual("ARM_W06_DISPARO", PistolaAPie.IdDisparo(ArmaAPie.Revolver));
            Assert.AreEqual("ARM_W04_DISPARO", PistolaAPie.IdDisparo(ArmaAPie.Sniper));
        }

        [Test]
        public void Catalogo_CadaArmaDeFuegoTieneClipDistinto()
        {
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            Assert.IsNotNull(catalogo);
            if (!catalogo.Tiene("ARM_W07_DISPARO") || !catalogo.Tiene("ARM_W06_DISPARO"))
            {
                Assert.Ignore("Falta correr MechaCyberpunk.Personaje.Editor.SonidosArmasT1309.Aplicar (asigna los clips nuevos al catalogo).");
            }
            HashSet<AudioClip> clips = new HashSet<AudioClip>();
            foreach (ArmaAPie arma in ArmasDeFuego)
            {
                if (arma == ArmaAPie.Subfusil)
                {
                    continue; // T-1472: el subfusil suena con el clip de la pistola a tono alto (lo pone T-1471)
                }
                string id = PistolaAPie.IdDisparo(arma);
                AudioClip clip = catalogo.Elegir(id);
                Assert.IsNotNull(clip, "Sin clip para " + id);
                Assert.IsTrue(clips.Add(clip), "Clip repetido para " + arma + " (" + clip.name + ")");
            }
        }

        [Test]
        public void Catalogo_ElChispeoDelPlasmaTieneClipYEstaEncendido()
        {
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            if (!catalogo.Tiene(ArmasEspecialesAPie.IdChispeoPlasma))
            {
                Assert.Ignore("Falta correr MechaCyberpunk.Personaje.Editor.SonidosArmasT1309.Aplicar.");
            }
            Assert.IsTrue(catalogo.Encendido(ArmasEspecialesAPie.IdChispeoPlasma));
            Assert.IsNotNull(catalogo.Elegir(ArmasEspecialesAPie.IdChispeoPlasma));
        }

        [Test]
        public void ZigzagRayo_ExtremosFijosYDesvioAcotado()
        {
            Vector3 a = new Vector3(0f, 1.5f, 0f);
            Vector3 b = new Vector3(0f, 1.5f, 20f);
            Vector3[] puntos = new Vector3[12];
            ReglaZigzagRayo.Puntos(a, b, 0.37f, 0.1f, 5, puntos);
            Assert.AreEqual(a, puntos[0]);
            Assert.AreEqual(b, puntos[puntos.Length - 1]);
            bool algunoDesviado = false;
            for (int i = 1; i < puntos.Length - 1; i++)
            {
                float desvio = Mathf.Sqrt(puntos[i].x * puntos[i].x + (puntos[i].y - 1.5f) * (puntos[i].y - 1.5f));
                Assert.LessOrEqual(desvio, 0.1f + 0.0001f, "Punto " + i + " se aleja de mas");
                algunoDesviado |= desvio > 0.0005f;
            }
            Assert.IsTrue(algunoDesviado, "El zigzag debe mover algun punto");
        }

        [Test]
        public void ZigzagRayo_CambiaConElTiempo()
        {
            Vector3[] uno = new Vector3[8];
            Vector3[] dos = new Vector3[8];
            ReglaZigzagRayo.Puntos(Vector3.zero, Vector3.forward * 10f, 0.1f, 0.1f, 3, uno);
            ReglaZigzagRayo.Puntos(Vector3.zero, Vector3.forward * 10f, 0.6f, 0.1f, 3, dos);
            Assert.AreNotEqual(uno[3], dos[3]);
        }

        [Test]
        public void TexturaGradiente_EsBrillanteAlCentroYTransparenteEnLosBordes()
        {
            Texture2D t = EfectoRayoPlasma.TexturaGradiente();
            try
            {
                float centro = t.GetPixel(1, t.height / 2).a;
                float borde = t.GetPixel(1, 0).a;
                Assert.Greater(centro, 0.9f);
                Assert.Less(borde, 0.1f);
            }
            finally
            {
                Object.DestroyImmediate(t);
            }
        }
    }
}
