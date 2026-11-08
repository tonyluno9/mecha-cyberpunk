using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-739 (A8): el jugador camina y corre con Loco_Caminar_SinImplante / Loco_Correr_SinImplante y, con un implante de piernas (IMP01) o de brazos (IMP02) puesto
    /// (contrato IImplantes de Core), con los ConImplante. Se cambia el implante en juego y se mira el clip activo del Animator. Capturas pj01-implante-loco-*.png con CAPTURAR_S1=1.
    /// </summary>
    public class ImplanteLocomocionPJ01T739Pruebas : BaseJugadorAPie
    {
        private sealed class ImplantesFalsos : IImplantes
        {
            private readonly int[] niveles = new int[4];
            public int Nivel(EspacioImplante espacio) => niveles[(int)espacio];
            public string Nombre(EspacioImplante espacio) => niveles[(int)espacio] > 0 ? "X" : "";
            public int Cantidad { get { int n = 0; foreach (int v in niveles) { if (v > 0) { n++; } } return n; } }
            public EfectosImplantes Efectos => EfectosImplantes.Ninguno;
            public event Action Cambio;
            public event Action<EspacioImplante, int> Instalado;
            public void Poner(EspacioImplante espacio, int nivel)
            {
                niveles[(int)espacio] = nivel;
                Cambio?.Invoke();
                if (nivel > 0) { Instalado?.Invoke(espacio, nivel); }
            }
        }

        private Animator anim;
        private ImplantesFalsos implantes;

        private IEnumerator Preparar()
        {
            yield return Calentar();
            anim = jugador.GetComponentInChildren<VisualPersonaje>(true).Animador;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            implantes = new ImplantesFalsos();
            Registro<IImplantes>.Agregar(implantes);
            yield return Esperar(0.5f);
        }

        private string Todos()
        {
            string s = "";
            foreach (AnimatorClipInfo c in anim.GetCurrentAnimatorClipInfo(0)) { s += c.clip.name + ":" + c.weight.ToString("F2") + " "; }
            return s + "(transicion=" + anim.IsInTransition(0) + ", estado=" + anim.GetCurrentAnimatorStateInfo(0).shortNameHash + ")";
        }

        // Tras un cambio el Animator tarda unos cuadros en ver el parametro nuevo (un cuadro con captura puede durar medio segundo).
        private static IEnumerator Asentar()
        {
            for (int i = 0; i < 6; i++) { yield return null; }
            yield return Esperar(0.2f);
        }

        // Clip con mas peso en la capa base.
        private string ClipActivo(out float peso)
        {
            string mejor = "";
            peso = 0f;
            foreach (AnimatorClipInfo c in anim.GetCurrentAnimatorClipInfo(0))
            {
                if (c.weight > peso) { peso = c.weight; mejor = c.clip.name; }
            }
            return mejor;
        }

        private void Fotos(string nombre)
        {
            CapturaS1.Foto("pj01-implante-loco-" + nombre + "-frente", jugador.transform, new Vector3(0.4f, 1.2f, 3.2f), new Vector3(0f, 0.95f, 0f), 40f, 700, 700);
            CapturaS1.Foto("pj01-implante-loco-" + nombre + "-lado", jugador.transform, new Vector3(3.2f, 1.1f, 0.2f), new Vector3(0f, 0.95f, 0f), 40f, 700, 700);
        }

        [UnityTest]
        public IEnumerator Caminar_SinImplante_SinImplanteClip_ConImplanteDePiernasOBrazos_ConImplanteClip()
        {
            yield return Preparar();
            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("Loco_Caminar_SinImplante", ClipActivo(out float peso), "Sin implante: Loco_Caminar_SinImplante");
            Assert.Greater(peso, 0.9f);
            Fotos("caminar-sin");

            implantes.Poner(EspacioImplante.Piernas, 1);
            yield return Asentar();
            Assert.IsTrue(MechaCyberpunk.Personaje.Jugador.AnimacionJugador.LlevaImplanteDeLocomocion(), "El contrato dice que lleva implante");
            Assert.AreEqual(1f, anim.GetFloat("ConImplante"), "parametro ConImplante");
            Assert.AreEqual("Loco_Caminar_ConImplante", ClipActivo(out peso), "IMP01 (piernas): Loco_Caminar_ConImplante " + Todos());
            Assert.Greater(peso, 0.9f);
            Fotos("caminar-con-piernas");

            implantes.Poner(EspacioImplante.Piernas, 0);
            implantes.Poner(EspacioImplante.Brazos, 2);
            yield return Asentar();
            Assert.AreEqual("Loco_Caminar_ConImplante", ClipActivo(out peso), "IMP02 (brazos): Loco_Caminar_ConImplante");

            implantes.Poner(EspacioImplante.Brazos, 0);
            yield return Asentar();
            Assert.AreEqual("Loco_Caminar_SinImplante", ClipActivo(out peso), "Al quitarlo, vuelve a SinImplante");

            // Ojos y sistema nervioso no tocan la locomocion.
            implantes.Poner(EspacioImplante.Ojos, 1);
            implantes.Poner(EspacioImplante.SistemaNervioso, 1);
            yield return Asentar();
            Assert.AreEqual("Loco_Caminar_SinImplante", ClipActivo(out peso), "Ojos y sistema nervioso no cambian el clip");
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator Correr_SinImplante_SinImplanteClip_ConImplante_ConImplanteClip()
        {
            yield return Preparar();
            Press(teclado.wKey);
            yield return null;
            Press(teclado.leftShiftKey);
            yield return Esperar(1.6f);
            Assert.AreEqual("Loco_Correr_SinImplante", ClipActivo(out float peso), "Sin implante: Loco_Correr_SinImplante");
            Assert.Greater(peso, 0.9f);
            Fotos("correr-sin");

            implantes.Poner(EspacioImplante.Brazos, 1);
            yield return Asentar();
            Assert.AreEqual("Loco_Correr_ConImplante", ClipActivo(out peso), "IMP02: Loco_Correr_ConImplante");
            Assert.Greater(peso, 0.9f);
            Fotos("correr-con-brazos");

            implantes.Poner(EspacioImplante.Brazos, 0);
            implantes.Poner(EspacioImplante.Piernas, 2);
            yield return Asentar();
            Assert.AreEqual("Loco_Correr_ConImplante", ClipActivo(out peso), "IMP01 nivel 2: Loco_Correr_ConImplante");
            Release(teclado.leftShiftKey);
            Release(teclado.wKey);
        }

        [UnityTest]
        public IEnumerator SinServicioDeImplantes_SeActuaComoSinImplante()
        {
            yield return Calentar();
            anim = jugador.GetComponentInChildren<VisualPersonaje>(true).Animador;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Press(teclado.wKey);
            yield return Esperar(1.0f);
            Assert.AreEqual("Loco_Caminar_SinImplante", ClipActivo(out float peso));
            Release(teclado.wKey);
        }
    }
}
