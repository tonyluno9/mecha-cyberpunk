using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Economia;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1309: el chispeo del rifle de plasma suena en bucle mientras carga y se corta al disparar o al soltar.</summary>
    public class ChispeoPlasmaT1309Pruebas : BaseJugadorAPie
    {
        private sealed class SonidosFalsos : ISonidos
        {
            public readonly List<string> Ids = new List<string>();
            public readonly HashSet<int> BuclesActivos = new HashSet<int>();
            private int siguiente;
            public void Reproducir(string id) { Ids.Add(id); }
            public void ReproducirEn(string id, Vector3 posicion) { Ids.Add(id); }
            public int ReproducirBucle(string id, Transform sigue)
            {
                Ids.Add("bucle:" + id);
                int n = siguiente++;
                BuclesActivos.Add(n);
                return n;
            }
            public void DetenerBucle(int bucle) { BuclesActivos.Remove(bucle); }
            public void FijarTono(int bucle, float tono) { }
            public float Volumen(SeccionSonido seccion) { return 1f; }
            public void FijarVolumen(SeccionSonido seccion, float volumen) { }
        }

        private SonidosFalsos sonidos;

        public override void Setup()
        {
            base.Setup();
            sonidos = new SonidosFalsos();
            Registro<ISonidos>.Agregar(sonidos);
        }

        public override void TearDown()
        {
            Registro<ISonidos>.Quitar(sonidos);
            base.TearDown();
        }

        private IEnumerator EquiparPlasma()
        {
            yield return Calentar();
            // Como en ArmasEspecialesT1013Pruebas: con la cámara de la prueba el personaje no se anima (fuera de pantalla) y el rifle no carga.
            MechaCyberpunk.Personaje.Visual.VisualPersonaje visual = jugador.GetComponentInChildren<MechaCyberpunk.Personaje.Visual.VisualPersonaje>(true);
            if (visual != null && visual.Animador != null)
            {
                visual.Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            yield return Esperar(0.3f);
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Assert.IsNotNull(arsenal, "Hay arsenal");
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(ArmaAPie.RiflePlasma, 0), 1);
            yield return null;
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.RiflePlasma), "El rifle de plasma se equipa");
            yield return Esperar(0.3f);
        }

        [UnityTest]
        public IEnumerator Plasma_MientrasCarga_ChispeaEnBucleYSeCortaAlCambiarDeArma()
        {
            yield return EquiparPlasma();
            ArmasEspecialesAPie especiales = jugador.GetComponent<ArmasEspecialesAPie>();
            Press(raton.rightButton);
            yield return Esperar(0.8f);
            Press(raton.leftButton);
            yield return Esperar(1f);
            Assert.AreEqual(EstadoPlasma.Cargando, especiales.EstadoPlasma);
            Assert.IsTrue(especiales.ChispeoSonando, "Suena el chispeo al cargar");
            Assert.Contains("bucle:" + ArmasEspecialesAPie.IdChispeoPlasma, sonidos.Ids);
            Assert.AreEqual(1, sonidos.BuclesActivos.Count, "Un solo bucle");
            // Cambiar de arma a media carga corta la carga y su chispeo.
            Assert.IsTrue(jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola), "Se cambia a la pistola");
            yield return Esperar(0.3f);
            Release(raton.leftButton);
            Release(raton.rightButton);
            Assert.AreNotEqual(EstadoPlasma.Cargando, especiales.EstadoPlasma, "Ya no carga");
            Assert.IsFalse(especiales.ChispeoSonando, "Al dejar el rifle se corta el chispeo");
            Assert.AreEqual(0, sonidos.BuclesActivos.Count);
        }

        [UnityTest]
        public IEnumerator Plasma_AlTerminarLaCarga_ElChispeoParaYSueltaElDisparoDelRayo()
        {
            yield return EquiparPlasma();
            ArmasEspecialesAPie especiales = jugador.GetComponent<ArmasEspecialesAPie>();
            Press(raton.rightButton);
            yield return Esperar(0.8f);
            Press(raton.leftButton);
            yield return Esperar(3.5f);
            Assert.AreEqual(EstadoPlasma.Rayo, especiales.EstadoPlasma);
            Assert.IsFalse(especiales.ChispeoSonando, "Con el rayo ya no chispea");
            Assert.Contains(ArmasEspecialesAPie.IdDisparoPlasma, sonidos.Ids);
            Release(raton.leftButton);
            Release(raton.rightButton);
        }
    }
}
