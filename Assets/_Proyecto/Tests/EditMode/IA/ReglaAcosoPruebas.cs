using System.Collections.Generic;
using MechaCyberpunk.IA.APie;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    public class ReglaAcosoPruebas
    {
        private const float GestoMin = 2.5f;
        private const float GestoMax = 5f;
        private const float Retraso = 1.6f;
        private const float RetrasoArriba = 2.6f;
        private const float Vista = 18f;
        private const float Angulo = 120f;
        private const float Inclinacion = 30f;
        private const float Oido = 4f;
        private const float Paso = 0.1f;

        // Azar con valores fijos (se repite el último).
        private static System.Func<float> Azar(params float[] valores)
        {
            Queue<float> cola = new Queue<float>(valores);
            float ultimo = valores[valores.Length - 1];
            return () => cola.Count > 0 ? (ultimo = cola.Dequeue()) : ultimo;
        }

        private static ReglaAcoso Crear(System.Func<float> azar, float empujon = 0.5f, float caida = 0.35f)
            => new ReglaAcoso(GestoMin, GestoMax, empujon, caida, Retraso, RetrasoArriba, azar);

        private static ReglaAcoso.Salida Avanzar(ReglaAcoso r, float segundos, ReglaAcoso.Entrada entrada)
        {
            ReglaAcoso.Salida ultima = default;
            for (float t = 0f; t < segundos; t += Paso)
            {
                ReglaAcoso.Salida s = r.Avanzar(Paso, entrada);
                if (s.Gesto != ReglaAcoso.Gesto.Ninguno || s.EntroAlerta || s.EntroSorpresa || s.EntroVencidos)
                {
                    return s;
                }
            }
            return ultima;
        }

        [Test]
        public void Acosando_SinNovedad_HaceUnGestoEntreElMinimoYElMaximo()
        {
            // azar 0 = el periodo mínimo (2.5 s)
            ReglaAcoso r = Crear(Azar(0f, 0.9f, 0.9f));
            Assert.AreEqual(ReglaAcoso.Gesto.Ninguno, Avanzar(r, GestoMin - 0.3f, default).Gesto, "Antes del periodo mínimo no hay gesto");
            ReglaAcoso.Salida s = Avanzar(r, 1f, default);
            Assert.AreNotEqual(ReglaAcoso.Gesto.Ninguno, s.Gesto);
            Assert.AreEqual(ReglaAcoso.Fase.Acosando, r.Actual);
        }

        [Test]
        public void Gesto_ConAzarBajo_EsEmpujonYConAzarAltoEsBurla()
        {
            // el azar se pide en este orden: periodo del 1er gesto (ctor), periodo del siguiente, tipo de gesto, si tira
            ReglaAcoso empuja = Crear(Azar(0f, 0.5f, 0.1f, 0.1f), 0.5f, 0.35f);
            ReglaAcoso.Salida e = Avanzar(empuja, GestoMax + 1f, default);
            Assert.AreEqual(ReglaAcoso.Gesto.Empujon, e.Gesto);
            Assert.IsTrue(e.EmpujonTira, "Azar 0.1 menor que 0.35: tira al civil");

            ReglaAcoso burla = Crear(Azar(0f, 0.5f, 0.9f));
            ReglaAcoso.Salida b = Avanzar(burla, GestoMax + 1f, default);
            Assert.AreEqual(ReglaAcoso.Gesto.Burla, b.Gesto);
            Assert.IsFalse(b.EmpujonTira);
        }

        [Test]
        public void Empujon_ConAzarDeCaidaAlto_NoTira()
        {
            ReglaAcoso r = Crear(Azar(0f, 0.5f, 0.1f, 0.9f), 0.5f, 0.35f);
            ReglaAcoso.Salida s = Avanzar(r, GestoMax + 1f, default);
            Assert.AreEqual(ReglaAcoso.Gesto.Empujon, s.Gesto);
            Assert.IsFalse(s.EmpujonTira);
        }

        [Test]
        public void Acosando_JugadorDetectado_PasaAAlerta()
        {
            ReglaAcoso r = Crear(Azar(0.5f));
            ReglaAcoso.Salida s = r.Avanzar(Paso, new ReglaAcoso.Entrada { JugadorDetectado = true });
            Assert.IsTrue(s.EntroAlerta);
            Assert.AreEqual(ReglaAcoso.Fase.Alerta, r.Actual);
        }

        [Test]
        public void Acosando_DanioSinVer_PasaASorpresaConRetrasoNormal()
        {
            ReglaAcoso r = Crear(Azar(0.5f));
            ReglaAcoso.Salida s = r.Avanzar(Paso, new ReglaAcoso.Entrada { DanioRecibido = true });
            Assert.IsTrue(s.EntroSorpresa);
            Assert.AreEqual(ReglaAcoso.Fase.Sorpresa, r.Actual);
            Assert.AreEqual(Retraso, r.SorpresaRestante, 1e-4f);
        }

        [Test]
        public void Acosando_DanioDesdeArriba_TieneMasRetraso()
        {
            ReglaAcoso r = Crear(Azar(0.5f));
            r.Avanzar(Paso, new ReglaAcoso.Entrada { DanioRecibido = true, JugadorDesdeArriba = true });
            Assert.AreEqual(RetrasoArriba, r.SorpresaRestante, 1e-4f);
            Assert.Greater(RetrasoArriba, Retraso);
        }

        [Test]
        public void Sorpresa_NoAtacaDuranteElRetrasoYDespuesPasaAAlerta()
        {
            ReglaAcoso r = Crear(Azar(0.5f));
            r.Avanzar(Paso, new ReglaAcoso.Entrada { DanioRecibido = true });
            r.Avanzar(Retraso - 0.3f, default);
            Assert.AreEqual(ReglaAcoso.Fase.Sorpresa, r.Actual, "Aún buscando");
            ReglaAcoso.Salida s = r.Avanzar(0.4f, default);
            Assert.IsTrue(s.EntroAlerta);
            Assert.AreEqual(ReglaAcoso.Fase.Alerta, r.Actual);
        }

        [Test]
        public void Sorpresa_SiLoVenAntes_PasaAAlertaYa()
        {
            ReglaAcoso r = Crear(Azar(0.5f));
            r.Avanzar(Paso, new ReglaAcoso.Entrada { DanioRecibido = true });
            ReglaAcoso.Salida s = r.Avanzar(Paso, new ReglaAcoso.Entrada { JugadorDetectado = true });
            Assert.IsTrue(s.EntroAlerta);
        }

        [Test]
        public void Sorpresa_UnSegundoDanioNoReiniciaElRetraso()
        {
            ReglaAcoso r = Crear(Azar(0.5f));
            r.Avanzar(Paso, new ReglaAcoso.Entrada { DanioRecibido = true });
            r.Avanzar(1f, new ReglaAcoso.Entrada { DanioRecibido = true });
            Assert.AreEqual(Retraso - 1f, r.SorpresaRestante, 1e-3f);
        }

        [TestCase(ReglaAcoso.Fase.Acosando)]
        [TestCase(ReglaAcoso.Fase.Sorpresa)]
        [TestCase(ReglaAcoso.Fase.Alerta)]
        public void TodosVencidos_DesdeCualquierFase_PasaAVencidosUnaVez(ReglaAcoso.Fase desde)
        {
            ReglaAcoso r = Crear(Azar(0.5f));
            if (desde == ReglaAcoso.Fase.Sorpresa)
            {
                r.Avanzar(Paso, new ReglaAcoso.Entrada { DanioRecibido = true });
            }
            else if (desde == ReglaAcoso.Fase.Alerta)
            {
                r.Avanzar(Paso, new ReglaAcoso.Entrada { JugadorDetectado = true });
            }
            Assert.AreEqual(desde, r.Actual);
            ReglaAcoso.Salida s = r.Avanzar(Paso, new ReglaAcoso.Entrada { TodosVencidos = true });
            Assert.IsTrue(s.EntroVencidos);
            Assert.AreEqual(ReglaAcoso.Fase.Vencidos, r.Actual);
            Assert.IsFalse(r.Avanzar(Paso, new ReglaAcoso.Entrada { TodosVencidos = true, JugadorDetectado = true }).EntroVencidos, "Solo avisa una vez");
        }

        [Test]
        public void Vencidos_NoVuelveAAcosar()
        {
            ReglaAcoso r = Crear(Azar(0f, 0.9f));
            r.Avanzar(Paso, new ReglaAcoso.Entrada { TodosVencidos = true });
            ReglaAcoso.Salida s = Avanzar(r, 10f, default);
            Assert.AreEqual(ReglaAcoso.Gesto.Ninguno, s.Gesto);
            Assert.AreEqual(ReglaAcoso.Fase.Vencidos, r.Actual);
        }

        [Test]
        public void Alerta_NoHaceGestos()
        {
            ReglaAcoso r = Crear(Azar(0f, 0.9f));
            r.Avanzar(Paso, new ReglaAcoso.Entrada { JugadorDetectado = true });
            Assert.AreEqual(ReglaAcoso.Gesto.Ninguno, Avanzar(r, 10f, default).Gesto);
        }

        // ---- Visión ----

        private static ReglaAcoso.Vista Ver(Vector3 jugador) => ReglaAcoso.Evaluar(Vector3.zero, Vector3.forward, jugador, Vista, Angulo, Inclinacion, Oido);

        [Test]
        public void Evaluar_DeFrenteA10m_VeConLosOjos()
        {
            Assert.AreEqual(ReglaAcoso.Vista.Ojos, Ver(new Vector3(0f, 0f, 10f)));
        }

        [Test]
        public void Evaluar_MasLejosQueLaDistanciaDeVista_NoLoVe()
        {
            Assert.AreEqual(ReglaAcoso.Vista.No, Ver(new Vector3(0f, 0f, Vista + 1f)));
            Assert.AreEqual(ReglaAcoso.Vista.Ojos, Ver(new Vector3(0f, 0f, Vista - 0.5f)));
        }

        [Test]
        public void Evaluar_ALaEspaldaA8m_NoLoVe()
        {
            Assert.AreEqual(ReglaAcoso.Vista.No, Ver(new Vector3(0f, 0f, -8f)));
        }

        [Test]
        public void Evaluar_ElBordeDelCono_SeRespetaConLosGrados()
        {
            float dentro = Angulo * 0.5f - 2f;
            float fuera = Angulo * 0.5f + 2f;
            Vector3 d1 = Quaternion.Euler(0f, dentro, 0f) * Vector3.forward * 10f;
            Vector3 d2 = Quaternion.Euler(0f, fuera, 0f) * Vector3.forward * 10f;
            Assert.AreEqual(ReglaAcoso.Vista.Ojos, Ver(d1));
            Assert.AreEqual(ReglaAcoso.Vista.No, Ver(d2));
        }

        [Test]
        public void Evaluar_ALaEspaldaPeroACuatroMetros_LoOye()
        {
            Assert.AreEqual(ReglaAcoso.Vista.Oido, Ver(new Vector3(0f, 0f, -(Oido - 0.5f))));
        }

        [Test]
        public void Evaluar_EnUnaAzoteaMuyAlta_NoLoVe()
        {
            // 10 m adelante y 8 m arriba = 38.7 grados sobre su horizonte (más de 30)
            Assert.AreEqual(ReglaAcoso.Vista.No, Ver(new Vector3(0f, 8f, 10f)));
            // 10 m adelante y 4 m arriba = 21.8 grados: sí
            Assert.AreEqual(ReglaAcoso.Vista.Ojos, Ver(new Vector3(0f, 4f, 10f)));
        }

        [Test]
        public void Evaluar_ElJugadorMuyAbajo_NoCuentaLaInclinacion()
        {
            Assert.AreEqual(ReglaAcoso.Vista.Ojos, Ver(new Vector3(0f, -8f, 10f)));
        }

        [Test]
        public void EstaDesdeArriba_ConDosMetrosYMedio_Cuenta()
        {
            Assert.IsTrue(ReglaAcoso.EstaDesdeArriba(6.7f, 4.2f, 2.5f));
            Assert.IsFalse(ReglaAcoso.EstaDesdeArriba(6.0f, 4.2f, 2.5f));
        }
    }
}
