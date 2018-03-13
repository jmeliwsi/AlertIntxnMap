using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace WSIMap
{
	public static class MosaicATMAirportLayoutFile
	{
		public static List<Layer> LoadAirportLayoutFile(string fileName)
		{
			return LoadAirportLayoutFile(fileName, string.Empty);
		}

		public static List<Layer> LoadAirportLayoutFile(string fileName, string password)
		{
			try
			{
				XmlTextReader regionReader = null;
				List<Layer> layerList = new List<Layer>();
				System.IO.MemoryStream memStream = null;
				bool zipFileExeption = false;

				// If a password was provided, the file is an encrypted zip file, otherwise it's text/XML
				if (!string.IsNullOrWhiteSpace(password))
				{
					try
					{
						// ZipFile.Read throws exceptions for unknown reasons sometimes
						using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(fileName))
						{
							memStream = new System.IO.MemoryStream();
							zip[0].ExtractWithPassword(memStream, password);
						}
					}
					catch
					{
						zipFileExeption = true;
					}

					// If an exception was throw above, try to read the zip file again. If it throws again, return null.
					if (zipFileExeption)
					{
						using (Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(fileName))
						{
							memStream = new System.IO.MemoryStream();
							zip[0].ExtractWithPassword(memStream, password);
						}
					}

					memStream.Seek(0, System.IO.SeekOrigin.Begin);
					regionReader = new XmlTextReader(memStream);
				}
				else
					regionReader = new XmlTextReader(fileName);

				// Iterate over all the regions (layers)
				while (regionReader.Read())
				{
					if (!regionReader.IsStartElement())
						continue;

					// Did we find a new region?
					if (string.Compare(regionReader.Name, "map:Region", StringComparison.CurrentCultureIgnoreCase) == 0)
					{
						Layer layer = new Layer(regionReader.GetAttribute("type"), string.Empty);

						// Create a reader to read all the polygons or polylines in this region
						XmlReader polyReader = regionReader.ReadSubtree();
						
						// Iterate over all the polygons in the region
						while (polyReader.Read())
						{
							if (!polyReader.IsStartElement())
								continue;

							// Did we find a new polygon?
							if (string.Compare(polyReader.Name, "Polygon", StringComparison.CurrentCultureIgnoreCase) == 0)
							{
								Polygon p = new Polygon();
								p.FeatureName = polyReader.GetAttribute("name");

								// Create a reader to read all the points in this polygon
								XmlReader ptReader = polyReader.ReadSubtree();

								// Iteration over all the points in the polygon
								while (ptReader.Read())
								{
									if (!ptReader.IsStartElement())
										continue;

									// Did we find a new point?
									if (string.Compare(ptReader.Name, "Point", StringComparison.CurrentCultureIgnoreCase) == 0)
									{
										double x = double.Parse(ptReader.GetAttribute("x"));
										double y = double.Parse(ptReader.GetAttribute("y"));
										if (y >= -180 && y <= 180 && x >= -90 && x <= 90) // check validity of x & y
											p.Add(new PointD(-y, x)); // assume west longitude, y is lon, x is lat
									}
								}

								// Close the point reader
								ptReader.Close();

								// Add the polygon to the layer
								if (p.Count > 0)
									layer.Features.Add(p);
							}

							// Did we find a new polyline?
							if (string.Compare(polyReader.Name, "Polyline", StringComparison.CurrentCultureIgnoreCase) == 0)
							{
								Curve c = new Curve();
								c.FeatureName = polyReader.GetAttribute("name");

								// Create a reader to read all the points in this polyline
								XmlReader ptReader = polyReader.ReadSubtree();

								// Iteration over all the points in the polyline
								while (ptReader.Read())
								{
									if (!ptReader.IsStartElement())
										continue;

									// Did we find a new point?
									if (string.Compare(ptReader.Name, "Point", StringComparison.CurrentCultureIgnoreCase) == 0)
									{
										double x = double.Parse(ptReader.GetAttribute("x"));
										double y = double.Parse(ptReader.GetAttribute("y"));
										if (y >= -180 && y <= 180 && x >= -90 && x <= 90) // check validity of x & y
											c.Add(new PointD(-y, x)); // assume west longitude, y is lon, x is lat
									}
								}

								// Close the point reader
								ptReader.Close();

								// Add the polyline to the layer
								if (c.Count > 0)
									layer.Features.Add(c);
							}

						}

						// Close the poly reader
						polyReader.Close();

						// Add the layer to the output list
						if (layer.Features.Count > 0)
							layerList.Add(layer);
					}
				}

				// Close the MemoryStream if applicable
				if (memStream != null)
				{
					memStream.Close();
					memStream.Dispose();
				}

				// Close the main reader
				regionReader.Close();

				// Return list of map layers
				return layerList;
			}
			catch
			{
				return null;
			}
		}
	}
}
