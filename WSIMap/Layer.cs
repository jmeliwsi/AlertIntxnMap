using System;
using System.Collections;


namespace WSIMap
{
	/**
	 * \class Layer
	 * \brief A layer holds a collection of map features and the map holds
	 * a collection of layers
	 */
	public class Layer
	{
		#region Data Members
		protected FeatureCollection features;
		protected string layerName;
		protected string layerInfo;
		protected bool visible;
		protected FUL.Utils.ZoomLevelType minLayerZoomLevel;
		protected FUL.Utils.ZoomLevelType maxLayerZoomLevel;
		protected bool drawn;
		protected bool declutter;
		protected bool useToolTips;
		protected bool dirty;
		protected bool showOne;			// Only display one context menu & tooltip (the first feature) when there are multiple ones around the mouse 
		protected int tooltipOrder;         // Decide the order of tooltip displayed
        #endregion

        public bool DeclutterPaused { get; set; }

        public Layer()
			: this(string.Empty, string.Empty, FUL.Utils.ZoomLevelType.None, FUL.Utils.ZoomLevelType.None)
		{
		}

		public Layer(string layerName, string layerInfo)
			: this(layerName, layerInfo, FUL.Utils.ZoomLevelType.None, FUL.Utils.ZoomLevelType.None)
		{
		}

		public Layer(string layerName, string layerInfo, FUL.Utils.ZoomLevelType maxLayerZoomLevel)
			: this(layerName, layerInfo, FUL.Utils.ZoomLevelType.None, maxLayerZoomLevel)
		{
		}

		public Layer(string layerName, string layerInfo, FUL.Utils.ZoomLevelType minLayerZoomLevel, FUL.Utils.ZoomLevelType maxLayerZoomLevel)
		{
			this.layerName = layerName;
			this.layerInfo = layerInfo;
			this.visible = true;
			this.minLayerZoomLevel = minLayerZoomLevel;
			this.maxLayerZoomLevel = maxLayerZoomLevel;
			this.declutter = false;
			this.features = new FeatureCollection();
			this.dirty = false;
			this.showOne = false;
			this.tooltipOrder = 0;
		}

		public FeatureCollection Features
		{
			get { return features; }
			set
			{
				features = value;
			}
		}

		public string LayerName
		{
			get { return layerName; }
			set { layerName = value; }
		}

		public string LayerInfo
		{
			get { return layerInfo; }
			set { layerInfo = value; }
		}

		public bool Visible
		{
			get { return visible; }
			set { visible = value; }
		}

		public FUL.Utils.ZoomLevelType MinZoomLevel
		{
			get { return minLayerZoomLevel; }
			set { minLayerZoomLevel = value; }
		}

		public FUL.Utils.ZoomLevelType MaxZoomLevel
		{
			get { return maxLayerZoomLevel; }
			set { maxLayerZoomLevel = value; }
		}

		public bool Declutter
		{
			get { return declutter; }
			set
			{
				declutter = value;
				ResetDeclutteredFeatures();
			}
		}

		public bool Drawn
		{
			get { return drawn; }
		}

		public bool UseToolTips
		{
			get { return useToolTips; }
			set { useToolTips = value; }
		}

		public bool ShowOne
		{
			get { return showOne; }
			set { showOne = value; }
		}

		public int TooltipOrder
		{
			get { return tooltipOrder; }
			set { tooltipOrder = value; }
		}

