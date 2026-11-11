using System.Globalization;
using System.Text;

namespace Infraestructure.Reportes
{
    /// <summary>
    /// Código de barras Code 128 (juego B: ASCII imprimible) dibujado como SVG, para los tickets impresos.
    /// Lo leen los lectores de mano (que lo escriben como teclado) y la cámara del navegador.
    /// </summary>
    public static class Code128
    {
        private const int InicioB = 104;
        private const int Parada = 106;
        /// <summary>Zona en blanco a cada lado, en módulos; los lectores la necesitan para detectar el código.</summary>
        private const int ZonaSilencio = 10;

        /// <summary>Anchos de barra y espacio (en módulos) de cada símbolo, alternando barra y espacio.</summary>
        private static readonly string[] Patrones =
        [
            "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
            "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
            "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
            "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
            "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
            "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
            "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
            "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
            "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
            "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
            "114131", "311141", "411131", "211412", "211214", "211232", "2331112",
        ];

        /// <summary>Valores de los símbolos: inicio B, datos, dígito de control y parada.</summary>
        public static IReadOnlyList<int> Simbolos(string texto)
        {
            var valores = new List<int>(texto.Length + 3) { InicioB };
            foreach (var c in texto)
            {
                if (c < 32 || c > 126)
                {
                    throw new ArgumentException($"El carácter '{c}' no se puede codificar en Code 128 B.", nameof(texto));
                }
                valores.Add(c - 32);
            }

            var control = InicioB;
            for (var i = 1; i < valores.Count; i++)
            {
                control += valores[i] * i;
            }
            valores.Add(control % 103);
            valores.Add(Parada);
            return valores;
        }

        /// <summary>Secuencia de anchos (barra, espacio, barra…) sin zonas de silencio.</summary>
        public static string Anchos(string texto) => string.Concat(Simbolos(texto).Select(v => Patrones[v]));

        /// <summary>
        /// SVG de <paramref name="alto"/> unidades de alto; un módulo mide una unidad de ancho. Con
        /// preserveAspectRatio="none" se estira al espacio que se le dé.
        /// </summary>
        public static string Svg(string texto, int alto = 40)
        {
            var anchos = Anchos(texto);
            var total = anchos.Sum(c => c - '0') + ZonaSilencio * 2;

            var svg = new StringBuilder();
            svg.Append(CultureInfo.InvariantCulture,
                $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {total} {alto}\" width=\"{total}\" height=\"{alto}\" preserveAspectRatio=\"none\" shape-rendering=\"crispEdges\">");
            svg.Append(CultureInfo.InvariantCulture, $"<rect width=\"{total}\" height=\"{alto}\" fill=\"#fff\"/>");

            var x = ZonaSilencio;
            for (var i = 0; i < anchos.Length; i++)
            {
                var ancho = anchos[i] - '0';
                if (i % 2 == 0)
                {
                    svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{x}\" width=\"{ancho}\" height=\"{alto}\" fill=\"#000\"/>");
                }
                x += ancho;
            }

            svg.Append("</svg>");
            return svg.ToString();
        }
    }
}
