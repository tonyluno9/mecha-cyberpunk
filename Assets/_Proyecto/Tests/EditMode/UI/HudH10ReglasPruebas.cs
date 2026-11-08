using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Hud;
using MechaCyberpunk.UI.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-700: las reglas puras del HUD nuevo (estrellas, ayudas de botón, muñequito, teclas, dinero) y su acomodo.</summary>
    public class HudH10ReglasPruebas
    {
        // ---- Estrellas ----

        [Test]
        public void Estrellas_SinCalor_NingunaLlena()
        {
            Assert.AreEqual(0, ReglaEstrellasH10.Llenas(0, 0f, 100f));
            Assert.AreEqual(0, ReglaEstrellasH10.Llenas(0, 50f, 100f), "Con nivel 0 no hay estrellas aunque quede valor");
        }

        [Test]
        public void Estrellas_ElNivelUnoSiempreMuestraUna_YNuncaPasaDeCinco()
        {
            Assert.AreEqual(1, ReglaEstrellasH10.Llenas(1, 0.1f, 100f));
            Assert.AreEqual(ReglaEstrellasH10.Total, ReglaEstrellasH10.Llenas(3, 1000f, 100f));
            Assert.AreEqual(3, ReglaEstrellasH10.Llenas(3, 10f, 100f), "El nivel manda si el valor es bajo");
        }

        [Test]
        public void Estrellas_CreceConElValor()
        {
            Assert.AreEqual(2, ReglaEstrellasH10.Llenas(1, 30f, 100f));
            Assert.AreEqual(4, ReglaEstrellasH10.Llenas(2, 70f, 100f));
            Assert.AreEqual(5, ReglaEstrellasH10.Llenas(3, 100f, 100f));
            Assert.AreEqual(3, ReglaEstrellasH10.Llenas(3, 5f, 0f), "Sin máximo cuenta el nivel");
        }

        [Test]
        public void Estrellas_ParpadeanSoloSiTeBuscan()
        {
            Assert.AreEqual(1f, ReglaEstrellasH10.Alfa(0.1f, false), 0.001f);
            Assert.AreEqual(1f, ReglaEstrellasH10.Alfa(0.5f, false), 0.001f, "Sin búsqueda están fijas");
            Assert.AreEqual(1f, ReglaEstrellasH10.Alfa(0.1f, true), 0.001f);
            Assert.AreEqual(ReglaEstrellasH10.AlfaApagada, ReglaEstrellasH10.Alfa(0.5f, true), 0.001f, "Buscando, la mitad del ciclo se apagan");
            Assert.AreEqual(1f, ReglaEstrellasH10.Alfa(0.9f, true), 0.001f);
        }

        [Test]
        public void Estrellas_SoloParpadeanCuandoTeEstanPorPerder()
        {
            Assert.IsFalse(ReglaEstrellasH10.DebeParpadear(false, false, 50f), "Sin búsqueda no parpadean");
            Assert.IsFalse(ReglaEstrellasH10.DebeParpadear(true, true, 50f), "Si te ven, fijas");
            Assert.IsFalse(ReglaEstrellasH10.DebeParpadear(true, false, 3f), "Te buscan pero hace poco que no te ven: fijas");
            Assert.IsFalse(ReglaEstrellasH10.DebeParpadear(true, false, ReglaEstrellasH10.SegundosSinVerParaParpadear - 0.1f));
            Assert.IsTrue(ReglaEstrellasH10.DebeParpadear(true, false, ReglaEstrellasH10.SegundosSinVerParaParpadear), "Llevas rato sin que te vean: te están por perder");
        }

        // ---- Ayudas de botón ----

        private static ContextoAyudaH10 Base() => new ContextoAyudaH10
        {
            Modo = ModoJugador.APie,
            ArmaDeFuego = true,
            Balas = 8,
            Cargador = 8,
            TeclaRecargar = "R",
            TeclaGolpe = "Clic",
            TeclaInteractuar = "E",
        };

        [Test]
        public void Ayudas_ConElCargadorLleno_NoHayRecargar()
        {
            Assert.AreEqual(0, ReglaAyudasH10.Ayudas(Base()).Count);
        }

        [Test]
        public void Ayudas_ConElCargadorALaMitad_OfreceRecargarConSuTecla()
        {
            ContextoAyudaH10 c = Base();
            c.Balas = 4;
            var ayudas = ReglaAyudasH10.Ayudas(c);
            Assert.AreEqual(1, ayudas.Count);
            Assert.AreEqual("R", ayudas[0].Tecla);
            Assert.AreEqual(ReglaAyudasH10.TextoRecargar, ayudas[0].Texto);
        }

        [Test]
        public void Ayudas_RecargandoOSinCargador_NoOfreceRecargar()
        {
            ContextoAyudaH10 c = Base();
            c.Balas = 2;
            c.Recargando = true;
            Assert.AreEqual(0, ReglaAyudasH10.Ayudas(c).Count);
            c.Recargando = false;
            c.Cargador = 0;
            Assert.AreEqual(0, ReglaAyudasH10.Ayudas(c).Count);
        }

        [Test]
        public void Ayudas_ConLosPunos_OfreceGolpe()
        {
            ContextoAyudaH10 c = Base();
            c.ArmaDeFuego = false;
            var ayudas = ReglaAyudasH10.Ayudas(c);
            Assert.AreEqual(1, ayudas.Count);
            Assert.AreEqual("Clic", ayudas[0].Tecla);
            Assert.AreEqual(ReglaAyudasH10.TextoGolpe, ayudas[0].Texto);
        }

        [Test]
        public void Ayudas_CercaDeUnCarro_OfreceSubirYVaPrimero()
        {
            ContextoAyudaH10 c = Base();
            c.Balas = 3;
            c.Candidato = TipoCandidatoInteraccion.Montable;
            c.NombreCandidato = "Carro de prueba";
            var ayudas = ReglaAyudasH10.Ayudas(c);
            Assert.AreEqual(2, ayudas.Count);
            Assert.AreEqual("E", ayudas[0].Tecla);
            Assert.AreEqual(ReglaAyudasH10.TextoSubirCarro, ayudas[0].Texto);
            Assert.AreEqual(ReglaAyudasH10.TextoRecargar, ayudas[1].Texto);
        }

        [Test]
        public void Ayudas_UnaMoto_DiceSubirALaMoto()
        {
            ContextoAyudaH10 c = Base();
            c.Candidato = TipoCandidatoInteraccion.Montable;
            c.NombreCandidato = "Moto del barrio";
            Assert.AreEqual(ReglaAyudasH10.TextoSubirMoto, ReglaAyudasH10.Ayudas(c)[0].Texto);
        }

        [Test]
        public void Ayudas_UsarHablarYPuertas_UsanSuTexto()
        {
            ContextoAyudaH10 c = Base();
            c.Candidato = TipoCandidatoInteraccion.Interactuable;
            c.TextoInteractuable = "Hablar con el Doc";
            c.PuedeUsarInteractuable = true;
            Assert.AreEqual("Hablar con el Doc", ReglaAyudasH10.Ayudas(c)[0].Texto);
            c.TextoInteractuable = string.Empty;
            Assert.AreEqual(ReglaAyudasH10.TextoUsar, ReglaAyudasH10.Ayudas(c)[0].Texto);
            c.PuedeUsarInteractuable = false;
            Assert.AreEqual(0, ReglaAyudasH10.Ayudas(c).Count, "Lo que no se puede usar es un aviso, no una ayuda");
            c.Candidato = TipoCandidatoInteraccion.Puerta;
            c.NombreCandidato = "Bar";
            Assert.AreEqual("Abrir Bar", ReglaAyudasH10.Ayudas(c)[0].Texto);
        }

        [Test]
        public void Ayudas_NuncaPasanDeDos_YSoloSalenAPie()
        {
            ContextoAyudaH10 c = Base();
            c.ArmaDeFuego = false;
            c.Candidato = TipoCandidatoInteraccion.Montable;
            Assert.AreEqual(ReglaAyudasH10.Maximo, ReglaAyudasH10.Ayudas(c).Count);
            c.Modo = ModoJugador.Carro;
            Assert.AreEqual(0, ReglaAyudasH10.Ayudas(c).Count);
            c.Modo = ModoJugador.Mecha;
            Assert.AreEqual(0, ReglaAyudasH10.Ayudas(c).Count);
        }

        // ---- Muñequito ----

        [Test]
        public void Cuerpo_LaZonaSalDeLaAlturaYElLado()
        {
            Assert.AreEqual(ZonaCuerpoH10.Cabeza, ReglaCuerpoH10.Zona(1.7f, 0f));
            Assert.AreEqual(ZonaCuerpoH10.Torso, ReglaCuerpoH10.Zona(1.2f, 0.1f));
            Assert.AreEqual(ZonaCuerpoH10.Brazos, ReglaCuerpoH10.Zona(1.2f, -0.4f));
            Assert.AreEqual(ZonaCuerpoH10.Piernas, ReglaCuerpoH10.Zona(0.5f, 0f));
        }

        [Test]
        public void Cuerpo_ElRojoBajaConElTiempoYSeApagaALosCuatroSegundos()
        {
            Assert.AreEqual(1f, ReglaCuerpoH10.Intensidad(0f), 0.001f);
            Assert.AreEqual(0.5f, ReglaCuerpoH10.Intensidad(ReglaCuerpoH10.SegundosRojo * 0.5f), 0.001f);
            Assert.AreEqual(0f, ReglaCuerpoH10.Intensidad(ReglaCuerpoH10.SegundosRojo), 0.001f);
            Assert.AreEqual(0f, ReglaCuerpoH10.Intensidad(99f), 0.001f);
        }

        // ---- Teclas ----

        [Test]
        public void Teclas_Nombre_TraduceLosBotonesDelRaton()
        {
            Assert.AreEqual("Clic", TeclasHud.Nombre("LMB"));
            Assert.AreEqual("Clic", TeclasHud.Nombre("Left Button"));
            Assert.AreEqual("Clic der.", TeclasHud.Nombre("RMB"));
            Assert.AreEqual("E", TeclasHud.Nombre("E"));
            Assert.AreEqual(string.Empty, TeclasHud.Nombre(null));
        }

        [Test]
        public void Teclas_Texto_LeeLaTeclaRealDeLaAccion()
        {
            InputAction recargar = new InputAction("Recargar", InputActionType.Button, "<Keyboard>/r");
            InputAction golpe = new InputAction("Atacar", InputActionType.Button, "<Mouse>/leftButton");
            InputAction otra = new InputAction("Interactuar", InputActionType.Button, "<Keyboard>/f");
            try
            {
                Assert.AreEqual("R", TeclasHud.Texto(recargar));
                Assert.AreEqual("Clic", TeclasHud.Texto(golpe));
                Assert.AreEqual("F", TeclasHud.Texto(otra), "Si se reasigna la tecla, el HUD muestra la nueva");
                Assert.AreEqual("X", TeclasHud.Texto(null, "X"), "Sin acción cae al respaldo");
            }
            finally
            {
                recargar.Dispose();
                golpe.Dispose();
                otra.Dispose();
            }
        }

        [Test]
        public void Teclas_Texto_ConGamepadYTeclado_ElijeElTeclado()
        {
            InputAction accion = new InputAction("Mixta", InputActionType.Button);
            accion.AddBinding("<Gamepad>/buttonSouth");
            accion.AddBinding("<Keyboard>/e");
            try
            {
                Assert.AreEqual("E", TeclasHud.Texto(accion));
            }
            finally
            {
                accion.Dispose();
            }
        }

        // ---- Dinero ----

        [Test]
        public void Dinero_SeVeConSimboloYSeparadorDeMiles()
        {
            Assert.AreEqual("$ 12,450", HudH10.TextoDineroH10(12450));
            Assert.AreEqual("$ 0", HudH10.TextoDineroH10(0));
            Assert.AreEqual("$ 1,000,000", HudH10.TextoDineroH10(1000000));
        }

        // ---- Acomodo (como H10_HUD.png a 1920x1080) ----

        [Test]
        public void Acomodo_ElMinimapaEsCuadradoArribaALaDerecha()
        {
            RanuraHud m = DisposicionHud.Minimapa;
            Assert.AreEqual(EsquinaHud.ArribaDerecha, m.Esquina);
            Assert.AreEqual(260f, m.Alto, 0.01f);
            Assert.AreEqual(63f, m.DesdeLado, 0.01f, "Margen derecho de H10");
            Assert.AreEqual(59f, m.DesdeBorde, 0.01f, "Margen de arriba de H10");
        }

        [Test]
        public void Acomodo_LaZonaNoTapaElMinimapa_NiLaAyudaDeControlesLosAliados()
        {
            RanuraHud zona = DisposicionHud.Zona;
            RanuraHud mapa = DisposicionHud.Minimapa;
            Assert.GreaterOrEqual(zona.DesdeLado, mapa.DesdeLado + DisposicionHud.LadoMinimapa, "La zona queda a la izquierda del minimapa");
            RanuraHud ayuda = DisposicionHud.AyudaControles;
            RanuraHud pandilla = DisposicionHud.Pandilla;
            Assert.AreEqual(EsquinaHud.AbajoIzquierda, ayuda.Esquina);
            Assert.GreaterOrEqual(ayuda.DesdeBorde, 62f + 114f, "La ayuda de controles sube sobre las casillas de objetos");
            Assert.GreaterOrEqual(pandilla.DesdeBorde, ayuda.Fin, "Los aliados suben sobre la ayuda de controles");
        }

        [Test]
        public void Acomodo_LaRentaBajaDelDineroDelHudNuevo()
        {
            Assert.GreaterOrEqual(DisposicionHud.Renta.DesdeBorde, 330f);
        }

        // ---- Assets ----

        [Test]
        public void Config_TraeFuenteYLosIconosDeLosCincoObjetos()
        {
            ConfigHudH10 config = Resources.Load<ConfigHudH10>(ConfigHudH10.NombreRecurso);
            Assert.IsNotNull(config, "Existe UI/Resources/ConfigHudH10.asset");
            Assert.IsNotNull(config.Fuente, "Rajdhani");
            Assert.IsNotNull(config.FuenteNumeros);
            foreach (string id in new[] { "bebida_energetica", "papitas", "venda", "cafe", "bateria_escudo" })
            {
                Assert.IsNotNull(config.Icono(id), "Ícono de " + id);
            }
            Assert.IsNull(config.Icono("no_existe"));
            Assert.IsNotNull(config.EstrellaContorno);
            Assert.IsNotNull(config.EstrellaLlena);
            Assert.IsNotNull(config.ConoMinimapa);
            Assert.IsNotNull(config.PuntoRuta);
            for (int z = 0; z < ConfigHudH10.Zonas; z++)
            {
                Assert.IsNotNull(config.CuerpoLinea((ZonaCuerpoH10)z), "Línea de " + (ZonaCuerpoH10)z);
                Assert.IsNotNull(config.CuerpoRelleno((ZonaCuerpoH10)z), "Relleno de " + (ZonaCuerpoH10)z);
            }
        }
    }
}
