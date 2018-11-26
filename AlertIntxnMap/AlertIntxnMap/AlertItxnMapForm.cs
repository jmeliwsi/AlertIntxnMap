using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WSIMap;
using Newtonsoft.Json;
using System.IO;

namespace AlertIntxnMap
{
    public partial class AlertItxnMapForm : Form
    {
        private VectorFile basemapl = null;
        private VectorFile basemapw = null;
        private WSIMap.Layer basemapLayerW;
        private WSIMap.Layer basemapLayerL;
        private WSIMap.FeatureCollection basemapFC = null;
        private WSIMap.Layer drawingLayer = null;
        WSIMap.Font arial12Font = null;
        private GridPoints gridPoints = null;
        private short centralLongitude = Projection.DefaultCentralLongitude;
        private const double POINT_HAZARD_DEFAULT_RADIUS_MILES = 75 * 1.15078; // 75 nautical miles

        public AlertItxnMapForm()
        {
            InitializeComponent();

            // Set up ContextState here because the Designer always deletes this code
            this.contextState1 = new WSIMap.ContextState();
            this.contextState1.Location = new System.Drawing.Point(13, 13);
            this.contextState1.Name = "contextState1";
            this.contextState1.Size = new System.Drawing.Size(24, 24);
            this.contextState1.TabIndex = 1;
            this.contextState1.Visible = false;
        }

