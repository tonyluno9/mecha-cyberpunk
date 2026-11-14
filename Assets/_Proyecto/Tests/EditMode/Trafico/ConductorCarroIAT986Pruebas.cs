using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using MechaCyberpunk.Vehiculos;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-986: el conductor IA de las patrullas (lógica pura): recta acelera, el volante sigue el lado del objetivo, frena al llegar y en las curvas cerradas,
    /// y da reversa con el volante contrario si se atora. Usa los números del carro del jugador y los de ConfigPersecucion_Trafico.
    /// </summary>
    public class ConductorCarroIAT986Pruebas
    {
        private const string RutaConfigCarro = "Assets/_Proyecto/Vehiculos/Datos/ConfigCarro_Viejo.asset";
        private const string RutaConfigPersecucion = "Assets/_Proyecto/Trafico/Datos/ConfigPersecucion_Trafico.asset";
        private const float Paso = 0.02f;
        private const float Tolerancia = 0.001f;
        private const float VelocidadDeCrucero = 12f;

        private ConductorCarroIA conductor;
        private ParametrosConductorIA parametros;
        private ParametrosManejo carro;

        [SetUp]
        public void Preparar()
        {
            carro = AssetDatabase.LoadAssetAtPath<ConfigCarro>(RutaConfigCarro).Parametros;
            parametros = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfigPersecucion).ParametrosConductor;
            conductor = new ConductorCarroIA(carro, parametros);
        }

        private EntradaCarro Pedir(Vector3 objetivo, float velocidadDeseada, float rapidezAdelante)
        {
            return conductor.Calcular(Vector3.zero, Vector3.forward, Vector3.forward * rapidezAdelante, objetivo, velocidadDeseada, Paso);
        }

        [Test]
        public void Recta_SinVelocidad_Acelera_YNoGira()
        {
            EntradaCarro e = Pedir(new Vector3(0f, 0f, 30f), VelocidadDeCrucero, 0f);
            Assert.Greater(e.Acelerar, 0.9f, "Arranca a fondo");
            Assert.AreEqual(0f, e.Frenar, Tolerancia);
            Assert.AreEqual(0f, e.Girar, Tolerancia, "Objetivo de frente: volante derecho");
            Assert.AreEqual(0f, e.FrenoMano, Tolerancia);
        }

        [Test]
        public void ObjetivoALaDerecha_VolanteConSignoPositivo_YALaIzquierdaNegativo()
        {
            EntradaCarro derecha = Pedir(new Vector3(6f, 0f, 12f), VelocidadDeCrucero, 8f);
            EntradaCarro izquierda = Pedir(new Vector3(-6f, 0f, 12f), VelocidadDeCrucero, 8f);
            Assert.Greater(derecha.Girar, 0.1f, "A la derecha: volante positivo");
            Assert.Less(izquierda.Girar, -0.1f, "A la izquierda: volante negativo");
            Assert.AreEqual(derecha.Girar, -izquierda.Girar, Tolerancia, "Simétrico");
        }

        [Test]
        public void Volante_SeNormalizaAlAnguloMaximoDeLasRuedas_YNuncaPasaDeUno()
        {
            EntradaCarro cerrado = Pedir(new Vector3(3f, 0f, 0.5f), 6f, 2f);
            Assert.AreEqual(1f, cerrado.Girar, Tolerancia, "Objetivo casi al costado: volante a tope");
            EntradaCarro atras = Pedir(new Vector3(0.5f, 0f, -20f), 6f, 2f);
            Assert.LessOrEqual(Mathf.Abs(atras.Girar), 1f);
        }

        [Test]
        public void VelocidadDeseadaCero_Frena_YDetenidaSeQuedaConFrenoDeMano()
        {
            EntradaCarro enMarcha = Pedir(new Vector3(0f, 0f, 10f), 0f, 10f);
            Assert.AreEqual(1f, enMarcha.Frenar, Tolerancia, "Frena a fondo");
            Assert.AreEqual(0f, enMarcha.Acelerar, Tolerancia);
            EntradaCarro parada = Pedir(new Vector3(0f, 0f, 10f), 0f, 0.1f);
            Assert.AreEqual(0f, parada.Frenar, Tolerancia, "Parada no pisa el freno (daría reversa)");
            Assert.AreEqual(1f, parada.FrenoMano, Tolerancia, "Parada se queda con el freno de mano");
        }

        [Test]
        public void VelocidadDeseadaMuyBajaConMuchaVelocidad_FrenaFuerte_YPocaDiferencia_FrenaSuave()
        {
            Assert.AreEqual(1f, Pedir(new Vector3(0f, 0f, 30f), 4f, 14f).Frenar, Tolerancia, "Sobran 10 m/s: a fondo");
            EntradaCarro suave = Pedir(new Vector3(0f, 0f, 30f), 11.8f, 12f);
            Assert.AreEqual(0f, suave.Frenar, Tolerancia, "Sobra muy poco (dentro de la zona muerta): sin freno");
            EntradaCarro medio = Pedir(new Vector3(0f, 0f, 30f), 10.5f, 12f);
            Assert.That(medio.Frenar, Is.GreaterThan(0.1f).And.LessThan(1f), "Sobran 1.5 m/s: freno proporcional");
        }

        [Test]
        public void CurvaCerrada_PideMenosVelocidadQueUnaRecta_YBajaConElAngulo()
        {
            float recta = ConductorCarroIA.VelocidadEnCurva(parametros, VelocidadDeCrucero, 0f);
            float esquina = ConductorCarroIA.VelocidadEnCurva(parametros, VelocidadDeCrucero, 90f);
            float vuelta = ConductorCarroIA.VelocidadEnCurva(parametros, VelocidadDeCrucero, 180f);
            Assert.AreEqual(VelocidadDeCrucero, recta, Tolerancia, "Recta: la velocidad de crucero");
            Assert.Less(esquina, recta, "Una esquina de 90 grados: menos velocidad");
            Assert.Less(vuelta, esquina, "Una vuelta en U: todavía menos");
            Assert.AreEqual(parametros.VelocidadMinimaCurva, vuelta, Tolerancia, "Nunca baja de la mínima");
            Assert.AreEqual(ConductorCarroIA.VelocidadEnCurva(parametros, VelocidadDeCrucero, -90f), esquina, Tolerancia, "El signo no importa");
        }

        [Test]
        public void ObjetivoQueExigeUnaCurvaMuyCerrada_FrenaAunqueLaVelocidadDeseadaSeaAlta()
        {
            // Objetivo a 5 m casi al costado a 12 m/s: pedir esa curva con el agarre que hay exige bajar mucho la velocidad.
            EntradaCarro e = Pedir(new Vector3(4.5f, 0f, 2f), VelocidadDeCrucero, 12f);
            Assert.Greater(e.Frenar, 0.5f, "Frena antes de meterse en la curva");
            Assert.AreEqual(0f, e.Acelerar, Tolerancia);
        }

        [Test]
        public void VelocidadParaFrenar_DaLaVelocidadDesdeLaQuePuedeParar()
        {
            Assert.AreEqual(0f, ConductorCarroIA.VelocidadParaFrenar(0f, 0f, 5f), Tolerancia);
            Assert.AreEqual(10f, ConductorCarroIA.VelocidadParaFrenar(10f, 0f, 5f), 0.01f, "v = raiz(2 a d)");
            Assert.Greater(ConductorCarroIA.VelocidadParaFrenar(10f, 4f, 5f), 10f, "Con velocidad final, puede ir más rápido");
        }

        [Test]
        public void DistanciaDeMira_CreceConLaRapidez()
        {
            float parada = ConductorCarroIA.DistanciaMira(parametros, 0f);
            float rapido = ConductorCarroIA.DistanciaMira(parametros, 12f);
            Assert.AreEqual(parametros.DistanciaMiraMinima, parada, Tolerancia);
            Assert.Greater(rapido, parada + 3f);
        }

        [Test]
        public void SiQuiereMoverseYLlevaSegundosQuieto_DaReversaConElVolanteContrario()
        {
            Vector3 objetivo = new Vector3(5f, 0f, 10f); // adelante a la derecha: de frente, el volante sería positivo
            Assert.Greater(Pedir(objetivo, 8f, 0f).Girar, 0f);
            conductor.Reiniciar();
            EntradaCarro e = default;
            float t = 0f;
            while (t < parametros.SegundosParaAtoro + 0.2f)
            {
                e = Pedir(objetivo, 8f, 0f);
                t += Paso;
            }
            Assert.IsTrue(conductor.EnReversa, "Tras el tiempo de atoro da reversa");
            Assert.AreEqual(1, conductor.Reversas);
            Assert.AreEqual(1f, e.Frenar, Tolerancia, "Frenar casi parado es reversa");
            Assert.AreEqual(0f, e.Acelerar, Tolerancia);
            Assert.Less(e.Girar, 0f, "Volante contrario al de ir de frente");
        }

        [Test]
        public void LaReversaDuraUnRato_Y_DespuesVuelveAIntentarHaciaAdelante()
        {
            Vector3 objetivo = new Vector3(0f, 0f, 10f);
            for (float t = 0f; t < parametros.SegundosParaAtoro + 0.1f; t += Paso)
            {
                Pedir(objetivo, 8f, 0f);
            }
            Assert.IsTrue(conductor.EnReversa);
            for (float t = 0f; t < parametros.SegundosReversa + 0.1f; t += Paso)
            {
                Pedir(objetivo, 8f, -1f);
            }
            Assert.IsFalse(conductor.EnReversa, "Terminó la reversa");
            EntradaCarro e = Pedir(objetivo, 8f, 0f);
            Assert.Greater(e.Acelerar, 0.5f, "Vuelve a intentar hacia adelante");
        }

        [Test]
        public void SiNoQuiereMoverse_NoSeConsideraAtorada()
        {
            for (float t = 0f; t < parametros.SegundosParaAtoro * 3f; t += Paso)
            {
                Pedir(new Vector3(0f, 0f, 10f), 0f, 0f);
            }
            Assert.IsFalse(conductor.EnReversa, "Detenida a propósito no da reversa");
            Assert.AreEqual(0, conductor.Reversas);
        }

        [Test]
        public void FrenoDeMano_SoloEnGirosMuyCerradosAVelocidad()
        {
            Assert.AreEqual(0f, Pedir(new Vector3(0f, 0f, 20f), 12f, 12f).FrenoMano, Tolerancia, "Recta: sin freno de mano");
            Assert.AreEqual(0f, Pedir(new Vector3(8f, 0f, 8f), 12f, 4f).FrenoMano, Tolerancia, "Giro cerrado pero despacio: sin freno de mano");
            Assert.AreEqual(1f, Pedir(new Vector3(8f, 0f, -2f), 12f, 12f).FrenoMano, Tolerancia, "Giro muy cerrado a velocidad: freno de mano");
        }

        [Test]
        public void Config_DaLosNumerosDelConductor_ConValoresPosibles()
        {
            Assert.Greater(parametros.DesaceleracionPlaneada, 0f);
            Assert.Less(parametros.DesaceleracionPlaneada, carro.FuerzaFreno / carro.Masa, "Planea frenar con menos de lo que el freno del carro da");
            Assert.Greater(parametros.VelocidadCurva90, parametros.VelocidadMinimaCurva);
            Assert.Less(parametros.VelocidadCurva90, carro.VelocidadMaxima);
            ConfigPersecucion config = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfigPersecucion);
            Assert.Greater(config.RadioLlegadaNodo, 0f);
            Assert.Greater(config.RadioLlegadaMiga, 0f);
        }

        [Test]
        public void CalculoPersecucion_VelocidadDeseada_FrenaParaQuedarseALaDistanciaDeSeguimiento()
        {
            ConfigPersecucion config = AssetDatabase.LoadAssetAtPath<ConfigPersecucion>(RutaConfigPersecucion);
            CalculoPersecucion calculo = config.Crear();
            float a = config.DesaceleracionPlaneada;
            Assert.AreEqual(config.VelocidadPatrulla, calculo.VelocidadDeseada(false, 500f, a), Tolerancia, "Lejos: su velocidad");
            Assert.AreEqual(0f, calculo.VelocidadDeseada(false, 3f, a), Tolerancia, "Ya a la distancia de seguimiento: parada");
            float cerca = calculo.VelocidadDeseada(false, 12f, a);
            Assert.That(cerca, Is.GreaterThan(0f).And.LessThan(config.VelocidadPatrulla), "Cerca: baja la velocidad");
            Assert.Less(calculo.VelocidadDeseada(false, 9f, a), cerca, "Más cerca, más lenta");
        }
    }
}
