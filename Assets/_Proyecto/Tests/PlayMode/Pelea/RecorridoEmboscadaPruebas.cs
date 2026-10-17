using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Pruebas;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Mecha;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Pelea.Emboscada;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Cabina;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Pelea
{
    /// <summary>
    /// T-072: la emboscada de Pruebas_Calle jugada de principio a fin con teclado y ratón simulados, como lo haría una persona:
    /// camina al callejón, llama al mecha (M), sube (E), hace el encendido (F, Espacio, Q), pelea mirando y golpeando, baja y
    /// lo devuelve. Del juego solo se lee estado para decidir la siguiente entrada; ningún método del juego avanza el recorrido.
    /// </summary>
    public class RecorridoEmboscadaPruebas : RecorridoEmboscadaBase
    {
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaCaptura1 = "docs/capturas/T-101-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-101-2.png";
        private const float ZCallejon = 87f;
        private const float XBocaCallejon = -6f;
        private const float XEsperaLlamado = -7.5f;
        private const float ZEsperaLlamado = 84.5f;
        private const float XJuntoAlMecha = -9.3f;
        private const float ZJuntoAlMecha = 85f;

        private IEnumerator CargarCalle()
        {
            yield return CargarEscena(RutaCalle);
            ResolverReferencias();
            yield return null;

            Assert.Less(Vector3.Distance(new Vector3(0f, AlturaJugador, 0f), jugador.transform.position), 0.1f, "Aparece en Calle_Inicio");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "El mecha empieza en el taller");
            juntoAlMecha = new Vector3(XJuntoAlMecha, AlturaJugador, ZJuntoAlMecha);
            EmpezarMedicion();
        }

        // a) Camina al callejón hasta que la pandilla aparece, y sigue hasta la zona del punto de llamado.
        private IEnumerator CaminarAlCallejon()
        {
            Assert.AreEqual(EstadoEmboscada.Esperando, director.Estado, "Lejos del callejón no pasa nada");
            yield return CaminarA(new Vector3(XBocaCallejon, AlturaJugador, ZCallejon), DistanciaLlegada, "la boca del callejón",
                () => director.Estado != EstadoEmboscada.Esperando);
            Assert.AreNotEqual(EstadoEmboscada.Esperando, director.Estado, "Al entrar al radio la pandilla aparece");
            yield return EsperarHasta(() => director.Estado == EstadoEmboscada.EsperandoMecha, 6f, "la pandilla termina de aparecer");
            Assert.AreEqual(2, director.Rivales.Count, "Aparecen dos pandilleros");
            yield return CaminarA(new Vector3(XEsperaLlamado, AlturaJugador, ZEsperaLlamado), DistanciaLlegada, "la zona de llamado");
        }

        // ---- Pruebas ----

        [UnityTest]
        public IEnumerator Recorrido_DeLaCalleAlCallejonYLaPandillaEnKO_JugandoComoUnaPersona()
        {
            yield return CargarCalle();

            yield return CaminarAlCallejon();
            yield return LlamarYSubir();
            yield return Encender();
            Assert.AreEqual(EstadoEmboscada.Peleando, director.Estado, "Con el mecha encendido la pandilla pelea");
            yield return Pelear(RutaCaptura1, RutaCaptura2);
            yield return BajarYDevolver();

            float segundos = Time.time - inicioJuego;
            Debug.Log("[Prueba T-072] Recorrido a-f: " + segundos.ToString("F1") + " s de juego, vida mínima del mecha "
                + vidaMinima.ToString("F1") + " de " + vidaMecha.Maxima.ToString("F0") + " (piso " + piso.ToString("F1")
                + "), frame más largo " + (medidor.Maximo * 1000f).ToString("F0") + " ms, calor final " + calor.Valor.ToString("F1")
                + " (nivel " + calor.Nivel + ")");

            Assert.Less(segundos, MaxSegundosRecorrido, "El recorrido dura menos de 120 s de juego");
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "El jugador termina a pie");
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado, "El mecha termina en el taller");
            Assert.Greater(vidaMinima, 0f, "El jugador no pudo perder: la vida del mecha nunca llegó a 0");
            Assert.GreaterOrEqual(vidaMinima, piso - MargenPiso, "Nunca bajó del piso de piedad");
            Assert.Greater(calor.Valor, 0f, "Al final hay calor");
            Assert.GreaterOrEqual(calor.Nivel, 1, "Y es de nivel 1 o más");
            Assert.IsTrue(medidor.Cumple, "Frames del recorrido: " + medidor.Resumen());
            AssertSinErrores();
        }

        [UnityTest]
        public IEnumerator SinPelear_SesentaSegundosEnElMechaEncendido_NoSePuedePerder()
        {
            yield return CargarCalle();

            yield return CaminarAlCallejon();
            yield return LlamarYSubir();
            yield return Encender();
            Assert.AreEqual(EstadoEmboscada.Peleando, director.Estado, "Con el mecha encendido la pandilla pelea");

            vidaMinima = vidaMecha.Actual;
            yield return Esperar(SegundosSinPelear);
            Debug.Log("[Prueba T-072] Sin pelear " + SegundosSinPelear + " s: vida mínima del mecha " + vidaMinima.ToString("F1")
                + " de " + vidaMecha.Maxima.ToString("F0") + " (piso " + piso.ToString("F1") + "), vida final "
                + vidaMecha.Actual.ToString("F1") + ", frame más largo " + (medidor.Maximo * 1000f).ToString("F0") + " ms");

            Assert.IsFalse(vidaMecha.EstaKO, "El mecha no cayó en KO");
            Assert.Greater(vidaMinima, 0f, "La vida del mecha nunca llegó a 0");
            Assert.GreaterOrEqual(vidaMinima, piso - MargenPiso, "Nunca bajó del piso de piedad");
            Assert.AreEqual(2, RivalesEnPie(), "Los rivales siguen en pie");
            Assert.AreEqual(EstadoEmboscada.Peleando, director.Estado, "Y la emboscada sigue");
            AssertSinErrores();
        }
    }
}
