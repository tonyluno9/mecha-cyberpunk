using System.Collections.Generic;
using MechaCyberpunk.Mecha.Llamado;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mecha
{
    public class ReglaLlamadoPruebas
    {
        private const float Radio = 30f;

        private static PuntoLlamado Punto(float x, float z, float radio = Radio, bool habilitado = true, float y = 0f)
        {
            return new PuntoLlamado(new Pose(new Vector3(x, y, z), Quaternion.identity), radio, habilitado);
        }

        private static ResultadoLlamado Decidir(Vector3 jugador, List<PuntoLlamado> puntos, EstadoLlamado estado, bool ocupado, out int indice)
        {
            return ReglaLlamado.Decidir(jugador, puntos, estado, ocupado, out indice);
        }

        [Test]
        public void Decidir_DentroDelRadio_Llega()
        {
            ResultadoLlamado r = Decidir(new Vector3(0f, 0f, 10f), new List<PuntoLlamado> { Punto(0f, 20f) }, EstadoLlamado.EnTaller, false, out int i);
            Assert.AreEqual(ResultadoLlamado.Llega, r);
            Assert.AreEqual(0, i);
        }

        [Test]
        public void Decidir_FueraDelRadio_ZonaProhibida()
        {
            ResultadoLlamado r = Decidir(new Vector3(0f, 0f, 90f), new List<PuntoLlamado> { Punto(0f, 20f) }, EstadoLlamado.EnTaller, false, out int i);
            Assert.AreEqual(ResultadoLlamado.ZonaProhibida, r);
            Assert.AreEqual(-1, i);
        }

        [Test]
        public void Decidir_JustoEnElBorde_Llega()
        {
            ResultadoLlamado r = Decidir(new Vector3(0f, 0f, 50f), new List<PuntoLlamado> { Punto(0f, 20f) }, EstadoLlamado.EnTaller, false, out _);
            Assert.AreEqual(ResultadoLlamado.Llega, r, "A 30 m exactos todavía está dentro");
        }

        [Test]
        public void Decidir_LaAlturaNoCuenta()
        {
            // 25 m en horizontal, 200 m arriba: dentro. A 31 m en horizontal, a ras de piso: fuera.
            Assert.AreEqual(ResultadoLlamado.Llega,
                Decidir(new Vector3(0f, 200f, 5f), new List<PuntoLlamado> { Punto(0f, 30f) }, EstadoLlamado.EnTaller, false, out _));
            Assert.AreEqual(ResultadoLlamado.ZonaProhibida,
                Decidir(new Vector3(0f, 0f, -11f), new List<PuntoLlamado> { Punto(0f, 20f) }, EstadoLlamado.EnTaller, false, out _));
        }

        [Test]
        public void Decidir_PuntoDeshabilitado_NoCuenta()
        {
            ResultadoLlamado r = Decidir(new Vector3(0f, 0f, 20f), new List<PuntoLlamado> { Punto(0f, 20f, Radio, false) },
                EstadoLlamado.EnTaller, false, out int i);
            Assert.AreEqual(ResultadoLlamado.ZonaProhibida, r);
            Assert.AreEqual(-1, i);
        }

        [Test]
        public void Decidir_UnoDeshabilitadoYOtroNo_GanaElHabilitado()
        {
            var puntos = new List<PuntoLlamado> { Punto(0f, 20f, Radio, false), Punto(0f, 40f) };
            ResultadoLlamado r = Decidir(new Vector3(0f, 0f, 21f), puntos, EstadoLlamado.EnTaller, false, out int i);
            Assert.AreEqual(ResultadoLlamado.Llega, r);
            Assert.AreEqual(1, i);
        }

        [Test]
        public void Decidir_DosZonasEncimadas_GanaLaLlegadaMasCercana()
        {
            var puntos = new List<PuntoLlamado> { Punto(0f, 0f), Punto(0f, 25f), Punto(0f, 60f) };
            ResultadoLlamado r = Decidir(new Vector3(0f, 0f, 20f), puntos, EstadoLlamado.EnTaller, false, out int i);
            Assert.AreEqual(ResultadoLlamado.Llega, r);
            Assert.AreEqual(1, i, "La llegada a 5 m gana a la de 20 m");

            Decidir(new Vector3(0f, 0f, 5f), puntos, EstadoLlamado.EnTaller, false, out int j);
            Assert.AreEqual(0, j, "Desde otro sitio gana la otra");
        }

        [Test]
        public void Decidir_RadiosDistintos_CadaUnoUsaElSuyo()
        {
            var puntos = new List<PuntoLlamado> { Punto(0f, 0f, 5f), Punto(0f, 100f, 50f) };
            Assert.AreEqual(1, IndiceDe(new Vector3(0f, 0f, 60f), puntos));
            Assert.AreEqual(0, IndiceDe(new Vector3(0f, 0f, 4f), puntos));
        }

        private static int IndiceDe(Vector3 jugador, List<PuntoLlamado> puntos)
        {
            Decidir(jugador, puntos, EstadoLlamado.EnTaller, false, out int i);
            return i;
        }

        [Test]
        public void Decidir_Llegando_YaViene()
        {
            ResultadoLlamado r = Decidir(Vector3.zero, new List<PuntoLlamado> { Punto(0f, 20f) }, EstadoLlamado.Llegando, false, out int i);
            Assert.AreEqual(ResultadoLlamado.YaViene, r);
            Assert.AreEqual(-1, i);
        }

        [Test]
        public void Decidir_Regresando_Nada()
        {
            Assert.AreEqual(ResultadoLlamado.Nada,
                Decidir(Vector3.zero, new List<PuntoLlamado> { Punto(0f, 20f) }, EstadoLlamado.Regresando, false, out _));
        }

        [Test]
        public void Decidir_EnCalleVacio_Regresa_AunqueEstesFueraDeZona()
        {
            Assert.AreEqual(ResultadoLlamado.Regresa,
                Decidir(new Vector3(0f, 0f, 500f), new List<PuntoLlamado> { Punto(0f, 20f) }, EstadoLlamado.EnCalle, false, out int i));
            Assert.AreEqual(-1, i);
        }

        [Test]
        public void Decidir_Ocupado_MandaSobreCualquierEstado()
        {
            foreach (EstadoLlamado estado in new[] { EstadoLlamado.EnTaller, EstadoLlamado.Llegando, EstadoLlamado.EnCalle, EstadoLlamado.Regresando })
            {
                Assert.AreEqual(ResultadoLlamado.Ocupado,
                    Decidir(Vector3.zero, new List<PuntoLlamado> { Punto(0f, 20f) }, estado, true, out _), estado.ToString());
            }
        }

        [Test]
        public void Decidir_ListaVacia_ZonaProhibida()
        {
            Assert.AreEqual(ResultadoLlamado.ZonaProhibida,
                Decidir(Vector3.zero, new List<PuntoLlamado>(), EstadoLlamado.EnTaller, false, out int i));
            Assert.AreEqual(-1, i);
        }

        [Test]
        public void Decidir_ListaNull_ZonaProhibida()
        {
            Assert.AreEqual(ResultadoLlamado.ZonaProhibida, Decidir(Vector3.zero, null, EstadoLlamado.EnTaller, false, out int i));
            Assert.AreEqual(-1, i);
        }

        [Test]
        public void DistanciaHorizontal_IgnoraLaAltura()
        {
            Assert.AreEqual(5f, ReglaLlamado.DistanciaHorizontal(new Vector3(0f, 100f, 0f), new Vector3(3f, -4f, 4f)), 0.0001f);
        }
    }
}
