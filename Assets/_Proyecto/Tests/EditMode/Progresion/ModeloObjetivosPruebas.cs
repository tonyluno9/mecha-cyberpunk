using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class ModeloObjetivosPruebas
    {
        private ModeloObjetivos modelo;
        private int avisos;
        private Objetivo ultimo;

        [SetUp]
        public void SetUp()
        {
            modelo = new ModeloObjetivos();
            avisos = 0;
            modelo.Cambio += o => { avisos++; ultimo = o; };
        }

        [Test]
        public void SinObjetivo_HayObjetivoEsFalseYActualEsNinguno()
        {
            Assert.IsFalse(modelo.HayObjetivo);
            Assert.AreEqual(Objetivo.Ninguno, modelo.Actual);
            Assert.IsNull(modelo.Actual.Texto);
            Assert.AreEqual(IdLugar.Ninguno, modelo.Actual.Lugar);
        }

        [Test]
        public void Fijar_AvisaUnaVezYGuarda()
        {
            modelo.Fijar(new Objetivo("Ve a casa", IdLugar.Casa));
            Assert.AreEqual(1, avisos);
            Assert.IsTrue(modelo.HayObjetivo);
            Assert.AreEqual("Ve a casa", modelo.Actual.Texto);
            Assert.AreEqual(IdLugar.Casa, ultimo.Lugar);
        }

        [Test]
        public void Fijar_MismoObjetivo_NoAvisa()
        {
            modelo.Fijar(new Objetivo("Ve a casa", IdLugar.Casa));
            modelo.Fijar(new Objetivo("Ve a casa", IdLugar.Casa));
            Assert.AreEqual(1, avisos);
            modelo.Fijar(new Objetivo("Ve al bar", IdLugar.Bar));
            Assert.AreEqual(2, avisos);
        }

        [Test]
        public void Fijar_MismaPosicion_NoAvisaYOtraPosicionSi()
        {
            modelo.Fijar(new Objetivo("Punto", new Vector3(1, 2, 3)));
            modelo.Fijar(new Objetivo("Punto", new Vector3(1, 2, 3)));
            Assert.AreEqual(1, avisos);
            modelo.Fijar(new Objetivo("Punto", new Vector3(4, 2, 3)));
            Assert.AreEqual(2, avisos);
            Assert.IsTrue(modelo.Actual.TienePosicion);
        }

        [Test]
        public void Quitar_AvisaYDejaNinguno()
        {
            modelo.Fijar(new Objetivo("Ve a casa", IdLugar.Casa));
            modelo.Quitar();
            Assert.AreEqual(2, avisos);
            Assert.IsFalse(modelo.HayObjetivo);
            Assert.AreEqual(Objetivo.Ninguno, modelo.Actual);
            Assert.AreEqual(Objetivo.Ninguno, ultimo);
        }

        [Test]
        public void Fijar_Ninguno_SeComportaComoQuitar()
        {
            modelo.Fijar(new Objetivo("Ve al taller", IdLugar.Taller));
            avisos = 0;
            modelo.Fijar(Objetivo.Ninguno);
            Assert.IsFalse(modelo.HayObjetivo);
            Assert.AreEqual(Objetivo.Ninguno, modelo.Actual);
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(Objetivo.Ninguno, ultimo);
        }

        [Test]
        public void Fijar_NingunoSinObjetivo_NoAvisaNiMarcaHayObjetivo()
        {
            modelo.Fijar(Objetivo.Ninguno);
            Assert.IsFalse(modelo.HayObjetivo);
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void Quitar_SinObjetivo_NoAvisa()
        {
            modelo.Quitar();
            Assert.AreEqual(0, avisos);
        }

        [Test]
        public void ObjetivoPorLugar_NoTienePosicion()
        {
            var o = new Objetivo("Ve al taller", IdLugar.Taller);
            Assert.IsFalse(o.TienePosicion);
            Assert.AreEqual(IdLugar.Taller, o.Lugar);
        }

        [Test]
        public void ObjetivoPorPosicion_TienePosicionYLugarNinguno()
        {
            var o = new Objetivo("Aquí", new Vector3(5, 0, 5));
            Assert.IsTrue(o.TienePosicion);
            Assert.AreEqual(new Vector3(5, 0, 5), o.Posicion);
            Assert.AreEqual(IdLugar.Ninguno, o.Lugar);
        }
    }
}
