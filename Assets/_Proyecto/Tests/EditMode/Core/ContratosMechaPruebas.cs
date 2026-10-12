using System;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    public class ContratosMechaPruebas
    {
        private class PuntoLlamadoFalso : IPuntoLlamadoMecha
        {
            public Pose Llegada => new Pose(new Vector3(4f, 0f, 7f), Quaternion.identity);
            public float Radio => 25f;
            public bool Habilitado { get; set; } = true;
        }

        private class TallerFalso : ITallerMecha
        {
            public Pose Estacionamiento => new Pose(new Vector3(1f, 0f, 2f), Quaternion.identity);
            public Vector3 PuntoInteraccion => new Vector3(3f, 0f, 2f);
        }

        private class PerfilFalso : IPerfilJugador
        {
            public string Nombre { get; private set; } = "Respaldo";
            public string Alias { get; private set; } = "Alias";
            public event Action Cambio;

            public void Poner(string nombre, string alias)
            {
                Nombre = nombre;
                Alias = alias;
                Cambio?.Invoke();
            }
        }

        [SetUp]
        public void SetUp() => Registros.LimpiarTodos();

        [TearDown]
        public void TearDown() => Registros.LimpiarTodos();

        [Test]
        public void PuntoLlamado_AgregarYQuitar_EntraYSaleDelRegistro()
        {
            var punto = new PuntoLlamadoFalso();
            Registro<IPuntoLlamadoMecha>.Agregar(punto);
            Assert.AreSame(punto, Registro<IPuntoLlamadoMecha>.Actual);
            Assert.AreEqual(25f, punto.Radio);
            Assert.AreEqual(new Vector3(4f, 0f, 7f), punto.Llegada.position);
            Registro<IPuntoLlamadoMecha>.Quitar(punto);
            Assert.AreEqual(0, Registro<IPuntoLlamadoMecha>.Cantidad);
        }

        [Test]
        public void PuntoLlamado_Deshabilitado_SeLeeDesdeLaInterfaz()
        {
            var punto = new PuntoLlamadoFalso { Habilitado = false };
            Registro<IPuntoLlamadoMecha>.Agregar(punto);
            Assert.IsFalse(Registro<IPuntoLlamadoMecha>.Actual.Habilitado);
        }

        [Test]
        public void Taller_AgregarYQuitar_EntraYSaleDelRegistro()
        {
            var taller = new TallerFalso();
            Registro<ITallerMecha>.Agregar(taller);
            Assert.AreSame(taller, Registro<ITallerMecha>.Actual);
            Assert.AreEqual(new Vector3(1f, 0f, 2f), taller.Estacionamiento.position);
            Assert.AreEqual(new Vector3(3f, 0f, 2f), taller.PuntoInteraccion);
            Registro<ITallerMecha>.Quitar(taller);
            Assert.AreEqual(0, Registro<ITallerMecha>.Cantidad);
        }

        [Test]
        public void Perfil_AgregarYQuitar_EntraYSaleYAvisaCambio()
        {
            var perfil = new PerfilFalso();
            int avisos = 0;
            perfil.Cambio += () => avisos++;
            Registro<IPerfilJugador>.Agregar(perfil);
            Assert.AreSame(perfil, Registro<IPerfilJugador>.Actual);
            perfil.Poner("Tony", "El Rayo");
            Assert.AreEqual(1, avisos);
            Assert.AreEqual("El Rayo", Registro<IPerfilJugador>.Actual.Alias);
            Registro<IPerfilJugador>.Quitar(perfil);
            Assert.AreEqual(0, Registro<IPerfilJugador>.Cantidad);
        }

        [Test]
        public void LimpiarTodos_ConLosTresRegistrados_DejaLosTresVacios()
        {
            Registro<IPuntoLlamadoMecha>.Agregar(new PuntoLlamadoFalso());
            Registro<ITallerMecha>.Agregar(new TallerFalso());
            Registro<IPerfilJugador>.Agregar(new PerfilFalso());
            Registros.LimpiarTodos();
            Assert.AreEqual(0, Registro<IPuntoLlamadoMecha>.Cantidad);
            Assert.AreEqual(0, Registro<ITallerMecha>.Cantidad);
            Assert.AreEqual(0, Registro<IPerfilJugador>.Cantidad);
        }
    }
}
