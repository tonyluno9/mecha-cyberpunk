using System.Collections;
using System.Globalization;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Apariencia;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Historia;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion.Implantes
{
    /// <summary>
    /// T-193: la clínica a pie con entrada simulada (W, E, flechas, Enter, Esc, F5 y F9). Precios, hora y textos salen de los assets
    /// y se imprimen. Capturas con CAPTURAR_T193=1.
    /// </summary>
    public class ClinicaPruebas : BaseImplantesPruebas
    {
        private const string RutaCaptura1 = "docs/capturas/T-193-1.png";
        private const int FilaOjos = 0;
        private const int FilaBrazos = 1;
        private const int FilaPiernas = 2;
        private const int DineroJusto = 1000;
        private const int DineroNegativo = -500;
        private const int LigaAlta = 3;
        private const float ToleranciaHora = 0.1f;
        private const float HorasPorSegundoDePrueba = 0.02f; // RelojDePrueba: 24 h cada 20 min
        private const float ToleranciaAlfa = 0.02f;

        private IEnumerator Bajar(int veces)
        {
            for (int i = 0; i < veces; i++) yield return Tecla(teclado.downArrowKey);
        }

        private static string F(float valor) => valor.ToString("0.###", CultureInfo.InvariantCulture);

        [UnityTest]
        public IEnumerator Clinica_InstalarPiernas_CobraAvanzaDosHorasYAvisa()
        {
            yield return CargarEscena();
            PonerDinero(DineroAlto);
            PonerLiga(1);
            DatosImplante piernas = Datos(EspacioImplante.Piernas, 1);
            int motivoClinica = 0;
            int cobrado = 0;
            cartera.Cambio += (delta, motivo) =>
            {
                if (motivo == MotivoDinero.Clinica) { motivoClinica++; cobrado += delta; }
            };

            yield return AbrirClinica();
            Assert.AreEqual(config.TextoTitulo, pantalla.Titulo, "el título de la pantalla");
            yield return Bajar(2);
            Assert.AreEqual(FilaPiernas, pantalla.Indice, "↓ ↓ elige PIERNAS");
            Assert.AreEqual(piernas.Descripcion, pantalla.Descripcion, "la descripción de la opción elegida");
            yield return Tecla(teclado.enterKey);
            Assert.IsTrue(pantalla.Confirmando, "el primer Enter pide confirmar");
            StringAssert.Contains("¿INSTALAR POR $" + piernas.Precio.ToString("N0", CultureInfo.InvariantCulture) + "?", pantalla.Aviso);
            Assert.AreEqual(DineroAlto, cartera.Dinero, "pedir confirmar no cobra");

            float hora0 = reloj.Hora;
            float t0 = Time.time;
            yield return Tecla(teclado.enterKey);
            float alfaMaxima = 0f;
            bool vioSubtitulo = false;
            float fin = Time.time + MaxSegundosOperacion;
            while (mostrador.Operando && Time.time < fin)
            {
                yield return null;
                Vigilar();
                alfaMaxima = Mathf.Max(alfaMaxima, PantallaFundido.Actual.Alfa);
                PantallaSubtitulos sub = Object.FindFirstObjectByType<PantallaSubtitulos>();
                if (sub != null && sub.SubtituloVisible && sub.Subtitulo == config.TextoSubtitulo) vioSubtitulo = true;
            }
            float transcurrido = Time.time - t0;
            Assert.IsFalse(mostrador.Operando, "terminó la operación");
            Assert.AreEqual(DineroAlto - piernas.Precio, cartera.Dinero, "se cobró el precio de PIERNAS I");
            Assert.AreEqual(1, motivoClinica, "un solo cobro con MotivoDinero.Clinica");
            Assert.AreEqual(-piernas.Precio, cobrado, "el cobro fue de -precio");
            Assert.AreEqual(1, implantes.Nivel(EspacioImplante.Piernas), "PIERNAS nivel 1");
            Assert.AreEqual(piernas.Nombre, implantes.Nombre(EspacioImplante.Piernas));
            Assert.IsFalse(pantalla.Visible, "la pantalla se cerró");
            Assert.GreaterOrEqual(alfaMaxima, 1f - ToleranciaAlfa, "el fundido llegó a negro");
            Assert.AreEqual(0f, PantallaFundido.Actual.Alfa, ToleranciaAlfa, "al despertar vuelve la imagen");
            Assert.IsTrue(vioSubtitulo, "el doc dijo su línea");

            float delta = Normalizar(reloj.Hora - hora0);
            float esperado = config.HorasOperacion + HorasPorSegundoDePrueba * transcurrido;
            Debug.Log("[T-193] precio " + piernas.Precio + ", horas de operación " + F(config.HorasOperacion) + ", hora " + F(hora0) + " -> " + F(reloj.Hora)
                + " (delta " + F(delta) + ", esperado " + F(esperado) + "), fundido " + F(config.SegundosFundidoNegro) + " s, operación real " + F(transcurrido) + " s");
            Assert.AreEqual(esperado, delta, ToleranciaHora, "pasaron 2 horas (más lo que corre el reloj de prueba)");

            Assert.IsTrue(hud.GrupoAvisoMision.activeSelf, "el HUD muestra el aviso");
            Assert.AreEqual("IMPLANTE INSTALADO: " + piernas.Nombre, hud.TextoAvisoMision.text);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(controlador.ControlActivo, "el control a pie vuelve");
            VerificarFinal("instalar-piernas");
        }

        [UnityTest]
        public IEnumerator Clinica_OpcionEnGris_EnterNoHaceNada()
        {
            yield return CargarEscena();
            PonerDinero(DineroAlto);
            PonerLiga(1);
            CargarImplantes(Datos(EspacioImplante.Ojos, 1).Id);
            DatosImplante ojos2 = Datos(EspacioImplante.Ojos, 2);

            yield return AbrirClinica();
            StringAssert.Contains("SE ABRE EN LA LIGA " + ojos2.LigaMinima, pantalla.TextoOpcion(FilaOjos), "la fila de OJOS dice en qué liga se abre");
            Assert.AreEqual(config.ColorApagado, pantalla.ColorOpcion(FilaOjos), "la fila va en gris");
            StringAssert.StartsWith(config.NombreEspacio(FilaOjos) + " · " + Datos(EspacioImplante.Ojos, 1).Nombre, pantalla.TextoEspacio(FilaOjos), "lo instalado");
            yield return Tecla(teclado.enterKey);
            Assert.IsFalse(pantalla.Confirmando, "Enter en gris no pide confirmar");
            Assert.AreEqual(string.Empty, pantalla.Aviso, "no avisa");
            yield return Tecla(teclado.enterKey);
            Assert.AreEqual(DineroAlto, cartera.Dinero, "no cobra");
            Assert.AreEqual(1, implantes.Nivel(EspacioImplante.Ojos), "OJOS sigue en 1");
            Assert.IsTrue(pantalla.Visible, "la pantalla sigue abierta");
            VerificarFinal("gris");
        }

        [UnityTest]
        public IEnumerator Clinica_SinDinero_AvisaYNoCobra()
        {
            yield return CargarEscena();
            PonerLiga(1);
            foreach (int dinero in new[] { DineroJusto, DineroNegativo })
            {
                PonerDinero(dinero);
                yield return AbrirClinica();
                yield return Bajar(FilaBrazos);
                DatosImplante brazos = Datos(EspacioImplante.Brazos, 1);
                Assert.Less(dinero, brazos.Precio, "el dinero no alcanza");
                StringAssert.Contains("NO TE ALCANZA", pantalla.TextoOpcion(FilaBrazos));
                Assert.AreEqual(config.ColorAviso, pantalla.ColorOpcion(FilaBrazos), "naranja suave");
                yield return Tecla(teclado.enterKey);
                Assert.AreEqual(config.TextoAvisoSinDinero, pantalla.Aviso, "el aviso de que no alcanza");
                Assert.IsFalse(pantalla.Confirmando);
                yield return Tecla(teclado.enterKey);
                Assert.AreEqual(dinero, cartera.Dinero, "no cobra con " + dinero);
                Assert.AreEqual(0, implantes.Nivel(EspacioImplante.Brazos), "BRAZOS sigue sin implante");
                yield return Tecla(teclado.escapeKey);
                Assert.IsFalse(pantalla.Visible, "Esc sale");
                yield return Cuadros(FramesParaElHud);
            }
            VerificarFinal("sin-dinero");
        }

        [UnityTest]
        public IEnumerator Clinica_PiernasIIEnLiga3_CobraLaDiferencia()
        {
            yield return CargarEscena();
            PonerDinero(DineroAlto);
            PonerLiga(LigaAlta);
            DatosImplante p1 = Datos(EspacioImplante.Piernas, 1);
            DatosImplante p2 = Datos(EspacioImplante.Piernas, 2);
            CargarImplantes(p1.Id);
            int diferencia = p2.Precio - p1.Precio;
            Debug.Log("[T-193] PIERNAS II $" + p2.Precio + " - PIERNAS I $" + p1.Precio + " = diferencia $" + diferencia);

            yield return AbrirClinica();
            yield return Bajar(FilaPiernas);
            StringAssert.Contains(config.TextoDiferencia, pantalla.TextoOpcion(FilaPiernas), "avisa que pagas la diferencia");
            StringAssert.Contains("$" + diferencia.ToString("N0", CultureInfo.InvariantCulture), pantalla.TextoOpcion(FilaPiernas), "muestra la diferencia");
            yield return Tecla(teclado.enterKey);
            yield return Tecla(teclado.enterKey);
            yield return EsperarOperacion();
            Assert.AreEqual(DineroAlto - diferencia, cartera.Dinero, "se cobró solo la diferencia");
            Assert.AreEqual(2, implantes.Nivel(EspacioImplante.Piernas), "PIERNAS nivel 2");
            VerificarFinal("diferencia");
        }

        [UnityTest]
        public IEnumerator Clinica_Esc_SaleSinCobrarYElControlVuelveUnCuadroDespues()
        {
            yield return CargarEscena();
            PonerDinero(DineroAlto);
            PonerLiga(1);
            yield return AbrirClinica();
            yield return Bajar(FilaPiernas);
            // Esc con la confirmación abierta solo cancela la confirmación.
            yield return Tecla(teclado.enterKey);
            Assert.IsTrue(pantalla.Confirmando);
            yield return Tecla(teclado.escapeKey);
            Assert.IsFalse(pantalla.Confirmando, "Esc cancela la confirmación");
            Assert.IsTrue(pantalla.Visible, "y la pantalla sigue abierta");

            Press(teclado.escapeKey);
            yield return null;
            Vigilar();
            Release(teclado.escapeKey);
            Assert.IsFalse(pantalla.Visible, "Esc sale");
            Assert.IsFalse(controlador.ControlActivo, "el control no vuelve en el mismo cuadro");
            yield return null;
            Vigilar();
            yield return null;
            Vigilar();
            Assert.IsTrue(controlador.ControlActivo, "el control vuelve un cuadro después");
            Assert.AreEqual(DineroAlto, cartera.Dinero, "no cobró");
            Assert.AreEqual(0, implantes.Cantidad, "sin implantes");

            Vector3 antes = jugador.Raiz.position;
            Press(teclado.sKey);
            float fin = Time.time + SegundosAndando;
            while (Time.time < fin)
            {
                yield return null;
                Vigilar();
            }
            Release(teclado.sKey);
            yield return null;
            Assert.Greater(Vector3.Distance(antes, jugador.Raiz.position), MinimoAvance, "el jugador camina después de salir");
            VerificarFinal("esc");
        }

        [UnityTest]
        public IEnumerator Clinica_VistaPrevia_MuestraLaOpcionElegidaYSeQuitaAlSalir()
        {
            yield return CargarEscena();
            PonerDinero(DineroAlto);
            PonerLiga(1);
            yield return AbrirClinica();
            CuerpoJugador cuerpo = pantalla.Vista.Cuerpo;
            Assert.IsTrue(pantalla.Vista.EstaEncendida, "la vista previa está encendida");
            Assert.IsNotNull(pantalla.Vista.Textura, "la vista previa tiene imagen");
            Assert.AreEqual(1, cuerpo.NivelMostrado(EspacioImplante.Ojos), "OJOS de prueba puestos (nivel 1)");
            yield return Bajar(FilaPiernas);
            Assert.AreEqual(0, cuerpo.NivelMostrado(EspacioImplante.Ojos), "al mover la elección se quita la prueba anterior");
            Assert.AreEqual(1, cuerpo.NivelMostrado(EspacioImplante.Piernas), "PIERNAS de prueba puestas");
            Assert.AreEqual(0, implantes.Nivel(EspacioImplante.Piernas), "lo de prueba no se instala");
            yield return Tecla(teclado.escapeKey);
            Assert.IsFalse(pantalla.Vista.EstaEncendida, "al salir se apaga la vista previa");
            Assert.AreEqual(0, cuerpo.NivelMostrado(EspacioImplante.Piernas), "al salir se quita la prueba");
            VerificarFinal("vista-previa");
        }

        [UnityTest]
        public IEnumerator Clinica_ALas21_DiceCerradaYNoAbre()
        {
            yield return CargarEscena();
            reloj.AvanzarHasta(HoraNoche);
            LugarDePrueba lugar = null;
            foreach (ILugar l in Registro<ILugar>.Todos)
            {
                if (l.Id == IdLugar.Clinica) lugar = (LugarDePrueba)l;
            }
            Assert.IsNotNull(lugar, "hay lugar Clinica");
            lugar.PonerAbiertoPorHorario(lugar.Horario.EstaAbierto(reloj.Hora));
            atajos.Add("Horario aplicado al lugar de prueba (el doble no sigue el reloj solo)");
            Assert.IsFalse(lugar.EstaAbierto, "a las 21:00 está cerrada");

            yield return IrAlMostrador(config.TextoCerrado);
            StringAssert.Contains("CERRADA", hud.TextoAvisoInteraccion.text);
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsFalse(pantalla.Visible, "E no abre la clínica cerrada");
            Assert.IsTrue(controlador.ControlActivo, "el control sigue a pie");

            reloj.AvanzarHasta(HoraNoche - 11);
            lugar.PonerAbiertoPorHorario(lugar.Horario.EstaAbierto(reloj.Hora));
            yield return Cuadros(FramesParaElHud);
            yield return Tecla(teclado.eKey);
            yield return Cuadros(FramesParaElHud);
            Assert.IsTrue(pantalla.Visible, "a las 10:00 vuelve a abrir");
            VerificarFinal("cerrada");
        }

        [UnityTest]
        public IEnumerator Clinica_GuardarYCargar_VuelveLoGuardado()
        {
            yield return CargarEscena();
            string piernas1 = Datos(EspacioImplante.Piernas, 1).Id;
            CargarImplantes(piernas1);
            yield return Tecla(teclado.f5Key);
            yield return Cuadros(FramesParaElHud);
            CargarImplantes(Datos(EspacioImplante.Brazos, 1).Id, Datos(EspacioImplante.Ojos, 1).Id);
            Assert.AreEqual(0, implantes.Nivel(EspacioImplante.Piernas), "cambiado por código");
            Assert.AreEqual(1, implantes.Nivel(EspacioImplante.Brazos));
            yield return Tecla(teclado.f9Key);
            yield return Cuadros(FramesParaElHud);
            Assert.AreEqual(1, implantes.Nivel(EspacioImplante.Piernas), "F9 devuelve PIERNAS");
            Assert.AreEqual(0, implantes.Nivel(EspacioImplante.Brazos), "F9 quita BRAZOS");
            Assert.AreEqual(0, implantes.Nivel(EspacioImplante.Ojos));
            VerificarFinal("guardado");
        }

        [UnityTest]
        public IEnumerator Clinica_Captura_PantallaConFilaEnGrisConPrecioYVistaPrevia()
        {
            yield return CargarEscena();
            PonerDinero(DineroAlto);
            PonerLiga(1);
            CargarImplantes(Datos(EspacioImplante.Ojos, 1).Id);
            yield return AbrirClinica();
            yield return Bajar(FilaPiernas);
            yield return Cuadros(FramesParaElHud * 4);
            StringAssert.Contains("SE ABRE EN LA LIGA", pantalla.TextoOpcion(FilaOjos), "una fila en gris");
            StringAssert.Contains("$", pantalla.TextoOpcion(FilaPiernas), "una fila con precio");
            Assert.AreEqual(1, pantalla.Vista.Cuerpo.NivelMostrado(EspacioImplante.Piernas), "la vista previa lleva el implante probado");
            CapturarConHud(RutaCaptura1);
            VerificarFinal("captura");
        }
    }
}
