using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Rival;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.IA
{
    public class TurnosAtaquePruebas
    {
        private class VidaFalsa : IVida
        {
            public float Actual { get; set; } = 10f;
            public float Maxima => 10f;
            public bool EstaKO => Actual <= 0f;
            public event Action Cambio;
            public event Action KO;
            public void Llegar() { Actual = 0f; Cambio?.Invoke(); KO?.Invoke(); }
        }

        private class PeleadorFalso : IPeleador
        {
            public PeleadorFalso(LadoPelea lado, GameObject objeto) { Lado = lado; Objeto = objeto; }
            public GameObject Objeto { get; }
            public VidaFalsa VidaFalsa { get; } = new VidaFalsa();
            public string Nombre => "falso";
            public LadoPelea Lado { get; }
            public Transform Raiz => Objeto != null ? Objeto.transform : null;
            public IVida Vida => VidaFalsa;
            public bool ControlActivo => true;
            public void ActivarControl(bool activo) { }
            public void PrepararPelea(Pose inicio) { }
        }

        private readonly List<GameObject> creados = new List<GameObject>();
        private TurnosAtaque turnos;

        [SetUp]
        public void SetUp() => turnos = new TurnosAtaque();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject objeto in creados)
            {
                if (objeto != null)
                {
                    UnityEngine.Object.DestroyImmediate(objeto);
                }
            }
            creados.Clear();
        }

        private PeleadorFalso Crear(LadoPelea lado = LadoPelea.Rival)
        {
            GameObject objeto = new GameObject("falso");
            creados.Add(objeto);
            return new PeleadorFalso(lado, objeto);
        }

        [Test]
        public void Pedir_TurnoLibre_LoDaAlPrimeroYNoAlSegundo()
        {
            PeleadorFalso a = Crear();
            PeleadorFalso b = Crear();

            Assert.IsTrue(turnos.Pedir(a));
            Assert.IsFalse(turnos.Pedir(b));
            Assert.IsTrue(turnos.Tiene(a));
            Assert.IsFalse(turnos.Tiene(b));
            Assert.AreSame(a, turnos.Dueno(LadoPelea.Rival));
        }

        [Test]
        public void Pedir_QuienYaLoTiene_SigueSiendoSuyo()
        {
            PeleadorFalso a = Crear();

            Assert.IsTrue(turnos.Pedir(a));
            Assert.IsTrue(turnos.Pedir(a));
        }

        [Test]
        public void Pedir_Null_NoDaTurno()
        {
            Assert.IsFalse(turnos.Pedir(null));
            Assert.IsNull(turnos.Dueno(LadoPelea.Rival));
        }

        [Test]
        public void Soltar_ElDueno_LoLiberaParaOtro()
        {
            PeleadorFalso a = Crear();
            PeleadorFalso b = Crear();
            turnos.Pedir(a);

            turnos.Soltar(a);

            Assert.IsNull(turnos.Dueno(LadoPelea.Rival));
            Assert.IsTrue(turnos.Pedir(b));
        }

        [Test]
        public void Soltar_QuienNoLoTiene_NoQuitaElTurnoDelOtro()
        {
            PeleadorFalso a = Crear();
            PeleadorFalso b = Crear();
            turnos.Pedir(a);

            turnos.Soltar(b);

            Assert.IsTrue(turnos.Tiene(a));
            Assert.IsFalse(turnos.Pedir(b));
        }

        [Test]
        public void Pedir_DuenoEnKO_SeLiberaSolo()
        {
            PeleadorFalso a = Crear();
            PeleadorFalso b = Crear();
            turnos.Pedir(a);

            a.VidaFalsa.Llegar();

            Assert.IsNull(turnos.Dueno(LadoPelea.Rival));
            Assert.IsTrue(turnos.Pedir(b));
            Assert.IsFalse(turnos.Tiene(a));
        }

        [Test]
        public void Pedir_DuenoDesactivado_SeLiberaSolo()
        {
            PeleadorFalso a = Crear();
            PeleadorFalso b = Crear();
            turnos.Pedir(a);

            a.Objeto.SetActive(false);

            Assert.IsTrue(turnos.Pedir(b));
            a.Objeto.SetActive(true);
            Assert.IsFalse(turnos.Tiene(a), "Al reactivarse ya no tiene el turno");
        }

        [Test]
        public void Pedir_DuenoDestruido_SeLiberaSolo()
        {
            PeleadorFalso a = Crear();
            PeleadorFalso b = Crear();
            turnos.Pedir(a);

            UnityEngine.Object.DestroyImmediate(a.Objeto);

            Assert.IsNull(turnos.Dueno(LadoPelea.Rival));
            Assert.IsTrue(turnos.Pedir(b));
        }

        [Test]
        public void Pedir_LadosDistintos_NoSeEstorban()
        {
            PeleadorFalso rival = Crear(LadoPelea.Rival);
            PeleadorFalso jugador = Crear(LadoPelea.Jugador);

            Assert.IsTrue(turnos.Pedir(rival));
            Assert.IsTrue(turnos.Pedir(jugador));
            Assert.AreSame(rival, turnos.Dueno(LadoPelea.Rival));
            Assert.AreSame(jugador, turnos.Dueno(LadoPelea.Jugador));
        }

        [Test]
        public void Limpiar_ConTurnosTomados_DejaTodoLibre()
        {
            PeleadorFalso a = Crear();
            turnos.Pedir(a);

            turnos.Limpiar();

            Assert.IsNull(turnos.Dueno(LadoPelea.Rival));
        }
    }
}
