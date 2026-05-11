using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace kalk
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string daneNBP = "https://static.nbp.pl/dane/kursy/xml/a050z240311.xml";
        List<PozycjaTabeliA> kursyAktualne = new List<PozycjaTabeliA>();

        public MainPage()
        {
            this.InitializeComponent();
            txtKwota.PlaceholderText = $"{0:f2}";

        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            DateTime dataAktualizacji = DateTime.Now;
            tbDataAktualizacji.Text = $"Data aktualizacji danych: {dataAktualizacji.ToString()}";

            Loaded += Grid_Loaded;
        }
        private void IbxZWaluty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ZapiszIndeksWybranejWaluty(IbxZWaluty.SelectedIndex, "wybranaWalutaZ");
        }

        

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            var serwerNBP = new HttpClient();

            try
            {
                string dane = await serwerNBP.GetStringAsync(new Uri(daneNBP));

                if (!string.IsNullOrEmpty(dane))
                {
                    XDocument daneKursowe = XDocument.Parse(dane);

                    kursyAktualne.Clear();

                    foreach (var element in daneKursowe.Descendants("pozycja"))
                    {
                        PozycjaTabeliA pozycja = new PozycjaTabeliA
                        {
                            przelicznik = element.Element("przelicznik").Value,
                            kod_waluty = element.Element("kod_waluty").Value,
                            kurs_sredni = element.Element("kurs_sredni").Value.Replace(',', '.') 
                        };

                        kursyAktualne.Add(pozycja);
                    }

                    kursyAktualne.Insert(0, new PozycjaTabeliA() { kurs_sredni = "1.0000", kod_waluty = "PLN", przelicznik = "1" });

                    IbxZWaluty.ItemsSource = kursyAktualne;
                    IbxNaWalute.ItemsSource = kursyAktualne;

                    IbxZWaluty.SelectedIndex = 0;
                    IbxNaWalute.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
            }
        }
        public class PozycjaTabeliA
        {
            public string przelicznik { get; set; }
            public string kod_waluty { get; set; }
            public string kurs_sredni { get; set; }
        }

        private void txtKwota_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtKwota.Text))
            {
                int indeksWybranejWaluty = IbxZWaluty.SelectedIndex;

                if (indeksWybranejWaluty >= 0 && indeksWybranejWaluty < kursyAktualne.Count)
                {
                    PozycjaTabeliA zWaluty = kursyAktualne[indeksWybranejWaluty];

                    double kursWyjsciowy;
                    if (double.TryParse(zWaluty.kurs_sredni.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out kursWyjsciowy))
                    {
                        double kwota;
                        if (double.TryParse(txtKwota.Text, out kwota))
                        {
                            double kwotaPLN = kwota * kursWyjsciowy;

                            int indeksWalutyDocelowej = IbxNaWalute.SelectedIndex;
                            if (indeksWalutyDocelowej >= 0 && indeksWalutyDocelowej < kursyAktualne.Count)
                            {
                                PozycjaTabeliA naWalute = kursyAktualne[indeksWalutyDocelowej];
                                double kursDocelowy;
                                if (double.TryParse(naWalute.kurs_sredni.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out kursDocelowy))
                                {
                                    double kwotaDocelowa = kwotaPLN / kursDocelowy;
                                    tbPrzeliczona.Text = kwotaDocelowa.ToString();
                                }
                            }
                        }
                    }
                }
            }
            if (!double.TryParse(txtKwota.Text, out _))
            {
                tbPrzeliczona.Text = string.Empty;
            }
        }

        private void IbxNaWalute_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ZapiszIndeksWybranejWaluty(IbxNaWalute.SelectedIndex, "wybranaWalutaNa");
        }
        private void ZapiszIndeksWybranejWaluty(int selectedIndex, string klucz)
        {
            // Zapisz indeks wybranej waluty w LocalSettings
            ApplicationData.Current.LocalSettings.Values[klucz] = selectedIndex;
        }
        private void OdczytajOstatnioUzywaneWaluty()
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("wybranaWalutaZ"))
            {
                int indeksZ = (int)ApplicationData.Current.LocalSettings.Values["wybranaWalutaZ"];
                if (indeksZ >= 0 && indeksZ < IbxZWaluty.Items.Count)
                    IbxZWaluty.SelectedIndex = indeksZ;
            }

            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("wybranaWalutaNa"))
            {
                int indeksNa = (int)ApplicationData.Current.LocalSettings.Values["wybranaWalutaNa"];
                if (indeksNa >= 0 && indeksNa < IbxNaWalute.Items.Count)
                    IbxNaWalute.SelectedIndex = indeksNa;
            }
        }

        private void btnOProgramie_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(OProgramie));
        }

        private void btnPomoc_Click(object sender, RoutedEventArgs e)
        {
            if (IbxZWaluty.SelectedItem != null)
            {
                PozycjaTabeliA wybranaWaluta = IbxZWaluty.SelectedItem as PozycjaTabeliA;
                this.Frame.Navigate(typeof(Pomoc), wybranaWaluta.kod_waluty);
            }
            else
            {
                this.Frame.Navigate(typeof(Pomoc), "Brak wybranej waluty");
            }
        }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }
    }
}
