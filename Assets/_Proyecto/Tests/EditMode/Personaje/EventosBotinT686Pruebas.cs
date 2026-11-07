using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-686: el aviso de botín llega a quien lo escucha con los datos del enemigo.</summary>
    public class EventosBotinT686Pruebas
    {
        [TearDown]
        public void Limpiar() => EventosBotin.LimpiarSuscriptores();

        [Test]
        public void Publicar_EntregaPosicionDineroYArma()
        {
            InfoBotin recibido = default;
            int veces = 0;
            EventosBotin.Soltado += i => { recibido = i; veces++; };
            EventosBotin.Publicar(new InfoBotin(new Vector3(1f, 2f, 3f), 40, ArmaBotin.Escopeta));
            Assert.AreEqual(1, veces);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), recibido.Posicion);
            Assert.AreEqual(40, recibido.Dinero);
            Assert.AreEqual(ArmaBotin.Escopeta, recibido.Arma);
        }

        [Test]
        public void SinSuscriptores_PublicarNoFalla()
        {
            Assert.DoesNotThrow(() => EventosBotin.Publicar(new InfoBotin(Vector3.zero, 0, ArmaBotin.Ninguna)));
        }
    }
}
