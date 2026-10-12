using System;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    public class RegistroPruebas
    {
        private class ArenaFalsa : IArenaPelea
        {
            public string Nombre => "Falsa";
            public Pose InicioJugador => new Pose(new Vector3(-6f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
            public Pose InicioRival => new Pose(new Vector3(6f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f));
            public Vector3 Centro => Vector3.zero;
            public Transform PantallaArena => null;
            public Vector2 TamanoPantalla => new Vector2(8f, 4f);
        }

        private class JugadorFalso : IJugador
        {
            public Transform Raiz => null;
            public ModoJugador Modo { get; private set; } = ModoJugador.APie;
            public Camera Camara => null;
            public event Action<ModoJugador> ModoCambio;
            public Pose UltimaPose { get; private set; }
            public void Colocar(Pose pose) => UltimaPose = pose;
            public void CambiarModo(ModoJugador modo) { Modo = modo; ModoCambio?.Invoke(modo); }
        }

        private class PuntoFalso : IPuntoAparicion
        {
            public string Id => "Casa_Cama";
            public Pose Pose => Pose.identity;
            public ModoJugador Modo => ModoJugador.APie;
        }

        [SetUp]
        public void SetUp() => Registros.LimpiarTodos();

        [TearDown]
        public void TearDown() => Registros.LimpiarTodos();

        [Test]
        public void Agregar_Elemento_QuedaRegistradoYCuenta()
        {
            var a = new ArenaFalsa();
            Registro<IArenaPelea>.Agregar(a);

            Assert.AreEqual(1, Registro<IArenaPelea>.Cantidad);
            Assert.AreSame(a, Registro<IArenaPelea>.Todos[0]);
        }

        [Test]
        public void Quitar_ElementoRegistrado_LoSaca()
        {
            var a = new ArenaFalsa();
            Registro<IArenaPelea>.Agregar(a);
            Registro<IArenaPelea>.Quitar(a);

            Assert.AreEqual(0, Registro<IArenaPelea>.Cantidad);
            Assert.IsNull(Registro<IArenaPelea>.Actual);
        }

        [Test]
        public void Agregar_NullYDuplicado_SeIgnoranSinAvisar()
        {
            var a = new ArenaFalsa();
            int avisos = 0;
            Registro<IArenaPelea>.Agregado += _ => avisos++;

            Registro<IArenaPelea>.Agregar(null);
            Registro<IArenaPelea>.Agregar(a);
            Registro<IArenaPelea>.Agregar(a);

            Assert.AreEqual(1, Registro<IArenaPelea>.Cantidad);
            Assert.AreEqual(1, avisos);
        }

        [Test]
        public void Actual_EsElUltimoAgregado_YVuelveAlAnteriorAlQuitarlo()
        {
            var a = new ArenaFalsa();
            var b = new ArenaFalsa();
            Registro<IArenaPelea>.Agregar(a);
            Registro<IArenaPelea>.Agregar(b);
            Assert.AreSame(b, Registro<IArenaPelea>.Actual);

            Registro<IArenaPelea>.Quitar(b);

            Assert.AreSame(a, Registro<IArenaPelea>.Actual);
        }

        [Test]
        public void Agregado_AvisaUnaVezConElElemento()
        {
            var a = new ArenaFalsa();
            IArenaPelea recibido = null;
            int avisos = 0;
            Registro<IArenaPelea>.Agregado += x => { recibido = x; avisos++; };

            Registro<IArenaPelea>.Agregar(a);

            Assert.AreEqual(1, avisos);
            Assert.AreSame(a, recibido);
        }

        [Test]
        public void Quitado_AvisaUnaVezPorCambioReal_YNoSiNoEstaba()
        {
            var a = new ArenaFalsa();
            var ajena = new ArenaFalsa();
            int avisos = 0;
            IArenaPelea recibido = null;
            Registro<IArenaPelea>.Quitado += x => { recibido = x; avisos++; };
            Registro<IArenaPelea>.Agregar(a);

            Registro<IArenaPelea>.Quitar(ajena);
            Assert.AreEqual(0, avisos);

            Registro<IArenaPelea>.Quitar(a);
            Registro<IArenaPelea>.Quitar(a);

            Assert.AreEqual(1, avisos);
            Assert.AreSame(a, recibido);
        }

        [Test]
        public void TiposDistintos_NoSeMezclan()
        {
            Registro<IArenaPelea>.Agregar(new ArenaFalsa());
            Registro<IJugador>.Agregar(new JugadorFalso());
            Registro<IPuntoAparicion>.Agregar(new PuntoFalso());
            Registro<IPuntoAparicion>.Agregar(new PuntoFalso());

            Assert.AreEqual(1, Registro<IArenaPelea>.Cantidad);
            Assert.AreEqual(1, Registro<IJugador>.Cantidad);
            Assert.AreEqual(2, Registro<IPuntoAparicion>.Cantidad);
        }

        [Test]
        public void LimpiarTodos_VaciaTodosLosTiposYQuitaSuscriptores()
        {
            int avisos = 0;
            Registro<IArenaPelea>.Agregado += _ => avisos++;
            Registro<IJugador>.Quitado += _ => avisos++;
            Registro<IArenaPelea>.Agregar(new ArenaFalsa());
            var j = new JugadorFalso();
            Registro<IJugador>.Agregar(j);
            Assert.AreEqual(1, avisos);

            Registros.LimpiarTodos();

            Assert.AreEqual(0, Registro<IArenaPelea>.Cantidad);
            Assert.AreEqual(0, Registro<IJugador>.Cantidad);
            Registro<IArenaPelea>.Agregar(new ArenaFalsa());
            Registro<IJugador>.Agregar(j);
            Registro<IJugador>.Quitar(j);
            Assert.AreEqual(1, avisos, "los suscriptores deben haberse quitado");
        }

        [Test]
        public void ContratosFalsos_SePuedenUsarPorSuInterfaz()
        {
            var jugador = new JugadorFalso();
            Registro<IJugador>.Agregar(jugador);
            ModoJugador modo = ModoJugador.APie;
            Registro<IJugador>.Actual.ModoCambio += m => modo = m;

            jugador.CambiarModo(ModoJugador.Mecha);
            var arena = new ArenaFalsa();
            Registro<IJugador>.Actual.Colocar(arena.InicioJugador);

            Assert.AreEqual(ModoJugador.Mecha, modo);
            Assert.AreEqual(arena.InicioJugador.position, jugador.UltimaPose.position);
        }
    }
}
