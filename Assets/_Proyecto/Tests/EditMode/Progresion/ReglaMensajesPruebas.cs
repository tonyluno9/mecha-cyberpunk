using System.Collections.Generic;
using MechaCyberpunk.Progresion;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-449: qué mensajes del teléfono le tocan al jugador según cómo va y que cada uno llega una sola vez.</summary>
    public class ReglaMensajesPruebas
    {
        [Test]
        public void Pendientes_AlPrincipio_NoLeTocaNada()
        {
            Assert.IsEmpty(ReglaMensajes.Pendientes(default, new HashSet<string>()));
        }

        [Test]
        public void Pendientes_ConLaHistiaTerminada_LlegaElDelInventor()
        {
            List<string> p = ReglaMensajes.Pendientes(new ReglaMensajes.Contexto { Libre = true }, new HashSet<string>());
            CollectionAssert.AreEqual(new[] { ReglaMensajes.IdLibre }, p);
        }

        [Test]
        public void Pendientes_CadaMensajeConSuCondicion()
        {
            Assert.IsTrue(ReglaMensajes.LeToca(ReglaMensajes.IdGuarida, new ReglaMensajes.Contexto { Libre = true, Dinero = ReglaMensajes.DineroGuarida }));
            Assert.IsFalse(ReglaMensajes.LeToca(ReglaMensajes.IdGuarida, new ReglaMensajes.Contexto { Libre = false, Dinero = 9999 }), "Antes de ser libre no");
            Assert.IsFalse(ReglaMensajes.LeToca(ReglaMensajes.IdGuarida, new ReglaMensajes.Contexto { Libre = true, Dinero = ReglaMensajes.DineroGuarida - 1 }));
            Assert.IsTrue(ReglaMensajes.LeToca(ReglaMensajes.IdImplantes, new ReglaMensajes.Contexto { Dinero = ReglaMensajes.DineroImplantes }));
            Assert.IsFalse(ReglaMensajes.LeToca(ReglaMensajes.IdImplantes, new ReglaMensajes.Contexto { Dinero = ReglaMensajes.DineroImplantes - 1 }));
            Assert.IsTrue(ReglaMensajes.LeToca(ReglaMensajes.IdZona, new ReglaMensajes.Contexto { ZonasTuyas = ReglaMensajes.ZonasParaAvisar }));
            Assert.IsFalse(ReglaMensajes.LeToca(ReglaMensajes.IdZona, new ReglaMensajes.Contexto { ZonasTuyas = 1 }));
            Assert.IsTrue(ReglaMensajes.LeToca(ReglaMensajes.IdCalor, new ReglaMensajes.Contexto { NivelCalor = ReglaMensajes.NivelCalorParaAvisar }));
            Assert.IsFalse(ReglaMensajes.LeToca(ReglaMensajes.IdCalor, new ReglaMensajes.Contexto { NivelCalor = 1 }));
            Assert.IsTrue(ReglaMensajes.LeToca(ReglaMensajes.IdContratos, new ReglaMensajes.Contexto { Libre = true, Dia = ReglaMensajes.DiaContratos }));
            Assert.IsFalse(ReglaMensajes.LeToca(ReglaMensajes.IdContratos, new ReglaMensajes.Contexto { Libre = true, Dia = 1 }));
            Assert.IsFalse(ReglaMensajes.LeToca("no_existe", new ReglaMensajes.Contexto { Libre = true, Dinero = 99999, ZonasTuyas = 9, NivelCalor = 5, Dia = 99 }));
        }

        [Test]
        public void Pendientes_NoRepiteLosYaEntregados_YRespetaElOrdenDelCatalogo()
        {
            ReglaMensajes.Contexto todo = new ReglaMensajes.Contexto { Libre = true, Dinero = 5000, ZonasTuyas = 3, NivelCalor = 3, Dia = 5 };
            List<string> primeros = ReglaMensajes.Pendientes(todo, new HashSet<string>());
            Assert.AreEqual(ReglaMensajes.Todos.Count, primeros.Count, "Con todo cumplido llegan todos");
            Assert.AreEqual(ReglaMensajes.Todos[0].Id, primeros[0]);
            HashSet<string> entregados = new HashSet<string>(primeros);
            Assert.IsEmpty(ReglaMensajes.Pendientes(todo, entregados), "Ninguno llega dos veces");
            entregados.Remove(ReglaMensajes.IdCalor);
            CollectionAssert.AreEqual(new[] { ReglaMensajes.IdCalor }, ReglaMensajes.Pendientes(todo, entregados));
        }

        [Test]
        public void Catalogo_TodosTienenRemitenteTextoYUnIdUnico()
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (ReglaMensajes.Mensaje m in ReglaMensajes.Todos)
            {
                Assert.IsNotEmpty(m.Id);
                Assert.IsNotEmpty(m.Remitente, m.Id);
                Assert.IsNotEmpty(m.Texto, m.Id);
                Assert.IsTrue(ids.Add(m.Id), "Id repetido: " + m.Id);
                Assert.IsTrue(ReglaMensajes.Buscar(m.Id, out ReglaMensajes.Mensaje otro));
                Assert.AreEqual(m.Texto, otro.Texto);
            }
            Assert.IsFalse(ReglaMensajes.Buscar("no_existe", out _));
        }
    }
}
