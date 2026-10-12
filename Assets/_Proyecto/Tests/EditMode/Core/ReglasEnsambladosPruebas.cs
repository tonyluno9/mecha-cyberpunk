using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace MechaCyberpunk.Tests.Core
{
    /// <summary>
    /// Vigila que ningún ensamblado referencie a uno de la otra persona y que Core no dependa de ningún área.
    /// </summary>
    public class ReglasEnsambladosPruebas
    {
        [Serializable]
        private class DatosAsmdef
        {
            public string name;
            public string[] references;
        }

        private const string Prefijo = "MechaCyberpunk.";
        private const string DuenoCore = "Core";
        private const string DuenoTony = "Tony";
        private const string DuenoAlan = "Alan";

        // Tabla de dueños: la misma de CLAUDE.md (D-028). Un área nueva se agrega aquí a propósito.
        private static readonly Dictionary<string, string> DuenoPorArea = new Dictionary<string, string>
        {
            { "Mecha", DuenoTony }, { "Combate", DuenoTony }, { "UI", DuenoTony }, { "IA", DuenoTony },
            { "Pelea", DuenoTony }, { "Personaje", DuenoTony }, { "Economia", DuenoTony },
            { "Progresion", DuenoTony }, { "Radio", DuenoTony },
            { "Arena", DuenoAlan }, { "Mundo", DuenoAlan }, { "Vehiculos", DuenoAlan },
            { "Casino", DuenoAlan }, { "Trafico", DuenoAlan },
        };

        /// <summary>Dueño de un ensamblado por su nombre, o null si el nombre no está en la tabla.</summary>
        private static string DuenoDe(string nombre)
        {
            if (nombre == Prefijo + "Core") return DuenoCore;
            string resto;
            if (nombre.StartsWith(Prefijo + "Tests.EditMode.")) resto = nombre.Substring((Prefijo + "Tests.EditMode.").Length);
            else if (nombre.StartsWith(Prefijo + "Tests.PlayMode.")) resto = nombre.Substring((Prefijo + "Tests.PlayMode.").Length);
            else if (nombre.StartsWith(Prefijo)) resto = nombre.Substring(Prefijo.Length);
            else return null;

            if (resto.EndsWith(".Editor")) resto = resto.Substring(0, resto.Length - ".Editor".Length);
            if (resto == DuenoCore) return DuenoCore;
            return DuenoPorArea.TryGetValue(resto, out var dueno) ? dueno : null;
        }

        /// <summary>Devuelve los errores de la lista de ensamblados del proyecto (vacía si todo está bien).</summary>
        private static List<string> Validar(List<DatosAsmdef> asmdefs)
        {
            var errores = new List<string>();
            var nombres = new HashSet<string>();

            foreach (var asm in asmdefs)
            {
                nombres.Add(asm.name);
                string dueno = DuenoDe(asm.name);
                if (dueno == null)
                {
                    errores.Add($"{asm.name}: nombre desconocido (agrégalo a la tabla de dueños a propósito)");
                    continue;
                }

                foreach (var referencia in asm.references ?? new string[0])
                {
                    if (referencia.StartsWith("GUID:"))
                    {
                        errores.Add($"{asm.name}: referencia por GUID ({referencia}); usa nombres");
                        continue;
                    }
                    if (!referencia.StartsWith(Prefijo)) continue;

                    string duenoReferencia = DuenoDe(referencia);
                    if (asm.name == Prefijo + "Core")
                        errores.Add($"{asm.name} (Core) referencia a {referencia}: Core no depende de nada del proyecto");
                    else if (duenoReferencia != null && duenoReferencia != DuenoCore && duenoReferencia != dueno)
                        errores.Add($"{asm.name} ({dueno}) referencia a {referencia} ({duenoReferencia}): usen un contrato de Core");
                }
            }

            foreach (var area in DuenoPorArea.Keys)
            {
                if (!nombres.Contains(Prefijo + area))
                    errores.Add($"falta el asmdef {Prefijo}{area}");
            }
            return errores;
        }

        private static DatosAsmdef Asm(string nombre, params string[] referencias) =>
            new DatosAsmdef { name = nombre, references = referencias };

        private static List<DatosAsmdef> ProyectoCompleto()
        {
            var lista = new List<DatosAsmdef> { Asm("MechaCyberpunk.Core") };
            foreach (var area in DuenoPorArea.Keys) lista.Add(Asm(Prefijo + area, "MechaCyberpunk.Core"));
            return lista;
        }

        private static List<DatosAsmdef> LeerProyecto()
        {
            string raiz = Path.Combine(Application.dataPath, "_Proyecto");
            var lista = new List<DatosAsmdef>();
            foreach (var ruta in Directory.GetFiles(raiz, "*.asmdef", SearchOption.AllDirectories))
                lista.Add(JsonUtility.FromJson<DatosAsmdef>(File.ReadAllText(ruta)));
            return lista;
        }

        [Test]
        public void ProyectoReal_NoTieneErrores()
        {
            var errores = Validar(LeerProyecto());

            Assert.IsEmpty(errores, string.Join("\n", errores));
        }

        [Test]
        public void ProyectoInventadoCorrecto_NoTieneErrores()
        {
            Assert.IsEmpty(Validar(ProyectoCompleto()));
        }

        [Test]
        public void IAReferenciaMundo_DaErrorQueNombraALosDos()
        {
            var lista = ProyectoCompleto();
            lista.Find(a => a.name == "MechaCyberpunk.IA").references = new[] { "MechaCyberpunk.Core", "MechaCyberpunk.Mundo" };

            var errores = Validar(lista);

            Assert.AreEqual(1, errores.Count);
            StringAssert.Contains("MechaCyberpunk.IA", errores[0]);
            StringAssert.Contains("MechaCyberpunk.Mundo", errores[0]);
        }

        [Test]
        public void CoreReferenciaMecha_DaError()
        {
            var lista = ProyectoCompleto();
            lista.Find(a => a.name == "MechaCyberpunk.Core").references = new[] { "MechaCyberpunk.Mecha" };

            Assert.AreEqual(1, Validar(lista).Count);
        }

        [Test]
        public void PruebasDeTonyReferencianAreaDeAlan_DaError()
        {
            var lista = ProyectoCompleto();
            lista.Add(Asm("MechaCyberpunk.Tests.PlayMode.Mecha", "MechaCyberpunk.Core", "MechaCyberpunk.Casino"));

            Assert.AreEqual(1, Validar(lista).Count);
        }

        [Test]
        public void NombreDesconocido_DaError()
        {
            var lista = ProyectoCompleto();
            lista.Add(Asm("MechaCyberpunk.Inventada", "MechaCyberpunk.Core"));

            var errores = Validar(lista);

            Assert.AreEqual(1, errores.Count);
            StringAssert.Contains("MechaCyberpunk.Inventada", errores[0]);
        }

        [Test]
        public void FaltaElAsmdefDeUnArea_DaError()
        {
            var lista = ProyectoCompleto();
            lista.RemoveAll(a => a.name == "MechaCyberpunk.Radio");

            var errores = Validar(lista);

            Assert.AreEqual(1, errores.Count);
            StringAssert.Contains("MechaCyberpunk.Radio", errores[0]);
        }

        [Test]
        public void ReferenciaPorGuid_DaError()
        {
            var lista = ProyectoCompleto();
            lista.Find(a => a.name == "MechaCyberpunk.Mecha").references = new[] { "GUID:0123456789abcdef0123456789abcdef" };

            Assert.AreEqual(1, Validar(lista).Count);
        }
    }
}
