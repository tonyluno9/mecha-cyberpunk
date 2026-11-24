using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.VR;
using MechaCyberpunk.VR.Guia;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR45/VR31b: los gestos de prótesis más tolerantes (piernas de la cadera a la rodilla con cualquier separación, brazos en T aproximada)
    /// y el gesto nuevo de la visión (las dos manos a los lados de la cabeza + los dos grips 0.5 s activan la habilidad CABEZA).
    /// </summary>
    public class VR45GestosProtesisPruebas : BaseJugadorVR
    {
        // ---- Lógica pura (marco de la cabeza: x derecha, y arriba desde los ojos, z frente) ----

        [Test]
        public void Piernas_ManosJuntasOSeparadasEntreCaderaYRodilla_Cuenta()
        {
            Assert.AreEqual(GestoProtesisVR.Piernas, ReglaGestosProtesisVR.Pose(new Vector3(-0.05f, -0.8f, 0.1f), new Vector3(0.05f, -0.8f, 0.1f)), "Juntas.");
            Assert.AreEqual(GestoProtesisVR.Piernas, ReglaGestosProtesisVR.Pose(new Vector3(-0.4f, -0.7f, 0f), new Vector3(0.4f, -1.1f, -0.1f)), "Separadas y a distinta altura.");
            Assert.AreEqual(GestoProtesisVR.Piernas, ReglaGestosProtesisVR.Pose(new Vector3(-0.17f, -0.9f, 0.05f), new Vector3(0.17f, -0.9f, 0.05f)), "Sobre los muslos, como antes.");
        }

        [Test]
        public void Piernas_ManoEnLaCinturaOEnElPiso_NoCuenta()
        {
            Assert.AreEqual(GestoProtesisVR.Ninguno, ReglaGestosProtesisVR.Pose(new Vector3(-0.2f, -0.45f, 0f), new Vector3(0.2f, -0.9f, 0f)), "Una mano arriba de la cadera.");
            Assert.AreEqual(GestoProtesisVR.Ninguno, ReglaGestosProtesisVR.Pose(new Vector3(-0.2f, -1.4f, 0f), new Vector3(0.2f, -0.9f, 0f)), "Una mano bajo la rodilla.");
            Assert.AreEqual(GestoProtesisVR.Ninguno, ReglaGestosProtesisVR.Pose(new Vector3(-0.2f, -0.9f, 0.7f), new Vector3(0.2f, -0.9f, 0f)), "Una mano muy al frente.");
        }

        [Test]
        public void Brazos_PoseTAproximada_Cuenta()
        {
            Assert.AreEqual(GestoProtesisVR.Brazos, ReglaGestosProtesisVR.Pose(new Vector3(-0.5f, -0.5f, 0.3f), new Vector3(0.48f, 0.05f, -0.2f)));
            Assert.AreEqual(GestoProtesisVR.Ninguno, ReglaGestosProtesisVR.Pose(new Vector3(-0.3f, -0.25f, 0.5f), new Vector3(0.3f, -0.25f, 0.5f)), "Brazos al frente no.");
        }

        [Test]
        public void Vision_ManosALosLadosDeLaCabeza_Cuenta()
        {
            Assert.AreEqual(GestoProtesisVR.Vision, ReglaGestosProtesisVR.Pose(new Vector3(-0.15f, 0f, 0f), new Vector3(0.15f, 0.05f, 0.05f)));
            Assert.AreEqual(GestoProtesisVR.Ninguno, ReglaGestosProtesisVR.Pose(new Vector3(-0.15f, 0f, 0f), new Vector3(0.15f, -0.4f, 0.3f)), "Una mano abajo no.");
            Assert.AreEqual(GestoProtesisVR.Ninguno, ReglaGestosProtesisVR.Pose(new Vector3(0.1f, 0f, 0f), new Vector3(0.15f, 0f, 0f)), "Las dos del mismo lado no.");
            Assert.AreEqual(HabilidadProtesis.Cabeza, GestosProtesisVR.Habilidad(GestoProtesisVR.Vision));
        }

        // ---- Con el jugador VR ----

        private sealed class ImplantesDoble : IImplantes
        {
            private readonly int[] niveles = new int[8];
            public int Cantidad { get { int n = 0; foreach (int x in niveles) if (x > 0) n++; return n; } }
            public EfectosImplantes Efectos => EfectosImplantes.Ninguno;
            public event Action Cambio;
            public event Action<EspacioImplante, int> Instalado;
            public int Nivel(EspacioImplante espacio) => niveles[(int)espacio];
            public string Nombre(EspacioImplante espacio) => niveles[(int)espacio] > 0 ? "DOBLE" : string.Empty;

            public void Poner(EspacioImplante espacio, int nivel)
            {
                niveles[(int)espacio] = nivel;
                Cambio?.Invoke();
                if (nivel > 0) Instalado?.Invoke(espacio, nivel);
            }
        }

        private ImplantesDoble implantes;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            implantes = new ImplantesDoble();
        }

        public override void TearDown()
        {
            Registro<IImplantes>.Quitar(implantes);
            ControlTiempo.SoltarTodo();
            Time.timeScale = 1f;
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Vision_ConOjos_DosGripsJuntoALaCabeza_ActivaCabeza()
        {
            implantes.Poner(EspacioImplante.Ojos, 1);
            yield return ArmarVR();
            Registro<IImplantes>.Agregar(implantes);
            PonerCabeza(new Vector3(0f, 1.6f, 0f), Quaternion.identity);
            yield return Cuadros(4);
            HabilidadesProtesis h = jugador.GetComponent<HabilidadesProtesis>();
            GestosProtesisVR gestos = jugador.GetComponent<GestosProtesisVR>();
            Assert.IsNotNull(h);
            Assert.IsNotNull(gestos);
            Assert.AreEqual(1, h.Nivel(HabilidadProtesis.Cabeza), "El implante de ojos da la visión.");

            PonerMano(false, new Vector3(-0.15f, 1.6f, 0f), Quaternion.identity);
            PonerMano(true, new Vector3(0.15f, 1.6f, 0f), Quaternion.identity);
            yield return Cuadros(3);
            Assert.AreEqual(GestoProtesisVR.Vision, gestos.PoseActual, "Las dos manos a los lados de la cabeza son la pose de VISIÓN.");
            Assert.IsFalse(GestosProtesisVR.EnPose, "Sin los grips la pose no toma los grips de nadie.");
            Mantener(controlIzq.gripButton);
            Mantener(controlDer.gripButton);
            yield return Segundos(0.2f);
            Assert.IsTrue(GestosProtesisVR.EnPose, "Con los dos grips sí.");
            Assert.AreEqual(EstadoHabilidad.Lista, h.Estado(HabilidadProtesis.Cabeza), "Antes de 0.5 s no se activa.");
            yield return Segundos(0.55f);
            Assert.AreEqual(EstadoHabilidad.Activa, h.Estado(HabilidadProtesis.Cabeza), "Con los dos grips 0.5 s se activa la VISIÓN.");
            Assert.AreEqual(1, gestos.Activaciones);
            Soltar(controlIzq.gripButton);
            Soltar(controlDer.gripButton);
            yield return Cuadros(2);
        }

        [UnityTest]
        public IEnumerator Piernas_ManosJuntasAbajo_SeActivanALaPrimera()
        {
            implantes.Poner(EspacioImplante.Piernas, 1);
            yield return ArmarVR();
            Registro<IImplantes>.Agregar(implantes);
            PonerCabeza(new Vector3(0f, 1.6f, 0f), Quaternion.identity);
            yield return Cuadros(4);
            HabilidadesProtesis h = jugador.GetComponent<HabilidadesProtesis>();
            // Manos juntas, a media pierna (antes tenían que ir sobre cada muslo).
            PonerMano(false, new Vector3(-0.04f, 0.85f, 0.1f), Quaternion.identity);
            PonerMano(true, new Vector3(0.04f, 0.85f, 0.1f), Quaternion.identity);
            yield return Cuadros(3);
            Mantener(controlIzq.gripButton);
            Mantener(controlDer.gripButton);
            yield return Segundos(0.75f);
            Assert.AreEqual(EstadoHabilidad.Activa, h.Estado(HabilidadProtesis.Piernas), "Las piernas se activan con las manos juntas abajo.");
            Soltar(controlIzq.gripButton);
            Soltar(controlDer.gripButton);
            yield return Cuadros(2);
        }
    }
}
