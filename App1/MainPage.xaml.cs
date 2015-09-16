using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Serialization;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region XML Constants

        const string Health = "Здраве";
        const string Strength = "Сила";

        const int MinDice = 2;
        const int MaxDice = 12;
        #endregion

        bool isLoadedGame = false;

        SaveGameData Game;

        //StackPanel spStats;
        //StackPanel spButtons;
        //TextBlock txtBlock;

        Game GameSource;
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void LoadXML()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Game));
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile file = await folder.GetFileAsync("game.xml");
            Stream stream = await file.OpenStreamForReadAsync();
            this.GameSource = (Game)serializer.Deserialize(stream);
            stream.Dispose();

            this.Game = new SaveGameData();
            //this.isLoadedGame = this.LoadGame("Autosave.xml");
            if (isLoadedGame == false)
            {
                this.InitializeGame();
            }

            this.ExecuteEpizode(this.Game.CurrentEpizode);

            //XmlSerializer serializer = new XmlSerializer(typeof(Game));

            //StorageFolder installFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            //StorageFile file = await StorageFile.GetFileFromApplicationUriAsync("ms-appx:///file.txt");

            //this.GameSource = (Game)serializer.Deserialize(file);
            //fs.Close();

            //Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-US");

            //this.Game = new SaveGameData();
            //this.isLoadedGame = this.LoadGame("Autosave.xml");
            //if (isLoadedGame == false)
            //{
            //    this.InitializeGame();
            //}

            //this.ExecuteEpizode(this.Game.CurrentEpizode);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.LoadXML();
        }

        private void ExecuteEpizode(int EpizodeNumber)
        {
            var epizode = GetEpizode(EpizodeNumber);
            if (epizode == null)
            {
                return;
            }

            if (this.isLoadedGame == false)
            {
                this.AddRemoveItems(epizode);

                this.AddRemoveStats(epizode);
            }

            this.Game.CurrentEpizode = EpizodeNumber;

            this.PrepareText(epizode);

            if (CheckIfDead())
            {
                var choice = new Decision();
                choice.Text = "Продължи";
                choice.GoTo = 1001;
                this.CreateButton(choice);
            }
            else
            {
                this.PrepareChoices(epizode);
            }

            this.isLoadedGame = false;

            this.RefreshStats();

            this.SaveGame("Autosave.xml");
        }

        private bool CheckIfDead()
        {
            var item = this.Game.lstStats.Find(n => n.Name == Health);
            if (item != null)
            {
                if (item.Value <= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void InitializeGame()
        {
            this.Game.CurrentEpizode = 1;
            this.Game.lstStats = new List<PersonStats>();
            this.Game.lstInventory = new List<Inventory>();

            Random rand = new Random(DateTime.Now.Second);

            var stats = this.GameSource.lstStats;
            foreach (var stat in stats)
            {
                var name = stat.Name;
                var min = stat.Min;
                var max = stat.Max;
                var val = rand.Next(min, max);
                this.Game.lstStats.Add(new PersonStats { Name = name, Value = val });
                this.Game.lstStats.Add(new PersonStats { Name = "Initial" + name, Value = val });
            }
        }

        private void RefreshStats()
        {
            this.spStats.Children.Clear();

            foreach (var s in this.Game.lstStats)
            {
                this.spStats.Children.Add(new TextBlock { Text = s.Name });
                this.spStats.Children.Add(new TextBlock { Text = s.Value.ToString() });
            }
        }

        private void ResetStat(string name, int qty)
        {
            var item = this.Game.lstStats.Find(n => n.Name == name);
            if (item != null)
            {
                item.Value = qty;
            }
        }

        private void RemoveStat(string name, int qty)
        {
            var item = this.Game.lstStats.Find(n => n.Name == name);
            if (item != null)
            {
                item.Value -= qty;
            }
        }

        private void AddStat(string name, int qty)
        {
            var item = this.Game.lstStats.Find(n => n.Name == name);
            if (item != null)
            {
                item.Value += qty;
            }
            else
            {
                this.Game.lstStats.Add(new PersonStats { Name = name, Value = qty });
            }
        }

        private void RemoveInventoryItem(string name, int qty)
        {
            var item = this.Game.lstInventory.Find(n => n.Name == name);
            if (item != null)
            {
                item.Quantity -= qty;
                if (item.Quantity < 0)
                {
                    
                    //MessageBox.Show("Грешка в играта. Позволено ви беше да използвате повече предмети от колкото имате налични. Въпреки това можете да продължите да играете нормално. Моля, уведомете авторите за отстраняване на грешката.");
                }
            }
            else
            {
                //MessageBox.Show("Грешка в играта. Позволено ви беше да използвате повече предмети от колкото имате налични. Въпреки това можете да продължите да играете нормално. Моля, уведомете авторите за отстраняване на грешката.");
            }
        }

        private void AddInventoryItem(string name, int qty)
        {
            var item = this.Game.lstInventory.Find(n => n.Name == name);
            if (item != null)
            {
                item.Quantity += qty;
            }
            else
            {
                this.Game.lstInventory.Add(new Inventory { Name = name, Quantity = qty });
            }
        }

        private void AddRemoveStats(EpizodeXML epizode)
        {
            var stats = epizode.Stats;
            foreach (var stat in stats)
            {
                var name = stat.Name;
                int qty = stat.Quantity;

                if (stat.Reset == true)
                {
                    int resetqty = Game.lstStats.Find(s => s.Name == "Initial" + stat.Name).Value;

                    this.ResetStat(stat.Name, resetqty);
                }
                else
                {
                    if (stat.Action == true)
                    {
                        this.AddStat(name, qty);
                    }
                    else
                    {
                        this.RemoveStat(name, qty);
                    }
                }
            }
        }

        private void AddRemoveItems(EpizodeXML epizode)
        {
            var invs = epizode.Inventories;
            foreach (var inv in invs)
            {
                var name = inv.Name;
                int qty = inv.Quantity;
                if (inv.Action == true)
                {
                    AddInventoryItem(name, qty);
                }
                else
                {
                    RemoveInventoryItem(name, qty);
                }
            }
        }

        private void PrepareChoices(EpizodeXML epizode)
        {
            this.spButtons.Children.Clear();

            this.PrepareDecisions(epizode);
            this.PrepareChances(epizode);
            this.PrepareBattle(epizode);
            this.PrepareInventory(epizode);
            this.PrepareConditions(epizode);
        }

        private void PrepareConditions(EpizodeXML epizode)
        {
            var conditions = epizode.Choices.Conditions;
            foreach (var cond in conditions)
            {
                var predicates = cond.Predicates;
                var pass = true;
                foreach (var pred in predicates)
                {
                    var type = pred.Type;
                    if (type == PredicateTypes.eInventory)
                    {
                        var name = pred.Name;

                        bool aval = pred.IsAvailable;

                        if (aval == true)
                        {
                            int qty = pred.Quantity;

                            var inv = this.Game.lstInventory.Find(i => i.Name == name);
                            if (inv == null || inv.Quantity < qty)
                            {
                                pass = false;
                            }
                        }
                        else
                        {
                            var inv = this.Game.lstInventory.Find(i => i.Name == name);
                            if (inv != null && inv.Quantity != 0)
                            {
                                pass = false;
                            }
                        }
                    }
                }
                if (pass)
                {
                    this.CreateButton(cond);
                    break;
                }
            }
        }

        private void PrepareInventory(EpizodeXML epizode)
        {
            var InventoryConditions = epizode.Choices.InventoryConditions;
            foreach (var invent in InventoryConditions)
            {
                var name = invent.Name;
                bool aval = invent.IsAvailable;

                if (aval == true)
                {
                    int qty = invent.Quantity;

                    var inv = this.Game.lstInventory.Find(i => i.Name == name);
                    if (inv != null)
                    {
                        if (inv.Quantity >= qty)
                        {
                            this.CreateButton(invent);
                        }
                    }
                }
                else
                {
                    var inv = this.Game.lstInventory.Find(i => i.Name == name);
                    if (inv == null || inv.Quantity == 0)
                    {
                        this.CreateButton(invent);
                    }
                }
            }
        }

        private void PrepareChances(EpizodeXML epizode)
        {
            if (epizode.Choices != null)
            {
                Random rand = new Random(DateTime.Now.Second);
                var r = rand.NextDouble();
                var chances = epizode.Choices.Chances;

                var cnt = chances.Count();
                double tillNow = 0;

                foreach (var chance in chances)
                {
                    tillNow += chance.Probability;//double.Parse(chance.Attribute(Probability).Value, System.Globalization.NumberStyles.AllowDecimalPoint);
                    if (r < tillNow)
                    {
                        this.CreateButton(chance);
                        break;
                    }
                }
            }
        }

        private void CreateBattleButton(Battle battle)
        {
            Button btn = new Button();
            btn.Tapped += this.BTapped;
            btn.Content = battle.Text;

            var es = battle.EnemyStrength;
            var eh = battle.EnemyHealth;

            var MyHealth = this.Game.lstStats.Find(f => f.Name == Health);
            var health = MyHealth.Value;
            var strength = this.Game.lstStats.Find(f => f.Name == Strength).Value;

            Random rand = new Random(DateTime.Now.Second);

            while (health > 0 && eh > 0)
            {
                var myHit = rand.Next(MinDice, MaxDice);
                var enemyHit = rand.Next(MinDice, MaxDice);

                myHit += strength + myHit;
                enemyHit += es + enemyHit;
                if (myHit > enemyHit)
                {
                    eh -= 2;
                }
                else
                {
                    health -= 2;
                }
            }

            if (health > eh)
            {
                MyHealth.Value = health;
                btn.Tag = battle.GoTo;
            }
            else
            {
                MyHealth.Value = health;
                btn.Tag = battle.Lose;
            }

            this.spButtons.Children.Add(btn);
        }

        private void CreateButton(Decision chance)
        {
            Button btn = new Button();
            btn.Tapped += this.BTapped;
            btn.Content = chance.Text;
            btn.Tag = chance.GoTo;
            this.spButtons.Children.Add(btn);
        }

        private void PrepareDecisions(EpizodeXML epizode)
        {
            if (epizode.Choices != null)
            {
                var choices = epizode.Choices.Decisions;
                foreach (var choice in choices)
                {
                    this.CreateButton(choice);
                }
            }
        }

        private void PrepareText(EpizodeXML epizode)
        {
            //var EpizodeContents = new FlowDocumentScrollViewer();
            //var Flowdoc = new FlowDocument();
            //Paragraph para = new Paragraph();
            //para.Inlines.Add(epizode.ID + "\n" + epizode.Text);
            //Flowdoc.Blocks.Add(para);
            ////this.txtEpizodeContents = new FlowDocumentScrollViewer();
            //this.txtEpizodeContents.Document = Flowdoc;
            //this.txtEpizodeContents
        }

        private void PrepareBattle(EpizodeXML epizode)
        {
            var choices = epizode.Choices.Battles;
            foreach (var choice in choices)
            {
                this.CreateBattleButton(choice);
            }
        }

        private EpizodeXML GetEpizode(int EpizodeNumber)
        {
            var epizode = this.GameSource.lstEpizodes.Where(e => e.ID == EpizodeNumber).FirstOrDefault();
            return epizode;
        }

        private void BTapped(object sender, TappedRoutedEventArgs e)
        {
            Button b = (Button)sender;
            ExecuteEpizode((int)b.Tag);
        }       

        private void Save_Click_1(object sender, RoutedEventArgs e)
        {
            //var ofd = new SaveFileDialog();
            //Nullable<bool> result = ofd.ShowDialog();

            //if (result == true)
            //{
            //    string file = ofd.FileName;
            //    SaveGame(file);
            //}
        }

        private void SaveGame(string file)
        {
            //XmlSerializer serializer = new XmlSerializer(typeof(SaveGameData));
            //TextWriter writer = new StreamWriter(file);

            //serializer.Serialize(writer, this.Game);
            //writer.Close();
        }

        private void Load_Click_1(object sender, RoutedEventArgs e)
        {
            //var ofd = new OpenFileDialog();
            //Nullable<bool> result = ofd.ShowDialog();
            //string file = ofd.FileName;
            //if (result == true)
            //{
            //    LoadGame(file);
            //}
            //this.ExecuteEpizode(this.Game.CurrentEpizode);
        }

        private bool LoadGame(string file)
        {
            //XmlSerializer serializer = new XmlSerializer(typeof(SaveGameData));
            //if (System.IO.File.Exists(file) == true)
            //{
            //    FileStream fs = new FileStream(file, FileMode.Open);
            //    this.Game = (SaveGameData)serializer.Deserialize(fs);
            //    fs.Close();
            //    this.isLoadedGame = true;
            //    return true;
            //}
            return false;
        }

        private void Exit_Click_1(object sender, RoutedEventArgs e)
        {
            //this.Close();
        }

        private void GameInfo_Click_1(object sender, RoutedEventArgs e)
        {
            //Window win = new Window();
            //var Grid = new Grid();
            //Grid.ColumnDefinitions.Add(new ColumnDefinition());
            //Grid.ColumnDefinitions.Add(new ColumnDefinition());
            //int row = 0;
            //foreach (var stat in this.Game.lstStats)
            //{
            //    Grid.RowDefinitions.Add(new RowDefinition());
            //    var label = new Label { Content = stat.Name, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
            //    Grid.Children.Add(label);
            //    Grid.SetRow(label, row);
            //    Grid.SetColumn(label, 0);

            //    var val = new Label { Content = stat.Value, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            //    Grid.Children.Add(val);
            //    Grid.SetRow(val, row);
            //    Grid.SetColumn(val, 1);
            //    row++;
            //}

            //foreach (var stat in this.Game.lstInventory)
            //{
            //    Grid.RowDefinitions.Add(new RowDefinition());
            //    var label = new Label { Content = stat.Name, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
            //    Grid.Children.Add(label);
            //    Grid.SetRow(label, row);
            //    Grid.SetColumn(label, 0);

            //    var val = new Label { Content = stat.Quantity, HorizontalAlignment = System.Windows.HorizontalAlignment.Left };
            //    Grid.Children.Add(val);
            //    Grid.SetRow(val, row);
            //    Grid.SetColumn(val, 1);
            //    row++;
            //}
            //win.Content = Grid;
            //win.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;
            //win.ShowDialog();
        }

        private void NewGame_Click_1(object sender, RoutedEventArgs e)
        {
            this.InitializeGame();
            this.ExecuteEpizode(this.Game.CurrentEpizode);
        }

        
    }

    [XmlRoot("SaveGameData")]
    public class SaveGameData
    {
        [XmlElement("CurrentEpizode")]
        public int CurrentEpizode { get; set; }

        [XmlArray("InventoryList"), XmlArrayItem(typeof(Inventory), ElementName = "Inventory")]
        public List<Inventory> lstInventory { get; set; }

        [XmlArray("StatsList"), XmlArrayItem(typeof(PersonStats), ElementName = "Stats")]
        public List<PersonStats> lstStats { get; set; }
    }    
}