		internal ArrayList GetNonClippedFeatureRectangles(MapGL parentMap)
		{
			if (parentMap == null) return null;

			var mapRect = parentMap.GetMapRectangleExt();
            var bottomLeft = parentMap.ToWinPoint(mapRect.Left, mapRect.Bottom);
            var topRight = parentMap.ToWinPoint(mapRect.Right, mapRect.Top);
			ArrayList rectList = new ArrayList();
			var winRect = new RectangleD(bottomLeft.Y, topRight.Y, bottomLeft.X, topRight.X);

			lock (features.SyncRoot)
			{
				for (int i = 0; i < features.Count; i++)
				{
                    var feature = features[i];
                    var label = feature as Label;

                    // Skip moved labels.
                    if (label != null && label.IsMovedByUser) continue;

                    if (label != null)
                    {
                        var rect = label.GetBoundingRectWin(parentMap);
                        var p = new PointD(rect.Left, rect.Bottom);

                        if (winRect.IsPointIn(p))
                        {
                            rect.id = label.FeatureName;
                            feature.id = label.FeatureName;
                            rectList.Add(rect);
                        }
                    }
                }
			}
			return rectList;
		}

		public Feature FindClosest(PointD p)
		{
			double distance = double.MaxValue;
			return FindClosestWithin(p, ref distance, false);
		}

		public Feature FindClosestWithin(PointD p, double distance, bool kilometers)
		{
			double dist = distance;
			return FindClosestWithin(p, ref dist, kilometers);
		}

		public Feature FindClosestWithin(PointD p, ref double distance, bool kilometers)
		{
			double minDistance = double.MaxValue;
			Feature closestFeature = null;

			if (!Object.Equals(p, null))
			{
				lock (features.SyncRoot)
				{
					for (int i = 0; i < features.Count; i++)
					{
						// Search the features inside the MultipartFeature
						if (features[i].GetType() == typeof(MultipartFeature))
						{
							MultipartFeature mpf = (MultipartFeature)features[i];
							// lock access to this multipart feature so that we do not cause
							// race conditions while someting tries to update it while this code is trying to read it
							lock (mpf.SyncRoot)
							{
								for (int j = 0; j < mpf.Count; j++)
								{
									if (mpf[j].GetType().BaseType == typeof(PointD) || mpf[j].GetType() == typeof(PointD) || mpf[j] is IMapPoint)
									{
										IMapPoint f = mpf[j] as IMapPoint;
										if (f == null) continue;
										double d = f.DistanceTo(p, kilometers);

										if (d > distance) continue;
										if (d < minDistance)
										{
											minDistance = d;
											closestFeature = (Feature)f;
											distance = minDistance;
										}
									}
									else if (mpf[j].GetType() == typeof(Curve) || mpf[j].GetType().BaseType == typeof(Curve))
									{
										Curve f = (Curve)mpf[j];
										double d = double.MaxValue;
										if (f.IsPointOn(p, out d) && (d < minDistance))
										{
											minDistance = d;
											closestFeature = f;
											distance = minDistance;
										}
									}
									else if (mpf[j].GetType() == typeof(Polygon))
									{
										Polygon poly = (Polygon)mpf[j];
										PointD polygonP = new PointD(p.Longitude, p.Latitude);
										if (poly.IsPointIn(polygonP))
										{
											distance = 0;
											return poly;
										}
									}
									else if (mpf[j].GetType() == typeof(Circle))
									{
										Circle circle = (Circle)mpf[j];

										PointD center = circle.Center;
										double dist = center.DistanceTo(p, kilometers);
										double radius = circle.Radius;

										if (kilometers)
											radius *= 1.609344;

										if (dist < radius)
										{
											distance = dist;
											return circle;
										}
									}
								}
							}
						}


						// Search the other (non-MultipartFeature) features
						if (features[i].GetType().BaseType == typeof(PointD) || features[i].GetType() == typeof(PointD) || features[i] is IMapPoint)
						{
							IMapPoint f = features[i] as IMapPoint;
							if (f == null) continue;
							double d = f.DistanceTo(p, kilometers);

							if (d > distance) continue;
							if (d < minDistance)
							{
								minDistance = d;
								closestFeature = (Feature)f;
								distance = minDistance;
							}
						}
						else if (features[i].GetType() == typeof(Curve) || features[i].GetType().BaseType == typeof(Curve))
						{
							Curve f = (Curve)features[i];
							double d = double.MaxValue;
							if (f.IsPointOn(p, out d) && (d < minDistance))
							{
								minDistance = d;
								closestFeature = f;
								distance = minDistance;
							}
						}
						else if (features[i].GetType() == typeof(Polygon))
						{
							Polygon poly = (Polygon)features[i];
							PointD polygonP = new PointD(p.Longitude, p.Latitude);
							if (poly.IsPointIn(polygonP))
							{
								distance = 0;
								return poly;
							}
						}
						else if (features[i].GetType() == typeof(Circle))
						{
							Circle circle = (Circle)features[i];

							PointD center = circle.Center;
							double dist = center.DistanceTo(p, kilometers);
							double radius = circle.Radius;

							if (kilometers)
								radius *= 1.609344;

							if (dist < radius)
							{
								distance = dist;
								return circle;
							}
						}
					}
				}
			}

			return closestFeature;
		}

