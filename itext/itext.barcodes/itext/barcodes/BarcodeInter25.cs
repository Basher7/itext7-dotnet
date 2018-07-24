/*

This file is part of the iText (R) project.
Copyright (c) 1998-2018 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using System.Text;
using iText.Kernel;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;

namespace iText.Barcodes {
    /// <summary>Implements the code interleaved 2 of 5.</summary>
    /// <remarks>
    /// Implements the code interleaved 2 of 5. The text can include
    /// non numeric characters that are printed but do not generate bars.
    /// The default parameters are:
    /// <pre>
    /// x = 0.8f;
    /// n = 2;
    /// font = new PdfType1Font(document, new TYPE_1_FONT(FontConstants.HELVETICA, PdfEncodings.WINANSI));
    /// size = 8;
    /// baseline = size;
    /// barHeight = size * 3;
    /// textAlignment = ALIGN_CENTER;
    /// generateChecksum = false;
    /// checksumText = false;
    /// </pre>
    /// </remarks>
    public class BarcodeInter25 : Barcode1D {
        /// <summary>The bars to generate the code.</summary>
        private static readonly byte[][] BARS = new byte[][] { new byte[] { 0, 0, 1, 1, 0 }, new byte[] { 1, 0, 0, 
            0, 1 }, new byte[] { 0, 1, 0, 0, 1 }, new byte[] { 1, 1, 0, 0, 0 }, new byte[] { 0, 0, 1, 0, 1 }, new 
            byte[] { 1, 0, 1, 0, 0 }, new byte[] { 0, 1, 1, 0, 0 }, new byte[] { 0, 0, 0, 1, 1 }, new byte[] { 1, 
            0, 0, 1, 0 }, new byte[] { 0, 1, 0, 1, 0 } };

        /// <summary>Creates new BarcodeInter25</summary>
        /// <param name="document">The document</param>
        public BarcodeInter25(PdfDocument document)
            : base(document) {
            x = 0.8f;
            n = 2;
            font = document.GetDefaultFont();
            size = 8;
            baseline = size;
            barHeight = size * 3;
            textAlignment = ALIGN_CENTER;
            generateChecksum = false;
            checksumText = false;
        }

        /// <summary>Deletes all the non numeric characters from <CODE>text</CODE>.</summary>
        /// <param name="text">the text</param>
        /// <returns>a <CODE>String</CODE> with only numeric characters</returns>
        public static String KeepNumbers(String text) {
            StringBuilder sb = new StringBuilder();
            for (int k = 0; k < text.Length; ++k) {
                char c = text[k];
                if (c >= '0' && c <= '9') {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>Calculates the checksum.</summary>
        /// <param name="text">the numeric text</param>
        /// <returns>the checksum</returns>
        public static char GetChecksum(String text) {
            int mul = 3;
            int total = 0;
            for (int k = text.Length - 1; k >= 0; --k) {
                int n = text[k] - '0';
                total += mul * n;
                mul ^= 2;
            }
            return (char)(((10 - (total % 10)) % 10) + '0');
        }

        /// <summary>Creates the bars for the barcode.</summary>
        /// <param name="text">the text. It can contain non numeric characters</param>
        /// <returns>the barcode</returns>
        public static byte[] GetBarsInter25(String text) {
            text = KeepNumbers(text);
            if ((text.Length & 1) != 0) {
                throw new PdfException(PdfException.TextMustBeEven);
            }
            byte[] bars = new byte[text.Length * 5 + 7];
            int pb = 0;
            bars[pb++] = 0;
            bars[pb++] = 0;
            bars[pb++] = 0;
            bars[pb++] = 0;
            int len = text.Length / 2;
            for (int k = 0; k < len; ++k) {
                int c1 = text[k * 2] - '0';
                int c2 = text[k * 2 + 1] - '0';
                byte[] b1 = BARS[c1];
                byte[] b2 = BARS[c2];
                for (int j = 0; j < 5; ++j) {
                    bars[pb++] = b1[j];
                    bars[pb++] = b2[j];
                }
            }
            bars[pb++] = 1;
            bars[pb++] = 0;
            bars[pb++] = 0;
            return bars;
        }

        /// <summary>
        /// Gets the maximum area that the barcode and the text, if
        /// any, will occupy.
        /// </summary>
        /// <remarks>
        /// Gets the maximum area that the barcode and the text, if
        /// any, will occupy. The lower left corner is always (0, 0).
        /// </remarks>
        /// <returns>the size the barcode occupies.</returns>
        public override Rectangle GetBarcodeSize() {
            float fontX = 0;
            float fontY = 0;
            if (font != null) {
                if (baseline > 0) {
                    fontY = baseline - GetDescender();
                }
                else {
                    fontY = -baseline + size;
                }
                String fullCode = code;
                if (generateChecksum && checksumText) {
                    fullCode += GetChecksum(fullCode);
                }
                fontX = font.GetWidth(altText != null ? altText : fullCode, size);
            }
            String fullCode_1 = KeepNumbers(code);
            int len = fullCode_1.Length;
            if (generateChecksum) {
                ++len;
            }
            float fullWidth = len * (3 * x + 2 * x * n) + (6 + n) * x;
            fullWidth = Math.Max(fullWidth, fontX);
            float fullHeight = barHeight + fontY;
            return new Rectangle(fullWidth, fullHeight);
        }

        /// <summary>Places the barcode in a <CODE>PdfCanvas</CODE>.</summary>
        /// <remarks>
        /// Places the barcode in a <CODE>PdfCanvas</CODE>. The
        /// barcode is always placed at coordinates (0, 0). Use the
        /// translation matrix to move it elsewhere.<p>
        /// The bars and text are written in the following colors:
        /// <br />
        /// <TABLE BORDER=1 SUMMARY="barcode properties">
        /// <TR>
        /// <TH><P><CODE>barColor</CODE></TH>
        /// <TH><P><CODE>textColor</CODE></TH>
        /// <TH><P>Result</TH>
        /// </TR>
        /// <TR>
        /// <TD><P><CODE>null</CODE></TD>
        /// <TD><P><CODE>null</CODE></TD>
        /// <TD><P>bars and text painted with current fill color</TD>
        /// </TR>
        /// <TR>
        /// <TD><P><CODE>barColor</CODE></TD>
        /// <TD><P><CODE>null</CODE></TD>
        /// <TD><P>bars and text painted with <CODE>barColor</CODE></TD>
        /// </TR>
        /// <TR>
        /// <TD><P><CODE>null</CODE></TD>
        /// <TD><P><CODE>textColor</CODE></TD>
        /// <TD><P>bars painted with current color<br />text painted with <CODE>textColor</CODE></TD>
        /// </TR>
        /// <TR>
        /// <TD><P><CODE>barColor</CODE></TD>
        /// <TD><P><CODE>textColor</CODE></TD>
        /// <TD><P>bars painted with <CODE>barColor</CODE><br />text painted with <CODE>textColor</CODE></TD>
        /// </TR>
        /// </TABLE>
        /// </remarks>
        /// <param name="canvas">the <CODE>PdfCanvas</CODE> where the barcode will be placed</param>
        /// <param name="barColor">the color of the bars. It can be <CODE>null</CODE></param>
        /// <param name="textColor">the color of the text. It can be <CODE>null</CODE></param>
        /// <returns>the dimensions the barcode occupies</returns>
        public override Rectangle PlaceBarcode(PdfCanvas canvas, Color barColor, Color textColor) {
            String fullCode = code;
            float fontX = 0;
            if (font != null) {
                if (generateChecksum && checksumText) {
                    fullCode += GetChecksum(fullCode);
                }
                fontX = font.GetWidth(fullCode = altText != null ? altText : fullCode, size);
            }
            String bCode = KeepNumbers(code);
            if (generateChecksum) {
                bCode += GetChecksum(bCode);
            }
            int len = bCode.Length;
            float fullWidth = len * (3 * x + 2 * x * n) + (6 + n) * x;
            float barStartX = 0;
            float textStartX = 0;
            switch (textAlignment) {
                case ALIGN_LEFT: {
                    break;
                }

                case ALIGN_RIGHT: {
                    if (fontX > fullWidth) {
                        barStartX = fontX - fullWidth;
                    }
                    else {
                        textStartX = fullWidth - fontX;
                    }
                    break;
                }

                default: {
                    if (fontX > fullWidth) {
                        barStartX = (fontX - fullWidth) / 2;
                    }
                    else {
                        textStartX = (fullWidth - fontX) / 2;
                    }
                    break;
                }
            }
            float barStartY = 0;
            float textStartY = 0;
            if (font != null) {
                if (baseline <= 0) {
                    textStartY = barHeight - baseline;
                }
                else {
                    textStartY = -GetDescender();
                    barStartY = textStartY + baseline;
                }
            }
            byte[] bars = GetBarsInter25(bCode);
            bool print = true;
            if (barColor != null) {
                canvas.SetFillColor(barColor);
            }
            for (int k = 0; k < bars.Length; ++k) {
                float w = (bars[k] == 0 ? x : x * n);
                if (print) {
                    canvas.Rectangle(barStartX, barStartY, w - inkSpreading, barHeight);
                }
                print = !print;
                barStartX += w;
            }
            canvas.Fill();
            if (font != null) {
                if (textColor != null) {
                    canvas.SetFillColor(textColor);
                }
                canvas.BeginText();
                canvas.SetFontAndSize(font, size);
                canvas.SetTextMatrix(textStartX, textStartY);
                canvas.ShowText(fullCode);
                canvas.EndText();
            }
            return GetBarcodeSize();
        }
        // AWT related methods (remove this if you port to Android / GAE)
    }
}
