using System.Linq;
using System.Text;
using MechaCyberpunk.Vehiculos.Carro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Vehiculos.Editor
{
    /// <summary>
    /// T-1342: las piezas que se mueven o se ven a través del carro (puertas, cofre y cristales) a partir de los hijos del FBX de Blender.
    /// Lo usa <see cref="CreadorCarrosT1342"/> y lo puede usar cualquier otro creador de carros cuyo FBX traiga <c>Cofre</c>, <c>Puerta_I</c>, <c>Puerta_D</c> y <c>Cristal_*</c>.
    /// </summary>
    public static class PiezasCarroT1342
    {
        public const string CarpetaMateriales = "Assets/Arte/Vehiculos/Materiales";
        public const string RutaCristal = CarpetaMateriales + "/CristalCarro.mat";
        public const string PrefijoCristal = "Cristal_";
        public const string NombreCristalPuertaI = "Cristal_Puerta_I";
        public const string NombreCristalPuertaD = "Cristal_Puerta_D";

        private static readonly Color ColorCristal = new Color(0.08f, 0.12f, 0.16f, 0.35f);
        private const float SuavidadCristal = 0.92f;

        /// <summary>Crea o actualiza el material del cristal: URP Lit transparente, oscuro y brillante.</summary>
        public static Material CristalCarro()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaMateriales))
            {
                AssetDatabase.CreateFolder("Assets/Arte/Vehiculos", "Materiales");
            }
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(RutaCristal);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, RutaCristal);
            }
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", ColorCristal);
            mat.SetFloat("_Smoothness", SuavidadCristal);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssetIfDirty(mat);
            return mat;
        }

        /// <summary>
        /// Mete en <paramref name="visual"/> (el Visual_T511 ya armado, hijo de la raíz del carro y con la Carroceria del FBX) los cristales fijos,
        /// las puertas con su bisagra, el cofre con su bisagra, y deja en la raíz el componente <see cref="PuertasCarro"/> con las tres bisagras.
        /// Las piezas son copias sin vínculo al FBX, en el mismo lugar que la Carroceria. Si una pieza falta en el FBX no se crea su bisagra y se anota en <paramref name="informe"/>.
        /// Correrlo otra vez deja lo mismo (borra antes lo que puso). La raíz es el padre de <paramref name="visual"/>; los ejes de cada bisagra son los de la raíz.
        /// </summary>
        public static void ArmarPiezasMoviles(GameObject fbx, Transform visual, Material carroceria, StringBuilder informe)
        {
            Transform raiz = visual.parent;
            string codigo = raiz != null ? raiz.name : visual.name;
            LimpiarAnteriores(visual);
            Material cristal = CristalCarro();
            StringBuilder faltan = new StringBuilder();

            // Cristales fijos (los de puerta van con su puerta).
            foreach (Transform hijo in fbx.transform.Cast<Transform>().ToList())
            {
                if (hijo.name.StartsWith(PrefijoCristal) && hijo.name != NombreCristalPuertaI && hijo.name != NombreCristalPuertaD && hijo.GetComponent<MeshFilter>() != null)
                {
                    PonerCristal(hijo, visual, cristal);
                }
            }

            // Piezas fijas que no son ni carrocería, ni ruedas, ni piezas móviles (Interior, Interior_Casco, Motor): se ven por el cristal y bajo el cofre.
            foreach (Transform hijo in fbx.transform.Cast<Transform>().ToList())
            {
                if (EsPiezaFija(hijo.name) && hijo.GetComponent<MeshFilter>() != null)
                {
                    CreadorCarrosBlenderT1312.Pieza(hijo, PrefijoFija + hijo.name, visual, carroceria);
                }
            }

            Transform bisagraI = ArmarPuerta(fbx, visual, raiz, carroceria, cristal, "Puerta_I", NombreCristalPuertaI, "Bisagra_Puerta_I", faltan);
            Transform bisagraD = ArmarPuerta(fbx, visual, raiz, carroceria, cristal, "Puerta_D", NombreCristalPuertaD, "Bisagra_Puerta_D", faltan);
            Transform bisagraCofre = ArmarCofre(fbx, visual, raiz, carroceria, faltan);

            PuertasCarro puertas = raiz.GetComponent<PuertasCarro>();
            if (puertas == null)
            {
                puertas = raiz.gameObject.AddComponent<PuertasCarro>();
            }
            SerializedObject so = new SerializedObject(puertas);
            Asignar(so, "bisagraPuertaIzquierda", bisagraI);
            Asignar(so, "bisagraPuertaDerecha", bisagraD);
            Asignar(so, "bisagraCofre", bisagraCofre);
            so.ApplyModifiedPropertiesWithoutUndo();
            if (informe != null && faltan.Length > 0)
            {
                informe.AppendLine(codigo + "\tpiezas que faltan en el FBX: " + faltan.ToString().TrimEnd(' ', ','));
            }
        }

        /// <summary>Prefijo de las piezas fijas copiadas del FBX (Interior, Motor…), para poder borrarlas al volver a armar.</summary>
        public const string PrefijoFija = "Fija_";

        private static readonly string[] NombresConocidos = { "Carroceria", "Cuerpo", "Cofre", "Puerta_I", "Puerta_D" };

        /// <summary>Hijo del FBX que no es carrocería, rueda, cristal ni pieza móvil.</summary>
        public static bool EsPiezaFija(string nombre)
        {
            return !NombresConocidos.Contains(nombre) && !nombre.StartsWith("Rueda_") && !nombre.StartsWith(PrefijoCristal);
        }

        private static void Asignar(SerializedObject so, string campo, Transform valor)
        {
            SerializedProperty p = so.FindProperty(campo);
            if (p != null)
            {
                p.objectReferenceValue = valor;
            }
        }

        private static void LimpiarAnteriores(Transform visual)
        {
            foreach (Transform t in visual.GetComponentsInChildren<Transform>(true).ToList())
            {
                if (t == null || t == visual)
                {
                    continue;
                }
                bool mia = t.name.StartsWith("Bisagra_") || t.name.StartsWith(PrefijoCristal) || t.name.StartsWith(PrefijoFija);
                if (mia && t.gameObject != null)
                {
                    Object.DestroyImmediate(t.gameObject);
                }
            }
            // Puertas y cofre que quedaron sin bisagra (si la bisagra se borró con ellos ya no existen).
            foreach (string nombre in new[] { "Puerta_I", "Puerta_D", "Cofre" })
            {
                Transform viejo = visual.Find(nombre);
                if (viejo != null)
                {
                    Object.DestroyImmediate(viejo.gameObject);
                }
            }
        }

        private static GameObject PonerCristal(Transform origen, Transform padre, Material cristal)
        {
            GameObject pieza = CreadorCarrosBlenderT1312.Pieza(origen, origen.name, padre, cristal);
            pieza.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return pieza;
        }

        private static Transform ArmarPuerta(GameObject fbx, Transform visual, Transform raiz, Material carroceria, Material cristal,
            string nombrePuerta, string nombreCristal, string nombreBisagra, StringBuilder faltan)
        {
            Transform origen = fbx.transform.Find(nombrePuerta);
            if (origen == null || origen.GetComponent<MeshFilter>() == null)
            {
                faltan.Append(nombrePuerta).Append(", ");
                return null;
            }
            GameObject puerta = CreadorCarrosBlenderT1312.Pieza(origen, nombrePuerta, visual, carroceria);
            Bounds b = CreadorCarrosBlenderT1312.LimitesEn(puerta, raiz);
            // Bisagra en el borde delantero (mayor Z de la raíz), en la cara exterior (el x más lejano al centro) y a media altura.
            float x = Mathf.Abs(b.min.x) > Mathf.Abs(b.max.x) ? b.min.x : b.max.x;
            Transform bisagra = CrearBisagra(nombreBisagra, visual, raiz, new Vector3(x, b.center.y, b.max.z));
            puerta.transform.SetParent(bisagra, true);
            // El cristal de la puerta viene anidado dentro de la puerta en el FBX: se busca en todo el árbol y se pone en su lugar respecto a la raíz del FBX.
            Transform origenCristal = fbx.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == nombreCristal);
            if (origenCristal != null && origenCristal.GetComponent<MeshFilter>() != null)
            {
                GameObject c = PonerCristal(origenCristal, visual, cristal);
                Matrix4x4 enRaizFbx = fbx.transform.worldToLocalMatrix * origenCristal.localToWorldMatrix;
                c.transform.localPosition = enRaizFbx.GetColumn(3);
                c.transform.localRotation = enRaizFbx.rotation;
                c.transform.localScale = enRaizFbx.lossyScale;
                c.transform.SetParent(puerta.transform, true);
            }
            else
            {
                faltan.Append(nombreCristal).Append(", ");
            }
            return bisagra;
        }

        private static Transform ArmarCofre(GameObject fbx, Transform visual, Transform raiz, Material carroceria, StringBuilder faltan)
        {
            Transform origen = fbx.transform.Find("Cofre");
            if (origen == null || origen.GetComponent<MeshFilter>() == null)
            {
                faltan.Append("Cofre, ");
                return null;
            }
            GameObject cofre = CreadorCarrosBlenderT1312.Pieza(origen, "Cofre", visual, carroceria);
            Bounds b = CreadorCarrosBlenderT1312.LimitesEn(cofre, raiz);
            // Bisagra en el borde trasero del cofre (menor Z, junto al parabrisas), arriba y al centro.
            Transform bisagra = CrearBisagra("Bisagra_Cofre", visual, raiz, new Vector3(b.center.x, b.max.y, b.min.z));
            cofre.transform.SetParent(bisagra, true);
            return bisagra;
        }

        // Vacío hijo de visual, en un punto dado en el marco de la raíz, con los ejes de la raíz (escala 1 local).
        private static Transform CrearBisagra(string nombre, Transform visual, Transform raiz, Vector3 puntoEnRaiz)
        {
            GameObject g = new GameObject(nombre);
            g.transform.SetParent(visual, false);
            g.transform.position = raiz.TransformPoint(puntoEnRaiz);
            g.transform.rotation = raiz.rotation;
            return g.transform;
        }
    }
}
