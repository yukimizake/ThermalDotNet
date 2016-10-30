// Test app for ThermalDotNet library

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Text;
using ThermalDotNet;

namespace ThermalPrinterTestApp
{
	class MainClass
	{
        static void TestReceipt(ThermalPrinter printer)
		{
			Dictionary<string,int> ItemList = new Dictionary<string, int>(100);
			printer.SetLineSpacing(0);			
			printer.SetAlignCenter();
			printer.WriteLine("MY SHOP",
				(byte)ThermalPrinter.PrintingStyle.DoubleHeight
				+(byte)ThermalPrinter.PrintingStyle.DoubleWidth);
			printer.WriteLine("My address, CITY");
			printer.LineFeed();		
			printer.LineFeed();
			
			ItemList.Add("Item #1",8990);
			ItemList.Add("Item #2 goes here",2000);
			ItemList.Add("Item #3",1490);
			ItemList.Add("Item number four",490);
			ItemList.Add("Item #5 is cheap",245);
			ItemList.Add("Item #6",2990);			
			ItemList.Add("The seventh item",790);
			
			int total = 0;
			foreach (KeyValuePair<string, int> item in ItemList) {
				CashRegister(printer,item.Key,item.Value);
				total += item.Value;
			}
			
			printer.HorizontalLine(32);
			
			double dTotal = Convert.ToDouble(total)/100;
			double VAT = 10.0;
			
			printer.WriteLine(String.Format("{0:0.00}", (dTotal)).PadLeft(32));
			
			printer.WriteLine("VAT 10,0%" + String.Format("{0:0.00}", (dTotal * VAT/100)).PadLeft(23));
			
			printer.WriteLine(String.Format("$ {0:0.00}",dTotal*VAT/100 + dTotal).PadLeft(16),
				ThermalPrinter.PrintingStyle.DoubleWidth);

			printer.LineFeed();
			
			printer.WriteLine("CASH" + String.Format("{0:0.00}",(double)total/100).PadLeft(28));
			printer.LineFeed();
			printer.LineFeed();
			printer.SetAlignCenter();
			printer.WriteLine("Have a good day.",ThermalPrinter.PrintingStyle.Bold);
			
			printer.LineFeed();
			printer.SetAlignLeft();
			printer.WriteLine("Seller : Bob");
			printer.WriteLine("09-28-2011 10:53 02331 509");
			printer.LineFeed();
			printer.LineFeed();
			printer.LineFeed();
		}
		
		static void TestBarcode(ThermalPrinter printer)
		{
			ThermalPrinter.BarcodeType myType = ThermalPrinter.BarcodeType.ean13;
			string myData = "3350030103392";
			printer.WriteLine(myType.ToString() + ", data: " + myData);
			printer.SetLargeBarcode(true);
			printer.LineFeed();
			printer.PrintBarcode(myType,myData);
			printer.SetLargeBarcode(false);
			printer.LineFeed();
			printer.PrintBarcode(myType,myData);
		}

		static void TestImage(ThermalPrinter printer)
		{
			printer.WriteLine("Test image:");
			Bitmap img = new Bitmap("../../../mono-logo.png");
			printer.LineFeed();
			printer.PrintImage(img);
			printer.LineFeed();
			printer.WriteLine("Image OK");
		}
		
		public static void Main(string[] args)
		{
			string printerPortName = "/dev/tty.usbserial-A600dP3F";
			
			//Serial port init
			SerialPort printerPort = new SerialPort(printerPortName, 9600);

			if (printerPort != null)
			{
				Console.WriteLine ("Port ok");
				if (printerPort.IsOpen)
					{
						printerPort.Close();
					}
			}
			
			Console.WriteLine ("Opening port");
			
			try {
				printerPort.Open();
			} catch{
				Console.WriteLine ("I/O error");
				Environment.Exit(0);
			}
			
			//Printer init
			ThermalPrinter printer = new ThermalPrinter(printerPort,2,180,2);
			printer.WakeUp();
			Console.WriteLine(printer.ToString());
		
			//TestReceipt(printer);
			
			//System.Threading.Thread.Sleep(5000);
			//printer.SetBarcodeLeftSpace(25);
			//TestBarcode(printer);
			
			//System.Threading.Thread.Sleep(5000);
			//TestImage(printer);

            //System.Threading.Thread.Sleep(5000);
			
			printer.WriteLineSleepTimeMs = 200;
			printer.WriteLine("Default style");
			printer.WriteLine("PrintingStyle.Bold",ThermalPrinter.PrintingStyle.Bold);
			printer.WriteLine("PrintingStyle.DeleteLine",ThermalPrinter.PrintingStyle.DeleteLine);
			printer.WriteLine("PrintingStyle.DoubleHeight",ThermalPrinter.PrintingStyle.DoubleHeight);
			printer.WriteLine("PrintingStyle.DoubleWidth",ThermalPrinter.PrintingStyle.DoubleWidth);
			printer.WriteLine("PrintingStyle.Reverse",ThermalPrinter.PrintingStyle.Reverse);
			printer.WriteLine("PrintingStyle.Underline",ThermalPrinter.PrintingStyle.Underline);
			printer.WriteLine("PrintingStyle.Updown",ThermalPrinter.PrintingStyle.Updown);
			printer.WriteLine("PrintingStyle.ThickUnderline",ThermalPrinter.PrintingStyle.ThickUnderline);
			printer.SetAlignCenter();
			printer.WriteLine("BIG TEXT!",((byte)ThermalPrinter.PrintingStyle.Bold +
				(byte)ThermalPrinter.PrintingStyle.DoubleHeight +
				(byte)ThermalPrinter.PrintingStyle.DoubleWidth));
			printer.SetAlignLeft();
			printer.WriteLine("Default style again");
			
			printer.LineFeed(3);
			printer.Sleep();
			Console.WriteLine("Printer is now offline.");
			printerPort.Close();
		}
		
		static void CashRegister(ThermalPrinter printer, string item, int price)
		{
			printer.Reset();
			printer.Indent(0);
			
			if (item.Length > 24) {
				item = item.Substring(0,23)+".";
			}
			
			printer.WriteToBuffer(item.ToUpper());
			printer.Indent(25);
			string sPrice = String.Format("{0:0.00}",(double)price/100);
	
			sPrice = sPrice.PadLeft(7);
			
			printer.WriteLine(sPrice);
			printer.Reset();
		}
	}
}
