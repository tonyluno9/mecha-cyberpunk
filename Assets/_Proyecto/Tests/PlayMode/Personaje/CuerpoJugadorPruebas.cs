using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>CuerpoJugador.prefab: colores y piezas según el catálogo, módulo de índices y seguimiento del perfil.</summary>
    public class CuerpoJugadorPruebas
    {
        private const string RutaCuerpo = "Assets/_Proyecto/Personaje/Prefabs/CuerpoJugador.prefab";
        private const string RutaServicios = "Assets/_Proyecto/Progresion/Prefabs/ServiciosProgresion.prefab";
        private const float Tolerancia = 0.01f;

        private GameObject cuerpoGo;
        private GameObject serviciosGo;

        [SetUp]
        public void SetUp() => Registros.LimpiarTodos();

        [TearDown]
        public void TearDown()
        {
            if (cuerpoGo != null) Object.Destroy(cuerpoGo);
            if (serviciosGo != null) Object.Destroy(serviciosGo);
            Registros.LimpiarTodos();
        }

        private CuerpoJugador CrearCuerpo()
        {
            cuerpoGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCuerpo));
            return cuerpoGo.GetComponent<CuerpoJugador>();
        }

        private static Color ColorDe(Renderer r)
        {
            var bloque = new MaterialPropertyBlock();
            r.GetPropertyBlock(bloque);
            return bloque.GetColor("_BaseColor");
        }

        private static void AssertColor(Color esperado, Color real, string mensaje)
        {
            Assert.AreEqual(esperado.r, real.r, Tolerancia, mensaje);
            Assert.AreEqual(esperado.g, real.g, Tolerancia, mensaje);
            Assert.AreEqual(esperado.b, real.b, Tolerancia, mensaje);
        }

        private static int ActivasEnLista(GameObject[] piezas)
        {
            int n = 0;
            foreach (var p in piezas) if (p != null && p.activeSelf) n++;
            return n;
        }

        [UnityTest]
        public IEnumerator Aplicar_CuatroPorCuatroPorCuatro_PintaPielRopaYMuestraLasPiezasDelCatalogo()
        {
            CuerpoJugador cuerpo = CrearCuerpo();
            yield return null;
            CatalogoApariencia cat = cuerpo.Catalogo;
            Assert.AreEqual(4, cat.CantidadCaras);
            Assert.AreEqual(4, cat.CantidadRopas);
            Assert.AreEqual(4, cat.CantidadImplantes);

            for (int c = 0; c < 4; c++)
            for (int r = 0; r < 4; r++)
            for (int i = 0; i < 4; i++)
            {
                cuerpo.Aplicar(new AparienciaJugador(c, r, i));
                string id = c + "/" + r + "/" + i;
                AssertColor(cat.Cara(c).piel, ColorDe(cuerpo.Cabeza), "piel " + id);
                AssertColor(cat.Ropa(r).color, ColorDe(cuerpo.Torso), "ropa " + id);
                Assert.AreEqual(1, ActivasEnLista(cuerpo.Peinados), "un peinado " + id);
                Assert.IsTrue(cuerpo.Peinados[(int)cat.Cara(c).peinado].activeSelf, "peinado " + id);
                Assert.AreEqual(1, ActivasEnLista(cuerpo.PiezasRopa), "una pieza de ropa " + id);
                Assert.IsTrue(cuerpo.PiezasRopa[(int)cat.Ropa(r).pieza].activeSelf, "pieza de ropa " + id);
                int impl = (int)cat.Implante(i).implante;
                if (impl == 0)
                {
                    Assert.AreEqual(0, ActivasEnLista(cuerpo.Implantes), "sin implante " + id);
                }
                else
                {
                    Assert.AreEqual(1, ActivasEnLista(cuerpo.Implantes), "un implante " + id);
                    Assert.IsTrue(cuerpo.Implantes[impl].activeSelf, "implante " + id);
                }
            }
        }

        [UnityTest]
        public IEnumerator Aplicar_ConCarasYRopasDistintas_CambiaElColor()
        {
            CuerpoJugador cuerpo = CrearCuerpo();
            yield return null;
            cuerpo.Aplicar(new AparienciaJugador(0, 0, 0));
            Color piel0 = ColorDe(cuerpo.Cabeza);
            Color ropa0 = ColorDe(cuerpo.Torso);
            cuerpo.Aplicar(new AparienciaJugador(3, 2, 0));
            Assert.AreNotEqual(piel0, ColorDe(cuerpo.Cabeza));
            Assert.AreNotEqual(ropa0, ColorDe(cuerpo.Torso));
        }

        [UnityTest]
        public IEnumerator Aplicar_ConIndiceSiete_SeTomaComoSieteModuloCuatro()
        {
            CuerpoJugador cuerpo = CrearCuerpo();
            yield return null;
            CatalogoApariencia cat = cuerpo.Catalogo;
            cuerpo.Aplicar(new AparienciaJugador(7, 7, 7));
            AssertColor(cat.Cara(3).piel, ColorDe(cuerpo.Cabeza), "piel 7 % 4");
            AssertColor(cat.Ropa(3).color, ColorDe(cuerpo.Torso), "ropa 7 % 4");
            Assert.AreEqual(cat.Implante(3).implante, cuerpo.ImplanteActivo);
            Assert.IsTrue(cuerpo.Implantes[(int)cat.Implante(3).implante].activeSelf);
        }

        [UnityTest]
        public IEnumerator Aplicar_ConIndiceNegativo_NoDaErroresYUsaModulo()
        {
            CuerpoJugador cuerpo = CrearCuerpo();
            yield return null;
            cuerpo.Aplicar(new AparienciaJugador(-1, -2, -3));
            AssertColor(cuerpo.Catalogo.Cara(3).piel, ColorDe(cuerpo.Cabeza), "piel -1");
            AssertColor(cuerpo.Catalogo.Ropa(2).color, ColorDe(cuerpo.Torso), "ropa -2");
        }

        [UnityTest]
        public IEnumerator SinAparienciaDada_SigueAlPerfilYSeRepintaConFijarApariencia()
        {
            serviciosGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaServicios));
            yield return null;
            CuerpoJugador cuerpo = CrearCuerpo();
            yield return null;
            CatalogoApariencia cat = cuerpo.Catalogo;
            AssertColor(cat.Cara(0).piel, ColorDe(cuerpo.Cabeza), "predeterminada");

            serviciosGo.GetComponent<ServicioPerfil>().FijarApariencia(new AparienciaJugador(2, 1, 3));
            AssertColor(cat.Cara(2).piel, ColorDe(cuerpo.Cabeza), "piel del perfil");
            AssertColor(cat.Ropa(1).color, ColorDe(cuerpo.Torso), "ropa del perfil");
            Assert.AreEqual(ImplanteApariencia.PlacaSien, cuerpo.ImplanteActivo);
            Assert.IsTrue(cuerpo.Implantes[3].activeSelf);
        }

        [UnityTest]
        public IEnumerator PerfilQueLlegaDespues_SeSigueAlRegistrarse()
        {
            CuerpoJugador cuerpo = CrearCuerpo();
            yield return null;
            serviciosGo = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaServicios));
            yield return null;
            serviciosGo.GetComponent<ServicioPerfil>().FijarApariencia(new AparienciaJugador(1, 3, 2));
            AssertColor(cuerpo.Catalogo.Ropa(3).color, ColorDe(cuerpo.Torso), "ropa del perfil tardío");
            Assert.AreEqual(ImplanteApariencia.OjoLed, cuerpo.ImplanteActivo);
        }

        private static void AssertSoloPrendido(CuerpoJugador cuerpo, EspacioImplante espacio, int nivel)
        {
            for (int i = 0; i < 4; i++)
            {
                bool esta = i == (int)espacio;
                Assert.AreEqual(esta, cuerpo.PiezasInstaladas[i].activeSelf, "pieza " + (EspacioImplante)i);
                Assert.AreEqual(esta && nivel >= 2, cuerpo.FranjasInstaladas[i].activeSelf, "franja " + (EspacioImplante)i);
            }
        }

        [UnityTest]
        public IEnumerator Implantes_CadaEspacioYNivel_PrendeSoloSuPieza()
        {
            CuerpoJugador cuerpo = CrearCuerpo();
            var falsos = new ImplantesFalsos();
            Registro<IImplantes>.Agregar(falsos);
            yield return null;
            for (int i = 0; i < 4; i++) Assert.IsFalse(cuerpo.PiezasInstaladas[i].activeSelf, "sin implantes " + (EspacioImplante)i);

            foreach (EspacioImplante espacio in System.Enum.GetValues(typeof(EspacioImplante)))
            {
                for (int nivel = 1; nivel <= 2; nivel++)
                {
                    falsos.Poner(espacio, nivel);
                    AssertSoloPrendido(cuerpo, espacio, nivel);
                    falsos.Poner(espacio, 0);
                }
            }
            for (int i = 0; i < 4; i++)
            {
                Assert.IsFalse(cuerpo.PiezasInstaladas[i].activeSelf, "quitar los apaga " + (EspacioImplante)i);
                Assert.IsFalse(cuerpo.FranjasInstaladas[i].activeSelf);
            }
        }

        [UnityTest]
        public IEnumerator Implantes_ColoresSinRojoPuro_OjosTurquesaFranjaMagenta()
        {
            CuerpoJugador cuerpo = CrearCuerpo();
            var falsos = new ImplantesFalsos();
            Registro<IImplantes>.Agregar(falsos);
            yield return null;
            foreach (EspacioImplante espacio in System.Enum.GetValues(typeof(EspacioImplante))) falsos.Poner(espacio, 2);
            foreach (GameObject pieza in cuerpo.PiezasInstaladas)
            {
                foreach (Renderer r in pieza.GetComponentsInChildren<Renderer>())
                {
                    Color c = ColorDe(r);
                    Assert.IsFalse(c.r > 0.9f && c.g < 0.2f && c.b < 0.2f, "rojo puro en " + pieza.name);
                }
            }
            Color ojos = ColorDe(cuerpo.PiezasInstaladas[(int)EspacioImplante.Ojos].GetComponentInChildren<Renderer>());
            Assert.Greater(ojos.g, 0.7f);
            Assert.Greater(ojos.b, 0.7f);
            Assert.Less(ojos.r, 0.3f);
            Color franja = ColorDe(cuerpo.FranjasInstaladas[(int)EspacioImplante.Brazos].GetComponentInChildren<Renderer>());
            Assert.Greater(franja.r, 0.8f);
            Assert.Less(franja.g, 0.3f);
            Assert.Greater(franja.b, 0.4f);
        }

        [UnityTest]
        public IEnumerator Implantes_MostrarPrueba_SeSumaALoInstaladoYQuitarPruebaLoDevuelve()
        {
            CuerpoJugador cuerpo = CrearCuerpo();
            var falsos = new ImplantesFalsos();
            Registro<IImplantes>.Agregar(falsos);
            yield return null;
            falsos.Poner(EspacioImplante.Brazos, 1);
            cuerpo.MostrarPrueba(EspacioImplante.Piernas, 2);
            Assert.IsTrue(cuerpo.PiezasInstaladas[(int)EspacioImplante.Brazos].activeSelf, "lo instalado sigue");
            Assert.IsFalse(cuerpo.FranjasInstaladas[(int)EspacioImplante.Brazos].activeSelf);
            Assert.IsTrue(cuerpo.PiezasInstaladas[(int)EspacioImplante.Piernas].activeSelf, "la prueba se suma");
            Assert.IsTrue(cuerpo.FranjasInstaladas[(int)EspacioImplante.Piernas].activeSelf);
            Assert.AreEqual(1, falsos.Nivel(EspacioImplante.Brazos));
            Assert.AreEqual(0, falsos.Nivel(EspacioImplante.Piernas), "la prueba no instala nada");

            cuerpo.MostrarPrueba(EspacioImplante.Brazos, 2);
            Assert.IsTrue(cuerpo.FranjasInstaladas[(int)EspacioImplante.Brazos].activeSelf, "la prueba sube el nivel mostrado");

            cuerpo.QuitarPrueba();
            Assert.IsTrue(cuerpo.PiezasInstaladas[(int)EspacioImplante.Brazos].activeSelf);
            Assert.IsFalse(cuerpo.FranjasInstaladas[(int)EspacioImplante.Brazos].activeSelf);
            Assert.IsFalse(cuerpo.PiezasInstaladas[(int)EspacioImplante.Piernas].activeSelf);
        }

        [UnityTest]
        public IEnumerator Implantes_ServicioQueLlegaDespues_SeSigue_YElCosmeticoNoCambia()
        {
            CuerpoJugador cuerpo = CrearCuerpo();
            yield return null;
            cuerpo.Aplicar(new AparienciaJugador(0, 0, 2));
            Assert.IsTrue(cuerpo.Implantes[(int)ImplanteApariencia.OjoLed].activeSelf);
            for (int i = 0; i < 4; i++) Assert.IsFalse(cuerpo.PiezasInstaladas[i].activeSelf);

            var falsos = new ImplantesFalsos();
            falsos.Poner(EspacioImplante.Ojos, 1);
            Registro<IImplantes>.Agregar(falsos);
            Assert.IsTrue(cuerpo.PiezasInstaladas[(int)EspacioImplante.Ojos].activeSelf, "se sigue al servicio tardío");
            Assert.IsTrue(cuerpo.Implantes[(int)ImplanteApariencia.OjoLed].activeSelf, "el cosmético sigue igual");
            Assert.AreEqual(1, ActivasEnLista(cuerpo.Implantes));
        }

        [UnityTest]
        public IEnumerator Prefab_NoTieneCollidersNiSombrasYMideUnosUnMetroSetentaYCinco()
        {
            CrearCuerpo();
            yield return null;
            Assert.AreEqual(0, cuerpoGo.GetComponentsInChildren<Collider>(true).Length);
            Bounds caja = default;
            bool primero = true;
            foreach (var r in cuerpoGo.GetComponentsInChildren<Renderer>(true))
            {
                Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, r.shadowCastingMode, r.name);
                if (!r.gameObject.activeSelf && r.transform != cuerpoGo.transform) continue;
                if (primero) { caja = r.bounds; primero = false; } else caja.Encapsulate(r.bounds);
            }
            Assert.AreEqual(1.75f, caja.size.y, 0.1f);
        }
    }
}
