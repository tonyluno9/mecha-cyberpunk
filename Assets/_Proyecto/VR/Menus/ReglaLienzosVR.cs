using System;
using UnityEngine;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR4, lógica pura: qué tipo de panel es cada Canvas de pantalla y dónde va el panel de menús frente a la cabeza.
    /// La pantalla de referencia del juego (1920 x 1080 px) mide <see cref="AnchoFrente"/> m a <see cref="DistanciaFrente"/> m.
    /// </summary>
    public static class ReglaLienzosVR
    {
        /// <summary>Ancho de referencia de los Canvas del juego (px), el de KitUI.</summary>
        public const float AnchoReferencia = 1920f;
        public const float AltoReferencia = 1080f;

        /// <summary>Ancho del panel de menús (m) a la distancia normal: unos 56° de la vista.</summary>
        public const float AnchoFrente = 1.6f;
        /// <summary>Distancia normal del panel a la cabeza (m).</summary>
        public const float DistanciaFrente = 1.5f;
        /// <summary>Lo más cerca que se pone si hay una pared enfrente (m).</summary>
        public const float DistanciaMinima = 0.6f;
        /// <summary>Separación con la pared que tapa (m): la UI no escribe profundidad y una pared más cerca la taparía.</summary>
        public const float MargenPared = 0.1f;
        /// <summary>El panel queda un poco abajo de los ojos (m).</summary>
        public const float BajadaFrente = 0.1f;
        /// <summary>Cuánto se adelanta un Canvas por cada punto de orden de dibujo (m), para que el láser y la vista respeten el orden.</summary>
        public const float AdelantoPorOrden = 0.0004f;
        public const int OrdenMaximoAdelanto = 200;

        /// <summary>Pantallas completas: pegadas a la cabeza a esta distancia (m) y de este ancho (m), más que la vista del visor.</summary>
        public const float DistanciaCompleta = 0.6f;
        public const float AnchoCompleta = 3.2f;

        /// <summary>Desde este orden de dibujo un Canvas es fundido, negro o cinemática (PantallaFundido, PantallaNegra, Cinematica). La carga no: ver <see cref="EsPanelDeCarga"/>.</summary>
        public const int OrdenPantallaCompleta = 30000;

        /// <summary>Si la cabeza se aleja tanto del panel (°), el panel la sigue.</summary>
        public const float AnguloSeguir = 35f;
        /// <summary>Con un menú abierto el panel se queda quieto; solo se recoloca si queda fuera de la vista tanto tiempo (s).</summary>
        public const float AnguloFueraDeVista = 60f;
        public const float SegundosFueraDeVista = 1f;
        /// <summary>Si el jugador se aleja del panel más que esto (m), el panel lo sigue.</summary>
        public const float DistanciaSeguir = 2.5f;
        /// <summary>Qué tan rápido sigue el panel a la cabeza sin menú (1/s).</summary>
        public const float RapidezSeguir = 3f;

        /// <summary>
        /// Los Canvas del HUD de pantalla que en VR no se ven (nombres de KitUI.CrearCanvas). Vida y dinero van en la muñeca.
        /// Casi todos van anidados en el Canvas raíz HudAPie (que sí va al frente, con avisos, tutorial y rueda): esos se encogen a 0.
        /// </summary>
        private static readonly string[] Ocultos =
        {
            "CanvasH10", "CanvasHabilidades", "CanvasImplantes", "CanvasTerritorio", "CanvasMinimapa", "CanvasHitmarker",
            "CanvasMiraSniper", "CanvasIndicadorDanio", "CanvasVinietaVida", "CanvasMarcador", "BarrasVidaEnemigos",
        };

        /// <summary>
        /// Canvas de pantalla completa que en VR van como panel a <see cref="DistanciaFrente"/> m frente a la cabeza (y quietos en el mundo),
        /// no pegados a ella: la pantalla de carga (PantallaCargando, "CanvasCargando"). Pegada a 0.6 m y más ancha que la vista marea y no se lee.
        /// </summary>
        private static readonly string[] PanelesDeCarga = { "CanvasCargando" };

        /// <summary>Tamaño en el mundo de un px de referencia (m/px) del panel de menús a la distancia normal.</summary>
        public static float EscalaFrente => AnchoFrente / AnchoReferencia;

        /// <summary>Tamaño en el mundo de un px de referencia (m/px) de una pantalla completa.</summary>
        public static float EscalaCompleta => AnchoCompleta / AnchoReferencia;

        /// <summary>True si el Canvas es la pantalla de carga (va como panel frente a la cabeza aunque su orden sea de pantalla completa).</summary>
        public static bool EsPanelDeCarga(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
            {
                return false;
            }
            foreach (string p in PanelesDeCarga)
            {
                if (nombre.StartsWith(p, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>El tipo de un Canvas de pantalla por su nombre y su orden de dibujo.</summary>
        public static TipoLienzoVR Clasificar(string nombre, int orden)
        {
            if (EsPanelDeCarga(nombre))
            {
                return TipoLienzoVR.Frente;
            }
            if (orden >= OrdenPantallaCompleta)
            {
                return TipoLienzoVR.PantallaCompleta;
            }
            if (!string.IsNullOrEmpty(nombre))
            {
                foreach (string o in Ocultos)
                {
                    if (nombre.StartsWith(o, StringComparison.Ordinal))
                    {
                        return TipoLienzoVR.Oculto;
                    }
                }
            }
            return TipoLienzoVR.Frente;
        }

        /// <summary>True si un Canvas (raíz o anidado) con este nombre es HUD de pantalla que en VR no se ve.</summary>
        public static bool EsOculto(string nombre)
        {
            return Clasificar(nombre, 0) == TipoLienzoVR.Oculto;
        }

        /// <summary>
        /// La distancia del panel: la normal, o menos si una pared está más cerca (<paramref name="distanciaPared"/> &lt; 0 = sin pared).
        /// </summary>
        public static float Distancia(float distanciaPared)
        {
            if (distanciaPared < 0f)
            {
                return DistanciaFrente;
            }
            return Mathf.Clamp(distanciaPared - MargenPared, DistanciaMinima, DistanciaFrente);
        }

        /// <summary>El frente de la cabeza en el plano (normalizado). Si mira justo arriba o abajo usa <paramref name="respaldo"/>.</summary>
        public static Vector3 FrentePlano(Vector3 frente, Vector3 respaldo)
        {
            frente.y = 0f;
            if (frente.sqrMagnitude < 1e-4f)
            {
                frente = respaldo;
                frente.y = 0f;
            }
            return frente.sqrMagnitude < 1e-4f ? Vector3.forward : frente.normalized;
        }

        /// <summary>La pose del panel de menús: frente a la cabeza, de pie (sin inclinarse) y mirando en la misma dirección que la cabeza.</summary>
        public static Pose PoseFrente(Vector3 cabeza, Vector3 frentePlano, float distancia)
        {
            Vector3 posicion = cabeza + frentePlano * distancia + Vector3.down * BajadaFrente;
            return new Pose(posicion, Quaternion.LookRotation(frentePlano, Vector3.up));
        }

        /// <summary>Cuánto se adelanta (m, hacia la cabeza) un Canvas de este orden, escalado con la distancia.</summary>
        public static float Adelanto(int orden, float factorDistancia)
        {
            return Mathf.Clamp(orden, 0, OrdenMaximoAdelanto) * AdelantoPorOrden * factorDistancia;
        }

        /// <summary>Ángulo en el plano (°) entre hacia dónde mira la cabeza y hacia dónde está el panel.</summary>
        public static float AnguloHacia(Vector3 cabeza, Vector3 frentePlano, Vector3 panel)
        {
            Vector3 hacia = panel - cabeza;
            hacia.y = 0f;
            if (hacia.sqrMagnitude < 1e-4f)
            {
                return 0f;
            }
            return Vector3.Angle(frentePlano, hacia);
        }
    }
}
