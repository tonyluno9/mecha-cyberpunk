using System;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-412: las 3 ranuras y el AUTOGUARDADO de ServicioGuardado, con una ruta temporal y dobles de jugador, zona, dinero y objetivo.</summary>
    public class RanurasPruebas
    {
        private class CarteraDoble : ICartera
        {
            public int Dinero { get; set; }
#pragma warning disable 67
            public event Action<int, MotivoDinero> Cambio;
#pragma warning restore 67
            public bool PuedePagar(int cantidad) => false;
            public bool IntentarPagar(int cantidad, MotivoDinero motivo) => false;
            public void Abonar(int cantidad, MotivoDinero motivo) { }
            public void CargarDeuda(int cantidad, MotivoDinero motivo) { }
        }

        // Dos zonas: x < 0 es "fosa" (SUBURBIOS) y x >= 0 es "tianguis" (EL TIANGUIS).
        private class TerritorioDoble : ITerritorio
        {
            public IReadOnlyList<string> Zonas => new[] { "fosa", "tianguis" };
            public string ZonaEn(Vector3 posicion) => posicion.x < 0f ? "fosa" : "tianguis";
            public string Nombre(string zona) => zona == "fosa" ? "SUBURBIOS" : zona == "tianguis" ? "EL TIANGUIS" : string.Empty;
            public IdPandilla Dueno(string zona) => IdPandilla.Ninguna;
            public int Respeto(string zona) => 0;
            public bool JefeDisponible(string zona) => false;
            public void RegistrarVencido(Vector3 posicion, IdPandilla pandilla, bool jefe) { }
            public void RegistrarCaida(Vector3 posicion) { }
#pragma warning disable 67
            public event Action<string> Cambio;
            public event Action<string, IdPandilla> Conquistada;
#pragma warning restore 67
        }

        private class JugadorDoble : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
#pragma warning disable 67
            public event Action<ModoJugador> ModoCambio;
#pragma warning restore 67
            public void Colocar(Pose pose) => Raiz.position = pose.position;
        }

        private class ObjetivosDoble : IObjetivos
        {
            public Objetivo Actual { get; set; }
            public bool HayObjetivo => !string.IsNullOrEmpty(Actual.Texto);
#pragma warning disable 67
            public event Action<Objetivo> Cambio;
#pragma warning restore 67
        }

        private class GuardableDoble : IGuardable
        {
            public string valor = "uno";
            public string ClaveGuardado => "doble";
            public string Guardar() => valor;
            public void Cargar(string datos) => valor = datos;
        }

        private string carpeta;
        private GameObject objeto;
        private GameObject cuerpo;
        private ServicioGuardado servicio;
        private CarteraDoble cartera;
        private JugadorDoble jugador;
        private ObjetivosDoble objetivos;

        [SetUp]
        public void Preparar()
        {
            Registros.LimpiarTodos();
            carpeta = Path.Combine(Application.temporaryCachePath, "ranuras_pruebas_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(carpeta);
            objeto = new GameObject("ServicioGuardado");
            servicio = objeto.AddComponent<ServicioGuardado>();
            servicio.FijarRuta(Path.Combine(carpeta, "partida.json"));
            cuerpo = new GameObject("Jugador");
            cartera = new CarteraDoble();
            jugador = new JugadorDoble { Raiz = cuerpo.transform };
            objetivos = new ObjetivosDoble();
            Registro<ICartera>.Agregar(cartera);
            Registro<ITerritorio>.Agregar(new TerritorioDoble());
            Registro<IJugador>.Agregar(jugador);
            Registro<IObjetivos>.Agregar(objetivos);
            Registro<IGuardable>.Agregar(new GuardableDoble());
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(objeto);
            Object.DestroyImmediate(cuerpo);
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
            Registros.LimpiarTodos();
        }

        private void GuardarEn(int ranura, DateTime fecha, float x, int dinero, string objetivo)
        {
            servicio.Reloj = () => fecha;
            cuerpo.transform.position = new Vector3(x, 0f, 0f);
            cartera.Dinero = dinero;
            objetivos.Actual = new Objetivo(objetivo, IdLugar.Ninguno);
            Assert.IsTrue(servicio.Guardar(ranura), "Se guarda la ranura " + ranura);
        }

        [Test]
        public void TresRanurasYAuto()
        {
            DateTime vieja = new DateTime(2026, 10, 29, 21, 14, 0, DateTimeKind.Local);
            DateTime media = new DateTime(2026, 10, 30, 8, 5, 0, DateTimeKind.Local);
            DateTime nueva = new DateTime(2026, 10, 30, 9, 30, 0, DateTimeKind.Local);
            var escritas = new List<int>();
            ServicioGuardado.RanuraEscrita += escritas.Add;
            try
            {
                GuardarEn(1, media, -20f, 120, "HABLA CON EL CHATO");
                GuardarEn(3, vieja, 10f, 340, "VENCE AL JEFE SKULL");
                GuardarEn(ServicioGuardado.RanuraAuto, nueva, 5f, 500, "VE A NEÓN ALTO");
            }
            finally
            {
                ServicioGuardado.RanuraEscrita -= escritas.Add;
            }
            CollectionAssert.AreEqual(new[] { 1, 3, 0 }, escritas, "Cada ranura escrita se avisa");

            // Los archivos: AUTO es la ruta fijada y las manuales van a su lado.
            Assert.AreEqual(Path.Combine(carpeta, "partida.json"), servicio.RutaRanura(ServicioGuardado.RanuraAuto));
            Assert.IsTrue(File.Exists(Path.Combine(carpeta, "partida_1.json")));
            Assert.IsTrue(File.Exists(Path.Combine(carpeta, "partida_3.json")));
            Assert.IsFalse(File.Exists(Path.Combine(carpeta, "partida_2.json")));

            // Leer los resúmenes no carga la partida.
            var doble = (GuardableDoble)Registro<IGuardable>.Actual;
            doble.valor = "cambiado";
            ResumenRanura uno = servicio.ResumenDe(1);
            ResumenRanura dos = servicio.ResumenDe(2);
            ResumenRanura tres = servicio.ResumenDe(3);
            ResumenRanura auto = servicio.ResumenDe(ServicioGuardado.RanuraAuto);
            Assert.AreEqual("cambiado", doble.valor, "El resumen no carga nada");

            Assert.IsFalse(uno.Vacia);
            Assert.AreEqual(media, uno.Fecha);
            Assert.AreEqual("SUBURBIOS", uno.Zona);
            Assert.AreEqual(120, uno.Dinero);
            Assert.AreEqual("HABLA CON EL CHATO", uno.Objetivo);
            Assert.IsTrue(dos.Vacia, "La ranura 2 está vacía");
            Assert.AreEqual(vieja, tres.Fecha);
            Assert.AreEqual("EL TIANGUIS", tres.Zona);
            Assert.AreEqual(340, tres.Dinero);
            Assert.AreEqual("VENCE AL JEFE SKULL", tres.Objetivo);
            Assert.AreEqual(nueva, auto.Fecha);
            Assert.AreEqual(500, auto.Dinero);
            Assert.AreEqual(ServicioGuardado.RanuraAuto, servicio.RanuraMasReciente(), "AUTO es la más nueva");

            // Volver a escribir la 3 con la fecha más nueva la hace la más reciente.
            GuardarEn(3, nueva.AddMinutes(1), 10f, 340, "VENCE AL JEFE SKULL");
            Assert.AreEqual(3, servicio.RanuraMasReciente());

            // Cargar la ranura 1 sí devuelve su dato.
            Assert.IsTrue(servicio.Cargar(1));
            Assert.AreEqual("uno", doble.valor);

            // Una ranura con texto basura sale vacía, sin excepciones y con un solo warning aunque se lea varias veces.
            File.WriteAllText(servicio.RutaRanura(1), "basura {{{ no es json");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("partida_1.json no se entiende"));
            ResumenRanura rota = default;
            Assert.DoesNotThrow(() => rota = servicio.ResumenDe(1));
            Assert.IsTrue(rota.Vacia, "La rota sale VACÍA");
            Assert.DoesNotThrow(() => servicio.RanuraMasReciente());
            Assert.IsTrue(servicio.ResumenDe(1).Vacia);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
