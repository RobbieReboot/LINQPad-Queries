<Query Kind="Program">
  <NuGetReference>Azure.Storage.Blobs</NuGetReference>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>static System.Text.Json.JsonElement</Namespace>
  <Namespace>System.Drawing.Drawing2D</Namespace>
</Query>

void Main()
{
	string json = "{\"Id\":null,\"Merchant\":{\"Name\":\"Bridgehouse Garage\",\"NameConfidence\":0.957,\"NameBounds\":[{\"IsEmpty\":false,\"X\":121,\"Y\":156},{\"IsEmpty\":false,\"X\":575,\"Y\":154},{\"IsEmpty\":false,\"X\":575,\"Y\":207},{\"IsEmpty\":false,\"X\":121,\"Y\":208}],\"Address\":\"\",\"Phone\":\"\u002B441246810600\",\"PhoneBounds\":[{\"IsEmpty\":false,\"X\":231,\"Y\":566},{\"IsEmpty\":false,\"X\":548,\"Y\":564},{\"IsEmpty\":false,\"X\":548,\"Y\":621},{\"IsEmpty\":false,\"X\":231,\"Y\":623}],\"City\":\"Sheffield\",\"Street\":null,\"BuildingNumber\":null,\"PostCode\":\"S21 3WA\",\"AddressConfidence\":0.823,\"PhoneConfidence\":0.988,\"AddressBounds\":[{\"IsEmpty\":false,\"X\":111,\"Y\":222},{\"IsEmpty\":false,\"X\":446,\"Y\":216},{\"IsEmpty\":false,\"X\":451,\"Y\":474},{\"IsEmpty\":false,\"X\":116,\"Y\":480}]},\"TransactionDate\":\"2022-12-03T00:00:00+00:00\",\"TransactionDateBounds\":[{\"IsEmpty\":false,\"X\":145,\"Y\":1002},{\"IsEmpty\":false,\"X\":400,\"Y\":1001},{\"IsEmpty\":false,\"X\":399,\"Y\":1058},{\"IsEmpty\":false,\"X\":145,\"Y\":1058}],\"TransactionDateConfidence\":0.988,\"Items\":[{\"Description\":\"Pump 1: SUPER ULD\",\"DescriptionBounds\":[{\"IsEmpty\":false,\"X\":94,\"Y\":1283},{\"IsEmpty\":false,\"X\":519,\"Y\":1279},{\"IsEmpty\":false,\"X\":519,\"Y\":1334},{\"IsEmpty\":false,\"X\":95,\"Y\":1338}],\"DescriptionConfidence\":0.983,\"Price\":173.9,\"PriceBounds\":[{\"IsEmpty\":false,\"X\":419,\"Y\":1351},{\"IsEmpty\":false,\"X\":546,\"Y\":1353},{\"IsEmpty\":false,\"X\":545,\"Y\":1408},{\"IsEmpty\":false,\"X\":418,\"Y\":1406}],\"PriceConfidence\":0.986,\"TotalPrice\":51,\"TotalPriceBounds\":[{\"IsEmpty\":false,\"X\":850,\"Y\":1353},{\"IsEmpty\":false,\"X\":1001,\"Y\":1353},{\"IsEmpty\":false,\"X\":1001,\"Y\":1405},{\"IsEmpty\":false,\"X\":850,\"Y\":1405}],\"TotalPriceConfidence\":0.984,\"Quantity\":29.33,\"QuantityBounds\":[{\"IsEmpty\":false,\"X\":148,\"Y\":1351},{\"IsEmpty\":false,\"X\":275,\"Y\":1351},{\"IsEmpty\":false,\"X\":274,\"Y\":1404},{\"IsEmpty\":false,\"X\":148,\"Y\":1404}],\"QuantityConfidence\":0.983}],\"SubTotal\":42.5,\"SubTotalBounds\":[{\"IsEmpty\":false,\"X\":392,\"Y\":1999},{\"IsEmpty\":false,\"X\":517,\"Y\":2001},{\"IsEmpty\":false,\"X\":516,\"Y\":2051},{\"IsEmpty\":false,\"X\":392,\"Y\":2049}],\"SubTotalConfidence\":0.986,\"TotalTax\":8.5,\"TotalTaxBounds\":[{\"IsEmpty\":false,\"X\":648,\"Y\":2002},{\"IsEmpty\":false,\"X\":749,\"Y\":2002},{\"IsEmpty\":false,\"X\":749,\"Y\":2054},{\"IsEmpty\":false,\"X\":648,\"Y\":2054}],\"TotalTaxConfidence\":0.987,\"TotalPrice\":51,\"TotalPriceBounds\":[{\"IsEmpty\":false,\"X\":791,\"Y\":1496},{\"IsEmpty\":false,\"X\":952,\"Y\":1496},{\"IsEmpty\":false,\"X\":951,\"Y\":1549},{\"IsEmpty\":false,\"X\":791,\"Y\":1547}],\"TotalPriceConfidence\":0.985}";
	JsonDocument jdoc = JsonDocument.Parse(json);
	JsonElement root = jdoc.RootElement;
	JsonElement merchant = jdoc.RootElement.GetProperty("Merchant");
	var mName = merchant.GetProperty("Name").GetString();
	var mConf = merchant.GetProperty("NameConfidence").GetDouble().ToString();

	//var merchBounds = getBounds(merchant.GetProperty("NameBounds").EnumerateArray());

	// Load an image
	Bitmap image = new Bitmap(@"C:\projects\Chevin\Assets\TestData\Receipts\06_12_2022, 19_56 Microsoft Lens.jpg");         // Morrisons petrol
																							//Bitmap image = new Bitmap(@"d:\dumpzone\10_12_2022, 15_46 Microsoft Lens(1).jpg");		// Costa 
	Annotate(image, merchant, "NameBounds", "NameConfidence");
	Annotate(image, merchant, "PhoneBounds", "PhoneConfidence");
	Annotate(image, merchant, "AddressBounds", "AddressConfidence");
	Annotate(image, root, "TransactionDateBounds", "TransactionDateConfidence");
	Annotate(image, root, "SubTotalBounds", "SubTotalConfidence");
	Annotate(image, root, "TotalTaxBounds", "TotalTaxConfidence");
	Annotate(image, root, "TotalPriceBounds", "TotalPriceConfidence");

	var items = root.GetProperty("Items").EnumerateArray();
	foreach (var item in items)
	{
		Annotate(image, item, "DescriptionBounds", "DescriptionConfidence");
		Annotate(image, item, "PriceBounds", "PriceConfidence");
		Annotate(image, item, "TotalPriceBounds", "TotalPriceConfidence");
		Annotate(image, item, "QuantityBounds", "QuantityConfidence");
	}
	// Save the image
	image.Save(@"d:\dumpzone\image_with_text_and_rectangle.jpg");

}
void Annotate(Bitmap image, JsonElement element, string boundsName, string confName)
{
	using (Graphics graphics = Graphics.FromImage(image))
	{
		int confBoxMargin = 4;
		Font font = new Font("Arial", 16);
		string confString;
		double conf;
		List<dynamic> bounds;
		try
		{
			// if there's something missing, we ignore it and just don't annotate the image.
			var prop = element.GetProperty(boundsName);
			if (prop.ValueKind==JsonValueKind.Null)
				return;
			bounds = getBounds(element.GetProperty(boundsName).EnumerateArray());
			conf = element.GetProperty(confName).GetDouble();
			confString = conf.ToString();
		}
		catch (KeyNotFoundException ex)
		{
			return;
		}
		var boundsLeft = bounds[0].X;
		var boundsWidth = bounds[2].X - bounds[0].X;
		var boundsTop = bounds[0].Y;
		var boundsHeight = bounds[2].Y - bounds[0].Y;

		Brush boxBrush;
		if (conf < 0.5)
			boxBrush = Brushes.Red;
		else if (conf < 0.8)
			boxBrush = Brushes.OrangeRed;
		else if (conf < 0.9)
			boxBrush = Brushes.Orange;
		else
			boxBrush = Brushes.Green;

		var boxPen = new Pen(Color.Blue, 3);
		boxPen.DashStyle = DashStyle.Dash;

		graphics.DrawRectangle(boxPen, boundsLeft, boundsTop, boundsWidth, boundsHeight);
		var confBox = graphics.MeasureString(confString, font);
		var confBoxWidth = (int)Math.Round(confBox.Width, MidpointRounding.AwayFromZero);
		var confBoxHeight = (int)Math.Round(confBox.Height, MidpointRounding.AwayFromZero);
		// Draw some text
		graphics.FillRectangle(boxBrush, new Rectangle(boundsLeft + boundsWidth - confBoxWidth,
															boundsTop + boundsHeight + confBoxMargin,
															confBoxWidth,
															confBoxHeight
															));
		graphics.DrawString(confString, font, Brushes.White, new PointF(boundsLeft + boundsWidth - confBoxWidth, boundsTop + boundsHeight + confBoxMargin));
	}
}
List<dynamic> getBounds(JsonElement.ArrayEnumerator nb)
{
	return nb.Select(p => new { X = p.GetProperty("X").GetInt32(), Y = p.GetProperty("Y").GetInt32() }).ToList<dynamic>();
}

