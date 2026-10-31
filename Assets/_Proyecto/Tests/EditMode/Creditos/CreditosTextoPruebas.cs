using System.Collections.Generic;
using System.Linq;
using MechaCyberpunk.UI.Creditos;
using NUnit.Framework;

namespace MechaCyberpunk.Tests
{
    public class CreditosTextoPruebas
    {
        [Test]
        public void Cargar_Recurso_TieneLasSeccionesYLosNombres()
        {
            List<LineaCredito> lineas = CreditosTexto.Parsear(CreditosTexto.Cargar());
            string[] secciones = lineas.Where(l => l.Tipo == TipoLineaCredito.Seccion).Select(l => l.Texto).ToArray();
            foreach (string esperada in new[] { "EQUIPO", "PROFES", "HERRAMIENTAS", "LICENCIAS DE MODELOS 3D", "LICENCIAS DE SONIDOS" })
            {
                CollectionAssert.Contains(secciones, esperada);
            }
            string[] listas = lineas.Where(l => l.Tipo == TipoLineaCredito.Lista).Select(l => l.Texto).ToArray();
            foreach (string nombre in new[] { "Tony", "Alan", "Cesar", "Miranda", "Mirflur", "Castillo", "El Doc: JOEL", "El Director: ESTRADA" })
            {
                CollectionAssert.Contains(listas, nombre);
            }
            Assert.AreEqual(TipoLineaCredito.Titulo, lineas[0].Tipo);
        }

        [Test]
        public void Parsear_TodosLosTipos_LosReconoce()
        {
            List<LineaCredito> l = CreditosTexto.Parsear("# A\n## B\n### C\n- D\n\nE");
            Assert.AreEqual(
                new[] { TipoLineaCredito.Titulo, TipoLineaCredito.Seccion, TipoLineaCredito.Subseccion, TipoLineaCredito.Lista, TipoLineaCredito.Espacio, TipoLineaCredito.Texto },
                l.Select(x => x.Tipo).ToArray());
        }
    }
}
