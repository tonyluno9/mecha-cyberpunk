using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Prologo;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>MaquinaPrologo (T-132): la secuencia de etapas y objetivos de la noche, avisos fuera de orden y reanudar. Sin Unity.</summary>
    public class MaquinaPrologoPruebas
    {
        private const string Carro = "Ve a tu carro";
        private const string Regreso = "Regresa a casa en tu carro";
        private const string Cama = "Duerme en tu cama";
        private const string RutaDatos = "Assets/_Proyecto/Progresion/Prologo/Datos/DatosPrologo_Base.asset";
        private const string Clandestino = "Ve al clandestino: tu primera pelea casera";
        private const string Mostrador = "Habla con el encargado (E)";
        private const int PasosDeRuta = 2;

        private MaquinaPrologo Nueva(EtapaPrologo etapa, int pasos = PasosDeRuta, bool reanudar = false)
        {
            MaquinaPrologo maquina = new MaquinaPrologo(Carro, Regreso, Cama, pasos, Clandestino, Mostrador);
            maquina.Iniciar(etapa, reanudar);
            return maquina;
        }

        [Test]
        public void Secuencia_DeLaNoche_PasaPorTodasLasEtapasConSusObjetivos()
        {
            MaquinaPrologo maquina = Nueva(EtapaPrologo.PeleaTutorial);
            Assert.IsTrue(maquina.Activa);
            Assert.AreEqual(DestinoObjetivo.Ninguno, maquina.ObjetivoActual.Destino, "En la pelea no hay objetivo del director");
            Assert.IsFalse(maquina.EmboscadaHabilitada, "En la pelea la emboscada está apagada");

            Assert.IsTrue(maquina.Avisar(AvisoPrologo.TutorialTerminado));
            Assert.AreEqual(EtapaPrologo.SalidaClandestino, maquina.Etapa);
            Assert.AreEqual(Carro, maquina.ObjetivoActual.Texto);
            Assert.AreEqual(DestinoObjetivo.Carro, maquina.ObjetivoActual.Destino);
            Assert.IsTrue(maquina.EmboscadaHabilitada, "Saliendo del clandestino la emboscada está prendida");

            Assert.IsTrue(maquina.Avisar(AvisoPrologo.EmboscadaEmpezo));
            Assert.AreEqual(EtapaPrologo.Emboscada, maquina.Etapa);
            Assert.AreEqual(DestinoObjetivo.Ninguno, maquina.ObjetivoActual.Destino, "En la emboscada los objetivos los pone ella");
            Assert.IsFalse(maquina.EmboscadaHabilitada);

            Assert.IsTrue(maquina.Avisar(AvisoPrologo.EmboscadaTerminada));
            Assert.AreEqual(EtapaPrologo.RegresoACasa, maquina.Etapa);
            Assert.AreEqual(Regreso, maquina.ObjetivoActual.Texto);
            Assert.AreEqual(DestinoObjetivo.PasoRuta, maquina.ObjetivoActual.Destino);
            Assert.AreEqual(0, maquina.ObjetivoActual.IndicePaso);

            Assert.IsTrue(maquina.Avisar(AvisoPrologo.LlegoAPaso));
            Assert.AreEqual(1, maquina.ObjetivoActual.IndicePaso, "Al llegar, el siguiente paso");
            Assert.IsTrue(maquina.Avisar(AvisoPrologo.LlegoAPaso));
            Assert.AreEqual(DestinoObjetivo.Casa, maquina.ObjetivoActual.Destino, "Después de las esquinas, la casa");
            Assert.AreEqual(Regreso, maquina.ObjetivoActual.Texto);

            Assert.IsTrue(maquina.Avisar(AvisoPrologo.EntroACasa));
            Assert.AreEqual(EtapaPrologo.Dormir, maquina.Etapa);
            Assert.AreEqual(Cama, maquina.ObjetivoActual.Texto);
            Assert.AreEqual(DestinoObjetivo.Cama, maquina.ObjetivoActual.Destino);
        }

        [Test]
        public void Avisos_FueraDeOrden_SeIgnoran()
        {
            MaquinaPrologo maquina = Nueva(EtapaPrologo.PeleaTutorial);
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.EmboscadaEmpezo), "No hay emboscada antes de salir");
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.EmboscadaTerminada));
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.LlegoAPaso));
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.EntroACasa), "Entrar a la casa en la pelea no salta a dormir");
            Assert.AreEqual(EtapaPrologo.PeleaTutorial, maquina.Etapa);

            maquina.Avisar(AvisoPrologo.TutorialTerminado);
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.TutorialTerminado), "Un aviso repetido no hace nada");
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.EmboscadaTerminada), "No termina una emboscada que no empezó");
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.EntroACasa), "Entrar a la casa antes de la emboscada no cuenta");
            Assert.AreEqual(EtapaPrologo.SalidaClandestino, maquina.Etapa);

            maquina.Avisar(AvisoPrologo.EmboscadaEmpezo);
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.EmboscadaEmpezo));
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.LlegoAPaso), "No hay ruta de regreso en la emboscada");
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.EntroACasa));
            Assert.AreEqual(EtapaPrologo.Emboscada, maquina.Etapa);
        }

        [Test]
        public void Casa_EntrarDesdeCualquierPasoDeLaRuta_LlevaADormir()
        {
            MaquinaPrologo maquina = Nueva(EtapaPrologo.RegresoACasa);
            Assert.AreEqual(0, maquina.IndicePaso);
            Assert.IsTrue(maquina.Avisar(AvisoPrologo.EntroACasa), "Entrar a la casa sin pasar por las esquinas también cuenta");
            Assert.AreEqual(EtapaPrologo.Dormir, maquina.Etapa);
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.EntroACasa), "Una vez dormir, volver a entrar no hace nada");
        }

        [Test]
        public void Ruta_EnElLugarFinal_LlegoAPasoSeIgnora()
        {
            MaquinaPrologo maquina = Nueva(EtapaPrologo.RegresoACasa, 1);
            Assert.IsTrue(maquina.Avisar(AvisoPrologo.LlegoAPaso));
            Assert.AreEqual(DestinoObjetivo.Casa, maquina.ObjetivoActual.Destino);
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.LlegoAPaso), "En la casa se llega entrando, no con un paso");
            Assert.AreEqual(DestinoObjetivo.Casa, maquina.ObjetivoActual.Destino);
        }

        [Test]
        public void Ruta_SinPasos_VaDirectoALaCasa()
        {
            MaquinaPrologo maquina = Nueva(EtapaPrologo.RegresoACasa, 0);
            Assert.AreEqual(DestinoObjetivo.Casa, maquina.ObjetivoActual.Destino);
            Assert.AreEqual(Regreso, maquina.ObjetivoActual.Texto);
        }

        [Test]
        public void Reanudar_SalidaClandestino_SeComportaComoSalida()
        {
            MaquinaPrologo maquina = Nueva(EtapaPrologo.SalidaClandestino, PasosDeRuta, true);
            Assert.AreEqual(EtapaPrologo.SalidaClandestino, maquina.Etapa);
            Assert.AreEqual(Carro, maquina.ObjetivoActual.Texto);
            Assert.IsTrue(maquina.EmboscadaHabilitada);
        }

        [Test]
        public void Reanudar_Emboscada_RepiteLaSalidaSinRetrocederLaEtapaGuardada()
        {
            MaquinaPrologo maquina = Nueva(EtapaPrologo.Emboscada, PasosDeRuta, true);
            Assert.AreEqual(EtapaPrologo.SalidaClandestino, maquina.Etapa, "La máquina repite la salida: la emboscada se repite");
            Assert.IsTrue(maquina.EmboscadaHabilitada);
            Assert.AreEqual(Carro, maquina.ObjetivoActual.Texto);
            Assert.AreEqual(EtapaPrologo.SalidaClandestino, MaquinaPrologo.EtapaAlReanudar(EtapaPrologo.Emboscada));
            Assert.IsTrue(maquina.Avisar(AvisoPrologo.EmboscadaEmpezo), "Y vuelve a emboscarse");
        }

        [Test]
        public void Reanudar_RegresoYDormir_SeQuedanEnSuEtapa()
        {
            MaquinaPrologo regreso = Nueva(EtapaPrologo.RegresoACasa, PasosDeRuta, true);
            Assert.AreEqual(EtapaPrologo.RegresoACasa, regreso.Etapa);
            Assert.AreEqual(0, regreso.ObjetivoActual.IndicePaso, "Reanuda desde el primer paso");
            MaquinaPrologo dormir = Nueva(EtapaPrologo.Dormir, PasosDeRuta, true);
            Assert.AreEqual(EtapaPrologo.Dormir, dormir.Etapa);
            Assert.AreEqual(Cama, dormir.ObjetivoActual.Texto);
            MaquinaPrologo tutorial = Nueva(EtapaPrologo.PeleaTutorial, PasosDeRuta, true);
            Assert.AreEqual(EtapaPrologo.PeleaTutorial, tutorial.Etapa, "La pelea del tutorial se repite");
        }

        [Test]
        public void EtapasFueraDeLaNoche_LaMaquinaEstaInactivaYNoPideNada()
        {
            foreach (EtapaPrologo etapa in new[] { EtapaPrologo.Ninguna, EtapaPrologo.CreacionPersonaje, EtapaPrologo.Flashback,
                EtapaPrologo.Libre })
            {
                MaquinaPrologo maquina = Nueva(etapa, PasosDeRuta, true);
                Assert.IsFalse(maquina.Activa, etapa + " no la lleva la máquina");
                Assert.IsFalse(maquina.EmboscadaHabilitada, etapa + ": la emboscada queda apagada");
                Assert.AreEqual(DestinoObjetivo.Ninguno, maquina.ObjetivoActual.Destino, etapa + ": sin objetivo");
                foreach (AvisoPrologo aviso in new[] { AvisoPrologo.TutorialTerminado, AvisoPrologo.EmboscadaEmpezo,
                    AvisoPrologo.EmboscadaTerminada, AvisoPrologo.LlegoAPaso, AvisoPrologo.EntroACasa, AvisoPrologo.Despertado,
                    AvisoPrologo.EntroAlClandestino, AvisoPrologo.PeleaCaseraTerminada, AvisoPrologo.SeguirJugando })
                {
                    Assert.IsFalse(maquina.Avisar(aviso), etapa + " ignora " + aviso);
                }
                Assert.AreEqual(etapa, maquina.Etapa);
            }
        }

        [Test]
        public void Presente_Secuencia_DespertarPeleaCaseraContinuaraLibre()
        {
            MaquinaPrologo maquina = Nueva(EtapaPrologo.Despertar);
            Assert.IsTrue(maquina.Activa, "El despertar lo lleva la máquina");
            Assert.IsFalse(maquina.EmboscadaHabilitada, "En el presente no hay emboscada");
            Assert.AreEqual(DestinoObjetivo.Ninguno, maquina.ObjetivoActual.Destino);

            Assert.IsTrue(maquina.Avisar(AvisoPrologo.Despertado));
            Assert.AreEqual(EtapaPrologo.PeleaCasera, maquina.Etapa);
            Assert.AreEqual(DestinoObjetivo.Clandestino, maquina.ObjetivoActual.Destino);
            Assert.AreEqual(Clandestino, maquina.ObjetivoActual.Texto);
            Assert.IsFalse(maquina.EmboscadaHabilitada);

            Assert.IsTrue(maquina.Avisar(AvisoPrologo.EntroAlClandestino));
            Assert.IsTrue(maquina.EnClandestino);
            Assert.AreEqual(DestinoObjetivo.Mostrador, maquina.ObjetivoActual.Destino);
            Assert.AreEqual(Mostrador, maquina.ObjetivoActual.Texto);
            Assert.IsFalse(maquina.Avisar(AvisoPrologo.EntroAlClandestino), "Entrar otra vez no cambia nada");

            Assert.IsTrue(maquina.Avisar(AvisoPrologo.PeleaCaseraTerminada));
            Assert.AreEqual(EtapaPrologo.Continuara, maquina.Etapa);
            Assert.IsTrue(maquina.Activa);
            Assert.AreEqual(DestinoObjetivo.Ninguno, maquina.ObjetivoActual.Destino, "En continuará no hay objetivo");

            Assert.IsTrue(maquina.Avisar(AvisoPrologo.SeguirJugando));
            Assert.AreEqual(EtapaPrologo.Libre, maquina.Etapa);
            Assert.IsFalse(maquina.Activa, "Libre: el director ya no hace nada");
            Assert.AreEqual(DestinoObjetivo.Ninguno, maquina.ObjetivoActual.Destino);
        }

        [Test]
        public void Presente_AvisosFueraDeOrden_SeIgnoran()
        {
            MaquinaPrologo despertar = Nueva(EtapaPrologo.Despertar);
            Assert.IsFalse(despertar.Avisar(AvisoPrologo.EntroAlClandestino), "No se entra al clandestino antes de despertar");
            Assert.IsFalse(despertar.Avisar(AvisoPrologo.PeleaCaseraTerminada));
            Assert.IsFalse(despertar.Avisar(AvisoPrologo.SeguirJugando));
            Assert.AreEqual(EtapaPrologo.Despertar, despertar.Etapa);

            MaquinaPrologo casera = Nueva(EtapaPrologo.PeleaCasera);
            Assert.IsFalse(casera.Avisar(AvisoPrologo.Despertado), "Despertar solo vale en Despertar");
            Assert.IsFalse(casera.Avisar(AvisoPrologo.SeguirJugando), "Seguir jugando solo vale en continuará");
            Assert.IsFalse(casera.Avisar(AvisoPrologo.TutorialTerminado));
            Assert.IsFalse(casera.Avisar(AvisoPrologo.EntroACasa));
            Assert.AreEqual(EtapaPrologo.PeleaCasera, casera.Etapa);

            MaquinaPrologo continuara = Nueva(EtapaPrologo.Continuara);
            Assert.IsFalse(continuara.Avisar(AvisoPrologo.PeleaCaseraTerminada), "La pelea ya terminó");
            Assert.IsFalse(continuara.Avisar(AvisoPrologo.EntroAlClandestino));
            Assert.AreEqual(EtapaPrologo.Continuara, continuara.Etapa);
        }

        [Test]
        public void Presente_PerderLaCasera_TambienLlevaAContinuara()
        {
            MaquinaPrologo maquina = Nueva(EtapaPrologo.PeleaCasera);
            Assert.IsTrue(maquina.Avisar(AvisoPrologo.PeleaCaseraTerminada), "Ganar o perder, el aviso es el mismo");
            Assert.AreEqual(EtapaPrologo.Continuara, maquina.Etapa);
        }

        [Test]
        public void Presente_Reanudar_PeleaCaseraEmpiezaPorElClandestinoYContinuaraSeQuedaEnSuEtapa()
        {
            MaquinaPrologo casera = Nueva(EtapaPrologo.PeleaCasera, PasosDeRuta, true);
            Assert.AreEqual(EtapaPrologo.PeleaCasera, casera.Etapa);
            Assert.IsFalse(casera.EnClandestino);
            Assert.AreEqual(DestinoObjetivo.Clandestino, casera.ObjetivoActual.Destino, "Reanuda desde la cama: primero el clandestino");
            MaquinaPrologo continuara = Nueva(EtapaPrologo.Continuara, PasosDeRuta, true);
            Assert.AreEqual(EtapaPrologo.Continuara, continuara.Etapa);
            Assert.IsTrue(continuara.Activa);
            MaquinaPrologo libre = Nueva(EtapaPrologo.Libre, PasosDeRuta, true);
            Assert.IsFalse(libre.Activa, "Libre no hace nada");
        }

        [Test]
        public void Maneja_LaNocheYElPresente_PeroNoLibreNiLoDeAntes()
        {
            foreach (EtapaPrologo etapa in new[] { EtapaPrologo.PeleaTutorial, EtapaPrologo.Dormir, EtapaPrologo.Despertar,
                EtapaPrologo.PeleaCasera, EtapaPrologo.Continuara })
            {
                Assert.IsTrue(MaquinaPrologo.Maneja(etapa), etapa + " la lleva el director");
            }
            foreach (EtapaPrologo etapa in new[] { EtapaPrologo.Ninguna, EtapaPrologo.CreacionPersonaje, EtapaPrologo.Flashback,
                EtapaPrologo.Libre })
            {
                Assert.IsFalse(MaquinaPrologo.Maneja(etapa), etapa + " no la lleva el director");
            }
            Assert.IsTrue(MaquinaPrologo.EsDelPresente(EtapaPrologo.Despertar));
            Assert.IsFalse(MaquinaPrologo.EsDelPresente(EtapaPrologo.Dormir));
            Assert.IsFalse(MaquinaPrologo.EsDelPresente(EtapaPrologo.Libre));
        }

        [Test]
        public void EsDeLaNoche_SoloDePeleaTutorialADormir()
        {
            Assert.IsTrue(MaquinaPrologo.EsDeLaNoche(EtapaPrologo.PeleaTutorial));
            Assert.IsTrue(MaquinaPrologo.EsDeLaNoche(EtapaPrologo.Dormir));
            Assert.IsFalse(MaquinaPrologo.EsDeLaNoche(EtapaPrologo.CreacionPersonaje));
            Assert.IsFalse(MaquinaPrologo.EsDeLaNoche(EtapaPrologo.Flashback));
        }

        [Test]
        public void DatosPrologoBase_TieneLosValoresDeLaNoche()
        {
            DatosPrologo datos = AssetDatabase.LoadAssetAtPath<DatosPrologo>(RutaDatos);
            Assert.IsNotNull(datos, "Existe DatosPrologo_Base");
            Assert.AreEqual(22f, datos.HoraNoche);
            Assert.AreEqual(Carro, datos.TextoIrAlCarro);
            Assert.AreEqual(Regreso, datos.TextoRegresarACasa);
            Assert.AreEqual(Cama, datos.TextoDormir);
            Assert.AreEqual("Prologo_Carro", datos.IdPuntoCarro);
            CollectionAssert.AreEqual(new[] { "Prologo_Esquina_1" }, datos.PasosRuta);
            Assert.AreEqual(IdLugar.Casa, datos.LugarFinal);
            Assert.AreEqual(20f, datos.HoraDespertar);
            Assert.AreEqual(0.45f, datos.FraccionVidaDespertar);
            Assert.AreEqual("Tu mecha quedó fregado. Puedes repararlo en el banco del taller antes de pelear.", datos.TextoAvisoDespertar);
            Assert.AreEqual(6f, datos.SegundosAvisoDespertar);
            Assert.AreEqual(Clandestino, datos.TextoIrAlClandestino);
            Assert.AreEqual(Mostrador, datos.TextoHablarEncargado);
            Assert.AreEqual(IdLugar.Clandestino, datos.LugarClandestino);
        }
    }
}
