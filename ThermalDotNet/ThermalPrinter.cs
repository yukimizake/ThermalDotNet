//  ThermalDotNet
//  An ESC/POS serial thermal printer library
//  by yukimizake
//  https://github.com/yukimizake/ThermalDotNet

//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Text;

namespace ThermalDotNet
{
	/// <summary>
	/// ESC/POS serial thermal printer library.
	/// https://github.com/yukimizake/ThermalDotNet
	/// This program is distributed under the GPLv3 licence.
	/// </summary>
	public class ThermalPrinter
	{	
		private SerialPort _serialPort;
		private byte _maxPrintingDots = 7;
		private byte _heatingTime = 80;
		private byte _heatingInterval = 2;
		
		/// <summary>
		/// Delay between two picture lines. (in ms)
		/// </summary>
		public int PictureLineSleepTimeMs = 40;
		/// <summary>
		/// Delay between two text lines. (in ms)
		/// </summary>
		public int WriteLineSleepTimeMs = 0;
		/// <summary>
		/// Current encoding used by the printer.
		/// </summary>
		public string Encoding		{ get; private set; }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ThermalDotNet.ThermalPrinter"/> class.
		/// </summary>
		/// <param name='serialPort'>
		/// Serial port used by printer.
		/// </param>
		/// <param name='maxPrintingDots'>
		/// Max printing dots (0-255), unit: (n+1)*8 dots, default: 7 ((7+1)*8 = 64 dots)
		/// </param>
		/// <param name='heatingTime'>
		/// Heating time (3-255), unit: 10µs, default: 80 (800µs)
		/// </param>
		/// <param name='heatingInterval'>
		/// Heating interval (0-255), unit: 10µs, default: 2 (20µs)
		/// </param>
		public ThermalPrinter(SerialPort serialPort, byte maxPrintingDots, byte heatingTime, byte heatingInterval)
		{
			_constructor(serialPort,maxPrintingDots,heatingTime,heatingInterval);
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ThermalDotNet.ThermalPrinter"/> class.
		/// </summary>
		/// <param name='serialPort'>
		/// Serial port used by printer.
		/// </param>
		public ThermalPrinter(SerialPort serialPort)
		{
			_constructor(serialPort,_maxPrintingDots,_heatingTime,_heatingInterval);
		}
		
		private void _constructor(SerialPort serialPort, byte maxPrintingDots, byte heatingTime, byte heatingInterval)
		{
			this.Encoding = "ibm850";
			
			_maxPrintingDots = maxPrintingDots;
			_heatingTime = heatingTime;
			_heatingInterval = heatingInterval;

			_serialPort = serialPort;
			Reset();
			SetPrintingParameters(maxPrintingDots,heatingTime,heatingInterval);
			_sendEncoding(this.Encoding);
		}
		
		/// <summary>
		/// Prints the line of text.
		/// </summary>
		/// <param name='text'>
		/// Text to print.
		/// </param>
		public void WriteLine(string text)
		{
			WriteToBuffer(text);
			_writeByte(10);
			System.Threading.Thread.Sleep(WriteLineSleepTimeMs);
		}
		
		/// <summary>
		/// Sends the text to the printer buffer. Does not print until a line feed (0x10) is sent.
		/// </summary>
		/// <param name='text'>
		/// Text to print.
		/// </param>
		public void WriteToBuffer(string text)
		{
			text = text.Trim('\n').Trim('\r');
			byte[] originalBytes = System.Text.Encoding.UTF8.GetBytes(text);
			byte[] outputBytes = System.Text.Encoding.Convert(System.Text.Encoding.UTF8,
				System.Text.Encoding.GetEncoding(this.Encoding),originalBytes);
			_serialPort.Write(outputBytes,0,outputBytes.Length);
		}
		
		/// <summary>
		/// Prints the line of text, white on black.
		/// </summary>
		/// <param name='text'>
		/// Text to print.
		/// </param>
		public void WriteLine_Invert(string text)
		{
			//Sets inversion on
			_writeByte(29);
			_writeByte(66);
			_writeByte(1);
			
			//Sends the text
			WriteLine(text);
			
			//Sets inversion off
			_writeByte(29);
			_writeByte(66);
			_writeByte(0);
			
			LineFeed();
		}
		
		/// <summary>
		/// Prints the line of text, double size.
		/// </summary>
		/// <param name='text'>
		/// Text to print.
		/// </param>
		public void WriteLine_Big(string text)
		{
			const byte DoubleHeight = 1 << 4;
			const byte DoubleWidth = 1 << 5;
			const byte Bold = 1 << 3;
			
			//big on
			_writeByte(27);
			_writeByte(33);
			_writeByte(DoubleHeight + DoubleWidth + Bold);
			
			//Sends the text
			WriteLine(text);
			
			//big off
			_writeByte(27);
			_writeByte(33);
			_writeByte(0);
		}
	
		/// <summary>
		/// Prints the line of text.
		/// </summary>
		/// <param name='text'>
		/// Text to print.
		/// </param>
		/// <param name='style'>
		/// Style of the text.
		/// </param> 
		public void WriteLine(string text, PrintingStyle style)
		{
			WriteLine(text,(byte)style);
		}
		
		/// <summary>
		/// Prints the line of text.
		/// </summary>
		/// <param name='text'>
		/// Text to print.
		/// </param>
		/// <param name='style'>
		/// Style of the text. Can be the sum of PrintingStyle enums.
		/// </param>
		public void WriteLine(string text, byte style)
		{
			byte underlineHeight = 0;
			
			if (_BitTest(style, 0))
			{
				style = _BitClear(style, 0);
				underlineHeight = 1;
			}
			
			if (_BitTest(style, 7))
			{
				style = _BitClear(style, 7);
				underlineHeight = 2;
			}
			
			if (underlineHeight != 0) {
				_writeByte(27);
				_writeByte(45);
				_writeByte(underlineHeight);
			}
			
			//style on
			_writeByte(27);
			_writeByte(33);
			_writeByte((byte)style);
			
			//Sends the text
			WriteLine(text);
			
			//style off
			if (underlineHeight != 0) {
				_writeByte(27);
				_writeByte(45);
				_writeByte(0);
			}
			_writeByte(27);
			_writeByte(33);
			_writeByte(0);
			
		}
		
		/// <summary>
		/// Prints the line of text in bold.
		/// </summary>
		/// <param name='text'>
		/// Text to print.
		/// </param>
		public void WriteLine_Bold(string text)
		{
			//bold on
			BoldOn();
			
			//Sends the text
			WriteLine(text);
			
			//bold off
			BoldOff();
			
			LineFeed();
		}
		
		/// <summary>
		/// Sets bold mode on.
		/// </summary>
		public void BoldOn()
		{
			_writeByte(27);
			_writeByte(32);
			_writeByte(1);
			_writeByte(27);
			_writeByte(69);
			_writeByte(1);
		}
		
		/// <summary>
		/// Sets bold mode off.
		/// </summary>
		public void BoldOff()
		{
			_writeByte(27);
			_writeByte(32);
			_writeByte(0);
			_writeByte(27);
			_writeByte(69);
			_writeByte(0);
		}
		
		/// <summary>
		/// Sets white on black mode on.
		/// </summary>
		public void WhiteOnBlackOn()
		{
			_writeByte(29);
			_writeByte(66);
			_writeByte(1);
		}
		
		/// <summary>
		/// Sets white on black mode off.
		/// </summary>
		public void WhiteOnBlackOff()
		{
			_writeByte(29);
			_writeByte(66);
			_writeByte(0);
		}
		
		/// <summary>
		/// Sets the text size.
		/// </summary>
		/// <param name='doubleWidth'>
		/// Double width
		/// </param>
		/// <param name='doubleHeight'>
		/// Double height
		/// </param>
		public void SetSize(bool doubleWidth, bool doubleHeight)
		{
			int sizeValue = (Convert.ToInt32(doubleWidth))*(0xF0) + (Convert.ToInt32(doubleHeight))*(0x0F);
			_writeByte(29);
			_writeByte(33);
			_writeByte((byte)sizeValue);
		}
		
		///	<summary>
		/// Prints the contents of the buffer and feeds one line.
		/// </summary>
		public void LineFeed()
		{
			_writeByte(10);
		}
		
		/// <summary>
		/// Prints the contents of the buffer and feeds n lines.
		/// </summary>
		/// <param name='lines'>
		/// Number of lines to feed.
		/// </param>
		public void LineFeed(byte lines)
		{
			_writeByte(27);
			_writeByte(100);
			_writeByte(lines);
		}
		
		/// <summary>
		/// Idents the text.
		/// </summary>
		/// <param name='columns'>
		/// Number of columns.
		/// </param>
		public void Indent(byte columns)
		{
			if (columns < 0 || columns > 31) {
				columns = 0;
			}
			
			_writeByte(27);
			_writeByte(66);
			_writeByte(columns);
		}
		
		/// <summary>
		/// Sets the line spacing.
		/// </summary>
		/// <param name='lineSpacing'>
		/// Line spacing (in dots), default value: 32 dots.
		/// </param>
		public void SetLineSpacing(byte lineSpacing)
		{
			_writeByte(27);
			_writeByte(51);
			_writeByte(lineSpacing);
		}
		
		/// <summary>
		/// Aligns the text to the left.
		/// </summary>
		public void SetAlignLeft()
		{
			_writeByte(27);
			_writeByte(97);
			_writeByte(0);
		}
		
		/// <summary>
		/// Centers the text.
		/// </summary>		
		public void SetAlignCenter()
		{
			_writeByte(27);
			_writeByte(97);
			_writeByte(1);
		}
		
		/// <summary>
		/// Aligns the text to the right.
		/// </summary>
		public void SetAlignRight()
		{
			_writeByte(27);
			_writeByte(97);
			_writeByte(2);
		}
		
		/// <summary>
		/// Prints a horizontal line.
		/// </summary>
		/// <param name='length'>
		/// Line length (in characters) (max 32).
		/// </param>
		public void HorizontalLine(int length)
		{
			if (length > 0) {
				if (length > 32) {
					length = 32;
				}
				
				for (int i = 0; i < length; i++) {
					_writeByte(0xC4);
				}
			_writeByte(10);
			}
		}
		
		/// <summary>
		/// Resets the printer.
		/// </summary>
		public void Reset()
		{
			_writeByte(27);
			_writeByte(64);	
			System.Threading.Thread.Sleep(50);
		}
		
		/// <summary>
		/// List of supported barcode types.
		/// </summary>
		public enum BarcodeType
		{
			/// <summary>
			/// UPC-A
			/// </summary>
			upc_a = 0,
			/// <summary>
			/// UPC-E
			/// </summary>
			upc_e = 1,
			/// <summary>
			/// EAN13
			/// </summary>
			ean13 = 2,
			/// <summary>
			/// EAN8
			/// </summary>
			ean8 = 3,
			/// <summary>
			/// CODE 39
			/// </summary>
			code39 = 4,
			/// <summary>
			/// I25
			/// </summary>
			i25 = 5,
			/// <summary>
			/// CODEBAR
			/// </summary>
			codebar = 6,
			/// <summary>
			/// CODE 93
			/// </summary>
			code93 = 7,
			/// <summary>
			/// CODE 128
			/// </summary>
			code128 = 8,
			/// <summary>
			/// CODE 11
			/// </summary>
			code11 = 9,
			/// <summary>
			/// MSI
			/// </summary>
			msi = 10
		}
		
		/// <summary>
		/// Prints the barcode data.
		/// </summary>
		/// <param name='type'>
		/// Type of barcode.
		/// </param>
		/// <param name='data'>
		/// Data to print.
		/// </param>
		public void PrintBarcode(BarcodeType type, string data)
		{
			byte[] originalBytes;
			byte[] outputBytes;
			
			if (type == BarcodeType.code93 || type == BarcodeType.code128)
			{
				originalBytes = System.Text.Encoding.UTF8.GetBytes(data);
				outputBytes = originalBytes;
			} else {
				originalBytes = System.Text.Encoding.UTF8.GetBytes(data.ToUpper());
				outputBytes = System.Text.Encoding.Convert(System.Text.Encoding.UTF8,System.Text.Encoding.GetEncoding(this.Encoding),originalBytes);
			}
			
			switch (type) {
			case BarcodeType.upc_a:
				if (data.Length ==  11 || data.Length ==  12) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(0);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.upc_e:
				if (data.Length ==  11 || data.Length ==  12) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(1);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.ean13:
				if (data.Length ==  12 || data.Length ==  13) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(2);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.ean8:
				if (data.Length ==  7 || data.Length ==  8) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(3);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.code39:
				if (data.Length > 1) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(4);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.i25:
				if (data.Length > 1 || data.Length % 2 == 0) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(5);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.codebar:
				if (data.Length > 1) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(6);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.code93: //todo: overload PrintBarcode method with a byte array parameter
				if (data.Length > 1) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(7); //todo: use format 2 (init string : 29,107,72) (0x00 can be a value, too)
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.code128: //todo: overload PrintBarcode method with a byte array parameter
				if (data.Length > 1) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(8); //todo: use format 2 (init string : 29,107,73) (0x00 can be a value, too)
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.code11:
				if (data.Length > 1) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(9);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			case BarcodeType.msi:
				if (data.Length > 1) {
					_writeByte(29);
					_writeByte(107);
					_writeByte(10);
					_serialPort.Write(outputBytes,0,data.Length);
					_writeByte(0);
				}
				break;
			}
		}
		
		/// <summary>
		/// Selects large barcode mode.
		/// </summary>
		/// <param name='large'>
		/// Large barcode mode.
		/// </param>
		public void SetLargeBarcode(bool large)
		{
			if (large) {
				_writeByte(29);
				_writeByte(119);
				_writeByte(3);
			} else {
				_writeByte(29);
				_writeByte(119);
				_writeByte(2);
			}
		}
		
		/// <summary>
		/// Sets the barcode left space.
		/// </summary>
		/// <param name='spacingDots'>
		/// Spacing dots.
		/// </param>
		public void SetBarcodeLeftSpace(byte spacingDots)
		{
				_writeByte(29);
				_writeByte(120);
				_writeByte(spacingDots);
		}
		
		/// <summary>
		/// Prints the image. The image must be 384px wide.
		/// </summary>
		/// <param name='fileName'>
		/// Image file path.
		/// </param>
		public void PrintImage(string fileName)
		{
			
			if (!File.Exists(fileName)) {
				throw(new Exception("File does not exist."));
			}
			
			PrintImage(new Bitmap(fileName));

		}
		
		/// <summary>
		/// Prints the image. The image must be 384px wide.
		/// </summary>
		/// <param name='image'>
		/// Image to print.
		/// </param>
		public void PrintImage(Bitmap image)
		{
			int width = image.Width;
			int height = image.Height;
			
			byte[,] imgArray = new byte[width,height];
			
			if (width != 384 || height > 65635) {
				throw(new Exception("Image width must be 384px, height cannot exceed 65635px."));
			}
			
			//Processing image data	
			for (int y = 0; y < image.Height; y++) {
				for (int x = 0; x < (image.Width/8); x++) {
					imgArray[x,y] = 0;
					for (byte n = 0; n < 8; n++) {
						Color pixel = image.GetPixel(x*8+n,y);
						if (pixel.GetBrightness() < 0.5) {
							imgArray[x,y] += (byte)(1 << n);
						}
					}
				}	
			}
			
			//Print LSB first bitmap
			_writeByte(18);
			_writeByte(118);
			
			_writeByte((byte)(height & 255)); 	//height LSB
			_writeByte((byte)(height >> 8)); 	//height MSB

			
			for (int y = 0; y < height; y++) {
				System.Threading.Thread.Sleep(PictureLineSleepTimeMs);
				for (int x = 0; x < (width/8); x++) {
					_writeByte(imgArray[x,y]);
				}	
			}
		}
		
		/// <summary>
		/// Sets the printing parameters.
		/// </summary>
		/// <param name='maxPrintingDots'>
		/// Max printing dots (0-255), unit: (n+1)*8 dots, default: 7 (beceause (7+1)*8 = 64 dots)
		/// </param>
		/// <param name='heatingTime'>
		/// Heating time (3-255), unit: 10µs, default: 80 (800µs)
		/// </param>
		/// <param name='heatingInterval'>
		/// Heating interval (0-255), unit: 10µs, default: 2 (20µs)
		/// </param>
		public void SetPrintingParameters(byte maxPrintingDots, byte heatingTime, byte heatingInterval)
		{
			_writeByte(27);
			_writeByte(55);	
			_writeByte(maxPrintingDots);
			_writeByte(heatingTime);				
			_writeByte(heatingInterval);
		}
		
		/// <summary>
		/// Sets the printer offine.
		/// </summary>
		public void Sleep()
		{
			_writeByte(27);
			_writeByte(61);
			_writeByte(0);
		}
		
		/// <summary>
		/// Sets the printer online.
		/// </summary>		
		public void WakeUp()
		{
			_writeByte(27);
			_writeByte(61);
			_writeByte(1);
		}
		
		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="ThermalDotNet.ThermalPrinter"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents the current <see cref="ThermalDotNet.ThermalPrinter"/>.
		/// </returns>
		public override string ToString()
		{
			return string.Format("ThermalPrinter:\n\t_serialPort={0},\n\t_maxPrintingDots={1}," +
				"\n\t_heatingTime={2},\n\t_heatingInterval={3},\n\tPictureLineSleepTimeMs={4}," +
				"\n\tWriteLineSleepTimeMs={5},\n\tEncoding={6}", _serialPort.PortName , _maxPrintingDots,
				_heatingTime, _heatingInterval, PictureLineSleepTimeMs, WriteLineSleepTimeMs, Encoding);
		}
		
		/// <summary>
		/// Returns a printing style.
		/// </summary>
		public enum PrintingStyle
		{
			/// <summary>
			/// White on black.
			/// </summary>
			Reverse = 1 << 1,
			/// <summary>
			/// Updown characters.
			/// </summary>
			Updown = 1 << 2,
			/// <summary>
			/// Bold characters.
			/// </summary>
			Bold = 1 << 3,
			/// <summary>
			/// Double height characters.
			/// </summary>
			DoubleHeight = 1 << 4,
			/// <summary>
			/// Double width characters.
			/// </summary>
			DoubleWidth = 1 << 5,
			/// <summary>
			/// Strikes text.
			/// </summary>
			DeleteLine = 1 << 6,
			/// <summary>
			/// Thin underline.
			/// </summary>
			Underline = 1 << 0,
			/// <summary>
			/// Thick underline.
			/// </summary>
			ThickUnderline = 1 << 7
		}
		
		/// <summary>
		/// Prints the contents of the buffer and feeds n dots.
		/// </summary>
		/// <param name='dotsToFeed'>
		/// Number of dots to feed.
		/// </param>
		public void FeedDots(byte dotsToFeed)
		{
			_writeByte(27);
			_writeByte(74);
			_writeByte(dotsToFeed);
		}
		
		private void _writeByte(byte valueToWrite)
		{
			byte[] tempArray = {valueToWrite};
			_serialPort.Write(tempArray,0,1);
		}
		
		private void _sendEncoding(string encoding)
		{
			switch (encoding)
			{
				case "IBM437":
					_writeByte(27);
					_writeByte(116);
					_writeByte(0);
					break;
				case "ibm850":
					_writeByte(27);
					_writeByte(116);
					_writeByte(1);
					break;				
			}
		}
		
		/// <summary>
        /// Tests the value of a given bit.
        /// </summary>
        /// <param name="valueToTest">The value to test</param>
        /// <param name="testBit">The bit number to test</param>
        /// <returns></returns>
        static private bool _BitTest(byte valueToTest, int testBit)
        {
            return ((valueToTest & (byte)(1 << testBit)) == (byte)(1 << testBit));
        }
		
		/// <summary>
        /// Return the given value with its n bit set.
        /// </summary>
        /// <param name="originalValue">The value to return</param>
        /// <param name="bit">The bit number to set</param>
        /// <returns></returns>
        static private byte _BitSet(byte originalValue, byte bit)
        {
            return originalValue |= (byte)((byte)1 << bit);
        }

        /// <summary>
        /// Return the given value with its n bit cleared.
        /// </summary>
        /// <param name="originalValue">The value to return</param>
        /// <param name="bit">The bit number to clear</param>
        /// <returns></returns>
        static private byte _BitClear(byte originalValue, int bit)
        {
            return originalValue &= (byte)(~(1 << bit));
        }  
	}
}

