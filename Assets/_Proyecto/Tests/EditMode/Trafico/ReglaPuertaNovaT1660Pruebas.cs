using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Persecucion;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests
{
    /// <summary>T-1660 (T42): Nova solo llega con 10 policías muertos y las camionetas llegadas; orden de la escalada; patrullas que no se chocan.</summary>
    public class ReglaPuertaNovaT1660Pruebas
    {
        private static ReglaEscaladaRespuesta Crear()
        {
            ParametrosRespuesta numeros = ParametrosRespuesta.PorOmision();
            numeros.Registrar = false;
            return new ReglaEscaladaRespuesta(numeros, new System.Random(1));
        }

        private static int[] Vivos() => new int[9];

        private static void MatarPolicias(ReglaEscaladaRespuesta r, int cuantos, float t)
        {
            for (int i = 0; i < cuantos; i++)
            {
                r.RegistrarPerdida(TipoRespuesta.PoliciaAPie, t);
            }
        }

        [Test]
        public void Puerta_ExigeLosDosRequisitos()
        {
            Assert.IsFalse(ReglaPuertaNova.Abierta(9, 10, 1, 1));
            Assert.IsFalse(ReglaPuertaNova.Abierta(10, 10, 0, 1));
            Assert.IsTrue(ReglaPuertaNova.Abierta(10, 10, 1, 1));
            Assert.IsTrue(ReglaPuertaNova.Abierta(15, 10, 2, 1));
        }

        [Test]
        public void Puerta_SinModeloDeCamioneta_NoEsperaCamionetas()
        {
            Assert.AreEqual(0, ReglaPuertaNova.CamionetasRequeridas(false, 1));
            Assert.AreEqual(1, ReglaPuertaNova.CamionetasRequeridas(true, 1));
            Assert.IsTrue(ReglaPuertaNova.Abierta(10, 10, 0, ReglaPuertaNova.CamionetasRequeridas(false, 1)));
        }

        [Test]
        public void SinPolicias_Muertos_CincoEstrellasNoTraeNova_NiPasadoMuchoTiempo()
        {
            ReglaEscaladaRespuesta r = Crear();
            r.CamionetasLlegadas = 1;
            r.Avanzar(0f, 5, false, false, Vivos());
            r.Avanzar(500f, 5, false, false, Vivos());
            Assert.IsFalse(r.PuertaNovaAbierta);
            Assert.IsFalse(r.ModoNova, "sin 10 policías muertos no hay modo Nova");
            Assert.LessOrEqual(r.NivelDesbloqueado, 3, "la escalada se queda en la policía");
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.NovaHumanoide, 1000f));
            Assert.IsTrue(r.PermiteMantener(TipoRespuesta.Patrulla), "la policía sigue llegando");
        }

        [Test]
        public void DiezMuertos_PeroSinCamionetas_NoHayNova()
        {
            ReglaEscaladaRespuesta r = Crear();
            r.Avanzar(0f, 5, false, false, Vivos());
            MatarPolicias(r, 10, 1f);
            r.Avanzar(2f, 5, false, false, Vivos());
            Assert.AreEqual(10, r.PoliciasMuertos);
            Assert.IsFalse(r.PuertaNovaAbierta, "faltan las camionetas");
            Assert.IsFalse(r.ModoNova);
        }

        [Test]
        public void DiezMuertos_YCamionetasLlegadas_AbreNova_ConLosHumanoidesPrimero()
        {
            ReglaEscaladaRespuesta r = Crear();
            r.Avanzar(0f, 5, false, false, Vivos());
            MatarPolicias(r, 10, 1f);
            r.CamionetasLlegadas = 1;
            r.Avanzar(2f, 5, false, false, Vivos());
            Assert.IsTrue(r.PuertaNovaAbierta);
            Assert.IsTrue(r.ModoNova);
            Assert.AreEqual(ReglaEscaladaRespuesta.PrimerNivelNova, r.NivelDesbloqueado, "empieza por los humanoides de Nova");
            Assert.AreEqual(4, ReglaEscaladaRespuesta.NivelDe(TipoRespuesta.NovaHumanoide));
            Assert.Less(ReglaEscaladaRespuesta.NivelDe(TipoRespuesta.NovaHumanoide), ReglaEscaladaRespuesta.NivelDe(TipoRespuesta.RobotMediano), "luego los de armadura");
            Assert.Less(ReglaEscaladaRespuesta.NivelDe(TipoRespuesta.RobotMediano), ReglaEscaladaRespuesta.NivelDe(TipoRespuesta.Perro));
            Assert.Less(ReglaEscaladaRespuesta.NivelDe(TipoRespuesta.Perro), ReglaEscaladaRespuesta.NivelDe(TipoRespuesta.RobotGrande));
            Assert.IsFalse(r.PuedeGenerar(TipoRespuesta.RobotMediano, 1000f), "los de armadura esperan a los humanoides");
        }

        [Test]
        public void SinModeloDeCamioneta_LaPuertaSoloPideLosPolicias()
        {
            ReglaEscaladaRespuesta r = Crear();
            r.Declarar(TipoRespuesta.Camioneta, false);
            r.Avanzar(0f, 5, false, false, Vivos());
            MatarPolicias(r, 10, 1f);
            r.Avanzar(2f, 5, false, false, Vivos());
            Assert.AreEqual(0, r.CamionetasRequeridas);
            Assert.IsTrue(r.PuertaNovaAbierta);
        }

        [Test]
        public void AlPerderLasEstrellas_LaCuentaVuelveACero()
        {
            ReglaEscaladaRespuesta r = Crear();
            r.Avanzar(0f, 5, false, false, Vivos());
            MatarPolicias(r, 10, 1f);
            r.CamionetasLlegadas = 1;
            r.Avanzar(2f, 5, false, false, Vivos());
            Assert.IsTrue(r.PuertaNovaAbierta);
            r.Avanzar(3f, 0, false, false, Vivos());
            Assert.AreEqual(0, r.PoliciasMuertos);
            Assert.AreEqual(0, r.CamionetasLlegadas);
            Assert.IsFalse(r.PuertaNovaAbierta);
            r.Avanzar(4f, 5, false, false, Vivos());
            Assert.IsFalse(r.ModoNova, "una búsqueda nueva empieza de cero");
        }

        [Test]
        public void MisionProtegidaConNova_NoEsperaLaPuerta()
        {
            ReglaEscaladaRespuesta r = Crear();
            r.Avanzar(0f, 4, true, true, Vivos());
            Assert.IsTrue(r.PuertaNovaAbierta);
        }

        [Test]
        public void NombreFase_SigueElOrden()
        {
            Assert.AreEqual("cerrada (solo policia)", ReglaPuertaNova.NombreFase(3, false));
            StringAssert.Contains("E15", ReglaPuertaNova.NombreFase(4, true));
            StringAssert.Contains("E18", ReglaPuertaNova.NombreFase(5, true));
            StringAssert.Contains("E16", ReglaPuertaNova.NombreFase(7, true));
            StringAssert.Contains("E14", ReglaPuertaNova.NombreFase(8, true));
        }

        [Test]
        public void PuntoDeAcople_RepartePatrullasADistintosLados()
        {
            Vector3 jugador = new Vector3(0f, 0f, 50f);
            Vector3 a = CalculoSeparacionPatrullas.PuntoDeAcople(Vector3.zero, jugador, 0, 3, 5f, 14f);
            Vector3 b = CalculoSeparacionPatrullas.PuntoDeAcople(Vector3.zero, jugador, 1, 3, 5f, 14f);
            Vector3 c = CalculoSeparacionPatrullas.PuntoDeAcople(Vector3.zero, jugador, 2, 3, 5f, 14f);
            Assert.AreEqual(jugador, b, "la de en medio va al jugador");
            Assert.AreEqual(10f, Vector3.Distance(a, c), 0.01f, "las de los lados quedan separadas");
            Assert.AreEqual(jugador, CalculoSeparacionPatrullas.PuntoDeAcople(Vector3.zero, jugador, 0, 1, 5f, 14f), "una sola va directo");
            Assert.AreEqual(jugador, CalculoSeparacionPatrullas.PuntoDeAcople(new Vector3(0f, 0f, 45f), jugador, 0, 3, 5f, 14f), "ya cerca va directo");
        }

        [Test]
        public void Separar_EmpujaLejosDeLaVecinaCercana_YNoCambiaSinVecinas()
        {
            Vector3 adelante = Vector3.forward;
            Vector3 sinVecinas = CalculoSeparacionPatrullas.Separar(Vector3.zero, adelante, new Vector3[0], 9f, 1.6f);
            Assert.AreEqual(adelante, sinVecinas);
            Vector3 lejos = CalculoSeparacionPatrullas.Separar(Vector3.zero, adelante, new[] { new Vector3(30f, 0f, 0f) }, 9f, 1.6f);
            Assert.AreEqual(adelante, lejos);
            Vector3 cerca = CalculoSeparacionPatrullas.Separar(Vector3.zero, adelante, new[] { new Vector3(2f, 0f, 1f) }, 9f, 1.6f);
            Assert.Less(cerca.x, -0.1f, "se aparta hacia el lado contrario de la vecina");
            Assert.AreEqual(1f, cerca.magnitude, 0.001f);
        }
    }
}
