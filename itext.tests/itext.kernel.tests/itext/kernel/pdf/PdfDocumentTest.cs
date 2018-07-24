/*
This file is part of the iText (R) project.
Copyright (c) 1998-2018 iText Group NV
Authors: iText Software.

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
using System.IO;
using System.Text;
using iText.IO.Image;
using iText.IO.Source;
using iText.Kernel.Pdf.Navigation;
using iText.Kernel.Utils;
using iText.Test;
using iText.Test.Attributes;

namespace iText.Kernel.Pdf {
    public class PdfDocumentTest : ExtendedITextTest {
        public static readonly String sourceFolder = iText.Test.TestUtil.GetParentProjectDirectory(NUnit.Framework.TestContext
            .CurrentContext.TestDirectory) + "/resources/itext/kernel/pdf/PdfDocumentTest/";

        public static readonly String destinationFolder = NUnit.Framework.TestContext.CurrentContext.TestDirectory
             + "/test/itext/kernel/pdf/PdfDocumentTest/";

        [NUnit.Framework.OneTimeSetUp]
        public static void BeforeClass() {
            CreateOrClearDestinationFolder(destinationFolder);
        }

        /// <exception cref="System.IO.IOException"/>
        [NUnit.Framework.Test]
        public virtual void WritingVersionTest01() {
            // There is a possibility to override version in stamping mode
            String @out = destinationFolder + "writing_pdf_version.pdf";
            PdfDocument pdfDoc = new PdfDocument(new PdfWriter(@out, new WriterProperties().SetPdfVersion(PdfVersion.PDF_2_0
                )));
            NUnit.Framework.Assert.AreEqual(PdfVersion.PDF_2_0, pdfDoc.GetPdfVersion());
            pdfDoc.AddNewPage();
            pdfDoc.Close();
            PdfDocument assertPdfDoc = new PdfDocument(new PdfReader(@out));
            NUnit.Framework.Assert.AreEqual(PdfVersion.PDF_2_0, assertPdfDoc.GetPdfVersion());
            assertPdfDoc.Close();
        }

        //We have this test in PdfOutlineTest as well, because we had some issues with outlines before. One test worked
        // fine, while another one failed.
        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void AddOutlinesWithNamedDestinations01() {
            String filename = destinationFolder + "outlinesWithNamedDestinations01.pdf";
            PdfDocument pdfDoc = new PdfDocument(new PdfReader(sourceFolder + "iphone_user_guide.pdf"), new PdfWriter(
                filename));
            PdfArray array1 = new PdfArray();
            array1.Add(pdfDoc.GetPage(2).GetPdfObject());
            array1.Add(PdfName.XYZ);
            array1.Add(new PdfNumber(36));
            array1.Add(new PdfNumber(806));
            array1.Add(new PdfNumber(0));
            PdfArray array2 = new PdfArray();
            array2.Add(pdfDoc.GetPage(3).GetPdfObject());
            array2.Add(PdfName.XYZ);
            array2.Add(new PdfNumber(36));
            array2.Add(new PdfNumber(806));
            array2.Add(new PdfNumber(1.25));
            PdfArray array3 = new PdfArray();
            array3.Add(pdfDoc.GetPage(4).GetPdfObject());
            array3.Add(PdfName.XYZ);
            array3.Add(new PdfNumber(36));
            array3.Add(new PdfNumber(806));
            array3.Add(new PdfNumber(1));
            pdfDoc.AddNamedDestination("test1", array2);
            pdfDoc.AddNamedDestination("test2", array3);
            pdfDoc.AddNamedDestination("test3", array1);
            PdfOutline root = pdfDoc.GetOutlines(false);
            PdfOutline firstOutline = root.AddOutline("Test1");
            firstOutline.AddDestination(PdfDestination.MakeDestination(new PdfString("test1")));
            PdfOutline secondOutline = root.AddOutline("Test2");
            secondOutline.AddDestination(PdfDestination.MakeDestination(new PdfString("test2")));
            PdfOutline thirdOutline = root.AddOutline("Test3");
            thirdOutline.AddDestination(PdfDestination.MakeDestination(new PdfString("test3")));
            pdfDoc.Close();
            NUnit.Framework.Assert.IsNull(new CompareTool().CompareByContent(filename, sourceFolder + "cmp_outlinesWithNamedDestinations01.pdf"
                , destinationFolder, "diff_"));
        }

        /// <exception cref="System.IO.IOException"/>
        [NUnit.Framework.Test]
        public virtual void FreeReferencesInObjectStream() {
            PdfReader reader = new PdfReader(sourceFolder + "styledLineArts_Redacted.pdf");
            PdfWriter writer = new PdfWriter(new MemoryStream());
            PdfDocument document = new PdfDocument(reader, writer, new StampingProperties().UseAppendMode());
            PdfDictionary dict = new PdfDictionary();
            dict.MakeIndirect(document);
            NUnit.Framework.Assert.IsTrue(dict.GetIndirectReference().GetObjNumber() > 0);
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void RemoveUnusedObjectsInWriterModeTest() {
            String filename = "removeUnusedObjectsInWriter.pdf";
            PdfDocument pdfDocument = new PdfDocument(new PdfWriter(destinationFolder + filename));
            pdfDocument.AddNewPage();
            PdfDictionary unusedDictionary = new PdfDictionary();
            PdfArray unusedArray = ((PdfArray)new PdfArray().MakeIndirect(pdfDocument));
            unusedArray.Add(new PdfNumber(42));
            unusedDictionary.Put(new PdfName("testName"), unusedArray);
            unusedDictionary.MakeIndirect(pdfDocument);
            NUnit.Framework.Assert.AreEqual(pdfDocument.GetXref().Size(), 8);
            //on closing, all unused objects shall not be written to resultant document
            pdfDocument.Close();
            PdfDocument testerDocument = new PdfDocument(new PdfReader(destinationFolder + filename));
            NUnit.Framework.Assert.AreEqual(testerDocument.GetXref().Size(), 6);
            testerDocument.Close();
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void RemoveUnusedObjectsInStampingModeTest() {
            String filenameIn = "docWithUnusedObjects_1.pdf";
            String filenameOut = "removeUnusedObjectsInStamping.pdf";
            PdfDocument pdfDocument = new PdfDocument(new PdfWriter(destinationFolder + filenameIn));
            pdfDocument.AddNewPage();
            PdfDictionary unusedDictionary = new PdfDictionary();
            PdfArray unusedArray = ((PdfArray)new PdfArray().MakeIndirect(pdfDocument));
            unusedArray.Add(new PdfNumber(42));
            unusedDictionary.Put(new PdfName("testName"), unusedArray);
            ((PdfDictionary)unusedDictionary.MakeIndirect(pdfDocument)).Flush();
            pdfDocument.Close();
            PdfDocument doc = new PdfDocument(new PdfReader(destinationFolder + filenameIn), new PdfWriter(destinationFolder
                 + filenameOut));
            NUnit.Framework.Assert.AreEqual(doc.GetXref().Size(), 8);
            //on closing, all unused objects shall not be written to resultant document
            doc.Close();
            PdfDocument testerDocument = new PdfDocument(new PdfReader(destinationFolder + filenameOut));
            NUnit.Framework.Assert.AreEqual(testerDocument.GetXref().Size(), 6);
            testerDocument.Close();
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void AddUnusedObjectsInWriterModeTest() {
            String filename = "addUnusedObjectsInWriter.pdf";
            PdfDocument pdfDocument = new PdfDocument(new PdfWriter(destinationFolder + filename));
            pdfDocument.AddNewPage();
            PdfDictionary unusedDictionary = new PdfDictionary();
            PdfArray unusedArray = ((PdfArray)new PdfArray().MakeIndirect(pdfDocument));
            unusedArray.Add(new PdfNumber(42));
            unusedDictionary.Put(new PdfName("testName"), unusedArray);
            unusedDictionary.MakeIndirect(pdfDocument);
            NUnit.Framework.Assert.AreEqual(pdfDocument.GetXref().Size(), 8);
            pdfDocument.SetFlushUnusedObjects(true);
            pdfDocument.Close();
            PdfDocument testerDocument = new PdfDocument(new PdfReader(destinationFolder + filename));
            NUnit.Framework.Assert.AreEqual(testerDocument.GetXref().Size(), 8);
            testerDocument.Close();
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void AddUnusedObjectsInStampingModeTest() {
            String filenameIn = "docWithUnusedObjects_2.pdf";
            String filenameOut = "addUnusedObjectsInStamping.pdf";
            PdfDocument pdfDocument = new PdfDocument(new PdfWriter(destinationFolder + filenameIn));
            pdfDocument.AddNewPage();
            PdfDictionary unusedDictionary = new PdfDictionary();
            PdfArray unusedArray = ((PdfArray)new PdfArray().MakeIndirect(pdfDocument));
            unusedArray.Add(new PdfNumber(42));
            unusedDictionary.Put(new PdfName("testName"), unusedArray);
            ((PdfDictionary)unusedDictionary.MakeIndirect(pdfDocument)).Flush();
            pdfDocument.Close();
            PdfDocument doc = new PdfDocument(new PdfReader(destinationFolder + filenameIn), new PdfWriter(destinationFolder
                 + filenameOut));
            NUnit.Framework.Assert.AreEqual(doc.GetXref().Size(), 8);
            doc.SetFlushUnusedObjects(true);
            doc.Close();
            PdfDocument testerDocument = new PdfDocument(new PdfReader(destinationFolder + filenameOut));
            NUnit.Framework.Assert.AreEqual(testerDocument.GetXref().Size(), 8);
            testerDocument.Close();
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void AddUnusedStreamObjectsTest() {
            String filenameIn = "docWithUnusedObjects_3.pdf";
            PdfDocument pdfDocument = new PdfDocument(new PdfWriter(destinationFolder + filenameIn));
            pdfDocument.AddNewPage();
            PdfDictionary unusedDictionary = new PdfDictionary();
            PdfArray unusedArray = ((PdfArray)new PdfArray().MakeIndirect(pdfDocument));
            unusedArray.Add(new PdfNumber(42));
            PdfStream stream = new PdfStream(new byte[] { 1, 2, 34, 45 }, 0);
            unusedArray.Add(stream);
            unusedDictionary.Put(new PdfName("testName"), unusedArray);
            ((PdfDictionary)unusedDictionary.MakeIndirect(pdfDocument)).Flush();
            pdfDocument.SetFlushUnusedObjects(true);
            pdfDocument.Close();
            PdfDocument testerDocument = new PdfDocument(new PdfReader(destinationFolder + filenameIn));
            NUnit.Framework.Assert.AreEqual(testerDocument.GetXref().Size(), 9);
            testerDocument.Close();
        }

        /// <exception cref="System.IO.IOException"/>
        [NUnit.Framework.Test]
        public virtual void TestImageCompressLevel() {
            byte[] b = ImageDataFactory.Create(sourceFolder + "berlin2013.jpg").GetData();
            ByteArrayOutputStream image = new ByteArrayOutputStream();
            image.AssignBytes(b, b.Length);
            MemoryStream byteArrayStream1 = new ByteArrayOutputStream();
            DeflaterOutputStream zip = new DeflaterOutputStream(byteArrayStream1, 9);
            image.WriteTo(zip);
            MemoryStream byteArrayStream2 = new ByteArrayOutputStream();
            DeflaterOutputStream zip2 = new DeflaterOutputStream(byteArrayStream2, -1);
            image.WriteTo(zip2);
            NUnit.Framework.Assert.IsTrue(byteArrayStream1.Length == byteArrayStream2.Length);
            zip.Dispose();
            zip2.Dispose();
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        [LogMessage(iText.IO.LogMessageConstant.FLUSHED_OBJECT_CONTAINS_REFERENCE_WHICH_NOT_REFER_TO_ANY_OBJECT)]
        public virtual void TestFreeReference() {
            PdfWriter writer = new PdfWriter(destinationFolder + "freeReference.pdf", new WriterProperties().SetFullCompressionMode
                (false));
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(sourceFolder + "baseFreeReference.pdf"), writer);
            pdfDocument.GetPage(1).GetResources().GetPdfObject().GetAsArray(new PdfName("d")).Get(0).GetIndirectReference
                ().SetFree();
            PdfStream pdfStream = new PdfStream();
            pdfStream.SetData(new byte[] { 24, 23, 67 });
            pdfStream.MakeIndirect(pdfDocument);
            pdfDocument.GetPage(1).GetResources().GetPdfObject().GetAsArray(new PdfName("d")).Add(pdfStream);
            pdfDocument.Close();
            NUnit.Framework.Assert.IsNull(new CompareTool().CompareByContent(destinationFolder + "freeReference.pdf", 
                sourceFolder + "cmp_freeReference.pdf", destinationFolder, "diff_"));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void FullCompressionAppendMode() {
            PdfWriter writer = new PdfWriter(destinationFolder + "fullCompressionAppendMode.pdf", new WriterProperties
                ().SetFullCompressionMode(true).SetCompressionLevel(CompressionConstants.NO_COMPRESSION));
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(sourceFolder + "fullCompressionDoc.pdf"), writer, 
                new StampingProperties().UseAppendMode());
            PdfPage page = pdfDocument.GetPage(1);
            PdfStream contentStream = new PdfStream();
            String contentStr = iText.IO.Util.JavaUtil.GetStringForBytes(pdfDocument.GetPage(1).GetFirstContentStream(
                ).GetBytes(), Encoding.ASCII);
            contentStream.SetData(contentStr.Replace("/F1 16", "/F1 24").GetBytes(Encoding.ASCII));
            page.GetPdfObject().Put(PdfName.Contents, contentStream);
            page.SetModified();
            pdfDocument.Close();
            NUnit.Framework.Assert.IsNull(new CompareTool().CompareByContent(destinationFolder + "fullCompressionAppendMode.pdf"
                , sourceFolder + "cmp_fullCompressionAppendMode.pdf", destinationFolder, "diff_"));
            PdfDocument assertDoc = new PdfDocument(new PdfReader(destinationFolder + "fullCompressionAppendMode.pdf")
                );
            NUnit.Framework.Assert.IsTrue(assertDoc.GetPdfObject(9).IsStream());
            NUnit.Framework.Assert.AreEqual(1, ((PdfDictionary)assertDoc.GetPdfObject(9)).GetAsNumber(PdfName.N).IntValue
                ());
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="System.Exception"/>
        [NUnit.Framework.Test]
        public virtual void CheckAndResolveCircularReferences() {
            PdfDocument pdfDocument = new PdfDocument(new PdfReader(sourceFolder + "datasheet.pdf"), new PdfWriter(destinationFolder
                 + "datasheet_mode.pdf"));
            PdfDictionary pdfObject = (PdfDictionary)pdfDocument.GetPdfObject(53);
            pdfDocument.GetPage(1).GetResources().AddForm((PdfStream)pdfObject);
            pdfDocument.Close();
            NUnit.Framework.Assert.IsNull(new CompareTool().CompareByContent(destinationFolder + "datasheet_mode.pdf", 
                sourceFolder + "cmp_datasheet_mode.pdf", "d:/", "diff_"));
        }

        /// <exception cref="System.IO.IOException"/>
        [NUnit.Framework.Test]
        public virtual void ReadEncryptedDocumentWithFullCompression() {
            PdfReader reader = new PdfReader(sourceFolder + "source.pdf", new ReaderProperties().SetPassword("123".GetBytes
                ()));
            PdfDocument pdfDocument = new PdfDocument(reader);
            PdfDictionary form = pdfDocument.GetCatalog().GetPdfObject().GetAsDictionary(PdfName.AcroForm);
            PdfDictionary field = form.GetAsArray(PdfName.Fields).GetAsDictionary(0);
            NUnit.Framework.Assert.AreEqual("ch", field.GetAsString(PdfName.T).ToUnicodeString());
            NUnit.Framework.Assert.AreEqual("SomeStringValueInDictionary", field.GetAsDictionary(new PdfName("TestDic"
                )).GetAsString(new PdfName("TestString")).ToUnicodeString());
            NUnit.Framework.Assert.AreEqual("SomeStringValueInArray", field.GetAsArray(new PdfName("TestArray")).GetAsString
                (0).ToUnicodeString());
            pdfDocument.Close();
        }
    }
}