		public FeatureCollection FindClosestFeaturesWithin(PointD p, double distance, bool kilometers)
		{
			FeatureCollection closestFeatures = new FeatureCollection();
			System.Collections.Generic.List<string> featureKeys = new System.Collections.Generic.List<string>();

			if (!Object.Equals(p, null))
			{
				lock (features.SyncRoot)
				{
					for (int i = 0; i < features.Count; i++)
					{
                        var feature = features[i];
                        // Search the features inside the MultipartFeature
                        if (feature.GetType() == typeof(MultipartFeature))
						{
							MultipartFeature mpf = (MultipartFeature)feature;
							// lock access to this multipart feature so that we do not cause
							// race conditions while someting tries to update it while this code is trying to read it
							lock (mpf.SyncRoot)
							{
								for (int j = 0; j < mpf.Count; j++)
								{
                                    var subFeature = mpf[j];

                                    if (IsFeatureClose(subFeature, p, distance, kilometers))
									{
										if (!featureKeys.Contains(subFeature.FeatureName))
										{
											featureKeys.Add(subFeature.FeatureName);
											closestFeatures.Add(subFeature);
										}
										// check to see if this multipart feature search is to not return all sub features (default behavior)
										// if so break and just return the first one found.
										// if not (true) then don't break, continue searching for sub features.
										if (mpf.SearchReturnAllThatMatch == false)
											break;
									}
								}
							}
						}
						// Search the other (non-MultipartFeature) features
						else if (IsFeatureClose(feature, p, distance, kilometers))
						{
							if (!featureKeys.Contains(feature.FeatureName))
							{
								featureKeys.Add(feature.FeatureName);
								closestFeatures.Add(feature);
								if (showOne)
									break;
							}
						}
					}
				}
			}

			return closestFeatures;
		}

