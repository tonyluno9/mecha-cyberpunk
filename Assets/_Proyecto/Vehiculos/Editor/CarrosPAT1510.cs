using System.IO;
using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-1510: las patrullas nuevas PA01 (sedán de policía, 4.8 m) y PA02 (SUV de policía, 5.0 m) y el V22 nuevo (superdeportivo, 4.6 m), partidos en Blender
    /// (imageassets/blender/_herramientas/t1510_partir_carro.py) en carrocería, cuatro llantas con el pivote en el eje, puertas con el pivote en la bisagra y cristales.
    /// Arma Carro_PA01.prefab y Carro_PA02.prefab desde cero y rehace Carro_V22.prefab en su mismo archivo (mismo GUID) con el flujo de T-1342 (ArmarUno).
    /// Después alinea la suspensión de todos los carros: el ancla de cada rueda queda a la altura para que, con el resorte en reposo, la llanta visible toque el piso
    /// (<see cref="AlturaAnclaEnReposo"/>). Correrlo otra vez deja lo mismo.
    /// Batchmode: -executeMethod MechaCyberpunk.Vehiculos.Editor.CarrosPAT1510.ArmarTodos (y después .AlinearSuspensionTodos si se cambió algún carro).
    /// </summary>
    public static class CarrosPAT1510
    {
        /// <summary>Código y largo real (m).</summary>
        public static readonly (string codigo, float largo)[] Carros = { ("PA01", 4.8f), ("PA02", 5.0f), ("V22", 4.6f) };

        public const string RutaInforme = "Logs/t1510-carros.txt";
        public const string RutaInformeSuspension = "Logs/t1510-suspension.txt";
        public const float Gravedad = 9.81f;
        private const float ToleranciaAjuste = 0.001f;

        public static string RutaPrefab(string codigo) => CreadorCarrosT511.CarpetaPrefabs + "Carro_" + codigo + ".prefab";

        [MenuItem("Mecha Cyberpunk/T-1510/Armar PA01, PA02 y V22")]
        public static void ArmarTodos()
        {
            AssetDatabase.Refresh();
            StringBuilder informe = new StringBuilder();
            foreach (var c in Carros)
            {
                CreadorCarrosBlenderT1312.ConfigurarImportador(c.codigo);
            }
            AssetDatabase.Refresh();
            foreach (var c in Carros)
            {
                informe.AppendLine(CreadorCarrosT1342.ArmarUno(c.codigo, c.largo, informe));
                // ArmarUno parte del CarroViejo base: cada carro vuelve a su tipo de manejo (PA01 sedán, PA02 camioneta, V22 sedán como antes).
                informe.AppendLine(CrearPerfilesManejoT1488.AsignarAPrefab(RutaPrefab(c.codigo)));
            }
            informe.AppendLine(AlinearSuspensionDe(Carros.Select(c => RutaPrefab(c.codigo)).ToArray()));
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInforme, informe.ToString());
            Debug.Log("[Vehiculos] T-1510:\n" + informe);
        }

        // ------------------------------------------------------------------ suspensión

        /// <summary>
        /// Altura (m, sobre el origen del carro) a la que debe ir el ancla de una rueda para que la llanta visible toque el piso cuando el resorte está en reposo.
        /// El rayo de <see cref="CarroViejo"/> sale <c>LargoReposo</c> arriba del ancla y mide una compresión de <c>LargoMaximo + RadioRueda - distancia</c>; el carro se queda donde la
        /// fuerza del resorte aguanta su cuarta parte del peso (compresión = masa * g / (4 * rigidez)). De ahí, ancla sobre el piso = LargoMaximo + RadioRueda - LargoReposo - compresión.
        /// </summary>
        public static float AlturaAnclaEnReposo(ConfigCarro config)
        {
            float compresion = config.Rigidez > 0f ? config.Masa * Gravedad / (4f * config.Rigidez) : 0f;
            return config.LargoMaximo + config.RadioRueda - config.LargoReposo - compresion;
        }

        /// <summary>Y local (sobre el origen del carro) de lo más bajo de la llanta visible de un ancla, o NaN si no tiene modelo.</summary>
        public static float FondoDeLlanta(Transform raiz, string nombreAncla)
        {
            Transform ancla = raiz.Find(nombreAncla);
            if (ancla == null || ancla.GetComponentInChildren<MeshFilter>(true) == null)
            {
                return float.NaN;
            }
            Bounds b = CreadorCarrosT511.LimitesExactos(ancla.gameObject);
            return raiz.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z)).y;
        }

        public static ConfigCarro ConfigDe(GameObject raiz)
        {
            CarroViejo carro = raiz.GetComponent<CarroViejo>();
            return carro == null ? null : new SerializedObject(carro).FindProperty("config").objectReferenceValue as ConfigCarro;
        }

        [MenuItem("Mecha Cyberpunk/T-1510/Alinear suspensión de todos los carros")]
        public static void AlinearSuspensionTodos()
        {
            string[] rutas = Directory.GetFiles(CreadorCarrosT511.CarpetaPrefabs, "Carro_*.prefab").Select(r => r.Replace('\\', '/')).ToArray();
            string informe = AlinearSuspensionDe(rutas);
            AssetDatabase.SaveAssets();
            Debug.Log("[Vehiculos] T-1510 suspensión:\n" + informe);
        }

        /// <summary>Alinea los anclajes de cada prefab (carros de cuatro ruedas con CarroViejo y cadena Direccion/Giro/Modelo). Escribe Logs/t1510-suspension.txt y devuelve el informe.</summary>
        public static string AlinearSuspensionDe(string[] rutas)
        {
            StringBuilder informe = new StringBuilder();
            foreach (string ruta in rutas)
            {
                informe.AppendLine(AlinearSuspension(ruta));
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RutaInformeSuspension, informe.ToString());
            return informe.ToString();
        }

        /// <summary>Sube o baja las cuatro anclas Rueda_xx de un prefab a <see cref="AlturaAnclaEnReposo"/> sobre el fondo de su llanta, sin mover la llanta que se ve. Devuelve una línea de informe.</summary>
        public static string AlinearSuspension(string ruta)
        {
            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                string nombre = Path.GetFileNameWithoutExtension(ruta);
                ConfigCarro config = ConfigDe(raiz);
                if (config == null || raiz.GetComponent<ManejoMoto>() != null)
                {
                    return nombre + "\tsin CarroViejo de cuatro ruedas: se salta";
                }
                float altura = AlturaAnclaEnReposo(config);
                StringBuilder linea = new StringBuilder(nombre + "\tancla sobre el piso " + altura.ToString("F3") + " m;");
                bool cambio = false;
                foreach (string rueda in CreadorCarrosPiezasT742.NombresRuedas)
                {
                    float fondo = FondoDeLlanta(raiz.transform, rueda);
                    Transform ancla = raiz.transform.Find(rueda);
                    Transform direccion = ancla != null ? ancla.Find("Direccion") : null;
                    if (float.IsNaN(fondo) || direccion == null)
                    {
                        linea.Append(" ").Append(rueda).Append(" sin llanta;");
                        continue;
                    }
                    float deseada = fondo + altura;
                    float antes = ancla.localPosition.y;
                    if (Mathf.Abs(antes - deseada) > ToleranciaAjuste)
                    {
                        Vector3 centroDeLlanta = direccion.position;
                        ancla.localPosition = new Vector3(ancla.localPosition.x, deseada, ancla.localPosition.z);
                        direccion.position = centroDeLlanta;
                        cambio = true;
                    }
                    linea.Append(" ").Append(rueda).Append(" ").Append(antes.ToString("F3")).Append("->").Append(deseada.ToString("F3")).Append(";");
                }
                if (cambio)
                {
                    PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                }
                return linea.ToString();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
