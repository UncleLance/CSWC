using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using HtmlAgilityPack;
using System.Diagnostics;

namespace CSWC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string Html;
        private string Domain, Url;
        private HtmlDocument Doc;
        private HtmlWeb web = new();
        private List<Page> Pages = new();
        private List<string> Urls = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartCrawlingButton_Click(object sender, RoutedEventArgs e)
        {
            Pages.Clear();
            Urls.Clear();
            
            Url = UrlTextBox.Text;

            if (Url.EndsWith('/'))
                Url = Url.Remove(Url.Length - 1);

            Domain = UrlTextBox.Text.Split('/')[2];

            if (string.IsNullOrEmpty(Url))
            {
                MessageBox.Show("Gegeven url klopt niet! Controleer en probeer opnieuw.");
                return;
            }

            Doc = new();

            Urls.Add(Url);

            StartCrawling(Url);
        }

        private void StartCrawling(string url)
        {
            using (WebClient wc = new())
            {
                try
                {
                    Doc = web.Load(url);

                    // Get title if it exists
                    string title = Doc.DocumentNode.SelectSingleNode("//title") != null ?
                                    Doc.DocumentNode.SelectSingleNode("//title").InnerText :
                                    string.Empty;

                    // Get meta description if it exists
                    string description = Doc.DocumentNode.SelectSingleNode("//meta[@name='description']") != null ?
                                            Doc.DocumentNode.SelectSingleNode("//meta[@name='description']").GetAttributeValue("content", string.Empty) :
                                            string.Empty;

                    // To prevent crawling on the same page again which causes title and description to not match the page
                    Pages.Add(new Page
                    {
                        Url = url,
                        Title = title,
                        Description = description
                    });

                    // Scrape all a tags
                    HtmlNodeCollection links = Doc.DocumentNode.SelectNodes("//a");

                    // If no links were found on the page, navigate the next page
                    if (links == null)
                    {
                        int urlIndex = Urls.IndexOf(url) + 1;

                        // Find the next url to crawl, if it reaches the end, wrap it up
                        if (urlIndex < Urls.Count)
                            StartCrawling(Urls[urlIndex]);
                        else
                            FinishCrawling();
                    }
                    else
                    {
                        foreach (HtmlNode node in links.DistinctBy(l => l.GetAttributeValue("href", "NotFound")).ToList())
                        {
                            string link = node.GetAttributeValue("href", "NotFound");

                            if (link.StartsWith("mailto:") ||
                                link.StartsWith("tel:") ||
                                link.StartsWith("//") ||
                                link.Contains('#') || 
                                link.EndsWith(".pdf") ||
                                link.EndsWith(".jpg") ||
                                link.EndsWith(".jpeg") ||
                                link.EndsWith(".png") ||
                                link.EndsWith(".mp4"))
                                continue;

                            if (link.EndsWith('/'))
                                link = link.Remove(link.Length - 1);

                            // Preventing to navigate a page that already has been crawled through
                            if (link.StartsWith("http://"))
                                link = link.Replace("http://", "https://");

                            // Relative path
                            if (link.StartsWith("/"))
                                link = string.Concat(Url, link);

                            if (!Urls.Exists(u => u == link) && link.Contains(Domain))
                                Urls.Add(link);
                        }

                        string last = Urls.Last();
                        foreach (string u in Urls)
                        {
                            if (!Pages.Exists(p => p.Url == u))
                            {
                                StartCrawling(u);

                                break;
                            }

                            if (u.Equals(last))
                                FinishCrawling();
                        }
                    }
                }
                catch (WebException ex)
                {
                    HttpWebResponse response = ex.Response as HttpWebResponse;

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            int urlIndex = Urls.IndexOf(url) + 1;
                            StartCrawling(Urls[urlIndex]);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error bij pagina: " + url + "\nFoutmelding: " + ex.GetType().Name + "\nOmschrijving: " + ex.Message);
                    return;
                }
            }
        }

        private void FinishCrawling()
        {            
            SitemapDataGrid.ItemsSource = Pages;

            PageCountLabel.Content = Pages.Count.ToString();
            
            List<string> links = new();

            for (int i = 0; i < Pages.Count; i++)
            {
                links.Add(Pages[i].Url);
            }

            ExportButton.IsEnabled = true;
            MessageBox.Show("Website succesvol gecrawld!");
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            //TableTemplate exportWindow = new TableTemplate();

            //exportWindow.ShowDialog();

            List<string> queries = new();

            foreach(Page page in Pages)
            {
                string path = page.Url.Replace(Url, string.Empty);

                int segmentCount = path.Count(p => p == '/');

                // Todo: correct colomn names
                string insertQuery = string.Format("INSERT INTO {0} (`title`, ``)");

                queries.Add(insertQuery);
            }

            File.WriteAllLines("C:\\" + "insert_queries.sql", queries);
        }

        private void SitemapDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Page selectedItem = (Page)SitemapDataGrid.SelectedItem;

            // Todo: cross OS support (mac)
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = selectedItem.Url
            });
        }
    }
}
