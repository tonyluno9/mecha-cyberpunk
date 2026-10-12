using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    public class UbicacionObjetivoPruebas
    {
        private class LugarFalso : ILugar
        {
            public IdLugar Id { get; set; }
            public string Nombre => "Falso";
            public HorarioLugar Horario => default;
            public bool EstaAbierto => true;
            public bool JugadorDentro => false;
            public Vector3 PosicionEntrada { get; set; }
            public event Action<bool> EstadoCambio { add { } remove { } }
        }

        [SetUp]
        public void SetUp() => Registros.LimpiarTodos();

        [TearDown]
        public void TearDown() => Registros.LimpiarTodos();

        [Test]
        public void Resolver_ObjetivoConPosicion_DevuelveEsaPosicion()
        {
            var objetivo = new Objetivo("Ir al punto", new Vector3(3f, 0f, 7f));
            bool ok = UbicacionObjetivo.Resolver(objetivo, new List<ILugar>(), out Vector3 pos);
            Assert.IsTrue(ok);
            Assert.AreEqual(new Vector3(3f, 0f, 7f), pos);
        }

        [Test]
        public void Resolver_ObjetivoDeLugarRegistrado_DevuelvePosicionEntrada()
        {
            var lugares = new List<ILugar>
            {
                new LugarFalso { Id = IdLugar.Bar, PosicionEntrada = new Vector3(1f, 0f, 1f) },
                new LugarFalso { Id = IdLugar.Taller, PosicionEntrada = new Vector3(9f, 0f, 4f) }
            };
            bool ok = UbicacionObjetivo.Resolver(new Objetivo("Ve al taller", IdLugar.Taller), lugares, out Vector3 pos);
            Assert.IsTrue(ok);
            Assert.AreEqual(new Vector3(9f, 0f, 4f), pos);
        }

        [Test]
        public void Resolver_LugarQueNoEstaEnLaLista_DevuelveFalse()
        {
            var lugares = new List<ILugar> { new LugarFalso { Id = IdLugar.Bar } };
            bool ok = UbicacionObjetivo.Resolver(new Objetivo("Casino", IdLugar.Casino), lugares, out Vector3 pos);
            Assert.IsFalse(ok);
            Assert.AreEqual(Vector3.zero, pos);
        }

        [Test]
        public void Resolver_ObjetivoNinguno_DevuelveFalse()
        {
            var lugares = new List<ILugar> { new LugarFalso { Id = IdLugar.Bar } };
            Assert.IsFalse(UbicacionObjetivo.Resolver(Objetivo.Ninguno, lugares, out _));
        }

        [Test]
        public void Resolver_ListaVacia_DevuelveFalse()
        {
            Assert.IsFalse(UbicacionObjetivo.Resolver(new Objetivo("x", IdLugar.Bar), new List<ILugar>(), out _));
        }

        [Test]
        public void Resolver_ListaNull_DevuelveFalseSinExcepcion()
        {
            bool ok = false;
            Assert.DoesNotThrow(() => ok = UbicacionObjetivo.Resolver(new Objetivo("x", IdLugar.Bar), null, out _));
            Assert.IsFalse(ok);
        }

        [Test]
        public void Resolver_ConRegistroDeLugares_UsaTodos()
        {
            Registro<ILugar>.Agregar(new LugarFalso { Id = IdLugar.Casa, PosicionEntrada = new Vector3(5f, 0f, 5f) });
            bool ok = UbicacionObjetivo.Resolver(new Objetivo("Casa", IdLugar.Casa), Registro<ILugar>.Todos, out Vector3 pos);
            Assert.IsTrue(ok);
            Assert.AreEqual(new Vector3(5f, 0f, 5f), pos);
        }
    }
}
