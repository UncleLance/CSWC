﻿using System;
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

            Domain = UrlTextBox.Text.Split('/')[2];

            if (string.IsNullOrEmpty(Url))
            {
                MessageBox.Show("Gegeven url klopt niet! Controleer en probeer opnieuw.");
                return;
            }

            SitemapDataGrid.ItemsSource = Pages;

            Doc = new();

            StartCrawling(Url);
        }

        private void StartCrawling(string url)
        {
            using (WebClient wc = new())
            {
                StringBuilder htmlSb = new StringBuilder(276438, Int32.MaxValue);

                try
                {
                    // Todo : find fix for StackOverflowException
                    Html = wc.DownloadString(url);
                }
                catch (WebException ex)
                {
                    
                    HttpWebResponse error = ex.Response as HttpWebResponse;
                    if (error.StatusCode == HttpStatusCode.NotFound)
                    {
                        //MessageBoxResult dialog = MessageBox.Show("Error bij pagina: " + url + "\n404 niet gevonden!\nWil je doorgaan?", "404 niet gevonden", MessageBoxButton.YesNo);

                        //if (dialog == MessageBoxResult.Yes) 
                        //{
                            int urlIndex = Urls.IndexOf(url) + 1;

                            StartCrawling(Urls[urlIndex]);
                        //}
                        //if (dialog == MessageBoxResult.No)
                            //return;
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error bij pagina: " + url + "\nFoutmelding: " + ex.GetType().Name + "\nOmschrijving: " + ex.Message);
                    return;
                }

                Doc.LoadHtml(Html);

                // Get title if it exists
                string title = Doc.DocumentNode.SelectSingleNode("//title") != null ?
                                Doc.DocumentNode.SelectSingleNode("//title").InnerText : 
                                string.Empty;

                // Get meta description if it exists
                string description = Doc.DocumentNode.SelectSingleNode("//meta[@name='description']") != null ?
                                        Doc.DocumentNode.SelectSingleNode("//meta[@name='description']").GetAttributeValue("content", string.Empty) :
                                        string.Empty;

                HtmlNodeCollection links = Doc.DocumentNode.SelectNodes("//a");

                // If no links were found on the page, navigate the next page
                if (links == null) 
                {
                    int urlIndex = Urls.IndexOf(url) + 1;

                    StartCrawling(Urls[urlIndex]);
                }

                foreach(HtmlNode node in links.DistinctBy(l => l.GetAttributeValue("href", "NotFound")).ToList())
                {
                    string link = node.GetAttributeValue("href", "NotFound");

                    if (link.StartsWith("mailto:") || 
                        link.StartsWith("tel:") || 
                        link.EndsWith(".pdf") || 
                        link.EndsWith(".jpg") ||
                        link.EndsWith(".jpeg") ||
                        link.EndsWith(".png"))
                        continue;

                    if (link.StartsWith("http://"))
                        link = link.Replace("http://", "https://");

                    if (link.StartsWith("/"))
                        link = string.Concat("https://", link);

                    if (!Urls.Exists(u => u == link) && (link.StartsWith('/') || link.Contains(Domain)))
                        Urls.Add(link);
                }

                //Urls.ForEach(url =>
                //{
                //    if(!Pages.Exists(p =>  p.Url == url))
                //    {
                //        Pages.Add(new Page
                //        {
                //            Url = u,
                //            Title = title,
                //            Description = description
                //        });
                //        StarCrawling(u);
                //        break;
                //    }
                //});

                string last = Urls.Last();
                foreach (string u in Urls)
                {
                    if (!Pages.Exists(p => p.Url == u))
                    {
                        Pages.Add(new Page
                        {
                            Url = u,
                            Title = title,
                            Description = description
                        });
                        StartCrawling(u);
                        break;
                    }

                    if (u.Equals(last))
                    {
                        Urls.Clear();
                        FinishCrawling();
                    }
                }
            }
        }

        private void FinishCrawling()
        {
            List<string> links = new();

            for (int i = 0; i < Pages.Count; i++)
            {
                links.Add(Pages[i].Url);
            }

            File.WriteAllLines("C:\\Users\\Youssri\\Desktop\\" + "links2.txt", links);
            MessageBox.Show("Website succesvol gecrawld!");
        }

        /// <summary>
        /// Return the page content without new lines, carriage return or tabs.
        /// </summary>
        private string PageContent(string html)
        {
            StringBuilder sb = new(html.Length);

            foreach (char c in html)
            {
                if(c != '\n' && c != '\r' && c != '\t')
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}