        public Label FindClosestLabel(PointD p, MapGL map, int x, int y)
        {
            if (!Object.Equals(p, null))
            {
                lock (features.SyncRoot)
                {
                    for (int i = 0; i < features.Count; i++)
                    {
                        if (features[i] is Label)
                        {
                            var label = (Label)features[i];
                            if (label.PointIsInsideLabel(map, this, x, y))
                            {
                                return label;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public void FindClosestFeaturesWithin(PointD p, double distance, bool kilometers, FeatureCollection closestFeatures, ref int tropicalIndex)
		{
			if (!Object.Equals(p, null))
			{
				lock (features.SyncRoot)
				{
					for (int i = 0; i < features.Count; i++)
					{
						if (features[i] == null)
							continue;
						if (!features[i].visible)
							continue;
						// Search the features inside the MultipartFeature
						if (features[i].GetType() == typeof(MultipartFeature) || features[i].GetType().BaseType == typeof(MultipartFeature))
						{
							MultipartFeature mpf = (MultipartFeature)features[i];
							for (int j = 0; j < mpf.Count; j++)
								if (IsFeatureClose(mpf[j], p, distance, kilometers))
									if (closestFeatures[mpf[j].FeatureName] == null)
										closestFeatures.Add(mpf[j]);
						}
						// Search the other (non-MultipartFeature) features
						else if (IsFeatureClose(features[i], p, distance, kilometers))
						{
							//check pireps
							if (features[i].GetType() == typeof(PIREPSymbol))
							{
								bool existed = false;
								int intensity = Convert.ToInt32(features[i].Tag);
								int index = features[i].FeatureInfo.IndexOf("Details:");
								if (index != -1)
								{
									string data = features[i].FeatureInfo.Substring(index);

									for (int m = 0; m < closestFeatures.Count; m++)
									{
										if (closestFeatures[m].GetType() == typeof(PIREPSymbol))
										{
											int pintensity = Convert.ToInt32(closestFeatures[m].Tag);
											int pindex = closestFeatures[m].FeatureInfo.IndexOf("Details:");
											if (pindex != -1)
											{
												string pdata = closestFeatures[m].FeatureInfo.Substring(pindex);
												if (pdata.Equals(data))
												{
													if (pintensity >= intensity)
														existed = true;
													else
														closestFeatures.RemoveAt(m--);

													break;
												}
											}
										}
									}
								}

								if (!existed)
									closestFeatures.Add(features[i]);
							}
							else if (closestFeatures[features[i].FeatureName] == null)
							{
								if (features[i].Tag != null && features[i].Tag.Equals("Tropical"))
								{
									//Storm information on top
									if (features[i].GetType() == typeof(Symbol))
									{
										if (tropicalIndex < 0)
											tropicalIndex = 0;
										closestFeatures.Insert(tropicalIndex, features[i]);
										tropicalIndex++;
									}
									else
									{
										if (tropicalIndex < 0)
											tropicalIndex = closestFeatures.Count;
										bool Gov = false;
										bool WSI = false;
										bool Observed = false;
										for (int node = 0; node < closestFeatures.Count; node++)
										{
											if (closestFeatures[node].FeatureName.Contains("WSI"))
												WSI = true;
											if (closestFeatures[node].FeatureName.Contains("Observed"))
												Observed = true;
											if (closestFeatures[node].FeatureName.Contains("Government"))
												Gov = true;
										}
										if (!Gov && features[i].FeatureName.IndexOf("WSI") == -1 && features[i].FeatureName.IndexOf("Observed") == -1)
											closestFeatures.Add(features[i]);
										if (!WSI && features[i].FeatureName.IndexOf("Government") == -1 && features[i].FeatureName.IndexOf("Observed") == -1)
											closestFeatures.Add(features[i]);
										if (!Observed && features[i].FeatureName.IndexOf("WSI") == -1 && features[i].FeatureName.IndexOf("Government") == -1)
											closestFeatures.Add(features[i]);
									}
								}
								else
								{
									closestFeatures.Add(features[i]);
									if (showOne)
										break;
								}
							}
						}
					}
				}
			}
		}

		private bool IsFeatureClose(Feature feature, PointD p, double distance, bool kilometers)
		{
			bool close = false;

			if (!feature.ToolTip || !(feature is IProjectable))
				return false;

			// Do not display tooltips for features below the equator in azimuthal projections
			IProjectable ipFeature = feature as IProjectable;
			MapProjectionTypes mpType = Projection.GetProjectionType(ipFeature.MapProjection);
			if (mpType == MapProjectionTypes.Azimuthal && p.Y < Projection.MinAzimuthalLatitude)
				return false;

			if (feature.GetType().BaseType == typeof(PointD) || feature.GetType() == typeof(PointD) || feature is IMapPoint)
			{
				IMapPoint f = feature as IMapPoint;
				if (f == null)
					close = false;
				else
				{
					double d = f.DistanceTo(p, kilometers);
					if (d < distance)
						close = true;
				}
			}
			else if (feature.GetType() == typeof(Curve) || feature.GetType().BaseType == typeof(Curve))
			{
				Curve f = (Curve)feature;
				close = f.IsPointOn(p, distance);
			}
			else if (feature.GetType() == typeof(Polygon))
			{
				Polygon poly = (Polygon)feature;
				if (((poly.PointList.Count == 3) && (poly.PointList[0].DistanceTo(poly.PointList[2], false) == 0)) || (poly.PointList.Count == 2))
				{
					Curve c = new Curve();
					c.Add(poly.PointList[0]);
					c.Add(poly.PointList[1]);
					close = c.IsPointOn(p, distance);
				}
				else
				{
					PointD polygonP = new PointD(p.Longitude, p.Latitude);
					close = poly.IsPointIn(polygonP);
				}
			}
			else if (feature.GetType() == typeof(Circle))
			{
				Circle circle = (Circle)feature;

				PointD center = circle.Center;
				double dist = center.DistanceTo(p, kilometers);
				double radius = circle.Radius;

				if (kilometers)
					radius *= 1.609344;

				if (dist < radius)
					close = true;

			}

			return close;
		}

		public Label SelectLabel(PointD p, MapGL parentMap)
		{
			if (!Object.Equals(p, null))
			{
				lock (features.SyncRoot)
				{
					for (int i = 0; i < features.Count; i++)
					{
						if (features[i].GetType() == typeof(Label))
						{
							Label label = (Label)features[i];
							RectangleD r = label.GetBoundingRect(parentMap);
                            if(label.Position != Label.PositionType.Fixed)
                            {
                                r.MoveLowerLeftTo(r.Bottom + (label.YOffset / parentMap.ScaleY), r.Left + (label.XOffset / parentMap.ScaleX));
                                r.StretchByPixels(4, parentMap);
                            }
							
							if (r.IsPointIn(p))
								return label;
						}
					}
				}
			}

			return null;
		}

		public ArrayList PolygonsContainingPoint(PointD p, MapGL parentMap)
		{
			ArrayList polygonList = new ArrayList();

			double mapCenterLat = (parentMap.BoundingBox.Map.top + parentMap.BoundingBox.Map.bottom) / 2;
			double degLonPerPixel = (parentMap.BoundingBox.Map.right - parentMap.BoundingBox.Map.left) / parentMap.BoundingBox.Window.width;
			double kmPerDegLon = Math.Cos(mapCenterLat * Math.PI / 180) * 111.32;
			double tolerance = degLonPerPixel * kmPerDegLon * 6;

			PointD polygonP = new PointD(p.Longitude, p.Latitude);

			RectangleD mapRect = parentMap.GetMapRectangle();
			if (mapRect.Left > mapRect.Right)
			{
				if (polygonP.Longitude > 0)
					polygonP.Longitude -= 360;
			}

			if (!Object.Equals(p, null))
			{
				lock (features.SyncRoot)
				{
					for (int i = 0; i < features.Count; i++)
					{
						Feature f = features[i];
						if (f.GetType() == typeof(Polygon))
						{
							Polygon polygon = f as Polygon;

							// Deal with special user defined polygon that has only two points 
							if (((polygon.PointList.Count == 3) && (polygon.PointList[0].DistanceTo(polygon.PointList[2], false) == 0)) || (polygon.PointList.Count == 2))
							{
								Curve c = new Curve();
								c.Add(polygon.PointList[0]);
								c.Add(polygon.PointList[1]);
								if (c.IsPointOn(polygonP) || c.IsPointOn(p))
									polygonList.Add(f);

								continue;
							}

							//Polygon denormalizedP = new Polygon();
							//foreach(PointD point in polygon.PointList)
							//    denormalizedP.Add(new PointD(parentMap.DenormalizeRouteLongitude(point.Longitude), point.Latitude));

							if (polygon.IsPointIn(polygonP) || polygon.IsPointIn(p))
								polygonList.Add(f);
						}
						else if (f.GetType() == typeof(Curve) || f.GetType().BaseType == typeof(Curve))
						{
							Curve curve = f as Curve;
							if (curve.IsPointOn(p))
							{
								polygonList.Add(f);
							}
						}
						else if (f.GetType() == typeof(Circle))
						{
							Circle circle = (Circle)f;

							PointD center = circle.Center;
							double dist = center.DistanceTo(p, false);

							if (dist < circle.Radius)
								polygonList.Add(f);
						}
						// TODO: Need to check Symbol type of warning area????
						else if (f.GetType().BaseType == typeof(PointD) || f.GetType() == typeof(PointD))
						{
							double d = ((PointD)f).DistanceTo(p, false);
							if (d <= tolerance)
								polygonList.Add(f);
						}
						else if (f.GetType() == typeof(MultipartFeature))
						{
							MultipartFeature multiFeature = f as MultipartFeature;
							for (int m = 0; m < multiFeature.Count; m++)
							{
								if (multiFeature[m] is Curve)
								{
									if (((Curve)multiFeature[m]).IsPointOn(p))
									{
										polygonList.Add(multiFeature[m]);
									}

									break;
								}
							}
						}
					}
				}
			}

			return polygonList;
		}

		public virtual void Refresh(MapProjections mapProjection, short centralLongitude)
		{
			lock (features.SyncRoot)
			{
				for (int i = 0; i < features.Count; i++)
				{
					IRefreshable f = features[i] as IRefreshable;
					if (f != null)
						f.Refresh(mapProjection, centralLongitude);
				}
				dirty = false;
			}
		}

		public bool Dirty
		{
			get { return dirty; }
			set
			{
				dirty = value;
			}
		}

		protected void ResetDeclutteredFeatures()
		{
			// Reset features (Labels only) to non-decluttered state
			lock (features.SyncRoot)
			{
				for (int i = 0; i < features.Count; i++)
				{
                    var label = features[i] as Label;
                    if (label == null || label.IsMovedByUser || label.IsMovingByUser) continue;

                    label.draw = true;
					label.XOffset = 0;
					label.YOffset = 0;
                    label.WinX = null;
                    label.WinY = null;
				}
			}
		}

		protected void DeclutterFeatures(MapGL parentMap, Layer layer)
		{
			if (parentMap == null) return;

            double cX, cY;
            var center = parentMap.GetMapCenterExt();
            Projection.ProjectPoint(parentMap.MapProjection, center.X, center.Y, parentMap.CentralLongitude, out cX, out cY);
            var centerWin = new PointD(parentMap.ToWinPoint(cX, cY).X, parentMap.ToWinPoint(cX, cY).Y);

            // Calculate the positions of the decluttered bounding rectangles
            Hashtable newRects = DeclutterLabels.Declutter(GetNonClippedFeatureRectangles(parentMap), centerWin, parentMap.ScaleFactor, parentMap);

            if (newRects != null && newRects.Count > 0)
            {
                var decluttered = new Hashtable();

                foreach (var key in newRects.Keys)
                {
                    var rect = (RectangleD)newRects[key];
                    decluttered.Add(key, rect);
                }

                newRects = decluttered;
            }

            // Make sure we got a good hashtable
            if (newRects == null || newRects.Count == 0) return;

			// Move the features (Labels only) to their new decluttered positions
			lock (features.SyncRoot)
			{
				for (int i = 0; i < features.Count; i++)
				{
                    var label = features[i] as Label;
                    if (label == null || label.id == null || label.IsMovingByUser || label.IsMovedByUser) continue;
					RectangleD rect = (RectangleD)newRects[label.id];
					if (rect != null)
					{
                        label.draw = true;

                        label.WinX = rect.Left;
                        label.WinY = rect.Bottom;
                    }
					else
                        label.draw = false;
				}
			}
		}

        internal virtual void Draw(MapGL parentMap)
        {
            FUL.Utils.ZoomLevelType parentMapZoomLevel = parentMap.ZoomLevel;

            // Check if the layer should be drawn at this map zoom level
            if (minLayerZoomLevel == FUL.Utils.ZoomLevelType.None && maxLayerZoomLevel != FUL.Utils.ZoomLevelType.None && maxLayerZoomLevel > parentMapZoomLevel)
                drawn = false;
            else if (minLayerZoomLevel != FUL.Utils.ZoomLevelType.None && maxLayerZoomLevel == FUL.Utils.ZoomLevelType.None && minLayerZoomLevel < parentMapZoomLevel)
                drawn = false;
            else if (minLayerZoomLevel != FUL.Utils.ZoomLevelType.None && maxLayerZoomLevel != FUL.Utils.ZoomLevelType.None && (maxLayerZoomLevel > parentMapZoomLevel || minLayerZoomLevel < parentMapZoomLevel))
                drawn = false;
            else
                drawn = true;
            if (!drawn) return;

            // Declutter the layer if requested
            if (parentMap != null)
            {
                if (declutter && !parentMap.trackingRectangle && !parentMap.panning && parentMap.RectToDraw.left == double.MinValue && !DeclutterPaused)
                {
                    //Console.WriteLine(parentMap.Handle.ToInt32().ToString());
                    DeclutterFeatures(parentMap, this);
                }
            }

            // Compute a reasonable margin for symbols centered off window, but still partially shown
            // Use 15 pixels as the upper limit of half a symbox (30 pixels total -- e.g. beyond large airplane symbol)
            // X & Y the same, so just get X-direction
            double marginSym = 15.0 / parentMap.ScaleX;

            double leftLimitLabel = 0, rightLimitLabel = 0, bottomLimitLabel = 0, topLimitLabel = 0;
            double leftLimitSym = 0, rightLimitSym = 0, bottomLimitSym = 0, topLimitSym = 0;

            if (features != null && features.Count > 0)
            {
                System.Drawing.Point lt, br;
                if (parentMap.RectToDraw.left != double.MinValue)
                {
                    // if RectToDraw is set, limit label drawing to newly expose region only
                    lt = parentMap.ToWinPointFromMap(parentMap.RectToDraw.left - 0.01, parentMap.RectToDraw.top + 0.01);
                    br = parentMap.ToWinPointFromMap(parentMap.RectToDraw.right + 0.01, parentMap.RectToDraw.bottom - 0.01);

                    // if RectToDraw is set, limit symbol drawing to newly expose region only
                    leftLimitSym = parentMap.RectToDraw.left - marginSym;
                    rightLimitSym = parentMap.RectToDraw.right + marginSym;
                    bottomLimitSym = parentMap.RectToDraw.bottom - marginSym;
                    topLimitSym = parentMap.RectToDraw.top + marginSym;
                }
                else
                {
                    // otherwise, use entire visible map (plus the margins)
                    lt = new System.Drawing.Point(-15, -15);
                    br = new System.Drawing.Point(parentMap.BoundingBox.Window.width + 15, parentMap.BoundingBox.Window.height + 15);

                    leftLimitSym = parentMap.BoundingBox.Map.left - marginSym;
                    rightLimitSym = parentMap.BoundingBox.Map.right + marginSym;
                    bottomLimitSym = parentMap.BoundingBox.Map.bottom - marginSym;
                    topLimitSym = parentMap.BoundingBox.Map.top + marginSym;
                }
                leftLimitLabel = lt.X;
                rightLimitLabel = br.X;
                bottomLimitLabel = br.Y;
                topLimitLabel = lt.Y;
            }

            // Draw the features in this Layer
            lock (features.SyncRoot)
            {
                for (int i = 0; i < features.Count; i++)
                {
                    Feature f = features[i];

                    if (!f.visible) continue;

                    // If the feature supports projection and retained mode drawing, make sure the display
                    // list was created with the same projection as the map.  If not, don't draw it.
                    if ((f is IProjectable) && (f is IRefreshable) &&
                        ((IProjectable)f).MapProjection != parentMap.MapProjection && !(f is MultipartFeature))
                    {
                        // We need an exception for a Curve in immediate mode. It could meet the above
                        // condition, but we want to draw it.  It will get the correct map projection from
                        // the parent map when it draws.
                        Curve c = f as Curve;
                        if (c == null || (c != null && !c.ImmediateMode))
                            continue;
                    }

                    // For features that don't support projection, only draw them if the map projection
                    // is set to cylindrical equidistant.
                    if (!(f is IProjectable) && parentMap.MapProjection != MapProjections.CylindricalEquidistant)
                        continue;

                    if (f.GetType() == typeof(Label))
                    {
                        //if (!f.draw) continue;

                        Label label = (Label)f;

                        if (!label.IsMovedByUser)
                        {
                            var labelPoint = parentMap.ToWinPointFromMap(label.X, label.Y);
                            if (labelPoint.X + label.XOffset < leftLimitLabel - label.Width)
                                continue;
                            if (labelPoint.X + label.XOffset > rightLimitLabel)
                                continue;
                            if (labelPoint.Y + label.YOffset > bottomLimitLabel - label.Height)
                                continue;
                            if (labelPoint.Y + label.YOffset < topLimitLabel)
                                continue;
                        }

                        try
                        {
                            label.Draw(parentMap, this);
                        }
                        catch { }
                    }
                    else if (f.GetType() == typeof(Symbol))
                    {
                        Symbol symbol = (Symbol)f;

                        double px, py;
                        Projection.ProjectPoint(parentMap.MapProjection, symbol.X, symbol.Y, parentMap.CentralLongitude, out px, out py);
                        double lon = parentMap.DenormalizeLongitude(px);

                        if (lon >= leftLimitSym && lon <= rightLimitSym && py >= bottomLimitSym && py <= topLimitSym)
                        {
                            try
                            {
                                symbol.Draw(parentMap, this);
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        try
                        {
                            f.Draw(parentMap, this);
                        }
                        catch { }
                    }
                }
            }
        }

#if false
		private bool longitudeIsBetween(double lonTest, double lonLeft, double lonRight)
		{
			// assumes 0 < lonRight - lonLeft < 360.0 (as it will be if it is taken from the map)
			// return true if lonLeft <= lonTest <= lonRight
			double delta = lonLeft - lonTest;
			// always round with pos numbers since processor could round negs up or down
			if (delta > 0.0)
				lonTest += (((int)delta) / 360 + 1) * 360;	// rounds down (toward neg infinite)
			else
				lonTest -= ((int)-delta) / 360 * 360; // rounds up (toward pos infinite)
			if (lonTest <= lonRight)
				return true;
			return false;
		}

		private void GetMaxLabelWidthAndHeight(MapGL parentMap, out double maxWidth, out double maxHeight)
		{
			maxWidth = 0.0;
			maxHeight = 0.0;
			lock (features.SyncRoot)
			{
				for (int i = 0; i < features.Count; ++i)
				{
					if (features[i].draw && features[i].GetType() == typeof(Label))
					{
						Label label = (Label)features[i];
						double width = label.Width;
						double height = label.Height;
						if (declutter)
						{
							// capture max total distance from label lower-left of either the label itself or the line to object
							if (Math.Abs(label.XOffset) > width)
								width = Math.Abs(label.XOffset);
							if (Math.Abs(label.YOffset) > height)
								height = Math.Abs(label.YOffset);
						}
						if (width > maxWidth)
							maxWidth = width;
						if (height > maxHeight)
							maxHeight = height;
					}
				}
			}

			// allow for 4 pixel boundary to label rect (worst case) and convert to degrees
			maxWidth = (maxWidth + 4.0) / parentMap.ScaleX;
			maxHeight = (maxHeight + 4.0) / parentMap.ScaleY;
		}
#endif
	}
}