        private void AlertIntxnMapForm_Load(object sender, EventArgs e)
        {
            // Initialize the map control
            mapGL.BackColor = Color.FromArgb(204, 230, 254);
            mapGL.InitializeContexts();

            #region Base Map Layers
            // Countries, filled and stenciled.  Stenciling constrains the terrain layer to the filled area.
            basemapl = new VectorFile(@"..\Data\50m_admin_0_countries.shp", "Countries", "Natural Earth 1:50m countries");
            basemapl.Stencil = true;
            basemapl.Fill = true;
            basemapl.Border = false;
            basemapl.BorderColor = Color.Black;
            basemapl.FillColor = Color.FromArgb(150, 150, 150);//ColorTables.ByteQuad.ToColor(ColorTables.ColorTable_elevgbsncap[3]);
            basemapLayerL = new Layer("BasemapCountries-Land", string.Empty);
            basemapLayerL.Visible = true;
            basemapFC = new FeatureCollection();
            basemapFC.Shared = true;
            basemapFC.Add(basemapl);
            basemapLayerL.Features = basemapFC;
            mapGL.Layers.Insert(basemapLayerL, 0);

            // Countries, unfilled.  Adding this layer twice allow us to put boundaries on top
            // of the terrain and to toggle the map between color and wire frame very quickly.
            VectorFile basemapc = new VectorFile(@"..\Data\50m_admin_0_countries.shp", "Countries", "Natural Earth 1:50m countries");
            basemapc.Stencil = false;
            basemapc.Fill = false;
            basemapc.Border = true;
            basemapc.BorderColor = Color.Black;
            basemapc.FillColor = Color.FromArgb(150, 150, 150);//ColorTables.ByteQuad.ToColor(ColorTables.ColorTable_elevgbsncap[3]);
            Layer basemapLayerC = new Layer("BasemapCountries-Borders", string.Empty);
            basemapLayerC.Visible = true;
            basemapLayerC.Features.Add(basemapc);
            mapGL.Layers.Insert(basemapLayerC, 0);

            // States and provinces.
            VectorFile states = new VectorFile(@"..\Data\50m-admin-1-states-provinces-lines-shp.shp", "States & Provinces", "Natural Earth 1:50m states and provinces");
            states.Stencil = false;
            states.Fill = false;
            states.Border = true;
            states.BorderColor = Color.Black;
            states.FillColor = Color.FromArgb(230, 218, 204);
            Layer statesLayer = new Layer("BasemapStatesProvinces", string.Empty);
            statesLayer.Visible = true;
            statesLayer.Features.Add(states);
            mapGL.Layers.Insert(statesLayer, 0);

            // Lakes, filled.
            basemapw = new VectorFile(@"..\Data\50m-lakes.shp", "Lakes", "Natural Earth 1:50m lakes");
            basemapw.Stencil = false;
            basemapw.Fill = true;
            basemapw.Border = false;
            basemapw.BorderColor = Color.Black;
            basemapw.FillColor = Color.FromArgb(204, 230, 254);
            basemapw.FillColor2 = Color.FromArgb(150, 150, 150);
            basemapw.UseTwoFillColors = true;
            basemapFC.Add(basemapw);
            basemapLayerW = new Layer("BasemapLakes-Water", string.Empty);
            basemapLayerW.Visible = true;
            basemapLayerW.Features.Add(basemapw);
            mapGL.Layers.Insert(basemapLayerW, 0);

            // Lakes, unfilled.  Adding this layer twice allows us to toggle the map between
            // color and wire frame very quickly.
            VectorFile basemaps = new VectorFile(@"..\Data\50m-lakes.shp", "Lakes", "Natural Earth 1:50m lakes");
            basemaps.Stencil = false;
            basemaps.Fill = false;
            basemaps.Border = true;
            basemaps.BorderColor = Color.Black;
            basemaps.FillColor = Color.FromArgb(204, 230, 254);
            Layer basemapLayerS = new Layer("BasemapLakes-Shoreline", string.Empty);
            basemapLayerS.Visible = true;
            basemapLayerS.Features.Add(basemaps);
            mapGL.Layers.Insert(basemapLayerS, 0);

            mapGL.Refresh();
            #endregion

            // Create font for labels
            arial12Font = new WSIMap.Font("Arial", 12, false);

            // Create a layer for drawing features
            drawingLayer = new Layer("Drawing", String.Empty);
            mapGL.Layers.Insert(drawingLayer, 0);

            // Set the map extents and initial rectangle
            WSIMap.RectangleD mapExtents = new WSIMap.RectangleD(-90, 90, -180, 180);
            mapGL.SetMapExtents(mapExtents);
            WSIMap.RectangleD mapRect = new WSIMap.RectangleD(24, 51, -128, -64);
            mapGL.SetMapRectangle(mapRect);

            // GridPoints object
            Layer gridPointsLayer = new Layer("GridPointsLayer", string.Empty);
            gridPoints = new GridPoints(SymbolType.Plus, Color.Black, 2);
            gridPointsLayer.Features.Add(gridPoints);
            gridPointsLayer.UseToolTips = true;
            mapGL.Layers.Insert(gridPointsLayer, 0);
            gridPoints.Visible = false;

            // Turn on lat/lon grid line
            mapGL.LatLonGrid = true;

            // Set the map projection; doing this last sets projection on all features
            mapGL.SetMapProjection(MapProjections.CylindricalEquidistant, Projection.DefaultCentralLongitude);
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            // If this method returns true mapGLKeyDown is not called;
            // if it returns false mapGLKeyDown will be called.
            return false;
        }

        private void SetMapProjection(MapProjections mapProjection)
        {
            switch (mapProjection)
            {
                case MapProjections.Stereographic:
                    mapGL.DisableDateLinePanning = true;
                    mapGL.SetMapProjection(MapProjections.Stereographic, centralLongitude);
                    break;
                case MapProjections.Orthographic:
                    mapGL.DisableDateLinePanning = true;
                    mapGL.SetMapProjection(MapProjections.Orthographic, centralLongitude);
                    break;
                case MapProjections.Mercator:
                    mapGL.DisableDateLinePanning = false;
                    mapGL.SetMapProjection(MapProjections.Mercator, Projection.DefaultCentralLongitude);
                    break;
                //case MapProjections.Lambert:
                //	mapGL.DisableDateLinePanning = true;
                //	mapGL.SetMapProjection(MapProjections.Lambert, centralLongitude);
                //	break;
                case MapProjections.CylindricalEquidistant:
                default:
                    mapGL.DisableDateLinePanning = false;
                    mapGL.SetMapProjection(MapProjections.CylindricalEquidistant, Projection.DefaultCentralLongitude);
                    break;
            }
        }

