using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-594: un mensaje nuevo suena como texto (tic bajito), no como timbre, y los tics seguidos no se encimen.</summary>
    public class SonidoUiT594Pruebas
    {
        private GameObject objeto;

        [TearDown]
        public void Limpiar()
        {
            if (objeto != null) Object.Destroy(objeto);
            Registros.LimpiarTodos();
        }

        private GestorSonidos CrearGestor()
        {
            Registros.LimpiarTodos();
            CatalogoSonidos catalogo = ScriptableObject.CreateInstance<CatalogoSonidos>();
            catalogo.Fijar(new List<CatalogoSonidos.Entrada>
            {
                new CatalogoSonidos.Entrada { id = "UI_TEXTO", finales = new[] { AudioClip.Create("tic", 4410, 1, 44100, false) } },
                new CatalogoSonidos.Entrada { id = "UI_TELEFONO", finales = new[] { AudioClip.Create("timbre", 44100, 1, 44100, false) } },
            });
            objeto = new GameObject("GestorSonidos");
            GestorSonidos g = objeto.AddComponent<GestorSonidos>();
            g.Catalogo = catalogo;
            return g;
        }

        [UnityTest]
        public IEnumerator UnMensajeNuevoPideElTicDeTexto_YNoElTimbre()
        {
            GestorSonidos g = CrearGestor();
            yield return null;
            GameObject servicio = new GameObject("ServicioMensajes");
            ServicioMensajes mensajes = servicio.AddComponent<ServicioMensajes>();
            yield return null;
            Assert.IsTrue(mensajes.Entregar(ReglaMensajes.IdLibre, 1));
            Assert.AreEqual(1, g.Pedidos("UI_TEXTO"));
            Assert.AreEqual(0, g.Pedidos("UI_TELEFONO"));
            Assert.AreEqual("tic", g.UltimoClip.name);
            Assert.LessOrEqual(g.UltimoVolumenRelativo, 0.5f);
            Object.Destroy(servicio);
        }

        [UnityTest]
        public IEnumerator TicsSeguidosNoSeEncimen()
        {
            GestorSonidos g = CrearGestor();
            yield return null;
            for (int i = 0; i < 20; i++) g.Reproducir("UI_TEXTO");
            Assert.AreEqual(1, g.VocesActivas("texto"));
            Assert.AreEqual(20, g.Pedidos("UI_TEXTO"));
        }
    }
}
