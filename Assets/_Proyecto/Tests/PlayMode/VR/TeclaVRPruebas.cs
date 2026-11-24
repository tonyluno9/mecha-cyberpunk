using MechaCyberpunk.VR;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>V6, lógica pura: la E de interactuar pasa a B en los avisos de VR, sin tocar la E de las palabras; y dónde va el letrero.</summary>
    public class TeclaVRPruebas
    {
        [TestCase("E · Hablar", "B · Hablar")]
        [TestCase("E · Abrir Bar", "B · Abrir Bar")]
        [TestCase("E: ENCARGO · Robo de carro", "B: ENCARGO · Robo de carro")]
        [TestCase("E: CANCELAR ENCARGO", "B: CANCELAR ENCARGO")]
        [TestCase("[E] Subir al carro", "[B] Subir al carro")]
        [TestCase("E·Usar", "B·Usar")]
        public void Convertir_TeclaE_PasaAB(string pc, string vr)
        {
            Assert.AreEqual(vr, TeclaVR.Convertir(pc));
        }

        [TestCase("Saltar", "A")]
        [TestCase("Interactuar", "B")]
        [TestCase("Recargar", "X")]
        [TestCase("Rueda", "MANTÉN Y")]
        public void Boton_RepartoVR19(string accion, string boton)
        {
            Assert.AreEqual(boton, TeclaVR.Boton(accion));
            Assert.IsNull(TeclaVR.Boton("NoExiste"));
        }

        [TestCase("ENCARGO: Robo")]
        [TestCase("CERRADO · abre 20:00")]
        [TestCase("TE: algo")]
        [TestCase("ELITE · E")]
        [TestCase("PRESIONA E PARA CONFIGURAR")]
        [TestCase("Hablar")]
        public void Convertir_EDentroDePalabras_NoCambia(string texto)
        {
            Assert.AreEqual(texto, TeclaVR.Convertir(texto));
        }

        [Test]
        public void Convertir_NullOVacio_DevuelveVacio()
        {
            Assert.AreEqual(string.Empty, TeclaVR.Convertir(null));
            Assert.AreEqual(string.Empty, TeclaVR.Convertir(string.Empty));
        }

        [TestCase("Hablar", "B · Hablar")]
        [TestCase("Abrir Bar", "B · Abrir Bar")]
        [TestCase("E: ENCARGO · Robo", "B: ENCARGO · Robo")]
        [TestCase("E · Dormir", "B · Dormir")]
        [TestCase("", "")]
        public void ConTecla_PoneBOCambiaLaE(string accion, string letrero)
        {
            Assert.AreEqual(letrero, TeclaVR.ConTecla(accion));
        }

        [Test]
        public void Posicion_ConBounds_EncimaDelCentro()
        {
            var bounds = new Bounds(new Vector3(2f, 0.9f, 3f), new Vector3(0.6f, 1.8f, 0.4f));
            Vector3 p = ReglaAvisoInteraccionVR.Posicion(true, bounds, new Vector3(2f, 0f, 3.5f));
            Assert.AreEqual(1.8f + ReglaAvisoInteraccionVR.MargenArriba, p.y, 1e-4f);
            Assert.AreEqual(2f, p.x, 1e-4f);
            Assert.AreEqual(3f, p.z, 1e-4f);
        }

        [Test]
        public void Posicion_SinBounds_AlturaFijaSobreElPunto()
        {
            Vector3 p = ReglaAvisoInteraccionVR.Posicion(false, default, new Vector3(1f, 0.2f, -1f));
            Assert.AreEqual(new Vector3(1f, 0.2f + ReglaAvisoInteraccionVR.AlturaRespaldo, -1f), p);
        }

        [Test]
        public void PiezaCuenta_LejosOMuyAlta_NoCuenta()
        {
            Vector3 punto = Vector3.zero;
            Assert.IsTrue(ReglaAvisoInteraccionVR.PiezaCuenta(new Bounds(new Vector3(0f, 1f, 0.5f), Vector3.one), punto));
            Assert.IsFalse(ReglaAvisoInteraccionVR.PiezaCuenta(new Bounds(new Vector3(0f, 1f, 5f), Vector3.one), punto), "Lejos del punto.");
            Assert.IsFalse(ReglaAvisoInteraccionVR.PiezaCuenta(new Bounds(new Vector3(0f, 10f, 0f), new Vector3(10f, 20f, 10f)), punto), "Un edificio.");
        }

        [Test]
        public void Escala_CreceConLaDistancia()
        {
            Assert.AreEqual(1f, ReglaAvisoInteraccionVR.Escala(ReglaAvisoInteraccionVR.DistanciaReferencia), 1e-4f);
            Assert.Greater(ReglaAvisoInteraccionVR.Escala(3f), ReglaAvisoInteraccionVR.Escala(1f));
            Assert.AreEqual(ReglaAvisoInteraccionVR.EscalaMinima, ReglaAvisoInteraccionVR.Escala(0.1f), 1e-4f);
            Assert.AreEqual(ReglaAvisoInteraccionVR.EscalaMaxima, ReglaAvisoInteraccionVR.Escala(50f), 1e-4f);
        }

        [Test]
        public void Giro_MiraLejosDeLaCabezaYDePie()
        {
            Quaternion giro = ReglaAvisoInteraccionVR.Giro(new Vector3(0f, 2f, 2f), new Vector3(0f, 1.7f, 0f));
            Vector3 frente = giro * Vector3.forward;
            Assert.AreEqual(1f, Vector3.Dot(frente, Vector3.forward), 1e-4f);
        }
    }
}