        private void mapGL_MouseDown(object sender, MouseEventArgs e)
        {
            mapGL.Panning(true);
        }

        private void mapGL_MouseUp(object sender, MouseEventArgs e)
        {
            mapGL.Panning(false);
            mapGL.Refresh();
        }

        private void mapGL_MouseMove(object sender, MouseEventArgs e)
        {
            // Display lon/lat of mouse position in status bar
            PointD pt = mapGL.ToMapPoint(e.X, e.Y);
            statusStrip.Items[1].Text = pt.ToString();
        }

        private void mapGL_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void toolStripButtonGridLines_Click(object sender, EventArgs e)
        {
            mapGL.LatLonGrid = !mapGL.LatLonGrid;
            mapGL.Refresh();
        }

        private void toolStripButtonLoadIntersectionsLogFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Cursor.Current = Cursors.WaitCursor;
                string[] lines = File.ReadAllLines(openFileDialog.FileName);
                comboBoxIntersectionLogEntries.Text = string.Empty;
                comboBoxIntersectionLogEntries.Items.Clear();
                foreach (string line in lines)
                {
                    if (line.Contains("REDUNDANT"))
                        continue;
                    comboBoxIntersectionLogEntries.Items.Add(line);
                }
            }
        }

        private void toolStripButtonClearMap_Click(object sender, EventArgs e)
        {
            comboBoxIntersectionLogEntries.Text = string.Empty;
            statusStrip.Items[0].Text = string.Empty;
            drawingLayer.Features.Clear(true);
            mapGL.Refresh();
        }

        private void comboBoxIntersectionLogEntries_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Get intersection log entry text from the combobox and clear the map
            string logEntry = comboBoxIntersectionLogEntries.Text;
            if (string.IsNullOrWhiteSpace(logEntry))
            {
                MessageBox.Show("The log entry is blank - nothing to draw.");
                return;
            }
            drawingLayer.Features.Clear(true, true);
            mapGL.Refresh();

            // Convert the intersection log entry to an object
            Intersection intxn = null;
            try
            {
                logEntry = logEntry.Remove(0, logEntry.IndexOf('{')); // remove non-JSON part
                intxn = JsonConvert.DeserializeObject<Intersection>(logEntry);
                if (intxn == null)
                    throw (new Exception("Intersection object is null."));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            // Display information in the status bar
            string statusBarText = intxn.flightPlan.key.icao + intxn.flightPlan.key.fn + " " + intxn.flightPlan.key.sdi + "-" + intxn.flightPlan.dst + " "
                + intxn.flightPlan.key.sdt + " " + GetHazardType(intxn).ToString();
            statusStrip.Items[0].Text = statusBarText;

            // Draw the intersection scenario
            DrawHazard(GetHazardType(intxn), intxn);
            DrawFlightPlan(intxn);
            DrawActiveRoute(intxn);
            DrawAircraft(intxn);
            DrawIntersectionPoint(intxn);
            mapGL.Refresh();
        }

        private HazardType GetHazardType (Intersection intxn)
        {
            HazardType hazardType;

            // Determine the type of hazard (weather advisory) contained in the Intersection object
            if (intxn.weatherAdvisory.points != null && intxn.weatherAdvisory.movingSpeed != null)
                hazardType = HazardType.SIGMET;
            else if (intxn.weatherAdvisory.points != null && intxn.weatherAdvisory.movingSpeed == null)
                hazardType = HazardType.FPG;
            else if (intxn.weatherAdvisory.hazardMetric != 0 && intxn.weatherAdvisory.rmsLoad != 0)
                hazardType = HazardType.TAPS;
            else if (intxn.weatherAdvisory.radius != 0 && !string.IsNullOrWhiteSpace(intxn.weatherAdvisory.hazardId))
                hazardType = HazardType.TBCA;
            else if (!string.IsNullOrWhiteSpace(intxn.weatherAdvisory.turbulenceText))
                hazardType = HazardType.PIREP;
            else
                hazardType = HazardType.UNKNOWN;

            return hazardType;
        }

        private void DrawHazard(HazardType hazardType, Intersection intxn)
        {
            switch (hazardType)
            {
                case HazardType.TAPS:
                case HazardType.PIREP:
                case HazardType.TBCA:
                    DrawPointHazard(hazardType, intxn);
                    break;
                case HazardType.SIGMET:
                case HazardType.FPG:
                    DrawPolygonHazard(hazardType, intxn);
                    break;
                default:
                    break;
            }
        }

        private void DrawPointHazard(HazardType hazardType, Intersection intxn)
        {
            PointD hazardPt = new PointD(intxn.weatherAdvisory.lon, intxn.weatherAdvisory.lat);
            Circle hazardCircle = new Circle(hazardPt, POINT_HAZARD_DEFAULT_RADIUS_MILES, Color.Red, 1, Color.Red, 50);
            hazardCircle.Refresh(MapProjections.CylindricalEquidistant, centralLongitude);
            Symbol hazardSymbol = new Symbol(SymbolType.EquilateralTriangle, Color.Black, 7, hazardPt, 0);
            drawingLayer.Features.Add(hazardCircle);
            drawingLayer.Features.Add(hazardSymbol);

            // Draw hazard label
            StringBuilder hazardLabelText = new StringBuilder();
            hazardLabelText.Append(Enum.GetName(typeof(HazardType), hazardType)).Append(Environment.NewLine);
            hazardLabelText.Append(intxn.weatherAdvisory.validTime).Append(Environment.NewLine);
            if (intxn.weatherAdvisory.flightLevel != 0)
                hazardLabelText.Append("alt: ").Append(intxn.weatherAdvisory.flightLevel);
            else if (intxn.weatherAdvisory.lowerFlightLevel != intxn.weatherAdvisory.upperFlightLevel)
                hazardLabelText.Append("alt: ").Append(intxn.weatherAdvisory.lowerFlightLevel).Append("-").Append(intxn.weatherAdvisory.upperFlightLevel);
            else
                hazardLabelText.Append("alt: ").Append(intxn.weatherAdvisory.lowerFlightLevel);
            WSIMap.Label hazardLabelPt = new WSIMap.Label(arial12Font, hazardLabelText.ToString(), Color.Black, hazardPt.Latitude, hazardPt.Longitude, 10, 0, true, Color.Yellow);
            drawingLayer.Features.Add(hazardLabelPt);
        }

        private void DrawPolygonHazard(HazardType hazardType, Intersection intxn)
        {
            Polygon hazardPolygon = new Polygon(PtListToPointDList(intxn.weatherAdvisory.points), Color.Red, 1, Color.Red, 50);
            hazardPolygon.Refresh(MapProjections.CylindricalEquidistant, centralLongitude);
            drawingLayer.Features.Add(hazardPolygon);

            // Draw hazard label
            PointD hazardPt = new PointD(intxn.weatherAdvisory.points[0].lon, intxn.weatherAdvisory.points[0].lat);
            StringBuilder hazardLabelText = new StringBuilder();
            hazardLabelText.Append(Enum.GetName(typeof(HazardType), hazardType)).Append(Environment.NewLine);
            hazardLabelText.Append("issued: ").Append(intxn.weatherAdvisory.issueTime).Append(Environment.NewLine);
            hazardLabelText.Append("start: ").Append(intxn.weatherAdvisory.validStart).Append(Environment.NewLine);
            hazardLabelText.Append("end: ").Append(intxn.weatherAdvisory.validEnd).Append(Environment.NewLine);
            hazardLabelText.Append("upper FL: ").Append(intxn.weatherAdvisory.upperFlightLevel).Append(Environment.NewLine);
            hazardLabelText.Append("lower FL: ").Append(intxn.weatherAdvisory.lowerFlightLevel);
            WSIMap.Label hazardLabelPg = new WSIMap.Label(arial12Font, hazardLabelText.ToString(), Color.Black, hazardPt.Latitude, hazardPt.Longitude, -50, 0, true, Color.Yellow);
            drawingLayer.Features.Add(hazardLabelPg);
        }

        private void DrawFlightPlan(Intersection intxn)
        {
            Curve flightPlan = new Curve(PtListToPointDList(intxn.flightPlan.pts), Color.Blue, 1, Curve.CurveType.SolidWithPoints);
            flightPlan.Refresh(MapProjections.CylindricalEquidistant, Projection.DefaultCentralLongitude);
            drawingLayer.Features.Add(flightPlan);
        }

        private void DrawActiveRoute(Intersection intxn)
        {
            if (intxn.flightState == null) return;

            Curve activeRoute = new Curve(PtListToPointDList(intxn.flightState.pts), Color.DarkGreen, 1, Curve.CurveType.SolidWithPoints);
            activeRoute.Refresh(MapProjections.CylindricalEquidistant, Projection.DefaultCentralLongitude);
            drawingLayer.Features.Add(activeRoute);
        }

        private void DrawAircraft(Intersection intxn)
        {
            if (intxn.flightState == null) return;

            Symbol aircraft = new Symbol(SymbolType.Plane, Color.Blue, 7, intxn.flightState.plat, intxn.flightState.plon, intxn.flightState.phd);
            drawingLayer.Features.Add(aircraft);

            // Draw the aircraft label
            StringBuilder aircraftLabelText = new StringBuilder();
            aircraftLabelText.Append(intxn.flightState.fn).Append(Environment.NewLine);
            aircraftLabelText.Append(intxn.flightState.dep).Append(" - ").Append(intxn.flightState.dst).Append(Environment.NewLine);
            aircraftLabelText.Append("dt: ").Append(intxn.flightState.dt).Append(Environment.NewLine);
            aircraftLabelText.Append("eta: ").Append(intxn.flightState.eta).Append(Environment.NewLine);
            aircraftLabelText.Append("aalt: ").Append(intxn.flightState.aalt).Append(Environment.NewLine);
            aircraftLabelText.Append("palt: ").Append(intxn.flightState.palt).Append(Environment.NewLine);
            aircraftLabelText.Append("pt: ").Append(intxn.flightState.pt);
            WSIMap.Label aircraftLabel = new WSIMap.Label(arial12Font, aircraftLabelText.ToString(), Color.Black, intxn.flightState.plat, intxn.flightState.plon, 10, 10, true, Color.White);
            drawingLayer.Features.Add(aircraftLabel);

            // Center the map on the aircraft
            mapGL.SetMapCenter(new PointD(intxn.flightState.plon, intxn.flightState.plat));
        }

        private void DrawIntersectionPoint(Intersection intxn)
        {
            Symbol intersectionSymbol = new Symbol(SymbolType.Plus, Color.Black, 7, intxn.intersectPosition.lat, intxn.intersectPosition.lon, 45);
            drawingLayer.Features.Add(intersectionSymbol);

            // Draw the intersection label
            StringBuilder intersectionLabelText = new StringBuilder();
            intersectionLabelText.Append("Intersection").Append(Environment.NewLine).Append(intxn.intersectPosition.time).Append(Environment.NewLine);
            intersectionLabelText.Append("alt: ").Append(intxn.intersectPosition.alt);
            WSIMap.Label intersectionLabel = new WSIMap.Label(arial12Font, intersectionLabelText.ToString(), Color.Black, intxn.intersectPosition.lat, intxn.intersectPosition.lon, 10, 0, true, Color.LightGreen);
            drawingLayer.Features.Add(intersectionLabel);
        }

        private List<PointD> PtListToPointDList(List<Pt> ptList)
        {
            List<PointD> pointList = new List<PointD>();
            foreach (Pt pt in ptList)
                pointList.Add(new PointD(pt.lon, pt.lat));
            return pointList;
        }
    }
}
