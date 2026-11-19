using System;
using MechaCyberpunk.Trafico.Drones;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    public class RutaAereaT1398Pruebas
    {
        // Cuadrado en sentido de las manecillas (mirando desde arriba, x al este y z al norte): puros giros a la derecha.
        private static readonly Vector2[] CuadradoHorario = { new Vector2(-50f, -50f), new Vector2(-50f, 50f), new Vector2(50f, 50f), new Vector2(50f, -50f) };

        private static Vector2[] Invertida(Vector2[] puntos)
        {
            Vector2[] r = (Vector2[])puntos.Clone();
            Array.Reverse(r);
            return r;
        }

        private static CalculoRutaAerea Crear(Vector2[] puntos, float altura = 20f, float velocidad = 10f, float amplitud = 0f, float periodo = 4f,
            float alabeoMax = 15f, float alabeoPorGiro = 0.5f)
        {
            return new CalculoRutaAerea(puntos, altura, velocidad, amplitud, periodo, alabeoMax, alabeoPorGiro);
        }

        [Test]
        public void Constructor_MenosDeTresPuntos_Falla()
        {
            Assert.Throws<ArgumentException>(() => Crear(new[] { Vector2.zero, Vector2.one }));
        }

        [Test]
        public void Constructor_VelocidadCero_Falla()
        {
            Assert.Throws<ArgumentException>(() => Crear(CuadradoHorario, velocidad: 0f));
        }

        [Test]
        public void Constructor_AmplitudSinPeriodo_Falla()
        {
            Assert.Throws<ArgumentException>(() => Crear(CuadradoHorario, amplitud: 1f, periodo: 0f));
        }

        [Test]
        public void DuracionVuelta_EsLongitudEntreVelocidad()
        {
            CalculoRutaAerea ruta = Crear(CuadradoHorario, velocidad: 10f);
            Assert.AreEqual(ruta.Longitud / 10f, ruta.DuracionVuelta, 0.0001f);
            Assert.Greater(ruta.Longitud, 300f, "El cuadrado de 100 m mide al menos 300 m.");
        }

        [Test]
        public void Muestra_UnaVueltaDespues_VuelveAlMismoPunto()
        {
            CalculoRutaAerea ruta = Crear(CuadradoHorario);
            Vector3 inicio = ruta.Muestra(0f).Posicion;
            Vector3 fin = ruta.Muestra(ruta.DuracionVuelta).Posicion;
            Assert.AreEqual(inicio.x, fin.x, 0.05f);
            Assert.AreEqual(inicio.z, fin.z, 0.05f);
        }

        [Test]
        public void Muestra_TiempoNegativo_EnvuelveSinFallar()
        {
            CalculoRutaAerea ruta = Crear(CuadradoHorario);
            Vector3 a = ruta.Muestra(-3f).Posicion;
            Vector3 b = ruta.Muestra(ruta.DuracionVuelta - 3f).Posicion;
            Assert.AreEqual(a.x, b.x, 0.05f);
            Assert.AreEqual(a.z, b.z, 0.05f);
        }

        [Test]
        public void Muestra_VelocidadConstante_ElDronAvanzaLoQueDiceLaVelocidad()
        {
            CalculoRutaAerea ruta = Crear(CuadradoHorario, velocidad: 12f);
            const float dt = 0.1f;
            for (float t = 0f; t < ruta.DuracionVuelta; t += 1.7f)
            {
                Vector3 a = ruta.Muestra(t).Posicion;
                Vector3 b = ruta.Muestra(t + dt).Posicion;
                float recorrido = new Vector2(b.x - a.x, b.z - a.z).magnitude;
                // La curva se aproxima con segmentos: en recta la distancia es exacta y en curva queda un poco corta.
                Assert.AreEqual(12f * dt, recorrido, 0.15f, "en t=" + t);
            }
        }

        [Test]
        public void Muestra_SinOscilacion_AlturaFija()
        {
            CalculoRutaAerea ruta = Crear(CuadradoHorario, altura: 25f, amplitud: 0f);
            for (float t = 0f; t < 30f; t += 3f)
            {
                Assert.AreEqual(25f, ruta.Muestra(t).Posicion.y, 0.0001f);
            }
        }

        [Test]
        public void Muestra_ConOscilacion_SubeYBajaDentroDeLaAmplitud()
        {
            CalculoRutaAerea ruta = Crear(CuadradoHorario, altura: 25f, amplitud: 1.5f, periodo: 4f);
            float min = float.MaxValue, max = float.MinValue;
            for (float t = 0f; t < 8f; t += 0.1f)
            {
                float y = ruta.Muestra(t).Posicion.y;
                min = Mathf.Min(min, y);
                max = Mathf.Max(max, y);
            }
            Assert.AreEqual(26.5f, max, 0.05f);
            Assert.AreEqual(23.5f, min, 0.05f);
        }

        [Test]
        public void Muestra_RumboEsUnitarioYHorizontal()
        {
            CalculoRutaAerea ruta = Crear(CuadradoHorario);
            for (float t = 0f; t < ruta.DuracionVuelta; t += 2.3f)
            {
                Vector3 rumbo = ruta.Muestra(t).Rumbo;
                Assert.AreEqual(1f, rumbo.magnitude, 0.001f);
                Assert.AreEqual(0f, rumbo.y, 0.0001f);
            }
        }

        [Test]
        public void Muestra_RumboSigueLaDireccionDelMovimiento()
        {
            CalculoRutaAerea ruta = Crear(CuadradoHorario);
            Vector3 a = ruta.Muestra(1f).Posicion;
            Vector3 b = ruta.Muestra(1.2f).Posicion;
            Vector3 movimiento = new Vector3(b.x - a.x, 0f, b.z - a.z).normalized;
            Assert.Greater(Vector3.Dot(ruta.Muestra(1.1f).Rumbo, movimiento), 0.99f);
        }

        [Test]
        public void Muestra_GiroALaDerecha_InclinaNegativo()
        {
            CalculoRutaAerea ruta = Crear(CuadradoHorario);
            float minimo = float.MaxValue;
            for (float t = 0f; t < ruta.DuracionVuelta; t += 0.2f)
            {
                minimo = Mathf.Min(minimo, ruta.Muestra(t).Alabeo);
            }
            Assert.Less(minimo, -1f, "En las esquinas se inclina.");
        }

        [Test]
        public void Muestra_GiroALaIzquierda_InclinaPositivo()
        {
            CalculoRutaAerea ruta = Crear(Invertida(CuadradoHorario));
            float maximo = float.MinValue;
            for (float t = 0f; t < ruta.DuracionVuelta; t += 0.2f)
            {
                maximo = Mathf.Max(maximo, ruta.Muestra(t).Alabeo);
            }
            Assert.Greater(maximo, 1f);
        }

        [Test]
        public void Muestra_Inclinacion_NuncaPasaDelTope()
        {
            CalculoRutaAerea ruta = Crear(CuadradoHorario, alabeoMax: 8f, alabeoPorGiro: 50f);
            for (float t = 0f; t < ruta.DuracionVuelta; t += 0.1f)
            {
                Assert.LessOrEqual(Mathf.Abs(ruta.Muestra(t).Alabeo), 8f + 0.0001f);
            }
        }

        [Test]
        public void Muestra_SinInclinacionPorGiro_NoSeInclina()
        {
            CalculoRutaAerea ruta = Crear(CuadradoHorario, alabeoPorGiro: 0f);
            for (float t = 0f; t < ruta.DuracionVuelta; t += 0.5f)
            {
                Assert.AreEqual(0f, ruta.Muestra(t).Alabeo, 0.0001f);
            }
        }

        [Test]
        public void Muestra_ConFase_ArrancaAdelantado()
        {
            CalculoRutaAerea ruta = Crear(CuadradoHorario);
            Vector3 mitad = ruta.Muestra(0f, 0.5f).Posicion;
            Vector3 igual = ruta.Muestra(ruta.DuracionVuelta * 0.5f).Posicion;
            Assert.AreEqual(igual.x, mitad.x, 0.05f);
            Assert.AreEqual(igual.z, mitad.z, 0.05f);
            Vector3 inicio = ruta.Muestra(0f).Posicion;
            Assert.Greater(new Vector2(mitad.x - inicio.x, mitad.z - inicio.z).magnitude, 50f, "A media vuelta está lejos del inicio.");
        }

        [Test]
        public void DentroDeAlcance_CorteDeGiroDeRotores()
        {
            Assert.IsTrue(CalculoGiroRotores.DentroDeAlcance(99f * 99f, 100f));
            Assert.IsFalse(CalculoGiroRotores.DentroDeAlcance(101f * 101f, 100f));
            Assert.IsTrue(CalculoGiroRotores.DentroDeAlcance(1e9f, 0f), "Con 0 no hay corte.");
        }
    }
}
