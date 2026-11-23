using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-1705 (T54): reduce a la mitad los carros estacionados de Ciudad_Prologo (los que llevan <see cref="CarroEstacionado"/> y están activos). Los de tráfico (no llevan ese
    /// componente), las patrullas y motos, los CarroRobable_* (los usa la historia), los de guion y los del jugador no se tocan. La regla (duplicados, 2-3 cerca de la casa,
    /// mitad por celda de 40 m, neón V20/V21 al final) está en <see cref="ReglaMitadCarrosT1705"/>. Los carros se apagan con SetActive(false), nunca se borran.
    /// Contar: escribe docs/capturas/_tareas/T54/conteo-antes.md (la primera vez) o conteo-despues.md (si ya existe el de antes) y no cambia nada.
    /// Aplicar: cuenta (antes), apaga, guarda la escena, cuenta (después) y lista lo apagado en docs/capturas/_tareas/T54/apagados.md. Si apagados.md ya existe no repite
    /// (borra ese archivo y recupera la escena de git para repetir). Zonas de Assets/Arte/Mundo/Zonas: ninguna tiene instancias de carros estacionados en la escena.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.MitadCarrosT1705.Contar (y .Aplicar).
    /// </summary>
    public static class MitadCarrosT1705
    {
        public const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string Carpeta = "docs/capturas/_tareas/T54/";
        public const string NombreCasa = "Inicio_Calle";
        public static readonly Vector3 CasaPorOmision = new Vector3(-53f, 0f, -76.5f);
        private const float LadoZona = 120f;
        private static readonly string[] CodigosNoEstacionados = { "MT01", "MT02", "C19", "V05", "V06", "V07", "V08", "V10", "V11", "PA01", "PA02" };

        private sealed class Registro
        {
            public CarroEstacionado Carro;
            public CarroEstacionadoDato Dato;
        }

        [MenuItem("Mecha Cyberpunk/T-1705/Contar carros estacionados")]
        public static void Contar()
        {
            EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            bool primera = !File.Exists(Carpeta + "conteo-antes.md");
            Escribir(primera ? "conteo-antes.md" : "conteo-despues.md", Tabla(Reunir(), primera ? "antes" : "después"));
        }

        [MenuItem("Mecha Cyberpunk/T-1705/Aplicar mitad de carros")]
        public static void Aplicar()
        {
            if (File.Exists(Carpeta + "apagados.md"))
            {
                Debug.LogWarning("[T-1705] Ya se aplicó (existe " + Carpeta + "apagados.md). Borra ese archivo y recupera la escena de git para repetir.");
                return;
            }
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            List<Registro> antes = Reunir();
            Escribir("conteo-antes.md", Tabla(antes, "antes"));
            Vector3 casa = CasaPorOmision;
            GameObject inicio = GameObject.Find(NombreCasa);
            if (inicio != null)
            {
                casa = inicio.transform.position;
            }
            HashSet<int> quitar = ReglaMitadCarrosT1705.Quitar(antes.Select(r => r.Dato).ToList(), casa.x, casa.z);
            StringBuilder lista = new StringBuilder("# T54: carros apagados (SetActive(false), no borrados)\n\nCasa en " + casa + "; " + quitar.Count + " de " + antes.Count + ".\n\n| Objeto | Modelo | Posición |\n| --- | --- | --- |\n");
            foreach (Registro r in antes.Where(r => quitar.Contains(r.Dato.Id)))
            {
                r.Carro.gameObject.SetActive(false);
                EditorUtility.SetDirty(r.Carro.gameObject);
                lista.AppendLine("| " + Ruta(r.Carro) + " | " + r.Dato.Modelo + " | (" + r.Dato.X.ToString("0.0") + ", " + r.Dato.Z.ToString("0.0") + ") |");
            }
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Escribir("apagados.md", lista.ToString());
            Escribir("conteo-despues.md", Tabla(Reunir(), "después"));
            Debug.Log("[T-1705] Carros estacionados: " + antes.Count + " -> " + (antes.Count - quitar.Count) + ".");
        }

        private static List<Registro> Reunir()
        {
            List<Registro> lista = new List<Registro>();
            CarroEstacionado[] todos = UnityEngine.Object.FindObjectsByType<CarroEstacionado>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (CarroEstacionado c in todos.OrderBy(x => Ruta(x), StringComparer.Ordinal))
            {
                string modelo = Modelo(c.gameObject);
                bool intocable = c.EsTuyo || c.name.StartsWith("CarroRobable", StringComparison.Ordinal) || c.name.IndexOf("Patrulla", StringComparison.OrdinalIgnoreCase) >= 0
                    || c.name.IndexOf("Guion", StringComparison.OrdinalIgnoreCase) >= 0 || c.name.IndexOf("Historia", StringComparison.OrdinalIgnoreCase) >= 0
                    || Array.IndexOf(CodigosNoEstacionados, modelo) >= 0;
                Vector3 p = c.transform.position;
                lista.Add(new Registro { Carro = c, Dato = new CarroEstacionadoDato(lista.Count, modelo, p.x, p.z, intocable) });
            }
            return lista;
        }

        private static string Modelo(GameObject go)
        {
            GameObject raiz = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            UnityEngine.Object fuente = raiz != null ? PrefabUtility.GetCorrespondingObjectFromSource(raiz) : null;
            string codigo = fuente != null ? MenosCarrosT1683.CodigoDe(fuente.name) : null;
            return codigo ?? MenosCarrosT1683.CodigoDe(go.name) ?? "?";
        }

        private static string Tabla(List<Registro> carros, string cuando)
        {
            StringBuilder sb = new StringBuilder("# T54: carros estacionados " + cuando + "\n\nTotal activos: " + carros.Count + " (intocables: " + carros.Count(c => c.Dato.Intocable) + ")\n\n");
            sb.AppendLine("## Por modelo\n\n| Modelo | Carros |\n| --- | --- |");
            foreach (IGrouping<string, Registro> g in carros.GroupBy(c => c.Dato.Modelo).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                sb.AppendLine("| " + g.Key + " | " + g.Count() + " |");
            }
            sb.AppendLine("\n## Por zona (cuadros de " + LadoZona + " m: x,z del cuadro)\n\n| Zona | Carros |");
            foreach (IGrouping<string, Registro> g in carros.GroupBy(c => Mathf.FloorToInt(c.Dato.X / LadoZona) + "," + Mathf.FloorToInt(c.Dato.Z / LadoZona)).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                sb.AppendLine("| " + g.Key + " | " + g.Count() + " |");
            }
            return sb.ToString();
        }

        private static void Escribir(string archivo, string texto)
        {
            Directory.CreateDirectory(Carpeta);
            File.WriteAllText(Carpeta + archivo, texto);
        }

        private static string Ruta(Component c)
        {
            string r = c.name;
            for (Transform t = c.transform.parent; t != null; t = t.parent)
            {
                r = t.name + "/" + r;
            }
            return r;
        }
    }
}
