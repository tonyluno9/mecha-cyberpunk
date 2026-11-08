using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-890 (docs/tareas-piezas-08nov.md, B3): la entrada de la casa con la fachada P11 y la reja PU03, y el consultorio del Doc con la
    /// fachada P12 y la puerta PU04; las dos abren con E. Idempotente: cada fachada vive en su raíz <see cref="RaizCasa"/> / <see cref="RaizDoc"/>.
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.ColocarS2pPuertas.Colocar
    /// </summary>
    public static class ColocarS2pPuertas
    {
        public const string RaizCasa = "S2p_Fachada_P11";
        public const string RaizDoc = "S2p_Fachada_P12";
        public const string RutaHojaCasa = "Barrio/Manzana_4/Lugar_Casa/Puerta/Hoja";

        // P11: mide 4 m con un hueco de 0.9: de ancho tapa un poco a cada lado del vano de 3 m y de alto deja el vano en 2.13 m. Los escalones
        // (0.55 m) se hunden bajo la banqueta: el piso de la casa está al nivel de la calle.
        private const float AnchoP11 = 4.4f;
        private const float AltoVanoP11 = 2.13f;
        private const float PlanoP11 = -56.93f; // 2 cm frente al muro de la casa
        private const float SueloCasa = 0.22f;
        private const float CentroCasaZ = -77f;
        private const float MedioVanoCasa = 1.5f;

        // P12: cierra el frente abierto del consultorio, entre las dos pilastras (x -103.55 a -96.45), hasta tapar el dintel (y 3.8).
        private static readonly Vector3 CentroDoc = new Vector3(-100f, 0.22f, -9.95f);
        private const float AnchoDoc = 7.1f;
        private const float AltoDoc = 3.58f;

        private const float GruesoHoja = 0.08f;
        private const float GruesoJamba = 0.3f;
        private const float AltoJamba = 3.3f;
        private const float HundidaDetrasDelMuro = 0.1f;

        [MenuItem("Mecha Cyberpunk/S2p/1 Fachadas P11 y P12 con puerta")]
        public static void Colocar()
        {
            ColocarS2.Informe.Clear();
            ColocarS2.Abrir();
            Casa();
            Doc();
            ColocarS2.Guardar("s2p-puertas");
        }

        private static void Casa()
        {
            Borrar(RaizCasa);
            Transform hojaCasa = ColocarS2.Buscar(RutaHojaCasa);
            if (hojaCasa == null)
            {
                ColocarS2.Informe.AppendLine("FALTA " + RutaHojaCasa);
                return;
            }
            KitS2c.Medida m = KitS2c.Medir("P11_Fachada");
            Rect v = m.Vano.Value;
            Vector3 normal = Vector3.right;
            Quaternion giro = Quaternion.LookRotation(-normal);
            float ey = AltoVanoP11 / v.height;
            float ex = AnchoP11 / m.Caja.size.x;
            Vector3 vanoAbajo = new Vector3(PlanoP11, SueloCasa, CentroCasaZ);
            GameObject raiz = new GameObject(RaizCasa);
            Fachada(raiz.transform, "P11_Fachada", m, vanoAbajo, giro, ex, ey);

            // La hoja de siempre (la que ya abre con E) se va al vano nuevo, con la reja como modelo.
            float ancho = v.width * ex, alto = v.height * ey;
            hojaCasa.position = vanoAbajo - normal * HundidaDetrasDelMuro + Vector3.up * (alto * 0.5f);
            hojaCasa.localScale = new Vector3(ancho, alto, GruesoHoja);
            PonerModelo(hojaCasa, "PU03_Reja", giro, ancho, alto);
            Jambas(raiz.transform, vanoAbajo, Vector3.forward, ancho, MedioVanoCasa, -normal, vanoAbajo);
            ColocarS2.Informe.AppendLine("P11 en " + vanoAbajo + ": vano " + ancho.ToString("F2") + " x " + alto.ToString("F2"));
        }

        private static void Doc()
        {
            ColocarS2Interiores.RehacerConsultorio();
            ColocarS2tDoc.Colocar(); // RehacerConsultorio esconde los muros de fuera que T-864 prende
            ColocarS2nSillaClinica.Colocar();
            ColocarS2.Abrir();
            Borrar(RaizDoc);
            KitS2c.Medida m = KitS2c.Medir("P12_Fachada");
            Rect v = m.Vano.Value;
            Vector3 normal = Vector3.forward;
            Quaternion giro = Quaternion.LookRotation(-normal);
            float ex = AnchoDoc / m.Caja.size.x;
            float ey = AltoDoc / m.Caja.size.y;
            float ancho = v.width * ex, alto = v.height * ey;
            // Con la fachada centrada entre las pilastras, el vano cae donde lo diga el modelo.
            Vector3 lado = giro * Vector3.right;
            Vector3 vanoAbajo = CentroDoc + lado * (v.center.x * ex);
            GameObject raiz = new GameObject(RaizDoc);
            Fachada(raiz.transform, "P12_Fachada", m, vanoAbajo, giro, ex, ey);

            GameObject puertaGo = new GameObject("Puerta");
            puertaGo.transform.SetParent(raiz.transform, false);
            puertaGo.transform.position = vanoAbajo;
            Transform umbral = new GameObject("Umbral").transform;
            umbral.SetParent(puertaGo.transform, false);
            umbral.localPosition = Vector3.up * 1.1f;
            Transform hoja = new GameObject("Hoja").transform;
            hoja.SetParent(puertaGo.transform, false);
            hoja.position = vanoAbajo - normal * HundidaDetrasDelMuro + Vector3.up * (alto * 0.5f);
            hoja.rotation = giro;
            hoja.localScale = new Vector3(ancho, alto, GruesoHoja);
            BoxCollider caja = hoja.gameObject.AddComponent<BoxCollider>();
            caja.size = Vector3.one;
            HojaPuerta hp = hoja.gameObject.AddComponent<HojaPuerta>();
            ColocarS2nPuertas.Bisagra(hp, new Vector3(0.5f, 0f, 0f), umbral, caja, new Vector3(1.8f, 2.2f, 2.4f));
            PuertaSinHorario puerta = puertaGo.AddComponent<PuertaSinHorario>();
            SerializedObject so = new SerializedObject(puerta);
            so.FindProperty("lugar").intValue = (int)IdLugar.Clinica;
            so.FindProperty("hoja").objectReferenceValue = hp;
            so.ApplyModifiedPropertiesWithoutUndo();
            PonerModelo(hoja, "PU04_Metalica", giro, ancho, alto);
            Jambas(raiz.transform, vanoAbajo, Vector3.right, ancho, AnchoDoc * 0.5f, -normal, CentroDoc);
            ColocarS2.Informe.AppendLine("P12 en " + vanoAbajo + ": vano " + ancho.ToString("F2") + " x " + alto.ToString("F2"));
        }

        private static void Borrar(string nombre)
        {
            Transform t = ColocarS2.Buscar(nombre);
            if (t != null)
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }

        /// <summary>Pone la fachada de modo que el centro inferior de su vano quede en <paramref name="vanoAbajo"/> (sobre el plano de su muro).</summary>
        private static void Fachada(Transform padre, string codigo, KitS2c.Medida m, Vector3 vanoAbajo, Quaternion giro, float ex, float ey)
        {
            Rect v = m.Vano.Value;
            Vector3 anclaLocal = new Vector3(v.center.x * ex, v.yMin * ey, m.Muro);
            KitS2c.Pieza(padre, codigo, vanoAbajo - giro * anclaLocal, giro, new Vector3(ex, ey, 1f), false);
        }

        /// <summary>Pone el modelo de la puerta como hijo de la hoja (que está escalada) y apaga el cubo de la hoja.</summary>
        private static void PonerModelo(Transform hoja, string codigo, Quaternion giro, float ancho, float alto)
        {
            Transform viejo = hoja.Find("Modelo");
            if (viejo != null)
            {
                Object.DestroyImmediate(viejo.gameObject);
            }
            KitS2c.Medida m = KitS2c.Medir(codigo);
            GameObject p = (GameObject)PrefabUtility.InstantiatePrefab(KitS2c.Prefab(codigo), hoja);
            p.name = "Modelo";
            p.transform.rotation = giro;
            Vector3 esc = hoja.lossyScale;
            p.transform.localScale = new Vector3(ancho / m.Caja.size.x / esc.x, alto / m.Caja.size.y / esc.y, KitS2c.GruesoPuerta / m.Caja.size.z / esc.z);
            p.transform.position = hoja.position - Vector3.up * (alto * 0.5f);
            foreach (Collider c in p.GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }
            MeshRenderer cubo = hoja.GetComponent<MeshRenderer>();
            if (cubo != null)
            {
                cubo.enabled = false;
            }
        }

        /// <summary>Dos cajas que cierran lo que queda a cada lado del vano, hasta el medio ancho del frente (medido desde su centro).</summary>
        private static void Jambas(Transform padre, Vector3 vanoAbajo, Vector3 eje, float anchoVano, float medioFrente, Vector3 haciaDentro, Vector3 centro)
        {
            float vanoEnEje = Vector3.Dot(vanoAbajo - centro, eje);
            foreach (float s in new[] { -1f, 1f })
            {
                float a = vanoEnEje + s * anchoVano * 0.5f;
                float b = s * medioFrente;
                if ((b - a) * s <= 0f)
                {
                    continue;
                }
                GameObject j = new GameObject(s < 0f ? "Jamba_A" : "Jamba_B");
                j.transform.SetParent(padre, false);
                j.transform.position = centro + eje * ((a + b) * 0.5f) + Vector3.up * (AltoJamba * 0.5f) + haciaDentro * (GruesoJamba * 0.5f);
                j.transform.rotation = Quaternion.LookRotation(haciaDentro);
                j.AddComponent<BoxCollider>().size = new Vector3(Mathf.Abs(b - a), AltoJamba, GruesoJamba);
            }
        }

        /// <summary>Las dos puertas de cerca, de día y de noche, cerradas.</summary>
        public static void Fotos()
        {
            ColocarS2.Abrir();
            Vector3 casa = new Vector3(PlanoP11, SueloCasa, CentroCasaZ);
            ColocarS2cLook.Foto(casa + new Vector3(7f, 1.6f, 0f), casa + Vector3.up * 1.6f, "docs/capturas/s2p-b3-p11-noche.png", 62f);
            ColocarS2t.FotoHora(13f, casa + new Vector3(7f, 1.6f, 2.5f), casa + Vector3.up * 1.6f, "docs/capturas/s2p-b3-p11-dia.png", 62f);
            ColocarS2cLook.Foto(CentroDoc + new Vector3(0f, 1.6f, 7f), CentroDoc + Vector3.up * 1.6f, "docs/capturas/s2p-b3-p12-noche.png", 62f);
            ColocarS2t.FotoHora(13f, CentroDoc + new Vector3(-3f, 1.6f, 7f), CentroDoc + Vector3.up * 1.6f, "docs/capturas/s2p-b3-p12-dia.png", 62f);
        }

        /// <summary>Rehace el kit y las calles que lo usan, y vuelve a poner la luz y los postes de T-880 y T-881.</summary>
        public static void RehacerCalles()
        {
            ColocarS2cKit.Colocar();
            ColocarS2cCalle.Colocar();
            ColocarS2cSuburbios.Colocar();
            ColocarS2cMercado.Colocar();
            ColocarS2cNeonAlto.Colocar();
            ColocarS2cNova.Colocar();
            ColocarS2oLuz.Aplicar();
            ColocarS2oPostes.Colocar();
        }

        /// <summary>Batchmode: lado a lado con K01, K05 y K06 (quedan en docs/capturas/s2c-*; la tarea las copia a s2p-*).</summary>
        public static void Comparar()
        {
            ColocarS2cCalle.Fotos(90);
            ColocarS2cKit.Fotos();
        }

        /// <summary>Batchmode de T-891: importa las piezas de calle, rehace las calles y saca las comparaciones con K01, K05 y K06.</summary>
        public static void TodoCalle()
        {
            ImportarS2p.ImportarCalle();
            RehacerCalles();
            Comparar();
        }

        /// <summary>Batchmode de toda la tarea: calles, fachadas de P11 y P12, y fotos.</summary>
        public static void Todo()
        {
            RehacerCalles();
            Colocar();
            Fotos();
        }
    }
}
