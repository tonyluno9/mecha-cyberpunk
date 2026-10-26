using System;
using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Editor
{
    /// <summary>
    /// Crea las 11 misiones del bloque y su catálogo (T-183, D-tony-148). Idempotente: un asset que ya existe no se toca,
    /// salvo con <see cref="RecrearTodo"/> (paso explícito).
    /// </summary>
    public static class CreadorMisiones
    {
        public const string Carpeta = "Assets/_Proyecto/Progresion/Datos/Misiones";
        public const string RutaCatalogo = Carpeta + "/CatalogoMisiones_Base.asset";
        private const string RutaChatarra = "Assets/_Proyecto/Economia/Datos/DatosObjeto_Chatarra.asset";
        private const string CarpetaFavores = "Assets/_Proyecto/Progresion/Datos/";

        [MenuItem("Mecha Cyberpunk/Crear T-183 misiones")]
        public static void Crear() => Ejecutar(false);

        /// <summary>Paso explícito: vuelve a escribir las misiones y el catálogo aunque ya existan.</summary>
        [MenuItem("Mecha Cyberpunk/Recrear T-183 misiones (pisa los assets)")]
        public static void RecrearTodo() => Ejecutar(true);

        private static DatosFavor Favor(string nombre) => AssetDatabase.LoadAssetAtPath<DatosFavor>(CarpetaFavores + "DatosFavor_" + nombre + ".asset");

        private static void Ejecutar(bool sobrescribir)
        {
            if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/_Proyecto/Progresion/Datos", "Misiones");
            var chatarra = AssetDatabase.LoadAssetAtPath<DatosObjeto>(RutaChatarra);
            var descuento = Favor("Descuento");
            var escondite = Favor("Escondite");
            var informacion = Favor("Informacion");
            if (chatarra == null || descuento == null || escondite == null || informacion == null)
            {
                Debug.LogError("[Progresion] CreadorMisiones: faltan la chatarra o los favores.");
                return;
            }

            var tabla = new List<DatosMision>
            {
                DatosMision.Crear("barrio_tiendita", "LA TIENDITA DE DOÑA LUPE", TipoMision.Barrio, OrigenMision.Calle, FranjaMision.Cualquiera,
                    new[]
                    {
                        new PasoMision(TipoPaso.Recoger, "Mision_Botin_Farmacia", "Recoge el pedido en la farmacia"),
                        new PasoMision(TipoPaso.Entregar, "Mision_Encargo_DonaLupe", "Llévaselo a Doña Lupe"),
                    },
                    pago: 50, deltaBarrio: 8, idEncargo: "Mision_Encargo_DonaLupe", favor: descuento,
                    descripcion: "Doña Lupe necesita unas medicinas de la farmacia y nadie le hace el paro."),
                DatosMision.Crear("barrio_callejon", "LOS DEL CALLEJÓN", TipoMision.Barrio, OrigenMision.Calle, FranjaMision.Noche,
                    new[]
                    {
                        new PasoMision(TipoPaso.Pelear, "Mision_Pelea_Callejon", "Saca a los del callejón"),
                        new PasoMision(TipoPaso.IrA, "Mision_Encargo_Taquero", "Avísale al taquero que ya se puede"),
                    },
                    pago: 100, deltaBarrio: 12, idEncargo: "Mision_Encargo_Taquero", favor: escondite,
                    descripcion: "Unos tipos le espantan la clientela al taquero por las noches."),
                DatosMision.Crear("barrio_luz", "LUZ PARA LA VECINDAD", TipoMision.Barrio, OrigenMision.Calle, FranjaMision.Cualquiera,
                    new[]
                    {
                        new PasoMision(TipoPaso.Recoger, "Mision_Botin_Subestacion", "Roba el fusible de la subestación", calor: 30f),
                        new PasoMision(TipoPaso.Entregar, "Mision_Encargo_Vecindad", "Llévalo a la vecindad"),
                    },
                    pago: 0, deltaBarrio: 15, deltaCorporaciones: -8, idEncargo: "Mision_Encargo_Vecindad", favor: informacion,
                    nivelBarrioMinimo: 1, descripcion: "La vecindad lleva días sin luz: hay que quitarle una pieza a la corporación."),
                DatosMision.Crear("corp_entrega", "ENTREGA URGENTE", TipoMision.Corporativa, OrigenMision.Telefono, FranjaMision.Dia,
                    new[]
                    {
                        new PasoMision(TipoPaso.Recoger, "Mision_Botin_Paqueteria", "Recoge el paquete en la paquetería"),
                        new PasoMision(TipoPaso.Entregar, "Mision_Destino_TorreCorp", "Entrégalo en la torre corporativa"),
                    },
                    pago: 400, deltaBarrio: -5, deltaCorporaciones: 8, limiteSegundos: 150f,
                    descripcion: "Un paquete que la corporación quiere en su torre antes de que cierre el día."),
                DatosMision.Crear("corp_cobro", "COBRO DE ADEUDO", TipoMision.Corporativa, OrigenMision.Telefono, FranjaMision.Dia,
                    new[]
                    {
                        new PasoMision(TipoPaso.Recoger, "Mision_Destino_Moroso", "Cobra al vecino"),
                        new PasoMision(TipoPaso.Entregar, "Mision_Destino_TorreCorp", "Lleva el dinero a la torre corporativa"),
                    },
                    pago: 500, deltaBarrio: -12, deltaCorporaciones: 10,
                    descripcion: "Un vecino debe dinero a la corporación y tú vas a cobrarlo."),
                DatosMision.Crear("corp_limpieza", "LIMPIEZA DE ZONA", TipoMision.Corporativa, OrigenMision.Telefono, FranjaMision.Dia,
                    new[] { new PasoMision(TipoPaso.Pelear, "Mision_Pelea_Baldio", "Despeja el baldío") },
                    pago: 900, deltaBarrio: -15, deltaCorporaciones: 15, nivelCorporacionesMinimo: 1, ligaMinima: 2,
                    descripcion: "La corporación quiere el baldío libre de gente, sin preguntas."),
                DatosMision.Crear("ilegal_carga", "CARGA CALIENTE", TipoMision.Ilegal, OrigenMision.Bar, FranjaMision.Noche,
                    new[]
                    {
                        new PasoMision(TipoPaso.Entregar, "Mision_Destino_Muelle", "Entrega la carga en el muelle", calor: 40f),
                        new PasoMision(TipoPaso.Escapar, "", "Piérdete antes de que te alcancen"),
                    },
                    pago: 350, deltaCorporaciones: -5,
                    descripcion: "Te dan un paquete al aceptar; llévalo al muelle sin que te sigan."),
                DatosMision.Crear("ilegal_pelea", "PELEA SIN JAULA", TipoMision.Ilegal, OrigenMision.Bar, FranjaMision.Noche,
                    new[]
                    {
                        new PasoMision(TipoPaso.Pelear, "Mision_Pelea_Baldio", "Gana la pelea del baldío"),
                        new PasoMision(TipoPaso.Escapar, "", "Largo de ahí antes de que lleguen las patrullas"),
                    },
                    pago: 450, deltaBarrio: 3, deltaCorporaciones: -5,
                    descripcion: "Una pelea sin reglas ni jaula, con apuestas y sin permiso."),
                DatosMision.Crear("robo_bodega", "BODEGA DE REFACCIONES", TipoMision.Robo, OrigenMision.Bar, FranjaMision.Noche,
                    new[]
                    {
                        new PasoMision(TipoPaso.Recoger, "Mision_Botin_BodegaCorp", "Saca las refacciones de la bodega", calor: 50f),
                        new PasoMision(TipoPaso.Escapar, "", "Piérdete con la mercancía"),
                    },
                    pago: 0, deltaBarrio: 3, deltaCorporaciones: -10, chatarraObjeto: chatarra, chatarraCantidad: 6,
                    descripcion: "Una bodega corporativa llena de refacciones que luego se venden como chatarra."),
                DatosMision.Crear("robo_camion", "CAMIÓN BLINDADO", TipoMision.Robo, OrigenMision.Bar, FranjaMision.Noche,
                    new[]
                    {
                        new PasoMision(TipoPaso.Pelear, "Mision_Pelea_Camion", "Detén al escolta del camión"),
                        new PasoMision(TipoPaso.Recoger, "Mision_Botin_Camion", "Abre el camión y saca la carga", calor: 60f),
                        new PasoMision(TipoPaso.Escapar, "", "Piérdete antes de que lleguen los refuerzos"),
                    },
                    pago: 200, deltaBarrio: 5, deltaCorporaciones: -15, chatarraObjeto: chatarra, chatarraCantidad: 10, ligaMinima: 2,
                    descripcion: "Un camión blindado con escolta lleva partes de mecha de lujo."),
                DatosMision.Crear("robo_soplo", "EL SOPLO", TipoMision.Robo, OrigenMision.Bar, FranjaMision.Noche,
                    new[]
                    {
                        new PasoMision(TipoPaso.Recoger, "Mision_Botin_BodegaCorp", "Entra por la puerta que te soplaron", calor: 15f),
                        new PasoMision(TipoPaso.Escapar, "", "Sal sin que te vean"),
                    },
                    pago: 0, deltaBarrio: 3, deltaCorporaciones: -8, chatarraObjeto: chatarra, chatarraCantidad: 12,
                    exigeFavor: true, favorRequerido: TipoFavor.Informacion,
                    descripcion: "Alguien te sopla cómo entrar a la bodega sin alarmas, si tienes un favor de información."),
            };

            var existentes = new List<DatosMision>();
            foreach (DatosMision m in tabla)
            {
                string ruta = Carpeta + "/DatosMision_" + m.Id + ".asset";
                var actual = AssetDatabase.LoadAssetAtPath<DatosMision>(ruta);
                if (actual != null && !sobrescribir)
                {
                    existentes.Add(actual);
                    UnityEngine.Object.DestroyImmediate(m);
                    continue;
                }
                if (actual != null)
                {
                    EditorUtility.CopySerialized(m, actual);
                    EditorUtility.SetDirty(actual);
                    existentes.Add(actual);
                    UnityEngine.Object.DestroyImmediate(m);
                }
                else
                {
                    AssetDatabase.CreateAsset(m, ruta);
                    existentes.Add(m);
                }
            }

            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoMisiones>(RutaCatalogo);
            if (catalogo == null)
            {
                catalogo = ScriptableObject.CreateInstance<CatalogoMisiones>();
                AssetDatabase.CreateAsset(catalogo, RutaCatalogo);
                sobrescribir = true;
            }
            if (sobrescribir)
            {
                var so = new SerializedObject(catalogo);
                SerializedProperty lista = so.FindProperty("misiones");
                lista.arraySize = existentes.Count;
                for (int i = 0; i < existentes.Count; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = existentes[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalogo);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Progresion] CreadorMisiones: " + existentes.Count + " misiones.");
        }

        /// <summary>Lista de todos los ids de punto que usan las misiones, con su tipo (para docs/revision-tony.md).</summary>
        [MenuItem("Mecha Cyberpunk/Imprimir ids de puntos de misión")]
        public static string ImprimirIdsDePuntos()
        {
            var ids = new SortedSet<string> { "Mision_Escondite_Azotea", "Mision_Escondite_Lavanderia" };
            var catalogo = AssetDatabase.LoadAssetAtPath<CatalogoMisiones>(RutaCatalogo);
            if (catalogo != null)
            {
                foreach (DatosMision m in catalogo.Misiones)
                {
                    if (m == null) continue;
                    if (!string.IsNullOrEmpty(m.IdEncargo)) ids.Add(m.IdEncargo);
                    foreach (PasoMision p in m.Pasos)
                    {
                        if (!string.IsNullOrEmpty(p.IdPunto)) ids.Add(p.IdPunto);
                    }
                }
            }
            var sb = new StringBuilder();
            foreach (string id in ids) sb.AppendLine(id + " (" + ReglaMisiones.TipoDePunto(id) + ")");
            Debug.Log("[Progresion] Ids de puntos de misión:\n" + sb);
            return sb.ToString();
        }

        /// <summary>Entrada para -executeMethod en batchmode.</summary>
        public static void CrearBatch()
        {
            Crear();
            ImprimirIdsDePuntos();
        }
    }
}
