using PZ3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Point = PZ3.Model.Point;

namespace PZ3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //lat = x 
        //lon = y


        public static KeyValuePair<GeometryModel3D, PowerEntity> element1 = new KeyValuePair<GeometryModel3D, PowerEntity>();
        public static KeyValuePair<GeometryModel3D, PowerEntity> element2 = new KeyValuePair<GeometryModel3D, PowerEntity>();

        public static bool foundElement1 = false;
        public static bool foundElement2 = false;

        public static bool showTooltip = false;


        public static double[,] abstractGrid = new double[100, 100];

        public static Tuple<double, double> minMapPoint = new Tuple<double, double>(19.793909, 45.2325);
        public static Tuple<double, double> maxMapPoint = new Tuple<double, double>(19.894459, 45.277031);

        public static List<PowerEntity> entities = new List<PowerEntity>();
        public static List<LineEntity> lines = new List<LineEntity>();

        public static Dictionary<GeometryModel3D, LineEntity> lineModels = new Dictionary<GeometryModel3D, LineEntity>();
        public static Dictionary<GeometryModel3D, PowerEntity> entityModels = new Dictionary<GeometryModel3D, PowerEntity>();

        private GeometryModel3D hitgeo;
        private ToolTip tooltip = new ToolTip();




        int currentZoom = 1;
        int maxZoom = 18;
        int minZoom = -5;
      


        private System.Windows.Point start = new System.Windows.Point();
        private System.Windows.Point startPosition = new System.Windows.Point();
        private System.Windows.Point diffOffset = new System.Windows.Point();


        public MainWindow()
        {
            InitializeComponent();

    

            LoadDataFromFile();

            AddElements();
            AddLines();
        }


        public static void LoadDataFromFile()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("Geographic.xml");

            XmlNodeList nodeList;

            double newX;
            double newY;


            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Substations/SubstationEntity");
            foreach (XmlNode node in nodeList)
            {

                SubstationEntity sub = new SubstationEntity();

                sub.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                sub.Name = node.SelectSingleNode("Name").InnerText;
                sub.X = double.Parse(node.SelectSingleNode("X").InnerText);
                sub.Y = double.Parse(node.SelectSingleNode("Y").InnerText);

                ToLatLon(sub.X, sub.Y, 34, out newY, out newX);

                sub.X = newX;
                sub.Y = newY;


                if (CheckCoordinates(sub.X, sub.Y))
                {
                    entities.Add(sub);
                }



            }





            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Nodes/NodeEntity");
            foreach (XmlNode node in nodeList)
            {

                NodeEntity nodeobj = new NodeEntity();

                nodeobj.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                nodeobj.Name = node.SelectSingleNode("Name").InnerText;
                nodeobj.X = double.Parse(node.SelectSingleNode("X").InnerText);
                nodeobj.Y = double.Parse(node.SelectSingleNode("Y").InnerText);


                ToLatLon(nodeobj.X, nodeobj.Y, 34, out newY, out newX);

                nodeobj.X = newX;
                nodeobj.Y = newY;

                if (CheckCoordinates(nodeobj.X, nodeobj.Y))
                {
                    entities.Add(nodeobj);
                }
            }




            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Switches/SwitchEntity");
            foreach (XmlNode node in nodeList)
            {

                SwitchEntity switchobj = new SwitchEntity();


                switchobj.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                switchobj.Name = node.SelectSingleNode("Name").InnerText;
                switchobj.X = double.Parse(node.SelectSingleNode("X").InnerText);
                switchobj.Y = double.Parse(node.SelectSingleNode("Y").InnerText);
                switchobj.Status = node.SelectSingleNode("Status").InnerText;

                ToLatLon(switchobj.X, switchobj.Y, 34, out newY, out newX);

                switchobj.X = newX;
                switchobj.Y = newY;

                if (CheckCoordinates(switchobj.X, switchobj.Y))
                {
                    entities.Add(switchobj);
                }
            }






            nodeList = xmlDoc.DocumentElement.SelectNodes("/NetworkModel/Lines/LineEntity");
            foreach (XmlNode node in nodeList)
            {

                LineEntity l = new LineEntity();

                l.Id = long.Parse(node.SelectSingleNode("Id").InnerText);
                l.Name = node.SelectSingleNode("Name").InnerText;
                if (node.SelectSingleNode("IsUnderground").InnerText.Equals("true"))
                {
                    l.IsUnderground = true;
                }
                else
                {
                    l.IsUnderground = false;
                }
                l.R = float.Parse(node.SelectSingleNode("R").InnerText);
                l.ConductorMaterial = node.SelectSingleNode("ConductorMaterial").InnerText;
                l.LineType = node.SelectSingleNode("LineType").InnerText;
                l.ThermalConstantHeat = long.Parse(node.SelectSingleNode("ThermalConstantHeat").InnerText);
                l.FirstEnd = long.Parse(node.SelectSingleNode("FirstEnd").InnerText);
                l.SecondEnd = long.Parse(node.SelectSingleNode("SecondEnd").InnerText);


                List<Point> linePoints = new List<Point>();
                foreach (XmlNode item in node.ChildNodes[9].ChildNodes)
                {
                    System.Windows.Point point = new System.Windows.Point();
                    point.X = double.Parse(item.SelectSingleNode("X").InnerText);
                    point.Y = double.Parse(item.SelectSingleNode("Y").InnerText);

                    ToLatLon(point.X, point.Y, 34, out newY, out newX);


                    if (CheckCoordinates(newX, newY))
                    {
                        linePoints.Add(new Point { X = newX, Y = newY });
                    }
                }

                l.Vertices = linePoints;





                bool firstEnd = false;
                bool secondEnd = false;

                foreach (PowerEntity entity in entities)
                {
                    if (entity.Id == l.FirstEnd)
                        firstEnd = true;

                    if (entity.Id == l.SecondEnd)
                        secondEnd = true;

                }

                if (firstEnd && secondEnd)
                    lines.Add(l);


                }




        }

        public void AddElements()
        {

            foreach (PowerEntity entity in entities)
            {
                GeometryModel3D entityModel = CreateElement3D(entity);

                entityModels.Add(entityModel, entity);    
                scene.Children.Add(entityModel);            
            }



        }

        public void AddLines()
        {

            foreach (LineEntity lineEntity in lines)
            {

                GeometryModel3D lineModel = Create3DLine(lineEntity);


                lineModels.Add(lineModel, lineEntity);
                scene.Children.Add(lineModel);
            }
        }

        //From UTM to Latitude and longitude in decimal
        public static void ToLatLon(double utmX, double utmY, int zoneUTM, out double latitude, out double longitude)
        {
            bool isNorthHemisphere = true;

            var diflat = -0.00066286966871111111111111111111111111;
            var diflon = -0.0003868060578;

            var zone = zoneUTM;
            var c_sa = 6378137.000000;
            var c_sb = 6356752.314245;
            var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
            var e2cuadrada = Math.Pow(e2, 2);
            var c = Math.Pow(c_sa, 2) / c_sb;
            var x = utmX - 500000;
            var y = isNorthHemisphere ? utmY : utmY - 10000000;

            var s = ((zone * 6.0) - 183.0);
            var lat = y / (c_sa * 0.9996);
            var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
            var a = x / v;
            var a1 = Math.Sin(2 * lat);
            var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
            var j2 = lat + (a1 / 2.0);
            var j4 = ((3 * j2) + a2) / 4.0;
            var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
            var alfa = (3.0 / 4.0) * e2cuadrada;
            var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
            var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
            var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
            var b = (y - bm) / v;
            var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
            var eps = a * (1 - (epsi / 3.0));
            var nab = (b * (1 - epsi)) + lat;
            var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
            var delt = Math.Atan(senoheps / (Math.Cos(nab)));
            var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

            longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
            latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
        }


        public static bool CheckCoordinates(double x, double y)
        {
            if ((x >= minMapPoint.Item1 && x <= maxMapPoint.Item1) && (y >= minMapPoint.Item2 && y <= maxMapPoint.Item2))
                return true;
            else
                return false;

        }


        public static double CalculateScaledValue(double oldValue, double maxMapSize, double minMapSize)
        {           
            return Math.Round((oldValue - minMapSize) / (maxMapSize - minMapSize) * 99);
        }

        public static GeometryModel3D CreateElement3D(PowerEntity entity)
        {
            Point3D newPoint = new Point3D();

            newPoint.X = CalculateScaledValue(entity.X, maxMapPoint.Item1, minMapPoint.Item1);
            newPoint.Z = 99 - CalculateScaledValue(entity.Y, maxMapPoint.Item2, minMapPoint.Item2);
            newPoint.Y = 0;

            abstractGrid[(int)newPoint.X, (int)newPoint.Z] = abstractGrid[(int)newPoint.X, (int)newPoint.Z] + 0.7;
            newPoint.Y = abstractGrid[(int)newPoint.X, (int)newPoint.Z];




            GeometryModel3D model3D = new GeometryModel3D();
            model3D.Geometry = CreateCubeMesh(newPoint);  

            model3D.Material = new DiffuseMaterial(Brushes.Blue);


            return model3D;

        }


        public static MeshGeometry3D CreateCubeMesh(Point3D point3D)
        {



            MeshGeometry3D meshGeometry3D = new MeshGeometry3D();
            meshGeometry3D.Positions = new Point3DCollection();

            meshGeometry3D.Positions = CreateCubeCollection(point3D);


            List<int> indicesOrder = new List<int>();
            indicesOrder = CreateTriangleIndicesOrderFoCube();

            foreach (int i in indicesOrder)
            {
                meshGeometry3D.TriangleIndices.Add(i);
            }



            //123102
            //713751
            //657645
            //620604
            //273266
            //015054

            return meshGeometry3D;


        }


        public static Point3DCollection CreateCubeCollection(Point3D point3D)
        {
            double multiply = 0.02;

            Point3D p1 = new Point3D(point3D.X * multiply - 0.005, (point3D.Y + 1) * multiply - 0.005, point3D.Z * multiply - 0.005);
            Point3D p2 = new Point3D(point3D.X * multiply + 0.005, (point3D.Y + 1) * multiply - 0.005, point3D.Z * multiply - 0.005);
            Point3D p3 = new Point3D(point3D.X * multiply - 0.005, (point3D.Y + 1) * multiply + 0.005, point3D.Z * multiply - 0.005);
            Point3D p4 = new Point3D(point3D.X * multiply + 0.005, (point3D.Y + 1) * multiply + 0.005, point3D.Z * multiply - 0.005);
            Point3D p5 = new Point3D(point3D.X * multiply - 0.005, (point3D.Y + 1) * multiply - 0.005, point3D.Z * multiply + 0.005);
            Point3D p6 = new Point3D(point3D.X * multiply + 0.005, (point3D.Y + 1) * multiply - 0.005, point3D.Z * multiply + 0.005);
            Point3D p7 = new Point3D(point3D.X * multiply - 0.005, (point3D.Y + 1) * multiply + 0.005, point3D.Z * multiply + 0.005);
            Point3D p8 = new Point3D(point3D.X * multiply + 0.005, (point3D.Y + 1) * multiply + 0.005, point3D.Z * multiply + 0.005);

            Point3DCollection pointCollection = new Point3DCollection();


            pointCollection.Add(p1);
            pointCollection.Add(p2);
            pointCollection.Add(p3);
            pointCollection.Add(p4);
            pointCollection.Add(p5);
            pointCollection.Add(p6);
            pointCollection.Add(p7);
            pointCollection.Add(p8);


            return pointCollection;
        }

        public static List<int> CreateTriangleIndicesOrderFoCube()
        {

            List<int> indices = new List<int>();



            indices.Add(1);
            indices.Add(2);
            indices.Add(3);
            indices.Add(1);
            indices.Add(0);
            indices.Add(2);

            indices.Add(7);
            indices.Add(1);
            indices.Add(3);
            indices.Add(7);
            indices.Add(5);
            indices.Add(1);

            indices.Add(6);
            indices.Add(5);
            indices.Add(7);
            indices.Add(6);
            indices.Add(4);
            indices.Add(5);


            indices.Add(6);
            indices.Add(2);
            indices.Add(0);
            indices.Add(6);
            indices.Add(0);
            indices.Add(4);

            indices.Add(2);
            indices.Add(7);
            indices.Add(3);
            indices.Add(2);
            indices.Add(6);
            indices.Add(7);

         

            indices.Add(0);
            indices.Add(1);
            indices.Add(5);
            indices.Add(0);
            indices.Add(5);
            indices.Add(4);



            return indices;



        }

        public static GeometryModel3D Create3DLine(LineEntity line)
        {

            GeometryModel3D line3DModel = new GeometryModel3D();


            line3DModel.Geometry = CreateLineMesh(line);

            DiffuseMaterial material = new DiffuseMaterial();

            if (line.ConductorMaterial == "Steel")
                material = new DiffuseMaterial(Brushes.Black);      
            else if (line.ConductorMaterial == "Acsr")
                material = new DiffuseMaterial(Brushes.Orange);
            else if (line.ConductorMaterial == "Copper")
                material = new DiffuseMaterial(Brushes.Brown);
            else
                material = new DiffuseMaterial(Brushes.Purple);

            line3DModel.Material = material;
           

            return line3DModel;

        }

        public static MeshGeometry3D CreateLineMesh(LineEntity line)
        {

            MeshGeometry3D lineMesh = new MeshGeometry3D();
            lineMesh.Positions = new Point3DCollection();
            lineMesh.TriangleIndices = new Int32Collection();


            lineMesh.Positions = CreateLineCollection(line);


            List<int> indicesOrder = new List<int>();
            indicesOrder = CreateTriangleIndicesOrderForLine(lineMesh.Positions);

            foreach(int i in indicesOrder)
            {
                lineMesh.TriangleIndices.Add(i);
            }

           

            return lineMesh;


        }


        public static Point3DCollection CreateLineCollection(LineEntity line)
        {

            Point3DCollection lineCollection = new Point3DCollection();

            for (int i = 0; i < line.Vertices.Count; i++)
            {
                Point3D point = new Point3D();

                point.X = CalculateScaledValue(line.Vertices[i].X, maxMapPoint.Item1, minMapPoint.Item1);
                point.Z = 99 - CalculateScaledValue(line.Vertices[i].Y, maxMapPoint.Item2, minMapPoint.Item2);
                point.Y = 0;

                double multiply = 0.02;

                lineCollection.Add(new Point3D(point.X * multiply, (point.Y + 1) * multiply + 0.0025, point.Z * multiply));
                lineCollection.Add(new Point3D(point.X * multiply, (point.Y + 1) * multiply - 0.0025, point.Z * multiply));
            }


            return lineCollection;
        }

        public static List<int> CreateTriangleIndicesOrderForLine(Point3DCollection positions)
        {
            List<int> indicesOrder = new List<int>();

            for (int i = 0; i < positions.Count - 2; i++)
            {
                indicesOrder.Add(i);
                indicesOrder.Add(i + 1);
                indicesOrder.Add(i + 2);

                indicesOrder.Add(i);
                indicesOrder.Add(i + 2);
                indicesOrder.Add(i + 1);
            }

            return indicesOrder;
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            System.Windows.Point p = e.MouseDevice.GetPosition(this);

            double scaleX = 1;
            double scaleY = 1;
            double scaleZ = 1;

            if (e.Delta > 0 && currentZoom < maxZoom)
            {
                scaleX = skaliranje.ScaleX + 0.1;
                scaleY = skaliranje.ScaleY + 0.1;
                scaleZ = skaliranje.ScaleZ + 0.1;
                currentZoom++;
                skaliranje.ScaleX = scaleX;
                skaliranje.ScaleY = scaleY;
                skaliranje.ScaleZ = scaleZ;
            }
            else if (e.Delta <= 0 && currentZoom > minZoom)
            {
                scaleX = skaliranje.ScaleX - 0.1;
                scaleY = skaliranje.ScaleY - 0.1;
                scaleZ = skaliranje.ScaleZ - 0.1;
                currentZoom--;
                skaliranje.ScaleX = scaleX;
                skaliranje.ScaleY = scaleY;
                skaliranje.ScaleZ = scaleZ;
            }
         
        }

        private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            viewport.CaptureMouse();
            start = e.GetPosition(this);
            diffOffset.X = translacija.OffsetX;
            diffOffset.Y = translacija.OffsetY;
        }

        private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            viewport.ReleaseMouseCapture();
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {

            System.Windows.Point currentPosition = e.GetPosition(this);
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                
                double offsetX = currentPosition.X - startPosition.X;
                double offsetY = currentPosition.Y - startPosition.Y;
                double step = 0.2;
                if ((rotateX.Angle + step * offsetY) < 180 && (rotateX.Angle + step * offsetY) > -180)
                    rotateX.Angle += step * offsetY;
                if ((rotateY.Angle + step * offsetX) < 180 && (rotateY.Angle + step * offsetX) > -180)
                    rotateY.Angle += step * offsetX;
            }

            startPosition = currentPosition;

            if (viewport.IsMouseCaptured)
            {
                System.Windows.Point end = e.GetPosition(this);
                double offsetX = end.X - start.X;
                double offsetY = end.Y - start.Y;
                double w = this.Width;
                double h = this.Height;
                double translateX = (offsetX * 100) / w;
                double translateY = -(offsetY * 100) / h;
                translacija.OffsetX = diffOffset.X + (translateX / (100 * skaliranje.ScaleX));
                translacija.OffsetY = diffOffset.Y + (translateY / (100 * skaliranje.ScaleX));
            }

        }



        private HitTestResultBehavior HTResult(System.Windows.Media.HitTestResult rawresult)
        {

            RayHitTestResult rayResult = rawresult as RayHitTestResult;

         

            if (rayResult != null)
            {

                DiffuseMaterial darkSide = new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Colors.Red));
                bool gasit = false;

              

                if (lineModels.ContainsKey((GeometryModel3D)rayResult.ModelHit))
                {

                    hitgeo = (GeometryModel3D)rayResult.ModelHit;

                    LineEntity line = lineModels[(GeometryModel3D)rayResult.ModelHit];
                    tooltip.IsOpen = true;
                    tooltip.Content = $"Id = {line.Id}, Name = {line.Name}, Type = {line.LineType}";
                    
                    foreach(KeyValuePair<GeometryModel3D, PowerEntity> key_value in entityModels)
                    {
                        if(key_value.Value.Id.Equals(line.FirstEnd))
                        {
                            key_value.Key.Material = darkSide;
                            element1 = key_value;
                            foundElement1 = true;
                        }

                        if (key_value.Value.Id.Equals(line.SecondEnd))
                        {
                            key_value.Key.Material = darkSide;
                            element2 = key_value;
                            foundElement2 = true;
                        }

                        if (foundElement1 && foundElement2)
                            break;
                    }

                  

                  
                }
                else
                {
                    if(entityModels.ContainsKey((GeometryModel3D)rayResult.ModelHit))
                    {
                        hitgeo = (GeometryModel3D)rayResult.ModelHit;
                        PowerEntity foundEntity = entityModels[(GeometryModel3D)rayResult.ModelHit];

                    


                        if (foundEntity is SubstationEntity)
                        {
                            tooltip.Content = $"Id = {foundEntity.Id}, Name = {foundEntity.Name}, Type = Substation";
               
                        }
                        else if (foundEntity is NodeEntity)
                        {
                            tooltip.Content = $"Id = {foundEntity.Id}, Name = {foundEntity.Name}, Type = Node";
                        }
                        else
                        {

                            tooltip.Content = $"Id = {foundEntity.Id}, Name = {foundEntity.Name}, Type = Switch";
                        }

                        tooltip.IsOpen = true;
                        showTooltip = true;
                    }

                  
                }


                if (!gasit)
                {
                    hitgeo = null;
                }
            }

            return HitTestResultBehavior.Stop;
        }

        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if(showTooltip)
            {
                tooltip.Content = "";
                tooltip.IsOpen = false;
            }

            if (!element1.Equals(default(KeyValuePair<GeometryModel3D, PowerEntity>)))
            {
                element1.Key.Material = new DiffuseMaterial(Brushes.Blue);
                foundElement1 = false;
            }

            if (!element2.Equals(default(KeyValuePair<GeometryModel3D, PowerEntity>)))
            {
                 element2.Key.Material = new DiffuseMaterial(Brushes.Blue);
                 foundElement2 = false;
            }


            if (e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point mouseposition = e.GetPosition(viewport);
                Point3D testpoint3D = new Point3D(mouseposition.X, mouseposition.Y, 0);
                Vector3D testdirection = new Vector3D(mouseposition.X, mouseposition.Y, 4);

                PointHitTestParameters pointparams =
                         new PointHitTestParameters(mouseposition);
                RayHitTestParameters rayparams =
                         new RayHitTestParameters(testpoint3D, testdirection);


                hitgeo = null;
                VisualTreeHelper.HitTest(viewport, null, HTResult, pointparams);
            }

            
        }
    }


}
