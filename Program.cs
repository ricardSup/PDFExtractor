using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using IronOcr;

namespace ConsoleApp1
{
    public class Program
    {
        static void Main(string[] args)
        {
            //ExtractAddressesFromPDF("C:\\Users\\richard.suprayogi\\Downloads\\Test File.pdf");
            //ExtractDeliveryAddressesFromPDF("C:\\Users\\richard.suprayogi\\Downloads\\Test File.pdf");
            //var Result = new IronTesseract().Read("C:\\Users\\richard.suprayogi\\Desktop\\IMG_20230301_143110.jpg");
            //var Result = new IronTesseract().Read("C:\\Users\\richard.suprayogi\\Downloads\\Test File.pdf");
            //Console.WriteLine(Result.Text);

            var addresses = new List<string>();

            var Ocr = new IronTesseract();
            using (var Input = new OcrInput())
            {
                var ContentArea = new System.Drawing.Rectangle()
                { X = (int)300, Y = (int)1200, Height = (int)600, Width = (int)1600 };  //<-- the area you want in px
                Input.AddPdf("C:\\Users\\richard.suprayogi\\Downloads\\Test File.pdf", null, ContentArea,600);

                try
                {
                    var Result = Ocr.Read(Input);

                    foreach (var item in Result.Pages)
                    {
                        if (!string.IsNullOrEmpty(item.Text))
                        {
                            addresses.Add(ExtractDeliveryAddress(item.Text));
                        }
                    }

                    string csvFilePath = "C:\\Users\\richard.suprayogi\\Downloads\\addresses1.csv";
                    using (StreamWriter writer = new StreamWriter(csvFilePath, false, Encoding.UTF8))
                    {
                        foreach (var address in addresses)
                        {
                            writer.WriteLine(address);
                        }
                    }

                    Console.WriteLine(Result.Text);
                }
                catch (Exception ex)
                {

                    throw;
                }
            }

        }



        static void ExtractDeliveryAddressesFromPDF(string filePath)
        {
            var pdfReader = new PdfReader(filePath);
            var addresses = new List<string>();

            for (int i = 1; i <= pdfReader.NumberOfPages; i++)
            {
                var currentPageText = PdfTextExtractor.GetTextFromPage(pdfReader, i);
                //var skipPage = new List<string> { "Permit Holder:", "CHARGES SINCE LAST STATEME" };
                //if (!skipPage.Contains(currentPageText))
                //{

                //}

                bool containsPermitHolder = currentPageText.Contains("Dear");
                bool containsChargesSinceLastStatement = currentPageText.Contains("CHARGES");

                if (!containsPermitHolder && !containsChargesSinceLastStatement)
                {
                    continue;
                }

                addresses.Add(ExtractDeliveryAddress(currentPageText));
            }

            // Process the extracted addresses as desired (e.g., save to a file)
            foreach (var address in addresses)
            {
                Console.WriteLine(string.Join(", ", address));
            }


            string csvFilePath = "C:\\Users\\richard.suprayogi\\Downloads\\addresses1.csv";
            using (StreamWriter writer = new StreamWriter(csvFilePath, false, Encoding.UTF8))
            {
                foreach (var address in addresses)
                {
                    writer.WriteLine(address);
                }
            }
        }

        static string ExtractDeliveryAddress(string text)
        {
            List<Tuple<string, string>> skipPattern = new List<Tuple<string, string>>{
                new Tuple<string,string>("PERMIT NO","text"),
                new Tuple<string,string>("PERMIT SITE","text"),
                new Tuple<string,string>("STATEMENT DATE","date"),
                new Tuple<string,string>("PAYMENT","text"),
                new Tuple<string,string>("PERMIT","text"),
                new Tuple<string,string>("DATE","date"),
                new Tuple<string,string>("PERMIT NUMBER","text"),
                new Tuple<string,string>("ALARM LOCATION","text"),
                new Tuple<string,string>("ATTN","text"),
                new Tuple<string,string>("OR CURRENT ALARM USER","text")
            };
            string[] lines = text.Split('\n');

            string deliveryAddress = "";
            //var skipPage = new List<string> { "Permit Holder:", "CHARGES SINCE LAST STATEME" };

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                trimmedLine = Regex.Replace(trimmedLine, @"[^\w\s\r\n]", " ").Trim();
                trimmedLine = Regex.Replace(trimmedLine, @"\s+", " ");

                bool containsPermitHolder = trimmedLine.Contains("Dear");
                bool containsChargesSinceLastStatement = trimmedLine.Contains("CHARGES");

                if (containsPermitHolder || containsChargesSinceLastStatement)
                {
                    deliveryAddress = deliveryAddress.Trim(); // Remove any leading/trailing whitespaces
                    return deliveryAddress; ;
                }

                for (int i = 0; i < skipPattern.Count; i++)
                {
                    if (skipPattern[i].Item2 == "date")
                    {
                        string pattern = $@"{skipPattern[i].Item1}\s+(\d{{2}} \d{{2}} \d{{4}})";
                        Match match = Regex.Match(trimmedLine, pattern);
                        if (match.Success)
                        {
                            trimmedLine = trimmedLine.Replace(match.ToString(), "");
                        }
                    }
                    else
                    {
                        int idx = trimmedLine.IndexOf(skipPattern[i].Item1);
                        if (idx != -1)
                        {
                            trimmedLine = trimmedLine.Substring(0, idx);
                        }
                        //string pattern = $@"{skipPattern[i].Item1}\s*.*$";
                        //Match match = Regex.Match(trimmedLine, pattern, RegexOptions.IgnoreCase);
                        //if (match.Success)
                        //{
                        //    trimmedLine = trimmedLine.Replace(match.ToString(), "");
                        //}
                    }
                }
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    trimmedLine = trimmedLine.Trim(); // Remove any leading/trailing whitespaces
                    deliveryAddress += trimmedLine + ";";
                }
            }
            return deliveryAddress;
        }
    }
}
