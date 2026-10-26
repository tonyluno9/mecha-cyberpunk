using System;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    /// <summary>Contratos de reputación, misión en curso y puntos de misión. Los dobles sirven de ejemplo para Alan.</summary>
    public class ContratosMisionesPruebas
    {
        private class ReputacionFalsa : IReputacion
        {
            private readonly int[] valores = new int[2];
            public int Minimo => -100;
            public int Maximo => 100;
            public int Valor(TipoReputacion tipo) => valores[(int)tipo];
            public int Nivel(TipoReputacion tipo) => NivelDe(valores[(int)tipo]);
            public string NombreNivel(TipoReputacion tipo)
            {
                switch (Nivel(tipo))
                {
                    case 2: return "JUSTICIERO";
                    case -2: return "ENEMIGO";
                    default: return "NEUTRAL";
                }
            }
            public event Action Cambio;
            public event Action<TipoReputacion, int> NivelCambio;

            private static int NivelDe(int valor) => valor >= 60 ? 2 : valor >= 20 ? 1 : valor <= -60 ? -2 : valor <= -20 ? -1 : 0;

            public void Sumar(TipoReputacion tipo, int delta)
            {
                int antes = Nivel(tipo);
                valores[(int)tipo] = Mathf.Clamp(valores[(int)tipo] + delta, Minimo, Maximo);
                Cambio?.Invoke();
                if (Nivel(tipo) != antes) NivelCambio?.Invoke(tipo, Nivel(tipo));
            }

            public void CargarValor(TipoReputacion tipo, int valor)
            {
                valores[(int)tipo] = Mathf.Clamp(valor, Minimo, Maximo);
                Cambio?.Invoke();
            }
        }

        private class MisionesFalsas : IMisiones
        {
            public bool HayMision { get; private set; }
            public TipoMision Tipo { get; private set; }
            public string Titulo { get; private set; } = "";
            public event Action Cambio;
            public event Action<TipoMision, bool> Terminada;

            public void Aceptar(TipoMision tipo, string titulo)
            {
                HayMision = true; Tipo = tipo; Titulo = titulo;
                Cambio?.Invoke();
            }

            public void Terminar(bool cumplida)
            {
                var tipo = Tipo;
                HayMision = false; Titulo = "";
                Cambio?.Invoke();
                Terminada?.Invoke(tipo, cumplida);
            }

            public void Cargar(TipoMision tipo, string titulo)
            {
                HayMision = true; Tipo = tipo; Titulo = titulo;
                Cambio?.Invoke();
            }
        }

        private class PuntoFalso : IPuntoMision
        {
            public string Id { get; set; }
            public TipoPuntoMision Tipo { get; set; }
            public Pose Pose { get; set; }
            public bool Activo;
            public void Activar(bool activo) { Activo = activo; }
        }

        [Test]
        public void Reputacion_Valor_SeRecortaAlRango()
        {
            var rep = new ReputacionFalsa();
            Assert.AreEqual(0, rep.Valor(TipoReputacion.Barrio));
            rep.Sumar(TipoReputacion.Barrio, 500);
            Assert.AreEqual(rep.Maximo, rep.Valor(TipoReputacion.Barrio));
            rep.Sumar(TipoReputacion.Corporaciones, -500);
            Assert.AreEqual(rep.Minimo, rep.Valor(TipoReputacion.Corporaciones));
        }

        [Test]
        public void Reputacion_NivelCambio_SoloCuandoCambiaElNivel()
        {
            var rep = new ReputacionFalsa();
            int avisos = 0; int nivelAvisado = 0;
            rep.NivelCambio += (t, n) => { avisos++; nivelAvisado = n; };
            rep.Sumar(TipoReputacion.Barrio, 5);
            Assert.AreEqual(0, avisos);
            rep.Sumar(TipoReputacion.Barrio, 20);
            Assert.AreEqual(1, avisos);
            Assert.AreEqual(1, nivelAvisado);
            rep.Sumar(TipoReputacion.Barrio, 1);
            Assert.AreEqual(1, avisos);
        }

        [Test]
        public void Reputacion_Cargar_AvisaCambioPeroNoNivelCambio()
        {
            var rep = new ReputacionFalsa();
            int cambios = 0, niveles = 0;
            rep.Cambio += () => cambios++;
            rep.NivelCambio += (t, n) => niveles++;
            rep.CargarValor(TipoReputacion.Barrio, 80);
            Assert.AreEqual(1, cambios);
            Assert.AreEqual(0, niveles);
            Assert.AreEqual(2, rep.Nivel(TipoReputacion.Barrio));
        }

        [Test]
        public void Reputacion_NombreNivel_NuncaEsNull()
        {
            var rep = new ReputacionFalsa();
            foreach (TipoReputacion tipo in Enum.GetValues(typeof(TipoReputacion)))
            {
                foreach (int valor in new[] { -100, -30, 0, 30, 100 })
                {
                    rep.CargarValor(tipo, valor);
                    Assert.IsNotNull(rep.NombreNivel(tipo));
                    Assert.IsTrue(rep.Nivel(tipo) >= -2 && rep.Nivel(tipo) <= 2);
                }
            }
        }

        [Test]
        public void Misiones_Terminada_AvisaSegunResultadoYNoAlCargar()
        {
            var m = new MisionesFalsas();
            Assert.IsFalse(m.HayMision);
            Assert.AreEqual("", m.Titulo);
            TipoMision tipoAvisado = TipoMision.Barrio; bool resultado = false; int terminadas = 0;
            m.Terminada += (t, ok) => { tipoAvisado = t; resultado = ok; terminadas++; };

            m.Cargar(TipoMision.Robo, "Golpe al almacén");
            Assert.AreEqual(0, terminadas);
            Assert.IsTrue(m.HayMision);

            m.Terminar(false);
            Assert.AreEqual(1, terminadas);
            Assert.AreEqual(TipoMision.Robo, tipoAvisado);
            Assert.IsFalse(resultado);
            Assert.IsFalse(m.HayMision);
            Assert.AreEqual("", m.Titulo);
        }

        [Test]
        public void Misiones_Cambio_AvisaAlAceptarYAlTerminar()
        {
            var m = new MisionesFalsas();
            int cambios = 0;
            m.Cambio += () => cambios++;
            m.Aceptar(TipoMision.Barrio, "Doña Lupe");
            m.Terminar(true);
            Assert.AreEqual(2, cambios);
        }

        [Test]
        public void PuntoMision_EnRegistro_SeEncuentraPorId()
        {
            var punto = new PuntoFalso { Id = "Mision_Encargo_DonaLupe", Tipo = TipoPuntoMision.Encargo };
            var otro = new PuntoFalso { Id = "Mision_Destino_Puente", Tipo = TipoPuntoMision.Destino };
            Registro<IPuntoMision>.Agregar(punto);
            Registro<IPuntoMision>.Agregar(otro);
            try
            {
                IPuntoMision hallado = null;
                foreach (var p in Registro<IPuntoMision>.Todos)
                    if (p.Id == "Mision_Encargo_DonaLupe") hallado = p;
                Assert.AreSame(punto, hallado);
                hallado.Activar(true);
                Assert.IsTrue(punto.Activo);
                Assert.IsFalse(otro.Activo);
            }
            finally
            {
                Registro<IPuntoMision>.Quitar(punto);
                Registro<IPuntoMision>.Quitar(otro);
            }
            Assert.AreEqual(0, Registro<IPuntoMision>.Cantidad);
        }

        [Test]
        public void Motivos_MisionEsElUltimoValor()
        {
            var dinero = (MotivoDinero[])Enum.GetValues(typeof(MotivoDinero));
            var calor = (MotivoCalor[])Enum.GetValues(typeof(MotivoCalor));
            Assert.AreEqual(MotivoDinero.Mision, dinero[dinero.Length - 1]);
            Assert.AreEqual(MotivoDinero.Carga, dinero[dinero.Length - 2]);
            Assert.AreEqual(MotivoCalor.Mision, calor[calor.Length - 1]);
            Assert.AreEqual(MotivoCalor.Otro, calor[calor.Length - 2]);
        }
    }
}
