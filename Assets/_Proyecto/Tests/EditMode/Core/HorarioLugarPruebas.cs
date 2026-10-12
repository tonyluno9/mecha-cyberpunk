using System;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    public class HorarioLugarPruebas
    {
        private class RelojFalso : IRelojMundo
        {
            public float Hora => 12f;
            public int Dia => 1;
            public FranjaDia Franja => FranjaDia.Dia;
            public bool EsDeNoche => false;
            public float HoraAmanecer => 6f;
            public float HoraAnochecer => 20f;
            public bool Pausado { get; private set; }
            public event Action<int> HoraEnteraCambio;
            public event Action<FranjaDia> FranjaCambio;
            public event Action<int> DiaCambio;
            public void Pausar(bool pausado) => Pausado = pausado;
            public void AvanzarHasta(float hora) { }
        }

        private static readonly HorarioLugar Nocturno = new HorarioLugar(20f, 6f);
        private static readonly HorarioLugar Diurno = new HorarioLugar(9f, 18f);

        [TestCase(23f)]
        [TestCase(2f)]
        [TestCase(20f)]
        public void EstaAbierto_Nocturno_AbiertoEnLaNocheYEnLaApertura(float hora)
            => Assert.IsTrue(Nocturno.EstaAbierto(hora));

        [TestCase(6f)]
        [TestCase(12f)]
        public void EstaAbierto_Nocturno_CerradoEnElCierreYDeDia(float hora)
            => Assert.IsFalse(Nocturno.EstaAbierto(hora));

        [Test]
        public void EstaAbierto_Diurno_AbreALas9()
            => Assert.IsTrue(Diurno.EstaAbierto(9f));

        [TestCase(18f)]
        [TestCase(22f)]
        public void EstaAbierto_Diurno_CerradoDesdeElCierre(float hora)
            => Assert.IsFalse(Diurno.EstaAbierto(hora));

        [TestCase(0f)]
        [TestCase(12.5f)]
        [TestCase(23.99f)]
        public void EstaAbierto_Siempre_EsTrueACualquierHora(float hora)
            => Assert.IsTrue(HorarioLugar.Siempre.EstaAbierto(hora));

        [TestCase(0f)]
        [TestCase(8f)]
        [TestCase(15f)]
        public void EstaAbierto_AbreIgualCierra_NuncaAbre(float hora)
        {
            var nunca = new HorarioLugar(8f, 8f);

            Assert.IsFalse(nunca.EstaAbierto(hora));
            Assert.IsTrue(float.IsPositiveInfinity(nunca.HorasHastaAbrir(hora)));
        }

        [Test]
        public void EstaAbierto_HoraFueraDeRango_SeNormaliza()
        {
            Assert.IsTrue(Nocturno.EstaAbierto(-1f), "-1 es 23");
            Assert.IsTrue(Nocturno.EstaAbierto(25f), "25 es 1");
            Assert.IsTrue(Nocturno.EstaAbierto(24f), "24 es 0");
            Assert.IsFalse(Diurno.EstaAbierto(24f), "24 es 0, cerrado de 9 a 18");
            Assert.IsTrue(Diurno.EstaAbierto(33f), "33 es 9");
            Assert.IsFalse(Diurno.EstaAbierto(-1f), "-1 es 23");
        }

        [Test]
        public void HorasHastaAbrir_Nocturno_ADoceFaltanOcho()
            => Assert.AreEqual(8f, Nocturno.HorasHastaAbrir(12f), 0.0001f);

        [Test]
        public void HorasHastaAbrir_Diurno_ALas19FaltanCatorce()
            => Assert.AreEqual(14f, Diurno.HorasHastaAbrir(19f), 0.0001f);

        [Test]
        public void HorasHastaAbrir_Abierto_EsCero()
        {
            Assert.AreEqual(0f, Nocturno.HorasHastaAbrir(23f));
            Assert.AreEqual(0f, HorarioLugar.Siempre.HorasHastaAbrir(5f));
        }

        [Test]
        public void TextoHora_FormatoYTruncado()
        {
            Assert.AreEqual("21:30", HorarioLugar.TextoHora(21.5f));
            Assert.AreEqual("00:00", HorarioLugar.TextoHora(24f));
            Assert.AreEqual("07:59", HorarioLugar.TextoHora(7.999f));
        }

        [Test]
        public void Texto_Casos()
        {
            Assert.AreEqual("20:00 a 06:00", Nocturno.Texto());
            Assert.AreEqual("24 horas", HorarioLugar.Siempre.Texto());
            Assert.AreEqual("Cerrado", new HorarioLugar(8f, 8f).Texto());
        }

        [Test]
        public void Serializacion_SeGuardaYSeLeeEnUnScriptableObject()
        {
            var contenedor = ScriptableObject.CreateInstance<ContenedorHorarioPrueba>();
            try
            {
                var so = new SerializedObject(contenedor);
                var horario = so.FindProperty("horario");
                horario.FindPropertyRelative("abre").floatValue = 20f;
                horario.FindPropertyRelative("cierra").floatValue = 6f;
                so.ApplyModifiedPropertiesWithoutUndo();

                Assert.AreEqual(20f, contenedor.Horario.Abre);
                Assert.AreEqual(6f, contenedor.Horario.Cierra);
                Assert.IsFalse(contenedor.Horario.SiempreAbierto);
                Assert.IsTrue(contenedor.Horario.EstaAbierto(23f));
                Assert.AreEqual("20:00 a 06:00", contenedor.Horario.Texto());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(contenedor);
            }
        }

        [Test]
        public void RelojFalso_ImplementaElContratoSinAreas()
        {
            IRelojMundo reloj = new RelojFalso();

            reloj.Pausar(true);

            Assert.IsTrue(reloj.Pausado);
            Assert.AreEqual(FranjaDia.Dia, reloj.Franja);
            Assert.IsFalse(Nocturno.EstaAbierto(reloj.Hora));
        }
    }
}
