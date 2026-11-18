using System;
using System.IO;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-1365 (bugs del mapa, 18-nov C): uniones entre las zonas nuevas y los barrios viejos. <see cref="Fotos"/> toma una captura de cada
    /// unión desde la calle (a 1.7 m, como quien camina) y desde arriba, para comparar antes y después de cada arreglo
    /// (docs/capturas/bugs-18nov/union-&lt;nombre&gt;-&lt;etapa&gt;.png). La etapa sale del argumento -etapa (antes/despues). No guarda la escena.
    /// </summary>
    public static class ColocarT1365Uniones
    {
        private const string Carpeta = "docs/capturas/bugs-18nov/";

        /// <summary>Uniones del plano (docs/plan-zonas-18nov.md): nombre, punto de la cámara a pie y punto al que mira.</summary>
        public static readonly (string nombre, Vector3 desde, Vector3 hacia)[] Vistas =
        {
            ("U1-sur-oeste", new Vector3(-47f, 1.7f, -185f), new Vector3(-47f, 0.5f, -230f)),
            ("U2-sur-este", new Vector3(47f, 1.7f, -185f), new Vector3(47f, 0.5f, -230f)),
            ("U3-malecon-cerro", new Vector3(-270f, 1.7f, -222f), new Vector3(-318f, 1f, -160f)),
            ("U4-oeste-cerro", new Vector3(-235f, 1.7f, 2f), new Vector3(-305f, 1f, 0f)),
            ("U5-malecon-puerto", new Vector3(120f, 1.7f, -232f), new Vector3(165f, 1f, -212f)),
            ("U6-boca-tunel", new Vector3(-290f, 11.7f, 250f), new Vector3(-330f, 11f, 250f)),
            ("U6-salida-tunel", new Vector3(-360f, 11.7f, 160f), new Vector3(-360f, 11f, 190f)),
            ("U7-rampa-norte", new Vector3(-95f, 1.7f, 196f), new Vector3(-120f, 2f, 222f)),
            ("U8-rampa-este", new Vector3(300f, 1.7f, 40f), new Vector3(300f, 6f, 150f)),
            ("U8-puerta-puerto", new Vector3(300f, 1.7f, -20f), new Vector3(300f, 1f, -60f)),
            ("U9-conector-este", new Vector3(240f, 1.7f, 2f), new Vector3(300f, 1f, 0f)),
        };

        public const string NombreRaiz = "T1365_Uniones";

        /// <summary>Lo que sobresale el piso de respaldo bajo el nivel de calle (m): no se ve donde hay piso y no hay z-fighting.</summary>
        public const float BajoCalle = 0.05f;

        /// <summary>
        /// Franjas sin piso que dejó apagar los edificios de fondo de T530 (barrido T-1360: "hueco" y "vacio"): x0, z0, x1, z1 en planta.
        /// Norte: alrededor de la calle cruzada norte y del enlace a la rampa (U7). Sur: entre la cruzada sur y las fachadas del Malecón (U1, U2).
        /// Este: de la calle cruzada este al límite del mundo (rampa este, U8, U9). Puerto norte: franja entre las afueras este y la reja del Puerto.
        /// </summary>
        public static readonly (string nombre, Vector4 rect)[] PisosRespaldo =
        {
            ("Norte", new Vector4(-125f, 160f, 125f, 206f)),
            ("Sur", new Vector4(-115f, -212f, 115f, -180f)),
            ("Este", new Vector4(280f, -48f, 345f, 290f)),
            ("PuertoNorte", new Vector4(115f, -48f, 280f, -28f)),
        };

        /// <summary>Pone los pisos de respaldo (asfalto con collider) bajo la raíz T1365_Uniones. Idempotente.</summary>
        public static void Aplicar()
        {
            ColocarS2.Abrir();
            KitS2c.Obra o = KitS2c.Empezar(NombreRaiz, 1365);
            Material asfalto = KitS2c.MatAsfalto();
            foreach ((string nombre, Vector4 r) in PisosRespaldo)
            {
                Vector3 tam = new Vector3(r.z - r.x, 1f, r.w - r.y);
                Vector3 centro = new Vector3((r.x + r.z) * 0.5f, -BajoCalle - tam.y * 0.5f, (r.y + r.w) * 0.5f);
                GameObject losa = KitS2c.Losa(o, o.Raiz, "PisoRespaldo_" + nombre, centro, Quaternion.identity, tam, asfalto, 4f, true);
                UnityEditor.GameObjectUtility.SetStaticEditorFlags(losa, UnityEditor.StaticEditorFlags.BatchingStatic | UnityEditor.StaticEditorFlags.OccludeeStatic);
            }
            KitS2c.Terminar(o);
            ColocarS2.Informe.Clear();
            ColocarS2.Informe.AppendLine("T-1365: " + PisosRespaldo.Length + " pisos de respaldo a " + BajoCalle + " m bajo la calle");
            ColocarS2.Guardar("t1365-uniones");
        }

        public static void Fotos()
        {
            string etapa = "antes";
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-etapa");
            if (i >= 0 && i + 1 < args.Length)
            {
                etapa = args[i + 1];
            }
            ColocarS2.Abrir();
            Directory.CreateDirectory(Carpeta);
            foreach ((string nombre, Vector3 desde, Vector3 hacia) in Vistas)
            {
                ColocarS2cLook.Foto(desde, hacia, Carpeta + "union-" + nombre + "-" + etapa + ".png", 70f);
                Vector3 arriba = Vector3.Lerp(desde, hacia, 0.5f) + new Vector3(0f, 28f, 0f) - (hacia - desde).normalized * 12f;
                ColocarS2cLook.Foto(arriba, Vector3.Lerp(desde, hacia, 0.6f), Carpeta + "union-" + nombre + "-" + etapa + "-arriba.png", 60f);
            }
        }
    }
}
