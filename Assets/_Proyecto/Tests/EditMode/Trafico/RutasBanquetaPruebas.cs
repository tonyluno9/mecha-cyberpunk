using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>Rutas de los peatones por las banquetas (T-331): contorno de la manzana, muestreo de 60 s dentro de la banqueta y números del config.</summary>
    public class RutasBanquetaPruebas
    {
        private const string RutaConfig = "Assets/_Proyecto/Trafico/Datos/ConfigPeatones_Trafico.asset";
        private const float MitadManzana = 40f;
        private const float AnchoBanqueta = 3f;
        private const float AlturaBanqueta = 0.2f;
        private const float SegundosDeMuestreo = 60f;
        private const float PasoMuestreo = 0.05f;
        private const float Tolerancia = 0.001f;
        private const int MaximoDePeatones = 30;
        private const int MinimoDePeatones = 12;

        private static readonly Vector3 CentroManzana = new Vector3(-94f, 0f, 47f);

        private static ConfigPeatones Config()
        {
            ConfigPeatones config = AssetDatabase.LoadAssetAtPath<ConfigPeatones>(RutaConfig);
            Assert.IsNotNull(config, "Falta ConfigPeatones_Trafico.asset");
            return config;
        }

        [Test]
        public void Contorno_DaLaVueltaALaManzana_YCierra()
        {
            float lado = MitadManzana - Config().MargenBanqueta;
            Vector3[] puntos = RutasBanqueta.Contorno(CentroManzana, lado, AlturaBanqueta);
            Assert.AreEqual(5, puntos.Length);
            Assert.AreEqual(puntos[0], puntos[4]);
            foreach (float largo in RutasBanqueta.Largos(puntos))
            {
                Assert.AreEqual(2f * lado, largo, Tolerancia);
            }
            foreach (Vector3 punto in puntos)
            {
                Assert.AreEqual(AlturaBanqueta, punto.y, Tolerancia);
            }
        }

        [Test]
        public void EnSesentaSegundos_ElPeatonNuncaSaleDeLaBanqueta()
        {
            ConfigPeatones config = Config();
            Vector3[] puntos = RutasBanqueta.Contorno(CentroManzana, MitadManzana - config.MargenBanqueta, AlturaBanqueta);
            float interior = MitadManzana - AnchoBanqueta;
            for (int semilla = 0; semilla < MaximoDePeatones; semilla++)
            {
                CalculoPeaton calculo = config.Crear(RutasBanqueta.Largos(puntos), new System.Random(semilla));
                for (float t = 0f; t < SegundosDeMuestreo; t += PasoMuestreo)
                {
                    calculo.Avanzar(PasoMuestreo);
                    Vector3 local = RutasBanqueta.Posicion(puntos, calculo) - CentroManzana;
                    float distancia = Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z));
                    Assert.GreaterOrEqual(distancia, interior - Tolerancia, "Entró al edificio (semilla " + semilla + ", t=" + t + ")");
                    Assert.LessOrEqual(distancia, MitadManzana + Tolerancia, "Salió a la calle (semilla " + semilla + ", t=" + t + ")");
                }
            }
        }

        [Test]
        public void Config_TieneMasDeDiaQueDeNoche_YCabeEnElTope()
        {
            ConfigPeatones config = Config();
            // T-447: de día hay más gente en las banquetas; de noche salen los pandilleros.
            Assert.Greater(config.Cantidad(false), config.Cantidad(true));
            Assert.GreaterOrEqual(config.Cantidad(false), MinimoDePeatones);
            Assert.LessOrEqual(config.Cantidad(false), MaximoDePeatones);
            Assert.AreEqual(config.Cantidad(false), config.Maximo);
            Assert.AreEqual(2f, config.DistanciaParada, Tolerancia);
        }
    }
}
