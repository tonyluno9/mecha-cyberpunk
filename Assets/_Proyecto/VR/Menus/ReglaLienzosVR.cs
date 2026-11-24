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
        /// Los Canvas que en VR no se ven (nombres de KitUI.CrearCanvas). Si van anidados en un Canvas convertido se encogen a 0.
        /// VR14: la mira del francotirador (taparía el HUD de casco) y las barras sobre los enemigos (se colocan con la pantalla del PC, que
        /// en el visor no coincide con el mundo).
        /// </summary>
        private static readonly string[] Ocultos =
        {
            "CanvasMiraSniper", "BarrasVidaEnemigos",
            // V12: el video del logo (IntroInicio) en VR se ve en la pantalla de cine; su Canvas (fondo negro a pantalla completa) taparía el menú.
            "IntroInicio_Pantalla",
        };

        /// <summary>
        /// VR14: los Canvas raíz del HUD de pantalla de PC que en VR van al HUD de casco (<see cref="TipoLienzoVR.Casco"/>), con su diseño,
        /// colores y posiciones de PC. HudAPie lleva anidados vida, arma, objetivo, H10, habilidades, implantes, avisos, tutoriales y la rueda;
        /// los demás pueden ser raíz según la escena (minimapa, territorio, marcador del objetivo, subtítulos, aviso de guardado, asalto).
        /// </summary>
        private static readonly string[] DelCasco =
        {
            "HudAPie", "CanvasH10", "CanvasHabilidades", "CanvasImplantes", "CanvasTerritorio", "CanvasMinimapa", "CanvasHitmarker",
            "CanvasIndicadorDanio", "CanvasVinietaVida", "CanvasMarcador", "CanvasAvisos", "CanvasAvisoMensaje", "CanvasTutorial",
            "PanelSubtitulos", "Subtitulos", "CanvasAvisoGuardado", "CanvasAsalto",
        };

        /// <summary>
        /// VR14: grupos hijos directos de un Canvas del casco que en VR se encogen a 0: la mira de PC (en VR se apunta con la mano) y el aviso
        /// "E · interactuar" (en VR va el letrero con B sobre el objeto, <see cref="AvisoInteraccionVR"/>).
        /// </summary>
        private static readonly string[] GruposOcultosCasco = { "Mira", "Interaccion" };

        /// <summary>VR14: el panel del HUD de casco: distancia a la cabeza (m, radio de la curva) y ancho (°).</summary>
        public const float RadioCasco = 1.3f;
        public const float GradosCasco = 70f;
        /// <summary>Qué tan rápido gira el panel del casco tras la cabeza (1/s): un leve retraso suave.</summary>
        public const float RapidezCasco = 12f;

        /// <summary>Alto del panel del casco (m): el del arco de <see cref="GradosCasco"/> a <see cref="RadioCasco"/> con la proporción 16:9.</summary>
        public static float AltoCasco => RadioCasco * GradosCasco * Mathf.Deg2Rad * AltoReferencia / AnchoReferencia;

        /// <summary>VR14: true si este hijo directo de un Canvas del casco se encoge a 0 en VR.</summary>
        public static bool EsGrupoOcultoCasco(string nombre)
        {
            return Empieza(nombre, GruposOcultosCasco, true);
        }

        private static bool Empieza(string nombre, string[] lista, bool exacto = false)
        {
            if (string.IsNullOrEmpty(nombre))
            {
                return false;
            }
            foreach (string p in lista)
            {
                if (exacto ? nombre == p : nombre.StartsWith(p, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Canvas de pantalla completa que en VR van como panel a <see cref="DistanciaFrente"/> m frente a la cabeza (y quietos en el mundo),
        /// no pegados a ella: la pantalla de carga (PantallaCargando, "CanvasCargando"). Pegada a 0.6 m y más ancha que la vista marea y no se lee.
        /// </summary>
        // V12: también las pantallas negras de texto (cinemática sin video, "CONTINUARÁ", muerte/arresto): panel legible, no pegado a la cara.
        private static readonly string[] PanelesDeCarga = { "CanvasCargando", "CanvasCinematica", "CanvasPantallaNegra", "CanvasPantallaAviso" };

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
            if (Empieza(nombre, Ocultos))
            {
                return TipoLienzoVR.Oculto;
            }
            if (Empieza(nombre, DelCasco))
            {
                return TipoLienzoVR.Casco;
            }
            return TipoLienzoVR.Frente;
        }

        /// <summary>True si un Canvas (raíz o anidado) con este nombre es de los que en VR no se ven.</summary>
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

        /// <summary>
        /// V12: en Inicio (rig de menús, con la pantalla de cine arriba) el centro del panel va tanto abajo de los ojos (m), para que no tape
        /// la pantalla grande (<see cref="PantallaCineVR"/>).
        /// </summary>
        public const float BajadaBajoCine = 0.6f;
        // Si una pared acerca el panel, la bajada no pasa de esta fracción de la distancia.
        private const float BajadaMaximaPorDistancia = 0.8f;

        /// <summary>
        /// V12: la pose del panel debajo de la pantalla de cine: a <paramref name="distancia"/> m de la cabeza (en línea recta), su centro
        /// <see cref="BajadaBajoCine"/> m abajo de los ojos e inclinado hacia la cabeza (de frente a la línea de la vista).
        /// </summary>
        public static Pose PoseBajoCine(Vector3 cabeza, Vector3 frentePlano, float distancia)
        {
            float bajada = Mathf.Min(BajadaBajoCine, distancia * BajadaMaximaPorDistancia);
            float adelante = Mathf.Sqrt(Mathf.Max(0f, distancia * distancia - bajada * bajada));
            Vector3 posicion = cabeza + frentePlano * adelante + Vector3.down * bajada;
            Vector3 hacia = (posicion - cabeza).normalized;
            return new Pose(posicion, Quaternion.LookRotation(hacia, Vector3.up));
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
