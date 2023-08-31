
using GMap.NET.WindowsForms;
using GMap.NET;
using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

using System.Windows.Forms;
using Org.BouncyCastle.Crypto.Macs;
using System.Drawing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Org.BouncyCastle.Utilities;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Rit_atomation
{

    



    public partial class Form1 : Form
    {



        ArrayList all_markers = new ArrayList();
        SqlConnection connection = new SqlConnection(@"Data Source=DESKTOP-UDUIPR4;Initial Catalog=RIT_DB;Integrated Security=True;Encrypt=False");
        System.Windows.Forms.TextBox lat;
        System.Windows.Forms.TextBox lng;
        bool move = false;
        bool down = false;
        bool after = false;
        GMap.NET.WindowsForms.GMapMarker global_marker=null;
        GMap.NET.WindowsForms.GMapOverlay markers = new GMap.NET.WindowsForms.GMapOverlay("markers");
        string[] coords;
        int options=0;

        public Form1()
        {
            InitializeComponent();
        }
        Random random = new Random();

        private async void  gMapControl1_Load(object sender, EventArgs e)
        {
            lat = lati;
            lng = longi;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache; //выбор подгрузки карты – онлайн или из ресурсов
            gMapControl1.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance; //какой провайдер карт используется (в нашем случае гугл) 
            gMapControl1.MinZoom = 2; //минимальный зум
            gMapControl1.MaxZoom = 16; //максимальный зум
            gMapControl1.Zoom = 4; // какой используется зум при открытии
            gMapControl1.Position = new GMap.NET.PointLatLng(66.4169575018027, 94.25025752215694);// точка в центре карты при открытии (центр России)
            gMapControl1.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter; // как приближает (просто в центр карты или по положению мыши)
            gMapControl1.CanDragMap = true; // перетаскивание карты мышью
            gMapControl1.DragButton = MouseButtons.Left; // какой кнопкой осуществляется перетаскивание
            gMapControl1.ShowCenter = false; //показывать или скрывать красный крестик в центре
            gMapControl1.ShowTileGridLines = false; //показывать или скрывать тайлы
            connectToDB();
            //TODO сделать выгрузку из БД
            GMap.NET.WindowsForms.GMapOverlay markers = new GMap.NET.WindowsForms.GMapOverlay("markers");

            GMapOverlay polygons = new GMapOverlay("polygons");
            List<PointLatLng> points = new List<PointLatLng>();
            points.Add(new PointLatLng(66.52136850968198, 83.21361632764956));
            points.Add(new PointLatLng(50.11856453098748, 90.23477273353308));
            points.Add(new PointLatLng(51.61108295782279, 120.31488491452886));
            points.Add(new PointLatLng(67.64356138301078, 117.80205015702772));
            GMapPolygon polygon = new GMapPolygon(points, "Jardin des Tuileries");
            polygon.Fill = new SolidBrush(Color.FromArgb(50, Color.Red));
            polygons.Polygons.Add(polygon);
            gMapControl1.Overlays.Add(polygons);
            coords = File.ReadAllLines("NMEA_msg.txt");
             

            //gMapControl1.Overlays[0].Markers[]
            foreach (GMap.NET.WindowsForms.GMapMarker pointer in all_markers) {
                //pointer += markerDragDropEvent;
               markers.Markers.Add(pointer);
            }

            gMapControl1.Overlays.Add(markers);
            wait_a_bit();

        }

        private async Task wait_a_bit() {
            await Task.Run(() => send_locations());
        }

        void send_locations() {

            SqlCommand command1 = connection.CreateCommand();
            string[] s;
            string[] after_dot;
            double x= gMapControl1.Overlays[1].Markers[0].Position.Lat;
            double y= gMapControl1.Overlays[1].Markers[0].Position.Lng;
            int minys;
            command1.CommandText = "update markers set lat=@latta,lon=@longi where id=@id_param";
            command1.Parameters.AddWithValue("@latta", x);
            command1.Parameters.AddWithValue("@longi", y);
            command1.Parameters.AddWithValue("@id_param", gMapControl1.Overlays[1].Markers[0].Tag);

            for (int i=0;i<coords.Length;i++) {

                
            Thread.Sleep(2500);
            s= coords[i].Split(',');

            after_dot=s[2].Split('.');
                minys = s[3].ToLower() == "s" ? -1 : 1;
                x = (int.Parse(after_dot[0].PadLeft(5, '0').Substring(0, 3))) +
                    (double.Parse(after_dot[0].PadLeft(5, '0').Substring(3) +","+ after_dot[1])/60)*minys;

                after_dot = s[4].Split('.');
                minys = s[5].ToLower() == "w" ? -1 : 1;
                y = (int.Parse(after_dot[0].PadLeft(5, '0').Substring(0, 3))) +
                    (double.Parse(after_dot[0].PadLeft(5, '0').Substring(3) + "," + after_dot[1]) / 60) * minys;
                gMapControl1.Overlays[1].Markers[0].Position=new PointLatLng(x, y);


                

                command1.ExecuteNonQuery();
                

                if (gMapControl1.Overlays[0].Polygons[0].IsInside(gMapControl1.Overlays[1].Markers[0].Position))
                {

                    switch (GetSelecetedIndex())
                    {
                        case 0:
                            MessageBox.Show("Маркер попал в зону полигона в координатах\n" + gMapControl1.Overlays[1].Markers[0].Position,
                            "Уведомление", MessageBoxButtons.OK); break;
                        case 1:
                            //gMapControl1.Refresh();
                            int tag = (int)gMapControl1.Overlays[1].Markers[0].Tag;

                            GMap.NET.WindowsForms.Markers.GMarkerGoogle
                                                    item1 = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(
                                                    new GMap.NET.PointLatLng(gMapControl1.Overlays[1].Markers[0].Position.Lat, gMapControl1.Overlays[1].Markers[0].Position.Lng),
                                                    (GMap.NET.WindowsForms.Markers.GMarkerGoogleType)random.Next(1, 38));
                            item1.Tag = tag;

                            gMapControl1.Overlays[1].Markers[tag - 1] = item1;
                            after = true;
                            //gMapControl1.Refresh();
                            break;

                        case 2:

                            int x1 = random.Next(1, 692);
                            int y1 = random.Next(1, 449);

                            GMap.NET.WindowsForms.Markers.GMarkerGoogle
                                                    item2 = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(
                               new GMap.NET.PointLatLng((double)gMapControl1.FromLocalToLatLng(x1, y1).Lat,
                                (double)gMapControl1.FromLocalToLatLng(x1, y1).Lng),
                                  GMap.NET.WindowsForms.Markers.GMarkerGoogleType.purple);
                            item2.Tag = gMapControl1.Overlays[1].Markers.Count+1;


                            gMapControl1.Overlays[1].Markers.Add(item2);

                            SqlCommand command = connection.CreateCommand();
                            command.CommandText = "INSERT INTO markers(lat,lon)VALUES(@lati, @longi)";
                            command.Parameters.AddWithValue("@lati", item2.Position.Lat);
                            command.Parameters.AddWithValue("@longi", item2.Position.Lng);

                            command.ExecuteNonQuery();

                            break;
                    }

                }

            }
        }

        private int GetSelecetedIndex()
        {
            if (select_options.InvokeRequired)
                return (int)select_options.Invoke(new Func<int>(GetSelecetedIndex));
            else
                return select_options.SelectedIndex;
        }

        private void connectToDB() {
            try
            {
                connection.Open();
                DataSet ds = new DataSet();
                MessageBox.Show("Соединение установлено, для перемещения маркера кликните по маркеру, для того чтобы прекратить перемещение щёлкните");
            }

            catch (Exception es)
            {MessageBox.Show(es.Message); }
            SqlDataAdapter adapter = new SqlDataAdapter();
            DataTable data = new DataTable();
            string str = $"select lat,lon from markers";
            SqlCommand command = new SqlCommand(str, connection);
            adapter.SelectCommand = command;
            adapter.Fill(data);

            GMap.NET.WindowsForms.GMapMarker marker;
            int i = 1;
            if (data.Rows.Count > 0) {
                foreach (DataRow row in data.Rows)
                {
                    marker = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(
                    new GMap.NET.PointLatLng((double)row[0], (double)row[1]),
                    GMap.NET.WindowsForms.Markers.GMarkerGoogleType.arrow);
                    marker.Tag= i;
                    all_markers.Add(marker);
                    i++;

                }

            }
        }

        private void gMapControl1_MouseDown(object sender, MouseEventArgs e)
        {
            //down = true;

        }

        private void gMapControl1_Click(object sender, EventArgs e)
        {
            down = move==true? true:false;


            

            if (down) {
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "update markers set lat=@lati,lon=@longi where id=@id_param";
                command.Parameters.AddWithValue("@lati", global_marker.Position.Lat);
                command.Parameters.AddWithValue("@longi", global_marker.Position.Lng);
                command.Parameters.AddWithValue("@id_param", (int)global_marker.Tag);

                command.ExecuteNonQuery();

                down = false;

                if (gMapControl1.Overlays[0].Polygons[0].IsInside(global_marker.Position))
                {

                    switch (select_options.SelectedIndex)
                    {
                        case 0:
                            MessageBox.Show("Маркер попал в зону полигона в координатах\n" + global_marker.Position,
                            "Уведомление", MessageBoxButtons.OK); break;
                        case 1:
                            gMapControl1.Refresh();
                            int tag = (int)global_marker.Tag;

                            GMap.NET.WindowsForms.Markers.GMarkerGoogle
                                                    item1 = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(
                                                    new GMap.NET.PointLatLng(global_marker.Position.Lat, global_marker.Position.Lng),
                                                    (GMap.NET.WindowsForms.Markers.GMarkerGoogleType)random.Next(1, 38));
                            item1.Tag = global_marker.Tag;

                            gMapControl1.Overlays[1].Markers[(int)global_marker.Tag - 1] = item1;
                            after = true;
                            gMapControl1.Refresh(); break;

                        case 2:

                            int x = random.Next(1, 692);
                            int y = random.Next(1, 449);

                            GMap.NET.WindowsForms.Markers.GMarkerGoogle
                                                    item2 = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(
                               new GMap.NET.PointLatLng((double)gMapControl1.FromLocalToLatLng(x, y).Lat,
                                (double)gMapControl1.FromLocalToLatLng(x, y).Lng),
                                  GMap.NET.WindowsForms.Markers.GMarkerGoogleType.purple);
                            item2.Tag = gMapControl1.Overlays[1].Markers.Count+1;


                            gMapControl1.Overlays[1].Markers.Add(item2);

                             command = connection.CreateCommand();
                            command.CommandText = "INSERT INTO markers(lat,lon)VALUES(@lati," +
                                " @longi)";
                            command.Parameters.AddWithValue("@lati", item2.Position.Lat);
                            command.Parameters.AddWithValue("@longi", item2.Position.Lng);

                            command.ExecuteNonQuery();

                            break;
                    }

                }

            }
            down = false;
            //move = false;
            //global_marker = null;



        }

       

        private void gMapControl1_MouseMove(object sender, MouseEventArgs e)
        {


            if (move&&!after)
            {
                
                double X = gMapControl1.FromLocalToLatLng(e.X, e.Y).Lng;
                double Y = gMapControl1.FromLocalToLatLng(e.X, e.Y+10).Lat;
                global_marker.Position = new GMap.NET.PointLatLng(Y, X);

            }
        }



        private void gMapControl1_DragDrop_1(object sender, DragEventArgs e)
        {

        }
        private void gMapControl1_OnMarkerEnter(GMap.NET.WindowsForms.GMapMarker item)
        {
            //move = true;
            //global_marker = move ? item : null;
        }

        private void gMapControl1_OnMarkerLeave(GMap.NET.WindowsForms.GMapMarker item)
        {/*

                move = false;
                global_marker = null;


                SqlCommand command = connection.CreateCommand();
                command.CommandText = "update markers set lat=@lati,lon=@longi where id=@id_param";
                command.Parameters.AddWithValue("@lati", item.Position.Lat);
                command.Parameters.AddWithValue("@longi", item.Position.Lng);
                command.Parameters.AddWithValue("@id_param", item.Tag);

                command.ExecuteNonQuery();
            if (gMapControl1.Overlays[0].Polygons[0].IsInside(item.Position))
            {
                //lock (gMapControl1.Overlays[1].Markers){

                switch (select_options.SelectedIndex)
                {
                    case 0: Console.WriteLine(); break;
                    case 1: Console.WriteLine(); break;
                        
                    case 2:
                        //gMapControl1.Overlays[1].Markers.Remove(item);
                        gMapControl1.Refresh();

                        int tag = (int) item.Tag;

                        GMap.NET.WindowsForms.Markers.GMarkerGoogle
                                                item1 = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(
                                                new GMap.NET.PointLatLng(item.Position.Lat, item.Position.Lng),
                                                GMap.NET.WindowsForms.Markers.GMarkerGoogleType.purple);
                        item1.Tag = item.Tag;
                        item = item1;

                            gMapControl1.Overlays[1].Markers[(int)item.Tag]=item;

                        //add_to_overkay(item1);
                        //gMapControl1.Overlays.Remove(markers);
                        //markers.Markers.Add(item);
                        //gMapControl1.Overlays.Add(markers);

                        
                        
                        break;
                }

            }*/
            }


        //}
        

        private void gMapControl1_MouseUp(object sender, MouseEventArgs e)
        {

            //down = false;
        }

        private void gMapControl1_OnMarkerClick(GMap.NET.WindowsForms.GMapMarker item, MouseEventArgs e)
        {
            
            after = false;
            //if (!down&&!move) { 
            lng.Text = item.Position.Lng.ToString();
            lati.Text = item.Position.Lat.ToString();



            move = move == false ? true : false;
            global_marker = global_marker == null ? item : null;

            



        }

    }


}


