using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>T-470: cada ID usado en el código tiene archivo, cada archivo está en el catálogo y los AMB van en streaming.</summary>
    public class IdsSonidoPruebas
    {
        private static readonly string[] Secciones = { "JUG", "PUN", "ARM", "VEH", "PAN", "CAL", "ZON", "ECO", "UI", "MUN", "AMB", "MUS" };
        private static readonly Regex Literal = new Regex("\"((?:JUG|PUN|ARM|VEH|PAN|CAL|ZON|ECO|UI|MUN|AMB|MUS)_[A-Z0-9_]+)\"");
        private static readonly Regex Variante = new Regex(@"^(.+?)(_\d\d)?$");

        private static SortedSet<string> IdsConArchivo(bool incluirProvisionales)
        {
            SortedSet<string> ids = new SortedSet<string>();
            foreach (string s in Secciones)
            {
                AgregarCarpeta("Assets/Arte/Audio/" + s, ids);
                if (incluirProvisionales) AgregarCarpeta("Assets/Arte/Audio/Provisional/" + s, ids);
            }
            return ids;
        }

        private static void AgregarCarpeta(string carpeta, SortedSet<string> ids)
        {
            if (!Directory.Exists(carpeta)) return;
            foreach (string f in Directory.GetFiles(carpeta, "*.wav"))
            {
                ids.Add(Variante.Match(Path.GetFileNameWithoutExtension(f)).Groups[1].Value);
            }
        }

        /// <summary>IDs entre comillas en el código del juego (sin pruebas ni editor) -> archivos donde aparecen.</summary>
        private static SortedDictionary<string, List<string>> IdsEnCodigo()
        {
            SortedDictionary<string, List<string>> usados = new SortedDictionary<string, List<string>>();
            foreach (string f in Directory.GetFiles("Assets/_Proyecto", "*.cs", SearchOption.AllDirectories))
            {
                string n = f.Replace('\\', '/');
                if (n.Contains("/Tests/") || n.Contains("/Editor/")) continue;
                foreach (Match m in Literal.Matches(File.ReadAllText(f)))
                {
                    string id = m.Groups[1].Value;
                    if (!usados.TryGetValue(id, out List<string> donde)) usados[id] = donde = new List<string>();
                    if (!donde.Contains(n)) donde.Add(n);
                }
            }
            return usados;
        }

        [Test]
        public void CadaIdDelCodigoTieneArchivo_YListaLosQueNoTienenGancho()
        {
            SortedSet<string> conFinal = IdsConArchivo(false);
            SortedSet<string> conAlgo = IdsConArchivo(true);
            SortedDictionary<string, List<string>> usados = IdsEnCodigo();

            StringBuilder informe = new StringBuilder();
            informe.AppendLine("IDs usados en el codigo: " + usados.Count + "; IDs con archivo final: " + conFinal.Count);
            List<string> sinArchivo = usados.Keys.Where(id => !conAlgo.Contains(id)).ToList();
            foreach (string id in usados.Keys)
            {
                string estado = conFinal.Contains(id) ? "FINAL       " : conAlgo.Contains(id) ? "PROVISIONAL " : "SIN ARCHIVO ";
                informe.AppendLine(estado + id + "  <- " + string.Join(", ", usados[id].Select(Path.GetFileName)));
            }
            informe.AppendLine("--- Con archivo final pero sin gancho en el codigo ---");
            foreach (string id in conFinal.Where(id => !usados.ContainsKey(id)))
            {
                informe.AppendLine(id);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/sonidos-ids.txt", informe.ToString());
            Debug.Log(informe.ToString());

            Assert.IsEmpty(sinArchivo, "IDs usados en el codigo sin ningun archivo: " + string.Join(", ", sinArchivo));
        }

        [Test]
        public void ElCatalogoTieneTodosLosIdsConArchivo()
        {
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>("Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset");
            Assert.IsNotNull(catalogo, "Falta el catalogo; corre Mecha Cyberpunk/Sonido/Crear catalogo.");
            HashSet<string> enCatalogo = new HashSet<string>(catalogo.Entradas.Where(e => e.finales.Length > 0).Select(e => e.id));
            List<string> faltan = IdsConArchivo(false).Where(id => !enCatalogo.Contains(id)).ToList();
            Assert.IsEmpty(faltan, "IDs con archivo que no estan en el catalogo: " + string.Join(", ", faltan));
        }

        [Test]
        public void LosAmbientesVanEnStreaming_YLosEfectosEnMono()
        {
            int amb = 0, efectos = 0;
            foreach (string s in Secciones)
            {
                string carpeta = "Assets/Arte/Audio/" + s;
                if (!Directory.Exists(carpeta)) continue;
                foreach (string f in Directory.GetFiles(carpeta, "*.wav"))
                {
                    AudioImporter imp = (AudioImporter)AssetImporter.GetAtPath(f.Replace('\\', '/'));
                    Assert.AreEqual(AudioCompressionFormat.Vorbis, imp.defaultSampleSettings.compressionFormat, f);
                    Assert.AreEqual(0.7f, imp.defaultSampleSettings.quality, 0.001f, f);
                    if (s == "AMB")
                    {
                        Assert.AreEqual(AudioClipLoadType.Streaming, imp.defaultSampleSettings.loadType, f);
                        amb++;
                    }
                    else
                    {
                        Assert.IsTrue(imp.forceToMono, f);
                        efectos++;
                    }
                }
            }
            Assert.Greater(amb, 0);
            Assert.Greater(efectos, 0);
        }

        [Test]
        public void ReglaAmbienteZona_PorLugarZonaYHora()
        {
            Assert.AreEqual("AMB_CALLE_NOCHE", ReglaAmbienteZona.Elegir(IdLugar.Calle, "fosa", true));
            Assert.AreEqual("AMB_CALLE_DIA", ReglaAmbienteZona.Elegir(IdLugar.Calle, "", false));
            Assert.AreEqual("AMB_TIANGUIS", ReglaAmbienteZona.Elegir(IdLugar.Calle, "tianguis", true));
            Assert.AreEqual("AMB_NEON_ALTO", ReglaAmbienteZona.Elegir(IdLugar.Calle, "neon", false));
            Assert.AreEqual("AMB_INTERIOR_CASA", ReglaAmbienteZona.Elegir(IdLugar.Casa, "neon", true));
            Assert.AreEqual("AMB_MERCADO", ReglaAmbienteZona.Elegir(IdLugar.MercadoNegro, "tianguis", true));
            Assert.AreEqual("AMB_CLANDESTINO", ReglaAmbienteZona.Elegir(IdLugar.Clandestino, "", true));
        }
    }
}
