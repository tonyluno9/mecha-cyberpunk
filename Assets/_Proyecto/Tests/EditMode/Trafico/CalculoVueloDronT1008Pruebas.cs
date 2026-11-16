using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>T-1008: el vuelo natural del dron (lógica pura): inercia, frenado suave, inclinación con la aceleración y rotación sin saltos.</summary>
    public class CalculoVueloDronT1008Pruebas
    {
        private const float Paso = 0.02f;
        private const float Tolerancia = 0.01f;

        private static ParametrosVueloDron Parametros(float flote = 0f, float balanceo = 0f)
        {
            return new ParametrosVueloDron
            {
                VelocidadMaxima = 9f, AceleracionMaxima = 7f, AceleracionFrenado = 4f, VelocidadVertical = 4f, AceleracionVertical = 4f,
                GradosRumboPorSegundo = 120f, InclinacionMaxima = 20f, GradosPorAceleracion = 2.5f, SuavizadoInclinacion = 6f,
                AmplitudFlote = flote, FrecuenciaFlote = 0.5f, AmplitudBalanceo = balanceo, FrecuenciaBalanceo = 0.35f,
            };
        }

        [Test]
        public void VaALaMeta_NuncaRebasaLaVelocidadMaxima()
        {
            CalculoVueloDron vuelo = new CalculoVueloDron(Parametros());
            Vector3 pos = Vector3.zero;
            Vector3 meta = new Vector3(0f, 0f, 200f);
            float maxima = 0f;
            for (int i = 0; i < 1500; i++)
            {
                pos += vuelo.Paso(pos, meta, 0f, Paso);
                maxima = Mathf.Max(maxima, new Vector3(vuelo.Velocidad.x, 0f, vuelo.Velocidad.z).magnitude);
            }
            Assert.LessOrEqual(maxima, 9f + Tolerancia);
            Assert.Greater(maxima, 8.5f, "Sí llega a su velocidad de crucero");
        }

        [Test]
        public void Arranque_NoSaltaALaVelocidadMaxima_AceleraConInercia()
        {
            CalculoVueloDron vuelo = new CalculoVueloDron(Parametros());
            vuelo.Paso(Vector3.zero, new Vector3(0f, 0f, 200f), 0f, Paso);
            Assert.LessOrEqual(vuelo.Velocidad.magnitude, 7f * Paso + Tolerancia, "En un paso solo gana lo que da la aceleración máxima");
        }

        [Test]
        public void AlLlegar_DesaceleraAntesDeLaMeta_YNoPasaDeLargo()
        {
            CalculoVueloDron vuelo = new CalculoVueloDron(Parametros());
            Vector3 pos = Vector3.zero;
            Vector3 meta = new Vector3(0f, 0f, 30f);
            bool desaceleroAntes = false;
            float velocidadAnterior = 0f;
            float pasoDeLargo = 0f;
            for (int i = 0; i < 1500; i++)
            {
                pos += vuelo.Paso(pos, meta, 0f, Paso);
                float v = vuelo.Velocidad.z;
                if (v < velocidadAnterior - 0.001f && meta.z - pos.z > 2f)
                {
                    desaceleroAntes = true;
                }
                velocidadAnterior = v;
                pasoDeLargo = Mathf.Max(pasoDeLargo, pos.z - meta.z);
            }
            Assert.IsTrue(desaceleroAntes, "Empieza a frenar a más de 2 m de la meta");
            Assert.LessOrEqual(pasoDeLargo, 0.2f, "No se pasa de la meta (m)");
            Assert.AreEqual(30f, pos.z, 0.1f, "Termina sobre la meta");
            Assert.Less(vuelo.Velocidad.magnitude, 0.05f, "Y queda quieto");
        }

        [Test]
        public void FrenadoSuave_LaDesaceleracionNoPasaDeLaMaxima()
        {
            CalculoVueloDron vuelo = new CalculoVueloDron(Parametros());
            Vector3 pos = Vector3.zero;
            Vector3 meta = new Vector3(0f, 0f, 40f);
            float anterior = 0f;
            float mayor = 0f;
            for (int i = 0; i < 1500; i++)
            {
                pos += vuelo.Paso(pos, meta, 0f, Paso);
                mayor = Mathf.Max(mayor, Mathf.Abs(vuelo.Velocidad.z - anterior) / Paso);
                anterior = vuelo.Velocidad.z;
            }
            Assert.LessOrEqual(mayor, 7f + Tolerancia);
        }

        [Test]
        public void AlAcelerarHaciaDelante_BajaLaTrompa_YEnReposoVuelveACero()
        {
            CalculoVueloDron vuelo = new CalculoVueloDron(Parametros());
            Vector3 pos = Vector3.zero;
            Vector3 meta = new Vector3(0f, 0f, 120f); // mira al norte (+Z), va al norte
            for (int i = 0; i < 25; i++)
            {
                pos += vuelo.Paso(pos, meta, 0f, Paso);
            }
            Assert.Greater(vuelo.Cabeceo, 5f, "Acelerando hacia delante baja la trompa");
            Assert.AreEqual(0f, vuelo.Alabeo, 0.1f, "Sin aceleración lateral no se ladea");
            meta = pos; // ya está en la meta: se queda
            for (int i = 0; i < 1500; i++)
            {
                pos += vuelo.Paso(pos, meta, 0f, Paso);
            }
            Assert.AreEqual(0f, vuelo.Cabeceo, 0.1f);
            Assert.AreEqual(0f, vuelo.Alabeo, 0.1f);
        }

        [Test]
        public void AlAcelerarALaDerecha_SeLadeaALaDerecha()
        {
            CalculoVueloDron vuelo = new CalculoVueloDron(Parametros()); // mira a +Z; su derecha es +X
            Vector3 pos = Vector3.zero;
            for (int i = 0; i < 25; i++)
            {
                pos += vuelo.Paso(pos, new Vector3(100f, 0f, 0f), 0f, Paso);
            }
            Assert.Less(vuelo.Alabeo, -5f, "Acelerar a la derecha inclina el lado derecho hacia abajo (alabeo negativo)");
            Assert.AreEqual(0f, vuelo.Cabeceo, 0.1f);
        }

        [Test]
        public void Inclinacion_NuncaPasaDelMaximo()
        {
            CalculoVueloDron vuelo = new CalculoVueloDron(Parametros());
            Vector3 pos = Vector3.zero;
            float mayor = 0f;
            for (int i = 0; i < 500; i++)
            {
                Vector3 meta = new Vector3(i % 100 < 50 ? 500f : -500f, 0f, i % 70 < 35 ? 500f : -500f);
                pos += vuelo.Paso(pos, meta, 0f, Paso);
                mayor = Mathf.Max(mayor, Mathf.Abs(vuelo.Cabeceo), Mathf.Abs(vuelo.Alabeo));
            }
            Assert.LessOrEqual(mayor, 20f + Tolerancia);
        }

        [Test]
        public void Rotacion_CambiaPocoEntreCuadros_AunConGiroBrusco()
        {
            CalculoVueloDron vuelo = new CalculoVueloDron(Parametros(0.12f, 1.5f));
            Vector3 pos = Vector3.zero;
            Quaternion anterior = vuelo.Rotacion;
            float mayor = 0f;
            for (int i = 0; i < 600; i++)
            {
                float rumbo = i < 300 ? 0f : 180f; // media vuelta de golpe
                Vector3 meta = i < 300 ? new Vector3(0f, 0f, 100f) : new Vector3(0f, 0f, -100f);
                pos += vuelo.Paso(pos, meta, rumbo, Paso);
                mayor = Mathf.Max(mayor, Quaternion.Angle(anterior, vuelo.Rotacion));
                anterior = vuelo.Rotacion;
            }
            Assert.LessOrEqual(mayor, 120f * Paso + 20f * 6f * Paso + 1f, "Sin saltos: lo que gira el rumbo más lo que cambia la inclinación en un cuadro (grados)");
        }

        [Test]
        public void Rumbo_GiraConVelocidadAngularLimitada_YPorElCaminoCorto()
        {
            CalculoVueloDron vuelo = new CalculoVueloDron(Parametros(), 350f);
            vuelo.Paso(Vector3.zero, Vector3.zero, 10f, Paso);
            Assert.AreEqual(Mathf.Repeat(350f + 120f * Paso, 360f), Mathf.Repeat(vuelo.Rumbo, 360f), 0.01f, "De 350 a 10 pasa por 0, no por 180");
        }

        [Test]
        public void Altura_SeAjustaSuave_ConVelocidadVerticalLimitada()
        {
            CalculoVueloDron vuelo = new CalculoVueloDron(Parametros());
            Vector3 pos = Vector3.zero;
            Vector3 meta = new Vector3(0f, 7f, 0f);
            float maxVertical = 0f;
            float maxCambio = 0f;
            float anterior = 0f;
            float pasoDeLargo = 0f;
            for (int i = 0; i < 1000; i++)
            {
                pos += vuelo.Paso(pos, meta, 0f, Paso);
                maxVertical = Mathf.Max(maxVertical, Mathf.Abs(vuelo.Velocidad.y));
                maxCambio = Mathf.Max(maxCambio, Mathf.Abs(vuelo.Velocidad.y - anterior) / Paso);
                anterior = vuelo.Velocidad.y;
                pasoDeLargo = Mathf.Max(pasoDeLargo, pos.y - 7f);
            }
            Assert.LessOrEqual(maxVertical, 4f + Tolerancia);
            Assert.LessOrEqual(maxCambio, 4f + Tolerancia, "Aceleración vertical limitada");
            Assert.LessOrEqual(pasoDeLargo, 0.2f);
            Assert.AreEqual(7f, pos.y, 0.1f);
        }

        [Test]
        public void Flote_SubeYBajaPeroNoSeAcumula()
        {
            CalculoVueloDron vuelo = new CalculoVueloDron(Parametros(0.12f));
            Vector3 pos = Vector3.zero;
            float minimo = 0f;
            float maximo = 0f;
            for (int i = 0; i < 1000; i++)
            {
                pos += vuelo.Paso(pos, pos, 0f, Paso);
                minimo = Mathf.Min(minimo, pos.y);
                maximo = Mathf.Max(maximo, pos.y);
            }
            Assert.Greater(maximo - minimo, 0.05f, "Flota: sube y baja");
            Assert.LessOrEqual(maximo - minimo, 2f * 0.12f + 0.01f, "Dentro de la amplitud del flote");
        }

        [Test]
        public void PasoConTiempoCero_NoHaceNada()
        {
            CalculoVueloDron vuelo = new CalculoVueloDron(Parametros());
            Assert.AreEqual(Vector3.zero, vuelo.Paso(Vector3.zero, Vector3.forward * 10f, 0f, 0f));
        }
    }
}
